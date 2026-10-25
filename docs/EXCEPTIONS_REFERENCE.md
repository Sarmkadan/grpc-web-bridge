# Exceptions Reference

Reference for every exception type the bridge throws: the class hierarchy, the properties each type carries, how `ErrorHandlingMiddleware` turns them into HTTP responses, and where each one is thrown.

Source files:
- `src/GrpcWebBridge/Domain/Exceptions/*.cs` - bridge exception types (`GrpcWebBridgeException` and its subclasses), `ProblemDetails`, and the mapping in `ProblemDetailsExtensions.cs`.
- `src/GrpcWebBridge/Events/EventBus.cs` - `EventBusException`, defined next to the bus that throws it.
- `src/GrpcWebBridge/Middleware/ErrorHandlingMiddleware.cs` - converts exceptions to HTTP responses.

Helper extensions for these types are catalogued in [ExceptionExtensionsCatalog](ExceptionExtensionsCatalog.md). Per-type notes: [GrpcWebBridgeException](GrpcWebBridgeException.md), [ConfigurationException](ConfigurationException.md), [ProtocolException](ProtocolException.md), [ServiceRegistrationException](ServiceRegistrationException.md), [StreamingException](StreamingException.md), [ValidationException](ValidationException.md), [ProblemDetails](ProblemDetails.md), [ErrorHandlingMiddleware](ErrorHandlingMiddleware.md).

---

## 1. Hierarchy

```
System.Exception
├── GrpcWebBridgeException            (Domain/Exceptions/GrpcWebBridgeException.cs)
│   ├── ConfigurationException        (Domain/Exceptions/ConfigurationException.cs)
│   ├── ProtocolException             (Domain/Exceptions/ProtocolException.cs)
│   ├── ServiceRegistrationException  (Domain/Exceptions/ServiceRegistrationException.cs)
│   ├── StreamingException            (Domain/Exceptions/StreamingException.cs)
│   └── ValidationException           (Domain/Exceptions/ValidationException.cs)
└── EventBusException  (sealed)       (Events/EventBus.cs)
```

- `GrpcWebBridgeException` is not abstract and is thrown directly in several places (see section 4.1).
- `EventBusException` is not part of the bridge hierarchy. It does not derive from `GrpcWebBridgeException`, so none of the bridge-specific fields exist on it and the HTTP mapper treats it as an unknown exception.
- Argument guards (`ArgumentNullException`, `ArgumentException`) are standard .NET types. They are mapped to 400 by the HTTP mapper but are not part of the bridge hierarchy.

---

## 2. Shared members of `GrpcWebBridgeException`

| Member | Type | Description |
|--------|------|-------------|
| `ErrorCode` | `string?` | Stable machine-readable code such as `CONFIG_INVALID` or `STREAM_FAILED`. Set by constructors; can also be assigned later. |
| `GrpcStatus` | `GrpcStatusCode?` | gRPC status used to pick the HTTP status when the exception is mapped by the generic branch (section 4.1). `null` means no gRPC status. |
| `Context` | `Dictionary<string, object>` | Free-form key/value data. Copied into the response `details` only for direct `GrpcWebBridgeException` instances (section 4.1). |
| `AddContext(key, value)` | `void` | Adds a context entry. Throws `ArgumentException` for a blank key and `ArgumentNullException` for a null value. |
| `WithContext(key, value)` | `GrpcWebBridgeException` | Fluent form of `AddContext`. Returns the same instance. |
| `GetContext(key)` | `object?` | Returns the context value, or `null` if the key is absent. |
| `WithInnerException(ex)` | `GrpcWebBridgeException` | Stores `ex` in `Data["innerException"]`. Does not set `InnerException`. Returns the same instance. |

Constructors that take a message throw `ArgumentNullException` when the message is `null`.

`GrpcStatusCode` and `StreamState` are enums in `src/GrpcWebBridge/Domain/Enums.cs`.

---

## 3. Bridge exception types

For each type: the properties it adds, the constructors with the `ErrorCode` and `GrpcStatus` each one sets, and when the bridge throws it. Only constructors that set something are listed in the table; `(message)` and `(message, innerException)` overloads without extra data are described in the notes.

### 3.1 `ConfigurationException`

Configuration is invalid or a value cannot be resolved.

| Property | Type | Set by |
|----------|------|--------|
| `ConfigurationKey` | `string?` | `(configurationKey, message)` and `(configurationKey, configurationValue, message)` |
| `ConfigurationValue` | `string?` | `(configurationKey, configurationValue, message)` |

| Constructor | `ErrorCode` | `GrpcStatus` | Message |
|-------------|-------------|--------------|---------|
| `(message)` | `CONFIGURATION_ERROR` | `InvalidArgument` | as given |
| `(message, innerException)` | `CONFIGURATION_ERROR` | `InvalidArgument` | as given |
| `(configurationKey, message)` | `CONFIG_INVALID` | `InvalidArgument` | `Configuration '{key}' error: {message}` |
| `(configurationKey, configurationValue, message)` | `CONFIG_INVALID` | `InvalidArgument` | `Configuration '{key}' with value '{value}' error: {message}` |

**Thrown when**
- `HttpClientFactory.cs` rejects a null or empty client name, base URI, URI, payload, or HTTP method (`CONFIG_INVALID`).
- `HttpClientFactory.cs` wraps a non-success HTTP response from an outbound GET, POST, or generic request as `ConfigurationException(uri, uri, "... failed with status ...")`. Note that this wraps a remote failure, not a local configuration fault (see section 6).
- `ProtoRegistry.cs` cannot resolve a protobuf message type name (`CONFIGURATION_ERROR`).

### 3.2 `ProtocolException`

Translation between HTTP/JSON and gRPC/protobuf fails, or a gRPC frame is malformed.

| Property | Type | Set by |
|----------|------|--------|
| `SourceFormat` | `string?` | `(sourceFormat, targetFormat, message)` |
| `TargetFormat` | `string?` | `(sourceFormat, targetFormat, message)` |
| `RequestId` | `string?` | Assigned by callers; no constructor sets it |

| Constructor | `ErrorCode` | `GrpcStatus` | Message |
|-------------|-------------|--------------|---------|
| `(message)` | `PROTOCOL_ERROR` | none | as given |
| `(message, innerException)` | `PROTOCOL_ERROR` | none | as given |
| `(sourceFormat, targetFormat, message)` | `TRANSLATION_FAILED` | `InvalidArgument` | `Protocol translation from {source} to {target} failed: {message}` |

**Thrown when**
- `Services/ProtocolTranslationService.cs` receives an empty service or method name, or the protobuf/JSON conversion throws (`TRANSLATION_FAILED` for conversion failures, `PROTOCOL_ERROR` for empty names).
- `Services/HttpTranscoder.cs` receives a gRPC path that is not `/ServiceName/MethodName`.
- `Services/GrpcWebBridge.cs` receives an empty or trailer-only request for a unary call.
- `Streaming/StreamAdapter.cs` finds a payload larger than the maximum frame size, an incomplete frame header or body, or an invalid frame length.

### 3.3 `ServiceRegistrationException`

Registering, locating, or connecting to a backend gRPC service fails.

| Property | Type | Set by |
|----------|------|--------|
| `ServiceName` | `string?` | `(serviceName, message)` and `(serviceName, endpoint, message)` |
| `ServiceEndpoint` | `string?` | `(serviceName, endpoint, message)` |

| Constructor | `ErrorCode` | `GrpcStatus` | Message |
|-------------|-------------|--------------|---------|
| `(message)` | `SERVICE_REGISTRATION_ERROR` | none | as given |
| `(message, innerException)` | `SERVICE_REGISTRATION_ERROR` | none | as given |
| `(serviceName, message)` | `SERVICE_NOT_FOUND` | `NotFound` | `Service registration failed for '{name}': {message}` |
| `(serviceName, endpoint, message)` | `SERVICE_UNAVAILABLE` | `Unavailable` | `Failed to connect to service '{name}' at {endpoint}: {message}` |

**Thrown when**
- `Services/ServiceRegistry.cs` rejects a duplicate registration (`SERVICE_NOT_FOUND`, see section 6) or a full registry (`SERVICE_NOT_FOUND`).
- `Services/ServiceRegistry.cs` cannot find a service by full name (`SERVICE_NOT_FOUND`).
- `Data/GrpcConnectionManager.cs` fails to create a channel to a service endpoint (`SERVICE_UNAVAILABLE`).

### 3.4 `StreamingException`

A streaming session fails, is missing, or exceeds limits.

| Property | Type | Set by |
|----------|------|--------|
| `StreamId` | `string?` | `(streamId, message)` and `(streamId, sequenceNumber, message)` |
| `SequenceNumber` | `int?` | `(streamId, sequenceNumber, message)` |
| `LastStreamState` | `StreamState?` | `SetStreamState(state)` |

`SetStreamState(state)` also writes `Context["StreamState"]`.

| Constructor | `ErrorCode` | `GrpcStatus` | Message |
|-------------|-------------|--------------|---------|
| `(message)` | `STREAMING_ERROR` | none | as given |
| `(message, innerException)` | `STREAMING_ERROR` | none | as given |
| `(streamId, message)` | `STREAM_FAILED` | `Internal` | `Stream '{id}' error: {message}` |
| `(streamId, sequenceNumber, message)` | `STREAM_MESSAGE_ERROR` | `Internal` | `Stream '{id}' message {seq} error: {message}` |

**Thrown when** (`Services/StreamingService.cs`)
- The maximum number of concurrent streams is reached (`(message)` overload, `STREAMING_ERROR`).
- A stream with the same ID already exists (`STREAM_FAILED`).
- A stream lookup fails: "Stream not found" (`STREAM_FAILED`), in three methods.

### 3.5 `ValidationException`

Input data fails validation.

| Property | Type | Set by |
|----------|------|--------|
| `FieldName` | `string?` | `(fieldName, invalidValue, validationRule, message)` |
| `InvalidValue` | `object?` | `(fieldName, invalidValue, validationRule, message)` |
| `ValidationRule` | `string?` | `(fieldName, invalidValue, validationRule, message)` |

| Constructor | `ErrorCode` | `GrpcStatus` | Message |
|-------------|-------------|--------------|---------|
| `(message)` | `VALIDATION_ERROR` | `InvalidArgument` | as given |
| `(message, innerException)` | `VALIDATION_ERROR` | `InvalidArgument` | as given |
| `(fieldName, invalidValue, validationRule, message)` | `VALIDATION_FAILED` | `InvalidArgument` | `Validation failed for '{field}': {message} (Value: {value}, Rule: {rule})` |

**Thrown when**
- The core pipeline does not throw this type. The only construction in `src/` is `ValidationExceptionExtensions.Combine`, which builds a combined instance from several validation failures and returns it rather than throwing it.
- The four-argument constructor is available to callers that validate their own input. It is the only constructor that populates the field-level properties used by the HTTP mapper (section 4.1).

---

## 4. HTTP mapping

`ErrorHandlingMiddleware` (`src/GrpcWebBridge/Middleware/ErrorHandlingMiddleware.cs`) catches every exception thrown inside the request pipeline, logs it at Error level, and calls `exception.ToProblemDetails(context)` (`ProblemDetailsExtensions.cs`). The status code comes from the resulting `ProblemDetails.Status`; a response is always written as `application/json`.

The mapper is a `switch` on the runtime type, evaluated top to bottom. The first matching case wins, so derived types are matched before their bases.

### 4.1 Mapping table

| Exception type | HTTP status | `Title` | `Type` URI (RFC 7807) | Extra `details` entries |
|----------------|-------------|---------|-----------------------|-------------------------|
| `ValidationException` | 400 | `Validation Failed` | `rfc7231#section-6.5.1` | `field`, `providedValue`, `validationRule`, `errors` |
| `ProtocolException` | 400 | `Protocol Translation Failed` | `rfc7231#section-6.5.1` | `sourceFormat`, `targetFormat` (`"unknown"` if null) |
| `StreamingException` | **500** | `Streaming Operation Failed` | `rfc7231#section-6.6.1` | `streamId` (`"unknown"` if null), `streamState` |
| `ServiceRegistrationException` | **400** | `Service Registration Failed` | `rfc7231#section-6.5.1` | `serviceName` (`"unknown"` if null), `serviceEndpoint` |
| `ConfigurationException` | 400 | `Configuration Error` | `rfc7231#section-6.5.1` | `configurationKey` (`"unknown"` if null), `configurationValue` |
| `ArgumentNullException` | 400 | `Invalid Request` | `rfc7231#section-6.5.1` | `paramName` |
| `ArgumentException` | 400 | `Invalid Argument` | `rfc7231#section-6.5.1` | none |
| `GrpcWebBridgeException` (base, direct instances only) | from `GrpcStatus` (table 4.2), or **500** if unset | `GrpcStatus` title, or `Bridge Operation Failed` | `about:blank` | `errorCode` (if set), plus every `Context` entry |
| `UnauthorizedAccessException` | 401 | `Unauthorized` | `rfc7235#section-3.1` | none |
| `TimeoutException` | 504 | `Operation Timeout` | `rfc7231#section-6.6.4` | none |
| `OperationCanceledException` | **400** | `Operation Cancelled` | `rfc7231#section-6.6.3` | none |
| Anything else (including `EventBusException`) | 500 | `Internal Server Error` | `about:blank` | none |

Every exception, regardless of type, also gets an `exceptionType` entry in `details` with its full .NET type name. So `details` is always present for exceptions that reach the middleware.

The `ErrorResponse` body contains: `success` (always `false`), `type`, `title`, `error` (same as `title`), `status`, `detail`, `message` (same as `detail`), `instance`, `path`, `traceId`, `timestamp`, and `details`. The body is serialized with camelCase names.

### 4.2 `GrpcStatus` to HTTP status

Used only for direct `GrpcWebBridgeException` instances that have a `GrpcStatus` set.

| `GrpcStatusCode` | HTTP | Title |
|------------------|------|-------|
| `InvalidArgument` | 400 | Invalid Argument |
| `OutOfRange` | 400 | Out of Range |
| `DeadlineExceeded` | 504 | Deadline Exceeded |
| `NotFound` | 404 | Not Found |
| `AlreadyExists` | 409 | Already Exists |
| `PermissionDenied` | 403 | Permission Denied |
| `ResourceExhausted` | 429 | Resource Exhausted |
| `FailedPrecondition` | 412 | Failed Precondition |
| `Aborted` | 500 | Aborted |
| `Unimplemented` | 501 | Not Implemented |
| `Internal` | 500 | Internal Error |
| `Unavailable` | 503 | Service Unavailable |
| `DataLoss` | 500 | Data Loss |
| `Unauthenticated` | 401 | Unauthenticated |
| any other / unset | 500 | Bridge Operation Failed |

### 4.3 Things the mapping does not do

These are easy to miss when reading a response or writing a test:

- **Derived types ignore `GrpcStatus` and `ErrorCode`.** Only the generic `GrpcWebBridgeException` branch reads `GrpcStatus` and adds `errorCode`. A `ServiceRegistrationException` with `GrpcStatus.NotFound` still returns 400, and a `StreamingException` with `GrpcStatus.Internal` still returns 500. Derived types also do not copy `Context` into `details`. Their `ErrorCode` is never exposed over HTTP.
- **Only direct `GrpcWebBridgeException` instances copy `Context` and `errorCode`.** Those are the ones thrown by `AuthenticationService` and by the generic branch of `HttpClientFactory`.
- **`EventBusException` is not mapped.** If it escapes a request, it becomes a generic 500 with `Detail` set to its message.
- **`ValidationException.ToProblemDetails(this ValidationException)` in `ValidationExceptionExtensions.cs` is not used by the middleware.** The middleware passes an `Exception`-typed value, so the generic `ToProblemDetails(this Exception, HttpContext?)` overload is selected.

---

## 5. `EventBusException`

Defined in `src/GrpcWebBridge/Events/EventBus.cs`.

| Aspect | Value |
|--------|-------|
| Base class | `System.Exception` (sealed; not a `GrpcWebBridgeException`) |
| Constructor | `(string message, Exception? innerException = null)` |
| Extra properties | none |
| `ToString()` | `EventBusException: {Message}` plus `, InnerException: {type}: {message}` when an inner exception is present |
| HTTP mapping | Generic 500 (section 4.1) |

**Thrown by `EventBus.PublishAsync<TEvent>`** in two situations.

1. **Inline dispatch (the default, `UseAsyncDispatch = false`).** Every subscribed handler runs, even if an earlier one throws. Each failure is logged and collected. If any failed, the bus throws `EventBusException("One or more event handlers failed for event type {name}", new AggregateException(failures))`. The exception is raised only after all handlers have run, so `InnerException` is an `AggregateException` holding every handler failure.
2. **Async dispatch (`UseAsyncDispatch = true`).** When the dispatch queue is full:
   - With `DispatchQueueFullPolicy.Block`, a write that does not complete within `MaxWaitTime` (default 5 s) throws `EventBusException("Event bus queue full and blocking timeout exceeded ...")`.
   - With `DispatchQueueFullPolicy.DropOldest` (the default policy), the code catches an `InvalidOperationException` from the channel write and throws `EventBusException("Event bus queue full with drop-oldest policy ...")`. The channel is configured to drop the oldest item rather than fail, so this path is defensive and is not expected in normal operation.

Handler failures in async dispatch are handled on the dispatch loop and are not rethrown to the publisher.

`PublishAsync` also throws `ArgumentNullException` for a null event and `ObjectDisposedException` after `Dispose`. Those are standard .NET types, not `EventBusException`.

---

## 6. Known quirks

- **`ServiceRegistry` uses `SERVICE_NOT_FOUND` for a duplicate registration.** The constructor `ServiceRegistrationException(serviceName, message)` always sets that code, and the duplicate-registration and full-registry paths both use it. Treat the code as "service registry rejected the request" rather than "not found".
- **`HttpClientFactory` wraps remote HTTP failures as `ConfigurationException`.** A 4xx or 5xx from a remote endpoint becomes `CONFIG_INVALID`, which maps to 400 even when the remote server failed. Callers that need to distinguish the cause should inspect `ConfigurationValue` or the inner exception.
- **`AuthenticationService` throws direct `GrpcWebBridgeException` values with no `GrpcStatus`.** Token-expired and invalid-token failures therefore map to 500 `Bridge Operation Failed`, not 401. The `errorCode` (`TOKEN_EXPIRED`, `INVALID_TOKEN`, ...) is still returned in `details`.
- **`ValidationException` is not thrown by the core pipeline.** See section 3.5.
- **`ProtocolException` and `ServiceRegistrationException` set `ErrorCode` in their constructors, but the HTTP body never shows it** because of the derived-type behaviour described in section 4.3.
