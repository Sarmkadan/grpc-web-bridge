#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for StreamMessage class
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the <see cref="StreamMessage"/> class.
/// </summary>
public sealed class StreamMessageTests
{
    private const string TestStreamId = "test-stream-123";
    private const int TestSequenceNumber = 42;
    private static readonly byte[] TestData = [0x01, 0x02, 0x03, 0x04];
    private static readonly Dictionary<string, string> TestHeaders = new() { { "key1", "value1" }, { "key2", "value2" } };

    /// <summary>
    /// Verifies that the default constructor initializes properties correctly.
    /// </summary>
    [Fact]
    public void DefaultConstructor_InitializesProperties()
    {
        // Act
        var message = new StreamMessage();

        // Assert
        message.Id.Should().NotBeNullOrEmpty();
        message.StreamId.Should().BeEmpty();
        message.MessageType.Should().Be(StreamMessageType.Data);
        message.SequenceNumber.Should().Be(0);
        message.Data.Should().BeEmpty();
        message.Format.Should().Be(SerializationFormat.Protobuf);
        message.Headers.Should().BeNull();
        message.Status.Should().BeNull();
        message.StatusMessage.Should().BeNull();
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.IsCompressed.Should().BeFalse();
        message.CompressionLevel.Should().BeNull();
        message.ErrorResponse.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the parameterized constructor with streamId, sequenceNumber, and data initializes correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithStreamIdSequenceNumberData_InitializesCorrectly()
    {
        // Act
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);

        // Assert
        message.Id.Should().NotBeNullOrEmpty();
        message.StreamId.Should().Be(TestStreamId);
        message.MessageType.Should().Be(StreamMessageType.Data);
        message.SequenceNumber.Should().Be(TestSequenceNumber);
        message.Data.Should().BeEquivalentTo(TestData);
        message.Format.Should().Be(SerializationFormat.Protobuf);
        message.Headers.Should().BeNull();
        message.Status.Should().BeNull();
        message.StatusMessage.Should().BeNull();
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Verifies that the parameterized constructor with streamId, sequenceNumber, and type initializes correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithStreamIdSequenceNumberType_InitializesCorrectly()
    {
        // Act
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, StreamMessageType.Metadata);

        // Assert
        message.Id.Should().NotBeNullOrEmpty();
        message.StreamId.Should().Be(TestStreamId);
        message.MessageType.Should().Be(StreamMessageType.Metadata);
        message.SequenceNumber.Should().Be(TestSequenceNumber);
        message.Data.Should().BeEmpty();
        message.Format.Should().Be(SerializationFormat.Protobuf);
    }

    /// <summary>
    /// Verifies that SetData correctly updates message properties.
    /// </summary>
    [Fact]
    public void SetData_UpdatesPropertiesCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.SetData(TestData, SerializationFormat.Json);

        // Assert
        message.Data.Should().BeEquivalentTo(TestData);
        message.Format.Should().Be(SerializationFormat.Json);
        message.MessageType.Should().Be(StreamMessageType.Data);
    }

    /// <summary>
    /// Verifies that SetData throws ArgumentNullException for null data.
    /// </summary>
    [Fact]
    public void SetData_WithNullData_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => message.SetData(null!));
    }

    /// <summary>
    /// Verifies that SetMetadata correctly updates message properties.
    /// </summary>
    [Fact]
    public void SetMetadata_UpdatesPropertiesCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.SetMetadata(TestHeaders);

        // Assert
        message.Headers.Should().NotBeNull();
        message.Headers.Should().HaveCount(2);
        message.Headers.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
        message.Headers.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
        message.MessageType.Should().Be(StreamMessageType.Metadata);
    }

    /// <summary>
    /// Verifies that SetMetadata throws ArgumentNullException for null headers.
    /// </summary>
    [Fact]
    public void SetMetadata_WithNullHeaders_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => message.SetMetadata(null!));
    }

    /// <summary>
    /// Verifies that SetMetadata creates a copy of the headers dictionary.
    /// </summary>
    [Fact]
    public void SetMetadata_CreatesCopyOfHeaders()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);
        var originalHeaders = new Dictionary<string, string> { { "key", "value" } };

        // Act
        message.SetMetadata(originalHeaders);
        originalHeaders.Add("newKey", "newValue");

        // Assert
        message.Headers.Should().HaveCount(1);
        message.Headers.Should().NotContainKey("newKey");
    }

    /// <summary>
    /// Verifies that SetStatus correctly updates message properties.
    /// </summary>
    [Fact]
    public void SetStatus_UpdatesPropertiesCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);
        const string statusMessage = "Stream completed successfully";

        // Act
        message.SetStatus(GrpcStatusCode.Ok, statusMessage);

        // Assert
        message.Status.Should().Be(GrpcStatusCode.Ok);
        message.StatusMessage.Should().Be(statusMessage);
        message.MessageType.Should().Be(StreamMessageType.Status);
    }

    /// <summary>
    /// Verifies that SetStatus works with null status message.
    /// </summary>
    [Fact]
    public void SetStatus_WithNullMessage_UpdatesPropertiesCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.SetStatus(GrpcStatusCode.Internal);

        // Assert
        message.Status.Should().Be(GrpcStatusCode.Internal);
        message.StatusMessage.Should().BeNull();
        message.MessageType.Should().Be(StreamMessageType.Status);
    }

    /// <summary>
    /// Verifies that SetHeartbeat updates message type correctly.
    /// </summary>
    [Fact]
    public void SetHeartbeat_UpdatesMessageType()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);

        // Act
        message.SetHeartbeat();

        // Assert
        message.MessageType.Should().Be(StreamMessageType.Heartbeat);
        message.Data.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that SetError correctly updates message properties.
    /// </summary>
    [Fact]
    public void SetError_UpdatesPropertiesCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);
        var errorResponse = new GrpcResponse("req-123", GrpcStatusCode.Internal, "Internal server error");

        // Act
        message.SetError(errorResponse);

        // Assert
        message.ErrorResponse.Should().BeSameAs(errorResponse);
        message.Status.Should().Be(GrpcStatusCode.Internal);
        message.StatusMessage.Should().Be("Internal server error");
        message.MessageType.Should().Be(StreamMessageType.Error);
    }

    /// <summary>
    /// Verifies that SetError throws ArgumentNullException for null error response.
    /// </summary>
    [Fact]
    public void SetError_WithNullErrorResponse_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => message.SetError(null!));
    }

    /// <summary>
    /// Verifies that EnableCompression correctly enables compression.
    /// </summary>
    [Fact]
    public void EnableCompression_EnablesCompression()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.EnableCompression(5);

        // Assert
        message.IsCompressed.Should().BeTrue();
        message.CompressionLevel.Should().Be(5);
    }

    /// <summary>
    /// Verifies that EnableCompression with default level works correctly.
    /// </summary>
    [Fact]
    public void EnableCompression_WithDefaultLevel_EnablesCompression()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.EnableCompression();

        // Assert
        message.IsCompressed.Should().BeTrue();
        message.CompressionLevel.Should().Be(6);
    }

    /// <summary>
    /// Verifies that EnableCompression throws ArgumentException for invalid compression level.
    /// </summary>
    /// <param name="level">The invalid compression level.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void EnableCompression_WithInvalidLevel_ThrowsArgumentException(int level)
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => message.EnableCompression(level));
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException for empty StreamId.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyStreamId_ThrowsArgumentException()
    {
        // Arrange
        var message = new StreamMessage { StreamId = "   " };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => message.Validate());
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException for negative sequence number.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeSequenceNumber_ThrowsArgumentException()
    {
        // Arrange
        var message = new StreamMessage { StreamId = TestStreamId, SequenceNumber = -1 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => message.Validate());
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException for empty data when message type is Data.
    /// </summary>
    [Fact]
    public void Validate_WithEmptyDataAndDataType_ThrowsArgumentException()
    {
        // Arrange
        var message = new StreamMessage { StreamId = TestStreamId, SequenceNumber = 1, MessageType = StreamMessageType.Data };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => message.Validate());
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException for null ErrorResponse when message type is Error.
    /// </summary>
    [Fact]
    public void Validate_WithErrorTypeAndNullErrorResponse_ThrowsArgumentException()
    {
        // Arrange
        var message = new StreamMessage { StreamId = TestStreamId, SequenceNumber = 1, MessageType = StreamMessageType.Error };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => message.Validate());
    }

    /// <summary>
    /// Verifies that Validate succeeds for valid message.
    /// </summary>
    [Fact]
    public void Validate_WithValidMessage_Succeeds()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);

        // Act
        var act = () => message.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that GetDataCopy returns a copy of the data.
    /// </summary>
    [Fact]
    public void GetDataCopy_ReturnsCopyOfData()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);

        // Act
        var dataCopy = message.GetDataCopy();

        // Assert
        dataCopy.Should().BeEquivalentTo(TestData);
        dataCopy.Should().NotBeSameAs(TestData);
        ReferenceEquals(dataCopy, TestData).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GetDataCopy returns empty array for empty data.
    /// </summary>
    [Fact]
    public void GetDataCopy_WithEmptyData_ReturnsEmptyArray()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        var dataCopy = message.GetDataCopy();

        // Assert
        dataCopy.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ToString returns expected format.
    /// </summary>
    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);

        // Act
        var result = message.ToString();

        // Assert
        result.Should().Contain(message.Id);
        result.Should().Contain(TestStreamId);
        result.Should().Contain(TestSequenceNumber.ToString());
        result.Should().Contain(StreamMessageType.Data.ToString());
    }

    /// <summary>
    /// Verifies that Equals returns true for messages with same Id, StreamId, and SequenceNumber.
    /// </summary>
    [Fact]
    public void Equals_ReturnsTrueForSameIdStreamIdSequenceNumber()
    {
        // Arrange
        var message1 = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);
        var message2 = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Equals returns false for messages with different Id.
    /// </summary>
    [Fact]
    public void Equals_ReturnsFalseForDifferentId()
    {
        // Arrange
        var message1 = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);
        var message2 = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);

        // Change Id of message2
        message2.Id = Guid.NewGuid().ToString("N");

        // Act
        var result = message1.Equals(message2);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GetHashCode returns same value for messages with same Id, StreamId, and SequenceNumber.
    /// </summary>
    [Fact]
    public void GetHashCode_ReturnsSameValueForSameIdStreamIdSequenceNumber()
    {
        // Arrange
        var message1 = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);
        var message2 = new StreamMessage(TestStreamId, TestSequenceNumber, TestData);

        // Act
        var hash1 = message1.GetHashCode();
        var hash2 = message2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    /// <summary>
    /// Verifies that constructor validates StreamId and throws ArgumentException for empty or whitespace.
    /// </summary>
    /// <param name="streamId">The stream ID to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ValidatesStreamId(string? streamId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StreamMessage(streamId!, TestSequenceNumber, TestData));
    }

    /// <summary>
    /// Verifies that constructor validates sequence number and throws ArgumentException for negative values.
    /// </summary>
    [Fact]
    public void Constructor_ValidatesSequenceNumber()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StreamMessage(TestStreamId, -1, TestData));
    }

    /// <summary>
    /// Verifies that constructor with type validates StreamId and throws ArgumentException for empty or whitespace.
    /// </summary>
    /// <param name="streamId">The stream ID to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorWithType_ValidatesStreamId(string? streamId)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StreamMessage(streamId!, TestSequenceNumber, StreamMessageType.Data));
    }

    /// <summary>
    /// Verifies that constructor with type validates sequence number and throws ArgumentException for negative values.
    /// </summary>
    [Fact]
    public void ConstructorWithType_ValidatesSequenceNumber()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StreamMessage(TestStreamId, -1, StreamMessageType.Data));
    }

    /// <summary>
    /// Verifies that Data property setter works correctly.
    /// </summary>
    [Fact]
    public void DataProperty_SetterWorksCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);
        var newData = new byte[] { 0xFF, 0xFE };

        // Act
        message.Data = newData;

        // Assert
        message.Data.Should().BeEquivalentTo(newData);
    }

    /// <summary>
    /// Verifies that Headers property setter works correctly.
    /// </summary>
    [Fact]
    public void HeadersProperty_SetterWorksCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);
        var newHeaders = new Dictionary<string, string> { { "header1", "value1" } };

        // Act
        message.Headers = newHeaders;

        // Assert
        message.Headers.Should().BeSameAs(newHeaders);
    }

    /// <summary>
    /// Verifies that Status property setter works correctly.
    /// </summary>
    [Fact]
    public void StatusProperty_SetterWorksCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.Status = GrpcStatusCode.Unavailable;

        // Assert
        message.Status.Should().Be(GrpcStatusCode.Unavailable);
    }

    /// <summary>
    /// Verifies that StatusMessage property setter works correctly.
    /// </summary>
    [Fact]
    public void StatusMessageProperty_SetterWorksCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.StatusMessage = "Service unavailable";

        // Assert
        message.StatusMessage.Should().Be("Service unavailable");
    }

    /// <summary>
    /// Verifies that CreatedAt property setter works correctly.
    /// </summary>
    [Fact]
    public void CreatedAtProperty_SetterWorksCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);
        var newDate = DateTime.UtcNow.AddHours(-1);

        // Act
        message.CreatedAt = newDate;

        // Assert
        message.CreatedAt.Should().Be(newDate);
    }

    /// <summary>
    /// Verifies that IsCompressed property setter works correctly.
    /// </summary>
    [Fact]
    public void IsCompressedProperty_SetterWorksCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.IsCompressed = true;

        // Assert
        message.IsCompressed.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CompressionLevel property setter works correctly.
    /// </summary>
    [Fact]
    public void CompressionLevelProperty_SetterWorksCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);

        // Act
        message.CompressionLevel = 9;

        // Assert
        message.CompressionLevel.Should().Be(9);
    }

    /// <summary>
    /// Verifies that ErrorResponse property setter works correctly.
    /// </summary>
    [Fact]
    public void ErrorResponseProperty_SetterWorksCorrectly()
    {
        // Arrange
        var message = new StreamMessage(TestStreamId, TestSequenceNumber, []);
        var errorResponse = new GrpcResponse("req-456", GrpcStatusCode.FailedPrecondition, "Precondition failed");

        // Act
        message.ErrorResponse = errorResponse;

        // Assert
        message.ErrorResponse.Should().BeSameAs(errorResponse);
    }
}