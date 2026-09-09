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
    /// <summary>
    /// Gets or sets the name of the field that failed validation.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Gets or sets the invalid value that caused the validation failure.
    /// </summary>
    public object? InvalidValue { get; set; }

    /// <summary>
    /// Gets or sets the validation rule that was violated.
    /// </summary>
    public string? ValidationRule { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    public ValidationException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ValidationException(string message) : base(message, "VALIDATION_ERROR")
    {
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ValidationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "VALIDATION_ERROR";
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with the field name, invalid value, validation rule, and a specified error message.
    /// </summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="invalidValue">The invalid value that caused the validation failure.</param>
    /// <param name="validationRule">The validation rule that was violated.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
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
