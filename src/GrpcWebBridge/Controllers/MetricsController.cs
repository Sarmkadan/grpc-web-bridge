#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Services;
using System.Diagnostics;

namespace GrpcWebBridge.Controllers;

/// <summary>
/// Exposes performance metrics and system statistics.
/// Tracks request rates, response times, errors, and stream activity.
/// </summary>
[ApiController]
[Route("api/metrics")]
[Produces("application/json")]
public sealed class MetricsController : ControllerBase
{
    private readonly StreamingService _streamingService;
    private readonly ServiceRegistry _serviceRegistry;
    private readonly ILogger<MetricsController> _logger;
    internal static long _totalRequests = 0;
    internal static long _totalErrors = 0;
    private static Dictionary<string, long> _methodCallCounts = new();
    private static Dictionary<string, long> _methodErrorCounts = new();
    private static DateTime _startTime = DateTime.UtcNow;

    public MetricsController(
        StreamingService streamingService,
        ServiceRegistry serviceRegistry,
        ILogger<MetricsController> logger)
    {
        _streamingService = streamingService;
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Get comprehensive system metrics and statistics.
    /// Returns uptime, request counts, error rates, and resource usage.
    /// </summary>
    [HttpGet]
    public IActionResult GetMetrics()
    {
        try
        {
            var uptime = DateTime.UtcNow - _startTime;
            var successCount = _totalRequests - _totalErrors;
            var errorRate = _totalRequests > 0 ? (_totalErrors / (double)_totalRequests) * 100 : 0;
            var services = _serviceRegistry.ListServices();

            var metrics = new
            {
                systemMetrics = new
                {
                    uptime = new
                    {
                        days = uptime.Days,
                        hours = uptime.Hours,
                        minutes = uptime.Minutes,
                        seconds = uptime.Seconds,
                        totalSeconds = uptime.TotalSeconds
                    },
                    startTime = _startTime,
                    currentTime = DateTime.UtcNow,
                    processId = Environment.ProcessId
                },
                requestMetrics = new
                {
                    totalRequests = _totalRequests,
                    successfulRequests = successCount,
                    failedRequests = _totalErrors,
                    errorRate = Math.Round(errorRate, 2),
                    averageRequestsPerMinute = _totalRequests > 0 ? Math.Round(_totalRequests / uptime.TotalMinutes, 2) : 0
                },
                streamMetrics = new
                {
                    activeStreams = _streamingService.ActiveStreamCount,
                    maxStreams = 10000,
                    streamUtilization = Math.Round((_streamingService.ActiveStreamCount / 10000.0) * 100, 2)
                },
                serviceMetrics = new
                {
                    totalServices = services.Count(),
                    healthyServices = services.Count(s => s.Status == ServiceStatus.Serving),
                    totalMethods = services.Sum(s => s.Methods.Count)
                },
                resourceMetrics = new
                {
                    memoryUsageMb = Math.Round(GC.GetTotalMemory(false) / (1024.0 * 1024.0), 2),
                    processorCount = Environment.ProcessorCount,
                    workingSetMb = GetWorkingSetMb()
                }
            };

            return Ok(new
            {
                success = true,
                data = metrics,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving metrics");
            return StatusCode(500, new { error = "Failed to retrieve metrics" });
        }
    }

    /// <summary>
    /// Get detailed method invocation statistics.
    /// Shows call counts and error counts per method.
    /// </summary>
    [HttpGet("methods")]
    public IActionResult GetMethodMetrics()
    {
        try
        {
            var methodStats = _methodCallCounts.Select(kvp => new
            {
                method = kvp.Key,
                callCount = kvp.Value,
                errorCount = _methodErrorCounts.ContainsKey(kvp.Key) ? _methodErrorCounts[kvp.Key] : 0,
                errorRate = kvp.Value > 0 ? Math.Round(((_methodErrorCounts.ContainsKey(kvp.Key) ? _methodErrorCounts[kvp.Key] : 0) / (double)kvp.Value) * 100, 2) : 0
            })
            .OrderByDescending(x => x.callCount)
            .ToList();

            return Ok(new
            {
                success = true,
                methodCount = methodStats.Count,
                totalCalls = methodStats.Sum(m => m.callCount),
                totalErrors = methodStats.Sum(m => m.errorCount),
                methods = methodStats,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving method metrics");
            return StatusCode(500, new { error = "Failed to retrieve method metrics" });
        }
    }

    /// <summary>
    /// Get streaming service performance metrics.
    /// Includes active stream count, buffer statistics, and throughput.
    /// </summary>
    [HttpGet("streaming")]
    public IActionResult GetStreamingMetrics()
    {
        try
        {
            var streamIds = _streamingService.GetAllStreamIds();
            var streamStats = new List<object>();

            foreach (var streamId in streamIds.Take(100)) // Limit to 100 streams for response size
            {
                var stats = _streamingService.GetStreamStatistics(streamId);
                streamStats.Add(stats);
            }

            return Ok(new
            {
                success = true,
                activeStreamCount = _streamingService.ActiveStreamCount,
                displayedStreamCount = streamStats.Count,
                totalMessageCount = streamStats.Cast<dynamic>().Sum(s => (long)(s.messageCount ?? 0)),
                streams = streamStats,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving streaming metrics");
            return StatusCode(500, new { error = "Failed to retrieve streaming metrics" });
        }
    }

    /// <summary>
    /// Record a successful method invocation for metrics tracking.
    /// </summary>
    public static void RecordMethodCall(string methodName)
    {
        Interlocked.Increment(ref _totalRequests);

        lock (_methodCallCounts)
        {
            if (!_methodCallCounts.ContainsKey(methodName))
                _methodCallCounts[methodName] = 0;

            _methodCallCounts[methodName]++;
        }
    }

    /// <summary>
    /// Record a failed method invocation for error tracking.
    /// </summary>
    public static void RecordMethodError(string methodName)
    {
        Interlocked.Increment(ref _totalErrors);

        lock (_methodErrorCounts)
        {
            if (!_methodErrorCounts.ContainsKey(methodName))
                _methodErrorCounts[methodName] = 0;

            _methodErrorCounts[methodName]++;
        }
    }

    /// <summary>
    /// Reset all metrics to zero (useful for baseline testing).
    /// </summary>
    [HttpPost("reset")]
    public IActionResult ResetMetrics()
    {
        try
        {
            _totalRequests = 0;
            _totalErrors = 0;
            _methodCallCounts.Clear();
            _methodErrorCounts.Clear();
            _startTime = DateTime.UtcNow;

            _logger.LogWarning("Metrics have been reset by administrator");

            return Ok(new
            {
                success = true,
                message = "Metrics reset successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting metrics");
            return StatusCode(500, new { error = "Failed to reset metrics" });
        }
    }

    private static double GetWorkingSetMb()
    {
        using (var process = Process.GetCurrentProcess())
        {
            return Math.Round(process.WorkingSet64 / (1024.0 * 1024.0), 2);
        }
    }
}
