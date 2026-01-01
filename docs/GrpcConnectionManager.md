# GrpcConnectionManager

`GrpcConnectionManager` manages the lifecycle of gRPC channels for a bridge service, providing creation, retrieval, closure, and telemetry for connections to backend gRPC endpoints. It tracks per-channel usage statistics and exposes connection health-checking capabilities.

## API

### Public Members

#### `public GrpcConnectionManager`
Constructor. Initializes a new instance of the connection manager. No parameters are exposed publicly.

#### `public GrpcChannel GetOrCreateChannel`
Creates a new gRPC channel for the configured address if one does not already exist, or returns the existing active channel. Subsequent calls with the same effective target return the cached channel.  
**Returns:** `GrpcChannel` — the active channel instance.  
**Throws:** May throw if the underlying channel factory fails to create a channel (e.g., invalid address).

#### `public GrpcChannel? GetChannel`
Retrieves the currently cached channel without creating a new one.  
**Returns:** `GrpcChannel?` — the active channel, or `null` if no channel has been created or the existing channel has been closed.

#### `public async Task CloseChannelAsync`
Asynchronously closes the currently cached channel, if one exists. Disposes the underlying channel resources and removes it from the manager’s cache.  
**Returns:** `Task` — completes when the channel is fully disposed.  
**Throws:** May throw if disposal of the underlying channel fails.

#### `public async Task CloseAllChannelsAsync`
Asynchronously closes all channels currently managed by this instance. Iterates over every cached channel and disposes each.  
**Returns:** `Task` — completes when all channels have been disposed.

#### `public ConnectionMetrics? GetMetrics`
Returns a snapshot of connection metrics for the currently cached channel, or `null` if no channel exists.  
**Returns:** `ConnectionMetrics?` — a metrics object containing usage statistics, or `null`.

#### `public async Task<bool> TestConnectionAsync`
Performs a lightweight connectivity check against the backend using the active channel. Typically sends a minimal request or checks channel state to determine reachability.  
**Returns:** `Task<bool>` — `true` if the connection is healthy; `false` otherwise.  
**Throws:** May throw if the underlying call fails unexpectedly (e.g., network error during the test).

#### `public async ValueTask DisposeAsync`
Asynchronously disposes the `GrpcConnectionManager` instance, closing all channels and releasing all resources. Implements `IAsyncDisposable`.  
**Returns:** `ValueTask` — completes when disposal is finished.

#### `public string? ServiceName`
Gets the logical service name associated with this connection manager. May be `null` if not set.

#### `public string? Address`
Gets the target address (URI or host:port) used for channel creation. May be `null` if not configured.

#### `public DateTime CreatedAt`
Gets the UTC timestamp when this `GrpcConnectionManager` instance was created.

#### `public DateTime LastUsedAt`
Gets the UTC timestamp of the most recent channel usage (creation, retrieval, or request).

#### `public int RequestCount`
Gets the total number of requests sent through channels managed by this instance.

#### `public long BytesSent`
Gets the total number of bytes transmitted over channels managed by this instance.

#### `public long BytesReceived`
Gets the total number of bytes received over channels managed by this instance.

#### `public TimeSpan GetConnectionDuration`
Returns the elapsed time since the active channel was created, or `TimeSpan.Zero` if no channel exists.  
**Returns:** `TimeSpan` — the duration the current channel has been alive.

## Usage

### Example 1: Basic channel lifecycle with health check

```csharp
var manager = new GrpcConnectionManager
{
    ServiceName = "orders.OrderService",
    Address = "https://orders-backend:5001"
};

// Obtain a channel and verify connectivity
GrpcChannel channel = manager.GetOrCreateChannel();
bool isHealthy = await manager.TestConnectionAsync();

if (!isHealthy)
{
    await manager.CloseChannelAsync();
    // Re-create after a delay or fallback logic
    channel = manager.GetOrCreateChannel();
}

// Use the channel for gRPC calls...
// Metrics are updated automatically
ConnectionMetrics? metrics = manager.GetMetrics();
Console.WriteLine($"Requests: {metrics?.RequestCount}");
```

### Example 2: Periodic cleanup and monitoring

```csharp
var manager = new GrpcConnectionManager
{
    Address = "https://inventory-backend:5001"
};

// Use the channel throughout application lifetime
var channel = manager.GetOrCreateChannel();

// Periodically log connection duration and usage
TimeSpan duration = manager.GetConnectionDuration();
Console.WriteLine($"Channel alive for {duration.TotalMinutes:F1} min, " +
                  $"sent {manager.BytesSent} bytes, received {manager.BytesReceived} bytes");

// On shutdown, dispose all resources
await manager.DisposeAsync();
```

## Notes

- **Thread safety:** `GetOrCreateChannel`, `GetChannel`, `CloseChannelAsync`, and `CloseAllChannelsAsync` are safe to call concurrently. Channel creation is guarded to prevent duplicate channels.
- **Null metrics:** `GetMetrics` returns `null` when no channel has been created or after the channel has been closed. Callers must null-check before accessing metric fields.
- **`TestConnectionAsync` behavior:** The method relies on the active channel; if no channel exists, it may return `false` or throw depending on implementation. Ensure a channel is obtained via `GetOrCreateChannel` before testing.
- **Disposal:** After `DisposeAsync` is called, all public members that depend on channels (`GetOrCreateChannel`, `GetChannel`, `TestConnectionAsync`, `GetMetrics`, `GetConnectionDuration`) will either return `null`, throw `ObjectDisposedException`, or behave as if no channel exists. Do not use the instance after disposal.
- **`LastUsedAt` updates:** This timestamp is updated on channel retrieval and request completion. It does not reflect idle time after the last operation.
- **`GetConnectionDuration`:** Returns `TimeSpan.Zero` when no channel is cached. The duration is measured from channel creation, not from manager creation (`CreatedAt`).
