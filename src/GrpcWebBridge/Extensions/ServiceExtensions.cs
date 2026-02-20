#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Data;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using System;

namespace GrpcWebBridge.Extensions;

/// <summary>
/// Extension methods for service operations
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Safely registers a service with error handling
    /// </summary>
    /// <param name="repository">The service repository to register with</param>
    /// <param name="service">The service to register</param>
    /// <returns>True if registration succeeded; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when repository or service is null</exception>
    public static async Task<bool> TryRegisterServiceAsync(
        this IServiceRepository repository,
        GrpcService service)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(service);

        try
        {
            service.Validate();
            return await repository.AddAsync(service).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates and registers a method to a service
    /// </summary>
    /// <param name="service">The service to add the method to</param>
    /// <param name="method">The method to add</param>
    /// <returns>True if method was added successfully; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when service or method is null</exception>
    public static bool TryAddMethod(this GrpcService service, GrpcMethod method)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(method);

        try
        {
            method.Validate();
            service.AddMethod(method);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates an error response from an exception
    /// </summary>
    /// <param name="exception">The exception to convert to a gRPC response</param>
    /// <param name="requestId">The request identifier</param>
    /// <param name="translationService">The protocol translation service</param>
    /// <returns>A gRPC response representing the error</returns>
    /// <exception cref="ArgumentNullException">Thrown when exception or translationService is null</exception>
    public static GrpcResponse ToGrpcResponse(
        this Exception exception,
        string requestId,
        ProtocolTranslationService translationService)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(translationService);
        ArgumentException.ThrowIfNullOrEmpty(requestId);

        var statusCode = exception switch
        {
            ArgumentNullException => GrpcStatusCode.InvalidArgument,
            ArgumentException => GrpcStatusCode.InvalidArgument,
            TimeoutException => GrpcStatusCode.DeadlineExceeded,
            InvalidOperationException => GrpcStatusCode.FailedPrecondition,
            OperationCanceledException => GrpcStatusCode.Cancelled,
            _ => GrpcStatusCode.Internal
        };

        return translationService.CreateErrorResponse(
            requestId,
            statusCode,
            exception.Message);
    }

    /// <summary>
    /// Gets human-readable status message
    /// </summary>
    /// <param name="statusCode">The gRPC status code</param>
    /// <returns>Human-readable status message</returns>
    public static string GetStatusMessage(this GrpcStatusCode statusCode)
    {
        return statusCode switch
        {
            GrpcStatusCode.Ok => "OK",
            GrpcStatusCode.Cancelled => "Request cancelled",
            GrpcStatusCode.Unknown => "Unknown error occurred",
            GrpcStatusCode.InvalidArgument => "Invalid argument provided",
            GrpcStatusCode.DeadlineExceeded => "Request deadline exceeded",
            GrpcStatusCode.NotFound => "Resource not found",
            GrpcStatusCode.AlreadyExists => "Resource already exists",
            GrpcStatusCode.PermissionDenied => "Permission denied",
            GrpcStatusCode.ResourceExhausted => "Resource exhausted",
            GrpcStatusCode.FailedPrecondition => "Failed precondition",
            GrpcStatusCode.Aborted => "Request aborted",
            GrpcStatusCode.OutOfRange => "Value out of range",
            GrpcStatusCode.Unimplemented => "Operation not implemented",
            GrpcStatusCode.Internal => "Internal server error",
            GrpcStatusCode.Unavailable => "Service unavailable",
            GrpcStatusCode.DataLoss => "Data loss error",
            GrpcStatusCode.Unauthenticated => "Authentication required",
            _ => "Unknown status"
        };
    }

    /// <summary>
    /// Converts stream state to human-readable string
    /// </summary>
    /// <param name="state">The stream state to convert</param>
    /// <returns>Human-readable display string</returns>
    public static string ToDisplayString(this StreamState state)
    {
        return state switch
        {
            StreamState.New => "Initializing",
            StreamState.Active => "Active",
            StreamState.HalfClosed => "Half-closed",
            StreamState.Closed => "Closed",
            StreamState.Failed => "Failed",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Checks if a status code represents an error
    /// </summary>
    /// <param name="statusCode">The status code to check</param>
    /// <returns>True if status represents an error; false otherwise</returns>
    public static bool IsError(this GrpcStatusCode statusCode)
    {
        return statusCode != GrpcStatusCode.Ok;
    }

    /// <summary>
    /// Gets the HTTP status code equivalent
    /// </summary>
    /// <param name="statusCode">The gRPC status code to convert</param>
    /// <returns>Equivalent HTTP status code</returns>
    public static int ToHttpStatusCode(this GrpcStatusCode statusCode)
    {
        return statusCode switch
        {
            GrpcStatusCode.Ok => 200,
            GrpcStatusCode.Cancelled => 499,
            GrpcStatusCode.Unknown => 500,
            GrpcStatusCode.InvalidArgument => 400,
            GrpcStatusCode.DeadlineExceeded => 504,
            GrpcStatusCode.NotFound => 404,
            GrpcStatusCode.AlreadyExists => 409,
            GrpcStatusCode.PermissionDenied => 403,
            GrpcStatusCode.ResourceExhausted => 429,
            GrpcStatusCode.FailedPrecondition => 400,
            GrpcStatusCode.Aborted => 409,
            GrpcStatusCode.OutOfRange => 400,
            GrpcStatusCode.Unimplemented => 501,
            GrpcStatusCode.Internal => 500,
            GrpcStatusCode.Unavailable => 503,
            GrpcStatusCode.DataLoss => 500,
            GrpcStatusCode.Unauthenticated => 401,
            _ => 500
        };
    }

    /// <summary>
    /// Creates a summary of service health
    /// </summary>
    /// <param name="registry">The service registry</param>
    /// <param name="streaming">The streaming service</param>
    /// <returns>Service health summary</returns>
    /// <exception cref="ArgumentNullException">Thrown when registry or streaming is null</exception>
    public static ServiceHealthSummary GetHealthSummary(
        this ServiceRegistry registry,
        StreamingService streaming)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(streaming);

        var services = registry.ListServices();

        return new ServiceHealthSummary
        {
            TotalServices = services.Count(),
            HealthyServices = services.Count(s => s.Status == ServiceStatus.Serving),
            UnhealthyServices = services.Count(s => s.Status != ServiceStatus.Serving),
            ActiveStreams = streaming.ActiveStreamCount,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts method type to description
    /// </summary>
    /// <param name="methodType">The method type to describe</param>
    /// <returns>Human-readable description</returns>
    public static string ToDescription(this MethodType methodType)
    {
        return methodType switch
        {
            MethodType.Unary => "Unary - Single request, single response",
            MethodType.ClientStreaming => "Client Streaming - Multiple requests, single response",
            MethodType.ServerStreaming => "Server Streaming - Single request, multiple responses",
            MethodType.BidirectionalStreaming => "Bidirectional Streaming - Multiple requests and responses",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Service health summary
/// </summary>
public sealed class ServiceHealthSummary
{
    public int TotalServices { get; set; }
    public int HealthyServices { get; set; }
    public int UnhealthyServices { get; set; }
    public int ActiveStreams { get; set; }
    public DateTime Timestamp { get; set; }

    public double HealthPercentage => TotalServices > 0 ? (double)HealthyServices / TotalServices * 100 : 0;

    public bool IsHealthy => UnhealthyServices == 0;
}
