// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Exception thrown during streaming operations
/// </summary>
public class StreamingException : GrpcWebBridgeException
{
    public string? StreamId { get; set; }
    public StreamState? LastStreamState { get; set; }
    public int? SequenceNumber { get; set; }

    public StreamingException() : base() { }

    public StreamingException(string message) : base(message, "STREAMING_ERROR") { }

    public StreamingException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "STREAMING_ERROR";
    }

    public StreamingException(string streamId, string message)
        : base($"Stream '{streamId}' error: {message}", "STREAM_FAILED")
    {
        StreamId = streamId;
        GrpcStatus = GrpcStatusCode.Internal;
    }

    public StreamingException(string streamId, int sequenceNumber, string message)
        : base($"Stream '{streamId}' message {sequenceNumber} error: {message}", "STREAM_MESSAGE_ERROR")
    {
        StreamId = streamId;
        SequenceNumber = sequenceNumber;
        GrpcStatus = GrpcStatusCode.Internal;
    }

    public void SetStreamState(StreamState state)
    {
        LastStreamState = state;
        AddContext("StreamState", state);
    }

    public override string ToString()
    {
        var result = base.ToString();
        if (!string.IsNullOrEmpty(StreamId))
            result += $" | Stream: {StreamId}";

        if (SequenceNumber.HasValue)
            result += $" | Seq: {SequenceNumber}";

        if (LastStreamState.HasValue)
            result += $" | State: {LastStreamState}";

        return result;
    }
}
