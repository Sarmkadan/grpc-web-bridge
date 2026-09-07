# HttpContextGrpcExtensions

The `HttpContextGrpcExtensions` class provides helpers for identifying supported gRPC request content types and retrieving the gRPC method path from an ASP.NET Core `HttpContext`.

## API

### `bool IsGrpcWebRequest(this HttpContext context)`

Returns `false` when `Request.ContentType` is null, empty, or whitespace, or when it cannot be parsed by `MediaTypeHeaderValue.TryParse`. For a parsed value, the media type must exactly match one of the following values, using an ordinal, case-insensitive comparison:

* `application/grpc-web+proto`
* `application/grpc-web-text+proto`
* `application/grpc-web-text`
* `application/grpc-web`
* `application/grpc+proto`
* `application/grpc`

The comparison uses the parsed `MediaType` value, so valid parameters such as `; charset=utf-8` do not participate in the match. Prefixes, suffixes, and other media types are not accepted.

### `string GetGrpcMethodPath(this HttpContext context)`

Returns `Request.Path.Value`. If the path value is null, it returns `string.Empty`.

## Middleware Usage

`ContentTypeValidationMiddleware` uses `IsGrpcWebRequest` to validate POST requests outside the excluded `/api`, `/swagger`, `/openapi`, `/health`, `/metrics`, and `/_` path prefixes. A failed check produces a `415 Unsupported Media Type` response.

The middleware uses `GetGrpcMethodPath` for excluded-prefix checks, request and rejection logging, and the `path` field in the 415 JSON response.
