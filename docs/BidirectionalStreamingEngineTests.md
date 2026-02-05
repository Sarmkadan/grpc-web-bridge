# BidirectionalStreamingEngineTests

The `BidirectionalStreamingEngineTests` class serves as the comprehensive test suite for validating the flow control, backpressure management, and lifecycle events of the bidirectional streaming engine within the `grpc-web-bridge` project. It rigorously verifies the engine's behavior under various load conditions, including credit window saturation, adaptive window resizing, timeout scenarios, and proper resource disposal, ensuring that the streaming infrastructure maintains stability and responsiveness during high-throughput operations.

## API

### CreditWindow_Saturation_ProducerBlocksUntilCreditReleased
Verifies that when the credit window is fully saturated, the producer logic blocks execution until additional credit is explicitly released by the consumer. This ensures that data production does not outpace consumption capacity.
*   **Parameters**: None (Test context inferred).
*   **Return Value**: `Task` (Completes when the blocking behavior is verified).
*   **Throws**: Fails the test if the producer proceeds without available credit or if the wait timeout is exceeded unexpectedly.

### CreditWindow_IsThrottled_TrueWhenWindowExhausted
Asserts that the `IsThrottled` state of the engine transitions to `true` immediately upon the exhaustion of the available credit window.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if the throttled state is not detected when the window reaches zero.

### CreditWindow_IsThrottled_FalseAfterCreditRelease
Confirms that the `IsThrottled` state reverts to `false` once sufficient credit is released back to the window, allowing production to resume.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if the engine remains in a throttled state despite available credit.

### ProducerTimeout_WhenWindowExhausted_ThrowsOperationCanceledException
Validates that if the credit window remains exhausted beyond the configured timeout period, the producer operation is cancelled and throws an `OperationCanceledException`.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Expects `OperationCanceledException`; fails if no exception or a different exception type is thrown.

### BackpressureChangedEvent_EmittedWhenThrottled
Ensures that the `BackpressureChanged` event is raised with the appropriate arguments when the engine enters a throttled state due to credit exhaustion.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if the event is not emitted or contains incorrect state data.

### BackpressureChangedEvent_EmittedWhenThrottleLifted
Ensures that the `BackpressureChanged` event is raised when the engine exits the throttled state following a credit release.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if the event is not emitted upon throttle relief.

### BackpressureChangedEvent_NotEmitted_WhenEmitDisabled
Verifies that the `BackpressureChanged` event is suppressed and not raised when the event emission feature is explicitly disabled in the engine configuration, even if throttling state changes occur.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if an event is emitted while the feature is disabled.

### Adaptive_LowUtilization_ControllerWidensWindow
Tests the adaptive flow control mechanism, confirming that the controller automatically increases (widens) the credit window size when it detects sustained low utilization of the current window.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if the window size does not increase under low utilization conditions.

### Adaptive_NonAdaptiveMode_ControllerExitsImmediately
Validates that when the engine is configured in non-adaptive mode, the adaptive controller logic terminates or bypasses adjustment calculations immediately without attempting to modify the window size.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if adaptive logic executes or modifies state in non-adaptive mode.

### CloseStreamAsync_RemovesStreamAndPublishesEndedEvent
Confirms that calling `CloseStreamAsync` successfully removes the stream from the active registry and publishes a stream-ended event to subscribers.
*   **Parameters**: None (Stream ID managed within test setup).
*   **Return Value**: `Task`.
*   **Throws**: Fails if the stream persists in the active collection or the event is not published.

### CloseStreamAsync_IdempotentForUnknownStreamId
Ensures that invoking `CloseStreamAsync` with an unknown or already closed stream ID does not throw an exception, verifying idempotent behavior for cleanup operations.
*   **Parameters**: None (Invalid/Unknown Stream ID managed within test setup).
*   **Return Value**: `Task`.
*   **Throws**: Fails if any exception is thrown during the operation.

### DisposeAsync_ClosesAllActiveStreams
Verifies that calling `DisposeAsync` on the engine forcibly closes all currently active streams and releases associated resources.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if any streams remain active or resources are not released after disposal.

### OpenStream_AfterDispose_ThrowsObjectDisposedException
Asserts that attempting to open a new stream via `OpenStream` after the engine has been disposed results in an `ObjectDisposedException`.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Expects `ObjectDisposedException`; fails if the stream opens successfully or a different exception occurs.

### OpenStream_PublishesStreamStartedEvent
Confirms that successfully opening a new stream triggers the publication of a stream-started event.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if the event is not published upon stream creation.

### GetAllMetrics_ReflectsActiveStreamCount
Validates that the `GetAllMetrics` method returns data where the active stream count accurately reflects the current number of open streams managed by the engine.
*   **Parameters**: None.
*   **Return Value**: `Task`.
*   **Throws**: Fails if the reported metric count diverges from the actual active stream count.

## Usage

### Example 1: Verifying Backpressure and Throttling Behavior
This example demonstrates how the test suite validates that the engine correctly throttles production when the credit window is exhausted and resumes when credit is restored.

```csharp
[TestFixture]
public class FlowControlValidation
{
    [Test]
    public async Task ValidateThrottlingLifecycle()
    {
        var engine = new BidirectionalStreamingEngine(new EngineOptions { InitialCredit = 2 });
        
        // Exhaust the credit window
        await engine.SendDataAsync(streamId: 1, payload: largePayload);
        await engine.SendDataAsync(streamId: 1, payload: largePayload);
        
        // Assert engine reports throttled state
        Assert.IsTrue(engine.IsThrottled, "Engine should be throttled when credit is exhausted");
        
        // Simulate consumer releasing credit
        engine.ReleaseCredit(amount: 1);
        
        // Assert throttling is lifted
        Assert.IsFalse(engine.IsThrottled, "Engine should resume when credit is released");
        
        await engine.DisposeAsync();
    }
}
```

### Example 2: Validating Lifecycle Events and Disposal Safety
This example illustrates testing the event publication during stream lifecycle changes and ensuring safe disposal behavior.

```csharp
[TestFixture]
public class LifecycleEventValidation
{
    [Test]
    public async Task ValidateStreamEventsAndDisposal()
    {
        var engine = new BidirectionalStreamingEngine();
        var eventsReceived = new List<string>();
        
        engine.StreamStarted += (s, e) => eventsReceived.Add("Started");
        engine.StreamEnded += (s, e) => eventsReceived.Add("Ended");

        // Open and close a stream
        var streamId = await engine.OpenStreamAsync();
        await engine.CloseStreamAsync(streamId);
        
        Assert.Contains("Started", eventsReceived);
        Assert.Contains("Ended", eventsReceived);

        // Dispose engine and verify protection against new streams
        await engine.DisposeAsync();
        
        Assert.ThrowsAsync<ObjectDisposedException>(async () => 
            await engine.OpenStreamAsync()
        );
    }
}
```

## Notes

*   **Thread Safety**: The tests imply that the underlying engine handles concurrent access to the credit window and stream registry safely. Specifically, the transition between throttled and non-throttled states (`IsThrottled`) and the emission of `BackpressureChangedEvent` must be atomic to prevent race conditions where a producer might proceed despite a saturated window.
*   **Idempotency**: The `CloseStreamAsync` method is designed to be idempotent. Calling it with an invalid or previously closed stream ID will not result in an exception, which simplifies cleanup logic in error handling paths where the stream state might be uncertain.
*   **Disposal Semantics**: Once `DisposeAsync` is invoked, the engine enters a terminal state. Any subsequent attempt to interact with the engine (e.g., `OpenStream`) will strictly throw `ObjectDisposedException`. The disposal process is comprehensive, ensuring all active streams are terminated before the task completes.
*   **Adaptive Mode Overhead**: When `NonAdaptiveMode` is enabled, the adaptive controller logic is bypassed entirely. Tests confirm this exit path is immediate, ensuring no unnecessary CPU cycles are spent calculating window adjustments when dynamic flow control is disabled.
*   **Timeout Sensitivity**: The `ProducerTimeout_WhenWindowExhausted` test highlights a critical failure mode where the system must fail fast if backpressure persists too long. This prevents indefinite hanging of producer tasks in scenarios where the consumer is unresponsive.
