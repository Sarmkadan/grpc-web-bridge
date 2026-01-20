#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Contract for a flow-controlled bidirectional gRPC stream that enforces
/// backpressure between the producer and consumer sides.
/// </summary>
public interface IFlowControlledStream : IAsyncDisposable
{
    /// <summary>Gets the unique identifier of this stream.</summary>
    string StreamId { get; }

    /// <summary>Gets the gRPC method type associated with this stream.</summary>
    MethodType MethodType { get; }

    /// <summary>Gets the current lifecycle state of the stream.</summary>
    StreamState State { get; }

    /// <summary>Gets a live snapshot of throughput and backpressure metrics.</summary>
    StreamThroughputMetrics Metrics { get; }

    /// <summary>Gets the backpressure controller governing outbound flow for this stream.</summary>
    IBackpressureController BackpressureController { get; }

    /// <summary>
    /// Writes a message to the outbound channel. Suspends asynchronously when the
    /// channel is at capacity, providing transparent backpressure to the caller.
    /// </summary>
    /// <param name="message">The message to enqueue.</param>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    ValueTask WriteAsync(StreamMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all inbound messages as an <see cref="IAsyncEnumerable{T}"/>.
    /// Each consumed message implicitly releases flow-control credits to the remote producer,
    /// ensuring the credit window stays open in proportion to consumer throughput.
    /// </summary>
    /// <param name="cancellationToken">Token to stop iteration.</param>
    IAsyncEnumerable<StreamMessage> ReadAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals that the local side has finished writing. The stream transitions to
    /// <see cref="StreamState.HalfClosed"/> until the remote side also completes.
    /// </summary>
    ValueTask CompleteWritingAsync();

    /// <summary>
    /// Aborts the stream immediately with the specified gRPC status, discarding any
    /// buffered messages and notifying all waiting readers and writers.
    /// </summary>
    /// <param name="status">The gRPC status code to attach to the abort.</param>
    /// <param name="detail">Optional human-readable detail message.</param>
    ValueTask AbortAsync(GrpcStatusCode status, string? detail = null);
}

/// <summary>
/// Manages the credit window for a single stream, issuing and consuming per-message
/// credits to apply backpressure when the consumer cannot keep up with the producer.
/// </summary>
public interface IBackpressureController
{
    /// <summary>Gets the identifier of the stream this controller is bound to.</summary>
    string StreamId { get; }

    /// <summary>Gets the number of credits currently available to the producer.</summary>
    int AvailableCredits { get; }

    /// <summary>Gets the current window utilisation as a fraction between 0 and 1.</summary>
    double WindowUtilization { get; }

    /// <summary>Gets whether backpressure is currently active on this stream.</summary>
    bool IsThrottled { get; }

    /// <summary>
    /// Non-blocking credit acquisition. Returns <c>true</c> and deducts
    /// <paramref name="count"/> credits when the window has sufficient capacity;
    /// returns <c>false</c> when the window is exhausted.
    /// </summary>
    bool TryConsumeCredit(int count = 1);

    /// <summary>
    /// Asynchronously waits until <paramref name="count"/> credits are available,
    /// then consumes them. Respects the <paramref name="cancellationToken"/> throughout.
    /// </summary>
    ValueTask ConsumeCreditAsync(int count = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <paramref name="count"/> credits to the window, unblocking any
    /// producer that is suspended waiting for capacity.
    /// </summary>
    void ReleaseCredit(int count = 1);

    /// <summary>Resets the credit window to its initial configured size.</summary>
    void ResetWindow();
}

/// <summary>
/// Manages the full lifecycle of bidirectional gRPC streams, including creation,
/// flow-control enforcement, backpressure signalling, and graceful teardown.
/// </summary>
public interface IBidirectionalStreamingEngine
{
    /// <summary>Gets the total number of currently open bidirectional streams.</summary>
    int ActiveStreamCount { get; }

    /// <summary>
    /// Opens a new flow-controlled bidirectional stream and registers it with the engine.
    /// </summary>
    /// <param name="streamId">Unique identifier for the stream.</param>
    /// <param name="methodType">gRPC method type governing stream directionality.</param>
    /// <param name="cancellationToken">Token to cancel the open operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when the global stream limit is reached.</exception>
    Task<IFlowControlledStream> OpenStreamAsync(
        string streamId,
        MethodType methodType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the registered stream for <paramref name="streamId"/>,
    /// or <c>null</c> when no such stream is active.
    /// </summary>
    IFlowControlledStream? GetStream(string streamId);

    /// <summary>
    /// Performs a graceful close: drains the outbound buffer, attaches a terminal
    /// status, and disposes all resources associated with the stream.
    /// </summary>
    /// <param name="streamId">Identifier of the stream to close.</param>
    /// <param name="finalStatus">gRPC status code to attach. Defaults to <see cref="GrpcStatusCode.Ok"/>.</param>
    /// <param name="cancellationToken">Token to abandon the drain wait.</param>
    Task CloseStreamAsync(
        string streamId,
        GrpcStatusCode? finalStatus = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a snapshot of throughput metrics for every active stream,
    /// indexed by stream identifier.
    /// </summary>
    IReadOnlyDictionary<string, StreamThroughputMetrics> GetAllMetrics();
}
