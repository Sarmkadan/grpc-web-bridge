# CLAUDE.md

## GoalTracker Integration

При работе над задачей, привязанной к цели в GoalTracker, в конце сессии залогируй чат:

```bash
# 1. Получить токен (кешируй - валиден 30 дней)
TOKEN=$(curl -s -X POST https://tracker.sarmkadan.com/api/auth/login \
 -H 'Content-Type: application/json' \
 -d '{"username":"vlad","password":"GoalTracker2026!Vlad"}' | python3 -c "import sys,json;print(json.load(sys.stdin)['token'])")

# 2. Залогировать сессию (GOAL_ID = id цели из трекера)
curl -s -X POST https://tracker.sarmkadan.com/api/goals/GOAL_ID/log \
 -H "Authorization: Bearer $TOKEN" \
 -H 'Content-Type: application/json' \
 -d '{"sessionId":"SESSION_ID","summary":"краткое описание что сделано"}'
```

**Правила:**
- `SESSION_ID` - текущий session id (виден в `claude --resume`). Если неизвестен - используй timestamp вида `manual-2026-06-01-1430`
- `summary` - 1-3 предложения что конкретно сделано в этой сессии. Без воды, только факты
- Логировать в КОНЦЕ сессии, когда работа завершена
- Если Влад явно указал goal ID или название цели - логировать. Если нет - спросить "залогировать в GoalTracker?"
- Не логировать сессии которые не связаны ни с какой целью

**Список целей:** `GET https://tracker.sarmkadan.com/api/goals` (с Bearer токеном)

**Добавить шаг к цели:**
```bash
curl -s -X POST https://tracker.sarmkadan.com/api/goals/GOAL_ID/steps \
 -H "Authorization: Bearer $TOKEN" \
 -H 'Content-Type: application/json' \
 -d '{"description":"описание шага"}'
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