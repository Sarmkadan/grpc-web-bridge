# BidirectionalStreamContext

A context object that manages the lifecycle and flow control of a bidirectional gRPC-Web stream, coordinating message exchange between client and server through inbound and outbound channels.

## API

### `public required string StreamId`
Identifies the stream instance. Used for logging, tracing, and correlating stream-specific events.

### `public required MethodType MethodType`
Indicates whether the stream is a unary, client-streaming, server-streaming, or bidirectional stream.

### `public DateTime CreatedAt`
Timestamp when the stream context was initialized. Used to measure stream duration and diagnose timeouts.

### `public StreamState State`
Current state of the stream (e.g., `Initial`, `Open`, `Closed`, `Failed`). Determines allowed operations and cleanup behavior.

### `public required Channel<StreamMessage> InboundChannel`
Channel receiving messages from the server. Messages are enqueued here by the transport layer and consumed by the stream processor.

### `public required Channel<StreamMessage> OutboundChannel`
Channel sending messages to the server. Messages are enqueued here by the application and consumed by the transport layer.

### `public CancellationTokenSource LifetimeCts`
Cancellation source tied to the stream’s lifetime. Used to signal stream-wide cancellation (e.g., on failure or explicit close).

### `public StreamThroughputMetrics Metrics`
Aggregates throughput statistics (bytes sent/received, message rates) for monitoring and diagnostics.

### `public GrpcStatusCode? FinalStatus`
Final status code of the stream upon closure. `null` if the stream is still active. Populated when `State` transitions to `Closed` or `Failed`.

### `public string? CloseReason`
Human-readable reason for stream closure. Populated when `State` is `Closed` or `Failed`, typically indicating the cause of termination.

### `public async ValueTask DisposeAsync()`
Releases all resources associated with the stream, including channels and metrics. Awaiting this task ensures pending messages are processed and channels are drained. Idempotent; safe to call multiple times.

### `public int MaxSize`
Maximum allowed message size in bytes. Enforced when enqueuing messages to prevent protocol violations.

### `public FlowControlWindow`
Current flow-control window size in bytes. Determines how many bytes the remote peer is allowed to send before backpressure is applied.

### `public bool TryConsume(int bytes)`
Attempts to consume `bytes` from the flow-control window. Returns `true` if successful; otherwise `false` if the window is exhausted. Used by the transport to regulate message ingestion.

### `public int Release(int bytes)`
Releases `bytes` back to the flow-control window, increasing available credits. Returns the updated available credits. Used after a message is fully processed to allow further sends.

### `public void Reset()`
Resets the stream to its initial state, clearing channels and metrics. Used when reusing stream contexts across multiple logical streams.

### `public required string StreamId`
Duplicate declaration of `StreamId`; see above.

### `public required bool IsThrottled`
Indicates whether the stream is currently under backpressure due to a depleted flow-control window.

### `public required double WindowUtilization`
Ratio of used flow-control window to total capacity (0.0 to 1.0). Used to monitor congestion and adjust sending behavior.

### `public required int AvailableCredits`
Number of bytes currently available in the flow-control window. Used to determine whether new messages can be enqueued.

## Usage

### Example 1: Creating and closing a bidirectional stream
