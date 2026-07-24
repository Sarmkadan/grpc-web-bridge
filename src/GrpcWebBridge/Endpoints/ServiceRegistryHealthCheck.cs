#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using GrpcWebBridge.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrpcWebBridge.Endpoints;

/// <summary>
/// Health check implementation that validates service registry functionality
/// Ensures the registry is operational and can track services
/// </summary>
public sealed class ServiceRegistryHealthCheck : IHealthCheck
{
    private readonly ServiceRegistry _serviceRegistry;
    private readonly ILogger<ServiceRegistryHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the ServiceRegistryHealthCheck class
    /// </summary>
    /// <param name="serviceRegistry">The service registry</param>
    /// <param name="logger">The logger</param>
    public ServiceRegistryHealthCheck(
        ServiceRegistry serviceRegistry,
        ILogger<ServiceRegistryHealthCheck> logger)
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
            // Test basic registry operations
            var initialCount = _serviceRegistry.RegisteredServiceCount;

            // Test registration
            var testService = new GrpcWebBridge.Domain.Models.GrpcService(
                "HealthCheckTestService",
                "HealthCheck.Test",
                "localhost",
                50051);

            var testMethod = new GrpcWebBridge.Domain.Models.GrpcMethod(
                "TestMethod",
                "HealthCheck.Test.HealthCheckTestService",
                GrpcWebBridge.Domain.MethodType.Unary,
                "string",
                "string");
            testService.AddMethod(testMethod);

            _serviceRegistry.RegisterService(testService);
            var afterRegistrationCount = _serviceRegistry.RegisteredServiceCount;

            // Test retrieval
            var retrievedService = _serviceRegistry.GetService(testService.FullName);
            var exists = _serviceRegistry.ServiceExists(testService.FullName);

            // Clean up
            _serviceRegistry.UnregisterService(testService.FullName);

            // Validate operations
            if (afterRegistrationCount != initialCount + 1)
            {
                _logger.LogError("Service registry health check: failed to register test service");
                return HealthCheckResult.Unhealthy("Failed to register test service");
            }

            if (retrievedService?.FullName != testService.FullName)
            {
                _logger.LogError("Service registry health check: failed to retrieve test service");
                return HealthCheckResult.Unhealthy("Failed to retrieve test service");
            }

            if (!exists)
            {
                _logger.LogError("Service registry health check: failed to confirm service existence");
                return HealthCheckResult.Unhealthy("Failed to confirm service existence");
            }

            _logger.LogDebug("Service registry health check: registry is operational ({ServiceCount} services)",
                _serviceRegistry.RegisteredServiceCount);
            return HealthCheckResult.Healthy($"Registry operational with {_serviceRegistry.RegisteredServiceCount} services");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service registry health check failed with exception");
            return HealthCheckResult.Unhealthy("Service registry health check failed", exception: ex);
        }
    }
}
