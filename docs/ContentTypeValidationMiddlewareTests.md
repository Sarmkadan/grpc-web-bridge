# ContentTypeValidationMiddlewareTests
The `ContentTypeValidationMiddlewareTests` class is designed to test the functionality of the `ContentTypeValidationMiddleware` in the `grpc-web-bridge` project. This middleware is responsible for validating the content type of incoming requests to ensure they are compatible with gRPC. The tests in this class cover various scenarios, including valid and invalid content types, missing content types, and excluded paths or non-POST methods.

## API
The `ContentTypeValidationMiddlewareTests` class contains the following public members:
* `InvokeAsync_WithValidGrpcContentType_CallsNextMiddleware`: Tests that the middleware calls the next middleware when the content type is valid for gRPC.
* `InvokeAsync_WithInvalidContentType_Returns415`: Verifies that the middleware returns a 415 status code when the content type is invalid.
* `InvokeAsync_WithMissingContentType_Returns415`: Checks that the middleware returns a 415 status code when the content type is missing.
* `InvokeAsync_WithExcludedPath_BypassesValidation`: Tests that the middleware bypasses validation for excluded paths.
* `InvokeAsync_WithNonPostMethod_BypassesValidation`: Verifies that the middleware bypasses validation for non-POST methods.
* `InvokeAsync_WithInvalidContentType_WritesJsonErrorBody`: Tests that the middleware writes a JSON error body when the content type is invalid.

## Usage
Here are two examples of using the `ContentTypeValidationMiddlewareTests` class:
```csharp
// Example 1: Testing with a valid gRPC content type
[TestMethod]
public async Task TestValidContentType()
{
    // Arrange
    var middleware = new ContentTypeValidationMiddleware();
    var context = new DefaultHttpContext();
    context.Request.Method = "POST";
    context.Request.ContentType = "application/grpc";

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    Assert.IsTrue(context.Response.HasStarted);
}

// Example 2: Testing with an invalid content type
[TestMethod]
public async Task TestInvalidContentType()
{
    // Arrange
    var middleware = new ContentTypeValidationMiddleware();
    var context = new DefaultHttpContext();
    context.Request.Method = "POST";
    context.Request.ContentType = "application/json";

    // Act
    await middleware.InvokeAsync(context);

    // Assert
    Assert.AreEqual(415, context.Response.StatusCode);
}
```

## Notes
When using the `ContentTypeValidationMiddlewareTests` class, note that the tests are designed to cover various edge cases, including:
* The middleware only validates content types for POST requests.
* Excluded paths are bypassed, regardless of the content type.
* The middleware returns a 415 status code for invalid or missing content types.
* The middleware writes a JSON error body when the content type is invalid.
Regarding thread-safety, the `ContentTypeValidationMiddlewareTests` class is designed to be thread-safe, as it does not maintain any internal state between test runs. However, it is still important to ensure that the test environment is properly configured to run tests concurrently without interfering with each other.
