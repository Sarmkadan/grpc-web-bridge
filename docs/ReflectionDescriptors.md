# Reflection descriptors

The reflection descriptor models represent gRPC services and methods discovered at runtime. They are returned by the APIs described in [ReflectionService](ReflectionService.md).

All types and extension methods in this document are in the `GrpcWebBridge.Domain.Models` namespace.

## `GrpcServiceDescriptor`

Describes a registered gRPC service and the connection information needed to reach its backing server.

| Property | Type | Description |
| --- | --- | --- |
| `FullName` | `string` | Fully qualified service name, such as `mypackage.MyService`. Defaults to an empty string. |
| `Name` | `string` | Unqualified service name. Defaults to an empty string. |
| `PackageName` | `string` | Proto package name. Defaults to an empty string. |
| `Description` | `string?` | Optional human-readable service description. |
| `Endpoint` | `string` | Host name or IP address of the backing gRPC server. Defaults to an empty string. |
| `Port` | `int` | Port of the backing gRPC server. |
| `UseTls` | `bool` | Indicates whether the backing server requires TLS. |
| `Methods` | `IReadOnlyCollection<MethodDescriptor>` | Descriptors for the methods exposed by the service. Defaults to an empty collection. |

## `MethodDescriptor`

Describes one gRPC method exposed by a service.

| Property | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Unqualified method name. Defaults to an empty string. |
| `FullName` | `string` | Fully qualified method name, such as `mypackage.MyService/MyMethod`. Defaults to an empty string. |
| `ServiceFullName` | `string` | Fully qualified name of the owning service. Defaults to an empty string. |
| `MethodType` | `string` | Method type: `Unary`, `ClientStreaming`, `ServerStreaming`, or `BidirectionalStreaming`. Defaults to an empty string. |
| `IsClientStreaming` | `bool` | Indicates whether the method accepts a client-side stream of messages. |
| `IsServerStreaming` | `bool` | Indicates whether the method produces a server-side stream of messages. |
| `InputMessageType` | `string` | Proto message type name for the request payload. Defaults to an empty string. |
| `OutputMessageType` | `string` | Proto message type name for the response payload. Defaults to an empty string. |
| `IsDeprecated` | `bool` | Indicates whether the method is deprecated. |
| `Description` | `string?` | Optional human-readable method description. |
| `TimeoutMilliseconds` | `int` | Default timeout for the method, in milliseconds. |

## `ReflectionResult<T>`

Wraps the payload and status metadata produced by a reflection query.

| Property | Type | Description |
| --- | --- | --- |
| `Data` | `T?` | Query payload. It is `null` when `Success` is `false`. |
| `Success` | `bool` | Indicates whether the query completed successfully. |
| `ErrorMessage` | `string?` | Failure description when `Success` is `false`; otherwise `null`. |
| `Timestamp` | `DateTime` | UTC time at which the result was created. Its default value is `DateTime.UtcNow`. |

### Factory methods

#### `static ReflectionResult<T> Ok(T data)`

Creates a successful result. `Data` is set to `data`, `Success` is `true`, and `Timestamp` uses its default UTC value.

#### `static ReflectionResult<T> Fail(string errorMessage)`

Creates a failed result. `Success` is `false`, `ErrorMessage` is set to `errorMessage`, and `Timestamp` uses its default UTC value.

## `GrpcServiceDescriptorExtensions`

The following extension methods operate on `GrpcServiceDescriptor`. Every method throws `ArgumentNullException` when `descriptor` is `null`.

### `GetDisplayName()`

```csharp
string GetDisplayName(this GrpcServiceDescriptor descriptor)
```

Returns `PackageName` and `Name` joined with a period. For example, a package named `mypackage` and a service named `MyService` produce `mypackage.MyService`.

### `IsSecureEndpoint()`

```csharp
bool IsSecureEndpoint(this GrpcServiceDescriptor descriptor)
```

Returns `true` only when `UseTls` is `true` and `Port` is either `443` or `8443`. Otherwise, it returns `false`.

### `GetStreamingMethods()`

```csharp
IEnumerable<MethodDescriptor> GetStreamingMethods(this GrpcServiceDescriptor descriptor)
```

Returns the methods for which `IsClientStreaming` or `IsServerStreaming` is `true`. The returned sequence is evaluated when it is enumerated.

### `GetMethodByName(string methodName)`

```csharp
MethodDescriptor? GetMethodByName(
    this GrpcServiceDescriptor descriptor,
    string methodName)
```

Trims leading and trailing whitespace from `methodName`, then returns the first method whose `Name` matches it using an ordinal, case-insensitive comparison. Returns `null` when no method matches.

The method throws `ArgumentException` when `methodName` is `null` or empty. A whitespace-only value passes the initial validation, is trimmed to an empty string, and normally produces no match.

## Example

```csharp
ReflectionResult<GrpcServiceDescriptor> result =
    await reflectionService.GetServiceDescriptorAsync("mypackage.MyService");

if (result.Success && result.Data is { } service)
{
    string displayName = service.GetDisplayName();
    bool isSecure = service.IsSecureEndpoint();
    IEnumerable<MethodDescriptor> streamingMethods = service.GetStreamingMethods();
    MethodDescriptor? method = service.GetMethodByName("MyMethod");
}
```
