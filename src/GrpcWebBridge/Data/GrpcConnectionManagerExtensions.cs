#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using Grpc.Net.Client;
using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Data;

/// <summary>
/// Extension methods for <see cref="GrpcConnectionManager"/> providing additional functionality
/// for managing and monitoring gRPC connections
/// </summary>
public static class GrpcConnectionManagerExtensions
{
    /// <summary>
    /// Gets the connection metrics for all active connections
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Collection of metrics for all active connections</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static IEnumerable<ConnectionMetrics> GetAllMetrics(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values.ToList();
        }
    }

    /// <summary>
    /// Gets the total number of active connections
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Count of active connections</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static int GetActiveConnectionCount(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        return manager.ActiveConnectionCount;
    }

    /// <summary>
    /// Gets the total request count across all connections
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Total request count</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static long GetTotalRequestCount(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values.Sum(m => m.RequestCount);
        }
    }

    /// <summary>
    /// Gets the total bytes sent across all connections
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Total bytes sent</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static long GetTotalBytesSent(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values.Sum(m => m.BytesSent);
        }
    }

    /// <summary>
    /// Gets the total bytes received across all connections
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Total bytes received</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static long GetTotalBytesReceived(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values.Sum(m => m.BytesReceived);
        }
    }

    /// <summary>
    /// Gets the average connection duration across all active connections
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Average connection duration</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static TimeSpan GetAverageConnectionDuration(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            var metrics = manager.GetMetricsDictionary().Values.ToList();
            return metrics.Count == 0
                ? TimeSpan.Zero
                : TimeSpan.FromTicks((long)metrics.Average(m => m.GetConnectionDuration().Ticks));
        }
    }

    /// <summary>
    /// Gets the most recently used connection's metrics
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Metrics for the most recently used connection, or null if no connections</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static ConnectionMetrics? GetMostRecentlyUsed(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values
                .OrderByDescending(m => m.LastUsedAt)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets the oldest connection's metrics
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Metrics for the oldest connection, or null if no connections</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static ConnectionMetrics? GetOldestConnection(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values
                .OrderBy(m => m.CreatedAt)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets the connection with the highest request count
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Metrics for the connection with highest request count, or null if no connections</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static ConnectionMetrics? GetMostActiveConnection(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values
                .OrderByDescending(m => m.RequestCount)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets the connection with the highest data throughput (bytes sent + received)
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Metrics for the connection with highest throughput, or null if no connections</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static ConnectionMetrics? GetHighestThroughputConnection(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values
                .OrderByDescending(m => m.BytesSent + m.BytesReceived)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets all connection addresses
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Collection of connection addresses</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static IEnumerable<string> GetAllConnectionAddresses(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values
                .Select(m => m.Address)
                .Where(a => !string.IsNullOrEmpty(a))!;
        }
    }

    /// <summary>
    /// Gets all service names
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Collection of service names</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static IEnumerable<string> GetAllServiceNames(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values
                .Select(m => m.ServiceName)
                .Where(s => !string.IsNullOrEmpty(s))!;
        }
    }

    /// <summary>
    /// Gets a dictionary of all metrics by service name
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>Dictionary mapping service names to their metrics</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static IReadOnlyDictionary<string, ConnectionMetrics> GetMetricsByService(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    /// <summary>
    /// Gets the connection duration for a specific service
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <param name="serviceFullName">The service name to look up</param>
    /// <returns>Connection duration for the specified service</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="serviceFullName"/> is null or whitespace</exception>
    public static TimeSpan GetConnectionDuration(this GrpcConnectionManager manager, string serviceFullName)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceFullName);

        lock (manager.GetLock())
        {
            var metrics = manager.GetMetricsDictionary().Values
                .FirstOrDefault(m => m.ServiceName?.Equals(serviceFullName, StringComparison.Ordinal) == true);
            return metrics?.GetConnectionDuration() ?? TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Gets the request count for a specific service
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <param name="serviceFullName">The service name to look up</param>
    /// <returns>Request count for the specified service</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="serviceFullName"/> is null or whitespace</exception>
    public static int GetRequestCount(this GrpcConnectionManager manager, string serviceFullName)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceFullName);

        lock (manager.GetLock())
        {
            var metrics = manager.GetMetricsDictionary().Values
                .FirstOrDefault(m => m.ServiceName?.Equals(serviceFullName, StringComparison.Ordinal) == true);
            return metrics?.RequestCount ?? 0;
        }
    }

    /// <summary>
    /// Gets the bytes sent for a specific service
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <param name="serviceFullName">The service name to look up</param>
    /// <returns>Bytes sent for the specified service</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="serviceFullName"/> is null or whitespace</exception>
    public static long GetBytesSent(this GrpcConnectionManager manager, string serviceFullName)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceFullName);

        lock (manager.GetLock())
        {
            var metrics = manager.GetMetricsDictionary().Values
                .FirstOrDefault(m => m.ServiceName?.Equals(serviceFullName, StringComparison.Ordinal) == true);
            return metrics?.BytesSent ?? 0;
        }
    }

    /// <summary>
    /// Gets the bytes received for a specific service
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <param name="serviceFullName">The service name to look up</param>
    /// <returns>Bytes received for the specified service</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="serviceFullName"/> is null or whitespace</exception>
    public static long GetBytesReceived(this GrpcConnectionManager manager, string serviceFullName)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceFullName);

        lock (manager.GetLock())
        {
            var metrics = manager.GetMetricsDictionary().Values
                .FirstOrDefault(m => m.ServiceName?.Equals(serviceFullName, StringComparison.Ordinal) == true);
            return metrics?.BytesReceived ?? 0;
        }
    }

    /// <summary>
    /// Gets the last used timestamp for a specific service
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <param name="serviceFullName">The service name to look up</param>
    /// <returns>Last used timestamp for the specified service</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="serviceFullName"/> is null or whitespace</exception>
    public static DateTime GetLastUsedAt(this GrpcConnectionManager manager, string serviceFullName)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceFullName);

        lock (manager.GetLock())
        {
            var metrics = manager.GetMetricsDictionary().Values
                .FirstOrDefault(m => m.ServiceName?.Equals(serviceFullName, StringComparison.Ordinal) == true);
            return metrics?.LastUsedAt ?? DateTime.MinValue;
        }
    }

    /// <summary>
    /// Gets the creation timestamp for a specific service
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <param name="serviceFullName">The service name to look up</param>
    /// <returns>Creation timestamp for the specified service</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="serviceFullName"/> is null or whitespace</exception>
    public static DateTime GetCreatedAt(this GrpcConnectionManager manager, string serviceFullName)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceFullName);

        lock (manager.GetLock())
        {
            var metrics = manager.GetMetricsDictionary().Values
                .FirstOrDefault(m => m.ServiceName?.Equals(serviceFullName, StringComparison.Ordinal) == true);
            return metrics?.CreatedAt ?? DateTime.MinValue;
        }
    }

    /// <summary>
    /// Checks if a specific service is currently connected
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <param name="serviceFullName">The service name to check</param>
    /// <returns>True if the service is connected, false otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static bool IsServiceConnected(this GrpcConnectionManager manager, string serviceFullName)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (string.IsNullOrWhiteSpace(serviceFullName))
            return false;

        lock (manager.GetLock())
        {
            return manager.GetMetricsDictionary().Values
                .Any(m => m.ServiceName?.Equals(serviceFullName, StringComparison.Ordinal) == true);
        }
    }

    /// <summary>
    /// Gets the channel for a specific service if it exists
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <param name="serviceFullName">The service name to look up</param>
    /// <returns>The channel if found, null otherwise</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    public static GrpcChannel? GetChannel(this GrpcConnectionManager manager, string serviceFullName)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (string.IsNullOrWhiteSpace(serviceFullName))
            return null;

            return manager.GetChannel(serviceFullName);
    }

    // Helper method to access the private _metrics dictionary through reflection
    /// <summary>
    /// Gets the metrics dictionary from the connection manager using reflection
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>The metrics dictionary</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">Reflection failed to access the metrics dictionary</exception>
    private static Dictionary<string, ConnectionMetrics> GetMetricsDictionary(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var field = typeof(GrpcConnectionManager).GetField("_metrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (Dictionary<string, ConnectionMetrics>)field!.GetValue(manager)!
            ?? throw new InvalidOperationException("Failed to access metrics dictionary via reflection");
    }

    // Helper method to access the private _lock object
    /// <summary>
    /// Gets the synchronization lock from the connection manager using reflection
    /// </summary>
    /// <param name="manager">The connection manager instance</param>
    /// <returns>The synchronization lock object</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manager"/> is <see langword="null"/></exception>
    /// <exception cref="InvalidOperationException">Reflection failed to access the lock object</exception>
    private static object GetLock(this GrpcConnectionManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        var field = typeof(GrpcConnectionManager).GetField("_lock", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field!.GetValue(manager)!
            ?? throw new InvalidOperationException("Failed to access lock object via reflection");
    }
}