#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrpcWebBridge.Endpoints;

/// <summary>
/// Health check implementation that validates service registry health status
/// Implements IHealthCheck for integration with ASP.NET Core health check infrastructure
/// </summary>
public sealed class ServiceHealthCheck : IHealthCheck
{
    private readonly ServiceRegistry _serviceRegistry;
    private readonly ILogger<ServiceHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the ServiceHealthCheck class
    /// </summary>
    /// <param name="serviceRegistry">The service registry</param>
    /// <param name="logger">The logger</param>
    public ServiceHealthCheck(
        ServiceRegistry serviceRegistry,
        ILogger<ServiceHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceRegistry);
        ArgumentNullException.ThrowIfNull(logger);

        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Executes the health check
    /// </summary>
    /// <param name="context">The health check context</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A health check result</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var services = _serviceRegistry.ListServices().ToList();

            if (services.Count == 0)
            {
                _logger.LogInformation("Service health check: no services registered");
                return HealthCheckResult.Healthy("No services registered");
            }

            var unhealthyServices = services
                .Where(s => s.Status != ServiceStatus.Serving)
                .ToList();

            if (unhealthyServices.Count == 0)
            {
                var healthyCount = services.Count(s => s.Status == ServiceStatus.Serving);
                _logger.LogDebug("Service health check: {HealthyCount}/{TotalCount} services healthy",
                    healthyCount, services.Count);
                return HealthCheckResult.Healthy($"{healthyCount}/{services.Count} services healthy");
            }

            var degradedCount = services.Count(s => s.Status == ServiceStatus.Unknown);
            var notServingCount = unhealthyServices.Count;
            var servingCount = services.Count - degradedCount - notServingCount;

            if (servingCount > 0 && notServingCount > 0)
            {
                // Degraded state - some services are serving, some are not
                var serviceNames = unhealthyServices.Select(s => s.FullName).ToList();
                _logger.LogWarning("Service health check: degraded state - {ServingCount}/{TotalCount} healthy, {NotServingCount} not serving: {ServiceNames}",
                    servingCount, services.Count, notServingCount, string.Join(", ", serviceNames));
                return HealthCheckResult.Degraded(
                    $"Degraded: {servingCount}/{services.Count} services healthy, {notServingCount} not serving",
                    data: new Dictionary<string, object>
                    {
                        ["healthy_services"] = servingCount,
                        ["unhealthy_services"] = notServingCount,
                        ["degraded_services"] = degradedCount,
                        ["unhealthy_service_names"] = serviceNames
                    });
            }

            // All services are unhealthy
            var allUnhealthyNames = unhealthyServices.Select(s => s.FullName).ToList();
            _logger.LogError("Service health check: unhealthy - all {TotalCount} services not serving: {ServiceNames}",
                services.Count, string.Join(", ", allUnhealthyNames));
            return HealthCheckResult.Unhealthy(
                $"Unhealthy: {notServingCount}/{services.Count} services not serving",
                data: new Dictionary<string, object>
                {
                    ["unhealthy_services"] = notServingCount,
                    ["total_services"] = services.Count,
                    ["unhealthy_service_names"] = allUnhealthyNames
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service health check failed with exception");
            return HealthCheckResult.Unhealthy("Service health check failed", exception: ex);
        }
    }
}
