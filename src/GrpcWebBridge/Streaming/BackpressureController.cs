#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Events;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Credit-based backpressure controller bound to a single stream.
/// <para>
/// The producer calls <see cref="ConsumeCreditAsync"/> before writing each outbound message;
/// the consumer calls <see cref="ReleaseCredit"/> after processing an inbound batch,
/// creating a closed feedback loop that caps in-flight message count and prevents
/// unbounded heap growth under sustained load.
/// </para>
/// <para>
/// Internally, a <see cref="SemaphoreSlim"/> is used for async-friendly credit waiting,
/// while a lock-free <see cref="FlowControlWindow"/> tracks utilisation for event emission.
/// All public methods are safe to call concurrently from multiple threads.
/// </para>
/// </summary>
public sealed class BackpressureController : IBackpressureController, IDisposable
{
    private readonly FlowControlWindow _window;
    private readonly FlowControlOptions _options;
    private readonly SemaphoreSlim _creditSemaphore;
    private readonly ILogger<BackpressureController> _logger;
    private readonly EventBus? _eventBus;

    private int _throttledFlag;   // 0 = not throttled, 1 = throttled (Interlocked)
    private int _disposed;

    /// <inheritdoc/>
    public string StreamId { get; }

    /// <inheritdoc/>
    public int AvailableCredits => _creditSemaphore.CurrentCount;

    /// <inheritdoc/>
    public double WindowUtilization => _window.Utilization;

    /// <inheritdoc/>
    public bool IsThrottled => Volatile.Read(ref _throttledFlag) == 1;

    /// <summary>Gets the total messages produced through the credit window since creation.</summary>
    public long TotalProduced => _window.TotalProduced;

    /// <summary>Gets the total messages consumed (acknowledged) since creation.</summary>
    public long TotalConsumed => _window.TotalConsumed;

    /// <param name="streamId">Identifier of the stream this controller governs.</param>
    /// <param name="options">Flow-control configuration.</param>
    /// <param name="logger">Logger for backpressure state transitions.</param>
    /// <param name="eventBus">
    /// Optional event bus. When provided and <see cref="FlowControlOptions.EmitBackpressureEvents"/>
    /// is <c>true</c>, <see cref="BackpressureChangedEvent"/> is published on every throttle transition.
    /// </param>
    public BackpressureController(
        string streamId,
        FlowControlOptions options,
        ILogger<BackpressureController> logger,
        EventBus? eventBus = null)
    {
        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID cannot be empty.", nameof(streamId));

        StreamId = streamId;
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventBus = eventBus;

        _window = new FlowControlWindow(options.InitialWindowSize, options.MaxWindowSize);
        _creditSemaphore = new SemaphoreSlim(options.InitialWindowSize, options.MaxWindowSize);
    }

    /// <inheritdoc/>
    public bool TryConsumeCredit(int count = 1)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_options.Mode == FlowControlMode.Disabled)
            return true;

        if (_creditSemaphore.CurrentCount < count)
        {
            ApplyThrottle();
            return false;
        }

        // Attempt to acquire 'count' slots non-blockingly.
        int acquired = 0;
        for (; acquired < count; acquired++)
        {
            if (!_creditSemaphore.Wait(0))
            {
                // Partial acquisition — return any already-acquired credits and signal backpressure.
                if (acquired > 0) _creditSemaphore.Release(acquired);
                ApplyThrottle();
                return false;
            }
        }

        _window.TryConsume(count);
        ConsiderLiftingThrottle();
        return true;
    }

    /// <inheritdoc/>
    public async ValueTask ConsumeCreditAsync(int count = 1, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (_options.Mode == FlowControlMode.Disabled)
            return;

        if (_creditSemaphore.CurrentCount < count)
        {
            ApplyThrottle();
            _logger.LogDebug(
                "Stream {StreamId}: producer waiting for {Count} credit(s). Available: {Available}.",
                StreamId, count, _creditSemaphore.CurrentCount);
        }

        CancellationTokenSource? timeoutCts = null;
        CancellationToken effectiveToken = cancellationToken;

        if (_options.MaxProducerWaitTime.HasValue)
        {
            timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_options.MaxProducerWaitTime.Value);
            effectiveToken = timeoutCts.Token;
        }

        long waitStart = Environment.TickCount64;

        try
        {
            for (int i = 0; i < count; i++)
                await _creditSemaphore.WaitAsync(effectiveToken);
        }
        finally
        {
            timeoutCts?.Dispose();
        }

        long waitedMs = Environment.TickCount64 - waitStart;
        if (waitedMs > 0)
            _logger.LogDebug("Stream {StreamId}: credit wait resolved in {Ms}ms.", StreamId, waitedMs);

        _window.TryConsume(count);
        ConsiderLiftingThrottle();
    }

    /// <inheritdoc/>
    public void ReleaseCredit(int count = 1)
    {
        if (_disposed == 1 || _options.Mode == FlowControlMode.Disabled)
            return;

        // Guard against releasing more than the semaphore's max count allows.
        int headroom = _options.MaxWindowSize - _creditSemaphore.CurrentCount;
        int toRelease = Math.Max(0, Math.Min(count, headroom));
        if (toRelease == 0)
            return;

        _window.Release(count);
        _creditSemaphore.Release(toRelease);

        ConsiderLiftingThrottle();

        _logger.LogDebug(
            "Stream {StreamId}: {Count} credit(s) released. Available: {Available}.",
            StreamId, toRelease, _creditSemaphore.CurrentCount);
    }

    /// <inheritdoc/>
    public void ResetWindow()
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        int deficit = _options.InitialWindowSize - _creditSemaphore.CurrentCount;
        if (deficit > 0)
            _creditSemaphore.Release(deficit);

        _window.Reset(_options.InitialWindowSize);

        // Clear throttle flag regardless of previous state.
        bool wasThrottled = Interlocked.Exchange(ref _throttledFlag, 0) == 1;
        if (wasThrottled)
        {
            _logger.LogInformation(
                "Stream {StreamId}: backpressure lifted after window reset.", StreamId);
        }

        _logger.LogInformation(
            "Stream {StreamId}: flow-control window reset to {Size} credits.",
            StreamId, _options.InitialWindowSize);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private void ApplyThrottle()
    {
        // Only emit once per throttle episode (CAS from 0 → 1).
        if (Interlocked.CompareExchange(ref _throttledFlag, 1, 0) != 0)
            return;

        _logger.LogWarning(
            "Stream {StreamId}: backpressure applied. Utilisation: {Utilization:P0}. Available: {Credits}.",
            StreamId, _window.Utilization, AvailableCredits);

        if (_options.EmitBackpressureEvents && _eventBus is not null)
        {
            _ = _eventBus.PublishAsync(new BackpressureChangedEvent
            {
                StreamId = StreamId,
                IsThrottled = true,
                WindowUtilization = _window.Utilization,
                AvailableCredits = AvailableCredits,
                Source = nameof(BackpressureController)
            });
        }
    }

    private void ConsiderLiftingThrottle()
    {
        if (Volatile.Read(ref _throttledFlag) == 0)
            return;

        if (_window.Utilization > _options.BackpressureThreshold)
            return;

        // Only emit once per lift (CAS from 1 → 0).
        if (Interlocked.CompareExchange(ref _throttledFlag, 0, 1) != 1)
            return;

        _logger.LogInformation(
            "Stream {StreamId}: backpressure lifted. Utilisation: {Utilization:P0}. Available: {Credits}.",
            StreamId, _window.Utilization, AvailableCredits);

        if (_options.EmitBackpressureEvents && _eventBus is not null)
        {
            _ = _eventBus.PublishAsync(new BackpressureChangedEvent
            {
                StreamId = StreamId,
                IsThrottled = false,
                WindowUtilization = _window.Utilization,
                AvailableCredits = AvailableCredits,
                Source = nameof(BackpressureController)
            });
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _creditSemaphore.Dispose();
    }
}
