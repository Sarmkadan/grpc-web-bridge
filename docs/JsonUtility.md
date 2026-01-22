# JsonUtility

A utility class for JSON serialization, deserialization, merging, and property manipulation, designed to simplify working with JSON data in C# applications, particularly within the `grpc-web-bridge` project.

## API

### `public static string Serialize<T>(T obj)`
Serializes the given object of type `T` into a JSON string using the default serialization settings.

- **Parameters**:
  - `obj`: The object to serialize.
- **Returns**: A JSON string representation of the object.
- **Throws**:
  - `ArgumentNullException`: If `obj` is `null`.
  - `JsonException`: If serialization fails.

---

### `public static string SerializeWithOptions<T>(T obj, JsonSerializerOptions options)`
Serializes the given object of type `T` into a JSON string using the provided `JsonSerializerOptions`.

- **Parameters**:
  - `obj`: The object to serialize.
  - `options`: The serialization options to apply.
- **Returns**: A JSON string representation of the object.
- **Throws**:
  - `ArgumentNullException`: If `obj` or `options` is `null`.
  - `JsonException`: If serialization fails.

---

### `public static T? Deserialize<T>(string json)`
Deserializes the given JSON string into an object of type `T`.

- **Parameters**:
  - `json`: The JSON string to deserialize.
- **Returns**: The deserialized object of type `T`, or `null` if deserialization fails.
- **Throws**:
  - `ArgumentNullException`: If `json` is `null`.
  - `JsonException`: If deserialization fails.

---
### `public static Dictionary<string, object>? DeserializeToDictionary(string json)`
Deserializes the given JSON string into a dictionary of string keys and object values.

- **Parameters**:
  - `json`: The JSON string to deserialize.
- **Returns**: A dictionary representing the JSON data, or `null` if deserialization fails.
- **Throws**:
  - `ArgumentNullException`: If `json` is `null`.
  - `JsonException`: If deserialization fails.

---
### `public static bool TryDeserialize<T>(string json, out T? result)`
Attempts to deserialize the given JSON string into an object of type `T`.

- **Parameters**:
  - `json`: The JSON string to deserialize.
  - `result`: Output parameter for the deserialized object.
- **Returns**: `true` if deserialization succeeds; otherwise, `false`.
- **Throws**:
  - `ArgumentNullException`: If `json` is `null`.

---
### `public static string MergeJson(string baseJson, string overlayJson)`
Merges two JSON strings by overlaying `overlayJson` onto `baseJson`.

- **Parameters**:
  - `baseJson`: The base JSON string to merge into.
  - `overlayJson`: The JSON string to overlay on top of `baseJson`.
- **Returns**: A merged JSON string.
- **Throws**:
  - `ArgumentNullException`: If `baseJson` or `overlayJson` is `null`.
  - `JsonException`: If merging fails.

---
### `public static object? GetPropertyValue(string json, string propertyPath)`
Retrieves the value of a nested property from the given JSON string.

- **Parameters**:
  - `json`: The JSON string to query.
  - `propertyPath`: Dot-separated path to the property (e.g., `"parent.child"`).
- **Returns**: The value of the property, or `null` if the property does not exist.
- **Throws**:
  - `ArgumentNullException`: If `json` or `propertyPath` is `null`.

---
### `public static string SetPropertyValue(string json, string propertyPath, object value)`
Sets the value of a nested property in the given JSON string and returns the updated JSON.

- **Parameters**:
  - `json`: The JSON string to modify.
  - `propertyPath`: Dot-separated path to the property (e.g., `"parent.child"`).
  - `value`: The value to set.
- **Returns**: The updated JSON string.
- **Throws**:
  - `ArgumentNullException`: If `json`, `propertyPath`, or `value` is `null`.
  - `JsonException`: If the property path is invalid or modification fails.

---
### `public static bool ValidateRequired(string json, string[] requiredProperties)`
Validates that the given JSON string contains all required properties.

- **Parameters**:
  - `json`: The JSON string to validate.
  - `requiredProperties`: An array of dot-separated property paths to check.
- **Returns**: `true` if all required properties exist; otherwise, `false`.
- **Throws**:
  - `ArgumentNullException`: If `json` or `requiredProperties` is `null`.

---
### `public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)`
Overrides the default `JsonConverter` behavior to read a `DateTime` value from a `Utf8JsonReader`.

- **Parameters**:
  - `reader`: The `Utf8JsonReader` to read from.
  - `typeToConvert`: The type to convert to (must be `DateTime`).
  - `options`: The serialization options.
- **Returns**: The `DateTime` value read from the reader.
- **Throws**:
  - `JsonException`: If the value cannot be read as a `DateTime`.

---
### `public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)`
Overrides the default `JsonConverter` behavior to write a `DateTime` value to a `Utf8JsonWriter`.

- **Parameters**:
  - `writer`: The `Utf8JsonWriter` to write to.
  - `value`: The `DateTime` value to write.
  - `options`: The serialization options.
- **Throws**:
  - `ArgumentNullException`: If `writer` is `null`.

## Usage

### Example 1: Basic Serialization and Deserialization
```csharp
using System;
using GrpcWebBridge;

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

var person = new Person { Name = "Alice", Age = 30 };
string json = JsonUtility.Serialize(person);
Console.WriteLine(json); // Output: {"Name":"Alice","Age":30}

Person? deserialized = JsonUtility.Deserialize<Person>(json);
Console.WriteLine(deserialized?.Name); // Output: Alice
```

### Example 2: Merging JSON and Property Manipulation
```csharp
using System;
using GrpcWebBridge;

string baseJson = "{\"name\":\"Alice\",\"age\":30}";
string overlayJson = "{\"age\":31,\"city\":\"New York\"}";

string merged = JsonUtility.MergeJson(baseJson, overlayJson);
Console.WriteLine(merged); // Output: {"name":"Alice","age":31,"city":"New York"}

string updated = JsonUtility.SetPropertyValue(merged, "city", "Boston");
Console.WriteLine(updated); // Output: {"name":"Alice","age":31,"city":"Boston"}

bool isValid = JsonUtility.ValidateRequired(updated, new[] { "name", "age", "city" });
Console.WriteLine(isValid); // Output: True
```

## Notes

- **Thread Safety**: The static methods of `JsonUtility` are thread-safe, as they do not rely on shared mutable state. Each method operates independently on its inputs.
- **Error Handling**: Methods that return `null` (e.g., `Deserialize`, `DeserializeToDictionary`) do not throw exceptions on failure but return `null`. Use `TryDeserialize` for explicit error handling without exceptions.
- **Performance**: Serialization and deserialization operations are relatively expensive. Cache results where possible, especially in performance-critical paths.
- **DateTime Handling**: The overridden `Read` and `Write` methods ensure consistent `DateTime` serialization/deserialization behavior across different environments.
