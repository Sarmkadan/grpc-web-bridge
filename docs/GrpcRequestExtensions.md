# GrpcRequestExtensions

`GrpcRequestExtensions` provides a set of helper extension methods for the `GrpcRequest` model used throughout the gRPC‑Web Bridge. The methods simplify common tasks such as inspecting metadata, logging request details, and working with the request payload without requiring callers to manipulate the underlying request object directly.

## API

| Member | Signature | Description |
|--------|-----------|-------------|
| **HasMetadataKey** | `public static bool HasMetadataKey(this GrpcRequest request, string key)` | Returns `true` if the request's metadata collection contains the specified `key`. The comparison is case‑insensitive. Throws `ArgumentNullException` if `request` or `key` is `null`. |
| **GetMetadataValue** | `public static string? GetMetadataValue(this GrpcRequest request, string key)` | Retrieves the first value associated with `key` from the request's metadata. Returns `null` when the key is absent. Throws `ArgumentNullException` if `request` or `key` is `null`. |
| **GetMetadataValue\<T>** | `public static T? GetMetadataValue<T>(this GrpcRequest request, string key)` | Attempts to convert the metadata value for `key` to the type `T` using the default `System.Convert` logic. Returns the converted value or `default(T)` if the key is missing or conversion fails. Throws `ArgumentNullException` if `request` or `key` is `null`. |
| **ToLogString** | `public static string ToLogString(this GrpcRequest request)` | Produces a concise, single‑line string suitable for logging. The output includes the request ID, service name, method name, and a short payload summary (size and hash). Throws `ArgumentNullException` if `request` is `null`. |
| **GetPayloadSize** | `public static int GetPayloadSize(this GrpcRequest request)` | Returns the size of the request payload in bytes. If the payload is `null`, the size is `0`. Throws `ArgumentNullException` if `request` is `null`. |
| **GetPayloadHashHex** | `public static string GetPayloadHashHex(this GrpcRequest request)` | Computes a SHA‑256 hash of the payload and returns it as a lower‑case hexadecimal string. For an empty or `null` payload, returns a string of 64 zeroes. Throws `ArgumentNullException` if `request` is `null`. |
| **IsPayloadEmpty** | `public static bool IsPayloadEmpty(this GrpcRequest request)` | Returns `true` when the payload is `null` or has a length of zero. Throws `ArgumentNullException` if `request` is `null`. |

## Usage

### Example 1 – Logging a request with metadata inspection

