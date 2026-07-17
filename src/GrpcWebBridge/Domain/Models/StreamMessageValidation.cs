#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Provides validation helpers for <see cref="StreamMessage"/> instances
/// </summary>
public static class StreamMessageValidation
{
	/// <summary>
	/// Determines whether the specified compression level is valid (between 0 and 9 inclusive).
	/// </summary>
	/// <param name="compressionLevel">The compression level to validate</param>
	/// <returns>True if the compression level is valid; otherwise, false</returns>
	private static bool IsValidCompressionLevel(int? compressionLevel)
	{
		return compressionLevel is not null && compressionLevel >= 0 && compressionLevel <= 9;
	}

	/// <summary>
	/// Validates the specified <see cref="StreamMessage"/> and returns a list of validation errors.
	/// </summary>
	/// <param name="value">The stream message to validate</param>
	/// <returns>An empty list if valid; otherwise, a list of human-readable error messages</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
	public static IReadOnlyList<string> Validate(this StreamMessage? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var errors = new List<string>();

		// Validate Id
		if (string.IsNullOrWhiteSpace(value.Id))
		{
			errors.Add("Message ID cannot be null or whitespace");
		}

		// Validate StreamId
		if (string.IsNullOrWhiteSpace(value.StreamId))
		{
			errors.Add("Stream ID cannot be null or whitespace");
		}

		// Validate MessageType
		if (!Enum.IsDefined(value.MessageType))
		{
			errors.Add($"Message type '{value.MessageType}' is not a valid StreamMessageType value");
		}

		// Validate SequenceNumber
		if (value.SequenceNumber < 0)
		{
			errors.Add("Sequence number cannot be negative");
		}

		// Validate Data based on message type
		if (value.MessageType == StreamMessageType.Data && value.Data.Length == 0)
		{
			errors.Add("Data message must contain non-empty data");
		}

		// Validate Format
		if (!Enum.IsDefined(value.Format))
		{
			errors.Add($"Serialization format '{value.Format}' is not a valid SerializationFormat value");
		}

		// Validate Headers
		if (value.Headers is { } headers)
		{
			foreach (var kvp in headers)
			{
				if (string.IsNullOrWhiteSpace(kvp.Key))
				{
					errors.Add("Header key cannot be null or whitespace");
					break;
				}

				if (kvp.Value is null)
				{
					errors.Add($"Header value for key '{kvp.Key}' cannot be null");
					break;
				}
			}
		}

		// Validate Status
		if (value.Status.HasValue && !Enum.IsDefined(value.Status.Value))
		{
			errors.Add($"Status code '{value.Status}' is not a valid GrpcStatusCode value");
		}

		// Validate StatusMessage
		if (value.StatusMessage is not null && string.IsNullOrWhiteSpace(value.StatusMessage))
		{
			errors.Add("Status message cannot be empty or whitespace");
		}

		// Validate CreatedAt
		if (value.CreatedAt == default)
		{
			errors.Add("CreatedAt must be set to a non-default DateTime value");
		}

		// Validate IsCompressed and CompressionLevel
		if (value.IsCompressed)
		{
			if (!value.CompressionLevel.HasValue)
			{
				errors.Add("Compression level must be set when IsCompressed is true");
			}
			else if (!IsValidCompressionLevel(value.CompressionLevel))
			{
				errors.Add("Compression level must be between 0 and 9 inclusive");
			}
		}

		// Validate Data size
		if (value.Data.Length > Constants.Streaming.DefaultBufferSize * 2)
		{
			errors.Add($"Message data exceeds maximum size of {Constants.Streaming.DefaultBufferSize * 2} bytes");
		}

		// Validate ErrorResponse
		if (value.MessageType == StreamMessageType.Error && value.ErrorResponse is null)
		{
			errors.Add("Error message type requires a non-null ErrorResponse");
		}

		return errors.AsReadOnly();
	}

	/// <summary>
	/// Determines whether the specified <see cref="StreamMessage"/> is valid.
	/// </summary>
	/// <param name="value">The stream message to check</param>
	/// <returns>True if the message is valid; otherwise, false</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
	public static bool IsValid(this StreamMessage? value)
		=> value is not null && Validate(value).Count == 0;

	/// <summary>
	/// Ensures that the specified <see cref="StreamMessage"/> is valid, throwing an exception if it is not.
	/// </summary>
	/// <param name="value">The stream message to validate</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
	/// <exception cref="ArgumentException">Thrown if the message is invalid, containing a list of validation errors</exception>
	public static void EnsureValid(this StreamMessage? value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var errors = Validate(value);
		if (errors.Count > 0)
		{
			throw new ArgumentException($"StreamMessage is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", errors)}");
		}
	}
}