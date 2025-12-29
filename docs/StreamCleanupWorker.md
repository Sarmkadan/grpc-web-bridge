# StreamCleanupWorker

A background worker that monitors and cleans up stale gRPC-Web streams based on configurable timeouts and thresholds. It tracks active streams, enforces idle and total stream lifetimes, and triggers garbage collection when thresholds are exceeded.

## API

### `public StreamCleanupWorker`

Initializes a new instance of the stream cleanup worker with default configuration values.

### `public object GetStatistics`

Retrieves a snapshot of current operational statistics for the cleanup worker.

- **Return Value**: An object containing metrics such as active stream count, cleanup counts, and timing information.
- **Exceptions**: May throw if internal state is corrupted or if statistics collection fails.

### `public int CleanupIntervalSeconds`

Gets or sets the interval, in seconds, at which the cleanup worker scans for stale streams.

- **Default**: 30 seconds.
- **Range**: Must be a positive integer.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if set to a non-positive value.

### `public TimeSpan IdleTimeoutDuration`

Gets or sets the duration after which an idle stream (no active messages) is considered stale and eligible for cleanup.

- **Default**: 5 minutes.
- **Range**: Must be a positive duration.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if set to a non-positive duration.

### `public TimeSpan StaleStreamDuration`

Gets or sets the total duration after which any stream, regardless of activity, is considered stale and eligible for cleanup.

- **Default**: 30 minutes.
- **Range**: Must be a positive duration.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if set to a non-positive duration.

### `public int GcTriggerThreshold`

Gets or sets the minimum number of streams that must be eligible for cleanup before triggering garbage collection.

- **Default**: 10 streams.
- **Range**: Must be a non-negative integer.
- **Exceptions**: Throws `ArgumentOutOfRangeException` if set to a negative value.

## Usage
