# StreamingExtensions

`StreamingExtensions` provides registration methods for configuring bidirectional streaming services within the `grpc-web-bridge` dependency injection container. It exposes several overloads of `AddBidirectionalStreaming` for general-purpose streaming, along with specialized convenience methods that pre-configure streaming for high-throughput or low-latency scenarios, and a diagnostics registration that enables monitoring and telemetry for active streaming connections.

## API

### AddBidirectionalStreaming (overload 1)

```csharp
public static IServiceCollection AddBidirectionalStreaming(this IServiceCollection services)
```

Registers the default bidirectional streaming infrastructure with standard options. This overload applies no custom configuration and relies on the built-in defaults for buffer sizes, timeouts, and concurrency limits.

- **Parameters:** `services` — the `IServiceCollection` to augment.
- **Return value:** The same `IServiceCollection` instance, enabling fluent chaining.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddBidirectionalStreaming (overload 2)

```csharp
public static IServiceCollection AddBidirectionalStreaming(this IServiceCollection services, Action<BidirectionalStreamingOptions> configure)
```

Registers bidirectional streaming and applies user-supplied configuration through a delegate.

- **Parameters:**
  - `services` — the `IServiceCollection` to augment.
  - `configure` — an `Action<BidirectionalStreamingOptions>` that mutates the options instance before registration.
- **Return value:** The same `IServiceCollection` instance, enabling fluent chaining.
- **Exceptions:** Throws `ArgumentNullException` if `services` or `configure` is `null`.

### AddStreamingDiagnostics

```csharp
public static IServiceCollection AddStreamingDiagnostics(this IServiceCollection services)
```

Registers diagnostics services that collect metrics, traces, and health signals for all active bidirectional streams. This method is additive and can be combined with any streaming registration.

- **Parameters:** `services` — the `IServiceCollection` to augment.
- **Return value:** The same `IServiceCollection` instance, enabling fluent chaining.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddHighThroughputBidirectionalStreaming

```csharp
public static IServiceCollection AddHighThroughputBidirectionalStreaming(this IServiceCollection services)
```

Registers bidirectional streaming pre-configured for maximum throughput. Internally, this applies options that favor larger buffer sizes, relaxed backpressure thresholds, and higher concurrency limits at the expense of increased memory consumption and potentially higher latency for individual messages.

- **Parameters:** `services` — the `IServiceCollection` to augment.
- **Return value:** The same `IServiceCollection` instance, enabling fluent chaining.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddLowLatencyBidirectionalStreaming

```csharp
public static IServiceCollection AddLowLatencyBidirectionalStreaming(this IServiceCollection services)
```

Registers bidirectional streaming pre-configured for minimal per-message latency. Internally, this applies options that favor small buffer sizes, aggressive flushing, and lower concurrency limits to reduce queueing delays.

- **Parameters:** `services` — the `IServiceCollection` to augment.
- **Return value:** The same `IServiceCollection` instance, enabling fluent chaining.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

## Usage

### Example 1: Basic registration with custom options and diagnostics

```csharp
using grpc_web_bridge;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddBidirectionalStreaming(options =>
    {
        options.MaxConcurrentStreams = 16;
        options.DefaultBufferSize = 32 * 1024; // 32 KB
    })
    .AddStreamingDiagnostics();

var app = builder.Build();
app.Run();
```

### Example 2: Profile-based registration in a configuration-driven host

```csharp
using grpc_web_bridge;

var builder = WebApplication.CreateBuilder(args);

bool lowLatencyMode = builder.Configuration.GetValue<bool>("Streaming:LowLatency");

if (lowLatencyMode)
{
    builder.Services.AddLowLatencyBidirectionalStreaming();
}
else
{
    builder.Services.AddHighThroughputBidirectionalStreaming();
}

builder.Services.AddStreamingDiagnostics();

var app = builder.Build();
app.Run();
```

## Notes

- All methods return the same `IServiceCollection` instance and are designed for fluent chaining. Order of registration matters only insofar as later registrations may override services registered earlier if they target the same service descriptors.
- `AddStreamingDiagnostics` is independent of the specific streaming profile; it can be safely appended after any combination of `AddBidirectionalStreaming`, `AddHighThroughputBidirectionalStreaming`, or `AddLowLatencyBidirectionalStreaming`.
- The specialized methods `AddHighThroughputBidirectionalStreaming` and `AddLowLatencyBidirectionalStreaming` are mutually exclusive in intent. Calling both on the same container will result in the last one winning for any overlapping service descriptors, which may produce an inconsistent configuration. Prefer using one profile and fine-tuning via the options overload of `AddBidirectionalStreaming` if custom behavior is required.
- These extension methods are not thread-safe by themselves; they should be called during the sequential service-collection build phase before the container is built. Once `BuildServiceProvider` has been called, modifying the collection concurrently is unsupported and may lead to undefined behavior.
- If `AddBidirectionalStreaming` is invoked multiple times with different option delegates, each invocation registers a new set of services. Depending on the underlying service descriptors, this may result in duplicate registrations that the container resolves using last-registered-wins semantics. To avoid ambiguity, prefer a single call with a comprehensive configuration delegate.
