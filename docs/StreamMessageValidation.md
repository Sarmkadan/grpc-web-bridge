# StreamMessageValidation

`StreamMessageValidation` provides three public extension methods for checking a [`StreamMessage`](StreamMessage.md): `Validate`, `IsValid`, and `EnsureValid`.

> **Call-site note:** `StreamMessage` also has an instance method named `Validate()` which returns `void` and throws on its smaller set of validation rules. In C#, that instance method takes precedence over an extension method. To obtain the error list described here, call `StreamMessageValidation.Validate(message)` explicitly.

## Validation rules

All three methods use the same rules. A message is invalid when any of the following conditions is true:

| Field or condition | Rule | Validation error |
| --- | --- | --- |
| `Id` | Must not be null, empty, or whitespace. | `Message ID cannot be null or whitespace` |
| `StreamId` | Must not be null, empty, or whitespace. | `Stream ID cannot be null or whitespace` |
| `MessageType` | Must be a defined `StreamMessageType` value. | `Message type '{value}' is not a valid StreamMessageType value` |
| `SequenceNumber` | Must be zero or greater. | `Sequence number cannot be negative` |
| Data message | When `MessageType` is `Data`, `Data` must not be empty. | `Data message must contain non-empty data` |
| `Format` | Must be a defined `SerializationFormat` value. | `Serialization format '{value}' is not a valid SerializationFormat value` |
| Header key | Every key must not be null, empty, or whitespace. Validation stops checking headers after the first invalid entry. | `Header key cannot be null or whitespace` |
| Header value | Every value must be non-null. Validation stops checking headers after the first invalid entry. | `Header value for key '{key}' cannot be null` |
| `Status` | If present, it must be a defined `GrpcStatusCode` value. | `Status code '{value}' is not a valid GrpcStatusCode value` |
| `StatusMessage` | If present, it must not be empty or whitespace. | `Status message cannot be empty or whitespace` |
| `CreatedAt` | Must not be the default `DateTime` value. | `CreatedAt must be set to a non-default DateTime value` |
| Compression level required | When `IsCompressed` is `true`, `CompressionLevel` must be present. | `Compression level must be set when IsCompressed is true` |
| Compression level range | When compressed, the level must be from 0 through 9, inclusive. | `Compression level must be between 0 and 9 inclusive` |
| Data size | `Data.Length` must not exceed `Constants.Streaming.DefaultBufferSize * 2` (currently 16,384 bytes). | `Message data exceeds maximum size of 16384 bytes` |
| Error message | When `MessageType` is `Error`, `ErrorResponse` must be non-null. | `Error message type requires a non-null ErrorResponse` |

Validation accumulates all applicable errors except within header validation, which reports only the first invalid header.

## Methods

### `Validate(StreamMessage? value)`

Returns `IReadOnlyList<string>`. A valid message produces an empty read-only list; an invalid message produces a read-only list containing the applicable messages above.

Throws `ArgumentNullException` when `value` is null. It does not throw merely because a message violates a validation rule.

The tests use a valid data message and also cover whitespace stream IDs, negative sequence numbers, empty data messages, and error messages without an error response. The same cases can be inspected as an error list through this method:

```csharp
var valid = new StreamMessage("test-stream-123", 42, new byte[] { 0x01, 0x02, 0x03, 0x04 });
IReadOnlyList<string> validErrors = StreamMessageValidation.Validate(valid);
// validErrors.Count == 0

var invalid = new StreamMessage
{
    StreamId = "   ",
    SequenceNumber = -1,
    MessageType = StreamMessageType.Data
};

IReadOnlyList<string> errors = StreamMessageValidation.Validate(invalid);
// Includes errors for StreamId, SequenceNumber, and empty Data.
```

### `IsValid(StreamMessage? value)`

Returns `bool`: `true` when validation produces no errors and `false` otherwise. A null value returns `false`; this method does not throw `ArgumentNullException` for null.

```csharp
var valid = new StreamMessage("test-stream-123", 42, new byte[] { 0x01, 0x02, 0x03, 0x04 });
bool validResult = valid.IsValid();
// true

var missingError = new StreamMessage
{
    StreamId = "test-stream-123",
    SequenceNumber = 1,
    MessageType = StreamMessageType.Error
};

bool invalidResult = missingError.IsValid();
// false because ErrorResponse is null

StreamMessage? absent = null;
bool nullResult = absent.IsValid();
// false
```

### `EnsureValid(StreamMessage? value)`

Returns `void`. It returns normally when the message is valid.

Throws:

- `ArgumentNullException` when `value` is null.
- `ArgumentException` when one or more rules fail. The exception message starts with `StreamMessage is invalid:` and lists every collected error on its own line.

This is the closest equivalent to the throwing validation style exercised in `StreamMessageTests`:

```csharp
var valid = new StreamMessage("test-stream-123", 42, new byte[] { 0x01, 0x02, 0x03, 0x04 });
valid.EnsureValid(); // Does not throw.

var invalid = new StreamMessage
{
    StreamId = "test-stream-123",
    SequenceNumber = 1,
    MessageType = StreamMessageType.Error
};

invalid.EnsureValid();
// Throws ArgumentException because ErrorResponse is null.
```

## Choosing a method

- Use `Validate` when a caller needs all human-readable validation errors.
- Use `IsValid` for a boolean guard, including null-safe checks.
- Use `EnsureValid` when invalid input should immediately stop the operation with an exception.
