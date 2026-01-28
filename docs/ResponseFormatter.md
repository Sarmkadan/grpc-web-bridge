# ResponseFormatter

Utility class that builds structured response objects for the gRPC‑Web bridge layer. The methods produce plain‑CLR objects that can be serialized to JSON (typically via `ToJson`) and sent over HTTP to clients.

## API

### FormatSuccess<T>
**Purpose** – Creates a response object representing a successful unary call with a payload of type `T`.  
**Parameters**  
- `value` – The result value of type `T`.  
**Return value** – An `object` whose shape conforms to the bridge’s success contract (e.g., `{ success: true, data: <value> }`).  
**Throws** – `ArgumentNullException` if `value` is `null` and the bridge configuration treats null results as invalid.

### FormatSuccessList<T>
**Purpose** – Creates a response object representing a successful call that returns a collection of items.  
**Parameters**  
- `items` – An `IEnumerable<T>` containing the list of results.  
**Return value** – An `object` representing a success response with a list payload (e.g., `{ success: true, data: [<items>] }`).  
**Throws** – `ArgumentNullException` if `items` is `null`.

### FormatError
**Purpose** – Produces an error response object for generic failure conditions.  
**Parameters**  
- `message` – Human‑readable error description.  
- `code` (optional) – Numeric error code; defaults to a framework‑defined generic code if omitted.  
**Return value** – An `object` representing an error response (e.g., `{ success: false, error: { message, code } }`).  
**Throws** – `ArgumentNullException` if `message` is `null`; `ArgumentOutOfRangeException` if `code` is negative when supplied.

### FormatValidationError
**Purpose** – Builds a response object for validation failures, typically containing a collection of validation details.  
**Parameters**  
- `failures` – An `IEnumerable<ValidationFailure>` where each element describes a specific validation problem.  
**Return value** – An `object` representing a validation error response (e.g., `{ success: false, validationErrors: [<failures>] }`).  
**Throws** – `ArgumentNullException` if `failures` is `null`; throws if any element in the enumeration is `null`.

### FormatStreamingResponse
**Purpose** – Wraps a stream of messages for server‑streaming or bidirectional streaming calls.  
**Parameters**  
- `stream` – An `IAsyncEnumerable<object>` or similar stream abstraction that yields response messages.  
**Return value** – An `object` that the bridge can interpret as a streaming response (often a placeholder that triggers chunked encoding).  
**Throws** – `ArgumentNullException` if `stream` is `null`.

### FormatBatchResponse
**Purpose** – Combines multiple individual responses into a single batch payload.  
**Parameters**  
- `responses` – An `IEnumerable<object>` where each item is a previously formatted response (success, error, etc.).  
**Return value** – An `object` representing a batch response (e.g., `{ batch: [<responses>] }`).  
**Throws** – `ArgumentNullException` if `responses` is `null`; throws if any element is `null`.

### FormatHealthCheckResponse
**Purpose** – Formats the outcome of a health‑checking service call.  
**Parameters**  
- `result` – A `HealthCheckResult` (or equivalent) containing status and optional payload.  
**Return value** – An `object` representing a health check response (e.g., `{ status: <result.Status>, details: <result.Details> }`).  
**Throws** – `ArgumentNullException` if `result` is `null`.

### FormatStatisticsResponse
**Purpose** – Encodes statistics or telemetry data for transmission.  
**Parameters**  
- `stats` – A `Statistics` object (or DTO) holding metric values.  
**Return value** – An `object` representing a statistics response (e.g., `{ statistics: <stats> }`).  
**Throws** – `ArgumentNullException` if `stats` is `null`.

### WrapResponse
**Purpose** – Takes an already‑formatted response object and applies any additional wrapping required by the bridge (e.g., adding protocol headers or envelope).  
**Parameters**  
- `response` – The response object to wrap.  
**Return value** – An `object` that is the wrapped version of the input.  
**Throws** – `ArgumentNullException` if `response` is `null`.

### ToJson
**Purpose** – Serializes a response object to its JSON representation suitable for HTTP transmission.  
**Parameters**  
- `obj` – The response object to serialize.  
**Return value** – A `string` containing the JSON text.  
**Throws** – `ArgumentNullException` if `obj` is `null`; may throw a `JsonSerializationException` if the object graph contains unsupported types.

### CreateCustomResponse
**Purpose** – Allows manual creation of a response from a pre‑formed JSON string, bypassing the standard formatting pipeline.  
**Parameters**  
- `json` – A valid JSON string representing the complete response payload.  
**Return value** – A `string` that is the JSON payload ready to be sent (often returned unchanged for direct use).  
**Throws** – `ArgumentNullException` if `json` is `null`; throws a `FormatException` if the string is not valid JSON.

### FormatServiceRegistryResponse
**Purpose** – Formats a response containing the service registry (list of available gRPC services) for discovery endpoints.  
**Parameters**  
- `registry` – A `ServiceRegistry` object (or similar) describing services and their methods.  
**Return value** – An `object` representing the registry response (e.g., `{ services: [<registry entries>] }`).  
**Throws** – `ArgumentNullException` if `registry` is `null`.

### FormatConfigurationResponse
**Purpose** – Formats a response that exposes configuration information (e.g., feature flags, environment settings).  
**Parameters**  
- `config` – A `Configuration` object (or DTO) holding key‑value pairs or hierarchical settings.  
**Return value** – An `object` representing the configuration response (e.g., `{ configuration: <config> }`).  
**Throws** – `ArgumentNullException` if `config` is `null`.

## Usage

```csharp
using GrpcWebBridge.Formatting;

// Successful unary call returning a user profile
var user = userService.GetProfile(request.UserId);
var success = ResponseFormatter.FormatSuccess(user);
string json = ResponseFormatter.ToJson(success);
// json now contains: {"success":true,"data":{...}}
```

```csharp
using GrpcWebBridge.Formatting;
using System.Collections.Generic;

// Handling validation errors and batching multiple responses
var validationErrors = new List<ValidationFailure>
{
    new ValidationFailure("Email", "Email address is required"),
    new ValidationFailure("Age", "Age must be greater than zero")
};

var validationResp = ResponseFormatter.FormatValidationError(validationErrors);
var healthResp     = ResponseFormatter.FormatHealthCheckResponse(HealthCheckResult.Healthy());

var batch = ResponseFormatter.FormatBatchResponse(new[]
{
    validationResp,
    healthResp
});

string batchJson = ResponseFormatter.ToJson(batch);
// batchJson contains a JSON array with the validation error and health check objects
```

## Notes

- All methods are **static** and contain no mutable state; therefore they are thread‑safe and can be invoked concurrently from any thread.  
- Null‑checking is performed primarily on arguments that are required for a meaningful response; passing `null` where not allowed results in an `ArgumentNullException`.  
- Generic methods (`FormatSuccess<T>` and `FormatSuccessList<T>`) impose no constraints on `T`; however, the subsequent call to `ToJson` will fail if the type cannot be serialized by the underlying JSON serializer.  
- The `CreateCustomResponse` method does **not** re‑serialize the supplied JSON; it assumes the caller has already produced a valid JSON string. Providing malformed JSON will cause a `FormatException` when the bridge attempts to transmit the payload.  
- Responses produced by the formatting methods are plain CLR objects; they are intentionally free of bridge‑specific types to keep the serialization layer agnostic. Consumers should treat them as opaque payloads until they are passed to `ToJson` (or a custom serializer) for transmission.  
- No method returns `null` under normal operation; a null return would indicate a programming error in the formatter itself.
