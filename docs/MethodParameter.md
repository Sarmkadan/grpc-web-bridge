# MethodParameter

Represents metadata for a gRPC-web method parameter, including its name, type, field number, serialization format, and validation rules.

## API

### Properties

#### `Name`
Gets the name of the parameter as defined in the gRPC service definition.
- **Type:** `string`
- **Access:** Public read-only

#### `TypeName`
Gets the fully qualified type name of the parameter (e.g., `"int32"`, `"google.protobuf.Timestamp"`).
- **Type:** `string`
- **Access:** Public read-only

#### `Description`
Gets an optional human-readable description of the parameter.
- **Type:** `string?`
- **Access:** Public read-only

#### `IsRequired`
Gets a value indicating whether the parameter is required in the request.
- **Type:** `bool`
- **Access:** Public read-only

#### `IsRepeated`
Gets a value indicating whether the parameter is a repeated field (e.g., array or list).
- **Type:** `bool`
- **Access:** Public read-only

#### `FieldNumber`
Gets the field number assigned in the protocol buffer schema.
- **Type:** `int`
- **Access:** Public read-only

#### `Format`
Gets the serialization format of the parameter (e.g., `"PROTOBUF"`, `"JSON"`).
- **Type:** `SerializationFormat`
- **Access:** Public read-only

### Constructors

#### `MethodParameter()`
Initializes a new instance with default values.
- **Parameters:** None
- **Remarks:** Sets `Name` to `null`, `TypeName` to `null`, `Description` to `null`, `IsRequired` to `false`, `IsRepeated` to `false`, `FieldNumber` to `0`, and `Format` to `SerializationFormat.Protobuf`.

#### `MethodParameter(string name, string typeName, int fieldNumber, SerializationFormat format)`
Initializes a new instance with the specified values.
- **Parameters:**
  - `name` – The parameter name.
  - `typeName` – The fully qualified type name.
  - `fieldNumber` – The protocol buffer field number.
  - `format` – The serialization format.
- **Throws:** `ArgumentNullException` if `name` or `typeName` is `null`.
- **Throws:** `ArgumentOutOfRangeException` if `fieldNumber` is less than `1`.

### Methods

#### `Validate()`
Validates the parameter metadata for correctness.
- **Parameters:** None
- **Throws:** `InvalidOperationException` if `Name` is `null` or empty, `TypeName` is `null` or empty, or `FieldNumber` is less than `1`.
- **Remarks:** Called automatically during serialization/deserialization to ensure consistency.

#### `ToString()`
Returns a string representation of the parameter.
- **Returns:** A string combining `Name`, `TypeName`, and `FieldNumber`.
- **Example:** `"param (int32, field 42)"`

#### `Equals(object? obj)`
Determines whether the specified object is equal to the current instance.
- **Parameters:**
  - `obj` – The object to compare.
- **Returns:** `true` if all public properties are equal; otherwise, `false`.

#### `GetHashCode()`
Serves as the default hash function.
- **Returns:** A hash code based on all public properties.

## Usage

### Example 1: Defining a Parameter for a gRPC-web Method
