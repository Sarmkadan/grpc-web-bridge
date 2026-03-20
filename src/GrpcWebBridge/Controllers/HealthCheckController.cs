#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Services;
using GrpcWebBridge.Formatters;
using System.Runtime.InteropServices;

namespace GrpcWebBridge.Controllers;

/// <summary>
/// Detailed health check and diagnostics endpoint.
/// Provides comprehensive system and service health information.
/// Used by load balancers, monitoring tools, and orchestration platforms.
/// </summary>
[ApiController]
[Route("api/health")]
[Produces("application/json")]
public class HealthCheckController : ControllerBase
{
    private readonly ServiceRegistry _serviceRegistry;
    private readonly StreamingService _streamingService;
    private readonly ILogger<HealthCheckController> _logger;
    private static DateTime _startupTime = DateTime.UtcNow;

    public HealthCheckController(
        ServiceRegistry serviceRegistry,
        StreamingService streamingService,
        ILogger<HealthCheckController> logger)
    {
        _serviceRegistry = serviceRegistry;
        _streamingService = streamingService;
        _logger = logger;
    }

    /// <summary>
    /// Gets overall health status - simple endpoint for load balancers.
    /// Returns 200 if healthy, 503 if degraded.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult GetHealthStatus()
    {
        try
        {
            var services = _serviceRegistry.ListServices().ToList();
            var healthyCount = services.Count(s => s.Status == ServiceStatus.Serving);
            var totalCount = services.Count;

            var isHealthy = totalCount == 0 || healthyCount >= (totalCount * 0.8); // 80% threshold

            var response = ResponseFormatter.FormatHealthCheckResponse(
                healthy: isHealthy,
                status: isHealthy ? "healthy" : "degraded",
                metrics: new Dictionary<string, object>
                {
                    { "totalServices", totalCount },
                    { "healthyServices", healthyCount },
                    { "unhealthyServices", totalCount - healthyCount },
                    { "healthPercentage", totalCount > 0 ? Math.Round((healthyCount / (double)totalCount) * 100, 2) : 100 }
                }
            );

            if (!isHealthy)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health status");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                ResponseFormatter.FormatError("Health Check Failed", "Unable to determine system health"));
        }
    }

    /// <summary>
    /// Gets detailed diagnostic information about the system.
    /// Includes service statuses, resource usage, and configuration.
    /// </summary>
    [HttpGet("detailed")]
    public IActionResult GetDetailedDiagnostics()
    {
        try
        {
            var uptime = DateTime.UtcNow - _startupTime;
            var services = _serviceRegistry.ListServices();
            var process = System.Diagnostics.Process.GetCurrentProcess();

            var diagnostics = new
            {
                timestamp = DateTime.UtcNow,
                uptime = new
                {
                    days = uptime.Days,
                    hours = uptime.Hours,
                    minutes = uptime.Minutes,
                    seconds = uptime.Seconds,
                    totalSeconds = uptime.TotalSeconds
                },
                system = new
                {
                    processorCount = Environment.ProcessorCount,
                    osVersion = Environment.OSVersion.VersionString,
                    processorArchitecture = RuntimeInformation.ProcessArchitecture,
                    totalMemoryBytes = GC.GetTotalMemory(false),
                    workingSetMb = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2)
                },
                services = new
                {
                    total = services.Count(),
                    healthy = services.Count(s => s.Status == ServiceStatus.Serving),
                    unhealthy = services.Count(s => s.Status != ServiceStatus.Serving),
                    details = services.Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Status,
                        s.Endpoint,
                        s.Port,
                        methodCount = s.Methods.Count,
                        s.CreatedAt,
                        s.UpdatedAt
                    }).ToList()
                },
                streaming = new
                {
                    activeStreams = _streamingService.ActiveStreamCount,
                    maxStreams = 10000,
                    utilizationPercent = Math.Round((_streamingService.ActiveStreamCount / 10000.0) * 100, 2)
                },
                gcMetrics = new
                {
                    totalMemoryBytes = GC.GetTotalMemory(false),
                    gen0Collections = GC.CollectionCount(0),
                    gen1Collections = GC.CollectionCount(1),
                    gen2Collections = GC.CollectionCount(2)
                }
            };

            return Ok(ResponseFormatter.FormatSuccess(diagnostics, "Detailed system diagnostics"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving detailed diagnostics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ResponseFormatter.FormatError("Diagnostics Failed", "Unable to retrieve system diagnostics"));
        }
    }

    /// <summary>
    /// Gets health status of individual services.
    /// </summary>
    [HttpGet("services")]
    public IActionResult GetServiceHealthStatus()
    {
        try
        {
            var services = _serviceRegistry.ListServices();
            var serviceHealth = services.Select(s => new
            {
                s.Id,
                s.Name,
                status = s.Status,
                endpoint = s.Endpoint,
                port = s.Port,
                isHealthy = s.Status == ServiceStatus.Serving,
                lastUpdated = s.UpdatedAt,
                methodCount = s.Methods.Count
            }).ToList();

            var summary = new
            {
                totalServices = services.Count(),
                healthyServices = serviceHealth.Count(s => (bool)s.isHealthy),
                unhealthyServices = serviceHealth.Count(s => !(bool)s.isHealthy)
            };

            return Ok(ResponseFormatter.FormatSuccess(
                new { summary, services = serviceHealth },
                "Service health status"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving service health");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ResponseFormatter.FormatError("Service Health Check Failed"));
        }
    }

    /// <summary>
    /// Gets resource usage metrics.
    /// </summary>
    [HttpGet("resources")]
    public IActionResult GetResourceMetrics()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);

            var resources = new
            {
                memory = new
                {
                    totalMb = Math.Round(totalMemory / (1024.0 * 1024.0), 2),
                    workingSetMb = Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2),
                    virtualMemoryMb = Math.Round(process.VirtualMemorySize64 / (1024.0 * 1024.0), 2),
                    managedHeapMb = Math.Round(totalMemory / (1024.0 * 1024.0), 2)
                },
                processor = new
                {
                    count = Environment.ProcessorCount,
                    cpuUsagePercent = GetCpuUsage(),
                    threadCount = process.Threads.Count
                },
                gc = new
                {
                    gen0Collections = GC.CollectionCount(0),
                    gen1Collections = GC.CollectionCount(1),
                    gen2Collections = GC.CollectionCount(2)
                }
            };

            return Ok(ResponseFormatter.FormatSuccess(resources, "Resource metrics"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving resource metrics");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ResponseFormatter.FormatError("Resource Metrics Failed"));
        }
    }

    /// <summary>
    /// Gets readiness status - indicates if service is ready to handle requests.
    /// </summary>
    [HttpGet("ready")]
    public IActionResult GetReadinessStatus()
    {
        try
        {
            var services = _serviceRegistry.ListServices().ToList();
            var isReady = services.Count > 0 && services.Any(s => s.Status == ServiceStatus.Serving);

            var status = new
            {
                ready = isReady,
                reason = isReady
                    ? "Service is ready to handle requests"
                    : "Service is not ready - no healthy services available",
                servicesAvailable = services.Count
            };

            return isReady
                ? Ok(status)
                : StatusCode(StatusCodes.Status503ServiceUnavailable, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving readiness status");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { ready = false, reason = "Unable to determine readiness" });
        }
    }

    /// <summary>
    /// Gets liveness status - indicates if service is alive.
    /// </summary>
    [HttpGet("alive")]
    public IActionResult GetLivenessStatus()
    {
        return Ok(new
        {
            alive = true,
            timestamp = DateTime.UtcNow,
            uptime = (DateTime.UtcNow - _startupTime).TotalSeconds
        });
    }

    private static double GetCpuUsage()
    {
        try
        {
            return Math.Round(
                System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds, 2);
        }
        catch
        {
            return 0;
        }
    }
}
