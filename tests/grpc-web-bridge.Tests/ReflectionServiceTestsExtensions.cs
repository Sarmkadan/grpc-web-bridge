#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Provides extension methods for <see cref="ReflectionServiceTests"/> to facilitate common testing scenarios
/// and assertions when working with gRPC reflection services.
/// </summary>
public static class ReflectionServiceTestsExtensions
{
    /// <summary>
    /// Creates and registers a test service with the specified configuration.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="name">The service name.</param>
    /// <param name="packageName">The package name.</param>
    /// <param name="endpoint">The endpoint address.</param>
    /// <param name="port">The port number.</param>
    /// <returns>A configured <see cref="GrpcService"/> instance ready for registration.</returns>
    /// <exception cref="ArgumentException">Thrown when name, packageName, or endpoint is null or empty.</exception>
    public static GrpcService CreateAndRegisterTestService(
        this ReflectionServiceTests tests,
        string name,
        string packageName,
        string endpoint,
        int port)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(packageName);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        var service = new GrpcService(name, packageName, endpoint, port);
        service.AddMethod(new GrpcMethod(
            "DummyMethod",
            $"{packageName}.{name}.DummyMethod",
            Domain.MethodType.Unary,
            "InputType",
            "OutputType"));

        tests.GetServiceRegistry().RegisterService(service);
        return service;
    }

    /// <summary>
    /// Gets the service registry instance from the test class.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <returns>The <see cref="ServiceRegistry"/> instance used by the test.</returns>
    public static ServiceRegistry GetServiceRegistry(this ReflectionServiceTests tests)
    {
        return tests.GetFieldValue<ServiceRegistry>("_serviceRegistry");
    }

    /// <summary>
    /// Gets the reflection service instance from the test class.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <returns>The <see cref="ReflectionService"/> instance used by the test.</returns>
    public static ReflectionService GetReflectionService(this ReflectionServiceTests tests)
    {
        return tests.GetFieldValue<ReflectionService>("_reflectionService");
    }

    /// <summary>
    /// Creates a test service with multiple methods for comprehensive testing.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <param name="name">The service name.</param>
    /// <param name="packageName">The package name.</param>
    /// <param name="endpoint">The endpoint address.</param>
    /// <param name="port">The port number.</param>
    /// <param name="methodDefinitions">Collection of method definitions to add.</param>
    /// <returns>A configured <see cref="GrpcService"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static GrpcService CreateTestServiceWithMethods(
        this ReflectionServiceTests tests,
        string name,
        string packageName,
        string endpoint,
        int port,
        IReadOnlyCollection<(string MethodName, string InputType, string OutputType, MethodType Type)> methodDefinitions)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(packageName);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNull(methodDefinitions);

        var service = new GrpcService(name, packageName, endpoint, port);

        foreach (var (methodName, inputType, outputType, methodType) in methodDefinitions)
        {
            service.AddMethod(new GrpcMethod(methodName, $"{packageName}.{name}.{methodName}", methodType, inputType, outputType));
        }

        return service;
    }

    /// <summary>
    /// Verifies that a service is properly registered in the service registry.
    /// </summary>
    /// <param name="registry">The service registry.</param>
    /// <param name="serviceFullName">The full name of the service to verify.</param>
    /// <returns>True if the service is registered; otherwise false.</returns>
    /// <exception cref="ArgumentException">Thrown when serviceFullName is null or empty.</exception>
    public static bool IsServiceRegistered(
        this ServiceRegistry registry,
        string serviceFullName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceFullName);
        return registry.GetAllServices().Any(s => s.FullName.Equals(serviceFullName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets all registered services from the service registry.
    /// </summary>
    /// <param name="registry">The service registry.</param>
    /// <returns>A read-only collection of all registered services.</returns>
    /// <exception cref="ArgumentNullException">Thrown when registry is null.</exception>
    public static IEnumerable<GrpcService> GetAllServices(this ServiceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.ListServices();
    }

    /// <summary>
    /// Gets the field value from a test instance using reflection.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="tests">The test instance.</param>
    /// <param name="fieldName">The name of the field to retrieve.</param>
    /// <returns>The field value.</returns>
    /// <exception cref="ArgumentException">Thrown when fieldName is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when tests is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the field is not found or cannot be accessed.</exception>
    private static T GetFieldValue<T>(this ReflectionServiceTests tests, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        var field = typeof(ReflectionServiceTests).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);

        return field is null
            ? throw new InvalidOperationException($"Field '{fieldName}' not found in ReflectionServiceTests")
            : (T)field.GetValue(tests)!;
    }
}