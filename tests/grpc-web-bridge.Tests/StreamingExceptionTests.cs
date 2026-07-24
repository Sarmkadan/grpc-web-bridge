#nullable enable
// =============================================================================
// Author: Automated Generation
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class StreamingExceptionTests
{
    [Fact]
    public void Constructor_NoParameters_CreatesExceptionWithDefaultValues()
    {
        // Act
        var exception = new StreamingException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeEmpty();
        exception.ErrorCode.Should().BeNull();
        exception.StreamId.Should().BeNull();
        exception.SequenceNumber.Should().BeNull();
        exception.LastStreamState.Should().BeNull();
        exception.GrpcStatus.Should().BeNull();
    }

    [Fact]
    public void Constructor_Message_CreatesExceptionWithMessageAndDefaultErrorCode()
    {
        // Arrange
        var message = "Test streaming error message";

        // Act
        var exception = new StreamingException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("STREAMING_ERROR");
        exception.StreamId.Should().BeNull();
        exception.SequenceNumber.Should().BeNull();
        exception.LastStreamState.Should().BeNull();
    }

    [Fact]
    public void Constructor_MessageAndInnerException_CreatesExceptionWithBoth()
    {
        // Arrange
        var message = "Test streaming error with inner exception";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new StreamingException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.ErrorCode.Should().Be("STREAMING_ERROR");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_StreamIdAndMessage_CreatesExceptionWithStreamIdAndFormattedMessage()
    {
        // Arrange
        var streamId = "test-stream-123";
        var message = "Stream failed due to network issues";

        // Act
        var exception = new StreamingException(streamId, message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Stream '{streamId}' error: {message}");
        exception.ErrorCode.Should().Be("STREAM_FAILED");
        exception.StreamId.Should().Be(streamId);
        exception.GrpcStatus.Should().Be(GrpcStatusCode.Internal);
        exception.SequenceNumber.Should().BeNull();
        exception.LastStreamState.Should().BeNull();
    }

    [Fact]
    public void Constructor_StreamIdSequenceNumberAndMessage_CreatesExceptionWithAllProperties()
    {
        // Arrange
        var streamId = "test-stream-456";
        var sequenceNumber = 42;
        var message = "Message processing failed";

        // Act
        var exception = new StreamingException(streamId, sequenceNumber, message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be($"Stream '{streamId}' message {sequenceNumber} error: {message}");
        exception.ErrorCode.Should().Be("STREAM_MESSAGE_ERROR");
        exception.StreamId.Should().Be(streamId);
        exception.SequenceNumber.Should().Be(sequenceNumber);
        exception.GrpcStatus.Should().Be(GrpcStatusCode.Internal);
        exception.LastStreamState.Should().BeNull();
    }

    [Fact]
    public void SetStreamState_SetsLastStreamStateAndAddsToContext()
    {
        // Arrange
        var exception = new StreamingException("Test message");
        var streamState = StreamState.Active;

        // Act
        exception.SetStreamState(streamState);

        // Assert
        exception.LastStreamState.Should().Be(streamState);
        exception.Context.Should().ContainKey("StreamState");
        exception.Context["StreamState"].Should().Be(streamState);
    }

    [Fact]
    public void ToString_WithStreamId_ReturnsFormattedStringWithStreamId()
    {
        // Arrange
        var streamId = "stream-789";
        var exception = new StreamingException(streamId, "Test error");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Stream:");
        result.Should().Contain(streamId);
    }

    [Fact]
    public void ToString_WithSequenceNumber_ReturnsFormattedStringWithSequenceNumber()
    {
        // Arrange
        var exception = new StreamingException("stream-101", 99, "Message error");

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Seq:");
        result.Should().Contain("99");
    }

    [Fact]
    public void ToString_WithLastStreamState_ReturnsFormattedStringWithState()
    {
        // Arrange
        var exception = new StreamingException("stream-202");
        exception.SetStreamState(StreamState.Failed);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("State:");
        result.Should().Contain("Failed");
    }

    [Fact]
    public void ToString_WithAllProperties_ReturnsCompleteFormattedString()
    {
        // Arrange
        var exception = new StreamingException("stream-303", 123, "Complete error");
        exception.SetStreamState(StreamState.HalfClosed);

        // Act
        var result = exception.ToString();

        // Assert
        result.Should().Contain("Stream:");
        result.Should().Contain("stream-303");
        result.Should().Contain("Seq:");
        result.Should().Contain("123");
        result.Should().Contain("State:");
        result.Should().Contain("HalfClosed");
    }

    [Fact]
    public void Properties_CanBeSetIndividually()
    {
        // Arrange
        var exception = new StreamingException();

        // Act
        exception.StreamId = "custom-stream-id";
        exception.SequenceNumber = 100;
        exception.LastStreamState = StreamState.Closed;

        // Assert
        exception.StreamId.Should().Be("custom-stream-id");
        exception.SequenceNumber.Should().Be(100);
        exception.LastStreamState.Should().Be(StreamState.Closed);
    }

    [Fact]
    public void SetStreamState_OverwritesPreviousState()
    {
        // Arrange
        var exception = new StreamingException("Test message");
        exception.SetStreamState(StreamState.New);
        exception.SetStreamState(StreamState.Active);

        // Act
        exception.SetStreamState(StreamState.Failed);

        // Assert
        exception.LastStreamState.Should().Be(StreamState.Failed);
        exception.Context["StreamState"].Should().Be(StreamState.Failed);
    }

    [Fact]
    public void ErrorCode_DefaultsToNull()
    {
        // Arrange & Act
        var exception = new StreamingException();

        // Assert
        exception.ErrorCode.Should().BeNull();
    }

    [Fact]
    public void GrpcStatus_DefaultsToNull()
    {
        // Arrange & Act
        var exception = new StreamingException();

        // Assert
        exception.GrpcStatus.Should().BeNull();
    }

    [Fact]
    public void Context_InitializedAsEmptyDictionary()
    {
        // Arrange & Act
        var exception = new StreamingException();

        // Assert
        exception.Context.Should().NotBeNull();
        exception.Context.Should().BeEmpty();
    }
}