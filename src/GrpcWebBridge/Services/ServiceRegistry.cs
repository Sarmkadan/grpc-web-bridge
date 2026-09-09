#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace GrpcWebBridge.Services;

/// <summary>
/// Service registry for managing and discovering gRPC services
/// </summary>
public sealed class ServiceRegistry
{
    private readonly ILogger<ServiceRegistry> _logger;
    private readonly Dictionary<string, GrpcService> _services = new();
    private readonly Dictionary<string, ServiceMetadata> _metadata = new();
    private readonly object _servicesLock = new();
    private readonly object _metadataLock = new();
    private readonly Dictionary<string, DateTime> _serviceRegistrationTimestamps = new();

    public int RegisteredServiceCount
    {
        get
        {
            lock (_servicesLock)
            {
                return _services.Count;
            }
        }
    }

    public ServiceRegistry(ILogger<ServiceRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new gRPC service
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
    public void RegisterService(GrpcService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        service.Validate();

        lock (_servicesLock)
        {
            if (_services.ContainsKey(service.FullName))
                throw new ServiceRegistrationException(service.Name, "Service already registered");

            if (RegisteredServiceCount >= Constants.ServiceRegistry.MaxCachedServices)
                throw new ServiceRegistrationException(service.Name, "Service registry is full");

            _services[service.FullName] = service;
            _serviceRegistrationTimestamps[service.FullName] = DateTime.UtcNow;
            _logger.LogInformation("Service registered: {ServiceName} ({Endpoint}:{Port})",
                service.FullName, service.Endpoint, service.Port);

            CacheServiceMetadata(service);
        }
    }

    /// <summary>
    /// Retrieves a registered service by full name
    /// </summary>
    public GrpcService? GetService(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        lock (_servicesLock)
        {
            return _services.TryGetValue(fullName, out var service) ? service : null;
        }
    }

    /// <summary>
    /// Retrieves a service by name and package
    /// </summary>
    public GrpcService? GetService(string serviceName, string packageName)
    {
        // Fix: handle null inputs to prevent invalid full name creation
        if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(packageName))
            return null;

        var fullName = $"{packageName}.{serviceName}";
        return GetService(fullName);
    }

    /// <summary>
    /// Unregisters a service
    /// </summary>
    public bool UnregisterService(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return false;

        lock (_servicesLock)
        {
            if (_services.Remove(fullName))
            {
                _serviceRegistrationTimestamps.Remove(fullName);
                _logger.LogInformation("Service unregistered: {ServiceName}", fullName);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Lists all registered services
    /// </summary>
    public IEnumerable<GrpcService> ListServices()
    {
        lock (_servicesLock)
        {
            return _services.Values.ToList();
        }
    }

    /// <summary>
    /// Lists services by package
    /// </summary>
    public IEnumerable<GrpcService> ListServicesByPackage(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return new List<GrpcService>();

        lock (_servicesLock)
        {
            return _services.Values
                .Where(s => s.PackageName == packageName)
                .ToList();
        }
    }

    /// <summary>
    /// Checks if a service is registered
    /// </summary>
    public bool ServiceExists(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return false;

        lock (_servicesLock)
        {
            return _services.ContainsKey(fullName);
        }
    }

    /// <summary>
    /// Updates service status
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fullName"/> is null or empty.</exception>
    public void UpdateServiceStatus(string fullName, ServiceStatus status)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullName);

        var service = GetService(fullName);
        if (service is null)
            throw new ServiceRegistrationException(fullName, "Service not found");

        service.Status = status;
        service.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Service status updated: {ServiceName} -> {Status}", fullName, status);
    }

    /// <summary>
    /// Caches service metadata for quick access
    /// </summary>
    private void CacheServiceMetadata(GrpcService service)
    {
        lock (_metadataLock)
        {
            _metadata[service.FullName] = new ServiceMetadata
            {
                ServiceName = service.Name,
                FullName = service.FullName,
                Endpoint = service.Endpoint,
                Port = service.Port,
                MethodCount = service.Methods.Count,
                CachedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(Constants.ServiceRegistry.ServiceMetadataCacheDurationMinutes)
            };
        }
    }

    /// <summary>
    /// Retrieves cached service metadata
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fullName"/> is null or empty.</exception>
    public ServiceMetadata? GetCachedMetadata(string fullName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullName);

        lock (_metadataLock)
        {
            if (_metadata.TryGetValue(fullName, out var metadata))
            {
                if (metadata.ExpiresAt > DateTime.UtcNow)
                    return metadata;

                _metadata.Remove(fullName);
            }

            return null;
        }
    }

    /// <summary>
    /// Gets service health status
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fullName"/> is null or empty.</exception>
    public ServiceHealthStatus GetHealthStatus(string fullName)
    {
        ArgumentException.ThrowIfNullOrEmpty(fullName);

        var service = GetService(fullName);
        if (service is null)
            return ServiceHealthStatus.Unknown;

        return service.Status switch
        {
            ServiceStatus.Serving => ServiceHealthStatus.Healthy,
            ServiceStatus.NotServing => ServiceHealthStatus.Unhealthy,
            _ => ServiceHealthStatus.Unknown
        };
    }

    /// <summary>
    /// Gets a registry snapshot
    /// </summary>
    public RegistrySnapshot GetRegistrySnapshot()
    {
        lock (_servicesLock)
        {
            return new RegistrySnapshot
            {
                TotalServiceCount = _services.Count,
                ServiceRegistrationTimestamps = new Dictionary<string, DateTime>(_serviceRegistrationTimestamps)
            };
        }
    }

    /// <summary>
    /// Gets a registry snapshot as JSON
    /// </summary>
    public string GetRegistrySnapshotJson()
    {
        return GetRegistrySnapshot().ToJson();
    }
}

/// <summary>
/// Cached metadata for a service
/// </summary>
public sealed class ServiceMetadata
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified service name.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the service endpoint.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the service port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the number of methods exposed by the service.
    /// </summary>
    public int MethodCount { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the metadata was cached.
    /// </summary>
    public DateTime CachedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the cached metadata expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Returns a string representation of the service metadata.
    /// </summary>
    /// <returns>A string containing the service metadata property values.</returns>
    public override string ToString() => $"ServiceMetadata {{ ServiceName = {ServiceName}, FullName = {FullName}, Endpoint = {Endpoint}, Port = {Port}, MethodCount = {MethodCount}, CachedAt = {CachedAt}, ExpiresAt = {ExpiresAt} }}";
}

/// <summary>
/// Service health status enumeration
/// </summary>
public enum ServiceHealthStatus
{
    Healthy,
    Unhealthy,
    Unknown
}
