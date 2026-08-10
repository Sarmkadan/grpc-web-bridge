#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// Cache key generation and management utilities.
/// Provides helpers for consistent cache key formatting and pattern matching.
/// </summary>
public static class CacheUtility
{
    /// <summary>
    /// Generates a cache key from multiple components.
    /// Uses pipe separator for consistency.
    /// </summary>
    public static string GenerateKey(params object?[] components)
    {
        if (components == null)
            throw new ArgumentNullException(nameof(components));
        if (components.Length == 0)
            throw new ArgumentException("At least one component is required", nameof(components));

        var parts = components
            .Where(c => c is not null)
            .Select(c => SanitizeKeyComponent(c!.ToString() ?? string.Empty))
            .ToList();

        if (parts.Count == 0)
            throw new ArgumentException("No valid components provided", nameof(components));

        return string.Join("|", parts);
    }

    /// <summary>
    /// Generates a namespaced cache key.
    /// Useful for grouping related cache entries.
    /// </summary>
    public static string GenerateNamespacedKey(string @namespace, params object?[] components)
    {
        if (@namespace == null)
            throw new ArgumentNullException(nameof(@namespace));
        if (@namespace.Length == 0)
            throw new ArgumentException("Namespace cannot be null or empty", nameof(@namespace));

        var sanitized = SanitizeKeyComponent(@namespace);
        var key = GenerateKey(components);
        return $"{sanitized}:{key}";
    }

    /// <summary>
    /// Sanitizes a cache key component for safe storage.
    /// Removes or escapes special characters.
    /// </summary>
    public static string SanitizeKeyComponent(string component)
    {
        if (component == null)
            return string.Empty;
        if (component.Length == 0)
            throw new ArgumentException("Component cannot be null or empty", nameof(component));

        return System.Text.RegularExpressions.Regex.Replace(
            component,
            @"[^a-zA-Z0-9_\-.]",
            "_");
    }

    /// <summary>
    /// Creates a cache key pattern for prefix matching.
    /// </summary>
    public static string CreatePatternKey(string prefix)
    {
        if (prefix == null)
            throw new ArgumentNullException(nameof(prefix));
        if (prefix.Length == 0)
            throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));

        return $"{SanitizeKeyComponent(prefix)}*";
    }

    /// <summary>
    /// Checks if a key matches a pattern.
    /// Supports wildcard patterns.
    /// </summary>
    public static bool MatchesPattern(string key, string pattern)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));
        if (key.Length == 0)
            throw new ArgumentException("Key cannot be empty", nameof(key));
        if (pattern.Length == 0)
            throw new ArgumentException("Pattern cannot be empty", nameof(pattern));

        if (pattern == "*")
            return true;

        var regexPattern = System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".");

        return System.Text.RegularExpressions.Regex.IsMatch(key, $"^{regexPattern}$");
    }

    /// <summary>
    /// Calculates a hash of the key for use with hash-based data structures.
    /// </summary>
    public static int GetKeyHash(string key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (key.Length == 0)
            throw new ArgumentException("Key cannot be empty", nameof(key));

        return key.GetHashCode();
    }

    /// <summary>
    /// Estimates the memory size of a cache key.
    /// </summary>
    public static long EstimateKeySize(string key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (key.Length == 0)
            throw new ArgumentException("Key cannot be empty", nameof(key));

        // Each char in .NET string is 2 bytes + string overhead
        return (key.Length * 2) + 26; // 26 bytes for string object overhead
    }

    /// <summary>
    /// Parses a composite cache key back into components.
    /// </summary>
    public static string[] ParseKey(string key, string separator = "|")
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (key.Length == 0)
            throw new ArgumentException("Key cannot be empty", nameof(key));

        return key.Split(separator);
    }

    /// <summary>
    /// Creates a cache key for a service method invocation.
    /// </summary>
    public static string GenerateMethodCacheKey(string serviceId, string methodName, object? parameters = null)
    {
        if (serviceId == null)
            throw new ArgumentNullException(nameof(serviceId));
        if (serviceId.Length == 0)
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));
        if (methodName == null)
            throw new ArgumentNullException(nameof(methodName));
        if (methodName.Length == 0)
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));

        if (string.IsNullOrEmpty(methodName))
            throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));

        var baseKey = GenerateKey(serviceId, methodName);

        if (parameters is not null)
        {
            var paramsHash = ComputeParametersHash(parameters);
            return $"{baseKey}|{paramsHash}";
        }

        return baseKey;
    }

    /// <summary>
    /// Creates a cache key for stream data.
    /// </summary>
    public static string GenerateStreamCacheKey(string streamId)
    {
        if (streamId == null)
            throw new ArgumentNullException(nameof(streamId));
        if (streamId.Length == 0)
            throw new ArgumentException("Stream ID cannot be null or empty", nameof(streamId));

        return GenerateNamespacedKey("stream", streamId);
    }

    /// <summary>
    /// Creates a cache key for service metadata.
    /// </summary>
    public static string GenerateServiceCacheKey(string serviceId)
    {
        if (serviceId == null)
            throw new ArgumentNullException(nameof(serviceId));
        if (serviceId.Length == 0)
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));

        return GenerateNamespacedKey("service", serviceId);
    }

    /// <summary>
    /// Creates a cache key for authentication context.
    /// </summary>
    public static string GenerateAuthCacheKey(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));

        return GenerateNamespacedKey("auth", userId);
    }

    /// <summary>
    /// Computes a hash of parameters for cache key generation.
    /// </summary>
    private static string ComputeParametersHash(object parameters)
    {
        try
        {
            var json = JsonUtility.Serialize(parameters);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                return Convert.ToHexString(hash)[..8]; // Use first 8 chars
            }
        }
        catch
        {
            return parameters.GetHashCode().ToString("x8");
        }
    }

    /// <summary>
    /// Validates that a key is in proper format.
    /// </summary>
    public static bool IsValidKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        if (key.Length > 512)
            return false;

        // Check for allowed characters
        return System.Text.RegularExpressions.Regex.IsMatch(key, @"^[a-zA-Z0-9_\-.:*|\s]+$");
    }

    /// <summary>
    /// Generates a debug-friendly representation of a cache key.
    /// </summary>
    public static string FormatKeyForDebug(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "<empty>";

        if (key.Length > 100)
            return key[..97] + "...";

        return key;
    }
}

/// <summary>
/// Cache statistics for analysis and optimization.
/// </summary>
public sealed class CacheStatistics
{
    public long TotalKeysGenerated { get; set; }
    public long TotalCacheHits { get; set; }
    public long TotalCacheMisses { get; set; }
    public double HitRate => TotalCacheHits + TotalCacheMisses > 0
        ? (TotalCacheHits / (double)(TotalCacheHits + TotalCacheMisses)) * 100
        : 0;
    public long TotalMemoryUsed { get; set; }
    public Dictionary<string, long> KeysByNamespace { get; set; } = new();
}
