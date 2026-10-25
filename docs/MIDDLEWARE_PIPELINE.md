# Middleware pipeline

This document describes the HTTP middleware pipeline of the gRPC-Web Bridge: the order in which components are registered in `src/GrpcWebBridge/Program.cs`, what each one does, and which ones can end a request early.

ASP.NET Core runs middleware in registration order on the way in and in reverse on the way out. Each step wraps the ones registered after it, so a component placed earlier sees exceptions and responses from everything below it.

## Order used in Program.cs

The pipeline below is the order of the `app.Use*` calls in `Program.cs` (lines 120-131 at the time of writing). Endpoint mapping comes after it.

| # | Registration | Component | Source | Can short-circuit? |
|---|--------------|-----------|--------|--------------------|
| 1 | `UseErrorHandling()` | `ErrorHandlingMiddleware` | `Middleware/ErrorHandlingMiddleware.cs` | Only on exception (writes JSON error) |
| 2 | `UseGrpcWebContentTypeValidation()` | `ContentTypeValidationMiddleware` | `Middleware/ContentTypeValidationMiddleware.cs` | Yes: 415 for non-gRPC-Web POSTs |
| 3 | `UseRequestLogging()` | `RequestLoggingMiddleware` | `Middleware/RequestLoggingMiddleware.cs` | No (logs and buffers the response) |
| 4 | `UseRouting()` | ASP.NET Core routing | framework | No (selects an endpoint) |
| 5 | `UseCorrelationId()` | `CorrelationIdMiddleware` | `Integration/CorrelationIdManager.cs` | No |
| 6 | `UseGrpcWeb(...)` | gRPC-Web translation | `Grpc.AspNetCore.Web` | No |
| 7 | `UseCors("AllowGrpcWeb")` | ASP.NET Core CORS | framework | Yes: answers CORS preflight |
| 8 | `UseAuthentication()` | JWT authentication | framework | No (populates `HttpContext.User`) |
| 9 | `UseRequestContext()` | `RequestContextMiddleware` | `Integration/RequestContextManager.cs` | No |
| 10 | `UseAuthorization()` | ASP.NET Core authorization | framework | Yes: 401 / 403 for protected endpoints |
| - | `MapControllers()`, `MapMetrics()`, `MapOpenApi()` (dev only), `MapGrpcReflectionEndpoints()`, `MapHealthEndpoints()`, minimal APIs | endpoints | - | Yes: endpoints produce the response |

Notes on the order:

- `UseErrorHandling` is first so that any exception thrown below it, including from authentication and endpoints, is turned into a JSON response instead of a bare 500.
- `UseGrpcWebContentTypeValidation` runs before routing so that invalid gRPC-Web requests are rejected before any controller logic runs.
- `UseCorrelationId` runs before authentication, so the correlation ID exists for everything after it.
- `UseRequestContext` runs after `UseAuthentication` so that the authenticated subject (`sub` claim or identity name) is recorded in the request context.
- `UseCors` runs after `UseGrpcWeb` and before authentication, so preflight `OPTIONS` requests are answered without requiring a token.

## Components in detail

### 1. ErrorHandlingMiddleware

Wraps the rest of the pipeline in a `try/catch`. On success it logs the request start and finish. On exception it logs the error, converts the exception to RFC 7807 `ProblemDetails` (`exception.ToProblemDetails(context)`), sets the HTTP status from the problem details (500 if none), and writes an `ErrorResponse` JSON body with `application/json`.

Short-circuit: only when an exception escapes the downstream pipeline. Successful requests pass through unchanged.

### 2. ContentTypeValidationMiddleware

Checks the `Content-Type` of incoming requests and rejects non-gRPC-Web requests.

- Only `POST` requests are checked.
- Paths starting with `/api`, `/swagger`, `/openapi`, `/health`, `/metrics` or `/_` are not checked (case-insensitive).
- Accepted media types are `application/grpc-web`, `application/grpc-web+proto`, `application/grpc-web-text` and `application/grpc-web-text+proto`.

Short-circuit: yes. A checked request with another or missing content type gets `415 Unsupported Media Type` with a JSON body (`error`, `message`, `path`, `traceId`), and the pipeline stops.

### 3. RequestLoggingMiddleware

Logs the incoming request (method, path, query, selected headers, body for JSON, XML and `text/*` content types) and the outgoing response (status, headers, body, elapsed milliseconds). Log level for the response is `Error` for 5xx, `Warning` for 4xx and `Information` otherwise.

- Paths starting with `/health`, `/swagger`, `/api/metrics` and `/favicon.ico` are not logged in detail.
- Sensitive headers (`Authorization`, `Cookie`, `X-Api-Key`, `X-Auth-Token`, `Token`) are removed from the logged headers.
- Bodies are truncated to 1000 characters in the log.
- The response is written to a `MemoryStream` and copied to the client after the downstream pipeline completes. Long or streaming responses are therefore buffered in memory.

Short-circuit: no. It always calls the next middleware.

### 4. Routing (`UseRouting`)

Matches the request path and method to an endpoint and stores the selected endpoint on the `HttpContext`. Routing does not write a response; if no endpoint matches, the request reaches the end of the pipeline and receives 404.

### 5. CorrelationIdMiddleware

Source: `Integration/CorrelationIdManager.cs`.

- Reads `X-Correlation-Id` from the request. If it is absent, a new GUID is generated.
- Stores the value in the `CorrelationIdManager` ambient context (an `AsyncLocal`), so logs and traces below can use it.
- Adds `X-Correlation-Id` to the response.
- Clears the ambient value in a `finally` block.

Short-circuit: no.

### 6. UseGrpcWeb (gRPC-Web translation)

Registered as `app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true })` from `Grpc.AspNetCore.Web`. It converts gRPC-Web requests (binary and text) into gRPC requests for gRPC endpoints, and converts the response back. It does not end the request on its own.

### 7. CORS (`UseCors("AllowGrpcWeb")`)

Applies the `AllowGrpcWeb` policy registered by `AddGrpcWebBridgeCors()`. It adds `Access-Control-*` headers to matching requests.

Short-circuit: yes for CORS preflight requests (`OPTIONS` with `Access-Control-Request-Method`). The response is produced here and the rest of the pipeline is not run.

### 8. Authentication (`UseAuthentication`)

Runs the JWT bearer handler configured by `AddGrpcWebBridgeAuthentication()`. It sets `HttpContext.User` when a valid token is present. It does not reject requests; that is left to authorization.

Short-circuit: no.

### 9. RequestContextMiddleware

Source: `Integration/RequestContextManager.cs`.

- Reads `X-Request-ID` from the request. The value is used only if it is a single header of at most 128 printable ASCII characters; otherwise a new GUID is generated.
- Takes the user ID from the `sub` claim, falling back to the identity name.
- Creates the request context with `RequestContextManager.CreateContext(...)`, which is stored in an `AsyncLocal` so the event bus and other singletons can read it.
- Sets `X-Request-ID` on the response.
- Records elapsed time and clears the context in a `finally` block.

Short-circuit: no.

### 10. UseAuthorization

Enforces `[Authorize]` and authorization policies on endpoints. It returns `401 Unauthorized` or `403 Forbidden` for protected endpoints when the request is not allowed.

Short-circuit: yes, for denied requests.

### Endpoints

`MapControllers()` (REST API under `api/`, including `api/bridge`), `MapMetrics()` (Prometheus), `MapOpenApi()` (development only), `MapGrpcReflectionEndpoints()`, `MapHealthEndpoints()`, and the minimal APIs `/health`, `/api/services`, `/api/services/{serviceId}` and `/api/streams`. Endpoints produce the response and end the pipeline.

## Components not in the default pipeline

These components exist in `src/GrpcWebBridge/Middleware/` but are not registered in `Program.cs`. They are opt-in: add the call to the pipeline to use them.

### MaxBodySizeMiddleware (`UseMaxRequestBodySize()`)

Rejects requests whose `Content-Length` exceeds `MaxBodySizeOptions.MaxRequestBodySizeBytes` (default 4 MiB) with `413 Payload Too Large` and a JSON body. Requests without a `Content-Length` header are not checked. Its extension comment recommends placing it before routing.

Short-circuit: yes, for oversized requests.

### RateLimitingMiddleware (`UseRateLimiting(options)`)

Per-client sliding-window limit, keyed by `X-Forwarded-For` (first entry) or the remote IP address, with an optional global limit. Paths in `ExemptPaths` (default `/health` and `/swagger`) are not limited.

- Per-client limit exceeded: `429 Too Many Requests` with `Retry-After`.
- Global limit exceeded: `503 Service Unavailable`.

Short-circuit: yes, for limited requests.

### RouteHeaderTransformMiddleware (`UseRouteHeaderTransforms()`)

Runs every registered `IRouteHeaderTransformHook` whose `RoutePrefix` matches the request path. Request hooks run before the next middleware and may add gRPC metadata to `HttpContext.Items`; response hooks run after it. Hook exceptions are logged and do not stop the request.

Short-circuit: no. Its extension comment recommends placing it before `UseRouting`.

### CorsHandler

`Middleware/CorsHandler.cs` is a standalone CORS handler with `CorsHandlerOptions`. It has no `Use...` extension method and is not registered anywhere, so it is not part of the pipeline. The live CORS behavior comes from `UseCors("AllowGrpcWeb")`.

## Related documentation

- `docs/ARCHITECTURE.md` - overall architecture.
- `docs/ErrorHandlingMiddleware.md`, `docs/ContentTypeValidationMiddleware.md` and other per-type files in `docs/`.
