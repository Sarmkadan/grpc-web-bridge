#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Services;
using GrpcWebBridge.Events;

namespace GrpcWebBridge.BackgroundWorkers;

/// <summary>
/// Background worker that monitors health of registered gRPC services.
/// Performs periodic health checks and updates service status.
/// Publishes health events for monitoring and alerting.
/// </summary>
public class HealthCheckWorker : BackgroundService
{
    private readonly ILogger<HealthCheckWorker> _logger;
    private readonly ServiceRegistry _serviceRegistry;
    private readonly EventBus _eventBus;
    private readonly HealthCheckOptions _options;
    private int _totalHealthChecksRun = 0;
    private int _healthyServicesCount = 0;
    private int _unhealthyServicesCount = 0;

    public HealthCheckWorker(
        ILogger<HealthCheckWorker> logger,
        ServiceRegistry serviceRegistry,
        EventBus eventBus,
        HealthCheckOptions? options = null)
    {
        _logger = logger;
        _serviceRegistry = serviceRegistry;
        _eventBus = eventBus;
        _options = options ?? new HealthCheckOptions();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Health check worker started with interval {IntervalSeconds}s", _options.CheckIntervalSeconds);

        // Initial delay to allow services to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(_options.InitialDelaySeconds), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthCheckAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(_options.CheckIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
            }
        }

        _logger.LogInformation("Health check worker stopped. Total checks: {Count}", _totalHealthChecksRun);
    }

    /// <summary>
    /// Performs health check cycle for all registered services.
    /// </summary>
    private async Task PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        _totalHealthChecksRun++;
        var startTime = DateTime.UtcNow;
        _healthyServicesCount = 0;
        _unhealthyServicesCount = 0;

        try
        {
            var services = _serviceRegistry.ListServices().ToList();

            if (services.Count == 0)
            {
                _logger.LogDebug("No services registered for health check");
                return;
            }

            var checkTasks = services.Select(service =>
                PerformServiceHealthCheckAsync(service, cancellationToken)).ToList();

            await Task.WhenAll(checkTasks);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Health check completed: Services={Total}, Healthy={Healthy}, Unhealthy={Unhealthy}, Duration={DurationMs}ms",
                services.Count, _healthyServicesCount, _unhealthyServicesCount, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in health check cycle");
        }
    }

    /// <summary>
    /// Performs health check for a single service.
    /// </summary>
    private async Task PerformServiceHealthCheckAsync(
        GrpcWebBridge.Domain.Models.GrpcService service,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check service connectivity
            var isHealthy = await CheckServiceConnectivityAsync(service, cancellationToken);

            if (isHealthy)
            {
                _healthyServicesCount++;
                if (service.Status != ServiceStatus.Serving)
                {
                    service.Status = ServiceStatus.Serving;
                    _logger.LogInformation("Service recovered: ServiceId={ServiceId}, Name={Name}",
                        service.Id, service.Name);
                }
            }
            else
            {
                _unhealthyServicesCount++;
                if (service.Status == ServiceStatus.Serving)
                {
                    service.Status = ServiceStatus.NotServing;
                    _logger.LogWarning("Service degraded: ServiceId={ServiceId}, Name={Name}",
                        service.Id, service.Name);

                    // Publish health event
                    await PublishServiceHealthEventAsync(service, false);
                }
            }

            service.UpdatedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for service: ServiceId={ServiceId}", service.Id);
            service.Status = ServiceStatus.NotServing;
            _unhealthyServicesCount++;
        }
    }

    /// <summary>
    /// Checks if a service is reachable and responding.
    /// </summary>
    private async Task<bool> CheckServiceConnectivityAsync(
        GrpcWebBridge.Domain.Models.GrpcService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = $"{service.Endpoint}:{service.Port}";
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(_options.CheckTimeoutMs);

                // Attempt to create a connection without making a full request
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync($"http://{endpoint}/health", cts.Token);
                    return response.IsSuccessStatusCode;
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Health check timeout for service: ServiceId={ServiceId}", service.Id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check connection failed for service: ServiceId={ServiceId}", service.Id);
            return false;
        }
    }

    /// <summary>
    /// Publishes service health event for monitoring.
    /// </summary>
    private async Task PublishServiceHealthEventAsync(
        GrpcWebBridge.Domain.Models.GrpcService service,
        bool isHealthy)
    {
        try
        {
            var @event = new ServiceHealthChangedEvent
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                IsHealthy = isHealthy,
                Timestamp = DateTime.UtcNow,
                Source = "HealthCheckWorker"
            };

            await _eventBus.PublishAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish health event for service: ServiceId={ServiceId}", service.Id);
        }
    }

    /// <summary>
    /// Gets health check statistics.
    /// </summary>
    public object GetStatistics()
    {
        return new
        {
            totalHealthChecksRun = _totalHealthChecksRun,
            lastHealthyCount = _healthyServicesCount,
            lastUnhealthyCount = _unhealthyServicesCount,
            checkInterval = _options.CheckIntervalSeconds,
            checkTimeout = _options.CheckTimeoutMs,
            initialDelay = _options.InitialDelaySeconds
        };
    }
}

/// <summary>
/// Configuration options for health check worker.
/// </summary>
public sealed class HealthCheckOptions
{
    public int CheckIntervalSeconds { get; set; } = 30;
    public int CheckTimeoutMs { get; set; } = 5000;
    public int InitialDelaySeconds { get; set; } = 10;
}

/// <summary>
/// Event fired when a service health status changes.
/// </summary>
public class ServiceHealthChangedEvent : EventBase
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public DateTime Timestamp { get; set; }
}
