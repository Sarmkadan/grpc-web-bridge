# AdaptiveFlowController

A hosted background service that periodically examines active bidirectional streams and adaptively adds credit when a stream has low window utilization. It runs only when `FlowControlOptions.Mode` is `FlowControlMode.Adaptive`; in other modes it exits immediately.

On each adjustment pass, the controller gets the active stream IDs from `IBidirectionalStreamingEngine`, resolves each stream, and inspects its `IBackpressureController`. A non-throttled stream below 30% window utilization receives `FlowControlOptions.CreditReplenishmentBatch` credits. Streams above 75% utilization are left to their existing backpressure behavior, and streams between those thresholds are unchanged.

## API

### `public sealed class AdaptiveFlowController : BackgroundService`

Defines the adaptive flow-control hosted service. The service lifecycle is supplied by `BackgroundService`; its periodic work is implemented by the protected `ExecuteAsync` override rather than by an additional public method.

### `public AdaptiveFlowController(IBidirectionalStreamingEngine engine, FlowControlOptions options, ILogger<AdaptiveFlowController> logger)`

Constructs the controller with the streaming engine to inspect, the shared flow-control configuration, and a logger. Each argument is required; passing `null` for any argument throws `ArgumentNullException`.

## Relationship to BackpressureController and FlowControlOptions

`AdaptiveFlowController` coordinates the per-stream controllers exposed through `IFlowControlledStream.BackpressureController`. It reads each controller's `WindowUtilization` and `IsThrottled` state, and calls `ReleaseCredit` only for low-utilization streams that are not already throttled. It does not replace `BackpressureController`: the latter still owns the stream's credit window, credit consumption, throttling state, and natural recovery behavior.

`FlowControlOptions` determines whether the hosted service is active and how often and by how much it adjusts streams:

- `Mode` must be `FlowControlMode.Adaptive`; otherwise the service exits.
- `AdaptiveAdjustmentInterval` sets the interval between adjustment passes.
- `CreditReplenishmentBatch` is the number of credits released when a stream is widened.

The controller's 30% low-utilization and 75% high-utilization boundaries are internal constants. `FlowControlOptions.BackpressureThreshold`, `InitialWindowSize`, and `MaxWindowSize` configure the underlying per-stream flow control, but are not read directly by `AdaptiveFlowController`.

## Usage

### Register adaptive flow control

The tests construct `FlowControlOptions` with adaptive mode and a replenishment batch, then supply those options to the controller. In an application, the equivalent configuration is normally registered through the streaming service extension, which also registers `AdaptiveFlowController` as a hosted service:

```csharp
using GrpcWebBridge.Streaming;

services.AddBidirectionalStreaming(new FlowControlOptions
{
    Mode = FlowControlMode.Adaptive,
    InitialWindowSize = 64,
    MaxWindowSize = 256,
    CreditReplenishmentBatch = 16,
    AdaptiveAdjustmentInterval = TimeSpan.FromSeconds(1),
    BackpressureThreshold = 0.85
});
```

With this configuration, each adjustment pass releases 16 credits to a stream whose utilization is below 30% and whose backpressure controller is not throttled. Medium-utilization, high-utilization, throttled, missing, and inactive streams are left unchanged.
