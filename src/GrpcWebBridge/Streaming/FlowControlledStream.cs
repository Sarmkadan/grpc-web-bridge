#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Runtime.CompilerServices;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Concrete implementation of <see cref="IFlowControlledStream"/> that wraps a
/// <see cref="BidirectionalStreamContext"/> channel pair with a
/// <see cref="BackpressureController"/> credit window.
/// <para>
/// Reads from the inbound channel are credit-aware: every
/// <see cref="FlowControlOptions.CreditReplenishmentBatch"/> messages yielded to the
/// caller returns a credit batch to the producer, proportionally keeping the window
/// open relative to actual consumer throughput.  Writes consume one credit per message
/// and suspend asynchronously when the window is exhausted.
/// </para>
/// <para>
/// Instances are created and owned exclusively by <see cref="BidirectionalStreamingEngine"/>.
/// </para>
/// </summary>
internal sealed class FlowControlledStream : IFlowControlledStream
{
    private readonly BidirectionalStreamContext _context;
    private readonly FlowControlOptions _options;
    private readonly ILogger _logger;
    private int _disposed;

    /// <inheritdoc/>
    public string StreamId => _context.StreamId;

    /// <inheritdoc/>
    public MethodType MethodType => _context.MethodType;

    /// <inheritdoc/>
    public StreamState State => _context.State;

    /// <inheritdoc/>
    public StreamThroughputMetrics Metrics => _context.Metrics;

    /// <inheritdoc/>
    public IBackpressureController BackpressureController { get; }

    /// <summary>UTC timestamp at which this stream was opened.</summary>
    public DateTime CreatedAt => _context.CreatedAt;

    /// <summary>
    /// Initialises a new flow-controlled stream.
    /// </summary>
    /// <param name="context">Channel pair and lifetime state for this stream.</param>
    /// <param name="controller">Backpressure credit window bound to this stream.</param>
    /// <param name="options">Flow-control configuration shared with <paramref name="controller"/>.</param>
    /// <param name="logger">Logger for diagnostic messages.</param>
    internal FlowControlledStream(
        BidirectionalStreamContext context,
        BackpressureController controller,
        FlowControlOptions options,
        ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        BackpressureController = controller ?? throw new ArgumentNullException(nameof(controller));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask WriteAsync(StreamMessage message, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        EnsureWritable();

        long waitStart = Environment.TickCount64;
        await BackpressureController.ConsumeCreditAsync(1, cancellationToken).ConfigureAwait(false);
        long waitedMs = Environment.TickCount64 - waitStart;

        if (waitedMs > 0)
        {
            _context.Metrics.RecordCreditWait(waitedMs);
            _context.Metrics.RecordBackpressure();
        }

        await _context.OutboundChannel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
        _context.Metrics.RecordOutbound(message.Data?.Length ?? 0);

        _logger.LogTrace(
            "Stream {StreamId}: wrote outbound message seq={Seq} ({Bytes}B).",
            StreamId, message.SequenceNumber, message.Data?.Length ?? 0);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<StreamMessage> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int pendingRelease = 0;

        await foreach (StreamMessage msg in _context.InboundChannel.Reader
            .ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            _context.Metrics.RecordInbound(msg.Data?.Length ?? 0);

            yield return msg;

            if (++pendingRelease >= _options.CreditReplenishmentBatch)
            {
                BackpressureController.ReleaseCredit(pendingRelease);
                pendingRelease = 0;
            }
        }

        // Flush any remaining credits from the final partial batch.
        if (pendingRelease > 0)
            BackpressureController.ReleaseCredit(pendingRelease);
    }

    /// <inheritdoc/>
    public ValueTask CompleteWritingAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        _context.OutboundChannel.Writer.TryComplete();

        if (_context.State == StreamState.Active)
            _context.State = StreamState.HalfClosed;

        _logger.LogInformation(
            "Stream {StreamId}: local write side completed — state={State}.",
            StreamId, _context.State);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async ValueTask AbortAsync(GrpcStatusCode status, string? detail = null)
    {
        _context.FinalStatus = status;
        _context.CloseReason = detail;

        var reason = new OperationCanceledException(
            $"Stream {StreamId} aborted with status {status}: {detail}");

        _context.OutboundChannel.Writer.TryComplete(reason);
        _context.InboundChannel.Writer.TryComplete(reason);
        _context.State = StreamState.Failed;

        await _context.LifetimeCts.CancelAsync().ConfigureAwait(false);

        _logger.LogWarning(
            "Stream {StreamId}: aborted — status={Status}, detail={Detail}.",
            StreamId, status, detail ?? "(none)");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (BackpressureController is IDisposable disposable)
            disposable.Dispose();

        await _context.DisposeAsync().ConfigureAwait(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────

    private void EnsureWritable()
    {
        if (_context.State is StreamState.Failed or StreamState.Closed or StreamState.HalfClosed)
        {
            throw new InvalidOperationException(
                $"Stream '{StreamId}' cannot accept writes in state {_context.State}.");
        }
    }
}
