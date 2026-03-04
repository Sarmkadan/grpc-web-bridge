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

/// <summary>
/// Unit tests for <see cref="CacheManager"/> class that verify cache operations including set/get, TTL management,
/// pattern-based removal, statistics tracking, and async operations.
/// </summary>
public sealed class CacheManagerTests : IDisposable
{
    /// <summary>
    /// The cache manager instance used for testing with a 5-minute default TTL.
    /// </summary>
    private readonly CacheManager _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheManagerTests"/> class.
    /// </summary>
    public CacheManagerTests()
    {
        _cache = new CacheManager(NullLogger<CacheManager>.Instance, new CacheManagerOptions
        {
            DefaultTtl = TimeSpan.FromMinutes(5)
        });
    }

    /// <summary>
    /// Disposes the cache manager after test execution.
    /// </summary>
    public void Dispose() => _cache.Dispose();

    // ─────────────────────────────────────────────────────────────────────
    // Set / TryGet
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that a value can be stored and retrieved from the cache using the same key.
    /// </summary>
    [Fact]
    public void Set_AndTryGet_WithSameKey_ReturnsCachedValue()
    {
        _cache.Set("key1", "value1");

        var found = _cache.TryGet<string>("key1", out var value);

        found.Should().BeTrue();
        value.Should().Be("value1");
    }

    /// <summary>
    /// Tests that attempting to retrieve a non-existent key returns false and null.
    /// </summary>
    [Fact]
    public void TryGet_WithNonExistentKey_ReturnsFalse()
    {
        var found = _cache.TryGet<string>("missing", out var value);

        found.Should().BeFalse();
        value.Should().BeNull();
    }

    /// <summary>
    /// Tests that setting a value with an existing key overwrites the previous value.
    /// </summary>
    [Fact]
    public void Set_OverwritesExistingKey()
    {
        _cache.Set("key", "original");
        _cache.Set("key", "updated");

        _cache.TryGet<string>("key", out var value);
        value.Should().Be("updated");
    }

    /// <summary>
    /// Tests that setting a null value throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void Set_WithNullValue_ThrowsArgumentNullException()
    {
        var act = () => _cache.Set<string>("key", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that setting a value with an empty key throws an ArgumentException.
    /// </summary>
    [Fact]
    public void Set_WithEmptyKey_ThrowsArgumentException()
    {
        var act = () => _cache.Set(string.Empty, "value");
        act.Should().Throw<ArgumentException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // TTL / Expiry
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that a cache entry expires after the specified TTL and can no longer be retrieved.
    /// </summary>
    [Fact]
    public void TryGet_AfterExpiry_ReturnsFalse()
    {
        _cache.Set("expiry-key", "will-expire", TimeSpan.FromMilliseconds(50));

        Thread.Sleep(100);

        var found = _cache.TryGet<string>("expiry-key", out _);
        found.Should().BeFalse("entry should have expired");
    }

    /// <summary>
    /// Tests that Contains returns false for an entry that has expired.
    /// </summary>
    [Fact]
    public void Contains_AfterExpiry_ReturnsFalse()
    {
        _cache.Set("c-key", "value", TimeSpan.FromMilliseconds(50));
        Thread.Sleep(100);

        _cache.Contains("c-key").Should().BeFalse();
    }

    /// <summary>
    /// Tests that GetTimeToLive returns a positive duration for a fresh cache entry.
    /// </summary>
    [Fact]
    public void GetTimeToLive_ForFreshEntry_ReturnsPositiveDuration()
    {
        _cache.Set("ttl-key", "value", TimeSpan.FromSeconds(10));

        var ttl = _cache.GetTimeToLive("ttl-key");

        ttl.Should().NotBeNull();
        ttl!.Value.Should().BeGreaterThan(TimeSpan.Zero);
        ttl.Value.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Tests that GetTimeToLive returns null for a missing key.
    /// </summary>
    [Fact]
    public void GetTimeToLive_ForMissingKey_ReturnsNull()
    {
        _cache.GetTimeToLive("nonexistent").Should().BeNull();
    }

    /// <summary>
    /// Tests that GetTimeToLive returns null for an empty key.
    /// </summary>
    [Fact]
    public void GetTimeToLive_ForEmptyKey_ReturnsNull()
    {
        _cache.GetTimeToLive(string.Empty).Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Contains / Remove
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that Contains returns true when checking for an existing key.
    /// </summary>
    [Fact]
    public void Contains_WithExistingKey_ReturnsTrue()
    {
        _cache.Set("present", 42);
        _cache.Contains("present").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Contains returns false when checking for a missing key.
    /// </summary>
    [Fact]
    public void Contains_WithMissingKey_ReturnsFalse()
    {
        _cache.Contains("absent").Should().BeFalse();
    }

    /// <summary>
    /// Tests that Contains returns false when checking for an empty key.
    /// </summary>
    [Fact]
    public void Contains_WithEmptyKey_ReturnsFalse()
    {
        _cache.Contains(string.Empty).Should().BeFalse();
    }

    /// <summary>
    /// Tests that Remove successfully removes an existing key and returns true.
    /// </summary>
    [Fact]
    public void Remove_ExistingKey_ReturnsTrueAndKeyIsGone()
    {
        _cache.Set("remove-me", "val");

        var removed = _cache.Remove("remove-me");

        removed.Should().BeTrue();
        _cache.Contains("remove-me").Should().BeFalse();
    }

    /// <summary>
    /// Tests that Remove returns false when attempting to remove a non-existent key.
    /// </summary>
    [Fact]
    public void Remove_NonExistentKey_ReturnsFalse()
    {
        _cache.Remove("ghost").Should().BeFalse();
    }

    /// <summary>
    /// Tests that Remove returns false when attempting to remove with an empty key.
    /// </summary>
    [Fact]
    public void Remove_WithEmptyKey_ReturnsFalse()
    {
        _cache.Remove(string.Empty).Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Clear
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that Clear removes all entries from the cache.
    /// </summary>
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

    /// <summary>
    /// Tests that RemovePattern removes all entries matching the specified pattern.
    /// </summary>
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

    /// <summary>
    /// Tests that RemovePattern returns 0 and does not remove any entries when no matches are found.
    /// </summary>
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

    /// <summary>
    /// Tests that GetOrSet calls the factory function when the key is missing,
    /// caches the result, and returns the cached value on subsequent calls.
    /// </summary>
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

    /// <summary>
    /// Tests that GetOrSetAsync calls the async factory function when the key is missing,
    /// caches the result, and returns the cached value on subsequent calls.
    /// </summary>
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

    /// <summary>
    /// Tests that SetExpiration updates the expiration time for an existing key
    /// and the entry eventually expires based on the new TTL.
    /// </summary>
    [Fact]
    public void SetExpiration_ForExistingKey_UpdatesExpiryAndEntryEventuallyExpires()
    {
        _cache.Set("exp-key", "value", TimeSpan.FromSeconds(60));

        var result = _cache.SetExpiration("exp-key", TimeSpan.FromMilliseconds(50));
        result.Should().BeTrue();

        Thread.Sleep(100);
        _cache.Contains("exp-key").Should().BeFalse("expiration was shortened to 50 ms");
    }

    /// <summary>
    /// Tests that SetExpiration returns false when attempting to update expiration for a missing key.
    /// </summary>
    [Fact]
    public void SetExpiration_ForMissingKey_ReturnsFalse()
    {
        _cache.SetExpiration("nonexistent", TimeSpan.FromSeconds(10)).Should().BeFalse();
    }

    /// <summary>
    /// Tests that SetExpiration returns false when attempting to update expiration with an empty key.
    /// </summary>
    [Fact]
    public void SetExpiration_WithEmptyKey_ReturnsFalse()
    {
        _cache.SetExpiration(string.Empty, TimeSpan.FromSeconds(10)).Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetStatistics
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that GetStatistics returns the correct counts after multiple cache operations.
    /// </summary>
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

    /// <summary>
    /// Tests that GetStatistics returns zero counts for an empty cache.
    /// </summary>
    [Fact]
    public void GetStatistics_OnEmptyCache_ReturnsZeroEntries()
    {
        var stats = _cache.GetStatistics();
        stats.EntryCount.Should().Be(0);
        stats.TotalHits.Should().Be(0);
    }
}
