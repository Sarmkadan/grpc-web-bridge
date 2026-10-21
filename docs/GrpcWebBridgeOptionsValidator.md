# GrpcWebBridgeOptionsValidator

`GrpcWebBridgeOptionsValidator` implements `IValidateOptions<GrpcWebBridgeOptions>`. `AddGrpcWebBridge` registers it and calls `ValidateOnStart()`, so invalid bridge configuration stops the host before it starts serving.

## Rules

| Property | Rule |
|----------|------|
| `Configuration.MaxMessageSize` | Must be greater than 0. |
| `Configuration.AllowedOrigins[i]` | Must not be empty or whitespace. Must be `*` or a bare `http`/`https` origin: `scheme://host[:port]`, with no path, query, fragment, user info or surrounding whitespace. |
| `Configuration.AllowedOrigins` | Must not be `null`. |
| `Configuration.StreamIdleTimeoutSeconds` | Must be greater than 0. |
| `Configuration.StreamHeartbeatIntervalSeconds` | Must be greater than 0. |
| `Configuration.DefaultTimeoutMilliseconds` | Must be greater than 0. |
| `Configuration.EnableCors` + `AllowedOrigins` | Mutually exclusive: `EnableCors = false` is rejected while `AllowedOrigins` lists any origin. Disable CORS or clear the origins. |

Zero is rejected for timeouts as well as negative values, matching the `With*` builder methods and `BridgeConfiguration.Validate()`.

## Behaviour

- All failures are collected, so a single `OptionsValidationException` message lists every failing property, separated by `; `.
- Validation runs during host start (`StartAsync`), before the bridge's hosted services run. Nothing is served with invalid options.
- The options are registered as an instance, so `AddGrpcWebBridge` binds `IOptions<GrpcWebBridgeOptions>` to that same `BridgeConfiguration` instance. The validator therefore checks the configuration the bridge actually uses.
- `Program.cs` sets a non-zero exit code when startup fails, so supervisors see the failure.

## Example

```text
OptionsValidationException: Configuration.MaxMessageSize must be greater than 0 (was 0);
Configuration.AllowedOrigins[1] 'not a url' is not a valid origin; expected '*' or scheme://host[:port] using http or https
```
