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
/// Provides System.Text.Json serialization extensions for <see cref="GrpcMethod"/> instances.
/// This is a static utility class and cannot be inherited.
/// </summary>
public static class GrpcMethodJsonExtensions
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
                    if (typeInfo.Type == typeof(GrpcMethod))
                    {
                        foreach (var property in typeInfo.Properties)
                        {
                            property.Name = property.Name switch
                            {
                                "Name" => "name",
                                "FullName" => "fullName",
                                "Type" => "type",
                                "InputMessageType" => "inputMessageType",
                                "OutputMessageType" => "outputMessageType",
                                "IsDeprecated" => "isDeprecated",
                                "Description" => "description",
                                "TimeoutMilliseconds" => "timeoutMilliseconds",
                                "CreatedAt" => "createdAt",
                                "UpdatedAt" => "updatedAt",
                                "InputParameters" => "inputParameters",
                                "OutputParameters" => "outputParameters",
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
    /// Converts a <see cref="GrpcMethod"/> instance to its JSON representation.
    /// </summary>
    /// <param name="value">The method to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the method.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this GrpcMethod value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptionsWithEnumConverter) { WriteIndented = true }
            : _jsonSerializerOptionsWithEnumConverter;

        return JsonSerializer.Serialize(value, options);
    }
}
