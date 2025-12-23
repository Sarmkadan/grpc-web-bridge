# GrpcService

Represents a gRPC service definition exposed via the gRPC-Web bridge, including metadata, endpoint configuration, and method management.

## API

### Properties

#### `public string Id`
A unique identifier for the service instance. Must not be null or empty.

#### `public string Name`
The short name of the service, typically the protobuf service name without the package prefix.

#### `public string PackageName`
The protobuf package name where this service is defined.

#### `public string FullName`
The fully-qualified service name in the format `{PackageName}.{Name}`.

#### `public string? Description`
An optional human-readable description of the service.

#### `public string Endpoint`
The network endpoint where the service is exposed (e.g., `localhost:50051`).

#### `public int Port`
The port number derived from `Endpoint`. Must be a valid port number (1–65535).

#### `public bool UseTls`
Indicates whether TLS/SSL is enabled for connections to this service.

#### `public ServiceStatus Status`
Current operational status of the service (e.g., `ServiceStatus.Running`, `ServiceStatus.Stopped`).

#### `public DateTime CreatedAt`
Timestamp when the service instance was created.

#### `public DateTime? UpdatedAt`
Timestamp when the service instance was last updated, or `null` if never updated.

#### `public Dictionary<string, string> Metadata`
A collection of key-value pairs providing additional service-level configuration or context.

### Methods

#### `public GrpcService()`
Constructs a new `GrpcService` with default values:
- `Id` generated as a GUID.
- `Status` set to `ServiceStatus.Stopped`.
- `CreatedAt` set to current UTC time.
- `Metadata` initialized as an empty dictionary.

#### `public GrpcService(string id, string name, string packageName, string endpoint, bool useTls)`
Constructs a new `GrpcService` with the specified required fields:
- `id`: Unique identifier.
- `name`: Service name.
- `packageName`: Protobuf package name.
- `endpoint`: Network endpoint.
- `useTls`: TLS flag.
Other fields (e.g., `Status`, `CreatedAt`) are initialized to defaults.

#### `public void AddMethod(GrpcMethod method)`
Adds a gRPC method to the service. Throws `ArgumentNullException` if `method` is `null`.

#### `public GrpcMethod? GetMethod(string methodName)`
Retrieves the method with the given `methodName`, or `null` if not found.

#### `public bool HasMethod(string methodName)`
Returns `true` if a method with the given `methodName` exists; otherwise, `false`.

#### `public void RemoveMethod(string methodName)`
Removes the method with the given `methodName`. Does nothing if the method does not exist.

#### `public void SetMetadata(string key, string value)`
Sets a metadata entry with the specified `key` and `value`. If the key already exists, its value is overwritten.

#### `public string? GetMetadata(string key)`
Retrieves the metadata value associated with `key`, or `null` if the key does not exist.

## Usage
