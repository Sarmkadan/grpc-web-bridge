# IStreamingContracts Interface Documentation

## Overview

This document describes the streaming contracts defined in `src/GrpcWebBridge/Streaming/IStreamingContracts.cs`. These interfaces define the core abstractions for flow-controlled bidirectional gRPC streaming with backpressure support in the GrpcWebBridge application.

## Interfaces

### IFlowControlledStream

Contract for a flow-controlled bidirectional gRPC stream that enforces backpressure between the producer and consumer sides.

| Member | Type | Description |
|--------|------|-------------|
| StreamId | string | Gets the unique identifier of this stream. |
| MethodType | MethodType | Gets the gRPC method type associated with this stream. |
| State | StreamState | Gets the current lifecycle state of the stream. |
| Metrics | StreamThroughputMetrics | Gets a live snapshot of throughput and backpressure metrics. |
| BackpressureController | IBackpressureController | Gets the backpressure controller governing outbound flow for this stream. |
| WriteAsync | ValueTask(StreamMessage message, CancellationToken cancellationToken = default) | Writes a message to the outbound channel. Suspends asynchronously when the channel is at capacity, providing transparent backpressure to the caller. |
| ReadAllAsync | IAsyncEnumerable<StreamMessage>(CancellationToken cancellationToken = default) | Returns all inbound messages as an `IAsyncEnumerable{T}`. Each consumed message implicitly releases flow-control credits to the remote producer, ensuring the credit window stays open in proportion to consumer throughput. |
| CompleteWritingAsync | ValueTask() | Signals that the local side has finished writing. The stream transitions to `StreamState.HalfClosed` until the remote side also completes. |
| AbortAsync | ValueTask(GrpcStatusCode status, string? detail = null) | Aborts the stream immediately with the specified gRPC status, discarding any buffered messages and notifying all waiting readers and writers. |

**Implementing Class:** `FlowControlledStream` (in `FlowControlledStream.cs`)

---

### IBackpressureController

Manages the credit window for a single stream, issuing and consuming per-message credits to apply backpressure when the consumer cannot keep up with the producer.

| Member | Type | Description |
|--------|------|-------------|
| StreamId | string | Gets the identifier of the stream this controller is bound to. |
| AvailableCredits | int | Gets the number of credits currently available to the producer. |
| WindowUtilization | double | Gets the current window utilisation as a fraction between 0 and 1. |
| IsThrottled | bool | Gets whether backpressure is currently active on this stream. |
| TryConsumeCredit | bool(int count = 1) | Non-blocking credit acquisition. Returns `true` and deducts `count` credits when the window has sufficient capacity; returns `false` when the window is exhausted. |
| ConsumeCreditAsync | ValueTask(int count = 1, CancellationToken cancellationToken = default) | Asynchronously waits until `count` credits are available, then consumes them. Respects the `cancellationToken` throughout. |
| ReleaseCredit | void(int count = 1) | Returns `count` credits to the window, unblocking any producer that is suspended waiting for capacity. |
| ResetWindow | void() | Resets the credit window to its initial configured size. |

**Implementing Classes:**
- `BackpressureController` (in `BackpressureController.cs`)
- `AdaptiveFlowController` (in `AdaptiveFlowController.cs`)

---

### IBidirectionalStreamingEngine

Manages the full lifecycle of bidirectional gRPC streams, including creation, flow-control enforcement, backpressure signalling, and graceful teardown.

| Member | Type | Description |
|--------|------|-------------|
| ActiveStreamCount | int | Gets the total number of currently open bidirectional streams. |
| OpenStreamAsync | Task<IFlowControlledStream>(string streamId, MethodType methodType, CancellationToken cancellationToken = default) | Opens a new flow-controlled bidirectional stream and registers it with the engine. |
| GetStream | IFlowControlledStream?(string streamId) | Returns the registered stream for `streamId`, or `null` when no such stream is active. |
| CloseStreamAsync | Task(string streamId, GrpcStatusCode? finalStatus = null, CancellationToken cancellationToken = default) | Performs a graceful close: drains the outbound buffer, attaches a terminal status, and disposes all resources associated with the stream. |
| GetAllMetrics | IReadOnlyDictionary<string, StreamThroughputMetrics>() | Returns a snapshot of throughput metrics for every active stream, indexed by stream identifier. |

**Implementing Class:** `BidirectionalStreamingEngine` (in `BidirectionalStreamingEngine.cs`)