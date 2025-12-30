# HealthCheckWorker

The `HealthCheckWorker` type encapsulates a periodic health‑checking routine for a gRPC‑Web service. It runs a timer‑based loop that evaluates the health of the identified service at a configurable interval, stores the result, and exposes statistics about the checks performed.

## API

### HealthCheckWorker (class)

Represents a worker that performs repeated health checks. Instances are created with the default constructor and then configured via properties before starting the internal timer (starting the timer is assumed to be handled elsewhere in the containing application).

### GetStatistics

```csharp
public object GetStatistics()
```

**Purpose**  
Returns a snapshot of diagnostic information collected by the worker, such as the number of checks performed, success/failure counts, and the last observed health state.

**Parameters**  
None.

**Return value**  
An `object` containing the statistics. The exact type is implementation‑specific; callers should cast or inspect the result as needed (e.g., via reflection or a known DTO).

**Exceptions**  
- `InvalidOperationException` – thrown if the worker has not been started or has been disposed, making statistics unavailable.

### CheckIntervalSeconds

```csharp
public int CheckIntervalSeconds { get; set; }
```

**Purpose**  
Specifies the delay, in seconds, between consecutive health checks. A value of zero or less disables periodic checking.

**Exceptions**  
- `ArgumentOutOfRangeException` – if a negative value is assigned.

### CheckTimeoutMs

```csharp
public int CheckTimeoutMs { get; set; }
```

**Purpose**  
Defines the maximum time, in milliseconds, that a single health check operation is allowed to run before being considered a timeout.

**Exceptions**  
- `ArgumentOutOfRangeException` – if a negative value is assigned.

### InitialDelaySeconds

```csharp
public int InitialDelaySeconds { get; set; }
```

**Purpose**  
Sets the initial wait time, in seconds, before the first health check is performed after the worker is started.

**Exceptions**  
- `ArgumentOutOfRangeException` – if a negative value is assigned.

### ServiceId

```csharp
public string ServiceId { get; set; }
```

**Purpose**  
A unique identifier for the service being monitored. Used internally to tag log entries and statistics.

**Exceptions**  
- `ArgumentNullException` – if `null` is assigned.

### ServiceName

```csharp
public string ServiceName { get; set; }
```

**Purpose**  
A human‑readable name of the service, useful for display in dashboards or alerts.

**Exceptions**  
- `ArgumentNullException` – if `null` is assigned.

### IsHealthy

```csharp
public bool IsHealthy { get; }
```

**Purpose**  
Indicates the result of the most recent health check. `true` means the service responded within the last check`false` indicates the service failed to respond correctly or timed out.

**Exceptions**  
None. The property is read‑only and always returns a Boolean value.

### Timestamp

```csharp
public DateTime Timestamp { get; }
```

**Purpose**  
The UTC date and time when the last health check was completed (whether successful or not).

**Exceptions**  
None. The property is read‑only and always returns a valid `DateTime`.

## Usage

### Example 1: Basic configuration and health check retrieval

```csharp
using System;
using GrpcWebBridge.Monitoring; // assumed namespace

var worker = new HealthCheckWorker
{
    ServiceId   = "svc-001",
    ServiceName = "OrderService",
    CheckIntervalSeconds = 30,
    CheckTimeoutMs       = 5000,
    InitialDelaySeconds  = 5
};

// Assume the worker's internal timer is started elsewhere.
Console.WriteLine($"Service {worker.ServiceName} healthy? {worker.IsHealthy}");
Console.WriteLine($"Last check at: {worker.Timestamp:O}");

// Obtain statistics for logging or UI display.
var stats = worker.GetStatistics();
Console.WriteLine($"Statistics: {stats}");
```

### Example 2: Adjusting intervals at runtime and handling missing data

```csharp
var worker = new HealthCheckWorker
{
    ServiceId   = "svc-042",
    ServiceName = "AuthService",
    CheckIntervalSeconds = 10,
    CheckTimeoutMs       = 2000,
    InitialDelaySeconds  = 0
};

// Start the worker (pseudo‑code; actual start mechanism depends on the host).
worker.Start(); // hypothetical method not part of the documented API

// After some runtime, increase the check interval to reduce load.
worker.CheckIntervalSeconds = 60;

// Safely retrieve statistics; handle the case where the worker isn't ready.
object stats;
try
{
    stats = worker.GetStatistics();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine("Unable to retrieve statistics: " + ex.Message);
    stats = null;
}

if (stats != null)
{
    Console.WriteLine("Current stats: " + stats);
}
```

## Notes

- **Thread safety** – The properties (`CheckIntervalSeconds`, `CheckTimeoutMs`, `InitialDelaySeconds`, `ServiceId`, `ServiceName`) are not synchronized. Concurrent reads and writes from multiple threads may lead to race conditions; external synchronization is required if the instance is accessed concurrently. The read‑only properties `IsHealthy` and `Timestamp` are updated by the worker's internal timer thread; reading them while they are being updated is safe because they are atomic for the respective types, but a memory barrier is not guaranteed, so stale values may be observed briefly.

- **Invalid values** – Assigning a negative value to any of the interval‑ or timeout‑related properties throws an `ArgumentOutOfRangeException`. Assigning `null` to `ServiceId` or `ServiceName` throws an `ArgumentNullException`.

- **GetStatistics availability** – The method will throw an `InvalidOperationException` if called before the worker has been started or after it has been stopped/disposed. Consumers should guard the call with a try/catch or ensure the worker's lifecycle is managed appropriately.

- **Timer precision** – The internal timer uses `System.Threading.Timer`; actual check intervals may drift slightly depending on system load. For strict scheduling requirements, consider using a higher‑resolution timing mechanism outside this worker.

- **Statistics object** – The returned `object` is intentionally opaque in this documentation; consumers should refer to the implementation or accompanying XML comments for the concrete type and its members. Casting to the expected type is safe when the worker is in a known state.
