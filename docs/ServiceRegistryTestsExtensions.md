# ServiceRegistryTestsExtensions

This static class contains extension methods that simplify the creation, manipulation, and inspection of a `ServiceRegistry` in unit‑test scenarios. The methods provide convenient ways to build test fixtures, register mock services, query service metadata, and assert expected states without repeating boilerplate code.

## API

### CreateTestRegistry
- **Purpose**: Produces a new `ServiceRegistry` instance configured with default test settings.
- **Parameters**: None.
- **Return value**: A `ServiceRegistry` ready for use in tests.
- **Exceptions**: None under normal operation.

### CreateAndRegisterTestService
- **Purpose**: Generates a mock `GrpcService` and adds it to the supplied registry.
- **Parameters**: 
  - `registry` (`ServiceRegistry`) – the registry to which the service will be added.
  - `serviceDescriptor` (`GrpcServiceDescriptor`) – description of the service to create (name, package, methods, etc.).
- **Return value**: The `GrpcService` instance that was registered.
- **Exceptions**: 
  - `ArgumentNullException` if `registry` or `serviceDescriptor` is `null`.
  - `InvalidOperationException` if a service with the same name and package already exists in the registry.

### CreateAndRegisterTestServices
- **Purpose**: Creates several mock `GrpcService` objects and registers them all at once.
- **Parameters**: 
  - `registry` (`ServiceRegistry`) – target registry.
  - `serviceDescriptors` (`IEnumerable<GrpcServiceDescriptor>`) – collection of service descriptions to create.
- **Return value**: An `IReadOnlyList<GrpcService>` containing the services that were added.
- **Exceptions**: 
  - `ArgumentNullException` if `registry` or `serviceDescriptors` is `null`.
  - `InvalidOperationException` if any descriptor would cause a duplicate registration.

### GetServiceOrThrow
- **Purpose**: Looks up a service by its identifier and throws if it cannot be found.
- **Parameters**: 
  - `registry` (`ServiceRegistry`) – registry to search.
  - `serviceName` (`string`) – name of the service to locate (package‑qualified if required by the registry).
- **Return value**: The matching `GrpcService`.
- **Exceptions**: 
  - `ArgumentNullException` if `registry` or `serviceName` is `null`.
  - `KeyNotFoundException` (or `InvalidOperationException` depending on implementation) when no service with the given name exists.

### ServiceExists
- **Purpose**: Determines whether a service with the specified identifier is present in the registry.
- **Parameters**: 
  - `registry` (`ServiceRegistry`) – registry to inspect.
  - `serviceName` (`string`) – name of the service to check.
- **Return value**: `true` if the service exists; otherwise `false`.
- **Exceptions**: 
  - `ArgumentNullException` if `registry` or `serviceName` is `null`.

### UpdateAndGetServiceStatus
- **Purpose**: Modifies the health status of an existing service and returns the updated service object.
- **Parameters**: 
  - `registry` (`ServiceRegistry`) – registry containing the service.
  - `serviceName` (`string`) – identifier of the service to update.
  - `newStatus` (`ServiceHealthStatus`) – the status to assign.
- **Return value**: The `GrpcService` instance with its status updated.
- **Exceptions**: 
  - `ArgumentNullException` if `registry` or `serviceName` is `null`.
  - `KeyNotFoundException` if the service does not exist.
  - `ArgumentOutOfRangeException` if `newStatus` is not a defined `ServiceHealthStatus` value.

### GetServiceHealthStatus
- **Purpose**: Retrieves the current health status of a service.
- **Parameters**: 
  - `registry` (`ServiceRegistry`) – registry to query.
  - `serviceName` (`string`) – identifier of the service.
- **Return value**: The `ServiceHealthStatus` of the service.
- **Exceptions**: 
  - `ArgumentNullException` if `registry` or `serviceName` is `null`.
  - `KeyNotFoundException` if the service cannot be found.

### ListServicesByPackage
- **Purpose**: Returns all services that belong to a given package.
- **Parameters**: 
  - `registry` (`ServiceRegistry`) – registry to search.
  - `packageName` (`string`) – package identifier.
- **Return value**: An `IReadOnlyList<GrpcService>` containing matching services; empty list if none are found.
- **Exceptions**: 
  - `ArgumentNullException` if `registry` or `packageName` is `null`.
  - (No exception is thrown for a package with no services; an empty list is returned.)

## Usage

```csharp
using GrpcWebBridge.Testing; // namespace containing the extensions
using GrpcWebBridge.Services;

// Arrange: create a test registry and add a couple of services
var registry = ServiceRegistryTestsExtensions.CreateTestRegistry();

var helloDescriptor = new GrpcServiceDescriptor(
    serviceName: "Greeter",
    package: "example.helloworld",
    methods: new[] { new GrpcMethodDescriptor("SayHello", typeof(HelloRequest), typeof(HelloReply)) });

var byeDescriptor = new GrpcServiceDescriptor(
    serviceName: "Farewell",
    package: "example.helloworld",
    methods: Array.Empty<GrpcMethodDescriptor>());

var helloService = ServiceRegistryTestsExtensions.CreateAndRegisterTestService(registry, helloDescriptor);
var byeService   = ServiceRegistryTestsExtensions.CreateAndRegisterTestService(registry, byeDescriptor);

// Act: retrieve a service and verify its health status
var retrieved = ServiceRegistryTestsExtensions.GetServiceOrThrow(registry, "Greeter");
var status    = ServiceRegistryTestsExtensions.GetServiceHealthStatus(registry, "Greeter");

// Assert
Assert.Same(helloService, retrieved);
Assert.Equal(ServiceHealthStatus.Healthy, status); // default status
```

```csharp
using GrpcWebBridge.Testing;
using GrpcWebBridge.Services;

// Arrange: registry with a single service
var registry = ServiceRegistryTestsExtensions.CreateTestRegistry();
var descriptor = new GrpcServiceDescriptor(
    serviceName: "Stats",
    package: "example.monitoring",
    methods: new[] { new GrpcMethodDescriptor("GetMetrics", typeof(Empty), typeof(MetricsReply)) });
var statsService = ServiceRegistryTestsExtensions.CreateAndRegisterTestService(registry, descriptor);

// Act: update the service to a degraded state
var updated = ServiceRegistryTestsExtensions.UpdateAndGetServiceStatus(
    registry,
    "Stats",
    ServiceHealthStatus.Degraded);

// Assert: the service exists and reflects the new status
Assert.True(ServiceRegistryTestsExtensions.ServiceExists(registry, "Stats"));
Assert.Same(statsService, updated);
Assert.Equal(ServiceHealthStatus.Degraded, updated.Status);

// Act: list all services in the "example.monitoring" package
var servicesInPackage = ServiceRegistryTestsExtensions.ListServicesByPackage(registry, "example.monitoring");
Assert.Single(servicesInPackage);
Assert.Same(statsService, servicesInPackage[0]);
```

## Notes

- All extension methods are **pure** with respect to their own logic; they do not retain internal state. Thread safety therefore depends entirely on the mutability of the supplied `ServiceRegistry` instance. If a single `ServiceRegistry` is accessed concurrently from multiple threads, callers must synchronize access externally (e.g., using locks) because the underlying registry is not guaranteed to be thread‑safe.
- Methods that throw on missing services (`GetServiceOrThrow`, `UpdateAndGetServiceStatus`, `GetServiceHealthStatus`) rely on the registry’s internal lookup mechanism. Passing `null` for the registry or any identifier argument results in an `ArgumentNullException`.
- Duplicate service registration is considered an error; the `CreateAndRegisterTestService` and `CreateAndRegisterTestServices` methods will throw `InvalidOperationException` when attempting to add a service that conflicts with an existing entry (same name and package).
- `ListServicesByPackage` returns an empty list rather than throwing when the package name is valid but no services are present; only a `null` package name triggers an exception.
- The health‑status related methods (`UpdateAndGetServiceStatus`, `GetServiceHealthStatus`) accept any value of the `ServiceHealthStatus` enum; supplying an undefined enum value triggers `ArgumentOutOfRangeException`. 
- These helpers are intended exclusively for test code. Production code should not depend on them, as they may be altered or removed without affecting the library’s public contract.
