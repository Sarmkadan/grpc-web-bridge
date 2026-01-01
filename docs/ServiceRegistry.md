# ServiceRegistry

The `ServiceRegistry` type provides an in‑memory catalog of gRPC services that can be looked up, updated, and enumerated by the gRPC‑Web bridge. It enables the bridge to resolve incoming HTTP requests to the appropriate service definition, track metadata such as health status and caching timestamps, and manage the lifecycle of registered services.

## API

### `ServiceRegistry()`
Initializes a new, empty service registry. No parameters are required. The instance is ready to accept service registrations immediately after construction.

### `void RegisterService(GrpcService service)`
Registers a service in the catalog.

- **Parameters**  
  - `service`: The `GrpcService` instance to register. Must not be `null`.
- **Return value**  
  - None.
- **Exceptions**  
  - `ArgumentNullException` if `service` is `null`.  
  - `InvalidOperationException` if a service with the same `ServiceName` (or `FullName`, depending on the implementation) is already registered.

### `GrpcService? GetService(string serviceName)`
Retrieves a service by its short name.

- **Parameters**  
  - `serviceName`: The name of the service to look up. Must not be `null` or empty.
- **Return value**  
  - The matching `GrpcService` instance, or `null` if no service with the given name is registered.
- **Exceptions**  
  - `ArgumentNullException` if `serviceName` is `null`.  
  - `ArgumentException` if `serviceName` is empty.

### `GrpcService? GetService(string fullName)`
Retrieves a service by its fully qualified name (e.g., `package.Service`).

- **Parameters**  
  - `fullName`: The fully qualified name of the service. Must not be `null` or empty.
- **Return value**  
  - The matching `GrpcService` instance, or `null` if no service with the given full name is registered.
- **Exceptions**  
  - `ArgumentNullException` if `fullName` is `null`.  
  - `ArgumentException` if `fullName` is empty.

### `bool UnregisterService(string serviceName)`
Removes a service from the registry.

- **Parameters**  
  - `serviceName`: The name of the service to unregister. Must not be `null` or empty.
- **Return value**  
  - `true` if a service was found and removed; `false` if no matching service existed.
- **Exceptions**  
  - `ArgumentNullException` if `serviceName` is `null`.  
  - `ArgumentException` if `serviceName` is empty.

### `IEnumerable<GrpcService> ListServices()`
Enumerates all currently registered services.

- **Parameters**  
  - None.
- **Return value**  
  - An enumerable containing a snapshot of all `GrpcService` instances. The enumeration is safe to iterate while the registry is being modified, but the snapshot reflects the state at the moment the method was called.
- **Exceptions**  
  - None.

### `IEnumerable<GrpcService> ListServicesByPackage(string package)`
Enumerates services that belong to a specific package.

- **Parameters**  
  - `package`: The package identifier (e.g., `my.package`). Must not be `null` or empty.
- **Return value**  
  - An enumerable of `GrpcService` instances whose `FullName` starts with the supplied package followed by a dot. Returns an empty enumerable if no services match.
- **Exceptions**  
  - `ArgumentNullException` if `package` is `null`.  
  - `ArgumentException` if `package` is empty.

### `bool ServiceExists(string serviceName)`
Checks whether a service with the given name is present in the registry.

- **Parameters**  
  - `serviceName`: The name to test. Must not be `null` or empty.
- **Return value**  
  - `true` if a service with that name is registered; otherwise `false`.
- **Exceptions**  
  - `ArgumentNullException` if `serviceName` is `null`.  
  - `ArgumentException` if `serviceName` is empty.

### `void UpdateServiceStatus(string serviceName, ServiceHealthStatus status)`
Updates the health status of a registered service.

- **Parameters**  
  - `serviceName`: The name of the service to update. Must not be `null` or empty.  
  - `status`: The new `ServiceHealthStatus` value.
- **Return value**  
  - None.
- **Exceptions**  
  - `ArgumentNullException` if `serviceName` is `null`.  
  - `ArgumentException` if `serviceName` is empty.  
  - `InvalidOperationException` if no service with the given name is found.

### `ServiceMetadata? GetCachedMetadata(string serviceName)`
Retrieves the cached metadata for a service, if available.

- **Parameters**  
  - `serviceName`: The name of the service. Must not be `null` or empty.
- **Return value**  
  - The `ServiceMetadata` instance associated with the service, or `null` if no metadata has been cached.
- **Exceptions**  
  - `ArgumentNullException` if `serviceName` is `null`.  
  - `ArgumentException` if `serviceName` is empty.

### `ServiceHealthStatus GetHealthStatus(string serviceName)`
Obtains the current health status of a service.

- **Parameters**  
  - `serviceName`: The name of the service. Must not be `null` or empty.
- **Return value**  
  - The `ServiceHealthStatus` value for the service.
- **Exceptions**  
  - `ArgumentNullException` if `serviceName` is `null`.  
  - `ArgumentException` if `serviceName` is empty.  
  - `KeyNotFoundException` if the service is not registered.

### `string? ServiceName`
Gets or sets the short name of the service associated with this registry instance (useful when the registry is scoped to a single service).

### `string? FullName`
Gets or sets the fully qualified name of the service associated with this registry instance.

### `string? Endpoint`
Gets or sets the network endpoint (e.g., `localhost:50051`) of the service associated with this registry instance.

### `int Port`
Gets or sets the port number of the service endpoint.

### `int MethodCount`
Gets or sets the number of methods defined in the service.

### `DateTime CachedAt`
Gets or sets the timestamp indicating when the service’s metadata was last cached.

### `DateTime ExpiresAt`
Gets or sets the timestamp indicating when the cached metadata should be considered stale.

## Usage

### Registering and retrieving a service
```csharp
var registry = new ServiceRegistry();

var myService = new GrpcService
{
    ServiceName = "MyService",
    FullName = "com.example.MyService",
    Endpoint = "localhost:50051",
    Port = 50051,
    MethodCount = 4,
    CachedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddMinutes(10)
};

registry.RegisterService(myService);

var retrieved = registry.GetService("MyService");
if (retrieved != null)
{
    Console.WriteLine($"Service {retrieved.FullName} found at {retrieved.Endpoint}");
}
```

### Listing services and updating health status
```csharp
var registry = new ServiceRegistry();
// Assume services have been registered elsewhere...

foreach (var svc in registry.ListServicesByPackage("com.example"))
{
    Console.WriteLine($"{svc.ServiceName} - {svc.Endpoint}");
}

if (registry.ServiceExists("MyService"))
{
    registry.UpdateServiceStatus("MyService", ServiceHealthStatus.Healthy);
    var status = registry.GetHealthStatus("MyService");
    Console.WriteLine($"MyService status: {status}");
}
```

## Notes

- The registry does **not** provide built‑in synchronization; concurrent access from multiple threads should be guarded by the caller (e.g., using a `lock` or a concurrent collection) to avoid race conditions.
- Registering a service with a name that already exists will throw an `InvalidOperationException`. To replace a service, call `UnregisterService` first.
- The `GetService` overloads treat the supplied strings as case‑sensitive matches; differing only in case will result in a `null` return.
- `ListServices` and `ListServicesByPackage` return snapshots; modifications to the registry after the call do not affect the enumerated values.
- Properties such as `ServiceName`, `FullName`, `Endpoint`, `Port`, `MethodCount`, `CachedAt`, and `ExpiresAt` are intended for scenarios where the registry instance represents a single service (e.g., a per‑request context). When the registry holds multiple services, these properties reflect the last value set and may be meaningless; callers should rely on the service‑specific methods instead.
