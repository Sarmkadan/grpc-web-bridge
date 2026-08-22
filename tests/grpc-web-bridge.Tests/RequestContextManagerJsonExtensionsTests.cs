#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using FluentAssertions;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Contains unit tests for <see cref="RequestContextManagerJsonExtensions"/> ensuring that
/// JSON serialization and deserialization of <see cref="RequestContext"/> works correctly.
/// </summary>
public sealed class RequestContextManagerJsonExtensionsTests
{
    private readonly ILogger<RequestContextManager> _logger;
    private readonly RequestContextManager _contextManager;

    /// <summary>
    /// Initializes a new instance of <see cref="RequestContextManagerJsonExtensionsTests"/>.
    /// Sets up a mock <see cref="ILogger{RequestContextManager}"/> and creates the
    /// <see cref="RequestContextManager"/> instance under test.
    /// </summary>
    public RequestContextManagerJsonExtensionsTests()
    {
        _logger = Substitute.For<ILogger<RequestContextManager>>();
        _contextManager = new RequestContextManager(_logger);
    }

    /// <summary>
    /// Verifies that serializing a valid RequestContext to JSON produces a non-empty string.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void ToJson_WithValidContext_ReturnsNonEmptyString()
    {
        // Arrange
        var context = _contextManager.CreateContext("test-request-123", "user-456");

        // Act
        var json = _contextManager.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("test-request-123");
        json.Should().Contain("user-456");
        _logger.LogInformation("Finished test ToJson_WithValidContext_ReturnsNonEmptyString with Result {Json}", json);
    }

    /// <summary>
    /// Verifies that serializing a valid RequestContext with indented flag produces formatted JSON.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        // Arrange
        var context = _contextManager.CreateContext("test-request-456", "user-789");

        // Act
        var json = _contextManager.ToJson(indented: true);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("{");
        json.Should().Contain("}");
        json.Should().Contain("requestId");
        json.Should().Contain("userId");
    }

    /// <summary>
    /// Verifies that serializing a valid RequestContext without indented flag produces compact JSON.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var context = _contextManager.CreateContext("test-request-789", "user-abc");

        // Act
        var json = _contextManager.ToJson(indented: false);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().NotContain("\n");
        json.Should().Contain("requestId");
        json.Should().Contain("userId");
    }

    /// <summary>
    /// Verifies that ToJson throws ArgumentNullException when context is null.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void ToJson_WithNullContext_ThrowsArgumentNullException()
    {
        // Arrange
        RequestContextManager? nullManager = null;

        // Act & Assert
        Action act = () => nullManager.ToJson();
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that deserializing a valid JSON string returns a non-null RequestContext.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void FromJson_WithValidJson_ReturnsNonNullContext()
    {
        // Arrange
        var context = _contextManager.CreateContext("test-request-234", "user-xyz");
        var json = _contextManager.ToJson();

        // Act
        var deserialized = RequestContextManagerJsonExtensions.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.RequestId.Should().Be("test-request-234");
        deserialized.UserId.Should().Be("user-xyz");
        deserialized.StartTime.Should().BeCloseTo(context.StartTime, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that deserializing null JSON throws ArgumentNullException.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void FromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullJson = null;

        // Act & Assert
        Action act = () => RequestContextManagerJsonExtensions.FromJson(nullJson!);
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that deserializing empty or whitespace JSON returns null.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void FromJson_WithEmptyOrWhitespaceJson_ReturnsNull(string emptyJson)
    {
        // Act
        var result = RequestContextManagerJsonExtensions.FromJson(emptyJson);

        // Assert
        result.Should().BeNull();
    }


    /// <summary>
    /// Verifies that FromJson throws JsonException when JSON is malformed.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void FromJson_WithMalformedJson_ThrowsJsonException()
    {
        // Arrange
        var malformedJson = "{ invalid json {{ {";

        // Act & Assert
        Action act = () => RequestContextManagerJsonExtensions.FromJson(malformedJson);
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    /// <summary>
    /// Verifies that TryFromJson returns true and deserializes valid JSON.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDeserializes()
    {
        // Arrange
        var context = _contextManager.CreateContext("test-request-345", "user-def");
        var json = _contextManager.ToJson();

        // Act
        var result = RequestContextManagerJsonExtensions.TryFromJson(json, out var deserialized);

        // Assert
        result.Should().BeTrue();
        deserialized.Should().NotBeNull();
        deserialized.RequestId.Should().Be("test-request-345");
        deserialized.UserId.Should().Be("user-def");
    }

    /// <summary>
    /// Verifies that TryFromJson returns false when JSON is empty or whitespace.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void TryFromJson_WithEmptyOrWhitespaceJson_ReturnsFalse(string emptyJson)
    {
        // Act
        var result = RequestContextManagerJsonExtensions.TryFromJson(emptyJson, out var deserialized);

        // Assert
        result.Should().BeFalse();
        deserialized.Should().BeNull();
    }

    /// <summary>
    /// Verifies that TryFromJson returns false when JSON is null.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void TryFromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullJson = null;

        // Act & Assert
        Action act = () => RequestContextManagerJsonExtensions.TryFromJson(nullJson!, out _);
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that TryFromJson returns false when JSON is malformed.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void TryFromJson_WithMalformedJson_ReturnsFalse()
    {
        // Arrange
        var malformedJson = "{ invalid json {{ {";

        // Act
        var result = RequestContextManagerJsonExtensions.TryFromJson(malformedJson, out var deserialized);

        // Assert
        result.Should().BeFalse();
        deserialized.Should().BeNull();
    }

    /// <summary>
    /// Verifies that round-trip serialization and deserialization preserves all data.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void RoundTrip_WithFullContext_PreservesAllData()
    {
        // Arrange
        var originalContext = _contextManager.CreateContext(
            "round-trip-request-123",
            "round-trip-user-456",
            new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } }
        );
        originalContext.EndTime = DateTime.UtcNow.AddSeconds(1);

        var json = _contextManager.ToJson();

        // Act
        var deserialized = RequestContextManagerJsonExtensions.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.RequestId.Should().Be(originalContext.RequestId);
        deserialized.UserId.Should().Be(originalContext.UserId);
        deserialized.StartTime.Should().BeCloseTo(originalContext.StartTime, TimeSpan.FromSeconds(1));
        deserialized.EndTime.Should().BeCloseTo(originalContext.EndTime.Value, TimeSpan.FromSeconds(1));
        deserialized.Metadata.Should().HaveCount(2);
        deserialized.Metadata["key1"].Should().Be("value1");
        deserialized.Metadata["key2"].Should().Be("value2");
    }

    /// <summary>
    /// Verifies that context with minimal properties serializes and deserializes correctly.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void RoundTrip_WithMinimalContext_PreservesRequiredFields()
    {
        // Arrange
        var originalContext = _contextManager.CreateContext("minimal-request-789");
        var json = _contextManager.ToJson();

        // Act
        var deserialized = RequestContextManagerJsonExtensions.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.RequestId.Should().Be("minimal-request-789");
        deserialized.UserId.Should().BeNull();
        deserialized.StartTime.Should().BeCloseTo(originalContext.StartTime, TimeSpan.FromSeconds(1));
        deserialized.Metadata.Should().NotBeNull();
        deserialized.Metadata.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that JSON serialization uses camelCase property naming policy.
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void ToJson_UsesCamelCaseNamingPolicy()
    {
        // Arrange
        var context = _contextManager.CreateContext("camel-case-test", "user-789");

        // Act
        var json = _contextManager.ToJson();

        // Assert
        json.Should().Contain("requestId");
        json.Should().Contain("userId");
        json.Should().Contain("startTime");
        json.Should().Contain("metadata");
        json.Should().NotContain("RequestId");
        json.Should().NotContain("UserId");
    }

    /// <summary>
    /// Verifies that null values are not included in JSON output (DefaultIgnoreCondition.WhenWritingNull).
    /// </summary>
    /// <returns>Nothing.</returns>
    [Fact]
    public void ToJson_WithNullUserId_DoesNotIncludeUserIdField()
    {
        // Arrange
        var context = _contextManager.CreateContext("null-user-test", userId: null);
        var json = _contextManager.ToJson();

        // Act
        var deserialized = RequestContextManagerJsonExtensions.FromJson(json);

        // Assert
        json.Should().NotContain("userId");
        deserialized.UserId.Should().BeNull();
    }
}