// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a single parameter in a gRPC method signature
/// </summary>
public class MethodParameter
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsRepeated { get; set; }
    public int FieldNumber { get; set; }
    public SerializationFormat Format { get; set; } = SerializationFormat.Protobuf;

    public MethodParameter() { }

    public MethodParameter(string name, string typeName, int fieldNumber, bool isRequired = true)
    {
        Name = ValidateName(name);
        TypeName = ValidateTypeName(typeName);
        FieldNumber = ValidateFieldNumber(fieldNumber);
        IsRequired = isRequired;
    }

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

    public override bool Equals(object? obj)
    {
        if (obj is not MethodParameter other)
            return false;

        return Name == other.Name && TypeName == other.TypeName && FieldNumber == other.FieldNumber;
    }

    public override int GetHashCode() => HashCode.Combine(Name, TypeName, FieldNumber);
}
