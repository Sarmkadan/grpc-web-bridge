// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Services;

/// <summary>
/// Service for managing gRPC streaming operations and stream lifecycle
/// </summary>
public class StreamingService
{
    private readonly ILogger<StreamingService> _logger;
    private readonly Dictionary<string, Stream> _streams = [];
    private readonly object _streamsLock = new();

    public int ActiveStreamCount => _streams.Count;

    public StreamingService(ILogger<StreamingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new stream and registers it
    /// </summary>
    public Stream CreateStream(string streamId, MethodType methodType)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be empty", nameof(streamId));

        lock (_streamsLock)
        {
            if (ActiveStreamCount >= Constants.Streaming.MaxStreamCount)
                throw new StreamingException($"Maximum stream count ({Constants.Streaming.MaxStreamCount}) reached");

            if (_streams.ContainsKey(streamId))
                throw new StreamingException(streamId, "Stream already exists");

            var stream = new Stream(streamId, methodType);
            _streams[streamId] = stream;

            _logger.LogInformation("Stream created: {StreamId} ({MethodType})", streamId, methodType);

            return stream;
        }
    }

    /// <summary>
    /// Retrieves an active stream
    /// </summary>
    public Stream? GetStream(string streamId)
    {
        lock (_streamsLock)
        {
            return _streams.TryGetValue(streamId, out var stream) ? stream : null;
        }
    }

    /// <summary>
    /// Adds a message to the stream queue
    /// </summary>
    public void EnqueueMessage(string streamId, StreamMessage message)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be empty", nameof(streamId));

        if (message is null)
            throw new ArgumentNullException(nameof(message));

        message.Validate();

        var stream = GetStream(streamId);
        if (stream is null)
            throw new StreamingException(streamId, "Stream not found");

        stream.EnqueueMessage(message);
        _logger.LogDebug("Message enqueued to stream {StreamId}: seq {Sequence}", streamId, message.SequenceNumber);
    }

    /// <summary>
    /// Dequeues the next message from stream
    /// </summary>
    public StreamMessage? DequeueMessage(string streamId)
    {
        var stream = GetStream(streamId);
        if (stream is null)
            return null;

        return stream.DequeueMessage();
    }

    /// <summary>
    /// Closes a stream and releases resources
    /// </summary>
    public void CloseStream(string streamId, GrpcStatusCode? statusCode = null, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be empty", nameof(streamId));

        lock (_streamsLock)
        {
            if (_streams.Remove(streamId, out var stream))
            {
                stream.Close(statusCode, message);
                _logger.LogInformation("Stream closed: {StreamId}", streamId);
            }
        }
    }

    /// <summary>
    /// Sends heartbeat message for idle stream detection
    /// </summary>
    public void SendHeartbeat(string streamId)
    {
        var stream = GetStream(streamId);
        if (stream is null)
            throw new StreamingException(streamId, "Stream not found");

        var heartbeat = new StreamMessage(streamId, stream.MessageCount, StreamMessageType.Heartbeat);

        stream.EnqueueMessage(heartbeat);
        _logger.LogDebug("Heartbeat sent to stream: {StreamId}", streamId);
    }

    /// <summary>
    /// Removes streams that have exceeded idle timeout
    /// </summary>
    public void CleanupIdleStreams()
    {
        var timeoutThreshold = DateTime.UtcNow.AddSeconds(-Constants.Streaming.StreamIdleTimeoutSeconds);
        List<string> streamsToClose = [];

        lock (_streamsLock)
        {
            foreach (var kvp in _streams)
            {
                if (kvp.Value.LastActivityTime < timeoutThreshold)
                    streamsToClose.Add(kvp.Key);
            }
        }

        foreach (var streamId in streamsToClose)
        {
            CloseStream(streamId, GrpcStatusCode.DeadlineExceeded, "Stream idle timeout");
        }

        if (streamsToClose.Count > 0)
            _logger.LogInformation("Cleaned up {Count} idle streams", streamsToClose.Count);
    }

    /// <summary>
    /// Gets all active streams
    /// </summary>
    public IEnumerable<string> GetAllStreamIds()
    {
        lock (_streamsLock)
        {
            return _streams.Keys.ToList();
        }
    }

    /// <summary>
    /// Gets stream statistics
    /// </summary>
    public StreamStatistics GetStreamStatistics(string streamId)
    {
        var stream = GetStream(streamId);
        if (stream is null)
            throw new StreamingException(streamId, "Stream not found");

        return new StreamStatistics
        {
            StreamId = streamId,
            MessageCount = stream.MessageCount,
            QueuedMessageCount = stream.QueuedMessageCount,
            State = stream.State,
            CreatedAt = stream.CreatedAt,
            LastActivityTime = stream.LastActivityTime,
            DurationSeconds = (int)(DateTime.UtcNow - stream.CreatedAt).TotalSeconds
        };
    }
}

/// <summary>
/// Represents an active stream
/// </summary>
public class Stream
{
    private readonly Queue<StreamMessage> _messageQueue = [];

    public string StreamId { get; set; }
    public MethodType MethodType { get; set; }
    public StreamState State { get; set; } = StreamState.New;
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
    public GrpcStatusCode? FinalStatus { get; set; }
    public string? FinalMessage { get; set; }

    public int QueuedMessageCount => _messageQueue.Count;

    public Stream(string streamId, MethodType methodType)
    {
        StreamId = streamId;
        MethodType = methodType;
        State = StreamState.Active;
    }

    public void EnqueueMessage(StreamMessage message)
    {
        _messageQueue.Enqueue(message);
        MessageCount++;
        LastActivityTime = DateTime.UtcNow;
    }

    public StreamMessage? DequeueMessage()
    {
        LastActivityTime = DateTime.UtcNow;
        return _messageQueue.Count > 0 ? _messageQueue.Dequeue() : null;
    }

    public void Close(GrpcStatusCode? statusCode = null, string? message = null)
    {
        State = StreamState.Closed;
        FinalStatus = statusCode ?? GrpcStatusCode.Ok;
        FinalMessage = message;
        LastActivityTime = DateTime.UtcNow;
        _messageQueue.Clear();
    }
}

/// <summary>
/// Statistics about an active stream
/// </summary>
public class StreamStatistics
{
    public string? StreamId { get; set; }
    public int MessageCount { get; set; }
    public int QueuedMessageCount { get; set; }
    public StreamState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityTime { get; set; }
    public int DurationSeconds { get; set; }
}
