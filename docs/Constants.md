# Constants

`GrpcWebBridge.Domain.Constants` groups application-wide constants by concern. The values below reproduce the declarations in `src/GrpcWebBridge/Domain/Constants.cs` exactly. Usage was determined by searching the C# source and tests for each constant; “No usages found” means that only the declaration was found.

## Grpc

| Constant | Value | Where it is used |
| --- | --- | --- |
| `ProtocolVersion` | `"1.0"` | No usages found. |
| `MaxMessageSize` | `4 * 1024 * 1024` | `Data/GrpcConnectionManager.cs` configures gRPC send/receive limits; `Domain/Models/BridgeConfiguration.cs` supplies the configuration default; `Domain/Models/GrpcRequest.cs`, `GrpcResponse.cs`, and `Services/ProtocolTranslationService.cs` validate payload sizes; `tests/grpc-web-bridge.Tests/GrpcRequestTests.cs` and `ProtocolTranslationServiceTests.cs` create oversized test payloads. |
| `DefaultTimeout` | `30000` | `Domain/Models/BridgeConfiguration.cs`, `GrpcMethod.cs`, and `GrpcRequest.cs` supply timeout defaults; `Domain/Models/GrpcMethodExtensions.cs` detects a non-default timeout; `tests/grpc-web-bridge.Tests/GrpcMethodTests.cs` verifies the default. |
| `ContentTypeProtobuf` | `"application/protobuf"` | No usages found. |
| `ContentTypeJson` | `"application/json"` | No usages found. |

## Http

| Constant | Value | Where it is used |
| --- | --- | --- |
| `GrpcWebContentType` | `"application/grpc-web"` | No usages found. |
| `GrpcWebTextContentType` | `"application/grpc-web-text"` | No usages found. |
| `AuthorizationHeader` | `"Authorization"` | No usages found. |
| `BearerScheme` | `"Bearer"` | No usages found. |
| `XGrpcMetadataHeader` | `"x-grpc-metadata"` | No usages found. |

## Authentication

| Constant | Value | Where it is used |
| --- | --- | --- |
| `JwtAudience` | `"grpc-web-bridge"` | No usages found. |
| `JwtIssuer` | `"sarmkadan.com"` | No usages found. |
| `JwtExpirationMinutes` | `60` | `Services/AuthenticationService.cs` sets the expiration for both authentication flows. |

## Logging

| Constant | Value | Where it is used |
| --- | --- | --- |
| `CategoryName` | `"GrpcWebBridge"` | No usages found. |
| `ProtocolTranslationCategory` | `"GrpcWebBridge.ProtocolTranslation"` | No usages found. |
| `StreamingCategory` | `"GrpcWebBridge.Streaming"` | No usages found. |
| `AuthenticationCategory` | `"GrpcWebBridge.Authentication"` | No usages found. |

## ServiceRegistry

| Constant | Value | Where it is used |
| --- | --- | --- |
| `DefaultNamespace` | `"grpc.web.bridge"` | `Domain/Models/GrpcService.cs` supplies the default package name; `tests/grpc-web-bridge.Tests/GrpcServiceTests.cs` verifies it. |
| `MaxCachedServices` | `1000` | `Services/ServiceRegistry.cs` enforces registry capacity; `BackgroundWorkers/MetricsCollectionWorker.cs` uses it as the default retained snapshot count. |
| `ServiceMetadataCacheDurationMinutes` | `30` | `Services/ServiceRegistry.cs` calculates service metadata expiration. |

## Streaming

| Constant | Value | Where it is used |
| --- | --- | --- |
| `DefaultBufferSize` | `8192` | `Domain/Models/StreamMessage.cs` and `StreamMessageValidation.cs` enforce a maximum data length of twice this value. |
| `MaxStreamCount` | `10000` | `Domain/Models/BridgeConfiguration.cs` supplies the default; `Services/StreamingService.cs` enforces capacity; `Streaming/BidirectionalStreamingEngine.cs` supplies and validates its default limit; `Endpoints/HealthEndpoints.cs` and `BackgroundWorkers/StreamingWorkerSupervisor.cs` report/check capacity. |
| `StreamIdleTimeoutSeconds` | `300` | `Domain/Models/BridgeConfiguration.cs` supplies the default; `Services/StreamingService.cs` determines the idle threshold; `BackgroundWorkers/StreamCleanupWorker.cs` supplies its idle timeout. |
| `StreamHeartbeatIntervalSeconds` | `30` | `Domain/Models/BridgeConfiguration.cs` supplies the default; `BackgroundWorkers/StreamCleanupWorker.cs` supplies its stale-stream duration. |

## Errors

| Constant | Value | Where it is used |
| --- | --- | --- |
| `ServiceNotFound` | `"Service not found"` | No usages found. |
| `MethodNotFound` | `"Method not found"` | No usages found. |
| `InvalidRequest` | `"Invalid request format"` | No usages found. |
| `AuthenticationFailed` | `"Authentication failed"` | No usages found. |
| `StreamingError` | `"Streaming error occurred"` | No usages found. |
| `SerializationError` | `"Serialization error"` | No usages found. |
