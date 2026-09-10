#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a single message within a gRPC stream
/// </summary>
public sealed class StreamMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Identifier of the stream to which this message belongs
    /// </summary>
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Type of the message (Data, Metadata, Status, Heartbeat, Error)
    /// </summary>
    public StreamMessageType MessageType { get; set; } = StreamMessageType.Data;

    /// <summary>
    /// Sequence number of the message within the stream
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// Payload data of the message
    /// </summary>
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// Serialization format of the data
    /// </summary>
    public SerializationFormat Format { get; set; } = SerializationFormat.Protobuf;

    /// <summary>
    /// Optional metadata headers
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// gRPC status code (if the message is a status message)
    /// </summary>
    public GrpcStatusCode? Status { get; set; }

    /// <summary>
    /// Optional status message
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates whether the data is compressed
    /// </summary>
    public bool IsCompressed { get; set; }

    /// <summary>
    /// Level of compression applied (0-9), if compressed
    /// </summary>
    public int? CompressionLevel { get; set; }

    /// <summary>
    /// Error response details (if the message is an error)
    /// </summary>
    public GrpcResponse? ErrorResponse { get; set; }

    public StreamMessage() { }

    public StreamMessage(string streamId, int sequenceNumber, byte[] data)
    {
        StreamId = ValidateStreamId(streamId);
        SequenceNumber = ValidateSequenceNumber(sequenceNumber);
        Data = data ?? [];
        MessageType = StreamMessageType.Data;
    }

    public StreamMessage(string streamId, int sequenceNumber, StreamMessageType type)
    {
        StreamId = ValidateStreamId(streamId);
        SequenceNumber = ValidateSequenceNumber(sequenceNumber);
        MessageType = type;
    }

    /// <summary>
    /// Sets the data and format of the message, and sets the message type to Data
    /// </summary>
    /// <param name="data">The data to set</param>
    /// <param name="format">The serialization format (default is Protobuf)</param>
    /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
    public void SetData(byte[] data, SerializationFormat format = SerializationFormat.Protobuf)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        Data = data;
        Format = format;
        MessageType = StreamMessageType.Data;
    }

    /// <summary>
    /// Sets the headers and sets the message type to Metadata
    /// </summary>
    /// <param name="headers">The headers to set</param>
    /// <exception cref="ArgumentNullException">Thrown when headers is null</exception>
    public void SetMetadata(Dictionary<string, string> headers)
    {
        if (headers is null)
            throw new ArgumentNullException(nameof(headers));

        Headers = new Dictionary<string, string>(headers);
        MessageType = StreamMessageType.Metadata;
    }

    /// <summary>
    /// Sets the status and status message, and sets the message type to Status
    /// </summary>
    /// <param name="status">The gRPC status code</param>
    /// <param name="message">Optional status message</param>
    public void SetStatus(GrpcStatusCode status, string? message = null)
    {
        Status = status;
        StatusMessage = message;
        MessageType = StreamMessageType.Status;
    }

    /// <summary>
    /// Sets the message type to Heartbeat and clears the data
    /// </summary>
    public void SetHeartbeat()
    {
        MessageType = StreamMessageType.Heartbeat;
        Data = [];
    }

    /// <summary>
    /// Sets the error response and sets the message type to Error. Also sets the status and status message from the error response
    /// </summary>
    /// <param name="errorResponse">The error response</param>
    /// <exception cref="ArgumentNullException">Thrown when errorResponse is null</exception>
    public void SetError(GrpcResponse errorResponse)
    {
        if (errorResponse is null)
            throw new ArgumentNullException(nameof(errorResponse));

        ErrorResponse = errorResponse;
        MessageType = StreamMessageType.Error;
        Status = errorResponse.Status;
        StatusMessage = errorResponse.StatusMessage;
    }

    /// <summary>
    /// Enables compression and sets the compression level
    /// </summary>
    /// <param name="level">The compression level (0-9). Default is 6.</param>
    /// <exception cref="ArgumentException">Thrown when level is less than 0 or greater than 9</exception>
    public void EnableCompression(int level = 6)
    {
        if (level < 0 || level > 9)
            throw new ArgumentException("Compression level must be between 0 and 9", nameof(level));

        IsCompressed = true;
        CompressionLevel = level;
    }

    /// <summary>
    /// Validates the message. Throws exceptions if the message is invalid
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when StreamId is empty, SequenceNumber is negative, Data is empty for Data message, ErrorResponse is null for Error message, or Data exceeds maximum size</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(StreamId))
            throw new ArgumentException("Stream ID cannot be empty", nameof(StreamId));

        if (SequenceNumber < 0)
            throw new ArgumentException("Sequence number cannot be negative", nameof(SequenceNumber));

        if (MessageType == StreamMessageType.Data && Data.Length == 0)
            throw new ArgumentException("Data message must contain data", nameof(Data));

        if (MessageType == StreamMessageType.Error && ErrorResponse is null)
            throw new ArgumentException("Error message must have error response", nameof(ErrorResponse));

        if (Data.Length > Constants.Streaming.DefaultBufferSize * 2)
            throw new ArgumentException("Message data exceeds maximum size", nameof(Data));
    }

    /// <summary>
    /// Returns a copy of the data
    /// </summary>
    /// <returns>A copy of the data</returns>
    public byte[] GetDataCopy() => (byte[])Data.Clone();

    private static string ValidateStreamId(string streamId)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be empty", nameof(streamId));
        return streamId.Trim();
    }

    private static int ValidateSequenceNumber(int sequenceNumber)
    {
        if (sequenceNumber < 0)
            throw new ArgumentException("Sequence number cannot be negative", nameof(sequenceNumber));
        return sequenceNumber;
    }

    public override string ToString() => $"Message {Id} in stream {StreamId} (seq: {SequenceNumber}, type: {MessageType})";

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// </summary>
    /// <param name="obj">The object to compare</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not StreamMessage other)
            return false;

        return Id == other.Id && StreamId == other.StreamId && SequenceNumber == other.SequenceNumber;
    }

    /// <summary>
    /// Serves as the default hash function
    /// </summary>
    /// <returns>A hash code for the current object</returns>
    public override int GetHashCode() => HashCode.Combine(Id, StreamId, SequenceNumber);
}