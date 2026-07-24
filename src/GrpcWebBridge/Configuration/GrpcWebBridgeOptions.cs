#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Configuration;

/// <summary>
/// Configuration options for the gRPC-Web bridge
/// </summary>
public sealed class GrpcWebBridgeOptions
{
    public BridgeConfiguration Configuration { get; set; } = new();

    public GrpcWebBridgeOptions() { }

    public GrpcWebBridgeOptions(string environment, string? instanceName = null)
    {
        Configuration = new BridgeConfiguration(environment, instanceName);
    }

    /// <summary>
    /// Configures the bridge for development environment
    /// </summary>
    public GrpcWebBridgeOptions WithDevelopment()
    {
        Configuration.Environment = "Development";
        Configuration.EnableSwagger = true;
        Configuration.EnableLogging = true;
        Configuration.AllowedOrigins = ["*"];
        return this;
    }

    /// <summary>
    /// Configures the bridge for production environment
    /// </summary>
    public GrpcWebBridgeOptions WithProduction()
    {
        Configuration.Environment = "Production";
        Configuration.RequireAuthentication = true;
        Configuration.CompressResponses = true;
        Configuration.AllowedOrigins = [];
        return this;
    }

    /// <summary>
    /// Configures the bridge for testing environment
    /// </summary>
    public GrpcWebBridgeOptions WithTesting()
    {
        Configuration.Environment = "Testing";
        Configuration.EnableSwagger = true;
        Configuration.EnableMetrics = false;
        Configuration.CompressResponses = false;
        return this;
    }

    /// <summary>
    /// Sets the maximum stream count
    /// </summary>
    public GrpcWebBridgeOptions WithMaxStreamCount(int maxCount)
    {
        if (maxCount <= 0)
            throw new ArgumentException("Max stream count must be greater than 0", nameof(maxCount));

        Configuration.MaxStreamCount = maxCount;
        return this;
    }

    /// <summary>
    /// Sets the stream idle timeout
    /// </summary>
    public GrpcWebBridgeOptions WithStreamIdleTimeout(int secondsTimeout)
    {
        if (secondsTimeout <= 0)
            throw new ArgumentException("Timeout must be greater than 0", nameof(secondsTimeout));

        Configuration.StreamIdleTimeoutSeconds = secondsTimeout;
        return this;
    }

    /// <summary>
    /// Sets the maximum message size
    /// </summary>
    public GrpcWebBridgeOptions WithMaxMessageSize(int sizeBytes)
    {
        if (sizeBytes <= 0)
            throw new ArgumentException("Max message size must be greater than 0", nameof(sizeBytes));

        Configuration.MaxMessageSize = sizeBytes;
        return this;
    }

    /// <summary>
    /// Sets the default timeout for gRPC calls
    /// </summary>
    public GrpcWebBridgeOptions WithDefaultTimeout(int millisecondsTimeout)
    {
        if (millisecondsTimeout <= 0)
            throw new ArgumentException("Timeout must be greater than 0", nameof(millisecondsTimeout));

        Configuration.DefaultTimeoutMilliseconds = millisecondsTimeout;
        return this;
    }

    /// <summary>
    /// Enables or disables compression
    /// </summary>
    public GrpcWebBridgeOptions WithCompression(bool enable, int compressionLevel = 6)
    {
        if (compressionLevel < 0 || compressionLevel > 9)
            throw new ArgumentException("Compression level must be between 0 and 9", nameof(compressionLevel));

        Configuration.CompressResponses = enable;
        Configuration.CompressionLevel = compressionLevel;
        return this;
    }

    /// <summary>
    /// Enables or disables Swagger documentation
    /// </summary>
    public GrpcWebBridgeOptions WithSwagger(bool enable)
    {
        Configuration.EnableSwagger = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables logging
    /// </summary>
    public GrpcWebBridgeOptions WithLogging(bool enable)
    {
        Configuration.EnableLogging = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables metrics
    /// </summary>
    public GrpcWebBridgeOptions WithMetrics(bool enable)
    {
        Configuration.EnableMetrics = enable;
        return this;
    }

    /// <summary>
    /// Enables or disables CORS
    /// </summary>
    public GrpcWebBridgeOptions WithCors(bool enable)
    {
        Configuration.EnableCors = enable;
        return this;
    }

    /// <summary>
    /// Requires authentication for all requests
    /// </summary>
    public GrpcWebBridgeOptions WithRequiredAuthentication()
    {
        Configuration.RequireAuthentication = true;
        return this;
    }

    /// <summary>
    /// Requires authentication for detailed health checks
    /// </summary>
    public GrpcWebBridgeOptions RequireAuthenticationForDetailedHealth()
    {
        Configuration.RequireAuthenticationForDetailedHealth = true;
        return this;
    }

    /// <summary>
    /// Adds an allowed origin for CORS
    /// </summary>
    public GrpcWebBridgeOptions AddAllowedOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            throw new ArgumentException("Origin cannot be empty", nameof(origin));

        Configuration.AddAllowedOrigin(origin);
        return this;
    }

    /// <summary>
    /// Adds multiple allowed origins
    /// </summary>
    public GrpcWebBridgeOptions AddAllowedOrigins(params string[] origins)
    {
        if (origins is null || origins.Length == 0)
            throw new ArgumentException("Origins cannot be null or empty", nameof(origins));

        foreach (var origin in origins.Where(o => !string.IsNullOrWhiteSpace(o)))
            Configuration.AddAllowedOrigin(origin);

        return this;
    }

    /// <summary>
    /// Adds a custom HTTP header
    /// </summary>
    public GrpcWebBridgeOptions AddCustomHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Header key cannot be empty", nameof(key));

        Configuration.AddCustomHeader(key, value);
        return this;
    }

    /// <summary>
    /// Sets instance name for identification
    /// </summary>
    public GrpcWebBridgeOptions WithInstanceName(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new ArgumentException("Instance name cannot be empty", nameof(instanceName));

        Configuration.InstanceName = instanceName;
        return this;
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public void Validate()
    {
        Configuration.Validate();
    }
}
