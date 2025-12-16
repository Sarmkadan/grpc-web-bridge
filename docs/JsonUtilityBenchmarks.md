# JsonUtilityBenchmarks

Provides a set of benchmark methods for measuring the performance of JSON serialization and deserialization operations. Designed for use with the BenchmarkDotNet framework, this type exposes configurable setup and targeted benchmark cases that exercise common JSON conversion patterns, including serialization to a string, deserialization to a generic object, and deserialization to a dictionary.

## API

### `public void Setup()`

Initializes the internal state required for the benchmark runs. This method is typically invoked once per benchmark iteration (e.g., via the `[GlobalSetup]` attribute in BenchmarkDotNet). It prepares the test data that will be used by the subsequent benchmark methods.

**Parameters:** None.

**Return value:** None.

**Throws:**  
- May throw an `InvalidOperationException` if the setup logic encounters a configuration error or missing dependencies.

---

### `public string Serialize`

Performs JSON serialization of the internal test data and returns the resulting JSON string.

**Parameters:** None.

**Return value:** A `string` containing the serialized JSON representation of the test object.

**Throws:**  
- `InvalidOperationException` if `Setup()` has not been called prior to invocation.  
- `JsonException` if the serialization fails due to circular references, unsupported types, or other serialization errors.

---

### `public object? Deserialize`

Deserializes a pre‑defined JSON string back into a generic `object`. The concrete runtime type of the returned object depends on the structure of the JSON and the deserialization settings used.

**Parameters:** None.

**Return value:** An `object?` representing the deserialized data. Returns `null` if the JSON represents a null value.

**Throws:**  
- `InvalidOperationException` if `Setup()` has not been called.  
- `JsonException` if the JSON string is malformed or cannot be deserialized into the expected target type.

---

### `public Dictionary<string, object>? DeserializeToDictionary`

Deserializes a pre‑defined JSON string into a `Dictionary<string, object?>`. Each JSON property becomes a key in the dictionary, and its value is mapped to a corresponding .NET type (e.g., `string`, `long`, `double`, `bool`, nested `Dictionary<string, object?>` for objects, or `List<object?>` for arrays).

**Parameters:** None.

**Return value:** A `Dictionary<string, object?>?` containing the deserialized key‑value pairs. Returns `null` if the JSON represents a null value.

**Throws:**  
- `InvalidOperationException` if `Setup()` has not been called.  
- `JsonException` if the JSON string is malformed or cannot be deserialized into a dictionary.

## Usage

### Example 1: Basic benchmark configuration

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class JsonBenchmarkHarness
{
    private readonly JsonUtilityBenchmarks _benchmarks = new();

    [GlobalSetup]
    public void GlobalSetup()
    {
        _benchmarks.Setup();
    }

    [Benchmark]
    public string SerializeBenchmark() => _benchmarks.Serialize;

    [Benchmark]
    public object? DeserializeBenchmark() => _benchmarks.Deserialize;

    [Benchmark]
    public Dictionary<string, object>? DeserializeToDictBenchmark() => _benchmarks.DeserializeToDictionary;
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<JsonBenchmarkHarness>();
    }
}
```

### Example 2: Manual invocation for verification

```csharp
var benchmarks = new JsonUtilityBenchmarks();
benchmarks.Setup();

string json = benchmarks.Serialize;
Console.WriteLine($"Serialized JSON: {json}");

object? deserialized = benchmarks.Deserialize;
Console.WriteLine($"Deserialized type: {deserialized?.GetType().Name ?? "null"}");

var dict = benchmarks.DeserializeToDictionary;
if (dict != null)
{
    foreach (var kvp in dict)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value} ({kvp.Value?.GetType().Name ?? "null"})");
    }
}
```

## Notes

- **Setup requirement:** All three benchmark methods (`Serialize`, `Deserialize`, `DeserializeToDictionary`) depend on `Setup()` having been called first. Calling them without prior setup will throw an `InvalidOperationException`.  
- **Thread safety:** This type is **not thread‑safe**. Each instance should be used by a single thread, or external synchronization must be applied. The `Setup()` method modifies internal state that is later read by the benchmark methods; concurrent calls to `Setup()` or interleaved calls between `Setup()` and the benchmark methods will produce undefined behavior.  
- **Null handling:** `Deserialize` and `DeserializeToDictionary` can return `null` when the underlying JSON represents a null value. Callers should check for `null` before accessing members of the returned object or dictionary.  
- **Edge cases:**  
  - If the internal test data contains types that are not natively supported by the JSON serializer (e.g., custom classes without a parameterless constructor), the `Serialize` method may throw a `JsonException`.  
  - `DeserializeToDictionary` will flatten nested objects into nested `Dictionary<string, object?>` instances. Arrays become `List<object?>`. Primitive values are mapped to their closest .NET equivalents (`long` for integers, `double` for floating‑point numbers, `string` for strings, `bool` for booleans).  
  - The exact behavior for duplicate keys, deeply nested structures, or very large JSON payloads is determined by the underlying serializer configuration used during `Setup()`.
