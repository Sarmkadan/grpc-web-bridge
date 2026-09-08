# GrpcService Validation Documentation

This document describes the validation rules and extension methods for the `GrpcService` class.

## GrpcServiceValidation Class

Provides validation helpers for `GrpcService` instances.

### Public Methods

#### Validate(this GrpcService? value)

Validates a `GrpcService` instance and returns a list of validation problems.

**Rules Checked:**

1. **Required String Properties**
   - `Id`: Cannot be null or whitespace; must be exactly 32 characters (GUID format)
   - `Name`: Cannot be null or whitespace; maximum 100 characters
   - `PackageName`: Cannot be null or whitespace; maximum 100 characters; must be a valid .NET package name (alphanumeric with dots and dashes, cannot start/end with dot/dash, no consecutive dots/dashes)
   - `FullName`: Cannot be null or whitespace; maximum 200 characters
   - `Endpoint`: Cannot be null or whitespace; maximum 100 characters; must be a valid hostname or IP address

2. **Numeric Properties**
   - `Port`: Must be between 1 and 65535

3. **Enum Values**
   - `Status`: Cannot be `ServiceStatus.Unknown`

4. **Date Properties**
   - `CreatedAt`: Cannot be default DateTime; cannot be in the future (more than 5 minutes ahead)
   - `UpdatedAt` (if has value): Cannot be default DateTime; cannot be in the future; cannot be earlier than `CreatedAt`

5. **Collections**
   - `Metadata`: Cannot be null; maximum 100 entries; keys cannot be null/empty or exceed 100 characters; values cannot exceed 1000 characters
   - `Methods`: Cannot be null; must contain at least one method

**Error Messages:**
- "Service Id cannot be null or whitespace"
- "Service Id must be a 32-character GUID"
- "Service Name cannot be null or whitespace"
- "Service Name cannot exceed 100 characters"
- "Service PackageName cannot be null or whitespace"
- "Service PackageName cannot exceed 100 characters"
- "Service PackageName must be a valid .NET package name (alphanumeric with dots)"
- "Service FullName cannot be null or whitespace"
- "Service FullName cannot exceed 200 characters"
- "Service Endpoint cannot be null or whitespace"
- "Service Endpoint cannot exceed 100 characters"
- "Service Endpoint must be a valid hostname or IP address"
- "Service Port must be between 1 and 65535"
- "Service Status cannot be Unknown"
- "Service CreatedAt cannot be default DateTime"
- "Service CreatedAt cannot be in the future"
- "Service UpdatedAt cannot be default DateTime"
- "Service UpdatedAt cannot be in the future"
- "Service UpdatedAt cannot be earlier than CreatedAt"
- "Service Metadata dictionary cannot be null"
- "Service Metadata cannot contain more than 100 entries"
- "Service Metadata contains an entry with null or empty key"
- "Service Metadata key cannot exceed 100 characters"
- "Service Metadata value cannot exceed 1000 characters"
- "Service Methods collection cannot be null"
- "Service must have at least one method"

**Examples from Tests:**
- Valid service: `new GrpcService("TestService", "Test.Package", "localhost", 50051)` with at least one method
- Invalid name: Empty or whitespace name throws validation error
- Invalid package name: Empty or whitespace package name throws validation error
- Invalid endpoint: Empty or whitespace endpoint throws validation error
- Invalid port: Port 0 or 65536 throws validation error
- No methods: Service without methods throws validation error

#### IsValid(this GrpcService? value)

Determines whether a `GrpcService` instance is valid.

**Returns:** True if valid (no validation problems), otherwise false.

**Usage:** 
```csharp
if (service.IsValid())
{
    // Service is valid
}
```

#### EnsureValid(this GrpcService? value)

Ensures that a `GrpcService` instance is valid, throwing an exception if not.

**Exceptions:**
- `ArgumentNullException`: Thrown if `value` is null
- `ArgumentException`: Thrown if `value` is invalid, containing a list of problems formatted as:
  ```
  GrpcService is invalid:
  - [first problem]
  - [second problem]
  - ...
  ```

**Usage:**
```csharp
service.EnsureValid(); // Throws ArgumentException if invalid
```

## GrpcServiceExtensions Class

Extension methods for `GrpcService` providing common helper functionality.

### Public Methods

#### GetFullEndpoint(this GrpcService service)

Gets the full endpoint URL for the service, including the scheme (http/https) and port.

**Returns:** A URL string such as `https://example.com:443`.

**Exceptions:** `ArgumentNullException` if `service` is null.

**Examples from Tests:**
- Service with endpoint "localhost" and port 50051 returns "http://localhost:50051" (when UseTls is false)
- Service with endpoint "example.com" and port 443 with UseTls=true returns "https://example.com:443"

#### GetMetadataValueOrDefault(this GrpcService service, string key, string defaultValue = "")

Retrieves a metadata value by `key`, or returns `defaultValue` when the key is not present.

**Parameters:**
- `key`: The metadata key to look up (cannot be null or empty)
- `defaultValue`: The value to return when the key is missing (defaults to empty string)

**Returns:** The metadata value associated with `key`, or `defaultValue`.

**Exceptions:**
- `ArgumentNullException` if `service` or `key` is null
- `ArgumentException` if `key` is an empty string

**Examples from Tests:**
- After setting metadata "key1" to "value1", calling `GetMetadataValueOrDefault("key1")` returns "value1"
- Calling `GetMetadataValueOrDefault("nonexistent", "default")` returns "default"
- Calling `GetMetadataValueOrDefault("nonexistent")` returns "" (empty string)

#### GetAllMetadataKeys(this GrpcService service)

Returns a read‑only collection of all metadata keys defined for the service.

**Returns:** An `IReadOnlyCollection<string>` containing the metadata keys.

**Exceptions:** `ArgumentNullException` if `service` is null.

**Examples from Tests:**
- After setting metadata with keys "key1" and "key2", calling `GetAllMetadataKeys()` returns a collection containing both keys
- For a service with no metadata, returns an empty collection

## Validation Implementation Details

### Package Name Validation (.NET Package Name Rules)
- Must contain only alphanumeric characters, dots, and dashes
- Cannot start or end with dot or dash
- Cannot contain consecutive dots or dashes

### Endpoint Validation (Hostname or IP Address)
- **IPv4 Address**: Four parts separated by dots, each part 0-255
- **Hostname**: 
  - Each label (between dots) must be 1-63 characters
  - Can contain letters, digits, hyphens
  - Cannot start or end with hyphen
  - Total length cannot exceed 253 characters

### Date Validation
- CreatedAt and UpdatedAt cannot be more than 5 minutes in the future (allows for clock skew)
- UpdatedAt cannot be earlier than CreatedAt

### Metadata Validation
- Maximum 100 entries
- Key maximum length: 100 characters
- Value maximum length: 1000 characters
- Keys cannot be null, empty, or whitespace