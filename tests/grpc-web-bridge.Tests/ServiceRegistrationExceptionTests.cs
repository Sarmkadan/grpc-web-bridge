#nullable enable
// =============================================================================
// Author: Automated Generation
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class ServiceRegistrationExceptionTests
{
    [Fact]
    public void Constructor_NoParameters_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new ServiceRegistrationException());
    }

    [Fact]
    public void Constructor_Message_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new ServiceRegistrationException(null));
    }

    [Fact]
    public void Constructor_ServiceName_ServiceEndpoint_Message_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new ServiceRegistrationException(null, null, null));
    }

    [Fact]
    public void ServiceName_Getter_ReturnsServiceName()
    {
        // Arrange
        var exception = new ServiceRegistrationException("Test message", "Test service", "Test endpoint");

        // Act and Assert
        exception.ServiceName.Should().Be("Test service");
    }

    [Fact]
    public void ServiceEndpoint_Getter_ReturnsServiceEndpoint()
    {
        // Arrange
        var exception = new ServiceRegistrationException("Test message", "Test service", "Test endpoint");

        // Act and Assert
        exception.ServiceEndpoint.Should().Be("Test endpoint");
    }

    [Fact]
    public void ToString_ReturnsExpectedString()
    {
        // Arrange
        var exception = new ServiceRegistrationException("Test message", "Test service", "Test endpoint");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Test message");
        result.Should().Contain("Test service");
        result.Should().Contain("Test endpoint");
    }
}
