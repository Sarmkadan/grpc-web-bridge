# StreamingServiceValidation

`StreamingServiceValidation` provides a set of static utility methods for validating the configuration and runtime state of streaming services within the `grpc-web-bridge` infrastructure. It centralizes integrity checks, returning structured lists of validation failures, offering boolean pass/fail assessments, and enforcing correctness by throwing on invalid conditions.

## API

### `Validate`

```csharp
public static IReadOnlyList<string> Validate( /* parameters omitted from public signature */ )
```

Performs a full validation pass over the target streaming service configuration or state. Returns a read-only list of diagnostic strings, each describing a distinct validation failure. An empty list indicates that no problems were found. This overload does not throw; all issues are captured in the returned collection.

### `IsValid`

```csharp
public static bool IsValid( /* parameters omitted from public signature */ )
```

Executes the same underlying validation logic as `Validate` but condenses the result into a simple boolean. Returns `true` when the validation produces zero failure messages; returns `false` otherwise. This method never throws.

### `EnsureValid`

```csharp
public static void EnsureValid( /* parameters omitted from public signature */ )
```

Runs the full validation routine and, if any failures are detected, throws an exception whose message aggregates the failure details. When validation passes, the method returns silently. This is the enforcement variant, suitable for guard clauses where proceeding with an invalid state is unacceptable.

*The three members above appear in multiple overload groups distinguished by their parameter types. Each group targets a different aspect of streaming service validation (e.g., service definition, active stream bindings, transport compatibility). All overloads follow the same return-type and exception-throwing semantics.*

## Usage

### Example 1: Logging warnings without halting execution

```csharp
var failures = StreamingServiceValidation.Validate(serviceDefinition);
if (failures.Count > 0)
{
    foreach (var message in failures)
    {
        logger.Warning("Streaming service validation issue: {Message}", message);
    }
}
// Execution continues; the service may operate in a degraded mode.
```

### Example 2: Guarding a critical code path

```csharp
public void RegisterStreamingService(ServiceDefinition definition)
{
    StreamingServiceValidation.EnsureValid(definition);
    // If we reach here, the definition is guaranteed valid.
    activeServices.Add(definition);
}
```

## Notes

- **Immutability of results:** The `IReadOnlyList<string>` returned by `Validate` is a snapshot of failures at the time of the call. Subsequent mutations to the validated object are not reflected in the list.
- **Exception aggregation:** When `EnsureValid` throws, the exception message may contain newline-separated failure strings. Callers that catch this exception should avoid parsing the message format; use `Validate` instead if programmatic access to individual failures is required.
- **Thread safety:** All methods are static and do not mutate shared state. They are safe to call concurrently provided the objects passed as arguments are not being modified by other threads during validation. No internal synchronization is performed on the validated targets.
- **Overload discovery:** The multiple `Validate` / `IsValid` / `EnsureValid` overloads are resolved at compile time based on argument type. When adding new validated entities, confirm that an appropriate overload exists rather than relying on implicit conversions.
- **Empty-list convention:** A returned empty list from `Validate` always means “no failures.” Null is never returned.
