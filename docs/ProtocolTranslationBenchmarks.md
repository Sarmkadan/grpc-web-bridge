# ProtocolTranslationBenchmarks

`ProtocolTranslationBenchmarks` provides a standardized suite for measuring the performance of protocol translation operations within the `grpc-web-bridge` library. It focuses on isolating the overhead of metadata mapping, binary serialization transformations, and gRPC-to-HTTP protocol conversion. This class is designed to be used in conjunction with benchmarking frameworks to quantify latency and throughput under various payload sizes and translation scenarios.

## API

### `public void Setup`
Prepares the internal state and initializes necessary data structures required for executing the benchmark operations.
*   **Parameters:** None.
*   **Returns:** `void`.
*   **Throws:** Throws an `InvalidOperationException` if the setup fails due to environment issues or misconfiguration.

### `public Dictionary<string, string> TranslateMetadata_Small`
Represents the result or input set for benchmarking small-scale metadata translation.
*   **Returns:** A `Dictionary<string, string>` containing a small set of key-value pairs representing translated metadata.

### `public Dictionary<string, string> TranslateMetadata_Large`
Represents the result or input set for benchmarking large-scale metadata translation.
*   **Returns:** A `Dictionary<string, string>` containing a large set of key-value pairs representing translated metadata.

### `public byte[] ConvertProtobufToJson_256B`
Represents the benchmark scenario for converting a 256-byte Protobuf payload to JSON.
*   **Returns:** A `byte[]` containing the resulting JSON data.

### `public byte[] ConvertJsonToProtobuf_Base64`
Represents the benchmark scenario for converting a Base64-encoded JSON payload to Protobuf format.
*   **Returns:** A `byte[]` containing the resulting Protobuf data.

### `public byte[] TranslateGrpcToHttp_Passthrough`
Represents the benchmark scenario for a passthrough translation from gRPC to HTTP.
*   **Returns:** A `byte[]` containing the resulting data after the passthrough operation.

### `public byte[] TranslateGrpcToHttp_Convert`
Represents the benchmark scenario for an active conversion translation from gRPC to HTTP.
*   **Returns:** A `byte[]` containing the resulting data after the active conversion operation.

## Usage

### Example 1: Basic Benchmarking Initialization
```csharp
var benchmarks = new ProtocolTranslationBenchmarks();
// Initialize resources before running benchmark scenarios
benchmarks.Setup();

// Access the translated metadata for small payloads
var metadata = benchmarks.TranslateMetadata_Small;
Console.WriteLine($"Metadata entries: {metadata.Count}");
```

### Example 2: Accessing Benchmark Transformation Results
```csharp
var benchmarks = new ProtocolTranslationBenchmarks();
benchmarks.Setup();

// Retrieve results from the conversion benchmark
byte[] jsonOutput = benchmarks.ConvertProtobufToJson_256B;
byte[] protoOutput = benchmarks.TranslateGrpcToHttp_Convert;

// Use results for further validation or performance analysis
Console.WriteLine($"JSON length: {jsonOutput.Length}");
Console.WriteLine($"Protobuf length: {protoOutput.Length}");
```

## Notes

*   **Thread Safety:** The members of `ProtocolTranslationBenchmarks` are not guaranteed to be thread-safe. Concurrent access to these members should be avoided, especially after calling `Setup`, as they may rely on mutable internal state.
*   **Edge Cases:**
    *   The `Setup` method must be invoked before accessing the benchmark members to ensure data structures are correctly initialized.
    *   If the underlying translation engine is misconfigured, the members might return empty or null-equivalent structures, depending on the implementation.
    *   The returned `byte[]` arrays and `Dictionary` objects should be treated as read-only representations of the benchmark outputs.
