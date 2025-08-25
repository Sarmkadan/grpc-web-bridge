// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Domain;

/// <summary>
/// Application-wide constants and configuration values
/// </summary>
public static class Constants
{
    public static class Grpc
    {
        public const string ProtocolVersion = "1.0";
        public const int MaxMessageSize = 4 * 1024 * 1024; // 4MB
        public const int DefaultTimeout = 30000; // milliseconds
        public const string ContentTypeProtobuf = "application/protobuf";
        public const string ContentTypeJson = "application/json";
    }

    public static class Http
    {
        public const string GrpcWebContentType = "application/grpc-web";
        public const string GrpcWebTextContentType = "application/grpc-web-text";
        public const string AuthorizationHeader = "Authorization";
        public const string BearerScheme = "Bearer";
        public const string XGrpcMetadataHeader = "x-grpc-metadata";
    }

    public static class Authentication
    {
        public const string JwtAudience = "grpc-web-bridge";
        public const string JwtIssuer = "sarmkadan.com";
        public const int JwtExpirationMinutes = 60;
    }

    public static class Logging
    {
        public const string CategoryName = "GrpcWebBridge";
        public const string ProtocolTranslationCategory = "GrpcWebBridge.ProtocolTranslation";
        public const string StreamingCategory = "GrpcWebBridge.Streaming";
        public const string AuthenticationCategory = "GrpcWebBridge.Authentication";
    }

    public static class ServiceRegistry
    {
        public const string DefaultNamespace = "grpc.web.bridge";
        public const int MaxCachedServices = 1000;
        public const int ServiceMetadataCacheDurationMinutes = 30;
    }

    public static class Streaming
    {
        public const int DefaultBufferSize = 8192;
        public const int MaxStreamCount = 10000;
        public const int StreamIdleTimeoutSeconds = 300;
        public const int StreamHeartbeatIntervalSeconds = 30;
    }

    public static class Errors
    {
        public const string ServiceNotFound = "Service not found";
        public const string MethodNotFound = "Method not found";
        public const string InvalidRequest = "Invalid request format";
        public const string AuthenticationFailed = "Authentication failed";
        public const string StreamingError = "Streaming error occurred";
        public const string SerializationError = "Serialization error";
    }
}
