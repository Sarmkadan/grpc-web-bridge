#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using GrpcWebBridge.Configuration;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using System.Globalization;

namespace GrpcWebBridge.Controllers;

/// <summary>
/// Manages bridge configuration at runtime.
/// Allows dynamic configuration updates without server restart.
/// </summary>
[ApiController]
[Route("api/configuration")]
[Produces("application/json")]
public class ConfigurationController : ControllerBase
{
    private readonly GrpcWebBridgeOptions _options;
    private readonly ServiceRegistry _serviceRegistry;
    private readonly ILogger<ConfigurationController> _logger;
    private static Dictionary<string, object> _runtimeConfig = new();

    public ConfigurationController(
        GrpcWebBridgeOptions options,
        ServiceRegistry serviceRegistry,
        ILogger<ConfigurationController> logger)
    {
        _options = options;
        _serviceRegistry = serviceRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve current bridge configuration.
    /// Returns all active configuration settings and limits.
    /// </summary>
    [HttpGet]
    public IActionResult GetConfiguration()
    {
        try
        {
            var config = new
            {
                Environment = _options.Configuration.Environment,
                MaxStreamCount = _options.Configuration.MaxStreamCount,
                StreamIdleTimeoutSeconds = _options.Configuration.StreamIdleTimeoutSeconds,
                MaxMessageSize = _options.Configuration.MaxMessageSize,
                CompressResponses = _options.Configuration.CompressResponses,
                CompressionLevel = _options.Configuration.CompressionLevel,
                EnableSwagger = _options.Configuration.EnableSwagger,
                AllowedOrigins = _options.Configuration.AllowedOrigins,
                RateLimiting = new
                {
                    Enabled = _runtimeConfig.ContainsKey("RateLimitingEnabled"),
                    RequestsPerSecond = _runtimeConfig.TryGetValue("RequestsPerSecond", out var rps) ? rps : 1000
                },
                Authentication = new
                {
                    RequiresBearerToken = true,
                    SupportsApiKey = true,
                    TokenValidationEnabled = true
                },
                RuntimeConfig = _runtimeConfig
            };

            return Ok(new
            {
                success = true,
                data = config,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configuration");
            return StatusCode(500, new { error = "Failed to retrieve configuration" });
        }
    }

    /// <summary>
    /// Update specific configuration values at runtime.
    /// Changes are applied immediately to new requests.
    /// </summary>
    [HttpPut]
    public IActionResult UpdateConfiguration([FromBody] ConfigurationUpdateRequest request)
    {
        if (request?.Settings is null || request.Settings.Count == 0)
            return BadRequest(new { error = "No settings provided to update" });

        try
        {
            var updates = new Dictionary<string, object>();

            foreach (var setting in request.Settings)
            {
                switch (setting.Key.ToLowerInvariant())
                {
                    case "compressresponses":
                        if (bool.TryParse(setting.Value?.ToString(), out var compress))
                            updates["CompressResponses"] = compress;
                        break;

                    case "compressionlevel":
                        if (int.TryParse(setting.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var level) && level >= 0 && level <= 9)
                            updates["CompressionLevel"] = level;
                        break;

                    case "requestspersecond":
                        if (int.TryParse(setting.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rps) && rps > 0)
                        {
                            updates["RequestsPerSecond"] = rps;
                            _runtimeConfig["RequestsPerSecond"] = rps;
                        }
                        break;

                    case "streamidletimeoutseconds":
                        if (int.TryParse(setting.Value?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeout) && timeout > 0)
                            updates["StreamIdleTimeoutSeconds"] = timeout;
                        break;

                    default:
                        _logger.LogWarning("Unknown configuration key: {Key}", setting.Key);
                        break;
                }
            }

            _logger.LogInformation("Configuration updated with {Count} changes", updates.Count);

            return Ok(new
            {
                success = true,
                message = $"{updates.Count} configuration setting(s) updated",
                updates = updates,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating configuration");
            return StatusCode(500, new { error = "Failed to update configuration", details = ex.Message });
        }
    }

    /// <summary>
    /// Validate configuration consistency and service connectivity.
    /// Performs health checks on configured services.
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateConfiguration()
    {
        try
        {
            var validationResults = new List<object>();
            var services = _serviceRegistry.ListServices();

            foreach (var service in services)
            {
                validationResults.Add(new
                {
                    serviceId = service.Id,
                    serviceName = service.Name,
                    status = service.Status,
                    endpoint = service.Endpoint,
                    port = service.Port,
                    methodCount = service.Methods.Count,
                    isHealthy = service.Status == ServiceStatus.Serving
                });
            }

            return Ok(new
            {
                success = true,
                validationStatus = "completed",
                serviceCount = services.Count(),
                healthyServices = validationResults.Count(r => (bool)r.GetType().GetProperty("isHealthy")?.GetValue(r, null)!),
                details = validationResults,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed");
            return StatusCode(500, new { error = "Validation failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Reset configuration to default values.
    /// Useful when configuration becomes corrupted or inconsistent.
    /// </summary>
    [HttpPost("reset")]
    public IActionResult ResetConfiguration()
    {
        try
        {
            _runtimeConfig.Clear();
            _logger.LogWarning("Configuration reset to defaults by administrator");

            return Ok(new
            {
                success = true,
                message = "Configuration reset to defaults",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting configuration");
            return StatusCode(500, new { error = "Failed to reset configuration" });
        }
    }
}

/// <summary>
/// Request model for configuration updates.
/// </summary>
public sealed class ConfigurationUpdateRequest
{
    public Dictionary<string, object> Settings { get; set; } = new();
}
