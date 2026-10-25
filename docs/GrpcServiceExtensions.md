# GrpcServiceExtensions

Extension methods for the `GrpcService` class (`GrpcWebBridge.Domain.Models`). `GrpcService` describes a backend gRPC service the bridge can call: its endpoint, port, TLS flag and metadata. These helpers read those properties and build derived values from them. None of them modify the `GrpcService` instance.

## API

### GetFullEndpoint
```csharp
public static string GetFullEndpoint(this GrpcService service)
```
Builds the full endpoint URL for the service in the form `{scheme}://{Endpoint}:{Port}`. The scheme is `https` when `UseTls` is `true` and `http` otherwise.

- **Parameters**
  - `service`: The `GrpcService` instance to read. Must not be `null`.
- **Return value**
  - A URL string such as `https://example.com:443` or `http://localhost:50051`.
- **Exceptions**
  - `ArgumentNullException` if `service` is `null`.
- **Notes**
  - The method does not validate `Endpoint` or `Port`. Those checks run when the service is constructed or validated, not here.
  - `Endpoint` is inserted as-is. An IPv6 literal such as `::1` is not wrapped in brackets, so the result is not a valid URL. Use a host name or an IPv4 address in that case.
  - The scheme comes from a private helper, `GetScheme`. It is not part of the public API.

## Usage

### Building the URL for a plain-text service
```csharp
var service = new GrpcService
{
    Endpoint = "localhost",
    Port = 50051,
    UseTls = false
};

string url = service.GetFullEndpoint();
// "http://localhost:50051"
```

### Building the URL for a TLS service
```csharp
var service = new GrpcService
{
    Endpoint = "example.com",
    Port = 443,
    UseTls = true
};

string url = service.GetFullEndpoint();
// "https://example.com:443"
```

### GetMetadataValueOrDefault
```csharp
public static string GetMetadataValueOrDefault(this GrpcService service, string key, string defaultValue = "")
```
Looks up `key` in `service.Metadata` and returns its value. When the key is missing, it returns `defaultValue` instead. The lookup is case-sensitive, because it uses the dictionary's default comparer.

- **Parameters**
  - `service`: The `GrpcService` instance to read. Must not be `null`.
  - `key`: The metadata key to look up. Must not be `null` or empty.
  - `defaultValue`: The value returned when `key` is not present. Defaults to `""`.
- **Return value**
  - The stored value for `key`, or `defaultValue` if the key is not present. A key that is present with an empty value returns that empty string, not `defaultValue`.
- **Exceptions**
  - `ArgumentNullException` if `service` or `key` is `null`.
  - `ArgumentException` if `key` is an empty string.
- **Notes**
  - Use this instead of `GrpcService.GetMetadata(key)` when you want a fallback value and not a `null` result.

## Usage

### Reading an auth token with a fallback
```csharp
var service = new GrpcService
{
    Endpoint = "greet.internal",
    Port = 50051
};

service.SetMetadata("auth-token", "abc123");

string token = service.GetMetadataValueOrDefault("auth-token");   // "abc123"
string region = service.GetMetadataValueOrDefault("region", "eu-west-1"); // "eu-west-1" - key missing
```

### Distinguishing a missing key from an empty value
```csharp
service.SetMetadata("tenant", "");

string tenant = service.GetMetadataValueOrDefault("tenant", "default");
// "" - the key exists, so its empty value is returned and "default" is not used
```

### GetAllMetadataKeys
```csharp
public static IReadOnlyCollection<string> GetAllMetadataKeys(this GrpcService service)
```
Returns the metadata keys defined for the service as a read-only collection.

- **Parameters**
  - `service`: The `GrpcService` instance to read. Must not be `null`.
- **Return value**
  - An `IReadOnlyCollection<string>` with one entry per key in `service.Metadata`. The collection is empty when the service has no metadata. Keys are returned in the dictionary's enumeration order, which is not guaranteed to be insertion order.
- **Exceptions**
  - `ArgumentNullException` if `service` is `null`.
- **Notes**
  - The method returns a snapshot. It copies the keys into a new list, so keys added or removed later do not change a collection that was already returned. Call the method again to see the current keys.
  - The method returns keys only. Use `GetMetadataValueOrDefault` to read the values.

## Usage

### Listing metadata keys for diagnostics
```csharp
var service = new GrpcService { Endpoint = "greet.internal", Port = 50051 };
service.SetMetadata("auth-token", "abc123");
service.SetMetadata("region", "eu-west-1");

foreach (string key in service.GetAllMetadataKeys())
{
    logger.LogDebug("Metadata key present: {Key}", key);
}
// Metadata key present: auth-token
// Metadata key present: region
```

### Checking for a key before reading it
```csharp
IReadOnlyCollection<string> keys = service.GetAllMetadataKeys();

if (keys.Contains("auth-token"))
{
    // Safe to read the value; GetMetadataValueOrDefault would also work without this check.
}
```

## Related

- [GrpcService](GrpcService.md) - the model these extensions operate on.
- [GrpcServiceValidation](GrpcServiceValidation.md) - validation of `GrpcService` endpoints, ports and metadata.
