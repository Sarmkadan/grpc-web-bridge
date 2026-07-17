namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="ProtocolException"/>.
/// </summary>
public static class ProtocolExceptionExtensions
{
    /// <summary>
    /// Returns a string that represents the protocol exception in a format suitable for logging.
    /// </summary>
    /// <param name="exception">The <see cref="ProtocolException"/> instance.</param>
    /// <returns>A string representation of the exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <c>null</c>.</exception>
    public static string ToLogString(this ProtocolException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return $"ProtocolException: {exception.Message} (SourceFormat: {exception.SourceFormat ?? "null"}, TargetFormat: {exception.TargetFormat ?? "null"}, RequestId: {exception.RequestId ?? "null"})";
    }

    /// <summary>
    /// Determines whether the protocol exception is related to a specific request.
    /// </summary>
    /// <param name="exception">The <see cref="ProtocolException"/> instance.</param>
    /// <param name="requestId">The ID of the request to check.</param>
    /// <returns><c>true</c> if the exception is related to the specified request; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="requestId"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requestId"/> is <c>null</c> or empty.</exception>
    public static bool IsRelatedToRequest(this ProtocolException exception, string requestId)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(requestId);

        return exception.RequestId == requestId;
    }
}
