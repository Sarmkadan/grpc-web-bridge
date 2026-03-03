#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace GrpcWebBridge.BackgroundWorkers;

/// <summary>
/// Extension methods for <see cref="MetricsCollectionWorker"/> providing additional utility functionality
/// for metrics analysis, filtering, and reporting.
/// </summary>
public static class MetricsCollectionWorkerExtensions
{
    /// <summary>
    /// Filters snapshot history by timestamp range.
    /// </summary>
    /// <param name="worker">The metrics collection worker instance.</param>
    /// <param name="startTime">Start of time range (inclusive).</param>
    /// <param name="endTime">End of time range (inclusive).</param>
    /// <exception cref="ArgumentNullException"><paramref name="worker"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startTime"/> is after <paramref name="endTime"/>.</exception>
    /// <returns>Filtered list of snapshots within the specified time range.</returns>
    public static List<MetricsSnapshot> GetSnapshotsInRange(this MetricsCollectionWorker worker, DateTime startTime, DateTime endTime)
    {
        ArgumentNullException.ThrowIfNull(worker);

        if (startTime > endTime)
            throw new ArgumentOutOfRangeException(nameof(startTime), "Start time must be before or equal to end time.");

        var history = worker.GetSnapshotHistory();
        return history
            .Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime)
            .OrderBy(s => s.Timestamp)
            .ToList();
    }

    /// <summary>
    /// Calculates peak usage statistics for CPU, memory, and threads.
    /// </summary>
    /// <param name="worker">The metrics collection worker instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="worker"/> is <see langword="null"/>.</exception>
    /// <returns>An object containing peak usage values and timestamps.</returns>
    public static object GetPeakUsageStatistics(this MetricsCollectionWorker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);

        var history = worker.GetSnapshotHistory();

        if (history.Count == 0)
            return new { message = "No snapshot history available" };

        var peakCpu = history.MaxBy(s => s.CpuUsagePercent);
        var peakMemory = history.MaxBy(s => s.MemoryUsageMb);
        var peakThreads = history.MaxBy(s => s.ThreadCount);

        return new
        {
            peakCpu = new
            {
                value = peakCpu.CpuUsagePercent,
                timestamp = peakCpu.Timestamp,
                memoryAtPeak = peakCpu.MemoryUsageMb,
                threadsAtPeak = peakCpu.ThreadCount
            },
            peakMemory = new
            {
                value = peakMemory.MemoryUsageMb,
                timestamp = peakMemory.Timestamp,
                cpuAtPeak = peakMemory.CpuUsagePercent,
                threadsAtPeak = peakMemory.ThreadCount
            },
            peakThreads = new
            {
                value = peakThreads.ThreadCount,
                timestamp = peakThreads.Timestamp,
                cpuAtPeak = peakThreads.CpuUsagePercent,
                memoryAtPeak = peakThreads.MemoryUsageMb
            }
        };
    }

    /// <summary>
    /// Gets trend analysis showing whether metrics are improving or degrading over time.
    /// </summary>
    /// <param name="worker">The metrics collection worker instance.</param>
    /// <param name="minutes">Time window in minutes to analyze (must be positive).</param>
    /// <exception cref="ArgumentNullException"><paramref name="worker"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minutes"/> is not positive.</exception>
    /// <returns>Trend analysis with slopes and direction indicators.</returns>
    public static object GetTrendAnalysis(this MetricsCollectionWorker worker, int minutes = 30)
    {
        ArgumentNullException.ThrowIfNull(worker);

        if (minutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(minutes), "Minutes must be positive.");

        var cutoffTime = DateTime.UtcNow.AddMinutes(-minutes);
        var recentHistory = worker.GetSnapshotHistory()
            .Where(s => s.Timestamp >= cutoffTime)
            .OrderBy(s => s.Timestamp)
            .ToList();

        if (recentHistory.Count < 2)
            return new { message = "Insufficient data for trend analysis" };

        // Calculate linear regression slopes
        var cpuValues = recentHistory.Select((s, i) => (X: (double)i, Y: s.CpuUsagePercent)).ToList();
        var memoryValues = recentHistory.Select((s, i) => (X: (double)i, Y: s.MemoryUsageMb)).ToList();
        var errorValues = recentHistory.Select((s, i) => (X: (double)i, Y: s.RequestMetrics?.ErrorRate ?? 0.0)).ToList();

        double cpuSlope = LinearRegressionHelper.CalculateSlope(cpuValues);
        double memorySlope = LinearRegressionHelper.CalculateSlope(memoryValues);
        double errorSlope = LinearRegressionHelper.CalculateSlope(errorValues);

        return new
        {
            timeWindow = $"Last {minutes} minutes",
            dataPoints = recentHistory.Count,
            cpuTrend = new
            {
                slope = Math.Round(cpuSlope, 4),
                direction = cpuSlope > 0.5 ? "Increasing" : cpuSlope < -0.5 ? "Decreasing" : "Stable",
                trendDescription = cpuSlope > 0.5 ? "CPU usage is trending upward" :
                                  cpuSlope < -0.5 ? "CPU usage is trending downward" :
                                  "CPU usage is stable"
            },
            memoryTrend = new
            {
                slope = Math.Round(memorySlope, 4),
                direction = memorySlope > 0.5 ? "Increasing" : memorySlope < -0.5 ? "Decreasing" : "Stable",
                trendDescription = memorySlope > 0.5 ? "Memory usage is trending upward" :
                                  memorySlope < -0.5 ? "Memory usage is trending downward" :
                                  "Memory usage is stable"
            },
            errorRateTrend = new
            {
                slope = Math.Round(errorSlope, 4),
                direction = errorSlope > 0.5 ? "Increasing" : errorSlope < -0.5 ? "Decreasing" : "Stable",
                trendDescription = errorSlope > 0.5 ? "Error rate is trending upward" :
                                  errorSlope < -0.5 ? "Error rate is trending downward" :
                                  "Error rate is stable"
            }
        };
    }

    /// <summary>
    /// Gets alert summary showing current alert conditions and recent alerts.
    /// </summary>
    /// <param name="worker">The metrics collection worker instance.</param>
    /// <param name="lookbackMinutes">How far back to check for alerts (must be positive).</param>
    /// <exception cref="ArgumentNullException"><paramref name="worker"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lookbackMinutes"/> is not positive.</exception>
    /// <returns>Summary of alert conditions and recent alerts.</returns>
    public static object GetAlertSummary(this MetricsCollectionWorker worker, int lookbackMinutes = 60)
    {
        ArgumentNullException.ThrowIfNull(worker);

        if (lookbackMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(lookbackMinutes), "Lookback minutes must be positive.");

        var cutoffTime = DateTime.UtcNow.AddMinutes(-lookbackMinutes);
        var history = worker.GetSnapshotHistory()
            .Where(s => s.Timestamp >= cutoffTime)
            .OrderBy(s => s.Timestamp)
            .ToList();

        var alerts = new List<object>();
        int cpuAlerts = 0;
        int memoryAlerts = 0;
        int errorRateAlerts = 0;

        // Get alert thresholds from options
        double cpuThreshold = GetNestedAverage(worker.GetAggregatedMetrics(60), "cpu") ?? 80.0;
        double memoryThreshold = GetNestedAverage(worker.GetAggregatedMetrics(60), "memory") ?? 1024.0;

        double errorRateThreshold = 5.0;

        foreach (var snapshot in history)
        {
            if (snapshot.CpuUsagePercent > cpuThreshold)
            {
                alerts.Add(new
                {
                    timestamp = snapshot.Timestamp,
                    type = "CPU",
                    value = snapshot.CpuUsagePercent,
                    threshold = cpuThreshold,
                    severity = "Warning"
                });
                cpuAlerts++;
            }

            if (snapshot.MemoryUsageMb > memoryThreshold)
            {
                alerts.Add(new
                {
                    timestamp = snapshot.Timestamp,
                    type = "Memory",
                    value = Math.Round(snapshot.MemoryUsageMb, 2),
                    threshold = memoryThreshold,
                    severity = "Warning"
                });
                memoryAlerts++;
            }

            if (snapshot.RequestMetrics?.ErrorRate > errorRateThreshold)
            {
                alerts.Add(new
                {
                    timestamp = snapshot.Timestamp,
                    type = "ErrorRate",
                    value = snapshot.RequestMetrics.ErrorRate,
                    threshold = errorRateThreshold,
                    severity = "Warning"
                });
                errorRateAlerts++;
            }
        }

        return new
        {
            lookbackPeriod = $"Last {lookbackMinutes} minutes",
            totalSnapshots = history.Count,
            alertsFound = alerts.Count,
            alertBreakdown = new
            {
                cpuAlerts,
                memoryAlerts,
                errorRateAlerts
            },
            recentAlerts = alerts.OrderByDescending(a => ((dynamic)a).timestamp).Take(10).ToList(),
            isHealthy = alerts.Count == 0
        };
    }

    /// <summary>
    /// Safely reads a nested "average" value (e.g. metrics.cpu.average) from an
    /// aggregated metrics result without throwing when the expected shape is absent.
    /// </summary>
    private static double? GetNestedAverage(object metrics, string categoryName)
    {
        var categoryProperty = metrics.GetType().GetProperty(categoryName);
        var categoryValue = categoryProperty?.GetValue(metrics);
        if (categoryValue is null)
            return null;

        var averageProperty = categoryValue.GetType().GetProperty("average");
        var averageValue = averageProperty?.GetValue(categoryValue);
        return averageValue is null
            ? null
            : Convert.ToDouble(averageValue, System.Globalization.CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// Helper class for linear regression calculations.
/// </summary>
internal static class LinearRegressionHelper
{
    public static double CalculateSlope(List<(double X, double Y)> dataPoints)
    {
        if (dataPoints.Count < 2)
            return 0;

        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
        int n = dataPoints.Count;

        foreach (var point in dataPoints)
        {
            double x = point.X;
            double y = point.Y;
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        return slope;
    }
}
