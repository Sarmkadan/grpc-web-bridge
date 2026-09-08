# RequestLoggingMiddleware

`RequestLoggingMiddleware` logs details about incoming HTTP requests and outgoing HTTP responses, including elapsed processing time. It temporarily replaces the response body with a memory stream so that a successfully completed response can be inspected and then copied to the original response stream.

The implementation is in `src/GrpcWebBridge/Middleware/RequestLoggingMiddleware.cs`. Related behavior examples are covered by `tests/grpc-web-bridge.Tests/RequestLoggingMiddlewareTests.cs` and summarized in [RequestLoggingMiddlewareTests](RequestLoggingMiddlewareTests.md).

## Registration

Register the middleware in the ASP.NET Core pipeline with `UseRequestLogging`:

```csharp
var app = builder.Build();

app.UseRequestLogging();

app.Run();
```

`UseRequestLogging(this IApplicationBuilder builder)` calls `UseMiddleware<RequestLoggingMiddleware>()` and returns the same pipeline builder for chaining. The middleware constructor receives the next `RequestDelegate` and an `ILogger<RequestLoggingMiddleware>` from dependency injection; no separate service registration or options object is required.

Pipeline order determines which downstream activity is observed. In this project's `Program.cs`, request logging is registered after error handling and gRPC-Web content-type validation, and before routing and the remaining downstream middleware.

## Log entries and levels

Every invocation first emits this `Information` entry, including paths for which detailed logging is excluded:

```text
InvokeAsync started for {Method} {Path}
```

Request processing then emits `LogRequestAsync started for {Method} {Path}` at `Information`. For a non-excluded path, it also emits `Incoming request: {RequestData}` at `Information` with these fields:

| Field | Value |
| --- | --- |
| `type` | The literal `request` |
| `method` | `HttpRequest.Method` |
| `path` | `HttpRequest.Path.Value`, or `unknown` when it is null |
| `queryString` | `HttpRequest.QueryString.Value` |
| `scheme` | `HttpRequest.Scheme` |
| `host` | `HttpRequest.Host.Host` (the host name without the port) |
| `contentType` | `HttpRequest.ContentType` |
| `contentLength` | `HttpRequest.ContentLength` |
| `headers` | Non-sensitive request headers |
| `body` | The captured loggable request body, at most 1,000 characters followed by `...` when truncated; otherwise an empty string when it is not captured |
| `ip` | The remote IP address converted to a string, or null when unavailable |
| `timestamp` | `DateTime.UtcNow` at creation of the request log data |

After the downstream delegate completes successfully, the middleware stops its stopwatch and emits `LogResponseAsync started for {Method} {Path} after {ElapsedMilliseconds} ms` at `Information`. For a non-excluded path, it emits `Outgoing response: {ResponseData}` with these fields:

| Field | Value |
| --- | --- |
| `type` | The literal `response` |
| `method` | The original request method |
| `path` | The request path, or `unknown` when it is null |
| `statusCode` | `HttpResponse.StatusCode` |
| `contentType` | `HttpResponse.ContentType` |
| `contentLength` | `HttpResponse.ContentLength` |
| `headers` | Non-sensitive response headers |
| `body` | The captured loggable response body, at most 1,000 characters followed by `...` when truncated; otherwise an empty string when it is not captured |
| `elapsedMilliseconds` | Whole milliseconds elapsed while request logging and the downstream pipeline ran |
| `timestamp` | `DateTime.UtcNow` at creation of the response log data |

The outgoing response entry uses a level selected from the status code:

| Status code | Log level |
| --- | --- |
| Below 400 | `Information` |
| 400 through 499 | `Warning` |
| 500 or greater | `Error` |

The invocation, request-start, and response-start entries remain at `Information` regardless of status code.

## Headers and bodies

Header names are filtered case-insensitively. The following headers are omitted from both request and response log data: `Authorization`, `Cookie`, `X-Api-Key`, `X-Auth-Token`, and `Token`. Other headers are recorded as a case-insensitive dictionary of header names to their combined string values.

A request body is read only when `ContentLength` is greater than zero and `ContentType` contains `application/json`, `application/xml`, or `text/`, using a case-insensitive comparison. Request buffering is enabled before reading, and the request stream position is reset to zero afterward so downstream middleware can read it. Binary content such as `application/octet-stream`, an absent content type, and bodies without a positive content length are not captured.

A response body is read only when the temporary response stream is seekable, `HttpResponse.ContentLength` is greater than zero, and the response content type matches the same loggable types. The stream is rewound after inspection and, on successful completion, copied to the original response stream. Consequently, a response written without setting a positive `Content-Length` is still forwarded but its body is logged as an empty string.

## Excluded paths

Detailed request and response data is skipped when the request path starts with one of these configured prefixes:

- `/health`
- `/swagger`
- `/api/metrics`
- `/favicon.ico`

The initial invocation, request-start, and—after successful downstream completion—response-start entries are still logged at `Information`. Exclusion does not short-circuit the request: the next middleware is still invoked, and the response continues through the same temporary-stream forwarding flow. The tests demonstrate pass-through for each configured prefix.

## Exception behavior

The middleware has no `catch` block. If request logging, the downstream delegate, response logging, or response copying throws, the exception propagates to earlier middleware or the host. The `finally` block always restores `HttpResponse.Body` to the original stream.

In particular, when the downstream delegate throws, the stopwatch is not explicitly stopped, no response-start or outgoing-response entry is produced, and the captured response is not copied to the original stream. To convert downstream exceptions into responses, an exception-handling middleware must wrap request logging by being registered before `UseRequestLogging`; that is how the project's `Program.cs` orders `UseErrorHandling`.

## Examples reflected by the tests

The test suite constructs the middleware with a `RequestDelegate` and `NullLogger<RequestLoggingMiddleware>`, then verifies the implemented pipeline behavior. For example, a JSON response written by the next delegate is forwarded to the context's original response stream:

```csharp
const string payload = "{\"result\":\"ok\"}";
var middleware = new RequestLoggingMiddleware(
    context =>
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(payload);
    },
    NullLogger<RequestLoggingMiddleware>.Instance);

var context = new DefaultHttpContext();
context.Request.Method = HttpMethods.Get;
context.Request.Path = "/api/bridge/test";
context.Response.Body = new MemoryStream();

await middleware.InvokeAsync(context);
```

Other tests exercise 200, 400, 404, and 500 responses; excluded paths; an `Authorization` request header; a JSON request body; a binary request body; and a non-gRPC path. These examples confirm pass-through and response forwarding without asserting the formatted output of a particular logging provider.
