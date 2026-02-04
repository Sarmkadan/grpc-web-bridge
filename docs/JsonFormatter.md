# JsonFormatter

Provides utilities for serializing .NET objects to JSON, manipulating JSON strings, and validating JSON payloads. The type is designed for use in gRPC‑Web bridge scenarios where deterministic JSON output (sorted keys, controlled depth, null‑value handling) and lightweight JSON transformation are required.

## API

### JsonFormatter()
Creates a new instance with default settings: `PrettyPrint = false`, `SortKeys = false`, `MaxDepth = 20`, `IncludeNullValues = false`.

### string Format<T>(T value)
Serializes an object of type `T` to a JSON string using the instance’s current formatting options.

- **Parameters**  
  - `value`: The object to serialize. If `null`, the method returns `"null"`.
- **Return value**  
  - A JSON‑encoded string representing `value`.
- **Exceptions**  
  - Throws `ArgumentException` if `value` contains a type that cannot be serialized (e.g., unsupported delegate).  
  - Throws `InvalidOperationException` if the object graph exceeds `MaxDepth`.

### string FormatWithSortedKeys<T>(T value)
Serializes an object of type `T` to a JSON string, sorting object property keys alphabetically at each level.

- **Parameters**  
  - `value`: The object to serialize. If `null`, returns `"null"`.
- **Return value**  
  - A JSON string with sorted keys.
- **Exceptions**  
  - Same as `Format<T>`.

### static string Minify(string json)
Removes all insignificant whitespace from a JSON string.

- **Parameters**  
  - `json`: The JSON string to minify. If `null`, throws `ArgumentNullException`.
- **Return value**  
  - A compact JSON string with no spaces, line breaks, or tabs.
- **Exceptions**  
  - Throws `ArgumentNullException` for a null input.  
  - Throws `FormatException` if the input is not valid JSON.

### static string PrettyPrint(string json)
Formats a JSON string with indentation and line breaks for readability.

- **Parameters**  
  - `json`: The JSON string to pretty‑print. If `null`, throws `ArgumentNullException`.
- **Return value**  
  - An indented JSON string.
- **Exceptions**  
  - Throws `ArgumentNullException` for a null input.  
  - Throws `FormatException` if the input is not valid JSON.

### string FormatForDocumentation { get; }
Gets a pre‑configured JSON representation intended for documentation output. The value is produced by calling `Format<T>` on a default instance with `PrettyPrint = true` and `SortKeys = true`.

- **Return value**  
  - A formatted JSON string suitable for inclusion in docs.  
  - The property does not accept parameters; its value reflects the current instance settings at the time of access.

### (bool Valid, List<string> Errors) Validate(string json)
Checks whether a string is valid JSON and collects validation messages.

- **Parameters**  
  - `json`: The JSON string to validate. If `null`, throws `ArgumentNullException`.
- **Return value**  
  - A tuple where `Valid` is `true` if the string is parsable JSON; `Errors` contains a list of descriptive messages when `Valid` is `false`. An empty list is returned when `Valid` is `true`.
- **Exceptions**  
  - Throws `ArgumentNullException` for a null input.

### bool AreEqual(string json1, string json2)
Determines whether two JSON strings represent the same logical JSON value, ignoring whitespace and key order.

- **Parameters**  
  - `json1`: First JSON string.  
  - `json2`: Second JSON string.  
  - If either argument is `null`, throws `ArgumentNullException`.
- **Return value**  
  - `true` if the JSON values are semantically equivalent; otherwise `false`.
- **Exceptions**  
  - Throws `ArgumentNullException` for a null argument.  
  - Throws `FormatException` if either input is not valid JSON.

### Dictionary<string, object?> ExtractFields(string json)
Returns a dictionary of the top‑level properties of a JSON object, with property names as keys and their values as `object?`.

- **Parameters**  
  - `json`: JSON string representing an object. If `null`, throws `ArgumentNullException`. If the JSON does not represent an object, throws `FormatException`.
- **Return value**  
  - A dictionary where each key is a property name and each value is the corresponding JSON value (primitives, nested objects, or arrays) as `object?`. Nested objects and arrays are retained as `JsonElement`‑derived instances (or `null`).
- **Exceptions**  
  - Throws `ArgumentNullException` for a null input.  
  - Throws `FormatException` if the input is not a JSON object.

### Dictionary<string, object?> Flatten(string json)
Converts a nested JSON object into a single‑level dictionary using dot‑notation for nested keys (e.g., `"a.b.c"`).

- **Parameters**  
  - `json`: JSON string representing an object. If `null`, throws `ArgumentNullException`. If the JSON is not an object, throws `FormatException`.
- **Return value**  
  - A dictionary where each key is a dot‑separated path to a leaf value and each value is the leaf’s primitive or `null` value. Arrays are flattened using zero‑based indices (e.g., `"arr.0"`).
- **Exceptions**  
  - Throws `ArgumentNullException` for a null input.  
  - Throws `FormatException` if the input is not a JSON object.  
  - Throws `InvalidOperationException` if flattening would exceed `MaxDepth`.

### Dictionary<string, object?> Unflatten(Dictionary<string, object?> flat)
Reverses the operation of `Flatten`, reconstructing a nested object from a dot‑notation dictionary.

- **Parameters**  
  - `flat`: Dictionary produced by `Flatten`. If `null`, throws `ArgumentNullException`.
- **Return value**  
  - A dictionary representing the reconstructed JSON object, where nested dictionaries correspond to object properties and lists correspond to arrays.
- **Exceptions**  
  - Throws `ArgumentNullException` for a null input.  
  - Throws `ArgumentException` if the dictionary contains malformed keys (e.g., empty segments, consecutive dots) that cannot be interpreted as a valid path.

### bool PrettyPrint { get; set; }
Gets or sets whether the formatter should produce indented JSON output. Default is `false`.

### bool SortKeys { get; set; }
Gets or sets whether object property keys should be sorted alphabetically during serialization. Default is `false`.

### int MaxDepth { get; set; }
Gets or sets the maximum depth allowed for object graphs during serialization and deserialization. Default is `20`. Setting a value less than `1` throws `ArgumentOutOfRangeException`.

### bool IncludeNullValues { get; set; }
Gets or sets whether null property values should be included in the serialized JSON. Default is `false`.

## Usage

```csharp
using GrpcWebBridge.Json; // assuming the namespace

var formatter = new JsonFormatter { PrettyPrint = true, SortKeys = true };
var person = new { Name = "Ada", Age = 30, (string?)Nickname = null };

string json = formatter.Format(person);
// json is:
// {
//   "Age": 30,
//   "Name": "Ada",
//   "Nickname": null
// }

bool valid = formatter.Validate(json).Valid; // true
```

```csharp
string compact = JsonFormatter.Minify(json);
// compact: {"Age":30,"Name":"Ada","Nickname":null}

var flat = JsonFormatter.Flatten(compact);
// flat: { "Age": 30, "Name": "Ada", "Nickname": null }

var restored = JsonFormatter.Unflatten(flat);
// restored contains the same key‑value pairs as the original object
```

## Notes

- Passing `null` to any method that expects a JSON string results in an `ArgumentNullException`.  
- Serialization methods respect the instance’s `MaxDepth`; exceeding this limit throws an `InvalidOperationException`.  
- The `Flatten` and `Unflatten` methods operate only on JSON objects; supplying an array or primitive value throws a `FormatException`.  
- Static methods (`Minify`, `PrettyPrint`, `Validate`, `AreEqual`) are thread‑safe as they rely solely on their inputs.  
- Instance members (`Format`, `FormatWithSortedKeys`, `FormatForDocumentation`, and the configuration properties) are not thread‑safe for concurrent writes; however, reading the configuration properties while another thread is writing may lead to undefined behavior. For concurrent use, either create separate `JsonFormatter` instances per thread or synchronize access to a shared instance.  
- When `IncludeNullValues` is `false`, properties with `null` values are omitted from the output; setting it to `true` ensures they appear as `null` in the JSON string.  
- The `FormatForDocumentation` property reflects the current state of the instance at the moment it is accessed; modifying configuration properties after reading it does not change the returned string.
