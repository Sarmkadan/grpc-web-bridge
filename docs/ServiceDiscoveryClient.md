# ServiceDiscoveryClient

The `ServiceDiscoveryClient` provides a managed interface for service registration, discovery, and health monitoring within the `grpc-web-bridge` infrastructure. It allows services to announce their availability, discover other service instances, and maintain registration status through automated heartbeats and periodic cache refreshes, facilitating resilient communication in distributed environments.

## API

### Properties

*   **`Id` (string)**: The unique identifier for this service instance.
*   **`Name` (string)**: The service name under which the instance is registered.
*   **`Host` (string)**: The hostname or IP address of the service instance.
*   **`Port` (int)**: The port number on which the service instance is listening.
*   **`Status` (string)**: The current operational status of the service (e.g., "Starting", "Healthy", "Degraded", "Unhealthy").
*   **`Metadata` (Dictionary<string, string>?)**: Optional key-value pairs containing additional configuration or identification data for the instance.
*   **`RegisteredAt` (DateTime)**: The timestamp indicating when the instance was first registered.
*   **`LastHeartbeat` (DateTime?)**: The timestamp of the most recent successful heartbeat sent to the discovery server.

### Methods

*   **`ServiceDiscoveryClient(...)`**: Constructor for initializing a new instance of the client.
*   **`async Task<bool> RegisterServiceAsync()`**: Registers this service instance with the discovery server. Returns `true` on success, `false` otherwise.
*   **`async Task<bool> DeregisterServiceAsync()`**: Removes the registration of this service instance. Returns `true` on success, `false` otherwise.
*   **`async Task<List<ServiceInstance>> DiscoverServicesAsync()`**: Queries the discovery server for all available instances. Returns a `List<ServiceInstance>`.
*   **`async Task<ServiceInstance?> GetHealthyInstanceAsync()`**: Retrieves a single instance marked as healthy. Returns the `ServiceInstance`, or `null` if no healthy instances are currently available.
*   **`async Task<bool> SendHeartbeatAsync()`**: Manually sends a heartbeat to the server to prevent registration expiration. Returns `true` on success.
*   **`void StartAutoRefresh()`**: Initiates a background task to periodically refresh the internal cache of service instances.
*   **`void StopAutoRefresh()`**: Halts the background cache refresh task.
*   **`List<ServiceInstance> GetCachedServices()`**: Returns the current list of service instances held in local cache without performing a network query.
*   **`void ClearCache()`**: Removes all entries from the local instance cache.
*   **`object GetStatistics()`**: Returns diagnostic information and metrics regarding the client's operation, such as success rates and cache state.
*   **`void Dispose()`**: Releases all resources held by the client, including network connections and background refresh tasks.

## Usage

### Example 1: Registering and discovering services
```csharp
var client = new ServiceDiscoveryClient(options);
// Register the current service
bool registered = await client.RegisterServiceAsync();

if (registered)
{
    // Discover other instances
    var instances = await client.DiscoverServicesAsync();
    var healthyInstance = await client.GetHealthyInstanceAsync();
    
    if (healthyInstance != null)
    {
        // Proceed with service interaction
    }
}
```

### Example 2: Configuring automatic heartbeat and cache management
```csharp
var client = new ServiceDiscoveryClient(options);
client.StartAutoRefresh();

// Register the service initially
await client.RegisterServiceAsync();

// The heartbeat should be managed either manually or via internal hooks
// Depending on implementation, manual heartbeat might be used for fine-grained control
await client.SendHeartbeatAsync();

// Ensure resources are cleaned up on application shutdown
client.Dispose();
```

## Notes

### Thread Safety
The `ServiceDiscoveryClient` is designed to be thread-safe for most operations. Access to the internal instance cache is synchronized, allowing `GetCachedServices` to be called safely while background refresh tasks are active. However, `Dispose()` should be called only once and after all other operations have concluded to prevent accessing disposed network resources.

### Error Handling
Asynchronous methods (`RegisterServiceAsync`, `DiscoverServicesAsync`, etc.) are designed to handle transient network failures internally where possible. If a method returns `bool`, a `false` result generally indicates a failure in communication with the discovery server or an invalid state. Consumers should be prepared to handle exceptions if the underlying transport layer fails catastrophically.

### Cache Staleness
`GetCachedServices` returns data from the local cache, which may be slightly stale depending on the interval set for `StartAutoRefresh`. For operations requiring immediate consistency, `DiscoverServicesAsync` should be used instead, as it performs a direct query to the discovery server.
