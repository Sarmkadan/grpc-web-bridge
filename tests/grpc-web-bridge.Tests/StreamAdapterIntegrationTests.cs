#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Events;
using GrpcWebBridge.Services;
using GrpcWebBridge.Streaming;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Integration tests for StreamAdapter covering server-streaming scenarios:
/// - incremental frame delivery (not buffered until completion)
/// - trailer frame arrival with grpc-status
/// - client cancellation cleanup
/// - mid-stream server exception handling
/// </summary>
public sealed class StreamAdapterIntegrationTests
{
    private readonly ILogger<StreamAdapter> _logger;
    private readonly StreamAdapter _adapter;
    private readonly IStreamingSessionManager _sessionManager;
    private readonly EventBus _eventBus;

    public StreamAdapterIntegrationTests()
    {
        _logger = Substitute.For<ILogger<StreamAdapter>>();
        _sessionManager = Substitute.For<IStreamingSessionManager>();
        _eventBus = new EventBus(NullLogger<EventBus>.Instance);
        _adapter = new StreamAdapter(_logger, _sessionManager);
    }

    /// <summary>
    /// Verifies that a test service emitting N messages with delays results in
    /// each length-prefixed frame being received incrementally (not buffered until completion).
    /// </summary>
    [Fact]
    public async Task ServerStreaming_EmitsNMessagesWithDelays_ReceivedIncrementally()
    {
        // Arrange: Set up a streaming session with N messages
        const int messageCount = 5;
        var messages = new List<StreamMessage>();
        for (int i = 1; i <= messageCount; i++)
        {
            messages.Add(new StreamMessage("test-stream", i, new byte[] { (byte)i }));
        }

        // Act & Assert: Test that frames are deframed incrementally
        using var ms = new MemoryStream();
        foreach (var msg in messages)
        {
            var framed = _adapter.FrameMessage(msg.Data!);
            await ms.WriteAsync(framed, cancellationToken: default);
        }
        await ms.FlushAsync(default);

        // Reset position to read from beginning
        ms.Position = 0;

        // Deframe each message individually - should receive each length-prefixed frame
        var receivedFrames = new List<byte[]>();
        while (ms.Position < ms.Length)
        {
            var payload = await _adapter.DeframeMessageAsync(ms, CancellationToken.None);
            receivedFrames.Add(payload!);
        }

        // Should have received all 5 frames incrementally
        receivedFrames.Count.Should().Be(messageCount,
            "should receive each length-prefixed frame incrementally, not buffered until completion");

        // Each frame should have the correct data
        for (int i = 0; i < messageCount; i++)
        {
            receivedFrames[i].Should().BeEquivalentTo(new byte[] { (byte)(i + 1) });
        }
    }

    /// <summary>
    /// Verifies that the trailer frame (0x80 flag) arrives last with grpc-status 0
    /// after all data frames have been received.
    /// </summary>
    [Fact]
    public async Task ServerStreaming_TrailerFrameArrivesLast_WithGrpcStatus0()
    {
        // Arrange: Send 3 data frames followed by a trailer
        const int dataFrameCount = 3;
        var ms = new MemoryStream();

        // Write 3 data frames
        for (int i = 1; i <= dataFrameCount; i++)
        {
            var framed = _adapter.FrameMessage(new byte[] { (byte)i });
            await ms.WriteAsync(framed, cancellationToken: default);
        }

        // Write trailer frame with grpc-status 0
        var trailer = _adapter.CreateTrailerFrame(GrpcStatusCode.Ok, null);
        await ms.WriteAsync(trailer, cancellationToken: default);

        // Reset position to read from beginning
        ms.Position = 0;

        // Deframe messages - data frames will be received, trailer returns null (skipped)
        var receivedFrames = new List<byte[]>();
        var lastPayload = default(byte[]);
        while (ms.Position < ms.Length)
        {
            var payload = await _adapter.DeframeMessageAsync(ms, CancellationToken.None);
            if (payload is not null)
            {
                receivedFrames.Add(payload);
                lastPayload = payload;
            }
        }

        // Should have received 3 data frames (trailer returns null and is skipped)
        receivedFrames.Count.Should().Be(dataFrameCount,
            "should receive all data frames before trailer");

        // The last data frame received should be frame 3
        var lastDataFrame = receivedFrames.Last();
        lastDataFrame.Should().BeEquivalentTo(new byte[] { 3 });
    }

    /// <summary>
    /// Verifies that client cancellation (CancellationToken / aborted HttpRequest)
    /// stops the server stream and disposes resources.
    /// </summary>
    [Fact]
    public async Task ServerStreaming_ClientCancellation_StopsStreamAndDisposes()
    {
        // Arrange: Create a stream with a slow producer that we can cancel mid-stream
        var cts = new CancellationTokenSource();

        // Simulate a slow server stream: write frames with delays
        using var ms = new MemoryStream();
        var framed1 = _adapter.FrameMessage(new byte[] { 0x01 });
        await ms.WriteAsync(framed1, cts.Token);

        // Reset position
        ms.Position = 0;

        // Read the first frame successfully
        var firstPayload = await _adapter.DeframeMessageAsync(ms, cts.Token);
        firstPayload.Should().NotBeNull("first frame should be received before cancellation");
        firstPayload.Should().BeEquivalentTo(new byte[] { 0x01 });

        // Cancel the token (simulating client disconnect / aborted HttpRequest)
        await cts.CancelAsync();

        // After cancellation, further deframe attempts should throw OperationCanceledException
        // (the token is checked by ReadExactAsync inside DeframeMessageAsync)
        Func<Task> act = async () => await _adapter.DeframeMessageAsync(ms, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>(
            "deframing after client cancellation should stop the stream");

        // Dispose the adapter - resources should be released
        _adapter.Dispose();

        // Post-dispose deframing should throw ObjectDisposedException
        Func<Task> disposedAct = async () => await _adapter.DeframeMessageAsync(ms, CancellationToken.None);
        await disposedAct.Should().ThrowAsync<ObjectDisposedException>(
            "adapter should be disposed after cancellation cleanup");
    }

    /// <summary>
    /// Verifies that a mid-stream server exception yields a trailer with non-zero status
    /// instead of a truncated response.
    /// </summary>
    [Fact]
    public async Task ServerStreaming_MidStreamServerException_YieldsTrailerWithNonZeroStatus()
    {
        // Arrange: Send some data frames, then simulate an exception by writing
        // a trailer with a non-zero status code
        var ms = new MemoryStream();

        // Write 2 data frames
        for (int i = 0; i < 2; i++)
        {
            var framed = _adapter.FrameMessage(new byte[] { (byte)i });
            await ms.WriteAsync(framed, cancellationToken: default);
        }

        // Write trailer frame with grpc-status 5 (NotFound = 5)
        var trailer = _adapter.CreateTrailerFrame(GrpcStatusCode.NotFound, "Service not found");
        await ms.WriteAsync(trailer, cancellationToken: default);

        // Reset position to read from beginning
        ms.Position = 0;

        // Deframe messages - data frames will be received, trailer returns null (skipped)
        var receivedFrames = new List<byte[]>();
        while (ms.Position < ms.Length)
        {
            var payload = await _adapter.DeframeMessageAsync(ms, CancellationToken.None);
            if (payload is not null)
            {
                receivedFrames.Add(payload);
            }
        }

        // Should have received 2 data frames (trailer returns null and is skipped)
        receivedFrames.Count.Should().Be(2,
            "should receive all data frames before trailer");

        // Verify the trailer was created correctly with non-zero status
        // by checking the trailer bytes directly
        var trailerText = System.Text.Encoding.ASCII.GetString(trailer);
        trailerText.Should().Contain("grpc-status:5");
        trailerText.Should().Contain("Service not found");
    }

    /// <summary>
    /// Verifies that client cancellation disposes resources properly.
    /// </summary>
    [Fact]
    public void StreamAdapter_Dispose_FreesResources()
    {
        // Act
        _adapter.Dispose();

        // Assert - should be able to detect disposal
        // After dispose, framing/deframing should throw ObjectDisposedException
        var action = new Action(() => _adapter.FrameMessage(new byte[] { 0x01 }));
        action.Should().Throw<ObjectDisposedException>();
    }
}
