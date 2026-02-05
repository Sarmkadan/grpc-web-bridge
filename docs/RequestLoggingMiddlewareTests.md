# RequestLoggingMiddlewareTests

The `RequestLoggingMiddlewareTests` class contains unit tests for the `RequestLoggingMiddleware` component in the `grpc-web-bridge` project. It verifies that the middleware correctly logs HTTP request information, passes through to the next delegate, handles excluded paths, captures or skips request bodies based on content type, and does not throw under various conditions including the presence of authorization headers and different status codes.

## API

### `public async Task InvokeAsync_WhenNoException_CallsNextMiddleware`
Verifies that when the middleware does not encounter an exception, it invokes the next middleware delegate in the pipeline.  
**Parameters:** None.  
**Return value:** `Task`.  
**Throws:** Does not throw; test assertions fail if the next delegate is not called.

### `public async Task InvokeAsync_WhenNoException_ResponseBodyIsForwardedToOriginalStream`
Ensures that after the middleware processes the request, the response body is correctly written to the original response stream.  
**Parameters:** None.  
**Return value:** `Task`.  
**Throws:** Does not throw; test assertions fail if the body is not forwarded.

### `public async Task InvokeAsync_ForExcludedPath_StillCallsNext`
Confirms that requests to paths configured as excluded still invoke the next middleware delegate, even though logging may be skipped.  
**Parameters:** None.  
**Return value:** `Task`.  
**Throws:** Does not throw; test assertions fail if the next delegate is not called.

### `public async Task InvokeAsync_WithVariousStatusCodes_DoesNotThrow`
Tests that the middleware handles responses with a range of HTTP status codes (e.g., 200, 400, 500) without throwing an exception.  
**Parameters:** None.  
**Return value:** `Task`.  
**Throws:** Does not throw; test assertions fail if an exception is thrown.

### `public async Task InvokeAsync_DoesNotThrow_WhenAuthorizationHeaderIsPresent`
Validates that the middleware does not throw when the incoming request contains an `Authorization` header.  
**Parameters:** None.  
**Return value:** `Task`.  
**Throws:** Does not throw; test assertions fail if an exception is thrown.

### `public async Task InvokeAsync_WithJsonRequestBody_DoesNotThrowAndCallsNext`
Ensures that when the request body is JSON, the middleware captures the body for logging and still calls the next delegate without throwing.  
**Parameters:** None.  
**Return value:** `Task`.  
**Throws:** Does not throw; test assertions fail if the next delegate is not called or an exception occurs.

### `public async Task InvokeAsync_WithBinaryContentType_DoesNotCaptureBody`
Verifies that requests with a binary content type (e.g., `application/octet-stream`) are not logged with a captured body, but the request still passes through.  
**Parameters:** None.  
**Return value:** `Task`.  
**Throws:** Does not throw; test assertions fail if the body is captured or the next delegate is not called.

### `public void Constructor_WithValidArguments_CreatesInstance`
Confirms that the `RequestLoggingMiddleware` constructor successfully creates an instance when provided with valid arguments (e.g., `RequestDelegate` and `ILogger`).  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** Does not throw; test assertions fail if the constructor throws or returns null.

### `public async Task InvokeAsync_WithNonGrpcPath_LogsAndPassesThrough`
Tests that requests to non-gRPC paths are logged and still passed through to the next middleware delegate.  
**Parameters:** None.  
**Return value:** `Task`.  
**Throws:** Does not throw; test assertions fail if logging does not occur or the next delegate is not called.

## Usage

The following examples demonstrate how to write tests using the same patterns as `RequestLoggingMiddlewareTests`. These examples assume the use of xUnit and a mocking framework such as Moq.

**Example 1: Testing that the next middleware is called for a normal request**

```csharp
[Fact]
public async Task InvokeAsync_WhenNoException_CallsNextMiddleware()
{
    // Arrange
    var nextCalled = false;
    RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
    var logger = Mock.Of<ILogger<RequestLoggingMiddleware>>();
    var middleware = new RequestLoggingMiddleware(next, logger);
    var context = new DefaultHttpContext();

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    Assert.True(nextCalled);
}
```

**Example 2: Testing that an excluded path still invokes the next delegate**

```csharp
[Fact]
public async Task InvokeAsync_ForExcludedPath_StillCallsNext()
{
    // Arrange
    var nextCalled = false;
    RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
    var logger = Mock.Of<ILogger<RequestLoggingMiddleware>>();
    var middleware = new RequestLoggingMiddleware(next, logger, excludedPaths: new[] { "/health" });
    var context = new DefaultHttpContext();
    context.Request.Path = "/health";

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    Assert.True(nextCalled);
}
```

## Notes

- **Edge cases:** The middleware is designed to handle excluded paths, binary content types (body not captured), and the presence of authorization headers without throwing. Tests cover these scenarios explicitly.
- **Thread safety:** The middleware itself is stateless and can be safely invoked concurrently. The test class is not intended to be thread-safe; tests should be run sequentially within a single test runner instance.
- **Body capture:** Only text-based content types (e.g., JSON, XML) are captured for logging. Binary content types are passed through without body logging to avoid memory overhead.
- **Status codes:** The middleware does not alter the response status code; it only logs the request and response details. Tests confirm that no exceptions are thrown for common status codes.
- **Constructor validation:** The constructor test ensures that valid arguments produce a non-null instance. Invalid arguments (e.g., null `RequestDelegate`) are expected to throw `ArgumentNullException` and are tested separately in the production code.
