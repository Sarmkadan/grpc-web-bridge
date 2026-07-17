#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Provides JSON serialization and deserialization extension methods for
/// <see cref="ContentTypeValidationMiddleware"/> using System.Text.Json.
/// </summary>
public static class ContentTypeValidationMiddlewareJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		WriteIndented = false,
	};

	/// <summary>
	/// Serializes the <see cref="ContentTypeValidationMiddleware"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The middleware instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the middleware.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this ContentTypeValidationMiddleware value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="ContentTypeValidationMiddleware"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized middleware instance, or null if the JSON is empty or whitespace.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static ContentTypeValidationMiddleware? FromJson(string json) =>
		string.IsNullOrWhiteSpace(json)
			? null
			: JsonSerializer.Deserialize<ContentTypeValidationMiddleware>(json, _jsonOptions);

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="ContentTypeValidationMiddleware"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized middleware instance if successful.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	public static bool TryFromJson(string json, out ContentTypeValidationMiddleware? value)
	{
		value = null;

		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}

		try
		{
			value = JsonSerializer.Deserialize<ContentTypeValidationMiddleware>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}
}
