# CLAUDE.md

## Project overview

gRPC-Web Bridge: a single ASP.NET Core (net10.0) service that translates gRPC-Web / JSON-over-HTTP requests from browsers into gRPC calls to backend services, with streaming sessions, a service registry, channel pooling, JWT auth, Prometheus metrics and OpenTelemetry tracing.

## Build

SDK pinned in `global.json`: 10.0.100 (`rollForward: latestMinor`). Solution: `grpc-web-bridge.sln`.

```bash
dotnet restore
dotnet build --configuration Release            # whole solution
dotnet build src/GrpcWebBridge --configuration Release   # bridge only (what `make build` does)
dotnet run --project src/GrpcWebBridge          # dev server (`make run`)
dotnet publish src/GrpcWebBridge -c Release -o ./bin/Release/publish
docker build -t grpc-web-bridge .               # multi-stage, sdk:10.0 -> aspnet:10.0
```

`Makefile` wraps these (`make build|test|run|format|lint|publish|docker-build|ci`). Note: `make test` runs `dotnet test` inside `src/GrpcWebBridge`, which is not a test project - run tests from the repo root or the test project instead.

Shared compiler settings live in `Directory.Build.props`: `LangVersion latest`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `Deterministic true`, a long `NoWarn` list (nullable CS86xx, CS1591, SYSLIB obsoletes). `GenerateDocumentationFile` is on for every project except `*.Tests`. Project files should not repeat these properties.

## Test

Test project: `tests/grpc-web-bridge.Tests` (xUnit 2.9, FluentAssertions 7, NSubstitute 5.3, Microsoft.AspNetCore.Mvc.Testing, OpenTelemetry.Exporter.InMemory). Root namespace `GrpcWebBridge.Tests`.

```bash
dotnet test --configuration Release                                  # all (CI: dotnet build then dotnet test --no-build)
dotnet test tests/grpc-web-bridge.Tests                              # test project only
dotnet test tests/grpc-web-bridge.Tests --filter "FullyQualifiedName~CacheManagerTests"
dotnet test --collect:"XPlat Code Coverage"
```

Conventions:
- One file per class under test, flat in the test project root: `<Type>Tests.cs`; extension classes get `<Type>ExtensionsTests.cs` / `<Type>JsonExtensionsTests.cs`.
- Test names: `Method_Scenario_ExpectedResult` or `Method_ShouldDoX` (`[Fact]`, `[Theory]`).
- Both plain `Assert.*` and FluentAssertions are used; new tests may use either, stay consistent within a file.
- NSubstitute for mocks, `NullLogger<T>` / `ILogger` substitutes for logging.
- Benchmarks: `benchmarks/grpc-web-bridge.Benchmarks` (BenchmarkDotNet) - `dotnet run -c Release --project benchmarks/grpc-web-bridge.Benchmarks`.
- `test/GrpcWebBridge/` (singular) is a stray directory NOT referenced by the solution; do not add tests there.

## Lint / Format

```bash
dotnet format                        # make format
dotnet format --verify-no-changes    # make lint / make format-check
```

`.editorconfig`: 4-space indent, UTF-8 BOM, trim trailing whitespace, final newline, `var` for built-in types. CI (`.github/workflows/ci.yml`, `build.yml`) runs trufflehog secret scanning, then restore/build/test on 8.0.x and 10.0.x; `codeql.yml` runs weekly. `.gitleaks.toml` is present.

## Architecture

Entry point: `src/GrpcWebBridge/Program.cs` (top-level statements, Serilog console + rolling file `logs/`). Full description in `docs/ARCHITECTURE.md`; per-type notes in `docs/*.md`.

Middleware order in `Program.cs` (order matters): `UseErrorHandling` -> `UseGrpcWebContentTypeValidation` -> `UseRequestLogging` -> `UseRouting` -> `UseCorrelationId` -> `UseRequestContext` -> `UseGrpcWeb` -> CORS `AllowGrpcWeb` -> Authentication/Authorization -> endpoints (`MapControllers`, `MapMetrics`, `MapOpenApi` dev-only, `MapGrpcReflectionEndpoints`, `MapHealthEndpoints`, minimal APIs `/health`, `/api/services`, `/api/streams`). `RateLimitingMiddleware` and `RouteHeaderTransformMiddleware` exist but are opt-in.

Key directories under `src/GrpcWebBridge/`:
- `Configuration/` - `DependencyInjection.cs` (`AddGrpcWebBridge`, `AddGrpcWebBridgeSwagger/Cors/Authentication/Prometheus/Tracing`), `GrpcWebBridgeOptions` (fluent `With*` builder), `StartupConfiguration`.
- `Controllers/` - `BridgeController` (`api/bridge`: `invoke`, `stream`, `batch`), `HealthCheckController`, `MetricsController`, `ConfigurationController`, `StreamDiagnosticsController`.
- `Services/` - `ProtocolTranslationService` (core HTTP<->gRPC translation), `StreamingService`, `AuthenticationService` (JWT), `ServiceRegistry` + `RegistrySnapshot`, `ReflectionService`, `BridgePrometheusMetrics`.
- `Streaming/` - `BidirectionalStreamingEngine`, `StreamingSessionManager` (`IStreamingSessionManager`), `AdaptiveFlowController`, `BackpressureController`, `FlowControlledStream`, `FlowControlOptions`.
- `Middleware/` - error handling, content-type validation, request logging, rate limiting, max body size, route header transform.
- `Data/` - `IServiceRepository`/`ServiceRepository` (in-memory), `GrpcConnectionManager` (channel pooling).
- `Domain/` - `Models/` (GrpcService, GrpcMethod, GrpcRequest/Response, StreamMessage, BridgeConfiguration, AuthenticationContext), `Exceptions/`, `Enums.cs`, `Constants.cs`.
- `BackgroundWorkers/` - `IHostedService`s: `StreamCleanupService`, `StreamingWorkerSupervisor`, `HealthCheckWorker`, `MetricsCollectionWorker`.
- `Integration/` - `CorrelationIdManager`, `RequestContextManager` (AsyncLocal ambient context), `HttpClientFactory`, `ServiceDiscoveryClient`, `WebhookPublisher`.
- `Endpoints/` - health checks (`MapHealthEndpoints`, `ServiceHealthCheck`, `SystemHealthCheck`).
- `Events/` - in-process `EventBus`. `Formatters/` - Json/Xml/Csv response formatters. `Telemetry/` - `BridgeActivitySource`, `TracingService`. `Utilities/` - static helpers (Json, Protobuf, Reflection, Stream, Validation, Cryptography, DateTime, Cache).
- `examples/` - client scripts (JS, Python, curl), docker-compose with Prometheus/Grafana, k8s manifest.

## Conventions

- Every `.cs` file starts with `#nullable enable` and the author banner comment block; copy it for new files.
- File-scoped namespaces mirroring the folder (`GrpcWebBridge.<Folder>`). One primary type per file.
- Companion partial-behaviour files: `<Type>Extensions.cs` (extension methods), `<Type>JsonExtensions.cs` (System.Text.Json serialization helpers), `<Type>Validation.cs` (validation helpers). Follow this split rather than growing the main type.
- Public APIs carry XML doc comments (`GenerateDocumentationFile` is on; CS1591 suppressed, so missing docs do not fail the build but are expected).
- DI: everything is registered via `IServiceCollection` extension methods in `Configuration/DependencyInjection.cs` or `<Feature>Extensions.cs` (`services.AddX()`, `app.UseX()`, `app.MapX()`). Core services are singletons; background work uses `AddHostedService<T>`. Interfaces are introduced only where mocking is needed (`IServiceRepository`, `IStreamingSessionManager`, `IRouteHeaderTransformHook`); most services are registered as concrete types.
- Options: `GrpcWebBridgeOptions` fluent builder (`WithDevelopment().WithMaxStreamCount(...)`), registered as a singleton instance, not `IOptions<T>`.
- Error handling: domain exceptions derive from `GrpcWebBridgeException` (`ErrorCode`, `GrpcStatus`, `Context` dictionary) - `ConfigurationException`, `ProtocolException`, `StreamingException`, `ValidationException`, `ServiceRegistrationException`. `ErrorHandlingMiddleware` maps them to HTTP status + JSON `ProblemDetails`/ErrorResponse; do not catch-and-swallow in controllers. Argument guards use `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrEmpty`.
- Middleware classes are `sealed`, take `RequestDelegate next, ILogger<T> logger`, expose `InvokeAsync(HttpContext)`, and are wired via a `Use<Name>()` extension.
- Logging: `ILogger<T>` with structured message templates (Serilog sink). Concurrency: `ConcurrentDictionary` for registries/streams, `CancellationToken` on all async APIs.
- Repo hygiene: git history is the changelog (`CHANGELOG.md` is legacy, do not extend it). Ignore stray root files (`test_problemdetails.cs`, `verify_problemdetails.cs`, `*.backup`, oddly named directories) - they are artifacts of past automated runs, not part of the build.
