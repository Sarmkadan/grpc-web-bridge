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
using System.Threading.Tasks;

namespace GrpcWebBridge.Tests;

public class ServiceRepositoryTests
{
    private readonly ILogger<ServiceRepository> _logger;
    private readonly ServiceRepository _repository;

    public ServiceRepositoryTests()
    {
        _logger = Substitute.For<ILogger<ServiceRepository>>();
        _repository = new ServiceRepository(_logger);
    }

    private GrpcService CreateValidGrpcService(string name, string packageName, string endpoint, int port)
    {
        var service = new GrpcService(name, packageName, endpoint, port);
        service.AddMethod(new GrpcMethod("DummyMethod", $"{packageName}.{name}.DummyMethod", MethodType.Unary, "InputType", "OutputType"));
        return service;
    }

    [Fact]
    public async Task AddAsync_WithNewService_ReturnsTrueAndStoresService()
    {
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
    }

    [Fact]
    public async Task AddAsync_WithDuplicateServiceId_ReturnsFalse()
    {
        // Arrange
        var service1 = CreateValidGrpcService("ServiceOne", "package.one", "host.one", 1000);
        await _repository.AddAsync(service1);

        var service2 = CreateValidGrpcService("ServiceTwo", "package.two", "host.two", 2000);
        service2.Id = service1.Id; // Simulate duplicate ID

        // Act
        var result = await _repository.AddAsync(service2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByFullNameAsync_WithExistingService_ReturnsService()
    {
        // Arrange
        var service = CreateValidGrpcService("TestService", "test.package", "localhost", 5000);
        await _repository.AddAsync(service);

        // Act
        var result = await _repository.GetByFullNameAsync("test.package.TestService");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(service.Id);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingService_ReturnsTrueAndRemoves()
    {
        // Arrange
        var service = CreateValidGrpcService("ServiceToRemove", "remove.package", "localhost", 5000);
        await _repository.AddAsync(service);

        // Act
        var deleteResult = await _repository.DeleteAsync(service.Id);
        var getResult = await _repository.GetByIdAsync(service.Id);

        // Assert
        deleteResult.Should().BeTrue();
        getResult.Should().BeNull();
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _repository.AddAsync(CreateValidGrpcService("Service1", "package.one", "localhost", 5001));
        await _repository.AddAsync(CreateValidGrpcService("Service2", "package.two", "localhost", 5002));

        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingService_UpdatesAndReturnsTrue()
    {
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
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentFullName_ReturnsFalse()
    {
        // Act
        var exists = await _repository.ExistsAsync("non.existent.Service");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AddRequestAsync_WithValidRequest_ReturnsTrue()
    {
        // Arrange
        var request = new GrpcRequest("TestService", "TestMethod", Array.Empty<byte>());

        // Act
        var result = await _repository.AddRequestAsync(request);
        var stored = await _repository.GetRequestAsync(request.Id);

        // Assert
        result.Should().BeTrue();
        stored.Should().NotBeNull();
        stored!.ServiceName.Should().Be("TestService");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("nonexistent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPackageAsync_ReturnsServicesForPackage()
    {
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
    }
}
