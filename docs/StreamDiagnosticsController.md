# StreamDiagnosticsController

The `StreamDiagnosticsController` is an ASP.NET Core controller that exposes HTTP endpoints for retrieving streaming diagnostics from the bidirectional streaming engine. It provides aggregated metrics across all active streams, including message counts, byte counts, backpressure events, and credit wait times.

## API

### `StreamDiagnosticsController(IBidirectionalStreamingEngine engine)`
- **Purpose**: Initializes a new instance of the controller.
- **Parameters**:
  - `engine` (IBidirectionalStreamingEngine) – The streaming engine providing diagnostic metrics.
- **Return value**: None.
- **Throws**: `ArgumentNullException` if `engine` is `null`.

### `IActionResult GetDiagnostics()`
- **Purpose**: Returns an aggregate snapshot of all active bidirectional streams.
- **Parameters**: None.
- **Return value**: An `IActionResult` containing a JSON object with the following properties:
  - `ActiveStreamCount` (int) – Number of currently active streams.
  - `TotalMessagesIn` (long) – Total inbound messages across all streams.
  - `TotalMessagesOut` (long) – Total outbound messages across all streams.
  - `TotalBytesIn` (long) – Total inbound bytes across all streams.
  - `TotalBytesOut` (long) – Total outbound bytes across all streams.
  - `TotalBackpressureEvents` (long) – Total backpressure events across all streams.
  - `TotalCreditWaitMs` (long) – Total credit wait time in milliseconds across all streams.
  - `ZeroActivityStreamCount` (int) – Number of streams with zero inbound and outbound messages.
  - `HighBackpressureStreamCount` (int) – Number of streams where the backpressure event ratio exceeds the warning threshold (0.10 by default).
- **Throws**: None under normal operation; may throw if the underlying engine is unavailable.

## Usage

### Example 1: Registering the controller and querying diagnostics

```csharp
// In Startup.cs or Program.cs:
app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<MyGrpcService>();
    endpoints.MapControllers(); // Registers StreamDiagnosticsController
});
```

### Example 2: Fetching diagnostics from an external monitoring tool

```csharp
// Using HttpClient to fetch diagnostics:
var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

var diagnostics = await client.GetStringAsync("/api/streams/diagnostics");
```

## Notes

- **Thread safety**: The controller relies on the thread-safety of the `IBidirectionalStreamingEngine` implementation. The `GetAllMetrics` method is expected to be thread-safe.
- **Edge cases**:
  - If there are no active streams, the counts will be zero and the totals will be zero.
  - The `HighBackpressureStreamCount` uses the same threshold as the background service (0.10) by default, but note that the threshold is hardcoded in the controller (see `DefaultBackpressureWarnThreshold`).
- **Performance**: The diagnostics endpoint aggregates data from all active streams. If there are a very large number of streams, consider caching or limiting the frequency of calls.