# BackpressureControllerExtensions

Provides a set of static helper methods for managing back‑pressure credits in the gRPC‑Web bridge. These extensions operate on a `BackpressureController` instance and allow producers to acquire, release, and query credits in a thread‑aware manner.

## API

### TryConsumeCredits
```csharp
public static bool TryConsumeCredits(this BackpressureController controller, int credits)
```
Attempts to reserving `credits` units from the controller’s available quota.  
- **Parameters**  
  - `controller`: The `BackpressureController` to query. Must not be `null`.  
  - `credits`: Number of credits to consume; must be zero or positive.  
- **Return value**  
  - `true` if the requested credits were successfully reserved; `false` if insufficient credits are available.  
- **Exceptions**  
  - `ArgumentNullException` if `controller` is `null`.  
  - `ArgumentOutOfRangeException` if `credits` is negative.

### ConsumeCreditsAsync
```csharp
public static async ValueTask ConsumeCreditsAsync(this BackpressureController controller, int credits, CancellationToken cancellationToken = default)
```
Asynchronously waits until `credits` units become available and then reserves them.  
- **Parameters**  
  - `controller`: The `BackpressureController` to draw from; must not be `null`.  
  - `credits`: Number of credits to consume; must be zero or positive.  
  - `cancellationToken`: Optional token to cancel the wait.  
- **Return value**  
  - Completes when the credits have been reserved.  
- **Exceptions**  
  - `ArgumentNullException` if `controller` is `null`.  
  - `ArgumentOutOfRangeException` if `credits` is negative.  
  - `OperationCanceledException` if the wait is cancelled via `cancellationToken`.

### ReleaseCredits
```csharp
public static void ReleaseCredits(this BackpressureController controller, int credits)
```
Returns previously consumed `credits` back to the controller’s pool, making them available for other consumers.  
- **Parameters**  
  - `controller`: The `BackpressureController` to release credits to; must not be `null`.  
  - `credits`: Number of credits to release; must be zero or positive.  
- **Return value**  
  - None.  
- **Exceptions**  
  - `ArgumentNullException` if `controller` is `null`.  
  - `ArgumentOutOfRangeException` if `credits` is negative.

### GetUtilizationPercentString
```csharp
public static string GetUtilizationPercentString(this BackpressureController controller)
```
Produces a human‑readable string representing the current credit utilization as a percentage (e.g., `"73%"`).  
- **Parameters**  
  - `controller`: The `BackpressureController` to inspect; must not be `null`.  
- **Return value**  
  - A formatted percentage string with no decimal places.  
- **Exceptions**  
  - `ArgumentNullException` if `controller` is `null`.

### GetStatusString
```csharp
public static string GetStatusString(this BackpressureController controller)
```
Returns a concise textual description of the controller’s current state, including available credits, total quota, and utilization.  
- **Parameters**  
  - `controller`: The `BackpressureController` to describe; must not be `null`.  
- **Return value**  
  - A multi‑line string suitable for logging or diagnostics.  
- **Exceptions**  
  - `ArgumentNullException` if `controller` is `null`.

## Usage

### Synchronous credit management
```csharp
var controller = new BackpressureController(initialQuota: 100);

// Try to reserve 10 credits without blocking
if (controller.TryConsumeCredits(10))
{
    // Process work that requires 10 credits
    DoWork();

    // Return the credits when finished
    controller.ReleaseCredits(10);
}
else
{
    // Not enough credits available; handle back‑pressure
    Log.Warning("Insufficient credits for work.");
}
```

### Asynchronous credit acquisition with status reporting
```csharp
var controller = new BackpressureController(initialQuota: 50);
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

await controller.ConsumeCreditsAsync(25, cts.Token);
// At this point 25 credits are held

Console.WriteLine($"Utilization: {controller.GetUtilizationPercentString()}");
Console.WriteLine(controller.GetStatusString());

// Simulate work …
await Task.Delay(TimeSpan.FromSeconds(5));

// Release the credits back to the pool
controller.ReleaseCredits(25);
```

## Notes
- All methods validate that the `BackpressureController` instance is not `null` and that credit amounts are non‑negative; violating these preconditions results in an `ArgumentNullException` or `ArgumentOutOfRangeException`.  
- `TryConsumeCredits` never blocks; it returns `false` immediately when the quota is insufficient, making it suitable for polling or fire‑and‑forget scenarios.  
- `ConsumeCreditsAsync` will block the calling async context until the requested credits become available or the operation is cancelled; it respects the supplied `CancellationToken`.  
- The controller itself may or may not be thread‑safe depending on its implementation. These extension methods do not add additional synchronization; callers must ensure external synchronization if the underlying `BackpressureController` is not thread‑safe.  
- Utilization strings are formatted without decimal places; values are rounded down to the nearest whole percent.  
- Releasing more credits than were previously consumed will increase the available quota beyond the original limit; callers should balance acquires and releases to avoid unintended quota inflation.
