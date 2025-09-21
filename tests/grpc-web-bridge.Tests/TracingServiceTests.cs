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

    public void Dispose() => _tracerProvider.Dispose();

    // ─────────────────────────────────────────────────────────────────────
    // BridgeActivitySource constants
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void BridgeActivitySource_Name_IsGrpcWebBridge()
    {
        BridgeActivitySource.Name.Should().Be("GrpcWebBridge");
    }

    [Fact]
    public void BridgeActivitySource_Source_HasExpectedName()
    {
        BridgeActivitySource.Source.Name.Should().Be("GrpcWebBridge");
    }

    // ─────────────────────────────────────────────────────────────────────
    // TracingService constructor
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullLogger_Throws()
    {
        Action act = () => _ = new TracingService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        var svc = new TracingService(NullLogger<TracingService>.Instance);
        svc.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────
    // StartGrpcCallActivity
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void StartGrpcCallActivity_WhenListenerActive_ReturnsNonNullActivity()
    {
        using var activity = _sut.StartGrpcCallActivity("UserService", "GetUser");

        activity.Should().NotBeNull();
    }

    [Fact]
    public void StartGrpcCallActivity_SetsRpcServiceTag()
    {
        using var activity = _sut.StartGrpcCallActivity("OrderService", "CreateOrder");

        activity.Should().NotBeNull();
        activity!.GetTagItem(BridgeActivitySource.TagRpcService).Should().Be("OrderService");
    }

    [Fact]
    public void StartGrpcCallActivity_SetsRpcMethodTag()
    {
        using var activity = _sut.StartGrpcCallActivity("OrderService", "CreateOrder");

        activity!.GetTagItem(BridgeActivitySource.TagRpcMethod).Should().Be("CreateOrder");
    }

    [Fact]
    public void StartGrpcCallActivity_SetsRpcSystemToGrpc()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");

        activity!.GetTagItem(BridgeActivitySource.TagRpcSystem).Should().Be("grpc");
    }

    [Fact]
    public void StartGrpcCallActivity_SetsInstanceTag()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");

        activity!.GetTagItem(BridgeActivitySource.TagBridgeInstance).Should().Be("test-instance");
    }

    [Fact]
    public void StartGrpcCallActivity_UnaryCall_HasClientKind()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method", isStreaming: false);

        activity!.Kind.Should().Be(ActivityKind.Client);
    }

    [Fact]
    public void StartGrpcCallActivity_StreamingCall_SetsStreamingTag()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Stream", isStreaming: true);

        activity!.GetTagItem(BridgeActivitySource.TagStreaming).Should().Be(true);
    }

    [Fact]
    public void StartGrpcCallActivity_UnaryCall_SetsStreamingTagFalse()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method", isStreaming: false);

        activity!.GetTagItem(BridgeActivitySource.TagStreaming).Should().Be(false);
    }

    // ─────────────────────────────────────────────────────────────────────
    // StartProtocolTranslationActivity
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void StartProtocolTranslationActivity_ReturnsNonNullActivity()
    {
        using var activity = _sut.StartProtocolTranslationActivity("grpc-web", "grpc");

        activity.Should().NotBeNull();
    }

    [Fact]
    public void StartProtocolTranslationActivity_SetsSourceProtocolTag()
    {
        using var activity = _sut.StartProtocolTranslationActivity("grpc-web", "grpc");

        activity!.GetTagItem("bridge.source_protocol").Should().Be("grpc-web");
    }

    [Fact]
    public void StartProtocolTranslationActivity_SetsTargetProtocolTag()
    {
        using var activity = _sut.StartProtocolTranslationActivity("grpc-web", "grpc");

        activity!.GetTagItem("bridge.target_protocol").Should().Be("grpc");
    }

    // ─────────────────────────────────────────────────────────────────────
    // StartAuthenticationActivity
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void StartAuthenticationActivity_ReturnsNonNullActivity()
    {
        using var activity = _sut.StartAuthenticationActivity("Bearer");

        activity.Should().NotBeNull();
    }

    [Fact]
    public void StartAuthenticationActivity_SetsSchemeTag()
    {
        using var activity = _sut.StartAuthenticationActivity("ApiKey");

        activity!.GetTagItem("auth.scheme").Should().Be("ApiKey");
    }

    // ─────────────────────────────────────────────────────────────────────
    // SetGrpcStatus
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetGrpcStatus_WithOkStatus_SetsOkStatusCode()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.SetGrpcStatus(activity, "OK");

        activity!.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void SetGrpcStatus_WithErrorStatus_SetsErrorStatusCode()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.SetGrpcStatus(activity, "UNAVAILABLE");

        activity!.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
    public void SetGrpcStatus_WithNonOkStatus_SetsGrpcStatusTag()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.SetGrpcStatus(activity, "NOT_FOUND");

        activity!.GetTagItem(BridgeActivitySource.TagGrpcStatus).Should().Be("NOT_FOUND");
    }

    [Fact]
    public void SetGrpcStatus_WhenActivityIsNull_DoesNotThrow()
    {
        Action act = () => TracingService.SetGrpcStatus(null, "OK");

        act.Should().NotThrow();
    }

    // ─────────────────────────────────────────────────────────────────────
    // RecordException
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void RecordException_WhenActivityIsNull_DoesNotThrow()
    {
        Action act = () => TracingService.RecordException(null, new InvalidOperationException("test"));

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordException_SetsErrorStatus()
    {
        using var activity = _sut.StartGrpcCallActivity("Svc", "Method");
        TracingService.RecordException(activity, new InvalidOperationException("boom"));

        activity!.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
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
    public void StartProtocolTranslationActivity_CompletedSpan_IsExported()
    {
        using (_sut.StartProtocolTranslationActivity("grpc-web", "grpc")) { }

        _tracerProvider.ForceFlush();
        _exported.Should().ContainSingle(a => a.DisplayName == BridgeActivitySource.ProtocolTranslation);
    }
}
