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

public sealed class EventBusJsonExtensionsTests
{
    private readonly ILogger<EventBus> _mockLogger;

    public EventBusJsonExtensionsTests()
    {
        _mockLogger = Substitute.For<ILogger<EventBus>>();
    }

    [Fact]
    public void ToJson_WithValidEventBus_ReturnsNonEmptyJsonString()
    {
        // Arrange
        var eventBus = new EventBus(_mockLogger);

        // Act
        var json = eventBus.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Be("{}");
    }

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