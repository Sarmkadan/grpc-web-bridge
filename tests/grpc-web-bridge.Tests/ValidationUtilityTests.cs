#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class ValidationUtilityTests
{
    [Fact]
    public void ValidateEmail_WithValidFormat_ReturnsValid()
    {
        // Arrange & Act
        var (valid, error) = ValidationUtility.ValidateEmail("user@example.com");

        // Assert
        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateEmail_WithMissingDomain_ReturnsInvalid()
    {
        // Arrange & Act
        var (valid, error) = ValidationUtility.ValidateEmail("user@");

        // Assert
        valid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ValidateMethodName_StartingWithDigit_ReturnsInvalid()
    {
        // Arrange & Act
        var (valid, error) = ValidationUtility.ValidateMethodName("1GetUser");

        // Assert
        valid.Should().BeFalse();
        error.Should().Contain("letter");
    }

    [Fact]
    public void ValidateServiceId_WithDotsAndHyphens_ReturnsValid()
    {
        // Arrange & Act
        var (valid, error) = ValidationUtility.ValidateServiceId("my-service.v1");

        // Assert
        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void SanitizeInput_WithHtmlTags_EscapesAllSpecialCharacters()
    {
        // Arrange
        const string input = "<script>alert('xss')</script>";

        // Act
        var result = ValidationUtility.SanitizeInput(input);

        // Assert
        result.Should().NotContain("<");
        result.Should().NotContain(">");
        result.Should().NotContain("'");
        result.Should().Contain("&lt;");
        result.Should().Contain("&gt;");
        result.Should().Contain("&#x27;");
    }
}
