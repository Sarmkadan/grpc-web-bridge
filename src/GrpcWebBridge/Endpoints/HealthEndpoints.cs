#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Services;
using GrpcWebBridge.BackgroundWorkers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrpcWebBridge.Endpoints;

/// <summary>
/// Endpoints for health monitoring and diagnostics using ASP.NET Core IHealthCheck infrastructure
/// </summary>
public static class HealthEndpoints
{
    private static DateTime _startupTime = DateTime.UtcNow;

    /// <summary>
    /// Gets the application startup time
    /// </summary>
    public static DateTime GetStartupTime()
    {
        return _startupTime;
    }

    /// <summary>
    /// Maps health-related endpoints to the application using ASP.NET Core health check infrastructure
    /// </summary>
    public static void MapHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Standard Kubernetes-style health check endpoints
        // /healthz - Liveness probe (should always return 200 unless app is completely dead)
        app.MapHealthChecks("/healthz", new global::Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        })
        .WithName("Liveness Health Check")
        .WithOpenApi();

        // /ready - Readiness probe (returns 503 when not ready to serve traffic)
        app.MapHealthChecks("/ready", new global::Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        })
        .WithName("Readiness Health Check")
        .WithOpenApi();

        // /health - Standard health endpoint (backward compatibility)
        app.MapHealthChecks("/health", new global::Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = HealthCheckResponseWriter.WriteHealthCheckResponse,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        })
        .WithName("Standard Health Check")
        .WithOpenApi();

        // Detailed health check endpoint - always requires authentication for security
        // This endpoint exposes internal service topology, versions, and failure details
        // that should not be publicly accessible
        app.MapGet("/health/detailed", async (
            ServiceRegistry registry,
            StreamingService streaming) =>
        {
            var uptime = DateTime.UtcNow - _startupTime;

            var response = new DetailedHealthResponse
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                uptime = uptime.ToString("c"),
                uptime_seconds = (int)uptime.TotalSeconds,
                services = new ServiceHealthSummary
                {
                    registered_count = registry.RegisteredServiceCount,
                    health_status = GetOverallServiceHealth(registry),
                    services = registry.ListServices().Select(s => new ServiceHealthItem
                    {
                        id = s.Id,
                        name = s.Name,
                        full_name = s.FullName,
                        endpoint = s.Endpoint,
                        port = s.Port,
                        status = s.Status.ToString(),
                        health_status = registry.GetHealthStatus(s.FullName).ToString(),
                        method_count = s.Methods.Count,
                        created_at = s.CreatedAt,
                        updated_at = s.UpdatedAt ?? s.CreatedAt
                    }).ToList()
                },
                workers = new WorkerStatusSummary
                {
                    streaming_service = new StreamingWorkerStatus
                    {
                        active_stream_count = streaming.ActiveStreamCount,
                        max_stream_count = Constants.Streaming.MaxStreamCount,
                        stream_capacity = $"{streaming.ActiveStreamCount}/{Constants.Streaming.MaxStreamCount}",
                        status = streaming.ActiveStreamCount > 0 ? "active" : "idle"
                    }
                },
                system = new SystemStatus
                {
                    environment = app.Environment.EnvironmentName,
                    application_name = app.Environment.ApplicationName,
                    version = "1.0.0",
                    timestamp = DateTime.UtcNow
                }
            };

            return Results.Ok(response);
        })
        .RequireAuthorization()
        .WithName("Authenticated Detailed Health Check")
        .Produces<DetailedHealthResponse>(200, "application/json")
        .WithOpenApi();

        // Registry snapshot endpoint
        app.MapGet("/health/registry", (ServiceRegistry registry) =>
        {
            var snapshot = registry.GetRegistrySnapshot();

            var response = new RegistryHealthResponse
            {
                total_service_count = snapshot.TotalServiceCount,
                registered_services = snapshot.ServiceRegistrationTimestamps.Count,
                service_registration_timestamps = snapshot.ServiceRegistrationTimestamps
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString("o")),
                timestamp = DateTime.UtcNow
            };

            return Results.Ok(response);
        })
        .WithName("Registry Snapshot")
        .Produces<RegistryHealthResponse>(200, "application/json")
        .WithOpenApi();
    }

    private static string GetOverallServiceHealth(ServiceRegistry registry)
    {
        var services = registry.ListServices().ToList();

        if (services.Count == 0)
            return "no_services";

        var healthyCount = services.Count(s => s.Status == ServiceStatus.Serving);
        var unhealthyCount = services.Count(s => s.Status == ServiceStatus.NotServing);

        if (unhealthyCount == 0 && healthyCount > 0)
            return "healthy";

        if (unhealthyCount > 0 && healthyCount == 0)
            return "unhealthy";

        return "degraded";
    }

    /// <summary>
    /// Detailed health response model
    /// </summary>
    public sealed class DetailedHealthResponse
    {
        /// <summary>
        /// Gets or sets the overall health status
        /// </summary>
        public string? status { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the response was generated
        /// </summary>
        public DateTime timestamp { get; set; }

        /// <summary>
        /// Gets or sets the application uptime as a formatted string
        /// </summary>
        public string? uptime { get; set; }

        /// <summary>
        /// Gets or sets the application uptime in seconds
        /// </summary>
        public int uptime_seconds { get; set; }

        /// <summary>
        /// Gets or sets the service health summary
        /// </summary>
        public ServiceHealthSummary? services { get; set; }

        /// <summary>
        /// Gets or sets the worker status summary
        /// </summary>
        public WorkerStatusSummary? workers { get; set; }

        /// <summary>
        /// Gets or sets the system status information
        /// </summary>
        public SystemStatus? system { get; set; }
    }

    /// <summary>
    /// Service health summary
    /// </summary>
    public sealed class ServiceHealthSummary
    {
        /// <summary>
        /// Gets or sets the total number of registered services
        /// </summary>
        public int registered_count { get; set; }

        /// <summary>
        /// Gets or sets the overall health status of services
        /// </summary>
        public string? health_status { get; set; }

        /// <summary>
        /// Gets or sets the list of individual service health items
        /// </summary>
        public List<ServiceHealthItem>? services { get; set; }
    }

    /// <summary>
    /// Individual service health item
    /// </summary>
    public sealed class ServiceHealthItem
    {
        /// <summary>
        /// Gets or sets the service identifier
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// Gets or sets the service name
        /// </summary>
        public string? name { get; set; }

        /// <summary>
        /// Gets or sets the full service name (package.service)
        /// </summary>
        public string? full_name { get; set; }

        /// <summary>
        /// Gets or sets the service endpoint
        /// </summary>
        public string? endpoint { get; set; }

        /// <summary>
        /// Gets or sets the service port
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// Gets or sets the service status
        /// </summary>
        public string? status { get; set; }

        /// <summary>
        /// Gets or sets the service health status
        /// </summary>
        public string? health_status { get; set; }

        /// <summary>
        /// Gets or sets the number of methods in the service
        /// </summary>
        public int method_count { get; set; }

        /// <summary>
        /// Gets or sets the service creation timestamp
        /// </summary>
        public DateTime created_at { get; set; }

        /// <summary>
        /// Gets or sets the service last update timestamp
        /// </summary>
        public DateTime updated_at { get; set; }
    }

    /// <summary>
    /// Worker status summary
    /// </summary>
    public sealed class WorkerStatusSummary
    {
        /// <summary>
        /// Gets or sets the streaming service worker status
        /// </summary>
        public StreamingWorkerStatus? streaming_service { get; set; }
    }

    /// <summary>
    /// Streaming service worker status
    /// </summary>
    public sealed class StreamingWorkerStatus
    {
        /// <summary>
        /// Gets or sets the active stream count
        /// </summary>
        public int active_stream_count { get; set; }

        /// <summary>
        /// Gets or sets the maximum stream count
        /// </summary>
        public int max_stream_count { get; set; }

        /// <summary>
        /// Gets or sets the stream capacity as a formatted string
        /// </summary>
        public string? stream_capacity { get; set; }

        /// <summary>
        /// Gets or sets the worker status
        /// </summary>
        public string? status { get; set; }
    }

    /// <summary>
    /// System status information
    /// </summary>
    public sealed class SystemStatus
    {
        /// <summary>
        /// Gets or sets the environment name
        /// </summary>
        public string? environment { get; set; }

        /// <summary>
        /// Gets or sets the application name
        /// </summary>
        public string? application_name { get; set; }

        /// <summary>
        /// Gets or sets the application version
        /// </summary>
        public string? version { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the status was generated
        /// </summary>
        public DateTime timestamp { get; set; }
    }

    /// <summary>
    /// Registry health response model
    /// </summary>
    public sealed class RegistryHealthResponse
    {
        /// <summary>
        /// Gets or sets the total service count
        /// </summary>
        public int total_service_count { get; set; }

        /// <summary>
        /// Gets or sets the registered services count
        /// </summary>
        public int registered_services { get; set; }

        /// <summary>
        /// Gets or sets the service registration timestamps
        /// </summary>
        public Dictionary<string, string>? service_registration_timestamps { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the response was generated
        /// </summary>
        public DateTime timestamp { get; set; }
    }
}