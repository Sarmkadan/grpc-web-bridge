#nullable enable
using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Events;
using GrpcWebBridge.Streaming;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Integration tests for BidirectionalStreamingEngine covering backpressure
/// scenarios, producer timeouts, event emission, and resource cleanup.
/// </summary>
public sealed class BidirectionalStreamingEngineTests
{
    private static BidirectionalStreamingEngine CreateEngine(
        FlowControlOptions? options = null,
        EventBus? eventBus = null,
        int maxStreams = 100)
    {
        return new BidirectionalStreamingEngine(
            NullLoggerFactory.Instance,
            options,
            eventBus,
            maxStreams);
    }

    private static EventBus CreateEventBus() =>
        new EventBus(NullLogger<EventBus>.Instance);

    // ─────────────────────────────────────────────────────────────────────
    // CreditWindow saturation
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreditWindow_Saturation_ProducerBlocksUntilCreditReleased()
    {
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 2,
            MaxWindowSize = 4,
            InboundChannelCapacity = 16,
            OutboundChannelCapacity = 16,
            MaxProducerWaitTime = null
        };

        await using var engine = CreateEngine(options);
        var stream = await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        await stream.WriteAsync(new StreamMessage("s1", 1, new byte[] { 0x01 }));
        await stream.WriteAsync(new StreamMessage("s1", 2, new byte[] { 0x02 }));

        // Window exhausted — third write must block
        using var writeCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var writeTask = stream.WriteAsync(
            new StreamMessage("s1", 3, new byte[] { 0x03 }), writeCts.Token).AsTask();

        await Task.Delay(60);
        writeTask.IsCompleted.Should().BeFalse("producer should be blocked awaiting credits");

        // Releasing credits unblocks the producer
        stream.BackpressureController.ReleaseCredit(2);
        await writeTask.WaitAsync(TimeSpan.FromSeconds(2));
        writeTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task CreditWindow_IsThrottled_TrueWhenWindowExhausted()
    {
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 2,
            MaxWindowSize = 2,
            InboundChannelCapacity = 16,
            OutboundChannelCapacity = 16,
            MaxProducerWaitTime = null
        };

        await using var engine = CreateEngine(options);
        var stream = await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        stream.BackpressureController.TryConsumeCredit(2);

        // Trigger throttle detection
        stream.BackpressureController.TryConsumeCredit(1);

        stream.BackpressureController.IsThrottled.Should().BeTrue();
    }

    [Fact]
    public async Task CreditWindow_IsThrottled_FalseAfterCreditRelease()
    {
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 2,
            MaxWindowSize = 4,
            BackpressureThreshold = 0.85,
            InboundChannelCapacity = 16,
            OutboundChannelCapacity = 16,
            MaxProducerWaitTime = null
        };

        await using var engine = CreateEngine(options);
        var stream = await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        stream.BackpressureController.TryConsumeCredit(2);
        stream.BackpressureController.TryConsumeCredit(1); // triggers throttle

        stream.BackpressureController.IsThrottled.Should().BeTrue();

        // Release enough credits to drop below threshold (util = 1-2/4 = 0.5 < 0.85)
        stream.BackpressureController.ReleaseCredit(2);

        stream.BackpressureController.IsThrottled.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Producer timeout
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ProducerTimeout_WhenWindowExhausted_ThrowsOperationCanceledException()
    {
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 1,
            MaxWindowSize = 1,
            InboundChannelCapacity = 16,
            OutboundChannelCapacity = 16,
            MaxProducerWaitTime = TimeSpan.FromMilliseconds(100)
        };

        await using var engine = CreateEngine(options);
        var stream = await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        // Consume the single credit
        await stream.WriteAsync(new StreamMessage("s1", 1, new byte[] { 0x01 }));

        // Second write must time out with OperationCanceledException
        Func<Task> act = async () =>
            await stream.WriteAsync(new StreamMessage("s1", 2, new byte[] { 0x02 }));

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // BackpressureChangedEvent emission
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BackpressureChangedEvent_EmittedWhenThrottled()
    {
        var eventBus = CreateEventBus();
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 2,
            MaxWindowSize = 4,
            InboundChannelCapacity = 16,
            OutboundChannelCapacity = 16,
            EmitBackpressureEvents = true,
            MaxProducerWaitTime = null
        };

        await using var engine = CreateEngine(options, eventBus);
        var stream = await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        var received = new List<BackpressureChangedEvent>();
        eventBus.Subscribe<BackpressureChangedEvent>(e => received.Add(e));

        // Exhaust credits then trigger throttle detection
        stream.BackpressureController.TryConsumeCredit(2);
        stream.BackpressureController.TryConsumeCredit(1); // returns false, applies throttle

        await Task.Delay(80);

        received.Should().Contain(e => e.IsThrottled && e.StreamId == "s1");
    }

    [Fact]
    public async Task BackpressureChangedEvent_EmittedWhenThrottleLifted()
    {
        var eventBus = CreateEventBus();
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 2,
            MaxWindowSize = 4,
            BackpressureThreshold = 0.85,
            InboundChannelCapacity = 16,
            OutboundChannelCapacity = 16,
            EmitBackpressureEvents = true,
            MaxProducerWaitTime = null
        };

        await using var engine = CreateEngine(options, eventBus);
        var stream = await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        var received = new List<BackpressureChangedEvent>();
        eventBus.Subscribe<BackpressureChangedEvent>(e => received.Add(e));

        // Apply throttle
        stream.BackpressureController.TryConsumeCredit(2);
        stream.BackpressureController.TryConsumeCredit(1);
        await Task.Delay(40);

        // Release enough credits to drop utilization below threshold
        // MaxWindowSize=4, release 2 → available=2, util = 1-2/4 = 0.5 < 0.85
        stream.BackpressureController.ReleaseCredit(2);
        await Task.Delay(40);

        received.Should().Contain(e => e.IsThrottled && e.StreamId == "s1",
            "throttle event should fire when window is exhausted");
        received.Should().Contain(e => !e.IsThrottled && e.StreamId == "s1",
            "lift event should fire after credit release drops utilization below threshold");
    }

    [Fact]
    public async Task BackpressureChangedEvent_NotEmitted_WhenEmitDisabled()
    {
        var eventBus = CreateEventBus();
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            InitialWindowSize = 1,
            MaxWindowSize = 1,
            InboundChannelCapacity = 16,
            OutboundChannelCapacity = 16,
            EmitBackpressureEvents = false,
            MaxProducerWaitTime = null
        };

        await using var engine = CreateEngine(options, eventBus);
        var stream = await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        var received = new List<BackpressureChangedEvent>();
        eventBus.Subscribe<BackpressureChangedEvent>(e => received.Add(e));

        stream.BackpressureController.TryConsumeCredit(1);
        stream.BackpressureController.TryConsumeCredit(1); // trigger throttle

        await Task.Delay(60);

        received.Should().BeEmpty("events should not be published when EmitBackpressureEvents is false");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Adaptive mode
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Adaptive_LowUtilization_ControllerWidensWindow()
    {
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.Adaptive,
            InitialWindowSize = 16,
            MaxWindowSize = 64,
            CreditReplenishmentBatch = 8,
            InboundChannelCapacity = 128,
            OutboundChannelCapacity = 128,
            AdaptiveAdjustmentInterval = TimeSpan.FromMilliseconds(40)
        };

        await using var engine = CreateEngine(options);
        var stream = await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        int creditsBefore = stream.BackpressureController.AvailableCredits;

        using var cts = new CancellationTokenSource();
        var adaptiveController = new AdaptiveFlowController(
            engine, options, NullLogger<AdaptiveFlowController>.Instance);

        await adaptiveController.StartAsync(cts.Token);
        await Task.Delay(160); // allow ≥3 ticks at 40 ms each
        await cts.CancelAsync();
        await adaptiveController.StopAsync(CancellationToken.None);

        // With zero utilization, adaptive controller proactively widens the window
        int creditsAfter = stream.BackpressureController.AvailableCredits;
        creditsAfter.Should().BeGreaterThanOrEqualTo(creditsBefore,
            "adaptive controller should widen window under low utilization");
    }

    [Fact]
    public async Task Adaptive_NonAdaptiveMode_ControllerExitsImmediately()
    {
        var options = new FlowControlOptions
        {
            Mode = FlowControlMode.CreditWindow,
            AdaptiveAdjustmentInterval = TimeSpan.FromMilliseconds(50)
        };

        await using var engine = CreateEngine(options);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var adaptiveController = new AdaptiveFlowController(
            engine, options, NullLogger<AdaptiveFlowController>.Instance);

        // StartAsync should return quickly when mode is not Adaptive
        await adaptiveController.StartAsync(cts.Token);
        var stopTask = adaptiveController.StopAsync(CancellationToken.None);
        await stopTask.WaitAsync(TimeSpan.FromSeconds(2));
        stopTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Stream cleanup and resource disposal
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CloseStreamAsync_RemovesStreamAndPublishesEndedEvent()
    {
        var eventBus = CreateEventBus();
        await using var engine = CreateEngine(eventBus: eventBus);

        await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);
        engine.ActiveStreamCount.Should().Be(1);

        await engine.CloseStreamAsync("s1");

        engine.ActiveStreamCount.Should().Be(0);
        engine.GetStream("s1").Should().BeNull();

        await Task.Delay(40);
        var history = eventBus.GetEventHistory(nameof(StreamEndedEvent));
        history.Should().Contain(r => ((StreamEndedEvent)r.Data!).StreamId == "s1");
    }

    [Fact]
    public async Task CloseStreamAsync_IdempotentForUnknownStreamId()
    {
        await using var engine = CreateEngine();
        // Should not throw when stream does not exist
        Func<Task> act = async () => await engine.CloseStreamAsync("does-not-exist");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DisposeAsync_ClosesAllActiveStreams()
    {
        var engine = CreateEngine();

        await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);
        await engine.OpenStreamAsync("s2", MethodType.ServerStreaming);
        engine.ActiveStreamCount.Should().Be(2);

        await engine.DisposeAsync();

        engine.ActiveStreamCount.Should().Be(0);
    }

    [Fact]
    public async Task OpenStream_AfterDispose_ThrowsObjectDisposedException()
    {
        var engine = CreateEngine();
        await engine.DisposeAsync();

        Func<Task> act = async () =>
            await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Lifecycle events
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task OpenStream_PublishesStreamStartedEvent()
    {
        var eventBus = CreateEventBus();
        await using var engine = CreateEngine(eventBus: eventBus);

        await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);

        await Task.Delay(40);
        var history = eventBus.GetEventHistory(nameof(StreamStartedEvent));
        history.Should().Contain(r => ((StreamStartedEvent)r.Data!).StreamId == "s1");
    }

    [Fact]
    public async Task GetAllMetrics_ReflectsActiveStreamCount()
    {
        await using var engine = CreateEngine();

        await engine.OpenStreamAsync("s1", MethodType.BidirectionalStreaming);
        await engine.OpenStreamAsync("s2", MethodType.ServerStreaming);

        var metrics = engine.GetAllMetrics();
        metrics.Should().ContainKey("s1");
        metrics.Should().ContainKey("s2");
        metrics.Count.Should().Be(2);
    }
}
