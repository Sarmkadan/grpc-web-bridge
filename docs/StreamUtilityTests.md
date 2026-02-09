# StreamUtilityTests

`StreamUtilityTests` is the test suite for the `StreamUtility` helper class in the `grpc-web-bridge` project. It validates the correctness of stream copying, reading, compression, Base64 encoding/decoding, stream duplication (tee), and stream validation logic under normal operation, edge cases, and error conditions.

## API

### `CopyStreamChunkedAsync_WithData_CopiesAllBytes`
Verifies that copying a stream containing data using a chunked approach transfers every byte to the destination. The source and destination streams end up with identical content.

### `CopyStreamChunkedAsync_WithEmptySource_ProducesEmptyDestination`
Ensures that when the source stream is empty, the destination stream remains empty and no bytes are written.

### `CopyStreamChunkedAsync_WithNullSource_ThrowsArgumentNullException`
Confirms that passing a `null` source stream causes an `ArgumentNullException` to be thrown immediately.

### `CopyStreamChunkedAsync_WithNullDestination_ThrowsArgumentNullException`
Confirms that passing a `null` destination stream causes an `ArgumentNullException` to be thrown immediately.

### `CopyStreamChunkedAsync_WithInvalidChunkSize_ThrowsArgumentOutOfRangeException`
Validates that providing a chunk size less than or equal to zero (or otherwise invalid) results in an `ArgumentOutOfRangeException`.

### `ReadStreamToEndAsync_WithData_ReturnsAllBytes`
Tests that reading a stream to completion returns a byte array containing every byte from the source, in order.

### `ReadStreamToEndAsync_WithEmptyStream_ReturnsEmptyArray`
Ensures that reading an empty stream produces a zero-length byte array rather than `null`.

### `ReadStreamToEndAsync_WithNullStream_ThrowsArgumentNullException`
Verifies that a `null` stream argument triggers an `ArgumentNullException`.

### `ReadStreamToEndAsync_WhenExceedsMaxSize_ThrowsInvalidOperationException`
Checks that when the stream content exceeds a configured maximum size limit, the method throws an `InvalidOperationException` to prevent unbounded memory consumption.

### `CompressAndDecompress_RoundTrip_RecoverOriginalData`
End-to-end test confirming that compressing a stream and then decompressing the result yields the original uncompressed data.

### `CompressStreamAsync_WithNullSource_ThrowsArgumentNullException`
Ensures that attempting to compress a `null` source stream throws an `ArgumentNullException`.

### `DecompressStreamAsync_WithNullDestination_ThrowsArgumentNullException`
Ensures that attempting to decompress into a `null` destination stream throws an `ArgumentNullException`.

### `StreamToBase64Async_WithData_ReturnsValidBase64`
Validates that converting a stream to a Base64 string produces a well-formed Base64 output that can be decoded back to the original bytes.

### `Base64ToStream_WithValidBase64_ReturnsStream`
Confirms that a valid Base64 string is correctly decoded into a stream containing the original binary data.

### `Base64ToStream_WithEmptyString_ThrowsArgumentException`
Verifies that an empty string input throws an `ArgumentException`, as there is no data to decode.

### `Base64ToStream_WithInvalidBase64_ThrowsInvalidOperationException`
Ensures that a malformed Base64 string causes an `InvalidOperationException` (or equivalent) during decoding.

### `TeeStreamAsync_WithMultipleDestinations_WritesIdenticalDataToAll`
Tests the tee operation: a single source stream is duplicated to multiple destination streams, and every destination receives the same complete set of bytes.

### `TeeStreamAsync_WithNullSource_ThrowsArgumentNullException`
Confirms that a `null` source stream results in an `ArgumentNullException`.

### `TeeStreamAsync_WithNoDestinations_ThrowsArgumentException`
Ensures that calling the tee method with an empty collection of destinations throws an `ArgumentException`.

### `IsStreamValid_WithReadableStream_ReturnsTrue`
Verifies that a stream that is readable and not disposed is reported as valid (`true`).

## Usage

### Example 1: Copying a stream with chunked reading and verifying the result
```csharp
[TestMethod]
public async Task CopyStreamChunkedAsync_WithData_CopiesAllBytes()
{
    // Arrange
    byte[] originalData = Encoding.UTF8.GetBytes("grpc-web bridge payload");
    using var source = new MemoryStream(originalData);
    using var destination = new MemoryStream();

    // Act
    await StreamUtility.CopyStreamChunkedAsync(source, destination, chunkSize: 4096);

    // Assert
    byte[] copiedData = destination.ToArray();
    CollectionAssert.AreEqual(originalData, copiedData);
}
```

### Example 2: Round-tripping data through compression and decompression
```csharp
[TestMethod]
public async Task CompressAndDecompress_RoundTrip_RecoverOriginalData()
{
    // Arrange
    byte[] payload = new byte[1024 * 64];
    new Random(42).NextBytes(payload);
    using var originalStream = new MemoryStream(payload);
    using var compressedStream = new MemoryStream();
    using var decompressedStream = new MemoryStream();

    // Act
    await StreamUtility.CompressStreamAsync(originalStream, compressedStream);
    compressedStream.Position = 0;
    await StreamUtility.DecompressStreamAsync(compressedStream, decompressedStream);

    // Assert
    byte[] recovered = decompressedStream.ToArray();
    CollectionAssert.AreEqual(payload, recovered);
}
```

## Notes

- All async methods in the tested `StreamUtility` class are expected to properly handle `CancellationToken` propagation even when not explicitly part of the test signatures; cancellation results in `OperationCanceledException`.
- Stream position is reset or managed internally by the utility methods before reading; tests assume the source stream is at position zero unless otherwise arranged.
- The `TeeStreamAsync` method writes to all destinations concurrently; implementations must ensure thread-safe writes to each independent destination and must not allow a fault in one destination to corrupt others.
- `ReadStreamToEndAsync` imposes a configurable maximum size to guard against memory exhaustion; the exact limit is defined by the utility’s configuration, not the test itself.
- Base64 operations use the standard UTF-8 byte-to-string encoding; `Base64ToStream` with invalid characters throws `InvalidOperationException` (or a format-specific exception wrapped accordingly).
- `IsStreamValid` checks readability; streams that are closed, disposed, or `null` return `false`. The test only covers the positive case.
