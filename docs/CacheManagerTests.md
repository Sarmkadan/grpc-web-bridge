# CacheManagerTests

The `CacheManagerTests` class serves as the comprehensive test suite for validating the functionality, reliability, and edge-case handling of the `CacheManager` component within the `grpc-web-bridge` project. It encapsulates a series of unit tests designed to verify correct behavior regarding data insertion, retrieval, expiration logic, key validation, and bulk removal operations, ensuring the caching layer maintains data integrity and performs as expected under various conditions.

## API

### Constructors

#### `public CacheManagerTests()`
Initializes a new instance of the `CacheManagerTests` class. This constructor typically sets up the necessary test context or instantiates the `CacheManager` instance required for subsequent test methods.

### Methods

#### `public void Dispose()`
Releases unmanaged resources and performs cleanup operations associated with the test instance. This method is invoked to ensure that any stateful resources, such as the cache instance or mocked dependencies, are properly reset or disposed of after test execution to prevent interference between test cases.

#### `public void Set_AndTryGet_WithSameKey_ReturnsCachedValue()`
Verifies that a value stored using a specific key can be successfully retrieved using the same key. This test confirms the basic write-and-read cycle of the cache, ensuring that the stored object reference or value matches the retrieved result.

#### `public void TryGet_WithNonExistentKey_ReturnsFalse()`
Validates that attempting to retrieve a value using a key that has never been added to the cache returns `false` (or the equivalent failure state for a `TryGet` pattern) and does not throw an exception.

#### `public void Set_OverwritesExistingKey()`
Ensures that calling the set operation with a key that already exists updates the associated value to the new provided value, rather than appending a duplicate entry or throwing an error.

#### `public void Set_WithNullValue_ThrowsArgumentNullException()`
Confirms that the cache implementation strictly enforces non-null values. Passing a `null` value to the set method must result in an `ArgumentNullException` being thrown.

#### `public void Set_WithEmptyKey_ThrowsArgumentException()`
Validates input sanitization for keys. Attempting to store a value with an empty string (`""`) as the key must result in an `ArgumentException`.

#### `public void TryGet_AfterExpiry_ReturnsFalse()`
Tests the time-to-live (TTL) mechanism. It verifies that a key-value pair that has exceeded its configured expiration duration is no longer retrievable via `TryGet`, returning `false`.

#### `public void Contains_AfterExpiry_ReturnsFalse()`
Similar to the expiry retrieval test, this ensures that the `Contains` method accurately reflects the state of expired entries, returning `false` for keys that have passed their TTL.

#### `public void GetTimeToLive_ForFreshEntry_ReturnsPositiveDuration()`
Checks that querying the remaining time-to-live for a recently added entry returns a positive `TimeSpan` value, indicating the entry is active and has not expired.

#### `public void GetTimeToLive_ForMissingKey_ReturnsNull()`
Verifies that requesting the TTL for a key that does not exist in the cache returns `null`, distinguishing missing keys from expired or present ones.

#### `public void GetTimeToLive_ForEmptyKey_ReturnsNull()`
Ensures that requesting the TTL with an empty string key returns `null` without throwing an exception, maintaining consistent behavior for invalid lookups.

#### `public void Contains_WithExistingKey_ReturnsTrue()`
Confirms that the `Contains` method returns `true` when queried with a valid, non-expired key currently stored in the cache.

#### `public void Contains_WithMissingKey_ReturnsFalse()`
Validates that `Contains` returns `false` for a key that has never been added to the cache.

#### `public void Contains_WithEmptyKey_ReturnsFalse()`
Ensures that checking for an empty string key returns `false` rather than throwing an exception or returning an ambiguous state.

#### `public void Remove_ExistingKey_ReturnsTrueAndKeyIsGone()`
Tests the removal logic for valid keys. It asserts that removing an existing key returns `true` and that subsequent attempts to retrieve or find that key fail.

#### `public void Remove_NonExistentKey_ReturnsFalse()`
Validates that attempting to remove a key that does not exist returns `false` and does not alter the state of the cache or throw an exception.

#### `public void Remove_WithEmptyKey_ReturnsFalse()`
Ensures that attempting to remove an entry using an empty string key returns `false` safely.

#### `public void Clear_RemovesAllEntries()`
Verifies that the `Clear` operation empties the entire cache, ensuring that no keys remain accessible and the count of entries drops to zero.

#### `public void RemovePattern_WithMatchingPrefix_RemovesMatchingEntries()`
Tests bulk removal capabilities based on key patterns. It confirms that providing a specific prefix removes all entries whose keys start with that prefix while leaving unrelated entries intact.

## Usage

The following examples demonstrate how the `CacheManagerTests` class validates specific behaviors within a testing framework context (e.g., xUnit or NUnit).

### Example 1: Verifying Basic Lifecycle and Expiration
This example illustrates the logical flow tested by `Set_AndTryGet_WithSameKey_ReturnsCachedValue` and `TryGet_AfterExpiry_ReturnsFalse`, showing the expected behavior of the underlying cache being tested.

```csharp
using System;
using System.Threading;

public class CacheLifecycleExample
{
    public void ValidateExpirationLogic()
    {
        // Arrange
        var cache = new CacheManager(TimeSpan.FromMilliseconds(100));
        string key = "user_session_123";
        var value = new { UserId = 1, Role = "Admin" };

        // Act: Set value
        cache.Set(key, value);

        // Assert: Immediate retrieval succeeds
        if (!cache.TryGet(key, out var retrieved))
        {
            throw new Exception("Failed to retrieve fresh value");
        }

        // Wait for expiration
        Thread.Sleep(150);

        // Assert: Retrieval after expiry fails
        if (cache.TryGet(key, out _))
        {
            throw new Exception("Value should have expired");
        }
    }
}
```

### Example 2: Validating Input Constraints and Pattern Removal
This example reflects the logic covered by `Set_WithNullValue_ThrowsArgumentNullException` and `RemovePattern_WithMatchingPrefix_RemovesMatchingEntries`.

```csharp
using System;
using System.Collections.Generic;

public class CacheConstraintsExample
{
    public void ValidateConstraintsAndBulkRemove()
    {
        var cache = new CacheManager();
        
        // Test Null Value Constraint
        try
        {
            cache.Set("invalid_key", null); 
            throw new Exception("Expected ArgumentNullException was not thrown");
        }
        catch (ArgumentNullException)
        {
            // Expected behavior
        }

        // Setup for Pattern Removal
        cache.Set("temp_data_01", "Value1");
        cache.Set("temp_data_02", "Value2");
        cache.Set("permanent_config", "Value3");

        // Act: Remove by prefix
        int removedCount = cache.RemovePattern("temp_data_");

        // Assert results
        if (removedCount != 2)
        {
            throw new Exception("Incorrect number of items removed");
        }
        
        if (cache.Contains("permanent_config") == false)
        {
            throw new Exception("Non-matching key was incorrectly removed");
        }
    }
}
```

## Notes

*   **Thread Safety**: While the test suite validates logical correctness, the specific signatures (e.g., separate `Set`, `Get`, `Remove` calls) imply that the underlying `CacheManager` implementation must handle concurrent access if used in a multi-threaded environment. The tests themselves typically run in isolation; however, the `Clear` and `RemovePattern` operations suggest potential race conditions if accessed simultaneously with write operations in a live system without internal locking mechanisms.
*   **Key Validation**: The explicit tests for empty keys (`Set_WithEmptyKey_ThrowsArgumentException`, `Contains_WithEmptyKey_ReturnsFalse`, etc.) indicate that the cache treats empty strings as invalid identifiers for storage but handles them gracefully during read/check operations by returning negative results rather than crashing.
*   **Null Handling**: There is a strict distinction between missing keys and null values. The cache prohibits storing `null` values entirely (`ArgumentNullException`), whereas missing keys return `false` or `null` depending on the method signature (`TryGet` vs `GetTimeToLive`).
*   **Expiration Granularity**: Tests like `TryGet_AfterExpiry_ReturnsFalse` rely on time passage. In real-world usage, the precision of expiration depends on the system clock and the frequency of the cache's internal cleanup cycle, which may introduce slight variances between the exact TTL moment and the moment the key becomes unavailable.
