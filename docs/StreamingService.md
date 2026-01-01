# StreamingService

The `StreamingService` class represents a bidirectional gRPC stream managed by the `grpc-web-bridge` service. It encapsulates the state and lifecycle of an individual stream, including message queuing, heartbeat tracking, and stream termination. This class is used internally by the bridge to maintain active streams, handle message exchange, and monitor stream health.

## API

### `public StreamingService(string streamId, MethodType methodType)`
Constructs a new `StreamingService` instance.
- **Parameters**:
  - `streamId`: A unique identifier for the stream.
  - `methodType`: The gRPC method type (e.g., unary, server streaming, client streaming, or bidirectional) associated with the stream.
- **Throws**:
  - `ArgumentNullException`: If `streamId` or `methodType` is `null`.

---

### `public Stream CreateStream()`
Creates and returns a new `Stream` instance for this service. This method is typically called once during stream initialization.
- **Returns**: A new `Stream` instance tied to this service.
- **Throws**:
  - `InvalidOperationException`: If the stream has already been created.

---

### `public Stream? GetStream()`
Retrieves the active `Stream` instance associated with this service.
- **Returns**: The `Stream` instance if it exists; otherwise, `null`.

---

### `public void EnqueueMessage(StreamMessage message)`
Adds a message to the stream's incoming message queue.
- **Parameters**:
  - `message`: The `StreamMessage` to enqueue.
- **Throws**:
  - `ArgumentNullException`: If `message` is `null`.
  - `InvalidOperationException`: If the stream is closed or in an invalid state for message enqueuing.

---

### `public StreamMessage? DequeueMessage()`
Removes and returns the next message from the stream's incoming message queue.
- **Returns**: The next `StreamMessage` if available; otherwise, `null`.
- **Throws**:
  - `InvalidOperationException`: If the stream is closed or in an invalid state for message dequeuing.

---

### `public void CloseStream(GrpcStatusCode status, string? message)`
Closes the stream with the specified status and optional message.
- **Parameters**:
  - `status`: The gRPC status code indicating the reason for closure.
  - `message`: An optional descriptive message accompanying the status.
- **Throws**:
  - `InvalidOperationException`: If the stream is already closed.

---

### `public void SendHeartbeat()`
Updates the `LastActivityTime` to the current timestamp, indicating the stream is still active. This method is used to prevent idle stream cleanup.
- **Throws**:
  - `InvalidOperationException`: If the stream is closed.

---

### `public void CleanupIdleStreams(TimeSpan idleTimeout)`
Closes streams that have not had any activity (messages or heartbeats) within the specified timeout period.
- **Parameters**:
  - `idleTimeout`: The maximum duration of inactivity before a stream is considered idle and closed.
- **Throws**:
  - `ArgumentOutOfRangeException`: If `idleTimeout` is negative.

---

### `public IEnumerable<string> GetAllStreamIds()`
Returns a collection of all active stream IDs managed by the service. This method is typically used for diagnostic or monitoring purposes.
- **Returns**: An enumerable of stream IDs.

---

### `public StreamStatistics GetStreamStatistics()`
Generates and returns statistics for the stream, including message counts, timestamps, and state.
- **Returns**: A `StreamStatistics` object containing the stream's metrics.

---

### `public string StreamId { get; }`
Gets the unique identifier for the stream.
- **Returns**: The stream ID.

---

### `public MethodType MethodType { get; }`
Gets the gRPC method type associated with the stream.
- **Returns**: The `MethodType` enum value.

---

### `public StreamState State { get; }`
Gets the current state of the stream (e.g., active, closed, error).
- **Returns**: The `StreamState` enum value.

---

### `public int MessageCount { get; }`
Gets the number of messages currently enqueued in the stream.
- **Returns**: The message count.

---

### `public DateTime CreatedAt { get; }`
Gets the timestamp when the stream was created.
- **Returns**: The creation time.

---

### `public DateTime LastActivityTime { get; }`
Gets the timestamp of the last activity (message or heartbeat) on the stream.
- **Returns**: The last activity time.

---

### `public GrpcStatusCode? FinalStatus { get; }`
Gets the final gRPC status code if the stream was closed, otherwise `null`.
- **Returns**: The status code or `null`.

---

### `public string? FinalMessage { get; }`
Gets the final descriptive message if the stream was closed, otherwise `null`.
- **Returns**: The message or `null`.

---

### `public Stream Stream { get; }`
Gets the underlying `Stream` instance associated with this service.
- **Returns**: The `Stream` instance.

---

### `public void EnqueueMessage(byte[] data, bool isBinary)`
Adds a raw message to the stream's incoming queue.
- **Parameters**:
  - `data`: The raw message data.
  - `isBinary`: Indicates whether the data is binary (`true`) or text (`false`).
- **Throws**:
  - `ArgumentNullException`: If `data` is `null`.
  - `InvalidOperationException`: If the stream is closed or in an invalid state for message enqueuing.
