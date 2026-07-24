#nullable enable
// =============================================================================
// Author: Automated Generation
// =====================================================================

using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class ServiceRegistrationExceptionExtensionsTests
{
    [Fact]
    public void ToDetailedString_HappyPath_WithServiceNameAndEndpoint_ReturnsDetailedString()
    {
        // Arrange
        var exception = new ServiceRegistrationException("test-service", "http://localhost:5000", "Connection failed");

        // Act
        var result = exception.ToDetailedString();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Connection failed");
        result.Should().Contain("test-service");
        result.Should().Contain("http://localhost:5000");
    }

    [Fact]
    public void ToDetailedString_HappyPath_WithOnlyServiceName_ReturnsDetailedString()
    {
        // Arrange
        var exception = new ServiceRegistrationException("test-service", "Service not found");

        // Act
        var result = exception.ToDetailedString();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Service not found");
        result.Should().Contain("test-service");
        result.Should().Contain("N/A"); // Endpoint should be N/A
    }

    [Fact]
    public void ToDetailedString_HappyPath_WithNullValues_ReturnsDetailedString()
    {
        // Arrange
        var exception = new ServiceRegistrationException("Generic error message");

        // Act
        var result = exception.ToDetailedString();

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Generic error message");
        result.Should().Contain("N/A"); // Both ServiceName and ServiceEndpoint should be N/A
    }

    [Fact]
    public void ToDetailedString_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceRegistrationException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.ToDetailedString());
    }

    [Fact]
    public void TryExtractEndpoint_HappyPath_WithValidEndpoint_ReturnsTrueAndExtractsEndpoint()
    {
        // Arrange
        var exception = new ServiceRegistrationException("test-service", "http://localhost:5000", "Connection failed");

        // Act
        var success = exception.TryExtractEndpoint(out var endpoint);

        // Assert
        success.Should().BeTrue();
        endpoint.Should().Be("http://localhost:5000");
    }

    [Fact]
    public void TryExtractEndpoint_HappyPath_WithNullEndpoint_ReturnsFalseAndNull()
    {
        // Arrange
        var exception = new ServiceRegistrationException("test-service", "Service not found");

        // Act
        var success = exception.TryExtractEndpoint(out var endpoint);

        // Assert
        success.Should().BeFalse();
        endpoint.Should().BeNull();
    }

    [Fact]
    public void TryExtractEndpoint_HappyPath_WithEmptyEndpoint_ReturnsFalseAndEmptyString()
    {
        // Arrange
        var exception = new ServiceRegistrationException("test-service", "", "Empty endpoint");

        // Act
        var success = exception.TryExtractEndpoint(out var endpoint);

        // Assert
        success.Should().BeFalse();
        endpoint.Should().BeEmpty();
    }

    [Fact]
    public void TryExtractEndpoint_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        ServiceRegistrationException? exception = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception!.TryExtractEndpoint(out _));
    }

    [Fact]
    public void CombineExceptions_HappyPath_WithSingleException_ReturnsAggregateException()
    {
        // Arrange
        var exceptions = new List<ServiceRegistrationException>
        {
            new ServiceRegistrationException("service1", "http://localhost:5001", "Error 1")
        };

        // Act
        var result = ServiceRegistrationExceptionExtensions.CombineExceptions(exceptions);

        // Assert
        result.Should().NotBeNull();
        result.InnerExceptions.Should().HaveCount(1);
        result.InnerExceptions[0].Should().BeOfType<ServiceRegistrationException>();
    }

    [Fact]
    public void CombineExceptions_HappyPath_WithMultipleExceptions_ReturnsAggregateException()
    {
        // Arrange
        var exceptions = new List<ServiceRegistrationException>
        {
            new ServiceRegistrationException("service1", "http://localhost:5001", "Error 1"),
            new ServiceRegistrationException("service2", "http://localhost:5002", "Error 2"),
            new ServiceRegistrationException("service3", "http://localhost:5003", "Error 3")
        };

        // Act
        var result = ServiceRegistrationExceptionExtensions.CombineExceptions(exceptions);

        // Assert
        result.Should().NotBeNull();
        result.InnerExceptions.Should().HaveCount(3);
        result.InnerExceptions.Should().AllBeOfType<ServiceRegistrationException>();
    }

    [Fact]
    public void CombineExceptions_HappyPath_WithMixedNullAndValidExceptions_FiltersOutNulls()
    {
        // Arrange
        var exceptions = new List<ServiceRegistrationException?>
        {
            new ServiceRegistrationException("service1", "http://localhost:5001", "Error 1"),
            null,
            new ServiceRegistrationException("service2", "http://localhost:5002", "Error 2"),
            null
        };

        // Act
        var result = ServiceRegistrationExceptionExtensions.CombineExceptions(exceptions);

        // Assert
        result.Should().NotBeNull();
        result.InnerExceptions.Should().HaveCount(2);
        result.InnerExceptions.Should().AllBeOfType<ServiceRegistrationException>();
    }

    [Fact]
    public void CombineExceptions_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        List<ServiceRegistrationException>? exceptions = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ServiceRegistrationExceptionExtensions.CombineExceptions(exceptions!));
    }

    [Fact]
    public void CombineExceptions_EmptyCollection_ThrowsArgumentException()
    {
        // Arrange
        var exceptions = new List<ServiceRegistrationException>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ServiceRegistrationExceptionExtensions.CombineExceptions(exceptions));
    }

    [Fact]
    public void CombineExceptions_AllNullValues_ThrowsArgumentException()
    {
        // Arrange
        var exceptions = new List<ServiceRegistrationException?>
        {
            null,
            null,
            null
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ServiceRegistrationExceptionExtensions.CombineExceptions(exceptions));
    }

    [Fact]
    public void CombineExceptions_BoundaryCase_MaximumNumberOfExceptions()
    {
        // Arrange
        var exceptions = new List<ServiceRegistrationException>();
        for (int i = 0; i < 100; i++)
        {
            exceptions.Add(new ServiceRegistrationException($"service{i}", $"http://localhost:500{i}", $"Error {i}"));
        }

        // Act
        var result = ServiceRegistrationExceptionExtensions.CombineExceptions(exceptions);

        // Assert
        result.Should().NotBeNull();
        result.InnerExceptions.Should().HaveCount(100);
    }
}