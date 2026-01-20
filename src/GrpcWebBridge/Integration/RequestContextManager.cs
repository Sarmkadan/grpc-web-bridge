#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Manages request context across async operations.
/// Provides ambient context for request-scoped data without explicit parameter passing.
/// Enables correlation logging and cross-cutting concerns.
/// </summary>
public sealed class RequestContextManager
{
    private static readonly AsyncLocal<RequestContext> _context = new();
    private readonly ILogger<RequestContextManager> _logger;

    public RequestContextManager(ILogger<RequestContextManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates and sets a new request context.
    /// </summary>
    public RequestContext CreateContext(
        string requestId,
        string? userId = null,
        Dictionary<string, string>? metadata = null)
    {
        var context = new RequestContext
        {
            RequestId = requestId,
            UserId = userId,
            StartTime = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        _context.Value = context;
        _logger.LogDebug("Request context created: RequestId={RequestId}, UserId={UserId}",
            requestId, userId ?? "anonymous");

        return context;
    }

    /// <summary>
    /// Gets the current request context.
    /// Returns null if no context is set.
    /// </summary>
    public RequestContext? GetContext()
    {
        return _context.Value;
    }

    /// <summary>
    /// Gets the current request ID.
    /// </summary>
    public string? GetRequestId()
    {
        return _context.Value?.RequestId;
    }

    /// <summary>
    /// Gets the current user ID.
    /// </summary>
    public string? GetUserId()
    {
        return _context.Value?.UserId;
    }

    /// <summary>
    /// Sets metadata for the current request.
    /// </summary>
    public void SetMetadata(string key, string value)
    {
        if (_context.Value is null)
        {
            _logger.LogWarning("No request context available for metadata storage");
            return;
        }

        _context.Value.Metadata[key] = value;
    }

    /// <summary>
    /// Gets metadata from the current request.
    /// </summary>
    public string? GetMetadata(string key)
    {
        if (_context.Value is null)
            return null;

        return _context.Value.Metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Records the elapsed time for the request.
    /// Should be called when request completes.
    /// </summary>
    public void RecordElapsedTime()
    {
        if (_context.Value is null)
            return;

        _context.Value.EndTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Request completed: RequestId={RequestId}, Duration={DurationMs}ms, UserId={UserId}",
            _context.Value.RequestId,
            _context.Value.ElapsedMilliseconds,
            _context.Value.UserId ?? "anonymous");
    }

    /// <summary>
    /// Clears the current request context.
    /// Should be called when request processing is complete.
    /// </summary>
    public void Clear()
    {
        var context = _context.Value;
        _context.Value = null;

        if (context is not null)
        {
            _logger.LogDebug("Request context cleared: RequestId={RequestId}", context.RequestId);
        }
    }

    /// <summary>
    /// Checks if a request context is currently active.
    /// </summary>
    public bool IsContextActive()
    {
        return _context.Value is not null;
    }
}

/// <summary>
/// Request context information.
/// </summary>
public sealed class RequestContext
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string? UserId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();

    public long ElapsedMilliseconds =>
        EndTime.HasValue ? (long)(EndTime.Value - StartTime).TotalMilliseconds : -1;

    public string? GetMetadata(string key) =>
        Metadata.TryGetValue(key, out var value) ? value : null;

    public override string ToString() =>
        $"RequestId={RequestId}, UserId={UserId ?? "anonymous"}, Duration={ElapsedMilliseconds}ms";
}

/// <summary>
/// Middleware to manage request context for all requests.
/// </summary>
public sealed class RequestContextMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestContextManager _contextManager;
    private readonly ILogger<RequestContextMiddleware> _logger;

    public RequestContextMiddleware(
        RequestDelegate next,
        RequestContextManager contextManager,
        ILogger<RequestContextMiddleware> logger)
    {
        _next = next;
        _contextManager = contextManager;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Generate or extract request ID
        var requestId = httpContext.Request.Headers.TryGetValue("X-Request-ID", out var headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        // Extract user ID from claims if available
        var userId = httpContext.User?.FindFirst("sub")?.Value
            ?? httpContext.User?.Identity?.Name;

        // Create request context
        var context = _contextManager.CreateContext(requestId, userId);

        // Add request ID to response headers
        httpContext.Response.Headers.Add("X-Request-ID", requestId);

        try
        {
            await _next(httpContext);
        }
        finally
        {
            _contextManager.RecordElapsedTime();
            _contextManager.Clear();
        }
    }
}

/// <summary>
/// Extension methods for request context.
/// </summary>
public static class RequestContextExtensions
{
    /// <summary>
    /// Gets the request context from HTTP context.
    /// </summary>
    public static RequestContext? GetRequestContext(this HttpContext httpContext)
    {
        return httpContext.Items.TryGetValue("RequestContext", out var context)
            ? context as RequestContext
            : null;
    }

    /// <summary>
    /// Sets the request context in HTTP context.
    /// </summary>
    public static void SetRequestContext(this HttpContext httpContext, RequestContext context)
    {
        httpContext.Items["RequestContext"] = context;
    }

    /// <summary>
    /// Registers request context middleware.
    /// </summary>
    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestContextMiddleware>();
    }

    /// <summary>
    /// Registers request context manager as a service.
    /// </summary>
    public static IServiceCollection AddRequestContextManager(this IServiceCollection services)
    {
        return services.AddScoped<RequestContextManager>();
    }
}
