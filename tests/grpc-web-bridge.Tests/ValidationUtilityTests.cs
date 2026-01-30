#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Utilities;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the ValidationUtility class.
/// </summary>
public sealed class ValidationUtilityTests
{
    [Fact]
    /// <summary>
    /// Validates that a correctly formatted email returns true and no error.
    /// </summary>
    public void ValidateEmail_WithValidFormat_ReturnsValid()
    {
        // Arrange & Act
        var (valid, error) = ValidationUtility.ValidateEmail("user@example.com");

        // Assert
        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that an email missing the domain part returns false and an error message.
    /// </summary>
    public void ValidateEmail_WithMissingDomain_ReturnsInvalid()
    {
        // Arrange & Act
        var (valid, error) = ValidationUtility.ValidateEmail("user@");

        // Assert
        valid.Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    /// <summary>
    /// Validates that a method name starting with a digit returns false and an error indicating the first character must be a letter.
    /// </summary>
    public void ValidateMethodName_StartingWithDigit_ReturnsInvalid()
    {
        // Arrange & Act
        var (valid, error) = ValidationUtility.ValidateMethodName("1GetUser");

        // Assert
        valid.Should().BeFalse();
        error.Should().Contain("letter");
    }

    [Fact]
    /// <summary>
    /// Validates that a service ID containing dots and hyphens returns true and no error.
    /// </summary>
    public void ValidateServiceId_WithDotsAndHyphens_ReturnsValid()
    {
        // Arrange & Act
        var (valid, error) = ValidationUtility.ValidateServiceId("my-service.v1");

        // Assert
        valid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    /// <summary>
    /// Validates that sanitizing an input string containing HTML tags escapes all special characters.
    /// </summary>
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
