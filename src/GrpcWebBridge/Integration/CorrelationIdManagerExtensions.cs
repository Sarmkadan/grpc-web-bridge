#nullable enable

using System.Diagnostics;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Extension methods for <see cref="CorrelationIdManager"/> that provide additional functionality
/// for working with correlation IDs and traces.
/// </summary>
public static class CorrelationIdManagerExtensions
{
    /// <summary>
    /// Checks if a correlation ID is currently set.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <returns>True if a correlation ID is set; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static bool HasCorrelationId(this CorrelationIdManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        return !string.IsNullOrEmpty(manager.GetCorrelationId());
    }

    /// <summary>
    /// Gets the duration of a trace if it has been completed.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <param name="traceId">The trace ID to get duration for.</param>
    /// <returns>The duration of the trace, or null if the trace is not completed or doesn't exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static TimeSpan? GetTraceDuration(this CorrelationIdManager manager, string traceId)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (string.IsNullOrEmpty(traceId))
            return null;

        var trace = manager.GetTrace(traceId);
        return trace?.GetDuration();
    }

    /// <summary>
    /// Checks if a trace exists and is successful.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <param name="traceId">The trace ID to check.</param>
    /// <returns>True if the trace exists and is successful; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static bool IsTraceSuccessful(this CorrelationIdManager manager, string traceId)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (string.IsNullOrEmpty(traceId))
            return false;

        var trace = manager.GetTrace(traceId);
        return trace?.Success == true;
    }

    /// <summary>
    /// Gets the error message for a trace if it failed.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <param name="traceId">The trace ID to get error for.</param>
    /// <returns>The error message if the trace failed; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static string? GetTraceError(this CorrelationIdManager manager, string traceId)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (string.IsNullOrEmpty(traceId))
            return null;

        var trace = manager.GetTrace(traceId);
        return trace?.Success == false ? trace.ErrorMessage : null;
    }

    /// <summary>
    /// Starts a trace with automatic correlation ID handling.
    /// If no correlation ID is set, one will be created automatically.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <param name="operationName">The name of the operation being traced.</param>
    /// <param name="metadata">Optional metadata to associate with the trace.</param>
    /// <returns>The created trace.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="manager"/> is null or when <paramref name="operationName"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="operationName"/> is empty.</exception>
    public static CorrelationTrace StartTraceWithAutoCorrelation(this CorrelationIdManager manager, string operationName, Dictionary<string, string>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(manager);

        ArgumentException.ThrowIfNullOrEmpty(operationName);

        // Ensure correlation ID exists
        manager.GetOrCreateCorrelationId();

        return manager.StartTrace(operationName, metadata: metadata);
    }

    /// <summary>
    /// Gets statistics formatted as a string for easy reading.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <returns>A formatted string containing correlation statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static string GetStatisticsFormatted(this CorrelationIdManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var stats = manager.GetStatistics();

        // Use dynamic to avoid reflection for anonymous type access
        dynamic statsDynamic = stats;
        return $$"""
        Correlation Statistics:
        - Total Traces: {statsDynamic.totalTraces}
        - Completed: {statsDynamic.completedTraces}
        - Active: {statsDynamic.activeTraces}
        - Successful: {statsDynamic.successfulTraces}
        - Failed: {statsDynamic.failedTraces}
        - Avg Duration: {statsDynamic.averageDurationMs}ms
        - Unique Correlations: {statsDynamic.totalUniqueCorrelations}
        """;
    }

    /// <summary>
    /// Cleans up old traces using a default retention period of 24 hours.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <returns>The number of traces that were removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static int CleanupOldTraces(this CorrelationIdManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        // Default retention: 24 hours
        return manager.CleanupOldTraces(TimeSpan.FromHours(24));
    }

    /// <summary>
    /// Gets all traces for the current correlation ID.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <returns>A list of traces for the current correlation ID.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static List<CorrelationTrace> GetCurrentTraces(this CorrelationIdManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var correlationId = manager.GetCorrelationId();
        if (string.IsNullOrEmpty(correlationId))
            return [];

        return manager.GetTracesForCorrelation(correlationId);
    }

    /// <summary>
    /// Checks if any traces exist for the current correlation ID.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <returns>True if traces exist for the current correlation ID; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static bool HasTraces(this CorrelationIdManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var correlationId = manager.GetCorrelationId();
        if (string.IsNullOrEmpty(correlationId))
            return false;

        return manager.GetTracesForCorrelation(correlationId).Count > 0;
    }

    /// <summary>
    /// Gets the most recent trace for the current correlation ID.
    /// </summary>
    /// <param name="manager">The correlation ID manager instance.</param>
    /// <returns>The most recent trace, or null if none exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> is null.</exception>
    public static CorrelationTrace? GetMostRecentTrace(this CorrelationIdManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var correlationId = manager.GetCorrelationId();
        if (string.IsNullOrEmpty(correlationId))
            return null;

        var traces = manager.GetTracesForCorrelation(correlationId);
        return traces.OrderByDescending(t => t.StartTime).FirstOrDefault();
    }
}