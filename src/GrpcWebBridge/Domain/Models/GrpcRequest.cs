// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a gRPC request intercepted or created by the bridge
/// </summary>
public class GrpcRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ServiceName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string FullMethodName { get; set; } = string.Empty;
    public byte[] Payload { get; set; } = [];
    public SerializationFormat PayloadFormat { get; set; } = SerializationFormat.Protobuf;
    public Dictionary<string, string> Metadata { get; set; } = [];
    public string? RequestId { get; set; }
    public string? TraceId { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TimeoutMilliseconds { get; set; } = Constants.Grpc.DefaultTimeout;
    public MethodType MethodType { get; set; } = MethodType.Unary;

    public GrpcRequest() { }

    public GrpcRequest(string serviceName, string methodName, byte[] payload)
    {
        ServiceName = ValidateServiceName(serviceName);
        MethodName = ValidateMethodName(methodName);
        FullMethodName = $"/{serviceName}/{methodName}";
        Payload = payload ?? [];
    }

    public void AddMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        Metadata[key] = value;
    }

    public string? GetMetadata(string key)
    {
        return Metadata.TryGetValue(key, out var value) ? value : null;
    }

    public bool HasMetadata(string key) => Metadata.ContainsKey(key);

    public void SetPayload(byte[] payload, SerializationFormat format = SerializationFormat.Protobuf)
    {
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        Payload = payload;
        PayloadFormat = format;
    }

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

    public string GetPayloadHash()
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Payload);
        return Convert.ToHexString(hash);
    }

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

    public override string ToString() => $"Request {Id}: {FullMethodName}";

    public override bool Equals(object? obj)
    {
        if (obj is not GrpcRequest other)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
