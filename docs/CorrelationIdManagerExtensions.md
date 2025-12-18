# CorrelationIdManagerExtensions

`CorrelationIdManagerExtensions` provides a suite of static extension methods designed to facilitate the management, monitoring, and diagnostic tracking of correlation IDs and associated tracing operations within the `grpc-web-bridge` framework. These utilities enable developers to inspect request lifecycles, evaluate performance metrics, and manage trace persistence for debugging and system observability.

## API

### HasCorrelationId
Checks whether a valid correlation ID is currently associated with the active execution context.
- **Returns:** `bool` - `true` if a correlation ID is present; otherwise, `false`.

### GetTraceDuration
Retrieves the elapsed time of the current trace, if a trace is active.
- **Returns:** `TimeSpan?` - The duration of the trace, or `null` if no trace is active or the duration is not yet available.

### IsTraceSuccessful
Determines if the current trace operation completed successfully.
- **Returns:** `bool` - `true` if the trace is successful or still in progress; `false` if an error state was recorded.

### GetTraceError
Retrieves the error message associated with the current trace, if an error occurred.
- **Returns:** `string?` - The error description, or `null` if the trace was successful or no error is recorded.

### StartTraceWithAutoCorrelation
Initiates a new trace operation, automatically generating or retrieving a correlation ID for the scope.
- **Returns:** `CorrelationTrace` - The newly started `CorrelationTrace` object.

### GetStatisticsFormatted
Generates a human-readable string representation of the current system trace statistics.
- **Returns:** `string` - A formatted string containing aggregated trace metrics.

### CleanupOldTraces
Removes expired or stale trace records from the internal storage to prevent memory accumulation.
- **Returns:** `int` - The number of trace records successfully cleaned up.

### GetCurrentTraces
Retrieves a list of all currently active `CorrelationTrace` objects.
- **Returns:** `List<CorrelationTrace>` - A collection of active traces.

### HasTraces
Checks if there are any active trace records in the system.
- **Returns:** `bool` - `true` if one or more traces are active; otherwise, `false`.

### GetMostRecentTrace
Retrieves the most recently initiated `CorrelationTrace`.
- **Returns:** `CorrelationTrace?` - The most recent `CorrelationTrace`, or `null` if no traces exist.

## Usage

### Example 1: Monitoring Trace Status in Middleware
```csharp
public void Invoke(HttpContext context)
{
    if (CorrelationIdManagerExtensions.HasCorrelationId)
    {
        var duration = CorrelationIdManagerExtensions.GetTraceDuration();
        if (duration.HasValue && duration.Value.TotalMilliseconds > 500)
        {
            Log.Warning("Request took too long: {Duration}ms", duration.Value.TotalMilliseconds);
        }
    }
    
    // ... proceed with request pipeline
}
```

### Example 2: Managing Traces and Cleanup
```csharp
public void PerformMaintenance()
{
    if (CorrelationIdManagerExtensions.HasTraces)
    {
        var recentTrace = CorrelationIdManagerExtensions.GetMostRecentTrace();
        Console.WriteLine($"Latest trace ID: {recentTrace?.Id}");
        
        int cleaned = CorrelationIdManagerExtensions.CleanupOldTraces();
        Console.WriteLine($"Cleanup performed. Removed {cleaned} traces.");
    }
}
```

## Notes

- **Thread Safety:** The methods in this class are designed to be thread-safe when accessing ambient context information (such as `HttpContext`). However, when accessing global trace collections (`GetCurrentTraces`, `CleanupOldTraces`), thread safety depends on the underlying implementation of the trace store.
- **Nullability:** Several methods return nullable types (`string?`, `CorrelationTrace?`) to account for scenarios where a trace might not be active, or no error information is associated with a specific request. Always verify return values for `null` before accessing members.
- **Cleanup Frequency:** It is recommended to invoke `CleanupOldTraces` periodically in a background task or as part of a scheduled maintenance routine to ensure optimal memory utilization, depending on the volume of requests and the configured trace retention policy.
