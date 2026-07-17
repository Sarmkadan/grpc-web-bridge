# CLAUDE.md

## GoalTracker Integration

... (rest of the content remains the same)

## StreamingExceptionExtensions
The `StreamingExceptionExtensions` class provides a set of extension methods for working with `StreamingException` instances. These methods allow you to determine the state of a stream, add context to an exception, and retrieve stream-related context. For example, you can use the `IsTerminalState` method to check if a stream is in a terminal state, or use the `WithContext` method to add additional context to an exception:

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
