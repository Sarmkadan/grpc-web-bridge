using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrpcWebBridge.Domain.Exceptions
{
    /// <summary>
    /// Provides JSON serialization and deserialization extensions for <see cref="StreamingException"/>.
    /// </summary>
    /// <remarks>
    /// This static class offers methods to convert <see cref="StreamingException"/> instances to JSON strings
    /// and parse JSON strings back into <see cref="StreamingException"/> objects using System.Text.Json.
    /// All serialization uses camelCase property naming policy and ignores null values by default.
    /// </remarks>
    public static class StreamingExceptionJsonExtensions
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Serializes the <see cref="StreamingException"/> to a JSON string.
        /// </summary>
        /// <param name="value">The exception to serialize.</param>
        /// <param name="indented">Whether to format the JSON with indentation.</param>
        /// <returns>A JSON string representation of the exception.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static string ToJson(this StreamingException value, bool indented = false)
        {
            ArgumentNullException.ThrowIfNull(value);

            var options = indented
                ? new JsonSerializerOptions(_jsonSerializerOptions)
                {
                    WriteIndented = true
                }
                : _jsonSerializerOptions;

            return JsonSerializer.Serialize(value, options);
        }

        /// <summary>
        /// Deserializes a JSON string to a <see cref="StreamingException"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>The deserialized exception, or null if the JSON is null or empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
        public static StreamingException? FromJson(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<StreamingException>(json, _jsonSerializerOptions);
        }

        /// <summary>
        /// Attempts to deserialize a JSON string to a <see cref="StreamingException"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="value">The deserialized exception, or null if deserialization failed.</param>
        /// <returns>True if deserialization succeeded; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        public static bool TryFromJson(string json, out StreamingException? value)
        {
            ArgumentNullException.ThrowIfNull(json);

            value = null;

            if (string.IsNullOrEmpty(json))
            {
                return true;
            }

            try
            {
                value = JsonSerializer.Deserialize<StreamingException>(json, _jsonSerializerOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}