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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the <see cref="ServiceRegistryExtensions"/> class
/// </summary>
public sealed class ServiceRegistryExtensionsTests
{
    private readonly ILogger<ServiceRegistry> _logger;
    private readonly ServiceRegistry _registry;

    public ServiceRegistryExtensionsTests()
    {
        _logger = NullLogger<ServiceRegistry>.Instance;
        _registry = new ServiceRegistry(_logger);
    }

    /// <summary>
    /// Creates a test GrpcService instance with a single method attached
    /// </summary>
    private static GrpcService CreateTestService(
        string name = "TestService",
        string packageName = "test.package",
        string endpoint = "localhost",
        int port = 50051)
    {
        var service = new GrpcService(name, packageName, endpoint, port);

        var method = new GrpcMethod
        {
            Name = "TestMethod",
            FullName = $"{packageName}.{name}/TestMethod",
            Type = MethodType.Unary,
            InputMessageType = "TestRequest",
            OutputMessageType = "TestResponse"
        };
        service.AddMethod(method);

        return service;
    }

    [Fact]
    public void GetServiceOrDefault_WhenServiceExists_ReturnsService()
    {
        var service = CreateTestService();
        _registry.RegisterService(service);

        var result = _registry.GetServiceOrDefault("TestService", "test.package");

        result.Should().NotBeNull();
        result!.FullName.Should().Be(service.FullName);
    }

    [Fact]
    public void GetServiceOrDefault_WhenServiceDoesNotExist_ReturnsDefaultValue()
    {
        var fallback = CreateTestService(name: "FallbackService", packageName: "fallback.package");

        var result = _registry.GetServiceOrDefault("Missing", "no.package", fallback);

        result.Should().BeSameAs(fallback);
    }

    [Fact]
    public void GetServiceOrDefault_WhenServiceDoesNotExistAndNoDefault_ReturnsNull()
    {
        var result = _registry.GetServiceOrDefault("Missing", "no.package");

        result.Should().BeNull();
    }

    [Fact]
    public void GetServiceOrDefault_WithNullRegistry_ThrowsArgumentNullException()
    {
        ServiceRegistry? registry = null;

        Action act = () => registry!.GetServiceOrDefault("Name", "pkg");

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null, "pkg")]
    [InlineData("", "pkg")]
    [InlineData("name", null)]
    [InlineData("name", "")]
    public void GetServiceOrDefault_WithNullOrEmptyArguments_ThrowsArgumentException(
        string? serviceName, string? packageName)
    {
        Action act = () => _registry.GetServiceOrDefault(serviceName!, packageName!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetServicesByEndpoint_WhenMatchingServicesExist_ReturnsThemCaseInsensitively()
    {
        var service = CreateTestService(name: "SvcA", endpoint: "10.0.0.1");
        _registry.RegisterService(service);

        var results = _registry.GetServicesByEndpoint("10.0.0.1").ToList();

        results.Should().ContainSingle();
        results[0].FullName.Should().Be(service.FullName);
    }

    [Fact]
    public void GetServicesByEndpoint_WhenNoServicesMatch_ReturnsEmptyCollection()
    {
        _registry.RegisterService(CreateTestService(name: "SvcB", endpoint: "10.0.0.2"));

        var results = _registry.GetServicesByEndpoint("192.168.1.1");

        results.Should().BeEmpty();
    }

    [Fact]
    public void GetServicesByEndpoint_WithEmptyEndpoint_ThrowsArgumentException()
    {
        Action act = () => _registry.GetServicesByEndpoint(string.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HasServiceWithHealthStatus_WhenNoServicesRegistered_ReturnsFalse()
    {
        var result = _registry.HasServiceWithHealthStatus(ServiceHealthStatus.Healthy);

        result.Should().BeFalse();
    }

    [Fact]
    public void HasServiceWithHealthStatus_WhenMatchingServiceRegistered_ReturnsTrue()
    {
        var service = CreateTestService(name: "SvcC");
        _registry.RegisterService(service);

        var result = _registry.HasServiceWithHealthStatus(ServiceHealthStatus.Healthy);

        result.Should().BeTrue();
    }

    [Fact]
    public void GetServicesByPackageDictionary_WhenRegistryEmpty_ReturnsEmptyDictionary()
    {
        var result = _registry.GetServicesByPackageDictionary();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetServicesByPackageDictionary_WithMultipleServices_GroupsByPackageName()
    {
        _registry.RegisterService(CreateTestService(name: "Svc1", packageName: "pkg.one"));
        _registry.RegisterService(CreateTestService(name: "Svc2", packageName: "pkg.one"));
        _registry.RegisterService(CreateTestService(name: "Svc3", packageName: "pkg.two"));

        var result = _registry.GetServicesByPackageDictionary();

        result.Should().HaveCount(2);
        result["pkg.one"].Should().HaveCount(2);
        result["pkg.two"].Should().HaveCount(1);
    }
}
