#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>Unit tests for <see cref="StreamingService"/> covering stream lifecycle, queueing and idle cleanup.</summary>
public sealed class StreamingServiceTests
{
    private static StreamingService CreateService() =>
        new(NullLogger<StreamingService>.Instance);

    [Fact]
    public void CreateStream_NewId_RegistersActiveStream()
    {
        var service = CreateService();

        var stream = service.CreateStream("s1", MethodType.ServerStreaming);

        stream.StreamId.Should().Be("s1");
        stream.State.Should().Be(StreamState.Active);
        service.GetStream("s1").Should().BeSameAs(stream);
        service.GetStream("unknown").Should().BeNull();
        service.ActiveStreamCount.Should().Be(1);
        service.GetAllStreamIds().Should().ContainSingle().Which.Should().Be("s1");
        service.GetStreamStatistics("s1").MessageCount.Should().Be(0);

        Action unknownStats = () => service.GetStreamStatistics("unknown");
        unknownStats.Should().Throw<StreamingException>();
    }

    [Fact]
    public void CreateStream_BlankOrDuplicateId_Throws()
    {
        var service = CreateService();
        service.CreateStream("dup", MethodType.Unary);

        Action nullId = () => service.CreateStream(null!, MethodType.Unary);
        Action whitespaceId = () => service.CreateStream("  ", MethodType.Unary);
        Action duplicate = () => service.CreateStream("dup", MethodType.Unary);

        nullId.Should().Throw<ArgumentException>();
        whitespaceId.Should().Throw<ArgumentException>();
        duplicate.Should().Throw<StreamingException>();
        service.ActiveStreamCount.Should().Be(1);
    }

    [Fact]
    public void CreateStream_AtMaxStreamCount_ThrowsStreamingException()
    {
        var service = CreateService();
        for (var i = 0; i < Constants.Streaming.MaxStreamCount; i++)
        {
            service.CreateStream($"s-{i}", MethodType.Unary);
        }

        service.ActiveStreamCount.Should().Be(Constants.Streaming.MaxStreamCount);

        Action overflow = () => service.CreateStream("overflow", MethodType.Unary);
        overflow.Should().Throw<StreamingException>();
        service.GetStream("overflow").Should().BeNull();
    }

    [Fact]
    public void EnqueueMessage_ThenDequeue_IsFifoAndNullWhenEmpty()
    {
        var service = CreateService();
        service.CreateStream("s1", MethodType.ClientStreaming);

        service.EnqueueMessage("s1", new StreamMessage("s1", 1, new byte[] { 1 }));
        service.EnqueueMessage("s1", new StreamMessage("s1", 2, new byte[] { 2 }));

        service.GetStream("s1")!.QueuedMessageCount.Should().Be(2);
        service.GetStreamStatistics("s1").MessageCount.Should().Be(2);
        service.DequeueMessage("s1")!.SequenceNumber.Should().Be(1);
        service.DequeueMessage("s1")!.SequenceNumber.Should().Be(2);
        service.DequeueMessage("s1").Should().BeNull();
        service.DequeueMessage("unknown").Should().BeNull();
    }

    [Fact]
    public void EnqueueMessage_InvalidArguments_Throw()
    {
        var service = CreateService();
        service.CreateStream("s1", MethodType.Unary);
        var valid = new StreamMessage("s1", 0, new byte[] { 1 });

        Action nullMessage = () => service.EnqueueMessage("s1", null!);
        Action blankId = () => service.EnqueueMessage(" ", valid);
        Action unknownStream = () => service.EnqueueMessage("missing", valid);
        Action emptyData = () => service.EnqueueMessage("s1", new StreamMessage("s1", 0, StreamMessageType.Data));

        nullMessage.Should().Throw<ArgumentNullException>();
        blankId.Should().Throw<ArgumentException>();
        unknownStream.Should().Throw<StreamingException>();
        emptyData.Should().Throw<ArgumentException>();
        service.GetStream("s1")!.QueuedMessageCount.Should().Be(0);
    }

    [Fact]
    public void CloseStream_RemovesStreamAndRecordsFinalStatus()
    {
        var service = CreateService();
        var stream = service.CreateStream("s1", MethodType.ServerStreaming);
        service.EnqueueMessage("s1", new StreamMessage("s1", 0, new byte[] { 1 }));

        service.CloseStream("s1", GrpcStatusCode.Cancelled, "client went away");

        service.GetStream("s1").Should().BeNull();
        service.ActiveStreamCount.Should().Be(0);
        stream.State.Should().Be(StreamState.Closed);
        stream.FinalStatus.Should().Be(GrpcStatusCode.Cancelled);
        stream.FinalMessage.Should().Be("client went away");
        stream.QueuedMessageCount.Should().Be(0);

        Action missing = () => service.CloseStream("missing");
        Action blank = () => service.CloseStream("");
        missing.Should().NotThrow();
        blank.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SendHeartbeat_QueuesHeartbeatMessage_AndUnknownStreamThrows()
    {
        var service = CreateService();
        service.CreateStream("s1", MethodType.BidirectionalStreaming);

        service.SendHeartbeat("s1");

        var heartbeat = service.DequeueMessage("s1");
        heartbeat.Should().NotBeNull();
        heartbeat!.MessageType.Should().Be(StreamMessageType.Heartbeat);
        heartbeat.StreamId.Should().Be("s1");
        heartbeat.SequenceNumber.Should().Be(0);

        Action act = () => service.SendHeartbeat("missing");
        act.Should().Throw<StreamingException>();
    }

    [Fact]
    public void CleanupIdleStreams_RemovesOnlyStreamsPastIdleTimeout()
    {
        var service = CreateService();
        var idle = service.CreateStream("idle", MethodType.Unary);
        service.CreateStream("fresh", MethodType.Unary);
        idle.LastActivityTime = DateTime.UtcNow.AddSeconds(-(Constants.Streaming.StreamIdleTimeoutSeconds + 1));

        service.CleanupIdleStreams();

        service.GetStream("idle").Should().BeNull();
        idle.State.Should().Be(StreamState.Closed);
        idle.FinalStatus.Should().Be(GrpcStatusCode.DeadlineExceeded);
        idle.FinalMessage.Should().Be("Stream idle timeout");
        service.GetAllStreamIds().Should().ContainSingle().Which.Should().Be("fresh");
    }
}
