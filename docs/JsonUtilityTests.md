# JsonUtilityTests

Unit tests for JSON serialization, deserialization, merging, and validation utilities used in the gRPC-Web bridge. The test class verifies behavior of `JsonUtility` helper methods with various input scenarios including null values, malformed JSON, nested objects, and dictionary conversions.

## API

### `Serialize_WithSimpleObject_ReturnsCamelCaseJson`
Tests that serializing a simple object produces camelCase JSON property names. The method validates that the output matches the expected camelCase format without altering the original object.

### `Serialize_WithNullObject_ReturnsNullLiteral`
Ensures that serializing a null object returns the JSON literal `null` instead of throwing an exception or returning an empty string.

### `Serialize_WithIndented_ReturnsFormattedJson`
Verifies that enabling indentation produces human-readable, multi-line JSON output with consistent formatting. The test compares the serialized string against a pre-formatted reference.

### `Serialize_WithNullProperty_OmitsNullProperty`
Confirms that properties with null values are omitted from the serialized JSON output when using default serialization settings.

### `Deserialize_WithValidJson_ReturnsMappedObject`
Validates that deserialization of a well-formed JSON string produces an object graph matching the expected structure and data types.

### `Deserialize_WithNullWhitespace_ReturnsDefault`
Checks that deserializing a string containing only whitespace or `null` returns the default value of the target type rather than throwing an exception.

### `Deserialize_WithInvalidJson_ThrowsInvalidOperationException`
Ensures that attempting to deserialize malformed or invalid JSON throws an `InvalidOperationException` with a descriptive message.

### `TryDeserialize_WithValidJson_ReturnsTrueAndResult`
Tests the safe deserialization method that returns a boolean success flag and the deserialized object via an `out` parameter. On valid input, it returns `true` and populates the result.

### `TryDeserialize_WithInvalidJson_ReturnsFalseWithError`
Validates that `TryDeserialize` returns `false` and sets the error message when given invalid JSON input.

### `TryDeserialize_WithEmptyString_ReturnsFalse`
Confirms that an empty string input causes `TryDeserialize` to return `false` without throwing.

### `MergeJson_SourceOverridesTargetProperty`
Tests JSON merging where properties in the source object override those in the target object. Nested objects are merged recursively.

### `MergeJson_WithEmptySource_ReturnsTarget`
Ensures that merging with an empty source JSON object returns the target object unchanged.

### `GetPropertyValue_WithSimplePath_ReturnsValue`
Verifies extraction of a top-level property value using a simple dot-separated path.

### `GetPropertyValue_WithMissingKey_ReturnsNull`
Checks that accessing a non-existent property via `GetPropertyValue` returns `null` instead of throwing.

### `GetPropertyValue_WithNestedPath_ReturnsNestedValue`
Tests retrieval of a value from a nested object using a dot-separated path such as `"parent.child.value"`.

### `ValidateRequired_WithAllRequiredPresent_ReturnsTrue`
Confirms that `ValidateRequired` returns `true` when all required properties are present and non-null in the JSON.

### `ValidateRequired_WithMissingRequiredProperty_ReturnsFalse`
Ensures that `ValidateRequired` returns `false` when any required property is missing from the JSON.

### `ValidateRequired_WithNullOrEmptyJson_ReturnsFalse`
Validates that `ValidateRequired` returns `false` when the input JSON is `null`, empty, or whitespace.

### `DeserializeToDictionary_WithValidJson_ReturnsDict`
Tests conversion of a JSON object into a `Dictionary<string, object>` where keys are property names and values are either primitives or nested dictionaries.

### `DeserializeToDictionary_WithEmptyJson_ReturnsNull`
Ensures that deserializing an empty JSON object (`{}`) returns `null` rather than an empty dictionary.

## Usage
