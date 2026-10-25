# SystemHealthCheck

`SystemHealthCheck` is a sealed `IHealthCheck` in `GrpcWebBridge.Endpoints` (`src/GrpcWebBridge/Endpoints/SystemHealthCheck.cs`). It inspects the current process and the .NET runtime, applies two status thresholds, and reports the outcome as a `HealthCheckResult`. It does not call any network or backend service, so it is a local, inexpensive probe.

For the other health checks and the shared JSON writer, see [HealthChecks](HealthChecks.md). For the HTTP routes, see [HealthEndpoints](HealthEndpoints.md).

## Constructor

```csharp
public SystemHealthCheck(ILogger<SystemHealthCheck> logger)
```

- `logger` must not be `null`; otherwise `ArgumentNullException` is thrown.
- The only dependency is the logger. The class has no options object and reads no configuration.

## What it measures

Each call to `CheckHealthAsync` takes a fresh snapshot of the current process:

| Value | Source | Used for status? |
| --- | --- | --- |
| Managed heap size | `GC.GetTotalMemory(false)` | Yes (512 MiB threshold) |
| Process uptime | `DateTime.UtcNow - Process.StartTime` | Yes (1 second threshold) |
| Working set | `Process.WorkingSet64` | No |
| Thread count | `Process.Threads.Count` | No |
| Processor count | `Environment.ProcessorCount` | No |
| Process ID and name | `Process.Id`, `Process.ProcessName` | No |
| Start time | `Process.StartTime` | No (used to compute uptime) |
| Environment name | `DOTNET_ENVIRONMENT` variable | No |

The heap figure comes from `GC.GetTotalMemory(false)`, which reports managed allocations only. It does not include native memory, so it is not the same as the working set.

## Thresholds

The check raises an issue for each condition that holds. Both conditions are evaluated on every call.

| Condition | Issue name | Effect |
| --- | --- | --- |
| Managed heap is greater than `536870912` bytes (512 MiB) | `high_memory_usage` | Degrades the result |
| Process uptime is less than 1 second | `process_just_started` | Degrades the result |

There is no separate `Unhealthy` threshold. The only path to `Unhealthy` is an exception (see below).

## Results

| Outcome | Status | Description |
| --- | --- | --- |
| No issues | `Healthy` | `System is healthy` |
| One or more issues | `Degraded` | `System has potential issues: <issues>`, where `<issues>` is the issue names joined with `, ` (for example `high_memory_usage, process_just_started`) |
| Any exception while reading process data | `Unhealthy` | `System health check failed`; the exception is attached to the result |

Logging:

- Healthy results are logged at `Debug`.
- Degraded results are logged at `Warning` with the issue list.
- Exceptions are logged at `Error` with the exception.

## Result data

The check builds a dictionary with the following keys, but **that dictionary is never passed to `HealthCheckResult`**. As coded, every result (healthy, degraded, or unhealthy) has an empty `Data` collection. The keys are listed here for reference, and they are the values a future change would need to attach to surface them.

| Key | Type | Value |
| --- | --- | --- |
| `process_id` | `int` | `Process.Id` |
| `process_name` | `string` | `Process.ProcessName` |
| `start_time` | `string` | `Process.StartTime` in round-trip `"o"` format |
| `uptime_seconds` | `double` | Seconds since process start |
| `total_memory_bytes` | `long` | `GC.GetTotalMemory(false)` |
| `working_set_bytes` | `long` | `Process.WorkingSet64` |
| `thread_count` | `int` | `Process.Threads.Count` |
| `processor_count` | `int` | `Environment.ProcessorCount` |
| `environment` | `string` | `DOTNET_ENVIRONMENT`, or `Unknown` when unset |

Note that `environment` reads `DOTNET_ENVIRONMENT` only. It does not read `ASPNETCORE_ENVIRONMENT`, so it reports `Unknown` in a host configured with the ASP.NET Core variable alone.

## Registration

`SystemHealthCheck` is not registered anywhere in this repository. `MapHealthEndpoints` maps routes only, and it does not call `AddHealthChecks()` or `AddCheck<SystemHealthCheck>()`. A host must register it explicitly:

```csharp
builder.Services
    .AddHealthChecks()
    .AddCheck<SystemHealthCheck>("system");
```

ASP.NET Core resolves the logger from DI, so no extra registration is needed for `ILogger<SystemHealthCheck>`.

Once registered, the check runs on every health route that executes the registered checks. The routes use an unrestricted predicate, so it runs on all of them, not only a dedicated "live" or "ready" route. If a host wants it on one route only, it should pass a `Predicate<HealthCheckRegistration>` filter (for example by tag) to `AddCheck` and the endpoint mapping.

## Limitations

- The `Data` dictionary is built but not returned (see above).
- `Process` is obtained with `Process.GetCurrentProcess()` and is not disposed. The check is called repeatedly, so each call creates a `Process` instance that is left to the finalizer.
- `process.Threads` enumerates all threads on each call, which is more costly than the other reads. The check is still cheap compared with a network probe.
- A process that has just started is `Degraded` for its first second. Orchestrators that treat `Degraded` as failing may see a brief failure at startup.
- The 512 MiB threshold is fixed in code. It cannot be changed without editing the class.
