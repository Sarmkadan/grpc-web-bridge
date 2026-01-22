# CacheManager

A thread-safe in-memory cache manager for .NET applications, providing generic typed access, expiration policies, and statistics tracking. Designed for scenarios requiring coordinated cache access across components, such as gRPC-web bridges or microservice communication layers.

## API

### `public CacheManager()`

Initializes a new instance of the `CacheManager` with default settings: no automatic expiration, unlimited size, and disabled statistics collection.

### `public void Set<T>(string key, T value)`

Stores the specified `value` in the cache under the given `key`.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
  - `value`: The object to store in the cache.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.
  - Throws `ArgumentNullException` if `value` is `null`.

### `public void Set<T>(string key, T value, TimeSpan? expiration)`

Stores the specified `value` in the cache under the given `key` with an optional expiration policy.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
  - `value`: The object to store in the cache.
  - `expiration`: The time span after which the entry expires. If `null`, the entry does not expire.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.
  - Throws `ArgumentNullException` if `value` is `null`.

### `public bool TryGet<T>(string key, out T value)`

Retrieves the value associated with the specified `key`, if present and not expired.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
  - `value`: When this method returns, contains the object from the cache if found; otherwise, the default value for type `T`.
- **Return Value**
  - `true` if the value was found and is valid; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.

### `public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> valueFactory, TimeSpan? expiration = null)`

Retrieves the value associated with the specified `key` if it exists and is not expired; otherwise, asynchronously computes, stores, and returns the value.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
  - `valueFactory`: A function to compute the value if it is not in the cache.
  - `expiration`: Optional expiration time span for the new entry.
- **Return Value**
  - A task that represents the asynchronous operation. The task result contains the cached or newly computed value.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.
  - Throws `ArgumentNullException` if `valueFactory` is `null`.
  - Propagates any exception thrown by `valueFactory`.

### `public T GetOrSet<T>(string key, Func<T> valueFactory, TimeSpan? expiration = null)`

Synchronous variant of `GetOrSetAsync`. Retrieves the value associated with the specified `key` if it exists and is not expired; otherwise, computes, stores, and returns the value.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
  - `valueFactory`: A function to compute the value if it is not in the cache.
  - `expiration`: Optional expiration time span for the new entry.
- **Return Value**
  - The cached or newly computed value.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.
  - Throws `ArgumentNullException` if `valueFactory` is `null`.

### `public bool Remove(string key)`

Removes the entry with the specified `key` from the cache.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
- **Return Value**
  - `true` if the entry was found and removed; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.

### `public int RemovePattern(string pattern)`

Removes all entries whose keys match the specified regex-style pattern.

- **Parameters**
  - `pattern`: A regex pattern used to match keys.
- **Return Value**
  - The number of entries removed.
- **Exceptions**
  - Throws `ArgumentNullException` if `pattern` is `null`.

### `public void Clear()`

Removes all entries from the cache.

### `public CacheStatistics GetStatistics()`

Returns a snapshot of current cache statistics.

- **Return Value**
  - A `CacheStatistics` object containing entry count, hit count, and other metrics.

### `public bool Contains(string key)`

Determines whether the cache contains an entry with the specified `key`.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
- **Return Value**
  - `true` if the entry exists and is not expired; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.

### `public bool SetExpiration(string key, TimeSpan? expiration)`

Updates the expiration policy for the entry with the specified `key`.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
  - `expiration`: The new expiration time span. If `null`, the entry does not expire.
- **Return Value**
  - `true` if the entry was found and updated; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.

### `public TimeSpan? GetTimeToLive(string key)`

Returns the remaining time until the entry with the specified `key` expires.

- **Parameters**
  - `key`: The unique identifier for the cache entry.
- **Return Value**
  - The remaining time span until expiration, or `null` if the entry does not expire or does not exist.
- **Exceptions**
  - Throws `ArgumentNullException` if `key` is `null`.

### `public void Dispose()`

Releases all resources used by the `CacheManager`.

### `public object? Value`

Gets the cached value for the current entry. Only valid when accessed via an active enumeration or internal context.

### `public DateTime ExpiresAt`

Gets the absolute expiration timestamp for the current entry. Only valid when accessed via an active enumeration or internal context.

### `public DateTime CreatedAt`

Gets the creation timestamp for the current entry. Only valid when accessed via an active enumeration or internal context.

### `public DateTime? LastAccessedAt`

Gets the last access timestamp for the current entry. Only valid when accessed via an active enumeration or internal context.

### `public long HitCount`

Gets the number of times the current entry has been accessed. Only valid when accessed via an active enumeration or internal context.

### `public int EntryCount`

Gets the total number of entries in the cache. Only valid when accessed via `GetStatistics()`.

## Usage
