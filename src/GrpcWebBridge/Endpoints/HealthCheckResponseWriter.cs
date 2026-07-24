#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace GrpcWebBridge.Endpoints;

/// <summary>
/// Provides response writing for health check endpoints
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Writes a health check response in a standardized format compatible with Kubernetes and other orchestration platforms
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <param name="healthReport">The health report</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task WriteHealthCheckResponse(
        HttpContext context,
        HealthReport healthReport)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(healthReport);

        var status = healthReport.Status.ToString().ToLowerInvariant();
        var totalDuration = healthReport.TotalDuration;

        var response = new
        {
            status,
            totalDuration = totalDuration.TotalSeconds,
            timestamp = DateTime.UtcNow,
            checks = healthReport.Entries.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    status = kvp.Value.Status.ToString().ToLowerInvariant(),
                    description = kvp.Value.Description,
                    duration = kvp.Value.Duration.TotalSeconds,
                    exception = kvp.Value.Exception?.Message
                })
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = healthReport.Status switch
        {
            HealthStatus.Healthy => StatusCodes.Status200OK,
            HealthStatus.Degraded => StatusCodes.Status200OK, // Degraded is still OK for Kubernetes
            HealthStatus.Unhealthy => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status503ServiceUnavailable
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, _jsonOptions));
    }
}
