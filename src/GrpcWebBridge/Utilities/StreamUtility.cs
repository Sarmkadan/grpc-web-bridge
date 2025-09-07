#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Buffers;
using System.IO.Pipelines;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// Stream handling utilities for efficient data transfer.
/// Provides buffering, compression, and chunking operations.
/// Optimized for high-throughput streaming scenarios.
/// </summary>
public static class StreamUtility
{
    /// <summary>
    /// Copies stream data with chunking for large transfers.
    /// </summary>
    public static async Task CopyStreamChunkedAsync(
        Stream source,
        Stream destination,
        int chunkSize = 81920)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (destination is null)
            throw new ArgumentNullException(nameof(destination));

        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize), chunkSize, "Chunk size must be positive");

        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, chunkSize))) > 0)
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead));

            await destination.FlushAsync();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Reads an entire stream into a byte array.
    /// Allocates memory as needed, respecting max size limits.
    /// </summary>
    public static async Task<byte[]> ReadStreamToEndAsync(Stream stream, int maxSizeBytes = 10 * 1024 * 1024)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        const int chunkSize = 81920;
        var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
        try
        {
            using var ms = new MemoryStream(stream.CanSeek ? (int)Math.Min(stream.Length, maxSizeBytes) : chunkSize);
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, chunkSize))) > 0)
            {
                if (ms.Length + bytesRead > maxSizeBytes)
                    throw new InvalidOperationException($"Stream exceeds maximum size of {maxSizeBytes} bytes");

                await ms.WriteAsync(buffer.AsMemory(0, bytesRead));
            }

            return ms.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Reads a stream line by line asynchronously.
    /// </summary>
    public static async IAsyncEnumerable<string> ReadLinesAsync(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        using (var reader = new StreamReader(stream))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                yield return line;
            }
        }
    }

    /// <summary>
    /// Creates a pipe reader from a stream.
    /// Enables high-performance streaming with pipelines.
    /// </summary>
    public static PipeReader CreatePipeReader(Stream stream, int bufferSize = 81920)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        return PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: bufferSize));
    }

    /// <summary>
    /// Creates a pipe writer from a stream.
    /// </summary>
    public static PipeWriter CreatePipeWriter(Stream stream, int bufferSize = 81920)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        return PipeWriter.Create(stream, new StreamPipeWriterOptions());
    }

    /// <summary>
    /// Compresses stream data using gzip.
    /// </summary>
    public static async Task CompressStreamAsync(Stream source, Stream destination)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (destination is null)
            throw new ArgumentNullException(nameof(destination));

        using (var gzip = new System.IO.Compression.GZipStream(destination, System.IO.Compression.CompressionMode.Compress))
        {
            await source.CopyToAsync(gzip);
        }
    }

    /// <summary>
    /// Decompresses gzip stream data.
    /// </summary>
    public static async Task DecompressStreamAsync(Stream source, Stream destination)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (destination is null)
            throw new ArgumentNullException(nameof(destination));

        using (var gzip = new System.IO.Compression.GZipStream(source, System.IO.Compression.CompressionMode.Decompress))
        {
            await gzip.CopyToAsync(destination);
        }
    }

    /// <summary>
    /// Writes data to a stream with retry logic.
    /// Handles transient failures and ensures data is written.
    /// </summary>
    public static async Task WriteWithRetryAsync(
        Stream stream,
        byte[] data,
        int maxRetries = 3,
        int delayMs = 100)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        // Fix: validate boundary values for retry parameters
        if (maxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "Max retries cannot be negative");

        if (delayMs < 0)
            throw new ArgumentOutOfRangeException(nameof(delayMs), delayMs, "Delay cannot be negative");

        if (data is null || data.Length == 0)
            return;

        int retries = 0;
        while (true)
        {
            try
            {
                await stream.WriteAsync(data.AsMemory());
                await stream.FlushAsync();
                return;
            }
            catch (IOException) when (retries < maxRetries)
            {
                retries++;
                await Task.Delay(delayMs * retries);
            }
        }
    }

    /// <summary>
    /// Seeks to a position in a stream with fallback for unseekable streams.
    /// </summary>
    public static void SafeSeek(Stream stream, long offset, SeekOrigin origin)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (stream.CanSeek)
        {
            stream.Seek(offset, origin);
        }
        else
        {
            throw new NotSupportedException("Stream does not support seeking");
        }
    }

    /// <summary>
    /// Gets the length of a stream, handling unseekable streams.
    /// </summary>
    public static long? GetStreamLength(Stream stream)
    {
        if (stream is null)
            return null;

        try
        {
            return stream.CanSeek ? stream.Length : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Validates that a stream is readable and open.
    /// </summary>
    public static bool IsStreamValid(Stream stream)
    {
        return stream is not null && stream.CanRead;
    }

    /// <summary>
    /// Converts stream to Base64 encoded string.
    /// </summary>
    public static async Task<string> StreamToBase64Async(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        var bytes = await ReadStreamToEndAsync(stream);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Converts Base64 string to stream.
    /// </summary>
    public static Stream Base64ToStream(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
            throw new ArgumentException("Base64 string cannot be null or empty", nameof(base64String));

        try
        {
            var bytes = Convert.FromBase64String(base64String);
            return new MemoryStream(bytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Invalid Base64 string", ex);
        }
    }

    /// <summary>
    /// Calculates hash of stream content.
    /// Resets stream position after calculation.
    /// </summary>
    public static async Task<string> CalculateStreamHashAsync(Stream stream, System.Security.Cryptography.HashAlgorithm algorithm)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (algorithm is null)
            throw new ArgumentNullException(nameof(algorithm));

        var originalPosition = stream.CanSeek ? stream.Position : -1;

        try
        {
            var hash = await algorithm.ComputeHashAsync(stream);
            return Convert.ToHexString(hash);
        }
        finally
        {
            if (originalPosition >= 0 && stream.CanSeek)
            {
                stream.Seek(originalPosition, SeekOrigin.Begin);
            }
        }
    }

    /// <summary>
    /// Tees stream data to multiple destinations.
    /// Useful for logging, monitoring, and data replication.
    /// </summary>
    public static async Task TeeStreamAsync(Stream source, params Stream[] destinations)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (destinations is null || destinations.Length == 0)
            throw new ArgumentException("At least one destination stream required", nameof(destinations));

        const int bufferSize = 81920;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, bufferSize))) > 0)
            {
                var segment = buffer.AsMemory(0, bytesRead);
                var tasks = destinations
                    .Where(d => d is not null && d.CanWrite)
                    .Select(d => d.WriteAsync(segment).AsTask());

                await Task.WhenAll(tasks);
            }

            foreach (var dest in destinations.Where(d => d is not null))
                await dest.FlushAsync();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
