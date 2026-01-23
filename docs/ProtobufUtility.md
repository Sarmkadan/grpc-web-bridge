# ProtobufUtility

ProtobufUtility provides a collection of static helper methods for working with Google Protocol Buffers messages in the `grpc-web-bridge` project. It offers JSON and binary serialization, message size calculation, deep cloning, merging, equality checking, validation, compression/decompression, and metadata extraction. The utility also exposes read‑only metadata types (`MessageMetadata` and `FieldMetadata`) that describe the structure of a protobuf message at runtime.

## API

### `public static string ToJson(this IMessage message)`
Serializes the supplied protobuf message to its JSON representation.  
- **Parameters**  
  - `message`: The protobuf message to serialize.  
- **Return value**  
  - A JSON string representing the message.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.  
  - `InvalidOperationException` if the message cannot be serialized to JSON (e.g., unknown field types).

### `public static T? FromJson<T>(string json) where T : IMessage, new`
Deserializes a JSON string into a protobuf message of type `T`.  
- **Parameters**  
  - `json`: The JSON string to deserialize.  
- **Return value**  
  - An instance of `T` populated from the JSON, or `null` if deserialization fails.  
- **Exceptions**  
  - `ArgumentNullException` if `json` is `null`.  
  - `InvalidOperationException` if the JSON is malformed or does not correspond to a valid `T` message.

### `public static byte[] ToBytes(this IMessage message)`
Serializes the supplied protobuf message to a byte array using the protobuf binary format.  
- **Parameters**  
  - `message`: The protobuf message to serialize.  
- **Return value**  
  - A byte array containing the binary representation.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.  
  - `InvalidOperationException` if serialization fails.

### `public static T? FromBytes<T>(byte[] data) where T : IMessage, new`
Deserializes a byte array into a protobuf message of type `T`.  
- **Parameters**  
  - `data`: The binary protobuf data.  
- **Return value**  
  - An instance of `T` populated from the data, or `null` if deserialization fails.  
- **Exceptions**  
  - `ArgumentNullException` if `data` is `null`.  
  - `InvalidOperationException` if the byte array does not contain a valid protobuf message of type `T`.

### `public static int GetMessageSize(this IMessage message)`
Calculates the size in bytes of the protobuf message when serialized.  
- **Parameters**  
  - `message`: The protobuf message to measure.  
- **Return value**  
  - The number of bytes required for the binary representation.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.  
  - `InvalidOperationException` if size calculation fails.

### `public static Dictionary<string, object?> ToDict(this IMessage message)`
Converts the protobuf message into a dictionary where keys are field names and values are the corresponding field values (boxed as `object?`).  
- **Parameters**  
  - `message`: The protobuf message to convert.  
- **Return value**  
  - A dictionary representation of the message.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.  
  - `InvalidOperationException` if conversion encounters an unsupported field type.

### `public static T Clone<T>(T message) where T : IMessage, new`
Creates a deep copy of the supplied protobuf message.  
- **Parameters**  
  - `message`: The message to clone.  
- **Return value**  
  - A new instance of `T` with the same field values as `message`.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.  
  - `InvalidOperationException` if cloning fails.

### `public static T Merge<T>(params T[] messages) where T : IMessage, new`
Merges multiple protobuf messages of the same type into a new instance, applying protobuf merge semantics (later messages overwrite earlier ones for scalar fields; repeated fields are concatenated; messages are merged recursively).  
- **Parameters**  
  - `messages`: One or more messages to merge.  
- **Return value**  
  - A new instance of `T` containing the merged data.  
- **Exceptions**  
  - `ArgumentNullException` if `messages` is `null` or any element is `null`.  
  - `InvalidOperationException` if merging cannot be performed (e.g., type mismatch).

### `public static bool AreEqual<T>(T left, T right) where T : IMessage, new`
Compares two protobuf messages for field‑by‑field equality.  
- **Parameters**  
  - `left`: First message to compare.  
  - `right`: Second message to compare.  
- **Return value**  
  - `true` if both messages have identical field values; otherwise `false`.  
- **Exceptions**  
  - `ArgumentNullException` if either `left` or `right` is `null`.  
  - `InvalidOperationException` if the types differ or comparison fails.

### `public static (bool Valid, List<string> Errors) Validate(this IMessage message)`
Validates the protobuf message against its schema, checking required fields and value constraints.  
- **Parameters**  
  - `message`: The message to validate.  
- **Return value**  
  - A tuple where `Valid` indicates whether the message passed validation and `Errors` contains a list of human‑readable error messages (empty when `Valid` is `true`).  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.  
  - `InvalidOperationException` if validation logic cannot be executed.

### `public static string CompressMessage(this IMessage message)`
Compresses the protobuf message using gzip and returns the result as a Base64‑encoded string.  
- **Parameters**  
  - `message`: The message to compress.  
- **Return value**  
  - Base64 string of the gzipped binary protobuf data.  
- **Exceptions**  
  - `ArgumentNullException` if `message` is `null`.  
  - `InvalidOperationException` if compression fails.

### `public static T? DecompressMessage<T>(string compressedBase64) where T : IMessage, new`
Decompresses a Base64‑encoded gzipped protobuf message and deserializes it into an instance of `T`.  
- **Parameters**  
  - `compressedBase64`: The Base64 string containing the compressed message.  
- **Return value**  
  - An instance of `T` populated from the decompressed data, or `null` if decompression/deserialization fails.  
- **Exceptions**  
  - `ArgumentNullException` if `compressedBase64` is `null` or empty.  
  - `InvalidOperationException` if Base64 decoding, gzip decompression, or protobuf deserialization fails.

### `public static MessageMetadata GetMessageMetadata<T>() where T : IMessage, new`
Retrieves metadata describing the structure of the protobuf type `T`.  
- **Type parameters**  
  - `T`: Any type implementing `Google.Protobuf.IMessage` with a parameterless constructor.  
- **Return value**  
  - A `MessageMetadata` instance containing name, full name, field count, and a list of `FieldMetadata` objects.  
- **Exceptions**  
  - `ArgumentNullException` if `typeof(T)` does not satisfy the `IMessage` constraint.  
  - `InvalidOperationException` if metadata cannot be retrieved for the given type.

### `public string Name` (on `MessageMetadata`)
- **Description**  
  - The simple name of the protobuf message (e.g., `Person`).  
- **Getter**  
  - Returns the name as a `string`.  
- **Thread safety**  
  - The property is immutable after the `MessageMetadata` instance is created; safe for concurrent reads.

### `public string FullName` (on `MessageMetadata`)
- **Description**  
  - The fully qualified name of the protobuf message, including its package (e.g., `my.package.Person`).  
- **Getter**  
  - Returns the full name as a `string`.  
- **Thread safety**  
  - Immutable after construction; safe for concurrent reads.

### `public int FieldCount` (on `MessageMetadata`)
- **Description**  
  - The number of fields defined in the protobuf message.  
- **Getter**  
  - Returns the count as an `int`.  
- **Thread safety**  
  - Immutable after construction; safe for concurrent reads.

### `public List<FieldMetadata> Fields` (on `MessageMetadata`)
- **Description**  
  - A read‑only list describing each field in the message, in field‑number order.  
- **Getter**  
  - Returns a `List<FieldMetadata>`. The list itself is immutable; however, the elements are also immutable.  
- **Thread safety**  
  - Safe for concurrent reads; the list does not change after the `MessageMetadata` instance is created.

### `public string Name` (on `FieldMetadata`)
- **Description**  
  - The name of the field as declared in the `.proto` file (e.g., `id`).  
- **Getter**  
  - Returns the field name as a `string`.  
- **Thread safety**  
  - Immutable after construction; safe for concurrent reads.

### `public string Type` (on `FieldMetadata`)
- **Description**  
  - The protobuf type of the field (e.g., `int32`, `string`, `Person.Address`).  
- **Getter**  
  - Returns the type as a `string`.  
- **Thread safety**  
  - Immutable after construction; safe for concurrent reads.

### `public bool IsRequired` (on `FieldMetadata`)
- **Description**  
  - Indicates whether the field is marked as required in the protobuf schema (proto2 only; always `false` for proto3).  
- **Getter**  
  - Returns `true` if the field is required, otherwise `false`.  
- **Thread safety**  
  - Immutable after construction; safe for concurrent reads.

## Usage

### Example 1: Serializing, cloning, and validating a message
```csharp
using Google.Protobuf;
using GrpcWebBridge; // namespace containing ProtobufUtility

var person = new Person { Id = 123, Name = "Ada Lovelace", Email = "ada@example.com" };

// Convert to JSON string
string json = person.ToJson();
// json now contains: {"id":123,"name":"Ada Lovelace","email":"ada@example.com"}

// Round‑trip back to a protobuf instance
Person? personFromJson = Person.Parser.ParseJson(json);
// Alternatively, using the utility:
// Person? personFromJson = ProtobufUtility.FromJson<Person>(json);

// Create a deep copy
Person clone = ProtobufUtility.Clone(person);

// Validate the cloned message
(var isValid, var errors) = clone.Validate();
if (!isValid)
{
    foreach (var err in errors)
    {
        Console.Error.WriteLine($"Validation error: {err}");
    }
}

// Compare original and clone for equality
bool areEqual = ProtobufUtility.AreEqual(person, clone); // true
```

### Example 2: Compressing a message, decompressing, and inspecting metadata
```csharp
using Google.Protobuf;
using GrpcWebBridge;

var order = new Order { OrderId = 999, Customer = "Acme Corp", Total = 2500 };
byte[] orderBytes = order.ToBase64(); // hypothetical helper; not part of utility

// Compress the message to a Base64 string
string compressed = ProtobufUtility.CompressMessage(order);
// compressed is something like "H4sIAAAAAAAA..."

// Decompress back into an Order instance
Order? decompressed = ProtobufUtility.DecompressMessage<Order>(compressed);
// decompressed.OrderId == 999, etc.

// Retrieve metadata about the Order type
MessageMetadata meta = ProtobufUtility.GetMessageMetadata<Order>();
Console.WriteLine($"Message: {meta.FullName} ({meta.FieldCount} fields)");
foreach (var field in meta.Fields)
{
    Console.WriteLine($"  {field.Name} ({field.Type}) required:{field.IsRequired}");
}
```

## Notes

- All static utility methods are **pure**: they do not modify any internal state and depend only on their input parameters. Consequently, they are safe to call from multiple threads without external synchronization.
- The returned metadata objects (`MessageMetadata` and `FieldMetadata`) are immutable after construction; their properties can be read concurrently.
- Methods that return nullable types (`FromJson<T>`, `FromBytes<T>`, `DecompressMessage<T>`) return `null` when the input cannot be parsed or does not match the expected message type. Callers should check for `null` before using the result.
- Validation (`Validate`) checks only protobuf‑level constraints (required fields, enum ranges, etc.). It does not enforce application‑specific business rules.
- Compression uses gzip; the resulting Base64 string is URL‑safe only if the caller applies additional encoding. The utility does not perform URL‑safe transformations.
- Equality (`AreEqual<T>`) performs a deep field‑by‑field comparison; it respects map and repeated field ordering semantics as defined by protobuf equality.
- The `Merge<T>` method follows protobuf merge semantics: scalar fields from later messages overwrite earlier ones, repeated fields are concatenated, and message‑type fields are merged recursively. Merging is not supported for `oneof` fields where multiple messages set different members; in such cases the last set wins.
- None of the utility methods allocate hidden static caches; each call works with the data provided, making the type suitable for high‑throughput scenarios where allocation overhead is a concern (callers may pool buffers if needed).
