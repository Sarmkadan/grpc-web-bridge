#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Google.Protobuf;
using System.Text.Json;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// Protobuf message handling utilities.
/// Provides conversion between Protobuf, JSON, and other formats.
/// Handles message serialization, deserialization, and introspection.
/// </summary>
public static class ProtobufUtility
{
    /// <summary>
    /// Converts a Protobuf message to JSON string.
    /// Uses JsonFormatter for standard JSON serialization.
    /// </summary>
    public static string ToJson(IMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var formatter = new JsonFormatter(JsonFormatter.Settings.Default);
        return formatter.Format(message);
    }

    /// <summary>
    /// Parses a JSON string into a Protobuf message.
    /// Uses JsonParser for standard JSON deserialization.
    /// </summary>
    public static T? FromJson<T>(string json) where T : IMessage, new()
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException("JSON string cannot be null or empty", nameof(json));

        try
        {
            var parser = new JsonParser(JsonParser.Settings.Default);
            var message = new T();
            return (T)parser.Parse(json, message.Descriptor);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse JSON to {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Converts a Protobuf message to byte array using binary serialization.
    /// </summary>
    public static byte[] ToBytes(IMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        return message.ToByteArray();
    }

    /// <summary>
    /// Parses a byte array into a Protobuf message.
    /// </summary>
    public static T? FromBytes<T>(byte[] data) where T : IMessage, new()
    {
        if (data is null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        try
        {
            var message = new T();
            message.MergeFrom(data);
            return message;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to parse bytes to {typeof(T).Name}", ex);
        }
    }

    /// <summary>
    /// Gets the size of a serialized Protobuf message in bytes.
    /// </summary>
    public static int GetMessageSize(IMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        return message.CalculateSize();
    }

    /// <summary>
    /// Converts a Protobuf message to a dictionary.
    /// Useful for inspection and serialization to other formats.
    /// </summary>
    public static Dictionary<string, object?> ToDict(IMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var json = ToJson(message);
        var dict = JsonUtility.DeserializeToDictionary(json);
        return dict ?? new Dictionary<string, object?>();
    }

    /// <summary>
    /// Clones a Protobuf message.
    /// Creates a deep copy of the entire message.
    /// </summary>
    public static T Clone<T>(T message) where T : IMessage, new()
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var clone = new T();
        clone.MergeFrom(message.ToByteArray());
        return clone;
    }

    /// <summary>
    /// Merges multiple Protobuf messages.
    /// Later messages override fields from earlier messages.
    /// </summary>
    public static T Merge<T>(params T[] messages) where T : IMessage, new()
    {
        if (messages is null || messages.Length == 0)
            throw new ArgumentException("At least one message is required", nameof(messages));

        var result = new T();
        foreach (var message in messages)
        {
            if (message is not null)
                result.MergeFrom(message.ToByteArray());
        }

        return result;
    }

    /// <summary>
    /// Checks if two Protobuf messages are equal.
    /// Compares all fields deeply.
    /// </summary>
    public static bool AreEqual<T>(T message1, T message2) where T : IMessage
    {
        if (message1 is null && message2 is null)
            return true;

        if (message1 is null || message2 is null)
            return false;

        return message1.Equals(message2);
    }

    /// <summary>
    /// Validates a Protobuf message against a schema.
    /// Checks required fields and type constraints.
    /// </summary>
    public static (bool Valid, List<string> Errors) Validate(IMessage message)
    {
        var errors = new List<string>();

        if (message is null)
        {
            errors.Add("Message cannot be null");
            return (false, errors);
        }

        var descriptor = message.Descriptor;
        if (descriptor is null)
        {
            errors.Add("Message descriptor is null");
            return (false, errors);
        }

        // Check required fields
        foreach (var field in descriptor.Fields.InDeclarationOrder())
        {
            if (field.IsRequired)
            {
                var value = message.GetType().GetProperty(field.Name)?.GetValue(message);
                if (value is null || (value is string && string.IsNullOrEmpty((string)value)))
                {
                    errors.Add($"Required field '{field.Name}' is missing or empty");
                }
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Compresses a Protobuf message using gzip.
    /// Returns Base64-encoded compressed data.
    /// </summary>
    public static string CompressMessage(IMessage message)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));

        var messageBytes = message.ToByteArray();
        using (var output = new MemoryStream())
        {
            using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
            {
                gzip.Write(messageBytes, 0, messageBytes.Length);
            }

            return Convert.ToBase64String(output.ToArray());
        }
    }

    /// <summary>
    /// Decompresses a gzip-compressed Protobuf message.
    /// </summary>
    public static T? DecompressMessage<T>(string compressedBase64) where T : IMessage, new()
    {
        if (string.IsNullOrEmpty(compressedBase64))
            throw new ArgumentException("Compressed data cannot be null or empty", nameof(compressedBase64));

        try
        {
            var compressedBytes = Convert.FromBase64String(compressedBase64);
            using (var input = new MemoryStream(compressedBytes))
            {
                using (var gzip = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress))
                {
                    var decompressed = new MemoryStream();
                    gzip.CopyTo(decompressed);
                    var decompressedBytes = decompressed.ToArray();

                    var message = new T();
                    message.MergeFrom(decompressedBytes);
                    return message;
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to decompress message", ex);
        }
    }

    /// <summary>
    /// Gets metadata about a Protobuf message type.
    /// Returns information about fields, nested types, etc.
    /// </summary>
    public static MessageMetadata GetMessageMetadata<T>() where T : IMessage, new()
    {
        var message = new T();
        var descriptor = message.Descriptor;

        return new MessageMetadata
        {
            Name = descriptor.Name,
            FullName = descriptor.FullName,
            FieldCount = descriptor.Fields.InDeclarationOrder().Count(),
            Fields = descriptor.Fields.InDeclarationOrder()
                .Select(f => new FieldMetadata
                {
                    Name = f.Name,
                    Type = f.FieldType.ToString(),
                    IsRequired = f.IsRequired,
                    IsRepeated = f.IsRepeated,
                    DefaultValue = null
                })
                .ToList()
        };
    }
}

/// <summary>
/// Metadata about a Protobuf message type.
/// </summary>
public sealed class MessageMetadata
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int FieldCount { get; set; }
    public List<FieldMetadata> Fields { get; set; } = new();
}

/// <summary>
/// Metadata about a Protobuf field.
/// </summary>
public sealed class FieldMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsRepeated { get; set; }
    public string? DefaultValue { get; set; }
}
