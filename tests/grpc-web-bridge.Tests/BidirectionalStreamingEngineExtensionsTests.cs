using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Streaming;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class BidirectionalStreamingEngineExtensionsTests
{
    private static BidirectionalStreamingEngine CreateEngine()
    {
        return new BidirectionalStreamingEngine(NullLoggerFactory.Instance);
    }

    [Fact]
    public async Task GetStreamMetrics_ValidStream_ReturnsMetrics()
    {
        var engine = CreateEngine();
        var streamId = "test-stream";
        await engine.OpenStreamAsync(streamId, MethodType.Unary);

        var metrics = engine.GetStreamMetrics(streamId);

        metrics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStreamMetrics_EngineNull_ThrowsArgumentNullException()
    {
        BidirectionalStreamingEngine? engine = null;
        
        Action act = () => engine!.GetStreamMetrics("test-stream");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetStreamsByMethodType_ReturnsMatchingStreams()
    {
        var engine = CreateEngine();
        await engine.OpenStreamAsync("stream1", MethodType.Unary);
        await engine.OpenStreamAsync("stream2", MethodType.ServerStreaming);
        await engine.OpenStreamAsync("stream3", MethodType.Unary);

        var streams = engine.GetStreamsByMethodType(MethodType.Unary);

        streams.Should().HaveCount(2);
        streams.Should().AllSatisfy(s => s.MethodType.Should().Be(MethodType.Unary));
    }

    [Fact]
    public async Task GetTotalMessageCount_ReturnsCorrectSum()
    {
        var engine = CreateEngine();
        var stream1 = await engine.OpenStreamAsync("stream1", MethodType.Unary);
        var stream2 = await engine.OpenStreamAsync("stream2", MethodType.Unary);

        // Record some messages by writing (WriteAsync triggers RecordOutbound)
        await stream1.WriteAsync(new StreamMessage { Data = new byte[10] });
        await stream2.WriteAsync(new StreamMessage { Data = new byte[20] });

        var totalMessages = engine.GetTotalMessageCount();

        totalMessages.Should().Be(2);
    }

    [Fact]
    public async Task GetTotalBytesTransferred_ReturnsCorrectSum()
    {
        var engine = CreateEngine();
        var stream1 = await engine.OpenStreamAsync("stream1", MethodType.Unary);
        var stream2 = await engine.OpenStreamAsync("stream2", MethodType.Unary);

        await stream1.WriteAsync(new StreamMessage { Data = new byte[10] });
        await stream2.WriteAsync(new StreamMessage { Data = new byte[20] });

        var totalBytes = engine.GetTotalBytesTransferred();

        totalBytes.Should().Be(30);
    }
}
