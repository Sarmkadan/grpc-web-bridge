# ServiceRegistryExtensions

Provides extension methods for querying and inspecting a registry of gRPC services exposed via the `grpc-web-bridge` infrastructure. These methods allow callers to retrieve services by endpoint, package, or health status, and to determine the presence of specific services.

## API

### `GetServiceOrDefault`

Returns the first `GrpcService` registered under the specified endpoint path, or `null` if no such service exists.

- **Parameters**
  - `endpoint`: The HTTP endpoint path (e.g., `/api.MyService/GetData`) to look up.
- **Returns**
  - The matching `GrpcService` instance if found; otherwise, `null`.
- **Throws**
  - Does not throw under normal operation.

### `GetServicesByEndpoint`

Returns all `GrpcService` instances registered under the specified endpoint path.

- **Parameters**
  - `endpoint`: The HTTP endpoint path (e.g., `/api.MyService/GetData`) to query.
- **Returns**
  - An `IEnumerable<GrpcService>` containing zero or more matching services. The order of services is not guaranteed.
- **Throws**
  - Does not throw under normal operation.

### `HasServiceWithHealthStatus`

Determines whether any registered service reports the specified health status.

- **Parameters**
  - `status`: The `HealthStatus` value to check against (e.g., `HealthStatus.Healthy`).
- **Returns**
  - `true` if at least one service has the given health status; otherwise, `false`.
- **Throws**
  - Does not throw under normal operation.

### `GetServicesByPackageDictionary`

Returns a read-only dictionary mapping package names to lists of services belonging to each package.

- **Returns**
  - An `IReadOnlyDictionary<string, IReadOnlyList<GrpcService>>` where each key is a package name and each value is an immutable list of services in that package. The dictionary and lists are guaranteed not to change after construction.
- **Throws**
  - Does not throw under normal operation.

## Usage
