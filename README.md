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


## Performance Benchmarks

The repository includes a benchmark suite built with [BenchmarkDotNet](https://benchmarkdotnet.org/) to monitor the performance of critical components such as authentication, protocol translation, stream processing, and JSON utilities.

### Running Benchmarks

To run the benchmarks, execute the following commands from the root directory:

```bash
cd benchmarks/grpc-web-bridge.Benchmarks
dotnet run -c Release -- --filter "*"
```

The benchmarks will run a series of tests and output a summary table, including execution time and memory allocation diagnostics.

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

