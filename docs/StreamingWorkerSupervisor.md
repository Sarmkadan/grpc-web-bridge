# StreamingWorkerSupervisor

The `StreamingWorkerSupervisor` type encapsulates a background worker that monitors the health of a `StreamingService` and implements self-healing by restarting the service when it becomes unhealthy or unresponsive to heartbeats. It tracks consecutive failures, total restarts, and timestamps of the last healthy state and heartbeat.

## API

### StreamingWorkerSupervisor (class)

Represents a background service that performs periodic health checks on a streaming service and initiates self-healing actions when unhealthy conditions are detected. Instances are created via dependency injection and managed by the host.

### ConsecutiveFailureCount

```csharp
public int ConsecutiveFailureCount { get; }
```

**Purpose**  
Gets the number of consecutive health check failures since the last successful health check or restart. This value resets to zero when the streaming service is determined to be healthy.

**Exceptions**  
None. The property is read-only and always returns an integer value.

### TotalRestarts

```csharp
public int TotalRestarts { get; }
```

**Purpose**  
Gets the total number of times the streaming service has been restarted by this supervisor since it started running.

**Exceptions**  
None. The property is read-only and always returns an integer value.

### IsRunning

```csharp
public bool IsRunning { get; }
```

**Purpose**  
Gets a value indicating whether the supervisor is currently running (i.e., the background service is active and performing health checks). Returns `false` when the supervisor has been stopped or has not yet started.

**Exceptions**  
None. The property is read-only and always returns a Boolean value.

### LastHealthyTime

```csharp
public DateTime? LastHealthyTime { get; }
```

**Purpose**  
Gets the UTC date and time when the streaming service was last observed to be healthy. Returns `null` if the service has never been healthy since the supervisor started.

**Exceptions**  
None. The property is read-only and always returns a valid `DateTime` or `null`.

### LastHeartbeatTime

```csharp
public DateTime? LastHeartbeatTime { get; }
```

**Purpose**  
Gets the UTC date and time of the last heartbeat received from the streaming service. Returns `null` if no heartbeat has been received since the supervisor started.

**Exceptions**  
None. The property is read-only and always returns a valid `DateTime` or `null`.

### GetStatistics

```csharp
public object GetStatistics()
```

**Purpose**  
Returns a snapshot of diagnostic information collected by the supervisor, including running state, failure counts, restart counts, timestamps, and stream counts.

**Parameters**  
None.

**Return value**  
An `object` containing the following properties:
- `isRunning` (bool): Whether the supervisor is currently running.
- `consecutiveFailureCount` (int): Current count of consecutive failures.
- `totalRestarts` (int): Total number of service restarts performed.
- `maxConsecutiveFailures` (int): Configured threshold for triggering a restart.
- `monitoringInterval` (int): Seconds between health checks.
- `heartbeatTimeout` (int): Seconds without a heartbeat before considering the service unresponsive.
- `lastHealthyTime` (string): ISO 8601 formatted timestamp of last healthy state, or `null`.
- `lastHeartbeatTime` (string): ISO 8601 formatted timestamp of last heartbeat, or `null`.
- `activeStreamCount` (int): Current number of active streams in the streaming service.
- `maxStreamCapacity` (int): Maximum allowed streams before considering the service at capacity.

**Exceptions**  
None. The method always returns an object with the diagnostic properties.

## Usage

### Example 1: Retrieving supervisor state and statistics

```csharp
using GrpcWebBridge.BackgroundWorkers;

// Assume supervisor is obtained via dependency injection or host services
var supervisor = ...; // StreamingWorkerSupervisor instance

Console.WriteLine($"Supervisor running: {supervisor.IsRunning}");
Console.WriteLine($"Consecutive failures: {supervisor.ConsecutiveFailureCount}");
Console.WriteLine($"Total restarts: {supervisor.TotalRestarts}");
Console.WriteLine($"Last healthy: {supervisor.LastHealthyTime:O}");
Console.WriteLine($"Last heartbeat: {supervisor.LastHeartbeatTime:O}");

// Obtain detailed statistics for logging or monitoring
var stats = supervisor.GetStatistics();
Console.WriteLine($"Statistics: {stats}");
```

### Example 2: Monitoring health status changes via events

The supervisor emits events through the event bus when health status changes or when a restart occurs. Handlers can subscribe to these events for logging or alerting.

```csharp
// Subscribe to health change events
eventBus.Subscribe<StreamingWorkerHealthChangedEvent>(e =>
{
    Console.WriteLine($"Worker health changed: Healthy={e.IsHealthy}, Failures={e.ConsecutiveFailures}");
});

// Subscribe to restart events
eventBus.Subscribe<StreamingWorkerRestartedEvent>(e =>
{
    Console.WriteLine($"Worker restarted: Failures before restart={e.ConsecutiveFailuresBeforeRestart}, Total restarts={e.TotalRestarts}");
});
```

## Notes

- **Thread safety** – The properties (`ConsecutiveFailureCount`, `TotalRestarts`, `IsRunning`, `LastHealthyTime`, `LastHeartbeatTime`) are updated by the background thread and are safe for concurrent reads because they are atomic for their respective types. However, reading multiple properties simultaneously (e.g., via `GetStatistics`) may return an inconsistent snapshot if the state changes during the read. For monitoring purposes, this inconsistency is acceptable.

- **GetStatistics availability** – The method can be called at any time after the supervisor instance is created, regardless of whether the background service has started. It returns the current state of the supervisor's fields.

- **Event-based monitoring** – For real-time notifications of health changes or restarts, prefer subscribing to the `StreamingWorkerHealthChangedEvent` and `StreamingWorkerRestartedEvent` events via the event bus rather than polling properties.

- **Configuration** – The supervisor's behavior is configured via `StreamingWorkerSupervisorOptions` provided during construction. These options (monitoring intervals, failure thresholds, timeouts) are not mutable after the supervisor is created but are reflected in the statistics returned by `GetStatistics`.

- **Self-healing logic** – The supervisor restarts the streaming service when either consecutive failures reach the configured threshold or a heartbeat timeout occurs. The restart is simulated in the current implementation (logging and counter reset) but designed to be replaced with actual service disposal and recreation in a production environment.