#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Formatters;

/// <summary>
/// Unified response formatting for all API endpoints.
/// Ensures consistent response structure across the bridge.
/// Supports wrapping data, adding metadata, and formatting errors.
/// </summary>
public static class ResponseFormatter
{
    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static object FormatSuccess<T>(T data, string? message = null, Dictionary<string, object>? metadata = null)
    {
        return new
        {
            success = true,
            message = message ?? "Operation completed successfully",
            data = data,
            metadata = metadata,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a successful response with multiple data items.
    /// </summary>
    public static object FormatSuccessList<T>(
        IEnumerable<T> items,
        int total,
        int page = 1,
        int pageSize = 50,
        string? message = null)
    {
        return new
        {
            success = true,
            message = message ?? "Operation completed successfully",
            data = items,
            pagination = new
            {
                total = total,
                page = page,
                pageSize = pageSize,
                totalPages = (total + pageSize - 1) / pageSize
            },
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static object FormatError(
        string error,
        string? message = null,
        int statusCode = 500,
        object? details = null)
    {
        return new
        {
            success = false,
            error = error,
            message = message ?? "An error occurred",
            statusCode = statusCode,
            details = details,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a validation error response.
    /// </summary>
    public static object FormatValidationError(Dictionary<string, string> validationErrors)
    {
        return new
        {
            success = false,
            error = "Validation Failed",
            message = "One or more validation errors occurred",
            statusCode = 400,
            validationErrors = validationErrors,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a response for streaming operations.
    /// </summary>
    public static object FormatStreamingResponse(
        string streamId,
        string status,
        int messageCount = 0,
        object? lastMessage = null)
    {
        return new
        {
            success = true,
            streamId = streamId,
            status = status,
            messageCount = messageCount,
            lastMessage = lastMessage,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a response for batch operations.
    /// </summary>
    public static object FormatBatchResponse(
        int operationCount,
        int successCount,
        int failureCount,
        List<object>? results = null)
    {
        return new
        {
            success = failureCount == 0,
            operationCount = operationCount,
            successCount = successCount,
            failureCount = failureCount,
            successRate = operationCount > 0 ? Math.Round((successCount / (double)operationCount) * 100, 2) : 0,
            results = results,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a response for health check operations.
    /// </summary>
    public static object FormatHealthCheckResponse(
        bool healthy,
        string status,
        Dictionary<string, object>? metrics = null,
        List<string>? warnings = null)
    {
        return new
        {
            success = true,
            healthy = healthy,
            status = status,
            metrics = metrics,
            warnings = warnings,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a response for statistics endpoints.
    /// </summary>
    public static object FormatStatisticsResponse(
        Dictionary<string, object>? statistics,
        string? period = null)
    {
        return new
        {
            success = true,
            statistics = statistics,
            period = period ?? "all-time",
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Wraps any object in a standard response envelope.
    /// </summary>
    public static object WrapResponse(
        object data,
        bool success = true,
        string? message = null,
        int statusCode = 200)
    {
        return new
        {
            success = success,
            message = message,
            data = data,
            statusCode = statusCode,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Converts a response to JSON string.
    /// </summary>
    public static string ToJson(object response, bool indented = false)
    {
        return JsonUtility.Serialize(response, indented);
    }

    /// <summary>
    /// Creates a response envelope with custom structure.
    /// Allows more granular control over response format.
    /// </summary>
    public static string CreateCustomResponse(
        bool success,
        object? body,
        Dictionary<string, object>? headers = null,
        int statusCode = 200)
    {
        var response = new Dictionary<string, object>
        {
            { "success", success },
            { "statusCode", statusCode },
            { "body", body ?? new object() },
            { "timestamp", DateTime.UtcNow }
        };

        if (headers is not null)
        {
            response["headers"] = headers;
        }

        return JsonUtility.Serialize(response);
    }

    /// <summary>
    /// Formats a service registry response.
    /// </summary>
    public static object FormatServiceRegistryResponse(
        int totalServices,
        int healthyServices,
        int unhealthyServices,
        int totalMethods,
        List<object>? services = null)
    {
        return new
        {
            success = true,
            summary = new
            {
                totalServices = totalServices,
                healthyServices = healthyServices,
                unhealthyServices = unhealthyServices,
                totalMethods = totalMethods,
                healthPercentage = totalServices > 0 ? Math.Round((healthyServices / (double)totalServices) * 100, 2) : 0
            },
            services = services,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Formats a configuration response.
    /// </summary>
    public static object FormatConfigurationResponse(
        Dictionary<string, object> config,
        string? environment = null)
    {
        return new
        {
            success = true,
            environment = environment ?? "unknown",
            configuration = config,
            timestamp = DateTime.UtcNow
        };
    }
}
