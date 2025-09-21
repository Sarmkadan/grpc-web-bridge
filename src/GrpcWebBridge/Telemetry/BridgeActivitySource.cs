#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace GrpcWebBridge.Telemetry;

/// <summary>
/// Central <see cref="ActivitySource"/> for the gRPC-Web bridge.
/// All distributed traces emitted by the bridge flow through this source, enabling
/// downstream consumers (Jaeger, Zipkin, OTLP exporters, etc.) to filter by the
/// well-known source name <c>GrpcWebBridge</c>.
/// </summary>
public static class BridgeActivitySource
{
    /// <summary>Source name used for all bridge activities.</summary>
    public const string Name = "GrpcWebBridge";

    /// <summary>Current semantic version of the bridge, embedded in every span.</summary>
    public const string Version = "2.0.2";

    /// <summary>
    /// Shared <see cref="ActivitySource"/> instance.
    /// Consumers that call <see cref="ActivitySource.StartActivity(string, ActivityKind)"/> on this
    /// source will only receive a non-null <see cref="Activity"/> when a listener is registered —
    /// that is, when OpenTelemetry tracing is configured via <c>AddGrpcWebBridgeTracing</c>.
    /// </summary>
    public static readonly ActivitySource Source = new(Name, Version);

    // ─── Well-known activity names ───────────────────────────────────────────

    /// <summary>Activity name for a gRPC unary call proxied through the bridge.</summary>
    public const string GrpcCall = "grpc.bridge.call";

    /// <summary>Activity name for a server-streaming call.</summary>
    public const string GrpcStream = "grpc.bridge.stream";

    /// <summary>Activity name for protocol translation (gRPC-Web → gRPC).</summary>
    public const string ProtocolTranslation = "grpc.bridge.translate";

    /// <summary>Activity name for authentication token validation.</summary>
    public const string Authentication = "grpc.bridge.auth";

    // ─── Tag key constants ────────────────────────────────────────────────────

    /// <summary>Semantic convention tag: gRPC service name.</summary>
    public const string TagRpcService = "rpc.service";

    /// <summary>Semantic convention tag: gRPC method name.</summary>
    public const string TagRpcMethod = "rpc.method";

    /// <summary>Semantic convention tag: gRPC system identifier.</summary>
    public const string TagRpcSystem = "rpc.system";

    /// <summary>Tag carrying the gRPC status code string (e.g. "OK", "UNAVAILABLE").</summary>
    public const string TagGrpcStatus = "rpc.grpc.status_code";

    /// <summary>Tag carrying the bridge instance name for multi-instance deployments.</summary>
    public const string TagBridgeInstance = "bridge.instance";

    /// <summary>Tag indicating whether the operation used streaming.</summary>
    public const string TagStreaming = "bridge.streaming";
}
