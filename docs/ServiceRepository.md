# ServiceRepository
The `ServiceRepository` provides an asynchronous storage layer for gRPC service definitions and their associated request/response message types within the `grpc-web-bridge` project. It enables CRUD operations, querying, and paging of `GrpcService`, `GrpcRequest`, and `GrpcResponse` entities, abstracting the underlying persistence mechanism.

## API
### AddAsync
```csharp
public async Task<bool> AddAsync(GrpcService service);
```
Adds a new `GrpcService` to the repository. Returns `true` if the service was inserted; returns `false` if a service with the same identifier already exists. Throws `ArgumentNullException` if `service` is `null`. May throw persistence‑specific exceptions (e.g., `DbUpdateException`) on storage failures.

### GetByIdAsync
```csharp
public async Task<GrpcService?> GetByIdAsync(Guid id);
```
Retrieves the `GrpcService` with the specified identifier. Returns the service instance or `null` if no match is found. Throws `ArgumentException` if `id` is `Guid.Empty`.

### GetByFullNameAsync
```csharp
public async Task<GrpcService?> GetByFullNameAsync(string fullName);
```
Retrieves the `GrpcService` whose fully‑qualified name matches `fullName`. Comparison is case‑sensitive. Returns `null` when no service matches. Throws `ArgumentNullException` if `fullName` is `null` or empty.

### GetAllAsync
```csharp
public async Task<IEnumerable<GrpcService>> GetAllAsync();
```
Returns all stored `GrpcService` instances. Returns an empty enumerable when the repository contains no services. Does not throw under normal conditions; propagates any storage‑layer exceptions.

### GetByPackageAsync
```csharp
public async Task<IEnumerable<GrpcService>> GetByPackageAsync(string package);
```
Returns all services belonging to the specified Protobuf package. Comparison is case‑sensitive. Returns an empty enumerable if no services match the package. Throws `ArgumentNullException` if `package` is `null` or empty.

### UpdateAsync
```csharp
public async Task<bool> UpdateAsync(GrpcService service);
```
Updates an existing `GrpcService` entry. Returns `true` if the service was found and updated; returns `false` if no service with the given identifier exists. Throws `ArgumentNullException` if `service` is `null`. May throw concurrency exceptions if the underlying store detects a conflict.

### DeleteAsync
```csharp
public async Task<bool> DeleteAsync(Guid id);
```
Removes the service with the specified identifier. Returns `true` if a service was deleted; returns `false` if no such service existed. Throws `ArgumentException` if `id` is `Guid.Empty`.

### ExistsAsync
```csharp
public async Task<bool> ExistsAsync(Guid id);
```
Checks whether a service with the given identifier is present. Returns `true` if found, otherwise `false`. Throws `ArgumentException` if `id` is `Guid.Empty`.

### CountAsync
```csharp
public async Task<int> CountAsync();
```
Returns the total number of `GrpcService` records stored. Does not throw under normal conditions; propagates any storage errors.

### SearchAsync
```csharp
public async Task<IEnumerable<GrpcService>> SearchAsync(string term);
```
Performs a case‑insensitive substring match on service full names and returns matching services. Returns an empty enumerable when no matches are found. Throws `ArgumentNullException` if `term` is `null`.

### GetPagedAsync
```csharp
public async Task<(IEnumerable<GrpcService> Items, int Total)> GetPagedAsync(int pageIndex, int pageSize);
```
Retrieves a page of services ordered by identifier. `pageIndex` is zero‑based. Returns a tuple where `Items` contains the services for the requested page and `Total` is the overall count of services. Throws `ArgumentOutOfRangeException` if `pageIndex` or `pageSize` is less than zero, or if `pageSize` is zero.

### AddRequestAsync
```csharp
public async Task<bool> AddRequestAsync(GrpcRequest request);
```
Associates a `GrpcRequest` message with its parent service. Returns `true` if the request was added; returns `false` if a request with the same identifier already exists. Throws `ArgumentNullException` if `request` is `null`.

### GetRequestAsync
```csharp
public async Task<GrpcRequest?> GetRequestAsync(Guid id);
```
Retrieves the `GrpcRequest` with the specified identifier. Returns the request instance or `null` if not found. Throws `ArgumentException` if `id` is `Guid.Empty`.

### AddResponseAsync
```csharp
public async Task<bool> AddResponseAsync(GrpcResponse response);
```
Associates a `GrpcResponse` message with its parent service. Returns `true` if the response was added; returns `false` if a response with the same identifier already exists. Throws `ArgumentNullException` if `response` is `null`.

### GetResponseAsync
```csharp
public async Task<GrpcResponse?> GetResponseAsync(Guid id);
```
Retrieves the `GrpcResponse` with the specified identifier. Returns the response instance or `null` if not found. Throws `ArgumentException` if `id` is `Guid.Empty`.

## Usage
```csharp
// Example 1: Adding a new service and retrieving it by full name
var repo = new ServiceRepository();
var service = new GrpcService { Id = Guid.NewGuid(), FullName = "my.package.MyService", Package = "my.package" };
await repo.AddAsync(service);

var fetched = await repo.GetByFullNameAsync("my.package.MyService");
if (fetched != null)
{
    Console.WriteLine($"Service {fetched.FullName} loaded.");
}
```

```csharp
// Example 2: Paged search for services containing "Greeter" in their name
var repo = new ServiceRepository();
var (page, total) = await repo.GetPagedAsync(pageIndex: 0, pageSize: 10);
var matches = page.Where(s => s.FullName.Contains("Greeter", StringComparison.OrdinalIgnoreCase));
Console.WriteLineFound {matches.Count()} of {total} services matching 'Greeter'.
```

## Notes
- All methods are asynchronous and intended to be awaited; calling them without `await` may lead to unobserved exceptions.
- The repository does not enforce thread‑safety by default. Concurrent calls from multiple threads are safe only if the underlying storage implementation provides its own concurrency control; otherwise, external synchronization is required.
- Methods that return a boolean indicating success (`AddAsync`, `UpdateAsync`, `DeleteAsync`, `AddRequestAsync`, `AddResponseAsync`) return `false` when the entity already exists or cannot be found, rather than throwing an exception.
- Query methods (`GetByIdAsync`, `GetByFullNameAsync`, `GetRequestAsync`, `GetResponseAsync`) return `null` when no matching entity is found; callers should check for `null` before dereferencing.
- `SearchAsync` performs a case‑insensitive substring search on the service full name; it does not search within request or response message definitions.
- `GetPagedAsync` expects a zero‑based `pageIndex`. Supplying a negative value or a page size of zero will result in an `ArgumentOutOfRangeException`.
- The repository does not automatically cascade deletions; removing a service does not delete its associated requests or responses. Manual cleanup is required if such behavior is desired.
