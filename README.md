// existing content ...

## GrpcWebBridgeOptionsTests
The `GrpcWebBridgeOptionsTests` class provides a comprehensive set of unit tests for the `GrpcWebBridgeOptions` configuration builder. It verifies the correct behavior of various configuration methods, including environment settings, development, production, and testing defaults, maximum stream count, allowed origins, and compression settings.

Below is a realistic usage example that demonstrates how to use some of its public members:
```csharp
using GrpcWebBridge.Tests;

// Create an instance of GrpcWebBridgeOptions
var options = new GrpcWebBridgeOptions();

// Set the environment to "Staging"
options = new GrpcWebBridgeOptions("Staging");

// Apply development defaults
options.WithDevelopment();

// Apply production defaults
options.WithProduction();

// Apply testing defaults
options.WithTesting();

// Set the maximum stream count to 500
options.WithMaxStreamCount(500);

// Add allowed origins
options.AddAllowedOrigins("https://example.com", "https://app.example.com");

// Enable compression with level 5
options.WithCompression(true, 5);
```

## TracingServiceTests
The `TracingServiceTests` class provides a comprehensive set of unit tests for the `TracingService` class, ensuring correct behavior in tracing gRPC calls, protocol translation, authentication, and error handling within the gRPC web bridge. These tests validate the creation and configuration of activities, tags, and status codes. Here’s an example of how to use some of its public members:
```csharp
var tests = new TracingServiceTests();
tests.Dispose(); // Ensure proper cleanup

var sut = new TracingServiceTests();
using var activity = sut._sut.StartGrpcCallActivity("UserService", "GetUser");
sut._exported.Should().BeEmpty(); // Verify no activities are exported yet

sut._tracerProvider.ForceFlush();
``` 

## ServiceRepositoryTests
The `ServiceRepositoryTests` class provides a suite of unit tests for the `ServiceRepository` implementation.  
It verifies that services can be added, retrieved, updated, deleted, counted, and queried by package, as well as handling request storage and existence checks.

Below is a realistic usage example that demonstrates how the public test methods can be invoked (e.g., from a custom test runner or interactive session):

```csharp
using System.Threading.Tasks;
using GrpcWebBridge.Tests;

// Instantiate the test class
var repoTests = new ServiceRepositoryTests();

// Add a new service and verify it was stored
await repoTests.AddAsync_WithNewService_ReturnsTrueAndStoresService();

// Attempt to add a duplicate service ID
await repoTests.AddAsync_WithDuplicateServiceId_ReturnsFalse();

// Retrieve a service by its full name
await repoTests.GetByFullNameAsync_WithExistingService_ReturnsService();

// Delete a service
await repoTests.DeleteAsync_WithExistingService_ReturnsTrueAndRemoves();

// Count services
await repoTests.CountAsync_ReturnsCorrectCount();

// Update a service
await repoTests.UpdateAsync_WithExistingService_UpdatesAndReturnsTrue();

// Check existence of a non‑existent service
await repoTests.ExistsAsync_WithNonExistentFullName_ReturnsFalse();

// Add a request
await repoTests.AddRequestAsync_WithValidRequest_ReturnsTrue();

// Get a service by a non‑existent ID
await repoTests.GetByIdAsync_WithNonExistentId_ReturnsNull();

// Get services by package
await repoTests.GetByPackageAsync_ReturnsServicesForPackage();
```

## CacheManagerTests
The `CacheManagerTests` class contains a thorough set of unit tests that verify the behavior of the `CacheManager` caching component, covering basic set/get operations, TTL handling, pattern‑based removal, and statistics tracking. Although intended for automated testing, its public members can be instantiated and called manually to demonstrate cache interactions.

```csharp
using GrpcWebBridge.Tests;

// Create the test fixture (which also creates a CacheManager instance)
var cacheTests = new CacheManagerTests();

// Basic set and get verification
cacheTests.Set_AndTryGet_WithSameKey_ReturnsCachedValue();

// Verify behavior when a key does not exist
cacheTests.TryGet_WithNonExistentKey_ReturnsFalse();

// Clean up resources when done
cacheTests.Dispose();
```

## JsonUtilityTests
The `JsonUtilityTests` class contains unit tests that verify the JSON utility functions used throughout the gRPC web bridge. It checks serialization, deserialization, merging, property extraction, required‑field validation, and dictionary conversion, ensuring correct handling of nulls, formatting, and error conditions.

Example usage (calling the public test methods directly):

```csharp
using GrpcWebBridge.Tests;

// Instantiate the test class
var jsonTests = new JsonUtilityTests();

// Serialization tests
jsonTests.Serialize_WithSimpleObject_ReturnsCamelCaseJson();
jsonTests.Serialize_WithNullObject_ReturnsNullLiteral();
jsonTests.Serialize_WithIndented_ReturnsFormattedJson();
jsonTests.Serialize_WithNullProperty_OmitsNullProperty();

// Deserialization tests
jsonTests.Deserialize_WithValidJson_ReturnsMappedObject();
jsonTests.Deserialize_WithNullWhitespace_ReturnsDefault();
jsonTests.Deserialize_WithInvalidJson_ThrowsInvalidOperationException();

// Try‑deserialize tests
jsonTests.TryDeserialize_WithValidJson_ReturnsTrueAndResult();
jsonTests.TryDeserialize_WithInvalidJson_ReturnsFalseWithError();
jsonTests.TryDeserialize_WithEmptyString_ReturnsFalse();

// Merge tests
jsonTests.MergeJson_SourceOverridesTargetProperty();
jsonTests.MergeJson_WithEmptySource_ReturnsTarget();

// Property value tests
jsonTests.GetPropertyValue_WithSimplePath_ReturnsValue();
jsonTests.GetPropertyValue_WithMissingKey_ReturnsNull();
jsonTests.GetPropertyValue_WithNestedPath_ReturnsNestedValue();

// Validation tests
jsonTests.ValidateRequired_WithAllRequiredPresent_ReturnsTrue();
jsonTests.ValidateRequired_WithMissingRequiredProperty_ReturnsFalse();
jsonTests.ValidateRequired_WithNullOrEmptyJson_ReturnsFalse();

// Dictionary conversion tests
jsonTests.DeserializeToDictionary_WithValidJson_ReturnsDict();
jsonTests.DeserializeToDictionary_WithEmptyJson_ReturnsNull();
```

## GrpcWebBridgeClientExample

The `GrpcWebBridgeClientExample` class provides a comprehensive .NET client for interacting with the gRPC-Web Bridge. It simplifies making gRPC calls through a RESTful interface, handling service registration, health checks, metrics collection, and error recovery with retry logic.

The client exposes public members for monitoring bridge operations, including service status, request statistics, and performance metrics.

Below is a realistic usage example that demonstrates how to instantiate and use the client:

```csharp
using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup dependency injection
var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddHttpClient();

// Add the gRPC-Web Bridge client with configuration
services.AddGrpcWebBridgeClient(
    "http://localhost:5000",  // Bridge URL
    jwtToken: "your-jwt-token-here"); // Optional JWT token

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>()
    .CreateLogger<Program>();

// Resolve the client instance
var bridgeClient = provider.GetRequiredService<GrpcWebBridgeClientExample>();

try
{
    // Example 1: Check bridge health
    var healthy = await bridgeClient.CheckHealthAsync();
    if (!healthy)
    {
        logger.LogError("Bridge is not healthy");
        return;
    }

    // Example 2: List registered services
    var servicesList = await bridgeClient.ListServicesAsync();
    foreach (var service in servicesList ?? new())
    {
        logger.LogInformation("Service: {ServiceName} - Status: {Status}", 
            service.ServiceName, service.Status);
    }

    // Example 3: Register a service dynamically
    await bridgeClient.RegisterServiceAsync(
        "TestService",
        "grpc://localhost:50051",
        enableHealthCheck: true);

    // Example 4: Make an RPC call
    var result = await bridgeClient.CallServiceAsync<object>(
        "TestService",
        "GetData",
        new { id = 42 });

    // Example 5: Get metrics
    var metrics = await bridgeClient.GetMetricsAsync();
    if (metrics != null)
    {
        logger.LogInformation("Bridge Metrics - " +
            "Total: {TotalRequests}, Success: {SuccessfulRequests}, " +
            "Failed: {FailedRequests}, Avg Latency: {AverageLatencyMs}ms",
            metrics.TotalRequests,
            metrics.SuccessfulRequests,
            metrics.FailedRequests,
            metrics.AverageLatencyMs);
    }

    // Example 6: Call with retry logic
    var retryResult = await bridgeClient.CallWithRetryAsync<object>(
        "TestService",
        "GetData",
        new { id = 42 });

    // Example 7: Monitor active streams
    var activeStreams = await bridgeClient.GetActiveStreamCountAsync();
    logger.LogInformation("Active streams: {Count}", activeStreams);
}
catch (Exception ex)
{
    logger.LogError(ex, "Example failed");
}
```
