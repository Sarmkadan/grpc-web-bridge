# ErrorHandlingMiddleware

A middleware component for the gRPC-web bridge that intercepts unhandled exceptions during HTTP request processing and transforms them into structured JSON error responses. It captures exception details, assigns a unique trace identifier, and writes a standardized error payload to the response stream while preserving the original HTTP status code where applicable.

## API

### `public ErrorHandlingMiddleware`

The constructor for the middleware. It accepts the next request delegate in the pipeline via dependency injection.

- **Parameters**: `RequestDelegate next` — the delegate representing the remainder of the request pipeline.
- **Remarks**: Instances are typically created by the ASP.NET Core middleware factory. Not intended for direct instantiation in application code.

### `public async Task InvokeAsync`

Processes an HTTP request by invoking the next middleware in the pipeline. If the downstream pipeline throws an exception, it catches the exception and writes a structured JSON error response.

- **Parameters**: `HttpContext context` — the current HTTP context for the request.
- **Returns**: A `Task` representing the asynchronous operation.
- **Exceptions**: Does not throw; all downstream exceptions are caught and handled internally.

### `public bool Success`

Indicates whether the request completed without an unhandled exception.

- **Type**: `bool`
- **Value**: `true` when no exception was caught; `false` when an error response was generated.
- **Remarks**: This property reflects the outcome of the most recent invocation for the current instance. In typical scoped or transient registrations, this corresponds to a single request.

### `public string? Error`

The fully qualified name of the exception type that was caught, or `null` if no exception occurred.

- **Type**: `string?`
- **Remarks**: Set from `exception.GetType().FullName` when an exception is handled.

### `public string? Message`

The exception message string, or `null` if no exception occurred.

- **Type**: `string?`
- **Remarks**: Derived from `exception.Message`. May be truncated or empty depending on the exception thrown.

### `public object? Details`

Additional contextual information about the error. The structure is exception-dependent.

- **Type**: `object?`
- **Remarks**: Populated from exception data or inner exception details when available. Consumers should expect arbitrary JSON-serializable content or `null`.

### `public string? Path`

The request path where the error occurred, relative to the application root.

- **Type**: `string?`
- **Remarks**: Captured from `context.Request.Path`. `null` if the context was not available or the path could not be determined.

### `public string? TraceId`

A unique identifier assigned to the error occurrence for correlation across logs and responses.

- **Type**: `string?`
- **Remarks**: Generated per error using a GUID or equivalent unique identifier. Included in the JSON response body and typically returned as a response header.

### `public DateTime Timestamp`

The UTC timestamp at which the error was caught and processed.

- **Type**: `DateTime`
- **Remarks**: Set to `DateTime.UtcNow` at the moment of exception interception.

### `public static IApplicationBuilder UseErrorHandling`

Extension method that registers the `ErrorHandlingMiddleware` into the application pipeline.

- **Parameters**: `this IApplicationBuilder builder` — the application builder instance.
- **Returns**: The `IApplicationBuilder` instance, enabling fluent chaining.
- **Remarks**: Typically invoked in `Startup.Configure` or `Program.cs` before other middleware that may throw.

## Usage

### Example 1: Basic Registration in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Register the error handling middleware early in the pipeline.
app.UseErrorHandling();

app.MapGrpcService<MyGrpcService>();
app.Run();
```

Any unhandled exception thrown by `MyGrpcService` or subsequent middleware results in a JSON response containing `success: false`, the error message, trace ID, and timestamp.

### Example 2: Accessing Error Details in Downstream Logging Middleware

```csharp
app.UseErrorHandling();

app.Use(async (context, next) =>
{
    await next();

    // After the pipeline completes, inspect the error-handling middleware
    // if it was resolved as a scoped service.
    var errorHandler = context.RequestServices.GetService<ErrorHandlingMiddleware>();
    if (errorHandler is { Success: false })
    {
        var logger = context.RequestServices.GetService<ILogger<Program>>();
        logger?.LogWarning(
            "Request to {Path} failed with {Error}: {Message} (TraceId: {TraceId})",
            errorHandler.Path,
            errorHandler.Error,
            errorHandler.Message,
            errorHandler.TraceId);
    }
});
```

This pattern allows centralized logging of structured error information without duplicating exception-handling logic.

## Notes

- **Pipeline ordering**: `UseErrorHandling` should be placed early in the middleware pipeline to catch exceptions from all downstream components. Middleware registered before it will not benefit from its exception handling.
- **Response already started**: If the downstream pipeline has already begun writing to the response stream before throwing, the middleware may be unable to replace the response body. In such cases, the error is still logged and the trace ID may be appended as a response header, but the original partial response content remains.
- **Thread safety**: The instance properties (`Success`, `Error`, `Message`, `Details`, `Path`, `TraceId`, `Timestamp`) are set during `InvokeAsync` and are not thread-safe for concurrent access. When the middleware is registered as scoped or transient, each instance serves a single request, making concurrent access a non-issue. If registered as a singleton, these properties are overwritten on each request and must not be read concurrently.
- **Nullability**: All string and object properties are nullable. Consumers must perform null checks before dereferencing `Error`, `Message`, `Details`, `Path`, or `TraceId`.
- **Serialization**: The JSON response written to the client includes `success`, `error`, `message`, `details`, `path`, `traceId`, and `timestamp` fields. The `Details` object is serialized using the configured JSON serializer, which may omit null values depending on settings.
- **Trace ID propagation**: The `TraceId` is typically also set as a response header (`X-Trace-Id` or similar) to enable client-side correlation without parsing the response body.
