# ReflectionServiceTestsExtensions

A static utility class designed to simplify the creation, registration, and inspection of gRPC services during unit and integration testing of the `grpc-web-bridge` reflection infrastructure. It provides factory methods for test services, access to the underlying service registry, and query methods to verify service registration state.

## API

### `CreateAndRegisterTestService`
```csharp
public static GrpcService CreateAndRegisterTestService()
```
Creates a new `GrpcService` instance configured for testing and automatically registers it in the global service registry.  
**Returns:** A `GrpcService` that is already registered.  
**Throws:** `InvalidOperationException` if the service registry has not been initialized or if registration fails due to a duplicate service name.

### `GetServiceRegistry`
```csharp
public static ServiceRegistry GetServiceRegistry()
```
Returns the current `ServiceRegistry` instance used by the test extensions.  
**Returns:** The active `ServiceRegistry`.  
**Throws:** `InvalidOperationException` if the registry has not been created yet (e.g., before any call to `CreateAndRegisterTestService`).

### `GetReflectionService`
```csharp
public static ReflectionService GetReflectionService()
```
Retrieves the `ReflectionService` instance that exposes the registered test services for gRPC reflection queries.  
**Returns:** A `ReflectionService` backed by the current service registry.  
**Throws:** `InvalidOperationException` if the reflection service has not been initialized.

### `CreateTestServiceWithMethods`
```csharp
public static GrpcService CreateTestServiceWithMethods(params string[] methodNames)
```
Creates a `GrpcService` that exposes the specified method names. The service is not automatically registered; registration must be performed separately if needed.  
**Parameters:**  
- `methodNames`: One or more method names to include in the service definition.  
**Returns:** A new `GrpcService` instance with the given methods.  
**Throws:** `ArgumentNullException` if `methodNames` is `null`; `ArgumentException` if any method name is empty or whitespace.

### `IsServiceRegistered`
```csharp
public static bool IsServiceRegistered(string serviceName)
```
Checks whether a service with the given name is currently registered in the test service registry.  
**Parameters:**  
- `serviceName`: The fully qualified name of the service (e.g., `"mypackage.MyService"`).  
**Returns:** `true` if the service is registered; otherwise `false`.  
**Throws:** `ArgumentNullException` if `serviceName` is `null`; `ArgumentException` if `serviceName` is empty or whitespace.

### `GetAllServices`
```csharp
public static IEnumerable<GrpcService> GetAllServices()
```
Enumerates all `GrpcService` instances currently registered in the test service registry.  
**Returns:** A collection of registered services. Returns an empty sequence if no services are registered.  
**Throws:** `InvalidOperationException` if the service registry has not been initialized.

## Usage

### Example 1: Basic test setup with automatic registration
```csharp
using GrpcWebBridge.Tests.Extensions;

public class ReflectionServiceTests
{
    [Fact]
    public void ReflectionService_ShouldListRegisteredService()
    {
        // Arrange
        var service = ReflectionServiceTestsExtensions.CreateAndRegisterTestService();
        var reflectionService = ReflectionServiceTestsExtensions.GetReflectionService();

        // Act
        var services = reflectionService.ListServices();

        // Assert
        Assert.Contains(service.Name, services);
    }
}
```

### Example 2: Creating a custom service and verifying registration
```csharp
using GrpcWebBridge.Tests.Extensions;

public class CustomServiceTests
{
    [Fact]
    public void CustomService_CanBeRegisteredAndQueried()
    {
        // Arrange
        var customService = ReflectionServiceTestsExtensions.CreateTestServiceWithMethods("SayHello", "SayGoodbye");
        var registry = ReflectionServiceTestsExtensions.GetServiceRegistry();
        registry.Register(customService);

        // Act
        bool isRegistered = ReflectionServiceTestsExtensions.IsServiceRegistered(customService.Name);
        var allServices = ReflectionServiceTestsExtensions.GetAllServices();

        // Assert
        Assert.True(isRegistered);
        Assert.Contains(customService, allServices);
    }
}
```

## Notes

- All methods that depend on the service registry (`GetServiceRegistry`, `GetReflectionService`, `IsServiceRegistered`, `GetAllServices`) will throw `InvalidOperationException` if called before the registry is initialized. The registry is automatically initialized on the first call to `CreateAndRegisterTestService`. To ensure a clean state between tests, call `CreateAndRegisterTestService` (or manually initialize the registry) at the start of each test method.
- `CreateTestServiceWithMethods` does not register the returned service. Use `GetServiceRegistry().Register(service)` to add it to the registry before querying with `IsServiceRegistered` or `GetAllServices`.
- Thread safety: The class is not guaranteed to be thread-safe. Concurrent calls from multiple threads may lead to inconsistent state. In test environments, ensure that each test runs sequentially or use synchronization mechanisms if parallel test execution is required.
- The `methodNames` parameter in `CreateTestServiceWithMethods` must not contain `null` or empty strings; otherwise an `ArgumentException` is thrown.
- The service name used for registration and lookup is case-sensitive.
