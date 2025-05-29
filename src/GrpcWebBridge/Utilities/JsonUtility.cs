// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// JSON serialization and deserialization utilities.
/// Provides consistent JSON handling across the application.
/// Handles type conversions, null-safety, and schema validation.
/// </summary>
public static class JsonUtility
{
    private static readonly JsonSerializerOptions DefaultOptions = CreateDefaultOptions();

    /// <summary>
    /// Creates default JSON serialization options.
    /// Configures handling of null values, property naming, and custom converters.
    /// </summary>
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        opts.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        opts.Converters.Add(new JsonDateTimeConverter());
        return opts;
    }

    /// <summary>
    /// Serialize an object to JSON string.
    /// Handles null objects gracefully.
    /// </summary>
    public static string Serialize<T>(T obj, bool indented = false)
    {
        if (obj == null)
            return "null";

        var options = indented ? CreateIndentedOptions() : DefaultOptions;
        return JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Serialize an object to JSON string with custom options.
    /// </summary>
    public static string SerializeWithOptions<T>(T obj, JsonSerializerOptions options)
    {
        return obj == null ? "null" : JsonSerializer.Serialize(obj, options);
    }

    /// <summary>
    /// Deserialize JSON string to specified type.
    /// Throws JsonException if deserialization fails.
    /// </summary>
    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON to type {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserialize JSON string to dynamic object.
    /// Returns Dictionary<string, object> for flexible property access.
    /// </summary>
    public static Dictionary<string, object>? DeserializeToDictionary(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json, DefaultOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize JSON to dictionary: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Try to deserialize JSON with error handling.
    /// Returns false if deserialization fails, true on success.
    /// </summary>
    public static bool TryDeserialize<T>(string json, out T? result, out string? errorMessage)
    {
        result = default;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            errorMessage = "JSON string cannot be null or empty";
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            return true;
        }
        catch (JsonException ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// Merge two JSON objects.
    /// Properties from source override properties in target.
    /// </summary>
    public static string MergeJson(string targetJson, string sourceJson)
    {
        try
        {
            var targetDict = DeserializeToDictionary(targetJson) ?? new Dictionary<string, object>();
            var sourceDict = DeserializeToDictionary(sourceJson) ?? new Dictionary<string, object>();

            foreach (var kvp in sourceDict)
            {
                targetDict[kvp.Key] = kvp.Value;
            }

            return Serialize(targetDict);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to merge JSON objects", ex);
        }
    }

    /// <summary>
    /// Extract a specific property from JSON object.
    /// Returns null if property doesn't exist.
    /// </summary>
    public static object? GetPropertyValue(string json, string propertyPath)
    {
        try
        {
            var jsonElement = JsonDocument.Parse(json).RootElement;
            var parts = propertyPath.Split('.');

            foreach (var part in parts)
            {
                if (!jsonElement.TryGetProperty(part, out var nextElement))
                    return null;

                jsonElement = nextElement;
            }

            return jsonElement.GetRawText();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Set a property value in a JSON object.
    /// Creates the property path if it doesn't exist.
    /// </summary>
    public static string SetPropertyValue(string json, string propertyPath, object value)
    {
        try
        {
            var dict = DeserializeToDictionary(json) ?? new Dictionary<string, object>();
            var parts = propertyPath.Split('.');

            // Navigate to the parent object, creating dictionaries as needed
            Dictionary<string, object> current = dict;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!current.ContainsKey(parts[i]))
                {
                    current[parts[i]] = new Dictionary<string, object>();
                }

                current = (Dictionary<string, object>)current[parts[i]];
            }

            current[parts[^1]] = value;
            return Serialize(dict);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set property '{propertyPath}'", ex);
        }
    }

    /// <summary>
    /// Validate JSON schema compliance (basic validation).
    /// Checks if required properties are present.
    /// </summary>
    public static bool ValidateRequired(string json, params string[] requiredProperties)
    {
        try
        {
            var dict = DeserializeToDictionary(json);
            if (dict == null)
                return false;

            return requiredProperties.All(p => dict.ContainsKey(p) && dict[p] != null);
        }
        catch
        {
            return false;
        }
    }

    private static JsonSerializerOptions CreateIndentedOptions()
    {
        var options = CreateDefaultOptions();
        options.WriteIndented = true;
        return options;
    }
}

/// <summary>
/// Custom JSON converter for DateTime objects.
/// Uses ISO 8601 format for consistency.
/// </summary>
public class JsonDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var stringValue = reader.GetString();
        return DateTime.Parse(stringValue ?? DateTime.UtcNow.ToString("O"), null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString("O"));
    }
}
