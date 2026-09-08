# MaxBodySizeMiddleware

A middleware component for ASP.NET Core applications that rejects requests whose declared body size exceeds a configurable limit. The middleware checks the request's `Content-Length` header before passing the request to the rest of the pipeline.

## API

### `MaxBodySizeMiddleware`
Initializes a new instance of the maximum request body size middleware with the configured options.

- **Parameters**
  - `next`: The next middleware in the request pipeline.
  - `logger`: The logger used to record rejected requests.
  - `options`: The configured maximum request body size options.

### `async Task InvokeAsync(HttpContext context)`
Checks the request's declared content length and passes the request to the next middleware when it is within the configured limit. If `Content-Length` exceeds the limit, the middleware returns `413 Payload Too Large` with a JSON error response.

- **Parameters**
  - `context`: The HTTP context for the current request.
- **Returns**
  - A `Task` representing the asynchronous operation.

### `MaxBodySizeOptions`
Provides configuration for `MaxBodySizeMiddleware`.

### `long MaxRequestBodySizeBytes`
Gets or sets the maximum allowed request body size in bytes.

- **Type**: `long`
- **Default**: `4194304` bytes (4 MB)

### `static IApplicationBuilder UseMaxRequestBodySize(IApplicationBuilder builder)`
Registers `MaxBodySizeMiddleware` with the ASP.NET Core request pipeline. Place it early in the pipeline, before routing, so oversized requests are rejected before further processing.

- **Parameters**
  - `builder`: The application builder.
- **Returns**
  - The application builder for method chaining.
- **Throws**
  - `ArgumentNullException`: If `builder` is `null`.

## Usage

### Basic Setup

```csharp
using GrpcWebBridge.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MaxBodySizeOptions>(
    builder.Configuration.GetSection("MaxBodySize"));

var app = builder.Build();

app.UseMaxRequestBodySize();
```

### Configuration

```json
{
  "MaxBodySize": {
    "MaxRequestBodySizeBytes": 4194304
  }
}
```
