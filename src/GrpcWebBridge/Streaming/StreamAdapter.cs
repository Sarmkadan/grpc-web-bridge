#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Buffers;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Adapts between gRPC-Web framed streams and raw gRPC binary streams.
/// Handles the 5-byte length-prefixed framing used by gRPC-Web protocol,
/// converting between HTTP/1.1 chunked responses and gRPC HTTP/2 frames.
/// </summary>
public sealed class StreamAdapter : IDisposable
{
    private const int GrpcFrameHeaderSize = 5;
    private const byte DataFrameFlag = 0x00;
    private const byte TrailerFrameFlag = 0x80;
    private const int MaxFrameSize = 4 * 1024 * 1024; // 4 MB

    private readonly ILogger<StreamAdapter> _logger;
    private readonly IStreamingSessionManager _sessionManager;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamAdapter"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording stream adaptation events.</param>
    /// <param name="sessionManager">The session manager for tracking active streams.</param>
    public StreamAdapter(ILogger<StreamAdapter> logger, IStreamingSessionManager sessionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    }

    /// <summary>
    /// Wraps raw protobuf bytes into a gRPC-Web data frame with the 5-byte length-prefixed header.
    /// </summary>
    /// <param name="payload">The raw protobuf message bytes.</param>
    /// <returns>The framed gRPC-Web message including the 5-byte header.</returns>
    /// <exception cref="ProtocolException">Thrown when the payload exceeds the maximum frame size.</exception>
    public byte[] FrameMessage(ReadOnlySpan<byte> payload)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (payload.Length > MaxFrameSize)
        {
            throw new ProtocolException($"Payload size {payload.Length} exceeds max frame size {MaxFrameSize}");
        }

        var frame = new byte[GrpcFrameHeaderSize + payload.Length];
        frame[0] = DataFrameFlag;
        WriteBigEndianInt32(frame.AsSpan(1), payload.Length);
        payload.CopyTo(frame.AsSpan(GrpcFrameHeaderSize));

        _logger.LogDebug("Framed data message: {PayloadSize} bytes", payload.Length);
        return frame;
    }

    /// <summary>
    /// Reads and deframes a single gRPC-Web message from the source stream.
    /// </summary>
    /// <param name="source">The source stream containing gRPC-Web framed data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The deframed message payload, or null if the stream ended or a trailer frame was received.</returns>
    public async Task<byte[]?> DeframeMessageAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(source);

        var header = new byte[GrpcFrameHeaderSize];
        var bytesRead = await ReadExactAsync(source, header, cancellationToken);
        if (bytesRead == 0)
        {
            return null;
        }

        if (bytesRead < GrpcFrameHeaderSize)
        {
            throw new ProtocolException($"Incomplete gRPC frame header: got {bytesRead} of {GrpcFrameHeaderSize} bytes");
        }

        var flags = header[0];
        var length = ReadBigEndianInt32(header.AsSpan(1));

        if (length < 0 || length > MaxFrameSize)
        {
            throw new ProtocolException($"Invalid gRPC frame length: {length}");
        }

        if ((flags & TrailerFrameFlag) != 0)
        {
            _logger.LogDebug("Received trailer frame, {Length} bytes", length);
            // Skip trailer content
            if (length > 0)
            {
                var skip = new byte[length];
                await ReadExactAsync(source, skip, cancellationToken);
            }
            return null;
        }

        var payload = new byte[length];
        var payloadRead = await ReadExactAsync(source, payload, cancellationToken);
        if (payloadRead < length)
        {
            throw new ProtocolException($"Incomplete gRPC frame body: got {payloadRead} of {length} bytes");
        }

        _logger.LogDebug("Deframed data message: {PayloadSize} bytes", length);
        return payload;
    }

    /// <summary>
    /// Creates a gRPC-Web trailer frame from status metadata.
    /// </summary>
    /// <param name="statusCode">The gRPC status code.</param>
    /// <param name="statusMessage">The status message.</param>
    /// <returns>The framed trailer bytes.</returns>
    public byte[] CreateTrailerFrame(GrpcStatusCode statusCode, string? statusMessage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var trailerText = $"grpc-status:{(int)statusCode}\r\ngrpc-message:{statusMessage ?? string.Empty}\r\n";
        var trailerBytes = System.Text.Encoding.ASCII.GetBytes(trailerText);

        var frame = new byte[GrpcFrameHeaderSize + trailerBytes.Length];
        frame[0] = TrailerFrameFlag;
        WriteBigEndianInt32(frame.AsSpan(1), trailerBytes.Length);
        trailerBytes.CopyTo(frame.AsSpan(GrpcFrameHeaderSize));

        _logger.LogDebug("Created trailer frame: status={StatusCode}", statusCode);
        return frame;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }

    private static void WriteBigEndianInt32(Span<byte> target, int value)
    {
        target[0] = (byte)(value >> 24);
        target[1] = (byte)(value >> 16);
        target[2] = (byte)(value >> 8);
        target[3] = (byte)value;
    }

    private static int ReadBigEndianInt32(ReadOnlySpan<byte> source)
    {
        return (source[0] << 24) | (source[1] << 16) | (source[2] << 8) | source[3];
    }

    private static async Task<int> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), ct);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }
        return totalRead;
    }
}
