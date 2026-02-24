#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Provides extension methods for <see cref="BidirectionalStreamingEngine"/>
/// that enhance stream management with additional monitoring, filtering, and
/// batching capabilities.
/// </summary>
public static class BidirectionalStreamingEngineExtensions
{
    /// <summary>
    /// Gets the stream throughput metrics for a specific stream by ID.
    /// </summary>
    /// <param name="engine">The streaming engine instance.</param>
    /// <param name="streamId">The unique identifier of the stream.</param>
    /// <returns>The metrics for the specified stream, or null if the stream doesn't exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="engine"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="streamId"/> is null or whitespace.</exception>
    public static StreamThroughputMetrics? GetStreamMetrics(
        this BidirectionalStreamingEngine engine,
        string streamId)
    {
        ArgumentNullException.ThrowIfNull(engine);

        if (string.IsNullOrWhiteSpace(streamId))
            throw new ArgumentException("Stream ID must be a non-empty string.", nameof(streamId));

        var stream = engine.GetStream(streamId);
        return stream?.Metrics;
    }

    /// <summary>
    /// Gets all active streams that match the specified method type.
    /// </summary>
    /// <param name="engine">The streaming engine instance.</param>
    /// <param name="methodType">The gRPC method type to filter by.</param>
    /// <returns>An enumerable of active streams matching the method type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="engine"/> is null.</exception>
    public static IEnumerable<IFlowControlledStream> GetStreamsByMethodType(
        this BidirectionalStreamingEngine engine,
        MethodType methodType)
    {
        ArgumentNullException.ThrowIfNull(engine);

        return engine.GetAllMetrics()
            .Select(static kvp => kvp.Key)
            .Select(streamId => engine.GetStream(streamId))
            .Where(static stream => stream is not null)
            .Select(static stream => stream!)
            .Where(stream => stream.MethodType == methodType)
            .ToList();
    }

    /// <summary>
    /// Gets the total number of messages (both inbound and outbound) across all active streams.
    /// </summary>
    /// <param name="engine">The streaming engine instance.</param>
    /// <returns>The total message count across all active streams.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="engine"/> is null.</exception>
    public static long GetTotalMessageCount(this BidirectionalStreamingEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        return engine.GetAllMetrics()
            .Sum(static kvp => kvp.Value.MessagesIn + kvp.Value.MessagesOut);
    }

    /// <summary>
    /// Gets the total bytes transferred (both inbound and outbound) across all active streams.
    /// </summary>
    /// <param name="engine">The streaming engine instance.</param>
    /// <returns>The total bytes transferred across all active streams.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="engine"/> is null.</exception>
    public static long GetTotalBytesTransferred(this BidirectionalStreamingEngine engine)
    {
        ArgumentNullException.ThrowIfNull(engine);

        return engine.GetAllMetrics()
            .Sum(static kvp => kvp.Value.BytesIn + kvp.Value.BytesOut);
    }
}
