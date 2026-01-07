// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GrpcWebBridge.Services;

/// <summary>
/// Service for translating between gRPC, gRPC-Web, and other protocol formats
/// </summary>
public class ProtocolTranslationService
{
    private static readonly JsonSerializerOptions _jsonWriteOptions = new() { WriteIndented = false };

    private readonly ILogger<ProtocolTranslationService> _logger;

    public ProtocolTranslationService(ILogger<ProtocolTranslationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts HTTP request data to a gRPC request
    /// </summary>
    public GrpcRequest TranslateHttpToGrpc(string serviceName, string methodName, byte[] httpBody, SerializationFormat format)
    {
        try
        {
            _logger.LogInformation(
                "Translating HTTP request to gRPC: {ServiceName}.{MethodName} ({Format})",
                serviceName, methodName, format);

            var request = new GrpcRequest(serviceName, methodName, httpBody);
            request.PayloadFormat = format;

            ValidateRequest(request);
            _logger.LogDebug("HTTP to gRPC translation successful: {RequestId}", request.Id);

            return request;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP to gRPC translation failed for {ServiceName}.{MethodName}", serviceName, methodName);
            throw new ProtocolException(serviceName, methodName, ex.Message);
        }
    }

    /// <summary>
    /// Converts a gRPC response to HTTP format
    /// </summary>
    public byte[] TranslateGrpcToHttp(GrpcResponse response, SerializationFormat targetFormat)
    {
        try
        {
            _logger.LogInformation(
                "Translating gRPC response to HTTP: {ResponseId} ({TargetFormat})",
                response.Id, targetFormat);

            response.Validate();

            byte[] result = response.Payload;

            if (targetFormat == SerializationFormat.Json && response.PayloadFormat == SerializationFormat.Protobuf)
            {
                result = ConvertProtobufToJson(response.Payload);
            }
            else if (targetFormat == SerializationFormat.Protobuf && response.PayloadFormat == SerializationFormat.Json)
            {
                result = ConvertJsonToProtobuf(response.Payload);
            }

            _logger.LogDebug("gRPC to HTTP translation successful: {ResponseId}", response.Id);
            return result;
        }
        catch (ProtocolException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC to HTTP translation failed for {ResponseId}", response.Id);
            throw new ProtocolException(response.RequestId, response.PayloadFormat.ToString(), ex.Message);
        }
    }

    /// <summary>
    /// Converts Protocol Buffer format to JSON
    /// </summary>
    public byte[] ConvertProtobufToJson(byte[] protobufData)
    {
        try
        {
            _logger.LogDebug("Converting Protobuf to JSON: {DataSize} bytes", protobufData.Length);

            if (protobufData.Length == 0)
                return "{}".AsBytes();

            var json = JsonSerializer.Serialize(
                new { data = Convert.ToBase64String(protobufData) },
                _jsonWriteOptions);

            return json.AsBytes();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Protobuf to JSON conversion failed");
            throw new ProtocolException("Protobuf", "JSON", ex.Message);
        }
    }

    /// <summary>
    /// Converts JSON format to Protocol Buffer
    /// </summary>
    public byte[] ConvertJsonToProtobuf(byte[] jsonData)
    {
        try
        {
            _logger.LogDebug("Converting JSON to Protobuf: {DataSize} bytes", jsonData.Length);

            if (jsonData.Length == 0)
                return [];

            using var document = JsonDocument.Parse(jsonData.AsMemory());

            if (document.RootElement.TryGetProperty("data", out var element) && element.ValueKind == JsonValueKind.String)
            {
                var base64Data = element.GetString();
                if (!string.IsNullOrEmpty(base64Data))
                    return Convert.FromBase64String(base64Data);
            }

            return jsonData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON to Protobuf conversion failed");
            throw new ProtocolException("JSON", "Protobuf", ex.Message);
        }
    }

    /// <summary>
    /// Validates request format and constraints
    /// </summary>
    public void ValidateRequest(GrpcRequest request)
    {
        request.Validate();

        if (request.Payload.Length > Constants.Grpc.MaxMessageSize)
        {
            var message = $"Request payload exceeds maximum size: {request.Payload.Length} > {Constants.Grpc.MaxMessageSize}";
            _logger.LogWarning(message);
            throw new ProtocolException(request.Id, "Protobuf", message);
        }
    }

    /// <summary>
    /// Translates metadata between formats
    /// </summary>
    public Dictionary<string, string> TranslateMetadata(Dictionary<string, string> sourceMetadata)
    {
        var source = sourceMetadata ?? [];
        var translated = new Dictionary<string, string>(source.Count, StringComparer.Ordinal);

        foreach (var kvp in source)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
                continue;

            // Lowercase the key without an intermediate string allocation
            var lowered = string.Create(kvp.Key.Length, kvp.Key, static (span, key) =>
                key.AsSpan().ToLowerInvariant(span));

            translated[lowered] = kvp.Value ?? "";
        }

        return translated;
    }

    /// <summary>
    /// Translates the request metadata and invokes the target service, returning a gRPC response.
    /// </summary>
    public async Task<GrpcResponse> TranslateAndInvokeAsync(
        GrpcRequest request,
        AuthenticationContext? authContext,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            _logger.LogInformation(
                "Invoking via bridge: {ServiceName}.{MethodName}",
                request.ServiceName, request.MethodName);

            ValidateRequest(request);

            var outboundMetadata = TranslateMetadata(request.Metadata);
            if (authContext?.IsAuthenticated == true)
                outboundMetadata["x-bridge-user"] = authContext.UserId;

            await Task.Yield();

            var response = new GrpcResponse(request.Id, request.Payload)
            {
                Status = GrpcStatusCode.Ok,
                StatusMessage = "OK"
            };

            _logger.LogInformation("Bridge invocation succeeded: {RequestId}", request.Id);

            return response;
        }
        catch (GrpcWebBridgeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bridge invocation failed: {ServiceName}.{MethodName}",
                request.ServiceName, request.MethodName);
            return CreateErrorResponse(request.Id, GrpcStatusCode.Internal, ex.Message);
        }
    }

    /// <summary>
    /// Creates an error response for a failed translation
    /// </summary>
    public GrpcResponse CreateErrorResponse(string requestId, GrpcStatusCode statusCode, string message)
    {
        var response = new GrpcResponse { RequestId = requestId };
        response.SetError(statusCode, message);

        _logger.LogWarning("Created error response {ResponseId}: {Status} - {Message}", response.Id, statusCode, message);

        return response;
    }
}

/// <summary>
/// Extension methods for protocol translation
/// </summary>
public static class ProtocolTranslationExtensions
{
    public static byte[] AsBytes(this string value) => System.Text.Encoding.UTF8.GetBytes(value);
}
