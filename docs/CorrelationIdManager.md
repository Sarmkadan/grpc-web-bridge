# CorrelationIdManager

The `CorrelationIdManager` class provides a centralized mechanism for managing correlation identifiers and their associated execution traces within the `grpc-web-bridge` project. It enables tracking of operations across asynchronous boundaries by generating, storing, and retrieving correlation identifiers, and by providing a structured way to record and query the lifecycle and metadata of traces associated with these operations.

## API

### Methods

*   **`CorrelationIdManager()`**: Initializes a new instance of the `CorrelationIdManager` class.
*   **`string GetOrCreateCorrelationId()`**: Retrieves the current correlation ID or generates a new one if it has not been set.
*   **`void SetCorrelationId(string id)`**: Explicitly sets the correlation ID.
*   **`string? GetCorrelationId()`**: Returns the current correlation ID, or `null` if none has been set.
*   **`CorrelationTrace StartTrace(string operationName, string? parentTraceId = null)`**: Initiates a new `CorrelationTrace` with the specified operation name, optionally linked to a parent trace.
*   **`void CompleteTrace(string traceId)`**: Marks the specified trace as completed.
*   **`CorrelationTrace? GetTrace(string traceId)`**: Retrieves a `CorrelationTrace` by its unique identifier, or `null` if the trace does not exist.
*   **`List<CorrelationTrace> GetTracesForCorrelation(string correlationId)`**: Retrieves all `CorrelationTrace` instances associated with the specified correlation ID.
*   **`void AddTraceMetadata(string traceId, string key, object value)`**: Attaches metadata to an existing trace identified by its `traceId`.
*   **`object GetStatistics()`**: Returns an object containing performance and usage metrics tracked by the manager.
*   **`int CleanupOldTraces()`**: Removes expired trace data from internal storage and returns the count of traces cleaned up.
*   **`void ClearAllTraces()`**: Removes all traces from internal storage.
*   **`void ClearCorrelationId()`**: Resets the current correlation ID to `null`.

### CorrelationTrace Properties

The following properties belong to the `CorrelationTrace` type returned by `StartTrace` and `GetTrace`.

*   **`string TraceId`**: A unique identifier for the trace.
*   **`string CorrelationId`**: The correlation ID associated with this trace.
*   **`string OperationName`**: The name of the operation being traced.
*   **`string? ParentTraceId`**: The unique identifier of the parent trace, if applicable.
*   **`DateTime StartTime`**: The timestamp when the trace was initiated.
*   **`DateTime? EndTime`**: The timestamp when the trace was completed, or `null` if still active.
*   **`bool Success`**: Indicates the outcome of the traced operation.

## Usage

### Managing a Correlation ID

```csharp
var manager = new CorrelationIdManager();
string correlationId = manager.GetOrCreateCorrelationId();

// Use correlationId in loggers or outgoing headers
// ...

manager.ClearCorrelationId();
```

### Tracing an Operation

```csharp
var manager = new CorrelationIdManager();
var trace = manager.StartTrace("DatabaseQueryOperation");

try 
{
    // Perform operation
    // ...
    trace.Success = true;
}
catch
{
    trace.Success = false;
    throw;
}
finally
{
    manager.CompleteTrace(trace.TraceId);
}
```

## Notes

*   **Thread Safety**: The `CorrelationIdManager` implementation is assumed to be thread-safe, allowing safe access and modification from multiple concurrent request contexts.
*   **Trace Lifetime**: `CorrelationTrace` instances should be completed promptly using `CompleteTrace` to ensure metrics and cleanup operations remain accurate and performant.
*   **Memory Management**: It is recommended to periodically invoke `CleanupOldTraces` in long-running services to prevent unbounded memory growth from stale trace data.
*   **Consistency**: Modifying properties of a `CorrelationTrace` instance directly (e.g., setting `Success`) should be done before calling `CompleteTrace` to ensure accurate state reporting.
