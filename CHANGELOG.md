// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Changelog

All notable changes to gRPC-Web Bridge are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-05-20

### Added
- **Stable Release**: Production-ready gRPC-Web bridge for .NET 10
- **Security Scanning**: CodeQL workflow and dependency vulnerability scanning
- **Dependabot**: Automated NuGet and GitHub Actions dependency updates
- **NuGet Packaging**: Full package metadata, README, and license inclusion
- **Kubernetes Manifests**: Production-grade Deployment, Service, and Ingress YAML
- **Prometheus Integration**: Alert rules and scrape configuration examples
- **Benchmark Suite**: BenchmarkDotNet micro-benchmarks for protocol translation, stream processing, and authentication hot paths
- **Input Validation**: Comprehensive boundary and edge-case checks across all public API surfaces

### Changed
- **Test Coverage**: Expanded unit tests for `AuthenticationService`, `ValidationUtility`, and `DateTimeUtility`
- **Stream Cleanup**: `StreamCleanupWorker` now uses configurable idle timeout and logs evictions
- **Error Messages**: All exception types expose machine-readable `ErrorCode` properties
- **Documentation**: Complete architecture, deployment, performance tuning, FAQ, and troubleshooting guides

### Fixed
- Resolved connection leak when a gRPC channel fails during handshake
- Fixed `ConcurrentDictionary` race in `AuthenticationService` context cache
- Corrected CORS preflight response for credentialed requests
- Fixed protobuf length-prefix framing for messages larger than 64 KB

### Security
- Validated all header values before forwarding to upstream gRPC services
- Added maximum message size enforcement in `StreamingService`
- Hardened `CryptographyUtility` to use constant-time comparison for API key validation

## [0.9.0] - 2025-04-28

### Added
- **Webhook Publishing**: `WebhookPublisher` for event-driven integrations with external systems
- **Response Formatters**: JSON, CSV, and XML output formatters with content-negotiation
- **Event Bus**: Internal `EventBus` for decoupled service communication
- **Service Discovery Client**: Consul-compatible service discovery integration
- **Docker Compose Stack**: Full example stack with Prometheus and Grafana
- **`appsettings.Production.json`**: Production configuration template with sane defaults

### Changed
- `BridgeController` now respects `Accept` header and delegates to the appropriate formatter
- `MetricsCollectionWorker` records per-method latency histograms
- `CacheManager` supports optional distributed cache backend via `IDistributedCache`

### Fixed
- Fixed stream message ordering under high concurrency
- Resolved null-reference in `ReflectionService` when descriptor has no methods
- Corrected XML formatter escaping for non-ASCII characters in field names

### Known Issues
- Service discovery refresh may delay up to one interval on first registration

## [0.8.0] - 2025-04-07

### Added
- **Swagger / OpenAPI**: Interactive API documentation at `/swagger`
- **Health Check Endpoints**: `/health/live` and `/health/ready` for Kubernetes probes
- **Metrics Endpoint**: `/api/metrics` with request counts, latency percentiles, and active streams
- **Caching Layer**: `CacheManager` and `CacheUtility` with configurable TTL
- **`HealthCheckWorker`**: Background worker that periodically probes registered gRPC services
- **`MetricsCollectionWorker`**: Background worker for aggregating runtime telemetry

### Changed
- `GrpcWebBridgeOptions` extended with `EnableSwagger`, `EnableMetrics`, and `EnableCors` flags
- `ConfigurationController` now exposes a `PUT /api/configuration` endpoint for live updates
- `ServiceRepository` stores service health state alongside metadata

### Fixed
- Fixed Swagger schema generation for gRPC method parameter types
- Resolved `HealthCheckWorker` timer drift under heavy load

## [0.7.0] - 2025-03-24

### Added
- **API Key Authentication**: Header-based API key support via `X-API-Key`
- **Rate Limiting Middleware**: Per-client token-bucket rate limiting
- **Request Logging Middleware**: Structured request/response logging with duration
- **Error Handling Middleware**: Centralised exception-to-HTTP-response mapping
- **`CorrelationIdManager`**: Propagates `X-Correlation-ID` across gRPC hops
- **`RequestContextManager`**: Scoped request context for downstream services

### Changed
- `AuthenticationService` supports both JWT Bearer and API key schemes simultaneously
- `DependencyInjection` extension now registers all middleware in the correct order
- `GrpcConnectionManager` exposes connection health metrics

### Fixed
- Fixed API key lookup for case-insensitive header names
- Resolved middleware ordering issue causing authentication to run after routing

## [0.6.0] - 2025-03-10

### Added
- **Bidirectional Streaming**: Full-duplex `AsyncDuplexStreamingCall` support
- **CORS Support**: Configurable `AllowedOrigins`, `AllowedMethods`, and `AllowedHeaders`
- **`StreamCleanupWorker`**: Background worker that evicts idle streams past timeout
- **`StreamMessage` Model**: Typed envelope for stream frames with sequence numbers
- **Compression**: gzip and deflate response compression with configurable level

### Changed
- `StreamingService` tracks all four RPC patterns (unary, server, client, bidirectional)
- `GrpcWebBridgeOptions` extended with `StreamHeartbeatIntervalSeconds` and `MaxStreamCount`
- `BridgeController` routes bidirectional streams to dedicated handler path

### Fixed
- Fixed client-streaming EOF propagation when the browser closes the connection early
- Resolved heartbeat timer not cancelling on stream disposal

## [0.5.0] - 2025-02-24

### Added
- **Client Streaming**: `AsyncClientStreamingCall` support
- **JWT Authentication**: Bearer token validation with issuer/audience checks
- **`AuthenticationService`**: Extracts and caches `AuthenticationContext` per request
- **`AuthenticationContext` Model**: Carries identity, roles, and raw token claims
- **`ReflectionServiceExtensions`**: Helpers for gRPC server reflection

### Changed
- `BridgeController` validates authentication context before forwarding requests
- `StartupConfiguration` wires JWT bearer middleware into the ASP.NET Core pipeline
- `GrpcWebBridgeOptions` extended with `RequireAuthentication` and JWT settings

### Fixed
- Fixed `IAsyncStreamReader` enumeration stopping prematurely on empty frames
- Corrected JWT claim mapping for custom role claims

## [0.4.0] - 2025-02-10

### Added
- **Server Streaming**: `AsyncServerStreamingCall` support in `StreamingService`
- **Connection Pooling**: `GrpcConnectionManager` with channel reuse and health-gating
- **`GrpcConnectionManager`**: Manages `GrpcChannel` lifecycle and disposal
- **`StreamingException`**: Dedicated exception type for stream lifecycle failures
- **Makefile**: Developer shortcuts for build, test, docker, and format targets

### Changed
- `ServiceRegistry` caches channel instances per service address
- `ProtocolTranslationService` handles length-prefix framing for streamed responses
- `BridgeController` returns `application/grpc-web+proto` content type for streams

### Fixed
- Fixed channel disposal on service deregistration
- Resolved frame boundary issue when multiple protobuf messages arrive in one TCP segment

## [0.3.0] - 2025-01-27

### Added
- **Unary RPC**: End-to-end unary call path from gRPC-Web client to gRPC backend
- **Service Registry**: `ServiceRegistry` and `IServiceRepository` for service metadata management
- **`GrpcMethod` Model**: Strongly-typed descriptor for RPC method signatures
- **`GrpcService` / `GrpcRequest` / `GrpcResponse` Models**: Domain types for the bridge pipeline
- **`ReflectionService`**: Wraps gRPC server reflection for dynamic method discovery
- **`ServiceRepository`**: In-memory store with thread-safe read/write access
- **`ConfigurationController`**: REST endpoints for service registration and configuration

### Changed
- `BridgeController` delegates request forwarding to `ProtocolTranslationService`
- `DependencyInjection` registers domain services with scoped lifetimes

### Fixed
- Fixed routing conflict between `/api/services` and `/api/configuration` prefixes

## [0.2.0] - 2025-01-13

### Added
- **Protocol Translation**: `ProtocolTranslationService` converts gRPC-Web frames to gRPC and back
- **`ProtobufUtility`**: Length-prefix framing, base64 encoding, and JSON interop helpers
- **`JsonUtility`**: Thin wrapper around `System.Text.Json` with cached `JsonSerializerOptions`
- **`StreamUtility`**: `ArrayPool`-backed stream helpers for zero-allocation I/O
- **`ValidationUtility`**: Input validation helpers used across controllers and services
- **Domain Exceptions**: `GrpcWebBridgeException`, `ProtocolException`, `ServiceRegistrationException`
- **`BridgeController`**: Initial HTTP controller skeleton with route templates
- **Serilog**: Structured logging wired into ASP.NET Core host

### Changed
- `Program.cs` configures Kestrel with HTTP/1.1 and HTTP/2 endpoints
- `appsettings.json` extended with `GrpcWebBridge` configuration section

### Fixed
- Fixed base64url padding in `ProtobufUtility.ToBase64` for payloads whose length is not a multiple of 3

## [0.1.0] - 2025-01-06

### Added
- Initial project scaffold: `src/GrpcWebBridge`, `tests/grpc-web-bridge.Tests`, `benchmarks/`
- `GrpcWebBridge.csproj` targeting .NET 10 with Grpc.AspNetCore, Google.Protobuf, and Serilog
- `GrpcWebBridgeOptions`: root configuration object with sensible defaults
- `BridgeConfiguration` and `GrpcWebBridgeException` domain types
- `Program.cs` with minimal ASP.NET Core host setup
- `appsettings.json` and `appsettings.Development.json`
- `.editorconfig`, `.gitignore`, `.dockerignore`
- `Dockerfile` (multi-stage build targeting `mcr.microsoft.com/dotnet/aspnet:10.0`)
- MIT `LICENSE`, `README.md` stub, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`
- GitHub Actions CI workflow (`build.yml`) for build and test on push/PR
- xUnit test project with FluentAssertions and Moq

---

## Version Comparison

| Feature | 0.1.0 | 0.3.0 | 0.5.0 | 0.7.0 | 0.9.0 | 1.0.0 |
|---------|:-----:|:-----:|:-----:|:-----:|:-----:|:-----:|
| Unary RPC | | ✓ | ✓ | ✓ | ✓ | ✓ |
| Server Streaming | | | | ✓ | ✓ | ✓ |
| Client Streaming | | | ✓ | ✓ | ✓ | ✓ |
| Bidirectional Streaming | | | | | ✓ | ✓ |
| JWT Auth | | | ✓ | ✓ | ✓ | ✓ |
| API Keys | | | | ✓ | ✓ | ✓ |
| CORS | | | | | ✓ | ✓ |
| Compression | | | | | ✓ | ✓ |
| Swagger / OpenAPI | | | | | ✓ | ✓ |
| Metrics | | | | | | ✓ |
| Webhooks | | | | | ✓ | ✓ |
| Health Checks | | | | | ✓ | ✓ |
| Rate Limiting | | | | ✓ | ✓ | ✓ |
| Docker Support | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Kubernetes Ready | | | | | ✓ | ✓ |
| BenchmarkDotNet | | | | | | ✓ |

## Upgrade Guide

### From 0.9.0 to 1.0.0

- No breaking API changes
- New `EnableMetrics` flag in `GrpcWebBridgeOptions` defaults to `true`
- `CryptographyUtility.CompareApiKey` now uses constant-time comparison — no code changes required
- Add the `benchmarks/` project reference to your IDE solution if you want to run micro-benchmarks

### From 0.8.0 to 0.9.0

- `BridgeController` response content-type now follows the `Accept` header — clients that relied on always receiving `application/json` should set the header explicitly
- Webhook URLs are configured under `GrpcWebBridge:Webhooks` in `appsettings.json`

### From 0.7.0 to 0.8.0

- Swagger is enabled by default in Development; set `EnableSwagger: false` in Production if not needed
- Health check paths changed from `/healthz` to `/health/live` and `/health/ready`

## Maintenance

### Supported Versions

- **Current**: 1.0.0 (Production Ready)
- **Previous**: 0.9.0 (Security fixes only, until 2025-11-20)
- **EOL**: 0.8.0 and earlier (no longer supported)

### Patch Schedule

- Security fixes: Released immediately
- Bug fixes: Released monthly
- Features: Released quarterly

## Contributors

Maintained by:
- **Vladyslav Zaiets** — Creator & Lead Developer

## License

MIT License — See [LICENSE](LICENSE) for details.
