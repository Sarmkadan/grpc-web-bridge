#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using System.Diagnostics.CodeAnalysis;

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Extension methods for <see cref="StreamingException"/> that provide additional functionality
/// for working with streaming errors and stream states.
/// </summary>
public static class StreamingExceptionExtensions
{
    /// <summary>
    /// Determines whether the exception represents a terminal stream state.
    /// </summary>
    /// <param name="exception">The streaming exception to check.</param>
    /// <returns>True if the exception represents a terminal state (Closed or Failed); otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool IsTerminalState(this StreamingException? exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.LastStreamState switch
        {
            StreamState.Closed or StreamState.Failed => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines whether the exception represents a recoverable stream state.
    /// </summary>
    /// <param name="exception">The streaming exception to check.</param>
    /// <returns>True if the exception represents a recoverable state (New, Active, or HalfClosed); otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool IsRecoverableState(this StreamingException? exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.LastStreamState switch
        {
            StreamState.New or StreamState.Active or StreamState.HalfClosed => true,
            _ => false
        };
    }

    /// <summary>
    /// Determines whether the exception represents a failed stream state.
    /// </summary>
    /// <param name="exception">The streaming exception to check.</param>
    /// <returns>True if the exception represents a failed state; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool IsFailedState(this StreamingException? exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.LastStreamState == StreamState.Failed;
    }

    /// <summary>
    /// Gets the stream state as a formatted string.
    /// </summary>
    /// <param name="exception">The streaming exception.</param>
    /// <param name="defaultValue">The default value to return if the stream state is not set.</param>
    /// <returns>The formatted stream state or the default value if not set.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string GetStreamStateString(this StreamingException? exception, string defaultValue = "Unknown")
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception.LastStreamState?.ToString() ?? defaultValue;
    }

    /// <summary>
    /// Creates a new <see cref="StreamingException"/> with additional context from an existing exception.
    /// </summary>
    /// <param name="exception">The original exception.</param>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    /// <returns>A new <see cref="StreamingException"/> with the additional context.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is null.
    /// Thrown when <paramref name="key"/> is null or empty.
    /// </exception>
    public static StreamingException WithContext(
        this StreamingException exception,
        string key,
        object value)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(key);

        var newException = new StreamingException(exception.Message, exception.InnerException)
        {
            StreamId = exception.StreamId,
            LastStreamState = exception.LastStreamState,
            SequenceNumber = exception.SequenceNumber,
            ErrorCode = exception.ErrorCode,
            GrpcStatus = exception.GrpcStatus
        };

        newException.AddContext(key, value);
        return newException;
    }

    /// <summary>
    /// Determines whether the exception has a specific error code.
    /// </summary>
    /// <param name="exception">The streaming exception to check.</param>
    /// <param name="errorCode">The error code to match against.</param>
    /// <returns>True if the exception has the specified error code; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="exception"/> is null.
    /// Thrown when <paramref name="errorCode"/> is null.
    /// </exception>
    public static bool HasErrorCode(this StreamingException? exception, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(errorCode);

        return string.Equals(exception.ErrorCode, errorCode, StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets all stream-related context from the exception as a dictionary.
    /// </summary>
    /// <param name="exception">The streaming exception.</param>
    /// <returns>A dictionary containing all stream-related context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static IReadOnlyDictionary<string, object> GetStreamContext(
        this StreamingException? exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var context = new Dictionary<string, object>(exception.Context);

        if (!string.IsNullOrEmpty(exception.StreamId))
            context["StreamId"] = exception.StreamId;

        if (exception.LastStreamState.HasValue)
            context["StreamState"] = exception.LastStreamState.Value;

        if (exception.SequenceNumber.HasValue)
            context["SequenceNumber"] = exception.SequenceNumber.Value;

        if (!string.IsNullOrEmpty(exception.ErrorCode))
            context["ErrorCode"] = exception.ErrorCode;

        if (exception.GrpcStatus.HasValue)
            context["GrpcStatus"] = exception.GrpcStatus.Value;

        return context.AsReadOnly();
    }
}