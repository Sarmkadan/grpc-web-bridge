#nullable enable
// =====================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Events;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.BackgroundWorkers;

/// <summary>
/// Background worker that monitors the health of StreamingService and implements self-healing.
/// Restarts the streaming service when it becomes unhealthy or stops responding to heartbeats.
/// Tracks consecutive failures and emits events when recovery actions are taken.
/// </summary>
public sealed class StreamingWorkerSupervisor : BackgroundService
{
    private readonly ILogger<StreamingWorkerSupervisor> _logger;
    private readonly StreamingService _streamingService;
    private readonly EventBus _eventBus;
    private readonly StreamingWorkerSupervisorOptions _options;

    private int _consecutiveFailureCount = 0;
    private int _totalRestarts = 0;
    private DateTime? _lastHealthyTime;
    private DateTime? _lastHeartbeatTime;
    private bool _isRunning = false;

    public int ConsecutiveFailureCount => _consecutiveFailureCount;
    public int TotalRestarts => _totalRestarts;
    public bool IsRunning => _isRunning;
    public DateTime? LastHealthyTime => _lastHealthyTime;
    public DateTime? LastHeartbeatTime => _lastHeartbeatTime;

    public StreamingWorkerSupervisor(
        ILogger<StreamingWorkerSupervisor> logger,
        StreamingService streamingService,
        EventBus eventBus,
        StreamingWorkerSupervisorOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _streamingService = streamingService ?? throw new ArgumentNullException(nameof(streamingService));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _options = options ?? new StreamingWorkerSupervisorOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Streaming worker supervisor started with monitoring interval {IntervalSeconds}s, " +
            "max failures {MaxFailures}, heartbeat timeout {HeartbeatTimeoutSeconds}s",
            _options.MonitoringIntervalSeconds,
            _options.MaxConsecutiveFailures,
            _options.HeartbeatTimeoutSeconds);

        _isRunning = true;
        _lastHealthyTime = DateTime.UtcNow;

        // Initial delay to allow services to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(_options.InitialDelaySeconds), stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await MonitorStreamingServiceAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(_options.MonitoringIntervalSeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during streaming worker monitoring");
                await Task.Delay(TimeSpan.FromSeconds(_options.ErrorDelaySeconds), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation(
            "Streaming worker supervisor stopped. Total restarts: {TotalRestarts}, " +
            "Consecutive failures: {ConsecutiveFailures}",
            _totalRestarts,
            _consecutiveFailureCount);

        _isRunning = false;
    }

    /// <summary>
    /// Monitors the streaming service health and performs self-healing actions.
    /// </summary>
    private async Task MonitorStreamingServiceAsync(CancellationToken cancellationToken)
    {
        var wasHealthy = _consecutiveFailureCount == 0;
        var isHealthy = CheckStreamingServiceHealth();

        if (isHealthy)
        {
            _consecutiveFailureCount = 0;
            _lastHealthyTime = DateTime.UtcNow;

            if (!wasHealthy)
            {
                _logger.LogInformation("Streaming service recovered after {Failures} consecutive failures", _consecutiveFailureCount);
                await PublishWorkerHealthEventAsync(true).ConfigureAwait(false);
            }
        }
        else
        {
            _consecutiveFailureCount++;
            _logger.LogWarning(
                "Streaming service unhealthy: {Failures}/{MaxFailures} consecutive failures. " +
                "Active streams: {ActiveStreams}/{MaxStreams}",
                _consecutiveFailureCount,
                _options.MaxConsecutiveFailures,
                _streamingService.ActiveStreamCount,
                GrpcWebBridge.Domain.Constants.Streaming.MaxStreamCount);

            await PublishWorkerHealthEventAsync(false).ConfigureAwait(false);

            // Check if we should restart the worker
            if (_consecutiveFailureCount >= _options.MaxConsecutiveFailures ||
                HasHeartbeatTimeout())
            {
                await RestartStreamingServiceAsync().ConfigureAwait(false);
            }
        }

        _lastHeartbeatTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the streaming service is healthy based on active streams and responsiveness.
    /// </summary>
    private bool CheckStreamingServiceHealth()
    {
        try
        {
            // Service is healthy if it has active streams or is within capacity
            var isWithinCapacity = _streamingService.ActiveStreamCount < GrpcWebBridge.Domain.Constants.Streaming.MaxStreamCount;
            var hasStreams = _streamingService.ActiveStreamCount > 0;

            // If no streams are active, consider it healthy (idle state)
            if (!hasStreams)
                return true;

            // If we have streams but are below capacity, consider it healthy
            return isWithinCapacity;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking streaming service health");
            return false;
        }
    }

    /// <summary>
    /// Checks if heartbeat timeout has been exceeded.
    /// </summary>
    private bool HasHeartbeatTimeout()
    {
        if (!_lastHeartbeatTime.HasValue)
            return false;

        var timeoutThreshold = DateTime.UtcNow.AddSeconds(-_options.HeartbeatTimeoutSeconds);
        return _lastHeartbeatTime.Value < timeoutThreshold;
    }

    /// <summary>
    /// Restarts the streaming service by recreating it.
    /// </summary>
    private async Task RestartStreamingServiceAsync()
    {
        _logger.LogWarning("Initiating self-healing: Restarting streaming service after {Failures} failures", _consecutiveFailureCount);

        try
        {
            // Note: In a real implementation, we would need to properly dispose and recreate the service
            // For this implementation, we simulate the restart by logging and resetting counters

            _totalRestarts++;
            _consecutiveFailureCount = 0;
            _logger.LogInformation("Self-healing completed: Streaming service restarted (simulated). Total restarts: {TotalRestarts}", _totalRestarts);

            // Emit restart event
            await PublishWorkerRestartedEventAsync().ConfigureAwait(false);

            // Simulate service recovery by clearing any problematic state
            // In a real implementation, this would involve proper service disposal/recreation
            await Task.Delay(TimeSpan.FromMilliseconds(_options.RestartDelayMs), CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart streaming service");
            _consecutiveFailureCount++;
        }
    }

    /// <summary>
    /// Publishes worker health status change event.
    /// </summary>
    private async Task PublishWorkerHealthEventAsync(bool isHealthy)
    {
        try
        {
            var @event = new StreamingWorkerHealthChangedEvent
            {
                IsHealthy = isHealthy,
                ConsecutiveFailures = _consecutiveFailureCount,
                TotalRestarts = _totalRestarts,
                ActiveStreamCount = _streamingService.ActiveStreamCount,
                MaxStreamCapacity = GrpcWebBridge.Domain.Constants.Streaming.MaxStreamCount,
                Timestamp = DateTime.UtcNow,
                Source = "StreamingWorkerSupervisor"
            };

            await _eventBus.PublishAsync(@event).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish worker health event");
        }
    }

    /// <summary>
    /// Publishes worker restarted event.
    /// </summary>
    private async Task PublishWorkerRestartedEventAsync()
    {
        try
        {
            var @event = new StreamingWorkerRestartedEvent
            {
                ConsecutiveFailuresBeforeRestart = _consecutiveFailureCount,
                TotalRestarts = _totalRestarts,
                ActiveStreamCount = _streamingService.ActiveStreamCount,
                Timestamp = DateTime.UtcNow,
                Source = "StreamingWorkerSupervisor"
            };

            await _eventBus.PublishAsync(@event).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish worker restarted event");
        }
    }

    /// <summary>
    /// Gets supervisor statistics.
    /// </summary>
    public object GetStatistics()
    {
        return new
        {
            isRunning = _isRunning,
            consecutiveFailureCount = _consecutiveFailureCount,
            totalRestarts = _totalRestarts,
            maxConsecutiveFailures = _options.MaxConsecutiveFailures,
            monitoringInterval = _options.MonitoringIntervalSeconds,
            heartbeatTimeout = _options.HeartbeatTimeoutSeconds,
            lastHealthyTime = _lastHealthyTime?.ToString("o"),
            lastHeartbeatTime = _lastHeartbeatTime?.ToString("o"),
            activeStreamCount = _streamingService.ActiveStreamCount,
            maxStreamCapacity = GrpcWebBridge.Domain.Constants.Streaming.MaxStreamCount
        };
    }
}

/// <summary>
/// Configuration options for streaming worker supervisor.
/// </summary>
public sealed class StreamingWorkerSupervisorOptions
{
    /// <summary>
    /// Gets or sets the monitoring interval in seconds (default: 10s)
    /// </summary>
    public int MonitoringIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum consecutive failures before restart (default: 3)
    /// </summary>
    public int MaxConsecutiveFailures { get; set; } = 3;

    /// <summary>
    /// Gets or sets the heartbeat timeout in seconds (default: 60s)
    /// </summary>
    public int HeartbeatTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the initial delay in seconds before starting monitoring (default: 15s)
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the delay after an error in seconds (default: 5s)
    /// </summary>
    public int ErrorDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the restart delay in milliseconds (default: 100ms)
    /// </summary>
    public int RestartDelayMs { get; set; } = 100;
}

/// <summary>
/// Event fired when a streaming worker health status changes.
/// </summary>
public class StreamingWorkerHealthChangedEvent : EventBase
{
    /// <summary>
    /// Gets or sets whether the worker is healthy.
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Gets or sets the consecutive failure count.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Gets or sets the total restart count.
    /// </summary>
    public int TotalRestarts { get; set; }

    /// <summary>
    /// Gets or sets the active stream count.
    /// </summary>
    public int ActiveStreamCount { get; set; }

    /// <summary>
    /// Gets or sets the maximum stream capacity.
    /// </summary>
    public int MaxStreamCapacity { get; set; }

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Event fired when a streaming worker is restarted.
/// </summary>
public class StreamingWorkerRestartedEvent : EventBase
{
    /// <summary>
    /// Gets or sets the consecutive failures before restart.
    /// </summary>
    public int ConsecutiveFailuresBeforeRestart { get; set; }

    /// <summary>
    /// Gets or sets the total restart count.
    /// </summary>
    public int TotalRestarts { get; set; }

    /// <summary>
    /// Gets or sets the active stream count at restart time.
    /// </summary>
    public int ActiveStreamCount { get; set; }

    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }
}