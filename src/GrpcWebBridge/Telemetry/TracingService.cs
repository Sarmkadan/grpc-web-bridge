#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using OpenTelemetry.Trace;

namespace GrpcWebBridge.Telemetry;

/// <summary>
/// Service that creates and manages distributed-tracing spans for bridge operations.
/// Wraps <see cref="BridgeActivitySource.Source"/> so callers do not need to deal
/// with <see cref="ActivitySource"/> directly. All methods are safe to call even
/// when no tracing listener is active (they return a no-op scope in that case).
/// </summary>
public sealed class TracingService
{
    private readonly ILogger<TracingService> _logger;
    private readonly string _instanceName;

    /// <summary>
    /// Initialises the tracing service.
    /// </summary>
    /// <param name="logger">Logger used to record internal diagnostics.</param>
    /// <param name="instanceName">
    ///   Optional bridge instance name attached to every span as
    ///   <c>bridge.instance</c>. Defaults to <c>"default"</c>.
    /// </param>
    public TracingService(ILogger<TracingService> logger, string? instanceName = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _instanceName = string.IsNullOrWhiteSpace(instanceName) ? "default" : instanceName;
    }

    // ─── Public factory methods ───────────────────────────────────────────────

    /// <summary>
    /// Starts a span representing a proxied gRPC call.
    /// </summary>
    /// <param name="serviceName">The downstream gRPC service name (e.g. "UserService").</param>
    /// <param name="methodName">The gRPC method being called (e.g. "GetUser").</param>
    /// <param name="isStreaming">
    ///   <see langword="true"/> if this is a streaming call; <see langword="false"/> for unary.
    /// </param>
    /// <returns>
    ///   An <see cref="Activity"/> scope. The caller is responsible for disposing it.
    ///   May be <see langword="null"/> when no tracing listener is registered — callers should
    ///   handle this via a null-conditional dispose or the <c>using var _ =</c> pattern.
    /// </returns>
    public Activity? StartGrpcCallActivity(string serviceName, string methodName, bool isStreaming = false)
    {
        var activityName = isStreaming ? BridgeActivitySource.GrpcStream : BridgeActivitySource.GrpcCall;
        var activity = BridgeActivitySource.Source.StartActivity(activityName, ActivityKind.Client);

        if (activity is null)
            return null;

        activity
            .SetTag(BridgeActivitySource.TagRpcSystem, "grpc")
            .SetTag(BridgeActivitySource.TagRpcService, serviceName)
            .SetTag(BridgeActivitySource.TagRpcMethod, methodName)
            .SetTag(BridgeActivitySource.TagStreaming, isStreaming)
            .SetTag(BridgeActivitySource.TagBridgeInstance, _instanceName);

        _logger.LogDebug("Started tracing activity {ActivityName} for {Service}/{Method}",
            activityName, serviceName, methodName);

        return activity;
    }

    /// <summary>
    /// Starts a span representing a protocol-translation step.
    /// </summary>
    /// <param name="sourceProtocol">Protocol being translated from (e.g. "grpc-web").</param>
    /// <param name="targetProtocol">Protocol being translated to (e.g. "grpc").</param>
    public Activity? StartProtocolTranslationActivity(string sourceProtocol, string targetProtocol)
    {
        var activity = BridgeActivitySource.Source.StartActivity(
            BridgeActivitySource.ProtocolTranslation, ActivityKind.Internal);

        if (activity is null)
            return null;

        activity
            .SetTag("bridge.source_protocol", sourceProtocol)
            .SetTag("bridge.target_protocol", targetProtocol)
            .SetTag(BridgeActivitySource.TagBridgeInstance, _instanceName);

        return activity;
    }

    /// <summary>
    /// Starts a span representing an authentication check.
    /// </summary>
    /// <param name="scheme">Authentication scheme (e.g. "Bearer", "ApiKey").</param>
    public Activity? StartAuthenticationActivity(string scheme)
    {
        var activity = BridgeActivitySource.Source.StartActivity(
            BridgeActivitySource.Authentication, ActivityKind.Internal);

        if (activity is null)
            return null;

        activity
            .SetTag("auth.scheme", scheme)
            .SetTag(BridgeActivitySource.TagBridgeInstance, _instanceName);

        return activity;
    }

    /// <summary>
    /// Marks an activity as failed and records the exception details as span events.
    /// </summary>
    /// <param name="activity">The activity to mark as failed. Ignored when <see langword="null"/>.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="grpcStatus">
    ///   Optional gRPC status string (e.g. "UNAVAILABLE") added as a tag.
    /// </param>
    public static void RecordException(Activity? activity, Exception exception, string? grpcStatus = null)
    {
        if (activity is null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);

        if (!string.IsNullOrWhiteSpace(grpcStatus))
            activity.SetTag(BridgeActivitySource.TagGrpcStatus, grpcStatus);
    }

    /// <summary>
    /// Sets the gRPC status code on a completed activity.
    /// </summary>
    /// <param name="activity">The activity to annotate. Ignored when <see langword="null"/>.</param>
    /// <param name="grpcStatus">gRPC status string, e.g. "OK" or "NOT_FOUND".</param>
    public static void SetGrpcStatus(Activity? activity, string grpcStatus)
    {
        if (activity is null)
            return;

        activity.SetTag(BridgeActivitySource.TagGrpcStatus, grpcStatus);

        if (grpcStatus != "OK")
            activity.SetStatus(ActivityStatusCode.Error, $"gRPC status: {grpcStatus}");
        else
            activity.SetStatus(ActivityStatusCode.Ok);
    }
}
