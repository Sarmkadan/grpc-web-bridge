# GoalTracker Integration

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
```