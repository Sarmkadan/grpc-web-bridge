#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="ValidationException"/> to enhance validation error handling and reporting.
/// Includes RFC 7807 ProblemDetails support for standardized error responses.
/// </summary>
public static class ValidationExceptionExtensions
{
    /// <summary>
    /// Creates a user-friendly error message from the validation exception.
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <returns>A formatted error message suitable for API responses</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string ToErrorMessage(this ValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message = exception.Message;

        if (!string.IsNullOrEmpty(exception.FieldName))
        {
            message = $"Validation failed for field '{exception.FieldName}': {message}";
        }

        if (exception.InvalidValue is not null)
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
    /// Checks if the validation exception represents a specific field validation failure.
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <param name="fieldName">The field name to check against</param>
    /// <returns>True if the exception is for the specified field, otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fieldName"/> is null.</exception>
    public static bool IsForField(this ValidationException exception, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(fieldName);

        return string.Equals(exception.FieldName, fieldName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates a simplified validation error object suitable for JSON serialization.
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <returns>A dictionary containing the validation error details</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static Dictionary<string, object?> ToErrorDetails(this ValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var details = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["message"] = exception.Message,
            ["errorCode"] = "VALIDATION_ERROR"
        };

        if (!string.IsNullOrEmpty(exception.FieldName))
        {
            details["field"] = exception.FieldName;
        }

        if (exception.InvalidValue is not null)
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
    /// Combines multiple validation exceptions into a single aggregated exception.
    /// </summary>
    /// <param name="exceptions">Collection of validation exceptions</param>
    /// <returns>A new ValidationException containing all validation errors</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exceptions"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the collection is empty.</exception>
    public static ValidationException Combine(this IEnumerable<ValidationException> exceptions)
    {
        ArgumentNullException.ThrowIfNull(exceptions);

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

    /// <summary>
    /// Creates a ProblemDetails object from a ValidationException for RFC 7807 compliance.
    /// </summary>
    /// <param name="exception">The validation exception</param>
    /// <returns>A ProblemDetails object with validation error information</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static ProblemDetails ToProblemDetails(this ValidationException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var problemDetails = new ProblemDetails
        {
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            Title = "Validation Failed",
            Status = (int)System.Net.HttpStatusCode.BadRequest,
            Detail = exception.Message,
            Timestamp = DateTime.UtcNow
        };

        // Add field-level error information
        var errors = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(exception.FieldName))
        {
            errors[exception.FieldName] = exception.Message;
            problemDetails.Extensions["field"] = exception.FieldName;
        }

        if (exception.InvalidValue is not null)
        {
            errors["value"] = exception.InvalidValue;
            problemDetails.Extensions["providedValue"] = exception.InvalidValue;
        }

        if (!string.IsNullOrEmpty(exception.ValidationRule))
        {
            errors["rule"] = exception.ValidationRule;
            problemDetails.Extensions["validationRule"] = exception.ValidationRule;
        }

        if (errors.Count > 0)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        problemDetails.Extensions["errorCode"] = "VALIDATION_ERROR";

        return problemDetails;
    }
}
