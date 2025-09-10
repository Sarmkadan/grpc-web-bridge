#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class StreamProcessingBenchmarks
{
    private byte[] _payload1KB = null!;
    private byte[] _payload64KB = null!;
    private byte[] _payload1MB = null!;

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

    [Benchmark(Description = "ReadStreamToEnd — 1 KB")]
    public async Task<byte[]> ReadStreamToEnd_1KB()
    {
        using var ms = new MemoryStream(_payload1KB);
        return await StreamUtility.ReadStreamToEndAsync(ms).ConfigureAwait(false);
    }

    [Benchmark(Description = "ReadStreamToEnd — 64 KB")]
    public async Task<byte[]> ReadStreamToEnd_64KB()
    {
        using var ms = new MemoryStream(_payload64KB);
        return await StreamUtility.ReadStreamToEndAsync(ms).ConfigureAwait(false);
    }

    [Benchmark(Description = "ReadStreamToEnd — 1 MB")]
    public async Task<byte[]> ReadStreamToEnd_1MB()
    {
        using var ms = new MemoryStream(_payload1MB);
        return await StreamUtility.ReadStreamToEndAsync(ms).ConfigureAwait(false);
    }

    [Benchmark(Description = "CopyStreamChunked — 1 KB")]
    public async Task CopyStreamChunked_1KB()
    {
        using var src = new MemoryStream(_payload1KB);
        using var dst = new MemoryStream(_payload1KB.Length);
        await StreamUtility.CopyStreamChunkedAsync(src, dst).ConfigureAwait(false);
    }

    [Benchmark(Description = "CopyStreamChunked — 64 KB")]
    public async Task CopyStreamChunked_64KB()
    {
        using var src = new MemoryStream(_payload64KB);
        using var dst = new MemoryStream(_payload64KB.Length);
        await StreamUtility.CopyStreamChunkedAsync(src, dst).ConfigureAwait(false);
    }

    [Benchmark(Description = "CopyStreamChunked — 1 MB")]
    public async Task CopyStreamChunked_1MB()
    {
        using var src = new MemoryStream(_payload1MB);
        using var dst = new MemoryStream(_payload1MB.Length);
        await StreamUtility.CopyStreamChunkedAsync(src, dst).ConfigureAwait(false);
    }

    [Benchmark(Description = "StreamToBase64 — 1 KB")]
    public async Task<string> StreamToBase64_1KB()
    {
        using var ms = new MemoryStream(_payload1KB);
        return await StreamUtility.StreamToBase64Async(ms).ConfigureAwait(false);
    }
}
