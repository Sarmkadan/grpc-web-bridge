# Exception Extensions Catalog

Reference for five exception helper files in `src/GrpcWebBridge/Domain/Exceptions/` that were not covered by a per-type doc. All of them live in the `GrpcWebBridge.Domain.Exceptions` namespace.

| File | Type | Kind |
|------|------|------|
| `ProtocolExceptionExtensions.cs` | `ProtocolExceptionExtensions` | Extension methods on `ProtocolException` |
| `ProtocolExceptionValidation.cs` | `ProtocolExceptionValidation` | Validation extension methods on `ProtocolException` |
| `ServiceRegistrationExceptionExtensions.cs` | `ServiceRegistrationExceptionExtensions` | Extension methods on `ServiceRegistrationException` (plus one static helper) |
| `GrpcWebBridgeExceptionExtensions.cs` | `GrpcWebBridgeExceptionExtensions` | Extension methods on `GrpcWebBridgeException` |
| `ProblemDetailsExtensions.cs` | `ProblemDetailsExtensions` | RFC 7807 conversion and helpers for `Exception` / `ProblemDetails` |

Shared conventions:
- Null arguments are rejected with `ArgumentNullException.ThrowIfNull`; empty strings with `ArgumentException.ThrowIfNullOrEmpty`.
- Methods that return an exception return the same instance (mutating it) unless stated otherwise.

```csharp
using GrpcWebBridge.Domain.Exceptions;
```

---

## 1. ProtocolExceptionExtensions

Source: `src/GrpcWebBridge/Domain/Exceptions/ProtocolExceptionExtensions.cs`

Helpers for logging and correlating a `ProtocolException` (see [ProtocolException](ProtocolException.md)). Neither method modifies the exception.

| Method | Signature | Returns | Description |
|--------|-----------|---------|-------------|
| `ToLogString` | `string ToLogString(this ProtocolException exception)` | `string` | Single-line log text: `ProtocolException: {Message} (SourceFormat: …, TargetFormat: …, RequestId: …)`. Missing values render as `null`. |
| `IsRelatedToRequest` | `bool IsRelatedToRequest(this ProtocolException exception, string requestId)` | `bool` | `true` when `exception.RequestId` equals `requestId` (ordinal `==` comparison). |

**Exceptions**
- `ToLogString`: `ArgumentNullException` if `exception` is `null`.
- `IsRelatedToRequest`: `ArgumentNullException` if `exception` or `requestId` is `null`; `ArgumentException` if `requestId` is empty.

**Example**

```csharp
try
{
    await translator.TranslateAsync(request, cancellationToken);
}
catch (ProtocolException ex) when (ex.IsRelatedToRequest(request.RequestId))
{
    logger.LogWarning("{Details}", ex.ToLogString());
    throw;
}
```

Output of `ToLogString` for `new ProtocolException("json", "grpc", "Unsupported payload") { RequestId = "r-42" }`:

```
ProtocolException: Unsupported payload (SourceFormat: json, TargetFormat: grpc, RequestId: r-42)
```

---

## 2. ProtocolExceptionValidation

Source: `src/GrpcWebBridge/Domain/Exceptions/ProtocolExceptionValidation.cs`

Validates the format metadata carried by a `ProtocolException`. The rules are:

| Rule | Error message |
|------|---------------|
| `SourceFormat` must be non-empty | `SourceFormat is required.` |
| `TargetFormat` must be non-empty | `TargetFormat is required.` |
| `RequestId` must be `null` or non-empty | `RequestId must be null or a non-empty string.` |

| Method | Signature | Returns | Description |
|--------|-----------|---------|-------------|
| `Validate` | `IReadOnlyList<string> Validate(this ProtocolException value)` | `IReadOnlyList<string>` | Read-only list of error messages; empty when valid. |
| `IsValid` | `bool IsValid(this ProtocolException value)` | `bool` | `true` when `Validate` returns no errors. |
| `EnsureValid` | `void EnsureValid(this ProtocolException value)` | `void` | Throws `ArgumentException` when invalid. The message is `ProtocolException is invalid:` followed by each error on its own line. |

**Exceptions**: all three throw `ArgumentNullException` when `value` is `null`. `EnsureValid` additionally throws `ArgumentException` for invalid instances.

**Example**

```csharp
var error = new ProtocolException(message: "Bad payload")
{
    SourceFormat = "json",
    TargetFormat = "",          // invalid: empty
};

if (!error.IsValid())
{
    IReadOnlyList<string> problems = error.Validate();
    // problems == ["TargetFormat is required."]
}

error.EnsureValid();            // throws ArgumentException
```

Note: the `(message)` constructor leaves the formats unset, so set `SourceFormat` and `TargetFormat` through the properties as shown, or use the `(sourceFormat, targetFormat, message)` constructor for valid instances.

---

## 3. ServiceRegistrationExceptionExtensions

Source: `src/GrpcWebBridge/Domain/Exceptions/ServiceRegistrationExceptionExtensions.cs`

Diagnostics for `ServiceRegistrationException` (see [ServiceRegistrationException](ServiceRegistrationException.md)) and a helper for aggregating several registration failures.

| Method | Signature | Returns | Description |
|--------|-----------|---------|-------------|
| `ToDetailedString` | `string ToDetailedString(this ServiceRegistrationException exception)` | `string` | `{Message} (Service: {ServiceName ?? "N/A"}, Endpoint: {ServiceEndpoint ?? "N/A"})`, formatted with the invariant culture. |
| `TryExtractEndpoint` | `bool TryExtractEndpoint(this ServiceRegistrationException exception, out string? endpoint)` | `bool` | Always assigns `endpoint = ServiceEndpoint`. Returns `true` only when that value is non-empty. |
| `CombineExceptions` | `AggregateException CombineExceptions(IEnumerable<ServiceRegistrationException> exceptions)` | `AggregateException` | **Static helper, not an extension method.** Wraps the non-null items in an `AggregateException`. |

**Exceptions**
- `ToDetailedString`, `TryExtractEndpoint`: `ArgumentNullException` if `exception` is `null`.
- `CombineExceptions`: `ArgumentNullException` if the sequence is `null`; `ArgumentException` if the sequence is empty, or if it contains only `null` items.

**Example**

```csharp
var failures = new List<ServiceRegistrationException>
{
    new ServiceRegistrationException("orders", "https://orders.internal:5001", "Health check failed"),
    new ServiceRegistrationException("billing", "Duplicate service name"),
};

foreach (var failure in failures)
{
    logger.LogError("{Details}", failure.ToDetailedString());
    // "Health check failed (Service: orders, Endpoint: https://orders.internal:5001)"
    // "Duplicate service name (Service: billing, Endpoint: N/A)"
}

if (failures[0].TryExtractEndpoint(out var endpoint))
{
    // endpoint == "https://orders.internal:5001"
}

AggregateException all = ServiceRegistrationExceptionExtensions.CombineExceptions(failures);
```

Note: `CombineExceptions` is called on the type, not on an instance. Its `ArgumentException` checks run before the null filter, so a sequence that is all `null` is rejected with the "non-null exception" message.

---

## 4. GrpcWebBridgeExceptionExtensions

Source: `src/GrpcWebBridge/Domain/Exceptions/GrpcWebBridgeExceptionExtensions.cs`

Helpers for the `Context` dictionary and error code of the base `GrpcWebBridgeException` (see [GrpcWebBridgeException](GrpcWebBridgeException.md)).

| Method | Signature | Returns | Description |
|--------|-----------|---------|-------------|
| `AddContextEntry` | `GrpcWebBridgeException AddContextEntry(this GrpcWebBridgeException exception, string key, object value)` | same instance | Adds `key = value` to `Context` (via `AddContext`, so an existing key is overwritten) and returns the exception for chaining. |
| `GetContextString` | `string GetContextString(this GrpcWebBridgeException exception)` | `string` | `key: value` pairs joined with `", "`. Returns `string.Empty` when `Context` is empty. Order follows the dictionary enumeration order. |
| `WithNewErrorCode` | `GrpcWebBridgeException WithNewErrorCode(this GrpcWebBridgeException exception, string newErrorCode)` | **new instance** | Creates a `GrpcWebBridgeException` with the same `Message` and `InnerException`, and sets `ErrorCode`. The original is not changed. |

**Exceptions**
- All three: `ArgumentNullException` if `exception` is `null`.
- `AddContextEntry`: `ArgumentNullException` if `key` is `null`; `ArgumentException` if `key` is empty.

**Caveats**
- `WithNewErrorCode` does **not** copy `Context`, `GrpcStatus`, or the derived type. The result is always a plain `GrpcWebBridgeException`, so a `ProtocolException` passed in comes back as a base-class instance. Copy any other state yourself if you need it.
- The XML comment on `AddContextEntry` says `ArgumentNullException` for an empty key. The code throws `ArgumentException` for an empty key. Treat the code as correct.

**Example**

```csharp
var ex = new GrpcWebBridgeException("Upstream call failed", GrpcStatusCode.Unavailable)
    .AddContextEntry("service", "orders")
    .AddContextEntry("attempt", 3);

logger.LogError("{Context}", ex.GetContextString());
// "service: orders, attempt: 3"

var coded = ex.WithNewErrorCode("ORDERS_UNAVAILABLE");
// coded.Message == ex.Message; coded.ErrorCode == "ORDERS_UNAVAILABLE"
// coded.Context is empty and coded.GrpcStatus is null (not copied)
```

---

## 5. ProblemDetailsExtensions

Source: `src/GrpcWebBridge/Domain/Exceptions/ProblemDetailsExtensions.cs`

Builds RFC 7807 `ProblemDetails` responses from exceptions and adds fields to them. See [ProblemDetails](ProblemDetails.md) for the model.

| Method | Signature | Returns | Description |
|--------|-----------|---------|-------------|
| `ToProblemDetails` | `ProblemDetails ToProblemDetails(this Exception exception, HttpContext? httpContext = null)` | `ProblemDetails` | Maps the exception to a `ProblemDetails` using the table below. Sets `Timestamp` (UTC now), `TraceId` and `Path` from `httpContext` when provided, and always adds `Extensions["exceptionType"]` (the full type name). |
| `AddValidationError` | `void AddValidationError(this ProblemDetails problemDetails, string fieldName, string errorMessage, object? invalidValue = null)` | `void` | Adds `errors[fieldName] = errorMessage`, sets `Extensions["field"]`, and sets `Extensions["value"]` when `invalidValue` is not `null`. Creates the `errors` dictionary if missing. |
| `AddContext` | `void AddContext(this ProblemDetails problemDetails, string key, object? value)` | `void` | Sets `Extensions[key] = value`. |

**Exceptions**
- `ToProblemDetails`: `ArgumentNullException` if `exception` is `null`.
- `AddValidationError`: `ArgumentNullException` if `problemDetails` is `null`; `ArgumentException` if `fieldName` or `errorMessage` is empty.
- `AddContext`: `ArgumentNullException` if `problemDetails` is `null`; `ArgumentException` if `key` is empty.

### Type mapping in `ToProblemDetails`

Defaults are `Type = "about:blank"`, `Title = "Internal Server Error"`, `Status = 500`, `Detail = exception.Message`. The first matching case in the `switch` wins, so the order below matters (`ArgumentNullException` is tested before `ArgumentException`, and the base `GrpcWebBridgeException` is tested last).

| Exception | Title | Status | Type | Extra extensions |
|-----------|-------|--------|------|------------------|
| `ValidationException` | Validation Failed | 400 | `rfc7231#section-6.5.1` | `field`, `providedValue`, `validationRule`, `errors` (dictionary) |
| `ProtocolException` | Protocol Translation Failed | 400 | `rfc7231#section-6.5.1` | `sourceFormat`, `targetFormat` (`"unknown"` when null) |
| `StreamingException` | Streaming Operation Failed | 500 | `rfc7231#section-6.6.1` | `streamId` (`"unknown"` when null), `streamState` |
| `ServiceRegistrationException` | Service Registration Failed | 400 | `rfc7231#section-6.5.1` | `serviceName` (`"unknown"` when null), `serviceEndpoint` |
| `ConfigurationException` | Configuration Error | 400 | `rfc7231#section-6.5.1` | `configurationKey` (`"unknown"` when null), `configurationValue` |
| `ArgumentNullException` | Invalid Request | 400 | `rfc7231#section-6.5.1` | `paramName` |
| `ArgumentException` | Invalid Argument | 400 | `rfc7231#section-6.5.1` | none |
| `GrpcWebBridgeException` | Bridge Operation Failed (or gRPC-derived title) | 500, or mapped from `GrpcStatus` | Default | `errorCode`, plus every `Context` entry copied verbatim |
| `UnauthorizedAccessException` | Unauthorized | 401 | `rfc7235#section-3.1` | none |
| `TimeoutException` | Operation Timeout | 504 | `rfc7231#section-6.6.4` | none |
| `OperationCanceledException` | Operation Cancelled | 400 | `rfc7231#section-6.6.3` | none |
| Any other exception | Internal Server Error | 500 | `about:blank` | none |

When `GrpcStatus` is set on a `GrpcWebBridgeException`, the status is mapped as follows: `InvalidArgument` and `OutOfRange` → 400; `NotFound` → 404; `AlreadyExists` → 409; `PermissionDenied` → 403; `ResourceExhausted` → 429; `FailedPrecondition` → 412; `Unimplemented` → 501; `Unavailable` → 503; `DeadlineExceeded` → 504; `Unauthenticated` → 401; all other codes → 500.

**Caveats**
- `Context` values are copied into the response `Extensions` verbatim. Keep secrets and internal data out of `Context`.
- `AddValidationError` writes the invalid value under `value`, but `ToProblemDetails` for `ValidationException` uses `providedValue`. Consumers should not assume one key for both paths.

### Examples

Converting an exception inside middleware:

```csharp
catch (Exception ex)
{
    var problem = ex.ToProblemDetails(context);
    context.Response.StatusCode = problem.Status ?? 500;
    await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
}
```

Building a validation response by hand:

```csharp
var problem = new ProblemDetails
{
    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
    Title = "Validation Failed",
    Status = StatusCodes.Status400BadRequest,
};

problem.AddValidationError("email", "Email is malformed.", "not-an-email");
problem.AddValidationError("name", "Name is required.");
problem.AddContext("requestId", "r-42");
// Extensions: errors = { email: "...", name: "..." }, field = "name", value = "not-an-email", requestId = "r-42"
```

The `ProblemDetails` model itself is documented in [ProblemDetails](ProblemDetails.md).
