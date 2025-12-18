# RequestContextManager

The `RequestContextManager` is a utility class designed to manage request-scoped contextual information within the `grpc-web-bridge` project. It provides mechanisms to create, retrieve, and manipulate request-specific data such as request IDs, user IDs, metadata, and timing metrics. This class is typically used in middleware or service layers to track and correlate request processing details across distributed systems.

## API

### `public RequestContextManager`
The constructor initializes a new instance of `RequestContextManager`. No parameters are required, and it does not throw exceptions.

---

### `public RequestContext CreateContext()`
Creates and returns a new `RequestContext` instance, initializing request-scoped properties such as `RequestId`, `UserId`, and `StartTime`.

**Returns:**
- A `RequestContext` object representing the current request context.

**Throws:**
- None.

---

### `public RequestContext? GetContext()`
Retrieves the active `RequestContext` for the current request, if one exists.

**Returns:**
- The active `RequestContext` if available; otherwise, `null`.

**Throws:**
- None.

---

### `public string? GetRequestId()`
Retrieves the request ID associated with the current request context.

**Returns:**
- The request ID as a `string` if the context is active; otherwise, `null`.

**Throws:**
- None.

---

### `public string? GetUserId()`
Retrieves the user ID associated with the current request context.

**Returns:**
- The user ID as a `string` if the context is active; otherwise, `null`.

**Throws:**
- None.

---

### `public void SetMetadata(Dictionary<string, string> metadata)`
Sets the metadata dictionary for the current request context. Overwrites any existing metadata.

**Parameters:**
- `metadata`: A `Dictionary<string, string>` containing key-value pairs of metadata.

**Throws:**
- `ArgumentNullException` if `metadata` is `null`.

---

### `public string? GetMetadata(string key)`
Retrieves the value associated with the specified key from the metadata dictionary.

**Parameters:**
- `key`: The key of the metadata entry to retrieve.

**Returns:**
- The metadata value as a `string` if the key exists; otherwise, `null`.

**Throws:**
- `ArgumentNullException` if `key` is `null`.

---

### `public void RecordElapsedTime()`
Records the end time of the current request context, calculating and storing the elapsed time since `StartTime`.

**Throws:**
- None.

---

### `public void Clear()`
Clears the current request context, resetting all properties to their default values.

**Throws:**
- None.

---

### `public bool IsContextActive`
Indicates whether a request context is currently active.

**Returns:**
- `true` if a context is active; otherwise, `false`.

**Throws:**
- None.

---

### `public string RequestId`
Gets or sets the request ID for the current context.

**Exceptions:**
- Throws `InvalidOperationException` if no context is active during a set operation.

---

### `public string? UserId`
Gets or sets the user ID for the current context.

**Exceptions:**
- Throws `InvalidOperationException` if no context is active during a set operation.

---

### `public DateTime StartTime`
Gets the start time of the current request context.

**Exceptions:**
- Throws `InvalidOperationException` if no context is active.

---

### `public DateTime? EndTime`
Gets or sets the end time of the current request context.

**Exceptions:**
- Throws `InvalidOperationException` if no context is active during a set operation.

---

### `public Dictionary<string, string> Metadata`
Gets the metadata dictionary for the current request context.

**Exceptions:**
- Throws `InvalidOperationException` if no context is active.

---

### `public override string ToString()`
Returns a string representation of the current request context, including `RequestId`, `UserId`, `StartTime`, `EndTime`, and `Metadata`.

**Returns:**
- A formatted string summarizing the context state.

**Throws:**
- None.

---

### `public RequestContextMiddleware`
A middleware class that integrates `RequestContextManager` into the ASP.NET Core pipeline. It ensures request context is initialized and cleared for each request.

---

### `public async Task InvokeAsync(HttpContext context, RequestDelegate next)`
Middleware invocation method that creates a request context, invokes the next middleware in the pipeline, and clears the context upon completion.

**Parameters:**
- `context`: The `HttpContext` for the current request.
- `next`: The next middleware delegate in the pipeline.

**Returns:**
- A `Task` representing the asynchronous operation.

**Throws:**
- None.

---

### `public static RequestContext? GetRequestContext()`
Static method to retrieve the current `RequestContext` from the ambient context (e.g., `AsyncLocal` or `HttpContext`).

**Returns:**
- The active `RequestContext` if available; otherwise, `null`.

**Throws:**
- None.

## Usage

### Example 1: Tracking Request Context in Middleware
