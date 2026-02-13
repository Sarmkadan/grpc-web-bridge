# BasicUsageExample

`BasicUsageExample` is a console‑based demonstration that shows how to interact with a gRPC‑Web bridge using the client‑side helpers provided by the `grpc-web-bridge` library. It encapsulates common operations such as health checking, invoking a simple service method, and retrieving bridge metrics.

## API

### BasicUsageExample()
Creates a new instance of the example helper. The constructor has no parameters and does not perform any network activity. Subsequent method calls on the instance will communicate with the bridge endpoint configured internally.

### CheckBridgeHealthAsync()
```csharp
public async Task<bool> CheckBridgeHealthAsync()
```
Sends a lightweight request to the bridge to verify that it is reachable and responsive.  
- **Return value:** `true` if the bridge replies with a successful status; `false` if the bridge reports an unhealthy state.  
- **Exceptions:** May throw `HttpRequestException` for network‑level failures, `TaskCanceledException` if the request times out, or any exception propagated by the underlying HTTP client (e.g., `InvalidOperationException` if the client is not initialized).

### MakeSimpleCallAsync()
```csharp
public async Task<object?> MakeSimpleCallAsync()
```
Invokes a predefined simple gRPC method via the bridge and returns the deserialized response.  
- **Return value:** The response object returned by the service, or `null` if the service returns an empty message.  
- **Exceptions:** Throws `HttpRequestException` on connectivity problems, `TaskCanceledException` on timeout, and `JsonException` (or similar) if the payload cannot be deserialized. Any gRPC‑level status errors are surfaced as `RpcException`.

### GetBridgeMetricsAsync()
```csharp
public async Task<string?> GetBridgeMetricsAsync()
```
Retrieves a textual representation of the bridge’s internal metrics (commonly in Prometheus format).  
- **Return value:** A string containing the metrics payload, or `null` if the bridge does not expose metrics.  
- **Exceptions:** Same as the other async methods: `HttpRequestException`, `TaskCanceledException`, and deserialization‑related exceptions if the payload is expected to be JSON.

### Main(string[] args)
```csharp
public static async Task Main(string[] args)
```
The application entry point. It instantiates `BasicUsageExample`, sequentially calls the health check, makes a simple call, and fetches metrics, writing the results to the console.  
- **Parameters:** `args` – command‑line arguments (currently unused).  
- **Return value:** A `Task` representing the asynchronous operation; the method completes when all demonstrations finish.  
- **Exceptions:** Any exception thrown by the invoked instance methods is caught, logged to stderr, and causes the program to exit with a non‑zero status code.

## Usage

```csharp
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var example = new BasicUsageExample();

        bool healthy = await example.CheckBridgeHealthAsync();
        Console.WriteLine($"Bridge healthy: {healthy}");

        object? response = await example.MakeSimpleCallAsync();
        Console.WriteLine($"Simple call response: {response ?? "<null>"}");

        string? metrics = await example.GetBridgeMetricsAsync();
        Console.WriteLine($"Metrics:\n{metrics ?? "<none>"}");
    }
}
```

A minimal variant that only verifies bridge availability:

```csharp
using System.Threading.Tasks;

class HealthCheckOnly
{
    static async Task<int> Main(string[] args)
    {
        var example = new BasicUsageExample();
        bool ok = await example.CheckBridgeHealthAsync();
        return ok ? 0 : 1;
    }
}
```

## Notes

- All instance methods are independent; they do not share mutable state, so concurrent calls from multiple threads are safe as long as the underlying HTTP client used internally is thread‑safe (the library’s default client meets this requirement).  
- If the bridge endpoint is not reachable, each method will throw an exception rather than returning a sentinel value; callers should handle `HttpRequestException` and related exceptions appropriately.  
- The `Main` method demonstrates a typical sequential workflow; for production use, consider isolating each operation and applying appropriate retry or timeout policies.  
- Return types that are nullable (`object?`, `string?`) indicate that the bridge may legitimately return an empty payload; a `null` result does not itself indicate an error.  
- The static `Main` method is the sole thread of execution when the application is launched; any asynchronous work it initiates runs on thread‑pool threads, but the method itself does not impose thread‑affinity constraints on the instance methods.
