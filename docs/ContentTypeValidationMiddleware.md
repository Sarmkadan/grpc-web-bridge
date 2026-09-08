# ContentTypeValidationMiddleware

A middleware component for ASP.NET Core applications that validates the `Content-Type` header on incoming requests and rejects any request whose content type is not a recognised gRPC-Web media type.

## API

### `ContentTypeValidationMiddleware`
Initialises the middleware.

- **Parameters**
  - `next`: The next middleware delegate in the pipeline.
  - `logger`: Logger for rejected requests.

### `async Task InvokeAsync(HttpContext context)`
Invokes the middleware pipeline, applying content-type validation before passing control to the next middleware.

- **Parameters**
  - `context`: The HTTP context for the current request.
- **Returns**
  - A `Task` representing the asynchronous operation.

### `static IApplicationBuilder UseGrpcWebContentTypeValidation(this IApplicationBuilder builder)`
Adds `ContentTypeValidationMiddleware` to the request pipeline. Place this call early in the pipeline, before routing, so that invalid requests are rejected before any routing or controller logic runs.

- **Parameters**
  - `builder`: The application builder.
- **Returns**
  - The application builder for method chaining.
- **Throws**
  - `ArgumentNullException`: If `builder` is `null`.

## JSON Extensions

The `ContentTypeValidationMiddlewareJsonExtensions` class provides JSON serialization and deserialization extension methods for `ContentTypeValidationMiddleware` using System.Text.Json.

### `string ToJson(this ContentTypeValidationMiddleware value, bool indented = false)`
Serializes the `ContentTypeValidationMiddleware` instance to a JSON string.

- **Parameters**
  - `value`: The middleware instance to serialize.
  - `indented`: Whether to format the JSON with indentation for readability.
- **Returns**
  - A JSON string representation of the middleware.
- **Throws**
  - `ArgumentNullException`: Thrown when `value` is null.

### `ContentTypeValidationMiddleware? FromJson(string json)`
Deserializes a JSON string to a `ContentTypeValidationMiddleware` instance.

- **Parameters**
  - `json`: The JSON string to deserialize.
- **Returns**
  - The deserialized middleware instance, or null if the JSON is empty or whitespace.
- **Throws**
  - `ArgumentNullException`: Thrown when `json` is null.
  - `JsonException`: Thrown when the JSON is invalid or cannot be deserialized.

### `bool TryFromJson(string json, out ContentTypeValidationMiddleware? value)`
Attempts to deserialize a JSON string to a `ContentTypeValidationMiddleware` instance.

- **Parameters**
  - `json`: The JSON string to deserialize.
  - `value`: Receives the deserialized middleware instance if successful.
- **Returns**
  - True if deserialization succeeded; otherwise, false.