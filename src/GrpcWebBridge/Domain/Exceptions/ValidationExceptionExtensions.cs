#nullable enable

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="ValidationException"/> to enhance validation error handling and reporting
/// </summary>
public static class ValidationExceptionExtensions
{
    /// <summary>
    /// Creates a user-friendly error message from the validation exception
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <returns>A formatted error message suitable for API responses</returns>
    public static string ToErrorMessage(this ValidationException exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var message = exception.Message;

        if (!string.IsNullOrEmpty(exception.FieldName))
        {
            message = $"Validation failed for field '{exception.FieldName}': {message}";
        }

        if (exception.InvalidValue != null)
        {
            message += $" | Provided value: {exception.InvalidValue}";
        }

        if (!string.IsNullOrEmpty(exception.ValidationRule))
        {
            message += $" | Validation rule: {exception.ValidationRule}";
        }

        return message;
    }

    /// <summary>
    /// Checks if the validation exception represents a specific field validation failure
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <param name="fieldName">The field name to check against</param>
    /// <returns>True if the exception is for the specified field, otherwise false</returns>
    public static bool IsForField(this ValidationException exception, string fieldName)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        return string.Equals(exception.FieldName, fieldName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a simplified validation error object suitable for JSON serialization
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <returns>A dictionary containing the validation error details</returns>
    public static Dictionary<string, object?> ToErrorDetails(this ValidationException exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var details = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["message"] = exception.Message,
            ["errorCode"] = "VALIDATION_ERROR"
        };

        if (!string.IsNullOrEmpty(exception.FieldName))
        {
            details["field"] = exception.FieldName;
        }

        if (exception.InvalidValue != null)
        {
            details["value"] = exception.InvalidValue;
        }

        if (!string.IsNullOrEmpty(exception.ValidationRule))
        {
            details["rule"] = exception.ValidationRule;
        }

        return details;
    }

    /// <summary>
    /// Combines multiple validation exceptions into a single aggregated exception
    /// </summary>
    /// <param name="exceptions">Collection of validation exceptions</param>
    /// <returns>A new ValidationException containing all validation errors</returns>
    public static ValidationException Combine(this IEnumerable<ValidationException> exceptions)
    {
        if (exceptions == null)
        {
            throw new ArgumentNullException(nameof(exceptions));
        }

        var exceptionList = exceptions.ToList();

        if (exceptionList.Count == 0)
        {
            throw new ArgumentException("Collection cannot be empty", nameof(exceptions));
        }

        if (exceptionList.Count == 1)
        {
            return exceptionList[0];
        }

        var combinedMessage = $"Multiple validation errors occurred: {exceptionList.Count} errors";
        var combinedException = new ValidationException(combinedMessage);

        // Aggregate field-specific errors
        var fieldErrors = exceptionList
            .Where(e => !string.IsNullOrEmpty(e.FieldName))
            .GroupBy(e => e.FieldName)
            .Select(g => new
            {
                Field = g.Key,
                Messages = g.Select(e => e.Message).ToList()
            })
            .ToList();

        if (fieldErrors.Count > 0)
        {
            var firstFieldError = fieldErrors.First();
            combinedException.FieldName = firstFieldError.Field;
            combinedException.InvalidValue = exceptionList.FirstOrDefault(e => e.FieldName == firstFieldError.Field)?.InvalidValue;
            combinedException.ValidationRule = "Multiple rules failed";
        }

        return combinedException;
    }
}