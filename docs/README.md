# Documentation Index

Index of every file in `docs/`, grouped by area. Each entry gives a one-line summary taken from the file's own title and opening paragraph.

## Architecture guides

- [ARCHITECTURE.md](ARCHITECTURE.md) - How the bridge is assembled: components, layers, and request flow.
- [StartupPipeline.md](StartupPipeline.md) - The startup sequence in `Program.cs`, from host build to middleware and endpoint mapping.
- [GETTING_STARTED.md](GETTING_STARTED.md) - Step-by-step guide to running the bridge and making a first call.
- [DEPLOYMENT.md](DEPLOYMENT.md) - Production deployment strategies for each supported environment.
- [PERFORMANCE_TUNING.md](PERFORMANCE_TUNING.md) - Settings for maximising throughput and minimising latency.
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Common problems and how to diagnose them.
- [FAQ.md](FAQ.md) - Quick answers to common questions.
- [SWAGGER_OPENAPI.md](SWAGGER_OPENAPI.md) - Enabling Swagger UI and the OpenAPI 3.0 document.
- [EXCEPTIONS_REFERENCE.md](EXCEPTIONS_REFERENCE.md) - Every exception type, its hierarchy, properties, and HTTP mapping.

## Controllers & endpoints

- [BridgeController.md](BridgeController.md) - `api/bridge` controller handling single invoke, stream, and batch calls.
- [ConfigurationController.md](ConfigurationController.md) - Controller for reading and updating application settings over gRPC-Web.
- [HealthCheckController.md](HealthCheckController.md) - Endpoints for service health, diagnostics, and resource metrics.
- [MetricsController.md](MetricsController.md) - Endpoints for retrieving and resetting runtime metrics.
- [StreamDiagnosticsController.md](StreamDiagnosticsController.md) - Endpoints returning diagnostics for bidirectional streams.
- [HealthChecks.md](HealthChecks.md) - The three `IHealthCheck` implementations and the shared JSON response writer.
- [HealthEndpoints.md](HealthEndpoints.md) - Registration of the health HTTP endpoints and their response models.

## Services

- [AuthenticationService.md](AuthenticationService.md) - Central authentication and authorization of gRPC requests, including JWT.
- [BridgeActivitySource.md](BridgeActivitySource.md) - The shared `ActivitySource` used for bridge tracing spans.
- [BridgePrometheusMetrics.md](BridgePrometheusMetrics.md) - Opt-in Prometheus metrics for bridge calls and streams.
- [CacheManager.md](CacheManager.md) - Thread-safe in-memory cache with typed access, expiration, and statistics.
- [EventBus.md](EventBus.md) - In-process publish/subscribe event bus for decoupled communication.
- [GrpcConnectionManager.md](GrpcConnectionManager.md) - Lifecycle of pooled gRPC channels: creation, reuse, closure, and telemetry.
- [ProtocolTranslationService.md](ProtocolTranslationService.md) - Core translation between HTTP/JSON and gRPC/protobuf messages.
- [ReflectionService.md](ReflectionService.md) - Runtime discovery and inspection of gRPC services.
- [ReflectionDescriptors.md](ReflectionDescriptors.md) - Descriptor models returned by the reflection APIs.
- [RegistrySnapshot.md](RegistrySnapshot.md) - Point-in-time snapshot of the service registry.
- [ServiceRegistry.md](ServiceRegistry.md) - In-memory catalog of gRPC services with lookup, update, and enumeration.
- [IServiceRepository.md](IServiceRepository.md) - Interface for gRPC service data access.
- [ServiceRepository.md](ServiceRepository.md) - Asynchronous storage of service definitions and their request/response message types.
- [TracingService.md](TracingService.md) - Tracing of gRPC calls, protocol translations, and authentication activity.

## Integration

- [CorrelationIdManager.md](CorrelationIdManager.md) - Creation and tracking of correlation IDs across execution traces.
- [HttpClientFactory.md](HttpClientFactory.md) - Central management and reuse of configured `HttpClient` instances.
- [RequestContextManager.md](RequestContextManager.md) - Request-scoped ambient context carried through `AsyncLocal` storage.
- [ServiceDiscoveryClient.md](ServiceDiscoveryClient.md) - Managed service registration, discovery, and health monitoring.
- [WebhookPublisher.md](WebhookPublisher.md) - Lifecycle and dispatch of webhook subscriptions and events.

## Background workers

- [HealthCheckWorker.md](HealthCheckWorker.md) - Timer-based background loop that periodically health-checks a gRPC service.
- [MetricsCollectionWorker.md](MetricsCollectionWorker.md) - Background aggregation of runtime performance data.
- [StreamCleanupService.md](StreamCleanupService.md) - Hosted service that periodically triggers idle-stream cleanup.
- [StreamCleanupWorker.md](StreamCleanupWorker.md) - Worker that finds and removes stale streams based on timeouts.
- [StreamDiagnosticsService.md](StreamDiagnosticsService.md) - Hosted service that periodically collects stream throughput metrics.
- [StreamingWorkerSupervisor.md](StreamingWorkerSupervisor.md) - Supervisor that monitors a streaming service and recovers it when unhealthy.

## Streaming

- [AdaptiveFlowController.md](AdaptiveFlowController.md) - Hosted service that adds flow-control credit to streams with a low window.
- [BackpressureController.md](BackpressureController.md) - Tracks and enforces backpressure credits for one bidirectional stream.
- [BidirectionalStreamContext.md](BidirectionalStreamContext.md) - Lifecycle and flow-control context coordinating message exchange on a stream.
- [BidirectionalStreamingEngine.md](BidirectionalStreamingEngine.md) - Manages the lifecycle of bidirectional gRPC-Web streams.
- [FlowControlledStream.md](FlowControlledStream.md) - Stream wrapper pairing a channel with a backpressure controller.
- [FlowControlOptions.md](FlowControlOptions.md) - Limits and policies for inbound and outbound stream flow control.
- [IStreamingContracts.md](IStreamingContracts.md) - The streaming interface contracts and what each one requires.
- [IStreamingSessionManager.md](IStreamingSessionManager.md) - Interface for the manager that groups related streams into sessions.
- [StreamingService.md](StreamingService.md) - A bidirectional gRPC stream's state and its management operations.
- [StreamingSession.md](StreamingSession.md) - A logical session grouping one or more streams under a client connection.
- [StreamingSessionManager.md](StreamingSessionManager.md) - Creates, tracks, and removes `StreamingSession` instances.
- [StreamMessage.md](StreamMessage.md) - Transport-agnostic message container for unary responses, chunks, heartbeats, and errors.
- [StreamDiagnosticsOptions.md](StreamDiagnosticsOptions.md) - Configuration and live telemetry snapshot for monitoring streaming connections.

## Middleware

- [ContentTypeValidationMiddleware.md](ContentTypeValidationMiddleware.md) - Validates `Content-Type` and rejects unsupported requests.
- [ErrorHandlingMiddleware.md](ErrorHandlingMiddleware.md) - Turns unhandled exceptions into `ProblemDetails` responses.
- [MaxBodySizeMiddleware.md](MaxBodySizeMiddleware.md) - Rejects requests whose declared body size exceeds a limit.
- [RateLimitingMiddleware.md](RateLimitingMiddleware.md) - Per-client and global request rate limiting (opt-in).
- [RequestLoggingMiddleware.md](RequestLoggingMiddleware.md) - Structured logging of incoming requests.
- [RouteHeaderTransformMiddleware.md](RouteHeaderTransformMiddleware.md) - Applies header transformation hooks per route (opt-in).
- [IRouteHeaderTransformHook.md](IRouteHeaderTransformHook.md) - Interface for hooks that transform request and response headers.

## Utilities

- [CacheUtility.md](CacheUtility.md) - Cache key generation, validation, and statistics helpers.
- [CryptographyUtility.md](CryptographyUtility.md) - Password hashing, token generation, API key management, and data encryption helpers.
- [CsvFormatter.md](CsvFormatter.md) - Conversion between CSV text and .NET objects.
- [DateTimeUtility.md](DateTimeUtility.md) - ISO 8601 serialization and other date and time operations.
- [JsonFormatter.md](JsonFormatter.md) - Serializing objects to JSON, manipulating JSON strings, and validating payloads.
- [JsonUtility.md](JsonUtility.md) - JSON serialization, deserialization, merging, and property manipulation.
- [ProtobufUtility.md](ProtobufUtility.md) - Static helpers for working with Protocol Buffers messages.
- [ReflectionUtility.md](ReflectionUtility.md) - Helpers for querying members, invoking methods, and accessing properties.
- [ResponseFormatter.md](ResponseFormatter.md) - Builders for structured response objects returned by the bridge.
- [StreamUtility.md](StreamUtility.md) - Chunked asynchronous copying between streams.
- [ValidationUtility.md](ValidationUtility.md) - Validation and sanitization helpers for strings, collections, and numeric ranges.
- [XmlFormatter.md](XmlFormatter.md) - Formatting and serializing XML content.

## Configuration

- [DependencyInjection.md](DependencyInjection.md) - `AddGrpcWebBridge` and the other extension methods that register bridge services.
- [GrpcWebBridgeOptions.md](GrpcWebBridgeOptions.md) - Fluent `With*` builder for configuring the bridge.
- [GrpcWebBridgeOptionsValidator.md](GrpcWebBridgeOptionsValidator.md) - Validation rules for bridge options, including timeouts.
- [BridgeConfiguration.md](BridgeConfiguration.md) - The mutable configuration model behind the options builder.
- [StartupConfiguration.md](StartupConfiguration.md) - Startup settings, operational limits, and runtime metadata for an instance.
- [HttpTranscoderOptionsValidator.md](HttpTranscoderOptionsValidator.md) - Validation of `HttpTranscoderOptions` at startup.

## Domain models & exceptions

- [GrpcService.md](GrpcService.md) - A gRPC service definition exposed through the bridge.
- [GrpcServiceDescriptor.md](GrpcServiceDescriptor.md) - Metadata describing a service and its methods.
- [GrpcMethod.md](GrpcMethod.md) - A single gRPC method definition: identity, request and response types.
- [MethodParameter.md](MethodParameter.md) - Metadata for a method parameter: name, type, field number, and validation rules.
- [GrpcRequest.md](GrpcRequest.md) - Request container with metadata, payload, and timing.
- [GrpcResponse.md](GrpcResponse.md) - Response container carrying output, status, and metadata.
- [AuthenticationContext.md](AuthenticationContext.md) - Identity, roles, claims, and metadata for an authenticated request.
- [Enums.md](Enums.md) - Every enum in `GrpcWebBridge.Domain` and `EnumExtensions.ToDisplayString`.
- [Constants.md](Constants.md) - Application-wide constants grouped by concern.
- [ProblemDetails.md](ProblemDetails.md) - The RFC 7807-style error payload returned to HTTP clients.
- [GrpcWebBridgeException.md](GrpcWebBridgeException.md) - Base exception with an error code, gRPC status, and context dictionary.
- [ConfigurationException.md](ConfigurationException.md) - Exception for configuration errors, carrying the offending key.
- [ProtocolException.md](ProtocolException.md) - Exception for failures during HTTP/gRPC protocol translation.
- [StreamingException.md](StreamingException.md) - Exception for stream lifecycle failures, carrying the stream ID and state.
- [ValidationException.md](ValidationException.md) - Exception for input or data validation failures.
- [ServiceRegistrationException.md](ServiceRegistrationException.md) - Exception for errors during service registration.

## Extensions & validation

- [BackpressureControllerExtensions.md](BackpressureControllerExtensions.md) - Static helpers for managing backpressure credits.
- [BidirectionalStreamingEngineExtensions.md](BidirectionalStreamingEngineExtensions.md) - Helpers for inspecting and monitoring streaming channels.
- [ConfigurationExceptionExtensions.md](ConfigurationExceptionExtensions.md) - Fluent helpers for attaching and reading `ConfigurationException` context.
- [CorrelationIdManagerExtensions.md](CorrelationIdManagerExtensions.md) - Checks whether a valid correlation ID is active in the current context.
- [CsvFormatterExtensions.md](CsvFormatterExtensions.md) - Extension methods converting objects to CSV and manipulating CSV files.
- [EventBusExtensions.md](EventBusExtensions.md) - Helpers for checking subscribers and conditionally publishing events.
- [ExceptionExtensions.md](ExceptionExtensions.md) - Overview of the small exception helper classes without their own page.
- [ExceptionExtensionsCatalog.md](ExceptionExtensionsCatalog.md) - Reference for the five exception helper files not covered by per-type pages.
- [GrpcConnectionManagerExtensions.md](GrpcConnectionManagerExtensions.md) - Extension methods for managing and monitoring gRPC connections.
- [GrpcMethodExtensions.md](GrpcMethodExtensions.md) - Helpers that analyze and transform gRPC method descriptors.
- [GrpcRequestExtensions.md](GrpcRequestExtensions.md) - Helpers for inspecting metadata and logging `GrpcRequest` instances.
- [GrpcResponseExtensions.md](GrpcResponseExtensions.md) - Helpers for creating `GrpcResponse` instances and adding metadata.
- [GrpcServiceValidation.md](GrpcServiceValidation.md) - Validation of `GrpcService` instances, returning human-readable problems.
- [HealthCheckWorkerValidation.md](HealthCheckWorkerValidation.md) - `Validate`, `IsValid`, and `EnsureValid` for `HealthCheckWorker`.
- [HttpContextGrpcExtensions.md](HttpContextGrpcExtensions.md) - Helpers for identifying gRPC content types and reading the gRPC method path.
- [InfrastructureJsonExtensions.md](InfrastructureJsonExtensions.md) - Planned JSON helper extensions; the page notes these files are not in the current tree.
- [MetricsCollectionWorkerExtensions.md](MetricsCollectionWorkerExtensions.md) - Static helpers extending the metrics collection worker.
- [MiddlewareFormatterJsonExtensions.md](MiddlewareFormatterJsonExtensions.md) - Planned middleware, formatter, and exception JSON extensions; the page notes these files are not in the current tree.
- [ReflectionServiceExtensions.md](ReflectionServiceExtensions.md) - Registers `ReflectionService` in the DI container.
- [ServiceDiscoveryClientExtensions.md](ServiceDiscoveryClientExtensions.md) - Registration, discovery, health checking, and cache helpers for service discovery.
- [ServiceDiscoveryClientValidation.md](ServiceDiscoveryClientValidation.md) - Validation of `ServiceInstance` before use with `ServiceDiscoveryClient`.
- [ServiceExtensions.md](ServiceExtensions.md) - Static helpers and data structures for service registration.
- [ServiceRegistryExtensions.md](ServiceRegistryExtensions.md) - Query and inspection helpers for the service registry.
- [StreamingExceptionExtensions.md](StreamingExceptionExtensions.md) - Helpers for inspecting stream state and metadata on `StreamingException`.
- [StreamingExtensions.md](StreamingExtensions.md) - Registration of the default bidirectional streaming infrastructure.
- [StreamingServiceValidation.md](StreamingServiceValidation.md) - Validation of streaming service configuration and runtime state.
- [StreamMessageValidation.md](StreamMessageValidation.md) - Validation rules for `StreamMessage` instances.
- [ValidationExceptionExtensions.md](ValidationExceptionExtensions.md) - Helpers for processing and transforming `ValidationException` instances.
- [WebhookPublisherExtensions.md](WebhookPublisherExtensions.md) - Helpers for webhook subscriptions and event publishing.
- [XmlFormatterExtensions.md](XmlFormatterExtensions.md) - Extension methods for formatting and querying XML with `XDocument`.

## Examples

- [BasicUsageExample.md](BasicUsageExample.md) - Minimal example helper showing typical bridge calls.
- [AdvancedUsageExample.md](AdvancedUsageExample.md) - Example patterns for resilience, circuit breaking, and streaming.
- [GrpcWebBridgeClientExample.md](GrpcWebBridgeClientExample.md) - Client implementation for calling the bridge service.

## Tests

- [AuthenticationServiceTests.md](AuthenticationServiceTests.md) - Tests for the authentication service.
- [BidirectionalStreamingEngineTests.md](BidirectionalStreamingEngineTests.md) - Tests for flow control, backpressure, and stream management.
- [CacheManagerTests.md](CacheManagerTests.md) - Tests for cache behavior, expiration, and edge cases.
- [ClientRateLimitTests.md](ClientRateLimitTests.md) - Tests for client-side rate limiting decisions.
- [ConfigurationControllerTests.md](ConfigurationControllerTests.md) - Tests for configuration retrieval and update endpoints.
- [ContentTypeValidationMiddlewareTests.md](ContentTypeValidationMiddlewareTests.md) - Tests for `Content-Type` validation.
- [CryptographyUtilityTests.md](CryptographyUtilityTests.md) - Tests for hashing and token generation.
- [DateTimeUtilityTests.md](DateTimeUtilityTests.md) - Tests for date and time utilities.
- [ErrorHandlingMiddlewareTests.md](ErrorHandlingMiddlewareTests.md) - Tests for exception-to-response mapping.
- [FlowControlOptionsEdgeCaseTests.md](FlowControlOptionsEdgeCaseTests.md) - Edge-case tests for `FlowControlOptions`.
- [GrpcWebBridgeOptionsTests.md](GrpcWebBridgeOptionsTests.md) - Tests for bridge options and environment-specific settings.
- [HealthCheckControllerTests.md](HealthCheckControllerTests.md) - Tests for health check endpoints under varied service states.
- [JsonFormatterTests.md](JsonFormatterTests.md) - Tests for the `JsonFormatter` utility.
- [JsonUtilityTests.md](JsonUtilityTests.md) - Tests for JSON serialization, merging, and validation.
- [ProtocolTranslationServiceTests.md](ProtocolTranslationServiceTests.md) - Tests for HTTP/JSON and gRPC/protobuf translation.
- [ReflectionServiceTests.md](ReflectionServiceTests.md) - Tests for runtime reflection of gRPC services.
- [ReflectionServiceTestsExtensions.md](ReflectionServiceTestsExtensions.md) - Test helpers for creating and inspecting gRPC services.
- [RequestLoggingMiddlewareTests.md](RequestLoggingMiddlewareTests.md) - Tests for request logging.
- [ServiceRegistryTests.md](ServiceRegistryTests.md) - Tests for service registry behavior.
- [ServiceRegistryTestsExtensions.md](ServiceRegistryTestsExtensions.md) - Test helpers for building and inspecting a `ServiceRegistry`.
- [ServiceRepositoryTests.md](ServiceRepositoryTests.md) - Tests for repository CRUD and request handling.
- [StreamUtilityTests.md](StreamUtilityTests.md) - Tests for chunked stream copying.
- [TracingServiceTests.md](TracingServiceTests.md) - Tests for tracing behavior.
- [ValidationUtilityTests.md](ValidationUtilityTests.md) - Tests for validation and sanitization helpers.

## Benchmarks

- [AuthenticationBenchmarks.md](AuthenticationBenchmarks.md) - BenchmarkDotNet scenarios for the authentication pipeline.
- [AuthenticationBenchmarksExtensions.md](AuthenticationBenchmarksExtensions.md) - Helpers for benchmarking and validating authentication mechanisms.
- [JsonUtilityBenchmarks.md](JsonUtilityBenchmarks.md) - Benchmarks for JSON serialization and deserialization.
- [ProtocolTranslationBenchmarks.md](ProtocolTranslationBenchmarks.md) - Benchmarks for protocol translation.
- [ProtocolTranslationBenchmarksExtensions.md](ProtocolTranslationBenchmarksExtensions.md) - Conversion helpers used by the protocol translation benchmarks.
- [StreamProcessingBenchmarks.md](StreamProcessingBenchmarks.md) - Benchmarks for stream processing efficiency.
