# Startup Pipeline

This document describes the startup sequence in `src/GrpcWebBridge/Program.cs`. For details about the bridge registration extensions, see [Dependency Injection](DependencyInjection.md). For instructions on building and running the application, see [Getting Started](GETTING_STARTED.md).

## Service registration

After creating the `WebApplicationBuilder`, the application configures the static Serilog logger with debug-level console output and daily rolling files at `logs/grpc-web-bridge-.txt`, then replaces the host logger through `builder.Host.UseSerilog()`.

The following service-registration calls occur in this order:

1. `AddGrpc` registers ASP.NET Core gRPC support. Both the maximum receive and maximum send message sizes are set to 4 MiB.
2. `AddGrpcWebBridge` registers the bridge services. The options enable the development preset, allow 10,000 streams, enable compression at level 6, enable Swagger and CORS support, and allow the `*` origin.
3. `AddGrpcWebBridgeSwagger` registers Swagger/OpenAPI generation with the title `gRPC-Web Bridge API` and version `1.0.0`.
4. `AddGrpcWebBridgeCors` registers the bridge CORS policy used later under the name `AllowGrpcWeb`.
5. `AddGrpcWebBridgeAuthentication` registers authentication and authorization. Its JWT configuration callback does not set any values in `Program.cs`.
6. `AddGrpcWebBridgePrometheus` registers Prometheus metrics collection.
7. `AddGrpcWebBridgeTracing` registers OpenTelemetry tracing with service name `grpc-web-bridge`, the configured instance name, and a console exporter when the host is running in Development.
8. `AddControllers` registers MVC controller services.
9. `AddRequestContextManager` registers ambient request-context tracking.
10. `AddSingleton<IStreamingSessionManager, StreamingSessionManager>` registers one streaming-session manager for the application lifetime.

Some bridge extension methods register multiple underlying services. See [Dependency Injection](DependencyInjection.md) for the responsibilities of those extensions.

## Configuration read

`Program.cs` reads the following configuration and environment values:

- `GrpcWebBridge:InstanceName` is read from `builder.Configuration` and passed to `AddGrpcWebBridgeTracing` as the tracing instance name. With standard ASP.NET Core configuration, the equivalent environment-variable key is `GrpcWebBridge__InstanceName`.
- `builder.Environment.IsDevelopment()` is evaluated while tracing is configured and again while endpoints are mapped. It controls the OpenTelemetry console exporter and the OpenAPI document endpoint, respectively.

All other bridge, Swagger, CORS, authentication, Prometheus, and tracing values shown above are supplied directly in code. In particular, the JWT callback contains only commented examples and does not explicitly bind a configuration section.

## Middleware pipeline

After `builder.Build()`, middleware is added in this exact order:

1. `UseErrorHandling`
2. `UseGrpcWebContentTypeValidation`
3. `UseRequestLogging`
4. `UseRouting`
5. `UseCorrelationId`
6. `UseRequestContext`
7. `UseGrpcWeb` with `DefaultEnabled = true`
8. `UseCors("AllowGrpcWeb")`
9. `UseAuthentication`
10. `UseAuthorization`

Requests pass through the list from top to bottom; responses and exceptions unwind in the reverse direction. Placing error handling first therefore lets it handle exceptions from the middleware that follows it.

## Endpoint mapping

Endpoints are mapped after the middleware registrations in this order:

1. `MapControllers` maps the attribute-routed controller endpoints:
   - `GET /api/configuration`, `PUT /api/configuration`, `POST /api/configuration/validate`, and `POST /api/configuration/reset`
   - `POST /api/bridge/invoke`, `POST /api/bridge/stream`, and `POST /api/bridge/batch`
   - `GET /api/streams/diagnostics`
   - `GET /api/metrics`, `GET /api/metrics/methods`, `GET /api/metrics/streaming`, and `POST /api/metrics/reset`
   - `GET /api/health`, `GET /api/health/detailed`, `GET /api/health/services`, `GET /api/health/resources`, `GET /api/health/ready`, and `GET /api/health/alive`
2. `MapMetrics` maps the Prometheus scrape endpoint at `GET /metrics`.
3. In Development only, `MapOpenApi` maps the generated OpenAPI document using its default `/openapi/{documentName}.json` route pattern.
4. `MapGrpcReflectionEndpoints` maps:
   - `GET /api/reflection/services`
   - `GET /api/reflection/services/{fullName}`
   - `GET /api/reflection/services/{fullName}/methods/{methodName}`
   - `GET /api/reflection/descriptors`
5. `MapHealthEndpoints` maps:
   - `/healthz`, `/ready`, and `/health` through ASP.NET Core health checks
   - `GET /health/detailed`, which requires authorization
   - `GET /health/registry`
6. A legacy minimal API handler maps `GET /health` for backward compatibility. This uses the same route and HTTP method already mapped by `MapHealthEndpoints`; both registrations are present in the current startup sequence.
7. `GET /api/services` returns the registered service list.
8. `GET /api/services/{serviceId}` returns details for one registered service.
9. `GET /api/streams` returns statistics for active streams.

Finally, `RunAsync` starts the host. Startup and fatal termination are logged through Serilog, and `Log.CloseAndFlush()` runs during shutdown.
