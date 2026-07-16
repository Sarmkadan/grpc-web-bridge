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

## CorrelationIdManagerExtensions

The `CorrelationIdManagerExtensions` class provides utilities for managing distributed tracing correlation IDs and tracking request lifecycles. It supports trace creation, status inspection, and cleanup of expired traces.

Example usage:
```csharp
// Start a new trace with auto-generated correlation ID
var trace = CorrelationIdManagerExtensions.StartTraceWithAutoCorrelation("MyTrace");

// Check if any traces exist
if (CorrelationIdManagerExtensions.HasTraces)
{
    // Get most recent trace
    var recentTrace = CorrelationIdManagerExtensions.GetMostRecentTrace();
    
    // Check trace status
    if (CorrelationIdManagerExtensions.IsTraceSuccessful(recentTrace.Id))
    {
        Console.WriteLine($"Trace duration: {CorrelationIdManagerExtensions.GetTraceDuration(recentTrace.Id)}");
    }
    else
    {
        Console.WriteLine($"Error: {CorrelationIdManagerExtensions.GetTraceError(recentTrace.Id)}");
    }
    
    // Clean up old traces
    int cleaned = CorrelationIdManagerExtensions.CleanupOldTraces(TimeSpan.FromMinutes(5));
    Console.WriteLine($"Cleaned {cleaned} old traces");
}
```

## ConfigurationExceptionExtensions

The `ConfigurationExceptionExtensions` class provides extension methods for `ConfigurationException` that enable fluent validation and common operations for configuration-related errors. It allows you to chain configuration properties, check for specific keys, and format detailed error messages.

Example usage:

```csharp
// Create a configuration exception with key and value
var configException = new ConfigurationException(
    "ConnectionString",
    "server=localhost;user=admin",
    "Failed to connect to database"
);

// Check if the exception contains a specific key
bool hasKey = configException.HasKey("ConnectionString");
Console.WriteLine($"Has connection string key: {hasKey}");

// Get a formatted error message with all configuration details
string formattedMessage = configException.GetFormattedMessage();
Console.WriteLine(formattedMessage);
```

## CsvFormatterExtensions

The `CsvFormatterExtensions` class provides extension methods for `CsvFormatter` that enable advanced CSV processing capabilities including file operations, streaming, and batch processing. It supports appending data to existing files, converting collections to CSV streams, merging multiple CSV files, and splitting large CSV files into smaller chunks.

Example usage:

```csharp
// Create a CSV formatter instance
var csvFormatter = new CsvFormatter();

// Example data model
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Convert a collection to CSV stream for HTTP response
var users = new List<User>
{
    new User { Id = 1, Name = "Alice", Email = "alice@example.com", CreatedAt = DateTime.UtcNow },
    new User { Id = 2, Name = "Bob", Email = "bob@example.com", CreatedAt = DateTime.UtcNow }
};

using var csvStream = csvFormatter.ToCsvStream(users);
// Stream can be returned from ASP.NET Core controller or used in other scenarios

// Append data to an existing CSV file
await csvFormatter.AppendToFileAsync(users, "users.csv");

// Merge multiple CSV files into one
await csvFormatter.MergeFilesAsync(
    "merged_users.csv",
    "users_part1.csv",
    "users_part2.csv"
);

// Split a large CSV file into smaller files (e.g., 1000 rows per file)
await csvFormatter.SplitFileAsync<User>(
    "large_users.csv",
    "split_output",
    rowsPerFile: 1000
);
```

## XmlFormatterExtensions

The `XmlFormatterExtensions` class provides extension methods for `XmlFormatter` that enable advanced XML processing capabilities including validation, LINQ-to-XML conversion, and XPath querying. It supports creating formatted XML strings with various options, validating XML content, extracting data using XPath expressions, and converting between XML formats and LINQ-to-XML objects.

Example usage:

```csharp
// Create an XML formatter instance
var xmlFormatter = new XmlFormatter();

// Example XML data
string xmlData = @"
<root>
  <person id="1">
    <name>Alice</name>
    <email>alice@example.com</email>
    <age>30</age>
  </person>
  <person id="2">
    <name>Bob</name>
    <email>bob@example.com</email>
    <age>25</age>
  </person>
</root>
";

// Validate XML content
bool isValid = xmlFormatter.IsValidXml(xmlData);
Console.WriteLine($"Is valid XML: {isValid}");

// Get root element name
string? rootName = xmlFormatter.GetRootElementName(xmlData);
Console.WriteLine($"Root element: {rootName}");

// Convert XML to XDocument for LINQ operations
var xDoc = xmlFormatter.ToXDocument(xmlData);
if (xDoc != null)
{
  Console.WriteLine($"Document has {xDoc.Descendants().Count()} total elements");
}

// Convert XML to XElement for LINQ operations
var xElement = xmlFormatter.ToXElement(xmlData);
if (xElement != null)
{
  Console.WriteLine($"Element name: {xElement.Name.LocalName}");
}

// Count elements matching XPath expression
int personCount = xmlFormatter.CountElementsByXPath(xmlData, "//person");
Console.WriteLine($"Found {personCount} person elements");

// Get all person names using XPath
var names = xmlFormatter.GetElementValuesByXPath(xmlData, "//person/name");
Console.WriteLine("Person names:");
foreach (var name in names)
{
  Console.WriteLine($" - {name}");
}

// Create formatter with custom options
var formattedXmlFormatter = new XmlFormatter(new XmlFormatterOptions
{
  Indent = true,
  IndentChars = "  ",
  OmitXmlDeclaration = false,
  OmitNamespaces = false,
  Encoding = Encoding.UTF8
});

// Format XML with indentation
string formattedXml = formattedXmlFormatter.Format(xmlData);
Console.WriteLine(formattedXml);
```

## ProtobufUtility

The `ProtobufUtility` class provides comprehensive utilities for working with Protocol Buffers messages in the gRPC-Web Bridge. It offers conversion between Protobuf, JSON, and binary formats, message cloning, validation, compression, and metadata introspection capabilities. This utility simplifies common Protobuf operations while maintaining type safety and performance.

Example usage:

```csharp
// Define a sample Protobuf message (using Google.Protobuf.IMessage)
// For demonstration, we'll use a simple message structure
public class Person : Google.Protobuf.IMessage
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
    
    public Google.Protobuf.MessageDescriptor Descriptor => throw new NotImplementedException();
    public int CalculateSize() => 0;
    public void MergeFrom(Google.Protobuf.IMessage other) {}
    public void MergeFrom(Google.Protobuf.CodedInputStream input) {}
    public void WriteTo(Google.Protobuf.CodedOutputStream output) {}
}

// Serialize a Protobuf message to JSON
var person = new Person { Name = "Alice", Age = 30, Email = "alice@example.com" };
string json = ProtobufUtility.ToJson(person);
Console.WriteLine(json);

// Parse JSON back to a Protobuf message
var parsedPerson = ProtobufUtility.FromJson<Person>(json);
Console.WriteLine($"Parsed: {parsedPerson?.Name}, {parsedPerson?.Age}");

// Convert a Protobuf message to byte array
byte[] bytes = ProtobufUtility.ToBytes(person);
Console.WriteLine($"Message size: {bytes.Length} bytes");

// Parse a byte array back to a Protobuf message
var fromBytes = ProtobufUtility.FromBytes<Person>(bytes);
Console.WriteLine($"From bytes: {fromBytes?.Name}");

// Get the size of a serialized message
int messageSize = ProtobufUtility.GetMessageSize(person);
Console.WriteLine($"Message size: {messageSize} bytes");

// Convert a Protobuf message to a dictionary for inspection
var personDict = ProtobufUtility.ToDict(person);
Console.WriteLine($"Dictionary has {personDict.Count} entries");

// Clone a Protobuf message (deep copy)
var clonedPerson = ProtobufUtility.Clone(person);
Console.WriteLine($"Cloned: {ProtobufUtility.AreEqual(person, clonedPerson)}");

// Merge multiple Protobuf messages
var person2 = new Person { Name = "Alice Updated", Age = 31 };
var mergedPerson = ProtobufUtility.Merge(person, person2);
Console.WriteLine($"Merged name: {mergedPerson.Name}");

// Validate a Protobuf message
var (isValid, errors) = ProtobufUtility.Validate(person);
Console.WriteLine($"Valid: {isValid}, Errors: {errors.Count}");

// Compress and decompress a Protobuf message
string compressed = ProtobufUtility.CompressMessage(person);
Console.WriteLine($"Compressed size: {compressed.Length} chars");
var decompressedPerson = ProtobufUtility.DecompressMessage<Person>(compressed);
Console.WriteLine($"Decompressed: {decompressedPerson?.Name}");

// Get metadata about a Protobuf message type
var metadata = ProtobufUtility.GetMessageMetadata<Person>();
Console.WriteLine($"Message: {metadata.Name} ({metadata.FieldCount} fields)");
foreach (var field in metadata.Fields)
{
    Console.WriteLine($"  - {field.Name}: {field.Type} (Required: {field.IsRequired})");
}
```

## ReflectionUtility

The `ReflectionUtility` class provides comprehensive reflection utilities for runtime type inspection, dynamic method invocation, and property manipulation. It simplifies working with .NET reflection APIs by offering strongly-typed, exception-safe methods for common reflection scenarios including method discovery, property access, interface checking, type hierarchy traversal, and custom attribute retrieval.

Example usage:

```csharp
// Define a sample model class
public class UserModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; } = new();
    
    public void AddRole(string role) => Roles.Add(role);
    public string GetDisplayName() => $"{Name} ({Email})" ;
}

// Usage examples

// 1. Get all public methods of a type
var methods = ReflectionUtility.GetPublicMethods(typeof(UserModel));
Console.WriteLine($"Public methods: {methods.Count}");

// 2. Get methods with a specific filter (e.g., no parameters)
var parameterlessMethods = ReflectionUtility.GetPublicMethods(
    typeof(UserModel),
    m => m.GetParameters().Length == 0
);

// 3. Get all public properties
var properties = ReflectionUtility.GetPublicProperties(typeof(UserModel));
Console.WriteLine($"Properties: {string.Join(", ", properties.Select(p => p.Name))}");

// 4. Check if type implements an interface
bool implementsIList = ReflectionUtility.ImplementsInterface(
    typeof(List<string>),
    typeof(IList<>)
);

// 5. Get generic type arguments
var genericArgs = ReflectionUtility.GetGenericArguments(typeof(List<string>));
Console.WriteLine($"Generic args count: {genericArgs.Count}");

// 6. Invoke a method dynamically
var user = new UserModel { Id = 1, Name = "Alice", Email = "alice@example.com" };
ReflectionUtility.InvokeMethod(user, "AddRole", "admin");
ReflectionUtility.InvokeMethod(user, "GetDisplayName");

// 7. Get and set property values
var userId = ReflectionUtility.GetPropertyValue(user, "Id");
ReflectionUtility.SetPropertyValue(user, "Name", "Alice Updated");

// 8. Convert object to dictionary
var userDict = ReflectionUtility.ObjectToDictionary(user);
foreach (var kvp in userDict)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// 9. Create instance dynamically
var newUserType = ReflectionUtility.FindType("GrpcWebBridge.Utilities.ReflectionUtility+UserModel")
    ?? typeof(UserModel);
var newUser = ReflectionUtility.CreateInstance(newUserType) as UserModel;

// 10. Get custom attributes
var attrs = ReflectionUtility.GetCustomAttributes<ObsoleteAttribute>(
    typeof(UserModel).GetMethod("GetDisplayName")!
);

// 11. Check if type is primitive or value type
bool isPrimitive = ReflectionUtility.IsPrimitiveOrValueType(typeof(int));
bool isString = ReflectionUtility.IsPrimitiveOrValueType(typeof(string));

// 12. Get type hierarchy
var hierarchy = ReflectionUtility.GetTypeHierarchy(typeof(List<string>));
Console.WriteLine($"Type hierarchy depth: {hierarchy.Count}");

// 13. Find type by name
var foundType = ReflectionUtility.FindType("System.String");
Console.WriteLine($"Found type: {foundType?.Name}");

// 14. Get assembly version
var assemblyVersion = ReflectionUtility.GetAssemblyVersion(typeof(UserModel));
Console.WriteLine($"Assembly version: {assemblyVersion}");

// 15. Check if method is async
var isAsync = ReflectionUtility.IsAsyncMethod(
    typeof(UserModel).GetMethod("AddRole")!
);
```

## CryptographyUtility

The `CryptographyUtility` class provides secure cryptographic operations including password hashing, token generation, data encryption, and message authentication. It uses industry-standard algorithms like PBKDF2, AES-256-GCM, HMAC-SHA256, and SHA-256 to ensure data integrity and security throughout the gRPC-Web Bridge application.

Example usage:

```csharp
// Hash a user password for secure storage
string password = "SecurePassword123!";
string hashedPassword = CryptographyUtility.HashPassword(password);
Console.WriteLine($"Hashed password: {hashedPassword}");

// Verify a password against stored hash
bool isValid = CryptographyUtility.VerifyPassword(password, hashedPassword);
Console.WriteLine($"Password valid: {isValid}");

// Generate a secure authentication token
string authToken = CryptographyUtility.GenerateToken(64);
Console.WriteLine($"Authentication token: {authToken}");

// Generate a human-readable API key
string apiKey = CryptographyUtility.GenerateApiKey(32);
Console.WriteLine($"API key: {apiKey}");

// Compute SHA256 hash for data integrity
string data = "Hello, gRPC-Web Bridge!";
string sha256Hash = CryptographyUtility.ComputeSha256(data);
Console.WriteLine($"SHA256 hash: {sha256Hash}");

// Compute HMAC-SHA256 for message authentication
string secretKey = "my-secret-key-123";
string hmacHash = CryptographyUtility.ComputeHmacSha256(data, secretKey);
Console.WriteLine($"HMAC-SHA256: {hmacHash}");

// Encrypt sensitive data with AES-256-GCM
string sensitiveData = "User SSN: 123-45-6789";
string encryptionKey = "32-character-long-secret-key-123456";
string encryptedData = CryptographyUtility.EncryptAes256(sensitiveData, encryptionKey);
Console.WriteLine($"Encrypted: {encryptedData}");

// Decrypt the data when needed
string decryptedData = CryptographyUtility.DecryptAes256(encryptedData, encryptionKey);
Console.WriteLine($"Decrypted: {decryptedData}");
```

## ValidationUtility

The `ValidationUtility` class provides comprehensive validation utilities for validating input data in the gRPC-Web Bridge. It includes methods for validating strings, emails, URLs, IP addresses, service IDs, method names, ranges, ports, and custom patterns, with consistent error message formatting and support for both individual and batch validation scenarios.

Example usage:

```csharp
// Validate a required non-empty string
var (isValid, error) = ValidationUtility.ValidateNotEmpty("user@example.com", "Email");
if (!isValid)
{
    Console.WriteLine($"Validation failed: {error}");
    return;
}

// Validate string length (between 3 and 50 characters)
var (lengthValid, lengthError) = ValidationUtility.ValidateStringLength("abc", 3, 50, "Username");
if (!lengthValid)
{
    Console.WriteLine($"Length validation failed: {lengthError}");
}

// Validate an email address format
var (emailValid, emailError) = ValidationUtility.ValidateEmail("invalid-email", "EmailAddress");
if (!emailValid)
{
    Console.WriteLine($"Email validation failed: {emailError}");
}

// Validate a URL format
var (urlValid, urlError) = ValidationUtility.ValidateUrl("not-a-url", "ServiceUrl");
if (!urlValid)
{
    Console.WriteLine($"URL validation failed: {urlError}");
}

// Validate an IP address
var (ipValid, ipError) = ValidationUtility.ValidateIpAddress("999.999.999.999", "ServerIp");
if (!ipValid)
{
    Console.WriteLine($"IP address validation failed: {ipError}");
}

// Validate a service ID format
var (serviceIdValid, serviceIdError) = ValidationUtility.ValidateServiceId("invalid-service-id", "ServiceId");
if (!serviceIdValid)
{
    Console.WriteLine($"Service ID validation failed: {serviceIdError}");
}

// Validate a method name format
var (methodValid, methodError) = ValidationUtility.ValidateMethodName("InvalidMethodName123!", "MethodName");
if (!methodValid)
{
    Console.WriteLine($"Method name validation failed: {methodError}");
}

// Validate a numeric range
var (rangeValid, rangeError) = ValidationUtility.ValidateRange(150, 0, 100, "Percentage");
if (!rangeValid)
{
    Console.WriteLine($"Range validation failed: {rangeError}");
}

// Validate a port number (0-65535)
var (portValid, portError) = ValidationUtility.ValidatePort(70000, "Port");
if (!portValid)
{
    Console.WriteLine($"Port validation failed: {portError}");
}

// Validate a collection has required keys
var dict = new Dictionary<string, string> { { "key1", "value1" } };
var (keysValid, keysError) = ValidationUtility.ValidateRequiredKeys(dict, new[] { "key1", "key2" }, "RequiredKeys");
if (!keysValid)
{
    Console.WriteLine($"Required keys validation failed: {keysError}");
}

// Validate against a regex pattern
var (patternValid, patternError) = ValidationUtility.ValidatePattern("abc123", "^[a-z]+\\d+$", "CustomPattern");
if (!patternValid)
{
    Console.WriteLine($"Pattern validation failed: {patternError}");
}

// Sanitize user input to prevent XSS and injection attacks
string maliciousInput = "<script>alert('xss')</script>";
string sanitized = ValidationUtility.SanitizeInput(maliciousInput);
Console.WriteLine($"Sanitized input: {sanitized}");

// Validate a JWT token format
var (jwtValid, jwtError) = ValidationUtility.ValidateJwtFormat("invalid.jwt.token", "AuthToken");
if (!jwtValid)
{
    Console.WriteLine($"JWT validation failed: {jwtError}");
}
```

## MetricsController

The `MetricsController` class provides comprehensive monitoring and metrics collection endpoints for the gRPC-Web Bridge server. It tracks system statistics including uptime, request rates, error counts, active streams, service health, and resource usage (memory, CPU). The controller also exposes method-level invocation statistics and streaming performance metrics for observability and debugging purposes.

Example usage:

```csharp
// In your ASP.NET Core application's Program.cs or Startup
builder.Services.AddControllers();

// The MetricsController is automatically registered when AddControllers() is called
// Access metrics endpoints:

// 1. Get comprehensive system metrics
var systemMetrics = await httpClient.GetFromJsonAsync<dynamic>("http://localhost:5000/api/metrics");
Console.WriteLine($"Uptime: {systemMetrics.data.systemMetrics.uptime.totalSeconds} seconds");
Console.WriteLine($"Active streams: {systemMetrics.data.streamMetrics.activeStreams}");
Console.WriteLine($"Error rate: {systemMetrics.data.requestMetrics.errorRate}%");

// 2. Get method-level invocation statistics
var methodMetrics = await httpClient.GetFromJsonAsync<dynamic>("http://localhost:5000/api/metrics/methods");
foreach (var method in methodMetrics.data.methods)
{
    Console.WriteLine($"{method.method}: {method.callCount} calls, {method.errorRate}% errors");
}

// 3. Get streaming performance metrics
var streamingMetrics = await httpClient.GetFromJsonAsync<dynamic>("http://localhost:5000/api/metrics/streaming");
Console.WriteLine($"Active streams: {streamingMetrics.data.activeStreamCount}");

// 4. Manually record method calls from your services
MetricsController.RecordMethodCall("UserService.GetUser");
MetricsController.RecordMethodCall("OrderService.CreateOrder");

// 5. Record method errors when they occur
MetricsController.RecordMethodError("PaymentService.ProcessPayment");

// 6. Reset metrics for baseline testing (admin operation)
var resetResponse = await httpClient.PostAsync("http://localhost:5000/api/metrics/reset", null);
if (resetResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Metrics reset successfully");
}
```

## BridgeController

The `BridgeController` class serves as the main REST endpoint interface for the gRPC-Web Bridge, implementing protocol translation between HTTP/gRPC-Web and gRPC services. It handles method invocation, streaming operations, and batch requests, providing a unified API for clients to interact with backend gRPC services through familiar REST conventions.

The controller routes requests to the appropriate gRPC service endpoints, manages authentication context, tracks performance metrics, and ensures proper resource cleanup for streaming operations.

Example usage:

```csharp
// Create a bridge client (typically via HttpClient)
var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

// Single method invocation
var invokeResponse = await httpClient.PostAsJsonAsync("api/bridge/invoke", new
{
    ServiceId = "UserService",
    MethodName = "GetUser",
    Payload = new { UserId = "123" },
    Headers = new Dictionary<string, string> { { "X-Request-Id", Guid.NewGuid().ToString() } },
    TimeoutMs = 30000
});

// Streaming messages to a gRPC service
var streamResponse = await httpClient.PostAsJsonAsync("api/bridge/stream", new
{
    ServiceId = "ChatService",
    MethodName = "StreamMessages",
    InitialMessage = new { RoomId = "general", UserId = "user-456" }
});

// Process streaming responses
using var stream = await streamResponse.Content.ReadAsStreamAsync();
using var reader = new StreamReader(stream);
while (!reader.EndOfStream)
{
    var line = await reader.ReadLineAsync();
    if (line != null)
    {
        var message = JsonSerializer.Deserialize<StreamMessage>(line);
        Console.WriteLine($"Received: {message?.Data}");
    }
}

// Batch invoke multiple methods
var batchResponse = await httpClient.PostAsJsonAsync("api/bridge/batch", new
{
    Operations = new List<object>
    {
        new
        {
            ServiceId = "UserService",
            MethodName = "GetUser",
            Payload = new { UserId = "1" }
        },
        new
        {
            ServiceId = "OrderService",
            MethodName = "GetOrders",
            Payload = new { UserId = "1", Status = "completed" }
        },
        new
        {
            ServiceId = "NotificationService",
            MethodName = "SendNotification",
            Payload = new { UserId = "1", Message = "Welcome!" }
        }
    }
});
```

## StreamUtility

The `StreamUtility` class provides comprehensive stream handling utilities for efficient data transfer operations in the gRPC-Web Bridge. It includes methods for chunked copying, compression/decompression, Base64 conversion, hashing, and multi-destination streaming (teeing) with optimized memory management and retry logic for robust data transfer operations.

Example usage:

```csharp
// Create a sample stream with some data
var sampleData = Encoding.UTF8.GetBytes("Hello, gRPC-Web Bridge! This is a test stream.");
using var sampleStream = new MemoryStream(sampleData);

// Copy stream with chunking (81920 bytes per chunk by default)
using var destinationStream = new MemoryStream();
await StreamUtility.CopyStreamChunkedAsync(sampleStream, destinationStream);
Console.WriteLine($"Copied {destinationStream.Length} bytes");

// Read entire stream to byte array (with 10MB size limit)
var bytes = await StreamUtility.ReadStreamToEndAsync(sampleStream);
Console.WriteLine($"Read {bytes.Length} bytes from stream");

// Create pipe reader/writer for high-performance streaming
var pipeReader = StreamUtility.CreatePipeReader(sampleStream);
var pipeWriter = StreamUtility.CreatePipeWriter(destinationStream);

// Compress and decompress streams
using var compressedStream = new MemoryStream();
using var compressedSource = new MemoryStream(sampleData);
await StreamUtility.CompressStreamAsync(compressedSource, compressedStream);

using var decompressedStream = new MemoryStream();
compressedStream.Seek(0, SeekOrigin.Begin);
await StreamUtility.DecompressStreamAsync(compressedStream, decompressedStream);

// Convert stream to Base64
compressedStream.Seek(0, SeekOrigin.Begin);
string base64String = await StreamUtility.StreamToBase64Async(compressedStream);
Console.WriteLine($"Base64 length: {base64String.Length} characters");

// Convert Base64 back to stream
using var base64Stream = StreamUtility.Base64ToStream(base64String);
Console.WriteLine($"Base64 stream length: {base64Stream.Length} bytes");

// Calculate stream hash (SHA256)
base64Stream.Seek(0, SeekOrigin.Begin);
string hash = await StreamUtility.CalculateStreamHashAsync(base64Stream, System.Security.Cryptography.SHA256.Create());
Console.WriteLine($"Stream hash: {hash}");

// Tee stream to multiple destinations (useful for logging/monitoring)
using var stream1 = new MemoryStream();
using var stream2 = new MemoryStream();
sampleStream.Seek(0, SeekOrigin.Begin);
await StreamUtility.TeeStreamAsync(sampleStream, stream1, stream2);
Console.WriteLine($"Tee streams: {stream1.Length} and {stream2.Length} bytes");

// Write with retry logic
using var retryStream = new MemoryStream();
await StreamUtility.WriteWithRetryAsync(retryStream, sampleData, maxRetries: 3, delayMs: 100);
Console.WriteLine($"Wrote with retry: {retryStream.Length} bytes");

// Check stream validity and get length
bool isValid = StreamUtility.IsStreamValid(sampleStream);
long? length = StreamUtility.GetStreamLength(sampleStream);
Console.WriteLine($"Stream valid: {isValid}, Length: {length}");
```

## CacheUtility

The `CacheUtility` class provides cache key generation and management utilities for consistent cache key formatting, pattern matching, and validation.

## DateTimeUtility

The `DateTimeUtility` class provides comprehensive date and time manipulation utilities for the gRPC-Web Bridge. It offers methods for ISO 8601 conversion, Unix timestamp handling, timezone conversions, relative time formatting, period calculations, business day counting, age calculation, and various date validation utilities.

Example usage:

```csharp
// Convert DateTime to ISO 8601 string
var now = DateTime.UtcNow;
string iso8601Date = DateTimeUtility.ToIso8601(now);
Console.WriteLine($"ISO 8601: {iso8601Date}");

// Parse ISO 8601 string back to DateTime
DateTime? parsedDate = DateTimeUtility.FromIso8601(iso8601Date);
Console.WriteLine($"Parsed date: {parsedDate}");

// Convert to Unix timestamp
long unixTimestamp = DateTimeUtility.ToUnixTimestamp(now);
Console.WriteLine($"Unix timestamp: {unixTimestamp}");

// Convert Unix timestamp back to DateTime
DateTime fromTimestamp = DateTimeUtility.FromUnixTimestamp(unixTimestamp);
Console.WriteLine($"From timestamp: {fromTimestamp}");

// Get human-readable relative time
var yesterday = DateTime.UtcNow.AddDays(-1);
string relativeTime = DateTimeUtility.ToRelativeTime(yesterday);
Console.WriteLine($"Relative time: {relativeTime}");

// Convert between timezones
var utcTime = DateTime.UtcNow;
DateTime easternTime = DateTimeUtility.ConvertToTimeZone(utcTime, "Eastern Standard Time");
Console.WriteLine($"Eastern time: {easternTime}");

// Get period start/end dates
var today = DateTime.Today;
DateTime monthStart = DateTimeUtility.GetPeriodStart(today, DateTimePeriod.Month);
DateTime monthEnd = DateTimeUtility.GetPeriodEnd(today, DateTimePeriod.Month);
Console.WriteLine($"Month: {monthStart:yyyy-MM-dd} to {monthEnd:yyyy-MM-dd}");

// Calculate business days between dates
int businessDays = DateTimeUtility.GetBusinessDaysBetween(
    new DateTime(2024, 1, 1),
    new DateTime(2024, 1, 31)
);
Console.WriteLine($"Business days in Jan 2024: {businessDays}");

// Calculate age from birthdate
int age = DateTimeUtility.GetAge(new DateTime(1990, 5, 15));
Console.WriteLine($"Age: {age} years");

// Check date properties
bool isWeekend = DateTimeUtility.IsWeekend(DateTime.Today);
bool isToday = DateTimeUtility.IsToday(DateTime.Today);
bool isFuture = DateTimeUtility.IsFuture(DateTime.Today.AddDays(1));
bool isPast = DateTimeUtility.IsPast(DateTime.Today.AddDays(-1));
Console.WriteLine($"Is weekend: {isWeekend}, Is today: {isToday}, Is future: {isFuture}, Is past: {isPast}");

// Round to nearest interval
var rounded = DateTimeUtility.RoundTo(DateTime.UtcNow, TimeSpan.FromMinutes(15));
Console.WriteLine($"Rounded to 15min: {rounded}");

// Format DateTime
string formatted = DateTimeUtility.Format(DateTime.UtcNow, "yyyy-MM-dd HH:mm:ss");
Console.WriteLine($"Formatted: {formatted}");

// Get duration string
string duration = DateTimeUtility.GetDurationString(
    DateTime.UtcNow.AddHours(-2),
    DateTime.UtcNow
);
Console.WriteLine($"Duration: {duration}");
```

## CacheUtility

The `CacheUtility` class provides cache key generation and management utilities for consistent cache key formatting, pattern matching, and validation. It includes methods for generating various types of cache keys (method calls, streams, services, authentication), sanitizing key components, and validating key formats.

Example usage:

```csharp
// Generate a simple cache key from components
string simpleKey = CacheUtility.GenerateKey("UserService", "GetUser", "123");
Console.WriteLine($"Simple key: {CacheUtility.FormatKeyForDebug(simpleKey)}");

// Generate a namespaced cache key for grouping related entries
string namespacedKey = CacheUtility.GenerateNamespacedKey("user", "profile", "123");
Console.WriteLine($"Namespaced key: {CacheUtility.FormatKeyForDebug(namespacedKey)}");

// Sanitize a key component (removes special characters)
string sanitized = CacheUtility.SanitizeKeyComponent("user@profile#123");
Console.WriteLine($"Sanitized: {sanitized}");

// Create a pattern key for prefix matching
string pattern = CacheUtility.CreatePatternKey("user:*");
Console.WriteLine($"Pattern: {pattern}");

// Check if a key matches a pattern
bool matches = CacheUtility.MatchesPattern("user:profile:123", pattern);
Console.WriteLine($"Pattern match: {matches}");

// Generate a method cache key for service method invocation
string methodKey = CacheUtility.GenerateMethodCacheKey("UserService", "GetUser", new { UserId = "123", IncludeDetails = true });
Console.WriteLine($"Method key: {CacheUtility.FormatKeyForDebug(methodKey)}");

// Generate a stream cache key
string streamKey = CacheUtility.GenerateStreamCacheKey("stream-abc-123");
Console.WriteLine($"Stream key: {CacheUtility.FormatKeyForDebug(streamKey)}");

// Generate a service cache key
string serviceKey = CacheUtility.GenerateServiceCacheKey("UserService");
Console.WriteLine($"Service key: {CacheUtility.FormatKeyForDebug(serviceKey)}");

// Generate an authentication cache key
string authKey = CacheUtility.GenerateAuthCacheKey("user-123");
Console.WriteLine($"Auth key: {CacheUtility.FormatKeyForDebug(authKey)}");

// Calculate a hash of a key
int keyHash = CacheUtility.GetKeyHash(methodKey);
Console.WriteLine($"Key hash: {keyHash}");

// Estimate the memory size of a cache key
long keySize = CacheUtility.EstimateKeySize(methodKey);
Console.WriteLine($"Key size estimate: {keySize} bytes");

// Parse a composite cache key back into components
string[] components = CacheUtility.ParseKey(methodKey);
Console.WriteLine($"Parsed components: {string.Join(", ", components)}");

// Validate a cache key
bool isValid = CacheUtility.IsValidKey(methodKey);
Console.WriteLine($"Key is valid: {isValid}");

// Format a key for debug output (truncates long keys)
string debugFormat = CacheUtility.FormatKeyForDebug(methodKey);
Console.WriteLine($"Debug format: {debugFormat}");
```

## JsonUtility

The `JsonUtility` class provides comprehensive JSON serialization and deserialization utilities for consistent JSON handling across the gRPC-Web Bridge application. It supports various serialization options, type-safe deserialization, dynamic object parsing, JSON merging, property manipulation, and schema validation with comprehensive error handling throughout.

Example usage:

```csharp
// Serialize an object to JSON string
var user = new { Name = "Alice", Age = 30, Email = "alice@example.com" };
string json = JsonUtility.Serialize(user);
Console.WriteLine(json); // {"name":"Alice","age":30,"email":"alice@example.com"}

// Serialize with indentation for debugging
string indentedJson = JsonUtility.Serialize(user, indented: true);
Console.WriteLine(indentedJson);

// Serialize with custom options
var customOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    WriteIndented = true
};
string customJson = JsonUtility.SerializeWithOptions(user, customOptions);

// Deserialize JSON to a strongly-typed object
string jsonData = "{\"name\":\"Bob\",\"age\":25,\"email\":\"bob@example.com\"}";
var deserializedUser = JsonUtility.Deserialize<User>(jsonData);
Console.WriteLine(deserializedUser?.Name); // Bob

// Deserialize to dynamic dictionary for flexible property access
string settingsJson = "{\"timeout\":30000,\"retries\":3,\"enabled\":true}";
var settingsDict = JsonUtility.DeserializeToDictionary(settingsJson);
if (settingsDict != null && settingsDict.TryGetValue("timeout", out var timeout))
{
    Console.WriteLine($"Timeout: {timeout}"); // Timeout: 30000
}

// Try to deserialize with error handling
bool success = JsonUtility.TryDeserialize(jsonData, out User? result, out string? error);
if (success && result != null)
{
    Console.WriteLine($"Deserialized user: {result.Name}");
}
else
{
    Console.WriteLine($"Deserialization failed: {error}");
}

// Merge two JSON objects
string baseConfig = "{\"timeout\":10000,\"retries\":2}";
string overrideConfig = "{\"timeout\":30000,\"enabled\":true}";
string merged = JsonUtility.MergeJson(baseConfig, overrideConfig);
Console.WriteLine(merged); // {"timeout":30000,"retries":2,"enabled":true}

// Get a property value from JSON
string userJson = "{\"user\":{\"name\":\"Charlie\",\"age\":35}}";
var nameValue = JsonUtility.GetPropertyValue(userJson, "user.name");
Console.WriteLine(nameValue); // "Charlie"

// Set a property value in JSON
string configJson = "{\"service\":{\"name\":\"UserService\"}}";
string updatedConfig = JsonUtility.SetPropertyValue(configJson, "service.timeout", 5000);
Console.WriteLine(updatedConfig); // {"service":{"name":"UserService","timeout":5000}}

// Validate required properties in JSON
string requestJson = "{\"userId\":\"123\",\"action\":\"create\"}";
bool isValid = JsonUtility.ValidateRequired(requestJson, "userId", "action");
Console.WriteLine(isValid); // true
```

## ConfigurationExceptionExtensions

Example usage:

```csharp
// Create a configuration exception with key and value
var configException = new ConfigurationException(
    "ConnectionString",
    "server=localhost;user=admin",
    "Failed to connect to database"
);

// Check if the exception contains a specific key
bool hasKey = configException.HasKey("ConnectionString");
Console.WriteLine($"Has connection string key: {hasKey}");

// Get a formatted error message with all configuration details
string formattedMessage = configException.GetFormattedMessage();
Console.WriteLine(formattedMessage);

// Create a new exception with updated message while preserving configuration
var updatedException = configException.WithMessage("Database connection timeout occurred");
Console.WriteLine(updatedException.Message);

// Create a new exception with updated key while preserving message and value
var keyUpdatedException = configException.WithKey("DatabaseConnectionString");
Console.WriteLine(keyUpdatedException.ConfigurationKey);

// Create a new exception with updated value while preserving message and key
var valueUpdatedException = configException.WithValue("server=prod-db;user=admin");
Console.WriteLine(valueUpdatedException.ConfigurationValue);

// Create a new exception with both updated key and value
var keyValueUpdatedException = configException.WithKeyValue(
    "DatabaseConnectionString",
    "server=prod-db;user=admin;timeout=30"
);
Console.WriteLine(keyValueUpdatedException.GetFormattedMessage());
```

## BackpressureController

The `BackpressureController` class implements a credit-based backpressure mechanism for streaming scenarios. It prevents unbounded heap growth by enforcing a configurable credit window that tracks in-flight message count between producers and consumers. The controller uses a `SemaphoreSlim` for async-friendly credit acquisition and a lock-free `FlowControlWindow` for utilization tracking, with all public methods designed for concurrent access.

Example usage:

```csharp
// Create a backpressure controller with flow control configuration
var logger = new Logger<BackpressureController>(new LoggerFactory());
var options = new FlowControlOptions
{
    InitialWindowSize = 100,
    MaxWindowSize = 1000,
    Mode = FlowControlMode.Enabled,
    BackpressureThreshold = 0.8, // 80% utilization
    MaxProducerWaitTime = TimeSpan.FromSeconds(30)
};

var controller = new BackpressureController(
    streamId: "stream-123",
    options: options,
    logger: logger
);

// Consume credits before sending messages (synchronous)
bool creditsAvailable = controller.TryConsumeCredit(5);
if (creditsAvailable)
{
    Console.WriteLine($"Successfully acquired credits. Available: {controller.AvailableCredits}");
    
    // Send messages...
    
    // Release credits when processing completes
    controller.ReleaseCredit(5);
}

// Consume credits asynchronously with cancellation support
try
{
    await controller.ConsumeCreditAsync(3, cancellationToken);
    
    // Send messages...
    
    // Release credits when done
    controller.ReleaseCredit(3);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Credit acquisition was cancelled");
}

// Reset the flow control window when needed
controller.ResetWindow();

// Monitor backpressure state
Console.WriteLine($"Stream ID: {controller.StreamId}");
Console.WriteLine($"Available Credits: {controller.AvailableCredits}");
Console.WriteLine($"Window Utilization: {controller.WindowUtilization:P0}");
Console.WriteLine($"Is Throttled: {controller.IsThrottled}");
Console.WriteLine($"Total Produced: {controller.TotalProduced}");
Console.WriteLine($"Total Consumed: {controller.TotalConsumed}");

// Dispose when the stream is complete
controller.Dispose();
```

## FlowControlOptions

The `FlowControlOptions` class defines configuration settings for the bidirectional streaming engine's flow-control and backpressure subsystem. It controls message throughput, memory usage, and latency characteristics by managing credit windows, channel capacities, and adaptive behavior. This immutable record is typically configured once at application startup and shared across all streaming operations.

Example usage:

```csharp
// Create a production-ready flow control configuration with high throughput
var flowControlOptions = new FlowControlOptions
{
    InitialWindowSize = 256,
    MaxWindowSize = 1024,
    InboundChannelCapacity = 512,
    OutboundChannelCapacity = 512,
    BackpressureThreshold = 0.90, // 90% utilization
    CreditReplenishmentBatch = 64,
    Mode = FlowControlMode.Adaptive,
    MaxProducerWaitTime = TimeSpan.FromSeconds(30),
    AdaptiveAdjustmentInterval = TimeSpan.FromSeconds(5),
    EmitBackpressureEvents = true
};

// Validate configuration before use
flowControlOptions.Validate();

// Use with bidirectional streaming engine
var engine = new BidirectionalStreamingEngine(
    loggerFactory: loggerFactory,
    options: flowControlOptions,
    eventBus: eventBus,
    maxStreams: 200
);

// Or use built-in presets for common scenarios
var highThroughputOptions = FlowControlOptions.HighThroughput;
var lowLatencyOptions = FlowControlOptions.LowLatency;

Console.WriteLine($"High throughput configuration: Initial={highThroughputOptions.InitialWindowSize}, Max={highThroughputOptions.MaxWindowSize}");
Console.WriteLine($"Low latency configuration: Initial={lowLatencyOptions.InitialWindowSize}, Max={lowLatencyOptions.MaxWindowSize}");
```

## StreamingExtensions

The `StreamingExtensions` class provides extension methods for registering the bidirectional streaming subsystem into the ASP.NET Core dependency injection container. These methods simplify the setup of the streaming engine, session manager, flow controllers, and diagnostics services, with support for both default and preset configurations.

Example usage:

```csharp
// Basic setup with default flow control options
builder.Services.AddBidirectionalStreaming();

// Custom flow control configuration via delegate
builder.Services.AddBidirectionalStreaming(options => options with
{
    Mode = FlowControlMode.Adaptive,
    InitialWindowSize = 128,
    MaxWindowSize = 512
});

// Configure with explicit FlowControlOptions
var flowControlOptions = new FlowControlOptions
{
    Mode = FlowControlMode.Enabled,
    InitialWindowSize = 256,
    MaxWindowSize = 1024,
    BackpressureThreshold = 0.90
};
builder.Services.AddBidirectionalStreaming(flowControlOptions);

// Add streaming diagnostics with custom intervals
builder.Services.AddStreamingDiagnostics(
    diagnosticsInterval: TimeSpan.FromSeconds(30),
    staleThreshold: TimeSpan.FromMinutes(10),
    backpressureWarnThreshold: 0.15
);

// Use high-throughput preset (registers engine + diagnostics)
builder.Services.AddHighThroughputBidirectionalStreaming();

// Use low-latency preset (registers engine + diagnostics with shorter intervals)
builder.Services.AddLowLatencyBidirectionalStreaming();

// In Program.cs after building the app
var app = builder.Build();

// The registered services include:
// - IBidirectionalStreamingEngine (BidirectionalStreamingEngine)
// - StreamingSessionManager
// - FlowControlOptions (if provided)
// - StreamDiagnosticsService (if AddStreamingDiagnostics called)
// - AdaptiveFlowController (if FlowControlMode.Adaptive)
```

## StreamDiagnosticsOptions

The `StreamDiagnosticsOptions` record configures the behavior of the `StreamDiagnosticsService`, which periodically collects aggregate metrics from all active bidirectional streams and publishes diagnostic events. It controls how often diagnostics are collected, when streams are considered stale, and what backpressure thresholds trigger warnings.

Example usage:

```csharp
// Create a production-ready diagnostics configuration
var diagnosticsOptions = new StreamDiagnosticsOptions
{
    CollectionInterval = TimeSpan.FromSeconds(30),  // Collect every 30 seconds
    StaleStreamThreshold = TimeSpan.FromMinutes(10),  // Mark streams idle for 10+ minutes as stale
    BackpressureWarnThreshold = 0.15  // Warn when backpressure events exceed 15% of messages
};

// Register the service with ASP.NET Core DI
builder.Services.AddSingleton<StreamDiagnosticsService>();
builder.Services.AddHostedService<StreamDiagnosticsService>();

// Or configure with options pattern
builder.Services.Configure<StreamDiagnosticsOptions>(options =>
{
    options.CollectionInterval = TimeSpan.FromSeconds(30);
    options.StaleStreamThreshold = TimeSpan.FromMinutes(10);
    options.BackpressureWarnThreshold = 0.15;
});

// The service will automatically start and publish StreamingDiagnosticsEvent
// to the application EventBus every CollectionInterval seconds
```

## BackpressureControllerExtensions

The `BackpressureControllerExtensions` class provides extension methods for managing credit-based flow control in streaming scenarios. It enables efficient backpressure handling by allowing atomic consumption and release of credits, monitoring window utilization, and generating formatted status strings for observability.

Example usage:
```csharp
// Create a backpressure controller with a credit limit
var controller = new BackpressureController(streamId: "stream-123", maxCredits: 100);

// Consume credits for sending messages
bool creditsAcquired = controller.TryConsumeCredits(5);
if (creditsAcquired)
{
Console.WriteLine($"Successfully acquired credits. Available: {controller.AvailableCredits}");
}

// Consume credits asynchronously with cancellation support
await controller.ConsumeCreditsAsync(3, cancellationToken);

// Release credits when processing completes
controller.ReleaseCredits(2);

// Get formatted utilization percentage
string utilization = controller.GetUtilizationPercentString();
Console.WriteLine($"Current utilization: {utilization}");

// Get detailed status string for monitoring
string status = controller.GetStatusString();
Console.WriteLine(status);
```

## EventBus

The `EventBus` class implements a publish-subscribe pattern for loose coupling between components in the gRPC-Web Bridge. It supports both synchronous and asynchronous event handling with comprehensive event history tracking and subscriber management.

Example usage:

```csharp
// Create an event bus (typically injected via DI)
var eventBus = new EventBus(logger);

// Define a custom event by inheriting from EventBase
public class UserCreatedEvent : EventBase
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Subscribe to events synchronously
void HandleUserCreated(UserCreatedEvent @event)
{
    Console.WriteLine($"User created: {@event.Username} ({@event.Email})");
}

eventBus.Subscribe<UserCreatedEvent>(HandleUserCreated);

// Subscribe to events asynchronously
async Task HandleUserCreatedAsync(UserCreatedEvent @event)
{
    await Task.Delay(100); // Simulate async work
    Console.WriteLine($"Async handling: User {@event.Username} created at {@event.CreatedAt}");
}

eventBus.Subscribe<UserCreatedEvent>(HandleUserCreatedAsync);

// Publish an event
var userEvent = new UserCreatedEvent
{
    UserId = Guid.NewGuid().ToString(),
    Username = "johndoe",
    Email = "john@example.com",
    Source = "UserService"
};

await eventBus.PublishAsync(userEvent);

// Check subscriber count
int subscriberCount = eventBus.GetSubscriberCount<UserCreatedEvent>();
Console.WriteLine($"Subscribers for UserCreatedEvent: {subscriberCount}");

// Get event history for auditing
var history = eventBus.GetEventHistory(typeof(UserCreatedEvent).Name);
Console.WriteLine($"Event history count: {history.Count}");

// Unsubscribe from events
eventBus.Unsubscribe<UserCreatedEvent>(HandleUserCreated);
eventBus.Unsubscribe<UserCreatedEvent>(HandleUserCreatedAsync);

// Clear all subscribers (useful for testing)
eventBus.ClearSubscribers();
```

## GrpcMethod

The `GrpcMethod` class represents a single gRPC method definition with full metadata, including method type, input/output message types, parameters, and timeout configuration. It provides comprehensive method information for protocol translation, service discovery, and client integration scenarios.

Example usage:

```csharp
// Create a new gRPC method definition
var method = new GrpcMethod(
    name: "GetUser",
    fullName: "user.v1.UserService/GetUser",
    type: MethodType.Unary,
    inputMessage: "GetUserRequest",
    outputMessage: "UserResponse"
)
{
    Description = "Retrieves a user profile by their unique identifier",
    TimeoutMilliseconds = 5000,
    IsDeprecated = false
};

// Add input parameters
method.AddInputParameter(new MethodParameter(
    name: "userId",
    type: "string",
    isRequired: true,
    description: "The unique identifier of the user to retrieve"
));

method.AddInputParameter(new MethodParameter(
    name: "includeDetails",
    type: "bool",
    isRequired: false,
    defaultValue: "false",
    description: "Whether to include detailed user information"
));

// Add output parameters
method.AddOutputParameter(new MethodParameter(
    name: "user",
    type: "User",
    isRequired: true,
    description: "The retrieved user profile"
));

method.AddOutputParameter(new MethodParameter(
    name: "metadata",
    type: "ResponseMetadata",
    isRequired: false,
    description: "Additional response metadata"
));

// Access method properties
Console.WriteLine($"Method: {method.Name}");
Console.WriteLine($"Full name: {method.FullName}");
Console.WriteLine($"Type: {method.Type}");
Console.WriteLine($"Input: {method.InputMessageType}");
Console.WriteLine($"Output: {method.OutputMessageType}");
Console.WriteLine($"Timeout: {method.TimeoutMilliseconds}ms");
Console.WriteLine($"Deprecated: {method.IsDeprecated}");
Console.WriteLine($"Description: {method.Description}");
Console.WriteLine($"Created: {method.CreatedAt}");
Console.WriteLine($"Parameters: {method.InputParameters.Count} input, {method.OutputParameters.Count} output");

// Validate method configuration
method.Validate();

// Update method (e.g., after adding parameters)
method.UpdatedAt = DateTime.UtcNow;

// Remove a parameter if needed
method.RemoveInputParameter("includeDetails");

// String representation
Console.WriteLine(method.ToString()); // "user.v1.UserService/GetUser (Unary)"
```

## GrpcResponse

The `GrpcResponse` class encapsulates the response from a gRPC service call, holding the payload, status information, and metadata. It provides methods to easily construct successful or failed responses, manage header/trailing metadata, and validate response integrity for client consumption.

Example usage:

```csharp
// Create a new response for a specific request ID
var response = new GrpcResponse(requestId: "req-abc-123", payload: new byte[0]);

// Set success status with a JSON payload
byte[] successPayload = Encoding.UTF8.GetBytes("{\"result\":\"success\"}");
response.SetSuccess(successPayload, SerializationFormat.Json);

// Add custom metadata headers
response.AddMetadata("X-Service-Id", "order-service");
response.AddMetadata("X-Processed-At", DateTime.UtcNow.ToString("O"));

// Access response status
Console.WriteLine($"Response status: {response.Status}");

// Validate response before serialization
response.Validate();
```

## MethodParameter

The `MethodParameter` class represents a single parameter in a gRPC method signature, encapsulating metadata about parameter names, types, field numbers, and serialization requirements. It is used extensively in method definitions to provide complete type information for protocol translation and client integration scenarios.

Example usage:

```csharp
// Create a required method parameter
var userIdParameter = new MethodParameter(
    name: "userId",
    typeName: "string",
    fieldNumber: 1,
    isRequired: true)
{
    Description = "The unique identifier of the user to retrieve",
    Format = SerializationFormat.Protobuf
};

// Create an optional repeated parameter
var tagsParameter = new MethodParameter(
    name: "tags",
    typeName: "string",
    fieldNumber: 2,
    isRequired: false)
{
    Description = "List of tags to filter results by",
    IsRepeated = true,
    Format = SerializationFormat.Json
};

// Access parameter properties
Console.WriteLine($"Parameter: {userIdParameter.Name} ({userIdParameter.TypeName})");
Console.WriteLine($"Field number: {userIdParameter.FieldNumber}");
Console.WriteLine($"Required: {userIdParameter.IsRequired}");
Console.WriteLine($"Repeated: {userIdParameter.IsRepeated}");
Console.WriteLine($"Description: {userIdParameter.Description}");
Console.WriteLine($"Format: {userIdParameter.Format}");

// Validate parameter configuration
userIdParameter.Validate();

// String representation
Console.WriteLine(userIdParameter.ToString()); // "userId: string (field 1)"

// Equality comparison
var sameParameter = new MethodParameter("userId", "string", 1);
Console.WriteLine($"Parameters equal: {userIdParameter.Equals(sameParameter)}"); // true
```

## Docker Support

The gRPC-Web Bridge can be run in Docker containers for easy deployment and scaling.

### Building the Docker Image

```bash
docker build -t grpc-web-bridge .
```

### Running the Container

```bash
docker run -d -p 8080:8080 --name grpc-web-bridge grpc-web-bridge
```

### Using Docker Compose

```bash
docker-compose up -d
```

This will start the gRPC-Web Bridge service with the following features:
- Port 8080 exposed
- Health check endpoint at `/health`
- Prometheus metrics at `/metrics`
- Environment variables configured for production
- Non-root user for security

### Configuration

Environment variables can be passed to the container:

```bash
docker run -d -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS=http://+:8080 \
  --name grpc-web-bridge \
  grpc-web-bridge
```

See `docker-compose.yml` for a complete configuration example.


## HttpClientFactory

The `HttpClientFactory` class provides a managed HTTP client factory for making HTTP requests to external services. It implements connection pooling, configurable timeouts, and supports multiple named clients with customizable settings for cookies, redirects, and HTTPS validation. The factory automatically manages client lifecycle and provides convenient methods for common HTTP operations like GET, POST with JSON, and custom requests.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddSingleton<IHttpClientFactory>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<HttpClientFactory>>();
    var options = new HttpClientFactoryOptions
    {
        RequestTimeoutMs = 15000,  // 15 seconds timeout
        MaxConnectionsPerServer = 20,
        UseCookies = false,
        AllowAutoRedirect = true,
        AllowInsecureHttps = false
    };
    return new HttpClientFactory(logger, options);
});

// In your service or controller
var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

// Get a default client
var defaultClient = httpClientFactory.GetClient();

// Get a client for a specific endpoint
var apiClient = httpClientFactory.GetClientForUri("https://api.example.com");

// Make a GET request
string response = await httpClientFactory.GetAsync("https://api.example.com/users");
Console.WriteLine(response);

// Make a POST request with JSON
var userData = new { Name = "John Doe", Email = "john@example.com" };
string postResponse = await httpClientFactory.PostJsonAsync(
    "https://api.example.com/users",
    userData
);

// Make a custom request
var responseMessage = await httpClientFactory.SendAsync(
    "https://api.example.com/data",
    HttpMethod.Put,
    new StringContent("{\"value\": 42}", Encoding.UTF8, "application/json"),
    new Dictionary<string, string> { { "X-Custom-Header", "value" } }
);

// Register a pre-configured client
var customClient = new HttpClient
{
    BaseAddress = new Uri("https://auth.example.com"),
    Timeout = TimeSpan.FromSeconds(10)
};
httpClientFactory.RegisterClient("auth-service", customClient);

// Get list of registered clients
var clientNames = httpClientFactory.GetRegisteredClientNames();
Console.WriteLine(string.Join(", ", clientNames));

// Remove a client when no longer needed
bool removed = httpClientFactory.RemoveClient("old-client");
```

## TracingService

The `TracingService` class creates and manages distributed-tracing spans for bridge operations. It wraps the OpenTelemetry `ActivitySource` to simplify span creation without requiring callers to handle the source directly. All methods safely return null when no tracing listener is active, allowing callers to use null-conditional disposal patterns.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddSingleton<TracingService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<TracingService>>();
    return new TracingService(logger, instanceName: "grpc-bridge-prod");
});

// In your middleware or controller
var tracingService = serviceProvider.GetRequiredService<TracingService>();

// Start a gRPC call span
using var grpcActivity = tracingService.StartGrpcCallActivity(
    serviceName: "UserService",
    methodName: "GetUser",
    isStreaming: false
);

// Start a protocol translation span
using var translationActivity = tracingService.StartProtocolTranslationActivity(
    sourceProtocol: "grpc-web",
    targetProtocol: "grpc"
);

// Start an authentication span
using var authActivity = tracingService.StartAuthenticationActivity(
    scheme: "Bearer"
);

try
{
    // Your bridge operation here
    await next(context);
    
    // Set success status on completion
    TracingService.SetGrpcStatus(grpcActivity, "OK");
}
catch (Exception ex)
{
    // Record exception details
    TracingService.RecordException(grpcActivity, ex, "INTERNAL");
    throw;
}
```

## ResponseFormatter

The `ResponseFormatter` class provides a unified response formatting utility for all API endpoints in the gRPC-Web Bridge. It ensures consistent response structure across the bridge by providing standardized methods for creating successful responses, error responses, validation errors, streaming responses, batch operations, health checks, and statistics. The formatter supports adding metadata, custom messages, and maintains a consistent timestamp across all responses.

Example usage:

```csharp
// Format a successful response with data
var userData = new { Id = 1, Name = "Alice", Email = "alice@example.com" };
var successResponse = ResponseFormatter.FormatSuccess(userData, "User retrieved successfully");
Console.WriteLine(ResponseFormatter.ToJson(successResponse, indented: true));

// Format a successful response with pagination
var users = new List<object> { userData };
var paginatedResponse = ResponseFormatter.FormatSuccessList(users, total: 1, page: 1, pageSize: 50);
Console.WriteLine(ResponseFormatter.ToJson(paginatedResponse, indented: true));

// Format an error response
var errorResponse = ResponseFormatter.FormatError(
    "NotFound", 
    "User not found", 
    statusCode: 404,
    details: new { UserId = "999" }
);
Console.WriteLine(ResponseFormatter.ToJson(errorResponse, indented: true));

// Format a validation error response
var validationErrors = new Dictionary<string, string> 
{
    { "email", "Email is required" },
    { "password", "Password must be at least 8 characters" }
};
var validationResponse = ResponseFormatter.FormatValidationError(validationErrors);
Console.WriteLine(ResponseFormatter.ToJson(validationResponse, indented: true));

// Format a streaming response
var streamingResponse = ResponseFormatter.FormatStreamingResponse(
    streamId: "stream-abc-123",
    status: "active",
    messageCount: 42,
    lastMessage: new { Content = "Last message content" }
);

// Format a batch response
var batchResponse = ResponseFormatter.FormatBatchResponse(
    operationCount: 10,
    successCount: 8,
    failureCount: 2,
    results: new List<object> { /* batch results */ }
);

// Format a health check response
var healthResponse = ResponseFormatter.FormatHealthCheckResponse(
    healthy: true,
    status: "All systems operational",
    metrics: new Dictionary<string, object> { { "responseTimeMs", 42 } },
    warnings: new List<string> { }
);

// Format a statistics response
var statsResponse = ResponseFormatter.FormatStatisticsResponse(
    statistics: new Dictionary<string, object> { { "requests", 1000 }, { "errors", 5 } },
    period: "last-hour"
);

// Wrap any object in a standard response envelope
var wrappedResponse = ResponseFormatter.WrapResponse(
    data: new { Message = "Hello, World!" },
    success: true,
    message: "Custom message",
    statusCode: 200
);

// Create a custom response with specific structure
var customResponse = ResponseFormatter.CreateCustomResponse(
    success: true,
    body: new { Data = "Custom body content" },
    headers: new Dictionary<string, object> { { "X-Custom-Header", "value" } },
    statusCode: 201
);

// Format a service registry response
var registryResponse = ResponseFormatter.FormatServiceRegistryResponse(
    totalServices: 15,
    healthyServices: 14,
    unhealthyServices: 1,
    totalMethods: 45,
    services: new List<object> { /* service details */ }
);

// Format a configuration response
var configResponse = ResponseFormatter.FormatConfigurationResponse(
    config: new Dictionary<string, object> { { "timeout", 30000 }, { "retries", 3 } },
    environment: "Production"
);
```

## ConfigurationController

The `ConfigurationController` class provides runtime configuration management for the gRPC-Web Bridge server. It allows administrators to retrieve, update, validate, and reset configuration settings without restarting the application. The controller exposes endpoints for dynamic configuration changes, service health validation, and system state inspection, making it ideal for production environments where configuration needs to be adjusted on-the-fly.

### Public Members

- `GetConfiguration()` - Retrieves current bridge configuration including environment settings, streaming parameters, message limits, compression settings, and runtime configuration
- `UpdateConfiguration(ConfigurationUpdateRequest)` - Updates specific configuration values at runtime (compression, rate limiting, timeouts, etc.)
- `ValidateConfiguration()` - Validates configuration consistency and service connectivity by checking all registered services
- `ResetConfiguration()` - Resets runtime configuration to default values
- `Settings` - Dictionary containing runtime configuration settings that can be modified dynamically

### Usage Examples

```csharp
// Example 1: Retrieve current configuration
var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:5001") };

var configResponse = await httpClient.GetAsync("/api/configuration");
if (configResponse.IsSuccessStatusCode)
{
    var configData = await configResponse.Content.ReadFromJsonAsync<dynamic>();
    Console.WriteLine($"Current compression: {configData.data.compressResponses}");
    Console.WriteLine($"Max streams: {configData.data.maxStreamCount}");
}

// Example 2: Update configuration dynamically
var updateRequest = new
{
    Settings = new Dictionary<string, object>
    {
        { "CompressResponses", false },
        { "CompressionLevel", 6 },
        { "RequestsPerSecond", 500 }
    }
};

var updateResponse = await httpClient.PutAsJsonAsync(
    "/api/configuration", 
    updateRequest
);

if (updateResponse.IsSuccessStatusCode)
{
    var result = await updateResponse.Content.ReadFromJsonAsync<dynamic>();
    Console.WriteLine($"Updated {result.updates.Count} settings");
}

// Example 3: Validate service connectivity
var validationResponse = await httpClient.PostAsync(
    "/api/configuration/validate", 
    null
);

if (validationResponse.IsSuccessStatusCode)
{
    var validationData = await validationResponse.Content.ReadFromJsonAsync<dynamic>();
    Console.WriteLine($"Healthy services: {validationData.healthyServices}/{validationData.serviceCount}");
}

// Example 4: Reset to defaults
var resetResponse = await httpClient.PostAsync(
    "/api/configuration/reset", 
    null
);

if (resetResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Configuration reset completed");
}
```

## StreamProcessingBenchmarks

The `StreamProcessingBenchmarks` class provides performance benchmarks for stream processing operations in the gRPC-Web Bridge, including reading streams to end, chunked copying, and base64 conversion. It uses BenchmarkDotNet to measure execution time and memory allocation for various stream sizes.

Example usage:

```csharp
// Create a benchmark instance
var benchmarks = new StreamProcessingBenchmarks();

// Setup the benchmark (required before running benchmarks)
benchmarks.Setup();

// Benchmark reading a 1KB stream to end
byte[] stream1KB = await benchmarks.ReadStreamToEnd_1KB();
Console.WriteLine($"Read 1KB stream: {stream1KB.Length} bytes");

// Benchmark reading a 64KB stream to end
byte[] stream64KB = await benchmarks.ReadStreamToEnd_64KB();
Console.WriteLine($"Read 64KB stream: {stream64KB.Length} bytes");

// Benchmark reading a 1MB stream to end
byte[] stream1MB = await benchmarks.ReadStreamToEnd_1MB();
Console.WriteLine($"Read 1MB stream: {stream1MB.Length} bytes");

// Benchmark chunked copying of a 1KB stream
await benchmarks.CopyStreamChunked_1KB();
Console.WriteLine("Completed chunked copy of 1KB stream");

// Benchmark chunked copying of a 64KB stream
await benchmarks.CopyStreamChunked_64KB();
Console.WriteLine("Completed chunked copy of 64KB stream");

// Benchmark chunked copying of a 1MB stream
await benchmarks.CopyStreamChunked_1MB();
Console.WriteLine("Completed chunked copy of 1MB stream");

// Benchmark converting a 1KB stream to base64
string base64String = await benchmarks.StreamToBase64_1KB();
Console.WriteLine($"Base64 encoded 1KB stream: {base64String.Length} characters");
```

## Performance Benchmarks

The repository includes a benchmark suite built with [BenchmarkDotNet](https://benchmarkdotnet.org/) to monitor the performance of critical components such as authentication, protocol translation, stream processing, and JSON utilities.

### Running Benchmarks

To run the benchmarks, execute the following commands from the root directory:

```bash
cd benchmarks/grpc-web-bridge.Benchmarks
dotnet run -c Release -- --filter "*"
```

The benchmarks will run a series of tests and output a summary table, including execution time and memory allocation diagnostics.

## AuthenticationBenchmarks

The `AuthenticationBenchmarks` class provides performance benchmarks for authentication-related operations in the gRPC-Web Bridge, including JWT token extraction, context caching, and API key authentication. It uses BenchmarkDotNet to measure execution time and memory allocation for critical authentication paths.

Example usage:

```csharp
// Create a benchmark instance
var benchmarks = new AuthenticationBenchmarks();

// Setup the benchmark (required before running benchmarks)
benchmarks.Setup();

// Benchmark extracting a valid Bearer token from a properly formatted header
string? validToken = benchmarks.ExtractBearerToken_Valid();
Console.WriteLine($"Extracted valid token: {validToken != null}");

// Benchmark extracting a token from an invalid header (e.g., Basic auth)
string? invalidToken = benchmarks.ExtractBearerToken_Invalid();
Console.WriteLine($"Extracted from invalid header: {invalidToken}");

// Benchmark extracting a token from a null header
string? nullToken = benchmarks.ExtractBearerToken_Null();
Console.WriteLine($"Extracted from null header: {nullToken}");

// Benchmark retrieving a cached authentication context (cache hit scenario)
AuthenticationContext? cachedContext = benchmarks.GetCachedContext_Hit();
Console.WriteLine($"Retrieved cached context: {cachedContext != null}");

// Benchmark retrieving a non-existent authentication context (cache miss scenario)
AuthenticationContext? missingContext = benchmarks.GetCachedContext_Miss();
Console.WriteLine($"Retrieved missing context: {missingContext}");

// Benchmark authenticating with an API key
AuthenticationContext apiKeyContext = benchmarks.AuthenticateApiKey();
Console.WriteLine($"Authenticated with API key: {apiKeyContext.Id}");

// Benchmark validating an authenticated context
bool isValid = benchmarks.ValidateContext();
Console.WriteLine($"Context validation result: {isValid}");
```

## HealthCheckController

The `HealthCheckController` provides health monitoring endpoints for the gRPC-Web Bridge server. It exposes endpoints to check overall system health, service-specific health status, resource metrics, and readiness/liveness probes. These endpoints are essential for container orchestration systems (like Kubernetes) and monitoring tools to determine the operational state of the bridge.

Example usage:

```csharp
// In your ASP.NET Core application's Program.cs
builder.Services.AddControllers();

// The HealthCheckController is automatically registered when AddControllers() is called

// Access health check endpoints:

// 1. Get basic health status (simple OK/Not OK response)
var healthStatus = await httpClient.GetStringAsync("http://localhost:5000/api/health");
Console.WriteLine($"Health status: {healthStatus}"); // "OK" or error message

// 2. Get detailed diagnostics with system information
var diagnostics = await httpClient.GetFromJsonAsync<dynamic>("http://localhost:5000/api/health/diagnostics");
Console.WriteLine($"System: {diagnostics.data.systemInfo.environment}");
Console.WriteLine($"Uptime: {diagnostics.data.systemInfo.uptime}");
Console.WriteLine($"Memory Usage: {diagnostics.data.resourceMetrics.memoryUsed}MB / {diagnostics.data.resourceMetrics.memoryTotal}MB");

// 3. Check specific service health
var serviceHealth = await httpClient.GetFromJsonAsync<dynamic>("http://localhost:5000/api/health/service/UserService");
Console.WriteLine($"UserService healthy: {serviceHealth.data.isHealthy}");

// 4. Get resource metrics for monitoring
var metrics = await httpClient.GetFromJsonAsync<dynamic>("http://localhost:5000/api/health/metrics");
Console.WriteLine($"Active Streams: {metrics.data.activeStreams}");
Console.WriteLine($"CPU Usage: {metrics.data.cpuUsage}%");
Console.WriteLine($"Request Rate: {metrics.data.requestRatePerSecond} req/s");

// 5. Check readiness for load balancer routing
var readiness = await httpClient.GetFromJsonAsync<dynamic>("http://localhost:5000/api/health/readiness");
Console.WriteLine($"Ready for traffic: {readiness.data.ready}");

// 6. Check liveness for container orchestration
var liveness = await httpClient.GetFromJsonAsync<dynamic>("http://localhost:5000/api/health/liveness");
Console.WriteLine($"Alive and responsive: {liveness.data.alive}");
```

## ProtocolTranslationBenchmarks

The `ProtocolTranslationBenchmarks` class provides performance benchmarks for protocol translation operations in the gRPC-Web Bridge, including metadata translation, protobuf-to-JSON conversion, and gRPC-to-HTTP response translation. It uses BenchmarkDotNet to measure execution time and memory allocation for various protocol translation scenarios.

Example usage:

```csharp
// Create a benchmark instance
var benchmarks = new ProtocolTranslationBenchmarks();

// Setup the benchmark (required before running benchmarks)
benchmarks.Setup();

// Benchmark translating small metadata (5 headers)
var smallMetadata = benchmarks.TranslateMetadata_Small();
Console.WriteLine($"Translated small metadata: {smallMetadata.Count} headers");

// Benchmark translating large metadata (50 headers)
var largeMetadata = benchmarks.TranslateMetadata_Large();
Console.WriteLine($"Translated large metadata: {largeMetadata.Count} headers");

// Benchmark converting a 256-byte Protobuf payload to JSON
var protobufPayload = benchmarks.ConvertProtobufToJson_256B();
Console.WriteLine($"Converted Protobuf to JSON: {protobufPayload.Length} bytes");

// Benchmark converting a base64-wrapped JSON payload back to Protobuf
var base64JsonPayload = benchmarks.ConvertJsonToProtobuf_Base64();
Console.WriteLine($"Converted JSON to Protobuf: {base64JsonPayload.Length} bytes");

// Benchmark translating a Protobuf response to HTTP (passthrough)
var protobufResponse = benchmarks.TranslateGrpcToHttp_Passthrough();
Console.WriteLine($"Translated gRPC to HTTP (passthrough): {protobufResponse.Length} bytes");

// Benchmark translating a Protobuf response to HTTP with JSON conversion
var jsonResponse = benchmarks.TranslateGrpcToHttp_Convert();
Console.WriteLine($"Translated gRPC to HTTP (converted): {jsonResponse.Length} bytes");
```

## ProtocolTranslationBenchmarksExtensions

The `ProtocolTranslationBenchmarksExtensions` class provides extension methods for the `ProtocolTranslationBenchmarks` class to simplify common benchmarking scenarios for gRPC protocol translation. It offers utilities for setting up test data, translating metadata, converting between protobuf and JSON formats, and creating test responses.

Example usage:

```csharp
// Create a benchmark instance
var benchmarks = new ProtocolTranslationBenchmarks();

// Configure with pre-configured test data
benchmarks.WithPreconfiguredData();

// Translate metadata with small headers (5 headers)
var smallMetadataResults = benchmarks.TranslateAllMetadata(smallHeaders: true);
Console.WriteLine($"Small metadata translation completed: {smallMetadataResults.Count} benchmarks");

// Convert protobuf to JSON string
var protobufData = Encoding.UTF8.GetBytes("test protobuf data");
var jsonString = benchmarks.ConvertProtobufToJsonString(protobufData);
Console.WriteLine($"Converted to JSON: {jsonString}");

// Convert base64 JSON to protobuf
var base64Json = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"test\":\"value\"}"));
var protobufBytes = benchmarks.ConvertBase64JsonToProtobuf(Encoding.UTF8.GetBytes(base64Json));
Console.WriteLine($"Converted back to protobuf: {protobufBytes.Length} bytes");

// Translate gRPC response to HTTP with automatic format detection
var testResponse = benchmarks.CreateTestResponse(protobufData, SerializationFormat.Protobuf);
var httpPayload = benchmarks.TranslateGrpcToHttpAuto(testResponse);
Console.WriteLine($"HTTP payload translated: {httpPayload.Length} bytes");
```

## MetricsCollectionWorker

The `MetricsCollectionWorker` class is a background worker that periodically collects system and application metrics including CPU usage, memory consumption, thread count, garbage collection statistics, and request metrics. It maintains a history of metrics snapshots for trend analysis and alerting purposes, and provides methods to retrieve aggregated metrics and manage the metrics history.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddMetricsCollectionWorker(options =>
{
    options.CollectionIntervalSeconds = 30;
    options.MaxSnapshotsToKeep = 1000;
    options.CpuAlertThresholdPercent = 90.0;
    options.MemoryAlertThresholdMb = 2048.0;
    options.ErrorRateAlertThresholdPercent = 5.0;
});

// In your ASP.NET Core application startup
var metricsWorker = app.Services.GetRequiredService<MetricsCollectionWorker>();

// Start the metrics collection worker
metricsWorker.Start();

// Get aggregated metrics for monitoring dashboards
var aggregatedMetrics = metricsWorker.GetAggregatedMetrics();
Console.WriteLine($"Current CPU: {aggregatedMetrics.CpuUsagePercent}%");
Console.WriteLine($"Current Memory: {aggregatedMetrics.MemoryUsageMb}MB");
Console.WriteLine($"Current Threads: {aggregatedMetrics.ThreadCount}");
Console.WriteLine($"Total Requests: {aggregatedMetrics.TotalRequests}");
Console.WriteLine($"Error Rate: {aggregatedMetrics.ErrorRate}%");

// Get the latest metrics snapshot
var latestSnapshot = metricsWorker.GetSnapshotHistory().LastOrDefault();
if (latestSnapshot != null)
{
    Console.WriteLine($"Latest snapshot at {latestSnapshot.Timestamp}: " +
                     $"CPU={latestSnapshot.CpuUsagePercent}%, " +
                     $"Memory={latestSnapshot.MemoryUsageMb}MB, " +
                     $"Threads={latestSnapshot.ThreadCount}");
}

// Access request metrics for performance monitoring
if (metricsWorker.RequestMetrics != null)
{
    Console.WriteLine($"Request metrics available:");
    Console.WriteLine($"  - Total requests: {metricsWorker.RequestMetrics.TotalRequests}");
    Console.WriteLine($"  - Total errors: {metricsWorker.RequestMetrics.TotalErrors}");
    Console.WriteLine($"  - Error rate: {metricsWorker.RequestMetrics.ErrorRate}%");
}

// Get historical metrics for trend analysis
var snapshotHistory = metricsWorker.GetSnapshotHistory();
Console.WriteLine($"Total snapshots collected: {snapshotHistory.Count}");

// Check if any alert thresholds are exceeded
if (metricsWorker.CpuUsagePercent > metricsWorker.CpuAlertThresholdPercent)
{
    Console.WriteLine($"ALERT: CPU usage {metricsWorker.CpuUsagePercent}% exceeds threshold {metricsWorker.CpuAlertThresholdPercent}%");
}

if (metricsWorker.MemoryUsageMb > metricsWorker.MemoryAlertThresholdMb)
{
    Console.WriteLine($"ALERT: Memory usage {metricsWorker.MemoryUsageMb}MB exceeds threshold {metricsWorker.MemoryAlertThresholdMb}MB");
}

if (metricsWorker.ErrorRate > metricsWorker.ErrorRateAlertThresholdPercent)
{
    Console.WriteLine($"ALERT: Error rate {metricsWorker.ErrorRate}% exceeds threshold {metricsWorker.ErrorRateAlertThresholdPercent}%");
}

// Clear old history when needed (e.g., during maintenance)
metricsWorker.ClearHistory();

// Stop the worker when application shuts down
metricsWorker.Stop();
```

## HealthCheckWorker

The `HealthCheckWorker` class is a background worker that periodically checks the health status of registered services. It monitors service availability, response times, and error rates, maintaining a health status for each service with configurable intervals and timeouts. The worker provides methods to retrieve current health status, service statistics, and historical health data for monitoring and alerting purposes.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddHealthCheckWorker(options =>
{
    options.CheckIntervalSeconds = 30;
    options.TimeoutMs = 5000;
    options.InitialDelaySeconds = 5;
    options.ServiceId = "grpc-web-bridge";
    options.ServiceName = "gRPC-Web Bridge";
});

// In your ASP.NET Core application startup
var healthCheckWorker = app.Services.GetRequiredService<HealthCheckWorker>();

// Start the health check worker (typically done automatically by IHostedService)
// healthCheckWorker.Start(); // Not needed - runs automatically

// Get current health status for all services
var healthStatus = healthCheckWorker.GetHealthStatus();
Console.WriteLine($"Overall health: {healthStatus.IsHealthy}");
Console.WriteLine($"Service ID: {healthStatus.ServiceId}");
Console.WriteLine($"Service Name: {healthStatus.ServiceName}");
Console.WriteLine($"Last checked: {healthStatus.Timestamp}");

// Access individual service health
if (healthStatus.Services.TryGetValue("UserService", out var userServiceHealth))
{
    Console.WriteLine($"UserService health: {(userServiceHealth.IsHealthy ? "HEALTHY" : "UNHEALTHY")}");
    Console.WriteLine($"Response time: {userServiceHealth.ResponseTimeMs}ms");
    Console.WriteLine($"Error count: {userServiceHealth.ErrorCount}");
}

// Get statistics for monitoring
var statistics = healthCheckWorker.GetStatistics();
Console.WriteLine($"Total checks performed: {statistics.totalChecks}");
Console.WriteLine($"Successful checks: {statistics.successfulChecks}");
Console.WriteLine($"Failed checks: {statistics.failedChecks}");
Console.WriteLine($"Average response time: {statistics.averageResponseTimeMs}ms");

// Access configuration properties
int interval = healthCheckWorker.CheckIntervalSeconds;
int timeout = healthCheckWorker.CheckTimeoutMs;
int initialDelay = healthCheckWorker.InitialDelaySeconds;
string serviceId = healthCheckWorker.ServiceId;
string serviceName = healthCheckWorker.ServiceName;
bool isHealthy = healthCheckWorker.IsHealthy;
DateTime timestamp = healthCheckWorker.Timestamp;
```

## IRouteHeaderTransformHook

The `IRouteHeaderTransformHook` interface defines a contract for per-route header transformation hooks. Implementations can inspect and rewrite HTTP request and response headers before they reach downstream gRPC services or after responses return. This is useful for adding authentication headers, modifying content types, adding tracing metadata, or implementing custom routing logic based on headers.

Example usage:

```csharp
// Create a custom hook implementation
public class CustomHeaderTransformHook : IRouteHeaderTransformHook
{
    public string? RoutePrefix => "/api/v1/";

    public async Task TransformRequestAsync(
        IHeaderDictionary requestHeaders,
        Dictionary<string, string> grpcMetadata,
        CancellationToken cancellationToken)
    {
        // Add custom headers to the request
        requestHeaders["X-Custom-Request-Id"] = Guid.NewGuid().ToString();
        
        // Add metadata for downstream gRPC services
        grpcMetadata["authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
        grpcMetadata["x-user-id"] = "user-123";
        
        // Remove sensitive headers
        requestHeaders.Remove("X-Internal-Header");
    }

    public async Task TransformResponseAsync(
        IHeaderDictionary responseHeaders,
        CancellationToken cancellationToken)
    {
        // Add security headers to the response
        responseHeaders["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        responseHeaders["X-Content-Type-Options"] = "nosniff";
        responseHeaders["X-Frame-Options"] = "DENY";
    }
}

// Register the hook in Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouteHeaderTransformHook<CustomHeaderTransformHook>();

// Use the middleware in the pipeline (typically before UseRouting)
var app = builder.Build();
app.UseRouteHeaderTransforms();
app.UseRouting();
app.UseEndpoints(...);

// Or use a delegate-based hook for simple transformations
builder.Services.AddRouteHeaderTransformHook(
    routePrefix: "/api/secure/",
    transformRequest: async (headers, metadata, ct) =>
    {
        if (headers.TryGetValue("Authorization", out var authValue))
        {
            metadata["auth-token"] = authValue.ToString();
        }
    },
    transformResponse: async (headers, ct) =>
    {
        headers["Cache-Control"] = "no-store";
    }
);
```

## ReflectionService

The `ReflectionService` provides runtime information about the services and methods available in the gRPC Web Bridge. It enables clients to discover available services and their descriptors without requiring pre-compiled protocol buffer definitions.

Example usage:

```csharp
// Create a reflection service instance
var reflectionService = new ReflectionService();

// List all available service names
var serviceNamesResult = await reflectionService.ListServiceNamesAsync();
if (serviceNamesResult.IsSuccess)
{
    foreach (var serviceName in serviceNamesResult.Value)
    {
        Console.WriteLine($"Service: {serviceName}");
    }
}

// Get descriptor for a specific service
var userServiceDescriptorResult = await reflectionService.GetServiceDescriptorAsync("UserService");
if (userServiceDescriptorResult.IsSuccess)
{
    var serviceDescriptor = userServiceDescriptorResult.Value;
    Console.WriteLine($"Service: {serviceDescriptor.Name}");
    Console.WriteLine($"Methods: {serviceDescriptor.Methods.Count}");
}

// Get all service descriptors
var allDescriptorsResult = await reflectionService.GetAllDescriptorsAsync();
if (allDescriptorsResult.IsSuccess)
{
    foreach (var descriptor in allDescriptorsResult.Value)
    {
        Console.WriteLine($"Service: {descriptor.FullName} ({descriptor.Methods.Count} methods)");
    }
}

// Get descriptor for a specific method
var methodDescriptorResult = await reflectionService.GetMethodDescriptorAsync("UserService", "GetUser");
if (methodDescriptorResult.IsSuccess)
{
    var methodDescriptor = methodDescriptorResult.Value;
    Console.WriteLine($"Method: {methodDescriptor.FullName}");
    Console.WriteLine($"Type: {methodDescriptor.MethodType}");
}
```

## ServiceRegistry

The `ServiceRegistry` class provides centralized service registration, discovery, and health monitoring for gRPC services in the gRPC-Web Bridge. It maintains an in-memory registry of available services with their endpoints, ports, and method definitions, enabling dynamic service discovery and load balancing. The registry supports service status tracking, metadata caching for performance optimization, and comprehensive service management capabilities.

Example usage:

```csharp
// Create service registry (typically injected via dependency injection)
var serviceRegistry = new ServiceRegistry(logger);

// Register a gRPC service
var userService = new GrpcService(
    name: "UserService",
    packageName: "com.example.grpc",
    endpoint: "user-service.example.com",
    port: 50051
)
{
    Description = "Handles user authentication and profile management",
    UseTls = true,
    Status = ServiceStatus.Serving
};

// Add methods to the service
userService.AddMethod(new GrpcMethod(
    name: "GetUser",
    methodType: MethodType.Unary,
    inputType: "GetUserRequest",
    outputType: "UserResponse"
));

userService.AddMethod(new GrpcMethod(
    name: "CreateUser",
    methodType: MethodType.Unary,
    inputType: "CreateUserRequest",
    outputType: "UserResponse"
));

// Register the service
serviceRegistry.RegisterService(userService);

// Retrieve a service by full name
var retrievedService = serviceRegistry.GetService("com.example.grpc.UserService");
if (retrievedService != null)
{
    Console.WriteLine($"Service found: {retrievedService.FullName}");
    Console.WriteLine($"Endpoint: {retrievedService.Endpoint}:{retrievedService.Port}");
    Console.WriteLine($"Methods: {retrievedService.Methods.Count}");
}

// Check service health status
var healthStatus = serviceRegistry.GetHealthStatus("com.example.grpc.UserService");
Console.WriteLine($"Health status: {healthStatus}");

// List all registered services
var allServices = serviceRegistry.ListServices();
Console.WriteLine($"Total services registered: {allServices.Count()}");

// Update service status
serviceRegistry.UpdateServiceStatus("com.example.grpc.UserService", ServiceStatus.NotServing);

// Get cached metadata for performance optimization
var metadata = serviceRegistry.GetCachedMetadata("com.example.grpc.UserService");
if (metadata != null)
{
    Console.WriteLine($"Cached at: {metadata.CachedAt}");
    Console.WriteLine($"Expires at: {metadata.ExpiresAt}");
    Console.WriteLine($"Method count: {metadata.MethodCount}");
}

// Unregister service when no longer needed
bool unregistered = serviceRegistry.UnregisterService("com.example.grpc.UserService");
Console.WriteLine($"Service unregistered: {unregistered}");
```

## AuthenticationService

The `AuthenticationService` class handles authentication and authorization for gRPC requests in the gRPC-Web Bridge. It supports multiple authentication schemes including Bearer tokens (JWT), API keys, and custom authentication methods. The service provides role-based authorization, context validation, caching for performance optimization, and generates appropriate error responses for failed authentication attempts.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddSingleton<AuthenticationService>();

// In your middleware or controller
var authService = app.Services.GetRequiredService<AuthenticationService>();

// Authenticate with Bearer token (JWT)
var bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTEyMzQ1Iiwicm9sZXMiOlsiYWRtaW4iLCJ1c2VyIl0sIm5hbWUiOiJKb2huIERvZSIsImV4cCI6MTc5OTk5OTk5OSwiaWF0IjoxNjk5OTk5OTk5fQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
var bearerContext = authService.AuthenticateBearer(bearerToken);

Console.WriteLine($"Authenticated user: {bearerContext.UserId}");
Console.WriteLine($"Roles: {string.Join(", ", bearerContext.Roles)}");

// Authenticate with API key
var apiKeyContext = authService.AuthenticateApiKey(
    apiKey: "sk_live_abc123xyz789",
    userId: "user-456"
);

Console.WriteLine($"API key authenticated for user: {apiKeyContext.UserId}");

// Authenticate with custom credentials
var customContext = authService.AuthenticateCustom(
    userId: "user-789",
    credentials: new Dictionary<string, string>
    {
        {"email", "user@example.com"},
        {"department", "engineering"}
    }
);

Console.WriteLine($"Custom authenticated user: {customContext.UserId}");

// Validate authentication context
bool isValid = authService.ValidateContext(bearerContext);
Console.WriteLine($"Context valid: {isValid}");

// Check for specific role
bool isAdmin = authService.AuthorizeRole(bearerContext, "admin");
Console.WriteLine($"Is admin: {isAdmin}");

// Check for any role in a list
bool hasRequiredRole = authService.AuthorizeAnyRole(bearerContext, "admin", "moderator");
Console.WriteLine($"Has required role: {hasRequiredRole}");

// Extract bearer token from authorization header
string? authHeader = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
string? extractedToken = authService.ExtractBearerToken(authHeader);
Console.WriteLine($"Extracted token: {extractedToken != null}");

// Get cached context (useful for subsequent requests)
var cachedContext = authService.GetCachedContext(bearerContext.Id);
Console.WriteLine($"Retrieved cached context: {cachedContext != null}");

// Create authentication failure response
var failureResponse = authService.CreateAuthFailureResponse(Guid.NewGuid().ToString());
Console.WriteLine($"Created auth failure response: {failureResponse.StatusCode}");
```

## ProtocolTranslationService

The `ProtocolTranslationService` class handles protocol translation between gRPC, gRPC-Web, and other protocol formats (JSON, Protocol Buffers). It provides bidirectional conversion capabilities for seamless communication between gRPC clients and REST/gRPC-Web endpoints, including metadata translation, format conversion, request validation, and error handling.

This service is the core component that enables the gRPC-Web Bridge to translate between different protocol formats while maintaining compatibility with downstream gRPC services.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddSingleton<ProtocolTranslationService>();

// In your ASP.NET Core middleware or controller
var translationService = app.Services.GetRequiredService<ProtocolTranslationService>();

// Create a gRPC request from HTTP data
var httpRequestBody = Encoding.UTF8.GetBytes("{\"userId\": \"123\"}");
var grpcRequest = translationService.TranslateHttpToGrpc(
    serviceName: "UserService",
    methodName: "GetUser",
    httpBody: httpRequestBody,
    format: SerializationFormat.Json
);

Console.WriteLine($"Created gRPC request: {grpcRequest.Id}");

// Validate the request
translationService.ValidateRequest(grpcRequest);

// Translate metadata between protocols
var metadata = new Dictionary<string, string>
{
    {"authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."},
    {"content-type", "application/json"},
    {"x-custom-header", "custom-value"}
};

var translatedMetadata = translationService.TranslateMetadata(metadata);
Console.WriteLine($"Translated {translatedMetadata.Count} metadata headers");

// Convert between formats
var protobufData = new byte[] { 0x0A, 0x04, 0x74, 0x65, 0x73, 0x74 }; // Protobuf format
var jsonData = translationService.ConvertProtobufToJson(protobufData);
Console.WriteLine($"Converted Protobuf to JSON: {Encoding.UTF8.GetString(jsonData)}");

var base64Json = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"test\":\"value\"}"));
var convertedBack = translationService.ConvertJsonToProtobuf(Encoding.UTF8.GetBytes(base64Json));
Console.WriteLine($"Converted JSON back to Protobuf: {convertedBack.Length} bytes");

// Translate and invoke a gRPC service asynchronously
var authContext = new AuthenticationContext(
    userId: "user-123",
    scheme: AuthenticationScheme.Bearer,
    token: "valid-token-here"
);

var response = await translationService.TranslateAndInvokeAsync(
    grpcRequest,
    authContext,
    cancellationToken: CancellationToken.None
);

if (response.Status == GrpcStatusCode.Ok)
{
    Console.WriteLine($"Service invocation successful: {response.Id}");
    
    // Convert response back to HTTP format
    var httpResponse = translationService.TranslateGrpcToHttp(
        response,
        SerializationFormat.Json
    );
    
    Console.WriteLine($"HTTP response: {Encoding.UTF8.GetString(httpResponse)}");
}
else
{
    Console.WriteLine($"Service invocation failed: {response.Status} - {response.StatusMessage}");
}

// Create an error response for failed operations
var errorResponse = translationService.CreateErrorResponse(
    requestId: Guid.NewGuid().ToString(),
    statusCode: GrpcStatusCode.Internal,
    message: "Service temporarily unavailable"
);
```

## GrpcConnectionManager

The `GrpcConnectionManager` class manages gRPC connections to backend services, providing connection pooling, lifecycle management, and metrics tracking for gRPC channels. It handles connection creation, retrieval, health monitoring, and cleanup while maintaining performance metrics including request counts, data transfer volumes, and connection durations.

Example usage:

```csharp
// Create a connection manager (typically injected via dependency injection)
var connectionManager = new GrpcConnectionManager(logger);

// Define a gRPC service
var userService = new GrpcService(
    name: "UserService",
    packageName: "user.v1",
    endpoint: "user-service.example.com",
    port: 50051
)
{
    Description = "Handles user authentication and profile management",
    UseTls = true,
    Status = ServiceStatus.Serving
};

// Get or create a channel for the service
var channel = connectionManager.GetOrCreateChannel(userService);
Console.WriteLine($"Channel created for {userService.FullName}");

// Test connection health
bool isHealthy = await connectionManager.TestConnectionAsync(userService);
Console.WriteLine($"Connection healthy: {isHealthy}");

// Get metrics for monitoring
var metrics = connectionManager.GetMetrics(userService.FullName);
if (metrics != null)
{
    Console.WriteLine($"Connection duration: {metrics.GetConnectionDuration().TotalSeconds:F2}s");
    Console.WriteLine($"Requests: {metrics.RequestCount}");
    Console.WriteLine($"Created: {metrics.CreatedAt}");
    Console.WriteLine($"Last used: {metrics.LastUsedAt}");
}

// Close a specific channel when no longer needed
await connectionManager.CloseChannelAsync(userService);

// Close all channels during application shutdown
await connectionManager.CloseAllChannelsAsync();

// Dispose the connection manager (automatically closes all channels)
await connectionManager.DisposeAsync();
```

## RateLimitingMiddleware

The `RateLimitingMiddleware` class provides request rate limiting using a sliding window token bucket algorithm. It enforces both per-client and global rate limits to protect backend services from abuse and overload. The middleware tracks request timestamps per client IP and path, allowing you to configure requests per second, window size, and retry-after periods for rate-limited clients.

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.Configure<RateLimitingOptions>(options =>
{
    options.RequestsPerSecond = 100; // 100 requests per second per client
    options.WindowSizeSeconds = 1; // 1-second sliding window
    options.RetryAfterSeconds = 60; // Wait 60 seconds before retry
    options.EnableGlobalLimit = true; // Enable global rate limiting
    options.GlobalRequestsPerSecond = 10000; // 10,000 total requests per second
});

// Register the middleware in the pipeline
var app = builder.Build();
app.UseRateLimiting(); // Uses default options

// Or with custom configuration:
app.UseRateLimiting(new RateLimitingOptions
{
    RequestsPerSecond = 50,
    WindowSizeSeconds = 2,
    RetryAfterSeconds = 30,
    EnableGlobalLimit = true,
    GlobalRequestsPerSecond = 5000
});
```

## RetryPolicyOptions

The `RetryPolicyOptions` class defines configuration options for retry policies used by the `RetryPolicyExecutor` to implement resilient request execution with configurable retry behavior. It controls maximum retry attempts, delay strategies, retryable status codes, and provides tracking information about retry execution.

Example usage:

```csharp
// Create retry policy options with sensible defaults
var retryOptions = new RetryPolicyOptions
{
    MaxAttempts = 5,
    BaseDelay = TimeSpan.FromSeconds(1),
    MaxDelay = TimeSpan.FromSeconds(30),
    BackoffMultiplier = 2.0,
    RetryableStatusCodes = new HashSet<GrpcStatusCode>
    {
        GrpcStatusCode.Unavailable,
        GrpcStatusCode.DeadlineExceeded,
        GrpcStatusCode.ResourceExhausted
    }
};

// Execute a retryable operation
var retryExecutor = new RetryPolicyExecutor();
var outcome = await retryExecutor.ExecuteAsync<string>(async () =>
{
    // Your gRPC or HTTP call here
    var response = await client.GetUserAsync(userId);
    return response.User;
}, retryOptions);

// Check the result
if (outcome.Succeeded)
{
    Console.WriteLine($"Success after {outcome.Attempts} attempts: {outcome.Result}");
    Console.WriteLine($"Total delay: {outcome.TotalDelay.TotalSeconds:F2}s");
}
else
{
    Console.WriteLine($"Failed after {outcome.Attempts} attempts: {outcome.LastException?.Message}");
}
```

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.Configure<RateLimitingOptions>(options =>
{
    options.RequestsPerSecond = 100;      // 100 requests per second per client
    options.WindowSizeSeconds = 1;         // 1-second sliding window
    options.RetryAfterSeconds = 60;        // Wait 60 seconds before retry
    options.EnableGlobalLimit = true;        // Enable global rate limiting
    options.GlobalRequestsPerSecond = 10000; // 10,000 total requests per second
});

// Register the middleware in the pipeline
var app = builder.Build();
app.UseRateLimiting(); // Uses default options

// Or with custom configuration:
app.UseRateLimiting(new RateLimitingOptions
{
    RequestsPerSecond = 50,
    WindowSizeSeconds = 2,
    RetryAfterSeconds = 30,
    EnableGlobalLimit = true,
    GlobalRequestsPerSecond = 5000
});
```

## BidirectionalStreamingEngine

The `BidirectionalStreamingEngine` class is the central engine that owns the full lifecycle of all bidirectional gRPC streams within a single bridge instance. It manages stream creation, message queuing, backpressure control, and automatic cleanup while publishing stream lifecycle events to the application's `EventBus` for diagnostics and session management.

The engine enforces a global ceiling on concurrent stream count and provides thread-safe access to all operations. It tracks comprehensive metrics for each stream including message counts, backpressure events, and throughput statistics.

Example usage:

```csharp
// Create the bidirectional streaming engine (typically via dependency injection)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var eventBus = new EventBus(loggerFactory.CreateLogger<EventBus>());
var engine = new BidirectionalStreamingEngine(
    loggerFactory,
    options: new FlowControlOptions
    {
        InitialWindowSize = 100,
        MaxWindowSize = 1000,
        Mode = FlowControlMode.Enabled
    },
    eventBus: eventBus,
    maxStreams: 200
);

// Open a new bidirectional stream
var stream = await engine.OpenStreamAsync(
    streamId: Guid.NewGuid().ToString(),
    methodType: MethodType.Duplex
);

Console.WriteLine($"Opened stream: {stream.StreamId}");
Console.WriteLine($"Active streams: {engine.ActiveStreamCount}");

// Get metrics for all active streams
var allMetrics = engine.GetAllMetrics();
foreach (var (streamId, metrics) in allMetrics)
{
    Console.WriteLine($"Stream {streamId}: " +
        $"MessagesIn={metrics.MessagesIn}, MessagesOut={metrics.MessagesOut}, " +
        $"BackpressureEvents={metrics.BackpressureEvents}");
}

// Get a specific stream
var existingStream = engine.GetStream(stream.StreamId);
if (existingStream != null)
{
    Console.WriteLine($"Found stream: {existingStream.StreamId}");
}

// Close the stream when done
await engine.CloseStreamAsync(stream.StreamId, GrpcStatusCode.Ok);

// Dispose the engine when the application shuts down
await engine.DisposeAsync();
```

## StreamCleanupWorker

The `StreamCleanupWorker` class is a background worker that periodically cleans up idle and stale streaming connections to prevent memory leaks. It monitors active streams and removes those that have been inactive beyond configurable thresholds, including streams with no activity (stale streams) and streams that have exceeded their idle timeout. The worker also triggers garbage collection when a threshold of removed streams is reached, helping maintain system stability under heavy streaming loads.

## StreamingSession

The `StreamingSession` class represents a persistent streaming session in the gRPC-Web Bridge, tracking user identity, client origin, authentication context, and session metadata throughout the lifetime of a streaming operation. It provides session lifecycle management, activity tracking, and metadata storage for diagnostic and monitoring purposes.

Example usage:

```csharp
// Create a streaming session manager
var sessionManager = new StreamingSessionManager();

// Create a new streaming session for a user
var session = sessionManager.CreateSession(
    userId: "user-12345",
    clientOrigin: "https://app.example.com",
    authContextId: "auth-abc-123",
    metadata: new Dictionary<string, string>
    {
        {"tracking-id", Guid.NewGuid().ToString()},
        {"device-type", "web"},
        {"version", "2.1.0"}
    }
);

Console.WriteLine($"Created session: {session.SessionId}");
Console.WriteLine($"User: {session.UserId}");
Console.WriteLine($"Client origin: {session.ClientOrigin}");
Console.WriteLine($"Created at: {session.CreatedAt}");
Console.WriteLine($"Active sessions: {sessionManager.GetActiveSessions().Count}");

// Associate a stream with the session
bool streamAssociated = sessionManager.AssociateStream(
    session.SessionId,
    streamId: "stream-xyz-789",
    streamType: "bidirectional"
);

Console.WriteLine($"Stream associated: {streamAssociated}");

// Get session for a specific stream
var sessionForStream = sessionManager.GetSessionForStream("stream-xyz-789");
if (sessionForStream != null)
{
    Console.WriteLine($"Found session for stream: {sessionForStream.SessionId}");
}

// Update session activity
sessionManager.UpdateSessionActivity(session.SessionId);

// Get session summaries for monitoring
var summaries = sessionManager.GetSessionSummaries();
foreach (var summary in summaries)
{
    Console.WriteLine($"Session {summary.SessionId}: " +
        $"User={summary.UserId}, " +
        $"Active={summary.IsActive}, " +
        $"LastActivity={summary.LastActivityAt}, " +
        $"Streams={summary.StreamCount}");
}

// Close session when complete
bool sessionClosed = await sessionManager.CloseSessionAsync(session.SessionId);
Console.WriteLine($"Session closed: {sessionClosed}");
```

## BidirectionalStreamContext

The `BidirectionalStreamContext` class represents the execution context for a bidirectional gRPC stream, managing the complete lifecycle from creation to disposal. It handles message channels for both inbound and outbound communication, flow control, backpressure management, metrics collection, and graceful stream termination with configurable timeouts and cleanup policies. This context is the central state container for all bidirectional streaming operations in the gRPC-Web Bridge.

Example usage:

```csharp
// Create a bidirectional stream context for duplex communication
var streamContext = new BidirectionalStreamContext(
    streamId: Guid.NewGuid().ToString(),
    methodType: MethodType.Duplex,
    maxSize: 1024 * 1024 // 1MB max message size
)
{
    CreatedAt = DateTime.UtcNow
};

Console.WriteLine($"Created stream context: {streamContext.StreamId}");
Console.WriteLine($"Method type: {streamContext.MethodType}");
Console.WriteLine($"Created at: {streamContext.CreatedAt}");

// Send a message through the outbound channel
await streamContext.OutboundChannel.Writer.WriteAsync(new StreamMessage(
    streamId: streamContext.StreamId,
    messageType: StreamMessageType.Data,
    sequenceNumber: 1,
    data: Encoding.UTF8.GetBytes("{\"message\": \"Hello from client!\"}")
));

// Receive a message from the inbound channel
if (streamContext.InboundChannel.Reader.TryRead(out var receivedMessage))
{
    Console.WriteLine($"Received message: {Encoding.UTF8.GetString(receivedMessage.Data)}");
}

// Monitor stream state and metrics
Console.WriteLine($"Stream state: {streamContext.State}");
Console.WriteLine($"Messages in: {streamContext.Metrics?.MessagesIn}");
Console.WriteLine($"Messages out: {streamContext.Metrics?.MessagesOut}");

// Check flow control status
Console.WriteLine($"Window utilization: {streamContext.WindowUtilization:P0}");
Console.WriteLine($"Available credits: {streamContext.AvailableCredits}");

// Close the stream gracefully with a status
streamContext.FinalStatus = GrpcStatusCode.Ok;
streamContext.CloseReason = "Client completed operation";

// Dispose the context when done (automatically cleans up resources)
await streamContext.DisposeAsync();
```

## StreamingService

The `StreamingService` class manages gRPC streaming sessions for the gRPC-Web Bridge, enabling bidirectional communication between gRPC-Web clients and gRPC services. It handles stream creation, message queuing and processing, heartbeat monitoring, and automatic cleanup of idle or completed streams. The service provides statistics tracking, stream state management, and supports both unary and streaming method types with configurable timeouts and heartbeats.

Example usage:

```csharp
// Create and configure a streaming service
var streamingService = new StreamingService(
    streamId: Guid.NewGuid().ToString(),
    methodType: MethodType.ServerStreaming,
    serviceName: "UserService",
    methodName: "StreamUserUpdates"
);

// Create a new stream
var newStream = streamingService.CreateStream();
Console.WriteLine($"Created stream: {newStream.StreamId}");

// Enqueue messages for streaming to clients
streamingService.EnqueueMessage(streamingService.StreamId, new StreamMessage(
    streamId: streamingService.StreamId,
    messageType: StreamMessageType.Data,
    sequenceNumber: 1,
    data: Encoding.UTF8.GetBytes("{\"userId\": \"user-123\", \"status\": \"active\"}")
));

// Dequeue messages for processing
var dequeuedMessage = streamingService.DequeueMessage(streamingService.StreamId);
if (dequeuedMessage != null)
{
    Console.WriteLine($"Dequeued message {dequeuedMessage.SequenceNumber}: {Encoding.UTF8.GetString(dequeuedMessage.Data)}");
}

// Send heartbeat to keep stream alive
streamingService.SendHeartbeat(streamingService.StreamId);

// Get stream statistics for monitoring
var stats = streamingService.GetStreamStatistics();
Console.WriteLine($"Stream {streamingService.StreamId}: {stats.MessageCount} messages, " +
                 $"State: {streamingService.State}, Created: {streamingService.CreatedAt}");

// Get all active stream IDs
var allStreamIds = streamingService.GetAllStreamIds();
Console.WriteLine($"Active streams: {string.Join(", ", allStreamIds)}");

// Close stream when complete
streamingService.CloseStream(streamingService.StreamId);
```

## StreamCleanupWorker

Example usage:

```csharp
// Configure services in Program.cs
builder.Services.AddStreamCleanupWorker(options =>
{
    options.CleanupIntervalSeconds = 30;
    options.IdleTimeoutDuration = TimeSpan.FromMinutes(2);
    options.StaleStreamDuration = TimeSpan.FromMinutes(5);
    options.GcTriggerThreshold = 5;
});

// In your ASP.NET Core application startup
var cleanupWorker = app.Services.GetRequiredService<StreamCleanupWorker>();

// Start the stream cleanup worker (typically done automatically by IHostedService)
// cleanupWorker.Start(); // Not needed - runs automatically

// Get cleanup statistics for monitoring
var statistics = cleanupWorker.GetStatistics();
Console.WriteLine($"Total cleanups run: {statistics.totalCleanupsRun}");
Console.WriteLine($"Total streams removed: {statistics.totalStreamsRemoved}");
Console.WriteLine($"Average streams per cleanup: {statistics.averageStreamsPerCleanup:F2}");
Console.WriteLine($"Current cleanup interval: {statistics.cleanupInterval}s");
Console.WriteLine($"Idle timeout: {statistics.idleTimeout}s");
Console.WriteLine($"Stale stream timeout: {statistics.staleStreamTimeout}s");

// Access configuration properties
int interval = cleanupWorker.CleanupIntervalSeconds;
TimeSpan idleTimeout = cleanupWorker.IdleTimeoutDuration;
TimeSpan staleTimeout = cleanupWorker.StaleStreamDuration;
int gcThreshold = cleanupWorker.GcTriggerThreshold;
```

## MetricsCollectionWorkerExtensions

The `MetricsCollectionWorkerExtensions` class provides extension methods for `MetricsCollectionWorker" that enable advanced metrics analysis, filtering, and reporting capabilities. It includes utilities for filtering snapshots by time range, calculating peak usage statistics, analyzing trends over time windows, and generating alert summaries based on configurable thresholds.

Example usage:

```csharp
// Create a metrics collection worker instance
var worker = new MetricsCollectionWorker();

// Get snapshots within a specific time range
var startTime = DateTime.UtcNow.AddHours(-1);
var endTime = DateTime.UtcNow;
var filteredSnapshots = worker.GetSnapshotsInRange(startTime, endTime);
Console.WriteLine($"Found {filteredSnapshots.Count} snapshots in range");

// Get peak usage statistics across all metrics
var peakStats = worker.GetPeakUsageStatistics();
Console.WriteLine($"Peak CPU: {peakStats.peakCpu.value}% at {peakStats.peakCpu.timestamp}");
Console.WriteLine($"Peak Memory: {peakStats.peakMemory.value}MB at {peakStats.peakMemory.timestamp}");
Console.WriteLine($"Peak Threads: {peakStats.peakThreads.value} at {peakStats.peakThreads.timestamp}");

// Get trend analysis for the last 30 minutes
var trendAnalysis = worker.GetTrendAnalysis(minutes: 30);
Console.WriteLine($"CPU trend: {trendAnalysis.cpuTrend.direction} (slope: {trendAnalysis.cpuTrend.slope})");
Console.WriteLine($"Memory trend: {trendAnalysis.memoryTrend.direction} (slope: {trendAnalysis.memoryTrend.slope})");
Console.WriteLine($"Error rate trend: {trendAnalysis.errorRateTrend.direction} (slope: {trendAnalysis.errorRateTrend.slope})");

// Get alert summary for the last hour
var alertSummary = worker.GetAlertSummary(lookbackMinutes: 60);
Console.WriteLine($"Healthy: {alertSummary.isHealthy}");
Console.WriteLine($"Total alerts: {alertSummary.alertsFound}");
Console.WriteLine($"Alert breakdown: CPU={alertSummary.alertBreakdown.cpuAlerts}, Memory={alertSummary.alertBreakdown.memoryAlerts}, ErrorRate={alertSummary.alertBreakdown.errorRateAlerts}");

if (alertSummary.recentAlerts.Count > 0)
{
    Console.WriteLine("Recent alerts:");
    foreach (var alert in alertSummary.recentAlerts)
    {
        Console.WriteLine($"  - [{alert.timestamp}] {alert.type}: {alert.value} (threshold: {alert.threshold})");
    }
}
```

## GrpcServiceDescriptor

The `GrpcServiceDescriptor` class represents a registered gRPC service as discovered through the reflection API. It contains all metadata required for clients to understand and invoke the service without prior knowledge of the proto descriptor file, including service identity, endpoint configuration, and method definitions.

Example usage:

```csharp
// Create a service descriptor from reflection discovery
var serviceDescriptor = new GrpcServiceDescriptor
{
    FullName = "example.v1.UserService",
    Name = "UserService",
    PackageName = "example.v1",
    Description = "Handles user authentication, profile management, and authorization",
    Endpoint = "user-service.example.com",
    Port = 50051,
    UseTls = true,
    Methods = [
        new MethodDescriptor
        {
            Name = "GetUser",
            FullName = "example.v1.UserService/GetUser",
            ServiceFullName = "example.v1.UserService",
            MethodType = "Unary",
            IsClientStreaming = false,
            IsServerStreaming = false,
            InputMessageType = "GetUserRequest",
            OutputMessageType = "UserResponse",
            IsDeprecated = false,
            Description = "Retrieves a user by their unique identifier",
            TimeoutMilliseconds = 5000
        },
        new MethodDescriptor
        {
            Name = "StreamUserUpdates",
            FullName = "example.v1.UserService/StreamUserUpdates",
            ServiceFullName = "example.v1.UserService",
            MethodType = "ServerStreaming",
            IsClientStreaming = false,
            IsServerStreaming = true,
            InputMessageType = "UserFilter",
            OutputMessageType = "UserUpdate",
            IsDeprecated = false,
            Description = "Streams real-time user profile updates matching the filter criteria",
            TimeoutMilliseconds = 30000
        }
    ]
};

// Access service metadata for client configuration
Console.WriteLine($"Service: {serviceDescriptor.FullName}");
Console.WriteLine($"Endpoint: {serviceDescriptor.Endpoint}:{serviceDescriptor.Port}");
Console.WriteLine($"TLS enabled: {serviceDescriptor.UseTls}");
Console.WriteLine($"Methods available: {serviceDescriptor.Methods.Count}");

// Find a specific method by name
var getUserMethod = serviceDescriptor.Methods.FirstOrDefault(m => m.Name == "GetUser");
if (getUserMethod != null)
{
    Console.WriteLine($"Method: {getUserMethod.FullName}");
    Console.WriteLine($"Type: {getUserMethod.MethodType}");
    Console.WriteLine($"Input: {getUserMethod.InputMessageType}");
    Console.WriteLine($"Output: {getUserMethod.OutputMessageType}");
    Console.WriteLine($"Timeout: {getUserMethod.TimeoutMilliseconds}ms");
}
```

## ServiceRegistrationException

The `ServiceRegistrationException` class represents exceptions thrown during service registration and discovery processes in the gRPC-Web Bridge. It allows for detailed error context by including the affected service name and endpoint, enabling robust error handling for service connectivity issues.

Example usage:

```csharp
// Create a basic registration exception
var exception = new ServiceRegistrationException("Service registration failed due to timeout");

// Create an exception with specific service details
var serviceException = new ServiceRegistrationException("UserService", "Failed to resolve endpoint");

// Create an exception with service and endpoint details
var connectionException = new ServiceRegistrationException(
    "OrderService",
    "https://orders.example.com",
    "Service unavailable"
);

// Access exception properties
Console.WriteLine(serviceException.ServiceName); // "UserService"
Console.WriteLine(connectionException.ServiceEndpoint); // "https://orders.example.com"

// Get formatted string representation
Console.WriteLine(connectionException.ToString());
// Output includes: Failed to connect to service 'OrderService' at https://orders.example.com: Service unavailable | Service: OrderService | Endpoint: https://orders.example.com
```

## ProtocolException

The `ProtocolException` class represents exceptions thrown during protocol translation and conversion operations in the gRPC-Web Bridge. It provides detailed error information including source/target formats, request identifiers, and comprehensive error context to enable robust error handling and debugging for protocol translation scenarios.

Example usage:

```csharp
// Create a protocol exception with source and target formats
var exception = new ProtocolException(
    sourceFormat: "Protobuf",
    targetFormat: "JSON",
    message: "Failed to convert message from Protobuf to JSON format"
);

// Set request ID for correlation tracking
exception.RequestId = Guid.NewGuid().ToString();

// Access exception properties
Console.WriteLine(exception.Message);
Console.WriteLine(exception.SourceFormat);  // "Protobuf"
Console.WriteLine(exception.TargetFormat);    // "JSON"
Console.WriteLine(exception.RequestId);       // Request GUID
Console.WriteLine(exception.ErrorCode);       // "TRANSLATION_FAILED"
Console.WriteLine(exception.ToString());       // Includes all context

// Create a simple protocol exception with custom message
var simpleException = new ProtocolException("Invalid protocol message received");
Console.WriteLine(simpleException.ErrorCode);  // "PROTOCOL_ERROR"
```

## StreamingException

The `StreamingException` class represents exceptions thrown during streaming operations in the gRPC-Web Bridge. It provides detailed error information including stream identifiers, sequence numbers, and stream state tracking to enable comprehensive error handling and debugging for streaming scenarios.

Example usage:

```csharp
// Create a streaming exception with a custom message
var exception = new StreamingException("Failed to establish streaming connection");
Console.WriteLine(exception.Message); // "Failed to establish streaming connection"
Console.WriteLine(exception.ErrorCode); // "STREAMING_ERROR"

// Create a streaming exception with stream ID and message
var streamException = new StreamingException("stream-123", "Connection timeout occurred");
Console.WriteLine(streamException.StreamId); // "stream-123"
Console.WriteLine(streamException.Message); // "Stream 'stream-123' error: Connection timeout occurred"
Console.WriteLine(streamException.ErrorCode); // "STREAM_FAILED"

// Create a streaming exception with stream ID and sequence number
var messageException = new StreamingException("stream-456", 42, "Invalid message format");
Console.WriteLine(messageException.StreamId); // "stream-456"
Console.WriteLine(messageException.SequenceNumber); // 42
Console.WriteLine(messageException.Message); // "Stream 'stream-456' message 42 error: Invalid message format"
Console.WriteLine(messageException.ErrorCode); // "STREAM_MESSAGE_ERROR"

// Set stream state for additional context
var exceptionWithState = new StreamingException("stream-789", "Processing failed");
exceptionWithState.SetStreamState(StreamState.Failed);
Console.WriteLine(exceptionWithState.LastStreamState); // StreamState.Failed

// Custom exception with inner exception
try
{
    await SomeStreamingOperationAsync();
}
catch (Exception ex)
{
    var streamingEx = new StreamingException("stream-999", "Stream processing error", ex);
    streamingEx.SetStreamState(StreamState.Processing);
    Console.WriteLine(streamingEx.ToString());
    // Output includes: Stream: stream-999 | State: Processing | ...
}
```

## ServiceRepository

The `ServiceRepository` class provides a centralized in-memory repository for managing gRPC services, their metadata, and associated request/response data. It serves as the primary data access layer for service discovery, registration, and lookup operations within the gRPC-Web Bridge. The repository maintains collections of services, requests, and responses with comprehensive CRUD operations and search capabilities.

Example usage:

```csharp
// Create a service repository instance
var serviceRepository = new ServiceRepository();

// Add a new gRPC service
var userService = new GrpcService(
    name: "UserService",
    host: "localhost",
    port: 50051,
    serviceType: ServiceType.Grpc
)
{
    Description = "Handles user authentication and profile management",
    UseTls = true,
    Status = ServiceStatus.Serving
};

bool addSuccess = await serviceRepository.AddAsync(userService);
Console.WriteLine($"Service added: {(addSuccess ? "SUCCESS" : "FAILED")}");

// Retrieve a service by ID
var retrievedService = await serviceRepository.GetByIdAsync(userService.Id);
if (retrievedService != null)
{
    Console.WriteLine($"Retrieved service: {retrievedService.Name} ({retrievedService.FullName})");
}

// Get service by full name
var serviceByName = await serviceRepository.GetByFullNameAsync("UserService");
Console.WriteLine($"Found by full name: {serviceByName?.Name}");

// Add a method to the service
var getUserMethod = new GrpcMethod(
    name: "GetUser",
    fullName: "UserService/GetUser",
    methodType: MethodType.Unary,
    inputMessage: "GetUserRequest",
    outputMessage: "UserResponse"
);
userService.AddMethod(getUserMethod);

// Update the service
bool updateSuccess = await serviceRepository.UpdateAsync(userService);
Console.WriteLine($"Service updated: {(updateSuccess ? "SUCCESS" : "FAILED")}");

// Add a request for this service
var request = new GrpcRequest(
    serviceId: userService.Id,
    method: "GetUser",
    payload: Encoding.UTF8.GetBytes("{\"userId\": \"123\"}")
);
bool requestAdded = await serviceRepository.AddRequestAsync(request);
Console.WriteLine($"Request added: {(requestAdded ? "SUCCESS" : "FAILED")}");

// Add a response
var response = new GrpcResponse(
    requestId: request.Id,
    payload: Encoding.UTF8.GetBytes("{\"id\": \"123\", \"name\": \"John Doe\"}")
);
response.SetSuccess(Encoding.UTF8.GetBytes("{\"success\": true}"), SerializationFormat.Json);
bool responseAdded = await serviceRepository.AddResponseAsync(response);
Console.WriteLine($"Response added: {(responseAdded ? "SUCCESS" : "FAILED")}");

// Search for services
var searchResults = await serviceRepository.SearchAsync("user", page: 1, pageSize: 10);
Console.WriteLine($"Found {searchResults.Items.Count()} matching services");

// Check if service exists
bool exists = await serviceRepository.ExistsAsync(userService.Id);
Console.WriteLine($"Service exists: {exists}");

// Get all services
var allServices = await serviceRepository.GetAllAsync();
Console.WriteLine($"Total services: {allServices.Count()}");

// Get services by package
var packageServices = await serviceRepository.GetByPackageAsync("example.v1");
Console.WriteLine($"Services in package: {packageServices.Count()}");

// Get paged results
var pagedResults = await serviceRepository.GetPagedAsync(page: 1, pageSize: 20);
Console.WriteLine($"Page 1: {pagedResults.Items.Count()} items, Total: {pagedResults.Total} items");

// Count services
int serviceCount = await serviceRepository.CountAsync();
Console.WriteLine($"Total service count: {serviceCount}");

// Delete a service
bool deleteSuccess = await serviceRepository.DeleteAsync(userService.Id);
Console.WriteLine($"Service deleted: {(deleteSuccess ? "SUCCESS" : "FAILED")}");
```

## ServiceExtensions

The `ServiceExtensions` class provides extension methods for service registration, validation, and health monitoring in the gRPC-Web Bridge. It includes utilities for safely registering services and methods, converting exceptions to gRPC responses, checking service health, and generating human-readable status messages.

Example usage:

```csharp
// Create a service repository and service instance
var serviceRepository = new ServiceRepository();
var service = new GrpcService(
    name: "UserService",
    host: "localhost",
    port: 50051,
    serviceType: ServiceType.Grpc
);

// Safely register the service (returns true on success, false on failure)
bool registrationSuccess = await serviceRepository.TryRegisterServiceAsync(service);
Console.WriteLine($"Service registration: {(registrationSuccess ? "SUCCESS" : "FAILED")}");

// Add a method to the service
var method = new GrpcMethod(
    name: "GetUser",
    methodType: MethodType.Unary,
    inputType: "UserRequest",
    outputType: "UserResponse"
);

bool methodAdded = service.TryAddMethod(method);
Console.WriteLine($"Method added: {(methodAdded ? "SUCCESS" : "FAILED")}");

// Create a protocol translation service for error handling
var translationService = new ProtocolTranslationService();

// Convert an exception to a gRPC response
try
{
    // Some operation that might fail
    var result = await SomeRiskyOperationAsync();
}
catch (Exception ex)
{
    var requestId = Guid.NewGuid().ToString();
    var grpcResponse = ex.ToGrpcResponse(requestId, translationService);
    Console.WriteLine($"Error response created: {grpcResponse.StatusCode}");
}

// Get human-readable status messages
string okMessage = GrpcStatusCode.Ok.GetStatusMessage();
string notFoundMessage = GrpcStatusCode.NotFound.GetStatusMessage();
Console.WriteLine($"Status messages: OK={okMessage}, NotFound={notFoundMessage}");

// Check if a status code represents an error
bool isError = GrpcStatusCode.Internal.IsError();
Console.WriteLine($"Is Internal error: {isError}");

// Convert gRPC status codes to HTTP status codes
int httpStatus = GrpcStatusCode.NotFound.ToHttpStatusCode();
Console.WriteLine($"HTTP status for NotFound: {httpStatus}");

// Get service health summary
var registry = new ServiceRegistry();
var streaming = new StreamingService();
var healthSummary = registry.GetHealthSummary(streaming);
Console.WriteLine($"Health: {healthSummary.TotalServices} total, {healthSummary.HealthyServices} healthy, {healthSummary.UnhealthyServices} unhealthy");
Console.WriteLine($"Health percentage: {healthSummary.HealthPercentage:F1}%");
Console.WriteLine($"Active streams: {healthSummary.ActiveStreams}");

// Convert method types to descriptions
string unaryDescription = MethodType.Unary.ToDescription();
string streamingDescription = MethodType.ServerStreaming.ToDescription();
Console.WriteLine($"Method descriptions: Unary={unaryDescription}, ServerStreaming={streamingDescription}");
```

## ValidationException

The `ValidationException` class represents exceptions thrown when input data validation fails in the gRPC-Web Bridge. It provides detailed error information including the invalid field name, the invalid value, and the validation rule that failed, enabling precise error handling and client feedback.

Example usage:

```csharp
// Create a validation exception with field name, invalid value, and validation rule
var validationException = new ValidationException(
    fieldName: "email",
    invalidValue: "invalid-email",
    validationRule: "email_format",
    message: "Email address format is invalid"
);

// Access exception properties for detailed error reporting
Console.WriteLine(validationException.Message); // "Validation failed for 'email': Email address format is invalid (Value: invalid-email, Rule: email_format)"
Console.WriteLine(validationException.FieldName); // "email"
Console.WriteLine(validationException.InvalidValue); // "invalid-email"
Console.WriteLine(validationException.ValidationRule); // "email_format"
Console.WriteLine(validationException.ToString()); // Includes all context

// Create a simple validation exception with custom message
var simpleException = new ValidationException("Username is required");
Console.WriteLine(simpleException.ErrorCode); // "VALIDATION_ERROR"
```

## WebhookPublisher

The `WebhookPublisher` class implements a webhook publisher that sends events to external HTTP endpoints. It supports subscribing to specific event types, custom HTTP headers, automatic retry on failure, and comprehensive statistics tracking. Events are processed asynchronously in the background for high throughput scenarios.

Example usage:

```csharp
// Create a webhook publisher (typically registered via dependency injection)
var webhookPublisher = new WebhookPublisher(logger, httpClientFactory);

// Subscribe to specific event types with custom headers and retry enabled
var subscriptionId = webhookPublisher.Subscribe(
    webhookUrl: "https://external-service.example.com/api/webhooks",
    eventTypes: ["UserCreatedEvent", "UserUpdatedEvent"],
    headers: new Dictionary<string, string>
    {
        {"Authorization", "Bearer secret-token-123"},
        {"X-Custom-Header", "custom-value"}
    },
    retryOnFailure: true
);

Console.WriteLine($"Created subscription: {subscriptionId}");

// Subscribe to all events (empty array means all event types)
var allEventsSubscriptionId = webhookPublisher.Subscribe(
    webhookUrl: "https://monitoring.example.com/events",
    eventTypes: []
);

// Publish an event to all matching subscriptions
var userCreatedEvent = new UserCreatedEvent
{
    EventId = Guid.NewGuid().ToString(),
    Username = "johndoe",
    Email = "john@example.com",
    CreatedAt = DateTime.UtcNow,
    Source = "UserService"
};

await webhookPublisher.PublishEventAsync(userCreatedEvent);

// Get all active subscriptions
var activeSubscriptions = webhookPublisher.GetSubscriptions();
Console.WriteLine($"Active subscriptions: {activeSubscriptions.Count}");

// Get publishing statistics
var statistics = webhookPublisher.GetStatistics();
Console.WriteLine($"Total events sent: {statistics.totalEventsSent}");
Console.WriteLine($"Total events failed: {statistics.totalEventsFailed}");

// Unsubscribe when no longer needed
bool unsubscribed = webhookPublisher.Unsubscribe(subscriptionId);
Console.WriteLine($"Unsubscribed: {unsubscribed}");

// Clean up when application shuts down
webhookPublisher.Dispose();
```

## GrpcWebBridgeException

The `GrpcWebBridgeException` class serves as the base exception type for all gRPC-Web bridge operations. It provides structured error handling with support for gRPC status codes, custom error codes, and contextual metadata that enables detailed error reporting and debugging across the bridge's protocol translation and service routing layers.

Example usage:

```csharp
// Create a basic bridge exception with a custom error code
var exception = new GrpcWebBridgeException(
    "Failed to establish connection to downstream service",
    "DOWNSTREAM_CONNECTION_FAILED"
);

// Access exception properties
Console.WriteLine(exception.ErrorCode); // "DOWNSTREAM_CONNECTION_FAILED"
Console.WriteLine(exception.Message); // "Failed to establish connection to downstream service"

// Add contextual metadata for debugging
var requestId = Guid.NewGuid().ToString();
exception.AddContext("requestId", requestId);
exception.AddContext("serviceName", "UserService");
exception.AddContext("attempt", 3);

// Create a bridge exception with gRPC status code
var grpcException = new GrpcWebBridgeException(
    "Invalid request format received",
    GrpcStatusCode.InvalidArgument
);
Console.WriteLine(grpcException.GrpcStatus); // GrpcStatusCode.InvalidArgument

// Chain operations using fluent API
var finalException = new GrpcWebBridgeException(
    "Failed to process gRPC-Web request",
    "PROCESSING_ERROR"
)
.WithContext("endpoint", "/api/UserService/GetUser")
.WithContext("timestamp", DateTime.UtcNow)
.WithInnerException(new InvalidOperationException("Database connection failed"));

// Retrieve context values
var endpoint = exception.GetContext("endpoint") as string;
var errorCode = exception.ErrorCode;

// Get formatted string representation
Console.WriteLine(finalException.ToString());
// Output includes: Failed to process gRPC-Web request [ErrorCode: PROCESSING_ERROR] [GrpcStatus: ...]
```

## ConfigurationException

The `ConfigurationException` class represents exceptions thrown when configuration validation fails in the gRPC-Web Bridge. It provides detailed error information including the configuration key, configuration value, and a descriptive error message, enabling precise error handling and debugging for configuration-related issues.

Example usage:

```csharp
// Create a basic configuration exception with a custom message
var exception = new ConfigurationException("Invalid configuration value provided");

// Access exception properties
Console.WriteLine(exception.Message); // "Invalid configuration value provided"
Console.WriteLine(exception.ErrorCode); // "CONFIGURATION_ERROR"
Console.WriteLine(exception.GrpcStatus); // GrpcStatusCode.InvalidArgument

// Create a configuration exception with key and message
var keyException = new ConfigurationException("DatabaseConnection", "Connection string is empty");
Console.WriteLine(keyException.ConfigurationKey); // "DatabaseConnection"
Console.WriteLine(keyException.Message); // "Configuration 'DatabaseConnection' error: Connection string is empty"

// Create a configuration exception with key, value, and message
var fullException = new ConfigurationException(
  "DatabaseConnection",
  "Server=localhost;Database=test",
  "Failed to connect to database"
);
Console.WriteLine(fullException.ConfigurationKey); // "DatabaseConnection"
Console.WriteLine(fullException.ConfigurationValue); // "Server=localhost;Database=test"
Console.WriteLine(fullException.Message); // "Configuration 'DatabaseConnection' with value 'Server=localhost;Database=test' error: Failed to connect to database"
Console.WriteLine(fullException.ToString()); // Includes all context with format: ... | ConfigKey: DatabaseConnection | ConfigValue: Server=localhost;Database=test

// Create a configuration exception with inner exception
try
{
  // Some configuration validation
}
catch (Exception ex)
{
  var configException = new ConfigurationException(
    "TimeoutSettings",
    "RequestTimeout",
    "Timeout value must be greater than 0 seconds",
    ex
  );
  Console.WriteLine(configException.ToString());
}
```


## GrpcRequest

The `GrpcRequest` class represents a gRPC request intercepted or created by the bridge. It encapsulates all metadata, payload, and routing information needed for protocol translation between gRPC and gRPC-Web clients. The class provides fluent APIs for building requests, managing metadata, and validating request integrity before processing.

Example usage:

```csharp
// Create a new gRPC request with service and method names
var request = new GrpcRequest(
    serviceName: "UserService",
    methodName: "GetUserById",
    payload: Encoding.UTF8.GetBytes("{\"userId\": \"12345\"}")
);

// Set request identifiers for tracing and correlation
request.RequestId = Guid.NewGuid().ToString("N");
request.TraceId = Guid.NewGuid().ToString("N");
request.UserId = "user-123";

// Configure request behavior
request.TimeoutMilliseconds = 5000; // 5 second timeout
request.MethodType = MethodType.Unary;

// Add metadata for downstream services
request.AddMetadata("x-request-id", request.RequestId);
request.AddMetadata("x-user-id", request.UserId);
request.AddMetadata("x-trace-id", request.TraceId);
request.AddMetadata("authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");

// Validate the request before processing
request.Validate();

// Access request properties for routing and processing
Console.WriteLine($"Processing request: {request.FullMethodName}");
Console.WriteLine($"Service: {request.ServiceName}, Method: {request.MethodName}");
Console.WriteLine($"Payload size: {request.Payload.Length} bytes");
Console.WriteLine($"Timeout: {request.TimeoutMilliseconds}ms");

// Retrieve metadata for downstream service calls
string? authHeader = request.GetMetadata("authorization");
string? userId = request.GetMetadata("x-user-id");

// Check if metadata exists
bool hasTraceId = request.HasMetadata("x-trace-id");

// Get payload for deserialization
byte[] payloadCopy = request.GetPayloadCopy();
string payloadHash = request.GetPayloadHash();

// Update payload if needed (e.g., after transformation)
request.SetPayload(Encoding.UTF8.GetBytes("{\"userId\": \"12345\", \"name\": \"John Doe\"}"), SerializationFormat.Json);
```

## GrpcService

The `GrpcService` class represents a gRPC service with its metadata, configuration, and available methods. It serves as a container for service information including endpoint details, service status, and method definitions, enabling the gRPC-Web Bridge to route requests to appropriate gRPC services.

Example usage:

```csharp
// Create a new gRPC service instance
var userService = new GrpcService(
    name: "UserService",
    packageName: "com.example.grpc",
    endpoint: "user-service.example.com",
    port: 50051
)
{
    Description = "Handles user authentication and profile management",
    UseTls = true,
    Status = ServiceStatus.Serving
};

// Add methods to the service
userService.AddMethod(new GrpcMethod(
    name: "GetUser",
    methodType: MethodType.Unary,
    inputType: "GetUserRequest",
    outputType: "UserResponse"
));

userService.AddMethod(new GrpcMethod(
    name: "CreateUser",
    methodType: MethodType.Unary,
    inputType: "CreateUserRequest",
    outputType: "UserResponse"
));

userService.AddMethod(new GrpcMethod(
    name: "StreamUserUpdates",
    methodType: MethodType.ServerStreaming,
    inputType: "UserFilter",
    outputType: "UserUpdate"
));

// Access service properties
Console.WriteLine($"Service: {userService.FullName}");
Console.WriteLine($"Endpoint: {userService.Endpoint}:{userService.Port}");
Console.WriteLine($"Status: {userService.Status}");
Console.WriteLine($"Created: {userService.CreatedAt}");

// Manage metadata
userService.SetMetadata("environment", "production");
userService.SetMetadata("version", "1.2.3");
userService.SetMetadata("team", "backend-platform");

string? environment = userService.GetMetadata("environment");
Console.WriteLine($"Environment: {environment}");

// Check if service has specific methods
bool hasGetUser = userService.HasMethod("GetUser");
bool hasNonExistentMethod = userService.HasMethod("NonExistentMethod");

// Retrieve a method by name
var getUserMethod = userService.GetMethod("GetUser");
if (getUserMethod != null)
{
    Console.WriteLine($"Found method: {getUserMethod.Name} ({getUserMethod.MethodType})");
}

// Remove a method
userService.RemoveMethod("CreateUser");

// Validate service configuration
try
{
    userService.Validate();
    Console.WriteLine("Service configuration is valid");
}
catch (Exception ex)
{
    Console.WriteLine($"Validation error: {ex.Message}");
}
```

## StreamMessage

The `StreamMessage` class represents a message in a gRPC streaming session, encapsulating both the payload and metadata required for protocol translation between gRPC and gRPC-Web clients. It supports various serialization formats, compression, error handling, and provides fluent APIs for building and modifying stream messages during bidirectional communication.

Example usage:

```csharp
// Create a new stream message for a user data stream
var message = new StreamMessage(
    streamId: "user-updates-stream-123",
    messageType: StreamMessageType.Data,
    sequenceNumber: 1,
    data: Encoding.UTF8.GetBytes("{\"userId\": \"user-456\", \"status\": \"active\"}")
);

// Set message metadata
message.SetMetadata("content-type", "application/json");
message.SetMetadata("user-id", "user-456");
message.SetMetadata("priority", "high");

// Configure compression for large payloads
message.IsCompressed = true;
message.CompressionLevel = 6;

// Set serialization format
message.Format = SerializationFormat.Json;

// Add custom headers for downstream processing
message.Headers = new Dictionary<string, string>
{
    {"x-request-id", Guid.NewGuid().ToString("N")},
    {"x-trace-id", Guid.NewGuid().ToString("N")}
};

// Set message status for error handling
message.SetStatus(GrpcStatusCode.OK, "User update processed successfully");

// Access message properties
Console.WriteLine($"Stream: {message.StreamId}");
Console.WriteLine($"Sequence: {message.SequenceNumber}");
Console.WriteLine($"Type: {message.MessageType}");
Console.WriteLine($"Created: {message.CreatedAt}");
Console.WriteLine($"Compressed: {message.IsCompressed}");
Console.WriteLine($"Format: {message.Format}");

// Update message data if needed
message.SetData(Encoding.UTF8.GetBytes("{\"userId\": \"user-456\", \"status\": \"active\", \"lastLogin\": \"2024-07-16T10:30:00Z\"}"), SerializationFormat.Json);

// Create a heartbeat message
var heartbeat = new StreamMessage(
    streamId: "heartbeat-stream-789",
    messageType: StreamMessageType.Heartbeat,
    sequenceNumber: 0
);
heartbeat.SetHeartbeat();
```

## ServiceDiscoveryClient

The `ServiceDiscoveryClient` class provides service discovery and health monitoring capabilities for gRPC services in the gRPC-Web Bridge. It enables dynamic registration and deregistration of services, discovery of available service instances, health checks via heartbeats, and automatic cache refresh for service instances.

Example usage:

```csharp
// Create a service discovery client (typically registered via dependency injection)
var serviceDiscovery = new ServiceDiscoveryClient(
    id: "order-service-client",
    name: "OrderService",
    host: "order-service.example.com",
    port: 50051,
    metadata: new Dictionary<string, string>
    {
        {"environment", "production"},
        {"version", "1.2.3"}
    }
);

Console.WriteLine($"Created service discovery client: {serviceDiscovery.Name} ({serviceDiscovery.Id})");

// Register the service instance with the discovery system
bool registrationSuccess = await serviceDiscovery.RegisterServiceAsync();
Console.WriteLine($"Service registration: {(registrationSuccess ? "SUCCESS" : "FAILED")}");

// Send a heartbeat to indicate the service is alive
bool heartbeatSuccess = await serviceDiscovery.SendHeartbeatAsync();
Console.WriteLine($"Heartbeat sent: {(heartbeatSuccess ? "SUCCESS" : "FAILED")}");

// Discover all available service instances
var allInstances = await serviceDiscovery.DiscoverServicesAsync();
Console.WriteLine($"Discovered {allInstances.Count} service instances");

// Get a healthy instance (filters by status and last heartbeat)
var healthyInstance = await serviceDiscovery.GetHealthyInstanceAsync();
if (healthyInstance != null)
{
    Console.WriteLine($"Healthy instance found: {healthyInstance.Host}:{healthyInstance.Port} (Status: {healthyInstance.Status})");
}

// Start automatic cache refresh (refreshes every 30 seconds by default)
serviceDiscovery.StartAutoRefresh();

// Get cached services (avoids network calls)
var cachedServices = serviceDiscovery.GetCachedServices();
Console.WriteLine($"Cached services count: {cachedServices.Count}");

// Clear the cache if needed (e.g., after configuration changes)
serviceDiscovery.ClearCache();

// Get service statistics
var stats = serviceDiscovery.GetStatistics();
Console.WriteLine($"Service statistics: {stats}");

// Check service status
Console.WriteLine($"Service ID: {serviceDiscovery.Id}");
Console.WriteLine($"Service Name: {serviceDiscovery.Name}");
Console.WriteLine($"Service Host: {serviceDiscovery.Host}");
Console.WriteLine($"Service Port: {serviceDiscovery.Port}");
Console.WriteLine($"Service Status: {serviceDiscovery.Status}");
Console.WriteLine($"Registered At: {serviceDiscovery.RegisteredAt}");
Console.WriteLine($"Last Heartbeat: {serviceDiscovery.LastHeartbeat}");

// Deregister the service when shutting down
bool deregistrationSuccess = await serviceDiscovery.DeregisterServiceAsync();
Console.WriteLine($"Service deregistration: {(deregistrationSuccess ? "SUCCESS" : "FAILED")}");

// Clean up resources
serviceDiscovery.Dispose();
```

## GrpcConnectionManagerExtensions

The `GrpcConnectionManagerExtensions` class provides extension methods for `GrpcConnectionManager` that enable comprehensive monitoring and analysis of gRPC connection metrics. It offers utilities for retrieving connection statistics, analyzing connection patterns, filtering by service, and generating observability data for monitoring and debugging purposes.

Example usage:

```csharp
// Create a connection manager instance (typically injected via DI)
var connectionManager = new GrpcConnectionManager();

// Get basic connection statistics
int activeConnections = connectionManager.GetActiveConnectionCount();
long totalRequests = connectionManager.GetTotalRequestCount();
long totalBytesSent = connectionManager.GetTotalBytesSent();
long totalBytesReceived = connectionManager.GetTotalBytesReceived();

// Get average connection duration across all active connections
TimeSpan avgDuration = connectionManager.GetAverageConnectionDuration();
Console.WriteLine($"Average connection duration: {avgDuration.TotalSeconds:F2}s");

// Get metrics for specific services
bool isUserServiceConnected = connectionManager.IsServiceConnected("UserService");
int userServiceRequests = connectionManager.GetRequestCount("UserService");
long userServiceBytesSent = connectionManager.GetBytesSent("UserService");
long userServiceBytesReceived = connectionManager.GetBytesReceived("UserService");
DateTime userServiceLastUsed = connectionManager.GetLastUsedAt("UserService");

// Get all connection metrics for detailed analysis
var allMetrics = connectionManager.GetAllMetrics();
Console.WriteLine($"Total connections: {allMetrics.Count()}");

// Find the most active connection
var mostActive = connectionManager.GetMostActiveConnection();
    if (mostActive != null)
    {
        Console.WriteLine($"Most active: {mostActive.ServiceName} with {mostActive.RequestCount} requests");
    }

// Find the connection with highest throughput
var highestThroughput = connectionManager.GetHighestThroughputConnection();
    if (highestThroughput != null)
    {
        Console.WriteLine($"Highest throughput: {highestThroughput.ServiceName} ({highestThroughput.BytesSent + highestThroughput.BytesReceived} bytes)");
    }

// Get metrics grouped by service
var metricsByService = connectionManager.GetMetricsByService();
    foreach (var kvp in metricsByService)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value.RequestCount} requests, {kvp.Value.BytesSent} bytes sent");
    }

// Get all connection addresses and service names
var addresses = connectionManager.GetAllConnectionAddresses();
var serviceNames = connectionManager.GetAllServiceNames();
```

