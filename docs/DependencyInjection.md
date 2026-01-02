# DependencyInjection
The `DependencyInjection` static class provides extension methods for registering gRPC‑Web bridge services in an ASP.NET Core DI container, along with a hosted service that cleans up idle streams to prevent resource leaks.

## API
### AddGrpcWebBridge(IServiceCollection services)
- **Purpose:** Registers the core gRPC‑Web bridge services required for protocol translation.
- **Parameters:** `services` – The `IServiceCollection` to add services to.
- **Return value:** The same `IServiceCollection` instance to allow chaining.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddGrpcWebBridge(IServiceCollection services, Action<GrpcWebBridgeOptions> configure)
- **Purpose:** Registers core gRPC‑Web bridge services with customizable options.
- **Parameters:** 
  - `services` – The `IServiceCollection` to add services to.
  - `configure` – A delegate to configure `GrpcWebBridgeOptions`.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` or `configure` is `null`.

### AddGrpcWebBridgeSwagger(IServiceCollection services)
- **Purpose:** Adds Swagger generation support for gRPC‑Web endpoints.
- **Parameters:** `services` – The `IServiceCollection` to add services to.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddGrpcWebBridgeSwagger(IServiceCollection services, Action<SwaggerGenOptions> configure)
- **Purpose:** Adds Swagger generation with additional configuration.
- **Parameters:** 
  - `services` – The `IServiceCollection` to add services to.
  - `configure` – A delegate to configure `SwaggerGenOptions`.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` or `configure` is `null`.

### AddGrpcWebBridgeCors(IServiceCollection services)
- **Purpose:** Configures the CORS policies required for gRPC‑Web bridge communication.
- **Parameters:** `services` – The `IServiceCollection` to add services to.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddGrpcWebBridgeCors(IServiceCollection services, Action<CorsOptions> configure)
- **Purpose:** Configures CORS with custom options.
- **Parameters:** 
  - `services` – The `IServiceCollection` to add services to.
  - `configure` – A delegate to configure `CorsOptions`.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` or `configure` is `null`.

### AddGrpcWebBridgeAuthentication(IServiceCollection services)
- **Purpose:** Adds authentication services for the gRPC‑Web bridge.
- **Parameters:** `services` – The `IServiceCollection` to add services to.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddGrpcWebBridgeAuthentication(IServiceCollection services, Action<AuthenticationOptions> configure)
- **Purpose:** Adds authentication with custom options.
- **Parameters:** 
  - `services` – The `IServiceCollection` to add services to.
  - `configure` – A delegate to configure `AuthenticationOptions`.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` or `configure` is `null`.

### AddGrpcWebBridgePrometheus(IServiceCollection services)
- **Purpose:** Registers Prometheus metrics collection for the gRPC‑Web bridge.
- **Parameters:** `services` – The `IServiceCollection` to add services to.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddGrpcWebBridgePrometheus(IServiceCollection services, Action<PrometheusOptions> configure)
- **Purpose:** Registers Prometheus metrics with custom options.
- **Parameters:** 
  - `services` – The `IServiceCollection` to add services to.
  - `configure` – A delegate to configure `PrometheusOptions`.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` or `configure` is `null`.

### AddGrpcWebBridgeTracing(IServiceCollection services)
- **Purpose:** Adds distributed tracing support for the gRPC‑Web bridge.
- **Parameters:** `services` – The `IServiceCollection` to add services to.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` is `null`.

### AddGrpcWebBridgeTracing(IServiceCollection services, Action<TracingOptions> configure)
- **Purpose:** Adds tracing with custom options.
- **Parameters:** 
  - `services` – The `IServiceCollection` to add services to.
  - `configure` – A delegate to configure `TracingOptions`.
- **Return value:** The same `IServiceCollection` instance.
- **Exceptions:** Throws `ArgumentNullException` if `services` or `configure` is `null`.

### StreamCleanupService
- **Purpose:** A hosted service that monitors and disposes of idle gRPC‑Web streams to prevent resource leaks.
- **Remarks:** Implements `IHostedService`; it is automatically registered when any `AddGrpcWebBridge*` method is called.
- **Thread safety:** Safe for concurrent use; internal state is protected by locks.

### StopAsync(CancellationToken cancellationToken)
- **Purpose:** Overrides `IHostedService.StopAsync` to gracefully halt stream cleanup during application shutdown.
- **Parameters:** `cancellationToken` – Token to observe while stopping.
- **Return value:** A `Task` that completes when the service has stopped.
- **Exceptions:** May throw `OperationCanceledException` if the token is triggered; otherwise propagates any exception encountered during cleanup.

## Usage
```csharp
using Microsoft.Extensions.DependencyInjection;
using GrpcWebBridge.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register core gRPC‑Web bridge services
builder.Services.AddGrpcWebBridge();

// Add optional features as needed
builder.Services.AddGrpcWebBridgeSwagger();
builder.Services.AddGrpcWebBridgeCors();
builder.Services.AddGrpcWebBridgeAuthentication();
builder.Services.AddGrpcWebBridgePrometheus();
builder.Services.AddGrpcWebBridgeTracing();

var app = builder.Build();
app.UseRouting();
app.UseEndpoints(endpoints => { endpoints.MapGrpcService<MyService>(); });
app.Run();
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using GrpcWebBridge.DependencyInjection;

var services = new ServiceCollection();

// Configure core options via lambda
services.AddGrpcWebBridge(options =>
{
    options.MaxReceiveMessageSize = 4 * 1024 * 1024;
    options.EnableHealthChecks = true;
});

// Configure Swagger
services.AddGrpcWebBridgeSwagger(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "gRPC-Web API", Version = "v1" });
});

// Configure CORS
services.AddGrpcWebBridgeCors(c =>
{
    c.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var provider = services.BuildServiceProvider();
// StreamCleanupService starts automatically as a hosted service.
```

## Notes
- All extension methods are idempotent; calling them multiple times after the first registration has no effect.
- `StreamCleanupService` runs as a background hosted service and does not block application startup.
- `StopAsync` is invoked automatically by the host during shutdown; manual invocation is unnecessary.
- If any configuration delegate throws an exception, the registration aborts and the exception propagates to the caller.
- The static `DependencyInjection` class contains no mutable state, making it thread‑safe for concurrent registration calls.
- Internal timers and locks within `StreamCleanupService` ensure safe disposal of streams across threads.
