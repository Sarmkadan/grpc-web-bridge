using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Net.Http.Headers;

namespace GrpcWebBridge.Extensions;

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

    public static string GetGrpcMethodPath(this HttpContext context)
    {
        return context.Request.Path.Value ?? string.Empty;
    }
}
