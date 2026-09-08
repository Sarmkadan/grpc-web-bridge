#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="AuthenticationContext"/> instances.
/// This is a static utility class and cannot be inherited.
/// </summary>
public static class AuthenticationContextJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Converts an <see cref="AuthenticationContext"/> instance to its JSON representation.
    /// </summary>
    /// <param name="value">The authentication context to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the authentication context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The <see cref="AuthenticationContext.Token"/> property is intentionally excluded from the JSON output.
    /// </remarks>
    public static string ToJson(this AuthenticationContext value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;

        var projection = new
        {
            value.Id,
            value.Scheme,
            value.UserId,
            value.Username,
            value.Roles,
            value.Claims,
            value.ExpiresAt,
            value.AuthenticatedAt,
            value.IsAuthenticated,
            value.IsExpired,
            value.IpAddress,
            value.CustomData
        };

        return JsonSerializer.Serialize(projection, options);
    }
}
