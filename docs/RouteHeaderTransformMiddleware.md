# RouteHeaderTransformMiddleware

`RouteHeaderTransformMiddleware` invokes registered [`IRouteHeaderTransformHook`](IRouteHeaderTransformHook.md) implementations for requests whose paths match each hook's `RoutePrefix`. Hooks can modify HTTP request headers before the downstream pipeline runs, add entries to a per-request gRPC metadata dictionary, and modify HTTP response headers after the downstream pipeline completes.

## Route selection

For each request, the middleware reads `HttpContext.Request.Path.Value`; a missing value is treated as an empty string. A hook is selected when either:

- `RoutePrefix` is `null` or empty, which matches every request; or
- the request path starts with `RoutePrefix`, using an ordinal, case-insensitive comparison.

The comparison is a direct string-prefix comparison. It does not require a path-segment boundary. For example, `/api` also matches `/apiv2`.

The applicable hooks are collected before any transform runs. They are then invoked sequentially in their resolved registration order for both request and response processing. If no hooks apply, the middleware immediately invokes the next middleware and does not create a gRPC metadata dictionary.

## Request transforms

When at least one hook applies, the middleware creates a `Dictionary<string, string>` with case-insensitive keys and stores it in `HttpContext.Items` under `RouteHeaderTransformConstants.GrpcMetadataKey`. The constant's value is `GrpcWebBridge.GrpcMetadata`.

Before invoking the next middleware, it calls each applicable hook as follows:

```csharp
await hook.TransformRequestAsync(
    context.Request.Headers,
    grpcMetadata,
    context.RequestAborted);
```

The hook receives the mutable request headers, the shared metadata dictionary for that request, and the request cancellation token. All applicable hooks receive the same dictionary, so entries written by an earlier hook are visible to later hooks. The middleware itself only creates and stores this dictionary; it does not copy its entries to a gRPC call.

If a request transform throws, the middleware logs the exception and continues with the next applicable hook. After all request transforms have been attempted, it invokes the next middleware.

## Response transforms

After the next middleware completes successfully, the middleware calls each applicable hook as follows:

```csharp
await hook.TransformResponseAsync(
    context.Response.Headers,
    context.RequestAborted);
```

The hook receives the mutable response headers and the request cancellation token. Response transforms run sequentially in the same order as request transforms. If a response transform throws, the middleware logs the exception and continues with the next applicable hook.

Response transforms are not placed in a `finally` block. If the next middleware throws, they are not invoked.

## Registration

Register one or more hooks with dependency injection, then add the middleware to the application pipeline.

### Hook implementation

`AddRouteHeaderTransformHook<THook>()` registers `THook` as a scoped `IRouteHeaderTransformHook`:

```csharp
services.AddRouteHeaderTransformHook<CustomHeaderTransformHook>();
```

`THook` must be a class that implements `IRouteHeaderTransformHook`. Because the registration is scoped, its constructor can receive scoped dependencies.

### Delegate registration

The delegate overload also registers a scoped hook:

```csharp
services.AddRouteHeaderTransformHook(
    routePrefix: "/api/bridge",
    transformRequest: (headers, metadata, cancellationToken) =>
    {
        headers["X-Bridge"] = "enabled";
        metadata["tenant-id"] = "example";
        return Task.CompletedTask;
    },
    transformResponse: (headers, cancellationToken) =>
    {
        headers["X-Bridge-Response"] = "processed";
        return Task.CompletedTask;
    });
```

The request delegate is required. The response delegate is optional; when omitted, `TransformResponseAsync` returns a completed task without changing the headers. The supplied `routePrefix` is exposed unchanged through `RoutePrefix`.

### Middleware registration

Add the middleware with `UseRouteHeaderTransforms()`:

```csharp
app.UseRouteHeaderTransforms();
```

This method adds `RouteHeaderTransformMiddleware` through `UseMiddleware<RouteHeaderTransformMiddleware>()`. Its position in the pipeline determines which downstream middleware sees request changes and which response headers are available when response transforms run.
