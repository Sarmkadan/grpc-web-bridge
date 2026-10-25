# HealthCheckResponseWriter

Static class in `GrpcWebBridge.Endpoints` (`src/GrpcWebBridge/Endpoints/HealthCheckResponseWriter.cs`). It provides one method, `WriteHealthCheckResponse`, that renders an ASP.NET Core `HealthReport` as JSON and sets the HTTP status code. It is the `ResponseWriter` used by the `/healthz`, `/ready` and `/health` endpoints registered in `HealthEndpoints.MapHealthEndpoints`.

It uses only the BCL (`System.Text.Json`) and `Microsoft.AspNetCore.Diagnostics.HealthChecks`. No NuGet package is needed.

## Overview

ASP.NET Core's health check middleware runs the registered checks, builds a `HealthReport`, and then calls the configured `ResponseWriter`. By default that writer emits plain text. This class replaces it with a JSON body that Kubernetes probes and other monitoring tools can read, and it also sets the status code from the overall health status.

The writer sets the status code itself. The middleware first applies `HealthCheckOptions.ResultStatusCodes`, and then the writer overwrites `Response.StatusCode`. Because of this, the mapping in the table below is what a client sees. `ResultStatusCodes` has no effect on endpoints that use this writer.

---

## API

### `public static Task WriteHealthCheckResponse(HttpContext context, HealthReport healthReport)`

Writes the health report to the response. It matches the `Func<HttpContext, HealthReport, Task>` delegate used by `HealthCheckOptions.ResponseWriter`, so it can be passed as a method group.

What it does, in order:

1. Throws `ArgumentNullException` if `context` or `healthReport` is `null`.
2. Sets `Response.ContentType` to `application/json`.
3. Sets `Response.StatusCode` from the overall status (see [Status codes](#status-codes)).
4. Serializes the body with `System.Text.Json` (camelCase property names, no indentation) and writes it with `Response.WriteAsync`.

- **Parameters**
  - `context`: The current `HttpContext`. Its response is written to.
  - `healthReport`: The aggregated report produced by the health check service.
- **Returns**
  - A `Task` that completes when the body has been written.
- **Throws**
  - `ArgumentNullException`: If `context` or `healthReport` is `null`.

---

## Output JSON shape

The body is a single JSON object with compact formatting. Example (two checks, one degraded):

```json
{"status":"degraded","totalDuration":0.0123456,"timestamp":"2026-10-25T10:15:30.1234567Z","checks":{"grpc-registry":{"status":"healthy","description":null,"duration":0.0004,"exception":null},"streaming":{"status":"degraded","description":"Stream capacity above 80%","duration":0.0011,"exception":null}}}
```

Pretty-printed for readability:

```json
{
  "status": "degraded",
  "totalDuration": 0.0123456,
  "timestamp": "2026-10-25T10:15:30.1234567Z",
  "checks": {
    "grpc-registry": {
      "status": "healthy",
      "description": null,
      "duration": 0.0004,
      "exception": null
    },
    "streaming": {
      "status": "degraded",
      "description": "Stream capacity above 80%",
      "duration": 0.0011,
      "exception": null
    }
  }
}
```

### Top-level fields

| Field | Type | Value |
|---|---|---|
| `status` | string | Overall status: `healthy`, `degraded` or `unhealthy` (lowercase `HealthStatus` name). |
| `totalDuration` | number | Total time the checks took, in seconds (`HealthReport.TotalDuration.TotalSeconds`). |
| `timestamp` | string | UTC time the response was generated, ISO 8601 (`DateTime.UtcNow`). |
| `checks` | object | One entry per registered check. Keys are the check names exactly as registered. |

### Fields in each `checks` entry

| Field | Type | Value |
|---|---|---|
| `status` | string | Status of this check: `healthy`, `degraded` or `unhealthy`. |
| `description` | string or null | `HealthReportEntry.Description`, set by the check. |
| `duration` | number | Time this check took, in seconds. |
| `exception` | string or null | `Exception.Message` of the exception thrown by the check, if any. Only the message is included, not the stack trace. |

### Naming notes

- Top-level and per-check property names are camelCase, because the serializer uses `JsonNamingPolicy.CamelCase`.
- The keys of `checks` are not changed. They are the check names as registered (for example `grpc-registry`), because the naming policy applies to properties and not to dictionary keys.
- `null` values are written as `null`. The properties are not omitted.

---

## Status codes

The writer sets the HTTP status code from the overall `HealthReport.Status`:

| Overall status | HTTP status |
|---|---|
| `Healthy` | 200 OK |
| `Degraded` | 200 OK |
| `Unhealthy` | 503 Service Unavailable |

`Degraded` returns 200 on purpose. A degraded service can still serve traffic, so a Kubernetes readiness probe should not remove it from the load balancer. Only `Unhealthy` returns 503. Any other value also returns 503.

> **Note:** `HealthEndpoints.MapHealthEndpoints` passes `ResultStatusCodes` that map `Degraded` to 503. Those values are overwritten by this writer, so the actual response for `Degraded` is 200. If you need 503 for degraded checks, write a different `ResponseWriter`.

The `/health/detailed` and `/health/registry` endpoints do not use this writer. They have their own response models in `HealthEndpoints`.

---

## Plugging it into `HealthCheckOptions`

Set `ResponseWriter` on the `HealthCheckOptions` passed to `MapHealthChecks`. Use the fully qualified name, because the project also defines its own `GrpcWebBridge.BackgroundWorkers.HealthCheckOptions`:

```csharp
using GrpcWebBridge.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

app.MapHealthChecks("/health", new global::Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse,
    AllowCachingResponses = false
});
```

Add one mapping per path you want to expose. This is how `HealthEndpoints.MapHealthEndpoints` does it for `/healthz`, `/ready` and `/health`.

To check one path with a subset of the checks, change `Predicate`. For example, `Predicate = check => check.Tags.Contains("ready")` returns only the checks tagged `ready`. The writer does not filter checks. It writes whatever report it receives.

### Using it with other code

Because the method has the `ResponseWriter` signature, you can also call it from your own writer. For example, to add a header and then delegate:

```csharp
ResponseWriter = async (context, report) =>
{
    context.Response.Headers["Cache-Control"] = "no-store";
    await HealthCheckResponseWriter.WriteHealthCheckResponse(context, report);
}
```

---

## Related

- `src/GrpcWebBridge/Endpoints/HealthEndpoints.cs`: maps the endpoints that use this writer.
- [HealthEndpoints](HealthEndpoints.md)
- [HealthChecks](HealthChecks.md)
