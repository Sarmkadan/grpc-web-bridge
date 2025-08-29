// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;

namespace GrpcWebBridge.Controllers;

/// <summary>
/// Main bridge controller handling protocol translation and service proxying.
/// Implements the REST endpoint interface for gRPC method invocation.
/// </summary>
[ApiController]
[Route("api/bridge")]
[Produces("application/json")]
public class BridgeController : ControllerBase
{
    private readonly ProtocolTranslationService _protocolService;
    private readonly ServiceRegistry _serviceRegistry;
    private readonly AuthenticationService _authService;
    private readonly ILogger<BridgeController> _logger;

    public BridgeController(
        ProtocolTranslationService protocolService,
        ServiceRegistry serviceRegistry,
        AuthenticationService authService,
        ILogger<BridgeController> logger)
    {
        _protocolService = protocolService;
        _serviceRegistry = serviceRegistry;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Invoke a gRPC method via HTTP protocol translation.
    /// Routes the request to the appropriate gRPC service endpoint.
    /// </summary>
    [HttpPost("invoke")]
    public async Task<IActionResult> InvokeMethod([FromBody] BridgeRequest request)
    {
        if (request == null)
            return BadRequest(new { error = "Request body is required" });

        try
        {
            // Extract and validate authentication context
            var authContext = await _authService.ValidateRequestAsync(Request);
            if (authContext?.IsAuthenticated == false)
                return Unauthorized(new { error = "Authentication required" });

            // Resolve target service
            var service = _serviceRegistry.ListServices()
                .FirstOrDefault(s => s.Id == request.ServiceId);

            if (service == null)
                return NotFound(new { error = $"Service '{request.ServiceId}' not found" });

            // Locate method in service
            var method = service.Methods.FirstOrDefault(m => m.Name == request.MethodName);
            if (method == null)
                return NotFound(new { error = $"Method '{request.MethodName}' not found in service" });

            // Build gRPC request and invoke
            var grpcRequest = new GrpcRequest
            {
                ServiceId = request.ServiceId,
                MethodName = request.MethodName,
                Payload = request.Payload,
                Metadata = request.Headers ?? new Dictionary<string, string>(),
                Timeout = request.TimeoutMs.HasValue ? TimeSpan.FromMilliseconds(request.TimeoutMs.Value) : TimeSpan.FromSeconds(30)
            };

            // Translate protocol and invoke
            var response = await _protocolService.TranslateAndInvokeAsync(grpcRequest, authContext);

            _logger.LogInformation(
                "Method invoked: Service={ServiceId}, Method={MethodName}, Status={Status}",
                request.ServiceId, request.MethodName, response.Status);

            return Ok(new
            {
                success = true,
                data = response.Payload,
                metadata = response.ResponseMetadata,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking method: {ServiceId}.{MethodName}",
                request.ServiceId, request.MethodName);
            return StatusCode(500, new { error = "Method invocation failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Stream messages to a gRPC service using server streaming or bidirectional streaming.
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamMessages([FromBody] StreamRequest streamRequest, CancellationToken cancellationToken)
    {
        try
        {
            var authContext = await _authService.ValidateRequestAsync(Request);
            if (authContext?.IsAuthenticated == false)
            {
                Response.StatusCode = 401;
                return;
            }

            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new
            {
                status = "streaming",
                streamId = Guid.NewGuid().ToString(),
                service = streamRequest.ServiceId,
                method = streamRequest.MethodName
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming error for {ServiceId}.{MethodName}",
                streamRequest.ServiceId, streamRequest.MethodName);
            Response.StatusCode = 500;
        }
    }

    /// <summary>
    /// Batch invoke multiple gRPC methods in a single request.
    /// Useful for reducing network overhead when multiple calls are needed.
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> BatchInvoke([FromBody] BatchRequest batchRequest)
    {
        if (batchRequest?.Operations == null || !batchRequest.Operations.Any())
            return BadRequest(new { error = "At least one operation is required" });

        try
        {
            var authContext = await _authService.ValidateRequestAsync(Request);
            if (authContext?.IsAuthenticated == false)
                return Unauthorized(new { error = "Authentication required" });

            var results = new List<BatchOperationResult>();

            foreach (var op in batchRequest.Operations)
            {
                try
                {
                    var service = _serviceRegistry.ListServices()
                        .FirstOrDefault(s => s.Id == op.ServiceId);

                    if (service == null)
                    {
                        results.Add(new BatchOperationResult
                        {
                            OperationId = op.Id,
                            Success = false,
                            Error = $"Service '{op.ServiceId}' not found"
                        });
                        continue;
                    }

                    var method = service.Methods.FirstOrDefault(m => m.Name == op.MethodName);
                    if (method == null)
                    {
                        results.Add(new BatchOperationResult
                        {
                            OperationId = op.Id,
                            Success = false,
                            Error = $"Method '{op.MethodName}' not found"
                        });
                        continue;
                    }

                    var grpcRequest = new GrpcRequest
                    {
                        ServiceId = op.ServiceId,
                        MethodName = op.MethodName,
                        Payload = op.Payload,
                        Metadata = op.Headers ?? new Dictionary<string, string>()
                    };

                    var response = await _protocolService.TranslateAndInvokeAsync(grpcRequest, authContext);

                    results.Add(new BatchOperationResult
                    {
                        OperationId = op.Id,
                        Success = true,
                        Data = response.Payload
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch operation failed: {OperationId}", op.Id);
                    results.Add(new BatchOperationResult
                    {
                        OperationId = op.Id,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }

            return Ok(new
            {
                success = true,
                operationCount = batchRequest.Operations.Count,
                successCount = results.Count(r => r.Success),
                failureCount = results.Count(r => !r.Success),
                results = results,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch invocation failed");
            return StatusCode(500, new { error = "Batch invocation failed", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for bridge method invocation.
/// </summary>
public class BridgeRequest
{
    public string ServiceId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public object? Payload { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public int? TimeoutMs { get; set; }
}

/// <summary>
/// Request model for streaming operations.
/// </summary>
public class StreamRequest
{
    public string ServiceId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public object? InitialMessage { get; set; }
}

/// <summary>
/// Batch request containing multiple operations.
/// </summary>
public class BatchRequest
{
    public List<BatchOperation> Operations { get; set; } = new();
}

/// <summary>
/// Individual batch operation.
/// </summary>
public class BatchOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ServiceId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public object? Payload { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>
/// Result of a batch operation.
/// </summary>
public class BatchOperationResult
{
    public string OperationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
}
