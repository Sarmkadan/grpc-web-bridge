# StreamUtility

`StreamUtility` is a static helper class providing asynchronous, chunked, and resilient operations over .NET streams and pipelines. It bridges common impedance mismatches between raw `Stream` I/O and higher-level patterns such as line-by-line reading, compression, retry-on-failure writes, base64 transcoding, hashing, and stream multiplexing via `System.IO.Pipelines`.

## API

### CopyStreamChunkedAsync
```csharp
public static async Task CopyStreamChunkedAsync(
    Stream source,
    Stream destination,
    int bufferSize = 81920,
    CancellationToken cancellationToken = default)
```
Copies all data from `source` to `destination` in configurable chunks. Reads are performed asynchronously; writes are issued immediately after each read. Throws `ArgumentNullException` if either stream is null, `ArgumentException` if `bufferSize` ≤ 0, and `OperationCanceledException` if the token is signaled. Standard `IOException` and `ObjectDisposedException` can propagate from the underlying streams.

### ReadStreamToEndAsync
```csharp
public static async Task<byte[]> ReadStreamToEndAsync(
    Stream stream,
    CancellationToken cancellationToken = default)
```
Reads the entire content of `stream` into a `byte[]`. Uses internal buffering and resizes dynamically. Throws `ArgumentNullException` when `stream` is null. If the stream does not support reading, a `NotSupportedException` is thrown. Cancellation and I/O faults propagate normally.

### ReadLinesAsync
```csharp
public static async IAsyncEnumerable<string> ReadLinesAsync(
    Stream stream,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
```
Returns an asynchronous enumeration of lines decoded from `stream` using UTF-8. Lines are yielded without the trailing line-break characters. The stream is read incrementally; the enumeration completes when the stream is exhausted. Throws `ArgumentNullException` for a null stream. Decoder fallback and I/O errors surface as `DecoderFallbackException` or `IOException` during iteration.

### CreatePipeReader
```csharp
public static PipeReader CreatePipeReader(
    Stream stream,
    PipeOptions? options = null)
```
Wraps a readable `Stream` as a `System.IO.Pipelines.PipeReader`. The returned reader pulls data from the stream on demand. If `options` is null, default `PipeOptions` are used. Throws `ArgumentNullException` when `stream` is null, and `ArgumentException` if the stream is not readable.

### CreatePipeWriter
```csharp
public static PipeWriter CreatePipeWriter(
    Stream stream,
    PipeOptions? options = null)
```
Wraps a writable `Stream` as a `System.IO.Pipelines.PipeWriter`. Flushing the writer advances the underlying stream. If `options` is null, default `PipeOptions` are used. Throws `ArgumentNullException` when `stream` is null, and `ArgumentException` if the stream is not writable.

### CompressStreamAsync
```csharp
public static async Task CompressStreamAsync(
    Stream input,
    Stream output,
    CompressionLevel compressionLevel = CompressionLevel.Optimal,
    CancellationToken cancellationToken = default)
```
Compresses `input` into `output` using the GZip algorithm. Both streams must be non-null and support their respective directions. Throws `ArgumentNullException` for null streams, `ArgumentException` for non-readable/non-writable streams, and propagates `InvalidDataException` on corrupt input data.

### DecompressStreamAsync
```csharp
public static async Task DecompressStreamAsync(
    Stream input,
    Stream output,
    CancellationToken cancellationToken = default)
```
Decompresses a GZip-compressed `input` stream into `output`. Both streams must be non-null and support their respective directions. Throws `ArgumentNullException` for null streams, `ArgumentException` for non-readable/non-writable streams, and `InvalidDataException` if the input is not valid GZip data.

### WriteWithRetryAsync
```csharp
public static async Task WriteWithRetryAsync(
    Stream stream,
    byte[] buffer,
    int offset,
    int count,
    int maxRetries = 3,
    CancellationToken cancellationToken = default)
```
Writes `count` bytes from `buffer` starting at `offset` to `stream`, retrying up to `maxRetries` times on transient write failures. A short delay is inserted between attempts. Throws `ArgumentNullException` for null stream or buffer, `ArgumentOutOfRangeException` for invalid offset/count, and `IOException` if all retries are exhausted. Cancellation is observed between retries.

### SafeSeek
```csharp
public static void SafeSeek(
    Stream stream,
    long offset,
    SeekOrigin origin)
```
Attempts to seek in `stream`. If the stream does not support seeking (`CanSeek` is false), the call is silently ignored. Otherwise, `Stream.Seek` is invoked and any resulting `NotSupportedException` is caught and suppressed. Throws `ArgumentNullException` when `stream` is null.

### GetStreamLength
```csharp
public static long? GetStreamLength(Stream stream)
```
Returns the length of `stream` if `CanSeek` is true and the length property is accessible; otherwise returns `null`. Throws `ArgumentNullException` when `stream` is null.

### IsStreamValid
```csharp
public static bool IsStreamValid(Stream stream)
```
Returns `true` if `stream` is not null, not disposed (checked via a best-effort probe), and readable. Returns `false` for null, disposed, or non-readable streams.

### StreamToBase64Async
```csharp
public static async Task<string> StreamToBase64Async(
    Stream stream,
    CancellationToken cancellationToken = default)
```
Reads the entire `stream` and returns its content as a Base64-encoded string. Uses internal buffering. Throws `ArgumentNullException` for a null stream, and `NotSupportedException` if the stream is not readable.

### Base64ToStream
```csharp
public static Stream Base64ToStream(string base64)
```
Decodes a Base64 string into a new `MemoryStream` containing the original bytes. The returned stream is positioned at the beginning and is readable. Throws `ArgumentNullException` for a null string, and `FormatException` if the input is not valid Base64.

### CalculateStreamHashAsync
```csharp
public static async Task<string> CalculateStreamHashAsync(
    Stream stream,
    HashAlgorithmName algorithmName = HashAlgorithmName.SHA256,
    CancellationToken cancellationToken = default)
```
Computes the hash of `stream` content using the specified algorithm (default SHA-256) and returns the hex-encoded digest string. The stream is read from its current position to the end. Throws `ArgumentNullException` for a null stream, `ArgumentException` for an unsupported algorithm name, and `NotSupportedException` if the stream is not readable.

### TeeStreamAsync
```csharp
public static async Task TeeStreamAsync(
    Stream source,
    Stream destination1,
    Stream destination2,
    int bufferSize = 81920,
    CancellationToken cancellationToken = default)
```
Reads from `source` and writes every chunk simultaneously to both `destination1` and `destination2`. Both destinations receive identical data. Throws `ArgumentNullException` if any stream is null, `ArgumentException` if `bufferSize` ≤ 0 or if any stream does not support its required direction. Write failures on either destination cause the operation to fault.

## Usage

### Resilient upload with retry and integrity verification
```csharp
using var fileStream = File.OpenRead("payload.bin");
if (!StreamUtility.IsStreamValid(fileStream))
    throw new InvalidOperationException("Source stream is invalid.");

// Upload to a network stream with retry logic
await using var networkStream = new MemoryStream(); // proxy for an actual network stream
byte[] buffer = new byte[81920];
int bytesRead;
while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
{
    await StreamUtility.WriteWithRetryAsync(networkStream, buffer, 0, bytesRead, maxRetries: 5);
}

// Verify integrity after upload
fileStream.SafeSeek(0, SeekOrigin.Begin);
networkStream.SafeSeek(0, SeekOrigin.Begin);
string sourceHash = await StreamUtility.CalculateStreamHashAsync(fileStream);
string destHash = await StreamUtility.CalculateStreamHashAsync(networkStream);
Console.WriteLine($"Integrity check: {sourceHash == destHash}");
```

### Line processing with compression and tee output
```csharp
using var source = File.OpenRead("logs.txt");
await using var compressed = File.Create("logs.gz");
await using var tee = new MemoryStream();

// Compress while also capturing a copy for immediate line processing
await StreamUtility.CompressStreamAsync(source, compressed);
source.SafeSeek(0, SeekOrigin.Begin);
await StreamUtility.TeeStreamAsync(source, compressed, tee); // conceptual; in practice use separate paths

// Process lines from the tee'd copy
tee.SafeSeek(0, SeekOrigin.Begin);
await foreach (string line in StreamUtility.ReadLinesAsync(tee))
{
    if (line.Contains("ERROR"))
        Console.WriteLine($"Alert: {line}");
}
```

## Notes

- **Seekable vs. non-seekable streams**: `SafeSeek` and `GetStreamLength` silently degrade for non-seekable streams (e.g., network streams, pipes). Callers should not rely on position or length after operations that consume the stream unless they first verify `CanSeek`.
- **Disposed detection**: `IsStreamValid` uses a best-effort probe that may not detect disposal on all stream implementations. A `false` result is definitive; a `true` result should be treated as optimistic.
- **Thread safety**: All static methods are stateless and operate exclusively on their supplied arguments. They are safe to invoke concurrently provided the underlying stream instances are themselves thread-safe or used with external synchronization. `ReadLinesAsync` and `TeeStreamAsync` internally iterate over a single stream and are not safe for concurrent enumeration of the same stream.
- **Cancellation**: Methods accepting a `CancellationToken` observe it at chunk boundaries. Cancellation may leave streams in a partially consumed state; callers should dispose or reset streams appropriately after an `OperationCanceledException`.
- **Retry semantics**: `WriteWithRetryAsync` retries only on `IOException` during write; it does not retry on argument validation failures or cancellation. The delay between retries is fixed and short.
- **Pipeline wrappers**: `CreatePipeReader` and `CreatePipeWriter` transfer ownership of the underlying stream’s lifetime to the caller. The returned `PipeReader`/`PipeWriter` must be completed (via `Complete` or `CompleteAsync`) to flush and release resources; failing to do so may leave the stream in an indeterminate state.
- **Base64 and hash**: `StreamToBase64Async` and `CalculateStreamHashAsync` consume the stream from its current position to the end. If the stream is seekable, callers should reset the position beforehand if the full content is desired. `Base64ToStream` returns a fresh `MemoryStream` that is independent of the input string.
