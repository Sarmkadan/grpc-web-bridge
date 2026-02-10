# HealthCheckControllerTests

Unit test class for `HealthCheckController` that verifies health check endpoints behavior under various service configurations and states. The tests validate HTTP status codes, response payloads, and edge cases for liveness, readiness, service-specific health, and resource metrics endpoints.

## API

### `GetHealthStatus_WithNoServicesRegistered_Returns200Healthy`
Verifies that the `/health` endpoint returns HTTP 200 with a healthy status when no services are registered. Ensures the default health status reflects system-wide availability even when no services are configured.

### `GetHealthStatus_WhenAllServicesHealthy_Returns200`
Validates that the `/health` endpoint returns HTTP 200 when all registered services report healthy. Confirms that the aggregate health status aggregates individual service states correctly.

### `GetHealthStatus_WhenMoreThan20PercentUnhealthy_Returns503`
Ensures the `/health` endpoint returns HTTP 503 when more than 20% of registered services are unhealthy. Validates the failure threshold logic for triggering unhealthy responses.

### `GetLivenessStatus_AlwaysReturns200`
Confirms that the `/live` endpoint always returns HTTP 200 regardless of service health states. Ensures liveness probes are independent of service-specific health checks.

### `GetLivenessStatus_ResponseContainsAliveTrue`
Verifies that the `/live` endpoint response includes a boolean `alive` field set to `true`. Validates the expected payload structure for liveness probes.

### `GetReadinessStatus_WithNoServices_Returns503`
Checks that the `/ready` endpoint returns HTTP 503 when no services are registered. Ensures readiness reflects service availability and defaults to unhealthy when no services exist.

### `GetReadinessStatus_WithServingService_Returns200`
Validates that the `/ready` endpoint returns HTTP 200 when at least one serving service is registered and healthy. Confirms readiness depends on service registration and health.

### `GetDetailedDiagnostics_Returns200`
Ensures the `/diagnostics` endpoint returns HTTP 200 under normal conditions. Validates that detailed diagnostics are accessible without failure.

### `GetDetailedDiagnostics_ResponseValueIsNotNull`
Confirms that the `/diagnostics` endpoint response payload is non-null. Validates that the diagnostics payload contains meaningful data.

### `GetServiceHealthStatus_WithRegisteredServices_Returns200`
Verifies that the `/health/services` endpoint returns HTTP 200 when services are registered. Ensures the service-specific health endpoint is accessible.

### `GetServiceHealthStatus_WithNoServices_Returns200WithEmptyList`
Validates that the `/health/services` endpoint returns HTTP 200 with an empty list when no services are registered. Confirms the endpoint handles absence of services gracefully.

### `GetResourceMetrics_Returns200`
Ensures the `/metrics` endpoint returns HTTP 200 under normal conditions. Validates that resource metrics are accessible without failure.

### `GetResourceMetrics_ResponseValueIsNotNull`
Confirms that the `/metrics` endpoint response payload is non-null. Validates that the metrics payload contains meaningful data.

## Usage
