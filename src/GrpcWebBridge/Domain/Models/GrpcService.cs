// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.ObjectModel;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Represents a gRPC service with its metadata and methods
/// </summary>
public class GrpcService
{
    private readonly List<GrpcMethod> _methods = [];

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string PackageName { get; set; } = Constants.ServiceRegistry.DefaultNamespace;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public int Port { get; set; } = 50051;
    public bool UseTls { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Serving;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = [];

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

    public GrpcMethod? GetMethod(string methodName)
    {
        return _methods.FirstOrDefault(m =>
            m.Name == methodName || m.FullName == methodName);
    }

    public bool HasMethod(string methodName)
    {
        return _methods.Any(m => m.Name == methodName || m.FullName == methodName);
    }

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
