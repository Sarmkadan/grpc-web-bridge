#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Data;
using GrpcWebBridge.Domain; // For MethodType
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the ServiceRepository class.
/// </summary>
public sealed class ServiceRepositoryTests
{
    private readonly ILogger<ServiceRepository> _logger;
    private readonly ServiceRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRepositoryTests"/> class.
    /// </summary>
    public ServiceRepositoryTests()
    {
        _logger = Substitute.For<ILogger<ServiceRepository>>();
        _repository = new ServiceRepository(_logger);
    }

    /// <summary>
    /// Creates a valid GrpcService instance with the specified name, package name, endpoint, and port.
    /// </summary>
    /// <param name="name">The name of the service.</param>
    /// <param name="packageName">The package name of the service.</param>
    /// <param name="endpoint">The endpoint of the service.</param>
    /// <param name="port">The port of the service.</param>
    /// <returns>A valid GrpcService instance.</returns>
    private GrpcService CreateValidGrpcService(string name, string packageName, string endpoint, int port)
    {
        var service = new GrpcService(name, packageName, endpoint, port);
        service.AddMethod(new GrpcMethod("DummyMethod", $"{packageName}.{name}.DummyMethod", MethodType.Unary, "InputType", "OutputType"));
        return service;
    }

    /// <summary>
    /// Tests that adding a new service returns true and stores the service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_WithNewService_ReturnsTrueAndStoresService()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(AddAsync_WithNewService_ReturnsTrueAndStoresService));
        // Arrange
        var service = CreateValidGrpcService("TestService", "test.package", "localhost", 5000);

        // Act
        var result = await _repository.AddAsync(service);
        var retrieved = await _repository.GetByIdAsync(service.Id);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.FullName.Should().Be("test.package.TestService");
        retrieved.Name.Should().Be("TestService");

        _logger.LogInformation("Completed test {TestName} with result: {Result}", nameof(AddAsync_WithNewService_ReturnsTrueAndStoresService), result);
    }

    /// <summary>
    /// Tests that adding a service with a duplicate ID returns false.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddAsync_WithDuplicateServiceId_ReturnsFalse()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(AddAsync_WithDuplicateServiceId_ReturnsFalse));
        // Arrange
        var service1 = CreateValidGrpcService("ServiceOne", "package.one", "host.one", 1000);
        await _repository.AddAsync(service1);

        var service2 = CreateValidGrpcService("ServiceTwo", "package.two", "host.two", 2000);
        service2.Id = service1.Id; // Simulate duplicate ID

        // Act
        var result = await _repository.AddAsync(service2);

        // Assert
        result.Should().BeFalse();

        _logger.LogInformation("Completed test {TestName} with result: {Result}", nameof(AddAsync_WithDuplicateServiceId_ReturnsFalse), result);
    }

    /// <summary>
    /// Tests that getting a service by full name returns the service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByFullNameAsync_WithExistingService_ReturnsService()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(GetByFullNameAsync_WithExistingService_ReturnsService));
        // Arrange
        var service = CreateValidGrpcService("TestService", "test.package", "localhost", 5000);
        await _repository.AddAsync(service);

        // Act
        var result = await _repository.GetByFullNameAsync("test.package.TestService");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(service.Id);

        _logger.LogInformation("Completed test {TestName} with result: {Result}", nameof(GetByFullNameAsync_WithExistingService_ReturnsService), result != null);
    }

    /// <summary>
    /// Tests that deleting a service returns true and removes the service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task DeleteAsync_WithExistingService_ReturnsTrueAndRemoves()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(DeleteAsync_WithExistingService_ReturnsTrueAndRemoves));
        // Arrange
        var service = CreateValidGrpcService("ServiceToRemove", "remove.package", "localhost", 5000);
        await _repository.AddAsync(service);

        // Act
        var deleteResult = await _repository.DeleteAsync(service.Id);
        var getResult = await _repository.GetByIdAsync(service.Id);

        // Assert
        deleteResult.Should().BeTrue();
        getResult.Should().BeNull();
        
        _logger.LogInformation("Completed test {TestName} with result: {Result}", nameof(DeleteAsync_WithExistingService_ReturnsTrueAndRemoves), deleteResult);
    }

    /// <summary>
    /// Tests that counting services returns the correct count.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(CountAsync_ReturnsCorrectCount));
        // Arrange
        await _repository.AddAsync(CreateValidGrpcService("Service1", "package.one", "localhost", 5001));
        await _repository.AddAsync(CreateValidGrpcService("Service2", "package.two", "localhost", 5002));

        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.Should().Be(2);

        _logger.LogInformation("Completed test {TestName} with result: {Count}", nameof(CountAsync_ReturnsCorrectCount), count);
    }

    /// <summary>
    /// Tests that updating a service returns true and updates the service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task UpdateAsync_WithExistingService_UpdatesAndReturnsTrue()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(UpdateAsync_WithExistingService_UpdatesAndReturnsTrue));
        // Arrange
        var service = CreateValidGrpcService("ServiceToUpdate", "update.package", "localhost", 5000);
        await _repository.AddAsync(service);
        
        // Modify some properties
        service.PackageName = "UpdatedPackage";
        service.Description = "Updated description";

        // Act
        var result = await _repository.UpdateAsync(service);
        var updated = await _repository.GetByIdAsync(service.Id);

        // Assert
        result.Should().BeTrue();
        updated!.PackageName.Should().Be("UpdatedPackage");
        updated.Description.Should().Be("Updated description");
        updated.UpdatedAt.Should().NotBeNull();

        _logger.LogInformation("Completed test {TestName} with result: {Result}", nameof(UpdateAsync_WithExistingService_UpdatesAndReturnsTrue), result);
    }

    /// <summary>
    /// Tests that checking if a service exists by full name returns false for a non-existent service.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task ExistsAsync_WithNonExistentFullName_ReturnsFalse()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(ExistsAsync_WithNonExistentFullName_ReturnsFalse));
        // Act
        var exists = await _repository.ExistsAsync("non.existent.Service");

        // Assert
        exists.Should().BeFalse();

        _logger.LogInformation("Completed test {TestName} with result: {Result}", nameof(ExistsAsync_WithNonExistentFullName_ReturnsFalse), exists);
    }

    /// <summary>
    /// Tests that adding a request returns true.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task AddRequestAsync_WithValidRequest_ReturnsTrue()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(AddRequestAsync_WithValidRequest_ReturnsTrue));
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", []);

        // Act
        var result = await _repository.AddRequestAsync(request);
        var stored = await _repository.GetRequestAsync(request.Id);

        // Assert
        result.Should().BeTrue();
        stored.Should().NotBeNull();
        stored!.ServiceName.Should().Be("TestService");

        _logger.LogInformation("Completed test {TestName} with result: {Result}", nameof(AddRequestAsync_WithValidRequest_ReturnsTrue), result);
    }

    /// <summary>
    /// Tests that getting a service by ID returns null for a non-existent ID.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(GetByIdAsync_WithNonExistentId_ReturnsNull));
        // Act
        var result = await _repository.GetByIdAsync("nonexistent-id");

        // Assert
        result.Should().BeNull();

        _logger.LogInformation("Completed test {TestName} with result: {Result}", nameof(GetByIdAsync_WithNonExistentId_ReturnsNull), result == null);
    }

    /// <summary>
    /// Tests that getting services by package returns the services for the specified package.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [Fact]
    public async Task GetByPackageAsync_ReturnsServicesForPackage()
    {
        _logger.LogInformation("Starting test {TestName}", nameof(GetByPackageAsync_ReturnsServicesForPackage));
        // Arrange
        var service1 = CreateValidGrpcService("Service1", "package.filter", "host.one", 1000);
        var service2 = CreateValidGrpcService("Service2", "package.filter", "host.two", 2000);
        var service3 = CreateValidGrpcService("Service3", "other.package", "host.three", 3000);
        await _repository.AddAsync(service1);
        await _repository.AddAsync(service2);
        await _repository.AddAsync(service3);

        // Act
        var servicesInPackage = await _repository.GetByPackageAsync("package.filter");

        // Assert
        servicesInPackage.Should().HaveCount(2);
        servicesInPackage.Should().Contain(service1);
        servicesInPackage.Should().Contain(service2);
        servicesInPackage.Should().NotContain(service3);

        _logger.LogInformation("Completed test {TestName} with count: {Count}", nameof(GetByPackageAsync_ReturnsServicesForPackage), servicesInPackage.Count());
    }
}
