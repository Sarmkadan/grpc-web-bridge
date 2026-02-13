# MetricsCollectionWorkerExtensions

The `MetricsCollectionWorkerExtensions` class provides a set of static utility methods designed to extend the functionality of the metrics collection subsystem within the gRPC Web Bridge. It facilitates the retrieval, analysis, and aggregation of historical metric data, enabling consumers to extract snapshots, identify peak usage patterns, analyze trends, and compute statistical slopes without directly manipulating the underlying storage mechanisms.

## API

### GetSnapshotsInRange
Retrieves a chronological list of metric snapshots occurring within a specified time window.
*   **Purpose**: To fetch historical data points for further processing or visualization.
*   **Parameters**: Accepts start and end `DateTime` boundaries defining the query range.
*   **Return Value**: Returns a `List<MetricsSnapshot>` containing all recorded snapshots falling within the inclusive range.
*   **Exceptions**: Throws an `ArgumentOutOfRangeException` if the start time is later than the end time, or if the range exceeds the maximum retention policy configured for the worker.

### GetPeakUsageStatistics
Computes aggregate statistics regarding the highest observed metric values over a defined period.
*   **Purpose**: To identify capacity bottlenecks and maximum load conditions.
*   **Parameters**: Requires a target metric identifier and an optional time scope.
*   **Return Value**: Returns an `object` containing a structured summary of peak values, timestamps of occurrence, and associated metadata.
*   **Exceptions**: Throws a `MetricsNotFoundException` if the specified metric identifier does not exist in the current collection context.

### GetTrendAnalysis
Performs a statistical analysis to determine the directional movement of metric values over time.
*   **Purpose**: To assess whether metric values are increasing, decreasing, or remaining stable.
*   **Parameters**: Takes a collection of data points or a specific metric key with a duration.
*   **Return Value**: Returns an `object` encapsulating trend coefficients, confidence intervals, and directional indicators.
*   **Exceptions**: Throws an `InvalidOperationException` if the provided dataset contains insufficient points to calculate a statistically significant trend.

### GetAlertSummary
Aggregates current alert states and recent trigger events into a concise summary object.
*   **Purpose**: To provide a high-level overview of system health and active violations.
*   **Parameters**: Accepts optional filters for severity levels or specific alert categories.
*   **Return Value**: Returns an `object` representing the summary, including counts of active, acknowledged, and resolved alerts.
*   **Exceptions**: Throws a `SecurityAccessException` if the current execution context lacks permissions to view alert configurations.

### CalculateSlope
Calculates the mathematical slope of a series of metric values to quantify the rate of change.
*   **Purpose**: To determine the velocity of metric growth or decay between data points.
*   **Parameters**: Accepts a `List<double>` representing the sequence of Y-axis values (assuming uniform X-axis intervals).
*   **Return Value**: Returns a `double` representing the calculated slope.
*   **Exceptions**: Throws an `ArgumentException` if the input list is null, empty, or contains fewer than two elements required for slope calculation.

## Usage

The following example demonstrates retrieving historical snapshots and calculating the rate of change for a specific interval.

```csharp
using GrpcWebBridge.Metrics;
using System;
using System.Collections.Generic;

public class MetricsAnalyzer
{
    public void AnalyzeLastHour()
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddHours(-1);

        // Retrieve raw data points
        List<MetricsSnapshot> snapshots = MetricsCollectionWorkerExtensions.GetSnapshotsInRange(startTime, endTime);
        
        if (snapshots.Count > 1)
        {
            // Extract values and calculate the slope of the trend
            List<double> values = snapshots.ConvertAll(s => s.Value);
            double changeRate = MetricsCollectionWorkerExtensions.CalculateSlope(values);
            
            Console.WriteLine($"Rate of change: {changeRate} units per interval");
        }
    }
}
```

The following example illustrates how to generate a peak usage report and a trend analysis summary for capacity planning.

```csharp
using GrpcWebBridge.Metrics;
using System;

public class CapacityPlanner
{
    public void GenerateReport(string metricId)
    {
        try 
        {
            // Get peak statistics object
            object peakStats = MetricsCollectionWorkerExtensions.GetPeakUsageStatistics(metricId);
            
            // Get trend analysis object
            object trendData = MetricsCollectionWorkerExtensions.GetTrendAnalysis(metricId);
            
            // Deserialize or cast objects based on expected internal schema
            Console.WriteLine("Peak statistics and trend analysis generated successfully.");
        }
        catch (MetricsNotFoundException ex)
        {
            Console.Error.WriteLine($"Failed to generate report: {ex.Message}");
        }
    }
}
```

## Notes

*   **Thread Safety**: All methods in `MetricsCollectionWorkerExtensions` are static and designed to be thread-safe. They operate on immutable snapshots or create defensive copies of input data where necessary to prevent race conditions during concurrent read/write operations on the metrics store.
*   **Empty Collections**: When calling `CalculateSlope`, ensure the input list contains at least two data points; passing a single-element list will result in an exception rather than returning zero, as a slope cannot be mathematically defined for a single point.
*   **Return Types**: Several methods (`GetPeakUsageStatistics`, `GetTrendAnalysis`, `GetAlertSummary`) return `object` types. Consumers must cast these results to the specific internal DTOs defined in the `GrpcWebBridge.Metrics.Models` namespace to access individual properties.
*   **Time Zones**: `GetSnapshotsInRange` expects input `DateTime` objects to be in UTC. Passing local time values without conversion may result in incorrect data retrieval ranges relative to the server's storage format.
