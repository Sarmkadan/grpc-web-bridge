# Health checks

The `GrpcWebBridge.Endpoints` namespace contains three `IHealthCheck` implementations and a shared JSON response writer. This page describes their current behavior as coded. For the other health routes and response models, see [HealthEndpoints](HealthEndpoints.md).

## `ServiceHealthCheck`

`ServiceHealthCheck` takes a snapshot of `ServiceRegistry.ListServices()` and evaluates each service's `ServiceStatus`.

| Condition | Result | Description |
| --- | --- | --- |
| The registry contains no services | `Healthy` | `No services registered` |
| Every service has status `Serving` | `Healthy` | `<serving>/<total> services healthy` |
| At least one service is considered serving by the calculated counts and at least one status is not `Serving` | `Degraded` | `Degraded: <serving>/<total> services healthy, <not-serving> not serving` |
| Otherwise, when one or more statuses are not `Serving` | `Unhealthy` | `Unhealthy: <not-serving>/<total> services not serving` |
| An exception is thrown | `Unhealthy` | `Service health check failed`; the exception is attached to the result |

The count calculation is worth noting precisely. `unhealthyServices` contains every service whose status is not `Serving`; `degradedCount` contains services whose status is `Unknown`; `notServingCount` is the size of `unhealthyServices`; and `servingCount` is calculated as `total - degradedCount - notServingCount`. Consequently, `Unknown` services are included in both `degradedCount` and `notServingCount` before `servingCount` is calculated. The table above reflects that implemented calculation rather than an inferred policy.

Data is attached only to degraded and unhealthy results:

| Result | Data keys |
| --- | --- |
| `Degraded` | `healthy_services` (`int`), `unhealthy_services` (`int`), `degraded_services` (`int`), `unhealthy_service_names` (`List<string>`) |
| `Unhealthy` because all services fail the status calculation | `unhealthy_services` (`int`), `total_services` (`int`), `unhealthy_service_names` (`List<string>`) |
| Healthy or exception result | None |

## `ServiceRegistryHealthCheck`

`ServiceRegistryHealthCheck` performs an operational probe against the shared registry:

1. It records `RegisteredServiceCount`.
2. It constructs and registers `HealthCheck.Test.HealthCheckTestService`, including one unary `TestMethod`.
3. It reads the service with `GetService` and checks it with `ServiceExists`.
4. It unregisters the test service.
5. It verifies that registration increased the count by exactly one, retrieval returned the expected full name, and existence was reported as true.

Each failed verification returns `Unhealthy` with, respectively, `Failed to register test service`, `Failed to retrieve test service`, or `Failed to confirm service existence`. If all verifications pass, the result is `Healthy` with `Registry operational with <count> services`, where the count is read after cleanup. An exception returns `Unhealthy` with `Service registry health check failed` and attaches the exception.

There is no `Degraded` threshold, and this check attaches no data keys to any result. Cleanup occurs before validation, but it is not in a `finally` block; an exception before or during cleanup can leave the probe service registered.

## `SystemHealthCheck`

`SystemHealthCheck` reads current-process and runtime information, then applies two thresholds:

| Condition | Issue name |
| --- | --- |
| `GC.GetTotalMemory(false)` is greater than `536,870,912` bytes (512 MiB) | `high_memory_usage` |
| Process uptime is less than 1 second | `process_just_started` |

No issues produces `Healthy` with `System is healthy`. One or both issues produces `Degraded` with `System has potential issues: <comma-separated issues>`. An exception produces `Unhealthy` with `System health check failed` and attaches the exception. Working-set size, thread count, processor count, and environment are observed but have no status thresholds.

The implementation builds a local dictionary containing these keys:

- `process_id`
- `process_name`
- `start_time` (round-trip `"o"` format)
- `uptime_seconds`
- `total_memory_bytes`
- `working_set_bytes`
- `thread_count`
- `processor_count`
- `environment` (`DOTNET_ENVIRONMENT`, or `Unknown` when unset)

That dictionary is not passed to `HealthCheckResult`, so the check currently exposes no result data keys.

## `HealthCheckResponseWriter`

`WriteHealthCheckResponse` serializes a `HealthReport` as compact JSON using camel-case property naming:

```json
{
  "status": "healthy",
  "totalDuration": 0.012,
  "timestamp": "2026-09-08T00:00:00Z",
  "checks": {
    "check-registration-name": {
      "status": "healthy",
      "description": "...",
      "duration": 0.004,
      "exception": null
    }
  }
}
```

Statuses are lower-case strings. Durations are seconds, and `timestamp` is generated with `DateTime.UtcNow`. The response content type is `application/json`. The writer does not serialize `HealthReportEntry.Data`, so data keys supplied by `ServiceHealthCheck` do not appear in this JSON response.

The writer sets these HTTP status codes:

| Aggregate health status | HTTP status |
| --- | --- |
| `Healthy` | 200 |
| `Degraded` | 200 |
| `Unhealthy` or any other value | 503 |

## Registration through `HealthEndpoints`

`HealthEndpoints.MapHealthEndpoints(WebApplication)` maps the ASP.NET Core health-check middleware at `/healthz`, `/ready`, and `/health`. All three routes:

- use a predicate that selects every check registered with `IHealthChecksBuilder`;
- use `HealthCheckResponseWriter.WriteHealthCheckResponse`;
- disable response caching;
- are named and included in OpenAPI.

Each route configures `ResultStatusCodes` as 200 for `Healthy` and 503 for both `Degraded` and `Unhealthy`. However, the response writer subsequently assigns the response status itself and maps `Degraded` to 200, so its assignment is the effective status for responses written through this path.

`MapHealthEndpoints` registers the routes, not the three `IHealthCheck` implementations. In the current source, it does not call `AddHealthChecks()` or `AddCheck<ServiceHealthCheck>()`, `AddCheck<ServiceRegistryHealthCheck>()`, or `AddCheck<SystemHealthCheck>()`. The routes will execute these implementations only if application startup or a consuming host adds them to the ASP.NET Core health-check registry. Because every route uses the same unrestricted predicate, any registered implementations run on all three routes.

The additional `/health/detailed` handler belongs to `HealthEndpoints`' custom diagnostic flow; it does not execute these `IHealthCheck` classes or use `HealthCheckResponseWriter`.
