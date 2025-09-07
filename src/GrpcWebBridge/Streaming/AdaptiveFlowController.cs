#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Hosted background service that periodically adjusts credit windows for streams
/// operating in <see cref="FlowControlMode.Adaptive"/>.
/// <para>
/// On each tick the controller inspects every active stream's <see cref="IBackpressureController"/>
/// utilisation ratio and applies one of three strategies:
/// </para>
/// <list type="bullet">
///   <item>
///     <term>Widen</term>
///     <description>
///       When utilisation falls below <see cref="LowUtilizationThreshold"/> and no
///       backpressure is currently active, a <see cref="FlowControlOptions.CreditReplenishmentBatch"/>
///       of extra credits is released to allow short producer bursts without throttling.
///     </description>
///   </item>
///   <item>
///     <term>Hold</term>
///     <description>
///       When utilisation exceeds <see cref="HighUtilizationThreshold"/> the controller
///       withholds proactive credit release and lets the existing
///       <see cref="BackpressureController"/> handle throttling naturally.
///     </description>
///   </item>
///   <item>
///     <term>Neutral</term>
///     <description>
///       Utilisation between the two thresholds — no intervention.
///     </description>
///   </item>
/// </list>
/// <para>
/// When <see cref="FlowControlOptions.Mode"/> is not <see cref="FlowControlMode.Adaptive"/>
/// the service exits immediately without consuming resources.
/// </para>
/// </summary>
public sealed class AdaptiveFlowController : BackgroundService
{
    private readonly IBidirectionalStreamingEngine _engine;
    private readonly FlowControlOptions _options;
    private readonly ILogger<AdaptiveFlowController> _logger;

    /// <summary>
    /// Utilisation below which the controller proactively widens the credit window.
    /// </summary>
    private const double LowUtilizationThreshold = 0.30;

    /// <summary>
    /// Utilisation above which the controller holds back and defers to natural throttling.
    /// </summary>
    private const double HighUtilizationThreshold = 0.75;

    /// <summary>
    /// Initialises the adaptive controller.
    /// </summary>
    /// <param name="engine">
    /// The bidirectional streaming engine whose streams are observed and adjusted.
    /// </param>
    /// <param name="options">
    /// Flow-control configuration. <see cref="FlowControlOptions.AdaptiveAdjustmentInterval"/>
    /// governs the tick rate; <see cref="FlowControlOptions.CreditReplenishmentBatch"/>
    /// controls how many credits are released per widen operation.
    /// </param>
    /// <param name="logger">Logger for diagnostic trace and adjustment summaries.</param>
    public AdaptiveFlowController(
        IBidirectionalStreamingEngine engine,
        FlowControlOptions options,
        ILogger<AdaptiveFlowController> logger)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.Mode != FlowControlMode.Adaptive)
        {
            _logger.LogInformation(
                "AdaptiveFlowController: flow-control mode is {Mode} — controller exiting.",
                _options.Mode);
            return;
        }

        _logger.LogInformation(
            "AdaptiveFlowController started — interval={IntervalS}s, " +
            "low={Low:P0}, high={High:P0}, batch={Batch}.",
            _options.AdaptiveAdjustmentInterval.TotalSeconds,
            LowUtilizationThreshold,
            HighUtilizationThreshold,
            _options.CreditReplenishmentBatch);

        using var timer = new PeriodicTimer(_options.AdaptiveAdjustmentInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                AdjustAll();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AdaptiveFlowController: unhandled error during adjustment pass.");
            }
        }

        _logger.LogInformation("AdaptiveFlowController stopped.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private void AdjustAll()
    {
        var metrics = _engine.GetAllMetrics();

        if (metrics.Count == 0)
            return;

        int widened = 0, pressured = 0, neutral = 0;

        foreach (var (streamId, _) in metrics)
        {
            var stream = _engine.GetStream(streamId);
            if (stream is null)
                continue;

            var bp = stream.BackpressureController;
            double utilization = bp.WindowUtilization;

            switch (utilization)
            {
                case < LowUtilizationThreshold when !bp.IsThrottled:
                    // Producer is comfortably below capacity — pre-release credits for burst headroom.
                    bp.ReleaseCredit(_options.CreditReplenishmentBatch);
                    widened++;
                    _logger.LogTrace(
                        "Adaptive: widened window for stream {StreamId} — " +
                        "utilization={U:P0}, released={Batch}, available={Available}.",
                        streamId, utilization, _options.CreditReplenishmentBatch, bp.AvailableCredits);
                    break;

                case > HighUtilizationThreshold:
                    // Producer is under heavy pressure — let BackpressureController throttle naturally.
                    pressured++;
                    _logger.LogTrace(
                        "Adaptive: holding stream {StreamId} — utilization={U:P0}, throttled={T}.",
                        streamId, utilization, bp.IsThrottled);
                    break;

                default:
                    neutral++;
                    break;
            }
        }

        if (widened > 0 || pressured > 0)
        {
            _logger.LogDebug(
                "Adaptive pass: {Total} stream(s) checked — widened={W}, pressured={P}, neutral={N}.",
                metrics.Count, widened, pressured, neutral);
        }
    }
}
