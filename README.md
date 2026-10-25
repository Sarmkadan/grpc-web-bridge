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

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for the full text.
