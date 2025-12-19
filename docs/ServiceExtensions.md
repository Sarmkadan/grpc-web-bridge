# ServiceExtensions

The `ServiceExtensions` class provides a collection of static utility methods and associated data structures designed to facilitate the registration, status management, and transformation of services within the `grpc-web-bridge` framework. It serves as a central point for converting internal service health data and gRPC-related responses into formats suitable for HTTP communication and status reporting.

## API

### Static Methods

*   **`Task<bool> TryRegisterServiceAsync(...)`**
    *   **Purpose**: Attempts to register a service within the bridge asynchronously.
    *   **Parameters**: Implementation-specific (requires contextual service descriptors/instances).
    *   **Return Value**: Returns `true` if registration was successful; otherwise, `false`.
    *   **Exceptions**: May throw if the underlying service infrastructure is unavailable.

*   **`bool TryAddMethod(...)`**
    *   **Purpose**: Attempts to add a method handler to a service.
    *   **Parameters**: Implementation-specific (requires method descriptor and handler function).
    *   **Return Value**: Returns `true` if the method was successfully added; otherwise, `false`.

*   **`GrpcResponse ToGrpcResponse(...)`**
    *   **Purpose**: Transforms internal service results or errors into a `GrpcResponse` object.
    *   **Return Value**: A `GrpcResponse` instance representing the service result.

*   **`string GetStatusMessage(...)`**
    *   **Purpose**: Retrieves a human-readable status message for a given service state or error.
    *   **Return Value**: A string containing the status description.

*   **`string ToDisplayString(...)`**
    *   **Purpose**: Converts service-related objects or health states into a user-friendly string format.
    *   **Return Value**: A string representation suitable for logging or UI display.

*   **`bool IsError(...)`**
    *   **Purpose**: Determines if a specific result or status represents an error state.
    *   **Return Value**: `true` if the input indicates an error; otherwise, `false`.

*   **`int ToHttpStatusCode(...)`**
    *   **Purpose**: Maps gRPC-specific status or result codes to corresponding HTTP status codes.
    *   **Return Value**: An integer representing the HTTP status code (e.g., 200, 500).

*   **`ServiceHealthSummary GetHealthSummary(...)`**
    *   **Purpose**: Aggregates health data across services to produce a summary report.
    *   **Return Value**: A `ServiceHealthSummary` object.

*   **`string ToDescription(...)`**
    *   **Purpose**: Generates a detailed description string for a service or method definition.
    *   **Return Value**: A descriptive string.

### Properties

*   **`int TotalServices`**: Gets the total number of registered services.
*   **`int HealthyServices`**: Gets the count of services currently reporting a healthy status.
*   **`int UnhealthyServices`**: Gets the count of services currently reporting an unhealthy status.
*   **`int ActiveStreams`**: Gets the current count of active streaming connections.
*   **`DateTime Timestamp`**: Gets the time at which the health snapshot was recorded.

## Usage

### Example 1: Registering a Service
```csharp
var serviceAdded = await ServiceExtensions.TryRegisterServiceAsync(myServiceDescriptor);
if (serviceAdded)
{
    Console.WriteLine("Service registered successfully.");
}
```

### Example 2: Converting an Internal Error to HTTP
```csharp
var result = PerformServiceAction();
if (ServiceExtensions.IsError(result))
{
    int statusCode = ServiceExtensions.ToHttpStatusCode(result);
    string message = ServiceExtensions.GetStatusMessage(result);
    // Respond to HTTP client
}
```

## Notes

*   **Thread Safety**: The static methods in `ServiceExtensions` are designed to be thread-safe, assuming the underlying collections they operate on are appropriately synchronized within the bridge infrastructure.
*   **Asynchronous Operations**: `TryRegisterServiceAsync` is the only method performing asynchronous I/O; ensure callers properly await this task to avoid blocking the calling thread.
*   **Edge Cases**: Methods accepting status codes or result objects should handle null inputs gracefully, typically returning default values or false, depending on the specific method contract.
