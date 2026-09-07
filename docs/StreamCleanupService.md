# StreamCleanupService

`StreamCleanupService` is a `BackgroundService` that periodically delegates idle-stream cleanup to `StreamingService`. It contains no cleanup policy of its own.

## Dependencies and service resolution

The constructor receives and stores:

- `IServiceProvider`, used to resolve `StreamingService` during each timer tick.
- `ILogger<StreamCleanupService>`, used for start, error, and stopping messages.

Both constructor arguments are checked for `null` and cause an `ArgumentNullException` when missing.

The service does not receive `StreamingService` directly. Inside the loop it calls `GetRequiredService<StreamingService>()` on the stored root `IServiceProvider`, then calls `CleanupIdleStreams()` on the resolved instance. Because the registered `StreamingService` is a singleton, no scope is created or required by this code.

## Execution loop

`ExecuteAsync` creates a `PeriodicTimer` with a fixed five-minute interval and logs that the service has started. It then waits for timer ticks with `WaitForNextTickAsync(stoppingToken)`.

There is no cleanup immediately at startup; the first cleanup runs after the first timer tick. On every tick, the service resolves `StreamingService` and invokes `CleanupIdleStreams()`. Exceptions thrown by resolution or cleanup are caught and logged, allowing the loop to continue to later ticks. Cancellation is supplied to the timer wait; the timer is disposed when `ExecuteAsync` exits.

`StopAsync` logs that the service is stopping and delegates shutdown to `BackgroundService.StopAsync`.

## Registration

`AddGrpcWebBridge()` registers the class with:

```csharp
services.AddHostedService<StreamCleanupService>();
```

The same method registers `StreamingService` as a singleton, making it available when the hosted service resolves it.

## Difference from StreamCleanupWorker

Unlike the separately documented [StreamCleanupWorker](StreamCleanupWorker.md), `StreamCleanupService` is the cleanup implementation registered by `AddGrpcWebBridge()`.

`StreamCleanupService` has a fixed five-minute `PeriodicTimer`, resolves `StreamingService` from `IServiceProvider` on every tick, and delegates the entire cleanup operation to `CleanupIdleStreams()`. It exposes no options or statistics.

By contrast, `StreamCleanupWorker` receives `StreamingService` directly, uses configurable `StreamCleanupOptions`, runs a cleanup before its first delay, enumerates and closes streams itself, tracks cleanup statistics, and can trigger generation-zero garbage collection. It is not registered by `AddGrpcWebBridge()` in the code described here.
