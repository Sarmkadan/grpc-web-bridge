#nullable enable

using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Services;

/// <summary>
/// Extension methods for ServiceRegistry providing additional utility functionality
/// </summary>
public static class ServiceRegistryExtensions
{
    /// <summary>
    /// Attempts to get a service by name and package, returning a default value if not found
    /// </summary>
    public static GrpcService? GetServiceOrDefault(this ServiceRegistry registry, string serviceName, string packageName, GrpcService? defaultValue = null)
    {
        if (registry is null)
            throw new ArgumentNullException(nameof(registry));

        if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(packageName))
            return defaultValue;

        var fullName = $"{packageName}.{serviceName}";
        return registry.GetService(fullName) ?? defaultValue;
    }

    /// <summary>
    /// Gets all services that match a specific endpoint
    /// </summary>
    public static IEnumerable<GrpcService> GetServicesByEndpoint(this ServiceRegistry registry, string endpoint)
    {
        if (registry is null)
            throw new ArgumentNullException(nameof(registry));

        if (string.IsNullOrWhiteSpace(endpoint))
            return Enumerable.Empty<GrpcService>();

        return registry.ListServices()
            .Where(s => string.Equals(s.Endpoint, endpoint, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Checks if any service is registered with the given health status
    /// </summary>
    public static bool HasServiceWithHealthStatus(this ServiceRegistry registry, ServiceHealthStatus status)
    {
        if (registry is null)
            throw new ArgumentNullException(nameof(registry));

        return registry.ListServices()
            .Any(s => registry.GetHealthStatus(s.FullName) == status);
    }

    /// <summary>
    /// Gets a dictionary of all services grouped by their package name
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<GrpcService>> GetServicesByPackageDictionary(this ServiceRegistry registry)
    {
        if (registry is null)
            throw new ArgumentNullException(nameof(registry));

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