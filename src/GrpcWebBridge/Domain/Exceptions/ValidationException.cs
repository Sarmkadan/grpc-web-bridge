#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Exception thrown when validation of input data fails
/// </summary>
public class ValidationException : GrpcWebBridgeException
{
    public string? FieldName { get; set; }
    public object? InvalidValue { get; set; }
    public string? ValidationRule { get; set; }

    public ValidationException() : base() { }

    public ValidationException(string message) : base(message, "VALIDATION_ERROR")
    {
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    public ValidationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "VALIDATION_ERROR";
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    public ValidationException(string fieldName, object? invalidValue, string validationRule, string message)
        : base($"Validation failed for '{fieldName}': {message} (Value: {invalidValue}, Rule: {validationRule})", "VALIDATION_FAILED")
    {
        FieldName = fieldName;
        InvalidValue = invalidValue;
        ValidationRule = validationRule;
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    public override string ToString()
    {
        var result = base.ToString();
        if (!string.IsNullOrEmpty(FieldName))
            result += $" | Field: {FieldName}";

        if (InvalidValue != null)
            result += $" | Value: {InvalidValue}";

        if (!string.IsNullOrEmpty(ValidationRule))
            result += $" | Rule: {ValidationRule}";

        return result;
    }
}
