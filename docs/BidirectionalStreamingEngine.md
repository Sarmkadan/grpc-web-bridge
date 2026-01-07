# BidirectionalStreamingEngine

The `BidirectionalStreamingEngine` class manages the lifecycle of bidirectional gRPC‑Web streams within the bridge. It provides methods to open, retrieve, close, and monitor streams, as well as asynchronous disposal of all associated resources.

## API

### BidirectionalStreamingEngine  
**Purpose**: Public class that encapsulates stream handling logic. Instances are created via its constructor (not shown) and are responsible for coordinating one or more `IFlowControlledStream` objects.  
**Parameters**: None (constructor parameters are implementation‑specific).  
**Return Value**: N/A.  
**Exceptions**: May throw `ObjectDisposedException` if the instance has already been disposed.

### Task<IFlowControlledStream> OpenStreamAsync()  
**Purpose**: Initiates a new bidirectional stream and returns a task that completes when the stream is ready for use.  
**Parameters**: None.  
**Return Value**: A `Task<IFlowControlledStream>` yielding the opened stream, or `null` if the engine cannot create a stream (implementation‑dependent).  
**Exceptions**:  
- `ObjectDisposedException` – the engine has been disposed.  
- `InvalidOperationException` – the engine is not in a state that permits opening a new stream (e.g., maximum concurrent streams exceeded).  
- Any exception propagated from the underlying transport layer.

### IFlowControlledStream? GetStream()  
**Purpose**: Retrieves the currently active stream associated with the engine, if one exists.  
**Parameters**: None.  
**Return Value**: The active `IFlowControlledStream` instance, or `null` when no stream is open.  
**Exceptions**:  
- `ObjectDisposedException` – the engine has been disposed.

### Task CloseStreamAsync()  
**Purpose**: Asynchronously closes the active stream, if any, and releases its resources.  
**Parameters**: None.  
**Return Value**: A `Task` that completes when the stream has been closed.  
**Exceptions**:  
- `ObjectDisposedException` – the engine has been disposed.  
- `InvalidOperationException` – there is no stream to close.  
- Any exception from the underlying transport during teardown.

### IReadOnlyDictionary<string, StreamThroughputMetrics> GetAllMetrics()  
**Purpose**: Provides a snapshot of throughput metrics for all streams managed by the engine.  
**Parameters**: None.  
**Return Value**: An immutable dictionary mapping stream identifiers to `StreamThroughputMetrics` objects. Returns an empty dictionary when no streams are present.  
**Exceptions**:  
- `ObjectDisposedException` – the engine has been disposed.

### async ValueTask DisposeAsync()  
**Purpose**: Asynchronously disposes of the engine, closing any open streams and releasing held resources.  
**Parameters**: None.  
**Return Value**: A `ValueTask` that completes when disposal is finished.  
**Exceptions**:  
- May throw if disposal logic encounters an internal error; subsequent calls after disposal typically return a completed `ValueTask` without throwing.

## Usage

### Example 1: Simple request‑response streaming

```csharp
using var engine = new BidirectionalStreamingEngine();

// Open a stream for a particular RPC.
IFlowControlledStream? stream = await engine.OpenStreamAsync();
if (stream == null)
{
    throw new InvalidOperationException("Failed to open stream.");
}

// Write a request message.
await stream.WriteAsync(requestMessage, cancellationToken);

// Read responses until the server signals completion.
await foreach (var response in stream.ReadAllAsync(cancellationToken))
{
    ProcessResponse(response);
}

// Gracefully close the stream.
await engine.CloseStreamAsync();

// Dispose the engine when no longer needed.
await engine.DisposeAsync();
```

### Example 2: Monitoring throughput while streaming

```csharp
await using var engine = new BidirectionalStreamingEngine();

IFlowControlledStream? stream = await engine.OpenStreamAsync();
if (stream == null) return;

// Start a background task that periodically logs metrics.
_ = Task.Run(async () =>
{
    while (!stream.IsClosed)
    {
        IReadOnlyDictionary<string, StreamThroughputMetrics> metrics = engine.GetAllMetrics();
        foreach (var (id, m) in metrics)
        {
            Console.WriteLine($"Stream {id}: {m.BytesSent} sent, {m.BytesReceived} received");
        }
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
});

// Use the stream as needed …
await stream.WriteAsync(message, cancellationToken);
await foreach (var msg in stream.ReadAllAsync(cancellationToken))
{
    Handle(msg);
}

await engine.CloseStreamAsync();
// Engine disposed automatically via await using.
```

## Notes

- **Thread safety**: The class is not thread‑safe for concurrent modifications. Calls to `OpenStreamAsync`, `GetStream`, `CloseStreamAsync`, and `DisposeAsync` should be serialized externally (e.g., by invoking them from a single logical thread or using a lock). `GetAllMetrics` may be called concurrently with other operations, but the returned snapshot reflects the state at the instant of invocation and may become stale immediately afterward.
- **Disposal**: After `DisposeAsync` completes, all subsequent member invocations will throw `ObjectDisposedException`. It is safe to call `DisposeAsync` multiple times; additional calls return a completed `ValueTask`.
- **Stream lifecycle**: At most one active stream is tracked by `GetStream`; opening a new stream while another is open may replace the previous stream or be prohibited, depending on the implementation. Consult the specific implementation for exact behavior.
- **Metrics**: `StreamThroughputMetrics` contains cumulative counters; they are not reset between streams unless the engine is disposed and recreated. The dictionary keys are implementation‑defined identifiers (e.g., GUIDs or numeric IDs).
