#nullable enable

using System.Globalization;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Provides extension methods for <see cref="ServiceDiscoveryClient"/> to simplify common service discovery operations.
/// Includes convenience methods for service registration, discovery, health checks, and monitoring.
/// </summary>
public static class ServiceDiscoveryClientExtensions
{
    /// <summary>
    /// Registers a service instance with the discovery service and returns the service ID.
    /// </summary>
    /// <param name="client">The service discovery client.</param>
    /// <param name="instance">The service instance to register.</param>
    /// <returns>The registered service ID if successful; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="instance"/> is <see langword="null"/>.</exception>
    public static async Task<string?> RegisterServiceAsync(this ServiceDiscoveryClient client, ServiceInstance instance)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(instance);

        var success = await client.RegisterServiceAsync(instance).ConfigureAwait(false);
        return success ? instance.Id : null;
    }

    /// <summary>
    /// Deregisters a service by its instance and returns whether the operation succeeded.
    /// </summary>
    /// <param name="client">The service discovery client.</param>
    /// <param name="instance">The service instance to deregister.</param>
    /// <returns><see langword="true"/> if the service was successfully deregistered; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="instance"/> is <see langword="null"/>.</exception>
    public static async Task<bool> DeregisterServiceAsync(this ServiceDiscoveryClient client, ServiceInstance instance)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(instance);

        return await client.DeregisterServiceAsync(instance.Id).ConfigureAwait(false);
    }

    /// <summary>
    /// Discovers all services matching the specified name and returns them as a read-only list.
    /// </summary>
    /// <param name="client">The service discovery client.</param>
    /// <param name="serviceName">The name of the service to discover.</param>
    /// <returns>A read-only list of matching service instances.</returns>
    /// <exception cref="ArgumentException"><paramref name="serviceName"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static async Task<IReadOnlyList<ServiceInstance>> DiscoverServicesAsync(this ServiceDiscoveryClient client, string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(client);

        var instances = await client.DiscoverServicesAsync(serviceName).ConfigureAwait(false);
        return instances.AsReadOnly();
    }

    /// <summary>
    /// Gets a healthy instance of the specified service, or <see langword="null"/> if none are available.
    /// </summary>
    /// <param name="client">The service discovery client.</param>
    /// <param name="serviceName">The name of the service to find.</param>
    /// <returns>A healthy service instance, or <see langword="null"/> if none are available.</returns>
    /// <exception cref="ArgumentException"><paramref name="serviceName"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static async Task<ServiceInstance?> GetHealthyInstanceAsync(this ServiceDiscoveryClient client, string serviceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serviceName);
        ArgumentNullException.ThrowIfNull(client);

        return await client.GetHealthyInstanceAsync(serviceName).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a heartbeat for the specified service instance and returns whether the operation succeeded.
    /// </summary>
    /// <param name="client">The service discovery client.</param>
    /// <param name="instance">The service instance to send heartbeat for.</param>
    /// <returns><see langword="true"/> if the heartbeat was successfully sent; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> or <paramref name="instance"/> is <see langword="null"/>.</exception>
    public static async Task<bool> SendHeartbeatAsync(this ServiceDiscoveryClient client, ServiceInstance instance)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(instance);

        return await client.SendHeartbeatAsync(instance.Id).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts automatic service discovery refresh with the specified interval.
    /// </summary>
    /// <param name="client">The service discovery client.</param>
    /// <param name="interval">The refresh interval.</param>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is not positive.</exception>
    public static void StartAutoRefresh(this ServiceDiscoveryClient client, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        client.StartAutoRefresh(interval);
    }

    /// <summary>
    /// Gets all cached services as a read-only list.
    /// </summary>
    /// <param name="client">The service discovery client.</param>
    /// <returns>A read-only list of cached service instances.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ServiceInstance> GetCachedServices(this ServiceDiscoveryClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var services = client.GetCachedServices();
        return services.AsReadOnly();
    }

    /// <summary>
    /// Gets service discovery statistics formatted as a dictionary.
    /// </summary>
    /// <param name="client">The service discovery client.</param>
    /// <returns>A dictionary containing service discovery statistics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    public static Dictionary<string, string> GetStatistics(this ServiceDiscoveryClient client)
    {
        ArgumentNullException.ThrowIfNull(client);

        var stats = client.GetStatistics();

        // Handle null case explicitly
        if (stats is null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        // Handle dictionary case directly
        if (stats is Dictionary<string, string> dict)
        {
            return dict;
        }

        // Convert anonymous object to dictionary using reflection
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var property in stats.GetType().GetProperties())
        {
            var value = property.GetValue(stats)?.ToString() ?? string.Empty;
            result[property.Name] = value;
        }

        return result;
    }
}
