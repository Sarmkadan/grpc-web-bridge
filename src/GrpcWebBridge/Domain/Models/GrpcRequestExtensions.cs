namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Extension methods for <see cref="GrpcRequest"/>.
/// Provides utility methods for working with gRPC requests including metadata access,
/// payload inspection, and request formatting for logging and debugging purposes.
/// </summary>
public static class GrpcRequestExtensions
{
    /// <summary>
    /// Determines if a request has a specific metadata key.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <param name="key">The metadata key to look for.</param>
    /// <returns>True if the request has the specified metadata key; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public static string? GetMetadataValue(this GrpcRequest request, string key, string? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(key);

        return request.Metadata.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Gets the metadata value as a specified type using type conversion.
    /// </summary>
    /// <typeparam name="T">The type to convert the metadata value to.</typeparam>
    /// <param name="request">The request to retrieve metadata from.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found or conversion fails.</param>
    /// <returns>The metadata value converted to type T, or the default value if not found or conversion fails.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or empty.</exception>
    public static T? GetMetadataValue<T>(this GrpcRequest request, string key, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (request.Metadata.TryGetValue(key, out var value) && value is not null)
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Creates a string representation of the request in a format suitable for logging.
    /// Includes request ID, method name, payload format, method type, and metadata summary.
    /// </summary>
    /// <param name="request">The request to format.</param>
    /// <param name="includeMetadata">Whether to include metadata summary in the output.</param>
    /// <param name="maxMetadataLength">Maximum length of metadata to include when <paramref name="includeMetadata"/> is true.</param>
    /// <returns>A string representation of the request suitable for logging.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public static string ToLogString(this GrpcRequest request, bool includeMetadata = false, int maxMetadataLength = 200)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = new System.Text.StringBuilder();
        builder.Append($"Request {request.Id}: {request.FullMethodName}");
        builder.Append($" | Format: {request.PayloadFormat}");
        builder.Append($" | Type: {request.MethodType}");
        builder.Append($" | Timeout: {request.TimeoutMilliseconds}ms");

        if (includeMetadata && request.Metadata.Count > 0)
        {
            var metadataSummary = FormatMetadataForLogging(request.Metadata, maxMetadataLength);
            builder.Append($" | Metadata: {metadataSummary}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Gets the payload size in bytes.
    /// </summary>
    /// <param name="request">The request containing the payload.</param>
    /// <returns>The size of the payload in bytes.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public static int GetPayloadSize(this GrpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Payload.Length;
    }

    /// <summary>
    /// Gets a hexadecimal representation of the payload hash.
    /// </summary>
    /// <param name="request">The request containing the payload.</param>
    /// <returns>The SHA-256 hash of the payload as a hexadecimal string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public static string GetPayloadHashHex(this GrpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.GetPayloadHash();
    }

    /// <summary>
    /// Determines whether the request payload is empty.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <returns>True if the payload is null or empty; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    public static bool IsPayloadEmpty(this GrpcRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Payload.Length == 0;
    }

    /// <summary>
    /// Gets a formatted string representation of the metadata dictionary.
    /// </summary>
    /// <param name="metadata">The metadata dictionary to format.</param>
    /// <param name="maxLength">Maximum length of the formatted string.</param>
    /// <returns>A formatted string representation of the metadata.</returns>
    private static string FormatMetadataForLogging(Dictionary<string, string> metadata, int maxLength)
    {
        var entries = metadata.Select(kvp => $"{kvp.Key}={kvp.Value}");
        var result = string.Join(", ", entries);

        return result.Length <= maxLength
            ? result
            : result[..maxLength] + "...(truncated)";
    }
}
