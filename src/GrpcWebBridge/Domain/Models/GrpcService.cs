#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.ObjectModel;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a registered gRPC service with its methods, connection details, and metadata.
/// Provides method management (add/remove/lookup) and validation for service registration.
/// </summary>
public sealed class GrpcService
{
    private readonly List<GrpcMethod> _methods = [];

    /// <summary>Unique service identifier (GUID without hyphens).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Service name as defined in the .proto file (e.g., "Greeter").</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Protobuf package name (e.g., "greet.v1").</summary>
    public string PackageName { get; set; } = Constants.ServiceRegistry.DefaultNamespace;
    /// <summary>Fully qualified service name: "{PackageName}.{Name}".</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Optional human-readable description of the service.</summary>
    public string? Description { get; set; }
    /// <summary>Host or IP address where the gRPC server is listening.</summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>Port number for the gRPC server. Defaults to 50051.</summary>
    public int Port { get; set; } = 50051;
    /// <summary>Whether to use TLS for the connection to this service.</summary>
    public bool UseTls { get; set; }
    /// <summary>Current serving status of the service.</summary>
    public ServiceStatus Status { get; set; } = ServiceStatus.Serving;
    /// <summary>UTC timestamp when the service was registered.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>UTC timestamp of the last modification, or null if never updated.</summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>Arbitrary key-value metadata attached to the service.</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>Read-only collection of methods registered on this service.</summary>
    public IReadOnlyCollection<GrpcMethod> Methods => _methods.AsReadOnly();

    public GrpcService() { }

    public GrpcService(string name, string packageName, string endpoint, int port)
    {
        Name = ValidateName(name);
        PackageName = ValidatePackage(packageName);
        Endpoint = ValidateEndpoint(endpoint);
        Port = ValidatePort(port);
        FullName = $"{packageName}.{name}";
    }

    /// <summary>
    /// Registers a new method on this service. Validates the method and rejects duplicates.
    /// </summary>
    /// <param name="method">The method to add.</param>
    /// <exception cref="InvalidOperationException">Thrown when a method with the same FullName already exists.</exception>
    public void AddMethod(GrpcMethod method)
    {
        if (method is null)
            throw new ArgumentNullException(nameof(method));

        method.Validate();

        if (_methods.Any(m => m.FullName == method.FullName))
            throw new InvalidOperationException($"Method '{method.FullName}' already exists");

        _methods.Add(method);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Finds a method by short name or fully qualified name.
    /// </summary>
    /// <param name="methodName">The method name or full name to search for.</param>
    /// <returns>The matching <see cref="GrpcMethod"/>, or null if not found.</returns>
    public GrpcMethod? GetMethod(string methodName)
    {
        return _methods.FirstOrDefault(m =>
            m.Name == methodName || m.FullName == methodName);
    }

    /// <summary>
    /// Checks whether a method with the given name exists on this service.
    /// </summary>
    public bool HasMethod(string methodName)
    {
        return _methods.Any(m => m.Name == methodName || m.FullName == methodName);
    }

    /// <summary>
    /// Removes a method by short name. No-op if the method does not exist.
    /// </summary>
    public void RemoveMethod(string methodName)
    {
        var method = _methods.FirstOrDefault(m => m.Name == methodName);
        if (method is not null)
        {
            _methods.Remove(method);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public string? GetMetadata(string key)
    {
        return Metadata.TryGetValue(key, out var value) ? value : null;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Service name cannot be empty", nameof(Name));

        if (string.IsNullOrWhiteSpace(PackageName))
            throw new ArgumentException("Package name cannot be empty", nameof(PackageName));

        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new ArgumentException("Endpoint cannot be empty", nameof(Endpoint));

        if (Port <= 0 || Port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(Port));

        if (_methods.Count == 0)
            throw new InvalidOperationException("Service must have at least one method");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Service name cannot be empty", nameof(name));
        return name.Trim();
    }

    private static string ValidatePackage(string package)
    {
        if (string.IsNullOrWhiteSpace(package))
            throw new ArgumentException("Package name cannot be empty", nameof(package));
        return package.Trim();
    }

    private static string ValidateEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty", nameof(endpoint));
        return endpoint.Trim();
    }

    private static int ValidatePort(int port)
    {
        if (port <= 0 || port > 65535)
            throw new ArgumentException("Port must be between 1 and 65535", nameof(port));
        return port;
    }

    public override string ToString() => $"{FullName} ({Endpoint}:{Port})";

    public override bool Equals(object? obj)
    {
        if (obj is not GrpcService other)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
