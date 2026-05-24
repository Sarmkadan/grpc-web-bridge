#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Caching;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class CacheManagerTests : IDisposable
{
    private readonly CacheManager _cache;

    public CacheManagerTests()
    {
        _cache = new CacheManager(NullLogger<CacheManager>.Instance, new CacheManagerOptions
        {
            DefaultTtl = TimeSpan.FromMinutes(5)
        });
    }

    public void Dispose() => _cache.Dispose();

    // ─────────────────────────────────────────────────────────────────────
    // Set / TryGet
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Set_AndTryGet_WithSameKey_ReturnsCachedValue()
    {
        _cache.Set("key1", "value1");

        var found = _cache.TryGet<string>("key1", out var value);

        found.Should().BeTrue();
        value.Should().Be("value1");
    }

    [Fact]
    public void TryGet_WithNonExistentKey_ReturnsFalse()
    {
        var found = _cache.TryGet<string>("missing", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void Set_OverwritesExistingKey()
    {
        _cache.Set("key", "original");
        _cache.Set("key", "updated");

        _cache.TryGet<string>("key", out var value);
        value.Should().Be("updated");
    }

    [Fact]
    public void Set_WithNullValue_ThrowsArgumentNullException()
    {
        var act = () => _cache.Set<string>("key", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_WithEmptyKey_ThrowsArgumentException()
    {
        var act = () => _cache.Set(string.Empty, "value");
        act.Should().Throw<ArgumentException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // TTL / Expiry
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void TryGet_AfterExpiry_ReturnsFalse()
    {
        _cache.Set("expiry-key", "will-expire", TimeSpan.FromMilliseconds(50));

        Thread.Sleep(100);

        var found = _cache.TryGet<string>("expiry-key", out _);
        found.Should().BeFalse("entry should have expired");
    }

    [Fact]
    public void Contains_AfterExpiry_ReturnsFalse()
    {
        _cache.Set("c-key", "value", TimeSpan.FromMilliseconds(50));
        Thread.Sleep(100);

        _cache.Contains("c-key").Should().BeFalse();
    }

    [Fact]
    public void GetTimeToLive_ForFreshEntry_ReturnsPositiveDuration()
    {
        _cache.Set("ttl-key", "value", TimeSpan.FromSeconds(10));

        var ttl = _cache.GetTimeToLive("ttl-key");

        ttl.Should().NotBeNull();
        ttl!.Value.Should().BeGreaterThan(TimeSpan.Zero);
        ttl.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GetTimeToLive_ForMissingKey_ReturnsNull()
    {
        _cache.GetTimeToLive("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetTimeToLive_ForEmptyKey_ReturnsNull()
    {
        _cache.GetTimeToLive(string.Empty).Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Contains / Remove
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Contains_WithExistingKey_ReturnsTrue()
    {
        _cache.Set("present", 42);
        _cache.Contains("present").Should().BeTrue();
    }

    [Fact]
    public void Contains_WithMissingKey_ReturnsFalse()
    {
        _cache.Contains("absent").Should().BeFalse();
    }

    [Fact]
    public void Contains_WithEmptyKey_ReturnsFalse()
    {
        _cache.Contains(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndKeyIsGone()
    {
        _cache.Set("remove-me", "val");

        var removed = _cache.Remove("remove-me");

        removed.Should().BeTrue();
        _cache.Contains("remove-me").Should().BeFalse();
    }

    [Fact]
    public void Remove_NonExistentKey_ReturnsFalse()
    {
        _cache.Remove("ghost").Should().BeFalse();
    }

    [Fact]
    public void Remove_WithEmptyKey_ReturnsFalse()
    {
        _cache.Remove(string.Empty).Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Clear
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _cache.Set("a", 1);
        _cache.Set("b", 2);
        _cache.Set("c", 3);

        _cache.Clear();

        _cache.Contains("a").Should().BeFalse();
        _cache.Contains("b").Should().BeFalse();
        _cache.Contains("c").Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // RemovePattern
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void RemovePattern_WithMatchingPrefix_RemovesMatchingEntries()
    {
        _cache.Set("user:1", "Alice");
        _cache.Set("user:2", "Bob");
        _cache.Set("product:1", "Widget");

        var count = _cache.RemovePattern("user:*");

        count.Should().Be(2);
        _cache.Contains("user:1").Should().BeFalse();
        _cache.Contains("user:2").Should().BeFalse();
        _cache.Contains("product:1").Should().BeTrue();
    }

    [Fact]
    public void RemovePattern_WithNoMatch_ReturnsZero()
    {
        _cache.Set("item", "value");

        var count = _cache.RemovePattern("nomatch:*");

        count.Should().Be(0);
        _cache.Contains("item").Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetOrSet / GetOrSetAsync
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetOrSet_WithMissingKey_CallsFactoryAndCachesResult()
    {
        int callCount = 0;

        var value = _cache.GetOrSet("factory-key", () =>
        {
            callCount++;
            return "factory-value";
        });

        value.Should().Be("factory-value");
        callCount.Should().Be(1);

        // Second call must not invoke factory again
        _cache.GetOrSet("factory-key", () =>
        {
            callCount++;
            return "factory-value";
        });
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrSetAsync_WithMissingKey_CallsFactoryAndCachesResult()
    {
        int callCount = 0;

        var value = await _cache.GetOrSetAsync("async-key", async () =>
        {
            callCount++;
            await Task.Delay(1);
            return "async-value";
        });

        value.Should().Be("async-value");
        callCount.Should().Be(1);

        await _cache.GetOrSetAsync("async-key", async () =>
        {
            callCount++;
            return await Task.FromResult("async-value");
        });
        callCount.Should().Be(1);
    }

    // ─────────────────────────────────────────────────────────────────────
    // SetExpiration
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetExpiration_ForExistingKey_UpdatesExpiryAndEntryEventuallyExpires()
    {
        _cache.Set("exp-key", "value", TimeSpan.FromSeconds(60));

        var result = _cache.SetExpiration("exp-key", TimeSpan.FromMilliseconds(50));
        result.Should().BeTrue();

        Thread.Sleep(100);
        _cache.Contains("exp-key").Should().BeFalse("expiration was shortened to 50 ms");
    }

    [Fact]
    public void SetExpiration_ForMissingKey_ReturnsFalse()
    {
        _cache.SetExpiration("nonexistent", TimeSpan.FromSeconds(10)).Should().BeFalse();
    }

    [Fact]
    public void SetExpiration_WithEmptyKey_ReturnsFalse()
    {
        _cache.SetExpiration(string.Empty, TimeSpan.FromSeconds(10)).Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetStatistics
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetStatistics_AfterMultipleHits_ReflectsCorrectCounts()
    {
        // Use an int value: Marshal.SizeOf works on unmanaged value types
        _cache.Set("stat-key", 42);
        _cache.TryGet<int>("stat-key", out _);
        _cache.TryGet<int>("stat-key", out _);

        var stats = _cache.GetStatistics();

        stats.EntryCount.Should().BeGreaterThanOrEqualTo(1);
        stats.TotalHits.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void GetStatistics_OnEmptyCache_ReturnsZeroEntries()
    {
        var stats = _cache.GetStatistics();
        stats.EntryCount.Should().Be(0);
        stats.TotalHits.Should().Be(0);
    }
}
