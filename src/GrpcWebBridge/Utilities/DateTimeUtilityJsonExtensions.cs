#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for date/time values
/// compatible with DateTimeUtility operations.
/// </summary>
public static class DateTimeUtilityJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Gets the appropriate JsonSerializerOptions based on the indented flag.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>Configured JsonSerializerOptions.</returns>
    private static JsonSerializerOptions GetOptions(bool indented) =>
        indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;

    /// <summary>
    /// Serializes a DateTime to a JSON string using ISO 8601 format.
    /// </summary>
    /// <param name="value">The DateTime value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the DateTime value.</returns>
    public static string ToJson(this DateTime value, bool indented = false)
        => JsonSerializer.Serialize(value, GetOptions(indented));

    /// <summary>
    /// Deserializes a JSON string to a DateTime value.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized DateTime value, or null if parsing fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static DateTime? FromJson(this string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<DateTime>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a DateTime value.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized DateTime value if successful.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(this string json, out DateTime? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<DateTime>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a DateTime? to a JSON string using ISO 8601 format.
    /// </summary>
    /// <param name="value">The nullable DateTime value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the DateTime? value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this DateTime? value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, GetOptions(indented));
    }

    /// <summary>
    /// Serializes a DateTimePeriod enum value to a JSON string.
    /// </summary>
    /// <param name="value">The DateTimePeriod value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the DateTimePeriod value.</returns>
    public static string ToJson(this DateTimePeriod value, bool indented = false)
        => JsonSerializer.Serialize(value, GetOptions(indented));
}