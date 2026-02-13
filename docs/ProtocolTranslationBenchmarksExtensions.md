# ProtocolTranslationBenchmarksExtensions

Provides a set of static extension methods used by the benchmarking suite in the **grpc‑web‑bridge** project to facilitate conversion between gRPC protobuf payloads, JSON representations, and HTTP‑compatible formats. These helpers enable realistic scenario setup and validation when measuring the performance of protocol translation logic.

## API

### WithPreconfiguredData
**Purpose:** Creates a new `ProtocolTranslationBenchmarks` instance with default benchmark data pre‑populated.  
**Parameters:** None.  
**Return:** `ProtocolTranslationBenchmarks` – a fully initialized benchmark harness ready for use.  
**Throws:** Does not throw under normal operation; may throw `InvalidOperationException` if internal resources cannot be initialized.

### TranslateAllMetadata
**Purpose:** Converts gRPC metadata into a nested dictionary suitable for HTTP header translation.  
**Parameters:**  
- `Metadata metadata` – the gRPC metadata to translate. Must not be null.  
**Return:** `Dictionary<string, Dictionary<string, string>>` – outer dictionary keyed by header group (e.g., `"request"`, `"response"`); inner dictionaries map header names to their string values.  
**Throws:**  
- `ArgumentNullException` if `metadata` is `null`.  
- `InvalidOperationException` if metadata contains entries that cannot be represented as HTTP headers (e.g., reserved pseudo‑headers such as `:method`).

### ConvertProtobufToJsonString
**Purpose:** Serializes a protobuf byte array to its JSON representation.  
**Parameters:**  
- `byte[] protobufBytes` – the protobuf‑encoded message. Must not be null or empty.  
**Return:** `string` – JSON string representing the same message.  
**Throws:**  
- `ArgumentNullException` if `protobufBytes` is `null`.  
- `ArgumentException` if `protobufBytes` is empty.  
- `InvalidProtocolBufferException` if the bytes do not form a valid protobuf message.  
- `JsonSerializationException` if JSON conversion fails.

### ConvertBase64JsonToProtobuf
**Purpose:** Decodes a Base64‑encoded JSON string back into a protobuf byte array.  
**Parameters:**  
- `string base64Json` – Base64 string containing the JSON representation of a protobuf message. Must not be null.  
**Return:** `byte[]` – protobuf‑encoded message.  
**Throws:**  
- `ArgumentNullException` if `base64Json` is `null`.  
- `FormatException` if the string is not valid Base64.  
- `JsonException` if the decoded JSON is malformed.  
- `InvalidProtocolBufferException` if the resulting JSON cannot be mapped to a protobuf message.

### TranslateGrpcToHttpAuto
**Purpose:** Automatically translates a gRPC payload (protobuf) into the appropriate HTTP wire format used by gRPC‑Web (JSON or Base64‑encoded JSON) based on internal heuristics.  
**Parameters:**  
- `byte[] grpcPayload` – the protobuf‑encoded gRPC message. Must not be null.  
**Return:** `byte[]` – the HTTP‑ready payload (either UTF‑8 JSON or Base64‑encoded JSON) ready to be sent over HTTP.  
**Throws:**  
- `ArgumentNullException` if `grpcPayload` is `null`.  
- `InvalidProtocolBufferException` if the payload is not a valid protobuf message.  
- `NotSupportedException` if the payload type cannot be automatically translated.

### CreateTestResponse
**Purpose:** Generates a sample `GrpcResponse` object populated with realistic data for use in benchmarks.  
**Parameters:** None.  
**Return:** `GrpcResponse` – a response with a status code, optional message, and sample trailers.  
**Throws:** Does not throw; returns a valid instance.

## Usage

### Example 1: Preparing a benchmark harness and translating metadata
```csharp
using Grpc.Core;
using GrpcWebBridge.Benchmarks;

// Create a benchmark configuration with default data.
var bench = ProtocolTranslationBenchmarksExtensions.WithPreconfiguredData();

// Sample gRPC metadata.
var metadata = new Metadata
{
    { "authorization", "Bearer abc123" },
    { "content-type", "application/grpc" }
};

// Translate metadata to the structure expected by the HTTP layer.
var httpMetadata = ProtocolTranslationBenchmarksExtensions.TranslateAllMetadata(metadata);
// httpMetadata can now be used to build HttpRequestHeaders.
```

### Example 2: Converting a protobuf message to JSON and back
```csharp
using GrpcWebBridge.Benchmarks;
using Google.Protobuf;

// Assume we have a protobuf message of type MyRequest.
MyRequest request = new MyRequest { Name = "test", Value = 42 };
byte[] protobuf = request.ToByteArray();

// Protobuf → JSON string.
string json = ProtocolTranslationBenchmarksExtensions.ConvertProtobufToJsonString(protobuf);

// JSON (Base64‑encoded) → Protobuf.
string base64Json = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
byte[] roundTrip = ProtocolTranslationBenchmarksExtensions.ConvertBase64JsonToProtobuf(base64Json);

// Verify round‑trip integrity.
MyRequest roundTripRequest = MyRequest.Parser.ParseFrom(roundTrip);
```

## Notes

- All extension methods are **static** and do not depend on mutable static state; therefore they are thread‑safe as long as the arguments are not mutated concurrently by other threads.  
- Methods that accept `byte[]` or `string` parameters will throw `ArgumentNullException` for null inputs; empty byte arrays are rejected where a meaningful message is required.  
- The metadata translation assumes that header names are valid HTTP field names; reserved gRPC pseudo‑headers (e.g., `:method`, `:path`, `:scheme`, `:authority`) are stripped or cause an `InvalidOperationException`.  
- `ConvertProtobufToJsonString` and `ConvertBase64JsonToProtobuf` use the default JSON formatter; custom options are not exposed via these overloads.  
- `TranslateGrpcToHttpAuto` attempts to auto‑detect whether the gRPC‑Web client expects JSON or Base64‑encoded JSON; if the payload cannot be determined, a `NotSupportedException` is thrown.  
- The `CreateTestResponse` method returns a new instance each call; it is safe to invoke from multiple threads simultaneously.  
- None of the methods allocate long‑lived caches; each call produces new objects, so there is no risk of cross‑call contamination.
