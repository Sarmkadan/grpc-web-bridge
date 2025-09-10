#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Central configuration for the gRPC-Web bridge instance. Controls streaming limits,
/// CORS policy, compression, authentication, and per-service defaults.
/// Use <see cref="GrpcWebBridge.Configuration.GrpcWebBridgeOptions"/> for fluent configuration.
/// </summary>
public sealed class BridgeConfiguration
{
    /// <summary>Auto-generated unique identifier for this bridge instance.</summary>
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Optional human-readable name for the bridge instance.</summary>
    public string? InstanceName { get; set; }
    /// <summary>Deployment environment (Development, Production, Testing).</summary>
    public string Environment { get; set; } = "Production";
    /// <summary>Enable structured request/response logging.</summary>
    public bool EnableLogging { get; set; } = true;
    /// <summary>Enable Swagger/OpenAPI documentation endpoint.</summary>
    public bool EnableSwagger { get; set; } = true;
    /// <summary>Enable Prometheus-compatible metrics collection.</summary>
    public bool EnableMetrics { get; set; } = true;
    /// <summary>Enable CORS headers on HTTP responses.</summary>
    public bool EnableCors { get; set; } = true;
    /// <summary>Require authentication for all bridge requests.</summary>
    public bool RequireAuthentication { get; set; }
    /// <summary>Maximum number of concurrent bidirectional streams.</summary>
    public int MaxStreamCount { get; set; } = Constants.Streaming.MaxStreamCount;
    /// <summary>Seconds of inactivity before a stream is automatically closed.</summary>
    public int StreamIdleTimeoutSeconds { get; set; } = Constants.Streaming.StreamIdleTimeoutSeconds;
    /// <summary>Interval in seconds between stream keepalive heartbeats.</summary>
    public int StreamHeartbeatIntervalSeconds { get; set; } = Constants.Streaming.StreamHeartbeatIntervalSeconds;
    /// <summary>Maximum gRPC message size in bytes.</summary>
    public int MaxMessageSize { get; set; } = Constants.Grpc.MaxMessageSize;
    /// <summary>Default timeout in milliseconds for unary gRPC calls.</summary>
    public int DefaultTimeoutMilliseconds { get; set; } = Constants.Grpc.DefaultTimeout;
    /// <summary>Whether to compress HTTP responses (gzip).</summary>
    public bool CompressResponses { get; set; } = true;
    /// <summary>Gzip compression level (0-9). Higher values yield smaller output but cost more CPU.</summary>
    public int CompressionLevel { get; set; } = 6;
    /// <summary>CORS allowed origins. Use ["*"] to allow all origins.</summary>
    public List<string> AllowedOrigins { get; set; } = ["*"];
    /// <summary>HTTP methods allowed in CORS preflight responses.</summary>
    public List<string> AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];
    /// <summary>Custom HTTP headers injected into all bridge responses.</summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = [];
    /// <summary>Per-service default configuration overrides, keyed by service name.</summary>
    public Dictionary<string, object> ServiceDefaults { get; set; } = [];
    /// <summary>UTC timestamp when this configuration was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>UTC timestamp of the last configuration change.</summary>
    public DateTime? UpdatedAt { get; set; }

    public BridgeConfiguration() { }

    public BridgeConfiguration(string environment, string? instanceName = null)
    {
        Environment = ValidateEnvironment(environment);
        InstanceName = instanceName;
    }

    public void SetServiceDefault(string serviceName, object defaultValue)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

        ServiceDefaults[serviceName] = defaultValue;
        UpdatedAt = DateTime.UtcNow;
    }

    public object? GetServiceDefault(string serviceName)
    {
        return ServiceDefaults.TryGetValue(serviceName, out var value) ? value : null;
    }

    public void AddAllowedOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            throw new ArgumentException("Origin cannot be empty", nameof(origin));

        if (!AllowedOrigins.Contains(origin))
            AllowedOrigins.Add(origin);

        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveAllowedOrigin(string origin)
    {
        if (AllowedOrigins.Remove(origin))
            UpdatedAt = DateTime.UtcNow;
    }

    public void AddCustomHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Header key cannot be empty", nameof(key));

        CustomHeaders[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public string? GetCustomHeader(string key)
    {
        return CustomHeaders.TryGetValue(key, out var value) ? value : null;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Environment))
            throw new ArgumentException("Environment cannot be empty", nameof(Environment));

        if (MaxStreamCount <= 0)
            throw new ArgumentException("Max stream count must be greater than 0", nameof(MaxStreamCount));

        if (StreamIdleTimeoutSeconds <= 0)
            throw new ArgumentException("Stream idle timeout must be greater than 0", nameof(StreamIdleTimeoutSeconds));

        if (MaxMessageSize <= 0)
            throw new ArgumentException("Max message size must be greater than 0", nameof(MaxMessageSize));

        if (DefaultTimeoutMilliseconds <= 0)
            throw new ArgumentException("Default timeout must be greater than 0", nameof(DefaultTimeoutMilliseconds));

        if (CompressionLevel < 0 || CompressionLevel > 9)
            throw new ArgumentException("Compression level must be between 0 and 9", nameof(CompressionLevel));

        if (AllowedOrigins.Count == 0)
            throw new ArgumentException("At least one allowed origin must be specified", nameof(AllowedOrigins));

        if (AllowedMethods.Count == 0)
            throw new ArgumentException("At least one allowed method must be specified", nameof(AllowedMethods));
    }

    private static string ValidateEnvironment(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Environment cannot be empty", nameof(environment));
        return environment.Trim();
    }

    public override string ToString() => $"BridgeConfig {Environment} ({InstanceName ?? InstanceId})";

    public override bool Equals(object? obj)
    {
        if (obj is not BridgeConfiguration other)
            return false;

        return InstanceId == other.InstanceId;
    }

    public override int GetHashCode() => InstanceId.GetHashCode();
}
