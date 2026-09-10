#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Data;

/// <summary>
/// In-memory implementation of service repository
/// </summary>
public sealed class ServiceRepository : IServiceRepository
{
    private readonly ILogger<ServiceRepository> _logger;
    private readonly Dictionary<string, GrpcService> _services = [];
    private readonly Dictionary<string, GrpcRequest> _requests = [];
    private readonly Dictionary<string, GrpcResponse> _responses = [];
    private readonly object _lock = new();

    /// <summary>
/// Initializes a new instance of the <see cref="ServiceRepository"/> class.
/// </summary>
/// <param name="logger">The logger instance for logging repository operations.</param>
/// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
public ServiceRepository(ILogger<ServiceRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
/// Adds a new service to the in-memory repository.
/// </summary>
/// <param name="service">The service to add.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>True if the service was added, false if a service with the same ID already exists.</returns>
/// <exception cref="ArgumentNullException">Thrown when service is null.</exception>
public async Task<bool> AddAsync(GrpcService service, CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        service.Validate();

        lock (_lock)
        {
            if (_services.ContainsKey(service.Id))
                return false;

            _services[service.Id] = service;
            _logger.LogInformation("Service added to repository: {ServiceId} ({ServiceName})", service.Id, service.FullName);

            return true;
        }
    }

    /// <inheritdoc/>
public async Task<GrpcService?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        lock (_lock)
        {
            return _services.TryGetValue(id, out var service) ? service : null;
        }
    }

    /// <inheritdoc/>
public async Task<GrpcService?> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        lock (_lock)
        {
            return _services.Values.FirstOrDefault(s => s.FullName == fullName);
        }
    }

    /// <inheritdoc/>
public async Task<IEnumerable<GrpcService>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return _services.Values.ToList();
        }
    }

    /// <inheritdoc/>
public async Task<IEnumerable<GrpcService>> GetByPackageAsync(string packageName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return [];

        lock (_lock)
        {
            return _services.Values
                .Where(s => s.PackageName == packageName)
                .ToList();
        }
    }

    /// <inheritdoc/>
public async Task<bool> UpdateAsync(GrpcService service, CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        service.Validate();

        lock (_lock)
        {
            if (!_services.ContainsKey(service.Id))
                return false;

            _services[service.Id] = service;
            service.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Service updated in repository: {ServiceId} ({ServiceName})", service.Id, service.FullName);

            return true;
        }
    }

    /// <inheritdoc/>
public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        lock (_lock)
        {
            if (_services.Remove(id, out var service))
            {
                _logger.LogInformation("Service deleted from repository: {ServiceId} ({ServiceName})", id, service.FullName);
                return true;
            }

            return false;
        }
    }

    /// <inheritdoc/>
public async Task<bool> ExistsAsync(string fullName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return false;

        lock (_lock)
        {
            return _services.Values.Any(s => s.FullName == fullName);
        }
    }

    /// <inheritdoc/>
public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return _services.Count;
        }
    }

    /// <inheritdoc/>
public async Task<IEnumerable<GrpcService>> SearchAsync(
        Func<GrpcService, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate is null)
            throw new ArgumentNullException(nameof(predicate));

        lock (_lock)
        {
            return _services.Values.Where(predicate).ToList();
        }
    }

    /// <inheritdoc/>
public async Task<(IEnumerable<GrpcService> Items, int Total)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
            pageNumber = 1;

        if (pageSize < 1)
            pageSize = 10;

        lock (_lock)
        {
            var total = _services.Count;
            var items = _services.Values
                .OrderBy(s => s.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (items, total);
        }
    }

    /// <summary>
/// Adds a new request record to the in-memory repository.
/// </summary>
/// <param name="request">The request to add.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>True if the request was added, false if a request with the same ID already exists.</returns>
/// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
public async Task<bool> AddRequestAsync(GrpcRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        request.Validate();

        lock (_lock)
        {
            if (_requests.ContainsKey(request.Id))
                return false;

            _requests[request.Id] = request;
            _logger.LogDebug("Request stored: {RequestId} ({MethodName})", request.Id, request.FullMethodName);

            return true;
        }
    }

    /// <inheritdoc/>
public async Task<GrpcRequest?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            return null;

        lock (_lock)
        {
            return _requests.TryGetValue(requestId, out var request) ? request : null;
        }
    }

    /// <summary>
/// Adds a new response record to the in-memory repository.
/// </summary>
/// <param name="response">The response to add.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>True if the response was added, false if a response with the same ID already exists.</returns>
/// <exception cref="ArgumentNullException">Thrown when response is null.</exception>
public async Task<bool> AddResponseAsync(GrpcResponse response, CancellationToken cancellationToken = default)
    {
        if (response is null)
            throw new ArgumentNullException(nameof(response));

        response.Validate();

        lock (_lock)
        {
            if (_responses.ContainsKey(response.Id))
                return false;

            _responses[response.Id] = response;
            _logger.LogDebug("Response stored: {ResponseId} ({Status})", response.Id, response.Status);

            return true;
        }
    }

    /// <inheritdoc/>
public async Task<GrpcResponse?> GetResponseAsync(string responseId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(responseId))
            return null;

        lock (_lock)
        {
            return _responses.TryGetValue(responseId, out var response) ? response : null;
        }
    }
}
