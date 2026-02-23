#nullable enable

using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Services;

/// <summary>
/// Extension methods for <see cref="ServiceRegistry"/> providing additional utility functionality
/// </summary>
public static class ServiceRegistryExtensions
{
    /// <summary>
    /// Attempts to get a service by name and package, returning a default value if not found
    /// </summary>
    /// <param name="registry">The service registry instance</param>
    /// <param name="serviceName">The service name</param>
    /// <param name="packageName">The package name</param>
    /// <param name="defaultValue">The default value to return if service is not found</param>
    /// <returns>The found service or default value if not found</returns>
    public static GrpcService? GetServiceOrDefault(
        this ServiceRegistry registry,
        string serviceName,
        string packageName,
        GrpcService? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentException.ThrowIfNullOrEmpty(packageName);

        var fullName = $"{packageName}.{serviceName}";
        return registry.GetService(fullName) ?? defaultValue;
    }

    /// <summary>
    /// Gets all services that match a specific endpoint
    /// </summary>
    /// <param name="registry">The service registry instance</param>
    /// <param name="endpoint">The endpoint to filter by</param>
    /// <returns>Collection of services matching the endpoint</returns>
    /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
    /// <exception cref="ArgumentException">Thrown if endpoint is null or empty</exception>
    public static IEnumerable<GrpcService> GetServicesByEndpoint(
        this ServiceRegistry registry,
        string endpoint)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        return registry.ListServices()
            .Where(s => string.Equals(s.Endpoint, endpoint, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Checks if any service is registered with the given health status
    /// </summary>
    /// <param name="registry">The service registry instance</param>
    /// <param name="status">The health status to check for</param>
    /// <returns>True if any service has the specified health status</returns>
    /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
    public static bool HasServiceWithHealthStatus(
        this ServiceRegistry registry,
        ServiceHealthStatus status)
    {
        ArgumentNullException.ThrowIfNull(registry);

        return registry.ListServices()
            .Any(s => registry.GetHealthStatus(s.FullName) == status);
    }

    /// <summary>
    /// Gets a dictionary of all services grouped by their package name
    /// </summary>
    /// <param name="registry">The service registry instance</param>
    /// <returns>Read-only dictionary mapping package names to lists of services</returns>
    /// <exception cref="ArgumentNullException">Thrown if registry is null</exception>
    public static IReadOnlyDictionary<string, IReadOnlyList<GrpcService>> GetServicesByPackageDictionary(
        this ServiceRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var result = new Dictionary<string, List<GrpcService>>(StringComparer.OrdinalIgnoreCase);

        foreach (var service in registry.ListServices())
        {
            if (string.IsNullOrWhiteSpace(service.PackageName))
                continue;

            if (!result.TryGetValue(service.PackageName, out var services))
            {
                services = new List<GrpcService>();
                result[service.PackageName] = services;
            }

            services.Add(service);
        }

        return result.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<GrpcService>)kvp.Value.AsReadOnly()
        );
    }
}