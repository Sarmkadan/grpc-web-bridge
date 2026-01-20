#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Exception thrown during service registration and discovery
/// </summary>
public class ServiceRegistrationException : GrpcWebBridgeException
{
    public string? ServiceName { get; set; }
    public string? ServiceEndpoint { get; set; }

    public ServiceRegistrationException() : base() { }

    public ServiceRegistrationException(string message) : base(message, "SERVICE_REGISTRATION_ERROR") { }

    public ServiceRegistrationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "SERVICE_REGISTRATION_ERROR";
    }

    public ServiceRegistrationException(string serviceName, string message)
        : base($"Service registration failed for '{serviceName}': {message}", "SERVICE_NOT_FOUND")
    {
        ServiceName = serviceName;
        GrpcStatus = GrpcStatusCode.NotFound;
    }

    public ServiceRegistrationException(string serviceName, string endpoint, string message)
        : base($"Failed to connect to service '{serviceName}' at {endpoint}: {message}", "SERVICE_UNAVAILABLE")
    {
        ServiceName = serviceName;
        ServiceEndpoint = endpoint;
        GrpcStatus = GrpcStatusCode.Unavailable;
    }

    public override string ToString()
    {
        var result = base.ToString();
        if (!string.IsNullOrEmpty(ServiceName))
            result += $" | Service: {ServiceName}";

        if (!string.IsNullOrEmpty(ServiceEndpoint))
            result += $" | Endpoint: {ServiceEndpoint}";

        return result;
    }
}
