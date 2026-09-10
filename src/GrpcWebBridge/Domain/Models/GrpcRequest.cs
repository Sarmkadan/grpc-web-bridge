#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a gRPC request intercepted or created by the bridge
/// </summary>
public sealed class GrpcRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for the request.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>
    /// Gets or sets the name of the gRPC service.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the name of the gRPC method.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the full method name in the format /ServiceName/MethodName.
    /// </summary>
    public string FullMethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the request payload as a byte array.
    /// </summary>
    public byte[] Payload { get; set; } = [];
    /// <summary>
    /// Gets or sets the serialization format of the payload.
    /// </summary>
    public SerializationFormat PayloadFormat { get; set; } = SerializationFormat.Protobuf;
    /// <summary>
    /// Gets or sets the metadata associated with the request.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
    /// <summary>
    /// Gets or sets the request identifier for tracing.
    /// </summary>
    public string? RequestId { get; set; }
    /// <summary>
    /// Gets or sets the trace identifier for distributed tracing.
    /// </summary>
    public string? TraceId { get; set; }
    /// <summary>
    /// Gets or sets the user identifier associated with the request.
    /// </summary>
    public string? UserId { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the request was created (in UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the timeout for the request in milliseconds.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = Constants.Grpc.DefaultTimeout;
    /// <summary>
    /// Gets or sets the type of the gRPC method (e.g., Unary, ServerStreaming, etc.).
    /// </summary>
    public MethodType MethodType { get; set; } = MethodType.Unary;

    public GrpcRequest() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcRequest"/> class with the specified service name, method name, and payload.
    /// </summary>
    /// <param name="serviceName">The name of the gRPC service.</param>
    /// <param name="methodName">The name of the gRPC method.</param>
    /// <param name="payload">The request payload as a byte array.</param>
    public GrpcRequest(string serviceName, string methodName, byte[] payload)
    {
        ServiceName = ValidateServiceName(serviceName);
        MethodName = ValidateMethodName(methodName);
        FullMethodName = $"/{serviceName}/{methodName}";
        Payload = payload ?? [];
    }

    /// <summary>
    /// Adds a metadata entry to the request.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <exception cref="ArgumentException">Thrown when the metadata key is empty or consists only of white-space.</exception>
    public void AddMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        Metadata[key] = value;
    }

    /// <summary>
    /// Gets the metadata value associated with the specified key.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>The metadata value if the key is found; otherwise, null.</returns>
    public string? GetMetadata(string key)
    {
        return Metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Determines whether the request contains metadata with the specified key.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <returns>true if the request contains metadata with the specified key; otherwise, false.</returns>
    public bool HasMetadata(string key) => Metadata.ContainsKey(key);

    /// <summary>
    /// Sets the request payload and optionally the serialization format.
    /// </summary>
    /// <param name="payload">The request payload as a byte array.</param>
    /// <param name="format">The serialization format of the payload. Default is Protobuf.</param>
    /// <exception cref="ArgumentNullException">Thrown when the payload is null.</exception>
    public void SetPayload(byte[] payload, SerializationFormat format = SerializationFormat.Protobuf)
    {
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        Payload = payload;
        PayloadFormat = format;
    }

    /// <summary>
    /// Validates the request properties.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the service name, method name is empty, payload exceeds maximum size, or timeout is not greater than zero.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServiceName))
            throw new ArgumentException("Service name cannot be empty", nameof(ServiceName));

        if (string.IsNullOrWhiteSpace(MethodName))
            throw new ArgumentException("Method name cannot be empty", nameof(MethodName));

        if (Payload.Length > Constants.Grpc.MaxMessageSize)
            throw new ArgumentException(
                $"Payload exceeds maximum size of {Constants.Grpc.MaxMessageSize} bytes",
                nameof(Payload));

        if (TimeoutMilliseconds <= 0)
            throw new ArgumentException("Timeout must be greater than 0", nameof(TimeoutMilliseconds));
    }

    /// <summary>
    /// Computes the SHA256 hash of the request payload.
    /// </summary>
    /// <returns>A hexadecimal string representing the hash of the payload.</returns>
    public string GetPayloadHash()
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Payload);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Creates a copy of the request payload.
    /// </summary>
    /// <returns>A byte array that is a copy of the payload.</returns>
    public byte[] GetPayloadCopy() => (byte[])Payload.Clone();

    private static string ValidateServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("Service name cannot be empty", nameof(serviceName));
        return serviceName.Trim();
    }

    private static string ValidateMethodName(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException("Method name cannot be empty", nameof(methodName));
        return methodName.Trim();
    }

    /// <summary>
    /// Returns a string that represents the current request.
    /// </summary>
    /// <returns>A string in the format "Request {Id}: {FullMethodName}".</returns>
    public override string ToString() => $"Request {Id}: {FullMethodName}";

    /// <summary>
    /// Determines whether the specified object is equal to the current request.
    /// </summary>
    /// <param name="obj">The object to compare with the current request.</param>
    /// <returns>true if the specified object is a GrpcRequest and has the same Id; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not GrpcRequest other)
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current request.</returns>
    public override int GetHashCode() => Id.GetHashCode();
}