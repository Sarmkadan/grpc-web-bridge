#nullable enable
using System.Threading.Channels;
using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Streaming;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for BidirectionalStreamContext covering construction defaults, lifecycle
/// disposal (cancellation, channel completion), mutable state, and string representation.
/// </summary>
public sealed class BidirectionalStreamContextTests
{
    private static BidirectionalStreamContext CreateContext(string streamId = "stream-1") =>
        new()
        {
            StreamId = streamId,
            MethodType = MethodType.BidirectionalStreaming,
            InboundChannel = Channel.CreateBounded<StreamMessage>(10),
            OutboundChannel = Channel.CreateBounded<StreamMessage>(10),
        };

    [Fact]
    public async Task Constructor_DefaultState_IsNewWithoutFinalStatusOrCloseReason()
    {
        await using var context = CreateContext();

        context.State.Should().Be(StreamState.New);
        context.FinalStatus.Should().BeNull();
        context.CloseReason.Should().BeNull();
        context.Metrics.Should().NotBeNull();
        context.Metrics.MessagesIn.Should().Be(0);
        context.Metrics.MessagesOut.Should().Be(0);
        context.LifetimeCts.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    public async Task Constructor_StoresIdentityChannelsAndUtcCreationTime()
    {
        var before = DateTime.UtcNow;
        var inbound = Channel.CreateBounded<StreamMessage>(5);
        var outbound = Channel.CreateBounded<StreamMessage>(5);

        await using var context = new BidirectionalStreamContext
        {
            StreamId = "abc",
            MethodType = MethodType.ServerStreaming,
            InboundChannel = inbound,
            OutboundChannel = outbound,
        };

        context.StreamId.Should().Be("abc");
        context.MethodType.Should().Be(MethodType.ServerStreaming);
        context.InboundChannel.Should().BeSameAs(inbound);
        context.OutboundChannel.Should().BeSameAs(outbound);
        context.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        context.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public async Task DisposeAsync_CancelsLifetimeTokenAndDisposesSource()
    {
        var context = CreateContext();
        var token = context.LifetimeCts.Token;

        await context.DisposeAsync();

        token.IsCancellationRequested.Should().BeTrue();
        var act = () => _ = context.LifetimeCts.Token;
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task DisposeAsync_CompletesChannels_RetainingBufferedMessages()
    {
        var context = CreateContext();
        var message = new StreamMessage("stream-1", 1, new byte[] { 1, 2, 3 });
        context.InboundChannel.Writer.TryWrite(message).Should().BeTrue();

        var pendingOutbound = context.OutboundChannel.Reader.ReadAsync().AsTask();

        await context.DisposeAsync();

        // Already-buffered messages remain readable after completion; Completion
        // only finishes once the buffer has been drained.
        context.InboundChannel.Reader.TryRead(out var buffered).Should().BeTrue();
        buffered!.Id.Should().Be(message.Id);
        context.InboundChannel.Reader.Completion.IsCompleted.Should().BeTrue();
        context.OutboundChannel.Reader.Completion.IsCompleted.Should().BeTrue();

        // A pending reader on an empty channel is released with ChannelClosedException.
        await FluentActions.Awaiting(() => pendingOutbound)
            .Should().ThrowAsync<ChannelClosedException>();
        await FluentActions.Awaiting(() => context.InboundChannel.Reader.ReadAsync().AsTask())
            .Should().ThrowAsync<ChannelClosedException>();
    }

    [Fact]
    public async Task DisposeAsync_RejectsWritesAfterDispose_AndIsIdempotent()
    {
        var context = CreateContext();
        await context.DisposeAsync();

        var act = () => context.DisposeAsync().AsTask();
        await act.Should().NotThrowAsync();

        var message = new StreamMessage("stream-1", 2, new byte[] { 9 });
        context.OutboundChannel.Writer.TryWrite(message).Should().BeFalse();
        context.InboundChannel.Writer.TryWrite(message).Should().BeFalse();
    }

    [Fact]
    public async Task ToString_ReflectsMutableStateFinalStatusAndCloseReason()
    {
        await using var context = CreateContext();

        context.State = StreamState.Closed;
        context.FinalStatus = GrpcStatusCode.Cancelled;
        context.CloseReason = "client gone";

        context.ToString().Should().Be(
            "BidirectionalStreamContext { State = Closed, FinalStatus = Cancelled, CloseReason = client gone }");
    }
}
