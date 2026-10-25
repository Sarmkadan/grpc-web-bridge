# ServiceHealthCheck

`ServiceHealthCheck` is a sealed `IHealthCheck` in `GrpcWebBridge.Endpoints` (`src/GrpcWebBridge/Endpoints/ServiceHealthCheck.cs`). It summarizes the status of every service in `ServiceRegistry` into one `HealthCheckResult`: `Healthy`, `Degraded`, or `Unhealthy`.

It does not probe the backend services itself. It reads the `ServiceStatus` already stored on each `GrpcService` in the registry. The probing is done by `HealthCheckWorker` (see [How services are probed](#how-services-are-probed)).

For the other health checks, the shared JSON writer, and the HTTP routes, see [HealthChecks](HealthChecks.md) and [HealthEndpoints](HealthEndpoints.md).

## Constructor

```csharp
public ServiceHealthCheck(
    ServiceRegistry serviceRegistry,
    ILogger<ServiceHealthCheck> logger)
```

- Both arguments must not be `null`; otherwise `ArgumentNullException` is thrown.
- The class has no options object and reads no configuration.

## How services are probed

`ServiceHealthCheck` makes no network call. On each `CheckHealthAsync` call it takes a snapshot with `ServiceRegistry.ListServices().ToList()` and reads each service's `Status`.

The status values are written by `HealthCheckWorker` (`src/GrpcWebBridge/BackgroundWorkers/HealthCheckWorker.cs`), which runs in the background:

| Setting | Default | Source |
| --- | --- | --- |
| Interval between check cycles | 30 seconds | `HealthCheckOptions.CheckIntervalSeconds` |
| Timeout per service | 5000 ms | `HealthCheckOptions.CheckTimeoutMs` |

For each service, the worker sends `GET http://{Endpoint}:{Port}/health` with a `new HttpClient()`, using the timeout above:

- A 2xx response means the service is reachable. If its status is not `Serving`, the worker sets it to `Serving` and logs a recovery message.
- Any other response, a timeout, or a connection error means it is not reachable. If its status is `Serving`, the worker sets it to `NotServing`, logs a warning, and publishes a `ServiceHealthChangedEvent` with `IsHealthy = false`. If the status is already not `Serving`, nothing changes.
- An unexpected exception during the probe also sets the status to `NotServing`.

`GrpcService.Status` defaults to `Serving` when a service is created. In the code reviewed, `HealthCheckWorker` writes only `Serving` and `NotServing`. `ServiceRegistry.UpdateServiceStatus` can store any `ServiceStatus`, so `Unknown` and `Unknown_ServiceNotServing` appear only if something else calls it.

The probe result is therefore only as fresh as the last worker cycle. A service that fails between cycles is reported as healthy until the next check.

## Status rules

The evaluation is done in this order:

| Condition | Result | Message |
| --- | --- | --- |
| The registry has no services | `Healthy` | `No services registered` |
| Every service has status `Serving` | `Healthy` | `<healthy>/<total> services healthy` |
| Some services are not `Serving`, and `servingCount > 0` | `Degraded` | `Degraded: <servingCount>/<total> services healthy, <notServingCount> not serving` |
| Some services are not `Serving`, and `servingCount <= 0` | `Unhealthy` | `Unhealthy: <notServingCount>/<total> services not serving` |
| An exception is thrown | `Unhealthy` | `Service health check failed`, with the exception attached |

The counts are defined in the code as follows:

- `unhealthyServices` is every service whose status is not `Serving`. Its size is `notServingCount`.
- `degradedCount` is the number of services whose status is `Unknown`.
- `servingCount = total - degradedCount - notServingCount`.

Because `Unknown` services are already in `notServingCount`, they are subtracted twice. This means `servingCount` equals the number of `Serving` services minus the number of `Unknown` services, not the number of `Serving` services. The result is that the healthy count can be understated, and the `Degraded`/`Unhealthy` split can be wrong when `Unknown` statuses are present:

| Statuses in registry | Computed `servingCount` | Result |
| --- | --- | --- |
| 3 `Serving`, 1 `NotServing` | 3 | `Degraded` (correct) |
| 3 `Serving`, 1 `Unknown` | 2 | `Degraded`, reports `2/4` healthy |
| 1 `Serving`, 1 `Unknown` | 0 | `Unhealthy`, although one service is serving |
| 2 `Serving`, 2 `Unknown` | 0 | `Unhealthy`, although two services are serving |

`HealthCheckWorker` does not write `Unknown`, so this only affects registries where some other code sets that status.

## Data keys

Data is attached only to `Degraded` and `Unhealthy` results. `Healthy` and exception results have no data.

### Degraded

| Key | Type | Value |
| --- | --- | --- |
| `healthy_services` | `int` | `servingCount` (see the calculation note above) |
| `unhealthy_services` | `int` | `notServingCount`, which includes `Unknown` services |
| `degraded_services` | `int` | Number of services with status `Unknown` |
| `unhealthy_service_names` | `List<string>` | `FullName` of each service whose status is not `Serving` |

### Unhealthy

| Key | Type | Value |
| --- | --- | --- |
| `unhealthy_services` | `int` | `notServingCount` |
| `total_services` | `int` | Total number of registered services |
| `unhealthy_service_names` | `List<string>` | `FullName` of each service whose status is not `Serving` |

The Unhealthy result has no `healthy_services` or `degraded_services` key.

## Logging

| Situation | Level |
| --- | --- |
| No services registered | Information |
| All services healthy | Debug |
| Degraded | Warning, with the names of the non-serving services |
| Unhealthy | Error, with the names of the non-serving services |
| Exception | Error, with the exception |

## Registration

The class is not registered anywhere in `src/` in the code reviewed. It must be added to the ASP.NET Core health-check builder by the host, for example:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<ServiceHealthCheck>("services");
```

`ServiceRegistry` and `ILogger<ServiceHealthCheck>` must be resolvable from the service provider.

## See also

- [HealthChecks](HealthChecks.md) - all three health checks and the shared JSON writer
- [HealthEndpoints](HealthEndpoints.md) - the HTTP routes that expose health results
- [HealthCheckWorker](HealthCheckWorker.md) - the background probe that sets service status
