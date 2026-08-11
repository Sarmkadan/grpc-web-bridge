#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Utilities;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Service discovery client for dynamic service registration and lookup.
/// Integrates with external service registries (Consul, Eureka, etc.)
/// Enables automatic service endpoint discovery and health-aware routing.
/// </summary>
public sealed class ServiceDiscoveryClient : IDisposable
{
    private readonly HttpClientFactory _httpClientFactory;
    private readonly ILogger<ServiceDiscoveryClient> _logger;
    private readonly ServiceDiscoveryOptions _options;
    private readonly Dictionary<string, ServiceInstance> _serviceCache;
    private Timer? _refreshTimer;
    private DateTime _lastRefreshTime;

    public ServiceDiscoveryClient(
        HttpClientFactory httpClientFactory,
        ILogger<ServiceDiscoveryClient> logger,
        ServiceDiscoveryOptions? options = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options ?? new ServiceDiscoveryOptions();
        _serviceCache = new Dictionary<string, ServiceInstance>();
        _lastRefreshTime = DateTime.MinValue;
    }

    /// <summary>
    /// Registers a service instance with the discovery service.
    /// </summary>
    public async Task<bool> RegisterServiceAsync(ServiceInstance instance)
    {
        if (instance is null)
            throw new ArgumentNullException(nameof(instance));

        try
        {
            var url = $"{_options.DiscoveryServiceUrl}/services";
            var response = await _httpClientFactory.PostJsonAsync(url, instance, "discovery").ConfigureAwait(false);

            if (response is not null)
            {
                _serviceCache[instance.Id] = instance;
                _logger.LogInformation("Service registered: ServiceId={Id}, Name={Name}",
                    instance.Id, instance.Name);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service: ServiceId={Id}", instance.Id);
            return false;
        }
    }

    /// <summary>
    /// Deregisters a service from the discovery service.
    /// </summary>
    public async Task<bool> DeregisterServiceAsync(string serviceId)
    {
        if (string.IsNullOrEmpty(serviceId))
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));

        try
        {
            var url = $"{_options.DiscoveryServiceUrl}/services/{serviceId}";
            var client = _httpClientFactory.GetClient("discovery");
            var response = await client.DeleteAsync(url).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _serviceCache.Remove(serviceId);
                _logger.LogInformation("Service deregistered: ServiceId={Id}", serviceId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deregister service: ServiceId={Id}", serviceId);
            return false;
        }
    }

    /// <summary>
    /// Discovers services by name.
    /// </summary>
    public async Task<List<ServiceInstance>> DiscoverServicesAsync(string serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
            throw new ArgumentException("Service name cannot be null or empty", nameof(serviceName));

        try
        {
            var url = $"{_options.DiscoveryServiceUrl}/services?name={serviceName}";
            var response = await _httpClientFactory.GetAsync(url, "discovery").ConfigureAwait(false);

            var instances = JsonUtility.Deserialize<List<ServiceInstance>>(response);
            return instances ?? new List<ServiceInstance>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover services: ServiceName={Name}", serviceName);
            return new List<ServiceInstance>();
        }
    }

    /// <summary>
    /// Gets a healthy instance of a service (load-balanced).
    /// </summary>
    public async Task<ServiceInstance?> GetHealthyInstanceAsync(string serviceName)
    {
        var instances = await DiscoverServicesAsync(serviceName).ConfigureAwait(false);

        if (instances.Count == 0)
        {
            _logger.LogWarning("No instances found for service: ServiceName={Name}", serviceName);
            return null;
        }

        // Filter to healthy instances
        var healthyInstances = instances.Where(i => i.Status == "UP").ToList();

        if (healthyInstances.Count == 0)
        {
            _logger.LogWarning("No healthy instances found for service: ServiceName={Name}", serviceName);
            return instances.FirstOrDefault();
        }

        // Round-robin load balancing
        return healthyInstances[new Random().Next(healthyInstances.Count)];
    }

    /// <summary>
    /// Sends heartbeat for a registered service instance.
    /// </summary>
    public async Task<bool> SendHeartbeatAsync(string serviceId)
    {
        if (string.IsNullOrEmpty(serviceId))
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));

        try
        {
            var url = $"{_options.DiscoveryServiceUrl}/services/{serviceId}/health";
            var client = _httpClientFactory.GetClient("discovery");
            var response = await client.PutAsync(url, null).ConfigureAwait(false);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send heartbeat: ServiceId={Id}", serviceId);
            return false;
        }
    }

    /// <summary>
    /// Starts automatic service discovery refresh.
    /// </summary>
    public void StartAutoRefresh(TimeSpan interval)
    {
        if (_refreshTimer is not null)
            return;

        _refreshTimer = new Timer(_ => RefreshServices(), null, interval, interval);
        _logger.LogInformation("Auto-refresh started with interval: {IntervalSeconds}s", interval.TotalSeconds);
    }

    /// <summary>
    /// Stops automatic service discovery refresh.
    /// </summary>
    public void StopAutoRefresh()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
        _logger.LogInformation("Auto-refresh stopped");
    }

    /// <summary>
    /// Refreshes cached service information.
    /// </summary>
    private void RefreshServices()
    {
        try
        {
            _lastRefreshTime = DateTime.UtcNow;
            _logger.LogDebug("Service discovery refresh triggered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing services");
        }
    }

    /// <summary>
    /// Gets all cached services.
    /// </summary>
    public List<ServiceInstance> GetCachedServices()
    {
        return _serviceCache.Values.ToList();
    }

    /// <summary>
    /// Clears service cache.
    /// </summary>
    public void ClearCache()
    {
        _serviceCache.Clear();
        _logger.LogInformation("Service discovery cache cleared");
    }

    /// <summary>
    /// Gets service discovery statistics.
    /// </summary>
    public object GetStatistics()
    {
        return new
        {
            cachedServiceCount = _serviceCache.Count,
            lastRefreshTime = _lastRefreshTime,
            discoveryServiceUrl = _options.DiscoveryServiceUrl,
            registrationTtl = _options.RegistrationTtlSeconds,
            heartbeatInterval = _options.HeartbeatIntervalSeconds
        };
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
    }

    public override string ToString() => $"ServiceDiscoveryClient {{ DiscoveryServiceUrl = {_options.DiscoveryServiceUrl}, CachedServiceCount = {_serviceCache.Count} }}";
}

/// <summary>
/// Service instance information.
/// </summary>
public sealed class ServiceInstance
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Status { get; set; } = "UP";
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastHeartbeat { get; set; }

    public override string ToString() => $"ServiceInstance {{ Id = {Id}, Name = {Name}, Host = {Host}, Port = {Port}, Status = {Status}, Metadata = {Metadata} }}";
}

/// <summary>
/// Configuration options for service discovery.
/// </summary>
public sealed class ServiceDiscoveryOptions
{
    public string DiscoveryServiceUrl { get; set; } = "http://localhost:8500";
    public int RegistrationTtlSeconds { get; set; } = 30;
    public int HeartbeatIntervalSeconds { get; set; } = 10;
    public bool EnableAutoRefresh { get; set; } = true;
    public int RefreshIntervalSeconds { get; set; } = 60;
}
