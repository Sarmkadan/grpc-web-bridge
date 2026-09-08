#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Data;

/// <summary>
/// Extension methods for <see cref="ConnectionMetrics"/>.
/// </summary>
public static class ConnectionMetricsExtensions
{
    /// <summary>
    /// Gets the average number of bytes sent and received per request.
    /// </summary>
    /// <param name="metrics">The connection metrics.</param>
    /// <returns>The average bytes per request, or <c>0</c> when no requests have been recorded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="metrics"/> is <see langword="null"/>.</exception>
    public static double GetAverageBytesPerRequest(this ConnectionMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        return metrics.RequestCount == 0
            ? 0
            : (double)(metrics.BytesSent + metrics.BytesReceived) / metrics.RequestCount;
    }
}
