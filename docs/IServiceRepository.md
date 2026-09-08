# IServiceRepository

Repository interface for gRPC service data access.

## Methods

### AddAsync
```csharp
Task<bool> AddAsync(GrpcService service, CancellationToken cancellationToken = default)
```
Adds a new service to storage.

**Parameters:**
- `service`: The service to add.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- `true` if the service was added successfully; `false` if a service with the same ID already exists.

### GetByIdAsync
```csharp
Task<GrpcService?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
```
Retrieves a service by ID.

**Parameters:**
- `id`: The unique identifier of the service.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- The service with the specified ID, or `null` if not found.

### GetByFullNameAsync
```csharp
Task<GrpcService?> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
```
Retrieves a service by full name.

**Parameters:**
- `fullName`: The full name of the service (package.name).
- `cancellationToken`: Optional cancellation token.

**Returns:**
- The service with the specified full name, or `null` if not found.

### GetAllAsync
```csharp
Task<IEnumerable<GrpcService>> GetAllAsync(CancellationToken cancellationToken = default)
```
Retrieves all services.

**Parameters:**
- `cancellationToken`: Optional cancellation token.

**Returns:**
- An enumerable collection of all services.

### GetByPackageAsync
```csharp
Task<IEnumerable<GrpcService>> GetByPackageAsync(string packageName, CancellationToken cancellationToken = default)
```
Retrieves services by package name.

**Parameters:**
- `packageName`: The package name to filter by.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- An enumerable collection of services matching the specified package name.

### UpdateAsync
```csharp
Task<bool> UpdateAsync(GrpcService service, CancellationToken cancellationToken = default)
```
Updates an existing service.

**Parameters:**
- `service`: The service with updated properties.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- `true` if the service was updated successfully; `false` if the service does not exist.

### DeleteAsync
```csharp
Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
```
Deletes a service by ID.

**Parameters:**
- `id`: The unique identifier of the service to delete.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- `true` if the service was deleted successfully; `false` if the service does not exist.

### ExistsAsync
```csharp
Task<bool> ExistsAsync(string fullName, CancellationToken cancellationToken = default)
```
Checks if a service exists by full name.

**Parameters:**
- `fullName`: The full name of the service to check.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- `true` if a service with the specified full name exists; otherwise, `false`.

### CountAsync
```csharp
Task<int> CountAsync(CancellationToken cancellationToken = default)
```
Gets total service count.

**Parameters:**
- `cancellationToken`: Optional cancellation token.

**Returns:**
- The total number of services.

### SearchAsync
```csharp
Task<IEnumerable<GrpcService>> SearchAsync(
    Func<GrpcService, bool> predicate,
    CancellationToken cancellationToken = default)
```
Searches services by criteria.

**Parameters:**
- `predicate`: A function to test each service for a condition.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- An enumerable collection of services that match the predicate.

### GetPagedAsync
```csharp
Task<(IEnumerable<GrpcService> Items, int Total)> GetPagedAsync(
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken = default)
```
Gets services with pagination.

**Parameters:**
- `pageNumber`: The page number (1-based index).
- `pageSize`: The number of items per page.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- A tuple containing the services for the specified page and the total count.

### AddRequestAsync
```csharp
Task<bool> AddRequestAsync(GrpcRequest request, CancellationToken cancellationToken = default)
```
Stores a request record.

**Parameters:**
- `request`: The request record to store.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- `true` if the request was stored successfully; otherwise, `false`.

### GetRequestAsync
```csharp
Task<GrpcRequest?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default)
```
Retrieves a request record by ID.

**Parameters:**
- `requestId`: The unique identifier of the request.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- The request record with the specified ID, or `null` if not found.

### AddResponseAsync
```csharp
Task<bool> AddResponseAsync(GrpcResponse response, CancellationToken cancellationToken = default)
```
Stores a response record.

**Parameters:**
- `response`: The response record to store.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- `true` if the response was stored successfully; otherwise, `false`.

### GetResponseAsync
```csharp
Task<GrpcResponse?> GetResponseAsync(string responseId, CancellationToken cancellationToken = default)
```
Retrieves a response record by ID.

**Parameters:**
- `responseId`: The unique identifier of the response.
- `cancellationToken`: Optional cancellation token.

**Returns:**
- The response record with the specified ID, or `null` if not found.

## Implementation
See the in-memory implementation: [ServiceRepository](ServiceRepository.md)

## Usage Example
The following example demonstrates basic usage of the repository, consistent with the tests in `ServiceRepositoryTests.cs`:

```csharp
using GrpcWebBridge.Data;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;

// Arrange
var logger = Substitute.For<ILogger<ServiceRepository>>();
var repository = new ServiceRepository(logger);

// Create a service
var service = new GrpcService("TestService", "test.package", "localhost", 5000);
service.AddMethod(new GrpcMethod("DummyMethod", "test.package.TestService.DummyMethod", MethodType.Unary, "InputType", "OutputType"));

// Add the service
var addResult = await repository.AddAsync(service);
Console.WriteLine($"Add result: {addResult}"); // Should be true

// Retrieve the service by ID
var retrievedService = await repository.GetByIdAsync(service.Id);
Console.WriteLine($"Retrieved service: {retrievedService?.FullName}"); // Should be "test.package.TestService"

// Check if service exists by full name
var exists = await repository.ExistsAsync("test.package.TestService");
Console.WriteLine($"Exists: {exists}"); // Should be true

// Get all services
var allServices = await repository.GetAllAsync();
Console.WriteLine($"Total services: {allServices.Count()}"); // Should be 1

// Update the service
service.PackageName = "updated.package";
var updateResult = await repository.UpdateAsync(service);
Console.WriteLine($"Update result: {updateResult}"); // Should be true

// Retrieve updated service
var updatedService = await repository.GetByIdAsync(service.Id);
Console.WriteLine($"Updated package: {updatedService?.PackageName}"); // Should be "updated.package"

// Delete the service
var deleteResult = await repository.DeleteAsync(service.Id);
Console.WriteLine($"Delete result: {deleteResult}"); // Should be true

// Verify deletion
var deletedService = await repository.GetByIdAsync(service.Id);
Console.WriteLine($"Deleted service: {deletedService}"); // Should be null
```