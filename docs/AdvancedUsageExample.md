# AdvancedUsageExample

A utility class that demonstrates advanced usage patterns for gRPC-Web bridge operations, including resilience, circuit breaking, streaming, metrics collection, and batch processing. Designed for scenarios requiring robust error handling, performance monitoring, and operational observability.

## API

### `AdvancedUsageExample`
The main entry point for advanced gRPC-Web operations. Initializes internal state for tracking metrics, errors, and resilience features.

### `public async Task<HealthStatus> CheckHealthWithCircuitBreakerAsync()`
Checks the health of the underlying gRPC-Web bridge using a circuit breaker pattern to prevent cascading failures. The circuit breaker trips after a configurable number of consecutive failures and remains open for a cooldown period.

- **Returns**: `Task<HealthStatus>` – A task that resolves to a `HealthStatus` indicating whether the service is healthy, degraded, or unavailable.
- **Throws**: `InvalidOperationException` – If the circuit breaker is in a tripped state and the cooldown period has not elapsed.

### `public async Task<T?> CallWithResilienceAsync<T>(Func<Task<T>> operation, string operationName)`
Executes an asynchronous gRPC-Web operation with resilience policies including retry, timeout, and fallback. Captures latency and success metrics for observability.

- **Parameters**:
  - `operation` (`Func<Task<T>>`) – The gRPC-Web call to execute.
  - `operationName` (`string`) – A descriptive name for the operation used in logging and metrics.
- **Returns**: `Task<T?>` – The result of the operation, or `null` if resilience policies triggered fallback or all retries failed.
- **Throws**: `OperationCanceledException` – If the operation times out or is canceled.
- **Throws**: `InvalidOperationException` – If the fallback value cannot be computed or is invalid.

### `public async Task<int> StreamDataWithProgressAsync<T>(IAsyncEnumerable<T> dataStream, Func<T, Task> processItem, IProgress<int> progress)`
Streams data from a gRPC-Web source and processes each item with progress reporting. Tracks total items processed and maintains a running count of successful and failed operations.

- **Parameters**:
  - `dataStream` (`IAsyncEnumerable<T>`) – The asynchronous stream of data items.
  - `processItem` (`Func<T, Task>`) – A function to process each item.
  - `progress` (`IProgress<int>`) – A progress reporter to notify of completion percentage.
- **Returns**: `Task<int>` – The total number of items successfully processed.
- **Throws**: `ArgumentNullException` – If `dataStream`, `processItem`, or `progress` is `null`.
- **Throws**: `InvalidOperationException` – If the stream is empty or processing fails irrecoverably.

### `public async Task<BridgeMetrics?> GetDetailedMetricsAsync()`
Retrieves a snapshot of detailed performance and operational metrics collected during gRPC-Web operations. Includes latency percentiles, success rates, error counts, and streaming statistics.

- **Returns**: `Task<BridgeMetrics?>` – A task resolving to a `BridgeMetrics` object containing detailed statistics, or `null` if no metrics are available.
- **Throws**: None – Operation is read-only and does not throw under normal conditions.

### `public async Task<RegistrationResult> RegisterServiceWithValidationAsync(string serviceName, Func<Task<bool>> validationCallback)`
Registers a gRPC-Web service with pre-registration validation. Validates service availability and compatibility before allowing registration. Supports conditional registration based on runtime checks.

- **Parameters**:
  - `serviceName` (`string`) – The name of the service to register.
  - `validationCallback` (`Func<Task<bool>>`) – A function that returns `true` if the service is valid for registration.
- **Returns**: `Task<RegistrationResult>` – A task resolving to a `RegistrationResult` indicating success or failure with a reason.
- **Throws**: `ArgumentException` – If `serviceName` is null or empty.
- **Throws**: `InvalidOperationException` – If the service is already registered or validation fails.

### `public async Task<BatchResult> ExecuteBatchOperationAsync<T>(IEnumerable<T> items, Func<T, Task> operation, int batchSize)`
Executes a batch of gRPC-Web operations in parallel with configurable batch size. Tracks success/failure per item and aggregates results. Designed for high-throughput scenarios.

- **Parameters**:
  - `items` (`IEnumerable<T>`) – The collection of items to process.
  - `operation` (`Func<T, Task>`) – The operation to apply to each item.
  - `batchSize` (`int`) – The maximum number of concurrent operations.
- **Returns**: `Task<BatchResult>` – A task resolving to a `BatchResult` containing per-item outcomes and summary statistics.
- **Throws**: `ArgumentNullException` – If `items` or `operation` is `null`.
- **Throws**: `ArgumentOutOfRangeException` – If `batchSize` is less than 1.

### `public int SuccessfulCount`
Gets the total number of successful operations processed by the instance. Includes both streaming and batch operations.

### `public int FailedCount`
Gets the total number of failed operations processed by the instance. Includes both streaming and batch operations.

### `public int TotalItemsProcessed`
Gets the total number of items processed across all streaming and batch operations.

### `public List<string> Errors`
Gets a list of error messages collected during operation execution. Thread-safe for concurrent reads; modifications occur only during operation failures.

### `public double SuccessRate`
Gets the overall success rate of operations as a value between 0.0 and 1.0. Calculated as `SuccessfulRequests / TotalRequests`.

### `public int TotalRequests`
Gets the total number of gRPC-Web requests initiated by the instance.

### `public int SuccessfulRequests`
Gets the total number of successful gRPC-Web requests.

### `public int FailedRequests`
Gets the total number of failed gRPC-Web requests.

### `public double AverageLatencyMs`
Gets the average latency of all completed requests in milliseconds.

### `public double P95LatencyMs`
Gets the 95th percentile latency of all completed requests in milliseconds.

### `public double P99LatencyMs`
Gets the 99th percentile latency of all completed requests in milliseconds.

### `public int ActiveStreams`
Gets the current number of active streaming operations.

### `public double CacheHitRate`
Gets the cache hit rate as a value between 0.0 and 1.0, representing the proportion of cache hits over total cache accesses.

## Usage
