// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Configuration options for <see cref="StreamDiagnosticsService"/>.
/// </summary>
public sealed record StreamDiagnosticsOptions
{
    /// <summary>
    /// Interval between diagnostic collection passes.
    /// Defaults to <c>60 seconds</c>.
    /// </summary>
    public TimeSpan CollectionInterval { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Streams with no inbound or outbound messages since the last collection are
    /// considered potentially stale after this idle duration.
    /// Defaults to <c>5 minutes</c>.
    /// </summary>
    public TimeSpan StaleStreamThreshold { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Streams whose backpressure-event-to-total-message ratio exceeds this value
    /// are flagged at <c>Warning</c> level.
    /// Defaults to <c>0.10</c> (10 %).
    /// </summary>
    public double BackpressureWarnThreshold { get; init; } = 0.10;
}

/// <summary>
/// Published to the application <see cref="EventBus"/> after each diagnostic collection pass,
/// providing a point-in-time aggregate snapshot of all active bidirectional streams.
/// Consumers such as metrics exporters or alerting hooks can subscribe to this event
/// without coupling to <see cref="StreamDiagnosticsService"/> directly.
/// </summary>
public sealed class StreamingDiagnosticsEvent : EventBase
{
    /// <summary>Total number of active streams at the time of collection.</summary>
    public required int ActiveStreamCount { get; init; }

    /// <summary>Sum of all inbound messages across active streams.</summary>
    public required long TotalMessagesIn { get; init; }

    /// <summary>Sum of all outbound messages across active streams.</summary>
    public required long TotalMessagesOut { get; init; }

    /// <summary>Cumulative inbound payload bytes across all active streams.</summary>
    public required long TotalBytesIn { get; init; }

    /// <summary>Cumulative outbound payload bytes across all active streams.</summary>
    public required long TotalBytesOut { get; init; }

    /// <summary>Total backpressure throttle events accumulated across all active streams.</summary>
    public required long TotalBackpressureEvents { get; init; }

    /// <summary>Aggregate credit wait time in milliseconds across all active streams.</summary>
    public required long TotalCreditWaitMs { get; init; }

    /// <summary>Number of streams identified as having zero message activity.</summary>
    public required int ZeroActivityStreamCount { get; init; }

    /// <summary>Number of streams flagged for high backpressure ratios.</summary>
    public required int HighBackpressureStreamCount { get; init; }
}

/// <summary>
/// Hosted background service that periodically collects aggregate throughput metrics
/// from the <see cref="IBidirectionalStreamingEngine"/>, emits a structured diagnostic
/// summary, and publishes a <see cref="StreamingDiagnosticsEvent"/> for downstream consumers.
/// <para>
/// Per-stream anomaly detection:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>High backpressure</term>
///     <description>
///       Streams whose backpressure event ratio exceeds
///       <see cref="StreamDiagnosticsOptions.BackpressureWarnThreshold"/> are logged at
///       <c>Warning</c> level, suggesting the window size or channel capacity should be increased.
///     </description>
///   </item>
///   <item>
///     <term>Zero activity</term>
///     <description>
///       Streams with neither inbound nor outbound messages since the last collection pass
///       are flagged at <c>Debug</c> level as potentially unused or leaked.
///     </description>
///   </item>
/// </list>
/// </summary>
public sealed class StreamDiagnosticsService : BackgroundService
{
    private readonly IBidirectionalStreamingEngine _engine;
    private readonly StreamDiagnosticsOptions _options;
    private readonly EventBus? _eventBus;
    private readonly ILogger<StreamDiagnosticsService> _logger;

    // Tracks per-stream message counts from the previous pass to detect zero-delta intervals.
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> _prevMessageCounts = new();

    /// <summary>
    /// Initialises the service.
    /// </summary>
    /// <param name="engine">The bidirectional streaming engine to observe.</param>
    /// <param name="options">Diagnostic collection configuration.</param>
    /// <param name="logger">Logger for structured diagnostic output.</param>
    /// <param name="eventBus">
    /// Optional application event bus. When provided, a <see cref="StreamingDiagnosticsEvent"/>
    /// is published after each collection pass.
    /// </param>
    public StreamDiagnosticsService(
        IBidirectionalStreamingEngine engine,
        StreamDiagnosticsOptions options,
        ILogger<StreamDiagnosticsService> logger,
        EventBus? eventBus = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "StreamDiagnosticsService started — interval={IntervalS}s, " +
            "bpWarnThreshold={BPThreshold:P0}.",
            _options.CollectionInterval.TotalSeconds,
            _options.BackpressureWarnThreshold);

        using var timer = new PeriodicTimer(_options.CollectionInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                CollectAndReport();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StreamDiagnosticsService: error during collection pass.");
            }
        }

        _logger.LogInformation("StreamDiagnosticsService stopped.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private void CollectAndReport()
    {
        var allMetrics = _engine.GetAllMetrics();
        int activeCount = allMetrics.Count;

        if (activeCount == 0)
        {
            _prevMessageCounts.Clear();
            _logger.LogDebug("StreamDiagnosticsService: no active streams.");
            return;
        }

        long totalIn = 0, totalOut = 0, totalBytesIn = 0, totalBytesOut = 0;
        long totalBP = 0, totalWaitMs = 0;
        int zeroActivity = 0, highBackpressure = 0;

        foreach (var (streamId, metrics) in allMetrics)
        {
            long messageTotal = metrics.MessagesIn + metrics.MessagesOut;

            totalIn += metrics.MessagesIn;
            totalOut += metrics.MessagesOut;
            totalBytesIn += metrics.BytesIn;
            totalBytesOut += metrics.BytesOut;
            totalBP += metrics.BackpressureEvents;
            totalWaitMs += metrics.TotalCreditWaitMs;

            // Detect high backpressure ratio.
            if (messageTotal > 0)
            {
                double bpRatio = (double)metrics.BackpressureEvents / messageTotal;

                if (bpRatio > _options.BackpressureWarnThreshold)
                {
                    highBackpressure++;
                    _logger.LogWarning(
                        "Stream {StreamId}: high backpressure ratio {Ratio:P1} " +
                        "({Events} events / {Total} messages, creditWait={WaitMs}ms). " +
                        "Consider increasing InitialWindowSize or channel capacity.",
                        streamId, bpRatio, metrics.BackpressureEvents, messageTotal,
                        metrics.TotalCreditWaitMs);
                }
            }

            // Detect zero-delta activity between collection passes.
            long prevCount = _prevMessageCounts.GetValueOrDefault(streamId, -1L);

            if (prevCount >= 0 && messageTotal == prevCount)
            {
                zeroActivity++;
                _logger.LogDebug(
                    "Stream {StreamId}: no message activity since last collection pass " +
                    "(totalMessages={Total}).",
                    streamId, messageTotal);
            }

            _prevMessageCounts[streamId] = messageTotal;
        }

        // Remove tracking entries for streams that closed between passes.
        foreach (var staleKey in _prevMessageCounts.Keys.Except(allMetrics.Keys).ToList())
            _prevMessageCounts.TryRemove(staleKey, out _);

        _logger.LogInformation(
            "Streaming diagnostics — activeStreams={Active}, zeroActivity={ZeroActivity}, " +
            "highBP={HighBP}, messagesIn={In}, messagesOut={Out}, " +
            "bytesIn={BytesIn:N0}, bytesOut={BytesOut:N0}, " +
            "backpressureEvents={BP}, creditWaitMs={WaitMs:N0}.",
            activeCount, zeroActivity, highBackpressure,
            totalIn, totalOut,
            totalBytesIn, totalBytesOut,
            totalBP, totalWaitMs);

        if (_eventBus is not null)
        {
            _ = _eventBus.PublishAsync(new StreamingDiagnosticsEvent
            {
                ActiveStreamCount = activeCount,
                TotalMessagesIn = totalIn,
                TotalMessagesOut = totalOut,
                TotalBytesIn = totalBytesIn,
                TotalBytesOut = totalBytesOut,
                TotalBackpressureEvents = totalBP,
                TotalCreditWaitMs = totalWaitMs,
                ZeroActivityStreamCount = zeroActivity,
                HighBackpressureStreamCount = highBackpressure,
                Source = nameof(StreamDiagnosticsService)
            });
        }
    }
}
