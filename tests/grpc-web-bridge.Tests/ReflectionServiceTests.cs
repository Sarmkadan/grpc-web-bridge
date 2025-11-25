#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GrpcWebBridge.Tests;

public sealed class ReflectionServiceTests
{
    private readonly ILogger<ReflectionService> _mockReflectionLogger;
    private readonly ILogger<ServiceRegistry> _mockServiceRegistryLogger;
    private readonly ServiceRegistry _serviceRegistry; // Real instance
    private readonly ReflectionService _reflectionService;

    public ReflectionServiceTests()
    {
        _mockReflectionLogger = Substitute.For<ILogger<ReflectionService>>();
        _mockServiceRegistryLogger = Substitute.For<ILogger<ServiceRegistry>>();
        _serviceRegistry = new ServiceRegistry(_mockServiceRegistryLogger); // Use real instance
        _reflectionService = new ReflectionService(_mockReflectionLogger, _serviceRegistry);
    }

    private GrpcService CreateTestService(string name, string packageName, string endpoint, int port)
    {
        var service = new GrpcService(name, packageName, endpoint, port);
        // Add a dummy method to satisfy GrpcService.Validate() which requires at least one method
        service.AddMethod(new GrpcMethod("DummyMethod", $"{packageName}.{name}.DummyMethod", MethodType.Unary, "InputType", "OutputType"));
        return service;
    }

    [Fact]
    public async Task ListServiceNamesAsync_WhenServicesExist_ReturnsOrderedNames()
    {
        // Arrange
        var service1 = CreateTestService("ServiceA", "package.a", "host.a", 1111);
        var service2 = CreateTestService("ServiceB", "package.b", "host.b", 2222);
        _serviceRegistry.RegisterService(service1);
        _serviceRegistry.RegisterService(service2);

        // Act
        var result = await _reflectionService.ListServiceNamesAsync().ConfigureAwait(false);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEquivalentTo(new List<string> { "package.a.ServiceA", "package.b.ServiceB" }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ListServiceNamesAsync_WhenNoServicesExist_ReturnsEmptyList()
    {
        // Arrange - No services registered in _serviceRegistry

        // Act
        var result = await _reflectionService.ListServiceNamesAsync().ConfigureAwait(false);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }


    [Fact]
    public async Task GetServiceDescriptorAsync_ForExistingService_ReturnsDescriptor()
    {
        // Arrange
        var service = CreateTestService("ServiceA", "package.a", "host.a", 1111);
        var method1 = new GrpcMethod("Method1", "package.a.ServiceA.Method1", MethodType.Unary, "InputA", "OutputA");
        service.AddMethod(method1); // Add a specific method for the test
        _serviceRegistry.RegisterService(service);

        // Act
        var result = await _reflectionService.GetServiceDescriptorAsync("package.a.ServiceA").ConfigureAwait(false);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FullName.Should().Be("package.a.ServiceA");
        result.Data!.Methods.Should().ContainSingle(m => m.Name == "Method1");
    }

    [Fact]
    public async Task GetServiceDescriptorAsync_ForNonExistingService_ReturnsFailure()
    {
        // Arrange - No service registered

        // Act
        var result = await _reflectionService.GetServiceDescriptorAsync("non.existent.Service").ConfigureAwait(false);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not registered");
    }

    [Fact]
    public async Task GetServiceDescriptorAsync_WithNullOrEmptyFullName_ThrowsArgumentException()
    {
        // Arrange
        // No setup needed

        // Act
        Func<Task> act = async () => await _reflectionService.GetServiceDescriptorAsync(null!).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("fullName");
    }

    [Fact]
    public async Task GetMethodDescriptorAsync_ForExistingMethod_ReturnsDescriptor()
    {
        // Arrange
        var service = CreateTestService("ServiceA", "package.a", "host.a", 1111);
        var method1 = new GrpcMethod("Method1", "package.a.ServiceA.Method1", MethodType.Unary, "InputA", "OutputA");
        service.AddMethod(method1);
        _serviceRegistry.RegisterService(service);

        // Act
        var result = await _reflectionService.GetMethodDescriptorAsync("package.a.ServiceA", "Method1").ConfigureAwait(false);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Method1");
        result.Data.FullName.Should().Be("package.a.ServiceA.Method1");
    }

    [Fact]
    public async Task GetMethodDescriptorAsync_ForNonExistingMethod_ReturnsFailure()
    {
        // Arrange
        var service = CreateTestService("ServiceA", "package.a", "host.a", 1111);
        _serviceRegistry.RegisterService(service);

        // Act
        var result = await _reflectionService.GetMethodDescriptorAsync("package.a.ServiceA", "NonExistentMethod").ConfigureAwait(false);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Method 'NonExistentMethod' not found");
    }

    [Fact]
    public async Task GetMethodDescriptorAsync_WithNullOrEmptyServiceFullName_ThrowsArgumentException()
    {
        // Arrange
        // No setup needed

        // Act
        Func<Task> act = async () => await _reflectionService.GetMethodDescriptorAsync(null!, "Method").ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("serviceFullName");
    }

    [Fact]
    public async Task GetMethodDescriptorAsync_WithNullOrEmptyMethodName_ThrowsArgumentException()
    {
        // Arrange
        // No setup needed

        // Act
        Func<Task> act = async () => await _reflectionService.GetMethodDescriptorAsync("package.Service", null!).ConfigureAwait(false);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("methodName");
    }

    [Fact]
    public async Task GetAllDescriptorsAsync_WhenServicesExist_ReturnsAllDescriptors()
    {
        // Arrange
        var serviceA = CreateTestService("ServiceA", "package.a", "host.a", 1111);
        var methodA1 = new GrpcMethod("MethodA1", "package.a.ServiceA.MethodA1", MethodType.Unary, "InputA1", "OutputA1");
        serviceA.AddMethod(methodA1);

        var serviceB = CreateTestService("ServiceB", "package.b", "host.b", 2222);
        var methodB1 = new GrpcMethod("MethodB1", "package.b.ServiceB.MethodB1", MethodType.Unary, "InputB1", "OutputB1");
        serviceB.AddMethod(methodB1);

        _serviceRegistry.RegisterService(serviceA);
        _serviceRegistry.RegisterService(serviceB);

        // Act
        var result = await _reflectionService.GetAllDescriptorsAsync().ConfigureAwait(false);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
        result.Data.Should().Contain(d => d.FullName == "package.a.ServiceA");
        result.Data.Should().Contain(d => d.FullName == "package.b.ServiceB");
    }

    [Fact]
    public async Task GetAllDescriptorsAsync_WhenNoServicesExist_ReturnsEmptyList()
    {
        // Arrange - No services registered in _serviceRegistry

        // Act
        var result = await _reflectionService.GetAllDescriptorsAsync().ConfigureAwait(false);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
