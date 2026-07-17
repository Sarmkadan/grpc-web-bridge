#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace GrpcWebBridge.Domain.Models;


/// <summary>
/// Provides extension methods for <see cref="GrpcResponse"/> to simplify common operations
/// </summary>
public static class GrpcResponseExtensions
{
    /// <summary>
    /// Creates a new successful GrpcResponse with the specified payload
    /// </summary>
    /// <param name="requestId">The request identifier</param>
    /// <param name="payload">The response payload data</param>
    /// <param name="format">The serialization format of the payload</param>
    /// <returns>A new GrpcResponse instance with Success status</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestId"/> or <paramref name="payload"/> is null</exception>
    public static GrpcResponse ToSuccessResponse(this string requestId, byte[] payload, SerializationFormat format = SerializationFormat.Protobuf)
    {
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(payload);

        return new GrpcResponse(requestId, GrpcStatusCode.Ok, "OK")
        {
            Payload = payload,
            PayloadFormat = format,
            StatusMessage = "OK"
        };
    }

    /// <summary>
    /// Creates a new error GrpcResponse with the specified status code and message
    /// </summary>
    /// <param name="requestId">The request identifier</param>
    /// <param name="statusCode">The gRPC status code indicating the error type</param>
    /// <param name="message">Human-readable error message</param>
    /// <param name="details">Optional detailed error information</param>
    /// <returns>A new GrpcResponse instance with error status</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestId"/> or <paramref name="message"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="statusCode"/> is Ok</exception>
    public static GrpcResponse ToErrorResponse(this string requestId, GrpcStatusCode statusCode, string message, string? details = null)
    {
        ArgumentNullException.ThrowIfNull(requestId);
        ArgumentNullException.ThrowIfNull(message);

        if (statusCode == GrpcStatusCode.Ok)
        {
            throw new ArgumentException("Cannot create error response with Ok status", nameof(statusCode));
        }

        return new GrpcResponse(requestId, statusCode, message)
        {
            ErrorDetails = details,
            Payload = []
        };
    }

    /// <summary>
    /// Adds multiple metadata entries to the GrpcResponse in a single operation
    /// </summary>
    /// <param name="response">The GrpcResponse instance</param>
    /// <param name="metadata">Dictionary of metadata key-value pairs to add</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> or <paramref name="metadata"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when a metadata key is null or whitespace</exception>
    public static void AddMetadata(this GrpcResponse response, Dictionary<string, string> metadata)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(metadata);

        foreach (var (key, value) in metadata)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                response.Metadata[key] = value;
            }
        }
    }

    /// <summary>
    /// Adds multiple trailing metadata entries to the GrpcResponse in a single operation
    /// </summary>
    /// <param name="response">The GrpcResponse instance</param>
    /// <param name="trailingMetadata">Dictionary of trailing metadata key-value pairs to add</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> or <paramref name="trailingMetadata"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when a metadata key is null or whitespace</exception>
    public static void AddTrailingMetadata(this GrpcResponse response, Dictionary<string, string> trailingMetadata)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(trailingMetadata);

        foreach (var (key, value) in trailingMetadata)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                response.TrailingMetadata[key] = value;
            }
        }
    }
}