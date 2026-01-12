#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="GrpcRequest"/> instances
/// </summary>
public static class GrpcRequestJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            {
                Modifiers = {
                    static typeInfo =>
                    {
                        if (typeInfo.Type == typeof(GrpcRequest))
                        {
                            foreach (var property in typeInfo.Properties)
                            {
                                property.Name = property.Name switch
                                {
                                    "Id" => "id",
                                    "ServiceName" => "serviceName",
                                    "MethodName" => "methodName",
                                    "FullMethodName" => "fullMethodName",
                                    "Payload" => "payload",
                                    "PayloadFormat" => "payloadFormat",
                                    "Metadata" => "metadata",
                                    "RequestId" => "requestId",
                                    "TraceId" => "traceId",
                                    "UserId" => "userId",
                                    "CreatedAt" => "createdAt",
                                    "TimeoutMilliseconds" => "timeoutMilliseconds",
                                    "MethodType" => "methodType",
                                    _ => property.Name
                                };
                            }
                        }
                    }
                }
            }
    };

    private static readonly JsonSerializerOptions _jsonSerializerOptionsWithEnumConverter = new(_jsonSerializerOptions)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Converts a <see cref="GrpcRequest"/> instance to its JSON representation
    /// </summary>
    /// <param name="value">The request to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability</param>
    /// <returns>A JSON string representation of the request</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static string ToJson(this GrpcRequest value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptionsWithEnumConverter)
            {
                WriteIndented = true
            }
            : _jsonSerializerOptionsWithEnumConverter;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Parses a JSON string and creates a <see cref="GrpcRequest"/> instance
    /// </summary>
    /// <param name="json">The JSON string to parse</param>
    /// <returns>A deserialized <see cref="GrpcRequest"/> instance, or null if parsing fails</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty</exception>
    public static GrpcRequest? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<GrpcRequest>(json, _jsonSerializerOptionsWithEnumConverter);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to parse a JSON string and create a <see cref="GrpcRequest"/> instance
    /// </summary>
    /// <param name="json">The JSON string to parse</param>
    /// <param name="value">Receives the deserialized instance if successful</param>
    /// <returns>True if parsing succeeded; otherwise, false</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty</exception>
    public static bool TryFromJson(string json, out GrpcRequest? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<GrpcRequest>(json, _jsonSerializerOptionsWithEnumConverter);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
