#nullable enable
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
    /// <summary>
    /// Constants related to gRPC protocol and communication.
    /// </summary>
    public static class Grpc
    {
        /// <summary>
        /// The version of the gRPC protocol supported by the bridge.
        /// </summary>
        public const string ProtocolVersion = "1.0";
        
        /// <summary>
        /// The maximum allowed size for a single gRPC message in bytes.
        /// </summary>
        public const int MaxMessageSize = 4 * 1024 * 1024; // 4MB
        
        /// <summary>
        /// The default timeout duration for gRPC calls in milliseconds.
        /// </summary>
        public const int DefaultTimeout = 30000; // milliseconds
        
        /// <summary>
        /// The MIME type for Protobuf-encoded gRPC messages.
        /// </summary>
        public const string ContentTypeProtobuf = "application/protobuf";
        
        /// <summary>
        /// The MIME type for JSON-encoded gRPC messages.
        /// </summary>
        public const string ContentTypeJson = "application/json";
    }

    /// <summary>
    /// Constants related to HTTP headers and content types used in gRPC-Web communication.
    /// </summary>
    public static class Http
    {
        /// <summary>
        /// The Content-Type header value for standard gRPC-Web requests.
        /// </summary>
        public const string GrpcWebContentType = "application/grpc-web";
        
        /// <summary>
        /// The Content-Type header value for base64-encoded gRPC-Web requests.
        /// </summary>
        public const string GrpcWebTextContentType = "application/grpc-web-text";
        
        /// <summary>
        /// The HTTP header name used to pass authentication tokens.
        /// </summary>
        public const string AuthorizationHeader = "Authorization";
        
        /// <summary>
        /// The authentication scheme prefix used in the Authorization header.
        /// </summary>
        public const string BearerScheme = "Bearer";
        
        /// <summary>
        /// The HTTP header name used to pass gRPC metadata in gRPC-Web.
        /// </summary>
        public const string XGrpcMetadataHeader = "x-grpc-metadata";
    }

    /// <summary>
    /// Constants related to JWT authentication and token validation.
    /// </summary>
    public static class Authentication
    {
        /// <summary>
        /// The expected audience claim (aud) for JWT validation.
        /// </summary>
        public const string JwtAudience = "grpc-web-bridge";
        
        /// <summary>
        /// The expected issuer claim (iss) for JWT validation.
        /// </summary>
        public const string JwtIssuer = "sarmkadan.com";
        
        /// <summary>
        /// The default expiration duration for generated JWTs in minutes.
        /// </summary>
        public const int JwtExpirationMinutes = 60;
    }

    /// <summary>
    /// Constants defining logging category names for different subsystems.
    /// </summary>
    public static class Logging
    {
        /// <summary>
        /// The default logging category name for the bridge.
        /// </summary>
        public const string CategoryName = "GrpcWebBridge";
        
        /// <summary>
        /// The logging category name for protocol translation events.
        /// </summary>
        public const string ProtocolTranslationCategory = "GrpcWebBridge.ProtocolTranslation";
        
        /// <summary>
        /// The logging category name for streaming operations.
        /// </summary>
        public const string StreamingCategory = "GrpcWebBridge.Streaming";
        
        /// <summary>
        /// The logging category name for authentication events.
        /// </summary>
        public const string AuthenticationCategory = "GrpcWebBridge.Authentication";
    }

    /// <summary>
    /// Constants related to service registration and caching.
    /// </summary>
    public static class ServiceRegistry
    {
        /// <summary>
        /// The default namespace used for registered gRPC services.
        /// </summary>
        public const string DefaultNamespace = "grpc.web.bridge";
        
        /// <summary>
        /// The maximum number of services to keep in the registry cache.
        /// </summary>
        public const int MaxCachedServices = 1000;
        
        /// <summary>
        /// The duration in minutes that service metadata is cached.
        /// </summary>
        public const int ServiceMetadataCacheDurationMinutes = 30;
    }

    /// <summary>
    /// Constants related to streaming configuration and behavior.
    /// </summary>
    public static class Streaming
    {
        /// <summary>
        /// The default buffer size in bytes for streaming data chunks.
        /// </summary>
        public const int DefaultBufferSize = 8192;
        
        /// <summary>
        /// The maximum number of concurrent streams allowed.
        /// </summary>
        public const int MaxStreamCount = 10000;
        
        /// <summary>
        /// The timeout in seconds before an idle stream is closed.
        /// </summary>
        public const int StreamIdleTimeoutSeconds = 300;
        
        /// <summary>
        /// The interval in seconds for sending stream heartbeat messages.
        /// </summary>
        public const int StreamHeartbeatIntervalSeconds = 30;
    }

    /// <summary>
    /// Constants containing standard error messages for various failure scenarios.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error message returned when a requested gRPC service is not registered.
        /// </summary>
        public const string ServiceNotFound = "Service not found";
        
        /// <summary>
        /// Error message returned when a requested gRPC method is not found.
        /// </summary>
        public const string MethodNotFound = "Method not found";
        
        /// <summary>
        /// Error message returned for malformed or invalid requests.
        /// </summary>
        public const string InvalidRequest = "Invalid request format";
        
        /// <summary>
        /// Error message returned when authentication fails.
        /// </summary>
        public const string AuthenticationFailed = "Authentication failed";
        
        /// <summary>
        /// Generic error message returned for streaming failures.
        /// </summary>
        public const string StreamingError = "Streaming error occurred";
        
        /// <summary>
        /// Error message returned when serialization or deserialization fails.
        /// </summary>
        public const string SerializationError = "Serialization error";
    }
}
