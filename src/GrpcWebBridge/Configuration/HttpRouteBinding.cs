#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Configuration;

/// <summary>
/// Describes one http-annotated gRPC method as it is exposed over REST, such as <c>GET /v1/items/{id}</c>.
/// </summary>
public sealed class HttpRouteBinding
{
    /// <summary>
    /// The gRPC service name, such as <c>items.v1.ItemService</c>.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// The gRPC method name, such as <c>GetItem</c>.
    /// </summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP verb: one of <c>GET</c>, <c>PUT</c>, <c>POST</c>, <c>DELETE</c> or <c>PATCH</c>.
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// The route template, such as <c>/v1/items/{id}</c>. Variables use the <c>{field}</c> or <c>{field=pattern}</c> form.
    /// </summary>
    public string? Template { get; set; }

    /// <summary>
    /// Top-level field names of the method's request message, used to check path variables.
    /// </summary>
    public List<string> RequestFieldNames { get; set; } = [];
}
