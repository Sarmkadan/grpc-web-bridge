# ProblemDetails

`ProblemDetails` is the bridge's RFC 7807-style, machine-readable description of an HTTP error. It is defined in `src/GrpcWebBridge/Domain/Exceptions/ProblemDetails.cs`; exception conversion and enrichment helpers are defined in `ProblemDetailsExtensions.cs` in the same directory.

## Properties

| C# property | Type | JSON representation | Purpose |
| --- | --- | --- | --- |
| `Type` | `string?` | `type` | URI identifying the problem type. The parameterless constructor defaults it to `about:blank`. |
| `Title` | `string?` | `title` | Short, human-readable summary of the problem type. |
| `Status` | `int?` | `status` | HTTP status code associated with the problem. |
| `Detail` | `string?` | `detail` | Explanation specific to this occurrence. |
| `Instance` | `string?` | `instance` | URI identifying this particular occurrence. Conversion from an exception does not currently set it. |
| `Extensions` | `Dictionary<string, object?>` | Extension keys are written as top-level properties | Additional problem-specific values. The dictionary uses ordinal key comparison and starts empty. |
| `TraceId` | `string?` | Ignored when `ProblemDetails` is serialized directly | Request trace identifier, populated from `HttpContext.TraceIdentifier` when a context is supplied. |
| `Path` | `string?` | Ignored when `ProblemDetails` is serialized directly | Request path, populated from `HttpContext.Request.Path.Value`. |
| `Timestamp` | `DateTime?` | Ignored when `ProblemDetails` is serialized directly | UTC time at which exception conversion occurred. |

`Extensions` has `[JsonExtensionData]`, so entries such as `exceptionType`, `field`, or `errors` are flattened into a directly serialized `ProblemDetails` object. `TraceId`, `Path`, and `Timestamp` have `[JsonIgnore]`; the error middleware exposes them through its separate `ErrorResponse` transport model instead.

The class also provides constructors accepting no arguments, a problem type, or a problem type and integer status. `ToString()` returns the type, status, title, and detail as a diagnostic string.

## Public extension methods

### `ToProblemDetails`

```csharp
public static ProblemDetails ToProblemDetails(
    this Exception exception,
    HttpContext? httpContext = null)
```

Creates a `ProblemDetails` from an exception. A null exception causes `ArgumentNullException`. The initial values are `about:blank`, status `500`, the title `An error occurred while processing your request.`, the exception message, the current UTC timestamp, and—when supplied—the context's trace identifier and request path. The fully qualified exception type is always added to `Extensions` as `exceptionType`.

The method then specializes the result for known exceptions:

| Exception | HTTP status | Title / notable extensions |
| --- | ---: | --- |
| `ValidationException` | 400 | `Validation Failed`; may add `field`, `providedValue`, `validationRule`, and `errors`. |
| `ProtocolException` | 400 | `Protocol Translation Failed`; adds `sourceFormat` and `targetFormat`. |
| `StreamingException` | 500 | `Streaming Operation Failed`; adds `streamId` and, when present, `streamState`. |
| `ServiceRegistrationException` | 400 | `Service Registration Failed`; adds `serviceName` and possibly `serviceEndpoint`. |
| `ConfigurationException` | 400 | `Configuration Error`; adds `configurationKey` and possibly `configurationValue`. |
| `ArgumentNullException` | 400 | `Invalid Request`; adds `paramName`. |
| `ArgumentException` | 400 | `Invalid Argument`. |
| `GrpcWebBridgeException` | 500 by default | `Bridge Operation Failed`; a gRPC status can override the HTTP status/title, while `ErrorCode` and exception context become extensions. |
| `UnauthorizedAccessException` | 401 | `Unauthorized`. |
| `TimeoutException` | 504 | `Gateway Timeout`. |
| `OperationCanceledException` | 400 | `Operation Cancelled`. |
| Any other exception | 500 | Retains the initial generic title and `about:blank` type. |

### `AddValidationError`

```csharp
public static void AddValidationError(
    this ProblemDetails problemDetails,
    string fieldName,
    string errorMessage,
    object? invalidValue = null)
```

Adds or replaces the field's message in the `errors` extension dictionary and sets the `field` extension. A non-null invalid value is stored as `value`. The method rejects a null `problemDetails` and null or empty field names and messages. If an existing `errors` entry is not a `Dictionary<string, object?>`, it is replaced with one.

### `AddContext`

```csharp
public static void AddContext(
    this ProblemDetails problemDetails,
    string key,
    object? value)
```

Adds or replaces an arbitrary extension value. The method rejects a null `problemDetails` and a null or empty key; the value itself may be null.

## Production by ErrorHandlingMiddleware

`ErrorHandlingMiddleware.InvokeAsync(HttpContext)` invokes the next request delegate. If downstream processing throws, it logs the exception and converts it with `exception.ToProblemDetails(context)`. It then:

1. Sets the response content type to `application/json`.
2. Uses `ProblemDetails.Status` as the HTTP response status, falling back to 500 if it is null.
3. Copies the problem fields plus `Path`, `TraceId`, and `Timestamp` into an `ErrorResponse` whose `Success` value is `false`.
4. Assigns the extension dictionary to `ErrorResponse.Details` when it is non-empty.
5. Writes the response with `WriteAsJsonAsync` and a camel-case naming policy.

Consequently, the middleware response is not a direct serialization of `ProblemDetails`: it adds `success`, emits correlation values as `path`, `traceId`, and `timestamp`, and nests extension data under `details`.

For example, an `ArgumentException("Invalid page size")` raised while handling `/grpc/books` with trace identifier `trace-123` produces a payload with this shape (the timestamp is illustrative, and the runtime-qualified exception name may vary):

```json
{
  "success": false,
  "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
  "title": "Invalid Argument",
  "status": 400,
  "detail": "Invalid page size",
  "instance": null,
  "details": {
    "exceptionType": "System.ArgumentException"
  },
  "path": "/grpc/books",
  "traceId": "trace-123",
  "timestamp": "2026-09-08T12:00:00Z"
}
```

## Tests

`tests/grpc-web-bridge.Tests/ErrorHandlingMiddlewareTests.cs` exercises middleware conversion for bridge, validation, protocol, streaming, configuration, argument, authorization, timeout, cancellation, and unknown exceptions. It also covers the no-error pass-through path, JSON content type, HTTP status selection, and response-field structure. There is currently no separate test file dedicated to `ProblemDetails` or its two enrichment helpers.
