# StreamingException

`StreamingException` represents errors encountered during the lifecycle of a gRPC-web stream, providing diagnostic information such as the stream identifier, the last known state of the stream, and the sequence number at which the error occurred. It inherits from a base exception type and is designed to facilitate structured error handling in streaming scenarios within the `grpc-web-bridge` infrastructure.

## API

- `StreamId` (`string?`, property): Gets or sets the unique identifier of the stream associated with the exception.
- `LastStreamState` (`StreamState?`, property): Gets or sets the `StreamState` at the time the exception was thrown.
- `SequenceNumber` (`int?`, property): Gets or sets the sequence number associated with the occurrence of the error.
- `StreamingException()`: Initializes a new instance of the `StreamingException` class.
- `StreamingException(string message)`: Initializes a new instance of the `StreamingException` class with a specified error message and the default error code "STREAMING_ERROR".
- `StreamingException` (Constructors): Additional constructor overloads are provided to support initialization with inner exceptions or extended diagnostic context.
- `SetStreamState(StreamState state)`: Updates the exception instance with the provided `StreamState`.
- `ToString()`: Returns a string representation of the exception, including the message and available diagnostic fields.

## Usage

### Example 1: Catching and Logging Diagnostic Information
```csharp
try 
{
    // Perform stream operation
} 
catch (StreamingException ex) 
{
    _logger.LogError(
        "Stream failure [ID: {StreamId}] at sequence {Sequence}: {Message}", 
        ex.StreamId, 
        ex.SequenceNumber, 
        ex.Message
    );
}
```

### Example 2: Throwing an Exception with Context
```csharp
public void ValidateStreamState(string streamId, StreamState currentState) 
{
    if (currentState == StreamState.Closed) 
    {
        var ex = new StreamingException("Attempted operation on a closed stream.");
        ex.StreamId = streamId;
        ex.SetStreamState(currentState);
        ex.SequenceNumber = 42;
        throw ex;
    }
}
```

## Notes

- **Data Availability**: The `StreamId`, `LastStreamState`, and `SequenceNumber` properties are nullable. Consumers should implement null checks before accessing these properties, as they may not be populated depending on the point of failure.
- **Thread Safety**: This exception class is not inherently thread-safe for concurrent writes to its properties. Instances should typically be treated as immutable once thrown and captured by a handler.
- **Serialization**: While this class provides a `ToString()` override for logging purposes, it does not guarantee specific behavior for cross-process serialization; implement custom logic if serialization of the diagnostic fields is required.
