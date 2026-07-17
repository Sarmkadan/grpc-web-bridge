# StreamingExceptionExtensions

Provides a set of extension methods for `StreamingException` that simplify inspection of stream state, retrieval of contextual metadata, and creation of enriched exception instances. These utilities allow callers to classify a streaming failure as terminal, recoverable, or failed without manually interpreting the underlying state enumeration, and to attach or extract structured context data carried alongside the exception.

## API

### IsTerminalState

```csharp
public static bool IsTerminalState(this StreamingException exception)
```

Determines whether the exception represents a terminal stream state—one that signals the stream has definitively ended and no further frames will be processed. Returns `true` when the internal state corresponds to a terminal condition; otherwise `false`. Does not throw.

### IsRecoverableState

```csharp
public static bool IsRecoverableState(this StreamingException exception)
```

Indicates whether the stream failure is classified as recoverable, meaning the client may retry or re-establish the stream without a fundamental protocol violation. Returns `true` for recoverable states, `false` otherwise. Does not throw.

### IsFailedState

```csharp
public static bool IsFailedState(this StreamingException exception)
```

Returns `true` when the exception state denotes a permanent failure that should not be retried automatically. This typically covers protocol errors, authentication failures, or internal server faults that are not transient. Does not throw.

### GetStreamStateString

```csharp
public static string GetStreamStateString(this StreamingException exception)
```

Returns a human-readable string representation of the current stream state stored in the exception. Useful for logging and diagnostics. Never returns `null`; for unrecognised states it returns the numeric value formatted as a string. Does not throw.

### WithContext

```csharp
public static StreamingException WithContext(
    this StreamingException exception,
    string key,
    object value)
```

Creates and returns a new `StreamingException` that copies all properties from the original instance and adds or overwrites a single context entry identified by `key`. The original exception is not modified. If `key` is `null` or empty, an `ArgumentException` is thrown. If `value` is `null`, the entry is stored as a null object.

### HasErrorCode

```csharp
public static bool HasErrorCode(this StreamingException exception)
```

Checks whether a non-zero gRPC status code is present in the exception’s metadata. Returns `true` if an error code is available; `false` when the code is zero, missing, or could not be parsed. Does not throw.

### GetStreamContext

```csharp
public static IReadOnlyDictionary<string, object> GetStreamContext(
    this StreamingException exception)
```

Returns a read-only dictionary containing all contextual key-value pairs attached to the exception. Returns an empty dictionary when no context has been set. The returned dictionary is a snapshot; subsequent modifications to the exception’s context (via `WithContext`) are not reflected. Does not throw.

## Usage

### Example 1: Classifying a stream error for retry logic

```csharp
try
{
    await streamingCall.ResponseStream.MoveNextAsync(token);
}
catch (StreamingException ex)
{
    if (ex.IsTerminalState())
    {
        _logger.LogInformation("Stream completed with terminal state: {State}", ex.GetStreamStateString());
        return;
    }

    if (ex.IsRecoverableState())
    {
        _logger.LogWarning("Recoverable stream fault, reconnecting: {State}", ex.GetStreamStateString());
        await ReconnectStreamAsync();
        return;
    }

    if (ex.IsFailedState())
    {
        _logger.LogError("Non-recoverable stream failure: {State}", ex.GetStreamStateString());
        throw;
    }
}
```

### Example 2: Enriching and inspecting context

```csharp
catch (StreamingException ex)
{
    var enriched = ex
        .WithContext("request_id", currentRequestId)
        .WithContext("retry_count", retryAttempt);

    if (enriched.HasErrorCode())
    {
        var ctx = enriched.GetStreamContext();
        LogError(enriched, ctx);
    }

    // Re-throw the enriched exception so upstream middleware sees the context.
    throw enriched;
}
```

## Notes

- All methods treat a `null` `StreamingException` reference as a standard null-reference error; no method guards against null `this` and will throw `NullReferenceException` in that case.
- `WithContext` always produces a new instance; it never mutates the original exception. This makes it safe to call concurrently on the same source exception from multiple threads.
- The dictionary returned by `GetStreamContext` is a snapshot copy. Concurrent calls to `WithContext` on the original exception do not affect already-returned dictionaries, preserving their immutability for the caller.
- `IsTerminalState`, `IsRecoverableState`, and `IsFailedState` are mutually exclusive for a given exception instance at a single point in time, but an exception’s underlying state is immutable once constructed, so the classification does not change over the lifetime of the object.
- `GetStreamStateString` is safe for diagnostic output and will never return `null`, even when the underlying state value is out of the expected enumeration range.
