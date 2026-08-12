#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Extensions;

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Middleware that validates the <c>Content-Type</c> header on incoming requests
/// and rejects any request whose content type is not a recognised gRPC-Web media type.
/// <para>
/// Only POST requests whose path does not begin with an excluded prefix are checked;
/// all other requests (GET, health checks, Swagger, metrics, etc.) pass through
/// unconditionally so that non-gRPC endpoints remain unaffected.
/// </para>
/// <para>
/// Requests with an invalid or missing content type receive a
/// <c>415 Unsupported Media Type</c> response.
/// </para>
/// </summary>
public sealed class ContentTypeValidationMiddleware
{
    /// <summary>
    /// Request path prefixes that bypass content-type validation.
    /// These correspond to infrastructure and REST API endpoints that are not
    /// gRPC-Web proxy paths.
    /// </summary>
    private static readonly string[] ExcludedPathPrefixes =
    [
        "/api",
        "/swagger",
        "/openapi",
        "/health",
        "/metrics",
        "/_",
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<ContentTypeValidationMiddleware> _logger;

    /// <summary>
    /// Initialises the middleware.
    /// </summary>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    /// <param name="logger">Logger for rejected requests.</param>
    public ContentTypeValidationMiddleware(
        RequestDelegate next,
        ILogger<ContentTypeValidationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc cref="IMiddleware.InvokeAsync"/>
    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation(
            "ContentTypeValidationMiddleware invoked for {Method} {Path}",
            context.Request.Method, context.GetGrpcMethodPath());

        if (ShouldValidate(context))
        {
            if (!context.IsGrpcWebRequest())
            {
                _logger.LogWarning(
                    "Rejected request to {Path} — invalid Content-Type: '{ContentType}'. " +
                    "Expected a gRPC-Web media type.",
                    context.GetGrpcMethodPath(), context.Request.ContentType ?? "(none)");

                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                context.Response.ContentType = "application/json";

                _logger.LogInformation("Response written for {Path}", context.GetGrpcMethodPath());
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Unsupported Media Type",
                    message = $"Content-Type '{context.Request.ContentType ?? "(none)"}' is not supported. " +
                               "Use one of: application/grpc-web, application/grpc-web+proto, " +
                               "application/grpc-web-text, application/grpc-web-text+proto.",
                    path = context.GetGrpcMethodPath(),
                    traceId = context.TraceIdentifier
                });

                return;
            }
        }

        await _next(context);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private static bool ShouldValidate(HttpContext context)
    {
        // Only POST requests carry a body subject to gRPC-Web protocol rules.
        if (!HttpMethods.IsPost(context.Request.Method))
            return false;

        var path = context.GetGrpcMethodPath();

        foreach (var prefix in ExcludedPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}

/// <summary>
/// Extension methods for registering <see cref="ContentTypeValidationMiddleware"/>.
/// </summary>
public static class ContentTypeValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds <see cref="ContentTypeValidationMiddleware"/> to the request pipeline.
    /// Place this call early in the pipeline, before routing, so that invalid
    /// requests are rejected before any routing or controller logic runs.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    public static IApplicationBuilder UseGrpcWebContentTypeValidation(
        this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<ContentTypeValidationMiddleware>();
    }
}
