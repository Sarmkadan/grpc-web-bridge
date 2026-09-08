# GrpcService Validation & Extensions Documentation

## `GrpcServiceValidation`

### Public Methods
- `Validate(GrpcService? value)`: Validates the service instance and returns a list of human-readable problem strings. Returns an empty list if valid.
- `IsValid(GrpcService? value)`: Returns `true` if the service passes all validation rules, otherwise `false`.
- `EnsureValid(GrpcService? value)`: Throws an `ArgumentException` containing all validation problems if the service is invalid.

### Validation Rules & Error Messages
| Property | Rule | Error Message |
|----------|------|---------------|
| `Id` | Not null/whitespace, length == 32 | `"Service Id cannot be null or whitespace"` / `"Service Id must be a 32-character GUID"` |
| `Name` | Not null/whitespace, length <= 100 | `"Service Name cannot be null or whitespace"` / `"Service Name cannot exceed 100 characters"` |
| `PackageName` | Not null/whitespace, length <= 100, valid format (alphanumeric, dots, dashes; no leading/trailing dots/dashes; no consecutive dots/dashes) | `"Service PackageName cannot be null or whitespace"` / `"Service PackageName cannot exceed 100 characters"` / `"Service PackageName must be a valid .NET package name (alphanumeric with dots)"` |
| `FullName` | Not null/whitespace, length <= 200 | `"Service FullName cannot be null or whitespace"` / `"Service FullName cannot exceed 200 characters"` |
| `Endpoint` | Not null/whitespace, length <= 100, valid IPv4 or hostname | `"Service Endpoint cannot be null or whitespace"` / `"Service Endpoint cannot exceed 100 characters"` / `"Service Endpoint must be a valid hostname or IP address"` |
| `Port` | 1 <= Port <= 65535 | `"Service Port must be between 1 and 65535"` |
| `Status` | Not `ServiceStatus.Unknown` | `"Service Status cannot be Unknown"` |
| `CreatedAt` | Not `default`, not > `DateTime.UtcNow + 5 minutes` | `"Service CreatedAt cannot be default DateTime"` / `"Service CreatedAt cannot be in the future"` |
| `UpdatedAt` | If present: not `default`, not > `DateTime.UtcNow + 5 minutes`, >= `CreatedAt` | `"Service UpdatedAt cannot be default DateTime"` / `"Service UpdatedAt cannot be in the future"` / `"Service UpdatedAt cannot be earlier than CreatedAt"` |
| `Metadata` | Not null, count <= 100, keys not null/empty, key length <= 100, value length <= 1000 | `"Service Metadata dictionary cannot be null"` / `"Service Metadata cannot contain more than 100 entries"` / `"Service Metadata contains an entry with null or empty key"` / `"Service Metadata key cannot exceed 100 characters"` / `"Service Metadata value cannot exceed 1000 characters"` |
| `Methods` | Not null, count > 0 | `"Service Methods collection cannot be null"` / `"Service must have at least one method"` |

### Test Examples
- `Validate_ValidService_DoesNotThrow`: Creates a valid `GrpcService`, adds a `GrpcMethod`, and calls `Validate()`. Asserts no exception is thrown.
- `Validate_EmptyName_ThrowsArgumentException`: Sets `service.Name = ""` and asserts `Validate()` throws `ArgumentException`.
- `Validate_EmptyPackageName_ThrowsArgumentException`: Sets `service.PackageName = ""` and asserts `Validate()` throws `ArgumentException`.
- `Validate_EmptyEndpoint_ThrowsArgumentException`: Sets `service.Endpoint = ""` and asserts `Validate()` throws `ArgumentException`.
- `Validate_InvalidPort_ThrowsArgumentException`: Sets `service.Port = 0` and asserts `Validate()` throws `ArgumentException`.
- `Validate_NoMethods_ThrowsInvalidOperationException`: Creates a service without adding any methods and asserts `Validate()` throws `InvalidOperationException`.

---

## `GrpcServiceExtensions`

### Public Methods
- `GetFullEndpoint(GrpcService service)`: Constructs and returns the full endpoint URL (e.g., `https://example.com:443`) using the service's `UseTls`, `Endpoint`, and `Port` properties.
- `GetMetadataValueOrDefault(GrpcService service, string key, string defaultValue = "")`: Retrieves a metadata value by `key`, returning `defaultValue` if the key is missing.
- `GetAllMetadataKeys(GrpcService service)`: Returns a read-only collection of all metadata keys defined for the service.

### Test Examples
- `SetMetadata_WithValidKeyValue_SetsMetadataAndUpdatesTimestamp`: Demonstrates setting metadata via `SetMetadata`, verifying counts and values, and confirming `UpdatedAt` is refreshed. Validates the underlying dictionary behavior utilized by extension methods.
- `GetMetadata_ExistingKey_ReturnsValue` / `GetMetadata_NonExistentKey_ReturnsNull`: Verify metadata retrieval behavior, which aligns with `GetMetadataValueOrDefault` functionality.
