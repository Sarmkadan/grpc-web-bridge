# StreamMessage

A transport-agnostic message container used by the gRPC-Web bridge to carry unary responses, streaming chunks, heartbeats, and error notifications between client and server. The type is immutable once published; mutation helpers (`SetData`, `SetMetadata`, `SetStatus`, `SetHeartbeat`) return a new instance rather than mutating the current one.

## API

### Properties

- **`public string Id`**
  A server-generated or client-generated unique identifier for the message. Used to correlate replies with requests and to deduplicate retransmissions.

- **`public string StreamId`**
  The identifier of the logical bidirectional stream this message belongs to. Empty for unary calls.

- **`public StreamMessageType MessageType`**
  Enumeration (`UnaryResponse`, `StreamChunk`, `Heartbeat`, `Error`) describing the semantic role of the message.

- **`public int SequenceNumber`**
  Monotonically increasing number assigned by the sender. Zero for unary responses; non-zero for streaming chunks and heartbeats. Gaps signal loss.

- **`public byte[] Data`**
  The serialized gRPC message payload. May be compressed if `IsCompressed` is true. Never null; empty array denotes no payload.

- **`public SerializationFormat Format`**
  Indicates the wire format (`Protobuf`, `Json`) used to encode `Data`.

- **`public Dictionary<string, string>? Headers`**
  Optional key/value pairs carried alongside the payload. Used for custom metadata such as tracing IDs or authentication tokens. Null when absent.

- **`public GrpcStatusCode? Status`**
  gRPC status code returned by the server. Present only for final messages (`MessageType == UnaryResponse` or `Error`). Null otherwise.

- **`public string? StatusMessage`**
  Human-readable diagnostic message accompanying `Status`. May be null even when `Status` is present.

- **`public DateTime CreatedAt`**
  Timestamp (UTC) when the message was constructed by the sender.

- **`public bool IsCompressed`**
  True when `Data` is compressed using the algorithm indicated by `CompressionLevel`.

- **`public int? CompressionLevel`**
  Optional compression level (0–9) used if `IsCompressed` is true. Null when compression is disabled.

- **`public GrpcResponse? ErrorResponse`**
  Detailed error envelope when `MessageType` is `Error`. Null otherwise.

### Methods

- **`public StreamMessage()`**
  Constructs an empty message with default values (`Id = Guid.NewGuid().ToString()`, `CreatedAt = DateTime.UtcNow`, etc.).

- **`public StreamMessage(string id, string streamId, StreamMessageType messageType, int sequenceNumber, byte[] data, SerializationFormat format, Dictionary<string, string>? headers, GrpcStatusCode? status, string? statusMessage, DateTime createdAt, bool isCompressed, int? compressionLevel, GrpcResponse? errorResponse)`**
  Fully-specified constructor. All parameters are validated; `data` and `headers` are defensively copied. Throws `ArgumentNullException` for non-nullable parameters that are null.

- **`public StreamMessage SetData(byte[] newData, SerializationFormat newFormat, bool compress = false, int? level = null)`**
  Returns a new `StreamMessage` with updated payload, format, and optional compression settings. `newData` is defensively copied; `compress` and `level` are validated. Throws `ArgumentNullException` if `newData` is null.

- **`public StreamMessage SetMetadata(Dictionary<string, string>? newHeaders)`**
  Returns a new `StreamMessage` with updated headers. `newHeaders` is defensively copied; null clears headers. Never throws.

- **`public StreamMessage SetStatus(GrpcStatusCode code, string? message = null)`**
  Returns a new `StreamMessage` with the given status code and optional message. `message` may be null. Never throws.

- **`public StreamMessage SetHeartbeat()`**
  Returns a new `StreamMessage` of type `Heartbeat` with `SequenceNumber` incremented and `Data` cleared. Never throws.

## Usage
