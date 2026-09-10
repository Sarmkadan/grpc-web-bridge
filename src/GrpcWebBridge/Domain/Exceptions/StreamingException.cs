#nullable enable
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
    /// <summary>Identifier of the stream where the exception occurred.</summary>
    public string? StreamId { get; set; }
    /// <summary>The last known state of the stream before the exception.</summary>
    public StreamState? LastStreamState { get; set; }
    /// <summary>The sequence number of the message in the stream that caused the exception (if applicable).</summary>
    public int? SequenceNumber { get; set; }

    /// <summary>Initializes a new instance of the <see cref="StreamingException"/> class.</summary>
    public StreamingException() : base() { }

    /// <summary>Initializes a new instance of the <see cref="StreamingException"/> class with a specified error message.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public StreamingException(string message) : base(message, "STREAMING_ERROR") { }

    /// <summary>Initializes a new instance of the <see cref="StreamingException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public StreamingException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "STREAMING_ERROR";
    }

    /// <summary>Initializes a new instance of the <see cref="StreamingException"/> class for a specific stream with a specified error message.</summary>
    /// <param name="streamId">The identifier of the stream where the exception occurred.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public StreamingException(string streamId, string message)
        : base($"Stream '{streamId}' error: {message}", "STREAM_FAILED")
    {
        StreamId = streamId;
        GrpcStatus = GrpcStatusCode.Internal;
    }

    /// <summary>Initializes a new instance of the <see cref="StreamingException"/> class for a specific stream and message sequence number with a specified error message.</summary>
    /// <param name="streamId">The identifier of the stream where the exception occurred.</param>
    /// <param name="sequenceNumber">The sequence number of the message in the stream that caused the exception.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public StreamingException(string streamId, int sequenceNumber, string message)
        : base($"Stream '{streamId}' message {sequenceNumber} error: {message}", "STREAM_MESSAGE_ERROR")
    {
        StreamId = streamId;
        SequenceNumber = sequenceNumber;
        GrpcStatus = GrpcStatusCode.Internal;
    }

    /// <summary>Sets the last known state of the stream.</summary>
    /// <param name="state">The state to set for the stream.</param>
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
