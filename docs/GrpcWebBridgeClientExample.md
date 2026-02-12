# GrpcWebBridgeClientExample

The `GrpcWebBridgeClientExample` class serves as a client implementation for interacting with the gRPC-Web Bridge service. It provides methods to check service health, list available services, register services, invoke service methods, and retrieve performance metrics. This class is designed to facilitate communication between a client application and a gRPC-Web Bridge server, handling both unary and streaming calls while tracking request statistics.

## API

### `GrpcWebBridgeClientExample`
**Constructor**
Initializes a new instance of the `GrpcWebBridgeClientExample` client with the specified service name and target address.

**Parameters**
- `serviceName` (string): The name of the service being interacted with.
- `address` (string): The server address (e.g., `http://localhost:5000`).

---

### `Task<bool> CheckHealthAsync()`
Checks the health status of the connected gRPC-Web Bridge service.

**Returns**
- `Task<bool>`: `true` if the service is healthy; otherwise, `false`.

**Throws**
- `Grpc.Core.RpcException`: If the health check request fails due to network or server errors.

---

### `Task<List<ServiceInfo>?> ListServicesAsync()`
Retrieves a list of services registered with the gRPC-Web Bridge.

**Returns**
- `Task<List<ServiceInfo>?>`: A list of `ServiceInfo` objects representing available services, or `null` if the request fails.

**Throws**
- `Grpc.Core.RpcException`: If the request fails due to network or server errors.

---

### `Task<bool> RegisterServiceAsync()`
Registers the current service with the gRPC-Web Bridge.

**Returns**
- `Task<bool>`: `true` if registration succeeds; otherwise, `false`.

**Throws**
- `Grpc.Core.RpcException`: If registration fails due to network or server errors.

---

### `Task<T?> CallServiceAsync<T>()`
Invokes a unary service method and deserializes the response into the specified type.

**Type Parameters**
- `T`: The expected response type.

**Returns**
- `Task<T?>`: The deserialized response, or `null` if the call fails.

**Throws**
- `Grpc.Core.RpcException`: If the service call fails due to network or server errors.
- `InvalidOperationException`: If the response cannot be deserialized into `T`.

---

### `Task<MetricsInfo?> GetMetricsAsync()`
Retrieves performance metrics for the client, including request counts and latency percentiles.

**Returns**
- `Task<MetricsInfo?>`: A `MetricsInfo` object containing metrics, or `null` if retrieval fails.

**Throws**
- `Grpc.Core.RpcException`: If the metrics request fails due to network or server errors.

---

### `Task<int> GetActiveStreamCountAsync()`
Returns the number of currently active streaming connections.

**Returns**
- `Task<int>`: The count of active streams.

**Throws**
- `Grpc.Core.RpcException`: If the request fails due to network or server errors.

---

### `Task<T?> CallWithRetryAsync<T>()`
Invokes a service method with automatic retry logic for transient failures.

**Type Parameters**
- `T`: The expected response type.

**Returns**
- `Task<T?>`: The deserialized response, or `null` if all retry attempts fail.

**Throws**
- `Grpc.Core.RpcException`: If all retry attempts fail due to persistent errors.
- `InvalidOperationException`: If the response cannot be deserialized into `T`.

---

### `Task<bool> ProcessBatchAsync<T>()`
Processes a batch of service calls, aggregating results or handling failures.

**Type Parameters**
- `T`: The expected response type for each batch item.

**Returns**
- `Task<bool>`: `true` if the batch completes successfully; otherwise, `false`.

**Throws**
- `Grpc.Core.RpcException`: If batch processing fails due to network or server errors.

---

### `string ServiceName`
Gets the name of the service associated with this client.

---

### `string Address`
Gets the server address the client is connected to.

---

### `string Status`
Gets the current connection status (e.g., `"Connected"`, `"Disconnected"`).

---

### `Dictionary<string, string> Metadata`
Gets or sets additional metadata sent with each request (e.g., authentication tokens).

---

### `int TotalRequests`
Gets the total number of requests sent by this client.

---

### `int SuccessfulRequests`
Gets the number of successful requests.

---

### `int FailedRequests`
Gets the number of failed requests.

---

### `double AverageLatencyMs`
Gets the average latency of requests in milliseconds.

---

### `double P95LatencyMs`
Gets the 95th percentile latency of requests in milliseconds.

---

### `double P99LatencyMs`
Gets the 99th percentile latency of requests in milliseconds.

---

### `int ActiveStreams`
Gets the number of currently active streaming connections.

## Usage

### Example 1: Basic Service Interaction
