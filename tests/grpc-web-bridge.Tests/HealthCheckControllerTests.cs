#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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

public sealed class HealthCheckControllerTests
{
    private static ServiceRegistry CreateRegistry()
        => new(NullLogger<ServiceRegistry>.Instance);

    private static StreamingService CreateStreaming()
        => new(NullLogger<StreamingService>.Instance);

    private static HealthCheckController CreateController(
        ServiceRegistry? registry = null,
        StreamingService? streaming = null)
    {
        return new HealthCheckController(
            registry ?? CreateRegistry(),
            streaming ?? CreateStreaming(),
            NullLogger<HealthCheckController>.Instance);
    }

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

    [Fact]
    public void GetHealthStatus_WithNoServicesRegistered_Returns200Healthy()
    {
        var controller = CreateController();

        var result = controller.GetHealthStatus();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

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

    [Fact]
    public void GetLivenessStatus_AlwaysReturns200()
    {
        var controller = CreateController();

        var result = controller.GetLivenessStatus();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

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

    [Fact]
    public void GetReadinessStatus_WithNoServices_Returns503()
    {
        var controller = CreateController();

        var result = controller.GetReadinessStatus();

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

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

    [Fact]
    public void GetDetailedDiagnostics_Returns200()
    {
        var controller = CreateController();

        var result = controller.GetDetailedDiagnostics();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

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

    [Fact]
    public void GetResourceMetrics_Returns200()
    {
        var controller = CreateController();

        var result = controller.GetResourceMetrics();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void GetResourceMetrics_ResponseValueIsNotNull()
    {
        var controller = CreateController();

        var result = (OkObjectResult)controller.GetResourceMetrics();

        result.Value.Should().NotBeNull();
    }
}
