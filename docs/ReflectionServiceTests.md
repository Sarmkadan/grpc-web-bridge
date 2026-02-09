# ReflectionServiceTests

`ReflectionServiceTests` is the test class for the gRPC reflection service implementation within the `grpc-web-bridge` project. It verifies the correctness of service name listing, service descriptor retrieval, method descriptor retrieval, and bulk descriptor collection, covering both successful paths and failure modes such as missing resources and invalid arguments.

## API

### `public ReflectionServiceTests`
The parameterless constructor. Initializes the test fixture, setting up any shared context or dependencies required by the individual test methods.

### `public async Task ListServiceNamesAsync_WhenServicesExist_ReturnsOrderedNames`
Tests that listing service names when one or more services are registered returns a collection of names sorted in lexicographical order.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** Assertion failures if the returned list is not ordered or does not contain the expected names.

### `public async Task ListServiceNamesAsync_WhenNoServicesExist_ReturnsEmptyList`
Tests that listing service names when no services are registered returns an empty collection.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** Assertion failures if the returned list is not empty.

### `public async Task GetServiceDescriptorAsync_ForExistingService_ReturnsDescriptor`
Tests that requesting a service descriptor by its full name for a registered service returns a valid, non-null descriptor.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** Assertion failures if the descriptor is null or does not match the expected service.

### `public async Task GetServiceDescriptorAsync_ForNonExistingService_ReturnsFailure`
Tests that requesting a service descriptor for a full name that does not correspond to any registered service returns a failure result.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** Assertion failures if the result incorrectly indicates success.

### `public async Task GetServiceDescriptorAsync_WithNullOrEmptyFullName_ThrowsArgumentException`
Tests that passing a `null` or empty string as the full service name immediately throws an `ArgumentException`.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** The test expects an `ArgumentException` to be thrown; the test fails if no exception or a different exception type is thrown.

### `public async Task GetMethodDescriptorAsync_ForExistingMethod_ReturnsDescriptor`
Tests that requesting a method descriptor for a valid service and method name combination returns a valid, non-null descriptor.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** Assertion failures if the descriptor is null or does not match the expected method.

### `public async Task GetMethodDescriptorAsync_ForNonExistingMethod_ReturnsFailure`
Tests that requesting a method descriptor for an existing service but a non-existent method name returns a failure result.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** Assertion failures if the result incorrectly indicates success.

### `public async Task GetMethodDescriptorAsync_WithNullOrEmptyServiceFullName_ThrowsArgumentException`
Tests that passing a `null` or empty service full name (with a valid method name) immediately throws an `ArgumentException`.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** The test expects an `ArgumentException` to be thrown; the test fails if no exception or a different exception type is thrown.

### `public async Task GetMethodDescriptorAsync_WithNullOrEmptyMethodName_ThrowsArgumentException`
Tests that passing a `null` or empty method name (with a valid service full name) immediately throws an `ArgumentException`.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** The test expects an `ArgumentException` to be thrown; the test fails if no exception or a different exception type is thrown.

### `public async Task GetAllDescriptorsAsync_WhenServicesExist_ReturnsAllDescriptors`
Tests that retrieving all descriptors when services are registered returns a collection containing every registered service descriptor.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** Assertion failures if the collection is missing any expected descriptor or contains extras.

### `public async Task GetAllDescriptorsAsync_WhenNoServicesExist_ReturnsEmptyList`
Tests that retrieving all descriptors when no services are registered returns an empty collection.  
**Returns:** A completed task representing the asynchronous test operation.  
**Throws:** Assertion failures if the returned collection is not empty.

## Usage

```csharp
// Example 1: Running all ReflectionServiceTests via a test runner
[TestClass]
public class ReflectionServiceTestSuite
{
    private ReflectionServiceTests _tests;

    [TestInitialize]
    public void Setup()
    {
        _tests = new ReflectionServiceTests();
    }

    [TestMethod]
    public async Task VerifyServiceListing_WithRegisteredServices()
    {
        await _tests.ListServiceNamesAsync_WhenServicesExist_ReturnsOrderedNames();
    }

    [TestMethod]
    public async Task VerifyServiceListing_EmptyRegistry()
    {
        await _tests.ListServiceNamesAsync_WhenNoServicesExist_ReturnsEmptyList();
    }
}
```

```csharp
// Example 2: Validating descriptor retrieval and argument validation in a CI pipeline
public static async Task RunReflectionValidation(ReflectionServiceTests tests)
{
    // Positive cases
    await tests.GetServiceDescriptorAsync_ForExistingService_ReturnsDescriptor();
    await tests.GetMethodDescriptorAsync_ForExistingMethod_ReturnsDescriptor();
    await tests.GetAllDescriptorsAsync_WhenServicesExist_ReturnsAllDescriptors();

    // Negative cases
    await tests.GetServiceDescriptorAsync_ForNonExistingService_ReturnsFailure();
    await tests.GetMethodDescriptorAsync_ForNonExistingMethod_ReturnsFailure();

    // Argument validation
    await tests.GetServiceDescriptorAsync_WithNullOrEmptyFullName_ThrowsArgumentException();
    await tests.GetMethodDescriptorAsync_WithNullOrEmptyServiceFullName_ThrowsArgumentException();
    await tests.GetMethodDescriptorAsync_WithNullOrEmptyMethodName_ThrowsArgumentException();
}
```

## Notes

- All test methods are asynchronous and should be awaited to ensure proper execution and exception propagation.
- The tests for `null` or empty string arguments expect `ArgumentException` to be thrown synchronously, before any asynchronous work begins; callers should catch the exception at the point of invocation.
- The ordering test assumes lexicographical sorting of service names; any change to the underlying sorting algorithm will cause this test to fail.
- Tests that verify failure results rely on a specific failure representation (e.g., a status object or null return with an error indicator); the exact type is determined by the service implementation under test.
- These tests are not thread-safe by design—they are intended to be executed sequentially within a test runner. Concurrent execution of multiple tests against a shared service registry may produce unpredictable results unless each test isolates its own state.
