# Architecture

How gRPC-Web Bridge is actually put together. Everything below is grounded in the
code under `src/GrpcWebBridge/` - if a component is not listed here, it does not exist.

## What this is

A single ASP.NET Core (net10.0) application that sits between browser clients
(gRPC-Web / plain JSON over HTTP) and backend gRPC services. It translates
protocols, manages streaming sessions, keeps a registry of known backend
services, and pools gRPC channels to them.

Solution layout:

| Project | Purpose |
|---|---|
| `src/GrpcWebBridge` | The bridge itself (only shipping code) |
| `tests/grpc-web-bridge.Tests` | xUnit tests |
| `benchmarks/grpc-web-bridge.Benchmarks` | BenchmarkDotNet suites (auth, protocol translation, stream processing, JSON) |

## Request pipeline

Middleware order as wired in `Program.cs` (order matters, this is the real one):

```
Client (gRPC-Web / JSON over HTTP)
  │
  ├─ ErrorHandlingMiddleware         - catches everything below, maps domain
  │                                    exceptions to HTTP codes + JSON ErrorResponse
  ├─ ContentTypeValidationMiddleware - rejects unsupported content types early
  ├─ RequestLoggingMiddleware        - structured Serilog request/response logging
  ├─ UseRouting
  ├─ UseGrpcWeb                      - Grpc.AspNetCore.Web, DefaultEnabled = true
  ├─ CORS ("AllowGrpcWeb" policy)
  ├─ Authentication / Authorization  - JWT bearer
  └─ Endpoints:
       MapControllers()              - Bridge/HealthCheck/Metrics/Configuration controllers
       MapMetrics()                  - Prometheus /metrics
       MapOpenApi()                  - dev only
       MapGrpcReflectionEndpoints()  - reflection discovery API
       minimal APIs: /health, /api/services, /api/services/{id}, /api/streams
```

`RateLimitingMiddleware` (sliding window, per client) and
`RouteHeaderTransformMiddleware` (pluggable `IRouteHeaderTransformHook` per route
prefix) exist and have `UseRateLimiting()` / extension wiring, but are **opt-in**:
the default `Program.cs` does not enable them.

## Components

### Controllers (`Controllers/`)

- `BridgeController` (`api/bridge`) - the front door. Three endpoints:
  `POST invoke` (unary call), `POST stream` (streaming session), `POST batch`
  (multiple operations in one request, each with its own result/status).
- `HealthCheckController`, `MetricsController`, `ConfigurationController` -
  operational surface (probes, runtime metrics, runtime config inspection).

### Services (`Services/`) - all registered as singletons

- `ProtocolTranslationService` - the core translation logic:
  `TranslateHttpToGrpc`, `TranslateGrpcToHttp`, protobuf<->JSON conversion
  (via `ProtobufUtility`), metadata translation, `TranslateAndInvokeAsync`
  for the full round trip, and error-response construction.
- `StreamingService` - owns a `ConcurrentDictionary` of `Stream` objects
  (stream id -> state machine with a per-stream message queue and
  last-activity timestamp). Supports enqueue/dequeue, heartbeats, per-stream
  statistics, and `CleanupIdleStreams()`.
- `AuthenticationService` - JWT validation and `AuthenticationContext`
  construction; JWT bearer options are configured through
  `AddGrpcWebBridgeAuthentication`.
- `ServiceRegistry` - in-memory catalog of backend `GrpcService` descriptors
  (register/unregister/list/by-package, status updates, cached
  `ServiceMetadata`, health status).
- `ReflectionService` - backs the reflection HTTP endpoints
  (`MapGrpcReflectionEndpoints` in `Configuration/ReflectionServiceExtensions.cs`).
- `BridgePrometheusMetrics` - prometheus-net counters/gauges, enabled via
  `AddGrpcWebBridgePrometheus()`.

### Data access (`Data/`)

- `GrpcConnectionManager` - gRPC channel pool keyed by service full name.
  `GetOrCreateChannel` reuses channels (they are expensive: HTTP/2 connection
  setup, TLS), tracks per-channel `ConnectionMetrics`, supports
  `TestConnectionAsync` and graceful teardown (`IAsyncDisposable`).
- `IServiceRepository` / `ServiceRepository` - persistence abstraction for
  services/requests/responses. The only implementation is in-memory
  dictionaries. The interface exists precisely so a real store can be swapped
  in without touching callers.

### Streaming subsystem (`Streaming/`)

The advanced streaming machinery, contract-first (see `IStreamingContracts.cs`):

- `IBidirectionalStreamingEngine` / `BidirectionalStreamingEngine` - manages
  bidirectional sessions (`BidirectionalStreamContext`).
- `IFlowControlledStream` / `FlowControlledStream` + `FlowControlOptions` -
  bounded, credit-based message flow.
- `IBackpressureController` / `BackpressureController` and
  `AdaptiveFlowController` - throttle producers when consumers lag.
- `StreamingSessionManager`, `StreamDiagnosticsService` - session bookkeeping
  and diagnostics.

Note the deliberate split: `Services/StreamingService` is the simple
queue-per-stream model used by `BridgeController`; `Streaming/` is the richer
engine with flow control. They are separate on purpose - the simple path stays
simple.

### Background workers (`BackgroundWorkers/`)

- `StreamCleanupService` - hosted service registered by `AddGrpcWebBridge()`;
  every 5 minutes calls `StreamingService.CleanupIdleStreams()`.
- `HealthCheckWorker`, `MetricsCollectionWorker`, `StreamCleanupWorker` -
  additional workers, opt-in (not registered by the default DI helper).

### Cross-cutting

- `Events/EventBus` - in-process pub/sub with sync and async subscribers,
  typed events derived from `EventBase`, and a bounded event history
  (default 1000 records). Used to decouple side effects (webhooks, metrics)
  from the request path.
- `Formatters/` - `ResponseFormatter` (static dispatcher) plus
  `JsonFormatter`, `CsvFormatter`, `XmlFormatter` for response shaping.
- `Integration/` - outward-facing helpers: `ServiceDiscoveryClient`,
  `WebhookPublisher`, `CorrelationIdManager`, `RequestContextManager`,
  `HttpClientFactory`.
- `Telemetry/` - `BridgeActivitySource` + `TracingService`; OpenTelemetry
  wired via `AddGrpcWebBridgeTracing()` (console exporter in dev, bring your
  own OTLP/Zipkin in prod).
- `Utilities/` - stateless helpers (`ProtobufUtility`, `JsonUtility`,
  `CryptographyUtility`, `ValidationUtility`, `StreamUtility`, etc.).
- `Domain/` - models (`GrpcRequest`, `GrpcResponse`, `GrpcService`,
  `GrpcMethod`, `StreamMessage`, `BridgeConfiguration`,
  `AuthenticationContext`), enums, constants, and an exception hierarchy
  rooted at `GrpcWebBridgeException` (`ProtocolException`,
  `StreamingException`, `ValidationException`, `ConfigurationException`,
  `ServiceRegistrationException`). `ErrorHandlingMiddleware` maps these to
  HTTP status codes, which is why throwing the right domain exception is the
  contract for error reporting anywhere in the stack.

## Composition (`Configuration/`)

All wiring lives in extension methods on `IServiceCollection`
(`DependencyInjection.cs`), consumed fluently in `Program.cs`:

```csharp
services.AddGrpcWebBridge(o => o.WithDevelopment()
    .WithMaxStreamCount(10000)
    .WithCompression(true, 6)
    .WithCors(true).AddAllowedOrigins("*"));
services.AddGrpcWebBridgeSwagger(...);
services.AddGrpcWebBridgeCors();
services.AddGrpcWebBridgeAuthentication(jwt => { ... });
services.AddGrpcWebBridgePrometheus();
services.AddGrpcWebBridgeTracing(...);
```

`GrpcWebBridgeOptions` is the single options object with a fluent builder API.
`StartupConfiguration` validates options and reports `SystemInfo` at boot.

## Key design decisions and trade-offs

1. **Everything in memory, single node.** Registry, repository, streams,
   cache, event history - all process-local. Rationale: the bridge is a
   stateless-ish edge component; losing the registry on restart is acceptable
   because services re-register (or are re-discovered via
   `ServiceDiscoveryClient`). Trade-off: no horizontal scaling of streaming
   state - a stream is pinned to the instance that created it. Sticky routing
   is required behind a load balancer.

2. **Singletons over scoped services.** Core services hold shared state
   (channel pool, stream table), so singleton is the honest lifetime. The
   cost: they must be thread-safe internally (hence `ConcurrentDictionary`
   everywhere), and scoped dependencies have to be resolved via
   `IServiceProvider` (see `StreamCleanupService`).

3. **Concrete types over interfaces for internal services.** Only boundaries
   that plausibly get swapped have interfaces: `IServiceRepository`
   (storage), the `Streaming/` contracts (alternative flow-control
   implementations), `IRouteHeaderTransformHook` (user extension). Interfaces
   for the sake of mocking were skipped deliberately - tests exercise the
   real singletons.

4. **Opt-in feature wiring.** Prometheus, tracing, Swagger, CORS, auth are
   each a separate `AddGrpcWebBridge*` call rather than one mega-registration.
   Keeps the default footprint small and makes the host's `Program.cs` read
   as a manifest of what is actually enabled.

5. **Channel pooling in `GrpcConnectionManager`.** gRPC channels multiplex,
   so one channel per backend service is the right granularity. Trade-off:
   a dead channel affects all in-flight calls to that service, mitigated by
   `TestConnectionAsync` and replacement on failure.

6. **EventBus instead of direct calls for side effects.** Webhooks and
   metrics collection subscribe to events rather than being invoked inline.
   Trade-off: in-process only, no delivery guarantees - fine for telemetry,
   not for anything transactional.

## Extension points

- `IRouteHeaderTransformHook` + `RouteHeaderTransformMiddleware` - mutate
  headers per route prefix.
- `IServiceRepository` - replace in-memory storage with a database.
- `IBackpressureController` / `IFlowControlledStream` - alternative flow
  control strategies.
- `EventBus.Subscribe<TEvent>` - react to bridge events without touching the
  request path.
- Additional response formatters alongside JSON/CSV/XML.
- Standard ASP.NET Core middleware - insert anywhere in the `Program.cs`
  pipeline.

## Known limitations

- No persistence: registry and stream state die with the process.
- Rate limiting exists but is not enabled by default.
- Message size capped at 4 MB (both directions) in the default `AddGrpc` config.
- `ServiceRepository` keeps requests/responses in unbounded dictionaries -
  fine for diagnostics in dev, needs eviction before heavy production use.
- Single-instance streaming: no shared session store across replicas.
