#nullable enable

using System.Text.Json;
using GrpcWebBridge.Streaming;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Provides System.Text.Json serialization helpers for <see cref="FlowControlOptionsEdgeCaseTests"/>.
/// </summary>
public static class FlowControlOptionsEdgeCaseTestsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static JsonSerializerOptions GetJsonOptions(bool indented)
    {
        var options = new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = indented
        };
        return options;
    }

    /// <summary>
    /// Serializes the <see cref="FlowControlOptionsEdgeCaseTests"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this FlowControlOptionsEdgeCaseTests value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, GetJsonOptions(indented));
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="FlowControlOptionsEdgeCaseTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">The JSON is invalid or cannot be deserialized.</exception>
    public static FlowControlOptionsEdgeCaseTests? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<FlowControlOptionsEdgeCaseTests>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="FlowControlOptionsEdgeCaseTests"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromJson(string json, out FlowControlOptionsEdgeCaseTests? value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            value = JsonSerializer.Deserialize<FlowControlOptionsEdgeCaseTests>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}