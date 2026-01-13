# FlowControlOptions

Configuration object that controls flow-control behavior for gRPC-Web bridge channels. It defines limits and policies for managing inbound and outbound data streams, backpressure thresholds, and adaptive flow-control adjustments to prevent resource exhaustion and ensure stable throughput.

## API

### `int InitialWindowSize`
Gets or sets the initial flow-control window size in bytes for new channels. This value determines how much data a sender is allowed to transmit before waiting for acknowledgments. Must be a positive integer.

- **Type**: `int`
- **Range**: > 0
- **Throws**: `ArgumentOutOfRangeException` if set to a value ≤ 0.

### `int MaxWindowSize`
Gets or sets the maximum flow-control window size in bytes. This caps the total outstanding data a sender can have in flight. Must be greater than or equal to `InitialWindowSize`.

- **Type**: `int`
- **Range**: ≥ `InitialWindowSize`
- **Throws**: `ArgumentOutOfRangeException` if set to a value < `InitialWindowSize`.

### `int InboundChannelCapacity`
Gets or sets the maximum number of inbound messages that can be buffered in memory before backpressure is applied. Controls memory usage on the receiving side.

- **Type**: `int`
- **Range**: ≥ 0
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

### `int OutboundChannelCapacity`
Gets or sets the maximum number of outbound messages that can be queued for sending. Helps prevent unbounded memory growth when the network or remote peer cannot keep up.

- **Type**: `int`
- **Range**: ≥ 0
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

### `double BackpressureThreshold`
Gets or sets the fraction (0.0 to 1.0) of `InboundChannelCapacity` at which backpressure events are emitted or throttling begins. For example, 0.8 means backpressure starts when 80% of capacity is reached.

- **Type**: `double`
- **Range**: 0.0 ≤ value ≤ 1.0
- **Throws**: `ArgumentOutOfRangeException` if set outside [0.0, 1.0].

### `int CreditReplenishmentBatch`
Gets or sets the number of flow-control credits to replenish per acknowledgment when using credit-based flow control. Larger values reduce acknowledgment overhead but may increase burstiness.

- **Type**: `int`
- **Range**: > 0
- **Throws**: `ArgumentOutOfRangeException` if set to a value ≤ 0.

### `FlowControlMode Mode`
Gets or sets the flow-control strategy to use. Determines how credits and window sizes are managed.

- **Type**: `FlowControlMode`
- **Throws**: `ArgumentOutOfRangeException` if set to an undefined value.

### `TimeSpan? MaxProducerWaitTime`
Gets or sets the maximum duration a producer may wait for flow-control credits before timing out and failing the operation. `null` indicates no timeout.

- **Type**: `TimeSpan?`
- **Range**: ≥ `TimeSpan.Zero` or `null`
- **Throws**: `ArgumentOutOfRangeException` if set to a negative value.

### `TimeSpan AdaptiveAdjustmentInterval`
Gets or sets how often the system reevaluates and potentially adjusts flow-control parameters (e.g., window size) based on observed traffic patterns.

- **Type**: `TimeSpan`
- **Range**: > `TimeSpan.Zero`
- **Throws**: `ArgumentOutOfRangeException` if set to `TimeSpan.Zero` or negative.

### `bool EmitBackpressureEvents`
Gets or sets whether to raise events when backpressure conditions are detected (e.g., buffer thresholds exceeded). Useful for monitoring and logging.

- **Type**: `bool`
- **Default**: `false`

### `void Validate()`
Validates all current property values for logical consistency and range constraints. Throws if any configuration is invalid.

- **Returns**: `void`
- **Throws**:
  - `InvalidOperationException` if `MaxWindowSize` < `InitialWindowSize`.
  - `InvalidOperationException` if `AdaptiveAdjustmentInterval` ≤ `TimeSpan.Zero`.
  - `InvalidOperationException` if `BackpressureThreshold` is outside [0.0, 1.0].
  - `InvalidOperationException` if any integer property is negative (where applicable).
  - `InvalidOperationException` if `MaxProducerWaitTime` is negative (if not `null`).

## Usage

### Example 1: Configuring Conservative Flow Control
