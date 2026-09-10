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
    /// <summary>
    /// Gets or sets the error code.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the gRPC status code.
    /// </summary>
    public GrpcStatusCode? GrpcStatus { get; set; }

    /// <summary>
    /// Gets the context data.
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcWebBridgeException"/> class.
    /// </summary>
    public GrpcWebBridgeException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcWebBridgeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public GrpcWebBridgeException(string message) : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcWebBridgeException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public GrpcWebBridgeException(string message, Exception? innerException) : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentNullException.ThrowIfNull(innerException);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcWebBridgeException"/> class with a specified error message and error code.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="errorCode">The error code.</param>
    public GrpcWebBridgeException(string message, string errorCode) : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        ArgumentException.ThrowIfNullOrEmpty(errorCode);
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcWebBridgeException"/> class with a specified error message and gRPC status code.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="statusCode">The gRPC status code.</param>
    public GrpcWebBridgeException(string message, GrpcStatusCode statusCode) : base(message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        GrpcStatus = statusCode;
    }

    /// <summary>
    /// Adds a context item with the specified key and value.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    /// <exception cref="ArgumentException">Thrown when the key is empty or consists only of white-space.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
    public void AddContext(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Context key cannot be empty", nameof(key));

        ArgumentNullException.ThrowIfNull(value);
        Context[key] = value;
    }

    /// <summary>
    /// Gets the context value associated with the specified key.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <returns>The context value if the key is found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is empty or consists only of white-space.</exception>
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

    /// <summary>
    /// Adds a context item with the specified key and value and returns the current exception instance.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    /// <returns>The current <see cref="GrpcWebBridgeException"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is empty or consists only of white-space.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
    public GrpcWebBridgeException WithContext(string key, object value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        AddContext(key, value);
        return this;
    }

    /// <summary>
    /// Adds an inner exception to the current exception and returns the current exception instance.
    /// </summary>
    /// <param name="innerException">The exception to add as an inner exception.</param>
    /// <returns>The current <see cref="GrpcWebBridgeException"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the innerException is null.</exception>
    public GrpcWebBridgeException WithInnerException(Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        Data[nameof(innerException)] = innerException;
        return this;
    }
}
