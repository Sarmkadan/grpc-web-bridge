#nullable enable

using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcWebBridgeExceptionExtensionsTests
{
    [Fact]
    public void AddContextEntry_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcWebBridgeException? exception = null;
        const string key = "testKey";
        const string value = "testValue";

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => exception!.AddContextEntry(key, value));
    }

    [Fact]
    public void AddContextEntry_NullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        string? key = null;
        const string value = "testValue";

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddContextEntry(key!, value));
    }

    [Fact]
    public void AddContextEntry_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        var key = string.Empty;
        const string value = "testValue";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => exception.AddContextEntry(key, value));
    }

    [Fact]
    public void AddContextEntry_WhitespaceKey_ThrowsArgumentException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        const string key = "   ";
        const string value = "testValue";

        // Act and Assert
        Assert.Throws<ArgumentException>(() => exception.AddContextEntry(key, value));
    }

    [Fact]
    public void AddContextEntry_ValidParameters_AddsContextAndReturnsSameException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        const string key = "requestId";
        const string value = "12345";

        // Act
        var result = exception.AddContextEntry(key, value);

        // Assert
        result.Should().BeSameAs(exception);
        exception.Context.Should().ContainKey(key).WhoseValue.Should().Be(value);
    }

    [Fact]
    public void AddContextEntry_MultipleEntries_ContextContainsAllEntries()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        const string key1 = "requestId";
        const string value1 = "12345";
        const string key2 = "userId";
        const string value2 = "user-789";

        // Act
        exception.AddContextEntry(key1, value1);
        exception.AddContextEntry(key2, value2);

        // Assert
        exception.Context.Should().HaveCount(2);
        exception.Context.Should().ContainKey(key1).WhoseValue.Should().Be(value1);
        exception.Context.Should().ContainKey(key2).WhoseValue.Should().Be(value2);
    }

    [Fact]
    public void AddContextEntry_ObjectValue_AddsObjectValue()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        const string key = "metadata";
        var value = new { Status = "active", Count = 42 };

        // Act
        var result = exception.AddContextEntry(key, value);

        // Assert
        result.Should().BeSameAs(exception);
        exception.Context.Should().ContainKey(key).WhoseValue.Should().BeSameAs(value);
    }

    [Fact]
    public void GetContextString_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcWebBridgeException? exception = null;

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => exception!.GetContextString());
    }

    [Fact]
    public void GetContextString_EmptyContext_ReturnsEmptyString()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");

        // Act
        var result = exception.GetContextString();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetContextString_SingleContextEntry_ReturnsFormattedString()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("requestId", "12345");

        // Act
        var result = exception.GetContextString();

        // Assert
        result.Should().Be("requestId: 12345");
    }

    [Fact]
    public void GetContextString_MultipleContextEntries_ReturnsCommaSeparatedString()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("requestId", "12345");
        exception.AddContext("userId", "user-789");
        exception.AddContext("timestamp", "2024-01-01T00:00:00Z");

        // Act
        var result = exception.GetContextString();

        // Assert
        result.Should().Be("requestId: 12345, userId: user-789, timestamp: 2024-01-01T00:00:00Z");
    }

    [Fact]
    public void GetContextString_ContextWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("error:details", "value:with:colons");
        exception.AddContext("normalKey", "normalValue");

        // Act
        var result = exception.GetContextString();

        // Assert
        result.Should().Be("error:details: value:with:colons, normalKey: normalValue");
    }

    [Fact]
    public void WithNewErrorCode_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        GrpcWebBridgeException? exception = null;
        const string newErrorCode = "NEW_ERROR_CODE";

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => exception!.WithNewErrorCode(newErrorCode));
    }

    [Fact]
    public void WithNewErrorCode_ValidParameters_ReturnsNewExceptionWithSameMessageAndInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var originalException = new GrpcWebBridgeException("Original message", innerException) { ErrorCode = "ORIGINAL_CODE" };
        const string newErrorCode = "NEW_ERROR_CODE";

        // Act
        var result = originalException.WithNewErrorCode(newErrorCode);

        // Assert
        result.Should().NotBeSameAs(originalException);
        result.Message.Should().Be(originalException.Message);
        result.InnerException.Should().BeSameAs(innerException);
        result.ErrorCode.Should().Be(newErrorCode);
    }

    [Fact]
    public void WithNewErrorCode_EmptyErrorCode_ReturnsNewExceptionWithEmptyErrorCode()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        const string newErrorCode = "";

        // Act
        var result = exception.WithNewErrorCode(newErrorCode);

        // Assert
        result.ErrorCode.Should().BeEmpty();
    }

    [Fact]
    public void WithNewErrorCode_ExceptionWithoutInnerException_ReturnsNewExceptionWithoutInnerException()
    {
        // Arrange
        var originalException = new GrpcWebBridgeException("Original message");
        const string newErrorCode = "NEW_ERROR_CODE";

        // Act
        var result = originalException.WithNewErrorCode(newErrorCode);

        // Assert
        result.InnerException.Should().BeNull();
    }

    [Fact]
    public void WithNewErrorCode_ExceptionWithContext_CreatesNewExceptionWithoutContext()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("requestId", "12345");
        exception.AddContext("userId", "user-789");
        const string newErrorCode = "NEW_ERROR_CODE";

        // Act
        var result = exception.WithNewErrorCode(newErrorCode);

        // Assert - The method creates a new exception without copying context
        result.Context.Should().BeEmpty();
        result.ErrorCode.Should().Be(newErrorCode);
    }
}