# FlowControlOptionsEdgeCaseTests
The `FlowControlOptionsEdgeCaseTests` class is designed to test the edge cases of the `FlowControlOptions` class, ensuring that it behaves correctly under various scenarios, including invalid or boundary input values. This class provides a set of test methods that validate the behavior of `FlowControlOptions` when configured with different settings, such as initial window size, maximum window size, inbound and outbound channel capacities, backpressure threshold, credit replenishment batch, and max producer wait time.

## API
The `FlowControlOptionsEdgeCaseTests` class contains the following public members:
* `Validate_DefaultOptions_DoesNotThrow`: Verifies that the default options do not throw any exceptions.
* `Validate_ZeroInitialWindowSize_Throws`: Tests that an exception is thrown when the initial window size is set to zero.
* `Validate_NegativeInitialWindowSize_Throws`: Tests that an exception is thrown when the initial window size is set to a negative value.
* `Validate_MaxWindowSizeLessThanInitial_Throws`: Tests that an exception is thrown when the maximum window size is less than the initial window size.
* `Validate_MaxWindowSizeEqualsInitial_DoesNotThrow`: Verifies that no exception is thrown when the maximum window size equals the initial window size.
* `Validate_ZeroInboundChannelCapacity_Throws`: Tests that an exception is thrown when the inbound channel capacity is set to zero.
* `Validate_ZeroOutboundChannelCapacity_Throws`: Tests that an exception is thrown when the outbound channel capacity is set to zero.
* `Validate_BackpressureThresholdAboveOne_Throws`: Tests that an exception is thrown when the backpressure threshold is above one.
* `Validate_BackpressureThresholdNegative_Throws`: Tests that an exception is thrown when the backpressure threshold is negative.
* `Validate_BackpressureThresholdAtBoundaries_DoesNotThrow`: Verifies that no exception is thrown when the backpressure threshold is at the boundaries (i.e., zero or one).
* `Validate_ZeroCreditReplenishmentBatch_Throws`: Tests that an exception is thrown when the credit replenishment batch is set to zero.
* `Validate_NegativeMaxProducerWaitTime_Throws`: Tests that an exception is thrown when the max producer wait time is negative.
* `Validate_NullMaxProducerWaitTime_DoesNotThrow`: Verifies that no exception is thrown when the max producer wait time is null.
* `Validate_ZeroAdaptiveAdjustmentInterval_Throws`: Tests that an exception is thrown when the adaptive adjustment interval is set to zero.
* `HighThroughputPreset_HasAdaptiveMode`: Verifies that the high-throughput preset has adaptive mode enabled.
* `LowLatencyPreset_HasCreditWindowMode`: Verifies that the low-latency preset has credit window mode enabled.
* `HighThroughputPreset_Validates`: Tests that the high-throughput preset validates correctly.
* `LowLatencyPreset_Validates`: Tests that the low-latency preset validates correctly.

## Usage
Here are two examples of using the `FlowControlOptionsEdgeCaseTests` class:
```csharp
// Example 1: Testing default options
var tests = new FlowControlOptionsEdgeCaseTests();
tests.Validate_DefaultOptions_DoesNotThrow();

// Example 2: Testing invalid initial window size
var tests = new FlowControlOptionsEdgeCaseTests();
try
{
    tests.Validate_ZeroInitialWindowSize_Throws();
}
catch (Exception ex)
{
    Console.WriteLine("Exception caught: " + ex.Message);
}
```

## Notes
The `FlowControlOptionsEdgeCaseTests` class is designed to test the edge cases of the `FlowControlOptions` class, and as such, it does not provide any guarantees about thread-safety. However, since the test methods do not modify any shared state, they can be safely executed concurrently. Additionally, the class does not handle any exceptions that may be thrown by the `FlowControlOptions` class, so users should be prepared to handle any exceptions that may be thrown. It is also worth noting that the `FlowControlOptionsEdgeCaseTests` class is not intended to be used in production code, but rather as a tool for testing and validating the behavior of the `FlowControlOptions` class.
