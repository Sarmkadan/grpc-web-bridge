# HealthEndpoints

Provides health check endpoint registration and response models for the gRPC-Web bridge. The static members configure HTTP endpoints that expose service, worker, and system health status, while the instance members define the JSON-serializable payloads returned by those endpoints.

## API

### `public static DateTime GetStartupTime`

Gets the process startup timestamp captured at application initialization.

**Returns:** `DateTime` — UTC time when the current process started.

**Throws:** Never.

---

### `public static void MapHealthEndpoints(IEndpointRouteBuilder endpoints)`

Registers health check routes (`/health`, `/health/services`, `/health/workers`, `/health/system`) on the provided endpoint route builder.

**Parameters:**
- `endpoints` (`IEndpointRouteBuilder`): The route builder to which health endpoints are added. Must not be null.

**Returns:** `void`

**Throws:**
- `ArgumentNullException` if `endpoints` is null.

---

### `public string? status`

Overall health status indicator for the aggregate response (e.g., "healthy", "degraded", "unhealthy").

**Returns:** `string?` — Null if the status has not been evaluated.

---

### `public DateTime timestamp`

UTC timestamp when the health snapshot was generated.

**Returns:** `DateTime` — Always set to the time of evaluation.

---

### `public string? uptime`

Human-readable uptime string (e.g., "2d 3h 15m").

**Returns:** `string?` — Null if startup time is unavailable.

---

### `public int uptime_seconds`

Total seconds elapsed since process startup.

**Returns:** `int` — Zero or positive integer.

---

### `public ServiceHealthSummary? services`

Summary of gRPC service health states.

**Returns:** `ServiceHealthSummary?` — Null if service health collection is disabled or unavailable.

---

### `public WorkerStatusSummary? workers`

Summary of background worker health states.

**Returns:** `WorkerStatusSummary?` — Null if worker monitoring is disabled or unavailable.

---

### `public SystemStatus? system`

System-level resource utilization (CPU, memory, disk, network).

**Returns:** `SystemStatus?` — Null if system metrics collection fails or is disabled.

---

### `public int registered_count`

Number of services currently registered in the bridge registry.

**Returns:** `int` — Non-negative count.

---

### `public string? health_status`

Aggregated health status for a specific service or worker entry.

**Returns:** `string?` — One of "healthy", "degraded", "unhealthy", or null if unknown.

---

### `public List<ServiceHealthItem>? services`

Detailed per-service health items when requesting `/health/services`.

**Returns:** `List<ServiceHealthItem>?` — Null if the detailed list is not requested or unavailable.

---

### `public string? id`

Unique identifier for a service or worker instance.

**Returns:** `string?` — Null if not assigned.

---

### `public string? name`

Short display name of the service or worker.

**Returns:** `string?` — Null if not set.

---

### `public string? full_name`

Fully qualified name including namespace or group.

**Returns:** `string?` — Null if not set.

---

### `public string? endpoint`

Network endpoint where the service or worker is reachable.

**Returns:** `string?` — Null if not applicable.

---

### `public int port`

TCP port number the service or worker listens on.

**Returns:** `int` — Zero if not applicable or unknown.

---

### `public string? status`

Current operational status of the individual service or worker (e.g., "running", "stopped", "starting").

**Returns:** `string?` — Null if state is indeterminate.

---

### `public string? health_status`

Health-specific status for the individual service or worker (e.g., "healthy", "degraded", "unhealthy").

**Returns:** `string?` — Null if health checks are not configured.

---

### `public int method_count`

Number of gRPC methods exposed by the service.

**Returns:** `int` — Zero or positive integer.

---

## Usage

### Registering health endpoints in `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// ... other service registrations ...

var app = builder.Build();

HealthEndpoints.MapHealthEndpoints(app);

app.Run();
```

### Consuming the `/health/services` endpoint

```csharp
using System.Net.Http.Json;
using GrpcWebBridge.Health;

var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

var response = await client.GetFromJsonAsync<HealthEndpoints>("/health/services");

if (response?.services is { Count: > 0 } items)
{
    foreach (var svc in items)
    {
        Console.WriteLine($"{svc.full_name} [{svc.health_status}] - {svc.method_count} methods on port {svc.port}");
    }
}
```

---

## Notes

- `GetStartupTime` captures the time once at static initialization; it does not update on configuration reloads or hot restarts.
- `MapHealthEndpoints` should be called after `WebApplication.Build()` but before `Run()`. Calling it multiple times registers duplicate routes.
- All instance properties are populated by the endpoint handlers at request time; they are not thread-safe for concurrent mutation. Treat returned objects as immutable snapshots.
- The duplicate-named properties (`status`, `services`, `health_status`) exist on different logical DTO shapes returned by different routes (`/health` vs `/health/services` vs `/health/workers`). Deserialize into the appropriate shape based on the requested path.
- `uptime_seconds` and `uptime` are derived from `GetStartupTime`; if the system clock is adjusted backward, values may be inaccurate until the next process restart.
- `SystemStatus` collection may throw internally (e.g., `PerformanceCounter` access denied); the endpoint catches and returns `null` for `system` rather than failing the request.
- `registered_count` reflects the in-memory registry at snapshot time and may differ from actual live connections during rapid scaling events.
