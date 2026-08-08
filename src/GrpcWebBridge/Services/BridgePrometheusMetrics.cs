#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Prometheus;

namespace GrpcWebBridge.Services;

/// <summary>
/// Opt-in Prometheus metrics for the gRPC-Web bridge.
/// Exposes:
/// <list type="bullet">
///   <item><term>grpcweb_bridge_requests_total</term><description>Counter – labelled by service, method, grpc_status.</description></item>
///   <item><term>grpcweb_bridge_request_duration_seconds</term><description>Histogram – labelled by service, method.</description></item>
///   <item><term>grpcweb_bridge_active_streams</term><description>Gauge – current number of active server-streaming / bidirectional streams.</description></item>
///   <item><term>grpcweb_bridge_stream_errors_total</term><description>Counter – labelled by service, method.</description></item>
/// </list>
/// </summary>
public sealed class BridgePrometheusMetrics
{
    private static readonly string[] RequestLabels = ["service", "method", "grpc_status"];
    private static readonly string[] DurationLabels = ["service", "method"];
    private static readonly string[] ErrorLabels = ["service", "method"];

    /// <summary>Total number of bridged RPC calls, labelled by service, method, and gRPC status.</summary>
    public static readonly Counter RequestsTotal = Metrics.CreateCounter(
        "grpcweb_bridge_requests_total",
        "Total number of gRPC-Web bridge requests.",
        new CounterConfiguration { LabelNames = RequestLabels });

    /// <summary>Histogram of end-to-end latency (seconds) per bridged RPC call.</summary>
    public static readonly Histogram RequestDuration = Metrics.CreateHistogram(
        "grpcweb_bridge_request_duration_seconds",
        "Duration of gRPC-Web bridge requests in seconds.",
        new HistogramConfiguration
        {
            LabelNames = DurationLabels,
            Buckets = [.005, .01, .025, .05, .1, .25, .5, 1, 2.5, 5, 10]
        });

    /// <summary>Current number of active streaming connections.</summary>
    public static readonly Gauge ActiveStreams = Metrics.CreateGauge(
        "grpcweb_bridge_active_streams",
        "Number of currently active server-streaming or bidirectional gRPC-Web streams.");

    /// <summary>Total number of streaming errors, labelled by service and method.</summary>
    public static readonly Counter StreamErrorsTotal = Metrics.CreateCounter(
        "grpcweb_bridge_stream_errors_total",
        "Total number of gRPC-Web bridge stream errors.",
        new CounterConfiguration { LabelNames = ErrorLabels });

    /// <summary>
    /// Records a completed RPC call.
    /// </summary>
    /// <param name="service">gRPC service name.</param>
    /// <param name="method">gRPC method name.</param>
    /// <param name="grpcStatus">gRPC status code string (e.g. "OK", "UNAVAILABLE").</param>
    /// <param name="durationSeconds">Call duration in seconds.</param>
    public static void RecordCall(string service, string method, string grpcStatus, double durationSeconds)
    {
        ArgumentException.ThrowIfNullOrEmpty(service);
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentException.ThrowIfNullOrEmpty(grpcStatus);
        RequestsTotal.WithLabels(service, method, grpcStatus).Inc();
        RequestDuration.WithLabels(service, method).Observe(durationSeconds);
    }

    /// <summary>
    /// Records a stream error.
    /// </summary>
    public static void RecordStreamError(string service, string method)
    {
        ArgumentException.ThrowIfNullOrEmpty(service);
        ArgumentException.ThrowIfNullOrEmpty(method);
        StreamErrorsTotal.WithLabels(service, method).Inc();
    }
}
