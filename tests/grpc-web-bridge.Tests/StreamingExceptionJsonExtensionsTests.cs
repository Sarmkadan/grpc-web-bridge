#nullable enable
// =============================================================================
// Author: Automated Generation
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class StreamingExceptionJsonExtensionsTests
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public StreamingExceptionJsonExtensionsTests()
    {
        _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    [Fact]
    public void ToJson_HappyPath_ReturnsJsonString()
    {
        // Arrange
        var exception = new StreamingException("Test message");

        // Act
        var json = StreamingExceptionJsonExtensions.ToJson(exception);

        // Assert
        json.Should().NotBeNull();
        json.Should().Contain("Test message");
    }

    [Fact]
    public void ToJson_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => StreamingExceptionJsonExtensions.ToJson(null));
    }

    [Fact]
    public void FromJson_HappyPath_ReturnsStreamingException()
    {
        // Arrange
        var json = "{\"Message\":\"Test message\"}";

        // Act
        var exception = StreamingExceptionJsonExtensions.FromJson(json);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Test message");
    }

    [Fact]
    public void FromJson_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => StreamingExceptionJsonExtensions.FromJson(null));
    }

    [Fact]
    public void FromJson_EmptyJson_ReturnsNull()
    {
        // Act
        var exception = StreamingExceptionJsonExtensions.FromJson("");

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_HappyPath_ReturnsTrue()
    {
        // Arrange
        var json = "{\"Message\":\"Test message\"}";

        // Act
        var success = StreamingExceptionJsonExtensions.TryFromJson(json, out _);

        // Assert
        success.Should().BeTrue();
    }

    [Fact]
    public void TryFromJson_NullInput_ReturnsFalse()
    {
        // Act
        var success = StreamingExceptionJsonExtensions.TryFromJson(null, out _);

        // Assert
        success.Should().BeFalse();
    }

    [Fact]
    public void TryFromJson_EmptyJson_ReturnsFalse()
    {
        // Act
        var success = StreamingExceptionJsonExtensions.TryFromJson("", out _);

        // Assert
        success.Should().BeFalse();
    }
}
