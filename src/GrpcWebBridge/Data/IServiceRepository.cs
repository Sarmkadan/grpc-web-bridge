#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain.Models;

namespace GrpcWebBridge.Data;

/// <summary>
/// Repository interface for gRPC service data access
/// </summary>
public interface IServiceRepository
{
    /// <summary>
    /// Adds a new service to storage
    /// </summary>
    Task<bool> AddAsync(GrpcService service, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a service by ID
    /// </summary>
    Task<GrpcService?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a service by full name
    /// </summary>
    Task<GrpcService?> GetByFullNameAsync(string fullName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all services
    /// </summary>
    Task<IEnumerable<GrpcService>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves services by package name
    /// </summary>
    Task<IEnumerable<GrpcService>> GetByPackageAsync(string packageName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing service
    /// </summary>
    Task<bool> UpdateAsync(GrpcService service, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a service by ID
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a service exists by full name
    /// </summary>
    Task<bool> ExistsAsync(string fullName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total service count
    /// </summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches services by criteria
    /// </summary>
    Task<IEnumerable<GrpcService>> SearchAsync(
        Func<GrpcService, bool> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets services with pagination
    /// </summary>
    Task<(IEnumerable<GrpcService> Items, int Total)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a request record
    /// </summary>
    Task<bool> AddRequestAsync(GrpcRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a request record by ID
    /// </summary>
    Task<GrpcRequest?> GetRequestAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a response record
    /// </summary>
    Task<bool> AddResponseAsync(GrpcResponse response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a response record by ID
    /// </summary>
    Task<GrpcResponse?> GetResponseAsync(string responseId, CancellationToken cancellationToken = default);
}
