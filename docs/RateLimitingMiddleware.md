# RateLimitingMiddleware

A middleware component for ASP.NET Core applications that enforces rate limiting based on configurable per-client and global request thresholds. It tracks request counts within sliding windows and can reject or delay requests when limits are exceeded.

## API

### `RateLimitingMiddleware`
Initializes a new instance of the rate limiting middleware with the specified configuration.

- **Parameters**
  - `options`: Configuration options including window size, request limits, and global limits.

### `async Task InvokeAsync(HttpContext context, RequestDelegate next)`
Invokes the middleware pipeline, applying rate limiting logic before passing control to the next middleware.

- **Parameters**
  - `context`: The HTTP context for the current request.
  - `next`: The delegate representing the next middleware in the pipeline.
- **Returns**
  - A `Task` representing the asynchronous operation.
- **Throws**
  - `ArgumentNullException`: If `context` or `next` is `null`.

### `bool AllowRequest(string clientId)`
Determines whether a request from the specified client is allowed based on the configured rate limits.

- **Parameters**
  - `clientId`: A unique identifier for the client (e.g., IP address or API key).
- **Returns**
  - `true` if the request is allowed; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `clientId` is `null` or empty.

### `int GetRequestCount(string clientId)`
Retrieves the number of requests made by the specified client within the current window.

- **Parameters**
  - `clientId`: A unique identifier for the client.
- **Returns**
  - The number of requests made by the client in the current window.
- **Throws**
  - `ArgumentNullException`: If `clientId` is `null` or empty.

### `bool IsStale(string clientId)`
Checks whether the rate limiting data for the specified client has expired and should be reset.

- **Parameters**
  - `clientId`: A unique identifier for the client.
- **Returns**
  - `true` if the client's data is stale and should be reset; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `clientId` is `null` or empty.

### `int RequestsPerSecond`
Gets or sets the maximum number of requests allowed per second per client.

- **Type**: `int`
- **Default**: `100`

### `int WindowSizeSeconds`
Gets or sets the duration (in seconds) of the sliding window for tracking requests.

- **Type**: `int`
- **Default**: `60`

### `int RetryAfterSeconds`
Gets or sets the number of seconds to include in the `Retry-After` header when a request is rejected due to rate limiting.

- **Type**: `int`
- **Default**: `5`

### `bool EnableGlobalLimit`
Gets or sets a value indicating whether a global rate limit should be enforced across all clients.

- **Type**: `bool`
- **Default**: `false`

### `int GlobalRequestsPerSecond`
Gets or sets the maximum number of requests allowed per second across all clients when global limiting is enabled.

- **Type**: `int`
- **Default**: `1000`

### `static IApplicationBuilder UseRateLimiting(IApplicationBuilder app, RateLimitingOptions options)`
Registers the rate limiting middleware with the ASP.NET Core pipeline.

- **Parameters**
  - `app`: The application builder.
  - `options`: Configuration options for the middleware.
- **Returns**
  - The application builder for method chaining.
- **Throws**
  - `ArgumentNullException`: If `app` or `options` is `null`.

## Usage

### Basic Setup
