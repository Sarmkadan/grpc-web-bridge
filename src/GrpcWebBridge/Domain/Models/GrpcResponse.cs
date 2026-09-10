#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a gRPC response from the backend service
/// </summary>
public sealed class GrpcResponse
{
    /// <summary>
    /// Unique identifier for the response.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Identifier of the request that this response corresponds to.
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// gRPC status code of the response.
    /// </summary>
    public GrpcStatusCode Status { get; set; } = GrpcStatusCode.Ok;

    /// <summary>
    /// Human-readable message associated with the status.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Binary payload of the response.
    /// </summary>
    public byte[] Payload { get; set; } = [];

    /// <summary>
    /// Serialization format of the payload.
    /// </summary>
    public SerializationFormat PayloadFormat { get; set; } = SerializationFormat.Protobuf;

    /// <summary>
    /// Dictionary of metadata entries (initial metadata).
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Dictionary of trailing metadata entries.
    /// </summary>
    public Dictionary<string, string> TrailingMetadata { get; set; } = [];

    /// <summary>
    /// Timestamp when the response was created (in UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the request in milliseconds.
    /// </summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>
    /// Boolean indicating if the response is successful (Status == Ok).
    /// </summary>
    public bool IsSuccess => Status == GrpcStatusCode.Ok;

    /// <summary>
    /// Additional details about the error, if any.
    /// </summary>
    public string? ErrorDetails { get; set; }

    /// <summary>
    /// Initializes a new instance of the GrpcResponse class with default values.
    /// </summary>
    public GrpcResponse() { }

    /// <summary>
    /// Initializes a new instance of the GrpcResponse class with the specified request identifier and payload.
    /// </summary>
    /// <param name="requestId">Identifier of the request that this response corresponds to.</param>
    /// <param name="payload">Binary payload of the response.</param>
    /// <exception cref="ArgumentException">Thrown when requestId is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when payload is null.</exception>
    public GrpcResponse(string requestId, byte[] payload)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestId);
        ArgumentNullException.ThrowIfNull(payload);
        RequestId = ValidateRequestId(requestId);
        Payload = payload ?? [];
    }

    /// <summary>
    /// Initializes a new instance of the GrpcResponse class with the specified request identifier, status, and optional message.
    /// </summary>
    /// <param name="requestId">Identifier of the request that this response corresponds to.</param>
    /// <param name="status">gRPC status code of the response.</param>
    /// <param name="message">Optional human-readable message associated with the status.</param>
    /// <exception cref="ArgumentException">Thrown when requestId is null or empty.</exception>
    public GrpcResponse(string requestId, GrpcStatusCode status, string? message = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(requestId);
        RequestId = ValidateRequestId(requestId);
        Status = status;
        StatusMessage = message;
    }

    /// <summary>
    /// Sets the response to a successful state with the specified payload and serialization format.
    /// </summary>
    /// <param name="payload">Binary payload of the response.</param>
    /// <param name="format">Serialization format of the payload (defaults to Protobuf).</param>
    /// <exception cref="ArgumentNullException">Thrown when payload is null.</exception>
    public void SetSuccess(byte[] payload, SerializationFormat format = SerializationFormat.Protobuf)
    {
        ArgumentNullException.ThrowIfNull(payload);
        Status = GrpcStatusCode.Ok;
        StatusMessage = "OK";
        Payload = payload ?? [];
        PayloadFormat = format;
    }

    /// <summary>
    /// Sets the response to an error state with the specified status code, message, and optional details.
    /// </summary>
    /// <param name="statusCode">gRPC status code for the error (must not be Ok).</param>
    /// <param name="message">Human-readable message associated with the error.</param>
    /// <param name="details">Optional additional details about the error.</param>
    /// <exception cref="ArgumentException">Thrown when message is null or empty, or when statusCode is Ok.</exception>
    public void SetError(GrpcStatusCode statusCode, string message, string? details = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);

        if (statusCode == GrpcStatusCode.Ok)
            throw new ArgumentException("Cannot set error status to Ok", nameof(statusCode));

        Status = statusCode;
        StatusMessage = message;
        ErrorDetails = details;
        Payload = [];
    }

    /// <summary>
    /// Adds a metadata entry to the initial metadata collection.
    /// </summary>
    /// <param name="key">Metadata key (must not be empty or whitespace).</param>
    /// <param name="value">Metadata value (must not be null or empty).</param>
    /// <exception cref="ArgumentException">Thrown when key is empty or whitespace, or when value is null or empty.</exception>
    public void AddMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));
        ArgumentException.ThrowIfNullOrEmpty(value);

        Metadata[key] = value;
    }

    /// <summary>
    /// Adds a metadata entry to the trailing metadata collection.
    /// </summary>
    /// <param name="key">Metadata key (must not be empty or whitespace).</param>
    /// <param name="value">Metadata value (must not be null or empty).</param>
    /// <exception cref="ArgumentException">Thrown when key is empty or whitespace, or when value is null or empty.</exception>
    public void AddTrailingMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));
        ArgumentException.ThrowIfNullOrEmpty(value);

        TrailingMetadata[key] = value;
    }

    /// <summary>
    /// Retrieves a metadata value by key from the initial metadata collection.
    /// </summary>
    /// <param name="key">Metadata key to look up (must not be null or empty).</param>
    /// <returns>The metadata value associated with the specified key, or null if not found.</returns>
    /// <exception cref="ArgumentException">Thrown when key is null or empty.</exception>
    public string? GetMetadata(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return Metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Retrieves a metadata value by key from the trailing metadata collection.
    /// </summary>
    /// <param name="key">Metadata key to look up.</param>
    /// <returns>The metadata value associated with the specified key, or null if not found.</returns>
    public string? GetTrailingMetadata(string key)
    {
        return TrailingMetadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Validates the response to ensure it meets required constraints.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when RequestId is empty, StatusMessage is missing for error responses, Payload exceeds maximum size, or DurationMilliseconds is negative.</exception>
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

    /// <summary>
    /// Computes the SHA256 hash of the payload.
    /// </summary>
    /// <returns>Hexadecimal string representation of the SHA256 hash of the payload.</returns>
    public string GetPayloadHash()
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(Payload);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Creates a copy of the payload byte array.
    /// </summary>
    /// <returns>A new byte array containing a copy of the payload.</returns>
    public byte[] GetPayloadCopy() => (byte[])Payload.Clone();

    /// <summary>
    /// Returns a string representation of the response.
    /// </summary>
    /// <returns>A string in the format "Response {Id}: {Status} ({DurationMilliseconds}ms)".</returns>
    public override string ToString() => $"Response {Id}: {Status} ({DurationMilliseconds}ms)";

    /// <summary>
    /// Determines whether the specified object is equal to the current response.
    /// </summary>
    /// <param name="obj">The object to compare with the current response.</param>
    /// <returns>true if the specified object is a GrpcResponse with the same Id; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not GrpcResponse other)
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// Returns the hash code for this response.
    /// </summary>
    /// <returns>A hash code based on the Id of the response.</returns>
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    /// Validates the request identifier by trimming whitespace and checking for emptiness.
    /// </summary>
    /// <param name="requestId">The request identifier to validate.</param>
    /// <returns>The trimmed request identifier.</returns>
    /// <exception cref="ArgumentException">Thrown when requestId is null, empty, or consists only of whitespace.</exception>
    private static string ValidateRequestId(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("Request ID cannot be empty", nameof(requestId));
        return requestId.Trim();
    }
}