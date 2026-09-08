# StreamDiagnosticsService

The `StreamDiagnosticsService` is a hosted background service that periodically collects aggregate throughput metrics from the `IBidirectionalStreamingEngine`, emits a structured diagnostic summary, and publishes a `StreamingDiagnosticsEvent` for downstream consumers.

## Constructor

### `StreamDiagnosticsService(IBidirectionalStreamingEngine engine, StreamDiagnosticsOptions options, ILogger<StreamDiagnosticsService> logger, EventBus? eventBus = null)`
- **Purpose**: Initializes a new instance of the service.
- **Parameters**:
  - `engine` (IBidirectionalStreamingEngine) – The bidirectional streaming engine to observe.
  - `options` (StreamDiagnosticsOptions) – Diagnostic collection configuration. See [StreamDiagnosticsOptions](./StreamDiagnosticsOptions.md) for details.
  - `logger` (ILogger<StreamDiagnosticsService>) – Logger for structured diagnostic output.
  - `eventBus` (EventBus?) – Optional application event bus. When provided, a `StreamingDiagnosticsEvent` is published after each collection pass.
- **Return value**: None.
- **Throws**: `ArgumentNullException` if `engine`, `options`, or `logger` is `null`.

## Methods

### `ExecuteAsync(CancellationToken stoppingToken)`
- **Purpose**: The main background service loop that collects diagnostics at the configured interval.
- **Parameters**:
  - `stoppingToken` (CancellationToken) – Token to signal when the service should stop.
- **Return value**: A `Task` that completes when the service stops.
- **Details**:
  - Logs service start information including collection interval and backpressure warning threshold.
  - Uses a `PeriodicTimer` to trigger collection passes at the interval specified in `StreamDiagnosticsOptions.CollectionInterval`.
  - On each tick, calls `CollectAndReport()` to gather metrics and log/publish diagnostics.
  - Handles exceptions during collection by logging them as errors without stopping the service.
  - Logs when the service stops.

## Relation to Other Components

- **StreamDiagnosticsOptions**: The service uses this configuration object to determine the collection interval, stale stream threshold, and backpressure warning threshold. See [StreamDiagnosticsOptions](./StreamDiagnosticsOptions.md) for full details.
- **StreamingDiagnosticsEvent**: After each collection pass, if an `EventBus` is provided, the service publishes a `StreamingDiagnosticsEvent` containing aggregate metrics (active stream counts, message/byte totals, backpressure events, credit wait times, and anomaly counts). This event is defined in the same source file.
- **StreamDiagnosticsController**: While the controller provides an on-demand HTTP endpoint for current diagnostics ([see documentation](./StreamDiagnosticsController.md)), the background service provides periodic, automated diagnostics collection and event publishing. Both components rely on the same `IBidirectionalStreamingEngine` for metric data and report similar aggregate information.

## Internal Workings

The service maintains a concurrent dictionary to track per-stream message counts between collection passes, enabling detection of:
- **Zero activity streams**: Streams with no change in total message count since the last pass.
- **High backpressure streams**: Streams where the ratio of backpressure events to total messages exceeds `StreamDiagnosticsOptions.BackpressureWarnThreshold`.

These anomalies are logged at `Debug` (zero activity) and `Warning` (high backpressure) levels, respectively.