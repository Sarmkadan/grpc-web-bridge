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
using NSubstitute;
using Xunit;
using System.Collections.Generic;

namespace GrpcWebBridge.Tests;

public sealed class ServiceRegistryTests
{
    private readonly ILogger<ServiceRegistry> _mockLogger;
    private readonly ServiceRegistry _serviceRegistry;

    public ServiceRegistryTests()
    {
        _mockLogger = Substitute.For<ILogger<ServiceRegistry>>();
        _serviceRegistry = new ServiceRegistry(_mockLogger);
    }

    private GrpcService CreateTestService(string name, string packageName, string endpoint, int port)
    {
        var service = new GrpcService(name, packageName, endpoint, port);
        service.AddMethod(new GrpcMethod("TestMethod", $"{packageName}.{name}.TestMethod", MethodType.Unary, "Input", "Output"));
        return service;
    }

    [Fact]
    public void RegisterService_WithValidGrpcService_AddsServiceToRegistry()
    {
        // Arrange
        var grpcService = CreateTestService("TestService", "test.package", "localhost", 5000);

        // Act
        _serviceRegistry.RegisterService(grpcService);

        // Assert
        _serviceRegistry.ListServices().Should().Contain(grpcService);
        _serviceRegistry.RegisteredServiceCount.Should().Be(1);
    }

    [Fact]
    public void RegisterService_WithDuplicateService_ThrowsServiceRegistrationException()
    {
        // Arrange
        var grpcService = CreateTestService("TestService", "test.package", "localhost", 5000);
        _serviceRegistry.RegisterService(grpcService); // Register once

        // Act
        Action act = () => _serviceRegistry.RegisterService(grpcService); // Register again

        // Assert
        act.Should().Throw<ServiceRegistrationException>()
           .Which.Message.Should().Contain("Service already registered");
        _serviceRegistry.RegisteredServiceCount.Should().Be(1);
        _mockLogger.Received(1).Log(LogLevel.Information, Arg.Any<EventId>(), Arg.Any<IReadOnlyList<KeyValuePair<string, object>>>(), null, Arg.Any<Func<IReadOnlyList<KeyValuePair<string, object>>, Exception?, string>>()); // Should log the initial registration
    }

    [Fact]
    public void GetService_WithExistingServiceFullName_ReturnsService()
    {
        // Arrange
        var grpcService = CreateTestService("ExistingService", "existing.package", "localhost", 5000);
        _serviceRegistry.RegisterService(grpcService);

        // Act
        var result = _serviceRegistry.GetService("existing.package.ExistingService");

        // Assert
        result.Should().Be(grpcService);
    }

    [Fact]
    public void GetService_WithNonExistingServiceFullName_ReturnsNull()
    {
        // Arrange - No service registered

        // Act
        var result = _serviceRegistry.GetService("non.existent.Service");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ListServices_ReturnsAllRegisteredServices()
    {
        // Arrange
        var service1 = CreateTestService("Service1", "package.one", "host.one", 1111);
        var service2 = CreateTestService("Service2", "package.two", "host.two", 2222);
        _serviceRegistry.RegisterService(service1);
        _serviceRegistry.RegisterService(service2);

        // Act
        var allServices = _serviceRegistry.ListServices();

        // Assert
        allServices.Should().HaveCount(2);
        allServices.Should().Contain(service1);
        allServices.Should().Contain(service2);
    }

    [Fact]
    public void UnregisterService_WithExistingService_ReturnsTrueAndRemoves()
    {
        // Arrange
        var grpcService = CreateTestService("ServiceToRemove", "remove.package", "localhost", 5000);
        _serviceRegistry.RegisterService(grpcService);
        _serviceRegistry.RegisteredServiceCount.Should().Be(1);

        // Act
        var result = _serviceRegistry.UnregisterService("remove.package.ServiceToRemove");

        // Assert
        result.Should().BeTrue();
        _serviceRegistry.RegisteredServiceCount.Should().Be(0);
        _serviceRegistry.GetService("remove.package.ServiceToRemove").Should().BeNull();
    }

    [Fact]
    public void UnregisterService_WithNonExistingService_ReturnsFalse()
    {
        // Arrange - No service registered

        // Act
        var result = _serviceRegistry.UnregisterService("non.existent.Service");

        // Assert
        result.Should().BeFalse();
        _serviceRegistry.RegisteredServiceCount.Should().Be(0);
    }

    [Fact]
    public void ServiceExists_WithExistingService_ReturnsTrue()
    {
        // Arrange
        var grpcService = CreateTestService("ServiceCheck", "check.package", "localhost", 5000);
        _serviceRegistry.RegisterService(grpcService);

        // Act
        var result = _serviceRegistry.ServiceExists("check.package.ServiceCheck");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void UpdateServiceStatus_WithExistingService_UpdatesStatus()
    {
        // Arrange
        var grpcService = CreateTestService("ServiceStatus", "status.package", "localhost", 5000);
        _serviceRegistry.RegisterService(grpcService);
        grpcService.Status.Should().Be(ServiceStatus.Serving);

        // Act
        _serviceRegistry.UpdateServiceStatus("status.package.ServiceStatus", ServiceStatus.NotServing);

        // Assert
        var updatedService = _serviceRegistry.GetService("status.package.ServiceStatus");
        updatedService!.Status.Should().Be(ServiceStatus.NotServing);
        updatedService.UpdatedAt.Should().NotBeNull();
    }
}
