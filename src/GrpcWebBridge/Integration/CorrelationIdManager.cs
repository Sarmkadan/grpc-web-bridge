#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Correlation ID management for distributed tracing.
/// Enables request tracking across multiple services and components.
/// Integrates with logging for comprehensive request visibility.
/// </summary>
public sealed class CorrelationIdManager
{
    private static readonly AsyncLocal<string?> _correlationId = new();
    private readonly ILogger<CorrelationIdManager> _logger;
    private readonly ConcurrentDictionary<string, CorrelationTrace> _traces;

    public CorrelationIdManager(ILogger<CorrelationIdManager> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _traces = new ConcurrentDictionary<string, CorrelationTrace>();
    }

    /// <summary>
    /// Gets the current correlation ID.
    /// Creates a new one if not set.
    /// </summary>
    public string GetOrCreateCorrelationId()
    {
        if (!string.IsNullOrEmpty(_correlationId.Value))
            return _correlationId.Value;

        var id = Guid.NewGuid().ToString();
        _correlationId.Value = id;
        return id;
    }

    /// <summary>
    /// Sets the correlation ID.
    /// </summary>
    public void SetCorrelationId(string? correlationId)
        {
            if (string.IsNullOrEmpty(correlationId))
                throw new ArgumentException("Correlation ID cannot be null or empty", nameof(correlationId));
            _correlationId.Value = correlationId;
        }

    /// <summary>
    /// Gets the current correlation ID without creating a new one.
    /// </summary>
    public string? GetCorrelationId()
    {
        return _correlationId.Value;
    }

    /// <summary>
    /// Starts tracking a correlated operation.
    /// </summary>
    public CorrelationTrace StartTrace(
        string operationName,
        string? parentTraceId = null,
        Dictionary<string, string>? metadata = null)
    {
        var correlationId = GetOrCreateCorrelationId();
        var traceId = Guid.NewGuid().ToString();

        var trace = new CorrelationTrace
        {
            TraceId = traceId,
            CorrelationId = correlationId,
            OperationName = operationName,
            ParentTraceId = parentTraceId,
            StartTime = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        _traces.TryAdd(traceId, trace);

        _logger.LogInformation(
            "Trace started: TraceId={TraceId}, CorrelationId={CorrelationId}, Operation={Operation}",
            traceId, correlationId, operationName);

        return trace;
    }

    /// <summary>
    /// Completes a trace and records timing information.
    /// </summary>
    public void CompleteTrace(string traceId, bool success = true, string? errorMessage = null)
    {
        if (string.IsNullOrEmpty(traceId))
            return;

        if (_traces.TryGetValue(traceId, out var trace))
        {
            trace.EndTime = DateTime.UtcNow;
            trace.Success = success;
            trace.ErrorMessage = errorMessage;

            var duration = trace.EndTime.Value - trace.StartTime;

            _logger.LogInformation(
                "Trace completed: TraceId={TraceId}, CorrelationId={CorrelationId}, Duration={DurationMs}ms, Success={Success}",
                traceId, trace.CorrelationId, duration.TotalMilliseconds, success);
        }
    }

    /// <summary>
    /// Gets a trace by ID.
    /// </summary>
    public CorrelationTrace? GetTrace(string traceId)
    {
        if (string.IsNullOrEmpty(traceId))
            return null;

        _traces.TryGetValue(traceId, out var trace);
        return trace;
    }

    /// <summary>
    /// Gets all traces for a correlation ID.
    /// </summary>
    public List<CorrelationTrace> GetTracesForCorrelation(string correlationId)
    {
        if (string.IsNullOrEmpty(correlationId))
            return new List<CorrelationTrace>();

        return _traces.Values
            .Where(t => t.CorrelationId == correlationId)
            .OrderBy(t => t.StartTime)
            .ToList();
    }

    /// <summary>
    /// Adds metadata to a trace.
    /// </summary>
    public void AddTraceMetadata(string traceId, string key, string value)
    {
        if (string.IsNullOrEmpty(traceId) || string.IsNullOrEmpty(key))
            return;

        if (_traces.TryGetValue(traceId, out var trace))
        {
            trace.Metadata[key] = value;
        }
    }

    /// <summary>
    /// Gets correlation statistics.
    /// </summary>
    public object GetStatistics()
    {
        var allTraces = _traces.Values.ToList();
        var completedTraces = allTraces.Where(t => t.EndTime.HasValue).ToList();

        return new
        {
            totalTraces = allTraces.Count,
            completedTraces = completedTraces.Count,
            activeTraces = allTraces.Count - completedTraces.Count,
            successfulTraces = completedTraces.Count(t => t.Success),
            failedTraces = completedTraces.Count(t => !t.Success),
            averageDurationMs = completedTraces.Count > 0
                ? Math.Round(completedTraces.Average(t => (t.EndTime!.Value - t.StartTime).TotalMilliseconds), 2)
                : 0,
            totalUniqueCorrelations = allTraces.Select(t => t.CorrelationId).Distinct().Count()
        };
    }

    /// <summary>
    /// Clears completed traces older than the specified duration.
    /// </summary>
    public int CleanupOldTraces(TimeSpan olderThan)
    {
        var cutoffTime = DateTime.UtcNow - olderThan;
        var keysToRemove = _traces
            .Where(kvp => kvp.Value.EndTime.HasValue && kvp.Value.EndTime < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _traces.TryRemove(key, out _);
        }

        return keysToRemove.Count;
    }

    /// <summary>
    /// Clears all traces.
    /// </summary>
    public void ClearAllTraces()
    {
        _traces.Clear();
    }

    /// <summary>
    /// Clears the current correlation ID.
    /// </summary>
    public void ClearCorrelationId()
    {
        _correlationId.Value = null;
    }
}

/// <summary>
/// Represents a correlation trace for a single operation.
/// </summary>
public sealed class CorrelationTrace
{
    public string TraceId { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public string? ParentTraceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    public TimeSpan? GetDuration() =>
        EndTime.HasValue ? EndTime.Value - StartTime : null;
}

/// <summary>
/// Middleware for managing correlation IDs.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly CorrelationIdManager _correlationIdManager;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(
        RequestDelegate next,
        CorrelationIdManager correlationIdManager,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _correlationIdManager = correlationIdManager;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Extract or create correlation ID
        var correlationId = httpContext.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        _correlationIdManager.SetCorrelationId(correlationId);

        // Add to response headers
        httpContext.Response.Headers.Add(CorrelationIdHeader, correlationId);

        try
        {
            await _next(httpContext).ConfigureAwait(false);
        }
        finally
        {
            _correlationIdManager.ClearCorrelationId();
        }
    }
}

/// <summary>
/// Extension methods for correlation ID management.
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Registers correlation ID middleware.
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }

    /// <summary>
    /// Registers correlation ID manager as a service.
    /// </summary>
    public static IServiceCollection AddCorrelationIdManager(this IServiceCollection services)
    {
        return services.AddScoped<CorrelationIdManager>();
    }
}
