# Enums

Documentation of all enums in the `GrpcWebBridge.Domain` namespace and the `EnumExtensions.ToDisplayString` helper.

## MethodType

Enumeration for gRPC method types

| Member | Value | Description |
|--------|-------|-------------|
| Unary | 0 | Unary gRPC method |
| ClientStreaming | 1 | Client-streaming gRPC method |
| ServerStreaming | 2 | Server-streaming gRPC method |
| BidirectionalStreaming | 3 | Bidirectional streaming gRPC method |

## SerializationFormat

Enumeration for message serialization formats

| Member | Value | Description |
|--------|-------|-------------|
| Protobuf | 0 | Protocol Buffers serialization |
| Json | 1 | JSON serialization |
| MessagePack | 2 | MessagePack serialization |

## AuthenticationScheme

Enumeration for authentication schemes

| Member | Value | Description |
|--------|-------|-------------|
| None | 0 | No authentication |
| Bearer | 1 | Bearer token authentication |
| ApiKey | 2 | API key authentication |
| Certificate | 3 | Certificate-based authentication |
| Custom | 4 | Custom authentication mechanism |

## StreamMessageType

Enumeration for streaming message types

| Member | Value | Description |
|--------|-------|-------------|
| Data | 0 | Data message |
| Metadata | 1 | Metadata message |
| Status | 2 | Status message |
| Heartbeat | 3 | Heartbeat message |
| Error | 4 | Error message |

## GrpcStatusCode

Enumeration for gRPC status codes

| Member | Value | Description |
|--------|-------|-------------|
| Ok | 0 | The operation completed successfully |
| Cancelled | 1 | The operation was cancelled |
| Unknown | 2 | Unknown error |
| InvalidArgument | 3 | Client specified an invalid argument |
| DeadlineExceeded | 4 | Deadline expired before operation could complete |
| NotFound | 5 | Requested entity was not found |
| AlreadyExists | 6 | Entity that a client attempted to create already exists |
| PermissionDenied | 7 | Caller does not have permission to execute the operation |
| ResourceExhausted | 8 | Resource has been exhausted (e.g., quota) |
| FailedPrecondition | 9 | Operation was rejected because the system is not in a state required for the operation's execution |
| Aborted | 10 | Operation was aborted, typically due to a concurrency issue |
| OutOfRange | 11 | Operation was attempted past the valid range |
| Unimplemented | 12 | Operation is not implemented or not supported/enabled |
| Internal | 13 | Internal error (e.g., invariants broken) |
| Unavailable | 14 | Service is currently unavailable |
| DataLoss | 15 | Unrecoverable data loss or corruption |
| Unauthenticated | 16 | Request does not have valid authentication credentials |

## ServiceStatus

Enumeration for service status

| Member | Value | Description |
|--------|-------|-------------|
| Unknown | 0 | Service status is unknown |
| Serving | 1 | Service is currently serving requests |
| NotServing | 2 | Service is not currently serving requests |
| Unknown_ServiceNotServing | 3 | Service status unknown, but known to not be serving |

## StreamState

Enumeration for stream state

| Member | Value | Description |
|--------|-------|-------------|
| New | 0 | Stream newly created |
| Active | 1 | Stream is active and processing messages |
| HalfClosed | 2 | Stream is half-closed (one direction closed) |
| Closed | 3 | Stream is fully closed |
| Failed | 4 | Stream has encountered a failure |

## EnumExtensions.ToDisplayString

Helper method in `GrpcWebBridge.Domain.EnumExtensions` that returns a display string for an enum value.

- If the enum member is decorated with `<see cref="DisplayAttribute"/>`, the `Name` property of that attribute is returned.
- Otherwise, the enum member name is returned.
- Throws `ArgumentNullException` if the input value is null.

**Signature:**
```csharp
public static string ToDisplayString(this Enum value)
```