#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcServiceValidationTests
{
    private static GrpcService CreateValidService()
    {
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051)
        {
            Id = Guid.NewGuid().ToString("N"),
            FullName = "Test.Package.TestService",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        service.AddMethod(new GrpcMethod("TestMethod", "Test.Package.TestMethod", MethodType.Unary, "TestInput", "TestOutput"));
        return service;
    }

    [Fact]
    public void Validate_WithValidService_ReturnsEmptyList()
    {
        // Arrange
        var service = CreateValidService();

        // Act
        var problems = GrpcServiceValidation.Validate(service);

        // Assert
        problems.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcService? service = null;

        // Act
        Action act = () => GrpcServiceValidation.Validate(service);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsErrorMessage()
    {
        // Arrange
        var service = CreateValidService();
        service.Name = string.Empty;

        // Act
        var problems = GrpcServiceValidation.Validate(service);

        // Assert
        problems.Should().ContainSingle()
            .Which.Should().Be("Service Name cannot be null or whitespace");
    }

    [Fact]
    public void Validate_WithInvalidPackageName_ReturnsErrorMessage()
    {
        // Arrange
        var service = CreateValidService();
        service.PackageName = "invalid_package";

        // Act
        var problems = GrpcServiceValidation.Validate(service);

        // Assert
        problems.Should().ContainSingle()
            .Which.Should().Be("Service PackageName must be a valid .NET package name (alphanumeric with dots)");
    }

    [Fact]
    public void Validate_WithInvalidPort_ReturnsErrorMessage()
    {
        // Arrange
        var service = CreateValidService();
        service.Port = 70000;

        // Act
        var problems = GrpcServiceValidation.Validate(service);

        // Assert
        problems.Should().ContainSingle()
            .Which.Should().Be("Service Port must be between 1 and 65535");
    }

    [Fact]
    public void Validate_WithUnknownStatus_ReturnsErrorMessage()
    {
        // Arrange
        var service = CreateValidService();
        service.Status = ServiceStatus.Unknown;

        // Act
        var problems = GrpcServiceValidation.Validate(service);

        // Assert
        problems.Should().ContainSingle()
            .Which.Should().Be("Service Status cannot be Unknown");
    }

    [Fact]
    public void Validate_WithEmptyMethods_ReturnsErrorMessage()
    {
        // Arrange
        var service = new GrpcService("TestService", "Test.Package", "localhost", 50051)
        {
            Id = Guid.NewGuid().ToString("N"),
            FullName = "Test.Package.TestService",
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        // Act
        var problems = GrpcServiceValidation.Validate(service);

        // Assert
        problems.Should().ContainSingle()
            .Which.Should().Be("Service must have at least one method");
    }

    [Fact]
    public void IsValid_WithValidService_ReturnsTrue()
    {
        // Arrange
        var service = CreateValidService();

        // Act
        var isValid = GrpcServiceValidation.IsValid(service);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithNullService_ReturnsFalse()
    {
        // Arrange
        GrpcService? service = null;

        // Act
        var isValid = GrpcServiceValidation.IsValid(service);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_WithValidService_DoesNotThrow()
    {
        // Arrange
        var service = CreateValidService();

        // Act
        Action act = () => GrpcServiceValidation.EnsureValid(service);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_WithNullService_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcService? service = null;

        // Act
        Action act = () => GrpcServiceValidation.EnsureValid(service);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureValid_WithInvalidService_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateValidService();
        service.Name = string.Empty;

        // Act
        Action act = () => GrpcServiceValidation.EnsureValid(service);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*GrpcService is invalid*");
    }
}