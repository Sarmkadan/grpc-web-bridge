# MetricsController

The `MetricsController` is an ASP.NET Core controller that exposes HTTP endpoints for retrieving and resetting runtime metrics collected from gRPC method calls within the `grpc-web-bridge` project. It provides a centralized way to inspect call counts, error rates, and streaming-specific statistics. The static methods `RecordMethodCall` and `RecordMethodError` are designed to be invoked from gRPC interceptors or service implementations to feed data into the metrics store that the controller reads.

## API

### `MetricsController()`
- **Purpose**: Initializes a new instance of the controller.
- **Parameters**: None.
- **Return value**: None.
- **Throws**: None.

### `IActionResult GetMetrics()`
- **Purpose**: Returns a snapshot of all accumulated metrics across all gRPC methods, including total calls, errors, and streaming statistics.
- **Parameters**: None.
- **Return value**: An `IActionResult` containing a JSON object with the aggregated metrics.
- **Throws**: None under normal operation; may throw if the underlying metrics store is unavailable (e.g., initialization failure).

### `IActionResult GetMethodMetrics(string methodName)`
- **Purpose**: Returns metrics specific to a single gRPC method, identified by its fully qualified name (e.g., `"package.Service/Method"`).
- **Parameters**:
  - `methodName` (string) – The fully qualified gRPC method name.
- **Return value**: An `IActionResult` containing a JSON object with the method’s call count, error count, and streaming details.
- **Throws**: `ArgumentNullException` if `methodName` is `null` or empty. `KeyNotFoundException` if no metrics exist for the given method.

### `IActionResult GetStreamingMetrics(string methodName)`
- **Purpose**: Returns metrics related to streaming gRPC calls (client, server, or bidirectional) for a specific method.
- **Parameters**:
  - `methodName` (string) – The fully qualified gRPC method name.
- **Return value**: An `IActionResult` containing a JSON object with streaming-specific counters (e.g., messages sent/received, active streams).
- **Throws**: `ArgumentNullException` if `methodName` is `null` or empty. `KeyNotFoundException` if no streaming metrics exist for the given method.

### `static void RecordMethodCall(string methodName)`
- **Purpose**: Records a successful gRPC method call. Intended to be called from server-side interceptors or service handlers after a request completes without error.
- **Parameters**:
  - `methodName` (string) – The fully qualified gRPC method name.
- **Return value**: None.
- **Throws**: `ArgumentNullException` if `methodName` is `null` or empty.

### `static void RecordMethodError(string methodName, string errorMessage)`
- **Purpose**: Records a failed gRPC method call, including an error description. Called when a request terminates with an exception or non‑OK status.
- **Parameters**:
  - `methodName` (string) – The fully qualified gRPC method name.
  - `errorMessage` (string) – A description of the error (e.g., exception message or gRPC status detail).
- **Return value**: None.
- **Throws**: `ArgumentNullException` if `methodName` or `errorMessage` is `null` or empty.

### `IActionResult ResetMetrics()`
- **Purpose**: Clears all accumulated metrics (call counts, errors, streaming data) and returns a confirmation.
- **Parameters**: None.
- **Return value**: An `IActionResult` with a success message (e.g., HTTP 200).
- **Throws**: None.

## Usage

### Example 1: Recording metrics from a gRPC interceptor and exposing them via HTTP

```csharp
// In a gRPC server interceptor:
public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
    TRequest request, ServerCallContext context,
    UnaryServerMethod<TRequest, TResponse> continuation)
{
    try
    {
        var response = await continuation(request, context);
        MetricsController.RecordMethodCall(context.Method);
        return response;
    }
    catch (Exception ex)
    {
        MetricsController.RecordMethodError(context.Method, ex.Message);
        throw;
    }
}

// In Startup.cs or Program.cs:
app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<MyGrpcService>();
    endpoints.MapControllers(); // Registers MetricsController
});
```

### Example 2: Querying metrics from an external monitoring tool

```csharp
// Using HttpClient to fetch metrics:
var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

// Get all metrics
var allMetrics = await client.GetStringAsync("/metrics/all");

// Get metrics for a specific method
var methodMetrics = await client.GetStringAsync("/metrics/method?methodName=myapp.MyService/DoWork");

// Get streaming metrics
var streamMetrics = await client.GetStringAsync("/metrics/streaming?methodName=myapp.MyService/StreamData");

// Reset metrics (e.g., after a deployment)
var resetResult = await client.PostAsync("/metrics/reset", null);
```

## Notes

- **Thread safety**: The static methods `RecordMethodCall` and `RecordMethodError` are thread‑safe and can be called concurrently from multiple gRPC handlers without corrupting internal counters. The controller actions read from the same thread‑safe store.
- **Edge cases**:
  - Calling `GetMethodMetrics` or `GetStreamingMetrics` for a method that has never been recorded will throw a `KeyNotFoundException`. Ensure the method name is valid and has been recorded at least once.
  - `ResetMetrics` clears all data immediately. Any concurrent `Record*` calls that occur during the reset may be lost or partially applied. For production use, consider a grace period or a separate reset endpoint that waits for in‑flight operations.
  - The `methodName` parameter must exactly match the string used in the gRPC interceptor (typically the `ServerCallContext.Method` value). Case‑sensitive matching is used.
- **Performance**: The metrics store is designed for low‑overhead recording; however, frequent calls to `GetMetrics` (e.g., every second) may cause contention. Use caching or a dedicated metrics pipeline for high‑frequency scraping.
