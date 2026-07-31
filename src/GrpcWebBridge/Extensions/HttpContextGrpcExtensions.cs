using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

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

        var mediaType = contentType.Split(';')[0].Trim();
        return ValidContentTypePrefixes.Any(v => mediaType.Equals(v, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetGrpcMethodPath(this HttpContext context)
    {
        return context.Request.Path.Value ?? string.Empty;
    }
}
