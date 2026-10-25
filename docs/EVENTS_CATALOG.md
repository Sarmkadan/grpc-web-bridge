# Events Catalog

Every `EventBase` subclass in `src/GrpcWebBridge`, published through the in-process `EventBus` (`Events/EventBus.cs`). Subscribe with `EventBus.Subscribe<TEvent>(...)`; see [EventBus.md](EventBus.md).

## Base type: `EventBase`

Defined in `Events/EventBus.cs`. Every event carries these members:

| Property | Type | Notes |
|----------|------|-------|
| `EventId` | `string` | `Guid` string, assigned at construction. |
| `CreatedAt` | `DateTime` | UTC, assigned at construction. |
| `Source` | `string?` | Set by the publisher (usually `nameof(<PublisherClass>)`). |
| `Metadata` | `Dictionary<string, object>?` | Optional free-form data. |
| `CorrelationId` | `string?` | Filled by `EventBus.PublishAsync` from the active request when left null. |
| `UserId` | `string?` | Filled by `EventBus.PublishAsync` from the active request when left null. |

## Summary

| Event | Publisher | Fires when | Status |
|-------|-----------|------------|--------|
| `ServiceRegisteredEvent` | none in `src` | A service is registered. | Defined, not published yet. |
| `ServiceUnregisteredEvent` | none in `src` | A service is unregistered. | Defined, not published yet. |
| `MethodInvokedEvent` | none in `src` | A gRPC method is invoked. | Defined, not published yet. |
| `AuthenticationFailedEvent` | none in `src` | Authentication fails. | Defined, not published yet. |
| `StreamStartedEvent` | `BidirectionalStreamingEngine` | A bidirectional stream is opened. | Active. |
| `StreamEndedEvent` | `BidirectionalStreamingEngine` | A bidirectional stream closes (completed, cancelled or faulted). | Active. |
| `BackpressureChangedEvent` | `BackpressureController` | A stream starts or stops being throttled. | Active. |
| `StreamingDiagnosticsEvent` | `StreamDiagnosticsService` | After each diagnostic collection pass. | Active. |
| `ServiceHealthChangedEvent` | `HealthCheckWorker` | A registered service transitions from serving to not serving. | Active (degradation only). |
| `StreamingWorkerHealthChangedEvent` | `StreamingWorkerSupervisor` | Streaming service health check fails, or recovers after failures. | Active. |
| `StreamingWorkerRestartedEvent` | `StreamingWorkerSupervisor` | The supervisor restarts the streaming service. | Active. |

## Event details

### `ServiceRegisteredEvent`

- **Source:** `Events/EventBus.cs`
- **Publisher:** none in `src`. Only tests publish it.
- **Properties:** `ServiceId` (`string`), `ServiceName` (`string`), `Endpoint` (`string`).
- **Fires when:** a service is registered. Intended for `ServiceRegistry`. Not wired up yet.

### `ServiceUnregisteredEvent`

- **Source:** `Events/EventBus.cs`
- **Publisher:** none in `src`.
- **Properties:** `ServiceId` (`string`), `ServiceName` (`string`).
- **Fires when:** a service is unregistered. Not wired up yet.

### `MethodInvokedEvent`

- **Source:** `Events/EventBus.cs`
- **Publisher:** none in `src`.
- **Properties:** `ServiceId` (`string`), `MethodName` (`string`), `DurationMs` (`long`), `Success` (`bool`).
- **Fires when:** a gRPC method is invoked. Not wired up yet.

### `AuthenticationFailedEvent`

- **Source:** `Events/EventBus.cs`
- **Publisher:** none in `src`.
- **Properties:** `FailureReason` (`string`), `ClientIp` (`string?`).
- **Fires when:** authentication fails. Not wired up yet.

### `StreamStartedEvent`

- **Source:** `Events/EventBus.cs`
- **Publisher:** `BidirectionalStreamingEngine` (`Streaming/BidirectionalStreamingEngine.cs`), after the stream is registered.
- **Properties:** `StreamId` (`string`), `ServiceId` (`string`), `MethodName` (`string`). `MethodName` holds the `methodType` enum value as a string. `ServiceId` is declared but not set by the publisher, so it is empty.
- **Fires when:** a bidirectional stream is opened and added to the engine's stream table.

### `StreamEndedEvent`

- **Source:** `Events/EventBus.cs`
- **Publisher:** `BidirectionalStreamingEngine` (`Streaming/BidirectionalStreamingEngine.cs`), in the `finally` block of the close path.
- **Properties:**
  - `StreamId` (`string`)
  - `MessageCount` (`long`): inbound plus outbound messages.
  - `DurationMs` (`long`)
  - `CompletionStatus` (`StreamCompletionStatus`): `Completed`, `Cancelled` or `Faulted`.
  - `ErrorCode` (`string?`): `INTERNAL_ERROR` when faulted.
  - `CloseReason` (`string?`)
- **Fires when:** a stream closes. It fires once per stream, including on error, because it is in a `finally` block.

### `BackpressureChangedEvent`

- **Source:** `Streaming/BidirectionalStreamContext.cs`
- **Publisher:** `BackpressureController` (`Streaming/BackpressureController.cs`).
- **Properties:**
  - `StreamId` (`string`)
  - `IsThrottled` (`bool`): `true` when throttling starts, `false` when it is lifted.
  - `WindowUtilization` (`double`, 0 to 1)
  - `AvailableCredits` (`int`)
- **Fires when:** a stream's backpressure state changes. Published only when `FlowControlOptions.EmitBackpressureEvents` is `true` (the default) and an `EventBus` is supplied. The controller uses a compare-and-swap, so each throttle episode produces one start event and one lift event.

### `StreamingDiagnosticsEvent`

- **Source:** `Streaming/StreamDiagnosticsService.cs`
- **Publisher:** `StreamDiagnosticsService` (`BackgroundService`).
- **Properties:**
  - `ActiveStreamCount` (`int`)
  - `TotalMessagesIn` / `TotalMessagesOut` (`long`)
  - `TotalBytesIn` / `TotalBytesOut` (`long`)
  - `TotalBackpressureEvents` (`long`)
  - `TotalCreditWaitMs` (`long`)
  - `ZeroActivityStreamCount` (`int`): streams with no new messages since the previous pass.
  - `HighBackpressureStreamCount` (`int`): streams above `BackpressureWarnThreshold`.
- **Fires when:** after each collection pass. The pass runs every `StreamDiagnosticsOptions.CollectionInterval`, which defaults to 60 seconds. Published only when an `EventBus` is supplied. The publish is fire-and-forget.

### `ServiceHealthChangedEvent`

- **Source:** `BackgroundWorkers/HealthCheckWorker.cs`
- **Publisher:** `HealthCheckWorker`, method `PublishServiceHealthEventAsync`.
- **Properties:** `ServiceId` (`string`), `ServiceName` (`string`), `IsHealthy` (`bool`), `Timestamp` (`DateTime`, UTC).
- **Fires when:** a service's status changes from `Serving` to `NotServing` during a health check. The worker publishes only this transition, so `IsHealthy` is always `false`. Recovery to `Serving` is logged but does not publish an event.

### `StreamingWorkerHealthChangedEvent`

- **Source:** `BackgroundWorkers/StreamingWorkerSupervisor.cs`
- **Publisher:** `StreamingWorkerSupervisor`, method `PublishWorkerHealthEventAsync`.
- **Properties:**
  - `IsHealthy` (`bool`)
  - `ConsecutiveFailures` (`int`)
  - `TotalRestarts` (`int`)
  - `ActiveStreamCount` (`int`)
  - `MaxStreamCapacity` (`int`)
  - `Timestamp` (`DateTime`, UTC)
- **Fires when:** a monitoring pass finds the streaming service unhealthy (`IsHealthy = false`), or finds it healthy again after one or more failures (`IsHealthy = true`). A healthy pass with no prior failures publishes nothing.

### `StreamingWorkerRestartedEvent`

- **Source:** `BackgroundWorkers/StreamingWorkerSupervisor.cs`
- **Publisher:** `StreamingWorkerSupervisor`, method `PublishWorkerRestartedEventAsync`.
- **Properties:**
  - `ConsecutiveFailuresBeforeRestart` (`int`)
  - `TotalRestarts` (`int`)
  - `ActiveStreamCount` (`int`)
  - `Timestamp` (`DateTime`, UTC)
- **Fires when:** the consecutive failure count reaches `MaxConsecutiveFailures`, or a heartbeat timeout is detected. The supervisor then calls `RestartStreamingServiceAsync`.
  - **Known issue:** `RestartStreamingServiceAsync` sets the failure counter to `0` before it publishes this event. As a result, `ConsecutiveFailuresBeforeRestart` is always `0` in practice. Fixing this needs a code change, so it is not changed here.
  - The restart itself is simulated: it only logs and resets counters.

## Adding a new event

1. Create a `sealed` class that derives from `EventBase` in `Events/EventBus.cs` or next to its publisher.
2. Publish it with `EventBus.PublishAsync`, and set `Source` to the publisher's name.
3. Add a row to the summary table and a section here.
