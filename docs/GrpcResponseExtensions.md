# GrpcResponseExtensions

`GrpcResponseExtensions` provides helper extension methods for creating `GrpcResponse` instances and adding initial or trailing metadata. The methods centralize common success and error response setup while guarding required inputs.

## API

| Member | Signature | Description |
|--------|-----------|-------------|
| **ToSuccessResponse** | `public static GrpcResponse ToSuccessResponse(this string requestId, byte[] payload, SerializationFormat format = SerializationFormat.Protobuf)` | Creates a successful response with status `GrpcStatusCode.Ok`, status message `"OK"`, the supplied payload, and the supplied serialization format. The format defaults to `SerializationFormat.Protobuf`. Throws `ArgumentNullException` if `requestId` or `payload` is `null`; an empty payload is allowed. |
| **ToErrorResponse** | `public static GrpcResponse ToErrorResponse(this string requestId, GrpcStatusCode statusCode, string message, string? details = null)` | Creates an error response with the supplied status, message, and optional details. The payload is empty. Throws `ArgumentNullException` if `requestId` or `message` is `null`, and `ArgumentException` if `statusCode` is `GrpcStatusCode.Ok`. |
| **AddMetadata** | `public static void AddMetadata(this GrpcResponse response, Dictionary<string, string> metadata)` | Adds entries to `response.Metadata`. Existing keys are overwritten, empty dictionaries make no changes, and null, empty, or whitespace-only keys are skipped. Throws `ArgumentNullException` if `response` or `metadata` is `null`. |
| **AddTrailingMetadata** | `public static void AddTrailingMetadata(this GrpcResponse response, Dictionary<string, string> trailingMetadata)` | Adds entries to `response.TrailingMetadata`. Existing keys are overwritten, empty dictionaries make no changes, and null, empty, or whitespace-only keys are skipped. Throws `ArgumentNullException` if `response` or `trailingMetadata` is `null`. |

## Usage

### Example 1 – Creating a successful response with metadata

```csharp
var payload = new byte[] { 0xAA, 0xBB, 0xCC };

var response = "request-123".ToSuccessResponse(payload);

response.AddMetadata(new Dictionary<string, string>
{
    ["X-API-Version"] = "1.0",
    ["X-Environment"] = "production"
});

response.AddTrailingMetadata(new Dictionary<string, string>
{
    ["X-Request-ID"] = "request-123"
});

// response.Status == GrpcStatusCode.Ok
// response.StatusMessage == "OK"
// response.PayloadFormat == SerializationFormat.Protobuf
// response.Metadata["X-API-Version"] == "1.0"
// response.TrailingMetadata["X-Request-ID"] == "request-123"
```

### Example 2 – Creating an error response

```csharp
var response = "error-request".ToErrorResponse(
    GrpcStatusCode.ResourceExhausted,
    "Rate limit exceeded",
    "Daily quota of 1000 requests exceeded");

response.AddMetadata(new Dictionary<string, string>
{
    ["Retry-After"] = "3600"
});

// response.IsSuccess == false
// response.Payload is empty
// response.ErrorDetails == "Daily quota of 1000 requests exceeded"
// response.Metadata["Retry-After"] == "3600"
```

### Example 3 – Input guards and metadata filtering

```csharp
// Null required arguments throw ArgumentNullException.
string? requestId = null;
requestId!.ToSuccessResponse(new byte[] { 0x01 });

// An OK status cannot be used to construct an error response.
"request-456".ToErrorResponse(GrpcStatusCode.Ok, "Invalid error");
// Throws ArgumentException.

var response = "request-789".ToSuccessResponse(Array.Empty<byte>());
response.AddMetadata(new Dictionary<string, string>
{
    ["valid-key"] = "value",
    [""] = "ignored",
    ["  "] = "also ignored"
});

// Only "valid-key" is added.
```
