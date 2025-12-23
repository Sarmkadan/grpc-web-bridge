# GrpcRequest

Represents a gRPC request container with metadata, payload, and timing information used for bridging gRPC-Web calls to backend services.

## API

### Properties

#### `public string Id`
A unique identifier for the request. Used for tracking and correlation.

#### `public string ServiceName`
The name of the gRPC service being called (e.g., "Greeter").

#### `public string MethodName`
The name of the method within the service being invoked (e.g., "SayHello").

#### `public string FullMethodName`
The fully qualified method name in gRPC format (e.g., "/greeter.Greeter/SayHello").

#### `public byte[] Payload`
The serialized request payload as a byte array.

#### `public SerializationFormat PayloadFormat`
The serialization format used for the payload (e.g., Protobuf, JSON).

#### `public Dictionary<string, string> Metadata`
A collection of key-value pairs representing HTTP/gRPC metadata headers.

#### `public string? RequestId`
An optional identifier for the request, distinct from `Id`. May be null.

#### `public string? TraceId`
An optional tracing identifier for distributed tracing systems. May be null.

#### `public string? UserId`
An optional identifier for the authenticated user making the request. May be null.

#### `public DateTime CreatedAt`
The timestamp when the request was created.

#### `public int TimeoutMilliseconds`
The request timeout duration in milliseconds.

#### `public MethodType MethodType`
The type of gRPC method being invoked (e.g., Unary, ServerStreaming).

### Constructors

#### `public GrpcRequest()`
Initializes a new instance of the `GrpcRequest` class with default values.

#### `public GrpcRequest(...)`
Initializes a new instance of the `GrpcRequest` class with the specified parameters. Parameters are inferred from the corresponding properties.

### Methods

#### `public void AddMetadata(string key, string value)`
Adds or updates a metadata entry.

- **Parameters**
  - `key`: The metadata key.
  - `value`: The metadata value.
- **Throws**
  - `ArgumentNullException`: If `key` is null.

#### `public string? GetMetadata(string key)`
Retrieves the value associated with the specified metadata key.

- **Parameters**
  - `key`: The metadata key to look up.
- **Returns**
  - The metadata value if found; otherwise, null.
- **Throws**
  - `ArgumentNullException`: If `key` is null.

#### `public bool HasMetadata(string key)`
Determines whether the metadata contains the specified key.

- **Parameters**
  - `key`: The metadata key to check.
- **Returns**
  - `true` if the metadata contains the key; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `key` is null.

#### `public void SetPayload(byte[] payload, SerializationFormat format)`
Sets the request payload and its serialization format.

- **Parameters**
  - `payload`: The serialized payload as a byte array.
  - `format`: The serialization format used.
- **Throws**
  - `ArgumentNullException`: If `payload` is null.

#### `public void Validate()`
Validates the request for required fields and logical consistency.

- **Throws**
  - `InvalidOperationException`: If required fields (e.g., `ServiceName`, `MethodName`) are missing or invalid.
  - `ArgumentException`: If `TimeoutMilliseconds` is negative.

## Usage

### Example 1: Creating and validating a unary gRPC request
