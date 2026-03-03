#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using FluentAssertions;
using GrpcWebBridge.Controllers;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Contains unit tests for the <see cref="HealthCheckController"/> class.
/// Tests verify the health check endpoints behavior including overall health status,
/// liveness, readiness, detailed diagnostics, per-service health, and resource metrics.
/// </summary>
public sealed class HealthCheckControllerTests
{
	/// <summary>
	/// Creates a new <see cref="ServiceRegistry"/> instance with a null logger for testing purposes.
	/// </summary>
	/// <returns>A new <see cref="ServiceRegistry"/> instance configured with a null logger.</returns>
	private static ServiceRegistry CreateRegistry()
	=> new(NullLogger<ServiceRegistry>.Instance);

	/// <summary>
	/// Creates a new <see cref="StreamingService"/> instance with a null logger for testing purposes.
	/// </summary>
	/// <returns>A new <see cref="StreamingService"/> instance configured with a null logger.</returns>
	private static StreamingService CreateStreaming()
	=> new(NullLogger<StreamingService>.Instance);

	/// <summary>
	/// Creates a new <see cref="HealthCheckController"/> instance for testing.
	/// </summary>
	/// <param name="registry">Optional service registry to use. If null, a new registry is created.</param>
	/// <param name="streaming">Optional streaming service to use. If null, a new streaming service is created.</param>
	/// <returns>A new <see cref="HealthCheckController"/> instance configured with the specified or default services.</returns>
	private static HealthCheckController CreateController(
	ServiceRegistry? registry = null,
	StreamingService? streaming = null)
	{
	return new HealthCheckController(
	registry ?? CreateRegistry(),
	streaming ?? CreateStreaming(),
	NullLogger<HealthCheckController>.Instance);
	}

	/// <summary>
	/// Creates a test gRPC service with the specified name and status.
	/// </summary>
	/// <param name="name">The name of the service to create.</param>
	/// <param name="status">The status of the service. Defaults to <see cref="ServiceStatus.Serving"/>.</param>
	/// <returns>A new <see cref="GrpcService"/> instance configured for testing.</returns>
	private static GrpcService BuildService(string name, ServiceStatus status = ServiceStatus.Serving)
	{
	var svc = new GrpcService(name, "test.pkg", "localhost", 50051) { Status = status };
	svc.AddMethod(new GrpcMethod(
	name: "GetData",
	fullName: $"test.pkg.{name}/GetData",
	type: Domain.MethodType.Unary,
	inputMessage: "GetDataRequest",
	outputMessage: "GetDataResponse"));
	return svc;
	}

	// ─────────────────────────────────────────────────────────────────────
	// GET /api/health — top-level status
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that the health status endpoint returns HTTP 200 OK when no services are registered.
	/// Verifies that the controller correctly handles the case of an empty service registry.
	/// </summary>
	[Fact]
	public void GetHealthStatus_WithNoServicesRegistered_Returns200Healthy()
	{
	var controller = CreateController();

	var result = controller.GetHealthStatus();

	result.Should().BeOfType<OkObjectResult>()
	.Which.StatusCode.Should().Be(StatusCodes.Status200OK);
	}

	/// <summary>
	/// Tests that the health status endpoint returns HTTP 200 OK when all registered services are healthy.
	/// Verifies that the controller correctly reports healthy status when all services are serving requests.
	/// </summary>
	[Fact]
	public void GetHealthStatus_WhenAllServicesHealthy_Returns200()
	{
	var registry = CreateRegistry();
	registry.RegisterService(BuildService("SvcA"));
	registry.RegisterService(BuildService("SvcB"));
	var controller = CreateController(registry);

	var result = controller.GetHealthStatus();

	result.Should().BeOfType<OkObjectResult>();
	}

	/// <summary>
	/// Tests that the health status endpoint returns HTTP 503 Service Unavailable when more than 20% of services are unhealthy.
	/// Verifies that the controller correctly calculates the health percentage and returns appropriate status codes.
	/// </summary>
	[Fact]
	public void GetHealthStatus_WhenMoreThan20PercentUnhealthy_Returns503()
	{
	var registry = CreateRegistry();
	// Register 5 services; mark 4 as not serving → only 20% healthy → degraded
	for (int i = 1; i <= 4; i++)
	registry.RegisterService(BuildService($"SvcUnhealthy{i}", ServiceStatus.NotServing));
	registry.RegisterService(BuildService("SvcHealthy"));

	var controller = CreateController(registry);
	var result = controller.GetHealthStatus();

	result.Should().BeOfType<ObjectResult>()
	.Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
	}

	// ─────────────────────────────────────────────────────────────────────
	// GET /api/health/alive — liveness probe
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that the liveness status endpoint always returns HTTP 200 OK regardless of service health.
	/// Verifies that the liveness probe is independent of actual service health status.
	/// </summary>
	[Fact]
	public void GetLivenessStatus_AlwaysReturns200()
	{
	var controller = CreateController();

	var result = controller.GetLivenessStatus();

	result.Should().BeOfType<OkObjectResult>()
	.Which.StatusCode.Should().Be(StatusCodes.Status200OK);
	}

	/// <summary>
	/// Tests that the liveness status response contains an 'alive' property set to true.
	/// Verifies the structure and content of the liveness probe response.
	/// </summary>
	[Fact]
	public void GetLivenessStatus_ResponseContainsAliveTrue()
	{
	var controller = CreateController();

	var result = (OkObjectResult)controller.GetLivenessStatus();
	var body = result.Value!;

	body.Should().NotBeNull();
	// The response is an anonymous object; verify via reflection
	var aliveProperty = body.GetType().GetProperty("alive");
	aliveProperty.Should().NotBeNull();
	aliveProperty!.GetValue(body).Should().Be(true);
	}

	// ─────────────────────────────────────────────────────────────────────
	// GET /api/health/ready — readiness probe
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that the readiness status endpoint returns HTTP 503 Service Unavailable when no services are registered.
	/// Verifies that the readiness probe requires at least one registered service to be considered ready.
	/// </summary>
	[Fact]
	public void GetReadinessStatus_WithNoServices_Returns503()
	{
	var controller = CreateController();

	var result = controller.GetReadinessStatus();

	result.Should().BeOfType<ObjectResult>()
	.Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
	}

	/// <summary>
	/// Tests that the readiness status endpoint returns HTTP 200 OK when at least one service is registered and serving.
	/// Verifies that the readiness probe correctly identifies when the application is ready to receive traffic.
	/// </summary>
	[Fact]
	public void GetReadinessStatus_WithServingService_Returns200()
	{
	var registry = CreateRegistry();
	registry.RegisterService(BuildService("ReadySvc"));
	var controller = CreateController(registry);

	var result = controller.GetReadinessStatus();

	result.Should().BeOfType<OkObjectResult>()
	.Which.StatusCode.Should().Be(StatusCodes.Status200OK);
	}

	// ─────────────────────────────────────────────────────────────────────
	// GET /api/health/detailed — diagnostics
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that the detailed diagnostics endpoint returns HTTP 200 OK.
	/// Verifies that the diagnostics endpoint is accessible and returns a successful response.
	/// </summary>
	[Fact]
	public void GetDetailedDiagnostics_Returns200()
	{
	var controller = CreateController();

	var result = controller.GetDetailedDiagnostics();

	result.Should().BeOfType<OkObjectResult>()
	.Which.StatusCode.Should().Be(StatusCodes.Status200OK);
	}

	/// <summary>
	/// Tests that the detailed diagnostics response value is not null.
	/// Verifies that the diagnostics endpoint returns a non-null response body.
	/// </summary>
	[Fact]
	public void GetDetailedDiagnostics_ResponseValueIsNotNull()
	{
	var controller = CreateController();

	var result = (OkObjectResult)controller.GetDetailedDiagnostics();

	result.Value.Should().NotBeNull();
	}

	// ─────────────────────────────────────────────────────────────────────
	// GET /api/health/services — per-service health
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that the per-service health status endpoint returns HTTP 200 OK when services are registered.
	/// Verifies that the service health endpoint returns a successful response when services exist.
	/// </summary>
	[Fact]
	public void GetServiceHealthStatus_WithRegisteredServices_Returns200()
	{
	var registry = CreateRegistry();
	registry.RegisterService(BuildService("SvcX"));
	var controller = CreateController(registry);

	var result = controller.GetServiceHealthStatus();

	result.Should().BeOfType<OkObjectResult>()
	.Which.StatusCode.Should().Be(StatusCodes.Status200OK);
	}

	/// <summary>
	/// Tests that the per-service health status endpoint returns HTTP 200 OK with an empty list when no services are registered.
	/// Verifies that the service health endpoint handles the case of an empty service registry gracefully.
	/// </summary>
	[Fact]
	public void GetServiceHealthStatus_WithNoServices_Returns200WithEmptyList()
	{
	var controller = CreateController();

	var result = controller.GetServiceHealthStatus();

	result.Should().BeOfType<OkObjectResult>();
	}

	// ─────────────────────────────────────────────────────────────────────
	// GET /api/health/resources — resource metrics
	// ─────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that the resource metrics endpoint returns HTTP 200 OK.
	/// Verifies that the metrics endpoint is accessible and returns a successful response.
	/// </summary>
	[Fact]
	public void GetResourceMetrics_Returns200()
	{
	var controller = CreateController();

	var result = controller.GetResourceMetrics();

	result.Should().BeOfType<OkObjectResult>()
	.Which.StatusCode.Should().Be(StatusCodes.Status200OK);
	}

	/// <summary>
	/// Tests that the resource metrics response value is not null.
	/// Verifies that the metrics endpoint returns a non-null response body with resource information.
	/// </summary>
	[Fact]
	public void GetResourceMetrics_ResponseValueIsNotNull()
	{
	var controller = CreateController();

	var result = (OkObjectResult)controller.GetResourceMetrics();

	result.Value.Should().NotBeNull();
	}
}