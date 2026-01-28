namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Extension methods for <see cref="GrpcRequest"/>.
/// </summary>
public static class GrpcRequestExtensions
{
    /// <summary>
    /// Determines if a request has a specific metadata key.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <param name="key">The metadata key to look for.</param>
    /// <returns>True if the request has the specified metadata key; otherwise, false.</returns>
    public static bool HasMetadataKey(this GrpcRequest request, string key)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(key);

        return request.Metadata.ContainsKey(key);
    }

    /// <summary>
    /// Gets the metadata value as a string.
    /// </summary>
    /// <param name="request">The request to retrieve metadata from.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>The metadata value as a string, or the default value if not found.</returns>
    public static string? GetMetadataValue(this GrpcRequest request, string key, string? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (request.Metadata.TryGetValue(key, out string? value))
        {
            return value;
        }
        return defaultValue;
    }

    /// <summary>
    /// Creates a string representation of the request in a format suitable for logging.
    /// </summary>
    /// <param name="request">The request to format.</param>
    /// <returns>A string representation of the request.</returns>
    public static string ToLogString(this GrpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return $"Request {request.Id}: {request.FullMethodName} (Payload format: {request.PayloadFormat})";
    }
}
