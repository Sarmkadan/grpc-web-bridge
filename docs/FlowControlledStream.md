# FlowControlledStream

Concrete implementation of `IFlowControlledStream` that wraps a `BidirectionalStreamContext` channel pair with a `BackpressureController` credit window. Reads from the inbound channel are credit-aware: every `FlowControlOptions.CreditReplenishmentBatch` messages yielded to the caller returns a credit batch to the producer, proportionally keeping the window open relative to actual consumer throughput. Writes consume one credit per message and suspend asynchronously when the window is exhausted. Instances are created and owned exclusively by `BidirectionalStreamingEngine`.

## API

### `public string StreamId`
Gets the unique identifier of this stream. Used for logging, tracing, and correlating stream-specific events.

### `public MethodType MethodType`
Gets the gRPC method type associated with this stream. Indicates whether the stream is a unary, client-streaming, server-streaming, or bidirectional stream.

### `public StreamState State`
Gets the current lifecycle state of the stream (e.g., `Initial`, `Open`, `HalfClosed`, `Closed`, `Failed`). Determines allowed operations and cleanup behavior.

### `public StreamThroughputMetrics Metrics`
Gets a live snapshot of throughput and backpressure metrics for this stream.

### `public IBackpressureController BackpressureController`
Gets the backpressure controller governing outbound flow for this stream. Manages the credit window for issuing and consuming per-message credits to apply backpressure when the consumer cannot keep up with the producer.

### `public DateTime CreatedAt`
UTC timestamp at which this stream was opened. Used to measure stream duration and diagnose timeouts.

### `public async ValueTask WriteAsync(StreamMessage message, CancellationToken cancellationToken = default)`
Writes a message to the outbound channel. Suspends asynchronously when the channel is at capacity, providing transparent backpressure to the caller. Consumes one credit from the backpressure controller before writing. Records credit wait time and backpressure events in metrics if waiting occurred.

### `public async IAsyncEnumerable<StreamMessage> ReadAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)`
Returns all inbound messages as an `IAsyncEnumerable{T}`. Each consumed message implicitly releases flow-control credits to the remote producer in batches of `FlowControlOptions.CreditReplenishmentBatch`, ensuring the credit window stays open in proportion to consumer throughput. Flushes any remaining credits from the final partial batch upon completion.

### `public ValueTask CompleteWritingAsync()`
Signals that the local side has finished writing. Completes the outbound channel writer and transitions the stream state to `HalfClosed` if currently `Active`. Logs the transition at information level.

### `public async ValueTask AbortAsync(GrpcStatusCode status, string? detail = null)`
Aborts the stream immediately with the specified gRPC status, discarding any buffered messages and notifying all waiting readers and writers. Completes both channel writers with an `OperationCanceledException`, sets stream state to `Failed`, cancels the lifetime cancellation token source, and logs the abort at warning level.

### `public async ValueTask DisposeAsync()`
Releases all resources associated with the stream, including the backpressure controller (if disposable) and the underlying `BidirectionalStreamContext`. Awaiting this task ensures pending messages are processed and channels are drained. Idempotent; safe to call multiple times.