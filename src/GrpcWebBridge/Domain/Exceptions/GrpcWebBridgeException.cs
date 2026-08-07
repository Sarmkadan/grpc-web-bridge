#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Base exception for all gRPC-Web bridge operations
/// </summary>
public class GrpcWebBridgeException : Exception
{
    public string? ErrorCode { get; set; }
    public GrpcStatusCode? GrpcStatus { get; set; }
    public Dictionary<string, object> Context { get; set; } = [];

    public GrpcWebBridgeException() : base() { }

    public GrpcWebBridgeException(string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }

    public GrpcWebBridgeException(string message, Exception? innerException) : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }

    public GrpcWebBridgeException(string message, string errorCode) : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        ErrorCode = errorCode;
    }

    public GrpcWebBridgeException(string message, GrpcStatusCode statusCode) : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        GrpcStatus = statusCode;
    }

    public void AddContext(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Context key cannot be empty", nameof(key));

        ArgumentNullException.ThrowIfNull(value);
        Context[key] = value;
    }

    public object? GetContext(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return Context.TryGetValue(key, out var value) ? value : null;
    }

    public override string ToString()
    {
        var message = base.ToString();
        if (!string.IsNullOrEmpty(ErrorCode))
            message += $" [ErrorCode: {ErrorCode}]";

        if (GrpcStatus.HasValue)
            message += $" [GrpcStatus: {GrpcStatus}]";

        return message;
    }

    public GrpcWebBridgeException WithContext(string key, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        AddContext(key, value);
        return this;
    }

    public GrpcWebBridgeException WithInnerException(Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        Data[nameof(innerException)] = innerException;
        return this;
    }
}
