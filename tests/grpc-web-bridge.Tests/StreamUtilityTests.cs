#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Utilities;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class StreamUtilityTests
{
    // ─────────────────────────────────────────────────────────────────────
    // CopyStreamChunkedAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CopyStreamChunkedAsync_WithData_CopiesAllBytes()
    {
        var data = Encoding.UTF8.GetBytes("hello streaming world");
        using var source = new MemoryStream(data);
        using var destination = new MemoryStream();

        await StreamUtility.CopyStreamChunkedAsync(source, destination);

        destination.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task CopyStreamChunkedAsync_WithEmptySource_ProducesEmptyDestination()
    {
        using var source = new MemoryStream();
        using var destination = new MemoryStream();

        await StreamUtility.CopyStreamChunkedAsync(source, destination);

        destination.Length.Should().Be(0);
    }

    [Fact]
    public async Task CopyStreamChunkedAsync_WithNullSource_ThrowsArgumentNullException()
    {
        using var destination = new MemoryStream();

        Func<Task> act = () => StreamUtility.CopyStreamChunkedAsync(null!, destination);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CopyStreamChunkedAsync_WithNullDestination_ThrowsArgumentNullException()
    {
        using var source = new MemoryStream(new byte[] { 1, 2, 3 });

        Func<Task> act = () => StreamUtility.CopyStreamChunkedAsync(source, null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CopyStreamChunkedAsync_WithInvalidChunkSize_ThrowsArgumentOutOfRangeException(int chunkSize)
    {
        using var source = new MemoryStream(new byte[] { 1 });
        using var destination = new MemoryStream();

        Func<Task> act = () => StreamUtility.CopyStreamChunkedAsync(source, destination, chunkSize);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // ReadStreamToEndAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadStreamToEndAsync_WithData_ReturnsAllBytes()
    {
        var data = Encoding.UTF8.GetBytes("complete stream content");
        using var stream = new MemoryStream(data);

        var result = await StreamUtility.ReadStreamToEndAsync(stream);

        result.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task ReadStreamToEndAsync_WithEmptyStream_ReturnsEmptyArray()
    {
        using var stream = new MemoryStream();

        var result = await StreamUtility.ReadStreamToEndAsync(stream);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadStreamToEndAsync_WithNullStream_ThrowsArgumentNullException()
    {
        Func<Task> act = () => StreamUtility.ReadStreamToEndAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ReadStreamToEndAsync_WhenExceedsMaxSize_ThrowsInvalidOperationException()
    {
        var data = new byte[200];
        using var stream = new MemoryStream(data);

        Func<Task> act = () => StreamUtility.ReadStreamToEndAsync(stream, maxSizeBytes: 100);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum size*");
    }

    // ─────────────────────────────────────────────────────────────────────
    // CompressStreamAsync / DecompressStreamAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CompressAndDecompress_RoundTrip_RecoverOriginalData()
    {
        var original = Encoding.UTF8.GetBytes("data that should survive round-trip compression");
        using var source = new MemoryStream(original);
        var compressedBytes = new MemoryStream();
        await StreamUtility.CompressStreamAsync(source, compressedBytes);

        // Reset position on a new stream to avoid closed-stream issues after GZipStream disposes it
        var compressedForDecompression = new MemoryStream(compressedBytes.ToArray());
        using var decompressed = new MemoryStream();
        await StreamUtility.DecompressStreamAsync(compressedForDecompression, decompressed);

        decompressed.ToArray().Should().BeEquivalentTo(original);
    }

    [Fact]
    public async Task CompressStreamAsync_WithNullSource_ThrowsArgumentNullException()
    {
        using var destination = new MemoryStream();
        Func<Task> act = () => StreamUtility.CompressStreamAsync(null!, destination);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DecompressStreamAsync_WithNullDestination_ThrowsArgumentNullException()
    {
        using var source = new MemoryStream();
        Func<Task> act = () => StreamUtility.DecompressStreamAsync(source, null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // StreamToBase64Async / Base64ToStream
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StreamToBase64Async_WithData_ReturnsValidBase64()
    {
        var data = Encoding.UTF8.GetBytes("encode me");
        using var stream = new MemoryStream(data);

        var base64 = await StreamUtility.StreamToBase64Async(stream);

        base64.Should().NotBeNullOrEmpty();
        Convert.FromBase64String(base64).Should().BeEquivalentTo(data);
    }

    [Fact]
    public void Base64ToStream_WithValidBase64_ReturnsStream()
    {
        var data = Encoding.UTF8.GetBytes("decode me");
        var base64 = Convert.ToBase64String(data);

        using var stream = StreamUtility.Base64ToStream(base64);

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public void Base64ToStream_WithEmptyString_ThrowsArgumentException()
    {
        var act = () => StreamUtility.Base64ToStream(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Base64ToStream_WithInvalidBase64_ThrowsInvalidOperationException()
    {
        var act = () => StreamUtility.Base64ToStream("not valid base64 !!!");
        act.Should().Throw<InvalidOperationException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // TeeStreamAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TeeStreamAsync_WithMultipleDestinations_WritesIdenticalDataToAll()
    {
        var data = Encoding.UTF8.GetBytes("broadcast this");
        using var source = new MemoryStream(data);
        using var dest1 = new MemoryStream();
        using var dest2 = new MemoryStream();

        await StreamUtility.TeeStreamAsync(source, dest1, dest2);

        dest1.ToArray().Should().BeEquivalentTo(data);
        dest2.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task TeeStreamAsync_WithNullSource_ThrowsArgumentNullException()
    {
        using var dest = new MemoryStream();
        Func<Task> act = () => StreamUtility.TeeStreamAsync(null!, dest);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task TeeStreamAsync_WithNoDestinations_ThrowsArgumentException()
    {
        using var source = new MemoryStream(new byte[] { 1 });
        Func<Task> act = () => StreamUtility.TeeStreamAsync(source);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // IsStreamValid / GetStreamLength / SafeSeek
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsStreamValid_WithReadableStream_ReturnsTrue()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        StreamUtility.IsStreamValid(stream).Should().BeTrue();
    }

    [Fact]
    public void IsStreamValid_WithNullStream_ReturnsFalse()
    {
        StreamUtility.IsStreamValid(null!).Should().BeFalse();
    }

    [Fact]
    public void GetStreamLength_WithSeekableStream_ReturnsLength()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        StreamUtility.GetStreamLength(stream).Should().Be(5);
    }

    [Fact]
    public void GetStreamLength_WithNullStream_ReturnsNull()
    {
        StreamUtility.GetStreamLength(null!).Should().BeNull();
    }

    [Fact]
    public void SafeSeek_OnSeekableStream_ChangesPosition()
    {
        using var stream = new MemoryStream(new byte[] { 0, 1, 2, 3, 4 });
        StreamUtility.SafeSeek(stream, 3, SeekOrigin.Begin);
        stream.Position.Should().Be(3);
    }

    // ─────────────────────────────────────────────────────────────────────
    // CalculateStreamHashAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateStreamHashAsync_WithData_ReturnsHexHash()
    {
        var data = Encoding.UTF8.GetBytes("hash me");
        using var stream = new MemoryStream(data);

        using var sha256 = SHA256.Create();
        var hash = await StreamUtility.CalculateStreamHashAsync(stream, sha256);

        hash.Should().NotBeNullOrEmpty();
        hash.Should().MatchRegex("^[0-9A-F]+$");
    }

    [Fact]
    public async Task CalculateStreamHashAsync_SameData_ProducesSameHash()
    {
        var data = Encoding.UTF8.GetBytes("deterministic");

        using var sha1 = SHA256.Create();
        using var stream1 = new MemoryStream(data);
        var hash1 = await StreamUtility.CalculateStreamHashAsync(stream1, sha1);

        using var sha2 = SHA256.Create();
        using var stream2 = new MemoryStream(data);
        var hash2 = await StreamUtility.CalculateStreamHashAsync(stream2, sha2);

        hash1.Should().Be(hash2);
    }

    // ─────────────────────────────────────────────────────────────────────
    // WriteWithRetryAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task WriteWithRetryAsync_WithValidData_WritesToStream()
    {
        using var stream = new MemoryStream();
        var data = Encoding.UTF8.GetBytes("retry write");

        await StreamUtility.WriteWithRetryAsync(stream, data);

        stream.ToArray().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task WriteWithRetryAsync_WithNullStream_ThrowsArgumentNullException()
    {
        Func<Task> act = () => StreamUtility.WriteWithRetryAsync(null!, new byte[] { 1 });
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WriteWithRetryAsync_WithNullOrEmptyData_DoesNotThrow()
    {
        using var stream = new MemoryStream();
        Func<Task> act = () => StreamUtility.WriteWithRetryAsync(stream, Array.Empty<byte>());
        await act.Should().NotThrowAsync();
    }
}
