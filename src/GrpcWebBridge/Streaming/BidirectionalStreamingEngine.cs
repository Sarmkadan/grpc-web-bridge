#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Threading.Channels;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Events;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Central engine that owns the full lifecycle of all bidirectional gRPC streams
/// within a single bridge instance.
/// <para>
/// Each <see cref="OpenStreamAsync"/> call allocates a <see cref="BidirectionalStreamContext"/>
/// with bounded inbound/outbound channels, a <see cref="BackpressureController"/> credit window,
/// and wraps them in a <see cref="FlowControlledStream"/> returned as
/// <see cref="IFlowControlledStream"/>.
/// </para>
/// <para>
/// A configurable global ceiling limits concurrent stream count. The engine publishes
/// <see cref="StreamStartedEvent"/> and <see cref="StreamEndedEvent"/> to the application
/// <see cref="EventBus"/> after each lifecycle transition, enabling diagnostics and session-
/// management components to react without coupling to the engine directly.
/// </para>
/// <para>
/// All public members are safe to call concurrently from multiple threads.
/// </para>
/// </summary>
public sealed class BidirectionalStreamingEngine : IBidirectionalStreamingEngine, IAsyncDisposable
{
    /// <summary>
    /// Bundles the live stream with the outbound channel's drain-completion task so that
    /// <see cref="CloseStreamAsync"/> can await a bounded grace period.
    /// </summary>
    private sealed record StreamEntry(FlowControlledStream Stream, Task OutboundDrained);

    private readonly ConcurrentDictionary<string, StreamEntry> _streams = new();
    private readonly FlowControlOptions _options;
    private readonly EventBus? _eventBus;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<BidirectionalStreamingEngine> _logger;
    private readonly int _maxStreams;
    private int _disposed;

    /// <inheritdoc/>
    public int ActiveStreamCount => _streams.Count;

    /// <summary>
    /// Initialises the engine with flow-control options and optional supporting services.
    /// </summary>
    /// <param name="loggerFactory">Factory for creating typed loggers for child components.</param>
    /// <param name="options">
    /// Flow-control configuration. Defaults to <see cref="FlowControlOptions"/> property defaults
    /// when <c>null</c>.
    /// </param>
    /// <param name="eventBus">
    /// Application event bus. When supplied, the engine publishes stream lifecycle events.
    /// </param>
    /// <param name="maxStreams">
    /// Global ceiling on concurrently open streams.
    /// Defaults to <see cref="Domain.Constants.Streaming.MaxStreamCount"/>.
    /// </param>
    public BidirectionalStreamingEngine(
        ILoggerFactory loggerFactory,
        FlowControlOptions? options = null,
        EventBus? eventBus = null,
        int maxStreams = Domain.Constants.Streaming.MaxStreamCount)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = loggerFactory.CreateLogger<BidirectionalStreamingEngine>();
        _options = options ?? new FlowControlOptions();
        _eventBus = eventBus;
        _maxStreams = maxStreams > 0 ? maxStreams : Domain.Constants.Streaming.MaxStreamCount;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="streamId"/> is <c>null</c> or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the global stream ceiling is reached or the stream ID is already registered.
    /// </exception>
    public Task<IFlowControlledStream> OpenStreamAsync(
        string streamId,
        MethodType methodType,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID must be a non-empty string.", nameof(streamId));

        if (_streams.Count >= _maxStreams)
            throw new InvalidOperationException(
                $"Global stream ceiling of {_maxStreams} concurrent streams has been reached.");

        if (_streams.ContainsKey(streamId))
            throw new InvalidOperationException(
                $"A stream with ID '{streamId}' is already registered.");

        var inbound = Channel.CreateBounded<StreamMessage>(new BoundedChannelOptions(_options.InboundChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

        var outbound = Channel.CreateBounded<StreamMessage>(new BoundedChannelOptions(_options.OutboundChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });

        var context = new BidirectionalStreamContext
        {
            StreamId = streamId,
            MethodType = methodType,
            InboundChannel = inbound,
            OutboundChannel = outbound,
            State = StreamState.Active
        };

        var controller = new BackpressureController(
            streamId,
            _options,
            _loggerFactory.CreateLogger<BackpressureController>(),
            _eventBus);

        var stream = new FlowControlledStream(
            context,
            controller,
            _options,
            _loggerFactory.CreateLogger<FlowControlledStream>());

        var entry = new StreamEntry(stream, outbound.Reader.Completion);

        if (!_streams.TryAdd(streamId, entry))
        {
            // A concurrent caller registered the same ID between our ContainsKey check and TryAdd.
            _ = stream.DisposeAsync().AsTask();
            throw new InvalidOperationException(
                $"Stream '{streamId}' was registered by a concurrent caller.");
        }

        _logger.LogInformation(
            "Opened stream {StreamId} — method={MethodType}, mode={Mode}, initialWindow={Window}.",
            streamId, methodType, _options.Mode, _options.InitialWindowSize);

        Publish(new StreamStartedEvent
        {
            StreamId = streamId,
            MethodName = methodType.ToString(),
            Source = nameof(BidirectionalStreamingEngine)
        });

        return Task.FromResult<IFlowControlledStream>(stream);
    }

    /// <inheritdoc/>
    public IFlowControlledStream? GetStream(string streamId) =>
        _streams.TryGetValue(streamId, out var entry) ? entry.Stream : null;

    /// <inheritdoc/>
    public async Task CloseStreamAsync(
        string streamId,
        GrpcStatusCode? finalStatus = null,
        CancellationToken cancellationToken = default)
    {
        if (!_streams.TryRemove(streamId, out var entry))
        {
            _logger.LogDebug(
                "CloseStreamAsync: stream '{StreamId}' not found — already closed.",
                streamId);
            return;
        }

        long startTick = Environment.TickCount64;
        var stream = entry.Stream;

        try
        {
            // Signal write-side completion so the transport reader observes channel EOF.
            await stream.CompleteWritingAsync().ConfigureAwait(false);

            // Give the transport a bounded grace window to drain any queued outbound messages.
            using var graceCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            graceCts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                await entry.OutboundDrained.WaitAsync(graceCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogDebug(
                    "Stream {StreamId}: outbound drain grace period elapsed — proceeding with close.",
                    streamId);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Stream {StreamId}: close cancelled by caller — aborting with {Status}.",
                streamId, finalStatus ?? GrpcStatusCode.Cancelled);

            await stream.AbortAsync(finalStatus ?? GrpcStatusCode.Cancelled, "Close was cancelled.").ConfigureAwait(false);
        }
        finally
        {
            long durationMs = Environment.TickCount64 - startTick;

            Publish(new StreamEndedEvent
            {
                StreamId = streamId,
                MessageCount = stream.Metrics.MessagesIn + stream.Metrics.MessagesOut,
                DurationMs = durationMs,
                Source = nameof(BidirectionalStreamingEngine)
            });

            await stream.DisposeAsync().ConfigureAwait(false);

            _logger.LogInformation(
                "Closed stream {StreamId} — finalStatus={Status}, durationMs={Duration}, " +
                "messagesIn={In}, messagesOut={Out}, backpressureEvents={BP}.",
                streamId,
                finalStatus ?? GrpcStatusCode.Ok,
                durationMs,
                stream.Metrics.MessagesIn,
                stream.Metrics.MessagesOut,
                stream.Metrics.BackpressureEvents);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, StreamThroughputMetrics> GetAllMetrics()
    {
        var snapshot = new Dictionary<string, StreamThroughputMetrics>(_streams.Count);

        foreach (var (id, entry) in _streams)
            snapshot[id] = entry.Stream.Metrics;

        return snapshot;
    }

    /// <summary>
    /// Closes all active streams concurrently with <see cref="GrpcStatusCode.Unavailable"/>,
    /// then disposes internal resources. Subsequent calls to <see cref="OpenStreamAsync"/>
    /// will throw <see cref="ObjectDisposedException"/>.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        var ids = _streams.Keys.ToArray();

        _logger.LogInformation(
            "BidirectionalStreamingEngine disposing — closing {Count} active stream(s).", ids.Length);

        await Task.WhenAll(ids.Select(id =>
            CloseStreamAsync(id, GrpcStatusCode.Unavailable)));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private void Publish<TEvent>(TEvent @event) where TEvent : EventBase
    {
        if (_eventBus is null) return;
        _ = _eventBus.PublishAsync(@event);
    }
}
