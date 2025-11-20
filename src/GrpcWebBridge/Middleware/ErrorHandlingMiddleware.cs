#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in request pipeline");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles exception conversion to HTTP response.
    /// Maps domain exceptions to appropriate HTTP status codes.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Success = false,
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            TraceId = context.TraceIdentifier
        };

        // Map exception types to HTTP status codes and error details
        switch (exception)
        {
            case ServiceRegistrationException sre:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = "Service Registration Failed";
                response.Message = sre.Message;
                response.Details = new { exception = nameof(ServiceRegistrationException) };
                break;

            case StreamingException se:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Error = "Streaming Operation Failed";
                response.Message = se.Message;
                response.Details = new { exception = nameof(StreamingException) };
                break;

            case ProtocolException pe:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = "Protocol Translation Failed";
                response.Message = pe.Message;
                response.Details = new { exception = nameof(ProtocolException) };
                break;

            case GrpcWebBridgeException gwbe:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Error = "Bridge Operation Failed";
                response.Message = gwbe.Message;
                response.Details = new { exception = nameof(GrpcWebBridgeException) };
                break;

            case ArgumentNullException ane:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = "Invalid Request";
                response.Message = $"Required parameter missing: {ane.ParamName}";
                response.Details = new { exception = nameof(ArgumentNullException), paramName = ane.ParamName };
                break;

            case ArgumentException ae:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = "Invalid Argument";
                response.Message = ae.Message;
                response.Details = new { exception = nameof(ArgumentException) };
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Error = "Unauthorized";
                response.Message = "You do not have permission to perform this operation";
                response.Details = new { exception = nameof(UnauthorizedAccessException) };
                break;

            case TimeoutException te:
                context.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
                response.Error = "Operation Timeout";
                response.Message = te.Message;
                response.Details = new { exception = nameof(TimeoutException) };
                break;

            case OperationCanceledException oce:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Error = "Operation Cancelled";
                response.Message = oce.Message;
                response.Details = new { exception = nameof(OperationCanceledException) };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Error = "Internal Server Error";
                response.Message = "An unexpected error occurred while processing your request";
                response.Details = new
                {
                    exception = exception.GetType().Name,
                    // Include message in development environment only
                    message = !string.IsNullOrEmpty(exception.Message) ? exception.Message : null
                };
                break;
        }

        return context.Response.WriteAsJsonAsync(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }
}

/// <summary>
/// Standard error response format for all error scenarios.
/// </summary>
public sealed class ErrorResponse
{
    public bool Success { get; set; } = false;
    public string? Error { get; set; }
    public string? Message { get; set; }
    public object? Details { get; set; }
    public string? Path { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; }
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
