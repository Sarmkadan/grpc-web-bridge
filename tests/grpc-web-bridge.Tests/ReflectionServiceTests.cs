#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

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

/// <summary>
/// Unit tests for the <see cref="ReflectionService"/> class that provides reflection capabilities for gRPC services.
/// Tests service discovery, descriptor generation, and reflection operations.
/// </summary>
public sealed class ReflectionServiceTests
{
	/// <summary>
	/// Mock logger for <see cref="ReflectionService"/> used to verify logging behavior.
	/// </summary>
	private readonly ILogger<ReflectionService> _mockReflectionLogger;

	/// <summary>
	/// Mock logger for <see cref="ServiceRegistry"/> used to verify logging behavior.
	/// </summary>
	private readonly ILogger<ServiceRegistry> _mockServiceRegistryLogger;

	/// <summary>
	/// Real instance of <see cref="ServiceRegistry"/> used to register and manage gRPC services.
	/// </summary>
	private readonly ServiceRegistry _serviceRegistry; // Real instance

	/// <summary>
	/// Instance of <see cref="ReflectionService"/> under test that provides reflection capabilities.
	/// </summary>
	private readonly ReflectionService _reflectionService;

	public ReflectionServiceTests()
	{
		_mockReflectionLogger = Substitute.For<ILogger<ReflectionService>>();
		_mockServiceRegistryLogger = Substitute.For<ILogger<ServiceRegistry>>();
		_serviceRegistry = new ServiceRegistry(_mockServiceRegistryLogger); // Use real instance
		_reflectionService = new ReflectionService(_mockReflectionLogger, _serviceRegistry);
	}

	/// <summary>
	/// Creates a test <see cref="GrpcService"/> instance for testing purposes.
	/// </summary>
	/// <param name="name">The name of the service.</param>
	/// <param name="packageName">The package name for the service.</param>
	/// <param name="endpoint">The endpoint address for the service.</param>
	/// <param name="port">The port number for the service.</param>
	/// <returns>A configured <see cref="GrpcService"/> instance with a dummy method added.</returns>
	private GrpcService CreateTestService(string name, string packageName, string endpoint, int port)
	{
		var service = new GrpcService(name, packageName, endpoint, port);
		// Add a dummy method to satisfy GrpcService.Validate() which requires at least one method
		service.AddMethod(new GrpcMethod("DummyMethod", $"{packageName}.{name}.DummyMethod", MethodType.Unary, "InputType", "OutputType"));
		return service;
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.ListServiceNamesAsync"/> returns service names in alphabetical order when services are registered.
	/// </summary>
	public async Task ListServiceNamesAsync_WhenServicesExist_ReturnsOrderedNames()
	{
		_mockReflectionLogger.LogInformation("ListServiceNamesAsync_WhenServicesExist_ReturnsOrderedNames called");
		// Arrange
		var service1 = CreateTestService("ServiceA", "package.a", "host.a", 1111);
		var service2 = CreateTestService("ServiceB", "package.b", "host.b", 2222);
		_serviceRegistry.RegisterService(service1);
		_serviceRegistry.RegisterService(service2);

		// Act
		var result = await _reflectionService.ListServiceNamesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Data.Should().BeEquivalentTo(new List<string> { "package.a.ServiceA", "package.b.ServiceB" }, options => options.WithStrictOrdering());
		_mockReflectionLogger.LogInformation("ListServiceNamesAsync_WhenServicesExist_ReturnsOrderedNames finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.ListServiceNamesAsync"/> returns an empty list when no services are registered.
	/// </summary>
	public async Task ListServiceNamesAsync_WhenNoServicesExist_ReturnsEmptyList()
	{
		_mockReflectionLogger.LogInformation("ListServiceNamesAsync_WhenNoServicesExist_ReturnsEmptyList called");
		// Arrange - No services registered in _serviceRegistry

		// Act
		var result = await _reflectionService.ListServiceNamesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Data.Should().BeEmpty();
		_mockReflectionLogger.LogInformation("ListServiceNamesAsync_WhenNoServicesExist_ReturnsEmptyList finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetServiceDescriptorAsync"/> returns a valid service descriptor when the service exists.
	/// </summary>
	public async Task GetServiceDescriptorAsync_ForExistingService_ReturnsDescriptor()
	{
		_mockReflectionLogger.LogInformation("GetServiceDescriptorAsync_ForExistingService_ReturnsDescriptor called");
		// Arrange
		var service = CreateTestService("ServiceA", "package.a", "host.a", 1111);
		var method1 = new GrpcMethod("Method1", "package.a.ServiceA.Method1", MethodType.Unary, "InputA", "OutputA");
		service.AddMethod(method1); // Add a specific method for the test
		_serviceRegistry.RegisterService(service);

		// Act
		var result = await _reflectionService.GetServiceDescriptorAsync("package.a.ServiceA");

		// Assert
		result.Success.Should().BeTrue();
		result.Data.Should().NotBeNull();
		result.Data!.FullName.Should().Be("package.a.ServiceA");
		result.Data!.Methods.Should().ContainSingle(m => m.Name == "Method1");
		_mockReflectionLogger.LogInformation("GetServiceDescriptorAsync_ForExistingService_ReturnsDescriptor finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetServiceDescriptorAsync"/> returns a failure result when the service does not exist.
	/// </summary>
	public async Task GetServiceDescriptorAsync_ForNonExistingService_ReturnsFailure()
	{
		_mockReflectionLogger.LogInformation("GetServiceDescriptorAsync_ForNonExistingService_ReturnsFailure called");
		// Arrange - No service registered

		// Act
		var result = await _reflectionService.GetServiceDescriptorAsync("non.existent.Service");

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorMessage.Should().Contain("not registered");
		_mockReflectionLogger.LogInformation("GetServiceDescriptorAsync_ForNonExistingService_ReturnsFailure finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetServiceDescriptorAsync"/> throws an <see cref="ArgumentException"/> when the fullName parameter is null or empty.
	/// </summary>
	public async Task GetServiceDescriptorAsync_WithNullOrEmptyFullName_ThrowsArgumentException()
	{
		_mockReflectionLogger.LogInformation("GetServiceDescriptorAsync_WithNullOrEmptyFullName_ThrowsArgumentException called");
		// Arrange
		// No setup needed

		// Act
		Func<Task> act = async () => await _reflectionService.GetServiceDescriptorAsync(null!);

		// Assert
		await act.Should().ThrowAsync<ArgumentException>()
			.WithParameterName("fullName");
		_mockReflectionLogger.LogInformation("GetServiceDescriptorAsync_WithNullOrEmptyFullName_ThrowsArgumentException finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetMethodDescriptorAsync"/> returns a valid method descriptor when the method exists.
	/// </summary>
	public async Task GetMethodDescriptorAsync_ForExistingMethod_ReturnsDescriptor()
	{
		_mockReflectionLogger.LogInformation("GetMethodDescriptorAsync_ForExistingMethod_ReturnsDescriptor called");
		// Arrange
		var service = CreateTestService("ServiceA", "package.a", "host.a", 1111);
		var method1 = new GrpcMethod("Method1", "package.a.ServiceA.Method1", MethodType.Unary, "InputA", "OutputA");
		service.AddMethod(method1);
		_serviceRegistry.RegisterService(service);

		// Act
		var result = await _reflectionService.GetMethodDescriptorAsync("package.a.ServiceA", "Method1");

		// Assert
		result.Success.Should().BeTrue();
		result.Data.Should().NotBeNull();
		result.Data!.Name.Should().Be("Method1");
		result.Data.FullName.Should().Be("package.a.ServiceA.Method1");
		_mockReflectionLogger.LogInformation("GetMethodDescriptorAsync_ForExistingMethod_ReturnsDescriptor finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetMethodDescriptorAsync"/> returns a failure result when the method does not exist.
	/// </summary>
	public async Task GetMethodDescriptorAsync_ForNonExistingMethod_ReturnsFailure()
	{
		_mockReflectionLogger.LogInformation("GetMethodDescriptorAsync_ForNonExistingMethod_ReturnsFailure called");
		// Arrange
		var service = CreateTestService("ServiceA", "package.a", "host.a", 1111);
		_serviceRegistry.RegisterService(service);

		// Act
		var result = await _reflectionService.GetMethodDescriptorAsync("package.a.ServiceA", "NonExistentMethod");

		// Assert
		result.Success.Should().BeFalse();
		result.ErrorMessage.Should().Contain("Method 'NonExistentMethod' not found");
		_mockReflectionLogger.LogInformation("GetMethodDescriptorAsync_ForNonExistingMethod_ReturnsFailure finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetMethodDescriptorAsync"/> throws an <see cref="ArgumentException"/> when the serviceFullName parameter is null or empty.
	/// </summary>
	public async Task GetMethodDescriptorAsync_WithNullOrEmptyServiceFullName_ThrowsArgumentException()
	{
		_mockReflectionLogger.LogInformation("GetMethodDescriptorAsync_WithNullOrEmptyServiceFullName_ThrowsArgumentException called");
		// Arrange
		// No setup needed

		// Act
		Func<Task> act = async () => await _reflectionService.GetMethodDescriptorAsync(null!, "Method");

		// Assert
		await act.Should().ThrowAsync<ArgumentException>()
			.WithParameterName("serviceFullName");
		_mockReflectionLogger.LogInformation("GetMethodDescriptorAsync_WithNullOrEmptyServiceFullName_ThrowsArgumentException finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetMethodDescriptorAsync"/> throws an <see cref="ArgumentException"/> when the methodName parameter is null or empty.
	/// </summary>
	public async Task GetMethodDescriptorAsync_WithNullOrEmptyMethodName_ThrowsArgumentException()
	{
		_mockReflectionLogger.LogInformation("GetMethodDescriptorAsync_WithNullOrEmptyMethodName_ThrowsArgumentException called");
		// Arrange
		// No setup needed

		// Act
		Func<Task> act = async () => await _reflectionService.GetMethodDescriptorAsync("package.Service", null!);

		// Assert
		await act.Should().ThrowAsync<ArgumentException>()
			.WithParameterName("methodName");
		_mockReflectionLogger.LogInformation("GetMethodDescriptorAsync_WithNullOrEmptyMethodName_ThrowsArgumentException finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetAllDescriptorsAsync"/> returns all service descriptors when services are registered.
	/// </summary>
	public async Task GetAllDescriptorsAsync_WhenServicesExist_ReturnsAllDescriptors()
	{
		_mockReflectionLogger.LogInformation("GetAllDescriptorsAsync_WhenServicesExist_ReturnsAllDescriptors called");
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
		var result = await _reflectionService.GetAllDescriptorsAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Data.Should().HaveCount(2);
		result.Data.Should().Contain(d => d.FullName == "package.a.ServiceA");
		result.Data.Should().Contain(d => d.FullName == "package.b.ServiceB");
		_mockReflectionLogger.LogInformation("GetAllDescriptorsAsync_WhenServicesExist_ReturnsAllDescriptors finished");
	}

	[Fact]
	/// <summary>
	/// Tests that <see cref="ReflectionService.GetAllDescriptorsAsync"/> returns an empty list when no services are registered.
	/// </summary>
	public async Task GetAllDescriptorsAsync_WhenNoServicesExist_ReturnsEmptyList()
	{
		_mockReflectionLogger.LogInformation("GetAllDescriptorsAsync_WhenNoServicesExist_ReturnsEmptyList called");
		// Arrange - No services registered in _serviceRegistry

		// Act
		var result = await _reflectionService.GetAllDescriptorsAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Data.Should().BeEmpty();
		_mockReflectionLogger.LogInformation("GetAllDescriptorsAsync_WhenNoServicesExist_ReturnsEmptyList finished");
	}
}