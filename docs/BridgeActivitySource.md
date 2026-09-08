# BridgeActivitySource

`BridgeActivitySource` is the central `System.Diagnostics.ActivitySource` used by the gRPC-Web bridge. Its shared `Source` instance is constructed as:

```csharp
new ActivitySource(Name, Version)
```

The source identity is:

| Constant | Exact value | Purpose |
| --- | --- | --- |
| `Name` | `"GrpcWebBridge"` | Activity source name. `AddGrpcWebBridgeTracing` registers this name with OpenTelemetry by calling `AddSource(BridgeActivitySource.Name)`. |
| `Version` | `"2.0.2"` | Activity source version. `AddGrpcWebBridgeTracing` also uses it as the OpenTelemetry resource service version. |

An activity is returned by `Source.StartActivity(...)` only when a matching listener is registered. Consequently, the `TracingService` start methods may return `null` when bridge tracing has not been configured.

## Activity-name constants

| Constant | Exact value | Use in `TracingService` |
| --- | --- | --- |
| `GrpcCall` | `"grpc.bridge.call"` | Used by `StartGrpcCallActivity` when `isStreaming` is `false`. |
| `GrpcStream` | `"grpc.bridge.stream"` | Used by `StartGrpcCallActivity` when `isStreaming` is `true`. |
| `ProtocolTranslation` | `"grpc.bridge.translate"` | Used by `StartProtocolTranslationActivity`. |
| `Authentication` | `"grpc.bridge.auth"` | Used by `StartAuthenticationActivity`. |

`StartGrpcCallActivity` creates an `ActivityKind.Client` activity. Protocol-translation and authentication activities use `ActivityKind.Internal`.

## Tag-name constants

| Constant | Exact value | Use in `TracingService` |
| --- | --- | --- |
| `TagRpcService` | `"rpc.service"` | Set to the `serviceName` passed to `StartGrpcCallActivity`. |
| `TagRpcMethod` | `"rpc.method"` | Set to the `methodName` passed to `StartGrpcCallActivity`. |
| `TagRpcSystem` | `"rpc.system"` | Set to the exact value `"grpc"` by `StartGrpcCallActivity`. |
| `TagGrpcStatus` | `"rpc.grpc.status_code"` | Set to the supplied gRPC status by `SetGrpcStatus`; also set by `RecordException` when its optional `grpcStatus` argument is not null, empty, or whitespace. |
| `TagBridgeInstance` | `"bridge.instance"` | Set on every activity created by `TracingService`. Its value is the constructor's `instanceName`, or `"default"` when that argument is null, empty, or whitespace. |
| `TagStreaming` | `"bridge.streaming"` | Set by `StartGrpcCallActivity` to the Boolean `isStreaming` argument. |

## TracingService behavior

`TracingService` wraps the shared `BridgeActivitySource.Source` and uses the constants as follows:

- `StartGrpcCallActivity(serviceName, methodName, isStreaming)` selects `GrpcStream` or `GrpcCall`, starts a client activity, and applies `TagRpcSystem`, `TagRpcService`, `TagRpcMethod`, `TagStreaming`, and `TagBridgeInstance`.
- `StartProtocolTranslationActivity(sourceProtocol, targetProtocol)` starts an internal `ProtocolTranslation` activity and applies `TagBridgeInstance`. It also emits the literal tag names `"bridge.source_protocol"` and `"bridge.target_protocol"`; these are not constants in `BridgeActivitySource`.
- `StartAuthenticationActivity(scheme)` starts an internal `Authentication` activity and applies `TagBridgeInstance`. It also emits the literal tag name `"auth.scheme"`, which is not a constant in `BridgeActivitySource`.
- `SetGrpcStatus(activity, grpcStatus)` applies `TagGrpcStatus`. The exact status string `"OK"` sets `ActivityStatusCode.Ok`; every other value sets `ActivityStatusCode.Error` with the description `"gRPC status: {grpcStatus}"`.
- `RecordException(activity, exception, grpcStatus)` marks the activity as an error using the exception message, records the exception, and applies `TagGrpcStatus` only when `grpcStatus` contains a non-whitespace value.

Both status helpers do nothing when their activity argument is `null`. Each activity-start method likewise returns `null` without applying tags when `ActivitySource.StartActivity` returns `null`.
