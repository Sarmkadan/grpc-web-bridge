#nullable enable
// =============================================================================
// Author: Automated Generation
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class ValidationExceptionTests
{
    [Fact]
    public void Constructor_NoParameters_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationException());
    }

    [Fact]
    public void Constructor_Message_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationException(null));
    }

    [Fact]
    public void Constructor_MessageInnerException_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationException(null, null));
    }

    [Fact]
    public void Constructor_FieldNameInvalidValueValidationRule_Message_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationException(null, null, null, null));
    }

    [Fact]
    public void FieldName_Getter_ReturnsFieldName()
    {
        // Arrange
        var exception = new ValidationException("Test message", "Test field", "Test value", "Test rule");

        // Act and Assert
        exception.FieldName.Should().Be("Test field");
    }

    [Fact]
    public void InvalidValue_Getter_ReturnsInvalidValue()
    {
        // Arrange
        var exception = new ValidationException("Test message", "Test field", "Test value", "Test rule");

        // Act and Assert
        exception.InvalidValue.Should().Be("Test value");
    }

    [Fact]
    public void ValidationRule_Getter_ReturnsValidationRule()
    {
        // Arrange
        var exception = new ValidationException("Test message", "Test field", "Test value", "Test rule");

        // Act and Assert
        exception.ValidationRule.Should().Be("Test rule");
    }

    [Fact]
    public void ToString_ReturnsExpectedString()
    {
        // Arrange
        var exception = new ValidationException("Test message", "Test field", "Test value", "Test rule");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Test message");
        result.Should().Contain("Test field");
        result.Should().Contain("Test value");
        result.Should().Contain("Test rule");
    }
}
