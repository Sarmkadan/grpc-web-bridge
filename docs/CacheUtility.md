# CacheUtility

Utility class providing cache key generation, validation, and statistics for gRPC-Web bridge caching mechanisms. It supports namespaced keys, pattern matching, size estimation, and detailed cache metrics.

## API

### `public static string GenerateKey(params string[] components)`
Generates a cache key by joining the provided components with the default separator (`:`). Empty components are ignored.

- **Parameters**
  - `components`: Variable list of strings to join into a key.
- **Returns**
  - A sanitized, joined string representing the cache key.
- **Throws**
  - `ArgumentNullException`: If `components` is `null`.

---

### `public static string GenerateNamespacedKey(string @namespace, params string[] components)`
Generates a namespaced cache key by prefixing the components with the given namespace.

- **Parameters**
  - `@namespace`: The namespace to prefix the key with.
  - `components`: Variable list of strings to append after the namespace.
- **Returns**
  - A sanitized, joined string with the namespace as the first component.
- **Throws**
  - `ArgumentNullException`: If `@namespace` or `components` is `null`.

---

### `public static string SanitizeKeyComponent(string component)`
Sanitizes a single key component by replacing invalid characters with underscores.

- **Parameters**
  - `component`: The component to sanitize.
- **Returns**
  - A sanitized version of the input string.
- **Throws**
  - `ArgumentNullException`: If `component` is `null`.

---

### `public static string CreatePatternKey(string @namespace, params string[] components)`
Creates a pattern-based cache key with wildcard support for matching multiple keys.

- **Parameters**
  - `@namespace`: The namespace to prefix the pattern with.
  - `components`: Variable list of strings to include in the pattern; use `*` as a wildcard.
- **Returns**
  - A sanitized pattern string with wildcards preserved.
- **Throws**
  - `ArgumentNullException`: If `@namespace` or `components` is `null`.

---
### `public static bool MatchesPattern(string key, string pattern)`
Determines if a cache key matches a given pattern containing wildcards.

- **Parameters**
  - `key`: The cache key to test.
  - `pattern`: The pattern to match against, supporting `*` wildcards.
- **Returns**
  - `true` if the key matches the pattern; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `key` or `pattern` is `null`.

---
### `public static int GetKeyHash(string key)`
Computes a hash code for the given cache key.

- **Parameters**
  - `key`: The cache key to hash.
- **Returns**
  - A 32-bit signed integer hash code.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.

---
### `public static long EstimateKeySize(string key)`
Estimates the approximate byte size of a cache key in UTF-8 encoding.

- **Parameters**
  - `key`: The cache key to estimate.
- **Returns**
  - The estimated size in bytes.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.

---
### `public static string[] ParseKey(string key)`
Parses a cache key into its components using the default separator (`:`).

- **Parameters**
  - `key`: The cache key to parse.
- **Returns**
  - An array of string components.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.

---
### `public static string GenerateMethodCacheKey(string service, string method, params string[] args)`
Generates a cache key for a gRPC method call, combining service, method, and arguments.

- **Parameters**
  - `service`: The gRPC service name.
  - `method`: The gRPC method name.
  - `args`: Variable list of argument strings to include in the key.
- **Returns**
  - A sanitized, joined cache key.
- **Throws**
  - `ArgumentNullException`: If `service`, `method`, or `args` is `null`.

---
### `public static string GenerateStreamCacheKey(string service, string method, string streamId, params string[] args)`
Generates a cache key for a streaming gRPC call, including a stream identifier.

- **Parameters**
  - `service`: The gRPC service name.
  - `method`: The gRPC method name.
  - `streamId`: A unique identifier for the stream.
  - `args`: Variable list of additional arguments.
- **Returns**
  - A sanitized, joined cache key.
- **Throws**
  - `ArgumentNullException`: If any parameter is `null`.

---
### `public static string GenerateServiceCacheKey(string service)`
Generates a cache key for an entire gRPC service.

- **Parameters**
  - `service`: The gRPC service name.
- **Returns**
  - A sanitized cache key representing the service.
- **Throws**
  - `ArgumentNullException`: If `service` is `null`.

---
### `public static string GenerateAuthCacheKey(string userId, string tokenHash)`
Generates a cache key for authentication-related data.

- **Parameters**
  - `userId`: The user identifier.
  - `tokenHash`: A hash of the authentication token.
- **Returns**
  - A sanitized cache key.
- **Throws**
  - `ArgumentNullException`: If `userId` or `tokenHash` is `null`.

---
### `public static bool IsValidKey(string key)`
Validates whether a string is a well-formed cache key.

- **Parameters**
  - `key`: The key to validate.
- **Returns**
  - `true` if the key is valid; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.

---
### `public static string FormatKeyForDebug(string key)`
Formats a cache key for display in debug output, truncating long keys.

- **Parameters**
  - `key`: The key to format.
- **Returns**
  - A display-friendly version of the key.
- **Throws**
  - `ArgumentNullException`: If `key` is `null`.

---
### `public long TotalKeysGenerated`
Gets the total number of cache keys generated by this utility.

- **Type**
  - `long`
- **Access**
  - Read-only

---
### `public long TotalCacheHits`
Gets the total number of cache hits recorded.

- **Type**
  - `long`
- **Access**
  - Read-only

---
### `public long TotalCacheMisses`
Gets the total number of cache misses recorded.

- **Type**
  - `long`
- **Access**
  - Read-only

---
### `public long TotalMemoryUsed`
Gets the estimated total memory used by all cache keys generated.

- **Type**
  - `long`
- **Access**
  - Read-only

---
### `public Dictionary<string, long> KeysByNamespace`
Gets a dictionary mapping namespaces to the count of keys generated within each.

- **Type**
  - `Dictionary<string, long>`
- **Access**
  - Read-only
