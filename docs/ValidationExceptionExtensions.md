# ValidationExceptionExtensions

The `ValidationExceptionExtensions` static class provides utility methods for processing and transforming `ValidationException` instances within the `grpc-web-bridge` project. These extensions facilitate the extraction of diagnostic error information, the determination of field-specific validation failures, and the aggregation of multiple validation exceptions into a single, cohesive instance, thereby streamlining error handling in service layers and API controllers.

## API

### ToErrorMessage
Converts the errors contained within a `ValidationException` into a single, human-readable error message string.

*   **Parameters:**
    *   `exception` (this): The source `ValidationException`.
*   **Returns:** A `string` representing the consolidated error message.
*   **Throws:** `ArgumentNullException` if `exception` is null.

### IsForField
Determines whether the `ValidationException` contains errors specifically associated with the specified field name.

*   **Parameters:**
    *   `exception` (this): The source `ValidationException`.
    *   `fieldName`: The name of the field to check.
*   **Returns:** `true` if the exception contains errors for the specified field; otherwise, `false`.
*   **Throws:** `ArgumentNullException` if `exception` or `fieldName` is null.

### ToErrorDetails
Extracts the detailed error information from a `ValidationException` and returns it as a dictionary mapping field names to error details.

*   **Parameters:**
    *   `exception` (this): The source `ValidationException`.
*   **Returns:** A `Dictionary<string, object?>` containing the mapped error details.
*   **Throws:** `ArgumentNullException` if `exception` is null.

### Combine
Aggregates errors from two distinct `ValidationException` instances into a new, unified `ValidationException`.

*   **Parameters:**
    *   `exception` (this): The primary `ValidationException`.
    *   `other`: The `ValidationException` to merge into the primary.
*   **Returns:** A new `ValidationException` containing the combined set of errors.
*   **Throws:** `ArgumentNullException` if `exception` or `other` is null.

## Usage

### Example 1: Consolidating and Extracting Errors for API Response
```csharp
try 
{
    // Assume some service call that throws ValidationException
    _userService.Validate(request);
}
catch (ValidationException ex)
{
    var errorMessage = ex.ToErrorMessage();
    var errorDetails = ex.ToErrorDetails();
    
    // Return structured error response
    return BadRequest(new { Message = errorMessage, Details = errorDetails });
}
```

### Example 2: Merging Validation Results
```csharp
ValidationException? validationEx = null;

try { ValidateHeader(request); }
catch (ValidationException ex) { validationEx = ex; }

try { ValidateBody(request); }
catch (ValidationException ex) 
{ 
    validationEx = validationEx == null ? ex : validationEx.Combine(ex);
}

if (validationEx != null)
{
    throw validationEx;
}
```

## Notes

- **Thread-Safety:** These extension methods are stateless and do not modify the state of the `ValidationException` instances provided, making them thread-safe for read-only access.
- **Null Handling:** All extension methods perform null checks on the `this` parameter and throw an `ArgumentNullException` if it is null. Ensure that exceptions passed to these methods are initialized.
- **Error Mapping:** The structure of the `Dictionary<string, object?>` returned by `ToErrorDetails` is dependent on the internal structure of the `ValidationException`. Ensure consumers are prepared to handle potentially heterogeneous object types within the dictionary values.
