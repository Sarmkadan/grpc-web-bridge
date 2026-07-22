#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Endpoints;
using GrpcWebBridge.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Tests for the HealthEndpoints class.
/// </summary>
public sealed class HealthEndpointsTests
{
    private readonly ServiceRegistry _registry;
    private readonly StreamingService _streamingService;

    public HealthEndpointsTests()
    {
        _registry = new ServiceRegistry(NullLogger<ServiceRegistry>.Instance);
        _streamingService = new StreamingService(NullLogger<StreamingService>.Instance);
    }

    /// <summary>
    /// Tests that GetStartupTime returns a valid DateTime.
    /// </summary>
    [Fact]
    public void GetStartupTime_ReturnsValidDateTime()
    {
        // Act
        var startupTime = HealthEndpoints.GetStartupTime();

        // Assert
        startupTime.Should().NotBe(default);
        startupTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    /// <summary>
    /// Tests that GetStartupTime returns the same value on multiple calls.
    /// </summary>
    [Fact]
    public void GetStartupTime_ReturnsConsistentValue()
    {
        // Arrange
        var firstCall = HealthEndpoints.GetStartupTime();

        // Small delay to ensure time has passed
        Thread.Sleep(10);

        // Act
        var secondCall = HealthEndpoints.GetStartupTime();

        // Assert
        firstCall.Should().Be(secondCall);
    }

    /// <summary>
    /// Tests that MapHealthEndpoints can be called without throwing.
    /// </summary>
    [Fact]
    public void MapHealthEndpoints_CanBeCalled()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        // Act
        Action act = () => HealthEndpoints.MapHealthEndpoints(app);

        // Assert - endpoints should be added without throwing
        act.Should().NotThrow();
        app.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that MapHealthEndpoints throws ArgumentNullException when app is null.
    /// </summary>
    [Fact]
    public void MapHealthEndpoints_WithNullApp_ThrowsArgumentNullException()
    {
        // Arrange
        WebApplication? app = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => HealthEndpoints.MapHealthEndpoints(app!));
    }

    /// <summary>
    /// Tests that DetailedHealthResponse properties are correctly set.
    /// </summary>
    [Fact]
    public void DetailedHealthResponse_PropertiesAreCorrect()
    {
        // Arrange
        var response = new HealthEndpoints.DetailedHealthResponse
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            uptime = "1.00:00:00",
            uptime_seconds = 86400,
            services = new HealthEndpoints.ServiceHealthSummary
            {
                registered_count = 5,
                health_status = "healthy",
                services = new List<HealthEndpoints.ServiceHealthItem>()
            },
            workers = new HealthEndpoints.WorkerStatusSummary
            {
                streaming_service = new HealthEndpoints.StreamingWorkerStatus
                {
                    active_stream_count = 10,
                    max_stream_count = 100,
                    stream_capacity = "10/100",
                    status = "active"
                }
            },
            system = new HealthEndpoints.SystemStatus
            {
                environment = "Development",
                application_name = "TestApp",
                version = "1.0.0",
                timestamp = DateTime.UtcNow
            }
        };

        // Assert
        response.status.Should().Be("healthy");
        response.timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        response.uptime.Should().Be("1.00:00:00");
        response.uptime_seconds.Should().Be(86400);
        response.services.Should().NotBeNull();
        response.services!.registered_count.Should().Be(5);
        response.services.health_status.Should().Be("healthy");
        response.workers.Should().NotBeNull();
        response.workers!.streaming_service.Should().NotBeNull();
        response.workers.streaming_service!.active_stream_count.Should().Be(10);
        response.workers.streaming_service.max_stream_count.Should().Be(100);
        response.workers.streaming_service.stream_capacity.Should().Be("10/100");
        response.workers.streaming_service.status.Should().Be("active");
        response.system.Should().NotBeNull();
        response.system!.environment.Should().Be("Development");
        response.system.application_name.Should().Be("TestApp");
        response.system.version.Should().Be("1.0.0");
    }

    /// <summary>
    /// Tests that ServiceHealthSummary handles null services list.
    /// </summary>
    [Fact]
    public void ServiceHealthSummary_HandlesNullServices()
    {
        // Arrange
        var summary = new HealthEndpoints.ServiceHealthSummary
        {
            registered_count = 0,
            health_status = "no_services",
            services = null
        };

        // Assert
        summary.registered_count.Should().Be(0);
        summary.health_status.Should().Be("no_services");
        summary.services.Should().BeNull();
    }

    /// <summary>
    /// Tests that WorkerStatusSummary handles null streaming_service.
    /// </summary>
    [Fact]
    public void WorkerStatusSummary_HandlesNullStreamingService()
    {
        // Arrange
        var summary = new HealthEndpoints.WorkerStatusSummary
        {
            streaming_service = null
        };

        // Assert
        summary.streaming_service.Should().BeNull();
    }

    /// <summary>
    /// Tests that SystemStatus properties are correctly set.
    /// </summary>
    [Fact]
    public void SystemStatus_PropertiesAreCorrect()
    {
        // Arrange
        var status = new HealthEndpoints.SystemStatus
        {
            environment = "Production",
            application_name = "GrpcWebBridge",
            version = "2.0.0",
            timestamp = DateTime.UtcNow
        };

        // Assert
        status.environment.Should().Be("Production");
        status.application_name.Should().Be("GrpcWebBridge");
        status.version.Should().Be("2.0.0");
        status.timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that RegistryHealthResponse properties are correctly set.
    /// </summary>
    [Fact]
    public void RegistryHealthResponse_PropertiesAreCorrect()
    {
        // Arrange
        var response = new HealthEndpoints.RegistryHealthResponse
        {
            total_service_count = 15,
            registered_services = 12,
            service_registration_timestamps = new Dictionary<string, string>
            {
                { "service1", DateTime.UtcNow.ToString("o") },
                { "service2", DateTime.UtcNow.AddHours(-1).ToString("o") }
            },
            timestamp = DateTime.UtcNow
        };

        // Assert
        response.total_service_count.Should().Be(15);
        response.registered_services.Should().Be(12);
        response.service_registration_timestamps.Should().NotBeNull();
        response.service_registration_timestamps.Should().ContainKey("service1");
        response.timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that RegistryHealthResponse handles null service_registration_timestamps.
    /// </summary>
    [Fact]
    public void RegistryHealthResponse_HandlesNullTimestamps()
    {
        // Arrange
        var response = new HealthEndpoints.RegistryHealthResponse
        {
            total_service_count = 0,
            registered_services = 0,
            service_registration_timestamps = null,
            timestamp = DateTime.UtcNow
        };

        // Assert
        response.total_service_count.Should().Be(0);
        response.registered_services.Should().Be(0);
        response.service_registration_timestamps.Should().BeNull();
    }

    /// <summary>
    /// Tests that ServiceHealthItem properties are correctly set.
    /// </summary>
    [Fact]
    public void ServiceHealthItem_PropertiesAreCorrect()
    {
        // Arrange
        var item = new HealthEndpoints.ServiceHealthItem
        {
            id = "svc-123",
            name = "TestService",
            full_name = "TestPackage.TestService",
            endpoint = "localhost",
            port = 50051,
            status = "Serving",
            health_status = "Healthy",
            method_count = 10,
            created_at = DateTime.UtcNow.AddDays(-1),
            updated_at = DateTime.UtcNow
        };

        // Assert
        item.id.Should().Be("svc-123");
        item.name.Should().Be("TestService");
        item.full_name.Should().Be("TestPackage.TestService");
        item.endpoint.Should().Be("localhost");
        item.port.Should().Be(50051);
        item.status.Should().Be("Serving");
        item.health_status.Should().Be("Healthy");
        item.method_count.Should().Be(10);
        item.created_at.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(1));
        item.updated_at.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that StreamingWorkerStatus properties are correctly set.
    /// </summary>
    [Fact]
    public void StreamingWorkerStatus_PropertiesAreCorrect()
    {
        // Arrange
        var status = new HealthEndpoints.StreamingWorkerStatus
        {
            active_stream_count = 5,
            max_stream_count = 1000,
            stream_capacity = "5/1000",
            status = "idle"
        };

        // Assert
        status.active_stream_count.Should().Be(5);
        status.max_stream_count.Should().Be(1000);
        status.stream_capacity.Should().Be("5/1000");
        status.status.Should().Be("idle");
    }

    /// <summary>
    /// Tests that GetOverallServiceHealth returns correct status for empty service list.
    /// </summary>
    [Fact]
    public void GetOverallServiceHealth_WithEmptyServices_ReturnsNoServices()
    {
        // Arrange
        var registry = new ServiceRegistry(NullLogger<ServiceRegistry>.Instance);

        // Act
        var healthStatus = GetOverallServiceHealthViaReflection(registry);

        // Assert
        healthStatus.Should().Be("no_services");
    }

    /// <summary>
    /// Tests that GetOverallServiceHealth returns "healthy" when all services are serving.
    /// </summary>
    [Fact]
    public void GetOverallServiceHealth_WithAllServingServices_ReturnsHealthy()
    {
        // Arrange
        var registry = new ServiceRegistry(NullLogger<ServiceRegistry>.Instance);
        var service1 = new GrpcService("TestService1", "TestPackage1", "localhost", 50051);
        var service2 = new GrpcService("TestService2", "TestPackage2", "localhost", 50052);
        service1.Status = ServiceStatus.Serving;
        service2.Status = ServiceStatus.Serving;

        // Add a dummy method to make service valid
        var method = new GrpcMethod("TestMethod", "TestPackage1.TestService1", MethodType.Unary, "string", "string");
        service1.AddMethod(method);
        service2.AddMethod(method);

        registry.RegisterService(service1);
        registry.RegisterService(service2);

        // Act
        var healthStatus = GetOverallServiceHealthViaReflection(registry);

        // Assert
        healthStatus.Should().Be("healthy");
    }

    /// <summary>
    /// Tests that GetOverallServiceHealth returns "unhealthy" when all services are not serving.
    /// </summary>
    [Fact]
    public void GetOverallServiceHealth_WithAllNotServingServices_ReturnsUnhealthy()
    {
        // Arrange
        var registry = new ServiceRegistry(NullLogger<ServiceRegistry>.Instance);
        var service1 = new GrpcService("TestService1", "TestPackage1", "localhost", 50051);
        var service2 = new GrpcService("TestService2", "TestPackage2", "localhost", 50052);
        service1.Status = ServiceStatus.NotServing;
        service2.Status = ServiceStatus.NotServing;

        // Add a dummy method to make service valid
        var method = new GrpcMethod("TestMethod", "TestPackage1.TestService1", MethodType.Unary, "string", "string");
        service1.AddMethod(method);
        service2.AddMethod(method);

        registry.RegisterService(service1);
        registry.RegisterService(service2);

        // Act
        var healthStatus = GetOverallServiceHealthViaReflection(registry);

        // Assert
        healthStatus.Should().Be("unhealthy");
    }

    /// <summary>
    /// Tests that GetOverallServiceHealth returns "degraded" when some services are serving and some are not.
    /// </summary>
    [Fact]
    public void GetOverallServiceHealth_WithMixedServices_ReturnsDegraded()
    {
        // Arrange
        var registry = new ServiceRegistry(NullLogger<ServiceRegistry>.Instance);
        var service1 = new GrpcService("TestService1", "TestPackage1", "localhost", 50051);
        var service2 = new GrpcService("TestService2", "TestPackage2", "localhost", 50052);
        service1.Status = ServiceStatus.Serving;
        service2.Status = ServiceStatus.NotServing;

        // Add a dummy method to make service valid
        var method = new GrpcMethod("TestMethod", "TestPackage1.TestService1", MethodType.Unary, "string", "string");
        service1.AddMethod(method);
        service2.AddMethod(method);

        registry.RegisterService(service1);
        registry.RegisterService(service2);

        // Act
        var healthStatus = GetOverallServiceHealthViaReflection(registry);

        // Assert
        healthStatus.Should().Be("degraded");
    }

    /// <summary>
    /// Helper method to access the private GetOverallServiceHealth method via reflection.
    /// </summary>
    private static string GetOverallServiceHealthViaReflection(ServiceRegistry registry)
    {
        var method = typeof(HealthEndpoints).GetMethod(
            "GetOverallServiceHealth",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        return (string)method!.Invoke(null, new object[] { registry })!;
    }
}
