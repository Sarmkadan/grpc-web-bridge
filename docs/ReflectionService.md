# ReflectionService

A service that provides runtime reflection capabilities for gRPC services, enabling discovery and inspection of available services and their methods through reflection metadata.

## API

### `ReflectionService`

The main service class providing gRPC reflection functionality. This class is designed to work with the `grpc-web-bridge` project to expose reflection data for gRPC services.

### `async Task<ReflectionResult<IReadOnlyList<string>>> ListServiceNamesAsync()`

Retrieves the names of all available gRPC services exposed by the server.

- **Returns**: A `ReflectionResult` containing an `IReadOnlyList<string>` of service names. The result may indicate failure if reflection data is unavailable or the operation times out.
- **Exceptions**: May throw if the underlying gRPC reflection call fails or if the service is not properly configured.

### `async Task<ReflectionResult<GrpcServiceDescriptor>> GetServiceDescriptorAsync(string serviceName)`

Retrieves the descriptor for a specific gRPC service by its name.

- **Parameters**:
  - `serviceName`: The name of the service to retrieve.
- **Returns**: A `ReflectionResult` containing a `GrpcServiceDescriptor` for the requested service. Returns a failed result if the service does not exist or the descriptor cannot be retrieved.
- **Exceptions**: May throw if the service name is invalid or the reflection call fails.

### `async Task<ReflectionResult<IReadOnlyList<GrpcServiceDescriptor>>> GetAllDescriptorsAsync()`

Retrieves descriptors for all available gRPC services.

- **Returns**: A `ReflectionResult` containing an `IReadOnlyList<GrpcServiceDescriptor>` of all service descriptors. The result may indicate failure if reflection data is unavailable or the operation times out.
- **Exceptions**: May throw if the underlying gRPC reflection call fails or if the service is not properly configured.

### `async Task<ReflectionResult<MethodDescriptor>> GetMethodDescriptorAsync(string serviceName, string methodName)`

Retrieves the descriptor for a specific method within a gRPC service.

- **Parameters**:
  - `serviceName`: The name of the service containing the method.
  - `methodName`: The name of the method to retrieve.
- **Returns**: A `ReflectionResult` containing a `MethodDescriptor` for the requested method. Returns a failed result if the service or method does not exist or the descriptor cannot be retrieved.
- **Exceptions**: May throw if the service name, method name, or combination is invalid, or if the reflection call fails.

## Usage

### Example 1: Listing all available services
