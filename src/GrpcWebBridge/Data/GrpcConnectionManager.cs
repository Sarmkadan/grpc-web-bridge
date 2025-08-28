#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Grpc.Core;
using Grpc.Net.Client;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Data;

/// <summary>
/// Manages gRPC connections to backend services
/// </summary>
public class GrpcConnectionManager : IAsyncDisposable
{
    private readonly ILogger<GrpcConnectionManager> _logger;
    private readonly Dictionary<string, GrpcChannel> _channels = [];
    private readonly Dictionary<string, ConnectionMetrics> _metrics = [];
    private readonly object _lock = new();
    private bool _disposed;

    public int ActiveConnectionCount
    {
        get
        {
            lock (_lock)
            {
                return _channels.Count;
            }
        }
    }

    public GrpcConnectionManager(ILogger<GrpcConnectionManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates or retrieves a connection to a gRPC service
    /// </summary>
    public GrpcChannel GetOrCreateChannel(GrpcService service)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        var connectionKey = GetConnectionKey(service);

        lock (_lock)
        {
            if (_channels.TryGetValue(connectionKey, out var existingChannel))
            {
                _logger.LogDebug("Using existing channel for {Service}", service.FullName);
                UpdateMetrics(connectionKey, isNewConnection: false);
                return existingChannel;
            }

            try
            {
                var address = BuildAddress(service);
                _logger.LogInformation("Creating new gRPC channel for {Service}: {Address}", service.FullName, address);

                var channelOptions = new GrpcChannelOptions
                {
                    MaxReceiveMessageSize = Constants.Grpc.MaxMessageSize,
                    MaxSendMessageSize = Constants.Grpc.MaxMessageSize,
                    DisposeHttpClient = false
                };

                var channel = GrpcChannel.ForAddress(address, channelOptions);
                _channels[connectionKey] = channel;

                _metrics[connectionKey] = new ConnectionMetrics
                {
                    ServiceName = service.FullName,
                    Address = address,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("gRPC channel created successfully for {Service}", service.FullName);

                return channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create gRPC channel for {Service}", service.FullName);
                throw new ServiceRegistrationException(service.Name, service.Endpoint, ex.Message);
            }
        }
    }

    /// <summary>
    /// Retrieves an existing channel without creating a new one
    /// </summary>
    public GrpcChannel? GetChannel(string serviceFullName)
    {
        if (string.IsNullOrWhiteSpace(serviceFullName))
            return null;

        lock (_lock)
        {
            var key = _channels.Keys.FirstOrDefault(k => k.EndsWith(serviceFullName));
            return key is not null ? _channels[key] : null;
        }
    }

    /// <summary>
    /// Closes a specific channel
    /// </summary>
    public async Task CloseChannelAsync(GrpcService service)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        var connectionKey = GetConnectionKey(service);

        lock (_lock)
        {
            if (_channels.Remove(connectionKey, out var channel))
            {
                _logger.LogInformation("Closing gRPC channel for {Service}", service.FullName);
                _metrics.Remove(connectionKey);
            }
        }
    }

    /// <summary>
    /// Closes all channels
    /// </summary>
    public async Task CloseAllChannelsAsync()
    {
        List<GrpcChannel> channelsToClose;
        lock (_lock)
        {
            channelsToClose = _channels.Values.ToList();
            _channels.Clear();
            _metrics.Clear();
        }

        foreach (var channel in channelsToClose)
        {
            try
            {
                await channel.ShutdownAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing channel");
            }
        }

        _logger.LogInformation("All gRPC channels closed");
    }

    /// <summary>
    /// Gets connection metrics
    /// </summary>
    public ConnectionMetrics? GetMetrics(string serviceFullName)
    {
        if (string.IsNullOrWhiteSpace(serviceFullName))
            return null;

        lock (_lock)
        {
            var key = _channels.Keys.FirstOrDefault(k => k.EndsWith(serviceFullName));
            return key is not null && _metrics.TryGetValue(key, out var metrics) ? metrics : null;
        }
    }

    /// <summary>
    /// Tests a connection to verify service availability
    /// </summary>
    public async Task<bool> TestConnectionAsync(GrpcService service)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        try
        {
            var channel = GetOrCreateChannel(service);
            var state = channel.State;
            _logger.LogInformation("Connection test for {Service}: {State}", service.FullName, state);

            return state == ConnectivityState.Ready || state == ConnectivityState.Idle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for {Service}", service.FullName);
            return false;
        }
    }

    /// <summary>
    /// Builds the full address for a service
    /// </summary>
    private static string BuildAddress(GrpcService service)
    {
        var scheme = service.UseTls ? "https" : "http";
        return $"{scheme}://{service.Endpoint}:{service.Port}";
    }

    /// <summary>
    /// Creates a unique connection key for a service
    /// </summary>
    private static string GetConnectionKey(GrpcService service)
    {
        return $"{service.Id}:{service.FullName}";
    }

    /// <summary>
    /// Updates connection metrics
    /// </summary>
    private void UpdateMetrics(string connectionKey, bool isNewConnection)
    {
        if (_metrics.TryGetValue(connectionKey, out var metrics))
        {
            metrics.LastUsedAt = DateTime.UtcNow;
            metrics.RequestCount++;

            if (isNewConnection)
                metrics.CreatedAt = DateTime.UtcNow;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await CloseAllChannelsAsync();
        _disposed = true;

        _logger.LogInformation("GrpcConnectionManager disposed");
    }
}

/// <summary>
/// Metrics for a gRPC connection
/// </summary>
public sealed class ConnectionMetrics
{
    public string? ServiceName { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
    public int RequestCount { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }

    public TimeSpan GetConnectionDuration() => DateTime.UtcNow - CreatedAt;
}
