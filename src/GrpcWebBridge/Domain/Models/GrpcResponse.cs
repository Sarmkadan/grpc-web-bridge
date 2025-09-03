// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a gRPC response from the backend service
/// </summary>
public class GrpcResponse
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RequestId { get; set; } = string.Empty;
    public GrpcStatusCode Status { get; set; } = GrpcStatusCode.Ok;
    public string? StatusMessage { get; set; }
    public byte[] Payload { get; set; } = [];
    public SerializationFormat PayloadFormat { get; set; } = SerializationFormat.Protobuf;
    public Dictionary<string, string> Metadata { get; set; } = [];
    public Dictionary<string, string> TrailingMetadata { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long DurationMilliseconds { get; set; }
    public bool IsSuccess => Status == GrpcStatusCode.Ok;
    public string? ErrorDetails { get; set; }

    public GrpcResponse() { }

    public GrpcResponse(string requestId, byte[] payload)
    {
        RequestId = ValidateRequestId(requestId);
        Payload = payload ?? [];
    }

    public GrpcResponse(string requestId, GrpcStatusCode status, string? message = null)
    {
        RequestId = ValidateRequestId(requestId);
        Status = status;
        StatusMessage = message;
    }

    public void SetSuccess(byte[] payload, SerializationFormat format = SerializationFormat.Protobuf)
    {
        Status = GrpcStatusCode.Ok;
        StatusMessage = "OK";
        Payload = payload ?? [];
        PayloadFormat = format;
    }

    public void SetError(GrpcStatusCode statusCode, string message, string? details = null)
    {
        if (statusCode == GrpcStatusCode.Ok)
            throw new ArgumentException("Cannot set error status to Ok", nameof(statusCode));

        Status = statusCode;
        StatusMessage = message;
        ErrorDetails = details;
        Payload = [];
    }

    public void AddMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        Metadata[key] = value;
    }

    public void AddTrailingMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        TrailingMetadata[key] = value;
    }

    public string? GetMetadata(string key)
    {
        return Metadata.TryGetValue(key, out var value) ? value : null;
    }

    public string? GetTrailingMetadata(string key)
    {
        return TrailingMetadata.TryGetValue(key, out var value) ? value : null;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RequestId))
            throw new ArgumentException("Request ID cannot be empty", nameof(RequestId));

        if (!IsSuccess && string.IsNullOrWhiteSpace(StatusMessage))
            throw new ArgumentException("Status message is required for error responses", nameof(StatusMessage));

        if (Payload.Length > Constants.Grpc.MaxMessageSize)
            throw new ArgumentException(
                $"Payload exceeds maximum size of {Constants.Grpc.MaxMessageSize} bytes",
                nameof(Payload));

        if (DurationMilliseconds < 0)
            throw new ArgumentException("Duration cannot be negative", nameof(DurationMilliseconds));
    }

    public string GetPayloadHash()
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Payload);
        return Convert.ToHexString(hash);
    }

    public byte[] GetPayloadCopy() => (byte[])Payload.Clone();

    private static string ValidateRequestId(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID cannot be empty", nameof(requestId));
        return requestId.Trim();
    }

    public override string ToString() => $"Response {Id}: {Status} ({DurationMilliseconds}ms)";

    public override bool Equals(object? obj)
    {
        if (obj is not GrpcResponse other)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
