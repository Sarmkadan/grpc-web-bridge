# HttpClientFactoryExtensions

`HttpClientFactoryExtensions` is a static class with two extension methods for `HttpClientFactory` (`GrpcWebBridge.Integration`). They are convenience helpers that configure the shared client returned by `HttpClientFactory.GetClient()` and then either return it or send a GET request with it.

Both methods operate on the client named `"default"` (the default argument of `GetClient`). That client is cached and shared by every caller of the factory, so changes made by these helpers affect all subsequent users of that client. See [Caveats](#caveats) before using them.

## API

### GetClientWithBaseAddress

Returns the default client after setting its `BaseAddress`.

```csharp
public static HttpClient GetClientWithBaseAddress(this HttpClientFactory factory, string baseAddress)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `factory` | `HttpClientFactory` | The factory that supplies the client. Must not be `null`. |
| `baseAddress` | `string` | The absolute URI to use as the client's base address. Must not be `null` or empty. It is parsed with `new Uri(baseAddress)`. |

**Returns:** `HttpClient` - the client returned by `factory.GetClient()`, with `BaseAddress` set.

**Exceptions**

| Exception | When |
|-----------|------|
| `ArgumentNullException` | `factory` is `null`. |
| `ArgumentException` | `baseAddress` is `null` or empty. |
| `UriFormatException` | `baseAddress` is not a valid URI (for example, a relative path). |
| `InvalidOperationException` | The client has already sent a request, because `HttpClient.BaseAddress` can only be changed before the first request. |

### SendGetAsyncWithTimeout

Sends a GET request using the default client, with the client's `Timeout` set to the given value.

```csharp
public static async Task<HttpResponseMessage> SendGetAsyncWithTimeout(this HttpClientFactory factory, string requestUri, TimeSpan timeout)
```

**Parameters**

| Name | Type | Description |
|------|------|-------------|
| `factory` | `HttpClientFactory` | The factory that supplies the client. Must not be `null`. |
| `requestUri` | `string` | The request URI. It can be absolute, or relative if the client has a `BaseAddress`. Must not be `null` or empty. |
| `timeout` | `TimeSpan` | The timeout applied to the client before the request is sent. Must not be negative. |

**Returns:** `Task<HttpResponseMessage>` - completes with the response to the GET request. The method awaits with `ConfigureAwait(false)`.

**Exceptions**

| Exception | When |
|-----------|------|
| `ArgumentNullException` | `factory` is `null`. |
| `ArgumentException` | `requestUri` is `null` or empty. |
| `ArgumentOutOfRangeException` | `timeout` is negative (checked by these helpers). `HttpClient` also rejects `TimeSpan.Zero` and values above `int.MaxValue` milliseconds when setting `Timeout`. |
| `InvalidOperationException` | The client has already sent a request, because `HttpClient.Timeout` can only be changed before the first request. |
| `HttpRequestException` | The request failed at the transport level. |
| `TaskCanceledException` | The request exceeded `timeout`. |

## Caveats

- **Shared client.** `GetClient()` returns the cached `"default"` client. Setting `BaseAddress` or `Timeout` on it changes the behavior of every other user of that client in the process.
- **Changing properties after first use fails.** `HttpClient` only allows `BaseAddress` and `Timeout` to be changed before the first request. After the default client has been used once, calling either helper throws `InvalidOperationException`. In practice, call `GetClientWithBaseAddress` during startup, or use `HttpClientFactory.GetClientForUri` / `RegisterClient` for a client with fixed settings.
- **Zero timeout.** The guard allows `TimeSpan.Zero`, but `HttpClient.Timeout` does not accept zero, so the call throws `ArgumentOutOfRangeException`. Pass a positive `TimeSpan`.
- **Timeout is per client, not per request.** Because `Timeout` is set on the shared client, a later call with a different value overrides the earlier one for everyone.

## Usage

### Example 1: Creating a client bound to an upstream service

```csharp
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging.Abstractions;

var factory = new HttpClientFactory(NullLogger<HttpClientFactory>.Instance);

// Set the base address once, before any request is sent through the default client.
HttpClient client = factory.GetClientWithBaseAddress("https://api.example.com/");

var response = await client.GetAsync("v1/status");
response.EnsureSuccessStatusCode();
```

### Example 2: Sending a GET request with a timeout

```csharp
using GrpcWebBridge.Integration;

// Waits at most 5 seconds for the response.
using HttpResponseMessage response = await factory.SendGetAsyncWithTimeout(
    "https://api.example.com/v1/health",
    TimeSpan.FromSeconds(5));

string body = await response.Content.ReadAsStringAsync();
```

### Example 3: Handling a timeout

```csharp
using GrpcWebBridge.Integration;

try
{
    using var response = await factory.SendGetAsyncWithTimeout(
        "https://api.example.com/v1/slow-report",
        TimeSpan.FromSeconds(2));
    response.EnsureSuccessStatusCode();
}
catch (TaskCanceledException)
{
    // The request did not complete within 2 seconds.
}
catch (HttpRequestException ex)
{
    // Connection or protocol failure.
    Console.Error.WriteLine($"Request failed: {ex.Message}");
}
```

### Example 4: Validating arguments

```csharp
using System;
using GrpcWebBridge.Integration;

try
{
    factory.SendGetAsyncWithTimeout("https://api.example.com/", TimeSpan.FromSeconds(-1));
}
catch (ArgumentOutOfRangeException ex)
{
    Console.Error.WriteLine(ex.Message); // timeout must not be negative
}
```

## Related

- `HttpClientFactory` (`src/GrpcWebBridge/Integration/HttpClientFactory.cs`) - the factory these extensions extend.
- `HttpClientFactoryOptions` - sets the default timeout (`RequestTimeoutMs`) and connection pooling for clients the factory creates.
