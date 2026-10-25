using System;
using System.Linq;
using System.Net.Http.Headers;
using GrpcWebBridge.Domain;
using Microsoft.AspNetCore.Http;

namespace GrpcWebBridge.Extensions;

/// <summary>
/// Provides extension methods for <see cref="HttpContext"/> to detect gRPC-Web requests and extract gRPC method paths.
/// </summary>
public static class HttpContextGrpcExtensions
{
    private static readonly string[] _validContentTypePrefixes =
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static bool IsGrpcWebRequest(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var contentType = context.Request.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        if (MediaTypeHeaderValue.TryParse(contentType, out var header))
        {
            var mediaType = header.MediaType;
            return _validContentTypePrefixes.Any(v =>
                string.Equals(mediaType, v, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified <see cref="HttpContext"/> represents a base64-encoded gRPC-Web (grpc-web-text) request based on the Content-Type header.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> to check.</param>
    /// <returns>
    /// true if the request's media type equals <see cref="Constants.Http.GrpcWebTextContentType"/> (application/grpc-web-text), ignoring case and parameters; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static bool IsGrpcWebTextRequest(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var contentType = context.Request.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return MediaTypeHeaderValue.TryParse(contentType, out var header)
            && string.Equals(header.MediaType, Constants.Http.GrpcWebTextContentType, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the gRPC method path from the request path in the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> containing the request.</param>
    /// <returns>
    /// The request path value if available; otherwise, an empty string.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static string GetGrpcMethodPath(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Request.Path.Value ?? string.Empty;
    }

    /// <summary>
    /// Attempts to get the gRPC method path from the request path in the specified <see cref="HttpContext"/> without throwing.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> containing the request.</param>
    /// <param name="path">
    /// When this method returns, contains the request path if it is available; otherwise, an empty string.
    /// </param>
    /// <returns>
    /// true if a non-empty request path is available; otherwise, false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static bool TryGetGrpcMethodPath(this HttpContext context, out string path)
    {
        ArgumentNullException.ThrowIfNull(context);

        path = context.Request.Path.Value ?? string.Empty;
        return path.Length > 0;
    }
}
