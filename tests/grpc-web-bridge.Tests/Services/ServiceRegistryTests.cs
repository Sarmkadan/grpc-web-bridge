#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests.Services;

/// <summary>
/// Tests for the ServiceRegistry class
/// </summary>
public sealed class ServiceRegistryTests
{
    private readonly ILogger<ServiceRegistry> _logger;
    private readonly ServiceRegistry _registry;

    public ServiceRegistryTests()
    {
        _logger = NullLogger<ServiceRegistry>.Instance;
        _registry = new ServiceRegistry(_logger);
    }

    /// <summary>
    /// Creates a test GrpcService instance
    /// </summary>
    private static GrpcService CreateTestService(
        string name = "TestService",
        string packageName = "test.package",
        string endpoint = "localhost",
        int port = 50051)
    {
        var service = new GrpcService(name, packageName, endpoint, port);

        // Add a test method
        var method = new GrpcMethod
        {
            Name = "TestMethod",
            FullName = "test.package.TestService/TestMethod",
            Type = MethodType.Unary,
            InputMessageType = "TestRequest",
            OutputMessageType = "TestResponse"
        };
        service.AddMethod(method);

        return service;
    }

    /// <summary>
    /// Tests that a service can be registered successfully
    /// </summary>
    [Fact]
    public void RegisterService_WithValidService_RegistersSuccessfully()
    {
        // Arrange
        var service = CreateTestService();

        // Act
        _registry.RegisterService(service);

        // Assert
        _registry.RegisteredServiceCount.Should().Be(1);
        _registry.ServiceExists(service.FullName).Should().BeTrue();
        var retrieved = _registry.GetService(service.FullName);
        retrieved.Should().NotBeNull();
        retrieved!.FullName.Should().Be(service.FullName);
    }

    /// <summary>
    /// Tests that registering a null service throws ArgumentNullException
    /// </summary>
    [Fact]
    public void RegisterService_WithNullService_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _registry.RegisterService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that registering a service with invalid data throws exception
    /// </summary>
    [Fact]
    public void RegisterService_WithInvalidService_ThrowsException()
    {
        // Arrange
        var invalidService = new GrpcService
        {
            Name = "", // Invalid name
            PackageName = "test.package",
            Endpoint = "localhost",
            Port = 50051
        };

        // Act
        Action act = () => _registry.RegisterService(invalidService);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that registering a duplicate service throws ServiceRegistrationException
    /// </summary>
    [Fact]
    public void RegisterService_WithDuplicateService_ThrowsServiceRegistrationException()
    {
        // Arrange
        var service = CreateTestService();
        _registry.RegisterService(service);

        // Act
        Action act = () => _registry.RegisterService(service);

        // Assert
        act.Should().Throw<ServiceRegistrationException>()
            .Where(e => e.Message.Contains("already registered"));
    }

    /// <summary>
    /// Tests that GetService returns null for non-existent service
    /// </summary>
    [Fact]
    public void GetService_WithNonExistentService_ReturnsNull()
    {
        // Act
        var result = _registry.GetService("nonexistent.service");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetService returns null for null or empty fullName
    /// </summary>
    [Fact]
    public void GetService_WithNullOrEmptyFullName_ReturnsNull()
    {
        // Act
        var result1 = _registry.GetService(null);
        var result2 = _registry.GetService("");
        var result3 = _registry.GetService("   ");

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetService with serviceName and packageName works correctly
    /// </summary>
    [Fact]
    public void GetService_WithServiceNameAndPackageName_ReturnsService()
    {
        // Arrange
        var service = CreateTestService("MyService", "my.package", "localhost", 50051);
        _registry.RegisterService(service);

        // Act
        var result = _registry.GetService("MyService", "my.package");

        // Assert
        result.Should().NotBeNull();
        result!.FullName.Should().Be("my.package.MyService");
    }

    /// <summary>
    /// Tests that GetService with null serviceName or packageName returns null
    /// </summary>
    [Fact]
    public void GetService_WithNullServiceNameOrPackageName_ReturnsNull()
    {
        // Act
        var result1 = _registry.GetService(null, "package");
        var result2 = _registry.GetService("service", null);
        var result3 = _registry.GetService("", "package");
        var result4 = _registry.GetService("service", "");

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        result3.Should().BeNull();
        result4.Should().BeNull();
    }

    /// <summary>
    /// Tests that UnregisterService returns false for non-existent service
    /// </summary>
    [Fact]
    public void UnregisterService_WithNonExistentService_ReturnsFalse()
    {
        // Act
        var result = _registry.UnregisterService("nonexistent.service");

        // Assert
        result.Should().BeFalse();
        _registry.RegisteredServiceCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that UnregisterService returns false for null or empty fullName
    /// </summary>
    [Fact]
    public void UnregisterService_WithNullOrEmptyFullName_ReturnsFalse()
    {
        // Act
        var result1 = _registry.UnregisterService(null);
        var result2 = _registry.UnregisterService("");
        var result3 = _registry.UnregisterService("   ");

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
        result3.Should().BeFalse();
    }

    /// <summary>
    /// Tests that UnregisterService successfully removes a registered service
    /// </summary>
    [Fact]
    public void UnregisterService_WithRegisteredService_RemovesService()
    {
        // Arrange
        var service = CreateTestService();
        _registry.RegisterService(service);
        _registry.ServiceExists(service.FullName).Should().BeTrue();

        // Act
        var result = _registry.UnregisterService(service.FullName);

        // Assert
        result.Should().BeTrue();
        _registry.RegisteredServiceCount.Should().Be(0);
        _registry.ServiceExists(service.FullName).Should().BeFalse();
        _registry.GetService(service.FullName).Should().BeNull();
    }

    /// <summary>
    /// Tests that ListServices returns all registered services
    /// </summary>
    [Fact]
    public void ListServices_ReturnsAllRegisteredServices()
    {
        // Arrange
        var service1 = CreateTestService("Service1", "package1", "localhost", 50051);
        var service2 = CreateTestService("Service2", "package2", "localhost", 50052);
        var service3 = CreateTestService("Service3", "package1", "localhost", 50053);

        _registry.RegisterService(service1);
        _registry.RegisterService(service2);
        _registry.RegisterService(service3);

        // Act
        var services = _registry.ListServices();

        // Assert
        services.Should().HaveCount(3);
        services.Should().Contain(s => s.FullName == service1.FullName);
        services.Should().Contain(s => s.FullName == service2.FullName);
        services.Should().Contain(s => s.FullName == service3.FullName);
    }

    /// <summary>
    /// Tests that ListServices returns empty list when no services are registered
    /// </summary>
    [Fact]
    public void ListServices_WhenNoServices_ReturnsEmptyList()
    {
        // Act
        var services = _registry.ListServices();

        // Assert
        services.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ListServicesByPackage returns services for a specific package
    /// </summary>
    [Fact]
    public void ListServicesByPackage_ReturnsServicesForPackage()
    {
        // Arrange
        var service1 = CreateTestService("Service1", "package1", "localhost", 50051);
        var service2 = CreateTestService("Service2", "package2", "localhost", 50052);
        var service3 = CreateTestService("Service3", "package1", "localhost", 50053);

        _registry.RegisterService(service1);
        _registry.RegisterService(service2);
        _registry.RegisterService(service3);

        // Act
        var package1Services = _registry.ListServicesByPackage("package1");
        var package2Services = _registry.ListServicesByPackage("package2");
        var package3Services = _registry.ListServicesByPackage("package3");

        // Assert
        package1Services.Should().HaveCount(2);
        package1Services.Should().Contain(s => s.FullName == service1.FullName);
        package1Services.Should().Contain(s => s.FullName == service3.FullName);

        package2Services.Should().HaveCount(1);
        package2Services.Should().Contain(s => s.FullName == service2.FullName);

        package3Services.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ListServicesByPackage returns empty list for null or empty packageName
    /// </summary>
    [Fact]
    public void ListServicesByPackage_WithNullOrEmptyPackageName_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateTestService();
        _registry.RegisterService(service);

        // Act
        var result1 = _registry.ListServicesByPackage(null);
        var result2 = _registry.ListServicesByPackage("");
        var result3 = _registry.ListServicesByPackage("   ");

        // Assert
        result1.Should().BeEmpty();
        result2.Should().BeEmpty();
        result3.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ServiceExists returns correct status for registered and unregistered services
    /// </summary>
    [Fact]
    public void ServiceExists_ReturnsCorrectStatus()
    {
        // Arrange
        var service = CreateTestService();
        _registry.RegisterService(service);

        // Act & Assert
        _registry.ServiceExists(service.FullName).Should().BeTrue();
        _registry.ServiceExists("nonexistent.service").Should().BeFalse();
        _registry.ServiceExists(null).Should().BeFalse();
        _registry.ServiceExists("").Should().BeFalse();
    }

    /// <summary>
    /// Tests that UpdateServiceStatus throws ServiceRegistrationException for non-existent service
    /// </summary>
    [Fact]
    public void UpdateServiceStatus_WithNonExistentService_ThrowsServiceRegistrationException()
    {
        // Act
        Action act = () => _registry.UpdateServiceStatus("nonexistent.service", ServiceStatus.NotServing);

        // Assert
        act.Should().Throw<ServiceRegistrationException>();
    }

    /// <summary>
    /// Tests that UpdateServiceStatus updates service status correctly
    /// </summary>
    [Fact]
    public void UpdateServiceStatus_UpdatesServiceStatus()
    {
        // Arrange
        var service = CreateTestService();
        _registry.RegisterService(service);

        // Verify initial status
        var initialService = _registry.GetService(service.FullName);
        initialService.Should().NotBeNull();
        initialService!.Status.Should().Be(ServiceStatus.Serving);

        // Act
        _registry.UpdateServiceStatus(service.FullName, ServiceStatus.NotServing);

        // Assert
        var updatedService = _registry.GetService(service.FullName);
        updatedService.Should().NotBeNull();
        updatedService!.Status.Should().Be(ServiceStatus.NotServing);
        updatedService.UpdatedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that GetCachedMetadata returns null for non-existent service
    /// </summary>
    [Fact]
    public void GetCachedMetadata_WithNonExistentService_ReturnsNull()
    {
        // Act
        var metadata = _registry.GetCachedMetadata("nonexistent.service");

        // Assert
        metadata.Should().BeNull();
    }

    /// <summary>
    /// Tests that GetCachedMetadata returns metadata for registered service
    /// </summary>
    [Fact]
    public void GetCachedMetadata_WithRegisteredService_ReturnsMetadata()
    {
        // Arrange
        var service = CreateTestService();
        _registry.RegisterService(service);

        // Act
        var metadata = _registry.GetCachedMetadata(service.FullName);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.ServiceName.Should().Be(service.Name);
        metadata.FullName.Should().Be(service.FullName);
        metadata.Endpoint.Should().Be(service.Endpoint);
        metadata.Port.Should().Be(service.Port);
        metadata.MethodCount.Should().Be(service.Methods.Count);
        metadata.CachedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that GetHealthStatus returns correct status for different service states
    /// </summary>
    [Fact]
    public void GetHealthStatus_ReturnsCorrectStatus()
    {
        // Arrange
        var service = CreateTestService();
        _registry.RegisterService(service);

        // Act & Assert
        _registry.GetHealthStatus(service.FullName).Should().Be(ServiceHealthStatus.Healthy);

        _registry.UpdateServiceStatus(service.FullName, ServiceStatus.NotServing);
        _registry.GetHealthStatus(service.FullName).Should().Be(ServiceHealthStatus.Unhealthy);

        _registry.UpdateServiceStatus(service.FullName, ServiceStatus.Serving);
        _registry.GetHealthStatus(service.FullName).Should().Be(ServiceHealthStatus.Healthy);
    }

    /// <summary>
    /// Tests that GetHealthStatus returns Unknown for non-existent service
    /// </summary>
    [Fact]
    public void GetHealthStatus_WithNonExistentService_ReturnsUnknown()
    {
        // Act
        var status = _registry.GetHealthStatus("nonexistent.service");

        // Assert
        status.Should().Be(ServiceHealthStatus.Unknown);
    }

    /// <summary>
    /// Tests that GetRegistrySnapshot returns correct snapshot with service count and timestamps
    /// </summary>
    [Fact]
    public void GetRegistrySnapshot_ReturnsCorrectSnapshot()
    {
        // Arrange
        var service1 = CreateTestService("Service1", "package1", "localhost", 50051);
        var service2 = CreateTestService("Service2", "package2", "localhost", 50052);

        _registry.RegisterService(service1);
        _registry.RegisterService(service2);

        // Act
        var snapshot = _registry.GetRegistrySnapshot();

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.TotalServiceCount.Should().Be(2);
        snapshot.ServiceRegistrationTimestamps.Should().HaveCount(2);
        snapshot.ServiceRegistrationTimestamps.Should().ContainKey(service1.FullName);
        snapshot.ServiceRegistrationTimestamps.Should().ContainKey(service2.FullName);
    }

    /// <summary>
    /// Tests that GetRegistrySnapshotJson returns valid JSON
    /// </summary>
    [Fact]
    public void GetRegistrySnapshotJson_ReturnsValidJson()
    {
        // Arrange
        var service = CreateTestService();
        _registry.RegisterService(service);

        // Act
        var json = _registry.GetRegistrySnapshotJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("TotalServiceCount");
        json.Should().Contain("ServiceRegistrationTimestamps");
        json.Should().Contain(service.FullName);
    }

    /// <summary>
    /// Tests snapshot isolation: mutating registry after snapshot does not change snapshot
    /// </summary>
    [Fact]
    public void GetRegistrySnapshot_IsolationTest_MutatingRegistryAfterSnapshotDoesNotChangeSnapshot()
    {
        // Arrange
        var service1 = CreateTestService("Service1", "package1", "localhost", 50051);
        var service2 = CreateTestService("Service2", "package2", "localhost", 50052);
        var service3 = CreateTestService("Service3", "package3", "localhost", 50053);

        _registry.RegisterService(service1);
        _registry.RegisterService(service2);

        // Act: Get snapshot before registering more services
        var snapshot = _registry.GetRegistrySnapshot();

        // Register another service after getting snapshot
        _registry.RegisterService(service3);

        // Assert: Snapshot should still have only 2 services
        snapshot.TotalServiceCount.Should().Be(2);
        snapshot.ServiceRegistrationTimestamps.Should().HaveCount(2);
        snapshot.ServiceRegistrationTimestamps.Should().NotContainKey(service3.FullName);

        // Verify registry now has 3 services
        _registry.RegisteredServiceCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that multiple registrations and unregistrations work correctly
    /// </summary>
    [Fact]
    public void MultipleOperations_RegisterUnregisterList_WorksCorrectly()
    {
        // Arrange
        var service1 = CreateTestService("Service1", "package1", "localhost", 50051);
        var service2 = CreateTestService("Service2", "package2", "localhost", 50052);
        var service3 = CreateTestService("Service3", "package1", "localhost", 50053);

        // Act & Assert
        _registry.RegisteredServiceCount.Should().Be(0);

        _registry.RegisterService(service1);
        _registry.RegisteredServiceCount.Should().Be(1);

        _registry.RegisterService(service2);
        _registry.RegisteredServiceCount.Should().Be(2);

        _registry.RegisterService(service3);
        _registry.RegisteredServiceCount.Should().Be(3);

        var services = _registry.ListServices();
        services.Should().HaveCount(3);

        // Unregister one service
        _registry.UnregisterService(service2.FullName);
        _registry.RegisteredServiceCount.Should().Be(2);

        services = _registry.ListServices();
        services.Should().HaveCount(2);
        services.Should().NotContain(s => s.FullName == service2.FullName);

        // Register another service
        _registry.RegisterService(service2);
        _registry.RegisteredServiceCount.Should().Be(3);
    }
}
