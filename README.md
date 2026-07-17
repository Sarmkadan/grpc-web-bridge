# gRPC-Web Bridge

A production-grade gRPC-Web bridge server for .NET 10 that enables seamless protocol translation between gRPC and gRPC-Web clients.

![Build](https://github.com/sarmkadan/grpc-web-bridge/actions/workflows/build.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/grpc-web-bridge)

## Installation

```bash
git clone https://github.com/sarmkadan/grpc-web-bridge.git
cd grpc-web-bridge
dotnet build
```

## Quick Start

```bash
cd src/GrpcWebBridge
dotnet run
```

## Architecture

The bridge is a single ASP.NET Core app: middleware pipeline (error handling,
content-type validation, logging) in front of `BridgeController`, protocol
translation and streaming services behind it, and a pooled gRPC channel
manager talking to backends. The full breakdown - components, design
decisions with trade-offs, extension points, known limitations - lives in
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Configuration

Configure the bridge in `appsettings.json`:

```json
{
  "GrpcWebBridge": {
    "CompressResponses": true
  }
}
```

## DependencyInjection

The `DependencyInjection` class provides extension methods for configuring gRPC-Web Bridge services in the ASP.NET Core dependency injection container. These methods simplify service registration for core components, authentication, Swagger documentation, CORS policies, Prometheus metrics, and distributed tracing. Each extension method follows the standard .NET DI pattern, returning the `IServiceCollection` for method chaining.

Example usage:

```csharp
// Basic setup with default options
builder.Services.AddGrpcWebBridge();

// Custom configuration via delegate
builder.Services.AddGrpcWebBridge(options =>
{
    options.Configuration.EnableSwagger = true;
    options.Configuration.EnableMetrics = true;
    options.Configuration.EnableCors = true;
});

// Add Swagger documentation
builder.Services.AddGrpcWebBridgeSwagger(
    title: "gRPC-Web Bridge API",
    version: "1.0.0"
);

// Add CORS configuration
builder.Services.AddGrpcWebBridgeCors();

// Add authentication with JWT Bearer tokens
builder.Services.AddGrpcWebBridgeAuthentication(options =>
{
    options.Authority = "https://auth.example.com";
    options.Audience = "grpc-web-bridge";
});

// Add Prometheus metrics
builder.Services.AddGrpcWebBridgePrometheus();

// Add OpenTelemetry distributed tracing
builder.Services.AddGrpcWebBridgeTracing(
    serviceName: "grpc-web-bridge",
    instanceName: "production-instance-1",
    configureBuilder: builder => builder.AddConsoleExporter()
);

// Core services are automatically registered:
// - ProtocolTranslationService
// - StreamingService  
// - AuthenticationService
// - ServiceRegistry
// - ServiceRepository
// - GrpcConnectionManager
// - StreamCleanupService (as IHostedService)

var app = builder.Build();

// Use Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseGrpcWebBridgeSwagger();
}

// Use CORS policy
app.UseCors("AllowGrpcWeb");

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Expose Prometheus metrics endpoint
app.MapMetrics();
```

## BridgeConfiguration

The `BridgeConfiguration` class defines all runtime configuration settings for the gRPC-Web bridge server. It controls logging, authentication, CORS policies, streaming behavior, message sizes, compression, and service defaults. This configuration object is typically loaded from application settings and validated before the bridge starts processing requests.

Example usage:

```csharp
// Create a production configuration with custom instance name
var config = new BridgeConfiguration(
    environment: "Production",
    instanceName: "user-service-bridge"
);

// Configure bridge behavior
config.EnableLogging = true;
config.EnableSwagger = true;
config.EnableMetrics = true;
config.EnableCors = true;
config.RequireAuthentication = true;

// Configure streaming behavior
config.MaxStreamCount = 100;
config.StreamIdleTimeoutSeconds = 30;
config.StreamHeartbeatIntervalSeconds = 10;

// Configure message limits
config.MaxMessageSize = 4 * 1024 * 1024; // 4MB
config.DefaultTimeoutMilliseconds = 30000; // 30 seconds

// Configure compression
config.CompressResponses = true;
config.CompressionLevel = 6;

// Configure CORS
config.AllowedOrigins = new List<string> { "https://example.com", "https://api.example.com" };
config.AllowedMethods = new List<string> { "GET", "POST", "PUT", "DELETE", "OPTIONS" };

// Add custom headers
config.AddCustomHeader("X-Service-Version", "1.2.3");
config.AddCustomHeader("X-Environment", "production");

// Set service defaults
config.SetServiceDefault("UserService", new { MaxRetries = 3, Timeout = 5000 });

// Validate configuration before use
config.Validate();

Console.WriteLine($"Bridge configured: {config.InstanceName ?? config.InstanceId}");
Console.WriteLine($"Environment: {config.Environment}");
Console.WriteLine($"Max streams: {config.MaxStreamCount}");
Console.WriteLine($"Compression: {config.CompressResponses} (level {config.CompressionLevel})");
```

## GrpcWebBridgeOptions

The `GrpcWebBridgeOptions` class provides a fluent interface for configuring the gRPC-Web bridge server. It allows you to programmatically configure all aspects of the bridge including environment settings, streaming behavior, message sizes, compression, authentication, CORS policies, and service defaults using a clean, method-chaining API.

This options class serves as the primary configuration mechanism when registering the bridge services in your ASP.NET Core application's dependency injection container.

Example usage:

```csharp
// Create options with environment and instance name
var options = new GrpcWebBridgeOptions("Production", "user-service-bridge-01");

// Configure bridge for production environment
options.WithProduction()
    .WithMaxStreamCount(200)
    .WithStreamIdleTimeout(60)
    .WithMaxMessageSize(8 * 1024 * 1024) // 8MB
    .WithDefaultTimeout(30000) // 30 seconds
    .WithCompression(true, compressionLevel: 6)
    .WithRequiredAuthentication()
    .AddAllowedOrigins(
        "https://app.example.com",
        "https://api.example.com"
    )
    .AddCustomHeader("X-Service-Version", "2.1.0")
    .AddCustomHeader("X-Environment", "production");

// Validate configuration before use
options.Validate();

// Use with dependency injection in Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpcWebBridge(opt =>
{
    opt.Configuration = options.Configuration;
    opt.Configuration.EnableSwagger = true;
    opt.Configuration.EnableMetrics = true;
});
```

## StartupConfiguration

The `StartupConfiguration` class provides service initialization and system information retrieval for the gRPC-Web bridge. It handles the startup sequence by registering default services, validating configuration, and providing runtime system information including instance identification, environment details, and resource limits.

Example usage:

```csharp
// Create startup configuration (typically done via dependency injection)
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());

var serviceProvider = services.BuildServiceProvider();
var startupConfig = new StartupConfiguration(
    serviceProvider,
    serviceProvider.GetRequiredService<ILogger<StartupConfiguration>>()
);

// Initialize the bridge with default services
await startupConfig.InitializeAsync();

// Validate configuration before starting the bridge
var options = new GrpcWebBridgeOptions("Production", "production-bridge-01");
startupConfig.ValidateConfiguration(options);

// Get system information for monitoring and logging
var systemInfo = startupConfig.GetSystemInfo();

Console.WriteLine($"Instance: {systemInfo.InstanceName} ({systemInfo.InstanceId})");
Console.WriteLine($"Environment: {systemInfo.Environment}");
Console.WriteLine($"Version: {systemInfo.Version}");
Console.WriteLine($"Start Time: {systemInfo.StartTime:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Max Streams: {systemInfo.MaxStreamCount}");
Console.WriteLine($"Max Message Size: {systemInfo.MaxMessageSize / 1024 / 1024}MB");
```

## Usage Examples

The repository includes comprehensive usage examples demonstrating how to integrate with the gRPC-Web Bridge:

- **[Basic Usage](examples/BasicUsage.cs)** - Minimal setup and first call
- **[Advanced Usage](examples/AdvancedUsage.cs)** - Configuration, custom options, error handling, and resilience patterns
- **[ASP.NET Core Integration](examples/IntegrationExample.cs)** - Dependency injection and production patterns


These examples show:
- Simple HTTP client integration
- Resilience and retry patterns with Polly
- Service registration and discovery
- Batch operations and streaming
- ASP.NET Core DI configuration
- Health monitoring and metrics
- Error handling and logging

See the `examples/` directory for complete, runnable code snippets.

## CacheManager

The `CacheManager` class provides an in-memory caching solution with TTL (Time-To-Live) support and comprehensive statistics tracking. It's designed for caching frequently accessed data to improve performance while supporting expiration policies and automatic cleanup of stale entries. The cache automatically removes expired entries during periodic cleanup cycles.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddSingleton<CacheManager>();

// In your service or controller
var cacheManager = app.Services.GetRequiredService<CacheManager>();

// Store a value with default TTL (5 minutes)
cacheManager.Set("user:123:profile", userProfile);

// Store a value with custom TTL (30 seconds)
cacheManager.Set("config:api-endpoint", apiEndpoint, TimeSpan.FromSeconds(30));

// Retrieve a value from cache
if (cacheManager.TryGet("user:123:profile", out UserProfile? cachedProfile))
{
    Console.WriteLine($"Retrieved from cache: {cachedProfile?.Name}");
}
else
{
    Console.WriteLine("Cache miss - fetching from source");
    // Fetch from database or API
    var freshProfile = await userService.GetProfileAsync("123");
    cacheManager.Set("user:123:profile", freshProfile);
}

// Get or set with factory pattern (automatic caching)
var settings = await cacheManager.GetOrSetAsync(
    "app:settings",
    async () => await configService.LoadSettingsAsync(),
    TimeSpan.FromHours(1)
);

// Check if key exists
bool exists = cacheManager.Contains("user:123:profile");

// Get remaining TTL for a cache entry
TimeSpan? ttl = cacheManager.GetTimeToLive("user:123:profile");
if (ttl.HasValue)
{
    Console.WriteLine($"TTL remaining: {ttl.Value.TotalSeconds:F0} seconds");
}

// Remove specific entry
cacheManager.Remove("user:123:profile");

// Remove entries matching pattern (e.g., all user profiles)
cacheManager.RemovePattern("user:*");

// Clear entire cache (useful for testing or during maintenance)
cacheManager.Clear();

// Get cache statistics for monitoring
var stats = cacheManager.GetStatistics();
Console.WriteLine($"Cache entries: {stats.EntryCount}");
Console.WriteLine($"Total hits: {stats.TotalHits}");
Console.WriteLine($"Average hits per entry: {stats.AverageHitsPerEntry:F2}");

// Update expiration for existing entry
cacheManager.SetExpiration("temp:data", TimeSpan.FromMinutes(10));

// Dispose when application shuts down (automatically cleans up resources)
cacheManager.Dispose();
```

## RequestContextManager

The `RequestContextManager` class provides ambient request context management for tracking request-scoped data across async operations. It enables correlation logging, cross-cutting concerns, and request lifecycle tracking without explicit parameter passing. The manager uses `AsyncLocal` storage to maintain context per-request and automatically cleans up when requests complete.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddRequestContextManager();
builder.Services.AddSingleton<RequestContextManager>();

// In your ASP.NET Core middleware or controller
var contextManager = app.Services.GetRequiredService<RequestContextManager>();

// Create a request context at the start of a request
var context = contextManager.CreateContext(
    requestId: Guid.NewGuid().ToString(),
    userId: "user-123",
    metadata: new Dictionary<string, string> { { "correlation-id", Guid.NewGuid().ToString() } }
);

Console.WriteLine($"Created context: {context.RequestId}");

// Access context properties
string? requestId = contextManager.GetRequestId();
string? userId = contextManager.GetUserId();

// Store and retrieve metadata
contextManager.SetMetadata("tracking-id", "track-456");
string? trackingId = contextManager.GetMetadata("tracking-id");

// Record completion time when request finishes
contextManager.RecordElapsedTime();

// Check if context is active
bool isActive = contextManager.IsContextActive();

// Clear context when done (typically in middleware's finally block)
contextManager.Clear();
```

## AuthenticationContext

The `AuthenticationContext` class represents authentication state for requests and streaming operations in the gRPC-Web Bridge. It encapsulates user identity, authentication scheme, roles, claims, and token information, providing utilities for role-based authorization, claim management, and expiration handling.

Example usage:

```csharp
// Create an authenticated context for a user
var authContext = new AuthenticationContext(
    userId: "user-12345",
    scheme: AuthenticationScheme.Bearer,
    token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTEyMzQ1Iiwicm9sZXMiOlsiYWRtaW4iLCJ1c2VyIl0sIm5hbWUiOiJKb2huIERvZSIsImV4cCI6MTc5OTk5OTk5OSwiaWF0IjoxNjk5OTk5OTk5fQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
);

// Add roles to the user
authContext.AddRole("admin");
authContext.AddRole("user");

// Add claims
authContext.AddClaim("email", "john.doe@example.com");
authContext.AddClaim("department", "engineering");

// Check roles
authContext.HasRole("admin"); // true
authContext.HasAnyRole("user", "guest"); // true
authContext.HasAllRoles("admin", "user"); // true

// Access user information
Console.WriteLine($"User: {authContext.UserId} ({authContext.Username ?? "unknown"})");
Console.WriteLine($"Authenticated: {authContext.IsAuthenticated}");
Console.WriteLine($"Roles: {string.Join(", ", authContext.Roles)}");
Console.WriteLine($"Claims: {authContext.Claims.Count} claims");

// Set expiration (e.g., 30 minutes from now)
authContext.SetExpiration(minutesFromNow: 30);
Console.WriteLine($"Expires in: {authContext.GetRemainingTime().TotalMinutes:F0} minutes");

// Add custom data for application-specific needs
authContext.AddCustomData("preferences", new { theme = "dark", notifications = true });

// Validate the context before use
authContext.Validate();

// Create an anonymous context for unauthenticated requests
var anonymousContext = new AuthenticationContext
{
    Scheme = AuthenticationScheme.None,
    IsAuthenticated = false
};

// Check if expired
if (authContext.IsExpired)
{
    Console.WriteLine("Token has expired!");
}
```

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides global error handling for the gRPC-Web Bridge application. It catches unhandled exceptions during request processing, converts them to appropriate HTTP responses with structured error details, and ensures consistent error formatting across all endpoints. The middleware maps various exception types to appropriate HTTP status codes and provides detailed error information including timestamps, request paths, and trace identifiers for debugging.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddControllers();

// In Program.cs - register the error handling middleware
var app = builder.Build();

// Add error handling middleware at the beginning of the pipeline
// This should be registered before other middleware that might throw exceptions
app.UseErrorHandling();

// Your other middleware and endpoints
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Example controller that might throw exceptions
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    
    public UserController(ILogger<UserController> logger)
    {
        _logger = logger;
    }
    
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        // If an exception occurs, ErrorHandlingMiddleware will catch it
        // and return a structured JSON error response
        var user = await _userService.GetUserAsync(userId);
        return Ok(user);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        // Validation errors, service exceptions, etc. will be handled
        var result = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetUser), new { userId = result.Id }, result);
    }
}

// Example of the structured error response format:
// {
//   "success": false,
//   "error": "Invalid Request",
//   "message": "Required parameter missing: userId",
//   "details": {
//     "exception": "ArgumentNullException",
//     "paramName": "userId"
//   },
//   "path": "/api/user",
//   "traceId": "00-1234567890abcdef1234567890abcdef-1234567890abcdef-00",
//   "timestamp": "2024-07-16T10:30:00Z"
// }
```

## BidirectionalStreamingEngineTests

The `BidirectionalStreamingEngineTests` class provides comprehensive integration tests for the `BidirectionalStreamingEngine`, covering backpressure scenarios, producer timeouts, event emission, adaptive flow control, and resource cleanup. The tests verify credit window saturation behavior, throttling detection, timeout handling, event publication, and proper stream lifecycle management.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddSingleton<BidirectionalStreamingEngine>();

// In your service or controller
var engine = app.Services.GetRequiredService<BidirectionalStreamingEngine>();

// Open a bidirectional stream
var stream = await engine.OpenStreamAsync("stream-123", MethodType.BidirectionalStreaming);

// Write messages with backpressure handling
await stream.WriteAsync(new StreamMessage("stream-123", 1, messageData));

// Check backpressure status
bool isThrottled = stream.BackpressureController.IsThrottled;
int availableCredits = stream.BackpressureController.AvailableCredits;

// Release credits to allow more writes
stream.BackpressureController.ReleaseCredit(2);

// Monitor stream events
var eventBus = app.Services.GetRequiredService<EventBus>();
eventBus.Subscribe<BackpressureChangedEvent>(e => {
    Console.WriteLine($"Stream {e.StreamId} backpressure changed: {e.IsThrottled}");
});

// Close stream when done
await engine.CloseStreamAsync("stream-123");

// Get metrics for monitoring
var metrics = engine.GetAllMetrics();
Console.WriteLine($"Active streams: {metrics.Count}");
```

## ErrorHandlingMiddlewareTests

The `ErrorHandlingMiddlewareTests` class provides comprehensive unit tests for the `ErrorHandlingMiddleware` class, verifying that it correctly handles various exception types and returns appropriate HTTP status codes with structured error responses. The tests cover exception-to-status-code mappings, response structure validation, and proper middleware pipeline behavior, ensuring the error handling middleware behaves as expected across different error scenarios.

Example usage:

```csharp
// Example test setup
public sealed class ErrorHandlingMiddlewareTests
{
    private static ErrorHandlingMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<ErrorHandlingMiddleware>.Instance);

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/grpc/test";
        context.TraceIdentifier = "trace-123";
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WithServiceRegistrationException_Returns400()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ServiceRegistrationException("Registration failed"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Service Registration Failed", body);
    }

    [Fact]
    public async Task InvokeAsync_WithStreamingException_Returns500()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new StreamingException("Stream broken"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Streaming Operation Failed", body);
    }

    [Fact]
    public async Task InvokeAsync_WithProtocolException_Returns400()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ProtocolException("Bad protocol"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Protocol Translation Failed", body);
    }

    [Fact]
    public async Task InvokeAsync_WithGrpcWebBridgeException_Returns500()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new GrpcWebBridgeException("Bridge error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Bridge Operation Failed", body);
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentNullException_Returns400()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentNullException("myParam"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Invalid Request", body);
        Assert.Contains("myParam", body);
    }

    [Fact]
    public async Task InvokeAsync_WithArgumentException_Returns400()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("Bad argument"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Invalid Argument", body);
    }

    [Fact]
    public async Task InvokeAsync_WithUnauthorizedAccessException_Returns401()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new UnauthorizedAccessException());

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Unauthorized", body);
    }

    [Fact]
    public async Task InvokeAsync_WithTimeoutException_Returns504()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new TimeoutException("Timed out"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status504GatewayTimeout, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Operation Timeout", body);
    }

    [Fact]
    public async Task InvokeAsync_WithOperationCanceledException_Returns400()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new OperationCanceledException("Cancelled"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Operation Cancelled", body);
    }

    [Fact]
    public async Task InvokeAsync_WithUnknownException_Returns500WithInternalServerError()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new InvalidProgramException("Unknown error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Internal Server Error", body);
    }

    [Fact]
    public async Task InvokeAsync_ErrorResponse_ContainsExpectedJsonFields()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new ArgumentException("test arg error"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("success", out _));
        Assert.True(root.TryGetProperty("error", out _));
        Assert.True(root.TryGetProperty("message", out _));
        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.True(root.TryGetProperty("path", out _));
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextAndDoesNotModifyResponse()
    {
        // Arrange
        bool nextCalled = false;
        var context = CreateContext();
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_SetsContentTypeToJson()
    {
        // Arrange
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new Exception("test"));

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains("application/json", context.Response.ContentType);
    }
}
```

## ServiceRegistryTests

The `ServiceRegistryTests` class provides comprehensive unit tests for the `ServiceRegistry` class, which manages gRPC service registration, discovery, and lifecycle operations. The tests verify service registration scenarios, duplicate service handling, service retrieval, service listing, unregistration, existence checking, and status updates.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddSingleton<ServiceRegistry>();

// In your service or controller
var serviceRegistry = app.Services.GetRequiredService<ServiceRegistry>();

// Register a new gRPC service
var userService = new GrpcService(
    name: "UserService",
    packageName: "user.package",
    endpoint: "localhost",
    port: 50051
);
userService.AddMethod(new GrpcMethod(
    name: "GetUser",
    fullName: "user.package.UserService.GetUser",
    type: MethodType.Unary,
    inputType: "UserRequest",
    outputType: "UserResponse"
));
serviceRegistry.RegisterService(userService);

// Check if a service exists
bool exists = serviceRegistry.ServiceExists("user.package.UserService");
Console.WriteLine($"UserService registered: {exists}");

// Get a registered service
var retrievedService = serviceRegistry.GetService("user.package.UserService");
if (retrievedService != null)
{
    Console.WriteLine($"Service endpoint: {retrievedService.Endpoint}:{retrievedService.Port}");
    Console.WriteLine($"Service status: {retrievedService.Status}");
}

// List all registered services
var allServices = serviceRegistry.ListServices();
Console.WriteLine($"Total registered services: {allServices.Count}");

// Update service status
serviceRegistry.UpdateServiceStatus("user.package.UserService", ServiceStatus.NotServing);

// Unregister a service when it's no longer available
bool unregistered = serviceRegistry.UnregisterService("user.package.UserService");
Console.WriteLine($"Service unregistered: {unregistered}");

// Get count of registered services
int serviceCount = serviceRegistry.RegisteredServiceCount;
Console.WriteLine($"Currently registered services: {serviceCount}");
```

## ClientRateLimitTests

The `ClientRateLimitTests` class provides comprehensive unit tests for the `ClientRateLimit` class, which implements a sliding-window rate limiter for tracking client requests. The tests verify rate limiting behavior, request counting, stale state detection, and thread safety under concurrent access scenarios.

Example usage:

```csharp
// Create a new rate limiter instance
var rateLimit = new ClientRateLimit();

// Allow requests within rate limit (5 requests per second)
for (int i = 0; i < 5; i++)
{
    bool allowed = rateLimit.AllowRequest(5, 1); // maxRequests=5, windowSeconds=1
    Console.WriteLine($"Request {i + 1} allowed: {allowed}");
}

// Check request count
int count = rateLimit.GetRequestCount(10); // windowSeconds=10
Console.WriteLine($"Total requests in last 10 seconds: {count}");

// Check if stale (no requests made yet)
bool isStale = rateLimit.IsStale(TimeSpan.FromSeconds(1));
Console.WriteLine($"Is stale: {isStale}");

// Make some requests
rateLimit.AllowRequest(100, 60); // 100 requests per 60 seconds
rateLimit.AllowRequest(100, 60);
rateLimit.AllowRequest(100, 60);

// Check count again
count = rateLimit.GetRequestCount(60);
Console.WriteLine($"Total requests in last 60 seconds: {count}");

// Check stale status after recent activity
isStale = rateLimit.IsStale(TimeSpan.FromMinutes(5));
Console.WriteLine($"Is stale after activity: {isStale}");

// Test concurrent access safety
var concurrentLimit = new ClientRateLimit();
Parallel.For(0, 200, _ =>
{
    concurrentLimit.AllowRequest(1000, 60);
});

int finalCount = concurrentLimit.GetRequestCount(60);
Console.WriteLine($"Concurrent requests processed: {finalCount}");
```

## ContentTypeValidationMiddlewareTests

The `ContentTypeValidationMiddlewareTests` class provides comprehensive unit tests for the `ContentTypeValidationMiddleware` class, verifying that it correctly validates content types for gRPC-Web requests. The tests ensure that only valid gRPC and gRPC-Web content types are allowed to pass through the middleware, while invalid content types result in HTTP 415 (Unsupported Media Type) responses with JSON error bodies.

The test suite covers:
- Valid gRPC-Web content types (application/grpc-web, application/grpc-web+proto, application/grpc-web-text, application/grpc-web-text+proto, application/grpc+proto, application/grpc)
- Invalid content types that should be rejected (application/json, text/plain, application/xml, multipart/form-data)
- Missing content type scenarios
- Excluded paths that bypass validation entirely (/api/services, /swagger/index.html, /openapi/v1.json, /health, /metrics, /_internal)
- Non-POST HTTP methods that bypass validation
- JSON error response body formatting

Example usage:

```csharp
// Example test setup
var logger = NullLogger<ContentTypeValidationMiddleware>.Instance;
var middleware = new ContentTypeValidationMiddleware(next, logger);

// Create a POST request context with valid gRPC-Web content type
var context = new DefaultHttpContext();
context.Request.Method = HttpMethods.Post;
context.Request.Path = "/grpc/UserService/GetUser";
context.Request.ContentType = "application/grpc-web+proto";
context.Response.Body = new MemoryStream();

// Invoke the middleware
await middleware.InvokeAsync(context);

// Assert that the request passed validation
Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);

// Create a POST request context with invalid content type
var invalidContext = new DefaultHttpContext();
invalidContext.Request.Method = HttpMethods.Post;
invalidContext.Request.Path = "/grpc/UserService/GetUser";
invalidContext.Request.ContentType = "application/json";
invalidContext.Response.Body = new MemoryStream();

// Invoke the middleware
await middleware.InvokeAsync(invalidContext);

// Assert that the request was rejected
Assert.Equal(StatusCodes.Status415UnsupportedMediaType, invalidContext.Response.StatusCode);

// Read the error response body
invalidContext.Response.Body.Position = 0;
using var reader = new StreamReader(invalidContext.Response.Body, Encoding.UTF8);
var errorBody = await reader.ReadToEndAsync();
Assert.Contains("Unsupported Media Type", errorBody);
```

## RequestLoggingMiddlewareTests

The `RequestLoggingMiddlewareTests` class provides comprehensive unit tests for the `RequestLoggingMiddleware` class, verifying that it correctly logs gRPC-Web requests and responses while maintaining proper middleware pipeline behavior. The tests ensure that logging occurs for various scenarios including successful requests, error conditions, excluded paths, different content types, and authorization scenarios.

The test suite covers:
- Successful request/response logging with body forwarding
- Error handling scenarios that don't throw exceptions
- Excluded path handling (paths like /health, /metrics, /_internal)
- Various HTTP status codes
- Authorization header presence
- JSON request body handling
- Binary content type scenarios
- Non-gRPC path handling
- Constructor validation with valid arguments

Example usage:

```csharp
// Example test setup
var logger = NullLogger<RequestLoggingMiddleware>.Instance;
var httpContextAccessor = new Mock<IHttpContextAccessor>();
var middleware = new RequestLoggingMiddleware(
    next: (innerHttpContext) => Task.CompletedTask,
    logger: logger,
    httpContextAccessor: httpContextAccessor.Object
);

// Create a test context with a JSON request
var context = new DefaultHttpContext();
context.Request.Method = HttpMethods.Post;
context.Request.Path = "/grpc/UserService/GetUser";
context.Request.ContentType = "application/grpc-web+proto";
context.Request.Headers["Authorization"] = "Bearer test-token";
context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"userId\":\"123\"}"));
context.Response.Body = new MemoryStream();
context.Response.StatusCode = StatusCodes.Status200OK;

// Set up HttpContextAccessor
var httpContext = new DefaultHttpContext();
httpContext.Request = context.Request;
httpContext.Response = context.Response;
httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

// Invoke the middleware
await middleware.InvokeAsync(context);

// Assert that the middleware completed without throwing
Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
```

## ValidationUtilityTests

The `ValidationUtilityTests` class provides comprehensive unit tests for the `ValidationUtility` class, which offers validation and sanitization utilities for common data validation scenarios. The tests verify email format validation, method name validation, service ID validation, and input sanitization to prevent XSS attacks.

Example usage:

```csharp
// Example test setup
var logger = NullLogger<ValidationUtilityTests>.Instance;

// Test email validation with valid format
var (isValidEmail, emailError) = ValidationUtility.ValidateEmail("user@example.com");
Console.WriteLine($"Email valid: {isValidEmail}, Error: {emailError}");

// Test email validation with missing domain
var (isInvalidEmail, invalidEmailError) = ValidationUtility.ValidateEmail("user@");
Console.WriteLine($"Email invalid: {isInvalidEmail}, Error: {invalidEmailError}");

// Test method name validation (should start with letter)
var (isValidMethod, methodError) = ValidationUtility.ValidateMethodName("GetUser");
Console.WriteLine($"Method valid: {isValidMethod}, Error: {methodError}");

// Test method name validation with digit at start (should fail)
var (isInvalidMethod, invalidMethodError) = ValidationUtility.ValidateMethodName("1GetUser");
Console.WriteLine($"Method invalid: {isInvalidMethod}, Error: {invalidMethodError}");

// Test service ID validation (allows dots and hyphens)
var (isValidService, serviceError) = ValidationUtility.ValidateServiceId("my-service.v1");
Console.WriteLine($"Service ID valid: {isValidService}, Error: {serviceError}");

// Test input sanitization to prevent XSS
var sanitized = ValidationUtility.SanitizeInput("<script>alert('xss')</script>");
Console.WriteLine($"Sanitized input: {sanitized}");
```

## CorrelationIdManager

The `CorrelationIdManager` class provides distributed tracing and correlation ID management for tracking requests across multiple services and components. It enables request lifecycle tracking, metadata storage, and comprehensive observability through trace hierarchies and statistics.

Example usage:
```csharp
// Configure services in Program.cs
builder.Services.AddCorrelationIdManager();
builder.Services.AddLogging(configure => configure.AddConsole());

// In your ASP.NET Core middleware or controller
var correlationIdManager = app.Services.GetRequiredService<CorrelationIdManager>();

// Get or create a correlation ID (automatically creates one if not set)
string correlationId = correlationIdManager.GetOrCreateCorrelationId();
Console.WriteLine($"Correlation ID: {correlationId}");

// Set a specific correlation ID from incoming request headers
string incomingCorrelationId = httpContext.Request.Headers["X-Correlation-ID"];
if (!string.IsNullOrEmpty(incomingCorrelationId))
{
    correlationIdManager.SetCorrelationId(incomingCorrelationId);
}

// Start a new trace for an operation
var trace = correlationIdManager.StartTrace(
    operationName: "ProcessOrder",
    parentTraceId: null,
    metadata: new Dictionary<string, string> { { "userId", "user-123" }, { "orderId", "order-456" } }
);

Console.WriteLine($"Trace started: {trace.TraceId}");

// Add metadata to the trace
correlationIdManager.AddTraceMetadata(trace.TraceId, "paymentMethod", "credit-card");
correlationIdManager.AddTraceMetadata(trace.TraceId, "amount", "99.99");

// Complete the trace when operation finishes
correlationIdManager.CompleteTrace(trace.TraceId, success: true);

// Get a specific trace
var retrievedTrace = correlationIdManager.GetTrace(trace.TraceId);
if (retrievedTrace != null)
{
    Console.WriteLine($"Trace duration: {retrievedTrace.GetDuration()?.TotalMilliseconds}ms");
    Console.WriteLine($"Success: {retrievedTrace.Success}");
}

// Get all traces for this correlation ID
var allTraces = correlationIdManager.GetTracesForCorrelation(correlationId);
Console.WriteLine($"Total traces for correlation: {allTraces.Count}");

// Get statistics about all traces
var stats = correlationIdManager.GetStatistics();
Console.WriteLine($"Total traces: {stats.totalTraces}");
Console.WriteLine($"Successful traces: {stats.successfulTraces}");

// Clean up old traces (older than 5 minutes)
int cleanedCount = correlationIdManager.CleanupOldTraces(TimeSpan.FromMinutes(5));
Console.WriteLine($"Cleaned {cleanedCount} old traces");

// Clear all traces when needed
correlationIdManager.ClearAllTraces();

// Clear the correlation ID when request completes
correlationIdManager.ClearCorrelationId();
```