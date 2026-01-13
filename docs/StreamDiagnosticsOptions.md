# StreamDiagnosticsOptions

Represents the configuration and live telemetry snapshot for monitoring gRPC streaming connections within the bridge. This type controls how diagnostics are collected and exposes counters that reflect the current state of all active streams, including throughput, backpressure, and idle connection metrics.

## API

### `TimeSpan CollectionInterval`
Gets or sets the period at which the diagnostics service samples and aggregates stream metrics. Reducing this interval increases CPU overhead but provides finer-grained data.

### `TimeSpan StaleStreamThreshold`
Gets or sets the duration of inactivity after which a stream is considered stale. Streams with no message activity for longer than this threshold are counted in `ZeroActivityStreamCount`.

### `double BackpressureWarnThreshold`
Gets or sets the ratio (0.0 to 1.0) of stream capacity utilization that triggers a backpressure warning. When a stream’s buffered-outbound-to-window ratio exceeds this value, it increments `HighBackpressureStreamCount`.

### `required int ActiveStreamCount`
The number of streams currently open and capable of sending or receiving messages. Must be supplied when constructing the options snapshot.

### `required long TotalMessagesIn`
Cumulative count of inbound messages received across all streams since the bridge started. Must be supplied when constructing the options snapshot.

### `required long TotalMessagesOut`
Cumulative count of outbound messages successfully sent across all streams since the bridge started. Must be supplied when constructing the options snapshot.

### `required long TotalBytesIn`
Cumulative bytes received across all streams since the bridge started. Must be supplied when constructing the options snapshot.

### `required long TotalBytesOut`
Cumulative bytes sent across all streams since the bridge started. Must be supplied when constructing the options snapshot.

### `required long TotalBackpressureEvents`
Total number of times backpressure was applied (e.g., send queued or delayed) across all streams. Must be supplied when constructing the options snapshot.

### `required long TotalCreditWaitMs`
Aggregate time in milliseconds that streams spent waiting for flow-control credit before sending. Must be supplied when constructing the options snapshot.

### `required int ZeroActivityStreamCount`
Number of active streams that have had no message activity within `StaleStreamThreshold`. Must be supplied when constructing the options snapshot.

### `required int HighBackpressureStreamCount`
Number of active streams whose buffered-outbound-to-window ratio exceeds `BackpressureWarnThreshold`. Must be supplied when constructing the options snapshot.

### `StreamDiagnosticsService`
A reference to the diagnostics service instance that owns or consumes this options object. This property is set by the service at initialization and provides access to control methods such as forced snapshot collection or stream enumeration.

## Usage

### Example 1: Configuring diagnostics at startup

```csharp
var options = new StreamDiagnosticsOptions
{
    CollectionInterval = TimeSpan.FromSeconds(5),
    StaleStreamThreshold = TimeSpan.FromMinutes(2),
    BackpressureWarnThreshold = 0.75,
    ActiveStreamCount = 0,
    TotalMessagesIn = 0,
    TotalMessagesOut = 0,
    TotalBytesIn = 0,
    TotalBytesOut = 0,
    TotalBackpressureEvents = 0,
    TotalCreditWaitMs = 0,
    ZeroActivityStreamCount = 0,
    HighBackpressureStreamCount = 0
};

var service = new StreamDiagnosticsService(options);
await service.StartAsync();
```

### Example 2: Reading a live snapshot for health checks

```csharp
public bool IsSystemHealthy(StreamDiagnosticsService diagnosticsService)
{
    var snapshot = diagnosticsService.GetLatestSnapshot();

    double backpressureRatio = snapshot.ActiveStreamCount > 0
        ? (double)snapshot.HighBackpressureStreamCount / snapshot.ActiveStreamCount
        : 0;

    bool tooManyStale = snapshot.ZeroActivityStreamCount > snapshot.ActiveStreamCount / 2;
    bool backpressureSpiking = backpressureRatio > 0.5;

    return !tooManyStale && !backpressureSpiking;
}
```

## Notes

- All `required` numeric members must be explicitly initialized when creating an instance; the compiler enforces this. They represent a point-in-time snapshot and are expected to be overwritten atomically by the diagnostics service on each collection cycle.
- `CollectionInterval` and `StaleStreamThreshold` should not be changed while the diagnostics service is running unless the service explicitly supports dynamic reconfiguration. Doing so may cause inconsistent intermediate states in the next sampling window.
- `BackpressureWarnThreshold` values outside the range 0.0–1.0 are technically assignable but produce meaningless classifications; the service may clamp or ignore out-of-range values internally.
- This type is not inherently thread-safe. The owning `StreamDiagnosticsService` is responsible for synchronizing writes to the snapshot fields and ensuring that readers see a consistent set of values. External code reading properties directly should do so only through the service’s documented thread-safe accessors.
