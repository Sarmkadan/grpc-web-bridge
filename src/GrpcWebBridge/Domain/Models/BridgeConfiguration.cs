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
    /// <summary>
    /// Gets or sets the unique identifier for the bridge instance.
    /// Defaults to a new GUID.
    /// </summary>
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the optional friendly name for the bridge instance.
    /// </summary>
    public string? InstanceName { get; set; }
    /// <summary>
    /// Gets or sets the environment name (e.g., Development, Staging, Production).
    /// Defaults to "Production".
    /// </summary>
    public string Environment { get; set; } = "Production";
    /// <summary>
    /// Gets or sets whether logging is enabled.
    /// Defaults to true.
    /// </summary>
    public bool EnableLogging { get; set; } = true;
    /// <summary>
    /// Gets or sets whether Swagger is enabled.
    /// Defaults to true.
    /// </summary>
    public bool EnableSwagger { get; set; } = true;
    /// <summary>
    /// Gets or sets whether metrics collection is enabled.
    /// Defaults to true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;
    /// <summary>
    /// Gets or sets whether CORS is enabled.
    /// Defaults to true.
    /// </summary>
    public bool EnableCors { get; set; } = true;
    /// <summary>
    /// Gets or sets whether authentication is required for the bridge.
    /// </summary>
    public bool RequireAuthentication { get; set; }

    /// <summary>
    /// Gets or sets whether detailed health checks require authentication
    /// Defaults to true for security - detailed health information should not be publicly accessible
    /// </summary>
    public bool RequireAuthenticationForDetailedHealth { get; set; } = true;
    /// <summary>
    /// Gets or sets the maximum number of concurrent streams allowed.
    /// Defaults to the value from Constants.Streaming.MaxStreamCount.
    /// </summary>
    public int MaxStreamCount { get; set; } = Constants.Streaming.MaxStreamCount;
    /// <summary>
    /// Gets or sets the idle timeout for streams in seconds.
    /// Defaults to the value from Constants.Streaming.StreamIdleTimeoutSeconds.
    /// </summary>
    public int StreamIdleTimeoutSeconds { get; set; } = Constants.Streaming.StreamIdleTimeoutSeconds;
    /// <summary>
    /// Gets or sets the interval for sending stream heartbeats in seconds.
    /// Defaults to the value from Constants.Streaming.StreamHeartbeatIntervalSeconds.
    /// </summary>
    public int StreamHeartbeatIntervalSeconds { get; set; } = Constants.Streaming.StreamHeartbeatIntervalSeconds;
    /// <summary>
    /// Gets or sets the maximum size of a gRPC message in bytes.
    /// Defaults to the value from Constants.Grpc.MaxMessageSize.
    /// </summary>
    public int MaxMessageSize { get; set; } = Constants.Grpc.MaxMessageSize;
    /// <summary>
    /// Gets or sets the default timeout for gRPC calls in milliseconds.
    /// Defaults to the value from Constants.Grpc.DefaultTimeout.
    /// </summary>
    public int DefaultTimeoutMilliseconds { get; set; } = Constants.Grpc.DefaultTimeout;
    /// <summary>
    /// Gets or sets whether responses should be compressed.
    /// Defaults to true.
    /// </summary>
    public bool CompressResponses { get; set; } = true;
    /// <summary>
    /// Gets or sets the compression level (0-9) for response compression.
    /// Defaults to 6.
    /// </summary>
    public int CompressionLevel { get; set; } = 6;
    /// <summary>
    /// Gets or sets the list of allowed origins for CORS.
    /// Defaults to a list containing "*" (allow all).
    /// </>
    public List<string> AllowedOrigins { get; set; } = ["*"];
    /// <summary>
    /// Gets or sets the list of allowed HTTP methods for CORS.
    /// Defaults to GET, POST, PUT, DELETE, OPTIONS.
    /// </summary>
    public List<string> AllowedMethods { get; set; } = ["GET", "POST", "PUT", "DELETE", "OPTIONS"];
    /// <summary>
    /// Gets or sets the custom headers to be added to responses.
    /// Defaults to an empty dictionary.
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = [];
    /// <summary>
    /// Gets or sets the default values for services.
    /// Defaults to an empty dictionary.
    /// </summary>
    public Dictionary<string, object> ServiceDefaults { get; set; } = [];
    /// <summary>
    /// Gets or sets the date and time when the configuration was created.
    /// Defaults to the current UTC date and time.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the date and time when the configuration was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the BridgeConfiguration class with default values.
    /// </summary>
    public BridgeConfiguration() { }

    /// <summary>
    /// Initializes a new instance of the BridgeConfiguration class.
    /// </summary>
    /// <param name="environment">The environment name (e.g., Development, Staging, Production). Cannot be null, empty, or whitespace.</param>
    /// <param name="instanceName">The optional friendly name for the bridge instance.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="environment"/> is null, empty, or whitespace.</exception>
    public BridgeConfiguration(string environment, string? instanceName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(environment);
        Environment = ValidateEnvironment(environment);
        InstanceName = instanceName;
    }

    /// <summary>
    /// Sets the default value for a service.
    /// </summary>
    /// <param name="serviceName">The name of the service. Cannot be null, empty, or whitespace.</param>
    /// <param name="defaultValue">The default value for the service.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="serviceName"/> is null, empty, or whitespace.</exception>
    public void SetServiceDefault(string serviceName, object defaultValue)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be empty", nameof(serviceName));

        ServiceDefaults[serviceName] = defaultValue;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the default value for a service.
    /// </summary>
    /// <param name="serviceName">The name of the service.</param>
    /// <returns>The default value for the service, or null if not found.</returns>
    public object? GetServiceDefault(string serviceName)
    {
        return ServiceDefaults.TryGetValue(serviceName, out var value) ? value : null;
    }

    /// <summary>
    /// Adds an origin to the list of allowed origins.
    /// </summary>
    /// <param name="origin">The origin to add. Cannot be null, empty, or whitespace.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="origin"/> is null, empty, or whitespace.</exception>
    public void AddAllowedOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            throw new ArgumentException("Origin cannot be empty", nameof(origin));

        if (!AllowedOrigins.Contains(origin))
            AllowedOrigins.Add(origin);

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Removes an origin from the list of allowed origins.
    /// </summary>
    /// <param name="origin">The origin to remove.</param>
    public void RemoveAllowedOrigin(string origin)
    {
        if (AllowedOrigins.Remove(origin))
            UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a custom header to the configuration.
    /// </summary>
    /// <param name="key">The header key. Cannot be null, empty, or whitespace.</param>
    /// <param name="value">The header value.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null, empty, or whitespace.</exception>
    public void AddCustomHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Header key cannot be empty", nameof(key));

        CustomHeaders[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the value of a custom header.
    /// </summary>
    /// <param name="key">The header key.</param>
    /// <returns>The header value, or null if not found.</returns>
    public string? GetCustomHeader(string key)
    {
        return CustomHeaders.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any validation fails.</exception>
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

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is a BridgeConfiguration and has the same InstanceId; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not BridgeConfiguration other)
            return false;

        return InstanceId == other.InstanceId;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => InstanceId.GetHashCode();
}