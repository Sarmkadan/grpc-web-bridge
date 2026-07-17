#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="BridgeConfiguration"/>.
/// </summary>
public static class BridgeConfigurationJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		WriteIndented = false,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	/// <summary>
	/// Serializes the <see cref="BridgeConfiguration"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The configuration to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the configuration.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this BridgeConfiguration value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string into a <see cref="BridgeConfiguration"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized configuration, or null if the JSON is invalid or malformed.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null, empty, or whitespace.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is malformed and cannot be parsed.</exception>
	public static BridgeConfiguration? FromJson(string json)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		try
		{
			return JsonSerializer.Deserialize<BridgeConfiguration>(json, _jsonOptions);
		}
		catch (JsonException ex)
		{
			throw new JsonException("Failed to deserialize BridgeConfiguration from JSON", ex);
		}
	}

	/// <summary>
	/// Attempts to deserialize a JSON string into a <see cref="BridgeConfiguration"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized configuration if successful; otherwise, null.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null, empty, or whitespace.</exception>
	public static bool TryFromJson(string json, out BridgeConfiguration? value)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		try
		{
			value = JsonSerializer.Deserialize<BridgeConfiguration>(json, _jsonOptions);
			return value is not null;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}