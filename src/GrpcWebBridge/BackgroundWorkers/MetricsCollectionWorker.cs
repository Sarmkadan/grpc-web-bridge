#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Controllers;
using GrpcWebBridge.Services;
using GrpcWebBridge.Domain;

namespace GrpcWebBridge.BackgroundWorkers;

/// <summary>
/// Background worker that periodically collects system and performance metrics.
/// Aggregates data about requests, responses, errors, and system resources.
/// Enables performance monitoring, trending, and alerting.
/// </summary>
public sealed class MetricsCollectionWorker : BackgroundService
{
    private readonly ILogger<MetricsCollectionWorker> _logger;
    private readonly MetricsCollectionOptions _options;
    private readonly MetricsSnapshot _currentSnapshot;
    private readonly Queue<MetricsSnapshot> _snapshotHistory;
    private readonly object _lockObject = new object();

    public override string ToString() => $"MetricsCollectionWorker {{ Timestamp = {_currentSnapshot.Timestamp}, CpuUsagePercent = {_currentSnapshot.CpuUsagePercent}, MemoryUsageMb = {_currentSnapshot.MemoryUsageMb}, ThreadCount = {_currentSnapshot.ThreadCount}, GcCollections = {_currentSnapshot.GcCollections}, RequestMetrics = {_currentSnapshot.RequestMetrics} }}";

    public MetricsCollectionWorker(
        ILogger<MetricsCollectionWorker> logger,
        MetricsCollectionOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new MetricsCollectionOptions();
        _currentSnapshot = new MetricsSnapshot { Timestamp = DateTime.UtcNow };
        _snapshotHistory = new Queue<MetricsSnapshot>(_options.MaxSnapshotsToKeep);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Metrics collection worker started with interval {IntervalSeconds}s", _options.CollectionIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetricsAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(_options.CollectionIntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during metrics collection");
            }
        }

        _logger.LogInformation("Metrics collection worker stopped");
    }

    /// <summary>
    /// Collects current system and application metrics.
    /// </summary>
    private async Task CollectMetricsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = new MetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = GetCpuUsage(),
                MemoryUsageMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
                ThreadCount = System.Diagnostics.Process.GetCurrentProcess().Threads.Count,
                GcCollections = new
                {
                    Gen0 = GC.CollectionCount(0),
                    Gen1 = GC.CollectionCount(1),
                    Gen2 = GC.CollectionCount(2)
                },
                RequestMetrics = new MetricsSnapshot.RequestMetricsData
                {
                    TotalRequests = MetricsController._totalRequests,
                    TotalErrors = MetricsController._totalErrors,
                    ErrorRate = MetricsController._totalRequests > 0
                        ? Math.Round((MetricsController._totalErrors / (double)MetricsController._totalRequests) * 100, 2)
                        : 0
                }
            };

            lock (_lockObject)
            {
                _snapshotHistory.Enqueue(snapshot);

                // Keep only the specified number of snapshots
                while (_snapshotHistory.Count > _options.MaxSnapshotsToKeep)
                {
                    _snapshotHistory.Dequeue();
                }
            }

            _logger.LogDebug("Metrics snapshot collected: CPU={CPU}%, Memory={Memory}MB",
                snapshot.CpuUsagePercent, Math.Round(snapshot.MemoryUsageMb, 2));

            // Check thresholds and trigger alerts
            await CheckAlertsAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting metrics snapshot");
        }
    }

    /// <summary>
    /// Checks if metrics exceed alert thresholds.
    /// </summary>
    private async Task CheckAlertsAsync(MetricsSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (snapshot.CpuUsagePercent > _options.CpuAlertThresholdPercent)
        {
            _logger.LogWarning("High CPU usage detected: {Usage}%", snapshot.CpuUsagePercent);
        }

        if (snapshot.MemoryUsageMb > _options.MemoryAlertThresholdMb)
        {
            _logger.LogWarning("High memory usage detected: {Usage}MB", Math.Round(snapshot.MemoryUsageMb, 2));
        }

        if (snapshot.RequestMetrics.ErrorRate > _options.ErrorRateAlertThresholdPercent)
        {
            _logger.LogWarning("High error rate detected: {ErrorRate}%", snapshot.RequestMetrics.ErrorRate);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets aggregated metrics for a time period.
    /// </summary>
    public object GetAggregatedMetrics(int lastMinutes = 5)
    {
        lock (_lockObject)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-lastMinutes);
            var relevantSnapshots = _snapshotHistory
                .Where(s => s.Timestamp >= cutoffTime)
                .ToList();

            if (relevantSnapshots.Count == 0)
                return new { message = "No metrics data available for the specified time period" };

            return new
            {
                period = $"Last {lastMinutes} minutes",
                snapshotCount = relevantSnapshots.Count,
                cpu = new
                {
                    average = Math.Round(relevantSnapshots.Average(s => s.CpuUsagePercent), 2),
                    min = Math.Round(relevantSnapshots.Min(s => s.CpuUsagePercent), 2),
                    max = Math.Round(relevantSnapshots.Max(s => s.CpuUsagePercent), 2)
                },
                memory = new
                {
                    average = Math.Round(relevantSnapshots.Average(s => s.MemoryUsageMb), 2),
                    min = Math.Round(relevantSnapshots.Min(s => s.MemoryUsageMb), 2),
                    max = Math.Round(relevantSnapshots.Max(s => s.MemoryUsageMb), 2)
                },
                threads = new
                {
                    average = Math.Round(relevantSnapshots.Average(s => s.ThreadCount), 0),
                    min = relevantSnapshots.Min(s => s.ThreadCount),
                    max = relevantSnapshots.Max(s => s.ThreadCount)
                },
                latestRequests = relevantSnapshots.Last().RequestMetrics
            };
        }
    }

    /// <summary>
    /// Gets the full snapshot history.
    /// </summary>
    public List<MetricsSnapshot> GetSnapshotHistory()
    {
        lock (_lockObject)
        {
            return _snapshotHistory.ToList();
        }
    }

    /// <summary>
    /// Clears snapshot history.
    /// </summary>
    public void ClearHistory()
    {
        lock (_lockObject)
        {
            _snapshotHistory.Clear();
        }

        _logger.LogInformation("Metrics history cleared");
    }

    private static double GetCpuUsage()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            return Math.Round(process.TotalProcessorTime.TotalSeconds, 2);
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// A single metrics snapshot.
/// </summary>
public sealed class MetricsSnapshot
{
    /// <summary>
    /// The timestamp of the snapshot.
    /// </summary>
    public DateTime Timestamp { get; set; }
    /// <summary>
    /// The CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; set; }
    /// <summary>
    /// The memory usage in megabytes.
    /// </summary>
    public double MemoryUsageMb { get; set; }
    /// <summary>
    /// The number of threads.
    /// </summary>
    public int ThreadCount { get; set; }
    /// <summary>
    /// The garbage collection counts per generation.
    /// </summary>
    public object? GcCollections { get; set; }
    /// <summary>
    /// The request metrics data.
    /// </summary>
    public RequestMetricsData? RequestMetrics { get; set; }

    /// <summary>
    /// Returns a string representation of the snapshot.
    /// </summary>
    /// <returns>A string containing Timestamp, CpuUsagePercent, MemoryUsageMb, ThreadCount, and RequestMetrics.</returns>
    public override string ToString()
    {
        return $"MetricsSnapshot {{ Timestamp = {Timestamp}, CpuUsagePercent = {CpuUsagePercent}, MemoryUsageMb = {MemoryUsageMb}, ThreadCount = {ThreadCount}, RequestMetrics = {RequestMetrics} }}";
    }

    /// <summary>
    /// Request metrics data collected during the snapshot period.
    /// </summary>
    public sealed class RequestMetricsData
    {
        /// <summary>
        /// Total number of requests processed.
        /// </summary>
        public long TotalRequests { get; set; }
        /// <summary>
        /// Total number of requests that resulted in errors.
        /// </summary>
        public long TotalErrors { get; set; }
        /// <summary>
        /// The error rate as a percentage (0-100).
        /// </summary>
        public double ErrorRate { get; set; }

        /// <summary>
        /// Returns a string representation of the request metrics.
        /// </summary>
        /// <returns>A string containing TotalRequests, TotalErrors, and ErrorRate.</returns>
        public override string ToString()
        {
            return $"RequestMetricsData {{ TotalRequests = {TotalRequests}, TotalErrors = {TotalErrors}, ErrorRate = {ErrorRate} }}";
        }
    }
}

/// <summary>
/// Configuration options for metrics collection worker.
/// </summary>
public sealed class MetricsCollectionOptions
{
    /// <summary>
    /// The interval in seconds between metric collections.
    /// </summary>
    public int CollectionIntervalSeconds { get; set; } = 30;
    /// <summary>
    /// The maximum number of snapshots to keep in history.
    /// </summary>
    public int MaxSnapshotsToKeep { get; set; } = Constants.ServiceRegistry.MaxCachedServices; // Using an existing constant as a default for this option
    /// <summary>
    /// The CPU usage percentage threshold for triggering alerts.
    /// </summary>
    public double CpuAlertThresholdPercent { get; set; } = 80;
    /// <summary>
    /// The memory usage in MB threshold for triggering alerts.
    /// </summary>
    public double MemoryAlertThresholdMb { get; set; } = 1024;
    /// <summary>
    /// The error rate percentage threshold for triggering alerts.
    /// </summary>
    public double ErrorRateAlertThresholdPercent { get; set; } = 5;
}
