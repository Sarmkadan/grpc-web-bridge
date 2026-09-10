#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a single parameter in a gRPC method signature
/// </summary>
public sealed class MethodParameter
{
    /// <summary>Represents the name of the parameter.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Represents the type name of the parameter.</summary>
    public string TypeName { get; set; } = string.Empty;
    /// <summary>Represents the description of the parameter.</summary>
    public string? Description { get; set; }
    /// <summary>Indicates whether the parameter is required.</summary>
    public bool IsRequired { get; set; }
    /// <summary>Indicates whether the parameter is repeated.</summary>
    public bool IsRepeated { get; set; }
    /// <summary>Represents the field number of the parameter in the protobuf schema.</summary>
    public int FieldNumber { get; set; }
    /// <summary>Represents the serialization format of the parameter.</summary>
    public SerializationFormat Format { get; set; } = SerializationFormat.Protobuf;

    /// <summary>Initializes a new instance of the MethodParameter class.</summary>
    public MethodParameter() { }

    /// <summary>Initializes a new instance of the MethodParameter class with the specified name, type name, field number, and requirement.</summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="typeName">The type name of the parameter.</param>
    /// <param name="fieldNumber">The field number of the parameter.</param>
    /// <param name="isRequired">Indicates whether the parameter is required. Defaults to true.</param>
    public MethodParameter(string name, string typeName, int fieldNumber, bool isRequired = true)
    {
        Name = ValidateName(name);
        TypeName = ValidateTypeName(typeName);
        FieldNumber = ValidateFieldNumber(fieldNumber);
        IsRequired = isRequired;
    }

    /// <summary>Validates the parameter properties.</summary>
    /// <exception cref="ArgumentException">Thrown when the parameter name is empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the parameter type name is empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the field number is less than or equal to zero.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Parameter name cannot be empty", nameof(Name));

        if (string.IsNullOrWhiteSpace(TypeName))
            throw new ArgumentException("Parameter type name cannot be empty", nameof(TypeName));

        if (FieldNumber <= 0)
            throw new ArgumentException("Field number must be greater than 0", nameof(FieldNumber));
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Parameter name cannot be empty", nameof(name));
        return name.Trim();
    }

    private static string ValidateTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be empty", nameof(typeName));
        return typeName.Trim();
    }

    private static int ValidateFieldNumber(int fieldNumber)
    {
        if (fieldNumber <= 0 || fieldNumber > 536870911)
            throw new ArgumentException("Field number must be between 1 and 536870911", nameof(fieldNumber));
        return fieldNumber;
    }

    public override string ToString() => $"{Name}: {TypeName} (field {FieldNumber})";

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not MethodParameter other)
            return false;

        return Name == other.Name && TypeName == other.TypeName && FieldNumber == other.FieldNumber;
    }

    /// <summary>Serves as the default hash function.</summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => HashCode.Combine(Name, TypeName, FieldNumber);
}