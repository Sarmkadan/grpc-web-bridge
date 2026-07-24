#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for <see cref="RequestContextManager"/>.
/// </summary>
public static class RequestContextManagerJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Sanitizes metadata values to prevent log injection attacks.
    /// Removes control characters and newlines from all metadata values.
    /// </summary>
    /// <param name="context">The request context to sanitize.</param>
    /// <returns>A new RequestContext with sanitized metadata, or the original if no sanitization needed.</returns>
    private static RequestContext SanitizeContextForSerialization(RequestContext context)
    {
        if (context.Metadata.Count == 0)
            return context;

        var sanitizedMetadata = new Dictionary<string, string>(context.Metadata.Count, StringComparer.Ordinal);
        var changed = false;

        foreach (var kvp in context.Metadata)
        {
            // Sanitize the value by removing control characters and newlines
            var sanitizedValue = new char[kvp.Value.Length];
            var length = 0;

            for (var i = 0; i < kvp.Value.Length; i++)
            {
                var c = kvp.Value[i];
                // Remove control characters (0x00-0x1F, 0x7F-0x9F) and newlines
                if (c >= ' ' && c != '\n' && c != '\r' && c <= '~')
                {
                    sanitizedValue[length++] = c;
                }
                else
                {
                    changed = true;
                }
            }

            var result = new string(sanitizedValue, 0, length);
            sanitizedMetadata[kvp.Key] = result;
        }

        if (!changed)
            return context;

        return new RequestContext
        {
            RequestId = context.RequestId,
            UserId = context.UserId,
            StartTime = context.StartTime,
            EndTime = context.EndTime,
            Metadata = sanitizedMetadata
        };
    }

    /// <summary>
    /// Serializes the <see cref="RequestContext"/> to a JSON string.
    /// </summary>
    /// <param name="value">The request context to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the request context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this RequestContextManager value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var context = value.GetContext();
        if (context is null)
            return "{}";

        // Sanitize metadata to prevent log injection
        var sanitizedContext = SanitizeContextForSerialization(context);
        return JsonSerializer.Serialize(sanitizedContext, indented ? _jsonOptionsIndented : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="RequestContext"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="RequestContext"/> instance, or null if the JSON is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized.</exception>
    public static RequestContext? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<RequestContext>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="RequestContext"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized <see cref="RequestContext"/> if successful; otherwise, null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out RequestContext? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<RequestContext>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
