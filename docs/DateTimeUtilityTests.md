# DateTimeUtilityTests

The `DateTimeUtilityTests` class serves as the dedicated test suite for validating the correctness and reliability of date and time manipulation logic within the `grpc-web-bridge` project. It contains a collection of unit tests designed to verify specific behaviors of utility methods, including Unix timestamp conversions, relative time formatting, business day calculations, and weekend detection, ensuring that temporal operations function as expected across various edge cases and standard scenarios.

## API

### `ToUnixTimestamp_WithUnixEpoch_ReturnsZero`
Verifies that converting the Unix Epoch (January 1, 1970, 00:00:00 UTC) to a Unix timestamp results in zero.
*   **Parameters**: None (uses internal test fixtures).
*   **Return Value**: `void` (asserts equality within the test framework).
*   **Throws**: Throws an assertion exception if the calculated timestamp does not equal `0`.

### `FromUnixTimestamp_WithZero_ReturnsUnixEpoch`
Validates that converting a Unix timestamp of zero back to a `DateTime` object yields the exact Unix Epoch start time.
*   **Parameters**: None (uses internal test fixtures).
*   **Return Value**: `void` (asserts equality within the test framework).
*   **Throws**: Throws an assertion exception if the resulting `DateTime` is not January 1, 1970, 00:00:00 UTC.

### `ToRelativeTime_WithinOneMinute_ReturnsJustNow`
Ensures that a `DateTime` occurring within the last 60 seconds is formatted as "Just now" (or equivalent localized string) when passed to the relative time utility.
*   **Parameters**: None (uses internal test fixtures with mocked current time).
*   **Return Value**: `void` (asserts string equality within the test framework).
*   **Throws**: Throws an assertion exception if the output string differs from the expected "Just now" representation.

### `GetBusinessDaysBetween_MondayToFriday_ReturnsFive`
Confirms that calculating the number of business days between a Monday and the following Friday (inclusive) returns exactly five days.
*   **Parameters**: None (uses internal test fixtures).
*   **Return Value**: `void` (asserts integer equality within the test framework).
*   **Throws**: Throws an assertion exception if the count is not `5`.

### `GetBusinessDaysBetween_AcrossWeekend_ExcludesSaturdayAndSunday`
Tests that the business day calculation logic correctly skips Saturday and Sunday when the date range spans across a weekend.
*   **Parameters**: None (uses internal test fixtures covering a multi-week range).
*   **Return Value**: `void` (asserts integer equality within the test framework).
*   **Throws**: Throws an assertion exception if weekend days are incorrectly included in the count.

### `IsWeekend_WithSaturday_ReturnsTrue`
Verifies that the weekend detection utility correctly identifies a Saturday as a weekend day.
*   **Parameters**: None (uses internal test fixtures).
*   **Return Value**: `void` (asserts boolean truth within the test framework).
*   **Throws**: Throws an assertion exception if the method returns `false` for a Saturday input.

## Usage

The following examples demonstrate how the logic verified by this test suite is typically consumed in application code, reflecting the scenarios covered by the tests.

```csharp
// Example 1: Validating timestamp conversion logic
// Corresponds to: ToUnixTimestamp_WithUnixEpoch_ReturnsZero 
// and FromUnixTimestamp_WithZero_ReturnsUnixEpoch
public void ProcessLegacyTimestamp(long legacyTimestamp)
{
    if (legacyTimestamp == 0)
    {
        // Ensure zero maps strictly to the Unix Epoch before processing
        var epoch = DateTimeUtility.FromUnixTimestamp(0);
        if (epoch != new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        {
            throw new InvalidOperationException("Timestamp zero mapping failed.");
        }
    }
    
    var convertedDate = DateTimeUtility.FromUnixTimestamp(legacyTimestamp);
    // Proceed with business logic using convertedDate
}
```

```csharp
// Example 2: Calculating SLA deadlines excluding weekends
// Corresponds to: GetBusinessDaysBetween_* and IsWeekend_* tests
public DateTime CalculateSlaDeadline(DateTime startDate, int businessDaysAllowed)
{
    int daysAdded = 0;
    DateTime currentDate = startDate;

    while (daysAdded < businessDaysAllowed)
    {
        currentDate = currentDate.AddDays(1);
        if (!DateTimeUtility.IsWeekend(currentDate))
        {
            daysAdded++;
        }
    }

    return currentDate;
}
```

## Notes

*   **Edge Cases**: The tests explicitly cover the boundary condition of the Unix Epoch (timestamp 0) and the transition across weekends. Implementations relying on these utilities should ensure that input dates are normalized to UTC before conversion to avoid timezone-related offsets during timestamp serialization.
*   **Thread Safety**: As this class consists entirely of unit test methods verifying stateless utility functions, the tests themselves are designed to be independent and side-effect free. The underlying utilities being tested should be assumed thread-safe if they perform only read-only calculations on value types (`DateTime`, `long`, `int`), which is implied by the deterministic nature of the assertions in this suite.
*   **Dependencies**: These tests assume the system clock can be controlled or mocked (particularly for `ToRelativeTime_WithinOneMinute_ReturnsJustNow`) to ensure deterministic results regardless of the actual execution time of the test suite.
