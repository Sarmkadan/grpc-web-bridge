# GrpcResponse

The `GrpcResponse` class serves as the primary data container for gRPC responses within the `grpc-web-bridge` project, encapsulating the outcome of a remote procedure call. It aggregates essential response components including the unique request identifier, status codes, optional status messages, serialized payload data, and both initial and trailing metadata dictionaries. This type facilitates the uniform handling of successful results and error conditions by providing dedicated methods to transition the object into a success or error state while maintaining a record of execution duration and creation time.

## API

### Properties

*   **`public string Id`**
    Gets the unique identifier for this specific response instance. This value is typically generated upon instantiation to track the response lifecycle independently of the request.

*   **`public string RequestId`**
    Gets or sets the identifier of the original request associated with this response. This property links the response back to the initiating `GrpcRequest`.

*   **`public GrpcStatusCode Status`**
    Gets or sets the gRPC status code indicating the result of the operation (e.g., `OK`, `NotFound`, `Internal`).

*   **`public string? StatusMessage`**
    Gets or sets an optional human-readable message describing the status. This is often populated when an error occurs to provide additional context.

*   **`public byte[] Payload`**
    Gets or sets the raw binary payload of the response. The content format is defined by the `PayloadFormat` property.

*   **`public SerializationFormat PayloadFormat`**
    Gets or sets the enumeration value specifying the serialization format used for the `Payload` (e.g., ProtoBuf, JSON).

*   **`public Dictionary<string, string> Metadata`**
    Gets the dictionary containing initial metadata key-value pairs sent with the response headers.

*   **`public Dictionary<string, string> TrailingMetadata`**
    Gets the dictionary containing trailing metadata key-value pairs sent at the end of the stream or call.

*   **`public DateTime CreatedAt`**
    Gets the timestamp indicating when the `GrpcResponse` instance was created.

*   **`public long DurationMilliseconds`**
    Gets or sets the total duration of the request processing in milliseconds.

*   **`public string? ErrorDetails`**
    Gets or sets optional detailed error information, often containing serialized debug data or stack traces when `Status` indicates a failure.

### Constructors

*   **`public GrpcResponse()`**
    Initializes a new instance of the `GrpcResponse` class. Default values are assigned to properties, including generating a new `Id` and initializing the `Metadata` and `TrailingMetadata` dictionaries.

*   **`public GrpcResponse`**
    *Note: The provided signature list indicates overloaded constructors exist but does not specify their parameters. These constructors typically allow initialization with specific request IDs or pre-populated status values.*

### Methods

*   **`public void SetSuccess(byte[] payload, GrpcStatusCode status = GrpcStatusCode.OK)`**
    Configures the response to represent a successful operation.
    *   **Parameters**:
        *   `payload`: The binary data to assign to the `Payload` property.
        *   `status`: (Optional) The status code to set; defaults to `OK`.
    *   **Behavior**: Sets the `Payload`, updates `Status`, and ensures `ErrorDetails` is null.

*   **`public void SetError(GrpcStatusCode status, string message, string? details = null)`**
    Configures the response to represent a failed operation.
    *   **Parameters**:
        *   `status`: The specific gRPC error code.
        *   `message`: The human-readable error message assigned to `StatusMessage`.
        *   `details`: (Optional) Detailed error information assigned to `ErrorDetails`.
    *   **Behavior**: Updates `Status`, `StatusMessage`, and `ErrorDetails`. The `Payload` is typically left empty or ignored in this state.

*   **`public void AddMetadata(string key, string value)`**
    Adds a key-value pair to the `Metadata` dictionary.
    *   **Parameters**:
        *   `key`: The metadata key.
        *   `value`: The metadata value.
    *   **Throws**: May throw `ArgumentException` if the key already exists, depending on the underlying dictionary implementation policy, or `ArgumentNullException` if the key is null.

*   **`public void AddTrailingMetadata(string key, string value)`**
    Adds a key-value pair to the `TrailingMetadata` dictionary.
    *   **Parameters**:
        *   `key`: The trailing metadata key.
        *   `value`: The trailing metadata value.
    *   **Throws**: May throw `ArgumentException` if the key already exists or `ArgumentNullException` if the key is null.

*   **`public string? GetMetadata(string key)`**
    Retrieves a value from the `Metadata` dictionary.
    *   **Parameters**:
        *   `key`: The key to look up.
    *   **Returns**: The associated value if found; otherwise, `null`.

*   **`public string? GetTrailingMetadata(string key)`**
    Retrieves a value from the `TrailingMetadata` dictionary.
    *   **Parameters**:
        *   `key`: The key to look up.
    *   **Returns**: The associated value if found; otherwise, `null`.

## Usage

### Example 1: Handling a Successful Response
This example demonstrates creating a response, setting a successful status with a serialized payload, and adding custom header metadata.

```csharp
using System;
using System.Text;

// Assume PayloadFormat and GrpcStatusCode are imported from the relevant namespace
var response = new GrpcResponse();
response.RequestId = "req-12345";

// Simulate serialized data
byte[] data = Encoding.UTF8.GetBytes("{\"result\": \"ok\"}");

// Set the response as successful
response.SetSuccess(data, GrpcStatusCode.OK);

// Add custom metadata
response.AddMetadata("x-custom-header", "value-1");
response.AddTrailingMetadata("x-trailing-info", "completed");

// Calculate duration
response.DurationMilliseconds = (long)(DateTime.UtcNow - response.CreatedAt).TotalMilliseconds;

Console.WriteLine($"Response {response.Id} completed with status {response.Status}");
```

### Example 2: Handling an Error Condition
This example illustrates constructing an error response with a specific status code, message, and detailed error payload.

```csharp
var response = new GrpcResponse();
response.RequestId = "req-98765";

// Configure the response for a NotFound error
string errorMsg = "The requested resource does not exist.";
string debugInfo = "Stacktrace: ...";

response.SetError(GrpcStatusCode.NotFound, errorMsg, debugInfo);

// Verify error details are populated
if (response.ErrorDetails != null)
{
    Console.WriteLine($"Error captured: {response.StatusMessage}");
    Console.WriteLine($"Details: {response.ErrorDetails}");
}

// Attempt to retrieve metadata safely
string correlationId = response.GetMetadata("x-correlation-id") ?? "unknown";
```

## Notes

*   **Thread Safety**: The `GrpcResponse` class utilizes standard `Dictionary<string, string>` instances for `Metadata` and `TrailingMetadata`. These collections are not thread-safe for concurrent writes. If multiple threads need to modify metadata simultaneously, external synchronization (e.g., `lock` statements) is required. Reading via `GetMetadata` while another thread writes may result in undefined behavior or exceptions.
*   **Payload Mutability**: The `Payload` property is a mutable reference to a byte array. Modifying the contents of the array after assigning it to `GrpcResponse` will affect the instance state. It is recommended to treat the payload as immutable once assigned.
*   **Status Consistency**: While the `SetSuccess` and `SetError` helper methods ensure logical consistency between `Status`, `StatusMessage`, and `ErrorDetails`, direct manipulation of these properties bypasses such guards. Developers should prefer the helper methods to maintain valid state transitions.
*   **Null Handling**: Properties `StatusMessage` and `ErrorDetails` are nullable. Consumers must check for null before accessing members of these strings. The `GetMetadata` and `GetTrailingMetadata` methods return `null` if a key is missing, rather than throwing an exception.
