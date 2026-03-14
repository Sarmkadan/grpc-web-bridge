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
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitingOptions _options;
    private readonly ConcurrentDictionary<string, ClientRateLimit> _clientLimits;
    private readonly Timer _cleanupTimer;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitingOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
        _clientLimits = new ConcurrentDictionary<string, ClientRateLimit>();

        // Periodically clean up old entries to prevent memory leaks
        _cleanupTimer = new Timer(CleanupOldEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip rate limiting for health checks
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIpAddress(context);
        var clientKey = $"{clientIp}_{context.Request.Path}";

        var clientLimit = _clientLimits.GetOrAdd(clientKey, _ => new ClientRateLimit());

        // Check if client has exceeded rate limit
        if (!clientLimit.AllowRequest(_options.RequestsPerSecond, _options.WindowSizeSeconds))
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientIp}, Path: {Path}",
                clientIp, context.Request.Path);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Retry-After", _options.RetryAfterSeconds.ToString());

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
            _logger.LogWarning("Global rate limit exceeded");

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
            _logger.LogDebug("Cleaned up {Count} stale rate limit entries", staleKeys.Count);
        }
    }
}

/// <summary>
/// Per-client rate limit tracking using sliding window.
/// </summary>
public class ClientRateLimit
{
    private readonly Queue<DateTime> _requestTimestamps = new();
    private readonly object _lockObject = new();
    private DateTime _lastRequestTime;

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
public class RateLimitingOptions
{
    public int RequestsPerSecond { get; set; } = 100;
    public int WindowSizeSeconds { get; set; } = 1;
    public int RetryAfterSeconds { get; set; } = 60;
    public bool EnableGlobalLimit { get; set; } = true;
    public int GlobalRequestsPerSecond { get; set; } = 10000;
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
