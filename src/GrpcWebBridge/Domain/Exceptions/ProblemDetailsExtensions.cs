#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Net;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Provides extension methods for converting exceptions and HTTP contexts to RFC 7807 ProblemDetails.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Converts an exception to a ProblemDetails object following RFC 7807 standards.
    /// </summary>
    /// <param name="exception">The exception to convert.</param>
    /// <param name="httpContext">The HTTP context (optional, provides trace ID and path).</param>
    /// <returns>A ProblemDetails object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
    public static ProblemDetails ToProblemDetails(this Exception exception, HttpContext? httpContext = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var problemDetails = new ProblemDetails
        {
            Type = "about:blank",
            Title = "An error occurred while processing your request.",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = exception.Message,
            Timestamp = DateTime.UtcNow,
            TraceId = httpContext?.TraceIdentifier,
            Path = httpContext?.Request.Path.Value
        };

        // Map based on exception type
        switch (exception)
        {
            case ValidationException ve:
                MapValidationException(ve, problemDetails);
                break;

            case ProtocolException pe:
                MapProtocolException(pe, problemDetails);
                break;

            case StreamingException se:
                MapStreamingException(se, problemDetails);
                break;

            case ServiceRegistrationException sre:
                MapServiceRegistrationException(sre, problemDetails);
                break;

            case ConfigurationException ce:
                MapConfigurationException(ce, problemDetails);
                break;

            case ArgumentNullException ane:
                MapArgumentNullException(ane, problemDetails);
                break;

            case ArgumentException ae:
                MapArgumentException(ae, problemDetails);
                break;

            case GrpcWebBridgeException gwbe:
                MapGrpcWebBridgeException(gwbe, problemDetails);
                break;

            case UnauthorizedAccessException:
                problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1";
                problemDetails.Title = "Unauthorized";
                problemDetails.Status = (int)HttpStatusCode.Unauthorized;
                problemDetails.Detail = exception.Message;
                break;

            case TimeoutException te:
                problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.4";
                problemDetails.Title = "Gateway Timeout";
                problemDetails.Status = (int)HttpStatusCode.GatewayTimeout;
                problemDetails.Detail = exception.Message;
                break;

            case OperationCanceledException oce:
                problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.3";
                problemDetails.Title = "Operation Cancelled";
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Detail = exception.Message;
                break;

            default:
                // Keep defaults set above
                break;
        }

        // Add exception type to extensions for debugging (not in production responses)
        problemDetails.Extensions["exceptionType"] = exception.GetType().FullName;

        return problemDetails;
    }

    /// <summary>
    /// Maps a GrpcWebBridgeException to ProblemDetails.
    /// </summary>
    private static void MapGrpcWebBridgeException(GrpcWebBridgeException exception, ProblemDetails problemDetails)
    {
        problemDetails.Title = "Bridge Operation Failed";
        problemDetails.Status = (int)HttpStatusCode.InternalServerError;

        if (exception.GrpcStatus.HasValue)
        {
            problemDetails.Status = MapGrpcStatusToHttpStatus(exception.GrpcStatus.Value);
            problemDetails.Title = GetGrpcStatusTitle(exception.GrpcStatus.Value);
        }

        if (!string.IsNullOrEmpty(exception.ErrorCode))
        {
            problemDetails.Extensions["errorCode"] = exception.ErrorCode;
        }

        // Add context data
        foreach (var kvp in exception.Context)
        {
            problemDetails.Extensions[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Maps a ValidationException to ProblemDetails with field-level errors.
    /// </summary>
    private static void MapValidationException(ValidationException exception, ProblemDetails problemDetails)
    {
        problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
        problemDetails.Title = "Validation Failed";
        problemDetails.Status = (int)HttpStatusCode.BadRequest;
        problemDetails.Detail = exception.Message;

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
    }

    /// <summary>
    /// Maps a ProtocolException to ProblemDetails.
    /// </summary>
    private static void MapProtocolException(ProtocolException exception, ProblemDetails problemDetails)
    {
        problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
        problemDetails.Title = "Protocol Translation Failed";
        problemDetails.Status = (int)HttpStatusCode.BadRequest;
        problemDetails.Detail = exception.Message;
        problemDetails.Extensions["sourceFormat"] = exception.SourceFormat ?? "unknown";
        problemDetails.Extensions["targetFormat"] = exception.TargetFormat ?? "unknown";
    }

    /// <summary>
    /// Maps a StreamingException to ProblemDetails.
    /// </summary>
    private static void MapStreamingException(StreamingException exception, ProblemDetails problemDetails)
    {
        problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1";
        problemDetails.Title = "Streaming Operation Failed";
        problemDetails.Status = (int)HttpStatusCode.InternalServerError;
        problemDetails.Detail = exception.Message;
        problemDetails.Extensions["streamId"] = exception.StreamId ?? "unknown";
        if (exception.LastStreamState.HasValue)
        {
            problemDetails.Extensions["streamState"] = exception.LastStreamState.Value.ToString();
        }
    }

    /// <summary>
    /// Maps a ServiceRegistrationException to ProblemDetails.
    /// </summary>
    private static void MapServiceRegistrationException(ServiceRegistrationException exception, ProblemDetails problemDetails)
    {
        problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
        problemDetails.Title = "Service Registration Failed";
        problemDetails.Status = (int)HttpStatusCode.BadRequest;
        problemDetails.Detail = exception.Message;
        problemDetails.Extensions["serviceName"] = exception.ServiceName ?? "unknown";
        if (!string.IsNullOrEmpty(exception.ServiceEndpoint))
        {
            problemDetails.Extensions["serviceEndpoint"] = exception.ServiceEndpoint;
        }
    }

    /// <summary>
    /// Maps a ConfigurationException to ProblemDetails.
    /// </summary>
    private static void MapConfigurationException(ConfigurationException exception, ProblemDetails problemDetails)
    {
        problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
        problemDetails.Title = "Configuration Error";
        problemDetails.Status = (int)HttpStatusCode.BadRequest;
        problemDetails.Detail = exception.Message;
        problemDetails.Extensions["configurationKey"] = exception.ConfigurationKey ?? "unknown";
        if (!string.IsNullOrEmpty(exception.ConfigurationValue))
        {
            problemDetails.Extensions["configurationValue"] = exception.ConfigurationValue;
        }
    }

    /// <summary>
    /// Maps an ArgumentNullException to ProblemDetails.
    /// </summary>
    private static void MapArgumentNullException(ArgumentNullException exception, ProblemDetails problemDetails)
    {
        problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
        problemDetails.Title = "Invalid Request";
        problemDetails.Status = (int)HttpStatusCode.BadRequest;
        problemDetails.Detail = exception.Message;
        problemDetails.Extensions["paramName"] = exception.ParamName;
    }

    /// <summary>
    /// Maps an ArgumentException to ProblemDetails.
    /// </summary>
    private static void MapArgumentException(ArgumentException exception, ProblemDetails problemDetails)
    {
        problemDetails.Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1";
        problemDetails.Title = "Invalid Argument";
        problemDetails.Status = (int)HttpStatusCode.BadRequest;
        problemDetails.Detail = exception.Message;
    }

    /// <summary>
    /// Maps a gRPC status code to an HTTP status code.
    /// </summary>
    private static int MapGrpcStatusToHttpStatus(GrpcStatusCode statusCode)
    {
        return statusCode switch
        {
            GrpcStatusCode.InvalidArgument => (int)HttpStatusCode.BadRequest,
            GrpcStatusCode.DeadlineExceeded => (int)HttpStatusCode.GatewayTimeout,
            GrpcStatusCode.NotFound => (int)HttpStatusCode.NotFound,
            GrpcStatusCode.AlreadyExists => (int)HttpStatusCode.Conflict,
            GrpcStatusCode.PermissionDenied => (int)HttpStatusCode.Forbidden,
            GrpcStatusCode.ResourceExhausted => (int)HttpStatusCode.TooManyRequests,
            GrpcStatusCode.FailedPrecondition => (int)HttpStatusCode.PreconditionFailed,
            GrpcStatusCode.Aborted => (int)HttpStatusCode.InternalServerError,
            GrpcStatusCode.OutOfRange => (int)HttpStatusCode.BadRequest,
            GrpcStatusCode.Unimplemented => (int)HttpStatusCode.NotImplemented,
            GrpcStatusCode.Internal => (int)HttpStatusCode.InternalServerError,
            GrpcStatusCode.Unavailable => (int)HttpStatusCode.ServiceUnavailable,
            GrpcStatusCode.DataLoss => (int)HttpStatusCode.InternalServerError,
            GrpcStatusCode.Unauthenticated => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    /// <summary>
    /// Gets a human-readable title for a gRPC status code.
    /// </summary>
    private static string GetGrpcStatusTitle(GrpcStatusCode statusCode)
    {
        return statusCode switch
        {
            GrpcStatusCode.InvalidArgument => "Invalid Argument",
            GrpcStatusCode.DeadlineExceeded => "Deadline Exceeded",
            GrpcStatusCode.NotFound => "Not Found",
            GrpcStatusCode.AlreadyExists => "Already Exists",
            GrpcStatusCode.PermissionDenied => "Permission Denied",
            GrpcStatusCode.ResourceExhausted => "Resource Exhausted",
            GrpcStatusCode.FailedPrecondition => "Failed Precondition",
            GrpcStatusCode.Aborted => "Aborted",
            GrpcStatusCode.OutOfRange => "Out of Range",
            GrpcStatusCode.Unimplemented => "Not Implemented",
            GrpcStatusCode.Internal => "Internal Error",
            GrpcStatusCode.Unavailable => "Service Unavailable",
            GrpcStatusCode.DataLoss => "Data Loss",
            GrpcStatusCode.Unauthenticated => "Unauthenticated",
            _ => "Bridge Operation Failed"
        };
    }

    /// <summary>
    /// Adds field-level validation errors to a ProblemDetails object.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails object.</param>
    /// <param name="fieldName">The name of the field with validation errors.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="invalidValue">The invalid value (optional).</param>
    public static void AddValidationError(this ProblemDetails problemDetails, string fieldName, string errorMessage, object? invalidValue = null)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);

        if (!problemDetails.Extensions.TryGetValue("errors", out var errorsObj) || errorsObj is not Dictionary<string, object?> errors)
        {
            errors = new Dictionary<string, object?>(StringComparer.Ordinal);
            problemDetails.Extensions["errors"] = errors;
        }

        errors[fieldName] = errorMessage;
        problemDetails.Extensions["field"] = fieldName;

        if (invalidValue is not null)
        {
            problemDetails.Extensions["value"] = invalidValue;
        }
    }

    /// <summary>
    /// Adds context information to a ProblemDetails object.
    /// </summary>
    /// <param name="problemDetails">The ProblemDetails object.</param>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    public static void AddContext(this ProblemDetails problemDetails, string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);
        ArgumentException.ThrowIfNullOrEmpty(key);

        problemDetails.Extensions[key] = value;
    }
}
