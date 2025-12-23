# GrpcWebBridgeException

`GrpcWebBridgeException` is a custom exception type used within the `grpc-web-bridge` project to encapsulate errors specific to gRPC-Web bridging scenarios. It extends standard exception handling by including gRPC-specific status codes, error codes, and contextual metadata, enabling more granular error reporting and debugging in gRPC-Web proxy or bridge implementations.

## API

### `public string? ErrorCode`
Gets or sets a machine-readable error code representing the specific failure condition. This is distinct from the gRPC status code and is intended for programmatic error handling logic.

### `public GrpcStatusCode? GrpcStatus`
Gets or sets the gRPC status code associated with the exception. This follows the standard gRPC status codes defined in `Grpc.Core` and is used to convey the nature of the failure in a gRPC-compliant manner.

### `public Dictionary<string, object> Context`
A dictionary containing additional contextual information about the exception. This may include request IDs, endpoint details, or other metadata useful for debugging. The dictionary is mutable and can be modified after instantiation.

### `public GrpcWebBridgeException()`
Initializes a new instance of `GrpcWebBridgeException` with default values. `ErrorCode` and `GrpcStatus` are `null`, and `Context` is initialized as an empty dictionary.

### `public GrpcWebBridgeException(string message)`
Initializes a new instance of `GrpcWebBridgeException` with the specified error message. `ErrorCode` and `GrpcStatus` remain `null`, and `Context` is initialized as an empty dictionary.

- **Parameters**:
  - `message`: A human-readable description of the error.

### `public GrpcWebBridgeException(string message, Exception? innerException)`
Initializes a new instance of `GrpcWebBridgeException` with the specified error message and inner exception.

- **Parameters**:
  - `message`: A human-readable description of the error.
  - `innerException`: The exception that caused the current exception, or `null` if no inner exception is specified.

### `public GrpcWebBridgeException(string message, string errorCode)`
Initializes a new instance of `GrpcWebBridgeException` with the specified error message and error code.

- **Parameters**:
  - `message`: A human-readable description of the error.
  - `errorCode`: A machine-readable error code representing the specific failure condition.

### `public GrpcWebBridgeException(string message, GrpcStatusCode statusCode)`
Initializes a new instance of `GrpcWebBridgeException` with the specified error message and gRPC status code.

- **Parameters**:
  - `message`: A human-readable description of the error.
  - `statusCode`: The gRPC status code associated with the exception.

### `public void AddContext(string key, object value)`
Adds a key-value pair to the `Context` dictionary. If the key already exists, its value is overwritten.

- **Parameters**:
  - `key`: The key of the contextual data.
  - `value`: The value associated with the key.
- **Throws**:
  - `ArgumentNullException`: If `key` is `null`.

### `public object? GetContext(string key)`
Retrieves the value associated with the specified key from the `Context` dictionary.

- **Parameters**:
  - `key`: The key of the contextual data to retrieve.
- **Returns**:
  - The value associated with the key, or `null` if the key does not exist.
- **Throws**:
  - `ArgumentNullException`: If `key` is `null`.

### `public override string ToString()`
Returns a string representation of the exception, including the error message, error code, gRPC status code, and contextual data. This overrides the base `Exception.ToString()` method to include additional gRPC-Web bridge-specific details.

### `public GrpcWebBridgeException WithContext(string key, object value)`
Creates a new `GrpcWebBridgeException` instance with the same properties as the current instance, but with the specified key-value pair added to the `Context` dictionary. This method does not modify the original instance.

- **Parameters**:
  - `key`: The key of the contextual data.
  - `value`: The value associated with the key.
- **Returns**:
  - A new `GrpcWebBridgeException` instance with the updated context.
- **Throws**:
  - `ArgumentNullException`: If `key` is `null`.

### `public GrpcWebBridgeException WithInnerException(Exception innerException)`
Creates a new `GrpcWebBridgeException` instance with the same properties as the current instance, but with the specified inner exception. This method does not modify the original instance.

- **Parameters**:
  - `innerException`: The exception that caused the current exception.
- **Returns**:
  - A new `GrpcWebBridgeException` instance with the updated inner exception.

## Usage

### Example 1: Throwing a `GrpcWebBridgeException` with Context
