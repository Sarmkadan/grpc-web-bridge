#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Services;

namespace GrpcWebBridge.BackgroundWorkers;

/// <summary>
/// Background worker that periodically cleans up idle and stale streams.
/// Prevents memory leaks from abandoned streaming connections.
/// Runs at configurable intervals to monitor stream health.
/// </summary>
public class StreamCleanupWorker : BackgroundService
{
    private readonly ILogger<StreamCleanupWorker> _logger;
    private readonly StreamingService _streamingService;
    private readonly StreamCleanupOptions _options;
    private int _totalCleanupsRun = 0;
    private int _totalStreamsRemoved = 0;

    public StreamCleanupWorker(
        ILogger<StreamCleanupWorker> logger,
        StreamingService streamingService,
        StreamCleanupOptions? options = null)
    {
        _logger = logger;
        _streamingService = streamingService;
        _options = options ?? new StreamCleanupOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stream cleanup worker started with interval {IntervalSeconds}s", _options.CleanupIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_options.CleanupIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when service is stopping
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stream cleanup");
                // Continue after error instead of stopping
            }
        }

        _logger.LogInformation("Stream cleanup worker stopped. Total cleanups: {Count}, Total streams removed: {Removed}",
            _totalCleanupsRun, _totalStreamsRemoved);
    }

    /// <summary>
    /// Performs stream cleanup cycle.
    /// Identifies and removes idle streams.
    /// </summary>
    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        _totalCleanupsRun++;
        var startTime = DateTime.UtcNow;

        try
        {
            var streamIds = _streamingService.GetAllStreamIds().ToList();
            var removedCount = 0;
            var staleCount = 0;
            var timeoutCount = 0;

            foreach (var streamId in streamIds)
            {
                try
                {
                    var stats = _streamingService.GetStreamStatistics(streamId);
                    if (stats is null)
                        continue;

                    var lastActivityTime = stats.LastActivityTime;
                    var idleDuration = startTime - lastActivityTime;

                    // Check if stream is idle beyond threshold
                    if (idleDuration > _options.IdleTimeoutDuration)
                    {
                        _logger.LogInformation(
                            "Removing idle stream: StreamId={StreamId}, IdleDuration={IdleSeconds}s",
                            streamId, idleDuration.TotalSeconds);

                        _streamingService.CloseStream(streamId);
                        removedCount++;
                        _totalStreamsRemoved++;
                    }

                    // Check if stream has been stale (no new messages)
                    if (stats.MessageCount == 0 && idleDuration > _options.StaleStreamDuration)
                    {
                        _logger.LogInformation(
                            "Removing stale stream: StreamId={StreamId}, Duration={DurationSeconds}s",
                            streamId, idleDuration.TotalSeconds);

                        _streamingService.CloseStream(streamId);
                        staleCount++;
                        _totalStreamsRemoved++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing stream for cleanup: StreamId={StreamId}", streamId);
                }
            }

            var duration = DateTime.UtcNow - startTime;

            if (removedCount > 0 || staleCount > 0 || timeoutCount > 0)
            {
                _logger.LogInformation(
                    "Stream cleanup completed: ScannedStreams={Total}, Removed={Removed}, Stale={Stale}, Duration={DurationMs}ms",
                    streamIds.Count, removedCount, staleCount, duration.TotalMilliseconds);
            }

            // Trigger garbage collection if many streams were removed
            if (removedCount + staleCount > _options.GcTriggerThreshold)
            {
                GC.Collect(0, GCCollectionMode.Optimized);
                _logger.LogDebug("Garbage collection triggered after stream cleanup");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in stream cleanup cycle");
        }
    }

    /// <summary>
    /// Gets cleanup statistics.
    /// </summary>
    public object GetStatistics()
    {
        return new
        {
            totalCleanupsRun = _totalCleanupsRun,
            totalStreamsRemoved = _totalStreamsRemoved,
            averageStreamsPerCleanup = _totalCleanupsRun > 0 ? _totalStreamsRemoved / (double)_totalCleanupsRun : 0,
            cleanupInterval = _options.CleanupIntervalSeconds,
            idleTimeout = _options.IdleTimeoutDuration.TotalSeconds,
            staleStreamTimeout = _options.StaleStreamDuration.TotalSeconds
        };
    }
}

/// <summary>
/// Configuration options for stream cleanup worker.
/// </summary>
public sealed class StreamCleanupOptions
{
    public int CleanupIntervalSeconds { get; set; } = 60;
    public TimeSpan IdleTimeoutDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan StaleStreamDuration { get; set; } = TimeSpan.FromMinutes(10);
    public int GcTriggerThreshold { get; set; } = 10;
}
