#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GrpcWebBridge.Endpoints;

/// <summary>
/// Health check implementation that validates system-level health
/// Checks basic system functionality like memory, disk, and process health
/// </summary>
public sealed class SystemHealthCheck : IHealthCheck
{
    private readonly ILogger<SystemHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the SystemHealthCheck class
    /// </summary>
    /// <param name="logger">The logger</param>
    public SystemHealthCheck(ILogger<SystemHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
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
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = process.WorkingSet64;
            var startTime = process.StartTime;
            var uptime = DateTime.UtcNow - startTime;

            // Basic system health checks
            var checks = new Dictionary<string, object>
            {
                ["process_id"] = process.Id,
                ["process_name"] = process.ProcessName,
                ["start_time"] = startTime.ToString("o"),
                ["uptime_seconds"] = uptime.TotalSeconds,
                ["total_memory_bytes"] = totalMemory,
                ["working_set_bytes"] = workingSet,
                ["thread_count"] = process.Threads.Count,
                ["processor_count"] = Environment.ProcessorCount,
                ["environment"] = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Unknown"
            };

            // Check for critical issues
            var issues = new List<string>();

            // Memory threshold check (512MB)
            const long memoryThresholdBytes = 536870912; // 512MB
            if (totalMemory > memoryThresholdBytes)
            {
                issues.Add("high_memory_usage");
            }

            // Process age check (must be running for at least 1 second)
            if (uptime.TotalSeconds < 1)
            {
                issues.Add("process_just_started");
            }

            if (issues.Count == 0)
            {
                _logger.LogDebug("System health check: system is healthy");
                return HealthCheckResult.Healthy("System is healthy");
            }

            var issueList = string.Join(", ", issues);
            _logger.LogWarning("System health check: potential issues detected - {Issues}", issueList);
            return HealthCheckResult.Degraded($"System has potential issues: {issueList}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System health check failed with exception");
            return HealthCheckResult.Unhealthy("System health check failed", ex);
        }
    }
}
