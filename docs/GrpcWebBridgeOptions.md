# GrpcWebBridgeOptions

The `GrpcWebBridgeOptions` class provides a fluent configuration API for setting up a gRPC‑Web bridge. It exposes a mutable `BridgeConfiguration` instance through the `Configuration` property and a suite of `With*` and `Add*` methods that allow developers to tailor transport limits, behavior, logging, metrics, CORS, authentication, and other runtime aspects before the bridge is instantiated.

## API

### `public BridgeConfiguration Configuration`
Gets or sets the underlying `BridgeConfiguration` that holds the actual settings applied to the bridge. Modifying this property directly replaces the entire configuration object; using the fluent `With*` methods is the preferred way to adjust individual values.

### `public GrpcWebBridgeOptions()`
Creates a new `GrpcWebBridgeOptions` instance with default settings. Equivalent to invoking the parameterless constructor; the returned object can be further customized via the fluent methods.

### `public GrpcWebBridgeOptions`
Creates a new `GrpcWebBridgeOptions` instance (parameterless). This constructor behaves identically to `GrpcWebBridgeOptions()` and is provided for completeness; it returns a fresh options object ready for configuration.

### `public GrpcWebBridgeOptions WithDevelopment()`
Enables development‑mode diagnostics (e.g., verbose logging, detailed error messages). Returns the same `GrpcWebBridgeOptions` instance to allow chaining. Throws `InvalidOperationException` if called after the bridge has already been built.

### `public GrpcWebBridgeOptions WithProduction()`
Switches the bridge to production mode, disabling development‑only features and optimizing for performance. Returns the same instance for chaining. Throws `InvalidOperationException` if the bridge is already built.

### `public GrpcWebBridgeOptions WithTesting()`
Configures the bridge for testing environments (e.g., enables in‑memory transport, disables external calls). Returns the same instance. Throws `InvalidOperationException` if the bridge has been built.

### `public GrpcWebBridgeOptions WithMaxStreamCount(int maxStreamCount)`
Sets the maximum number of concurrent HTTP/2 streams allowed per connection. `maxStreamCount` must be greater than zero. Returns the same instance. Throws `ArgumentOutOfRangeException` if the value is ≤ 0.

### `public GrpcWebBridgeOptions WithStreamIdleTimeout(TimeSpan streamIdleTimeout)`
Defines the idle timeout after which an inactive stream is closed. `streamIdleTimeout` must be a positive `TimeSpan`. Returns the same instance. Throws `ArgumentOutOfRangeException` if the timeout is ≤ zero.

### `public GrpcWebBridgeOptions WithMaxMessageSize(int maxMessageSize)`
Specifies the maximum permitted size (in bytes) of a single gRPC message. `maxMessageSize` must be greater than zero. Returns the same instance. Throws `ArgumentOutOfRangeException` if the value is ≤ 0.

### `public GrpcWebBridgeOptions WithDefaultTimeout(TimeSpan defaultTimeout)`
Sets the default timeout applied to gRPC calls when no per‑call timeout is supplied. `defaultTimeout` must be positive. Returns the same instance. Throws `ArgumentOutOfRangeException` if the timeout is ≤ zero.

### `public GrpcWebBridgeOptions WithCompression(bool enableCompression)`
Enables or disables message compression (e.g., gzip) for both request and response payloads. Returns the same instance. No exceptions are thrown for valid boolean values.

### `public GrpcWebBridgeOptions WithSwagger(bool enableSwagger)`
When `true`, adds Swagger/OpenAPI middleware to serve API documentation alongside the gRPC‑Web endpoint. Returns the same instance. No exceptions are thrown for valid boolean values.

### `public GrpcWebBridgeOptions WithLogging(bool enableLogging)`
Turns on or off internal logging of bridge activity. Returns the same instance. No exceptions are thrown for valid boolean values.

### `public GrpcWebBridgeOptions WithMetrics(bool enableMetrics)`
Enables collection and exposure of Prometheus‑compatible metrics. Returns the same instance. No exceptions are thrown for valid boolean values.

### `public GrpcWebBridgeOptions WithCors(Action<CorsOptions> configure)`
Configures Cross‑Origin Resource Sharing (CORS) policies. The `configure` delegate receives a `CorsOptions` object to set allowed origins, methods, headers, etc. Returns the same instance. Throws `ArgumentNullException` if `configure` is `null`.

### `public GrpcWebBridgeOptions WithRequiredAuthentication(bool required)`
When `true`, enforces that every incoming request carries valid authentication credentials; otherwise, unauthenticated calls are permitted. Returns the same instance. No exceptions are thrown for valid boolean values.

### `public GrpcWebBridgeOptions AddAllowedOrigin(string origin)`
Adds a single origin to the CORS allowed‑origins list. `origin` must be a non‑empty, well‑formed URL. Returns the same instance. Throws `ArgumentException` if `origin` is `null`, empty, or malformed.

### `public GrpcWebBridgeOptions AddAllowedOrigins(IEnumerable<string> origins)`
Adds multiple origins to the CORS allowed‑origins list. `origins` must not be `null` and must contain only non‑empty, well‑formed URLs. Returns the same instance. Throws `ArgumentNullException` if `origins` is `null`; throws `ArgumentException` if any entry is invalid.

### `public GrpcWebBridgeOptions AddCustomHeader(string name, string value)`
Registers a custom HTTP header to be included on every response. Both `name` and `value` must be non‑null and non‑empty. Returns the same instance. Throws `GrpcWebBridgeOptions.AddCustomHeader(string name, string value)  
Adds a custom HTTP header to be included on every response. Both `name` and `value` must be non‑null and non‑empty. Returns the same instance. Throws `ArgumentException` if either parameter is `null` or empty.

### `public GrpcWebBridgeOptions WithInstanceName(string instanceName)`
Assigns a friendly name to the bridge instance, useful for logging and metrics identification. `instanceName` must not be `null` or empty. Returns the same instance. Throws `ArgumentException` if the name is invalid.

## Usage

```csharp
using GrpcWebBridge;

// Basic setup with development logging and a custom CORS origin
var options = new GrpcWebBridgeOptions()
    .WithDevelopment()
    .WithLogging(true)
    .AddAllowedOrigin("https://myapp.example.com")
    .WithMaxStreamCount(100)
    .WithStreamIdleTimeout(TimeSpan.FromMinutes(5));

var bridge = new GrpcWebBridge(options.Configuration);
```

```csharp
using GrpcWebBridge;
using GrpcWebBridge.Cors;

// Production‑ready configuration with metrics, Swagger, and required authentication
var corsOpts = new CorsOptions();
corsOpts.AllowAnyHeader().AllowAnyMethod(); // example configuration

var options = new GrpcWebBridgeOptions()
    .WithProduction()
    .WithMetrics(true)
    .WithSwagger(true)
    .WithRequiredAuthentication(true)
    .WithCors(c => c.WithOrigins("https://trusted.client.com")
                    .AllowAnyHeader()
                    .AllowAnyMethod())
    .AddAllowedOrigins(new[] { "https://client1.com", "https://client2.com" })
    .AddCustomHeader("X-Server-Version", "1.2.3")
    .WithInstanceName("grpc‑web‑prod‑01");

var bridge = new GrpcWebBridge(options.Configuration);
```

## Notes

- The `GrpcWebBridgeOptions` instance is mutable; concurrent modification from multiple threads while configuring the same instance is not thread‑safe. It is recommended to configure the options on a single thread before passing the resulting `BridgeConfiguration` to the bridge constructor.
- Once a `GrpcWebBridge` has been created from a `BridgeConfiguration`, further changes to the original `GrpcWebBridgeOptions` object do not affect the running bridge. To alter settings at runtime, a new bridge must be instantiated with updated options.
- Several `With*` methods (e.g., `WithDevelopment`, `WithProduction`, `WithTesting`) are mutually exclusive in practice; calling more than one may lead to contradictory behavior. The implementation does not prevent such calls, but the resulting configuration may be undefined.
- Methods that accept collections (`AddAllowedOrigins`) defensively copy the supplied data; subsequent changes to the original enumerable will not affect the bridge’s configuration.
- All validation exceptions (`ArgumentException`, `ArgumentOutOfRangeException`, `ArgumentNullException`, `InvalidOperationException`) are thrown prior to any state change, leaving the options object unchanged.
