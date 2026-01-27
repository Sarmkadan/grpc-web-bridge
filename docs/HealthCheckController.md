# HealthCheckController

Provides endpoints for monitoring the health, diagnostics, and resource metrics of services within the `grpc-web-bridge` project. Supports both basic health status checks and detailed diagnostic information for operational visibility.

## API

### `public HealthCheckController`

Initializes a new instance of the `HealthCheckController` with required dependencies for health monitoring and diagnostics.

### `public IActionResult GetHealthStatus()`

Returns a high-level health status of the service.

- **Return value**: `IActionResult` with HTTP 200 OK and a JSON body indicating the overall health status (e.g., `{ "status": "Healthy" }`).
- **Throws**: May throw if critical dependencies are unreachable or misconfigured.

### `public IActionResult GetDetailedDiagnostics()`

Returns comprehensive diagnostic information including health checks, resource usage, and service dependencies.

- **Return value**: `IActionResult` with HTTP 200 OK and a detailed JSON payload containing health status, resource metrics, and diagnostic data.
- **Throws**: May throw if diagnostic collection fails due to system constraints or permission issues.

### `public IActionResult GetServiceHealthStatus(string serviceName)`

Returns the health status of a specific service identified by `serviceName`.

- **Parameters**:
  - `serviceName` (string): The name of the service to check.
- **Return value**: `IActionResult` with HTTP 200 OK and a JSON body indicating the service's health (e.g., `{ "serviceName": "grpc-service", "status": "Healthy" }`). Returns HTTP 404 Not Found if the service is unknown or not monitored.
- **Throws**: May throw if the service name is invalid or the lookup mechanism fails.

### `public IActionResult GetResourceMetrics()`

Returns current resource utilization metrics such as CPU, memory, and network usage.

- **Return value**: `IActionResult` with HTTP 200 OK and a JSON body containing resource metrics (e.g., `{ "cpuUsage": 0.45, "memoryUsedMB": 128, "activeConnections": 42 }`).
- **Throws**: May throw if metrics collection is unsupported or restricted by the runtime environment.

### `public IActionResult GetReadinessStatus()`

Indicates whether the service is ready to accept traffic.

- **Return value**: `IActionResult` with HTTP 200 OK and a JSON body indicating readiness (e.g., `{ "ready": true }`). Returns HTTP 503 Service Unavailable if not ready.
- **Throws**: May throw if readiness state cannot be determined due to internal errors.

### `public IActionResult GetLivenessStatus()`

Indicates whether the service is alive and responding to requests.

- **Return value**: `IActionResult` with HTTP 200 OK and a JSON body indicating liveness (e.g., `{ "alive": true }`). Returns HTTP 503 Service Unavailable if unresponsive.
- **Throws**: May throw if the liveness probe mechanism fails (e.g., deadlock or timeout).

## Usage
