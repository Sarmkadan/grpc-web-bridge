# JsonFormatterTests

The `JsonFormatterTests` class contains unit tests that verify the behavior of the `JsonFormatter` utility used in the grpc-web-bridge project to serialize .NET objects to JSON strings. These tests validate correct JSON output, key ordering when sorting is enabled, and proper handling of null values.

## API

### JsonFormatterTests()
Parameterless constructor that creates a new instance of the test class.  
- **Parameters:** none  
- **Return value:** a new `JsonFormatterTests` object  
- **Exceptions:** none

### Format_ShouldReturnValidJson()
Verifies that `JsonFormatter.Format` produces syntactically valid JSON for a typical object.  
- **Parameters:** none  
- **Return value:** void  
- **Exceptions:** throws an exception (e.g., `Xunit.Assert` exception) if the generated string is not valid JSON or does not match the expected output.

### FormatWithSortedKeys_ShouldSortKeys()
Checks that when key sorting is enabled, the serialized JSON string contains object properties in alphabetical order.  
- **Parameters:** none  
- **Return value:** void  
- **Exceptions:** throws an exception if the ordering of keys in the output does not match the expected sorted sequence.

### Format_WithNullObject_ShouldReturnNullString()
Ensures that passing a `null` reference to `JsonFormatter.Format` results in the JSON literal `"null"`.  
- **Parameters:** none  
- **Return value:** void  
- **Exceptions:** throws an exception if the output is not exactly the string `"null"`.

## Usage

### Example 1: Running the tests with xUnit
```csharp
using Xunit;
using GrpcWebBridge.Json; // Adjust namespace as needed

public class JsonFormatterTestsRunner
{
    [Fact]
    public void ExecuteAllFormatterTests()
    {
        var tests = new JsonFormatterTests();
        tests.Format_ShouldReturnValidJson();
        tests.FormatWithSortedKeys_ShouldSortKeys();
        tests.Format_WithNullObject_ShouldReturnNullString();
    }
}
```

### Example 2: Direct invocation in a custom test harness
```csharp
var formatterTests = new JsonFormatterTests();

// Validate normal serialization
formatterTests.Format_ShouldReturnValidJson();

// Validate null handling
formatterTests.Format_WithNullObject_ShouldReturnNullString();
```

## Notes
- The test methods do not modify any shared state; they are stateless and therefore thread‑safe. Multiple threads may invoke them concurrently without risk of interference.  
- These tests focus on the happy path and null case; they do not cover edge cases such as circular references, unsupported types, or custom converters—those scenarios would cause the underlying `JsonFormatter` to throw, which in turn would make the corresponding test fail.  
- If `JsonFormatter.Format` is changed to throw on null input, the `Format_WithNullObject_ShouldReturnNullString` test will begin to fail, indicating a breaking change.  
- The tests assume that the formatter’s output is UTF‑8 encoded and does not include a byte‑order mark. Any deviation in encoding will be caught by the string comparison assertions.
