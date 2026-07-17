using System;
using System.Collections.Generic;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Extension methods for <see cref="GrpcService"/> providing common helper functionality.
/// </summary>
public static class GrpcServiceExtensions
{
    /// <summary>
    /// Gets the full endpoint URL for the service, including the scheme (http/https) and port.
    /// </summary>
    /// <param name="service">The <see cref="GrpcService"/> instance.</param>
    /// <returns>A URL string such as <c>https://example.com:443</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <c>null</c>.</exception>
    public static string GetFullEndpoint(this GrpcService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return $"{GetScheme(service)}://{service.Endpoint}:{service.Port}";
    }

    /// <summary>
    /// Gets the appropriate URI scheme based on TLS configuration.
    /// </summary>
    /// <param name="service">The <see cref="GrpcService"/> instance.</param>
    /// <returns><c>"https"</c> if TLS is enabled, otherwise <c>"http"</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <c>null</c>.</exception>
    private static string GetScheme(this GrpcService service) => service.UseTls ? "https" : "http";

    /// <summary>
    /// Retrieves a metadata value by <paramref name="key"/>, or returns <paramref name="defaultValue"/>
    /// when the key is not present.
    /// </summary>
    /// <param name="service">The <see cref="GrpcService"/> instance.</param>
    /// <param name="key">The metadata key to look up.</param>
    /// <param name="defaultValue">The value to return when the key is missing. Defaults to an empty string.</param>
    /// <returns>The metadata value associated with <paramref name="key"/>, or <paramref name="defaultValue"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> or <paramref name="key"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    public static string GetMetadataValueOrDefault(this GrpcService service, string key, string defaultValue = "")
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrEmpty(key);
        return service.Metadata.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Returns a read‑only collection of all metadata keys defined for the service.
    /// </summary>
    /// <param name="service">The <see cref="GrpcService"/> instance.</param>
    /// <returns>An <see cref="IReadOnlyCollection{T}"/> containing the metadata keys.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is <c>null</c>.</exception>
    public static IReadOnlyCollection<string> GetAllMetadataKeys(this GrpcService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.Metadata.Keys.ToList().AsReadOnly();
    }
}
