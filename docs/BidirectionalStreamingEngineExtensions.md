# BidirectionalStreamingEngineExtensions

Provides utility methods for inspecting and monitoring bidirectional gRPC-Web streaming channels managed by the `BidirectionalStreamingEngine`. These extensions expose metrics and stream enumeration capabilities for diagnostics, performance tuning, and observability.

## API

### `GetStreamMetrics`

Retrieves throughput metrics for a specific bidirectional stream identified by its method type.

- **Parameters**
  - `methodType` (`MethodType`): The gRPC method type identifying the stream.
- **Returns**
  - `StreamThroughputMetrics?`: A snapshot of throughput metrics (e.g., messages per second, bytes per second) for the stream, or `null` if the stream is not found or has no metrics.
- **Exceptions**
  - Throws `ArgumentNullException` if `methodType` is `null`.

### `GetStreamsByMethodType`

Enumerates all active bidirectional streams filtered by their method type.

- **Parameters**
  - `methodType` (`MethodType`): The gRPC method type to filter streams by.
- **Returns**
  - `IEnumerable<IFlowControlledStream>`: A collection of streams matching the method type. The collection is read-only and may be empty if no streams are active.
- **Exceptions**
  - Throws `ArgumentNullException` if `methodType` is `null`.

### `GetTotalMessageCount`

Returns the cumulative count of messages processed across all bidirectional streams managed by the engine.

- **Returns**
  - `long`: The total number of messages sent or received. This value is monotonically increasing and may wrap around on overflow.
- **Exceptions**
  - None.

### `GetTotalBytesTransferred`

Returns the cumulative count of bytes transferred across all bidirectional streams managed by the engine.

- **Returns**
  - `long`: The total number of bytes sent or received. This value is monotonically increasing and may wrap around on overflow.
- **Exceptions**
  - None.

## Usage

### Monitoring Active Streams
