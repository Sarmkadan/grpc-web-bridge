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

## Configuration

Configure the bridge in `appsettings.json`:

```json
{
  "GrpcWebBridge": {
    "CompressResponses": true
  }
}
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

## MetricsCollectionWorkerExtensions

The `MetricsCollectionWorkerExtensions` class provides extension methods for `MetricsCollectionWorker` that enable advanced metrics analysis, filtering, and reporting capabilities. It includes utilities for filtering snapshots by time range, calculating peak usage statistics, analyzing trends over time windows, and generating alert summaries based on configurable thresholds.

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

