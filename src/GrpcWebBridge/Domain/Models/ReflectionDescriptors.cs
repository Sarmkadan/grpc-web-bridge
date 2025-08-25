// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Describes a registered gRPC service as surfaced by the reflection API.
/// Carries all metadata needed for a client to understand and invoke the service
/// without prior knowledge of the proto descriptor file.
/// </summary>
public sealed class GrpcServiceDescriptor
{
    /// <summary>Gets the fully-qualified service name (e.g. <c>mypackage.MyService</c>).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets the unqualified service name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the proto package name.</summary>
    public string PackageName { get; init; } = string.Empty;

    /// <summary>Gets an optional human-readable description of the service.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the host or IP address of the backing gRPC server.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Gets the port number of the backing gRPC server.</summary>
    public int Port { get; init; }

    /// <summary>Gets a value indicating whether the backing server requires TLS.</summary>
    public bool UseTls { get; init; }

    /// <summary>Gets the descriptors of every method exposed by this service.</summary>
    public IReadOnlyCollection<MethodDescriptor> Methods { get; init; } = [];
}

/// <summary>
/// Describes a single gRPC method exposed by a service.
/// </summary>
public sealed class MethodDescriptor
{
    /// <summary>Gets the unqualified method name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the fully-qualified method name (e.g. <c>mypackage.MyService/MyMethod</c>).</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets the fully-qualified name of the service that owns this method.</summary>
    public string ServiceFullName { get; init; } = string.Empty;

    /// <summary>Gets the method type as a string: Unary, ClientStreaming, ServerStreaming, or BidirectionalStreaming.</summary>
    public string MethodType { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the method accepts a client-side stream of messages.</summary>
    public bool IsClientStreaming { get; init; }

    /// <summary>Gets a value indicating whether the method produces a server-side stream of messages.</summary>
    public bool IsServerStreaming { get; init; }

    /// <summary>Gets the proto message type name for the request payload.</summary>
    public string InputMessageType { get; init; } = string.Empty;

    /// <summary>Gets the proto message type name for the response payload.</summary>
    public string OutputMessageType { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether this method is deprecated.</summary>
    public bool IsDeprecated { get; init; }

    /// <summary>Gets an optional human-readable description of the method.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the default timeout for this method in milliseconds.</summary>
    public int TimeoutMilliseconds { get; init; }
}

/// <summary>
/// Wraps the output of a reflection query with status metadata.
/// </summary>
/// <typeparam name="T">The type of the result payload.</typeparam>
public sealed class ReflectionResult<T>
{
    /// <summary>Gets the query payload. <c>null</c> when <see cref="Success"/> is <c>false</c>.</summary>
    public T? Data { get; init; }

    /// <summary>Gets a value indicating whether the query completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Gets an error message when <see cref="Success"/> is <c>false</c>; otherwise <c>null</c>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Gets the UTC timestamp at which the reflection query was evaluated.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Creates a successful result wrapping <paramref name="data"/>.</summary>
    /// <param name="data">The payload to return to the caller.</param>
    public static ReflectionResult<T> Ok(T data) =>
        new() { Data = data, Success = true };

    /// <summary>Creates a failed result with a descriptive <paramref name="errorMessage"/>.</summary>
    /// <param name="errorMessage">A human-readable explanation of the failure.</param>
    public static ReflectionResult<T> Fail(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
