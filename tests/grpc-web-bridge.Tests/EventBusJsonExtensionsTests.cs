#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for EventBusJsonExtensions
// =============================================================================

using System;
using FluentAssertions;
using GrpcWebBridge.Events;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Contains unit tests for the <see cref="EventBusJsonExtensions"/> class, verifying JSON serialization and deserialization behavior of EventBus objects.
/// </summary>
public sealed class EventBusJsonExtensionsTests
{
    private readonly ILogger<EventBus> _mockLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventBusJsonExtensionsTests"/> class with a mock logger.
    /// </summary>
    public EventBusJsonExtensionsTests()
    {
        _mockLogger = Substitute.For<ILogger<EventBus>>();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.ToJson(EventBus)"/> returns a non-empty JSON string when called with a valid EventBus instance.
    /// </summary>
    [Fact]
    public void ToJson_WithValidEventBus_ReturnsNonEmptyJsonString()
    {
        _mockLogger.LogInformation("Finished test {TestName}", nameof(ToJson_WithValidEventBus_ReturnsNonEmptyJsonString));
        _mockLogger.LogInformation("Starting test {TestName}", nameof(ToJson_WithValidEventBus_ReturnsNonEmptyJsonString));
        // Arrange
        var eventBus = new EventBus(_mockLogger);

        // Act
        var json = eventBus.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Be("{}");
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.ToJson(EventBus,bool)"/> returns formatted JSON when the indented parameter is true.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        // Arrange
        var eventBus = new EventBus(_mockLogger);

        // Act
        var json = eventBus.ToJson(indented: true);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("{");
        json.Should().Contain("}");
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.ToJson(EventBus)"/> throws an <see cref="ArgumentNullException"/> when called with a null EventBus instance.
    /// </summary>
    [Fact]
    public void ToJson_WithNullEventBus_ThrowsArgumentNullException()
    {
        // Arrange
        EventBus? nullEventBus = null;

        // Act
        Action act = () => nullEventBus!.ToJson();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.FromJson(string)"/> throws an <see cref="InvalidOperationException"/> when given valid JSON but the EventBus type lacks a parameterless constructor.
    /// </summary>
    [Fact]
    public void FromJson_WithValidJson_ThrowsExceptionDueToConstructorRequirements()
    {
        // Arrange
        var json = "{}";

        // Act
        Action act = () => EventBusJsonExtensions.FromJson(json);

        // Assert
        act.Should().Throw<System.InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.FromJson(string)"/> returns null when given an empty or whitespace-only JSON string.
    /// </summary>
    [Fact]
    public void FromJson_WithEmptyJsonString_ReturnsNull()
    {
        // Arrange
        var emptyJson = "   ";

        // Act
        var result = EventBusJsonExtensions.FromJson(emptyJson);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.FromJson(string)"/> returns null when given a JSON string containing only whitespace characters.
    /// </summary>
    [Fact]
    public void FromJson_WithWhitespaceJsonString_ReturnsNull()
    {
        // Arrange
        var whitespaceJson = "\n\t  \r  ";

        // Act
        var result = EventBusJsonExtensions.FromJson(whitespaceJson);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.FromJson(string)"/> throws an <see cref="ArgumentNullException"/> when called with a null JSON string.
    /// </summary>
    [Fact]
    public void FromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullJson = null;

        // Act
        Action act = () => EventBusJsonExtensions.FromJson(nullJson!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.FromJson(string)"/> throws an <see cref="InvalidOperationException"/> when given invalid JSON.
    /// </summary>
    [Fact]
    public void FromJson_WithInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var invalidJson = "{ invalid json {";

        // Act
        Action act = () => EventBusJsonExtensions.FromJson(invalidJson);

        // Assert
        act.Should().Throw<System.InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.TryFromJson(string,out EventBus?)"/> returns false and null when given valid JSON but deserialization fails due to missing constructor.
    /// </summary>
    [Fact]
    public void TryFromJson_WithValidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = EventBusJsonExtensions.TryFromJson(json, out var deserializedEventBus);

        // Assert
        result.Should().BeFalse();
        deserializedEventBus.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.TryFromJson(string,out EventBus?)"/> returns false and null when given an empty or whitespace-only JSON string.
    /// </summary>
    [Fact]
    public void TryFromJson_WithEmptyJsonString_ReturnsFalseAndNull()
    {
        // Arrange
        var emptyJson = "   ";

        // Act
        var result = EventBusJsonExtensions.TryFromJson(emptyJson, out var deserializedEventBus);

        // Assert
        result.Should().BeFalse();
        deserializedEventBus.Should().BeNull();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.TryFromJson(string,out EventBus?)"/> throws an <see cref="ArgumentNullException"/> when called with a null JSON string.
    /// </summary>
    [Fact]
    public void TryFromJson_WithNullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? nullJson = null;

        // Act
        Action act = () => EventBusJsonExtensions.TryFromJson(nullJson!, out _);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.TryFromJson(string,out EventBus?)"/> throws an exception when given invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_WithInvalidJson_ThrowsException()
    {
        // Arrange
        var invalidJson = "{ invalid json {";

        // Act
        Action act = () => EventBusJsonExtensions.TryFromJson(invalidJson, out _);

        // Assert
        act.Should().Throw<System.InvalidOperationException>();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.ToJson(EventBus)"/> produces valid JSON structure representing an empty object.
    /// </summary>
    [Fact]
    public void ToJson_ProducesValidJsonStructure()
    {
        // Arrange
        var eventBus = new EventBus(_mockLogger);

        // Act
        var json = eventBus.ToJson();

        // Assert
        json.Should().Be("{}");
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.TryFromJson(string,out EventBus?)"/> returns false when given a valid empty JSON object string.
    /// </summary>
    [Fact]
    public void TryFromJson_WithValidEmptyObject_ReturnsFalse()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = EventBusJsonExtensions.TryFromJson(json, out _);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.ToJson(EventBus,bool)"/> returns compact JSON when the indented parameter is false.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var eventBus = new EventBus(_mockLogger);

        // Act
        var json = eventBus.ToJson(indented: false);

        // Assert
        json.Should().Be("{}");
    }

    /// <summary>
    /// Tests that <see cref="EventBusJsonExtensions.FromJson(string)"/> throws an <see cref="InvalidOperationException"/> when given an empty JSON object string.
    /// </summary>
    [Fact]
    public void FromJson_WithEmptyObjectString_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = "{}";

        // Act
        Action act = () => EventBusJsonExtensions.FromJson(json);

        // Assert
        act.Should().Throw<System.InvalidOperationException>();
    }
}