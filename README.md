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
