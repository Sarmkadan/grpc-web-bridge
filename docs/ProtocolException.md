# ProtocolException

`ProtocolException` represents an error occurring during communication or processing within the `grpc-web-bridge` framework. This exception is designed to capture diagnostic context, specifically regarding protocol format mismatches and request identification, to facilitate troubleshooting and error handling in distributed systems.

## API

### Properties

*   `public string? SourceFormat`
    Gets or sets the format of the incoming data that caused the exception.

*   `public string? TargetFormat`
    Gets or sets the expected format of the data.

*   `public string? RequestId`
    Gets or sets the unique identifier associated with the request that triggered the exception.

### Constructors

*   `public ProtocolException() : base()`
    Initializes a new instance of the `ProtocolException` class with default property values.

*   `public ProtocolException(string message) : base(message, "PROTOCOL_ERROR")`
    Initializes a new instance of the `ProtocolException` class with a specified error message and sets the internal error code to `"PROTOCOL_ERROR"`.

*   `public ProtocolException`
    Initializes a new instance of the `ProtocolException` class using additional constructor overloads.

*   `public ProtocolException`
    Initializes a new instance of the `ProtocolException` class using additional constructor overloads.

### Methods

*   `public override string ToString()`
    Returns a string representation of the current exception, including the error message, the "PROTOCOL_ERROR" code, and available diagnostic properties like `RequestId`, `SourceFormat`, and `TargetFormat`.

## Usage

### Throwing an exception with diagnostic details

```csharp
if (incomingFormat != expectedFormat)
{
    throw new ProtocolException("Data format mismatch detected.")
    {
        SourceFormat = incomingFormat,
        TargetFormat = expectedFormat,
        RequestId = currentRequestContext.Id
    };
}
```

### Catching and logging the exception

```csharp
try
{
    await bridge.ProcessRequestAsync(request);
}
catch (ProtocolException ex)
{
    Logger.LogError("Protocol error occurred for Request {RequestId}: {Message} (Source: {Source}, Target: {Target})",
        ex.RequestId, ex.Message, ex.SourceFormat, ex.TargetFormat);
}
```

## Notes

*   **Error Code:** Instances created via `ProtocolException(string message)` are automatically assigned the internal error code `"PROTOCOL_ERROR"`, which is used by the base exception implementation for standardized logging and reporting.
*   **Thread Safety:** Like most exception types in .NET, `ProtocolException` is intended to be immutable once it is thrown. Modifying properties (`SourceFormat`, `TargetFormat`, `RequestId`) after the exception has been thrown and caught is strongly discouraged.
*   **Property Nullability:** The diagnostic properties `SourceFormat`, `TargetFormat`, and `RequestId` are nullable and may not be populated depending on the context in which the exception was thrown. Consumers should handle potential `null` values when accessing these properties.
