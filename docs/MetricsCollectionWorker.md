# MetricsCollectionWorker

The `MetricsCollectionWorker` is a core component within the `grpc-web-bridge` project responsible for aggregating runtime performance data and request statistics. It operates by periodically sampling system resources such as CPU usage, memory consumption, and thread count, while simultaneously tracking gRPC request volumes and error rates. This class maintains a historical record of these snapshots for trend analysis and provides configurable thresholds to trigger alerts when resource utilization or error rates exceed defined limits.

## API

### Constructors

#### `public MetricsCollectionWorker`
Initializes a new instance of the `MetricsCollectionWorker` class. This constructor sets up the internal data structures required for metric aggregation and starts the background collection logic based on default or configured intervals.

### Properties

#### `public DateTime Timestamp`
Gets the timestamp of the most recent metric collection cycle. This value indicates when the current values for CPU, memory, and request metrics were last updated.

#### `public double CpuUsagePercent`
Gets the current CPU usage percentage calculated during the last collection interval. The value ranges from 0.0 to 100.0.

#### `public double MemoryUsageMb`
Gets the current memory usage in megabytes (MB) as observed during the last collection cycle.

#### `public int ThreadCount`
Gets the number of active threads in the current process at the time of the last measurement.

#### `public object? GcCollections`
Gets an object representing garbage collection statistics. The specific structure of this object depends on the underlying runtime implementation, and it may be `null` if GC data is unavailable or not yet collected.

#### `public RequestMetricsData? RequestMetrics`
Gets the detailed metrics related to gRPC requests, such as latency distributions or payload sizes. This property returns `null` if no request data has been recorded yet.

#### `public long TotalRequests`
Gets the cumulative count of all requests processed since the worker was initialized or the history was last cleared.

#### `public long TotalErrors`
Gets the cumulative count of all failed requests encountered since the worker was initialized or the history was last cleared.

#### `public double ErrorRate`
Gets the calculated error rate as a percentage based on recent request activity. This value represents the ratio of `TotalErrors` to `TotalRequests` over a specific sliding window or the entire lifetime, depending on implementation details.

#### `public int CollectionIntervalSeconds`
Gets or sets the interval, in seconds, at which the worker samples system metrics and updates the snapshot history. Modifying this value affects the frequency of future collections.

#### `public int MaxSnapshotsToKeep`
Gets or sets the maximum number of historical `MetricsSnapshot` entries retained in memory. Once this limit is reached, the oldest snapshots are discarded to prevent unbounded memory growth.

#### `public double CpuAlertThresholdPercent`
Gets or sets the CPU usage percentage threshold. If `CpuUsagePercent` exceeds this value, the system may trigger an alert condition (logic for handling the alert is external to this property).

#### `public double MemoryAlertThresholdMb`
Gets or sets the memory usage threshold in megabytes. If `MemoryUsageMb` exceeds this value, an alert condition may be triggered.

#### `public double ErrorRateAlertThresholdPercent`
Gets or sets the error rate percentage threshold. If `ErrorRate` exceeds this value, an alert condition may be triggered.

### Methods

#### `public object GetAggregatedMetrics`
Retrieves a consolidated object containing the current aggregated metrics.
*   **Return Value**: An `object` representing the current state of all collected metrics. The concrete type of this object is implementation-specific.
*   **Exceptions**: May throw exceptions if the internal aggregation state is corrupted or if accessed during disposal (if applicable).

#### `public List<MetricsSnapshot> GetSnapshotHistory`
Retrieves the list of historically collected metric snapshots.
*   **Return Value**: A `List<MetricsSnapshot>` containing up to `MaxSnapshotsToKeep` entries, ordered chronologically.
*   **Exceptions**: Generally does not throw unless the internal list is in an invalid state.

#### `public void ClearHistory`
Clears all stored historical snapshots and resets cumulative counters such as `TotalRequests` and `TotalErrors` to zero.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: May throw if called concurrently with a collection cycle in an unsafe manner, though the class is expected to handle internal synchronization.

## Usage

### Example 1: Basic Initialization and Monitoring
This example demonstrates how to instantiate the worker, configure thresholds, and retrieve the current system status.

```csharp
using GrpcWebBridge.Metrics;

// Initialize the worker
var worker = new MetricsCollectionWorker();

// Configure thresholds
worker.CpuAlertThresholdPercent = 85.0;
worker.MemoryAlertThresholdMb = 1024.0;
worker.ErrorRateAlertThresholdPercent = 5.0;

// Adjust collection frequency and history size
worker.CollectionIntervalSeconds = 10;
worker.MaxSnapshotsToKeep = 100;

// Retrieve current metrics
var currentMetrics = worker.GetAggregatedMetrics();
Console.WriteLine($"CPU: {worker.CpuUsagePercent:F2}%");
Console.WriteLine($"Memory: {worker.MemoryUsageMb:F2} MB");
Console.WriteLine($"Active Threads: {worker.ThreadCount}");

// Check for alert conditions manually if needed
if (worker.CpuUsagePercent > worker.CpuAlertThresholdPercent)
{
    Console.WriteLine("WARNING: CPU usage exceeds threshold.");
}
```

### Example 2: Historical Analysis and Reset
This example shows how to analyze historical trends using snapshots and how to reset the data after a deployment or maintenance window.

```csharp
using GrpcWebBridge.Metrics;
using System.Linq;

// Assume worker is already running and collecting data
var history = worker.GetSnapshotHistory();

// Calculate average memory usage over the last available snapshots
if (history.Any())
{
    double avgMemory = history.Average(s => s.MemoryUsageMb);
    Console.WriteLine($"Average Memory Usage ({history.Count} samples): {avgMemory:F2} MB");
    
    // Identify peak error rate in history
    var peakErrorSample = history.OrderByDescending(s => s.ErrorRate).FirstOrDefault();
    if (peakErrorSample != null)
    {
        Console.WriteLine($"Peak Error Rate: {peakErrorSample.ErrorRate:F2}% at {peakErrorSample.Timestamp}");
    }
}

// Perform a reset after analyzing the data
worker.ClearHistory();

// Verify reset
Console.WriteLine($"Total Requests after clear: {worker.TotalRequests}");
Console.WriteLine($"History count after clear: {worker.GetSnapshotHistory().Count}");
```

## Notes

*   **Thread Safety**: The `MetricsCollectionWorker` is designed to be accessed from multiple threads. Properties such as `CpuUsagePercent` and `TotalRequests` are updated by a background collection thread while being read by monitoring threads. Implementations typically utilize locking mechanisms or atomic operations to ensure consistency. However, when calling `ClearHistory`, care should be taken in high-concurrency scenarios as it resets cumulative counters which might be in the middle of an increment operation.
*   **Snapshot Management**: The `GetSnapshotHistory` method returns a list bounded by `MaxSnapshotsToKeep`. If the collection interval is very short and `MaxSnapshotsToKeep` is large, memory consumption may increase. It is recommended to balance these two properties based on the required resolution for historical analysis.
*   **Nullability**: Properties `GcCollections` and `RequestMetrics` are nullable. Consumers must check for `null` before dereferencing these properties, especially immediately after initialization or during periods of low activity where specific data points might not be populated.
*   **Alert Thresholds**: Setting threshold properties (e.g., `CpuAlertThresholdPercent`) does not automatically invoke callback methods or log warnings. These properties serve as configuration values for external monitoring systems or logic that polls the worker's state.
*   **Data Precision**: Metrics like `ErrorRate` and `CpuUsagePercent` are represented as `double`. Small floating-point inaccuracies may occur; comparisons for alerting should account for potential precision variance if exact equality checks are used (though greater-than comparisons are standard for thresholds).
