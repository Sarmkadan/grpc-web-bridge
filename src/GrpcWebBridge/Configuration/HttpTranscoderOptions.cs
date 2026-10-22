#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using GrpcWebBridge.Middleware;

namespace GrpcWebBridge.Configuration;

/// <summary>
/// Options for <see cref="Services.HttpTranscoder"/>. Validated at host start by <see cref="HttpTranscoderOptionsValidator"/>.
/// </summary>
public sealed class HttpTranscoderOptions
{
    /// <summary>
    /// Optional path prefix for REST routes, such as <c>/api</c>. Null means no prefix.
    /// When set, it must start with <c>/</c> and must not end with <c>/</c>.
    /// </summary>
    public string? RoutePrefix { get; set; }

    /// <summary>
    /// JSON serializer settings used when transcoding JSON bodies. Must not be null.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Maximum allowed REST request body size in bytes. Must be greater than 0.
    /// Default is 4 MiB, the same as <see cref="MaxBodySizeOptions.DefaultMaxRequestBodySizeBytes"/>.
    /// </summary>
    public long MaxRequestBodySize { get; set; } = MaxBodySizeOptions.DefaultMaxRequestBodySizeBytes;

    /// <summary>
    /// The http-annotated gRPC methods the host exposes over REST. The bridge does not read
    /// <c>google.api.http</c> annotations itself, so each binding is declared here. Must not be null.
    /// </summary>
    public List<HttpRouteBinding>? Routes { get; set; } = [];
}
