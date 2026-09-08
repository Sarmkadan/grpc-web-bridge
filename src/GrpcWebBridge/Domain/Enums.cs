#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain;

/// <summary>
/// Enumeration for gRPC method types.
/// </summary>
public enum MethodType
{
    /// <summary>
    /// A unary method.
    /// </summary>
    Unary = 0,

    /// <summary>
    /// A client-streaming method.
    /// </summary>
    ClientStreaming = 1,

    /// <summary>
    /// A server-streaming method.
    /// </summary>
    ServerStreaming = 2,

    /// <summary>
    /// A bidirectional-streaming method.
    /// </summary>
    BidirectionalStreaming = 3,
}

/// <summary>
/// Enumeration for message serialization formats.
/// </summary>
public enum SerializationFormat
{
    /// <summary>
    /// The Protocol Buffers serialization format.
    /// </summary>
    Protobuf = 0,

    /// <summary>
    /// The JSON serialization format.
    /// </summary>
    Json = 1,

    /// <summary>
    /// The MessagePack serialization format.
    /// </summary>
    MessagePack = 2,
}

/// <summary>
/// Enumeration for authentication schemes.
/// </summary>
public enum AuthenticationScheme
{
    /// <summary>
    /// No authentication.
    /// </summary>
    None = 0,

    /// <summary>
    /// Bearer token authentication.
    /// </summary>
    Bearer = 1,

    /// <summary>
    /// API key authentication.
    /// </summary>
    ApiKey = 2,

    /// <summary>
    /// Certificate authentication.
    /// </summary>
    Certificate = 3,

    /// <summary>
    /// Custom authentication.
    /// </summary>
    Custom = 4,
}

/// <summary>
/// Enumeration for streaming message types.
/// </summary>
public enum StreamMessageType
{
    /// <summary>
    /// A data message.
    /// </summary>
    Data = 0,

    /// <summary>
    /// A metadata message.
    /// </summary>
    Metadata = 1,

    /// <summary>
    /// A status message.
    /// </summary>
    Status = 2,

    /// <summary>
    /// A heartbeat message.
    /// </summary>
    Heartbeat = 3,

    /// <summary>
    /// An error message.
    /// </summary>
    Error = 4,
}

/// <summary>
/// Enumeration for gRPC status codes.
/// </summary>
public enum GrpcStatusCode
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Ok = 0,

    /// <summary>
    /// The operation was cancelled.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    Unknown = 2,

    /// <summary>
    /// The client specified an invalid argument.
    /// </summary>
    InvalidArgument = 3,

    /// <summary>
    /// The deadline expired before the operation completed.
    /// </summary>
    DeadlineExceeded = 4,

    /// <summary>
    /// The requested entity was not found.
    /// </summary>
    NotFound = 5,

    /// <summary>
    /// The entity that the client attempted to create already exists.
    /// </summary>
    AlreadyExists = 6,

    /// <summary>
    /// The caller does not have permission to execute the operation.
    /// </summary>
    PermissionDenied = 7,

    /// <summary>
    /// A resource has been exhausted.
    /// </summary>
    ResourceExhausted = 8,

    /// <summary>
    /// The system is not in the state required for the operation.
    /// </summary>
    FailedPrecondition = 9,

    /// <summary>
    /// The operation was aborted.
    /// </summary>
    Aborted = 10,

    /// <summary>
    /// The operation was attempted outside the valid range.
    /// </summary>
    OutOfRange = 11,

    /// <summary>
    /// The operation is not implemented or supported.
    /// </summary>
    Unimplemented = 12,

    /// <summary>
    /// An internal error occurred.
    /// </summary>
    Internal = 13,

    /// <summary>
    /// The service is currently unavailable.
    /// </summary>
    Unavailable = 14,

    /// <summary>
    /// Unrecoverable data loss or corruption occurred.
    /// </summary>
    DataLoss = 15,

    /// <summary>
    /// The request does not have valid authentication credentials.
    /// </summary>
    Unauthenticated = 16,
}

/// <summary>
/// Enumeration for service status.
/// </summary>
public enum ServiceStatus
{
    /// <summary>
    /// The service status is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The service is serving requests.
    /// </summary>
    Serving = 1,

    /// <summary>
    /// The service is not serving requests.
    /// </summary>
    NotServing = 2,

    /// <summary>
    /// The service is unknown and not serving requests.
    /// </summary>
    Unknown_ServiceNotServing = 3,
}

/// <summary>
/// Enumeration for stream state.
/// </summary>
public enum StreamState
{
    /// <summary>
    /// The stream is new.
    /// </summary>
    New = 0,

    /// <summary>
    /// The stream is active.
    /// </summary>
    Active = 1,

    /// <summary>
    /// The stream is half-closed.
    /// </summary>
    HalfClosed = 2,

    /// <summary>
    /// The stream is closed.
    /// </summary>
    Closed = 3,

    /// <summary>
    /// The stream has failed.
    /// </summary>
    Failed = 4,
}
