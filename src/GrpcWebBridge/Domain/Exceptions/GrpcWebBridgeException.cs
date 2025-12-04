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

    public GrpcWebBridgeException(string message) : base(message) { }

    public GrpcWebBridgeException(string message, Exception? innerException) : base(message, innerException) { }

    public GrpcWebBridgeException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public GrpcWebBridgeException(string message, GrpcStatusCode statusCode) : base(message)
    {
        GrpcStatus = statusCode;
    }

    public void AddContext(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Context key cannot be empty", nameof(key));

        Context[key] = value;
    }

    public object? GetContext(string key)
    {
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
        AddContext(key, value);
        return this;
    }

    public GrpcWebBridgeException WithInnerException(Exception innerException)
    {
        Data[nameof(innerException)] = innerException;
        return this;
    }
}
