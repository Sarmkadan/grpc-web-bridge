#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading.Channels;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Events;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Encapsulates all mutable state for a single active bidirectional stream,
/// including the underlying channel pair, lifetime management, and live metrics.
/// Instances are created and owned exclusively by <see cref="BidirectionalStreamingEngine"/>.
/// </summary>
public sealed class BidirectionalStreamContext : IAsyncDisposable
{
    private int _disposed;

    /// <summary>Unique stream identifier.</summary>
    public required string StreamId { get; init; }

    /// <summary>
    /// gRPC method type. Bidirectional contexts support
    /// <see cref="MethodType.BidirectionalStreaming"/>, <see cref="MethodType.ClientStreaming"/>,
    /// and <see cref="MethodType.ServerStreaming"/>.
    /// </summary>
    public required MethodType MethodType { get; init; }

    /// <summary>UTC timestamp at which this context was instantiated.</summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Current lifecycle state. Transitions are managed exclusively by the engine.
    /// </summary>
    public StreamState State { get; set; } = StreamState.New;

    /// <summary>
    /// Bounded channel carrying messages arriving from the remote peer (inbound).
    /// The transport layer writes to it; application consumers read from it.
    /// </summary>
    public required Channel<StreamMessage> InboundChannel { get; init; }

    /// <summary>
    /// Bounded channel carrying messages destined for the remote peer (outbound).
    /// Application producers write to it; the transport layer reads from it.
    /// </summary>
    public required Channel<StreamMessage> OutboundChannel { get; init; }

    /// <summary>
    /// <see cref="CancellationTokenSource"/> governing the entire lifetime of this stream.
    /// Cancelled on both graceful close and abort.
    /// </summary>
    public CancellationTokenSource LifetimeCts { get; } = new();

    /// <summary>
    /// Live throughput and backpressure metrics, updated atomically by the engine.
    /// </summary>
    public StreamThroughputMetrics Metrics { get; } = new();

    /// <summary>gRPC status code set during graceful termination or abort.</summary>
    public GrpcStatusCode? FinalStatus { get; set; }

    /// <summary>Human-readable description accompanying the final status.</summary>
    public string? CloseReason { get; set; }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await LifetimeCts.CancelAsync().ConfigureAwait(false);
        LifetimeCts.Dispose();

        InboundChannel.Writer.TryComplete();
        OutboundChannel.Writer.TryComplete();
    }
    public override string ToString()
    {
        return $"BidirectionalStreamContext {{ State = {State}, FinalStatus = {FinalStatus}, CloseReason = {CloseReason} }}";
    }
}

/// <summary>
/// Thread-safe sliding credit window that tracks how many messages the producer
/// is permitted to send before it must block and await consumer acknowledgement.
/// <para>
/// All mutations use lock-free compare-and-swap operations so that the window
/// can be read and updated safely from multiple threads without a lock.
/// </para>
/// </summary>
public sealed class FlowControlWindow
{
    private int _availableCredits;
    private long _totalProduced;
    private long _totalConsumed;

    /// <summary>Maximum credits this window can hold at any point in time.</summary>
    public int MaxSize { get; }

    /// <summary>Credits currently available for consumption by the producer.</summary>
    public int AvailableCredits => Volatile.Read(ref _availableCredits);

    /// <summary>
    /// Utilisation ratio in [0, 1]. As this approaches 1.0 the window is nearly exhausted.
    /// The engine emits <see cref="BackpressureChangedEvent"/> when utilisation crosses
    /// the configured threshold.
    /// </summary>
    public double Utilization =>
        MaxSize == 0 ? 0.0 : 1.0 - ((double)AvailableCredits / MaxSize);

    /// <summary>Total messages produced through this window since creation.</summary>
    public long TotalProduced => Interlocked.Read(ref _totalProduced);

    /// <summary>Total messages consumed (acknowledged) since creation.</summary>
    public long TotalConsumed => Interlocked.Read(ref _totalConsumed);

    /// <param name="initialSize">Starting credit balance.</param>
    /// <param name="maxSize">Upper bound on the credit balance.</param>
    public FlowControlWindow(int initialSize, int maxSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(initialSize);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSize, initialSize);

        MaxSize = maxSize;
        _availableCredits = initialSize;
    }

    /// <summary>
    /// Attempts a non-blocking deduction of <paramref name="count"/> credits via
    /// compare-and-swap. Returns <c>true</c> when successful; <c>false</c> when
    /// the window has insufficient credits.
    /// </summary>
    public bool TryConsume(int count = 1)
    {
        int current, updated;
        do
        {
            current = Volatile.Read(ref _availableCredits);
            if (current < count) return false;
            updated = current - count;
        }
        while (Interlocked.CompareExchange(ref _availableCredits, updated, current) != current);

        Interlocked.Add(ref _totalProduced, count);
        return true;
    }

    /// <summary>
    /// Returns <paramref name="count"/> credits, capped at <see cref="MaxSize"/>.
    /// Returns the new credit balance after the operation.
    /// </summary>
    public int Release(int count = 1)
    {
        int current, updated;
        do
        {
            current = Volatile.Read(ref _availableCredits);
            updated = Math.Min(MaxSize, current + count);
        }
        while (Interlocked.CompareExchange(ref _availableCredits, updated, current) != current);

        Interlocked.Add(ref _totalConsumed, count);
        return updated;
    }

    /// <summary>
    /// Forcibly sets the credit balance to <paramref name="size"/>,
    /// clamped to [0, <see cref="MaxSize"/>].
    /// </summary>
    public void Reset(int size) =>
        Interlocked.Exchange(ref _availableCredits, Math.Clamp(size, 0, MaxSize));
}

/// <summary>
/// Atomically-updated throughput and latency counters for a single stream.
/// All properties are safe to read from any thread without additional synchronisation.
/// </summary>
public sealed class StreamThroughputMetrics
{
    private long _messagesIn;
    private long _messagesOut;
    private long _bytesIn;
    private long _bytesOut;
    private long _backpressureEvents;
    private long _totalCreditWaitMs;

    /// <summary>Total messages received from the remote peer since stream creation.</summary>
    public long MessagesIn => Interlocked.Read(ref _messagesIn);

    /// <summary>Total messages dispatched to the remote peer since stream creation.</summary>
    public long MessagesOut => Interlocked.Read(ref _messagesOut);

    /// <summary>Cumulative payload bytes received.</summary>
    public long BytesIn => Interlocked.Read(ref _bytesIn);

    /// <summary>Cumulative payload bytes sent.</summary>
    public long BytesOut => Interlocked.Read(ref _bytesOut);

    /// <summary>Number of times backpressure throttling was applied to the producer.</summary>
    public long BackpressureEvents => Interlocked.Read(ref _backpressureEvents);

    /// <summary>
    /// Total wall-clock milliseconds the producer spent blocked waiting for
    /// flow-control credits.
    /// </summary>
    public long TotalCreditWaitMs => Interlocked.Read(ref _totalCreditWaitMs);

    internal void RecordInbound(int bytes)
    {
        Interlocked.Increment(ref _messagesIn);
        Interlocked.Add(ref _bytesIn, bytes);
    }

    internal void RecordOutbound(int bytes)
    {
        Interlocked.Increment(ref _messagesOut);
        Interlocked.Add(ref _bytesOut, bytes);
    }

    internal void RecordBackpressure() =>
        Interlocked.Increment(ref _backpressureEvents);

    internal void RecordCreditWait(long milliseconds) =>
        Interlocked.Add(ref _totalCreditWaitMs, milliseconds);
}

/// <summary>
/// Published on the application <see cref="EventBus"/> whenever the backpressure
/// state of a stream changes — either entering or leaving throttled mode.
/// </summary>
public sealed class BackpressureChangedEvent : EventBase
{
    /// <summary>Identifier of the stream whose backpressure state changed.</summary>
    public required string StreamId { get; init; }

    /// <summary><c>true</c> when throttling started; <c>false</c> when it was lifted.</summary>
    public required bool IsThrottled { get; init; }

    /// <summary>Window utilisation at the moment of the state change (0–1).</summary>
    public required double WindowUtilization { get; init; }

    /// <summary>Credits available to the producer when the event was raised.</summary>
    public required int AvailableCredits { get; init; }
}
