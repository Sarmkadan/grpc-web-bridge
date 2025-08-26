// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.Text;

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Request/response logging middleware.
/// Logs all incoming requests and outgoing responses with performance metrics.
/// Captures request headers, body (when applicable), response status, and elapsed time.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly HashSet<string> _excludedPaths;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        // Exclude noisy endpoints from detailed logging
        _excludedPaths = new HashSet<string>
        {
            "/health",
            "/swagger",
            "/api/metrics",
            "/favicon.ico"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            // Log incoming request
            await LogRequestAsync(context);

            // Enable response body capture for logging
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                await _next(context);

                // Log response
                stopwatch.Stop();
                await LogResponseAsync(context, stopwatch.ElapsedMilliseconds);

                // Copy captured response to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Logs incoming HTTP request details.
    /// Captures method, path, query string, headers, and body if applicable.
    /// </summary>
    private async Task LogRequestAsync(HttpContext context)
    {
        var request = context.Request;
        var logPath = request.Path.Value ?? "unknown";

        // Skip logging for excluded paths (health checks, swagger, etc.)
        if (_excludedPaths.Any(p => logPath.StartsWith(p)))
            return;

        var requestBodyContent = "";
        if (request.ContentLength > 0 && IsContentTypeLoggable(request.ContentType))
        {
            request.EnableBuffering();
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, false, 1024, leaveOpen: true))
            {
                requestBodyContent = await reader.ReadToEndAsync();
                request.Body.Position = 0; // Reset stream position
            }
        }

        var logData = new
        {
            type = "request",
            method = request.Method,
            path = logPath,
            queryString = request.QueryString.Value,
            scheme = request.Scheme,
            host = request.Host.Host,
            contentType = request.ContentType,
            contentLength = request.ContentLength,
            headers = GetLoggableHeaders(request.Headers),
            body = requestBodyContent.Length > 1000 ? $"{requestBodyContent.Substring(0, 1000)}..." : requestBodyContent,
            ip = context.Connection.RemoteIpAddress?.ToString(),
            timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Incoming request: {RequestData}", logData);
    }

    /// <summary>
    /// Logs outgoing HTTP response details.
    /// Captures status code, content type, content length, and elapsed time.
    /// </summary>
    private async Task LogResponseAsync(HttpContext context, long elapsedMilliseconds)
    {
        var request = context.Request;
        var response = context.Response;
        var logPath = request.Path.Value ?? "unknown";

        // Skip logging for excluded paths
        if (_excludedPaths.Any(p => logPath.StartsWith(p)))
            return;

        var responseBodyContent = "";
        if (response.Body.CanSeek && response.ContentLength > 0 && IsContentTypeLoggable(response.ContentType))
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(response.Body, Encoding.UTF8, false, 1024, leaveOpen: true))
            {
                responseBodyContent = await reader.ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin); // Reset stream position
            }
        }

        var logData = new
        {
            type = "response",
            method = request.Method,
            path = logPath,
            statusCode = response.StatusCode,
            contentType = response.ContentType,
            contentLength = response.ContentLength,
            headers = GetLoggableHeaders(response.Headers),
            body = responseBodyContent.Length > 1000 ? $"{responseBodyContent.Substring(0, 1000)}..." : responseBodyContent,
            elapsedMilliseconds = elapsedMilliseconds,
            timestamp = DateTime.UtcNow
        };

        var logLevel = response.StatusCode >= 500 ? LogLevel.Error :
                      response.StatusCode >= 400 ? LogLevel.Warning :
                      LogLevel.Information;

        _logger.Log(logLevel, "Outgoing response: {ResponseData}", logData);
    }

    /// <summary>
    /// Filters request/response headers to exclude sensitive information.
    /// </summary>
    private static Dictionary<string, string> GetLoggableHeaders(IHeaderDictionary headers)
    {
        var sensitivePaths = new[] { "authorization", "cookie", "x-api-key", "x-auth-token", "token" };
        return headers
            .Where(h => !sensitivePaths.Contains(h.Key.ToLowerInvariant()))
            .ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines if a content type should have its body logged.
    /// Avoids logging binary content, large files, etc.
    /// </summary>
    private static bool IsContentTypeLoggable(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var loggableTypes = new[] { "application/json", "application/xml", "text/" };
        return loggableTypes.Any(type => contentType.Contains(type, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to register request logging middleware.
/// </summary>
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
