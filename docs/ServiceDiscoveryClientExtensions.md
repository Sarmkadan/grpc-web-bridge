# ServiceDiscoveryClientExtensions

Provides extension methods for service registration, discovery, health checking, and cache management against a service discovery backend. Designed for gRPC-Web bridge scenarios where clients need to locate backend services dynamically.

## API

### RegisterServiceAsync
```csharp
public static async Task<string?> RegisterServiceAsync(
    this IServiceDiscoveryClient client,
    ServiceRegistration registration,
    CancellationToken cancellationToken = default)
```
Registers a service instance with the discovery backend. Returns the assigned instance identifier on success, or `null` if registration was rejected. Throws `ArgumentNullException` if `client` or `registration` is null. Throws `OperationCanceledException` if the operation is cancelled. Throws `ServiceDiscoveryException` on backend communication failures.

### DeregisterServiceAsync
```csharp
public static async Task<bool> DeregisterServiceAsync(
    this IServiceDiscoveryClient client,
    string instanceId,
    CancellationToken cancellationToken = default)
```
Removes a previously registered service instance. Returns `true` if the instance existed and was removed; `false` if the instance was not found. Throws `ArgumentNullException` if `client` or `instanceId` is null. Throws `OperationCanceledException` if cancelled. Throws `ServiceDiscoveryException` on backend errors.

### DiscoverServicesAsync
```csharp
public static async Task<IReadOnlyList<ServiceInstance>> DiscoverServicesAsync(
    this IServiceDiscoveryClient client,
    string serviceName,
    CancellationToken cancellationToken = default)
```
Queries the backend for all instances of a given service. Returns a read-only list (empty if none registered). Throws `ArgumentNullException` if `client` or `serviceName` is null. Throws `OperationCanceledException` if cancelled. Throws `ServiceDiscoveryException` on query failures.

### GetHealthyInstanceAsync
```csharp
public static async Task<ServiceInstance?> GetHealthyInstanceAsync(
    this IServiceDiscoveryClient client,
    string serviceName,
    CancellationToken cancellationToken = default)
```
Returns a single healthy instance for the service, applying backend health checks. Returns `null` if no healthy instances are available. Throws `ArgumentNullException` if `client` or `serviceName` is null. Throws `OperationCanceledException` if cancelled. Throws `ServiceDiscoveryException` on backend errors.

### SendHeartbeatAsync
```csharp
public static async Task<bool> SendHeartbeatAsync(
    this IServiceDiscoveryClient client,
    string instanceId,
    CancellationToken cancellationToken = default)
```
Sends a liveness signal for the specified instance. Returns `true` if the heartbeat was accepted; `false` if the instance is unknown or expired. Throws `ArgumentNullException` if `client` or `instanceId` is null. Throws `OperationCanceledException` if cancelled. Throws `ServiceDiscoveryException` on transport failures.

### StartAutoRefresh
```csharp
public static void StartAutoRefresh(
    this IServiceDiscoveryClient client,
    string serviceName,
    TimeSpan interval,
    Action<IReadOnlyList<ServiceInstance>> onChange,
    CancellationToken cancellationToken = default)
```
Begins periodic background refresh of the service cache at the specified interval. Invokes `onChange` on each successful refresh with the current instance list. The callback executes on a thread-pool thread. Throws `ArgumentNullException` if `client`, `serviceName`, or `onChange` is null. Throws `ArgumentOutOfRangeException` if `interval` is less than 1 second. Throws `InvalidOperationException` if a refresh loop for the same service is already running.

### GetCachedServices
```csharp
public static IReadOnlyList<ServiceInstance> GetCachedServices(
    this IServiceDiscoveryClient client,
    string serviceName)
```
Returns the locally cached instance list for the service without contacting the backend. Returns an empty list if the service has never been refreshed or the cache was cleared. Throws `ArgumentNullException` if `client` or `serviceName` is null.

### GetStatistics
```csharp
public static Dictionary<string, string> GetStatistics(
    this IServiceDiscoveryClient client)
```
Returns a snapshot of client-side metrics including cache hit/miss counts, refresh durations, and error rates. The dictionary keys are stable across versions. Throws `ArgumentNullException` if `client` is null.

## Usage

### Register and maintain a service instance with heartbeats
```csharp
var client = new ConsulDiscoveryClient(new ConsulClient());
var registration = new ServiceRegistration
{
    Name = "order-api",
    Address = "10.0.1.42",
    Port = 5000,
    Tags = new[] { "v1", "zone-a" },
    HealthCheck = new HttpHealthCheck("http://10.0.1.42:5000/health", TimeSpan.FromSeconds(10))
};

string? instanceId = await client.RegisterServiceAsync(registration);
if (instanceId is null)
{
    throw new InvalidOperationException("Registration rejected by discovery backend");
}

var cts = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        bool ok = await client.SendHeartbeatAsync(instanceId, cts.Token);
        if (!ok)
        {
            // Instance expired or deregistered; attempt re-registration
            break;
        }
        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
    }
}, cts.Token);

// On shutdown:
await client.DeregisterServiceAsync(instanceId);
cts.Cancel();
```

### Discover services with auto-refresh and fallback to cache
```csharp
var client = new EtcdDiscoveryClient(new EtcdClient());
var instances = await client.DiscoverServicesAsync("payment-gateway");

if (instances.Count == 0)
{
    // Fallback to stale cache if backend is unreachable
    instances = client.GetCachedServices("payment-gateway");
}

var healthy = await client.GetHealthyInstanceAsync("payment-gateway");
if (healthy is null)
{
    throw new ServiceUnavailableException("No healthy payment-gateway instances");
}

var channel = GrpcChannel.ForAddress($"http://{healthy.Address}:{healthy.Port}");
var paymentClient = new PaymentGateway.PaymentGatewayClient(channel);

// Start background refresh for subsequent calls
client.StartAutoRefresh("payment-gateway", TimeSpan.FromSeconds(30), updated =>
{
    // Log or update local load balancer
    Logger.LogInformation("Payment gateway instances updated: {Count}", updated.Count);
});
```

## Notes

- All async methods accept a `CancellationToken`; callers should propagate cancellation to avoid leaking requests during shutdown.
- `StartAutoRefresh` captures the `CancellationToken` at call time; cancelling it stops the refresh loop but does not clear the cache.
- `GetCachedServices` and `GetStatistics` are synchronous and non-blocking; they read from in-memory state updated by background refresh or explicit discovery calls.
- The cache is per-service-name; multiple callers requesting the same service share the same cached data and refresh loop.
- Thread safety: `GetCachedServices` and `GetStatistics` are safe for concurrent reads. `StartAutoRefresh` must not be called concurrently for the same `serviceName`; doing so throws `InvalidOperationException`.
- Heartbeats are the caller's responsibility; the backend will expire instances that miss their TTL window. A 5-second heartbeat interval with a 15-second TTL is a common starting point.
- `DiscoverServicesAsync` always hits the backend; use `GetCachedServices` for latency-sensitive paths where slight staleness is acceptable.
- Statistics keys include: `cache_hits`, `cache_misses`, `refresh_count`, `refresh_failures`, `last_refresh_duration_ms`. Values are string-formatted for uniform serialization.
