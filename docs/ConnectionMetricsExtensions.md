# ConnectionMetricsExtensions

Extension methods for the `ConnectionMetrics` class (`GrpcWebBridge.Data`). `ConnectionMetrics` is the per-connection statistics record kept by `GrpcConnectionManager`; these helpers derive values from its raw counters without changing them.

## API

### GetAverageBytesPerRequest
```csharp
public static double GetAverageBytesPerRequest(this ConnectionMetrics metrics)
```
Returns the average number of bytes transferred per request on the connection. Bytes sent and bytes received are added together and divided by `RequestCount`.

- **Formula**: `(BytesSent + BytesReceived) / RequestCount`
- **Parameters**
  - `metrics`: The `ConnectionMetrics` instance to read. Must not be `null`.
- **Return value**
  - The average as a `double`. Returns `0` when `RequestCount` is `0`, so no division by zero occurs.
  - The sum is cast to `double` before the division, so the result keeps its fractional part (for example, 10 bytes over 4 requests gives `2.5`, not `2`).
- **Exceptions**
  - `ArgumentNullException` if `metrics` is `null`.
- **Notes**
  - The method only reads the properties. It does not modify the metrics object or the connection.
  - The result covers both directions combined. To get only the outbound or inbound average, compute it from `BytesSent` or `BytesReceived` directly.

## Usage

### Computing the average for a fixed record
```csharp
var metrics = new ConnectionMetrics
{
    ServiceName = "greet.Greeter",
    Address = "http://localhost:5000",
    RequestCount = 4,
    BytesSent = 2_000,
    BytesReceived = 6_000
};

double average = metrics.GetAverageBytesPerRequest();
// (2_000 + 6_000) / 4 = 2_000
```

### Handling a connection with no traffic
```csharp
var idle = new ConnectionMetrics { RequestCount = 0 };

double average = idle.GetAverageBytesPerRequest();
// 0 - no requests recorded, so no division is performed
```

### Reading metrics from the connection manager
```csharp
ConnectionMetrics? metrics = connectionManager.GetMetrics("greet.Greeter");

// GetMetrics returns null for an unknown service, so use null-conditional access.
double? average = metrics?.GetAverageBytesPerRequest();

if (average is { } bytes)
{
    logger.LogInformation("Average payload per request: {Bytes:F1} bytes", bytes);
}
```

## Related

- [GrpcConnectionManager](GrpcConnectionManager.md) - owns the `ConnectionMetrics` instances and exposes them through `GetMetrics`.
- [GrpcConnectionManagerExtensions](GrpcConnectionManagerExtensions.md) - helpers for selecting and listing connection metrics.
