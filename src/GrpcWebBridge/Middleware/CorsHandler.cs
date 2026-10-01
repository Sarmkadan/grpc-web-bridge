#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Configuration options for CORS handling in the gRPC-Web bridge.
/// </summary>
public sealed class CorsHandlerOptions
{
    /// <summary>
    /// Gets or sets the allowed origins. Use "*" for any origin.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = ["*"];

    /// <summary>
    /// Gets or sets the allowed HTTP methods.
    /// </summary>
    public string[] AllowedMethods { get; set; } = ["GET", "POST", "OPTIONS"];

    /// <summary>
    /// Gets or sets the allowed request headers.
    /// </summary>
    public string[] AllowedHeaders { get; set; } =
    [
        "Content-Type",
        "Authorization",
        "X-Grpc-Web",
        "X-User-Agent",
        "X-Requested-With",
        "X-Correlation-Id"
    ];

    /// <summary>
    /// Gets or sets the headers exposed to the client.
    /// </summary>
    public string[] ExposedHeaders { get; set; } =
    [
        "Grpc-Status",
        "Grpc-Message",
        "Grpc-Encoding",
        "X-Correlation-Id"
    ];

    /// <summary>
    /// Gets or sets the preflight cache duration in seconds.
    /// </summary>
    public int MaxAgeSeconds { get; set; } = 86400;

    /// <summary>
    /// Gets or sets whether credentials (cookies, auth headers) are allowed.
    /// </summary>
    public bool AllowCredentials { get; set; }
}

/// <summary>
/// Middleware that handles CORS for gRPC-Web requests.
/// Adds the necessary Access-Control headers and handles preflight OPTIONS requests.
/// Supports configurable origins, methods, and headers for fine-grained access control.
/// </summary>
public sealed class CorsHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorsHandler> _logger;
    private readonly CorsHandlerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorsHandler"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording CORS decisions.</param>
    /// <param name="options">The CORS configuration options.</param>
    public CorsHandler(RequestDelegate next, ILogger<CorsHandler> logger, IOptions<CorsHandlerOptions> options)
    {
        _next = next;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Processes an HTTP request, applying CORS headers and short-circuiting preflight requests.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();

        if (!string.IsNullOrEmpty(origin) && IsOriginAllowed(origin))
        {
            ApplyCorsHeaders(context.Response, origin);

            if (HttpMethods.IsOptions(context.Request.Method))
            {
                _logger.LogDebug("CORS preflight handled for origin: {Origin}", origin);
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    /// Checks whether the given origin is in the allowed origins list.
    /// </summary>
    /// <param name="origin">The request origin to check.</param>
    /// <returns>True if the origin is allowed; otherwise false.</returns>
    public bool IsOriginAllowed(string origin)
    {
        if (_options.AllowedOrigins.Length == 0)
        {
            return false;
        }

        if (_options.AllowedOrigins.Contains("*"))
        {
            return true;
        }

        return _options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }

    private void ApplyCorsHeaders(HttpResponse response, string origin)
    {
        var effectiveOrigin = _options.AllowedOrigins.Contains("*") && !_options.AllowCredentials
            ? "*"
            : origin;

        response.Headers["Access-Control-Allow-Origin"] = effectiveOrigin;
        response.Headers["Access-Control-Allow-Methods"] = string.Join(", ", _options.AllowedMethods);
        response.Headers["Access-Control-Allow-Headers"] = string.Join(", ", _options.AllowedHeaders);
        response.Headers["Access-Control-Expose-Headers"] = string.Join(", ", _options.ExposedHeaders);
        response.Headers["Access-Control-Max-Age"] = _options.MaxAgeSeconds.ToString();

        if (_options.AllowCredentials)
        {
            response.Headers["Access-Control-Allow-Credentials"] = "true";
        }
    }
}
