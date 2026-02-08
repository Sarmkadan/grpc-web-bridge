# ClientRateLimitTests

`ClientRateLimitTests` is a test suite that validates the behaviour of a client-side rate-limiting component in the `grpc-web-bridge` project. It exercises the core logic for allowing or denying requests based on configured limits, tracking request counts, and determining staleness of the rate-limiter state. The tests cover boundary conditions, concurrent access safety, and correct count reporting.

## API

### `public void AllowRequest_BelowLimit_ReturnsTrue`
Verifies that when the number of requests is below the configured limit, the rate-limiter permits the request.  
**Purpose:** Ensures normal operation under expected load.  
**Parameters:** None (test method).  
**Return value:** `void` (asserts internally).  
**Throws:** Only assertion failures if the implementation does not return `true`.

### `public void AllowRequest_AtLimitBoundary_ReturnsFalseForExtraRequest`
Confirms that once the request count reaches the exact limit, any additional request is denied.  
**Purpose:** Validates strict enforcement at the boundary.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Assertion failures if the extra request is incorrectly allowed.

### `public void AllowRequest_WithGenerousLimits_AllowsManyRequests`
Exercises the rate-limiter with a high limit and verifies that a large number of requests are all permitted.  
**Purpose:** Demonstrates that the component scales correctly and does not prematurely reject requests.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Assertion failures if any request within the generous limit is denied.

### `public void GetRequestCount_AfterSeveralRequests_ReturnsCorrectCount`
Issues a known number of allowed requests and then checks that the reported count matches exactly.  
**Purpose:** Ensures accurate internal tracking of request counts.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Assertion failures if the count diverges from the expected value.

### `public void GetRequestCount_OnFreshInstance_ReturnsZero`
Creates a new rate-limiter instance and asserts that the request count starts at zero.  
**Purpose:** Confirms correct initialisation of the counter.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Assertion failures if the initial count is non-zero.

### `public void IsStale_FreshInstance_IsStaleForNonZeroTimeout`
Checks that a freshly created rate-limiter with a non-zero timeout is immediately considered stale.  
**Purpose:** Validates that staleness detection works when no requests have been recorded yet.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Assertion failures if the instance is incorrectly reported as not stale.

### `public void IsStale_AfterRecentRequest_ReturnsFalse`
Performs a request and then immediately checks staleness; expects the instance to be considered active (not stale).  
**Purpose:** Ensures that recent activity resets or prevents the stale state.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Assertion failures if the instance is wrongly reported as stale.

### `public void AllowRequest_ConcurrentAccess_DoesNotThrowOrCorruptState`
Subjects the rate-limiter to concurrent requests from multiple threads and verifies that no exceptions are thrown and the internal state remains consistent.  
**Purpose:** Proves thread-safety of the implementation under parallel load.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Assertion failures if an exception occurs or the final state is corrupted.

## Usage

Below are two realistic examples showing how the tests exercise the production rate-limiter. The test methods themselves are invoked by a test runner, but the patterns illustrate the intended usage of the underlying component.

### Example 1: Basic rate-limit enforcement
```csharp
// Simulates the logic tested by AllowRequest_BelowLimit_ReturnsTrue
// and AllowRequest_AtLimitBoundary_ReturnsFalseForExtraRequest.
var limiter = new ClientRateLimit(maxRequests: 3, timeout: TimeSpan.FromMinutes(1));

// First three requests should be allowed.
Assert.True(limiter.AllowRequest());
Assert.True(limiter.AllowRequest());
Assert.True(limiter.AllowRequest());

// Fourth request hits the boundary and must be denied.
Assert.False(limiter.AllowRequest());

// Count should reflect exactly the allowed requests.
Assert.Equal(3, limiter.GetRequestCount());
```

### Example 2: Staleness and concurrent usage
```csharp
// Simulates the logic tested by IsStale_FreshInstance_IsStaleForNonZeroTimeout,
// IsStale_AfterRecentRequest_ReturnsFalse, and AllowRequest_ConcurrentAccess_DoesNotThrowOrCorruptState.
var limiter = new ClientRateLimit(maxRequests: 100, timeout: TimeSpan.FromSeconds(30));

// Fresh instance with non-zero timeout is stale.
Assert.True(limiter.IsStale());

// After a request it becomes active.
limiter.AllowRequest();
Assert.False(limiter.IsStale());

// Concurrent access should not throw or corrupt state.
Parallel.For(0, 50, i =>
{
    limiter.AllowRequest();
});
Assert.Equal(51, limiter.GetRequestCount()); // 1 earlier + 50 concurrent
```

## Notes

- **Boundary conditions:** The tests explicitly cover the exact limit boundary. Any off-by-one error in the implementation will cause `AllowRequest_AtLimitBoundary_ReturnsFalseForExtraRequest` to fail.
- **Staleness semantics:** A fresh instance with a non-zero timeout is considered stale until the first request arrives. This implies that staleness is tied to activity, not merely elapsed wall-clock time. A zero or negative timeout may behave differently, though such cases are not covered by these signatures.
- **Thread safety:** `AllowRequest_ConcurrentAccess_DoesNotThrowOrCorruptState` indicates that the rate-limiter must handle overlapping calls without internal corruption. The test expects deterministic final counts, so the implementation likely uses atomic operations or fine-grained locking.
- **Count accuracy:** `GetRequestCount` is expected to return the number of *allowed* requests, not total attempts. Denied requests should not increment the counter.
- **Test isolation:** Each test method operates on its own rate-limiter instance, ensuring no shared state between scenarios.
