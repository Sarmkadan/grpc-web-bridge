# ServiceRegistryHealthCheck

`ServiceRegistryHealthCheck` is a sealed `IHealthCheck` in `GrpcWebBridge.Endpoints` (`src/GrpcWebBridge/Endpoints/ServiceRegistryHealthCheck.cs`). It verifies that the shared `ServiceRegistry` can register, retrieve, and unregister a service. It returns `Healthy` or `Unhealthy`; it never returns `Degraded`.

It is a self-test of the registry, not a check of the backend services. It makes no network call and does not read the status of the services already registered (see [ServiceHealthCheck](ServiceHealthCheck.md) for that). For the other health checks, the JSON writer, and the HTTP routes, see [HealthChecks](HealthChecks.md) and [HealthEndpoints](HealthEndpoints.md).

## Constructor

```csharp
public ServiceRegistryHealthCheck(
    ServiceRegistry serviceRegistry,
    ILogger<ServiceRegistryHealthCheck> logger)
```

- Both arguments must not be `null`; otherwise `ArgumentNullException` is thrown.
- `ServiceRegistry` is registered as a singleton by `AddGrpcWebBridge` (`src/GrpcWebBridge/Configuration/DependencyInjection.cs`), so the check operates on the same registry that the bridge uses.
- The class has no options object and reads no configuration.

## Registry checks performed

`CheckHealthAsync` runs these steps in order:

1. Reads `RegisteredServiceCount` as `initialCount`.
2. Builds a probe service: `GrpcService("HealthCheckTestService", "HealthCheck.Test", "localhost", 50051)`, with full name `HealthCheck.Test.HealthCheckTestService`. It adds one unary method, `TestMethod`, with full name `HealthCheck.Test.HealthCheckTestService` as passed to the `GrpcMethod` constructor, and input and output message type `string`.
3. Calls `RegisterService` and reads `RegisteredServiceCount` again as `afterRegistrationCount`.
4. Calls `GetService(fullName)` and `ServiceExists(fullName)`.
5. Calls `UnregisterService(fullName)`. This runs before any validation.
6. Validates the three results in this order:
   - `afterRegistrationCount` must equal `initialCount + 1`.
   - The retrieved service's `FullName` must equal the probe's full name.
   - `ServiceExists` must return `true`.

The probe's endpoint (`localhost:50051`) is never contacted.

## Status rules

| Condition | Result | Description |
| --- | --- | --- |
| All six steps complete and all three validations pass | `Healthy` | `Registry operational with <count> services` (count read after cleanup) |
| Count did not increase by exactly one | `Unhealthy` | `Failed to register test service` |
| Retrieved service is `null` or has a different full name | `Unhealthy` | `Failed to retrieve test service` |
| `ServiceExists` returns `false` | `Unhealthy` | `Failed to confirm service existence` |
| Any exception is thrown (see below) | `Unhealthy` | `Service registry health check failed`; the exception is attached to the result |

The first failed validation returns immediately; later validations are not evaluated.

### Exceptions that produce `Unhealthy`

Because the whole method is wrapped in one `try`, any exception produces the generic `Unhealthy` result. The most likely causes are:

- **Probe service already registered.** `RegisterService` throws `ServiceRegistrationException` ("Service already registered") if `HealthCheck.Test.HealthCheckTestService` is present. This happens when two checks run at the same time, or when an earlier run threw before cleanup.
- **Registry at capacity.** `RegisterService` throws ("Service registry is full") when `RegisteredServiceCount` reaches `Constants.ServiceRegistry.MaxCachedServices` (1000). The check reports `Unhealthy` even though the registry itself still works.

In both cases the result description is the generic message, not the specific reason. The reason appears only in the attached exception and in the error log.

## Known edge cases

- **Concurrent registrations can cause a false `Unhealthy`.** The count is read and the probe is inserted under separate lock acquisitions. If another thread adds or removes a service between those reads, the `initialCount + 1` comparison fails and the result is `Failed to register test service`.
- **Cleanup is not in a `finally` block.** If `RegisterService` succeeds and the code then throws before `UnregisterService` runs, the probe service stays registered. The next run then fails with "already registered" until the entry is removed.
- **The probe leaves a metadata cache entry.** `UnregisterService` removes the service from the registry but not from the metadata cache. The `HealthCheck.Test.HealthCheckTestService` entry remains readable through `GetCachedMetadata` until it expires (`Constants.ServiceRegistry.ServiceMetadataCacheDurationMinutes`), and is removed only when it is read after expiry.
- **Each run writes logs.** `ServiceRegistry` logs at `Information` level for the probe's register and unregister on every run. The check logs `Error` for each failed validation or exception, and `Debug` on success.

## Data

No result data is attached. `HealthCheckResult` is created with only a status, a description, and optionally an exception. The probe's values are not exposed in the result.

## Registration

`AddGrpcWebBridge` does not register this check. `MapHealthEndpoints` does not register it either. In the current source, neither calls `AddHealthChecks()` or `AddCheck<...>()`. The host must register it explicitly:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<ServiceRegistryHealthCheck>("service-registry");
```

The registered name (`"service-registry"` above) is the key under which the result appears in the health report. Once registered, the check runs on every route that `MapHealthEndpoints` maps, because those routes use an unrestricted predicate. See [HealthChecks](HealthChecks.md) for details.
