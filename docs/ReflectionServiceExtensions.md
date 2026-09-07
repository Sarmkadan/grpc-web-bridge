# ReflectionServiceExtensions

`ReflectionServiceExtensions` provides the dependency-injection registration and endpoint mapping needed to expose [`ReflectionService`](./ReflectionService.md) through the ASP.NET Core application. Register the core bridge services described in [`DependencyInjection`](./DependencyInjection.md) before adding reflection.

## Public methods

### `AddGrpcWebBridgeReflection`

```csharp
public static IServiceCollection AddGrpcWebBridgeReflection(
    this IServiceCollection services)
```

Registers `ReflectionService` as a singleton and returns the same `IServiceCollection` so calls can be chained. It throws `ArgumentNullException` when `services` is `null`.

### `MapGrpcReflectionEndpoints`

```csharp
public static IEndpointRouteBuilder MapGrpcReflectionEndpoints(
    this IEndpointRouteBuilder endpoints)
```

Maps the reflection REST API and returns the same `IEndpointRouteBuilder`. It throws `ArgumentNullException` when `endpoints` is `null`.

## Options and routes

These methods do not expose a configuration delegate or an options object. `MapGrpcReflectionEndpoints` uses the fixed `/api/reflection` route prefix and maps these endpoints:

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/reflection/services` | Lists the full names of registered gRPC services. |
| `GET` | `/api/reflection/services/{fullName}` | Gets the descriptor for one service. URL-encode the full service name when necessary. |
| `GET` | `/api/reflection/services/{fullName}/methods/{methodName}` | Gets the descriptor for one method on a service. |
| `GET` | `/api/reflection/descriptors` | Gets descriptors for all registered services. |

The endpoints are tagged `Reflection` in OpenAPI metadata. Lookup failures for a service or method return `404 Not Found`; failures while listing services or all descriptors return `500 Internal Server Error`.

## Program.cs example

```csharp
using GrpcWebBridge.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpcWebBridge();
builder.Services.AddGrpcWebBridgeReflection();

var app = builder.Build();

app.MapGrpcReflectionEndpoints();

app.Run();
```

Call `AddGrpcWebBridgeReflection` before `builder.Build()`, and call `MapGrpcReflectionEndpoints` after building the application.
