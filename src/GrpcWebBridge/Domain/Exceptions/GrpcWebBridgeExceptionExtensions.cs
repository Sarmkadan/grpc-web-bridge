namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="GrpcWebBridgeException"/>.
/// </summary>
public static class GrpcWebBridgeExceptionExtensions
{
    /// <summary>
    /// Adds a context entry to the exception.
    /// </summary>
    /// <param name="exception">The exception to add context to.</param>
    /// <param name="key">The key of the context entry.</param>
    /// <param name="value">The value of the context entry.</param>
    /// <returns>The exception with the added context.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null or empty.</exception>
    public static GrpcWebBridgeException AddContextEntry(this GrpcWebBridgeException exception, string key, object value)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(key);

        exception.AddContext(key, value);
        return exception;
    }

    /// <summary>
    /// Gets a string representation of the exception's context.
    /// </summary>
    /// <param name="exception">The exception to get the context string from.</param>
    /// <returns>A string representation of the exception's context.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static string GetContextString(this GrpcWebBridgeException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var contextString = string.Join(", ", exception.Context.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        return contextString;
    }

    /// <summary>
    /// Creates a new exception with the same message and inner exception as the original exception, 
    /// but with a different error code.
    /// </summary>
    /// <param name="exception">The exception to create a new exception from.</param>
    /// <param name="newErrorCode">The new error code.</param>
    /// <returns>A new exception with the same message and inner exception, but with a different error code.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
    public static GrpcWebBridgeException WithNewErrorCode(this GrpcWebBridgeException exception, string newErrorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new GrpcWebBridgeException(exception.Message, exception.InnerException) { ErrorCode = newErrorCode };
    }
}
