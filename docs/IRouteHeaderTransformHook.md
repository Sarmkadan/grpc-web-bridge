# IRouteHeaderTransformHook
The `IRouteHeaderTransformHook` type is designed to enable transformations of HTTP request and response headers in the context of gRPC-Web bridge routing. It provides a mechanism for modifying headers at specific points in the request-response cycle, allowing for customization and extension of the routing behavior.

## API
* `RouteHeaderTransformMiddleware`: A property that returns a middleware instance for route header transformations.
* `InvokeAsync`: An asynchronous method that invokes the hook's transformation logic. It does not take any parameters and does not return a value. It may throw exceptions if the transformation fails.
* `UseRouteHeaderTransforms`: A static method that adds route header transforms to an `IApplicationBuilder` instance. It takes no parameters and returns the modified `IApplicationBuilder`.
* `AddRouteHeaderTransformHook<THook>`: A generic static method that adds a route header transform hook of type `THook` to an `IServiceCollection` instance. It takes no parameters and returns the modified `IServiceCollection`.
* `AddRouteHeaderTransformHook`: A non-generic static method that adds a route header transform hook to an `IServiceCollection` instance. It takes no parameters and returns the modified `IServiceCollection`.
* `RoutePrefix`: A property that gets the route prefix associated with the hook. It returns a string or null if no prefix is set.
* `TransformRequestAsync`: An asynchronous method that transforms the request headers. It takes no parameters and returns a task that completes when the transformation is finished. It may throw exceptions if the transformation fails.
* `TransformResponseAsync`: An asynchronous method that transforms the response headers. It takes no parameters and returns a task that completes when the transformation is finished. It may throw exceptions if the transformation fails.

## Usage
The following examples demonstrate how to use the `IRouteHeaderTransformHook` type:
```csharp
// Example 1: Adding a custom route header transform hook
public class CustomHook : IRouteHeaderTransformHook
{
    public async Task TransformRequestAsync()
    {
        // Custom request header transformation logic
    }

    public async Task TransformResponseAsync()
    {
        // Custom response header transformation logic
    }
}

public void ConfigureServices(IServiceCollection services)
{
    services.AddRouteHeaderTransformHook<CustomHook>();
}

// Example 2: Using the UseRouteHeaderTransforms method
public void Configure(IApplicationBuilder app)
{
    app.UseRouteHeaderTransforms();
    // Other middleware and routing configuration
}
```

## Notes
When implementing custom route header transform hooks, consider the following edge cases and thread-safety remarks:
* The `TransformRequestAsync` and `TransformResponseAsync` methods are asynchronous, so they should be implemented to handle concurrent requests and responses safely.
* The `RoutePrefix` property may be null if no prefix is set, so checks should be performed before using its value.
* The `InvokeAsync` method may throw exceptions if the transformation fails, so error handling should be implemented accordingly.
* The `AddRouteHeaderTransformHook` and `AddRouteHeaderTransformHook<THook>` methods modify the `IServiceCollection` instance, so they should be used carefully to avoid conflicts with other service registrations.
