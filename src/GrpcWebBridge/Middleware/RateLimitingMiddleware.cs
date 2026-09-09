#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Net;

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Rate limiting middleware using token bucket algorithm.
/// Enforces per-IP and global rate limits to protect against abuse.
/// Uses sliding window approach for accurate rate calculation.
/// </summary>
public sealed partial class RateLimitingMiddleware : IDisposable
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, ClientRateLimit> _clientLimits;
    private readonly Timer _cleanupTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingMiddleware"/> class.
    /// </summary>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitingOptions options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _next = next;
        _logger = logger;
        _options = options;
        _clientLimits = new ConcurrentDictionary<string, ClientRateLimit>();

        // Periodically clean up old entries to prevent memory leaks
        _cleanupTimer = new Timer(CleanupOldEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Processes an HTTP request and applies the configured rate limits.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        LogProcessingRequest(_logger, context.Request.Path);

        // Skip rate limiting for exempt paths
        foreach (var exemptPath in _options.ExemptPaths)
        {
            if (context.Request.Path.StartsWithSegments(exemptPath))
            {
                await _next(context);
                LogRequestProcessed(_logger, context.Request.Path);
                return;
            }
        }

        var clientIp = GetClientIpAddress(context);
        var clientKey = clientIp;

        var clientLimit = _clientLimits.GetOrAdd(clientKey, _ => new ClientRateLimit());

        // Check if client has exceeded rate limit
        if (!clientLimit.AllowRequest(_options.RequestsPerSecond, _options.WindowSizeSeconds))
        {
            LogRateLimitExceeded(_logger, clientIp, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers["Retry-After"] = _options.RetryAfterSeconds.ToString();

            var response = new
            {
                error = "Rate Limit Exceeded",
                message = $"Too many requests. Maximum {_options.RequestsPerSecond} requests per {_options.WindowSizeSeconds} seconds allowed",
                retryAfter = _options.RetryAfterSeconds,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        // Check global rate limit
        if (_options.EnableGlobalLimit && !CheckGlobalRateLimit(clientLimit))
        {
            LogGlobalRateLimitExceeded(_logger);

            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Service Overloaded",
                message = "The service is currently overloaded. Please try again later.",
                retryAfter = _options.RetryAfterSeconds,
                timestamp = DateTime.UtcNow
            };

            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        await _next(context);
        LogRequestProcessed(_logger, context.Request.Path);
    }

    /// <summary>
    /// Releases the resources used by the middleware.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }

    /// <summary>
    /// Extracts the client's IP address from the request.
    /// Handles X-Forwarded-For headers for requests behind proxies.
    /// </summary>
    private static string GetClientIpAddress(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor))
        {
            return xForwardedFor.ToString().Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Checks if global rate limit has been exceeded.
    /// Prevents service overload from multiple clients.
    /// </summary>
    private bool CheckGlobalRateLimit(ClientRateLimit clientLimit)
    {
        var totalRequests = _clientLimits.Values.Sum(c => c.GetRequestCount(_options.WindowSizeSeconds));
        return totalRequests < _options.GlobalRequestsPerSecond * _options.WindowSizeSeconds;
    }

    /// <summary>
    /// Removes old rate limit entries to prevent memory leaks.
    /// Called periodically by a background timer.
    /// </summary>
    private void CleanupOldEntries(object? state)
    {
        var staleKeys = _clientLimits
            .Where(kvp => kvp.Value.IsStale(TimeSpan.FromMinutes(10)))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            _clientLimits.TryRemove(key, out _);
        }

        if (staleKeys.Count > 0)
        {
            LogCleanedUpStaleEntries(_logger, staleKeys.Count);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Processing request: {Path}")]
    private static partial void LogProcessingRequest(ILogger logger, PathString path);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Request processed: {Path}")]
    private static partial void LogRequestProcessed(ILogger logger, PathString path);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Rate limit exceeded for client: {ClientIp}, Path: {Path}")]
    private static partial void LogRateLimitExceeded(ILogger logger, string clientIp, PathString path);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Global rate limit exceeded")]
    private static partial void LogGlobalRateLimitExceeded(ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "Cleaned up {Count} stale rate limit entries")]
    private static partial void LogCleanedUpStaleEntries(ILogger logger, int count);
}

/// <summary>
/// Per-client rate limit tracking using sliding window.
/// </summary>
public sealed class ClientRateLimit
{
    private readonly Queue<DateTime> _requestTimestamps = new();
    private readonly object _lockObject = new();
    private DateTime _lastRequestTime;

    /// <summary>
    /// Determines whether a request is allowed within the specified rate-limit window.
    /// </summary>
    public bool AllowRequest(int requestsPerSecond, int windowSizeSeconds)
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-windowSizeSeconds);

            // Remove timestamps older than the window
            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
            {
                _requestTimestamps.Dequeue();
            }

            // Check if within rate limit
            if (_requestTimestamps.Count < requestsPerSecond * windowSizeSeconds)
            {
                _requestTimestamps.Enqueue(now);
                _lastRequestTime = now;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the number of requests recorded within the specified window.
    /// </summary>
    public int GetRequestCount(int windowSizeSeconds)
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            var windowStart = now.AddSeconds(-windowSizeSeconds);

            while (_requestTimestamps.Count > 0 && _requestTimestamps.Peek() < windowStart)
            {
                _requestTimestamps.Dequeue();
            }

            return _requestTimestamps.Count;
        }
    }

    /// <summary>
    /// Determines whether the client has been inactive for longer than the specified timeout.
    /// </summary>
    public bool IsStale(TimeSpan timeout)
    {
        lock (_lockObject)
        {
            return DateTime.UtcNow - _lastRequestTime > timeout;
        }
    }
}

/// <summary>
/// Configuration options for rate limiting.
/// </summary>
public sealed class RateLimitingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests allowed per second.
    /// </summary>
    public int RequestsPerSecond { get; set; } = 100;

    /// <summary>
    /// Gets or sets the size of the rate-limit window, in seconds.
    /// </summary>
    public int WindowSizeSeconds { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of seconds clients should wait before retrying.
    /// </summary>
    public int RetryAfterSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether the global rate limit is enabled.
    /// </summary>
    public bool EnableGlobalLimit { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of requests allowed globally per second.
    /// </summary>
    public int GlobalRequestsPerSecond { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the request paths that are exempt from rate limiting.
    /// </summary>
    public IReadOnlyList<string> ExemptPaths { get; set; } = new[] { "/health", "/swagger" };

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"RequestsPerSecond={RequestsPerSecond}, WindowSizeSeconds={WindowSizeSeconds}, " +
               $"RetryAfterSeconds={RetryAfterSeconds}, EnableGlobalLimit={EnableGlobalLimit}, " +
               $"GlobalRequestsPerSecond={GlobalRequestsPerSecond}, ExemptPathsCount={ExemptPaths.Count}";
    }

    public void Validate()
    {
        if (RequestsPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RequestsPerSecond));
        }

        if (WindowSizeSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(WindowSizeSeconds));
        }

        if (RetryAfterSeconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RetryAfterSeconds));
        }
    }
}

/// <summary>
/// Extension method to register rate limiting middleware.
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, RateLimitingOptions? options = null)
    {
        var opts = options ?? new RateLimitingOptions();
        return builder.UseMiddleware<RateLimitingMiddleware>(opts);
    }
}
