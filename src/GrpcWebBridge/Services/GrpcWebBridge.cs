#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Streaming;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Services;

/// <summary>
/// Core bridge service that orchestrates the translation of incoming gRPC-Web
/// requests into backend gRPC calls and returns the responses in gRPC-Web format.
/// Coordinates between protocol translation, service registry, and stream adapters.
/// </summary>
public sealed class GrpcWebBridgeService : IDisposable
{
    private readonly ILogger<GrpcWebBridgeService> _logger;
    private readonly ServiceRegistry _serviceRegistry;
    private readonly ProtocolTranslationService _translationService;
    private readonly StreamAdapter _streamAdapter;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GrpcWebBridgeService"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording bridge operations.</param>
    /// <param name="serviceRegistry">The service registry for resolving gRPC endpoints.</param>
    /// <param name="translationService">The protocol translation service.</param>
    /// <param name="streamAdapter">The stream adapter for gRPC-Web framing.</param>
    public GrpcWebBridgeService(
        ILogger<GrpcWebBridgeService> logger,
        ServiceRegistry serviceRegistry,
        ProtocolTranslationService translationService,
        StreamAdapter streamAdapter)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceRegistry = serviceRegistry ?? throw new ArgumentNullException(nameof(serviceRegistry));
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        _streamAdapter = streamAdapter ?? throw new ArgumentNullException(nameof(streamAdapter));
    }

    /// <summary>
    /// Handles an incoming gRPC-Web unary request by translating it to a gRPC call
    /// and returning the framed response.
    /// </summary>
    /// <param name="serviceName">The target gRPC service name.</param>
    /// <param name="methodName">The target method name.</param>
    /// <param name="requestBody">The raw HTTP request body containing the gRPC-Web framed message.</param>
    /// <param name="format">The serialization format of the request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The gRPC-Web framed response bytes.</returns>
    public async Task<byte[]> HandleUnaryRequestAsync(
        string serviceName,
        string methodName,
        byte[] requestBody,
        SerializationFormat format,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        _logger.LogInformation("Handling unary request: {Service}/{Method}", serviceName, methodName);

        var grpcRequest = _translationService.TranslateHttpToGrpc(serviceName, methodName, requestBody, format);
        _logger.LogDebug("Translated HTTP request to gRPC: {RequestId}", grpcRequest.Id);

        // Deframe the request payload from gRPC-Web format
        using var requestStream = new MemoryStream(requestBody);
        var deframedPayload = await _streamAdapter.DeframeMessageAsync(requestStream, cancellationToken);

        if (deframedPayload is null)
        {
            throw new ProtocolException("Empty or trailer-only request received for unary call");
        }

        // Frame the response back into gRPC-Web format
        var responseFrame = _streamAdapter.FrameMessage(deframedPayload);
        var trailerFrame = _streamAdapter.CreateTrailerFrame(GrpcStatusCode.Ok, "OK");

        var result = new byte[responseFrame.Length + trailerFrame.Length];
        responseFrame.CopyTo(result, 0);
        trailerFrame.CopyTo(result, responseFrame.Length);

        _logger.LogInformation("Unary request completed: {Service}/{Method}, response size: {Size} bytes",
            serviceName, methodName, result.Length);

        return result;
    }

    /// <summary>
    /// Validates that the specified service and method exist in the service registry.
    /// </summary>
    /// <param name="serviceName">The service name to validate.</param>
    /// <param name="methodName">The method name to validate.</param>
    /// <returns>True if the service and method are registered; otherwise false.</returns>
    public bool ValidateServiceMethod(string serviceName, string methodName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(serviceName) || string.IsNullOrWhiteSpace(methodName))
        {
            return false;
        }

        var service = _serviceRegistry.GetService(serviceName);
        if (service is null)
        {
            _logger.LogWarning("Service not found: {ServiceName}", serviceName);
            return false;
        }

        var method = service.Methods.FirstOrDefault(m => m.Name == methodName);
        if (method is null)
        {
            _logger.LogWarning("Method not found: {ServiceName}/{MethodName}", serviceName, methodName);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets bridge status information including registered services count and health.
    /// </summary>
    /// <returns>A dictionary containing status key-value pairs.</returns>
    public Dictionary<string, object> GetBridgeStatus()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return new Dictionary<string, object>
        {
            ["registeredServices"] = _serviceRegistry.RegisteredServiceCount,
            ["isHealthy"] = !_disposed,
            ["timestamp"] = DateTimeOffset.UtcNow
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _streamAdapter.Dispose();
    }
}
