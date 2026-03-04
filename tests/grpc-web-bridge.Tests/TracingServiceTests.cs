#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using FluentAssertions;
using GrpcWebBridge.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class TracingServiceTests : IDisposable
{
/// <summary>
/// Contains unit tests for the <see cref="TracingService"/> class.
/// Tests verify the tracing functionality for gRPC calls, protocol translation,
/// authentication, and error handling within the gRPC web bridge.
/// </summary>

    private readonly TracerProvider _tracerProvider;
    private readonly List<Activity> _exported = [];
    private readonly TracingService _sut;

    public TracingServiceTests()
    {
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(BridgeActivitySource.Name)
            .AddInMemoryExporter(_exported)
            .Build()!;

        _sut = new TracingService(NullLogger<TracingService>.Instance, "test-instance");
    }

    	/// <summary>
	/// Disposes the tracer provider and cleans up resources.
	/// </summary>
	public void Dispose() => _tracerProvider.Dispose();

    // ─────────────────────────────────────────────────────────────────────
    // BridgeActivitySource constants
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
	/// <summary>
	/// Tests that the <see cref="BridgeActivitySource.Name"/> constant has the expected value.
	/// </summary>
    public void BridgeActivitySource_Name_IsGrpcWebBridge()
    {
        BridgeActivitySource.Name.Should().Be("GrpcWebBridge");
    }

    [Fact]
	/// <summary>
	/// Tests that the <see cref="BridgeActivitySource.Source"/> has the expected name.
	/// </summary>
    public void BridgeActivitySource_Source_HasExpectedName()
    {
        BridgeActivitySource.Source.Name.Should().Be("GrpcWebBridge");
    }

    // ─────────────────────────────────────────────────────────────────────
    // TracingService constructor
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
	/// <summary>
	/// Tests that the constructor throws when passed a null logger.
	/// </summary>
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => _ = new TracingService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
	/// <summary>
	/// Tests that the constructor creates an instance when passed a valid logger.
	/// </summary>
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        var svc = new TracingService(NullLogger<TracingService>.Instance);
        svc.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────
    // StartGrpcCallActivity
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartGrpcCallActivity"/> returns a non-null activity when the listener is active.
	/// </summary>
    public void StartGrpcCallActivity_WhenListenerActive_ReturnsNonNullActivity()
    {
        using var activity = _sut.StartGrpcCallActivity("UserService", "GetUser");

        activity.Should().NotBeNull();
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartGrpcCallActivity"/> sets the RPC service tag correctly.
	/// </summary>
    public void StartGrpcCallActivity_SetsRpcServiceTag()
    {
        using var activity = _sut.StartGrpcCallActivity("OrderService", "CreateOrder");

        activity.Should().NotBeNull();
        activity!.GetTagItem(BridgeActivitySource.TagRpcService).Should().Be("OrderService");
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartGrpcCallActivity"/> sets the RPC method tag correctly.
	/// </summary>
    public void StartGrpcCallActivity_SetsRpcMethodTag()
    {
        using var activity = _sut.StartGrpcCallActivity("OrderService", "CreateOrder");

        activity!.GetTagItem(BridgeActivitySource.TagRpcMethod).Should().Be("CreateOrder");
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartGrpcCallActivity"/> sets the RPC system tag to "grpc".
	/// </summary>
    public void StartGrpcCallActivity_SetsRpcSystemToGrpc()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");

        activity!.GetTagItem(BridgeActivitySource.TagRpcSystem).Should().Be("grpc");
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartGrpcCallActivity"/> sets the instance tag correctly.
	/// </summary>
    public void StartGrpcCallActivity_SetsInstanceTag()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");

        activity!.GetTagItem(BridgeActivitySource.TagBridgeInstance).Should().Be("test-instance");
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartGrpcCallActivity"/> sets the activity kind to Client for unary calls.
	/// </summary>
    public void StartGrpcCallActivity_UnaryCall_HasClientKind()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method", isStreaming: false);

        activity!.Kind.Should().Be(ActivityKind.Client);
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartGrpcCallActivity"/> sets the streaming tag to true for streaming calls.
	/// </summary>
    public void StartGrpcCallActivity_StreamingCall_SetsStreamingTag()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Stream", isStreaming: true);

        activity!.GetTagItem(BridgeActivitySource.TagStreaming).Should().Be(true);
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartGrpcCallActivity"/> sets the streaming tag to false for unary calls.
	/// </summary>
    public void StartGrpcCallActivity_UnaryCall_SetsStreamingTagFalse()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method", isStreaming: false);

        activity!.GetTagItem(BridgeActivitySource.TagStreaming).Should().Be(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // StartProtocolTranslationActivity
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartProtocolTranslationActivity"/> returns a non-null activity.
	/// </summary>
    public void StartProtocolTranslationActivity_ReturnsNonNullActivity()
    {
        using var activity = _sut.StartProtocolTranslationActivity("grpc-web", "grpc");

        activity.Should().NotBeNull();
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartProtocolTranslationActivity"/> sets the source protocol tag correctly.
	/// </summary>
    public void StartProtocolTranslationActivity_SetsSourceProtocolTag()
    {
        using var activity = _sut.StartProtocolTranslationActivity("grpc-web", "grpc");

        activity!.GetTagItem("bridge.source_protocol").Should().Be("grpc-web");
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartProtocolTranslationActivity"/> sets the target protocol tag correctly.
	/// </summary>
    public void StartProtocolTranslationActivity_SetsTargetProtocolTag()
    {
        using var activity = _sut.StartProtocolTranslationActivity("grpc-web", "grpc");

        activity!.GetTagItem("bridge.target_protocol").Should().Be("grpc");
    }

    // ─────────────────────────────────────────────────────────────────────
    // StartAuthenticationActivity
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartAuthenticationActivity"/> returns a non-null activity.
	/// </summary>
    public void StartAuthenticationActivity_ReturnsNonNullActivity()
    {
        using var activity = _sut.StartAuthenticationActivity("Bearer");

        activity.Should().NotBeNull();
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.StartAuthenticationActivity"/> sets the scheme tag correctly.
	/// </summary>
    public void StartAuthenticationActivity_SetsSchemeTag()
    {
        using var activity = _sut.StartAuthenticationActivity("ApiKey");

        activity!.GetTagItem("auth.scheme").Should().Be("ApiKey");
    }

    // ─────────────────────────────────────────────────────────────────────
    // SetGrpcStatus
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.SetGrpcStatus"/> sets the activity status to Ok when the status is "OK".
	/// </summary>
    public void SetGrpcStatus_WithOkStatus_SetsOkStatusCode()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.SetGrpcStatus(activity, "OK");

        activity!.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.SetGrpcStatus"/> sets the activity status to Error when the status is an error code.
	/// </summary>
    public void SetGrpcStatus_WithErrorStatus_SetsErrorStatusCode()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.SetGrpcStatus(activity, "UNAVAILABLE");

        activity!.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.SetGrpcStatus"/> sets the gRPC status tag correctly.
	/// </summary>
    public void SetGrpcStatus_WithNonOkStatus_SetsGrpcStatusTag()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.SetGrpcStatus(activity, "NOT_FOUND");

        activity!.GetTagItem(BridgeActivitySource.TagGrpcStatus).Should().Be("NOT_FOUND");
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.SetGrpcStatus"/> does not throw when passed a null activity.
	/// </summary>
    public void SetGrpcStatus_WhenActivityIsNull_DoesNotThrow()
    {
        Action act = () => TracingService.SetGrpcStatus(null, "OK");

        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────────────────────────────
    // RecordException
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.RecordException"/> does not throw when passed a null activity.
	/// </summary>
    public void RecordException_WhenActivityIsNull_DoesNotThrow()
    {
        Action act = () => TracingService.RecordException(null, new InvalidOperationException("test"));

        act.Should().NotThrow();
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.RecordException"/> sets the activity status to Error.
	/// </summary>
    public void RecordException_SetsErrorStatus()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.RecordException(activity, new InvalidOperationException("boom"));

        activity!.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
	/// <summary>
	/// Tests that <see cref="TracingService.RecordException"/> sets the gRPC status tag when provided.
	/// </summary>
    public void RecordException_WithGrpcStatus_SetsStatusTag()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.RecordException(activity, new InvalidOperationException("boom"), "INTERNAL");

        activity!.GetTagItem(BridgeActivitySource.TagGrpcStatus).Should().Be("INTERNAL");
    }

    // ─────────────────────────────────────────────────────────────────────
    // End-to-end: activities are exported
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
	/// <summary>
	/// Tests that completed gRPC call activities are exported to the tracer provider.
	/// </summary>
    public void StartGrpcCallActivity_CompletedSpan_IsExported()
    {
        using (var activity = _sut.StartGrpcCallActivity("UserService", "GetUser"))
        {
            TracingService.SetGrpcStatus(activity, "OK");
        }

        _tracerProvider.ForceFlush();
        _exported.Should().ContainSingle(a => a.DisplayName == BridgeActivitySource.GrpcCall);
    }

    [Fact]
	/// <summary>
	/// Tests that completed protocol translation activities are exported to the tracer provider.
	/// </summary>
    public void StartProtocolTranslationActivity_CompletedSpan_IsExported()
    {
        using (_sut.StartProtocolTranslationActivity("grpc-web", "grpc")) { }

        _tracerProvider.ForceFlush();
        _exported.Should().ContainSingle(a => a.DisplayName == BridgeActivitySource.ProtocolTranslation);
    }
}
