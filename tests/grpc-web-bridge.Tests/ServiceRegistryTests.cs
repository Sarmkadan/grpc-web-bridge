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
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceRegistryTests"/> class.
        /// </summary>
        /// <param name="_mockLogger">Mocked logger instance.</param>
        /// <param name="_serviceRegistry">Instance of the <see cref="ServiceRegistry"/>.</param>
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
        _mockLogger.LogInformation("Starting test {TestName}", nameof(RegisterService_WithValidGrpcService_AddsServiceToRegistry));
        // Arrange
        var grpcService = CreateTestService("TestService", "test.package", "localhost", 5000);

        // Act
        _serviceRegistry.RegisterService(grpcService);

        // Assert
        _serviceRegistry.ListServices().Should().Contain(grpcService);
        _serviceRegistry.RegisteredServiceCount.Should().Be(1);
        _mockLogger.LogInformation("Completed test {TestName}", nameof(RegisterService_WithValidGrpcService_AddsServiceToRegistry));
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
        _mockLogger.LogInformation("Starting test {TestName}", nameof(GetService_WithExistingServiceFullName_ReturnsService));
        // Arrange
        var grpcService = CreateTestService("ExistingService", "existing.package", "localhost", 5000);
        _serviceRegistry.RegisterService(grpcService);

        // Act
        var result = _serviceRegistry.GetService("existing.package.ExistingService");

        // Assert
        result.Should().Be(grpcService);
        _mockLogger.LogInformation("Completed test {TestName}", nameof(GetService_WithExistingServiceFullName_ReturnsService));
    }

    [Fact]
    public void GetService_WithNonExistingServiceFullName_ReturnsNull()
    {
        _mockLogger.LogInformation("Starting test {TestName}", nameof(GetService_WithNonExistingServiceFullName_ReturnsNull));
        // Arrange - No service registered

        // Act
        var result = _serviceRegistry.GetService("non.existent.Service");

        // Assert
        result.Should().BeNull();
        _mockLogger.LogInformation("Completed test {TestName}", nameof(GetService_WithNonExistingServiceFullName_ReturnsNull));
    }

    [Fact]
    public void ListServices_ReturnsAllRegisteredServices()
    {
        _mockLogger.LogInformation("Starting test {TestName}", nameof(ListServices_ReturnsAllRegisteredServices));
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
        _mockLogger.LogInformation("Completed test {TestName}", nameof(ListServices_ReturnsAllRegisteredServices));
    }

    [Fact]
    public void UnregisterService_WithExistingService_ReturnsTrueAndRemoves()
    {
        _mockLogger.LogInformation("Starting test {TestName}", nameof(UnregisterService_WithExistingService_ReturnsTrueAndRemoves));
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
        _mockLogger.LogInformation("Completed test {TestName}", nameof(UnregisterService_WithExistingService_ReturnsTrueAndRemoves));
    }

    [Fact]
    public void UnregisterService_WithNonExistingService_ReturnsFalse()
    {
        _mockLogger.LogInformation("Starting test {TestName}", nameof(UnregisterService_WithNonExistingService_ReturnsFalse));
        // Arrange - No service registered

        // Act
        var result = _serviceRegistry.UnregisterService("non.existent.Service");

        // Assert
        result.Should().BeFalse();
        _serviceRegistry.RegisteredServiceCount.Should().Be(0);
        _mockLogger.LogInformation("Completed test {TestName}", nameof(UnregisterService_WithNonExistingService_ReturnsFalse));
    }

    [Fact]
    public void ServiceExists_WithExistingService_ReturnsTrue()
    {
        _mockLogger.LogInformation("Starting test {TestName}", nameof(ServiceExists_WithExistingService_ReturnsTrue));
        // Arrange
        var grpcService = CreateTestService("ServiceCheck", "check.package", "localhost", 5000);
        _serviceRegistry.RegisterService(grpcService);

        // Act
        var result = _serviceRegistry.ServiceExists("check.package.ServiceCheck");

        // Assert
        result.Should().BeTrue();
        _mockLogger.LogInformation("Completed test {TestName}", nameof(ServiceExists_WithExistingService_ReturnsTrue));
    }

    [Fact]
    public void UpdateServiceStatus_WithExistingService_UpdatesStatus()
    {
        _mockLogger.LogInformation("Starting test {TestName}", nameof(UpdateServiceStatus_WithExistingService_UpdatesStatus));
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
        _mockLogger.LogInformation("Completed test {TestName}", nameof(UpdateServiceStatus_WithExistingService_UpdatesStatus));
    }
}