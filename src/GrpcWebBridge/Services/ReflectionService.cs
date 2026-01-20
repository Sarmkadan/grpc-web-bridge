#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Services;

/// <summary>
/// Provides gRPC server reflection protocol support, enabling runtime discovery
/// of services and methods registered with the bridge.
/// <para>
/// Reflection allows web clients, CLI tools (grpcurl, grpc_cli), and API explorers
/// to introspect available services without prior knowledge of proto descriptor files.
/// This service wraps <see cref="ServiceRegistry"/> and projects its data into the
/// reflection descriptor model.
/// </para>
/// </summary>
public sealed class ReflectionService
{
    private readonly ILogger<ReflectionService> _logger;
    private readonly ServiceRegistry _serviceRegistry;

    /// <summary>
    /// Initializes a new instance of <see cref="ReflectionService"/>.
    /// </summary>
    /// <param name="logger">Structured logger.</param>
    /// <param name="serviceRegistry">Registry containing all registered gRPC services.</param>
    public ReflectionService(ILogger<ReflectionService> logger, ServiceRegistry serviceRegistry)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));
    }

    /// <summary>
    /// Returns the fully-qualified names of all services currently registered with the bridge.
    /// </summary>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A <see cref="ReflectionResult{T}"/> containing an alphabetically-sorted list of
    /// service full names on success, or an error message on failure.
    /// </returns>
    public async Task<ReflectionResult<IReadOnlyList<string>>> ListServiceNamesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await Task.Yield();

            _logger.LogDebug("Reflection: listing all registered service names");

            var names = _serviceRegistry
                .ListServices()
                .Select(s => s.FullName)
                .OrderBy(n => n)
                .ToList()
                .AsReadOnly();

            _logger.LogInformation("Reflection: returned {Count} service name(s)", names.Count);

            return ReflectionResult<IReadOnlyList<string>>.Ok(names);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reflection: failed to list service names");
            return ReflectionResult<IReadOnlyList<string>>.Fail("Unable to retrieve service names");
        }
    }

    /// <summary>
    /// Returns a <see cref="GrpcServiceDescriptor"/> for a specific registered service.
    /// </summary>
    /// <param name="fullName">
    /// The fully-qualified service name, e.g. <c>mypackage.MyService</c>.
    /// </param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A successful <see cref="ReflectionResult{T}"/> with the descriptor when the service
    /// is found; a failure result with an explanatory message when it is not.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fullName"/> is null or whitespace.</exception>
    public async Task<ReflectionResult<GrpcServiceDescriptor>> GetServiceDescriptorAsync(
        string fullName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Service full name cannot be empty", nameof(fullName));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await Task.Yield();

            _logger.LogDebug("Reflection: resolving descriptor for service {FullName}", fullName);

            var service = _serviceRegistry.GetService(fullName);
            if (service is null)
            {
                _logger.LogWarning("Reflection: service not found: {FullName}", fullName);
                return ReflectionResult<GrpcServiceDescriptor>.Fail($"Service '{fullName}' is not registered");
            }

            return ReflectionResult<GrpcServiceDescriptor>.Ok(BuildServiceDescriptor(service));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reflection: error resolving descriptor for {FullName}", fullName);
            return ReflectionResult<GrpcServiceDescriptor>.Fail($"Failed to resolve descriptor: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns descriptors for all services currently registered with the bridge.
    /// </summary>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A <see cref="ReflectionResult{T}"/> with an alphabetically-sorted list of
    /// <see cref="GrpcServiceDescriptor"/> instances on success, or an error message on failure.
    /// </returns>
    public async Task<ReflectionResult<IReadOnlyList<GrpcServiceDescriptor>>> GetAllDescriptorsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await Task.Yield();

            _logger.LogDebug("Reflection: building full service descriptor list");

            var descriptors = _serviceRegistry
                .ListServices()
                .OrderBy(s => s.FullName)
                .Select(BuildServiceDescriptor)
                .ToList()
                .AsReadOnly();

            _logger.LogInformation("Reflection: returned {Count} descriptor(s)", descriptors.Count);

            return ReflectionResult<IReadOnlyList<GrpcServiceDescriptor>>.Ok(descriptors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reflection: error building descriptor list");
            return ReflectionResult<IReadOnlyList<GrpcServiceDescriptor>>.Fail("Failed to retrieve service descriptors");
        }
    }

    /// <summary>
    /// Returns a <see cref="MethodDescriptor"/> for a specific method on a service.
    /// </summary>
    /// <param name="serviceFullName">The fully-qualified service name.</param>
    /// <param name="methodName">The unqualified or fully-qualified method name to look up.</param>
    /// <param name="cancellationToken">Token to observe for cancellation.</param>
    /// <returns>
    /// A successful <see cref="ReflectionResult{T}"/> with the method descriptor when found;
    /// a failure result when the service or method cannot be located.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="serviceFullName"/> or <paramref name="methodName"/> is null or whitespace.
    /// </exception>
    public async Task<ReflectionResult<MethodDescriptor>> GetMethodDescriptorAsync(
        string serviceFullName,
        string methodName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceFullName))
            throw new ArgumentException("Service full name cannot be empty", nameof(serviceFullName));

        if (string.IsNullOrWhiteSpace(methodName))
            throw new ArgumentException("Method name cannot be empty", nameof(methodName));

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await Task.Yield();

            _logger.LogDebug(
                "Reflection: resolving method descriptor {MethodName} on {ServiceFullName}",
                methodName, serviceFullName);

            var service = _serviceRegistry.GetService(serviceFullName);
            if (service is null)
            {
                _logger.LogWarning(
                    "Reflection: service not found during method lookup: {ServiceFullName}", serviceFullName);
                return ReflectionResult<MethodDescriptor>.Fail(
                    $"Service '{serviceFullName}' is not registered");
            }

            var method = service.GetMethod(methodName);
            if (method is null)
            {
                _logger.LogWarning(
                    "Reflection: method not found: {MethodName} on {ServiceFullName}",
                    methodName, serviceFullName);
                return ReflectionResult<MethodDescriptor>.Fail(
                    $"Method '{methodName}' not found on service '{serviceFullName}'");
            }

            return ReflectionResult<MethodDescriptor>.Ok(BuildMethodDescriptor(method, serviceFullName));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Reflection: error resolving method {MethodName} on {ServiceFullName}",
                methodName, serviceFullName);
            return ReflectionResult<MethodDescriptor>.Fail($"Failed to resolve method descriptor: {ex.Message}");
        }
    }

    private static GrpcServiceDescriptor BuildServiceDescriptor(GrpcService service) =>
        new()
        {
            FullName = service.FullName,
            Name = service.Name,
            PackageName = service.PackageName,
            Description = service.Description,
            Endpoint = service.Endpoint,
            Port = service.Port,
            UseTls = service.UseTls,
            Methods = service.Methods
                .OrderBy(m => m.Name)
                .Select(m => BuildMethodDescriptor(m, service.FullName))
                .ToList()
                .AsReadOnly()
        };

    private static MethodDescriptor BuildMethodDescriptor(GrpcMethod method, string serviceFullName) =>
        new()
        {
            Name = method.Name,
            FullName = method.FullName,
            ServiceFullName = serviceFullName,
            MethodType = method.Type.ToString(),
            IsClientStreaming = method.Type is MethodType.ClientStreaming or MethodType.BidirectionalStreaming,
            IsServerStreaming = method.Type is MethodType.ServerStreaming or MethodType.BidirectionalStreaming,
            InputMessageType = method.InputMessageType,
            OutputMessageType = method.OutputMessageType,
            IsDeprecated = method.IsDeprecated,
            Description = method.Description,
            TimeoutMilliseconds = method.TimeoutMilliseconds
        };
}
