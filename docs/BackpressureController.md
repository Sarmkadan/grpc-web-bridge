# BackpressureController

A lightweight controller that tracks and enforces backpressure for a specific bidirectional gRPC-Web stream identified by `StreamId`. It manages a credit-based flow control window, allowing consumers to signal their capacity to process incoming messages and the producer to respect those limits.

## API

### `public string StreamId`

Gets the unique identifier for the stream this controller manages. This value is set at construction and never changes.

### `public BackpressureController`

Constructs a new controller for a stream. The initial credit window is zero; consumers must call `ReleaseCredit` or `ResetWindow` to allocate capacity before processing begins.

### `public bool TryConsumeCredit(int count)`

Attempts to atomically consume `count` credits from the current window. Returns `true` if the credits were available and consumed; otherwise returns `false` without blocking. Throws `ArgumentOutOfRangeException` if `count` is negative.

### `public async ValueTask ConsumeCreditAsync(int count, CancellationToken cancellationToken = default)`

Asynchronously waits until at least `count` credits are available, then atomically consumes them. The operation can be canceled via `cancellationToken`. Throws `OperationCanceledException` if the token is triggered before credits become available. Throws `ArgumentOutOfRangeException` if `count` is negative.

### `public void ReleaseCredit(int count)`

Adds `count` credits back to the window, increasing the available capacity for future consumption. Throws `ArgumentOutOfRangeException` if `count` is negative.

### `public void ResetWindow()`

Resets the credit window to zero, effectively halting further message delivery until new credits are released. This is typically used when the stream is being reset or reconfigured.

### `public void Dispose()`

Releases all resources held by the controller. After disposal, any further calls to `TryConsumeCredit`, `ConsumeCreditAsync`, `ReleaseCredit`, or `ResetWindow` will throw `ObjectDisposedException`. This method is idempotent.

## Usage

### Basic credit-based flow control
