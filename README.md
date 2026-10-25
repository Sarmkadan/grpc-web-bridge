# GoalTracker Integration

## Table of Contents

- [Environment Variables](#environment-variables)
- [Usage](#usage)
- [Rules](#rules)
- [Goals List](#goals-list)
- [Add Step to Goal](#add-step-to-goal)
- [Getting Started](#getting-started)
- [REST/JSON transcoding](#restjson-transcoding)
- [StreamingExceptionExtensions](#streamingexceptionextensions)
- [GrpcRequestExtensions](#grpcrequestextensions)
- [StreamCleanupWorkerJsonExtensions](#streamcleanupworkerjsonextensions)
- [StreamingServiceValidation](#streamingservicevalidation)
- [DateTimeUtilityJsonExtensions](#datetimeutilityjsonextensions)
- [EventBusJsonExtensionsTests](#eventbusjsonextensionstests)
- [ServiceRegistryTestsExtensions](#serviceregistrytestsextensions)
- [ReflectionServiceTestsExtensions](#reflectionservicetestsextensions)
- [HealthEndpoints](#healthendpoints)
- [RequestContextManagerJsonExtensionsTests](#requestcontextmanagerjsonextensionstests)
- [EventBusExtensionsTests](#eventbusextensionstests)
- [CorrelationIdManagerTests](#correlationidmanagertests)
- [AdaptiveFlowControllerTests](#adaptiveflowcontrollertests)
- [ServiceDiscoveryClientValidationTests](#servicediscoveryclientvalidationtests)
- [EventBusTests](#eventbustests)
- [HealthEndpointsTests](#healthendpointstests)
- [Service Registration (DependencyInjection)](#service-registration-dependencyinjection)
- [HttpClientFactory](#httpclientfactory)
- [EventBus](#eventbus)
- [CacheManager](#cachemanager)
- [BridgeController](#bridgecontroller)
- [MetricsController](#metricscontroller)
- [HealthCheckController](#healthcheckcontroller)
- [ConfigurationController](#configurationcontroller)
- [StreamDiagnosticsController](#streamdiagnosticscontroller)
- [RateLimitingMiddleware](#ratelimitingmiddleware)
- [MaxBodySizeMiddleware](#maxbodysizemiddleware)
- [ContentTypeValidationMiddleware](#contenttypevalidationmiddleware)
- [RouteHeaderTransformMiddleware](#routeheadertransformmiddleware)
- [ErrorHandlingMiddleware](#errorhandlingmiddleware)
- [RequestLoggingMiddleware](#requestloggingmiddleware)
- [WebhookPublisher](#webhookpublisher)
- [TracingService and BridgeActivitySource](#tracingservice-and-bridgeactivitysource)
- [BridgePrometheusMetrics](#bridgeprometheusmetrics)
- [StreamingSessionManager](#streamingsessionmanager)
- [StreamDiagnosticsService](#streamdiagnosticsservice)
- [AuthenticationService](#authenticationservice)
- [ProtocolTranslationService](#protocoltranslationservice)
- [Response Formatters (JSON/XML/CSV)](#response-formatters-jsonxmlcsv)
- [CryptographyUtility](#cryptographyutility)
- [ValidationUtility](#validationutility)
- [License](#license)

For GoalTracker integration, credentials should be stored in environment variables rather than hardcoded in documentation.

## Environment Variables

```bash
# Set these environment variables before using the GoalTracker integration commands
# GOAL_TRACKER_USERNAME - Your GoalTracker username
# GOAL_TRACKER_PASSWORD - Your GoalTracker password  
# GOAL_TRACKER_BASE_URL - Base URL for GoalTracker API (default: https://tracker.sarmkadan.com)
```

## Usage

```bash
# 1. Get token from environment variables
TOKEN=$(curl -s -X POST "${GOAL_TRACKER_BASE_URL:-https://tracker.sarmkadan.com}/api/auth/login" \
-H 'Content-Type: application/json' \
-d "{\"username\":\"${GOAL_TRACKER_USERNAME}\",\"password\":\"${GOAL_TRACKER_PASSWORD}\"}" | python3 -c "import sys,json;print(json.load(sys.stdin)['token'])")

# 2. Log session (GOAL_ID = goal ID from tracker)
curl -s -X POST "${GOAL_TRACKER_BASE_URL:-https://tracker.sarmkadan.com}/api/goals/GOAL_ID/log" \
-H "Authorization: Bearer $TOKEN" \
-H 'Content-Type: application/json' \
-d '{"sessionId":"SESSION_ID","summary":"brief description of what was done"}'
```

## Rules
- `SESSION_ID` - current session ID (visible in `claude --resume`). If unknown, use timestamp format `manual-2026-06-01-1430`
- `summary` - 1-3 sentences describing what was accomplished in this session. Keep it factual.
- Log sessions at the END of work when complete
- If Vlad explicitly provides goal ID or name - log it. Otherwise - ask "log to GoalTracker?"
- Do not log sessions unrelated to any specific goal

## Goals List
`GET ${GOAL_TRACKER_BASE_URL:-https://tracker.sarmkadan.com}/api/goals` (with Bearer token)

## Add Step to Goal
```bash
curl -s -X POST "${GOAL_TRACKER_BASE_URL:-https://tracker.sarmkadan.com}/api/goals/GOAL_ID/steps" \
-H "Authorization: Bearer $TOKEN" \
-H 'Content-Type: application/json' \
-d '{"description":"step description"}'
```

---

# README

This document provides an overview of the gRPC Web Bridge project, including setup instructions, architecture, and usage examples.

## Getting Started

This walkthrough runs the bridge in an ASP.NET Core app and makes a first unary call from a browser. Read [Known limitations](#known-limitations) before relying on the result: the invoke endpoint does not forward calls to the backend yet.

### 1. Install the package

```bash
dotnet add package grpc-web-bridge
```

The package id is `grpc-web-bridge`. If it is not on your NuGet feed, reference the project directly instead:

```bash
dotnet add reference path/to/grpc-web-bridge/src/GrpcWebBridge/GrpcWebBridge.csproj
```

### 2. Register and wire the bridge in `Program.cs`

There is no single `UseGrpcWebBridge()` call. Register the bridge with `AddGrpcWebBridge`, add its CORS policy, and map its controllers. `AddBidirectionalStreaming()` is required: the bridge's `StreamingWorkerSupervisor` depends on the `EventBus` that it registers, and the host fails at startup without it.

```csharp
using GrpcWebBridge.Configuration;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Middleware;
using GrpcWebBridge.Services;
using GrpcWebBridge.Streaming;

var builder = WebApplication.CreateBuilder(args);

var bridgeOptions = new GrpcWebBridgeOptions()
    .AddAllowedOrigin("http://localhost:3000");

builder.Services.AddGrpcWebBridge(bridgeOptions);
builder.Services.AddGrpcWebBridgeCors(bridgeOptions);
builder.Services.AddBidirectionalStreaming(); // required, see above
builder.Services.AddControllers();

var app = builder.Build();

app.UseErrorHandling();
app.UseRouting();
app.UseCors("AllowGrpcWeb");
app.MapControllers();

// Register the Greeter service from the proto below so the bridge can route to it.
var registry = app.Services.GetRequiredService<ServiceRegistry>();
var greeter = new GrpcService("Greeter", "helloworld", "127.0.0.1", 50051);
greeter.AddMethod(new GrpcMethod(
    "SayHello", "helloworld.Greeter.SayHello", MethodType.Unary, "HelloRequest", "HelloReply"));
registry.RegisterService(greeter);

app.Run();
```

### 3. Describe the backend service in a `.proto` file

The bridge does not read `.proto` files. The names in the `Greeter` registration above (package, service, method, and message types) must match this file.

```proto
syntax = "proto3";

package helloworld;

service Greeter {
  rpc SayHello (HelloRequest) returns (HelloReply);
}

message HelloRequest {
  string name = 1;
}

message HelloReply {
  string message = 1;
}
```

### 4. Call a unary method from the browser

Serve the page from `http://localhost:3000`, the origin allowed in step 2. The bridge listens on `http://localhost:5000` by default; adjust the URL if you changed it.

```html
<script type="module">
  const response = await fetch('http://localhost:5000/api/bridge/invoke', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      serviceId: 'Greeter',
      methodName: 'SayHello',
      payload: { name: 'Ada' } // not forwarded yet, see Known limitations
    })
  });

  const result = await response.json();
  console.log(response.status, result); // 200 { success: true, data: "", metadata: {}, timestamp: "..." }
</script>
```

The bridge's browser endpoint takes JSON, not `application/grpc-web-text`. See Known limitations.

### Known limitations

- **Calls are not forwarded to the backend.** `ProtocolTranslationService.TranslateAndInvokeAsync` returns an OK response without opening a channel to the registered `Endpoint` and `Port`. The invoke endpoint therefore answers `success: true` with empty `data`.
- **JSON payloads are dropped.** `BridgeRequest.Payload` is forwarded only when it is a `byte[]`. A JSON object, as in the example above, arrives as an empty payload.
- **No `application/grpc-web-text` route.** The bridge's only browser endpoint is the JSON `POST /api/bridge/invoke`. Nothing in the bridge maps gRPC-Web paths such as `/helloworld.Greeter/SayHello`, so a grpc-web-text request to a bridged method is not handled.

## REST/JSON transcoding

> **Status: not wired up.** `HttpTranscoder` exists, but no endpoint calls it, and it does not read `google.api.http` annotations. The bridge does not serve REST paths such as `GET /v1/items/{id}` or `POST /v1/items` today. This section shows the intended proto, the registration, and what the bridge returns now.

### 1. Annotate the proto

The annotations come from `google/api/annotations.proto` in googleapis. The bridge ignores them, so this service is handled as a plain gRPC service.

```proto
syntax = "proto3";

package items.v1;

import "google/api/annotations.proto";

service ItemService {
  rpc GetItem (GetItemRequest) returns (Item) {
    option (google.api.http) = {
      get: "/v1/items/{id}"
    };
  }

  rpc CreateItem (CreateItemRequest) returns (Item) {
    option (google.api.http) = {
      post: "/v1/items"
      body: "*"
    };
  }
}

message GetItemRequest {
  string id = 1;
}

message CreateItemRequest {
  string name = 1;
}

message Item {
  string id = 1;
  string name = 2;
}
```

### 2. Register HttpTranscoder

`AddGrpcWebBridge` does not register `HttpTranscoder`. Add it yourself. Its dependencies, `ILogger<HttpTranscoder>` and `ProtocolTranslationService`, are registered by `AddGrpcWebBridge`, so one line is enough:

```csharp
using GrpcWebBridge.Configuration;
using GrpcWebBridge.Services;

builder.Services.AddGrpcWebBridge(bridgeOptions);
builder.Services.AddSingleton<HttpTranscoder>();
```

The transcoder only accepts gRPC-style paths, `/ServiceName/MethodName`, and passes the raw body through. Calling it with a REST path does not route the request: `/v1/items` is parsed as service `v1`, method `items`.

### 3. Call the REST paths

Both calls below are answered by ASP.NET Core's router, not the bridge, because no route is bound to them. The bridge returns a 404 with an empty body:

```bash
curl -i http://localhost:5000/v1/items/42
```

```http
HTTP/1.1 404 Not Found
Content-Length: 0
```

```bash
curl -i -X POST http://localhost:5000/v1/items \
  -H 'Content-Type: application/json' \
  -d '{"name":"widget"}'
```

```http
HTTP/1.1 404 Not Found
Content-Length: 0
```

Headers such as `Date` and `Server` are trimmed. The JSON success response for these calls is not shown because the bridge does not produce one yet.

### 4. Error output from the transcoder

If your endpoint calls `HttpTranscoder.TranscodeRequest` with a path that is not `/ServiceName/MethodName`, the transcoder throws `ProtocolException`. `ErrorHandlingMiddleware` returns it as HTTP 400. Example for `path=/SayHello`, trimmed (`traceId` and `timestamp` vary):

```json
{
  "success": false,
  "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
  "title": "Protocol Translation Failed",
  "error": "Protocol Translation Failed",
  "status": 400,
  "detail": "Invalid gRPC path format: '/SayHello'. Expected /ServiceName/MethodName",
  "message": "Invalid gRPC path format: '/SayHello'. Expected /ServiceName/MethodName",
  "details": {
    "sourceFormat": "unknown",
    "targetFormat": "unknown",
    "exceptionType": "GrpcWebBridge.Domain.Exceptions.ProtocolException"
  },
  "path": "/your-endpoint",
  "traceId": "...",
  "timestamp": "..."
}
```

A valid gRPC-style path, such as `/items.v1.ItemService/GetItem`, does not produce an error. It returns a `GrpcRequest` for that method.

### 5. gRPC status to HTTP status

When an exception is a `GrpcWebBridgeException` with `GrpcStatus` set, the HTTP status comes from `MapGrpcStatusToHttpStatus` in `src/GrpcWebBridge/Domain/Exceptions/ProblemDetailsExtensions.cs`:

| gRPC status | HTTP status |
|---|---|
| `InvalidArgument` | 400 Bad Request |
| `OutOfRange` | 400 Bad Request |
| `Unauthenticated` | 401 Unauthorized |
| `PermissionDenied` | 403 Forbidden |
| `NotFound` | 404 Not Found |
| `AlreadyExists` | 409 Conflict |
| `FailedPrecondition` | 412 Precondition Failed |
| `ResourceExhausted` | 429 Too Many Requests |
| `Aborted` | 500 Internal Server Error |
| `Internal` | 500 Internal Server Error |
| `DataLoss` | 500 Internal Server Error |
| `Unimplemented` | 501 Not Implemented |
| `Unavailable` | 503 Service Unavailable |
| `DeadlineExceeded` | 504 Gateway Timeout |
| anything else, including `Cancelled` and `Unknown` | 500 Internal Server Error |

Other exceptions do not use this table. For example, `ProtocolException` and `ValidationException` return 400, `UnauthorizedAccessException` returns 401, `TimeoutException` returns 504, and any unlisted exception returns 500.

## StreamingExceptionExtensions

The `StreamingExceptionExtensions` class provides a set of extension methods for working with `StreamingException` instances. These methods allow you to determine the state of a stream, add context to an exception, and retrieve stream-related context. For example, you can use the `IsTerminalState` method to check if a stream is in a terminal state, or use the `WithContext` method to add additional context to an exception:

```csharp
// Example usage of StreamingExceptionExtensions
var exception = new StreamingException("Stream failed");
if (exception.IsTerminalState(StreamState.Completed))
{
Console.WriteLine("Stream is in terminal state");
}
```

## GrpcRequestExtensions

The `GrpcRequestExtensions` class provides utility extension methods for working with `GrpcRequest` instances. These methods simplify access to request metadata, payload inspection, and request formatting for logging and debugging purposes.

For example, you can check if a request contains specific metadata, retrieve metadata values (including typed conversion), inspect payload properties, and generate formatted log strings:

```csharp
var request = new GrpcRequest
{
Id = Guid.NewGuid(),
FullMethodName = "/service/method",
PayloadFormat = "application/grpc+protobuf",
MethodType = "Unary",
TimeoutMilliseconds = 5000,
Payload = Encoding.UTF8.GetBytes("test payload"),
Metadata = new Dictionary<string, string>
{
{ "authorization", "Bearer token123" },
{ "user-id", "42" },
{ "request-id", Guid.NewGuid().ToString() }
}
};

// Check if metadata key exists
bool hasAuth = request.HasMetadataKey("authorization");

// Get metadata value as string
string? authToken = request.GetMetadataValue("authorization");

// Get metadata value with type conversion
int userId = request.GetMetadataValue<int>("user-id", 0);

// Check payload properties
int payloadSize = request.GetPayloadSize();
bool isEmpty = request.IsPayloadEmpty();
string payloadHash = request.GetPayloadHashHex();

// Generate formatted log string
string logEntry = request.ToLogString(includeMetadata: true);
Console.WriteLine(logEntry);
```

## StreamCleanupWorkerJsonExtensions

The `StreamCleanupWorkerJsonExtensions` class provides System.Text.Json serialization and deserialization extensions for `StreamCleanupWorker` instances. These methods enable round-trip serialization of worker state and configuration, allowing you to persist worker instances to JSON strings and reconstruct them later.

For example, you can serialize a worker to JSON, modify the JSON string, and deserialize it back to a worker instance:

```csharp
// Create a worker instance
var worker = new StreamCleanupWorker
{
WorkerId = Guid.NewGuid(),
StreamId = Guid.NewGuid(),
CleanupInterval = TimeSpan.FromMinutes(5),
MaxAge = TimeSpan.FromHours(2),
IsActive = true,
LastCleanup = DateTime.UtcNow
};

// Serialize to JSON
string json = worker.ToJson(indented: true);
Console.WriteLine(json);

// Deserialize from JSON
StreamCleanupWorker? restoredWorker = StreamCleanupWorkerJsonExtensions.FromJson(json);

// Try to deserialize with error handling
if (StreamCleanupWorkerJsonExtensions.TryFromJson(json, out var safeWorker))
{
Console.WriteLine($"Successfully restored worker: {safeWorker?.WorkerId}");
}
```

## StreamingServiceValidation

The `StreamingServiceValidation` class provides validation extension methods for `StreamingService` instances, allowing you to validate the state of streaming services and their active streams. These methods help ensure that streaming services are in a valid state before operations and provide detailed validation feedback.

For example, you can validate a streaming service instance and check if it's valid:

```csharp
// Create a streaming service with some streams
var streamingService = new StreamingService();

// Add some streams
streamingService.AddStream(new Stream
{
StreamId = Guid.NewGuid().ToString(),
MethodType = MethodType.ServerStreaming,
State = StreamState.Active,
MessageCount = 0,
CreatedAt = DateTime.UtcNow,
LastActivityTime = DateTime.UtcNow
});

// Validate the streaming service
IReadOnlyList<string> validationErrors = streamingService.Validate();

if (validationErrors.Count > 0)
{
Console.WriteLine("Validation failed:");
foreach (var error in validationErrors)
{
Console.WriteLine($"- {error}");
}
}
else
{
Console.WriteLine("Streaming service is valid!");
}

// Quick validation check
bool isValid = streamingService.IsValid();
Console.WriteLine($"Is valid: {isValid}");

// Ensure validation (throws if invalid)
try
{
streamingService.EnsureValid();
Console.WriteLine("Streaming service passed validation");
}
catch (ArgumentException ex)
{
Console.WriteLine($"Validation failed: {ex.Message}");
}
```

## DateTimeUtilityJsonExtensions

The `DateTimeUtilityJsonExtensions` class provides System.Text.Json serialization and deserialization extensions for `DateTime` and `DateTime?` values. These methods enable round-trip serialization of date/time values using ISO 8601 format, which is compatible with the DateTimeUtility operations.

For example, you can serialize a DateTime to JSON, deserialize it back, and handle potential parsing errors:

```csharp
// Create a DateTime value
DateTime now = DateTime.UtcNow;

// Serialize to JSON string
string json = now.ToJson(indented: true);
Console.WriteLine(json);

// Deserialize from JSON
DateTime? restoredDate = json.FromJson();
Console.WriteLine($"Restored date: {restoredDate}");

// Try to deserialize with error handling
if (json.TryFromJson(out var safeDate))
{
Console.WriteLine($"Successfully deserialized: {safeDate}");
}
else
{
Console.WriteLine("Failed to deserialize date");
}

// Serialize nullable DateTime
DateTime? nullableDate = DateTime.Now;
string nullableJson = nullableDate.ToJson();
Console.WriteLine(nullableJson);
```

## EventBusJsonExtensionsTests

The `EventBusJsonExtensionsTests` class provides comprehensive unit tests for the `EventBusJsonExtensions` class, covering serialization and deserialization scenarios including handling of nulls, invalid JSON, and formatting options. It ensures robust JSON processing for `EventBus` instances using `ToJson`, `FromJson`, and `TryFromJson` methods.

```csharp
// Example usage of EventBusJsonExtensions (which is tested by EventBusJsonExtensionsTests)
var eventBus = new EventBus(logger); // Assuming logger is available

// Serialize to JSON
string json = eventBus.ToJson(indented: true);

// Deserialize from JSON
var restoredEventBus = EventBusJsonExtensions.FromJson(json);

// Safe deserialization
if (EventBusJsonExtensions.TryFromJson(json, out var safeEventBus))
{
    // Process deserialized EventBus
}
```

## ServiceRegistryTestsExtensions

The `ServiceRegistryTestsExtensions` class provides extension methods for `ServiceRegistryTests` that simplify testing of service registry functionality. These methods create test services, manage registry state, and provide utilities for service discovery and health checks, making it easier to write comprehensive unit tests for service registration and discovery scenarios.

For example, you can create a test registry, register services, and perform various registry operations:

```csharp
// Create a test registry
var registry = new ServiceRegistryTests().CreateTestRegistry();

// Create and register a test service
var service = new ServiceRegistryTests().CreateAndRegisterTestService(
"TestService",
"TestPackage");

// Create and register multiple test services
var services = new ServiceRegistryTests().CreateAndRegisterTestServices(
3,
"MyPackage");

// Check if a service exists
bool exists = new ServiceRegistryTests().ServiceExists(
registry,
"TestPackage.TestService");

// Get a service or throw if not found
var foundService = new ServiceRegistryTests().GetServiceOrThrow(
registry,
"TestPackage.TestService");

// Update service status and get the updated service
var updatedService = new ServiceRegistryTests().UpdateAndGetServiceStatus(
registry,
"TestPackage.TestService",
ServiceStatus.Maintenance);

// Get service health status
var healthStatus = new ServiceRegistryTests().GetServiceHealthStatus(
registry,
"TestPackage.TestService");

// List all services by package
var packageServices = new ServiceRegistryTests().ListServicesByPackage(
registry,
"MyPackage");
```

## ReflectionServiceTestsExtensions

The `ReflectionServiceTestsExtensions` class provides utility methods for testing reflection services in gRPC web bridge. It includes methods for creating test services, managing service registries, and verifying service registration states.

Example usage:

```csharp
// Get the service registry
var registry = ReflectionServiceTestsExtensions.GetServiceRegistry();

// Create and register a test service
var testService = ReflectionServiceTestsExtensions.CreateAndRegisterTestService();

// Create a test service with specific methods
var serviceWithMethods = ReflectionServiceTestsExtensions.CreateTestServiceWithMethods(
"TestService",
new[] { "Method1", "Method2" });

// Check if a service is registered
bool isRegistered = ReflectionServiceTestsExtensions.IsServiceRegistered("TestService");

// Get all registered services
var allServices = ReflectionServiceTestsExtensions.GetAllServices();

// Get the reflection service
var reflectionService = ReflectionServiceTestsExtensions.GetReflectionService();
```

## HealthEndpoints

The `HealthEndpoints` class provides static methods for setting up health check endpoints in an ASP.NET Core application. It includes endpoints for liveness, readiness, and detailed health checks, exposing service and system status information.

Example usage:

```csharp
var builder = WebApplication.CreateBuilder(args);
// ... configure services, including registering health checks and services ...
var app = builder.Build();

// Get the application startup time
DateTime startupTime = HealthEndpoints.GetStartupTime();

// Map health endpoints
app.MapHealthEndpoints();

app.Run();
```

## RequestContextManagerJsonExtensionsTests

The `RequestContextManagerJsonExtensionsTests` class provides unit tests for the `RequestContextManagerJsonExtensions` class, covering JSON serialization and deserialization of `RequestContextManager` instances. It tests various scenarios including handling of null values, formatting options, and round-trip consistency.

Example usage of `RequestContextManagerJsonExtensions` (which is tested by `RequestContextManagerJsonExtensionsTests`):

```csharp
// Create a RequestContextManager instance
var context = new RequestContextManager
{
    UserId = 123,
    SessionId = Guid.NewGuid(),
    // Initialize other properties as needed
};

// Serialize to JSON (compact)
string json = context.ToJson();

// Serialize to JSON (indented)
string indentedJson = context.ToJson(indented: true);

// Deserialize from JSON
RequestContextManager? restored = RequestContextManagerJsonExtensions.FromJson(json);

// Safe deserialization
if (RequestContextManagerJsonExtensions.TryFromJson(json, out var safeContext))
{
    // Process deserialized context
}
```

## EventBusExtensionsTests

The `EventBusExtensionsTests` class provides comprehensive unit tests for the `EventBusExtensions` class, covering subscriber management, conditional publishing, event history serialization, and subscriber reset functionality. It ensures robust behavior of event bus extension methods including null checking and proper event handling.

Example usage of `EventBusExtensions` (which is tested by `EventBusExtensionsTests`):

```csharp
// Create an event bus with a logger
var logger = Substitute.For<ILogger<EventBus>>();
var eventBus = new EventBus(logger, maxHistorySize: 100);

// Check if subscribers exist for an event type
bool hasSubscribers = eventBus.HasSubscribers<MyEvent>();

// Publish event only if subscribers exist
await eventBus.PublishIfHasSubscribersAsync(myEvent);

// Get event history as JSON
string historyJson = eventBus.GetEventHistoryJson();

// Reset/clear all subscribers
eventBus.Reset();
```

## CorrelationIdManagerTests

The `CorrelationIdManagerTests` class provides comprehensive unit tests for the `CorrelationIdManager` class, validating correlation ID generation, preservation, async-local flow, and distributed tracing capabilities. It ensures proper handling of correlation IDs across asynchronous operations and trace management.

Example usage of `CorrelationIdManager` (which is tested by `CorrelationIdManagerTests`):

```csharp
// Create a logger and correlation ID manager
var logger = Substitute.For<ILogger<CorrelationIdManager>>();
var correlationManager = new CorrelationIdManager(logger);

// Generate or get correlation ID (creates new if none exists)
string correlationId = correlationManager.GetOrCreateCorrelationId();

// Set a custom correlation ID
correlationManager.SetCorrelationId("custom-correlation-id-123");

// Get current correlation ID
string currentId = correlationManager.GetCorrelationId();

// Start a new trace operation
var trace = correlationManager.StartTrace("database-query", 
    new Dictionary<string, string> { { "query", "SELECT * FROM Users" } });

// Complete the trace with success status
correlationManager.CompleteTrace(trace.TraceId, success: true);

// Get all traces for a correlation ID
List<CorrelationTrace> traces = correlationManager.GetTracesForCorrelation(correlationId);

// Get tracing statistics
object stats = correlationManager.GetStatistics();
```

## AdaptiveFlowControllerTests

The `AdaptiveFlowControllerTests` class provides comprehensive unit tests for the `AdaptiveFlowController` background service, which periodically adjusts credit windows for streams operating in `FlowControlMode.Adaptive`. It verifies the three window-adjustment strategies (widen below the low-utilization threshold, hold above the high-utilization threshold, and neutral in between), the handling of throttled and missing streams, and the configuration presets and thresholds used by the controller.

Example usage of `AdaptiveFlowController` (which is tested by `AdaptiveFlowControllerTests`):

```csharp
// Create a bidirectional streaming engine
var engine = Substitute.For<IBidirectionalStreamingEngine>();

// Configure adaptive flow control
var options = new FlowControlOptions
{
    Mode = FlowControlMode.Adaptive,
    InitialWindowSize = 64,
    MaxWindowSize = 256,
    CreditReplenishmentBatch = 16,
    AdaptiveAdjustmentInterval = TimeSpan.FromSeconds(1),
    BackpressureThreshold = 0.85
};

// Create the controller and start it as a hosted background service
var logger = Substitute.For<ILogger<AdaptiveFlowController>>();
var controller = new AdaptiveFlowController(engine, options, logger);

// Register it with the host so ExecuteAsync runs on each adjustment interval
builder.Services.AddHostedService(_ => controller);

## BackpressureControllerTests

The `BackpressureControllerTests` class provides comprehensive unit tests for the `BackpressureController` class, covering credit-based flow control mechanisms, backpressure signaling, boundary conditions, and thread safety. It tests credit acquisition and release, pressure calculation under load, throttling behavior, disabled mode operation, and concurrent access patterns.

Example usage of `BackpressureController` (which is tested by `BackpressureControllerTests`):

```csharp
// Create a backpressure controller with default options
var controller = new BackpressureController(
    "test-stream",
    new FlowControlOptions
    {
        Mode = FlowControlMode.CreditWindow,
        InitialWindowSize = 10,
        MaxWindowSize = 20,
        BackpressureThreshold = 0.8
    },
    NullLogger<BackpressureController>.Instance);

// Try to consume credits
bool success = controller.TryConsumeCredit(3);
if (success)
{
    // Credits consumed successfully
    int available = controller.AvailableCredits; // Should be 7
}

// Release credits back to the pool
controller.ReleaseCredit(2);

// Check current pressure/utilization level
double utilization = controller.WindowUtilization; // Value between 0.0 and 1.0
bool isThrottled = controller.IsThrottled;

// Reset to initial state
controller.ResetWindow();

## ServiceDiscoveryClientValidationTests

The `ServiceDiscoveryClientValidationTests` class provides unit tests for the `ServiceDiscoveryClientValidation` extension methods that validate a `ServiceInstance` before it is registered with the discovery client. It verifies that `Validate` returns a list of human-readable problems for invalid instances (empty identifiers, out-of-range ports, invalid statuses, future or default heartbeats, and malformed metadata), that `IsValid` returns a boolean summary, and that `EnsureValid` throws an `ArgumentException` carrying the collected problems when the instance is invalid.

Example usage of `ServiceDiscoveryClientValidation` (which is tested by `ServiceDiscoveryClientValidationTests`):

```csharp
// Build a service instance to register with the discovery client
var instance = new ServiceInstance
{
    Id = "service-1",
    Name = "TestService",
    Host = "localhost",
    Port = 5000,
    Status = "UP",
    RegisteredAt = DateTime.UtcNow.AddMinutes(-1),
    LastHeartbeat = DateTime.UtcNow.AddSeconds(-30)
};

// Validate the instance before registration
var problems = instance.Validate();
if (problems.Any())
{
    throw new InvalidOperationException(string.Join(Environment.NewLine, problems));
}

// Or use the boolean convenience check
if (!instance.IsValid())
{
    throw new InvalidOperationException("Service instance is not valid.");
}

// Or throw directly with the collected problems when invalid
instance.EnsureValid();
```

## EventBusTests

The `EventBusTests` class provides comprehensive unit tests for the `EventBus` class, verifying its publish-subscribe functionality, event history management, disposal behavior, and thread safety. It tests subscribing with synchronous and asynchronous handlers, unsubscribing, publishing events, handling exceptions in handlers, and proper disposal of resources.

Example usage of `EventBus` (which is tested by `EventBusTests`):

```csharp
// Create a logger and event bus
var logger = Substitute.For<ILogger<EventBus>>();
var eventBus = new EventBus(logger, maxHistorySize: 100);

// Subscribe to events with synchronous and asynchronous handlers
var serviceHandlerCalled = false;
void ServiceHandler(ServiceRegisteredEvent @event) => serviceHandlerCalled = true;
eventBus.Subscribe<ServiceRegisteredEvent>(ServiceHandler);

// Publish an event
var @event = new ServiceRegisteredEvent
{
    ServiceId = "test-service-id",
    ServiceName = "TestService",
    Endpoint = "http://localhost:5000"
};
await eventBus.PublishAsync(@event);

// Verify handler was called
serviceHandlerCalled.Should().BeTrue();

// Check subscriber count
eventBus.GetSubscriberCount<ServiceRegisteredEvent>().Should().Be(1);

// Unsubscribe
eventBus.Unsubscribe<ServiceRegisteredEvent>(ServiceHandler).Should().BeTrue();

// Dispose when finished
eventBus.Dispose();
```

## HealthEndpointsTests

The `HealthEndpointsTests` class provides comprehensive unit tests for the `HealthEndpoints` class, covering health check endpoints, startup time tracking, service health aggregation, and response model validation. It tests mapping health endpoints to web applications, validating detailed health responses, and verifying service health summary calculations.

Example usage of `HealthEndpoints` (which is tested by `HealthEndpointsTests`):

```csharp
// Get application startup time (returns consistent UTC DateTime)
var startupTime = HealthEndpoints.GetStartupTime();
startupTime.Should().NotBe(default);
startupTime.Kind.Should().Be(DateTimeKind.Utc);

// Map health endpoints to a web application
var builder = WebApplication.CreateBuilder();
var app = builder.Build();
HealthEndpoints.MapHealthEndpoints(app); // Should not throw

// Create sample health response objects
var detailedResponse = new HealthEndpoints.DetailedHealthResponse
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    uptime = "00:30:00",
    uptime_seconds = 1800,
    services = new HealthEndpoints.ServiceHealthSummary
    {
        registered_count = 3,
        health_status = "healthy",
        services = new List<HealthEndpoints.ServiceHealthItem>()
    },
    workers = new HealthEndpoints.WorkerStatusSummary
    {
        streaming_service = new HealthEndpoints.StreamingWorkerStatus
        {
            active_stream_count = 5,
            max_stream_count = 100,
            stream_capacity = "5/100",
            status = "active"
        }
    },
    system = new HealthEndpoints.SystemStatus
    {
        environment = "Development",
        application_name = "GrpcWebBridge",
        version = "1.0.0",
        timestamp = DateTime.UtcNow
    }
};

// Validate response properties
detailedResponse.status.Should().Be("healthy");
detailedResponse.services.registered_count.Should().Be(3);
detailedResponse.workers.streaming_service.active_stream_count.Should().Be(5);
```

## Service Registration (DependencyInjection)

`GrpcWebBridge.Configuration.DependencyInjection` provides seven extension methods for registering bridge services and optional integrations with an `IServiceCollection`:

```csharp
public static IServiceCollection AddGrpcWebBridge(
    this IServiceCollection services,
    GrpcWebBridgeOptions? options = null);

public static IServiceCollection AddGrpcWebBridge(
    this IServiceCollection services,
    Action<GrpcWebBridgeOptions> configureOptions);

public static IServiceCollection AddGrpcWebBridgeSwagger(
    this IServiceCollection services,
    string title = "gRPC-Web Bridge API",
    string version = "1.0.0");

public static IServiceCollection AddGrpcWebBridgeCors(
    this IServiceCollection services,
    GrpcWebBridgeOptions? options = null);

public static IServiceCollection AddGrpcWebBridgeAuthentication(
    this IServiceCollection services,
    Action<JwtBearerOptions>? configureJwtBearer = null);

public static IServiceCollection AddGrpcWebBridgePrometheus(
    this IServiceCollection services);

public static IServiceCollection AddGrpcWebBridgeTracing(
    this IServiceCollection services,
    string serviceName = "grpc-web-bridge",
    string? instanceName = null,
    Action<TracerProviderBuilder>? configureBuilder = null);
```

A minimal `Program.cs` can register the bridge and each optional integration as follows:

```csharp
using GrpcWebBridge.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpcWebBridge(options =>
{
    // Configure GrpcWebBridgeOptions here.
});
builder.Services.AddGrpcWebBridgeSwagger();
builder.Services.AddGrpcWebBridgeCors();
builder.Services.AddGrpcWebBridgeAuthentication();
builder.Services.AddGrpcWebBridgePrometheus();
builder.Services.AddGrpcWebBridgeTracing();

var app = builder.Build();
app.Run();
```

## HttpClientFactory

`GrpcWebBridge.Integration.HttpClientFactory` creates and caches named `HttpClient` instances. Configure it with `HttpClientFactoryOptions`, whose properties and defaults are:

| Property | Default | Purpose |
| --- | ---: | --- |
| `RequestTimeoutMs` | `30000` | Request timeout in milliseconds. Non-positive values fall back to 30 seconds. |
| `MaxConnectionsPerServer` | `10` | Maximum concurrent connections allowed per server. |
| `UseCookies` | `false` | Enables cookie handling on the underlying handler. |
| `AllowAutoRedirect` | `true` | Enables automatic HTTP redirects. |
| `AllowInsecureHttps` | `false` | Accepts any server certificate when enabled; use only in controlled environments. |
| `PooledConnectionLifetimeMs` | `120000` | Lifetime and idle timeout of pooled connections, in milliseconds. |

Factory-created clients are cached by name: repeated calls to `GetClient` with the same name return the same instance, while different names return distinct clients. All factory-created clients share one `SocketsHttpHandler` and therefore its connection pool. When `PooledConnectionLifetimeMs` has elapsed, the next `GetClient` call rotates the shared handler, disposes the cached clients that used it, and clears the cache. Set the lifetime to zero or a negative value to disable factory-level handler rotation.

The public methods are:

- `ToString()` returns a summary of the active client options.
- `GetClient(string name = "default")` gets or creates a cached named client.
- `RegisterClient(string name, HttpClient client)` registers a client, replacing and disposing any client already stored under that name.
- `GetClientForUri(string baseUri)` gets a client keyed by the URI and initializes its base address.
- `GetAsync(string uri, string? clientName = null)` sends a GET request and returns its content as a string.
- `PostJsonAsync(string uri, object payload, string? clientName = null)` serializes a payload as JSON, sends it with POST, and returns the response content.
- `SendAsync(string uri, HttpMethod method, HttpContent? content = null, Dictionary<string, string>? headers = null, string? clientName = null)` sends a custom request and returns the response.
- `RemoveClient(string name)` removes and disposes a named client, returning whether it existed.
- `GetRegisteredClientNames()` returns a snapshot of the currently cached names.
- `Dispose()` disposes the shared handler and all cached clients.

This example mirrors the named-client and timeout setup exercised by `HttpClientFactoryTests`:

```csharp
using System.Diagnostics;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging.Abstractions;

var options = new HttpClientFactoryOptions
{
    RequestTimeoutMs = 5_000,
    MaxConnectionsPerServer = 5,
    UseCookies = false,
    AllowAutoRedirect = true,
    AllowInsecureHttps = false
};

using var factory = new HttpClientFactory(
    NullLogger<HttpClientFactory>.Instance,
    options);

var client = factory.GetClient("backend");
client.BaseAddress = new Uri("https://example.com");

var sameClient = factory.GetClient("backend");
Debug.Assert(ReferenceEquals(client, sameClient));
Debug.Assert(client.Timeout == TimeSpan.FromMilliseconds(5_000));
```

## EventBus

`GrpcWebBridge.Events.EventBus` provides typed publish/subscribe handling for events derived from `EventBase`. Its four public publish/subscribe methods are:

- `Subscribe<TEvent>(Action<TEvent> handler)` registers a synchronous handler.
- `Subscribe<TEvent>(Func<TEvent, Task> handler)` registers an asynchronous handler.
- `Unsubscribe<TEvent>(Delegate handler)` removes a handler and returns whether it was registered.
- `PublishAsync<TEvent>(TEvent event)` records and dispatches an event to the handlers registered for its type.

Pass `EventBus.DispatchOptions` to the constructor to configure dispatch and backpressure. `UseAsyncDispatch` defaults to `false`, so handlers run inline and `PublishAsync` waits for them. When enabled, events use a bounded queue whose size is controlled by `MaxQueueSize` (default `1024`). `FullQueuePolicy` is either `DropOldest` (the default) or `Block`; `MaxWaitTime` (default five seconds) limits how long a blocked publisher waits for queue capacity.

The built-in event types are `ServiceRegisteredEvent`, `ServiceUnregisteredEvent`, `MethodInvokedEvent`, `StreamStartedEvent`, `StreamEndedEvent`, and `AuthenticationFailedEvent`. Each inherits the common `EventId`, `CreatedAt`, `Source`, and `Metadata` members from `EventBase`.

This synchronous-handler example follows the subscribe/publish pattern exercised by `EventBusTests`:

```csharp
using GrpcWebBridge.Events;
using Microsoft.Extensions.Logging.Abstractions;

using var eventBus = new EventBus(NullLogger<EventBus>.Instance);

void OnServiceRegistered(ServiceRegisteredEvent @event)
{
    Console.WriteLine($"Registered {@event.ServiceName} at {@event.Endpoint}");
}

eventBus.Subscribe<ServiceRegisteredEvent>(OnServiceRegistered);

await eventBus.PublishAsync(new ServiceRegisteredEvent
{
    ServiceId = "catalog-service",
    ServiceName = "Catalog",
    Endpoint = "https://localhost:5001"
});

eventBus.Unsubscribe<ServiceRegisteredEvent>(OnServiceRegistered);
```

## CacheManager

`GrpcWebBridge.Caching.CacheManager` is an in-memory, string-keyed cache with per-entry expiration and basic hit statistics. It is disposable; disposing it stops the periodic expired-entry cleanup and clears the cache.

Its cache operations are:

- `Set<T>(string key, T value)` stores a value using the configured default TTL.
- `Set<T>(string key, T value, TimeSpan ttl)` stores a value with a custom TTL, replacing an existing entry with the same key.
- `TryGet<T>(string key, out T? value)` retrieves a non-expired value and returns whether it was found. There is no separate `Get` method.
- `GetOrSet<T>(string key, Func<T> factory, TimeSpan? ttl = null)` returns a cached value or synchronously creates and caches one.
- `GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)` is the asynchronous factory variant.
- `Remove(string key)` removes one entry and returns whether it existed.
- `RemovePattern(string pattern)` removes entries whose keys match a case-insensitive pattern, where `*` is a wildcard, and returns the number removed.
- `Clear()` removes every entry.
- `GetStatistics()` returns a `CacheStatistics` snapshot containing entry and hit counts, average hits and estimated size, and the oldest and most-accessed entries.
- `Contains(string key)` reports whether a non-expired entry exists.
- `SetExpiration(string key, TimeSpan expiresIn)` resets an existing entry's expiration relative to the current time and returns whether the entry exists.
- `GetTimeToLive(string key)` returns the remaining TTL, zero for an expired entry still awaiting cleanup, or `null` when the key does not exist.

`CacheManagerOptions` exposes `DefaultTtl` (five minutes by default), `MaxEntries` (`10000`), and `EnableStatistics` (`true`). `DefaultTtl` controls the overloads that do not receive a TTL. `MaxEntries` and `EnableStatistics` are configuration properties but are not currently enforced or consulted by `CacheManager`.

This example follows the set, retrieval, TTL, and statistics patterns covered by `CacheManagerTests`:

```csharp
using GrpcWebBridge.Caching;
using Microsoft.Extensions.Logging.Abstractions;

var options = new CacheManagerOptions
{
    DefaultTtl = TimeSpan.FromMinutes(5)
};

using var cache = new CacheManager(
    NullLogger<CacheManager>.Instance,
    options);

cache.Set("user:42", "Ada");

if (cache.TryGet<string>("user:42", out var userName))
{
    Console.WriteLine(userName);
}

cache.SetExpiration("user:42", TimeSpan.FromMinutes(1));
var remaining = cache.GetTimeToLive("user:42");
var statistics = cache.GetStatistics();
```

## BridgeController

`src/GrpcWebBridge/Controllers/BridgeController.cs` is mounted at `api/bridge` (`[Route("api/bridge")]`) and produces `application/json`. It exposes three `POST` endpoints:

| Verb | Route | Purpose |
|------|-------|---------|
| POST | `/api/bridge/invoke` | Invoke one gRPC method and return a JSON result |
| POST | `/api/bridge/stream` | Open a streaming session and write JSON objects to the response until the client disconnects |
| POST | `/api/bridge/batch` | Invoke several methods in one request and return a result per operation |

Caveats that apply to all three endpoints:

- **No backend call is made.** `invoke` and `batch` go through `ProtocolTranslationService.TranslateAndInvokeAsync`, which returns an OK response without opening a channel to the registered endpoint. Responses echo the request payload.
- **Authentication is optional.** An `Authorization: Bearer <token>` header is read if present. A token that fails validation is treated as anonymous rather than rejected. When the token is valid, its user ID is added to the outbound gRPC metadata. Requests without a token are not rejected.
- **Service lookup.** `invoke` and `stream` match a service by `Id` or `Name`. `batch` matches by `Id` only.

### POST /api/bridge/invoke

Request body (`BridgeRequest`):

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `serviceId` | string | yes | Service `Id` or `Name` from the registry |
| `methodName` | string | yes | Method name within that service |
| `payload` | any JSON | no | Forwarded only when it binds to `byte[]`. JSON values bind to a JSON element, so in practice the payload is sent empty |
| `headers` | object (string → string) | no | Metadata for the gRPC request |
| `timeoutMs` | int | no | Call timeout in milliseconds. Defaults to `30000` |

Success (`200 OK`):

```json
{
  "success": true,
  "data": "",
  "metadata": {},
  "timestamp": "2026-10-25T12:00:00.0000000Z"
}
```

`data` is the response payload as a base64 string (`""` when empty), and `metadata` is the response metadata dictionary.

Errors:

| Status | Cause | Body |
|--------|-------|------|
| 400 | Request body is missing | `{ "error": "Request body is required" }` |
| 404 | Service not found | `{ "error": "Service '<serviceId>' not found" }` |
| 404 | Method not found in the service | `{ "error": "Method '<methodName>' not found in service" }` |
| 500 | Unhandled exception | `{ "error": "Method invocation failed", "details": "<exception message>" }` |

Example:

```bash
BRIDGE_URL="${BRIDGE_URL:-http://localhost:5000}"

curl -s -X POST "$BRIDGE_URL/api/bridge/invoke" \
  -H 'Content-Type: application/json' \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
        "serviceId": "greeter",
        "methodName": "SayHello",
        "headers": { "x-request-source": "docs" },
        "timeoutMs": 5000
      }'
```

### POST /api/bridge/stream

Request body (`StreamRequest`):

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `serviceId` | string | yes | Service `Id` or `Name` |
| `methodName` | string | yes | Method name |
| `initialMessage` | any JSON | no | Accepted by the model but not read by the endpoint |

The method type comes from the registry. If the service or method is unknown, the stream defaults to server streaming rather than returning `404`.

The response is `application/json` with no SSE framing. It is written as back-to-back JSON objects, with no delimiter between them:

1. A header object, written immediately:

   ```json
   {"status":"streaming","streamId":"<guid>","service":"greeter","method":"SayHello"}
   ```

2. For each message queued on the stream by other components, a message object:

   ```json
   {"sequenceNumber":1,"type":"<MessageType>","data":{}}
   ```

   The endpoint only drains the queue. Nothing in this controller enqueues messages, so only the header is written unless another component adds messages.

The stream is closed when the client disconnects (the request cancellation token fires) or when the handler exits. Cleanup runs in a `finally` block.

Errors:

| Status | Cause |
|--------|-------|
| 401 | The auth context reports the caller is not authenticated |
| 500 | Unhandled exception outside the streaming loop |

Example (`-N` disables curl buffering so messages print as they arrive):

```bash
curl -N -X POST "$BRIDGE_URL/api/bridge/stream" \
  -H 'Content-Type: application/json' \
  -d '{"serviceId":"greeter","methodName":"SayHelloStream"}'
```

### POST /api/bridge/batch

Request body (`BatchRequest`):

```json
{
  "operations": [
    {
      "id": "op-1",
      "serviceId": "greeter",
      "methodName": "SayHello",
      "payload": null,
      "headers": { "x-request-source": "docs" }
    }
  ]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `operations` | array | yes, at least one | Operations to run in order |
| `operations[].id` | string | no | Operation ID echoed in the result. Defaults to a new GUID |
| `operations[].serviceId` | string | yes | Service `Id` (not `Name`) |
| `operations[].methodName` | string | yes | Method name |
| `operations[].payload` | any JSON | no | Same binding rules as `invoke` |
| `operations[].headers` | object | no | Metadata for the gRPC request |

Success (`200 OK`). A failed operation does not fail the batch. Its entry has `success: false` and an `error` message:

```json
{
  "success": true,
  "operationCount": 1,
  "successCount": 1,
  "failureCount": 0,
  "results": [
    { "operationId": "op-1", "success": true, "data": "", "error": null }
  ],
  "timestamp": "2026-10-25T12:00:00.0000000Z"
}
```

Errors:

| Status | Cause | Body |
|--------|-------|------|
| 400 | `operations` is missing or empty | `{ "error": "At least one operation is required" }` |
| 500 | Failure outside the per-operation loop | `{ "error": "Batch invocation failed", "details": "<exception message>" }` |

Example:

```bash
curl -s -X POST "$BRIDGE_URL/api/bridge/batch" \
  -H 'Content-Type: application/json' \
  -d '{"operations":[{"id":"op-1","serviceId":"greeter","methodName":"SayHello"}]}'
```

## MetricsController

`src/GrpcWebBridge/Controllers/MetricsController.cs` is mounted at `api/metrics` (`[Route("api/metrics")]`) and produces `application/json`. It exposes four endpoints:

| Verb | Route | Purpose |
|------|-------|---------|
| GET | `/api/metrics` | System, request, stream, service and resource metrics in one object |
| GET | `/api/metrics/methods` | Call and error counts per gRPC method, busiest first |
| GET | `/api/metrics/streaming` | Active stream count and per-stream statistics (at most 100 streams) |
| POST | `/api/metrics/reset` | Clear request, error and per-method counters and restart the uptime clock |

Caveats:

- **Counters have no producer in `src/`.** Request and error totals come from the static methods `MetricsController.RecordMethodCall(methodName)` and `MetricsController.RecordMethodError(methodName)`. Nothing under `src/` calls them, so `totalRequests`, `totalErrors` and the method list stay empty until a caller wires them up (for example, a gRPC interceptor).
- **Counters are process-wide.** They are static fields, so they reset on restart and are shared by all requests. `reset` clears them but leaves live stream state alone.
- **`errorRate` is a percentage, not a fraction.** `2.5` means 2.5%. It is rounded to two decimals.
- **Stream utilization uses a fixed capacity.** `streamUtilization` is `activeStreams / maxStreams * 100`, where `maxStreams` is the controller's own constant `10000`. It does not read the configured limit.
- **No authorization attribute.** The controller has no `[Authorize]`, so `POST /api/metrics/reset` is open to any caller that can reach the bridge. Restrict it at the network or gateway level.

### GET /api/metrics

Success (`200 OK`). Every field sits under `data`:

| Field | Type | Description |
|-------|------|-------------|
| `data.systemMetrics.uptime` | object | `days`, `hours`, `minutes`, `seconds` (the clock components) and `totalSeconds` (the full span) since `startTime` |
| `data.systemMetrics.startTime` | datetime (UTC) | When the counters were last initialized or reset |
| `data.systemMetrics.currentTime` | datetime (UTC) | Server time when the response was built |
| `data.systemMetrics.processId` | int | OS process ID of the bridge |
| `data.requestMetrics.totalRequests` | long | Count recorded by `RecordMethodCall` |
| `data.requestMetrics.successfulRequests` | long | `totalRequests - totalErrors` |
| `data.requestMetrics.failedRequests` | long | Count recorded by `RecordMethodError` |
| `data.requestMetrics.errorRate` | double | `failedRequests / totalRequests * 100`, rounded to two decimals. `0` when there are no requests |
| `data.requestMetrics.averageRequestsPerMinute` | double | `totalRequests` divided by uptime in minutes, rounded to two decimals. `0` when there are no requests |
| `data.streamMetrics.activeStreams` | int | Streams currently open in `StreamingService` |
| `data.streamMetrics.maxStreams` | int | Fixed capacity used for utilization (`10000`) |
| `data.streamMetrics.streamUtilization` | double | `activeStreams / maxStreams * 100`, rounded to two decimals |
| `data.serviceMetrics.totalServices` | int | Services in the registry |
| `data.serviceMetrics.healthyServices` | int | Services whose `Status` is `Serving` |
| `data.serviceMetrics.totalMethods` | int | Methods across all registered services |
| `data.resourceMetrics.memoryUsageMb` | double | Managed heap size (`GC.GetTotalMemory(false)`) in MB |
| `data.resourceMetrics.processorCount` | int | Logical processors visible to the process |
| `data.resourceMetrics.workingSetMb` | double | Process working set in MB |

The top level also has `success` (always `true` on success) and `timestamp` (UTC).

Errors:

| Status | Body |
|--------|------|
| 500 | `{ "error": "Failed to retrieve metrics" }` |

Example response (values are illustrative):

```json
{
  "success": true,
  "data": {
    "systemMetrics": {
      "uptime": { "days": 2, "hours": 3, "minutes": 14, "seconds": 9, "totalSeconds": 184449.48 },
      "startTime": "2026-10-23T08:46:00.1234567Z",
      "currentTime": "2026-10-25T12:00:09.4821033Z",
      "processId": 4242
    },
    "requestMetrics": {
      "totalRequests": 1520,
      "successfulRequests": 1482,
      "failedRequests": 38,
      "errorRate": 2.5,
      "averageRequestsPerMinute": 0.49
    },
    "streamMetrics": {
      "activeStreams": 12,
      "maxStreams": 10000,
      "streamUtilization": 0.12
    },
    "serviceMetrics": {
      "totalServices": 4,
      "healthyServices": 3,
      "totalMethods": 17
    },
    "resourceMetrics": {
      "memoryUsageMb": 84.37,
      "processorCount": 4,
      "workingSetMb": 131.9
    }
  },
  "timestamp": "2026-10-25T12:00:09.4821033Z"
}
```

Example:

```bash
curl -s "$BRIDGE_URL/api/metrics" | jq '.data.requestMetrics'
```

### GET /api/metrics/methods

Success (`200 OK`). `methods` is sorted by `callCount`, highest first:

| Field | Type | Description |
|-------|------|-------------|
| `methodCount` | int | Number of distinct methods with at least one recorded call |
| `totalCalls` | long | Sum of `callCount` |
| `totalErrors` | long | Sum of `errorCount` |
| `methods[].method` | string | Method name as passed to `RecordMethodCall` |
| `methods[].callCount` | long | Calls recorded for this method |
| `methods[].errorCount` | long | Errors recorded for this method (`0` if none) |
| `methods[].errorRate` | double | Percentage of calls that failed, rounded to two decimals |

```json
{
  "success": true,
  "methodCount": 2,
  "totalCalls": 1520,
  "totalErrors": 38,
  "methods": [
    { "method": "greeter.SayHello", "callCount": 1100, "errorCount": 10, "errorRate": 0.91 },
    { "method": "orders.GetOrder", "callCount": 420, "errorCount": 28, "errorRate": 6.67 }
  ],
  "timestamp": "2026-10-25T12:00:09.4821033Z"
}
```

Errors: `500` with `{ "error": "Failed to retrieve method metrics" }`.

### GET /api/metrics/streaming

Success (`200 OK`):

| Field | Type | Description |
|-------|------|-------------|
| `activeStreamCount` | int | Total open streams |
| `displayedStreamCount` | int | Streams included in `streams`, capped at 100 |
| `totalMessageCount` | long | Sum of `messageCount` across the displayed streams only |
| `streams` | array | `StreamStatistics` objects: `streamId`, `messageCount`, `queuedMessageCount`, `state`, `createdAt`, `lastActivityTime` |

Errors: `500` with `{ "error": "Failed to retrieve streaming metrics" }`.

### POST /api/metrics/reset

Clears `totalRequests`, `totalErrors` and both per-method dictionaries, and sets `startTime` to now. Active streams are not affected. Logs a warning on each call.

Success (`200 OK`):

```json
{
  "success": true,
  "message": "Metrics reset successfully",
  "timestamp": "2026-10-25T12:05:00.0000000Z"
}
```

Errors: `500` with `{ "error": "Failed to reset metrics" }`.

Example:

```bash
curl -s -X POST "$BRIDGE_URL/api/metrics/reset"
```

## HealthCheckController

`src/GrpcWebBridge/Controllers/HealthCheckController.cs` is mounted at `api/health` (`[Route("api/health")]`) and produces `application/json`. It exposes six endpoints:

| Verb | Route | Purpose |
|------|-------|---------|
| GET | `/api/health` | Overall health for load balancers. Returns `200` or `503` (see below) |
| GET | `/api/health/detailed` | System, process, service, streaming and GC diagnostics |
| GET | `/api/health/services` | Per-service status with a summary of healthy and unhealthy counts |
| GET | `/api/health/resources` | Memory, CPU time, thread and GC counters |
| GET | `/api/health/ready` | Readiness: `200` once at least one service is `Serving`, otherwise `503` |
| GET | `/api/health/alive` | Liveness: always `200` while the process can respond |

Caveats:

- **Service health counts only `Serving`.** A service is healthy when its `Status` is `ServiceStatus.Serving`. Every other status, including `Unknown`, counts as unhealthy.
- **Stream capacity is a fixed constant.** `utilizationPercent` and `maxStreams` use the controller's private constant `10000`. They do not read the configured stream limit.
- **Counters are per process.** `alive.uptime` and the diagnostics uptime are measured from a static start time that is set when the controller type is first loaded. Restarts reset them.
- **No authorization attribute.** The controller has no `[Authorize]`, so every endpoint is open to any caller that can reach the bridge. `detailed` and `resources` expose process and host details, so restrict them at the gateway or network level.

### GET /api/health

Load-balancer check. The response is not wrapped in `data`. Fields are at the top level, and the counts sit under `metrics`.

Healthy rules:

- With no registered services, the bridge is healthy.
- With registered services, it is healthy when `healthyServices >= totalServices * 0.8` (at least 80% are `Serving`).

Responses:

| Code | Condition | Body |
|------|-----------|------|
| `200 OK` | Healthy by the rule above | `success: true`, `healthy: true`, `status: "healthy"` |
| `503 Service Unavailable` | Fewer than 80% of services are `Serving` | Same shape, with `healthy: false` and `status: "degraded"` |
| `503 Service Unavailable` | Unexpected exception while reading the registry | `FormatError` payload (`error: "Health Check Failed"`, `message: "Unable to determine system health"`). No `metrics` field |

Fields under `metrics` on the success and degraded responses:

| Field | Type | Description |
|-------|------|-------------|
| `metrics.totalServices` | int | Number of registered services |
| `metrics.healthyServices` | int | Services with `Status == Serving` |
| `metrics.unhealthyServices` | int | `totalServices - healthyServices` |
| `metrics.healthPercentage` | double | `healthyServices / totalServices * 100`, rounded to two decimals. `100` when there are no services |

Also present: `timestamp` (UTC) and `warnings` (null).

Example, with 4 of 5 services serving (`80%`, so `200`):

```json
{
  "success": true,
  "healthy": true,
  "status": "healthy",
  "metrics": {
    "totalServices": 5,
    "healthyServices": 4,
    "unhealthyServices": 1,
    "healthPercentage": 80
  },
  "warnings": null,
  "timestamp": "2026-10-25T12:00:00Z"
}
```

With 3 of 5 serving (`60%`), the same endpoint returns `503` with `"healthy": false` and `"status": "degraded"`.

### GET /api/health/detailed

Success (`200 OK`). Every field sits under `data`. On exception it returns `500` with `{ "error": "Diagnostics Failed" }`.

The streaming block is the only place `utilizationPercent` appears:

| Field | Type | Description |
|-------|------|-------------|
| `data.streaming.activeStreams` | int | Streams currently open in `StreamingService` |
| `data.streaming.maxStreams` | int | Fixed `10000` |
| `data.streaming.utilizationPercent` | double | `activeStreams / maxStreams * 100`, rounded to two decimals. Can exceed `100` if more than `10000` streams are open |

The other blocks are `data.uptime`, `data.system`, `data.services` (with per-service `details`) and `data.gcMetrics`.

### Other endpoints

- `GET /api/health/services` returns `data.summary` (`totalServices`, `healthyServices`, `unhealthyServices`) and `data.services`. Each item has `status`, `isHealthy`, `endpoint`, `port`, `lastUpdated` and `methodCount`. On exception it returns `500`.
- `GET /api/health/resources` returns `data.memory`, `data.processor` (`cpuUsagePercent` is total processor time in seconds, not a percentage) and `data.gc`. On exception it returns `500`.
- `GET /api/health/ready` returns `{ "ready": true, "reason": "...", "servicesAvailable": n }` with `200`. It returns `503` with the same shape and `ready: false` when no service is `Serving`. On exception it returns `503` with `{ "ready": false, "reason": "Unable to determine readiness" }`.
- `GET /api/health/alive` always returns `200` with `{ "alive": true, "timestamp": "...", "uptime": <seconds> }`. It does not check services.

Example:

```bash
curl -s -o /dev/null -w "%{http_code}\n" "$BRIDGE_URL/api/health"
curl -s "$BRIDGE_URL/api/health/detailed" | jq '.data.streaming'
```

## ConfigurationController

`src/GrpcWebBridge/Controllers/ConfigurationController.cs` is mounted at `api/configuration` (`[Route("api/configuration")]`) and produces `application/json`. It reads the bridge's `GrpcWebBridgeOptions` and the `ServiceRegistry`, and keeps a small runtime-only settings store.

| Verb | Route | Purpose |
|------|-------|---------|
| GET | `/api/configuration` | Read the active configuration snapshot |
| PUT | `/api/configuration` | Submit a set of setting changes and get back which ones were accepted |
| POST | `/api/configuration/validate` | List registered services with their status and health |
| POST | `/api/configuration/reset` | Clear the runtime settings store |

Caveats:

- **PUT does not change `GrpcWebBridgeOptions`.** Accepted values are returned in `updates` and are not written back to the options object. The only value kept anywhere is `RequestsPerSecond`, which goes into the controller's runtime store. Every other accepted value is reported as applied but has no effect on the running bridge.
- **Runtime store is static and not thread-safe.** `_runtimeConfig` is a `static` `Dictionary<string, object>`. It is shared across requests and across instances of the controller, and it is not synchronized. It also resets on process restart.
- **`RateLimiting.Enabled` is always `false` in practice.** It is `true` only when the runtime store contains the key `RateLimitingEnabled`. No PUT key writes that key, so the flag can only be set by code.
- **Some GET fields are fixed.** `Authentication` always reports `RequiresBearerToken`, `SupportsApiKey` and `TokenValidationEnabled` as `true`, whatever the real authentication setup is.
- **No authorization attribute.** The controller has no `[Authorize]`, so GET, PUT, validate and reset are open to any caller that can reach the bridge. Restrict `api/configuration` at the gateway or network level.

### GET /api/configuration

Success (`200 OK`). The payload is wrapped in `data`, with a `timestamp` (UTC) at the top level.

Values exposed under `data` from `GrpcWebBridgeOptions.Configuration` (`BridgeConfiguration`):

| Field | Source | Notes |
|-------|--------|-------|
| `Environment` | `Configuration.Environment` | For example `Production`, `Development`, `Testing` |
| `MaxStreamCount` | `Configuration.MaxStreamCount` | Streaming session limit |
| `StreamIdleTimeoutSeconds` | `Configuration.StreamIdleTimeoutSeconds` | Idle stream timeout |
| `MaxMessageSize` | `Configuration.MaxMessageSize` | Maximum gRPC message size in bytes |
| `CompressResponses` | `Configuration.CompressResponses` | Response compression on or off |
| `CompressionLevel` | `Configuration.CompressionLevel` | Compression level |
| `EnableSwagger` | `Configuration.EnableSwagger` | Swagger toggle |
| `AllowedOrigins` | `Configuration.AllowedOrigins` | CORS origins list |

Derived or fixed fields:

| Field | Value |
|-------|-------|
| `RateLimiting.Enabled` | `true` only if the runtime store has `RateLimitingEnabled` (see caveats) |
| `RateLimiting.RequestsPerSecond` | Runtime store value, or `1000` when unset |
| `Authentication.*` | Hard-coded `true` values (see caveats) |
| `RuntimeConfig` | Full contents of the runtime store |

Not exposed by this endpoint: `StreamHeartbeatIntervalSeconds`, `DefaultTimeoutMilliseconds`, `EnableLogging`, `EnableMetrics`, `EnableCors`, `RequireAuthentication`, `AllowedMethods`, `CustomHeaders`, `ServiceDefaults`, `InstanceId`.

On exception it returns `500` with `{ "error": "Failed to retrieve configuration" }`.

### PUT /api/configuration

Request body: a `settings` object of key/value pairs. Keys are matched case-insensitively. Values are compared as text, so JSON numbers, booleans and quoted strings all work.

```json
{
  "settings": {
    "CompressionLevel": 4,
    "RequestsPerSecond": 500
  }
}
```

Accepted keys:

| Key | Valid value | Effect |
|-----|-------------|--------|
| `CompressResponses` | `true` or `false` | Reported in `updates` only |
| `CompressionLevel` | Integer `0` to `9` | Reported in `updates` only |
| `RequestsPerSecond` | Integer greater than `0` | Stored in the runtime store and reported in `updates` |
| `StreamIdleTimeoutSeconds` | Integer greater than `0` | Reported in `updates` only |

Behavior:

- A missing or empty `settings` object returns `400` with `{ "error": "No settings provided to update" }`.
- Unknown keys are logged as warnings and ignored. A value that fails validation is skipped silently. Neither case returns an error, so the response is still `200` and `updates` lists only the applied values.
- Success (`200 OK`) returns `success: true`, a `message` such as `"2 configuration setting(s) updated"`, `updates` (the applied key/value pairs) and `timestamp`.
- On exception it returns `500` with `{ "error": "Failed to update configuration", "details": "..." }`.

Example:

```bash
curl -s -X PUT "$BRIDGE_URL/api/configuration" \
  -H "Content-Type: application/json" \
  -d '{"settings":{"CompressionLevel":4,"RequestsPerSecond":500}}'
```

### POST /api/configuration/validate

Lists every service in `ServiceRegistry` and marks it healthy when `Status` is `ServiceStatus.Serving`. This check does not contact the backends.

Success (`200 OK`):

| Field | Type | Description |
|-------|------|-------------|
| `success` | bool | Always `true` on success |
| `validationStatus` | string | Always `"completed"` |
| `serviceCount` | int | Number of registered services |
| `healthyServices` | int | Services with `isHealthy: true` |
| `details` | array | One item per service: `serviceId`, `serviceName`, `status`, `endpoint`, `port`, `methodCount`, `isHealthy` |
| `timestamp` | string | UTC time |

On exception it returns `500` with `{ "error": "Validation failed", "details": "..." }`.

### POST /api/configuration/reset

Clears the runtime store (`RuntimeConfig`, `RateLimiting`). It does not touch `GrpcWebBridgeOptions`, so values set at startup stay in effect.

Success (`200 OK`) returns `success: true`, `message: "Configuration reset to defaults"` and `timestamp`. On exception it returns `500` with `{ "error": "Failed to reset configuration" }`.

Example:

```bash
curl -s "$BRIDGE_URL/api/configuration" | jq '.data.Environment, .data.CompressionLevel'
curl -s -X POST "$BRIDGE_URL/api/configuration/reset"
```

## StreamDiagnosticsController

`src/GrpcWebBridge/Controllers/StreamDiagnosticsController.cs` is mounted at `api/streams` (`[Route("api/streams")]`) and exposes one read-only route, `GET /api/streams/diagnostics`. Each request takes a fresh snapshot from `IBidirectionalStreamingEngine.GetAllMetrics()` and aggregates it in the action. Nothing is stored between requests.

| Verb | Route | Purpose |
|------|-------|---------|
| GET | `/api/streams/diagnostics` | Aggregate counters and anomaly counts for all active bidirectional streams |

### Relation to StreamDiagnosticsService

`StreamDiagnosticsService` (`src/GrpcWebBridge/Streaming/StreamDiagnosticsService.cs`) is a periodic `BackgroundService`. It is registered by `AddStreamingDiagnostics(...)`. On each pass it logs a summary, warns about high-backpressure streams, and publishes a `StreamingDiagnosticsEvent` to the `EventBus`.

The controller is a pull-based alternative and does not call the service:

- **Same engine source.** Both read `IBidirectionalStreamingEngine.GetAllMetrics()`. The controller needs only the engine to be registered (`AddBidirectionalStreaming`). It does not need `AddStreamingDiagnostics`, and it does not depend on the service being running.
- **Same response fields.** The JSON body has the same property names as `StreamingDiagnosticsEvent`, so a consumer can use either source.
- **Same high-backpressure rule.** A stream counts as high-backpressure when `BackpressureEvents / (MessagesIn + MessagesOut)` is greater than `0.10`. The service uses the same ratio, but its threshold comes from `StreamDiagnosticsOptions.BackpressureWarnThreshold`.
- **No event, no log.** A request to the controller publishes nothing to the `EventBus` and writes no diagnostic log line.

Caveats:

- **Threshold is hard-coded.** `DefaultBackpressureWarnThreshold` is a `const` of `0.10` in the controller. Changing `backpressureWarnThreshold` in `AddStreamingDiagnostics` has no effect on this endpoint.
- **Zero-activity means different things.** The controller counts a stream as zero-activity when its lifetime `MessagesIn` and `MessagesOut` are both `0`. The service counts a stream as zero-activity when its total message count did not change since the previous collection pass. A long-lived stream that was once busy but is now idle is counted by the service and not by the controller.
- **Anomaly counts are a point-in-time view.** `ZeroActivityStreamCount` and `HighBackpressureStreamCount` are recomputed on each request. They are not kept as history.
- **No authorization attribute.** The controller has no `[Authorize]`, so any caller that can reach the bridge can read these counters. Restrict `api/streams` at the gateway or network level.

### GET /api/streams/diagnostics

Success (`200 OK`). The body is a flat JSON object, with no `data` wrapper:

| Field | Type | Description |
|-------|------|-------------|
| `ActiveStreamCount` | int | Number of streams returned by `GetAllMetrics()` |
| `TotalMessagesIn` | long | Sum of `MessagesIn` across all streams |
| `TotalMessagesOut` | long | Sum of `MessagesOut` across all streams |
| `TotalBytesIn` | long | Sum of `BytesIn` across all streams |
| `TotalBytesOut` | long | Sum of `BytesOut` across all streams |
| `TotalBackpressureEvents` | long | Sum of `BackpressureEvents` across all streams |
| `TotalCreditWaitMs` | long | Sum of `TotalCreditWaitMs` across all streams |
| `ZeroActivityStreamCount` | int | Streams with `MessagesIn == 0` and `MessagesOut == 0` |
| `HighBackpressureStreamCount` | int | Streams with at least one message whose backpressure ratio is above `0.10` |

When no streams are open, every field is `0`.

The action has no `try`/`catch`. Any exception from the engine goes to `ErrorHandlingMiddleware`.

Example:

```bash
curl -s "$BRIDGE_URL/api/streams/diagnostics" | jq '.ActiveStreamCount, .HighBackpressureStreamCount'
```

## RateLimitingMiddleware

`src/GrpcWebBridge/Middleware/RateLimitingMiddleware.cs` limits how many requests a client may send per time window, and optionally caps the total request rate across all clients. It is **opt-in**: nothing in `Program.cs` adds it, so requests are not limited until you call `UseRateLimiting()`.

Each client is tracked in a sliding window: the middleware keeps the timestamps of recent requests and rejects a request when the count in the window has reached `RequestsPerSecond * WindowSizeSeconds`. Stale client entries are removed every 5 minutes after they have been idle for 10 minutes.

### Enabling it

`UseRateLimiting` is an `IApplicationBuilder` extension in `RateLimitingMiddlewareExtensions`. It takes an optional `RateLimitingOptions`; when omitted, the defaults below are used.

```csharp
app.UseRateLimiting(new RateLimitingOptions
{
    RequestsPerSecond = 50,
    GlobalRequestsPerSecond = 5000
});
```

The middleware runs at the point where you call `UseRateLimiting()`. Requests rejected by it never reach the middleware or endpoints registered after it.

### RateLimitingOptions

All properties are settable. Defaults are also available as `public const` values on the class (`DefaultRequestsPerSecond`, `DefaultWindowSizeSeconds`, `DefaultRetryAfterSeconds`, `DefaultGlobalRequestsPerSecond`).

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RequestsPerSecond` | int | `100` | Maximum requests per client in one window. The per-client limit is `RequestsPerSecond * WindowSizeSeconds`. |
| `WindowSizeSeconds` | int | `1` | Length of the sliding window, in seconds. |
| `RetryAfterSeconds` | int | `60` | Value sent in the `Retry-After` header on a per-client `429`, and reported as `retryAfter` in both rejection bodies. |
| `EnableGlobalLimit` | bool | `true` | Turns the global cap on or off. |
| `GlobalRequestsPerSecond` | int | `10000` | Maximum requests across all clients in one window. The global limit is `GlobalRequestsPerSecond * WindowSizeSeconds`. |
| `ExemptPaths` | `IReadOnlyList<string>` | `["/health", "/swagger"]` | Request paths that skip rate limiting. A path matches when it starts with one of these segments (`PathString.StartsWithSegments`). |

Validation runs in the middleware constructor and throws `ArgumentOutOfRangeException` when:

- `RequestsPerSecond` is `0` or negative;
- `WindowSizeSeconds` is `0` or negative;
- `RetryAfterSeconds` is negative.

`GlobalRequestsPerSecond` is **not** checked by the constructor. A zero or negative value is accepted and causes every request to be rejected with `503` while `EnableGlobalLimit` is `true`. `RateLimitingOptionsExtensions.Validate(this RateLimitingOptions)` checks these cases too, but nothing in `src/` calls it, so call it yourself if you need that check.

### Client identification

The client key is the first value of the `X-Forwarded-For` header when present, otherwise `Connection.RemoteIpAddress`, otherwise `"unknown"`. The header is trusted without checking that the request came through a proxy. A client that can reach the bridge directly can send any `X-Forwarded-For` value and get a fresh limit each time. Set this header only at a trusted proxy, or remove it there.

### Responses

Each request is checked in this order:

1. **Exempt path.** Passed through without any check.
2. **Per-client limit.** If the client is over `RequestsPerSecond * WindowSizeSeconds`, the response is:
   - Status `429 Too Many Requests`.
   - Header `Retry-After: <RetryAfterSeconds>`.
   - Content type `application/json`, with the body:

   ```json
   {
     "error": "Rate Limit Exceeded",
     "message": "Too many requests. Maximum 100 requests per 1 seconds allowed",
     "retryAfter": 60,
     "timestamp": "2026-10-25T12:00:00Z"
   }
   ```

3. **Global limit** (only when `EnableGlobalLimit` is `true`). If the total across all clients is over `GlobalRequestsPerSecond * WindowSizeSeconds`, the response is:
   - Status `503 Service Unavailable`, **not** `429`.
   - No `Retry-After` header. The body has `retryAfter` with the same value as `RetryAfterSeconds`.
   - Body `error` is `"Service Overloaded"`.

Only the per-client limit produces `429`. Code that backs off on `429` alone will not back off on a global `503`, so handle both if you need to.

Caveats:

- **A request rejected by the global limit still counts against its client.** The per-client check records the request before the global check runs, so the client's own window also fills up.
- **The global total is the sum of all clients' windows.** It walks every tracked client on each request, so cost grows with the number of distinct client keys.
- **Rejections are logged as warnings.** Each per-client `429` writes a warning with the client IP and path. Each global `503` writes a warning without a client identifier.
- **Process-local state.** Counters live in memory. With several bridge instances behind a load balancer, each instance enforces its own limits.

## MaxBodySizeMiddleware

`src/GrpcWebBridge/Middleware/MaxBodySizeMiddleware.cs` rejects requests whose declared body is larger than a configured limit. It checks the `Content-Length` header only, before the request reaches routing or any endpoint. It is **opt-in**: nothing in `Program.cs` adds it, so call `UseMaxRequestBodySize()` to enable it.

The check runs only when `Content-Length` is present. A request without that header (for example, a chunked upload) is passed through without a size check. Kestrel's own request body limit still applies to such requests, and this middleware does not replace it.

### MaxBodySizeOptions

`MaxBodySizeOptions` is a plain class with one settable property. Its default is also exposed as the `public const` `MaxBodySizeOptions.DefaultMaxRequestBodySizeBytes`.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MaxRequestBodySizeBytes` | long | `4194304` (4 MiB) | Largest accepted `Content-Length`, in bytes. A request is rejected when its `Content-Length` is greater than this value. |

The middleware reads the options through `IOptions<MaxBodySizeOptions>`. Without any configuration, the 4 MiB default applies. To change the limit, register the options in `Program.cs` before `builder.Build()`:

```csharp
builder.Services.Configure<MaxBodySizeOptions>(options =>
{
    options.MaxRequestBodySizeBytes = 1 * 1024 * 1024;
});
```

`MaxBodySizeOptionsExtensions` provides two helpers:

- `Validate()` throws `ArgumentOutOfRangeException` when `MaxRequestBodySizeBytes` is zero or negative. Nothing in `src/` calls it, so call it yourself if you want that check.
- `Clone()` returns a detached copy of the options.

### Enabling it

`UseMaxRequestBodySize` is an `IApplicationBuilder` extension in `MaxBodySizeMiddlewareExtensions`. Place it early in the pipeline, before `UseRouting()`, so oversized requests are rejected before any further processing:

```csharp
app.UseMaxRequestBodySize();
app.UseRouting();
```

### 413 response

When `Content-Length` is greater than the limit, the middleware does not call the next component. It writes this response:

- Status `413 Payload Too Large`.
- Content type `application/json`, with a body shaped like this:

```json
{
  "error": "Payload Too Large",
  "message": "Request body size 5242880 exceeds the maximum allowed 4194304 bytes.",
  "path": "/api/bridge/invoke",
  "traceId": "0HN1ABCDEF123:00000001"
}
```

The `traceId` value is `HttpContext.TraceIdentifier`. Each rejection is logged as a warning that includes the path, the declared size, and the limit. The body is not read, so no request data is buffered.

## ContentTypeValidationMiddleware

`src/GrpcWebBridge/Middleware/ContentTypeValidationMiddleware.cs` rejects `POST` requests whose `Content-Type` is not a gRPC media type. It runs before routing, so a rejected request never reaches a controller or the gRPC-Web translation layer. It is registered by default in `Program.cs`.

### Accepted content types

The media type is compared case-insensitively, and any parameters (for example `; charset=utf-8`) are ignored. These six media types are accepted:

| Media type | Encoding |
|------------|----------|
| `application/grpc-web` | Binary gRPC-Web frames |
| `application/grpc-web+proto` | Binary gRPC-Web frames, protobuf payload |
| `application/grpc-web-text` | Base64-encoded gRPC-Web frames |
| `application/grpc-web-text+proto` | Base64-encoded gRPC-Web frames, protobuf payload |
| `application/grpc` | Native gRPC framing |
| `application/grpc+proto` | Native gRPC framing, protobuf payload |

A missing or blank `Content-Type` header is rejected.

### Which requests are checked

Only requests that meet both conditions are validated:

- The method is `POST`. Other methods (`GET`, `PUT`, `DELETE`, and so on) pass through unchanged.
- The path does not start with one of these prefixes. The comparison is case-insensitive.

| Excluded prefix | Typical use |
|-----------------|-------------|
| `/api` | REST endpoints such as `/api/bridge/invoke`, `/api/configuration` |
| `/swagger` | Swagger UI |
| `/openapi` | OpenAPI document |
| `/health` | Health checks |
| `/metrics` | Prometheus metrics |
| `/_` | Framework and internal paths |

The prefix list is a private constant. To change it, edit the source file.

### 415 response

When a checked request has an unsupported or missing `Content-Type`, the middleware does not call the next component. It writes this response:

- Status `415 Unsupported Media Type`.
- Content type `application/json`, with a body shaped like this:

```json
{
  "error": "Unsupported Media Type",
  "message": "Content-Type 'text/plain' is not supported. Use one of: application/grpc-web, application/grpc-web+proto, application/grpc-web-text, application/grpc-web-text+proto.",
  "path": "/grpc.health.v1.Health/Check",
  "traceId": "0HN1ABCDEF123:00000002"
}
```

- `message` names the rejected value. A missing header is shown as `(none)`. It also lists four example types. The middleware accepts the two extra `application/grpc` and `application/grpc+proto` types as well, so the list in the message is not the complete set.
- `path` is the request path.
- `traceId` is `HttpContext.TraceIdentifier`.

Each rejection is logged as a warning with the path and the received content type. The body is not read.

### Registration

`UseGrpcWebContentTypeValidation()` is an `IApplicationBuilder` extension in `ContentTypeValidationMiddlewareExtensions`. The middleware has no options and needs no service registration. In `Program.cs` it sits directly after the error handler:

```csharp
app.UseErrorHandling();
app.UseGrpcWebContentTypeValidation();
app.UseRequestLogging();
app.UseRouting();
```

Keep it after `UseErrorHandling()` so that exceptions thrown by the rest of the pipeline still become JSON error responses. Keep it before `UseRouting()` so that invalid requests are rejected before routing runs.

## RouteHeaderTransformMiddleware

`src/GrpcWebBridge/Middleware/RouteHeaderTransformMiddleware.cs` runs every registered `IRouteHeaderTransformHook` whose route prefix matches the request path. A hook can rewrite HTTP request headers before the rest of the pipeline sees them, add entries to a per-request gRPC metadata dictionary, and rewrite response headers after the rest of the pipeline returns. The file defines four public types: `IRouteHeaderTransformHook`, `RouteHeaderTransformMiddleware`, `RouteHeaderTransformConstants`, and `RouteHeaderTransformExtensions`. It is **opt-in**: nothing in `Program.cs` adds it, so the middleware is a pass-through until you call `UseRouteHeaderTransforms()` and register at least one hook.

### IRouteHeaderTransformHook

Implement this interface to define a transform. It has one property and two methods:

| Member | Description |
|--------|-------------|
| `string? RoutePrefix { get; }` | The path prefix the hook applies to, such as `/api/bridge/invoke`. `null` or an empty string applies the hook to every route. |
| `Task TransformRequestAsync(IHeaderDictionary requestHeaders, Dictionary<string, string> grpcMetadata, CancellationToken cancellationToken)` | Called before the next middleware runs. Changes to `requestHeaders` affect the live `HttpRequest` headers. Entries added to `grpcMetadata` are collected for the request. |
| `Task TransformResponseAsync(IHeaderDictionary responseHeaders, CancellationToken cancellationToken)` | Called after the next middleware has returned. Changes to `responseHeaders` are applied to the live `HttpResponse` headers. |

Matching is a case-insensitive `StartsWith` on `HttpContext.Request.Path`. Because it is a plain string prefix, `/api/bridge` also matches `/api/bridgeXYZ`. Write the prefix with a trailing `/` if you need segment-level matching.

```csharp
public sealed class TenantHeaderHook : IRouteHeaderTransformHook
{
    public string? RoutePrefix => "/api/bridge";

    public Task TransformRequestAsync(
        IHeaderDictionary requestHeaders,
        Dictionary<string, string> grpcMetadata,
        CancellationToken cancellationToken)
    {
        var tenant = requestHeaders["X-Tenant-Id"];
        if (!StringValues.IsNullOrEmpty(tenant))
        {
            grpcMetadata["x-tenant-id"] = tenant.ToString();
        }

        return Task.CompletedTask;
    }

    public Task TransformResponseAsync(
        IHeaderDictionary responseHeaders,
        CancellationToken cancellationToken)
    {
        responseHeaders.Remove("Server");
        return Task.CompletedTask;
    }
}
```

### Registration

`RouteHeaderTransformExtensions` provides three members:

| Member | Purpose |
|--------|---------|
| `UseRouteHeaderTransforms()` | `IApplicationBuilder` extension that adds `RouteHeaderTransformMiddleware` to the pipeline. |
| `AddRouteHeaderTransformHook<THook>()` | Registers a class that implements `IRouteHeaderTransformHook` as a **scoped** `IRouteHeaderTransformHook`. The class itself is not registered, so only the interface can resolve it. The hook can take scoped dependencies through its constructor. |
| `AddRouteHeaderTransformHook(routePrefix, transformRequest, transformResponse = null)` | Registers a delegate-based hook without a class. `transformRequest` has the same shape as `TransformRequestAsync`. `transformResponse` is optional; when it is omitted, response headers are not changed. |

The delegate-based hook is also registered as a scoped service.

```csharp
builder.Services.AddRouteHeaderTransformHook<TenantHeaderHook>();

builder.Services.AddRouteHeaderTransformHook(
    "/api/bridge/invoke",
    (requestHeaders, grpcMetadata, cancellationToken) =>
    {
        grpcMetadata["x-request-source"] = "web";
        return Task.CompletedTask;
    });

// In Program.cs, before UseRouting():
app.UseRouteHeaderTransforms();
```

The middleware resolves hooks from the request scope on each request, so hooks registered as scoped are fresh for each request. Register all hooks before `builder.Build()`.

Place `UseRouteHeaderTransforms()` before `UseRouting()`. The XML comment on the method says so: the transforms run before routing.

### Request flow

For each request, the middleware runs these steps in order:

1. Collect the hooks whose `RoutePrefix` matches the path. If none match, the request goes straight to the next middleware and nothing else runs.
2. Create a `Dictionary<string, string>` with case-insensitive keys and store it in `HttpContext.Items` under `RouteHeaderTransformConstants.GrpcMetadataKey` (`"GrpcWebBridge.GrpcMetadata"`).
3. Call `TransformRequestAsync` on each matching hook, in DI registration order.
4. Call the next middleware.
5. Call `TransformResponseAsync` on each matching hook, in the same order.

### RouteHeaderTransformConstants

| Member | Value | Description |
|--------|-------|-------------|
| `GrpcMetadataKey` | `"GrpcWebBridge.GrpcMetadata"` | Key in `HttpContext.Items` for the per-request gRPC metadata dictionary. |

The middleware only writes this key; nothing in `src/` reads it. Entries that hooks add to the dictionary are not merged into the gRPC call metadata by the bridge itself. Code that performs the gRPC call must read `HttpContext.Items[RouteHeaderTransformConstants.GrpcMetadataKey]` and apply the entries.

The key is set only when at least one hook matches. Read it with a null check.

### Error handling and caveats

- **Hook exceptions are logged and swallowed.** If a `TransformRequestAsync` or `TransformResponseAsync` call throws, the middleware logs an error with the hook type name and path, and continues with the next hook and the rest of the pipeline. A failing hook does not fail the request.
- **Exceptions from the rest of the pipeline are not caught here.** They propagate to `UseErrorHandling()`.
- **Response transforms can fail after the response has started.** `TransformResponseAsync` runs after the next middleware returns. If the response has already started (for example, a body was streamed), changing its headers throws. That exception is caught and logged like any other hook failure.
- **Cancellation.** Both methods receive `HttpContext.RequestAborted`. A hook that observes the token can stop early if the client disconnects.
- **Without any hook, the middleware does nothing.** It does not inspect the request until at least one hook is registered and matches.

## ErrorHandlingMiddleware

`ErrorHandlingMiddleware` (`src/GrpcWebBridge/Middleware/ErrorHandlingMiddleware.cs`) is the outermost error boundary of the pipeline. It catches exceptions thrown by the rest of the pipeline, logs them, and writes an RFC 7807 `ProblemDetails` response as JSON. Register it with `app.UseErrorHandling()` as the first middleware, as `Program.cs` does.

### Request flow

1. The middleware logs `Processing {Method} {Path}` at Information level and calls the next component.
2. If the pipeline completes without an exception, it logs `Finished processing {Method} {Path}`.
3. If an exception is thrown, it logs it at Error level with the message `Unhandled exception in request pipeline`, converts it with `Exception.ToProblemDetails(HttpContext)`, and sets the response status from `ProblemDetails.Status`. The body is written with `Content-Type: application/json`.

The conversion is in `src/GrpcWebBridge/Domain/Exceptions/ProblemDetailsExtensions.cs`. It uses a `switch` on the exception type, and the first matching case wins. The table below follows that order. The `ValidationException` case comes before the `GrpcWebBridgeException` case, and `ArgumentNullException` comes before `ArgumentException`, because it derives from it.

### Exception to ProblemDetails mapping

Every conversion starts with `type` set to `about:blank`, `title` set to `Internal Server Error`, `status` set to 500, and `detail` set to `exception.Message`. The cases below override those defaults.

| Exception | HTTP status | `type` | `title` | Extra `details` keys |
|-----------|-------------|--------|---------|----------------------|
| `ValidationException` | 400 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1` | `Validation Failed` | `field`, `providedValue`, `validationRule`, and `errors`, each only when set |
| `ProtocolException` | 400 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1` | `Protocol Translation Failed` | `sourceFormat`, `targetFormat` (`"unknown"` when unset) |
| `StreamingException` | 500 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1` | `Streaming Operation Failed` | `streamId` (`"unknown"` when unset), `streamState` when set |
| `ServiceRegistrationException` | 400 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1` | `Service Registration Failed` | `serviceName` (`"unknown"` when unset), `serviceEndpoint` when set |
| `ConfigurationException` | 400 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1` | `Configuration Error` | `configurationKey` (`"unknown"` when unset), `configurationValue` when set |
| `ArgumentNullException` | 400 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1` | `Invalid Request` | `paramName` |
| `ArgumentException` | 400 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1` | `Invalid Argument` | none |
| `GrpcWebBridgeException` | Mapped from `GrpcStatus`, otherwise 500 | `about:blank` | GRPC status title, or `Bridge Operation Failed` when `GrpcStatus` is not set | `errorCode` when set, plus every entry in `Context` |
| `UnauthorizedAccessException` | 401 | `https://datatracker.ietf.org/doc/html/rfc7235#section-3.1` | `Unauthorized` | none |
| `TimeoutException` | 504 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4` | `Operation Timeout` | none |
| `OperationCanceledException` (includes `TaskCanceledException`) | 400 | `https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.3` | `Operation Cancelled` | none |
| Any other exception | 500 | `about:blank` | `Internal Server Error` | none |

Every conversion also adds `exceptionType` to `details`, set to the full .NET type name of the exception.

**`GrpcWebBridgeException` status.** When `GrpcStatus` is set, the HTTP status comes from the table in [5. gRPC status to HTTP status](#5-grpc-status-to-http-status). The subclasses above (`ValidationException`, `ProtocolException`, `StreamingException`, `ServiceRegistrationException`, `ConfigurationException`) have their own cases, so their `GrpcStatus` is ignored. For example, `ServiceRegistrationException` returns 400 even when its `GrpcStatus` is `NotFound` or `Unavailable`.

### Response body

The response body is the `ErrorResponse` class from the same file. Its fields are camelCase:

| Field | Value |
|-------|-------|
| `success` | Always `false`. |
| `type` | The `type` URI from the mapping. |
| `title` | The `title` from the mapping. |
| `error` | Same as `title`. Kept for backward compatibility. |
| `status` | The HTTP status code. |
| `detail` | The exception message, or the mapped detail text. |
| `message` | Same as `detail`. Kept for backward compatibility. |
| `instance` | Not set by the middleware, so it is `null`. |
| `path` | `HttpContext.Request.Path`. |
| `traceId` | `HttpContext.TraceIdentifier`. |
| `timestamp` | The UTC time when the problem was created. |
| `details` | The extension entries from the mapping. Always contains `exceptionType`. |

An example for a `ValidationException` (the `instance` field is `null` and is left out):

```json
{
  "success": false,
  "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "error": "Validation Failed",
  "status": 400,
  "detail": "Value must be between 1 and 100.",
  "message": "Value must be between 1 and 100.",
  "path": "/api/bridge/invoke",
  "traceId": "0HN1ABCDEF123:00000002",
  "timestamp": "2026-10-25T10:15:30.1234567Z",
  "details": {
    "field": "pageSize",
    "providedValue": 500,
    "validationRule": "Range",
    "errors": {
      "pageSize": "Value must be between 1 and 100.",
      "value": 500,
      "rule": "Range"
    },
    "exceptionType": "GrpcWebBridge.Domain.Exceptions.ValidationException"
  }
}
```

### Caveats

- **Exception messages reach the client.** `detail` is set from `exception.Message` for every exception type, including the 500 fallback. Internal messages, such as connection strings or file paths in an exception message, are returned to the caller. Keep sensitive data out of exception messages.
- **`OperationCanceledException` is logged as an error.** The catch-all logs every exception at Error level, including cancellations such as a client that disconnected. Those requests get a 400 response, which the disconnected client does not see.
- **The `type` URIs for 504 and 400 point at other RFC 7231 sections.** `TimeoutException` uses `section-6.6.4` and `OperationCanceledException` uses `section-6.6.3`. In RFC 7231, those sections describe 503 and 502. The URIs are copied from the code, so the table lists them as they are.
- **If the response has already started, the middleware fails.** It sets `Response.StatusCode` before writing the body. Once the response has started, that assignment throws, and the exception leaves the middleware.
- **`exceptionType` is always added.** The code comment says it is for debugging and not for production, but the code adds it on every response.

## RequestLoggingMiddleware

`src/GrpcWebBridge/Middleware/RequestLoggingMiddleware.cs` logs each HTTP request when it arrives and each response when it leaves the pipeline. It also records how long the rest of the pipeline took. It is **on by default**: `Program.cs` calls `app.UseRequestLogging()` right after `UseGrpcWebContentTypeValidation()` and before `UseRouting()`, so requests are logged before routing and before any endpoint runs.

### What is logged

For every request that is not excluded (see [Excluded paths](#excluded-paths)), the middleware writes two structured entries at Information level:

1. **Incoming request** (`Incoming request: {RequestData}`), written before the next middleware runs. Fields:
   - `type` (`"request"`), `method`, `path`, `queryString`, `scheme`, `host`, `contentType`, `contentLength`;
   - `headers`, with sensitive headers removed (see [Header filtering](#header-filtering));
   - `body`, the request body when its content type is loggable (see [Body capture](#body-capture));
   - `ip` (the remote IP address, or `null`), and `timestamp` (UTC).
2. **Outgoing response** (`Outgoing response: {ResponseData}`), written after the rest of the pipeline returns. Fields:
   - `type` (`"response"`), `method`, `path`, `statusCode`, `contentType`, `contentLength`;
   - `headers` (filtered the same way), `body` (the response body when its content type is loggable);
   - `elapsedMilliseconds`, the time from the start of `InvokeAsync` until the rest of the pipeline returned, and `timestamp` (UTC).

The response entry's level depends on the status code:

| Status code | Log level |
|-------------|-----------|
| `>= 500` | Error |
| `400`-`499` | Warning |
| Anything else | Information |

The middleware also writes progress lines at Information level that name the method and path: `InvokeAsync started for {Method} {Path}`, `LogRequestAsync started for ...`, and `LogResponseAsync started for ... after {ElapsedMilliseconds} ms`. These are written before the exclusion check, so they appear even for excluded paths.

### Excluded paths

Excluded paths get no `Incoming request` or `Outgoing response` entry and no header or body capture. The list is a private constant, `DefaultExcludedPaths`, and cannot be changed without editing the source:

| Prefix | Notes |
|--------|-------|
| `/health` | Health endpoints. The prefix also matches paths such as `/healthcheck`. |
| `/swagger` | Swagger UI and OpenAPI documents. |
| `/api/metrics` | `MetricsController` routes. |
| `/favicon.ico` | Browser favicon requests. |

The check is a prefix match on `Request.Path` using `string.StartsWith`. The only exclusion is the prefix list above; the middleware does not match on method or query string.

### Header filtering

Headers whose names match one of these entries (compared in lowercase, exact match) are left out of both the request and response `headers` fields:

- `authorization`
- `cookie`
- `x-api-key`
- `x-auth-token`
- `token`

All other headers are logged in full, including any custom header that carries a secret. Add such names to the code if your clients send them.

### Body capture

A body is logged only when its content type contains `application/json`, `application/xml` or `text/` (case-insensitive). Other types, including `application/grpc-web+proto` and `application/grpc-web-text`, are logged as an empty string. The check uses `Contains`, so `application/grpc-web-text` does not match `text/`.

- **Request:** the body is logged only when `ContentLength > 0`. The middleware calls `EnableBuffering()`, reads the whole body into memory, and then resets the stream position so that downstream code can read it.
- **Response:** the response is written into a `MemoryStream` while the rest of the pipeline runs. The body is logged only when `ContentLength > 0`. Responses without a known length, such as streamed or chunked responses, are logged with an empty `body`. After logging, the buffered bytes are copied to the real response stream.

Bodies longer than 1000 characters are cut to the first 1000 characters and followed by `...`. The cut happens only in the log. The whole body is still read into memory first, so large JSON payloads cost memory even though only part of the body is logged.

### UseRequestLogging

`UseRequestLogging()` is an `IApplicationBuilder` extension in `RequestLoggingMiddlewareExtensions`. It takes no options and adds the middleware with `UseMiddleware<RequestLoggingMiddleware>()`:

```csharp
app.UseErrorHandling();
app.UseGrpcWebContentTypeValidation();
app.UseRequestLogging();
app.UseRouting();
```

The position in the pipeline matters. Logging only sees what runs after it, so the `ErrorHandlingMiddleware` placed before it can still turn exceptions into responses that this middleware never logs. See the caveats below.

### Caveats

- **Sensitive data in bodies.** Request and response bodies are logged as they are, without masking. A JSON body with a password or token is written to the log in clear text, up to 1000 characters.
- **Exceptions skip the response entry.** The response entry is written only when the rest of the pipeline returns normally. If an exception is thrown, no `Outgoing response` entry is written for that request. `ErrorHandlingMiddleware` logs the exception instead.
- **Timing excludes the body copy.** `elapsedMilliseconds` stops before the buffered response is copied to the client, so it does not include that copy.
- **Logs can be large.** Every non-excluded request produces two entries with full headers, and the Error and Warning levels follow the status code. Set the log level for this type in configuration if the volume is too high.

## WebhookPublisher

`GrpcWebBridge.Integration.WebhookPublisher` sends bridge events to external HTTP endpoints. A subscription names a URL and the event types it wants. Each published event that matches is queued and then POSTed as JSON by one background worker. The worker starts when the publisher is constructed, and the publisher implements `IDisposable`. The queue is in memory, so events that are still queued when the process exits are lost.

### Public API

- `WebhookPublisher(ILogger<WebhookPublisher> logger, HttpClientFactory httpClientFactory, WebhookPublisherOptions? options = null)` creates the publisher and starts processing. A `null` `options` uses the default values.
- `string Subscribe(string webhookUrl, string[] eventTypes, Dictionary<string, string>? headers = null, bool retryOnFailure = true)` registers a subscription and returns its ID. It throws `ArgumentException` when the URL is null or empty, and `InvalidOperationException` when the URL is not an absolute URI. `eventTypes` are compared with the event's class name, such as `"ServiceRegisteredEvent"`. An empty array matches every event. `headers` are sent with every request. `retryOnFailure: false` disables retries for this subscription.
- `bool Unsubscribe(string subscriptionId)` removes a subscription. It returns `false` when the ID is unknown.
- `Task PublishEventAsync(EventBase @event)` queues one delivery for each active subscription that matches the event. It throws `ArgumentNullException` for a `null` event. It returns once the deliveries are queued, not once they are sent. If no subscription matches, it only logs at debug level.
- `List<WebhookSubscription> GetSubscriptions()` returns the active subscriptions only.
- `object GetStatistics()` returns an anonymous object with `totalSubscriptions`, `activeSubscriptions`, `totalEventsSent`, `totalEventsFailed` and `averageFailureRate`. `totalEventsSent` is the sum of `SuccessCount`, and `totalEventsFailed` is the sum of `FailureCount`. `averageFailureRate` is the average of `FailureCount / (SuccessCount + FailureCount + 1)` across all subscriptions, rounded to two decimals.
- `void Dispose()` cancels the worker, waits up to five seconds for it to stop, and completes the queue.

### Types

`WebhookSubscription` describes a subscription:

| Property | Description |
|----------|-------------|
| `Id` | Subscription ID returned by `Subscribe`. |
| `Url` | Target endpoint. |
| `EventTypes` | Event class names to match. An empty array matches all events. |
| `Headers` | Extra HTTP headers sent with each request. |
| `RetryOnFailure` | Whether failed deliveries are retried. |
| `IsActive` | `false` after the subscription is disabled for failures. |
| `CreatedAt` | UTC creation time. |
| `LastSuccessfulDelivery` | UTC time of the last successful delivery, or `null`. |
| `SuccessCount` / `FailureCount` | Delivery counters. `FailureCount` counts failed attempts, so one event can add more than one. |

`WebhookEvent` is the queued record for one delivery: `EventId`, `EventType`, `Timestamp`, `Payload` (the original event object), `SubscriptionId`, `RetryCount` and `MaxRetries`. `RetryCount` is always `0` in the queued record, and `MaxRetries` is copied from the options when the event is published. The retry loop counts attempts in a local variable and does not update these fields, so they do not show the retries that were performed.

`WebhookPublisherOptions` holds the configuration:

| Property | Default | Description |
|----------|---------|-------------|
| `MaxRetries` | `3` | Maximum retries per event. Delivery uses at most `Math.Min(3, MaxRetries)`. |
| `FailureThresholdForDisable` | `10` | A subscription is set to inactive once its `FailureCount` is greater than this value. |

### Delivery format

Each delivery is an HTTP `POST` with `Content-Type: application/json`. The body has camelCase property names:

```json
{
  "eventId": "…",
  "eventType": "ServiceRegisteredEvent",
  "timestamp": "…",
  "payload": { "eventId": "…", "createdAt": "…", "serviceId": "…", "serviceName": "…", "endpoint": "…" }
}
```

The `payload` object is the published event, serialized with the bridge's default `JsonUtility` settings.

### Retries and backoff

- **Attempts.** A subscription with `RetryOnFailure = true` makes up to `1 + Math.Min(3, MaxRetries)` attempts. With `RetryOnFailure = false` it makes one attempt.
- **Delay.** Before retry *n* (starting at 1), the worker waits `2^n` seconds multiplied by a random jitter factor in the range `[0.5, 1.5)`. With the default options, the waits are 1–3 s, 2–6 s and 4–12 s. The delay is capped at 10 seconds, so no wait is longer than 10 s.
- **What is retried.** Timeouts and 5xx responses are retried. The code also throws on other non-success responses, such as 4xx, and the same handler catches that exception and retries it. Other exceptions are retried in the same way.
- **After the last attempt.** The event is dropped and the failure is logged. `FailureCount` is incremented.
- **Disabling.** Once `FailureCount` is greater than `FailureThresholdForDisable`, the subscription is set to inactive. It no longer receives events and is not returned by `GetSubscriptions`. There is no method to reactivate it, so subscribe again instead.

### Caveats

- **Sequential delivery.** One worker sends all queued events in order, so a retry wait for one endpoint delays every other delivery.
- **Unsubscribing drops queued events.** A subscription is looked up when its event is processed. If it is removed before that, the event is dropped without an error.
- **Retries are not persisted.** Pending retries live in memory only and are lost when the process stops.

### Usage example

```csharp
using GrpcWebBridge.Events;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging.Abstractions;

var webhookToken = Environment.GetEnvironmentVariable("WEBHOOK_TOKEN") ?? string.Empty;

using var httpClientFactory = new HttpClientFactory(NullLogger<HttpClientFactory>.Instance);
using var webhooks = new WebhookPublisher(
    NullLogger<WebhookPublisher>.Instance,
    httpClientFactory,
    new WebhookPublisherOptions
    {
        MaxRetries = 3,
        FailureThresholdForDisable = 10
    });

var subscriptionId = webhooks.Subscribe(
    "https://hooks.example.com/grpc-bridge",
    new[] { "ServiceRegisteredEvent" },
    headers: new Dictionary<string, string> { ["X-Webhook-Token"] = webhookToken });

await webhooks.PublishEventAsync(new ServiceRegisteredEvent
{
    ServiceId = "catalog-service",
    ServiceName = "Catalog",
    Endpoint = "https://localhost:5001"
});

var statistics = webhooks.GetStatistics();
Console.WriteLine($"Subscription {subscriptionId}: {statistics}");
```

The `WEBHOOK_TOKEN` variable is read from the environment, so no credential is stored in code. `PublishEventAsync` returns once the event is queued, not once it is delivered. Disposing the publisher cancels the worker, so events that have not been sent by then are not delivered.

## TracingService and BridgeActivitySource

The `GrpcWebBridge.Telemetry` namespace holds the bridge's distributed-tracing types. `BridgeActivitySource` defines the `ActivitySource` and the names used for its spans and tags. `TracingService` starts those spans for bridge operations and records their status. Spans are only created when an OpenTelemetry listener is subscribed to the source. The OpenTelemetry hook-up is done by `AddGrpcWebBridgeTracing`.

### BridgeActivitySource

`GrpcWebBridge.Telemetry.BridgeActivitySource` is a static class.

- `const string Name = "GrpcWebBridge"` is the activity source name. Register it with `AddSource` or filter on it in a tracing backend.
- `const string Version = "2.0.2"` is the source version. It is also reported as the `service.version` resource attribute by `AddGrpcWebBridgeTracing`.
- `static readonly ActivitySource Source` is the shared source, created as `new(Name, Version)`. `StartActivity` returns `null` unless a listener is subscribed to `Name`.

Well-known span names:

| Constant | Span name | Used for |
|----------|-----------|----------|
| `GrpcCall` | `grpc.bridge.call` | Unary gRPC call proxied by the bridge. |
| `GrpcStream` | `grpc.bridge.stream` | Server-streaming call. |
| `ProtocolTranslation` | `grpc.bridge.translate` | Translation between gRPC-Web and gRPC. |
| `Authentication` | `grpc.bridge.auth` | Authentication token check. |

Tag keys:

| Constant | Tag | Set on |
|----------|-----|--------|
| `TagRpcSystem` | `rpc.system` | gRPC call spans (value `"grpc"`). |
| `TagRpcService` | `rpc.service` | gRPC call spans. |
| `TagRpcMethod` | `rpc.method` | gRPC call spans. |
| `TagStreaming` | `bridge.streaming` | gRPC call spans. |
| `TagGrpcStatus` | `rpc.grpc.status_code` | Set by `RecordException` and `SetGrpcStatus`. |
| `TagBridgeInstance` | `bridge.instance` | All spans created by `TracingService`. |

Some tags are written as string literals rather than constants: `bridge.source_protocol` and `bridge.target_protocol` on translation spans, and `auth.scheme` on authentication spans.

### TracingService

`GrpcWebBridge.Telemetry.TracingService` is a `sealed` class. It is registered as a singleton by `AddGrpcWebBridgeTracing`. Its constructor is `TracingService(ILogger<TracingService> logger, string? instanceName = null)`. A `null` or blank `instanceName` becomes `"default"`. The value is attached to every span as `bridge.instance`.

Instance methods start spans:

- `Activity? StartGrpcCallActivity(string serviceName, string methodName, bool isStreaming = false)` starts a `grpc.bridge.call` span, or a `grpc.bridge.stream` span when `isStreaming` is `true`. The span kind is `ActivityKind.Client`. It sets `rpc.system`, `rpc.service`, `rpc.method`, `bridge.streaming` and `bridge.instance`. It logs an information message on every call, and a warning when no span is created. Throws `ArgumentException` when `serviceName` or `methodName` is null or empty.
- `Activity? StartProtocolTranslationActivity(string sourceProtocol, string targetProtocol)` starts a `grpc.bridge.translate` span with kind `ActivityKind.Internal`. It sets `bridge.source_protocol`, `bridge.target_protocol` and `bridge.instance`. Throws `ArgumentException` when either argument is null or empty.
- `Activity? StartAuthenticationActivity(string scheme)` starts a `grpc.bridge.auth` span with kind `ActivityKind.Internal`. It sets `auth.scheme` and `bridge.instance`. Throws `ArgumentException` when `scheme` is null or empty.

Static methods update a span the caller already owns. Each one ignores a `null` activity.

- `static void RecordException(Activity? activity, Exception exception, string? grpcStatus = null)` sets the status to `Error` with the exception message, adds the exception as a span event, and sets `rpc.grpc.status_code` when `grpcStatus` is not blank. Throws `ArgumentNullException` when `exception` is `null`.
- `static void SetGrpcStatus(Activity? activity, string grpcStatus)` sets `rpc.grpc.status_code`. The status is `Ok` when the value is `"OK"`, and `Error` with the message `gRPC status: <code>` for any other value. Throws `ArgumentException` when `grpcStatus` is null or empty.

The returned span is owned by the caller. Dispose it when the operation ends, with `using var`, which also accepts `null`:

```csharp
using var activity = tracing.StartGrpcCallActivity("UserService", "GetUser");
try
{
    var user = await userClient.GetUserAsync(request, cancellationToken: ct);
    TracingService.SetGrpcStatus(activity, "OK");
    return user;
}
catch (Exception ex)
{
    TracingService.RecordException(activity, ex, "UNAVAILABLE");
    throw;
}
```

The example hard-codes `"UNAVAILABLE"`. In real code, pass the status code that the gRPC exception carries.

### OpenTelemetry hook-up

`AddGrpcWebBridgeTracing` is an extension on `IServiceCollection` in `GrpcWebBridge.Configuration.DependencyInjection`:

```csharp
public static IServiceCollection AddGrpcWebBridgeTracing(
    this IServiceCollection services,
    string serviceName = "grpc-web-bridge",
    string? instanceName = null,
    Action<TracerProviderBuilder>? configureBuilder = null)
```

It does four things:

1. Registers `TracingService` as a singleton, with the resolved `instanceName`.
2. Calls `AddOpenTelemetry()` and sets the resource to `serviceName`, with `service.version` set to `BridgeActivitySource.Version`, and the `bridge.instance` attribute.
3. Adds the `GrpcWebBridge` source with `AddSource`, so bridge spans are exported.
4. Adds ASP.NET Core instrumentation with `RecordException = true`. It skips requests whose path starts with `/metrics` or `/health`, to reduce noise.

`configureBuilder` runs last, on the same `TracerProviderBuilder`. Use it to add an exporter. The bridge does not add one itself. In `src/GrpcWebBridge/Program.cs`, the Development environment adds the console exporter. Other environments need an exporter added in the same way, such as OTLP:

```csharp
services.AddGrpcWebBridgeTracing(
    serviceName: "grpc-web-bridge",
    instanceName: builder.Configuration["GrpcWebBridge:InstanceName"],
    configureBuilder: tracing => tracing.AddOtlpExporter());
```

`AddOtlpExporter` comes from the `OpenTelemetry.Exporter.OpenTelemetryProtocol` package. The bridge does not reference it, so add that package to the host project before you use this call.

### Caveats

- **Spans only appear when the source is registered.** If no listener subscribes to `GrpcWebBridge`, `StartActivity` returns `null`. `TracingService` then logs a warning on every gRPC call. This happens when `TracingService` is created without `AddGrpcWebBridgeTracing`.
- **No built-in call sites.** Nothing in `src/GrpcWebBridge` calls the `Start*Activity` methods yet. The only wiring in the bridge is the DI registration and the `AddSource` call. A span exists only where your code calls `TracingService`.
- **The caller ends each span.** `TracingService` does not dispose spans. A span is reported to exporters when it is stopped, which happens on `Dispose`, so a span that is never disposed is never exported.
- **Parenting follows `Activity.Current`.** A `grpc.bridge.*` span started while an ASP.NET Core request is active becomes a child of that request's span.

## BridgePrometheusMetrics

`src/GrpcWebBridge/Services/BridgePrometheusMetrics.cs` is a static class that defines the bridge's Prometheus metrics with `prometheus-net` (the `prometheus-net.AspNetCore` package, version 8.2.1). The metrics are registered in prometheus-net's default registry. The class is opt-in: nothing is exported until the host calls both `AddGrpcWebBridgePrometheus()` and `app.MapMetrics()`.

```csharp
builder.Services.AddGrpcWebBridgePrometheus();   // registers the metric definitions
// ...
app.MapMetrics();                                // serves the scrape endpoint at /metrics
```

`AddGrpcWebBridgePrometheus()` touches each static field so the metrics exist before the first request. `MapMetrics()` is the call that serves them.

### Exposed metrics

| Metric | Type | Labels | Description | Recorded by |
|--------|------|--------|-------------|-------------|
| `grpcweb_bridge_requests_total` | counter | `service`, `method`, `grpc_status` | Completed bridged RPC calls | `BridgePrometheusMetrics.RecordCall` |
| `grpcweb_bridge_request_duration_seconds` | histogram | `service`, `method` | End-to-end call latency in seconds | `BridgePrometheusMetrics.RecordCall` |
| `grpcweb_bridge_active_streams` | gauge | none | Streams currently open | `ActiveStreams.Inc()` when a stream starts, `ActiveStreams.Dec()` in `finally` |
| `grpcweb_bridge_stream_errors_total` | counter | `service`, `method` | Stream failures | `BridgePrometheusMetrics.RecordStreamError` |

Notes on the series:

- **Histogram buckets** are `0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10` seconds. A histogram exposes `_bucket`, `_sum` and `_count` series, for example `grpcweb_bridge_request_duration_seconds_bucket`.
- **Counter names** already end in `_total`, so the exposed names match the table.
- **`grpc_status` values** come from `response.Status.ToString()`, which gives the `GrpcStatusCode` enum name in PascalCase, such as `Ok`, `NotFound` or `Unavailable`. The exception path in the invoke action records the literal `INTERNAL` instead, so a failed call does not use the same value as an `Internal` status from the backend.
- **Label values come from requests.** `service` and `method` are the `ServiceId` and `MethodName` from the request body, so any caller can create new series. Put the endpoint behind a gateway or network policy.

### Where the metrics are recorded

Only `BridgeController` calls the recorders. Nothing else under `src/` does.

- **Unary invoke** (`api/bridge/invoke`) records one call per request, with the status returned by the backend or `INTERNAL` if the call throws. The duration covers the protocol translation and the backend call.
- **Streaming** (`api/bridge/stream`) increments `grpcweb_bridge_active_streams` when the stream is created and decrements it in `finally`. A stream error is counted only when an unhandled exception reaches the outer `catch`. A client disconnect is not counted as an error.
- **No per-message metrics.** Stream messages and batch calls do not record anything.

### Scraping

The endpoint is `GET /metrics` on the bridge's HTTP port. It is served by `app.MapMetrics()` and returns the Prometheus text format.

```bash
curl -s http://localhost:5000/metrics | grep '^grpcweb_bridge_'
```

Replace `localhost:5000` with the address the bridge listens on. The `Kestrel` section of `appsettings.json` sets it to `http://0.0.0.0:5000` by default, so bind it to a loopback or internal address in production.

A static Prometheus scrape job, based on `examples/prometheus.yml`:

```yaml
scrape_configs:
  - job_name: 'grpc-web-bridge'
    metrics_path: '/metrics'
    scrape_interval: 10s
    scrape_timeout: 5s
    static_configs:
      - targets: ['localhost:5000']
```

The Kubernetes example in `examples/kubernetes.yaml` has two options. The pod annotations are `prometheus.io/scrape: "true"`, `prometheus.io/port: "5000"` and `prometheus.io/path: "/metrics"`. The Prometheus Operator option is a `ServiceMonitor` that selects the `grpc-web-bridge` service on the `http` port with `path: /metrics`.

Example queries:

```promql
# Requests per second, by method and gRPC status
sum by (service, method, grpc_status) (rate(grpcweb_bridge_requests_total[5m]))

# p95 latency per method
histogram_quantile(0.95, sum by (le, service, method) (rate(grpcweb_bridge_request_duration_seconds_bucket[5m])))

# Open streams
grpcweb_bridge_active_streams
```

### Caveats

- **Unauthenticated endpoint.** `/metrics` has no `[Authorize]` requirement and is excluded from the content-type validation middleware. Anyone who can reach the port can read the metrics, including the service and method names.
- **Process-wide state.** The metrics are static fields. Counters reset when the process restarts, and the gauge is only meaningful within one process.
- **`MetricsController` is separate.** The JSON endpoints under `api/metrics` read their own static counters and do not read these Prometheus metrics. See [MetricsController](#metricscontroller).

## StreamingSessionManager

`GrpcWebBridge.Streaming.StreamingSessionManager` groups related gRPC streams under a logical session, usually one per client connection. A session carries client-level context (user, origin, authentication context, and free-form metadata) that applies to every stream opened in it. The manager is registered as a singleton behind `IStreamingSessionManager` in `Streaming/StreamingExtensions.cs` and in `Program.cs`. It keeps two in-memory maps: session ID to `StreamingSession`, and stream ID to session ID. The second map gives O(1) lookup from a stream to its session.

Source: `src/GrpcWebBridge/Streaming/StreamingSessionManager.cs` (with `src/GrpcWebBridge/Streaming/IStreamingSessionManager.cs` for the interface).

### StreamingSession lifecycle

1. **Created.** `CreateSession` builds a `StreamingSession` with a new `SessionId` (a 32-character GUID in `"N"` format) and registers it. `CreatedAt` and `LastActivityAt` are set to the current UTC time.
2. **Streams attached.** `AssociateStream(sessionId, streamId)` adds the stream ID to the session and writes the reverse mapping. Associating an unknown session returns `false` and logs a warning. Associating the same stream twice is harmless: the set does not grow, and `LastActivityAt` is still updated.
3. **Streams detached.** `DisassociateStream(streamId)` removes the reverse mapping and the stream ID from its session. It is a no-op for unknown streams. It does not close the stream; it only forgets the association.
4. **Closed.** `CloseSessionAsync(sessionId, finalStatus, cancellationToken)` removes the session from the registry first. It then, for every stream still attached, removes the reverse mapping and calls `IBidirectionalStreamingEngine.CloseStreamAsync(streamId, finalStatus, ...)`. The calls run in parallel through `Task.WhenAll`. The method returns `false` if the session is unknown.

`LastActivityAt` is updated when a stream is associated or disassociated. Nothing in the manager expires sessions on idle time. Sessions stay registered until `CloseSessionAsync` is called. `GetSessionSummaries` exposes the idle time so that a caller can decide what to close.

### StreamingSession members

| Member | Description |
|--------|-------------|
| `SessionId` | Generated at construction. Read-only. |
| `UserId`, `ClientOrigin`, `AuthContextId` | Optional, set once at creation (`init`). |
| `CreatedAt` | UTC creation time. |
| `LastActivityAt` | UTC time of the last stream association or disassociation. |
| `Metadata` | `Dictionary<string, string>` for extra values such as a user agent or tenant ID. It is set once at creation, and its contents are not thread-safe. |
| `StreamIds` | Snapshot copy of the attached stream IDs. |
| `StreamCount` | Number of attached streams. |
| `IsEmpty` | `true` when no streams are attached. |

`AddStream`, `RemoveStream` and `Touch` are `internal`. Callers outside the assembly cannot change the stream set directly and must use the manager.

### SessionSummary

`SessionSummary` is a `sealed record` returned by `GetSessionSummaries()`. It is a point-in-time diagnostic view and does not change with the session afterwards.

| Property | Description |
|----------|-------------|
| `SessionId` | Session identifier. |
| `UserId` | User identifier, or `null` for anonymous sessions. |
| `ClientOrigin` | Client origin, or `null`. |
| `StreamCount` | Number of attached streams. |
| `CreatedAt` | UTC creation time. |
| `IdleSeconds` | Seconds between `LastActivityAt` and the moment the summary was built. |

`SessionSummary` does not include the metadata or the stream IDs. Use `GetSession` or `GetActiveSessions` for those.

### IStreamingSessionManager members

| Member | Description |
|--------|-------------|
| `int ActiveSessionCount { get; }` | Number of registered sessions. |
| `StreamingSession CreateSession(string? userId = null, string? clientOrigin = null, string? authContextId = null, Dictionary<string, string>? metadata = null)` | Creates and registers a session. Throws `ArgumentException` when `userId`, `clientOrigin` or `authContextId` is `null` or empty. |
| `bool AssociateStream(string sessionId, string streamId)` | Attaches a stream to a session. Returns `false` when the session is not found. |
| `void DisassociateStream(string streamId)` | Detaches a stream. Safe to call for an unknown stream. |
| `StreamingSession? GetSessionForStream(string streamId)` | Returns the owning session, or `null` when the stream has no mapping. |
| `StreamingSession? GetSession(string sessionId)` | Returns a session by ID, or `null`. |
| `IReadOnlyCollection<StreamingSession> GetActiveSessions()` | Point-in-time snapshot of all registered sessions. |
| `Task<bool> CloseSessionAsync(string sessionId, GrpcStatusCode finalStatus = GrpcStatusCode.Ok, CancellationToken cancellationToken = default)` | Tears down the session's streams with `finalStatus` and removes the session. Returns `false` if the session is unknown. |
| `IReadOnlyList<SessionSummary> GetSessionSummaries()` | Diagnostic summaries of all registered sessions. |

Parameter checks on `AssociateStream`, `DisassociateStream` and `CloseSessionAsync` use `ArgumentException.ThrowIfNullOrEmpty`.

### Caveats

- **`CreateSession` requires all three identity arguments.** The parameters are optional in the signature, but `userId`, `clientOrigin` and `authContextId` must each be non-empty. Passing `null` for any of them throws, so an anonymous session is not possible through this method as written.
- **No automatic expiry.** A session that is never closed stays in memory, and so do its stream mappings. The caller must call `CloseSessionAsync`, typically when the client connection ends.
- **A stream belongs to one session at a time.** `AssociateStream` overwrites the reverse mapping if the stream was already attached elsewhere. The old session still lists the stream ID in `StreamIds` until it is disassociated or closed.
- **Teardown is not transactional.** The session is removed before streams are closed. If the engine throws for one stream, `CloseSessionAsync` throws, and the session is not in the registry any more.
- **Metadata is mutable.** The dictionary passed to `CreateSession` is stored as-is, not copied. Changes made by the caller afterwards are visible to every reader, and concurrent writes are unsafe.

### Usage example

```csharp
using GrpcWebBridge.Domain;
using GrpcWebBridge.Streaming;

public sealed class ClientConnectionHandler(IStreamingSessionManager sessions)
{
    public StreamingSession OnConnected(string userId, string origin, string authContextId)
    {
        return sessions.CreateSession(
            userId: userId,
            clientOrigin: origin,
            authContextId: authContextId,
            metadata: new Dictionary<string, string> { ["tenant"] = "default" });
    }

    public void OnStreamOpened(string sessionId, string streamId)
    {
        sessions.AssociateStream(sessionId, streamId);
    }

    public Task OnDisconnectedAsync(string sessionId, CancellationToken cancellationToken)
    {
        return sessions.CloseSessionAsync(sessionId, GrpcStatusCode.Cancelled, cancellationToken);
    }
}
```

## StreamDiagnosticsService

`GrpcWebBridge.Streaming.StreamDiagnosticsService` is a `BackgroundService` that periodically reads the metrics of every active bidirectional stream from `IBidirectionalStreamingEngine.GetAllMetrics()`. On each pass it aggregates them, logs a structured summary, flags streams with anomalies, and publishes a `StreamingDiagnosticsEvent` to the application `EventBus`. The service only observes. It does not close or modify streams.

Source: `src/GrpcWebBridge/Streaming/StreamDiagnosticsService.cs`. Registration is done by `AddStreamingDiagnostics(...)` in `src/GrpcWebBridge/Streaming/StreamingExtensions.cs`:

```csharp
services.AddStreamingDiagnostics(
    diagnosticsInterval: TimeSpan.FromSeconds(30),
    staleThreshold: TimeSpan.FromMinutes(5),
    backpressureWarnThreshold: 0.10);
```

`AddStreamingDiagnostics` registers a `StreamDiagnosticsOptions` singleton with `TryAddSingleton`, so the first registration wins. It then calls `AddHostedService<StreamDiagnosticsService>()`. A `null` interval or threshold argument falls back to the default shown in the table below.

### StreamDiagnosticsOptions

`StreamDiagnosticsOptions` is a `sealed record` with `init` properties, so it can be created with `new` and `with` expressions.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `CollectionInterval` | `TimeSpan` | `60 seconds` | Sampling interval between diagnostic passes. Passed to a `PeriodicTimer`. |
| `StaleStreamThreshold` | `TimeSpan` | `5 minutes` | Documented as the idle duration after which a stream is considered stale. It is currently stored but never read; see the Caveats below. |
| `BackpressureWarnThreshold` | `double` | `0.10` (10 %) | A stream is flagged at `Warning` when `BackpressureEvents / (MessagesIn + MessagesOut)` is greater than this value. |

### Sampling interval

- `CollectionInterval` is the only scheduling setting. `ExecuteAsync` waits on a `PeriodicTimer` built from it, so the first pass runs one full interval after the service starts, not immediately.
- Each pass is a point-in-time sample of the streams that exist at that moment. Counters are read as cumulative values, so the service does not compute rates. Rates can be derived by the consumer from two events.
- An exception in one pass is logged at `Error` and the loop continues with the next tick. A failing pass does not stop the service.
- The loop ends when the host shuts down, and the service logs `stopped`.

### Collection pass

For each pass the service does the following:

1. If there are no active streams, it clears its internal tracking, logs at `Debug` level, and returns. No event is published in this case.
2. Otherwise it sums messages, bytes, backpressure events and credit wait time across all streams.
3. For each stream with at least one message, it computes the backpressure ratio. A ratio above `BackpressureWarnThreshold` is logged at `Warning` with the suggestion to increase `InitialWindowSize` or channel capacity.
4. For each stream it compares `MessagesIn + MessagesOut` with the value stored at the previous pass. An unchanged value is counted as zero-activity and logged at `Debug` level. The stored value is then updated.
5. Tracking entries for streams that have closed since the last pass are removed.
6. It logs one `Information` summary line containing the counts and totals.
7. If an `EventBus` is available, it publishes a `StreamingDiagnosticsEvent` (see below).

### StreamingDiagnosticsEvent

`StreamingDiagnosticsEvent` derives from `EventBase`. It is a `sealed class` with `required init` properties, so every field must be set by the publisher. `Source` is set to `nameof(StreamDiagnosticsService)`.

| Property | Type | Description |
|----------|------|-------------|
| `ActiveStreamCount` | `int` | Number of active streams in the sample. |
| `TotalMessagesIn` | `long` | Sum of inbound messages. |
| `TotalMessagesOut` | `long` | Sum of outbound messages. |
| `TotalBytesIn` | `long` | Sum of inbound payload bytes. |
| `TotalBytesOut` | `long` | Sum of outbound payload bytes. |
| `TotalBackpressureEvents` | `long` | Sum of backpressure throttle events. |
| `TotalCreditWaitMs` | `long` | Sum of credit wait time, in milliseconds. |
| `ZeroActivityStreamCount` | `int` | Streams whose message total did not change since the previous pass. |
| `HighBackpressureStreamCount` | `int` | Streams whose backpressure ratio is above the threshold. |

`EventBase` also provides `EventId`, `CreatedAt` and `Source`. The event is published with `_ = _eventBus.PublishAsync(...)`, which is fire-and-forget. The service does not await the publish.

### Caveats

- **`StaleStreamThreshold` has no effect.** The option is declared and registered, but `CollectAndReport` never reads it. Zero-activity detection is based only on the change between two consecutive passes, not on elapsed idle time. Treat the option as reserved until it is wired up.
- **The first pass cannot detect zero activity.** A stream has no previous count on the first pass it appears in, so it is not counted as zero-activity until the next pass.
- **Zero-activity is a message-count check.** A stream that is busy on bytes but sends no messages in an interval still counts as zero-activity. A stream with no messages at all also has no backpressure ratio, so it is never flagged as high-backpressure.
- **The event is optional.** `StreamDiagnosticsService` takes `EventBus?`. If no `EventBus` is resolved, the service still logs, but no event is published.
- **Empty passes publish nothing.** Subscribers do not receive an event with `ActiveStreamCount = 0`.
- **Totals cover live streams only.** The totals are sums over streams alive at the sample. When a stream closes, its counters leave the sum, so totals can decrease between events.

### Usage example

```csharp
using GrpcWebBridge.Events;
using GrpcWebBridge.Streaming;

public sealed class StreamingAlertHook
{
    public StreamingAlertHook(EventBus eventBus)
    {
        eventBus.Subscribe<StreamingDiagnosticsEvent>(evt =>
        {
            if (evt.HighBackpressureStreamCount > 0)
            {
                // Forward to metrics or alerting here.
            }
        });
    }
}
```

## AuthenticationService

`AuthenticationService` (`src/GrpcWebBridge/Services/AuthenticationService.cs`) is a sealed singleton that authenticates incoming requests and checks roles. It supports three of the `AuthenticationScheme` values: `Bearer`, `ApiKey` and `Custom`. Each successful call returns an `AuthenticationContext` and stores it in an in-memory cache keyed by the context `Id`. Failures throw `GrpcWebBridgeException` with a fixed error code. The service only logs failures. It does not publish events.

### Supported schemes

| `AuthenticationScheme` | Method | Token in the context | Expiration |
|------------------------|--------|----------------------|------------|
| `Bearer` (1) | `AuthenticateBearer(string token, string? userId = null)` | The raw JWT. | The token's `exp` claim (`ValidTo`). |
| `ApiKey` (2) | `AuthenticateApiKey(string apiKey, string userId)` | The API key. | 60 minutes from now. |
| `Custom` (4) | `AuthenticateCustom(string userId, Dictionary<string, string> credentials)` | `null`. | 60 minutes from now. |

`AuthenticationScheme.None` and `AuthenticationScheme.Certificate` are defined in the enum, but this service has no method for them.

- **Bearer.** The token is read with `JwtSecurityTokenHandler`. It is not verified. The service does not check the signature, the issuer, or the audience. It only checks that the token is readable and that `ValidTo` is in the future. A token without an `exp` claim has `ValidTo` set to its default value, so it is rejected as `TOKEN_EXPIRED`. The user ID is `userId` if given, otherwise the `sub` claim, otherwise `"unknown"`.
- **ApiKey.** The key is stored as the context token and is not checked against any store. Any non-empty key is accepted, so the caller has to validate the key before calling this method.
- **Custom.** Every entry in `credentials` becomes a claim. The service does not validate the credentials.

`ExtractBearerToken(string? authHeader)` reads the token from an `Authorization` header. It matches the `Bearer ` prefix without regard to case, trims the value, and returns `null` when the header is missing, has another scheme, or has an empty token.

### JWT constants

`GrpcWebBridge.Domain.Constants.Authentication` holds the JWT settings:

| Constant | Value | Used by |
|----------|-------|---------|
| `JwtIssuer` | `"sarmkadan.com"` | Expected `iss` value. Not checked by `AuthenticationService`. |
| `JwtAudience` | `"grpc-web-bridge"` | Expected `aud` value. Not checked by `AuthenticationService`. |
| `JwtExpirationMinutes` | `60` | Lifetime of `ApiKey` and `Custom` contexts. |

Nothing in `AuthenticationService` reads `JwtIssuer` or `JwtAudience`. The ASP.NET JWT bearer handler registered by `AddGrpcWebBridgeAuthentication` also has `ValidateIssuer`, `ValidateAudience` and `ValidateIssuerSigningKey` set to `false`. Validating the issuer and audience is the job of the caller who configures `TokenValidationParameters`.

### AuthenticationContext result

Every successful call returns an `AuthenticationContext` (`GrpcWebBridge.Domain.Models`). The service sets these members:

| Member | Bearer | ApiKey | Custom |
|--------|--------|--------|--------|
| `Id` | New GUID (N format). | Same. | Same. |
| `Scheme` | `Bearer` | `ApiKey` | `Custom` |
| `Token` | Raw JWT. | API key. | `null` |
| `UserId` | See above. | `userId` argument. | `userId` argument. |
| `Roles` | From the role claim (see below). | Empty. | Empty. |
| `Claims` | All other JWT claims. | Empty. | One entry per credential. |
| `ExpiresAt` | `ValidTo` (UTC). | Now + 60 min. | Now + 60 min. |
| `IsAuthenticated` | `true` | `true` | `true` |

The service does not set `Username`, `IpAddress` or `CustomData`. Callers can set them afterwards. `AuthenticatedAt` is the time the context was created.

**Roles.** A claim whose type is exactly `ClaimTypes.Role` (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`) is added to `Roles`. Every other claim goes into `Claims`, keyed by its type. Because the token is only read, the claim types are the raw names from the token. A plain `role` claim is stored under `Claims["role"]` and is not added to `Roles`.

### Role and state checks

- `bool ValidateContext(AuthenticationContext context)` returns `false` for `null`, for an expired context, for a context that is not authenticated, and when `Validate()` throws. It never throws itself.
- `bool AuthorizeRole(AuthenticationContext context, string requiredRole)` runs `ValidateContext` and then `HasRole`.
- `bool AuthorizeAnyRole(AuthenticationContext context, params string[] requiredRoles)` runs `ValidateContext` and then `HasAnyRole`. With no roles given it returns `false`.
- `AuthenticationContext? GetCachedContext(string contextId)` returns a context that an earlier call cached, or `null`.

### Failure responses and error codes

Each failing call throws `GrpcWebBridgeException`. The exception's `ErrorCode` is one of the values below. The message is the text shown in the table.

| Method | Condition | `ErrorCode` | Message |
|--------|-----------|-------------|---------|
| `AuthenticateBearer` | Token is null, empty or whitespace. | `INVALID_TOKEN` | `Token cannot be empty` |
| `AuthenticateBearer` | Token cannot be read as a JWT. | `INVALID_TOKEN` | `Invalid token format` |
| `AuthenticateBearer` | Token read but is not a `JwtSecurityToken`. | `TOKEN_PARSE_ERROR` | `Failed to parse token` |
| `AuthenticateBearer` | `ValidTo` is earlier than now (or missing). | `TOKEN_EXPIRED` | `Token has expired` |
| `AuthenticateBearer` | Any other exception, including validation of the context. | `AUTH_FAILED` | `Authentication failed: {reason}` |
| `AuthenticateApiKey` | API key is null, empty or whitespace. | `INVALID_API_KEY` | `API key cannot be empty` |
| `AuthenticateApiKey` | `userId` is null, empty or whitespace. | `INVALID_USER_ID` | `User ID cannot be empty` |
| `AuthenticateCustom` | `userId` is null, empty or whitespace. | `INVALID_USER_ID` | `User ID cannot be empty` |
| `AuthenticateCustom` | `credentials` is `null` or empty. | `INVALID_CREDENTIALS` | `Credentials cannot be empty` |

`CreateAuthFailureResponse(string requestId)` builds a `GrpcResponse` with `RequestId` set and an error status of `GrpcStatusCode.Unauthenticated` with the message `Authentication required`. It does not throw. Use it when a request has no usable credentials.

### Failure events

`AuthenticationFailedEvent` (`GrpcWebBridge.Events`, in `Events/EventBus.cs`) has a `FailureReason` string and an optional `ClientIp`. It derives from `EventBase`. The service never publishes it. The only failure output from `AuthenticationService` is a log entry at Warning or Error level. To get an event, the caller must catch the `GrpcWebBridgeException` and publish one itself.

### Caveats

- **Bearer tokens are not verified.** `AuthenticateBearer` accepts any readable JWT whose `exp` is in the future. An attacker can forge the payload and the service will accept it, so the output of this method must not be trusted as an identity on its own. Put signature validation in front of it. `BridgeController` calls `AuthenticateBearer` directly and does not verify the signature itself.
- **Failed bearer authentication is anonymous in `BridgeController`.** Its `ExtractAuthContext` catches every exception from `AuthenticateBearer` and treats the request as unauthenticated. See [BridgeController](#bridgecontroller).
- **The API key is logged.** `AuthenticateApiKey` writes the raw key to the Information log with the message `AuthenticateApiKey called with {ApiKey} and {UserId}`. Anyone with access to the logs can read the keys.
- **The context cache is never trimmed.** Every successful call adds its context to an in-memory dictionary keyed by `Id`, and nothing removes entries. Expired contexts stay in memory until the process restarts.
- **Empty credential values raise `ArgumentException`.** `AuthenticateCustom` calls `AuthenticationContext.AddClaim` for each credential. An empty key or value throws `ArgumentException`, not `GrpcWebBridgeException`, so callers that catch only the bridge exception will not see it.

### Usage example

```csharp
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging.Abstractions;

var auth = new AuthenticationService(NullLogger<AuthenticationService>.Instance);

// Returns null when the request may proceed, otherwise the response to send.
GrpcResponse? Authorize(string? authorizationHeader, string requestId)
{
    var token = auth.ExtractBearerToken(authorizationHeader);
    if (token is null)
    {
        return auth.CreateAuthFailureResponse(requestId);
    }

    try
    {
        // The signature and iss/aud must be verified before this call (see Caveats).
        var context = auth.AuthenticateBearer(token);

        // CreateAuthFailureResponse always uses Unauthenticated, so a missing role gets the same status.
        return auth.AuthorizeRole(context, "admin")
            ? null
            : auth.CreateAuthFailureResponse(requestId);
    }
    catch (GrpcWebBridgeException)
    {
        return auth.CreateAuthFailureResponse(requestId);
    }
}
```

`AuthenticateBearer` throws `GrpcWebBridgeException` for every failure listed above, so the `catch` covers an invalid, expired or unreadable token.

## ProtocolTranslationService

`ProtocolTranslationService` (`src/GrpcWebBridge/Services/ProtocolTranslationService.cs`) translates request and response payloads and metadata between the HTTP/JSON side and the binary protobuf side of a gRPC call. It works on message payload bytes only. The gRPC-Web 5-byte frame header is handled separately by `StreamAdapter` (see [gRPC-Web framing](#grpc-web-framing)).

The same file defines `ProtocolTranslationExtensions`, which contains one helper: `AsBytes(this string value)`, a UTF-8 encoding of a string. Add `using GrpcWebBridge.Services;` to use it.

The service is registered as a singleton (`Configuration/DependencyInjection.cs`) and takes an `ILogger<ProtocolTranslationService>`. It holds no mutable state, so one instance can be shared across requests.

### Where it is called

| Caller | Method used |
|---|---|
| `Controllers/BridgeController.cs` (`invoke`, `stream`, `batch`) | `TranslateAndInvokeAsync` |
| `Services/GrpcWebBridge.cs` | `TranslateHttpToGrpc` |
| `Services/HttpTranscoder.cs` | `TranslateHttpToGrpc` |
| `Extensions/ServiceExtensions.cs` | `CreateErrorResponse` |

`ConvertJsonToProtobuf`, `ConvertProtobufToJson` and `TranslateGrpcToHttp` are public helpers. Nothing in `src/` calls them today; they are exercised by the unit tests and benchmarks.

### Request flow: JSON to protobuf

1. **Accept the body.** The caller passes the service name, method name, body bytes and a `SerializationFormat` to `TranslateHttpToGrpc`.
2. **Build the request.** The blank checks run first. A `GrpcRequest` is then created, which validates the names and the payload size. The format is stored in `PayloadFormat`.
3. **Validate.** `ValidateRequest` calls `GrpcRequest.Validate()` and then checks the payload against `Constants.Grpc.MaxMessageSize` (4 MB).
4. **Translate metadata.** `TranslateMetadata` lowercases keys using invariant casing, skips blank keys, drops the `grpc-timeout` key, and replaces `null` values with `""`. When a key appears more than once after lowercasing, the last value wins.
5. **Invoke.** `TranslateAndInvokeAsync` validates the request again. If the caller is authenticated, it adds `x-bridge-user` with the user ID to the outbound metadata. It then returns a `GrpcResponse` with status `Ok` and the request payload. See [Current limitations](#current-limitations).
6. **Translate the response.** `TranslateGrpcToHttp(response, targetFormat)` converts the payload only when the formats differ:
   - target `Json` and payload `Protobuf` goes through `ConvertProtobufToJson`;
   - target `Protobuf` and payload `Json` goes through `ConvertJsonToProtobuf`;
   - any other combination returns the payload unchanged.

### JSON and protobuf conversion

The conversion is an envelope, not a schema-aware mapping. The service does not read `.proto` definitions and does not map JSON fields to protobuf fields. The protobuf bytes are carried as base64 in a `data` property.

| Method | Input | Output |
|---|---|---|
| `ConvertProtobufToJson` | Empty array | `{}` |
| `ConvertProtobufToJson` | Any other bytes | `{"data":"<base64>"}`, with no indentation |
| `ConvertJsonToProtobuf` | Empty array | Empty array |
| `ConvertJsonToProtobuf` | Object with a non-empty string `data` | The base64-decoded bytes |
| `ConvertJsonToProtobuf` | Any other valid JSON | The input bytes returned unchanged |

```csharp
using GrpcWebBridge.Services;

var translator = serviceProvider.GetRequiredService<ProtocolTranslationService>();

// {"data":"AQID"} decodes to the three bytes 0x01 0x02 0x03.
byte[] protobuf = translator.ConvertJsonToProtobuf("{\"data\":\"AQID\"}".AsBytes());

// Encodes back to {"data":"AQID"}.
byte[] json = translator.ConvertProtobufToJson(protobuf);
```

### gRPC-Web framing

Framing is not done by `ProtocolTranslationService`. `Streaming/StreamAdapter.cs` implements it, and `Services/GrpcWebBridge.cs` uses it to deframe the request and frame the response. Each frame has a 5-byte header followed by the payload:

| Byte offset | Size | Meaning |
|---|---|---|
| 0 | 1 | Flags. `0x00` is a data frame. `0x80` is a trailer frame. |
| 1–4 | 4 | Payload length as a 32-bit big-endian integer. |
| 5 | Length | Payload bytes. For a data frame, these are the protobuf message bytes. |

- The maximum frame payload is 4 MB (`StreamAdapter.MaxFrameSize`).
- A trailer payload is ASCII text in the form `grpc-status:<code>\r\ngrpc-message:<text>\r\n`. `CreateTrailerFrame` writes it.
- `DeframeMessageAsync` returns the payload of a data frame. It returns `null` when the stream ends cleanly or when it reads a trailer frame; trailer content is read and discarded.

Example: a 3-byte message `0x0A 0x01 0x41` is framed as `00 00 00 00 03 0A 01 41`.

### Error cases

| Method | Condition | Result |
|---|---|---|
| `TranslateHttpToGrpc` | Blank service or method name | `ProtocolException` (`PROTOCOL_ERROR`) |
| `TranslateHttpToGrpc` | `null` body | `ArgumentNullException` |
| `TranslateHttpToGrpc` | Invalid name, payload over 4 MB, or any other failure | `ProtocolException` with the message `Protocol translation from <service> to <method> failed: ...` |
| `ConvertJsonToProtobuf` | Malformed JSON, invalid base64 in `data`, or a root that is not an object (for example `[1]`) | `ProtocolException` (`JSON` to `Protobuf`) |
| `ConvertProtobufToJson` | `null` input | `ArgumentNullException` |
| `TranslateGrpcToHttp` | `null` response | `ArgumentNullException` |
| `TranslateGrpcToHttp` | `response.Validate()` fails, such as an empty request ID or a negative duration | `ProtocolException` (`Protobuf` or `JSON` to the target format) |
| `TranslateAndInvokeAsync` | `GrpcWebBridgeException` from validation | Rethrown unchanged |
| `TranslateAndInvokeAsync` | Any other exception, including validation failures | Not thrown. Returns an error `GrpcResponse` with status `Internal` and the exception message |
| `CreateErrorResponse` | Blank request ID or message | `ArgumentException` |
| `CreateErrorResponse` | `statusCode` is `Ok` | `ArgumentException`, raised by `GrpcResponse.SetError` |
| `StreamAdapter.DeframeMessageAsync` | Incomplete header or body, or a length that is negative or over 4 MB | `ProtocolException` |
| `StreamAdapter.FrameMessage` | Payload over 4 MB | `ProtocolException` |

Note that `TranslateAndInvokeAsync` does not throw for ordinary validation failures. The caller receives an `Internal` response instead, so check `GrpcResponse.Status` as well as catching exceptions.

### Current limitations

- **No backend call yet.** `TranslateAndInvokeAsync` does not open a gRPC channel. It returns `Ok` with the request payload echoed back. Callers that need a real backend response must wait for that integration.
- **`x-bridge-user` is not sent.** The method adds it to the outbound metadata, but nothing forwards that metadata to a backend yet.
- **Conversion is not schema-aware.** `ConvertJsonToProtobuf` and `ConvertProtobufToJson` only wrap and unwrap the base64 `data` envelope. Clients must send and receive that envelope, not plain JSON objects that match the message definition.

## Response Formatters (JSON/XML/CSV)

`src/GrpcWebBridge/Formatters/` contains four classes for turning data into response bodies:

| File | Type | Output |
|------|------|--------|
| `ResponseFormatter.cs` | `static class ResponseFormatter` | Standard JSON envelopes (`success`, `message`, `data`, `timestamp`, ...) |
| `JsonFormatter.cs` | `sealed class JsonFormatter` | Plain JSON for any object, with `JsonFormatterOptions` |
| `XmlFormatter.cs` | `sealed class XmlFormatter` | XML through `XmlSerializer`, with `XmlFormatterOptions` |
| `CsvFormatter.cs` | `sealed class CsvFormatter` | RFC 4180-style CSV, with `CsvFormatterOptions` |

`XmlFormatterExtensions.cs` and `CsvFormatterExtensions.cs` sit beside the XML and CSV classes. The JSON formatter has no companion extension file.

### How the formatter is selected

The bridge does not select a formatter from the `Accept` header. No output formatter is registered with MVC, and no code reads `Accept` to choose between JSON, XML, and CSV. The caller picks a class and calls it directly.

- Controllers are annotated `[Produces("application/json")]` (`BridgeController`, `HealthCheckController`, `MetricsController`, `ConfigurationController`). ASP.NET serializes their results as JSON.
- `ResponseFormatter` is used by `HealthCheckController` to build its envelopes. The helpers return anonymous objects, and `ResponseFormatter.ToJson` turns them into a string.
- `XmlFormatter` and `CsvFormatter` have no callers outside the `Formatters/` folder. They are available for code that needs them, and nothing in the request pipeline calls them.
- `ProtobufUtility` uses `Google.Protobuf.JsonFormatter`, not the `JsonFormatter` in this folder.

If you want `Accept`-based negotiation, your code has to read the header and choose the formatter. The bridge does not do it for you:

```csharp
// Caller-side selection. This is not built into the bridge.
var accept = context.Request.Headers.Accept.ToString();
string body = accept switch
{
    "text/csv"        => new CsvFormatter().ToCsv(rows),
    "application/xml" => new XmlFormatter().ToXml(response),
    _                 => ResponseFormatter.ToJson(response),
};
```

### ResponseFormatter

Each `Format*` method returns an `object`. Pass it to `ToJson` to get a string. `ToJson(response, indented: true)` pretty-prints the output. The default is compact.

`FormatSuccess(data, message, metadata)` with `ToJson(..., indented: true)`:

```json
{
  "success": true,
  "message": "ok",
  "data": {
    "id": 1,
    "name": "alpha",
    "active": true
  },
  "metadata": {
    "source": "demo"
  },
  "timestamp": "2026-10-25T10:08:44.4865198Z"
}
```

`FormatError("Not Found", "Missing", 404)` with the default compact output:

```json
{"success":false,"error":"Not Found","message":"Missing","statusCode":404,"timestamp":"2026-10-25T10:08:44.5590721Z"}
```

The other helpers are `FormatSuccessList`, `FormatValidationError`, `FormatStreamingResponse`, `FormatBatchResponse`, `FormatHealthCheckResponse`, `FormatStatisticsResponse`, `WrapResponse`, `FormatServiceRegistryResponse`, and `FormatConfigurationResponse`. Each returns an anonymous object with a `timestamp` key, plus the fields for that response type. `CreateCustomResponse` is the exception: it returns a JSON `string` directly, not an object. `ResponseFormatter` only builds data. It does not write to the HTTP response.

### JsonFormatter

`new JsonFormatter(options)` takes `JsonFormatterOptions`:

| Option | Default | Effect |
|--------|---------|--------|
| `PrettyPrint` | `false` | Indents the output. `Format` and `FormatWithSortedKeys` read this option. |
| `SortKeys` | `false` | Stored on the instance. Use `FormatWithSortedKeys` to sort keys. `Format` does not sort them. |
| `MaxDepth` | `10` | Stored and compared in `Equals`. `Format` does not apply it. |
| `IncludeNullValues` | `false` | Stored and compared in `Equals`. `Format` does not apply it. |

`Format(item)` with defaults:

```json
{"id":1,"name":"alpha","active":true}
```

`Format(item)` with `PrettyPrint = true`:

```json
{
  "id": 1,
  "name": "alpha",
  "active": true
}
```

Keys are camelCase. The static helpers `Minify` and `PrettyPrint` take a JSON string and re-serialize it. `Validate` checks that required fields are present.

### XmlFormatter

`ToXml<T>(obj)` runs `XmlSerializer` on `T`. The type needs a public parameterless constructor and public read/write members, the same rules that apply to `XmlSerializer` in general. It throws `InvalidOperationException` if serialization fails.

`ToXml(new Wrapper { Status = "ok", Count = 2 })` with defaults:

```xml
<?xml version="1.0" encoding="utf-16"?>
<Wrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <Status>ok</Status>
  <Count>2</Count>
</Wrapper>
```

The same object with `OmitXmlDeclaration = true` and `OmitNamespaces = true`:

```xml
<Wrapper>
  <Status>ok</Status>
  <Count>2</Count>
</Wrapper>
```

`XmlFormatterOptions` has these members:

| Option | Default | Effect |
|--------|---------|--------|
| `Indent` | `true` | Indents the output. |
| `IndentChars` | `"  "` | The indentation string. |
| `OmitXmlDeclaration` | `false` | Leaves out the `<?xml ...?>` line. |
| `OmitNamespaces` | `false` | Leaves out the `xsi` and `xsd` namespace declarations. |
| `Encoding` | `UTF8` | Used by `ExportToFileAsync`. |

The declaration reads `encoding="utf-16"` even though `Encoding` defaults to UTF-8. This happens because `ToXml` writes into a `StringWriter`, which reports UTF-16. `ExportToFileAsync` writes that same string with the configured `Encoding`, so a file written with the default settings contains UTF-8 bytes under a UTF-16 declaration. If a consumer reads the declaration, set `OmitXmlDeclaration = true` or write the declaration yourself.

`FormatXml`, `MinifyXml`, `ValidateXml`, `GetElementValueByXPath`, `MergeXml`, `GetTextContent`, and `XmlToDictionary` work on XML strings. `FromXml<T>` and `ImportFromFileAsync<T>` do the reverse.

### CsvFormatter

`ToCsv(items)` writes a header row from the public property names of `T`, then one row per item. Each value is its `ToString()`, so a `bool` is written as `True` or `False`. An empty collection returns an empty string. Each line ends with a newline.

A field is wrapped in double quotes when it contains the delimiter, a double quote, `\r`, or `\n`. Inner quotes are doubled, as RFC 4180 requires.

`ToCsv` on the two-item list `[Item { Id = 1, Name = "alpha", Active = true }, Item { Id = 2, Name = "be,ta \"q\"", Active = false }]`:

```csv
Id,Name,Active
1,alpha,True
2,"be,ta ""q""",False
```

`DictsToCsv` takes `IEnumerable<Dictionary<string, string>>`. The header comes from the keys of the first dictionary. A row that lacks a key gets an empty cell. For `[{ "id": "1", "name": "a" }]`:

```csv
id,name
1,a
```

`CsvFormatterOptions` has these members:

| Option | Default | Effect |
|--------|---------|--------|
| `Delimiter` | `","` | Separates fields. `ToCsv`, `DictsToCsv`, and the quoting rule read it. |
| `Encoding` | `UTF8` | Used by file operations. |
| `IncludeHeaders` | `true` | Stored, but `ToCsv` and `DictsToCsv` always write a header. |
| `TrimWhitespace` | `false` | Stored, but no method applies it. |

`FromCsv` and `FromCsv<T>` parse CSV back into dictionaries or objects. `FromCsv<T>` skips any property it cannot convert. `ValidateCsv` reports rows whose column count does not match the header.

## CryptographyUtility

`CryptographyUtility` (`src/GrpcWebBridge/Utilities/CryptographyUtility.cs`) is a static class in the `GrpcWebBridge.Utilities` namespace. It covers password hashing, SHA-256 and HMAC-SHA256 digests, random token generation, and AES-256-GCM encryption. It uses only `System.Security.Cryptography` and needs no extra packages.

```csharp
using GrpcWebBridge.Utilities;

var hash = CryptographyUtility.HashPassword("correct horse battery staple");
var ok = CryptographyUtility.VerifyPassword("correct horse battery staple", hash); // true

var key = CryptographyUtility.GenerateApiKey(32);           // keep this in configuration
var sealedText = CryptographyUtility.EncryptAes256("secret", key);
var clear = CryptographyUtility.DecryptAes256(sealedText, key); // "secret"
```

### Password hashing (PBKDF2-SHA256)

| Method | Behaviour |
|---|---|
| `HashPassword(string password)` | Generates a random 16-byte salt and derives a 32-byte key with PBKDF2-SHA256 and 100,000 iterations. Returns `{iterations}.{base64 salt}.{base64 hash}`, for example `100000.<salt>.<hash>`. Two calls with the same password return different strings. |
| `VerifyPassword(string password, string hash)` | Recomputes the hash with the iteration count and salt stored in `hash` and compares the result with `CryptographicOperations.FixedTimeEquals`. Returns `false` for any malformed or unparseable hash instead of throwing. |

`VerifyPassword` also accepts the legacy format: a single Base64 string holding the 16-byte salt followed by the 32-byte hash (48 bytes in total). Legacy hashes are verified with 10,000 iterations. Rehash them with `HashPassword` after a successful login if you want to move them to the current format; the class does not do this for you.

Both methods throw `ArgumentException` when `password` (or `hash` in `VerifyPassword`) is `null` or empty.

### SHA-256 and HMAC-SHA256

| Method | Input | Output |
|---|---|---|
| `ComputeSha256(string input)` | UTF-8 bytes of `input` | 64 uppercase hex characters |
| `ComputeHmacSha256(string input, string secretKey)` | UTF-8 bytes of `input`, keyed with the UTF-8 bytes of `secretKey` | 64 uppercase hex characters |

Both methods throw `ArgumentException` for a `null` or empty argument. `ComputeSha256` is suited to checksums and integrity checks. `ComputeHmacSha256` is suited to message authentication. The secret key is used as-is, with no derivation step, so it should be a high-entropy value.

### Random tokens

| Method | Default | Minimum | Output |
|---|---|---|---|
| `GenerateToken(int length = 32)` | 32 bytes | 16 bytes | Base64 of `length` random bytes. The string is longer than `length`. |
| `GenerateApiKey(int length = 32)` | 32 characters | 16 characters | `length` characters drawn from `A-Z`, `a-z` and `0-9`. |

Values below the minimum throw `ArgumentOutOfRangeException`. Both methods use `RandomNumberGenerator`.

### AES-256-GCM encryption

| Method | Behaviour |
|---|---|
| `EncryptAes256(string plaintext, string key)` | Encrypts the UTF-8 bytes of `plaintext` with AES-256-GCM. Returns Base64 of `IV (12 bytes) || ciphertext || tag (16 bytes)`. A fresh random IV is generated for each call. |
| `DecryptAes256(string ciphertext, string key)` | Reverses `EncryptAes256`. Returns the decrypted text as UTF-8. |

**Key requirements**

- `key` must be at least 32 characters long. Only the first 32 characters are used.
- Those 32 characters are encoded as UTF-8 and must produce exactly 32 bytes. Use ASCII characters for the key. A non-ASCII character in the first 32 positions makes the key longer than 32 bytes, and encryption then fails.
- The key string is used directly as the AES key. There is no salt and no key-derivation function. Generate it with `GenerateApiKey(32)` or an equivalent random source, and store it in configuration or a secret store, not in source code.

**Errors**

- `EncryptAes256` throws `ArgumentException` for a `null` or empty `plaintext`, or for a short `key`. Any failure inside the cipher is wrapped in `InvalidOperationException` ("Encryption failed").
- `DecryptAes256` throws `ArgumentException` for an empty `ciphertext` or a short `key`. A wrong key, a modified ciphertext, invalid Base64, or input too short to hold an IV and tag all throw `InvalidOperationException` ("Decryption failed"). The authentication tag is checked, so tampering is detected rather than returning garbled text.

## ValidationUtility

`ValidationUtility` (`src/GrpcWebBridge/Utilities/ValidationUtility.cs`) is a static class in the `GrpcWebBridge.Utilities` namespace. It holds the input checks used for requests and configuration: required values, lengths, email, URL, IP address, service IDs, method names, numeric ranges, ports, required dictionary keys, regex patterns and JWT shape. It uses only the BCL (`System.Net`, `System.Text.RegularExpressions`) and needs no extra packages.

```csharp
using GrpcWebBridge.Utilities;

var (valid, error) = ValidationUtility.ValidateEmail("user@example.com"); // (true, null)
var (ok, msg) = ValidationUtility.ValidatePort(70000);                    // (false, "Port must be between 1 and 65535, got 70000")

ValidationUtility.ValidateServiceId(serviceId).ThrowIfInvalid(nameof(serviceId));
```

### The `(Valid, Error)` contract

Every `Validate*` method returns `(bool Valid, string? Error)`:

- When `Valid` is `true`, `Error` is `null`.
- When `Valid` is `false`, `Error` is a human-readable message that names the field. Use it directly in a response or log line. Do not parse it.

The methods never throw for bad input. The exception is `ValidatePattern`, which throws `ArgumentNullException` when the `pattern` argument itself is `null`.

`ThrowIfInvalid(this (bool Valid, string? Error) result, string paramName)` is an extension method that turns a failed result into an `ArgumentException`. The exception's message is `Error` and its `ParamName` is `paramName`. It does nothing when `Valid` is `true`. Use it in guards: `ValidateEmail(email).ThrowIfInvalid(nameof(email));`.

### Methods

| Method | Accepts | Valid when | Limits and notes |
|---|---|---|---|
| `ValidateNotEmpty(string? value, string fieldName)` | string | Not `null`, not empty, and not whitespace-only. | Whitespace-only input is rejected here. |
| `ValidateNotEmpty<T>(IEnumerable<T>? collection, string fieldName)` | sequence | Not `null` and contains at least one element. | Uses `Any()`, so only the first element is read. |
| `ValidateStringLength(string? value, string fieldName, int minLength = 0, int maxLength = int.MaxValue)` | string | Not `null` or empty, and length within `[minLength, maxLength]`. | Only the empty check is applied when the bounds are left at their defaults. Whitespace is not rejected. |
| `ValidateEmail(string? email)` | string | Not `null` or whitespace-only, matches `^[^\s@]+@[^\s@]+\.[^\s@]+$` (case-insensitive), and is at most **254** characters. | The regex is checked before the length, so an over-long string that also has a bad shape reports "Email format is invalid". |
| `ValidateUrl(string? url)` | string | Not `null` or whitespace-only, parses as an absolute URI, and the scheme is `http` or `https`. | No length limit. Other schemes such as `ftp:` or `mailto:` are rejected. |
| `ValidateIpAddress(string? ipAddress)` | string | Parses with `IPAddress.TryParse`. Accepts IPv4 and IPv6. | Not `null` or whitespace-only. |
| `ValidateServiceId(string? serviceId)` | string | Not `null` or whitespace-only, at most **255** characters, and matches `^[a-zA-Z0-9_\-\.]+$`. | Letters, digits, `_`, `-` and `.` only. |
| `ValidateMethodName(string? methodName)` | string | Not `null` or whitespace-only, at most **255** characters, and matches `^[a-zA-Z][a-zA-Z0-9_]*$`. | Must start with a letter. Letters, digits and `_` only, so `.` and `-` are rejected. |
| `ValidateRange(int value, string fieldName, int minValue = int.MinValue, int maxValue = int.MaxValue)` | `int` | `minValue <= value <= maxValue`. | The message gives the bound that was crossed, not the value. |
| `ValidatePort(int port, string fieldName = "Port")` | `int` | `1 <= port <= 65535`. | Port `0` is rejected because it is reserved for dynamic assignment. The message includes the value received. |
| `ValidateRequiredKeys(Dictionary<string, object>? dict, string fieldName, params string[] requiredKeys)` | dictionary | `dict` is not `null` and contains every key in `requiredKeys`. | The message lists the missing keys. Key comparison uses the dictionary's comparer. |
| `ValidatePattern(string? value, string fieldName, Regex pattern, string patternDescription = "")` | string | Not `null` or empty, and `pattern.IsMatch(value)` is `true`. | Throws `ArgumentNullException` for a `null` pattern. `patternDescription` is used in the message; when empty the message says "specified pattern". |
| `ValidateJwtFormat(string? token)` | string | Not `null` or whitespace-only, splits on `.` into exactly three segments, and each segment decodes with `Convert.FromBase64String` after padding. | Checks shape only. It does not verify the signature or the claims. The decoder is standard Base64, so segments that use base64url characters `-` or `_` are reported as invalid. |

### Other helpers

`SanitizeInput(string? input)` is not a validator. It returns an empty string for `null` or empty input and HTML-encodes `&`, `<`, `>`, `"` and `'`. It does not reject anything.

### Length limits at a glance

| Input | Limit | Enforced by |
|---|---|---|
| Email | 254 characters (RFC 5321) | `ValidateEmail` |
| Service ID | 255 characters | `ValidateServiceId` |
| Method name | 255 characters | `ValidateMethodName` |
| Port | 1 to 65535 | `ValidatePort` |
| Other strings | none by default | `ValidateStringLength` with explicit `minLength` and `maxLength` |

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for the full text.
