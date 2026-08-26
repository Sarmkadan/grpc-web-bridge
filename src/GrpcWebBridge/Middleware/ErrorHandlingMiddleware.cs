#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using GrpcWebBridge.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Global error handling middleware.
/// Catches unhandled exceptions and converts them to appropriate HTTP responses.
/// Provides structured error responses with proper status codes and error details.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Processing {Method} {Path}", context.Request.Method, context.Request.Path);
        try
        {
            await _next(context);
            _logger.LogInformation("Finished processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in request pipeline");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exception conversion to HTTP response.
    /// Maps domain exceptions to appropriate HTTP status codes using RFC 7807 ProblemDetails.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Convert exception to RFC 7807 ProblemDetails
        var problemDetails = exception.ToProblemDetails(context);

        // Set HTTP status code from ProblemDetails
        if (problemDetails.Status.HasValue)
        {
            context.Response.StatusCode = problemDetails.Status.Value;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }

        // Create response with backward compatibility fields
        var response = new ErrorResponse
        {
            Success = false,
            Type = problemDetails.Type,
            Title = problemDetails.Title,
            Status = problemDetails.Status,
            Detail = problemDetails.Detail,
            Instance = problemDetails.Instance,
            Path = problemDetails.Path,
            TraceId = problemDetails.TraceId,
            Timestamp = problemDetails.Timestamp
        };

        // Copy extensions to Details for backward compatibility
        if (problemDetails.Extensions.Count > 0)
        {
            response.Details = problemDetails.Extensions;
        }

        return context.Response.WriteAsJsonAsync(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}

/// <summary>
/// Standard error response format for all error scenarios.
/// Maintains backward compatibility with the previous ErrorResponse structure
/// while supporting RFC 7807 ProblemDetails in the Details property.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Indicates whether the request was successful (always false for error responses).
    /// </summary>
    public bool Success { get; set; } = false;

    /// <summary>
    /// The problem type URI (RFC 7807).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// A short, human-readable summary of the problem type.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The HTTP status code.
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence.
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// A URI reference that identifies the specific occurrence.
    /// </summary>
    public string? Instance { get; set; }

    /// <summary>
    /// Additional problem-specific details (RFC 7807 extensions).
    /// </summary>
    public object? Details { get; set; }

    /// <summary>
    /// The request path where the error occurred.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// The trace identifier for correlation.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// The timestamp when the error occurred.
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Gets the ProblemDetails object for RFC 7807 compliance.
    /// </summary>
    public ProblemDetails ToProblemDetails()
    {
        return new ProblemDetails
        {
            Type = Type,
            Title = Title,
            Status = Status,
            Detail = Detail,
            Instance = Instance,
            Extensions = Details as Dictionary<string, object?> ?? new Dictionary<string, object?>(),
            TraceId = TraceId,
            Path = Path,
            Timestamp = Timestamp
        };
    }

    public override string ToString()
    {
        return $"ErrorResponse {{ Success = {Success}, Error = {Title}, Message = {Detail}, Details = {Details}, Path = {Path}, TraceId = {TraceId} }}";
    }
}

/// <summary>
/// Extension method to register error handling middleware.
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
