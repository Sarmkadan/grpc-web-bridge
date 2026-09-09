using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.Http.Headers;

namespace GrpcWebBridge.Extensions;

/// <summary>
/// Provides extension methods for <see cref="HttpContext"/> to detect gRPC-Web requests and extract gRPC method paths.
/// </summary>
public static class HttpContextGrpcExtensions
{
    private static readonly string[] ValidContentTypePrefixes =
    [
        "application/grpc-web+proto",
        "application/grpc-web-text+proto",
        "application/grpc-web-text",
        "application/grpc-web",
        "application/grpc+proto",
        "application/grpc",
    ];

    /// <summary>
    /// Determines whether the specified <see cref="HttpContext"/> represents a gRPC-Web request based on the Content-Type header.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> to check.</param>
    /// <returns>
    /// true if the request's Content-Type matches one of the valid gRPC-Web content types (application/grpc-web+proto, application/grpc-web-text+proto, application/grpc-web-text, application/grpc-web, application/grpc+proto, application/grpc), ignoring case; otherwise, false.
    /// </returns>
    public static bool IsGrpcWebRequest(this HttpContext context)
    {
        var contentType = context.Request.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        if (MediaTypeHeaderValue.TryParse(contentType, out var header))
        {
            var mediaType = header.MediaType;
            return ValidContentTypePrefixes.Any(v =>
                string.Equals(mediaType, v, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    /// <summary>
    /// Gets the gRPC method path from the request path in the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> containing the request.</param>
    /// <returns>
    /// The request path value if available; otherwise, an empty string.
    /// </returns>
    public static string GetGrpcMethodPath(this HttpContext context)
    {
        return context.Request.Path.Value ?? string.Empty;
    }
}
