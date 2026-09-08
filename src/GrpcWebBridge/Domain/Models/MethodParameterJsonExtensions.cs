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
/// Provides System.Text.Json serialization extensions for <see cref="MethodParameter"/> instances.
/// This is a static utility class and cannot be inherited.
/// </summary>
public static class MethodParameterJsonExtensions
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
                    if (typeInfo.Type == typeof(MethodParameter))
                    {
                        foreach (var property in typeInfo.Properties)
                        {
                            property.Name = property.Name switch
                            {
                                "Name" => "name",
                                "TypeName" => "typeName",
                                "Description" => "description",
                                "IsRequired" => "isRequired",
                                "IsRepeated" => "isRepeated",
                                "FieldNumber" => "fieldNumber",
                                "Format" => "format",
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
    /// Converts a <see cref="MethodParameter"/> instance to its JSON representation.
    /// </summary>
    /// <param name="value">The method parameter to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the method parameter.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this MethodParameter value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptionsWithEnumConverter) { WriteIndented = true }
            : _jsonSerializerOptionsWithEnumConverter;

        return JsonSerializer.Serialize(value, options);
    }
}
