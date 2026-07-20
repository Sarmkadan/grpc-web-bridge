#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Options for <see cref="MaxBodySizeMiddleware"/>.
/// </summary>
public sealed class MaxBodySizeOptions
{
    /// <summary>
    /// Maximum allowed request body size in bytes.
    /// Default is 4 MiB.
    /// </summary>
    public long MaxRequestBodySizeBytes { get; set; } = 4 * 1024 * 1024;
}

/// <summary>
/// Middleware that enforces a configurable maximum request body size.
/// If the request's <c>Content-Length</c> exceeds the configured limit,
/// the request is rejected with <c>413 Payload Too Large</c> and a JSON error
/// payload matching the shape used by <see cref="ErrorHandlingMiddleware"/>.
/// </summary>
public sealed class MaxBodySizeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MaxBodySizeMiddleware> _logger;
    private readonly MaxBodySizeOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="MaxBodySizeMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger used for rejected requests.</param>
    /// <param name="options">Configured maximum body size options.</param>
    public MaxBodySizeMiddleware(
        RequestDelegate next,
        ILogger<MaxBodySizeMiddleware> logger,
        IOptions<MaxBodySizeOptions> options)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var contentLength = context.Request.ContentLength;

        if (contentLength.HasValue && contentLength.Value > _options.MaxRequestBodySizeBytes)
        {
            _logger.LogWarning(
                "Rejected request to {Path} — request body size {Size} exceeds limit {Max}.",
                context.Request.Path,
                contentLength.Value,
                _options.MaxRequestBodySizeBytes);

            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "Payload Too Large",
                message = $"Request body size {contentLength.Value} exceeds the maximum allowed {_options.MaxRequestBodySizeBytes} bytes.",
                path = context.Request.Path.Value,
                traceId = context.TraceIdentifier
            });

            return;
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering <see cref="MaxBodySizeMiddleware"/>.
/// </summary>
public static class MaxBodySizeMiddlewareExtensions
{
    /// <summary>
    /// Adds <see cref="MaxBodySizeMiddleware"/> to the request pipeline.
    /// Place this call early in the pipeline (before routing) so that oversized
    /// requests are rejected before any further processing.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    public static IApplicationBuilder UseMaxRequestBodySize(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<MaxBodySizeMiddleware>();
    }
}
