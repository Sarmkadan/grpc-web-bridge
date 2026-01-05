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

namespace GrpcWebBridge.Tests;

/// <summary>
/// Extension methods for ServiceRegistryTests to provide additional test utilities
/// </summary>
public static class ServiceRegistryTestsExtensions
{
    /// <summary>
    /// Creates a test ServiceRegistry instance for testing
    /// </summary>
    /// <param name="tests">The test instance</param>
    /// <returns>New ServiceRegistry instance</returns>
    public static ServiceRegistry CreateTestRegistry(this ServiceRegistryTests tests)
    {
        return new ServiceRegistry(Substitute.For<ILogger<ServiceRegistry>>());
    }

    /// <summary>
    /// Creates and registers a test service with default values
    /// </summary>
    /// <param name="tests">The test instance</param>
    /// <param name="name">Service name</param>
    /// <param name="packageName">Package name</param>
    /// <returns>The created and registered service</returns>
    public static GrpcService CreateAndRegisterTestService(
        this ServiceRegistryTests tests,
        string name,
        string packageName)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(packageName);

        var registry = tests.CreateTestRegistry();

        var service = new GrpcService(
            name,
            packageName,
            "localhost",
            50051)
        {
            Status = ServiceStatus.Serving
        };

        service.AddMethod(new GrpcMethod(
            "TestMethod",
            $"{packageName}.{name}.TestMethod",
            MethodType.Unary,
            "InputMessage",
            "OutputMessage"));

        registry.RegisterService(service);
        return service;
    }

    /// <summary>
    /// Creates and registers multiple test services with sequential numbering
    /// </summary>
    /// <param name="tests">The test instance</param>
    /// <param name="count">Number of services to create</param>
    /// <param name="packageName">Base package name</param>
    /// <returns>Read-only list of created services</returns>
    public static IReadOnlyList<GrpcService> CreateAndRegisterTestServices(
        this ServiceRegistryTests tests,
        int count,
        string packageName)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive");
        }

        ArgumentNullException.ThrowIfNull(packageName);

        var services = new List<GrpcService>(count);
        for (var i = 0; i < count; i++)
        {
            var service = tests.CreateAndRegisterTestService(
                $"Service{i + 1}",
                packageName);
            services.Add(service);
        }

        return services.AsReadOnly();
    }

    /// <summary>
    /// Gets a service from the registry or throws if not found
    /// </summary>
    /// <param name="tests">The test instance</param>
    /// <param name="registry">ServiceRegistry instance</param>
    /// <param name="fullName">Full service name to check</param>
    /// <returns>The service if found</returns>
    /// <exception cref="InvalidOperationException">Thrown if service not found</exception>
    public static GrpcService GetServiceOrThrow(
        this ServiceRegistryTests tests,
        ServiceRegistry registry,
        string fullName)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrEmpty(fullName);

        var service = registry.GetService(fullName);
        return service ?? throw new InvalidOperationException($"Service '{fullName}' not found in registry");
    }

    /// <summary>
    /// Checks if a service exists in the registry
    /// </summary>
    /// <param name="tests">The test instance</param>
    /// <param name="registry">ServiceRegistry instance</param>
    /// <param name="fullName">Full service name to check</param>
    /// <returns>True if service exists</returns>
    public static bool ServiceExists(
        this ServiceRegistryTests tests,
        ServiceRegistry registry,
        string fullName)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrEmpty(fullName);

        return registry.ServiceExists(fullName);
    }

    /// <summary>
    /// Updates service status and returns the updated service
    /// </summary>
    /// <param name="tests">The test instance</param>
    /// <param name="registry">ServiceRegistry instance</param>
    /// <param name="fullName">Full service name</param>
    /// <param name="status">New status to set</param>
    /// <returns>The updated service</returns>
    public static GrpcService UpdateAndGetServiceStatus(
        this ServiceRegistryTests tests,
        ServiceRegistry registry,
        string fullName,
        ServiceStatus status)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrEmpty(fullName);

        registry.UpdateServiceStatus(fullName, status);
        return tests.GetServiceOrThrow(registry, fullName);
    }

    /// <summary>
    /// Gets the health status of a service
    /// </summary>
    /// <param name="tests">The test instance</param>
    /// <param name="registry">ServiceRegistry instance</param>
    /// <param name="fullName">Full service name</param>
    /// <returns>Service health status</returns>
    public static ServiceHealthStatus GetServiceHealthStatus(
        this ServiceRegistryTests tests,
        ServiceRegistry registry,
        string fullName)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrEmpty(fullName);

        return registry.GetHealthStatus(fullName);
    }

    /// <summary>
    /// Lists all services by package name
    /// </summary>
    /// <param name="tests">The test instance</param>
    /// <param name="registry">ServiceRegistry instance</param>
    /// <param name="packageName">Package name to filter by</param>
    /// <returns>Read-only list of services in package</returns>
    public static IReadOnlyList<GrpcService> ListServicesByPackage(
        this ServiceRegistryTests tests,
        ServiceRegistry registry,
        string packageName)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrEmpty(packageName);

        var services = registry.ListServicesByPackage(packageName).ToList();
        return services.AsReadOnly();
    }
}
