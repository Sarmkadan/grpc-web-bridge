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
    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the endpoint of the service.
    /// </summary>
    public string? ServiceEndpoint { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationException"/> class.
    /// </summary>
    public ServiceRegistrationException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ServiceRegistrationException(string message) : base(message, "SERVICE_REGISTRATION_ERROR") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ServiceRegistrationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "SERVICE_REGISTRATION_ERROR";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationException"/> class with a specified service name and error message.
    /// </summary>
    /// <param name="serviceName">The name of the service that failed to register.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ServiceRegistrationException(string serviceName, string message)
        : base($"Service registration failed for '{serviceName}': {message}", "SERVICE_NOT_FOUND")
    {
        ServiceName = serviceName;
        GrpcStatus = GrpcStatusCode.NotFound;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceRegistrationException"/> class with a specified service name, endpoint, and error message.
    /// </summary>
    /// <param name="serviceName">The name of the service that failed to connect.</param>
    /// <param name="endpoint">The endpoint of the service that failed to connect.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
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
