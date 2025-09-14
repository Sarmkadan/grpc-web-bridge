#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Contract for a per-route header transformation hook.
/// Implement this interface to inspect and rewrite HTTP request and response
/// headers before the request reaches a downstream gRPC service or after
/// the response returns from it.
/// </summary>
public interface IRouteHeaderTransformHook
{
    /// <summary>
    /// The route prefix this hook applies to (e.g. "/api/bridge/invoke").
    /// Use <c>null</c> or an empty string to apply the hook to every route.
    /// Prefix matching is case-insensitive.
    /// </summary>
    string? RoutePrefix { get; }

    /// <summary>
    /// Called before the downstream handler receives the request.
    /// Implementations may add, remove, or rename headers in
    /// <paramref name="requestHeaders"/> and append extra gRPC metadata entries
    /// to <paramref name="grpcMetadata"/>.
    /// </summary>
    /// <param name="requestHeaders">Mutable set of incoming HTTP request headers.</param>
    /// <param name="grpcMetadata">
    ///   Mutable dictionary that will be merged into the gRPC call metadata.
    /// </param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    Task TransformRequestAsync(
        IHeaderDictionary requestHeaders,
        Dictionary<string, string> grpcMetadata,
        CancellationToken cancellationToken);

    /// <summary>
    /// Called after the downstream handler has produced a response.
    /// Implementations may add or remove headers from
    /// <paramref name="responseHeaders"/>.
    /// </summary>
    /// <param name="responseHeaders">Mutable set of outgoing HTTP response headers.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    Task TransformResponseAsync(
        IHeaderDictionary responseHeaders,
        CancellationToken cancellationToken);
}

/// <summary>
/// Middleware that runs all registered <see cref="IRouteHeaderTransformHook"/> instances
/// whose <see cref="IRouteHeaderTransformHook.RoutePrefix"/> matches the current request path.
/// Hooks are resolved from DI so they can take constructor dependencies.
/// </summary>
public sealed class RouteHeaderTransformMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RouteHeaderTransformMiddleware> _logger;

    public RouteHeaderTransformMiddleware(
        RequestDelegate next,
        ILogger<RouteHeaderTransformMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(
        HttpContext context,
        IEnumerable<IRouteHeaderTransformHook> hooks)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var applicableHooks = hooks
            .Where(h => string.IsNullOrEmpty(h.RoutePrefix) ||
                        path.StartsWith(h.RoutePrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (applicableHooks.Count == 0)
        {
            await _next(context);
            return;
        }

        // Provide a scratch metadata dictionary that hook implementations can populate.
        // Downstream code can read it from HttpContext.Items.
        var grpcMetadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        context.Items[RouteHeaderTransformConstants.GrpcMetadataKey] = grpcMetadata;

        foreach (var hook in applicableHooks)
        {
            try
            {
                await hook.TransformRequestAsync(
                    context.Request.Headers,
                    grpcMetadata,
                    context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Request header transform hook {HookType} failed for path {Path}",
                    hook.GetType().Name, path);
            }
        }

        await _next(context);

        foreach (var hook in applicableHooks)
        {
            try
            {
                await hook.TransformResponseAsync(
                    context.Response.Headers,
                    context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Response header transform hook {HookType} failed for path {Path}",
                    hook.GetType().Name, path);
            }
        }
    }
}

/// <summary>
/// Well-known keys used by the header transform middleware.
/// </summary>
public static class RouteHeaderTransformConstants
{
    /// <summary>
    /// Key under which the accumulated gRPC metadata dictionary is stored in
    /// <see cref="HttpContext.Items"/>.
    /// </summary>
    public const string GrpcMetadataKey = "GrpcWebBridge.GrpcMetadata";
}

/// <summary>
/// Extension methods for registering the header transform middleware and hooks.
/// </summary>
public static class RouteHeaderTransformExtensions
{
    /// <summary>
    /// Registers <see cref="RouteHeaderTransformMiddleware"/> in the pipeline.
    /// Call this before <c>UseRouting</c> so that transforms run before routing.
    /// </summary>
    public static IApplicationBuilder UseRouteHeaderTransforms(
        this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseMiddleware<RouteHeaderTransformMiddleware>();
    }

    /// <summary>
    /// Registers a concrete <see cref="IRouteHeaderTransformHook"/> implementation as a
    /// scoped service so that it can take scoped dependencies.
    /// </summary>
    public static IServiceCollection AddRouteHeaderTransformHook<THook>(
        this IServiceCollection services)
        where THook : class, IRouteHeaderTransformHook
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IRouteHeaderTransformHook, THook>();
        return services;
    }

    /// <summary>
    /// Registers a delegate-based <see cref="IRouteHeaderTransformHook"/> without requiring
    /// a separate class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="routePrefix">
    ///   The route prefix to match, or <c>null</c> to match all routes.
    /// </param>
    /// <param name="transformRequest">
    ///   Delegate invoked to transform request headers and populate gRPC metadata.
    /// </param>
    /// <param name="transformResponse">
    ///   Optional delegate invoked to transform response headers.
    /// </param>
    public static IServiceCollection AddRouteHeaderTransformHook(
        this IServiceCollection services,
        string? routePrefix,
        Func<IHeaderDictionary, Dictionary<string, string>, CancellationToken, Task> transformRequest,
        Func<IHeaderDictionary, CancellationToken, Task>? transformResponse = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(transformRequest);

        services.AddScoped<IRouteHeaderTransformHook>(
            _ => new DelegateRouteHeaderTransformHook(routePrefix, transformRequest, transformResponse));

        return services;
    }
}

/// <summary>
/// Internal delegate-based implementation of <see cref="IRouteHeaderTransformHook"/>.
/// </summary>
internal sealed class DelegateRouteHeaderTransformHook : IRouteHeaderTransformHook
{
    private readonly Func<IHeaderDictionary, Dictionary<string, string>, CancellationToken, Task> _requestTransform;
    private readonly Func<IHeaderDictionary, CancellationToken, Task>? _responseTransform;

    public string? RoutePrefix { get; }

    internal DelegateRouteHeaderTransformHook(
        string? routePrefix,
        Func<IHeaderDictionary, Dictionary<string, string>, CancellationToken, Task> requestTransform,
        Func<IHeaderDictionary, CancellationToken, Task>? responseTransform)
    {
        RoutePrefix = routePrefix;
        _requestTransform = requestTransform;
        _responseTransform = responseTransform;
    }

    public Task TransformRequestAsync(
        IHeaderDictionary requestHeaders,
        Dictionary<string, string> grpcMetadata,
        CancellationToken cancellationToken) =>
        _requestTransform(requestHeaders, grpcMetadata, cancellationToken);

    public Task TransformResponseAsync(
        IHeaderDictionary responseHeaders,
        CancellationToken cancellationToken) =>
        _responseTransform is not null
            ? _responseTransform(responseHeaders, cancellationToken)
            : Task.CompletedTask;
}
