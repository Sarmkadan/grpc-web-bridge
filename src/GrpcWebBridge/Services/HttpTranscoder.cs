#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Services;

/// <summary>
/// Transcodes HTTP/JSON requests into gRPC binary format and vice versa.
/// Supports REST-style URL path bindings (e.g., /v1/services/{id}/method)
/// and query parameter mapping for gRPC-JSON transcoding scenarios.
/// </summary>
public sealed class HttpTranscoder
{
    private const string ApplicationJson = "application/json";
    private const string ApplicationGrpcWebText = "application/grpc-web-text";
    private const string ApplicationGrpcWeb = "application/grpc-web";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly ILogger<HttpTranscoder> _logger;
    private readonly ProtocolTranslationService _translationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpTranscoder"/> class.
    /// </summary>
    /// <param name="logger">The logger for recording transcoding operations.</param>
    /// <param name="translationService">The protocol translation service for format conversion.</param>
    public HttpTranscoder(ILogger<HttpTranscoder> logger, ProtocolTranslationService translationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
    }

    /// <summary>
    /// Determines the serialization format from an HTTP Content-Type header value.
    /// </summary>
    /// <param name="contentType">The Content-Type header value.</param>
    /// <returns>The corresponding serialization format.</returns>
    public SerializationFormat ResolveFormat(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return SerializationFormat.Protobuf;
        }

        var mediaType = contentType.Split(';')[0].Trim().ToLowerInvariant();
        return mediaType switch
        {
            ApplicationJson => SerializationFormat.Json,
            ApplicationGrpcWeb => SerializationFormat.Protobuf,
            ApplicationGrpcWebText => SerializationFormat.Protobuf,
            _ => SerializationFormat.Protobuf
        };
    }

    /// <summary>
    /// Extracts the gRPC service and method names from an HTTP request path.
    /// Expects paths in the format: /package.ServiceName/MethodName
    /// </summary>
    /// <param name="path">The HTTP request path.</param>
    /// <returns>A tuple of (serviceName, methodName).</returns>
    /// <exception cref="ProtocolException">Thrown when the path format is invalid.</exception>
    public (string ServiceName, string MethodName) ParseGrpcPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Remove leading slash and split
        var trimmed = path.TrimStart('/');
        var slashIndex = trimmed.LastIndexOf('/');

        if (slashIndex <= 0 || slashIndex >= trimmed.Length - 1)
        {
            throw new ProtocolException($"Invalid gRPC path format: '{path}'. Expected /ServiceName/MethodName");
        }

        var serviceName = trimmed[..slashIndex];
        var methodName = trimmed[(slashIndex + 1)..];

        _logger.LogDebug("Parsed gRPC path: service={Service}, method={Method}", serviceName, methodName);
        return (serviceName, methodName);
    }

    /// <summary>
    /// Transcodes an HTTP/JSON request body into a gRPC request object.
    /// </summary>
    /// <param name="path">The HTTP request path identifying the gRPC service/method.</param>
    /// <param name="body">The raw request body bytes.</param>
    /// <param name="contentType">The Content-Type header value.</param>
    /// <returns>The transcoded gRPC request.</returns>
    public GrpcRequest TranscodeRequest(string path, byte[] body, string? contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(body);

        var (serviceName, methodName) = ParseGrpcPath(path);
        var format = ResolveFormat(contentType);

        _logger.LogInformation("Transcoding HTTP request to gRPC: {Path}, format={Format}", path, format);

        return _translationService.TranslateHttpToGrpc(serviceName, methodName, body, format);
    }

    /// <summary>
    /// Selects the appropriate Content-Type header for the gRPC-Web response
    /// based on the original request format.
    /// </summary>
    /// <param name="requestContentType">The original request Content-Type.</param>
    /// <returns>The response Content-Type string.</returns>
    public string GetResponseContentType(string? requestContentType)
    {
        var format = ResolveFormat(requestContentType);
        return format == SerializationFormat.Json ? ApplicationJson : ApplicationGrpcWeb;
    }
}
