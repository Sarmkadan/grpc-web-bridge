#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using System.Diagnostics;

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
    private readonly StreamingService _streamingService;
    private readonly ILogger<BridgeController> _logger;

    public BridgeController(
        ProtocolTranslationService protocolService,
        ServiceRegistry serviceRegistry,
        AuthenticationService authService,
        StreamingService streamingService,
        ILogger<BridgeController> logger)
    {
        _protocolService = protocolService;
        _serviceRegistry = serviceRegistry;
        _authService = authService;
        _streamingService = streamingService;
        _logger = logger;
    }

    /// <summary>
    /// Invoke a gRPC method via HTTP protocol translation.
    /// Routes the request to the appropriate gRPC service endpoint.
    /// </summary>
    [HttpPost("invoke")]
    public async Task<IActionResult> InvokeMethod([FromBody] BridgeRequest request)
    {
        if (request is null)
            return BadRequest(new { error = "Request body is required" });

        var sw = Stopwatch.StartNew();
        try
        {
            var authContext = ExtractAuthContext();

            // Resolve target service
            var service = _serviceRegistry.ListServices()
                .FirstOrDefault(s => s.Id == request.ServiceId || s.Name == request.ServiceId);

            if (service is null)
                return NotFound(new { error = $"Service '{request.ServiceId}' not found" });

            // Locate method in service
            var method = service.Methods.FirstOrDefault(m => m.Name == request.MethodName);
            if (method is null)
                return NotFound(new { error = $"Method '{request.MethodName}' not found in service" });

            // Build gRPC request and invoke
            var grpcRequest = new GrpcRequest
            {
                ServiceName = request.ServiceId,
                MethodName = request.MethodName,
                Payload = request.Payload is byte[] rawBytes ? rawBytes : [],
                Metadata = request.Headers ?? new Dictionary<string, string>(),
                TimeoutMilliseconds = request.TimeoutMs ?? 30000
            };

            // Translate protocol and invoke
            var response = await _protocolService.TranslateAndInvokeAsync(grpcRequest, authContext);

            sw.Stop();
            BridgePrometheusMetrics.RecordCall(
                request.ServiceId, request.MethodName,
                response.Status.ToString(), sw.Elapsed.TotalSeconds);

            _logger.LogInformation(
                "Method invoked: Service={ServiceId}, Method={MethodName}, Status={Status}",
                request.ServiceId, request.MethodName, response.Status);

            return Ok(new
            {
                success = true,
                data = response.Payload,
                metadata = response.Metadata,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            BridgePrometheusMetrics.RecordCall(
                request.ServiceId, request.MethodName,
                "INTERNAL", sw.Elapsed.TotalSeconds);
            _logger.LogError(ex, "Error invoking method: {ServiceId}.{MethodName}",
                request.ServiceId, request.MethodName);
            return StatusCode(500, new { error = "Method invocation failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Stream messages to a gRPC service using server streaming or bidirectional streaming.
    /// Detects client disconnection via the request cancellation token and closes the upstream
    /// stream to prevent resource leaks.
    /// </summary>
    [HttpPost("stream")]
    public async Task StreamMessages([FromBody] StreamRequest streamRequest, CancellationToken cancellationToken)
    {
        var streamId = Guid.NewGuid().ToString();
        GrpcWebBridge.Services.Stream? stream = null;

        try
        {
            var authContext = ExtractAuthContext();
            if (authContext?.IsAuthenticated == false)
            {
                Response.StatusCode = 401;
                return;
            }

            // Determine method type from service registry
            var service = _serviceRegistry.ListServices()
                .FirstOrDefault(s => s.Id == streamRequest.ServiceId || s.Name == streamRequest.ServiceId);
            var method = service?.Methods.FirstOrDefault(m => m.Name == streamRequest.MethodName);
            var methodType = method?.Type ?? MethodType.ServerStreaming;

            stream = _streamingService.CreateStream(streamId, methodType);
            BridgePrometheusMetrics.ActiveStreams.Inc();

            // When the client disconnects, close the upstream stream immediately to avoid
            // resource leaks (thread accumulation and connection pool exhaustion).
            var registration = cancellationToken.Register(() =>
            {
                try
                {
                    _streamingService.CloseStream(streamId, GrpcStatusCode.Cancelled, "Client disconnected");
                    _logger.LogInformation("Stream {StreamId} closed due to client disconnect", streamId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing stream {StreamId} on client disconnect", streamId);
                }
            });

            await using (registration)
            {
                Response.ContentType = "application/json";
                await Response.WriteAsJsonAsync(new
                {
                    status = "streaming",
                    streamId,
                    service = streamRequest.ServiceId,
                    method = streamRequest.MethodName
                }, cancellationToken);

                // Drain queued messages until the client disconnects or the stream closes.
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = _streamingService.DequeueMessage(streamId);
                    if (message is not null)
                    {
                        await Response.WriteAsJsonAsync(new
                        {
                            sequenceNumber = message.SequenceNumber,
                            type = message.MessageType.ToString(),
                            data = message.Data
                        }, cancellationToken);
                        await Response.Body.FlushAsync(cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(50, cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected — stream cleanup is handled by the cancellation registration above.
            _logger.LogDebug("Stream {StreamId} ended by client disconnect", streamId);
        }
        catch (Exception ex)
        {
            BridgePrometheusMetrics.RecordStreamError(streamRequest.ServiceId, streamRequest.MethodName);
            _logger.LogError(ex, "Streaming error for {ServiceId}.{MethodName}",
                streamRequest.ServiceId, streamRequest.MethodName);
            Response.StatusCode = 500;
        }
        finally
        {
            // Ensure the stream is removed even if cancellation callback did not fire.
            _streamingService.CloseStream(streamId, GrpcStatusCode.Ok, "Stream completed");
            BridgePrometheusMetrics.ActiveStreams.Dec();
        }
    }

    /// <summary>
    /// Batch invoke multiple gRPC methods in a single request.
    /// Useful for reducing network overhead when multiple calls are needed.
    /// </summary>
    [HttpPost("batch")]
    public async Task<IActionResult> BatchInvoke([FromBody] BatchRequest batchRequest)
    {
        if (batchRequest?.Operations is null || !batchRequest.Operations.Any())
            return BadRequest(new { error = "At least one operation is required" });

        try
        {
            var authContext = ExtractAuthContext();

            var results = new List<BatchOperationResult>();

            foreach (var op in batchRequest.Operations)
            {
                try
                {
                    var service = _serviceRegistry.ListServices()
                        .FirstOrDefault(s => s.Id == op.ServiceId);

                    if (service is null)
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
                    if (method is null)
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
                        ServiceName = op.ServiceId,
                        MethodName = op.MethodName,
                        Payload = op.Payload is byte[] rawBytes ? rawBytes : [],
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

    private GrpcWebBridge.Domain.Models.AuthenticationContext? ExtractAuthContext()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var token = _authService.ExtractBearerToken(authHeader);
        if (token is null) return null;
        try { return _authService.AuthenticateBearer(token); }
        catch { return null; }
    }
}

/// <summary>
/// Request model for bridge method invocation.
/// </summary>
public sealed class BridgeRequest
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
public sealed class StreamRequest
{
    public string ServiceId { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public object? InitialMessage { get; set; }
}

/// <summary>
/// Batch request containing multiple operations.
/// </summary>
public sealed class BatchRequest
{
    public List<BatchOperation> Operations { get; set; } = new();
}

/// <summary>
/// Individual batch operation.
/// </summary>
public sealed class BatchOperation
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
public sealed class BatchOperationResult
{
    public string OperationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }
}
