#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Determines the algorithm used to regulate message flow between producer and consumer.
/// </summary>
public enum FlowControlMode
{
    /// <summary>
    /// Credit-window mode. The producer may send at most <see cref="FlowControlOptions.InitialWindowSize"/>
    /// messages before blocking; credits are returned as the consumer reads.
    /// </summary>
    CreditWindow = 0,

    /// <summary>
    /// Adaptive mode. The engine widens the window under low utilisation and tightens it
    /// under high utilisation, trading latency for throughput automatically.
    /// </summary>
    Adaptive = 1,

    /// <summary>
    /// Flow control disabled. Suitable only for trusted, low-latency, bounded producers
    /// where memory growth is not a concern.
    /// </summary>
    Disabled = 2
}

/// <summary>
/// Immutable configuration for the bidirectional streaming engine's flow-control
/// and backpressure subsystem.
/// <para>
/// Mutate via the <c>with</c> expression or use one of the built-in presets
/// (<see cref="HighThroughput"/>, <see cref="LowLatency"/>).
/// </para>
/// </summary>
public sealed record FlowControlOptions
{
    /// <summary>
    /// Credits granted to each stream on opening.
    /// Each outbound message consumes one credit; credits are replenished as the
    /// consumer reads messages from the inbound channel.
    /// Defaults to <c>64</c>.
    /// </summary>
    public int InitialWindowSize { get; init; } = 64;

    /// <summary>
    /// Upper bound on the credit window.
    /// In <see cref="FlowControlMode.Adaptive"/> mode the engine may grow the window
    /// up to this value under favourable conditions.
    /// Defaults to <c>256</c>.
    /// </summary>
    public int MaxWindowSize { get; init; } = 256;

    /// <summary>
    /// Bounded capacity of the inbound <see cref="System.Threading.Channels.Channel{T}"/>.
    /// When the channel is full, the remote producer is suspended until the consumer
    /// reads at least one message.
    /// Defaults to <c>128</c>.
    /// </summary>
    public int InboundChannelCapacity { get; init; } = 128;

    /// <summary>
    /// Bounded capacity of the outbound <see cref="System.Threading.Channels.Channel{T}"/>.
    /// Defaults to <c>128</c>.
    /// </summary>
    public int OutboundChannelCapacity { get; init; } = 128;

    /// <summary>
    /// Window utilisation (0–1) above which the engine begins signalling backpressure
    /// via <see cref="BackpressureChangedEvent"/>.
    /// Defaults to <c>0.85</c> (85 %).
    /// </summary>
    public double BackpressureThreshold { get; init; } = 0.85;

    /// <summary>
    /// Credits returned to the producer each time the consumer processes a batch.
    /// Larger batches reduce acknowledgement traffic; smaller batches give finer control.
    /// Defaults to <c>16</c>.
    /// </summary>
    public int CreditReplenishmentBatch { get; init; } = 16;

    /// <summary>
    /// Active flow-control strategy.
    /// Defaults to <see cref="FlowControlMode.CreditWindow"/>.
    /// </summary>
    public FlowControlMode Mode { get; init; } = FlowControlMode.CreditWindow;

    /// <summary>
    /// Maximum time a producer may block waiting for a credit before the awaited
    /// operation is cancelled with <see cref="OperationCanceledException"/>.
    /// <c>null</c> means wait indefinitely, honouring only the caller's token.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan? MaxProducerWaitTime { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Interval at which the adaptive controller re-evaluates and adjusts the window size.
    /// Ignored when <see cref="Mode"/> is not <see cref="FlowControlMode.Adaptive"/>.
    /// Defaults to 5 seconds.
    /// </summary>
    public TimeSpan AdaptiveAdjustmentInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When <c>true</c>, the engine publishes <see cref="BackpressureChangedEvent"/> on
    /// the application event bus whenever throttling starts or is lifted.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool EmitBackpressureEvents { get; init; } = true;

    // ─────────────────────────────────────────────────────────────────────
    // Built-in presets
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a <see cref="FlowControlOptions"/> instance tuned for high-throughput workloads.
    /// Window and channel sizes are enlarged; the adaptive controller is enabled.
    /// </summary>
    public static FlowControlOptions HighThroughput => new()
    {
        InitialWindowSize = 256,
        MaxWindowSize = 1024,
        InboundChannelCapacity = 512,
        OutboundChannelCapacity = 512,
        CreditReplenishmentBatch = 64,
        BackpressureThreshold = 0.90,
        Mode = FlowControlMode.Adaptive
    };

    /// <summary>
    /// Returns a <see cref="FlowControlOptions"/> instance tuned for low-latency workloads.
    /// Window and channel sizes are reduced; credits are replenished in fine-grained batches.
    /// </summary>
    public static FlowControlOptions LowLatency => new()
    {
        InitialWindowSize = 16,
        MaxWindowSize = 64,
        InboundChannelCapacity = 32,
        OutboundChannelCapacity = 32,
        CreditReplenishmentBatch = 4,
        BackpressureThreshold = 0.75,
        Mode = FlowControlMode.CreditWindow
    };

    // ─────────────────────────────────────────────────────────────────────
    // Validation
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates all configuration values, throwing <see cref="ArgumentOutOfRangeException"/>
    /// or <see cref="ArgumentException"/> on any violation.
    /// </summary>
    public void Validate()
    {
        if (InitialWindowSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(InitialWindowSize), "Must be positive.");

        if (MaxWindowSize < InitialWindowSize)
            throw new ArgumentOutOfRangeException(nameof(MaxWindowSize),
                $"Must be >= {nameof(InitialWindowSize)} ({InitialWindowSize}).");

        if (InboundChannelCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(InboundChannelCapacity), "Must be positive.");

        if (OutboundChannelCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(OutboundChannelCapacity), "Must be positive.");

        if (BackpressureThreshold is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(BackpressureThreshold),
                "Must be a value between 0.0 and 1.0 inclusive.");

        if (CreditReplenishmentBatch <= 0)
            throw new ArgumentOutOfRangeException(nameof(CreditReplenishmentBatch), "Must be positive.");

        if (MaxProducerWaitTime.HasValue && MaxProducerWaitTime.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(MaxProducerWaitTime),
                "Must be a positive duration, or null for indefinite wait.");

        if (AdaptiveAdjustmentInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(AdaptiveAdjustmentInterval), "Must be positive.");
    }
}
