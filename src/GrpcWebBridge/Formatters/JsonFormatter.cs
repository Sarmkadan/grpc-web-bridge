#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Formatters;

/// <summary>
/// Specialized JSON formatting with customization options.
/// Provides formatting, pretty-printing, and schema enforcement.
/// </summary>
public sealed class JsonFormatter : IEquatable<JsonFormatter>
{
    private readonly JsonFormatterOptions _options;

    public JsonFormatter(JsonFormatterOptions? options = null)
    {
        _options = options ?? new JsonFormatterOptions();
    }

    public bool Equals(JsonFormatter? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _options.PrettyPrint == other._options.PrettyPrint &&
               _options.SortKeys == other._options.SortKeys &&
               _options.MaxDepth == other._options.MaxDepth &&
               _options.IncludeNullValues == other._options.IncludeNullValues;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as JsonFormatter);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_options.PrettyPrint, _options.SortKeys, _options.MaxDepth, _options.IncludeNullValues);
    }

    public static bool operator ==(JsonFormatter? left, JsonFormatter? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(JsonFormatter? left, JsonFormatter? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Formats an object as JSON string.
    /// </summary>
    public string Format<T>(T obj) where T : class
    {
        return JsonUtility.Serialize(obj, _options.PrettyPrint);
    }

    /// <summary>
    /// Formats JSON with custom sorting of keys.
    /// Useful for consistency and readability.
    /// </summary>
    public string FormatWithSortedKeys<T>(T obj) where T : class
    {
        var json = JsonUtility.Serialize(obj);
        var dict = JsonUtility.DeserializeToDictionary(json);

        if (dict is null)
            return json;

        var sorted = new SortedDictionary<string, object?>(dict, StringComparer.Ordinal);
        return JsonUtility.Serialize(sorted, _options.PrettyPrint);
    }

    /// <summary>
    /// Minifies JSON by removing unnecessary whitespace.
    /// </summary>
    public static string Minify(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        var dict = JsonUtility.DeserializeToDictionary(json);
        return JsonUtility.Serialize(dict, indented: false);
    }

    /// <summary>
    /// Pretty-prints JSON with consistent indentation.
    /// </summary>
    public static string PrettyPrint(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        var dict = JsonUtility.DeserializeToDictionary(json);
        return JsonUtility.Serialize(dict, indented: true);
    }

    /// <summary>
    /// Formats JSON for API documentation.
    /// Includes type information and examples.
    /// </summary>
    public string FormatForDocumentation(object obj, string title = "", string description = "")
    {
        var data = new
        {
            title = title,
            description = description,
            schema = new
            {
                type = "object",
                properties = GetPropertyDescriptions(obj)
            },
            example = obj
        };

        return JsonUtility.Serialize(data, _options.PrettyPrint);
    }

    /// <summary>
    /// Validates JSON structure against requirements.
    /// </summary>
    public (bool Valid, List<string> Errors) Validate(string json, string[] requiredFields)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(json))
        {
            errors.Add("JSON cannot be null or empty");
            return (false, errors);
        }

        if (!JsonUtility.TryDeserialize<Dictionary<string, object>>(json, out var dict, out var error))
        {
            errors.Add($"Invalid JSON: {error}");
            return (false, errors);
        }

        if (dict is null)
        {
            errors.Add("Failed to parse JSON");
            return (false, errors);
        }

        foreach (var field in requiredFields)
        {
            if (!dict.ContainsKey(field) || dict[field] is null)
            {
                errors.Add($"Required field missing: {field}");
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Compares two JSON objects for equality.
    /// </summary>
    public bool AreEqual(string json1, string json2)
    {
        try
        {
            var dict1 = JsonUtility.DeserializeToDictionary(json1);
            var dict2 = JsonUtility.DeserializeToDictionary(json2);

            return AreObjectsEqual(dict1, dict2);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts specific fields from JSON.
    /// Useful for data extraction and filtering.
    /// </summary>
    public Dictionary<string, object?> ExtractFields(string json, params string[] fieldNames)
    {
        var result = new Dictionary<string, object?>();

        var dict = JsonUtility.DeserializeToDictionary(json);
        if (dict is null)
            return result;

        foreach (var field in fieldNames)
        {
            if (dict.TryGetValue(field, out var value))
            {
                result[field] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Flattens nested JSON object into flat structure.
    /// </summary>
    public Dictionary<string, object?> Flatten(string json, string separator = ".")
    {
        var dict = JsonUtility.DeserializeToDictionary(json) ?? new Dictionary<string, object>();
        var flat = new Dictionary<string, object?>();

        FlattenObject(dict, "", flat, separator);
        return flat;
    }

    /// <summary>
    /// Converts flat structure back to nested object.
    /// </summary>
    public Dictionary<string, object?> Unflatten(Dictionary<string, object?> flat, string separator = ".")
    {
        var result = new Dictionary<string, object?>();

        foreach (var kvp in flat)
        {
            var parts = kvp.Key.Split(separator);
            Dictionary<string, object?> current = result;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                if (!current.ContainsKey(part))
                {
                    current[part] = new Dictionary<string, object?>();
                }

                current = (Dictionary<string, object?>)current[part]!;
            }

            current[parts[^1]] = kvp.Value;
        }

        return result;
    }

    private Dictionary<string, object> GetPropertyDescriptions(object obj)
    {
        var descriptions = new Dictionary<string, object>();

        if (obj is null)
            return descriptions;

        var properties = obj.GetType().GetProperties();
        foreach (var prop in properties)
        {
            descriptions[prop.Name] = new
            {
                type = prop.PropertyType.Name,
                value = prop.GetValue(obj)
            };
        }

        return descriptions;
    }

    private bool AreObjectsEqual(object? obj1, object? obj2)
    {
        if (obj1 is null && obj2 is null)
            return true;

        if (obj1 is null || obj2 is null)
            return false;

        if (obj1 is Dictionary<string, object?> dict1 && obj2 is Dictionary<string, object?> dict2)
        {
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var value2))
                    return false;

                if (!AreObjectsEqual(kvp.Value, value2))
                    return false;
            }

            return true;
        }

        return Equals(obj1, obj2);
    }

    private void FlattenObject(Dictionary<string, object?> dict, string prefix, Dictionary<string, object?> result, string separator)
    {
        foreach (var kvp in dict)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}{separator}{kvp.Key}";

            if (kvp.Value is Dictionary<string, object?> nested)
            {
                FlattenObject(nested, key, result, separator);
            }
            else
            {
                result[key] = kvp.Value;
            }
        }
    }
}

/// <summary>
/// Configuration options for JSON formatter.
/// </summary>
public sealed class JsonFormatterOptions
{
    public bool PrettyPrint { get; set; } = false;
    public bool SortKeys { get; set; } = false;
    public int MaxDepth { get; set; } = 10;
    public bool IncludeNullValues { get; set; } = false;
}
