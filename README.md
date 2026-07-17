// existing content ...

## HealthCheckControllerTests
The `HealthCheckControllerTests` class provides a set of unit tests for the `HealthCheckController` class, covering various scenarios such as health status, liveness, readiness, and resource metrics. It ensures that the controller behaves correctly under different conditions, such as when services are registered or not, and when they are healthy or unhealthy. Here's an example of how to use some of its public members:
```csharp
var tests = new HealthCheckControllerTests();
tests.GetHealthStatus_WithNoServicesRegistered_Returns200Healthy();
tests.GetHealthStatus_WhenAllServicesHealthy_Returns200();
tests.GetHealthStatus_WhenMoreThan20PercentUnhealthy_Returns503();
tests.GetLivenessStatus_AlwaysReturns200();
tests.GetLivenessStatus_ResponseContainsAliveTrue();
tests.GetReadinessStatus_WithNoServices_Returns503();
tests.GetReadinessStatus_WithServingService_Returns200();
tests.GetDetailedDiagnostics_Returns200();
tests.GetDetailedDiagnostics_ResponseValueIsNotNull();
tests.GetServiceHealthStatus_WithRegisteredServices_Returns200();
tests.GetServiceHealthStatus_WithNoServices_Returns200WithEmptyList();
tests.GetResourceMetrics_Returns200();
tests.GetResourceMetrics_ResponseValueIsNotNull();
```

## AuthenticationServiceTests
The `AuthenticationServiceTests` class provides a set of unit tests for the `AuthenticationService` class, covering various authentication scenarios. It tests the authentication of API keys, custom credentials, and bearer tokens. Here's an example of how to use some of its public members:
```csharp
var tests = new AuthenticationServiceTests();
tests.AuthenticateApiKey_WithValidCredentials_ReturnsAuthenticatedContext();
tests.AuthenticateApiKey_WithEmptyKey_ThrowsGrpcWebBridgeException();
tests.AuthenticateCustom_WithCredentials_AddsClaimsToContext();
tests.AuthenticateCustom_WithEmptyCredentials_ThrowsGrpcWebBridgeException();
tests.ValidateContext_WithNullContext_ReturnsFalse();
tests.AuthorizeRole_WithContextHoldingMatchingRole_ReturnsTrue();
tests.ExtractBearerToken_WithBearerPrefix_ReturnsRawToken();
tests.ExtractBearerToken_WithNullHeader_ReturnsNull();
```
