#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.ObjectModel;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a gRPC service with its metadata and methods
/// </summary>
public sealed class GrpcService
{
    private readonly List<GrpcMethod> _methods = [];

    /// <summary>
    /// Gets or sets the unique identifier for the service.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the package name (namespace) of the service.
    /// </summary>
    public string PackageName { get; set; } = Constants.ServiceRegistry.DefaultNamespace;

    /// <summary>
    /// Gets or sets the full name of the service (package.name).
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the service.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the endpoint (host) of the service.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port number of the service.
    /// </summary>
    public int Port { get; set; } = 50051;

    /// <summary>
    /// Gets or sets a value indicating whether the service uses TLS.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Gets or sets the current status of the service.
    /// </summary>
    public ServiceStatus Status { get; set; } = ServiceStatus.Serving;

    /// <summary>
    /// Gets or sets the date and time when the service was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the service was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the metadata key-value pairs associated with the service.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>
    /// Gets the read-only collection of methods defined in the service.
    /// </summary>
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
    /// Adds a method to the service.
    /// </summary>
    /// <param name="method">The method to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when method is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a method with the same full name already exists.</exception>
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
    /// Gets a method by its name or full name.
    /// </summary>
    /// <param name="methodName">The name or full name of the method to retrieve.</param>
    /// <returns>The method if found; otherwise, null.</returns>
    public GrpcMethod? GetMethod(string methodName)
    {
        return _methods.FirstOrDefault(m =>
            m.Name == methodName || m.FullName == methodName);
    }

    /// <summary>
    /// Determines whether the service contains a method with the specified name or full name.
    /// </summary>
    /// <param name="methodName">The name or full name of the method to locate.</param>
    /// <returns>true if the method is found; otherwise, false.</returns>
    public bool HasMethod(string methodName)
    {
        return _methods.Any(m => m.Name == methodName || m.FullName == methodName);
    }

    /// <summary>
    /// Removes a method from the service by its name.
    /// </summary>
    /// <param name="methodName">The name of the method to remove.</param>
    public void RemoveMethod(string methodName)
    {
        var method = _methods.FirstOrDefault(m => m.Name == methodName);
        if (method is not null)
        {
            _methods.Remove(method);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Sets a metadata value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the metadata entry.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentException">Thrown when key is empty or consists only of white-space.</exception>
    public void SetMetadata(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Metadata key cannot be empty", nameof(key));

        Metadata[key] = value;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the metadata value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the metadata entry to get.</param>
    /// <returns>The value associated with the specified key, or null if the key is not found.</returns>
    public string? GetMetadata(string key)
    {
        return Metadata.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Validates the service configuration.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when service name, package name, endpoint, or port is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service has no methods.</exception>
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

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not GrpcService other)
            return false;

        return Id == other.Id;
    }

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode() => Id.GetHashCode();
}
