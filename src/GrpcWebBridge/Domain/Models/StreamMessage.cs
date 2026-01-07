// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a single message within a gRPC stream
/// </summary>
public class StreamMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string StreamId { get; set; } = string.Empty;
    public StreamMessageType MessageType { get; set; } = StreamMessageType.Data;
    public int SequenceNumber { get; set; }
    public byte[] Data { get; set; } = [];
    public SerializationFormat Format { get; set; } = SerializationFormat.Protobuf;
    public Dictionary<string, string>? Headers { get; set; }
    public GrpcStatusCode? Status { get; set; }
    public string? StatusMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCompressed { get; set; }
    public int? CompressionLevel { get; set; }

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

    public void SetData(byte[] data, SerializationFormat format = SerializationFormat.Protobuf)
    {
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        Data = data;
        Format = format;
        MessageType = StreamMessageType.Data;
    }

    public void SetMetadata(Dictionary<string, string> headers)
    {
        if (headers is null)
            throw new ArgumentNullException(nameof(headers));

        Headers = new Dictionary<string, string>(headers);
        MessageType = StreamMessageType.Metadata;
    }

    public void SetStatus(GrpcStatusCode status, string? message = null)
    {
        Status = status;
        StatusMessage = message;
        MessageType = StreamMessageType.Status;
    }

    public void SetHeartbeat()
    {
        MessageType = StreamMessageType.Heartbeat;
        Data = [];
    }

    public void SetError(GrpcResponse errorResponse)
    {
        if (errorResponse is null)
            throw new ArgumentNullException(nameof(errorResponse));

        ErrorResponse = errorResponse;
        MessageType = StreamMessageType.Error;
        Status = errorResponse.Status;
        StatusMessage = errorResponse.StatusMessage;
    }

    public void EnableCompression(int level = 6)
    {
        if (level < 0 || level > 9)
            throw new ArgumentException("Compression level must be between 0 and 9", nameof(level));

        IsCompressed = true;
        CompressionLevel = level;
    }

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

    public override bool Equals(object? obj)
    {
        if (obj is not StreamMessage other)
            return false;

        return Id == other.Id && StreamId == other.StreamId && SequenceNumber == other.SequenceNumber;
    }

    public override int GetHashCode() => HashCode.Combine(Id, StreamId, SequenceNumber);
}
