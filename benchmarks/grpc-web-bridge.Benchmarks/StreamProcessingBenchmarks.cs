#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Benchmarks;

/// <summary>
/// Provides benchmarks for stream processing operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class StreamProcessingBenchmarks
{
    private byte[] _payload1KB = null!;
    private byte[] _payload64KB = null!;
    private byte[] _payload1MB = null!;

    /// <summary>
    /// Initializes the benchmark by generating random payloads for testing.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _payload1KB = new byte[1024];
        _payload64KB = new byte[64 * 1024];
        _payload1MB = new byte[1024 * 1024];

        var rng = new Random(42);
        rng.NextBytes(_payload1KB);
        rng.NextBytes(_payload64KB);
        rng.NextBytes(_payload1MB);
    }

    /// <summary>
    /// Reads the entire stream to the end and returns the contents as a byte array.
    /// </summary>
    /// <returns>The contents of the stream as a byte array.</returns>
    [Benchmark(Description = "ReadStreamToEnd — 1 KB")]
    public async Task<byte[]> ReadStreamToEnd_1KB()
    {
        using var ms = new MemoryStream(_payload1KB);
        return await StreamUtility.ReadStreamToEndAsync(ms).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the entire stream to the end and returns the contents as a byte array.
    /// </summary>
    /// <returns>The contents of the stream as a byte array.</returns>
    [Benchmark(Description = "ReadStreamToEnd — 64 KB")]
    public async Task<byte[]> ReadStreamToEnd_64KB()
    {
        using var ms = new MemoryStream(_payload64KB);
        return await StreamUtility.ReadStreamToEndAsync(ms).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads the entire stream to the end and returns the contents as a byte array.
    /// </summary>
    /// <returns>The contents of the stream as a byte array.</returns>
    [Benchmark(Description = "ReadStreamToEnd — 1 MB")]
    public async Task<byte[]> ReadStreamToEnd_1MB()
    {
        using var ms = new MemoryStream(_payload1MB);
        return await StreamUtility.ReadStreamToEndAsync(ms).ConfigureAwait(false);
    }

    /// <summary>
    /// Copies the stream in chunked mode and returns the contents as a byte array.
    /// </summary>
    [Benchmark(Description = "CopyStreamChunked — 1 KB")]
    public async Task CopyStreamChunked_1KB()
    {
        using var src = new MemoryStream(_payload1KB);
        using var dst = new MemoryStream(_payload1KB.Length);
        await StreamUtility.CopyStreamChunkedAsync(src, dst).ConfigureAwait(false);
    }

    /// <summary>
    /// Copies the stream in chunked mode and returns the contents as a byte array.
    /// </summary>
    [Benchmark(Description = "CopyStreamChunked — 64 KB")]
    public async Task CopyStreamChunked_64KB()
    {
        using var src = new MemoryStream(_payload64KB);
        using var dst = new MemoryStream(_payload64KB.Length);
        await StreamUtility.CopyStreamChunkedAsync(src, dst).ConfigureAwait(false);
    }

    /// <summary>
    /// Copies the stream in chunked mode and returns the contents as a byte array.
    /// </summary>
    [Benchmark(Description = "CopyStreamChunked — 1 MB")]
    public async Task CopyStreamChunked_1MB()
    {
        using var src = new MemoryStream(_payload1MB);
        using var dst = new MemoryStream(_payload1MB.Length);
        await StreamUtility.CopyStreamChunkedAsync(src, dst).ConfigureAwait(false);
    }

    /// <summary>
    /// Converts the stream to a base64-encoded string.
    /// </summary>
    /// <returns>The base64-encoded string representation of the stream.</returns>
    [Benchmark(Description = "StreamToBase64 — 1 KB")]
    public async Task<string> StreamToBase64_1KB()
    {
        using var ms = new MemoryStream(_payload1KB);
        return await StreamUtility.StreamToBase64Async(ms).ConfigureAwait(false);
    }
}
