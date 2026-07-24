#nullable enable

using System;
using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcWebBridgeExceptionTests
{
    [Fact]
    public void Constructor_Parameterless_CreatesExceptionWithDefaultMessage()
    {
        // Act
        var exception = new GrpcWebBridgeException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeEmpty(); // Default exception message is not empty
        exception.ErrorCode.Should().BeNull();
        exception.GrpcStatus.Should().BeNull();
        exception.Context.Should().NotBeNull();
        exception.Context.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new GrpcWebBridgeException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().BeNull();
        exception.GrpcStatus.Should().BeNull();
        exception.Context.Should().NotBeNull();
        exception.Context.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new GrpcWebBridgeException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.ErrorCode.Should().BeNull();
        exception.GrpcStatus.Should().BeNull();
        exception.Context.Should().NotBeNull();
        exception.Context.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMessageAndErrorCode_SetsMessageAndErrorCode()
    {
        // Arrange
        var message = "Test error message";
        var errorCode = "TEST_ERROR_CODE";

        // Act
        var exception = new GrpcWebBridgeException(message, errorCode);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be(errorCode);
        exception.GrpcStatus.Should().BeNull();
        exception.Context.Should().NotBeNull();
        exception.Context.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMessageAndGrpcStatus_SetsMessageAndGrpcStatus()
    {
        // Arrange
        var message = "Test error message";
        var statusCode = GrpcStatusCode.Internal;

        // Act
        var exception = new GrpcWebBridgeException(message, statusCode);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().BeNull();
        exception.GrpcStatus.Should().Be(statusCode);
        exception.Context.Should().NotBeNull();
        exception.Context.Should().BeEmpty();
    }

    [Fact]
    public void AddContext_WithValidKeyValue_AddsToContextDictionary()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        var key = "testKey";
        var value = "testValue";

        // Act
        exception.AddContext(key, value);

        // Assert
        exception.Context.Should().ContainKey(key);
        exception.Context[key].Should().Be(value);
    }

    [Fact]
    public void AddContext_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => exception.AddContext(null!, "value"));
    }

    [Fact]
    public void AddContext_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => exception.AddContext(string.Empty, "value"));
        Assert.Throws<ArgumentException>(() => exception.AddContext("   ", "value"));
    }

    [Fact]
    public void AddContext_WithWhitespaceKey_ThrowsArgumentException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => exception.AddContext("  \t  ", "value"));
    }

    [Fact]
    public void AddContext_WithMultipleValues_StoresAllValues()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("key1", "value1");
        exception.AddContext("key2", 123);
        exception.AddContext("key3", new { Property = "nested" });

        // Act & Assert
        exception.Context.Should().HaveCount(3);
        exception.Context["key1"].Should().Be("value1");
        exception.Context["key2"].Should().Be(123);
        exception.Context["key3"].Should().NotBeNull();
    }

    [Fact]
    public void AddContext_WithSameKey_OverwritesPreviousValue()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("key", "value1");
        exception.Context.Should().ContainKey("key").WhoseValue.Should().Be("value1");

        // Act
        exception.AddContext("key", "value2");

        // Assert
        exception.Context["key"].Should().Be("value2");
        exception.Context.Should().HaveCount(1);
    }

    [Fact]
    public void GetContext_WithExistingKey_ReturnsValue()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("testKey", "testValue");

        // Act
        var result = exception.GetContext("testKey");

        // Assert
        result.Should().Be("testValue");
    }

    [Fact]
    public void GetContext_WithNonExistingKey_ReturnsNull()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("existingKey", "value");

        // Act
        var result = exception.GetContext("nonExistingKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetContext_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("key", "value");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.GetContext(null!));
    }

    [Fact]
    public void GetContext_WithEmptyStringKey_ReturnsNull()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        exception.AddContext("key", "value");

        // Act
        var result = exception.GetContext(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetContext_WithComplexValue_ReturnsCorrectValue()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        var complexValue = new { Id = 1, Name = "Test", Nested = new { Value = 42 } };
        exception.AddContext("complex", complexValue);

        // Act
        var result = exception.GetContext("complex");

        // Assert
        result.Should().BeSameAs(complexValue);
        result.Should().NotBeNull();
    }

    [Fact]
    public void WithContext_ChainsAndAddsContext()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");

        // Act
        var result = exception.WithContext("key1", "value1").WithContext("key2", "value2");

        // Assert
        result.Should().BeSameAs(exception);
        exception.Context.Should().HaveCount(2);
        exception.Context["key1"].Should().Be("value1");
        exception.Context["key2"].Should().Be("value2");
    }

    [Fact]
    public void WithInnerException_ChainsAndStoresInnerException()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message");
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var result = exception.WithInnerException(innerException);

        // Assert
        result.Should().BeSameAs(exception);
        exception.Data[nameof(innerException)].Should().BeSameAs(innerException);
    }

    [Fact]
    public void ToString_WithErrorCodeAndGrpcStatus_IncludesBothInOutput()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message", "TEST_CODE");
        exception.GrpcStatus = GrpcStatusCode.Unavailable;

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Test message");
        result.Should().Contain("TEST_CODE");
        result.Should().Contain("Unavailable");
    }

    [Fact]
    public void ToString_WithOnlyErrorCode_IncludesErrorCode()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message", "TEST_CODE");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Test message");
        result.Should().Contain("TEST_CODE");
        result.Should().NotContain("GrpcStatus:");
    }

    [Fact]
    public void ToString_WithOnlyGrpcStatus_IncludesGrpcStatus()
    {
        // Arrange
        var exception = new GrpcWebBridgeException("Test message", GrpcStatusCode.PermissionDenied);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Test message");
        result.Should().Contain("PermissionDenied");
        result.Should().NotContain("ErrorCode:");
    }

    [Fact]
    public void ToString_WithEmptyMessage_StillIncludesErrorDetails()
    {
        // Arrange
        var exception = new GrpcWebBridgeException(string.Empty, "EMPTY_MSG");
        exception.GrpcStatus = GrpcStatusCode.DataLoss;

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("ErrorCode: EMPTY_MSG");
        result.Should().Contain("DataLoss");
    }

    [Fact]
    public void Context_InitializedAsEmptyDictionary()
    {
        // Arrange & Act
        var exception = new GrpcWebBridgeException();

        // Assert
        exception.Context.Should().NotBeNull();
        exception.Context.Should().BeEmpty();
        exception.Context.Should().BeAssignableTo<Dictionary<string, object>>();
    }

    [Fact]
    public void ErrorCode_PropertyCanBeSetAndRead()
    {
        // Arrange
        var exception = new GrpcWebBridgeException();

        // Act
        exception.ErrorCode = "CUSTOM_ERROR";

        // Assert
        exception.ErrorCode.Should().Be("CUSTOM_ERROR");
    }

    [Fact]
    public void GrpcStatus_PropertyCanBeSetAndRead()
    {
        // Arrange
        var exception = new GrpcWebBridgeException();
        var statusCode = GrpcStatusCode.DeadlineExceeded;

        // Act
        exception.GrpcStatus = statusCode;

        // Assert
        exception.GrpcStatus.Should().Be(statusCode);
    }

    [Fact]
    public void Inheritance_ExtendsException()
    {
        // Arrange & Act
        var exception = new GrpcWebBridgeException("Test message");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}