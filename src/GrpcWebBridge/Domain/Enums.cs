#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain;

/// <summary>
/// Enumeration for gRPC method types
/// </summary>
public enum MethodType
{
    Unary = 0,
    ClientStreaming = 1,
    ServerStreaming = 2,
    BidirectionalStreaming = 3
}

/// <summary>
/// Enumeration for message serialization formats
/// </summary>
public enum SerializationFormat
{
    Protobuf = 0,
    Json = 1,
    MessagePack = 2
}

/// <summary>
/// Enumeration for authentication schemes
/// </summary>
public enum AuthenticationScheme
{
    None = 0,
    Bearer = 1,
    ApiKey = 2,
    Certificate = 3,
    Custom = 4
}

/// <summary>
/// Enumeration for streaming message types
/// </summary>
public enum StreamMessageType
{
    Data = 0,
    Metadata = 1,
    Status = 2,
    Heartbeat = 3,
    Error = 4
}

/// <summary>
/// Enumeration for gRPC status codes
/// </summary>
public enum GrpcStatusCode
{
    Ok = 0,
    Cancelled = 1,
    Unknown = 2,
    InvalidArgument = 3,
    DeadlineExceeded = 4,
    NotFound = 5,
    AlreadyExists = 6,
    PermissionDenied = 7,
    ResourceExhausted = 8,
    FailedPrecondition = 9,
    Aborted = 10,
    OutOfRange = 11,
    Unimplemented = 12,
    Internal = 13,
    Unavailable = 14,
    DataLoss = 15,
    Unauthenticated = 16
}

/// <summary>
/// Enumeration for service status
/// </summary>
public enum ServiceStatus
{
    Unknown = 0,
    Serving = 1,
    NotServing = 2,
    Unknown_ServiceNotServing = 3
}

/// <summary>
/// Enumeration for stream state
/// </summary>
public enum StreamState
{
    New = 0,
    Active = 1,
    HalfClosed = 2,
    Closed = 3,
    Failed = 4
}
