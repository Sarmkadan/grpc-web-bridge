# HttpTranscoderOptionsValidator

`HttpTranscoderOptionsValidator` implements `IValidateOptions<HttpTranscoderOptions>`. `AddGrpcWebBridge` registers it and calls `ValidateOnStart()` on `HttpTranscoderOptions`, so invalid REST transcoding configuration stops the host before it starts serving.

Configure the options with `services.Configure<HttpTranscoderOptions>(...)`. The defaults are valid.

## Rules

| Property | Rule |
|----------|------|
| `RoutePrefix` | If set, must start with `/` and must not end with `/`. `""` and `"/"` are rejected. Null means no prefix. |
| `JsonSerializerOptions` | Must not be `null`. |
| `MaxRequestBodySize` | Must be greater than 0. |
| `Routes` | Must not be `null`. |
| `Routes[i].HttpMethod` | Must be `GET`, `PUT`, `POST`, `DELETE` or `PATCH` (upper case). |
| `Routes[i].Template` | Must start with `/`, must not be empty, and must parse: braces balanced, no nested `{`, no empty variable name, no variable declared twice. A `{name=pattern}` variable declares `name`. |
| `Routes[i].Template` variables | Each variable's top-level field (the text before the first `.`) must appear in `Routes[i].RequestFieldNames`. |
| `Routes` (duplicates) | Two routes with the same HTTP method and the same template shape are rejected, across all services. Variable names are ignored, so `/v1/items/{id}` and `/v1/items/{itemId}` collide. The same template under different HTTP methods is allowed. |

## Behaviour

- All failures are collected, so a single `OptionsValidationException` message lists every violated rule, separated by `; `.
- Validation runs during host start (`StartAsync`), before the bridge's hosted services run.
- `Routes` is declared by the host. The bridge does not read `google.api.http` annotations, so the host supplies each `HttpRouteBinding` and its request field names.
- Nested variables such as `{book.id}` are checked only on their top-level field. Deeper fields are not checked against the proto.

## Example

```text
OptionsValidationException: HttpTranscoderOptions.RoutePrefix must start with '/' (was 'api/'); HttpTranscoderOptions.MaxRequestBodySize must be greater than 0 (was 0); HttpTranscoderOptions.Routes[1] route 'GET /v1/items/{itemId}' for items.v1.ArchiveService/GetArchivedItem duplicates 'GET /v1/items/{id}' for items.v1.ItemService/GetItem at Routes[0]
```
