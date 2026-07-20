#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Services;

namespace GrpcWebBridge.Endpoints;

/// <summary>
/// Endpoints for health monitoring and diagnostics
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
    /// Maps health-related endpoints to the application
    /// </summary>
    public static void MapHealthEndpoints(this WebApplication app)
    {
        if (app is null)
            throw new ArgumentNullException(nameof(app));

        // Detailed health check endpoint
        app.MapGet("/health/detailed", async (
            ServiceRegistry registry,
            StreamingService streaming,
            TimeProvider timeProvider) =>
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
        .WithName("Detailed Health Check")
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
        public string? status { get; set; }
        public DateTime timestamp { get; set; }
        public string? uptime { get; set; }
        public int uptime_seconds { get; set; }
        public ServiceHealthSummary? services { get; set; }
        public WorkerStatusSummary? workers { get; set; }
        public SystemStatus? system { get; set; }
    }

    /// <summary>
    /// Service health summary
    /// </summary>
    public sealed class ServiceHealthSummary
    {
        public int registered_count { get; set; }
        public string? health_status { get; set; }
        public List<ServiceHealthItem>? services { get; set; }
    }

    /// <summary>
    /// Individual service health item
    /// </summary>
    public sealed class ServiceHealthItem
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? full_name { get; set; }
        public string? endpoint { get; set; }
        public int port { get; set; }
        public string? status { get; set; }
        public string? health_status { get; set; }
        public int method_count { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    /// <summary>
    /// Worker status summary
    /// </summary>
    public sealed class WorkerStatusSummary
    {
        public StreamingWorkerStatus? streaming_service { get; set; }
    }

    /// <summary>
    /// Streaming service worker status
    /// </summary>
    public sealed class StreamingWorkerStatus
    {
        public int active_stream_count { get; set; }
        public int max_stream_count { get; set; }
        public string? stream_capacity { get; set; }
        public string? status { get; set; }
    }

    /// <summary>
    /// System status information
    /// </summary>
    public sealed class SystemStatus
    {
        public string? environment { get; set; }
        public string? application_name { get; set; }
        public string? version { get; set; }
        public DateTime timestamp { get; set; }
    }

    /// <summary>
    /// Registry health response model
    /// </summary>
    public sealed class RegistryHealthResponse
    {
        public int total_service_count { get; set; }
        public int registered_services { get; set; }
        public Dictionary<string, string>? service_registration_timestamps { get; set; }
        public DateTime timestamp { get; set; }
    }
}