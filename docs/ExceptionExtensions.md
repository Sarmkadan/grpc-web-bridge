# Exception extension classes

This page documents the small helper classes in `GrpcWebBridge.Domain.Exceptions` that do not have individual documentation pages.

## `GrpcWebBridgeExceptionExtensions`

Helpers for adding, formatting, and replacing information on a `GrpcWebBridgeException`.

### `AddContextEntry`

```csharp
public static GrpcWebBridgeException AddContextEntry(
    this GrpcWebBridgeException exception,
    string key,
    object value)
```

Adds `value` to the exception's context under `key` by calling `AddContext`, then returns the same exception instance for fluent use. An existing entry with the same key is overwritten. Throws `ArgumentNullException` when `exception` or `value` is null, and when `key` is null. Throws `ArgumentException` when `key` is empty or consists only of whitespace.

### `GetContextString`

```csharp
public static string GetContextString(this GrpcWebBridgeException exception)
```

Returns an empty string when the context has no entries. Otherwise, formats each entry as `key: value` and joins the entries with `, `, using the context dictionary's enumeration order and normal string interpolation for values. Throws `ArgumentNullException` when `exception` is null.

### `WithNewErrorCode`

```csharp
public static GrpcWebBridgeException WithNewErrorCode(
    this GrpcWebBridgeException exception,
    string newErrorCode)
```

Creates a new `GrpcWebBridgeException` with the original message and inner exception, and initializes its `ErrorCode` to `newErrorCode`. It does not copy the original exception's context, gRPC status, stack trace, or other exception metadata. Throws `ArgumentNullException` when `exception` is null. Because the constructor used by this method requires a non-null inner exception, it also throws `ArgumentNullException` when `exception.InnerException` is null. The method does not explicitly validate `newErrorCode`.

## `ProtocolExceptionExtensions`

Formatting and request-correlation helpers for `ProtocolException`.

### `ToLogString`

```csharp
public static string ToLogString(this ProtocolException exception)
```

Returns a single-line value in this form:

```text
ProtocolException: {Message} (SourceFormat: {SourceFormat}, TargetFormat: {TargetFormat}, RequestId: {RequestId})
```

Each null format or request ID is rendered as the literal `null`. Throws `ArgumentNullException` when `exception` is null.

### `IsRelatedToRequest`

```csharp
public static bool IsRelatedToRequest(
    this ProtocolException exception,
    string requestId)
```

Returns the result of an ordinal, case-sensitive string equality comparison between `exception.RequestId` and `requestId`. Throws `ArgumentNullException` when `exception` or `requestId` is null, and `ArgumentException` when `requestId` is empty.

## `ProtocolExceptionValidation`

Validation extensions for the format and request metadata on a `ProtocolException`.

### `Validate`

```csharp
public static IReadOnlyList<string> Validate(this ProtocolException value)
```

Returns a read-only list containing zero or more of these messages, in this order:

- `SourceFormat is required.` when `SourceFormat` is null or empty.
- `TargetFormat is required.` when `TargetFormat` is null or empty.
- `RequestId must be null or a non-empty string.` when `RequestId` is the empty string.

Whitespace-only values pass validation. A null `RequestId` is valid. Throws `ArgumentNullException` when `value` is null.

### `IsValid`

```csharp
public static bool IsValid(this ProtocolException value)
```

Returns `true` when `Validate` returns no errors and `false` otherwise. Throws `ArgumentNullException` when `value` is null.

### `EnsureValid`

```csharp
public static void EnsureValid(this ProtocolException value)
```

Returns normally when the exception passes `Validate`. Otherwise, throws an `ArgumentException` whose message starts with `ProtocolException is invalid:` and places every validation error on a new line. Throws `ArgumentNullException` when `value` is null.

## `ServiceRegistrationExceptionExtensions`

Diagnostic and aggregation helpers for service-registration failures.

### `ToDetailedString`

```csharp
public static string ToDetailedString(
    this ServiceRegistrationException exception)
```

Returns `{Message} (Service: {ServiceName}, Endpoint: {ServiceEndpoint})` using invariant culture. A null service name or endpoint is represented as `N/A`. Throws `ArgumentNullException` when `exception` is null.

### `TryExtractEndpoint`

```csharp
public static bool TryExtractEndpoint(
    this ServiceRegistrationException exception,
    out string? endpoint)
```

Assigns `exception.ServiceEndpoint` to `endpoint`. Returns `true` when that value is neither null nor empty; otherwise returns `false` and leaves `endpoint` as the original null or empty value. Throws `ArgumentNullException` when `exception` is null.

### `CombineExceptions`

```csharp
public static AggregateException CombineExceptions(
    IEnumerable<ServiceRegistrationException> exceptions)
```

Creates an `AggregateException` containing the non-null items from `exceptions`, preserving their enumeration order. Throws `ArgumentNullException` when the sequence is null. Throws `ArgumentException` when the sequence is empty or contains no non-null items. The implementation enumerates the input once to test whether it is empty and again to collect its non-null items, so callers should supply a repeatable sequence.

## `StreamingExceptionJsonExtensions`

JSON serialization helpers for `StreamingException`, implemented with `System.Text.Json` web defaults, camel-case property names, and omission of null-valued properties.

### `ToJson`

```csharp
public static string ToJson(
    this StreamingException value,
    bool indented = false)
```

Serializes `value` to JSON. The output is compact by default; passing `true` creates a per-call copy of the shared serializer options with indented output enabled. Throws `ArgumentNullException` when `value` is null and can propagate serialization exceptions from `JsonSerializer`.

### `FromJson`

```csharp
public static StreamingException? FromJson(string json)
```

Returns null for an empty string. Otherwise, deserializes the JSON using the class's shared options and returns the resulting `StreamingException`, which can also be null when the JSON token is `null`. Throws `ArgumentNullException` when `json` is null and `JsonException` when non-empty JSON is invalid or cannot be deserialized.

### `TryFromJson`

```csharp
public static bool TryFromJson(
    string json,
    out StreamingException? value)
```

Sets `value` to null before processing. An empty string is considered successful and returns `true` with a null value. For other input, returns `true` with the deserialized value when `JsonSerializer` completes, including when the JSON token is `null`. If deserialization throws `JsonException`, returns `false` and leaves `value` null. Throws `ArgumentNullException` when `json` is null; exceptions other than `JsonException` are not caught.
