#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace GrpcWebBridge.Caching;

/// <summary>
/// In-memory cache manager with TTL support and statistics.
/// Provides fast local caching for frequently accessed data.
/// Supports expiration policies and automatic cleanup.
/// </summary>
public class CacheManager : IDisposable
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache;
    private readonly Timer _cleanupTimer;
    private readonly ILogger<CacheManager> _logger;
    private readonly CacheManagerOptions _options;

    public CacheManager(ILogger<CacheManager> logger, CacheManagerOptions? options = null)
    {
        _cache = new ConcurrentDictionary<string, CacheEntry>();
        _logger = logger;
        _options = options ?? new CacheManagerOptions();

        // Start periodic cleanup of expired entries
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    /// <summary>
    /// Stores a value in cache with default TTL.
    /// </summary>
    public void Set<T>(string key, T value)
    {
        Set(key, value, _options.DefaultTtl);
    }

    /// <summary>
    /// Stores a value in cache with custom TTL.
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan ttl)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (value is null)
            throw new ArgumentNullException(nameof(value));

        var entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(ttl),
            CreatedAt = DateTime.UtcNow,
            HitCount = 0
        };

        _cache.AddOrUpdate(key, entry, (_, _) => entry);

        _logger.LogDebug("Cache entry set: Key={Key}, TTL={Ttl}ms", key, ttl.TotalMilliseconds);
    }

    /// <summary>
    /// Retrieves a value from cache if it exists and hasn't expired.
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        value = default;

        if (string.IsNullOrEmpty(key))
            return false;

        if (!_cache.TryGetValue(key, out var entry))
        {
            _logger.LogDebug("Cache miss: Key={Key}", key);
            return false;
        }

        // Check if entry has expired
        if (DateTime.UtcNow > entry.ExpiresAt)
        {
            _cache.TryRemove(key, out _);
            _logger.LogDebug("Cache entry expired: Key={Key}", key);
            return false;
        }

        // Update hit count
        entry.HitCount++;
        entry.LastAccessedAt = DateTime.UtcNow;

        value = (T?)entry.Value;
        _logger.LogDebug("Cache hit: Key={Key}, HitCount={HitCount}", key, entry.HitCount);
        return true;
    }

    /// <summary>
    /// Retrieves a value from cache or executes a factory function to populate it.
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        if (TryGet(key, out T? cachedValue) && cachedValue is not null)
            return cachedValue;

        var value = await factory();
        Set(key, value, ttl ?? _options.DefaultTtl);
        return value;
    }

    /// <summary>
    /// Synchronous version of GetOrSetAsync.
    /// </summary>
    public T GetOrSet<T>(string key, Func<T> factory, TimeSpan? ttl = null)
    {
        if (TryGet(key, out T? cachedValue) && cachedValue is not null)
            return cachedValue;

        var value = factory();
        Set(key, value, ttl ?? _options.DefaultTtl);
        return value;
    }

    /// <summary>
    /// Removes a specific entry from cache.
    /// </summary>
    public bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        var removed = _cache.TryRemove(key, out _);
        if (removed)
            _logger.LogDebug("Cache entry removed: Key={Key}", key);

        return removed;
    }

    /// <summary>
    /// Removes all entries matching a pattern.
    /// </summary>
    public int RemovePattern(string pattern)
    {
        var regex = new System.Text.RegularExpressions.Regex(
            System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*"),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogDebug("Cache pattern removed: Pattern={Pattern}, RemovedCount={Count}", pattern, keysToRemove.Count);
        return keysToRemove.Count;
    }

    /// <summary>
    /// Clears all entries from cache.
    /// </summary>
    public void Clear()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogInformation("Cache cleared: RemovedCount={Count}", count);
    }

    /// <summary>
    /// Gets cache statistics and metrics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var entries = _cache.Values.ToList();
        var totalHits = entries.Sum(e => e.HitCount);

        return new CacheStatistics
        {
            EntryCount = _cache.Count,
            TotalHits = totalHits,
            AverageHitsPerEntry = entries.Count > 0 ? totalHits / (double)entries.Count : 0,
            OldestEntry = entries.MinBy(e => e.CreatedAt),
            MostAccessedEntry = entries.MaxBy(e => e.HitCount),
            AverageEntrySize = EstimateAverageSizeInBytes(entries)
        };
    }

    /// <summary>
    /// Checks if a key exists in cache and hasn't expired.
    /// </summary>
    public bool Contains(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        if (!_cache.TryGetValue(key, out var entry))
            return false;

        if (DateTime.UtcNow > entry.ExpiresAt)
        {
            _cache.TryRemove(key, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sets a key to expire after specified time from now.
    /// </summary>
    public bool SetExpiration(string key, TimeSpan expiresIn)
    {
        // Fix: handle null or empty key edge case
        if (string.IsNullOrEmpty(key))
            return false;

        if (!_cache.TryGetValue(key, out var entry))
            return false;

        entry.ExpiresAt = DateTime.UtcNow.Add(expiresIn);
        return true;
    }

    /// <summary>
    /// Gets the remaining TTL for a cache entry.
    /// Returns null if entry doesn't exist.
    /// </summary>
    public TimeSpan? GetTimeToLive(string key)
    {
        // Fix: handle null or empty key edge case
        if (string.IsNullOrEmpty(key))
            return null;

        if (!_cache.TryGetValue(key, out var entry))
            return null;

        var remaining = entry.ExpiresAt - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Periodically removes expired entries.
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => now > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogDebug("Cache cleanup completed: RemovedExpiredEntries={Count}", expiredKeys.Count);
        }
    }

    /// <summary>
    /// Estimates average size of cache entries.
    /// </summary>
    private static long EstimateAverageSizeInBytes(List<CacheEntry> entries)
    {
        if (entries.Count == 0)
            return 0;

        long totalSize = 0;
        foreach (var entry in entries)
        {
            if (entry.Value is not null)
            {
                totalSize += System.Runtime.InteropServices.Marshal.SizeOf(entry.Value);
            }
        }

        return totalSize / entries.Count;
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _cache?.Clear();
    }
}

/// <summary>
/// Single cache entry with metadata.
/// </summary>
public sealed class CacheEntry
{
    public object? Value { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public long HitCount { get; set; }
}

/// <summary>
/// Cache statistics.
/// </summary>
public sealed class CacheStatistics
{
    public int EntryCount { get; set; }
    public long TotalHits { get; set; }
    public double AverageHitsPerEntry { get; set; }
    public CacheEntry? OldestEntry { get; set; }
    public CacheEntry? MostAccessedEntry { get; set; }
    public long AverageEntrySize { get; set; }
}

/// <summary>
/// Configuration options for cache manager.
/// </summary>
public sealed class CacheManagerOptions
{
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxEntries { get; set; } = 10000;
    public bool EnableStatistics { get; set; } = true;
}
