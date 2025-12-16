# StreamProcessingBenchmarks

The `StreamProcessingBenchmarks` class provides a suite of performance benchmarking operations designed to measure the efficiency of various stream processing tasks within the `grpc-web-bridge` project. It focuses on evaluating the performance of reading, copying, and transforming stream data of specific sizes—1KB, 64KB, and 1MB—to ensure optimal data handling in high-throughput scenarios.

## API

### Setup
`public void Setup()`
Prepares the environment required for benchmark execution, such as initializing test streams or pre-populating data buffers. This method should be invoked prior to executing any benchmark operations. It may throw an `InvalidOperationException` if the environment fails to initialize correctly.

### ReadStreamToEnd_1KB, ReadStreamToEnd_64KB, ReadStreamToEnd_1MB
`public async Task<byte[]> ReadStreamToEnd_1KB()`
`public async Task<byte[]> ReadStreamToEnd_64KB()`
`public async Task<byte[]> ReadStreamToEnd_1MB()`
Asynchronously reads the entire content of the corresponding test stream into a byte array. Returns a `Task<byte[]>` containing the stream data. Throws `IOException` if the stream cannot be read.

### CopyStreamChunked_1KB, CopyStreamChunked_64KB, CopyStreamChunked_1MB
`public async Task CopyStreamChunked_1KB()`
`public async Task CopyStreamChunked_64KB()`
`public async Task CopyStreamChunked_1MB()`
Asynchronously performs a chunked copy operation on the corresponding test stream. Returns a `Task` representing the completion of the copy operation. Throws `IOException` if the copy operation fails.

### StreamToBase64_1KB
`public async Task<string> StreamToBase64_1KB()`
Asynchronously reads the 1KB test stream and converts the binary content into a Base64 encoded string. Returns a `Task<string>` containing the Base64 representation of the stream data. Throws `IOException` if the stream cannot be read or conversion fails.

## Usage

### Example 1: Basic Execution
```csharp
var benchmarks = new StreamProcessingBenchmarks();

// Prepare the benchmarking environment
benchmarks.Setup();

// Measure performance for reading a 64KB stream
byte[] data = await benchmarks.ReadStreamToEnd_64KB();
Console.WriteLine($"Read {data.Length} bytes.");
```

### Example 2: Integration in a Benchmark Runner
```csharp
public async Task ExecuteBenchmarkSuite()
{
    var runner = new StreamProcessingBenchmarks();
    runner.Setup();

    // Perform chunked copy for a 1MB stream
    await runner.CopyStreamChunked_1MB();
    
    // Convert 1KB stream to Base64
    string base64Data = await runner.StreamToBase64_1KB();
}
```

## Notes

*   **Thread Safety**: This class is not designed for concurrent usage. A single instance of `StreamProcessingBenchmarks` should generally be used for a linear sequence of operations, or isolated instances should be used for parallel benchmark execution to avoid race conditions during `Setup` or internal state mutation.
*   **Exception Handling**: All asynchronous members are subject to standard `IOException` or `ObjectDisposedException` if the underlying stream resources are inaccessible or closed prematurely.
*   **Initialization**: Ensure `Setup()` is called before executing any benchmark methods. Failure to do so may result in `NullReferenceException` or `InvalidOperationException` depending on the internal implementation of the benchmark.
