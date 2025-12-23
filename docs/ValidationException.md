# ValidationException

`ValidationException` is a specialized exception type used by the `grpc-web-bridge` library to indicate that a data validation failure has occurred during processing. It provides structured metadata about the failure, including the identifier of the invalid field, the specific value that triggered the validation error, and the name of the violated validation rule, enabling callers to construct precise error responses or diagnostic logs.

## API

### Properties

*   **`string? FieldName`**: Gets the name of the field or property that failed validation. Returns `null` if the field information is not applicable or unavailable.
*   **`object? InvalidValue`**: Gets the value that failed the validation check. Returns `null` if the value is null or cannot be captured.
*   **`string? ValidationRule`**: Gets the description or identifier of the validation rule that was violated.

### Constructors

*   **`ValidationException()`**: Initializes a new instance of the `ValidationException` class with default values.
*   **`ValidationException(string message)`**: Initializes a new instance of the `ValidationException` class with a specified error message.
*   **`ValidationException(string message, string fieldName, object invalidValue, string validationRule)`**: Initializes a new instance of the `ValidationException` class with a specified error message, the name of the invalid field, the invalid value, and the violated validation rule.
*   **`ValidationException(string message, Exception innerException)`**: Initializes a new instance of the `ValidationException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

### Methods

*   **`override string ToString()`**: Returns a string representation of the current exception, including the message, stack trace, and details regarding the invalid field, value, and rule if they are set.

## Usage

### Throwing a Validation Exception

```csharp
public void ValidateUsername(string username)
{
    if (string.IsNullOrWhiteSpace(username))
    {
        throw new ValidationException(
            "Username cannot be empty.",
            nameof(username),
            username,
            "RequiredFieldRule");
    }
}
```

### Catching and Inspecting a Validation Exception

```csharp
try
{
    service.ProcessRequest(request);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed on field '{ex.FieldName}': {ex.Message}");
    Console.WriteLine($"Invalid value: {ex.InvalidValue}");
    Console.WriteLine($"Rule violated: {ex.ValidationRule}");
}
```

## Notes

*   **Nullability**: The `FieldName`, `InvalidValue`, and `ValidationRule` properties are nullable. Consumers should perform null checks before accessing these properties, as they may not be populated depending on the constructor used.
*   **Thread Safety**: `ValidationException` instances are immutable in terms of their validation-specific properties after construction, making them safe to read across multiple threads. However, as with all exceptions, the standard `Exception` properties may be modified by serialization or reflection processes.
*   **Serialization**: If this exception is intended to be serialized across process boundaries, ensure that any custom types assigned to `InvalidValue` are also serializable.
