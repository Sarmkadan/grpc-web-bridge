# FlowControlOptionsExtensions

Provides extension methods for `FlowControlOptions`, the immutable configuration record for the bidirectional streaming engine's flow-control and backpressure subsystem. The class offers two helpers: `Validate`, which checks a configuration against the engine's constraints, and `Clone`, which produces an independent copy of a configuration.

The class lives in the `GrpcWebBridge.Streaming` namespace, in `src/GrpcWebBridge/Streaming/FlowControlOptionsExtensions.cs`.

> **Important:** `FlowControlOptions` already declares an instance method named `Validate()` with the same shape. The compiler binds `options.Validate()` to the instance method, so the extension is never selected by that syntax. To call the extension, invoke it statically, as `FlowControlOptionsExtensions.Validate(options)`. The two validators also disagree on zero-valued durations; see [Differences from the instance Validate](#differences-from-the-instance-validate).

## API

### Validate
**Purpose**  
Checks the values of a `FlowControlOptions` instance against the constraints the streaming engine depends on. Rules are evaluated in the order listed below, and the method stops at the first violation.

**Parameters**  
- `options`: The `FlowControlOptions` instance to validate. The method does not guard against `null`.

**Return Value**  
None (`void`). Returns normally when every rule passes.

**Validation rules**

| # | Property | Rule | Parameter name in exception |
|---|----------|------|-----------------------------|
| 1 | `InitialWindowSize` | Must be greater than zero. | `InitialWindowSize` |
| 2 | `MaxWindowSize` | Must be greater than or equal to `InitialWindowSize`. | `MaxWindowSize` |
| 3 | `InboundChannelCapacity` | Must be greater than zero. | `InboundChannelCapacity` |
| 4 | `OutboundChannelCapacity` | Must be greater than zero. | `OutboundChannelCapacity` |
| 5 | `BackpressureThreshold` | Must be between `0.0` and `1.0` inclusive. | `BackpressureThreshold` |
| 6 | `CreditReplenishmentBatch` | Must be greater than zero. | `CreditReplenishmentBatch` |
| 7 | `MaxProducerWaitTime` | If it has a value, that value must not be less than zero. `null` is allowed and means an indefinite wait. | `MaxProducerWaitTime` |
| 8 | `AdaptiveAdjustmentInterval` | Must not be less than zero. | `AdaptiveAdjustmentInterval` |

`Mode` and `EmitBackpressureEvents` are not checked; any value of those properties is accepted.

**Exceptions**  
- `ArgumentOutOfRangeException` is thrown for the first rule that fails. The `ParamName` is the property name from the table above. The message is a short explanation, for example `Must be >= InitialWindowSize (64).` for rule 2.  
- `NullReferenceException` is thrown if `options` is `null`, because the method dereferences it without a guard. Callers should check for `null` first if the value can be absent.

No other exception types are thrown by this method.

### Clone
**Purpose**  
Creates a new `FlowControlOptions` instance whose property values are copied from the source. Changes to the copy do not affect the source, and the reverse is also true. The method does not validate its input or its output.

**Parameters**  
- `options`: The `FlowControlOptions` instance to copy.

**Return Value**  
A new `FlowControlOptions` instance with the copied values. The `MaxProducerWaitTime` value is copied as a value-type `TimeSpan?`, so there are no shared mutable references between source and copy.

**Exceptions**  
- `NullReferenceException` is thrown if `options` is `null`.

**Known gap: `EmitBackpressureEvents` is not copied**  
`Clone` copies every property except `EmitBackpressureEvents`. The copy always gets the default value `true`, regardless of the source. If the source has `EmitBackpressureEvents = false`, the clone silently re-enables backpressure events. Callers that need an exact copy should use the record's `with` expression instead, which preserves all properties:

```csharp
var exactCopy = original with { };
```

This gap is documented here as current behaviour. It has not been changed in this file.

## Differences from the instance Validate

`FlowControlOptions.Validate()` (the instance method) and `FlowControlOptionsExtensions.Validate(options)` (this extension) apply the same rules except for two duration checks:

| Property | Instance `Validate()` | Extension `Validate(options)` |
|----------|------------------------|--------------------------------|
| `MaxProducerWaitTime` | Rejects values `<= TimeSpan.Zero` | Rejects values `< TimeSpan.Zero` (zero is accepted) |
| `AdaptiveAdjustmentInterval` | Rejects values `<= TimeSpan.Zero` | Rejects values `< TimeSpan.Zero` (zero is accepted) |

The extension's message for `AdaptiveAdjustmentInterval` says `Must be positive.`, which does not match its behaviour for zero. Choose the validator that matches the constraint you want. If you need a zero-length wait or interval to be rejected, use the instance method.

## Examples

### Validating a configuration with the extension

```csharp
using GrpcWebBridge.Streaming;

var options = new FlowControlOptions
{
    InitialWindowSize = 32,
    MaxWindowSize = 128,
    CreditReplenishmentBatch = 8
};

// Use the static call form. options.Validate() would bind to the instance method.
FlowControlOptionsExtensions.Validate(options);
```

### Handling a validation failure

```csharp
using GrpcWebBridge.Streaming;

var options = new FlowControlOptions
{
    InitialWindowSize = 100,
    MaxWindowSize = 50   // violates rule 2
};

try
{
    FlowControlOptionsExtensions.Validate(options);
}
catch (ArgumentOutOfRangeException ex)
{
    // ex.ParamName == "MaxWindowSize"
    // ex.Message contains "Must be >= InitialWindowSize (100)."
    Console.Error.WriteLine($"Invalid flow control setting '{ex.ParamName}': {ex.Message}");
}
```

### Validating a preset

The built-in presets are expected to pass validation. This is a quick sanity check, for example in a test:

```csharp
FlowControlOptionsExtensions.Validate(FlowControlOptions.HighThroughput);
FlowControlOptionsExtensions.Validate(FlowControlOptions.LowLatency);
```

### Creating a modified copy with Clone

```csharp
using GrpcWebBridge.Streaming;

var baseline = FlowControlOptions.LowLatency;

// Clone gives an independent copy. The record's `with` expression is also available.
FlowControlOptions tuned = baseline.Clone() with
{
    InitialWindowSize = 24,
    MaxWindowSize = 96
};

FlowControlOptionsExtensions.Validate(tuned);
```

Because `Clone` drops `EmitBackpressureEvents`, prefer `with` when you need an exact copy:

```csharp
FlowControlOptions quiet = baseline with { EmitBackpressureEvents = false };
```

### Validate after Clone

`Clone` does not validate. Run `Validate` on the copy if it is going to be modified:

```csharp
FlowControlOptions copy = source.Clone();
copy = copy with { CreditReplenishmentBatch = 0 };

// Throws ArgumentOutOfRangeException for CreditReplenishmentBatch.
FlowControlOptionsExtensions.Validate(copy);
```

## Related types

- `FlowControlOptions` (`docs/FlowControlOptions.md`): the record these methods operate on, including the `HighThroughput` and `LowLatency` presets.
- `FlowControlMode`: the enum for the `Mode` property. It is not checked by `Validate`.
- `FlowControlOptionsEdgeCaseTests` (`docs/FlowControlOptionsEdgeCaseTests.md`): tests that cover the instance `Validate` method.
