#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Configuration settings for the gRPC-Web bridge
/// </summary>
public sealed class BridgeConfiguration
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");
    public string? InstanceName { get; set; }
    public string Environment { get; set; } = "Production";
    public bool EnableLogging { get; set; } = true;
    public bool EnableSwagger { get; set; } = true;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableCors { get; set; } = true;
    public bool RequireAuthentication { get; set; }

/// <summary>
/// Gets or sets whether detailed health checks require authentication
/// Defaults to true for security - detailed health information should not be publicly accessible
/// </summary>
public bool RequireAuthenticationForDetailedHealth { get; set; } = true;
    public int MaxStreamCount { get; set; } = Constants.Streaming.MaxStreamCount;
    public int StreamIdleTimeoutSeconds { get; set; } = Constants.Streaming.StreamIdleTimeoutSeconds;
    public int StreamHeartbeatIntervalSeconds { get; set; } = Constants.Streaming.StreamHeartbeatIntervalSeconds;
    public int MaxMessageSize { get; set; } = Constants.Grpc.MaxMessageSize;
    public int DefaultTimeoutMilliseconds { get; set; } = Constants.Grpc.DefaultTimeout;
    public bool CompressResponses { get; set; } = true;
    public int CompressionLevel { get; set; } = 6;
    public List<string> AllowedOrigins { get; set; } = ["*"];
    public List<string> AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];
    public Dictionary<string, string> CustomHeaders { get; set; } = [];
    public Dictionary<string, object> ServiceDefaults { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
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
