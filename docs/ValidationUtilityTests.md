# ValidationUtilityTests

The `ValidationUtilityTests` class serves as the dedicated test suite for verifying the correctness and robustness of input validation and sanitization logic within the `grpc-web-bridge` project. It encapsulates a series of unit tests designed to assert that email formats, gRPC method names, and service identifiers adhere to strict specification requirements, while also ensuring that user inputs containing potentially dangerous characters, such as HTML tags, are properly sanitized to prevent injection vulnerabilities.

## API

### `ValidateEmail_WithValidFormat_ReturnsValid`
Verifies that the validation logic correctly identifies and accepts email addresses that conform to standard formatting rules.
*   **Purpose**: To assert that a well-formed email string passes the validation check without errors.
*   **Parameters**: None (inputs are defined internally within the test context).
*   **Return Value**: `void` (success is indicated by the absence of an assertion failure).
*   **Throws**: Throws an assertion exception if the valid email format is incorrectly rejected by the underlying utility.

### `ValidateEmail_WithMissingDomain_ReturnsInvalid`
Ensures that email addresses lacking a domain component are correctly flagged as invalid.
*   **Purpose**: To confirm that the validation logic rejects malformed email strings where the domain part is absent.
*   **Parameters**: None (inputs are defined internally within the test context).
*   **Return Value**: `void` (success is indicated by the absence of an assertion failure).
*   **Throws**: Throws an assertion exception if the missing domain case is incorrectly accepted or fails to trigger the expected invalid result.

### `ValidateMethodName_StartingWithDigit_ReturnsInvalid`
Validates that gRPC method names adhering to naming conventions do not begin with numeric digits.
*   **Purpose**: To enforce the rule that method names must start with a letter, rejecting those starting with a digit.
*   **Parameters**: None (inputs are defined internally within the test context).
*   **Return Value**: `void` (success is indicated by the absence of an assertion failure).
*   **Throws**: Throws an assertion exception if a method name starting with a digit is incorrectly deemed valid.

### `ValidateServiceId_WithDotsAndHyphens_ReturnsValid`
Confirms that service identifiers containing allowed special characters, specifically dots (`.`) and hyphens (`-`), are accepted.
*   **Purpose**: To verify that the service ID validation logic permits standard separators used in hierarchical naming schemes.
*   **Parameters**: None (inputs are defined internally within the test context).
*   **Return Value**: `void` (success is indicated by the absence of an assertion failure).
*   **Throws**: Throws an assertion exception if a service ID containing dots and hyphens is incorrectly rejected.

### `SanitizeInput_WithHtmlTags_EscapesAllSpecialCharacters`
Tests the input sanitization routine to ensure HTML tags and special characters are properly escaped.
*   **Purpose**: To guarantee that raw input containing HTML markup is neutralized by escaping special characters, mitigating cross-site scripting (XSS) risks.
*   **Parameters**: None (inputs are defined internally within the test context).
*   **Return Value**: `void` (success is indicated by the absence of an assertion failure).
*   **Throws**: Throws an assertion exception if the output string retains unescaped HTML tags or special characters.

## Usage

The following examples demonstrate how the scenarios covered by `ValidationUtilityTests` correspond to actual usage of the underlying `ValidationUtility` class in a production context.

**Example 1: Validating gRPC Service and Method Names**
This example illustrates the validation of a service ID containing hyphens and a method name, ensuring compliance with the rules tested in `ValidateServiceId_WithDotsAndHyphens_ReturnsValid` and `ValidateMethodName_StartingWithDigit_ReturnsInvalid`.

```csharp
using GrpcWebBridge.Utilities;

public class RpcRequestHandler
{
    public void ProcessRequest(string serviceId, string methodName)
    {
        // Validates service ID allows dots and hyphens
        if (!ValidationUtility.IsValidServiceId(serviceId))
        {
            throw new ArgumentException("Invalid service identifier format.");
        }

        // Ensures method name does not start with a digit
        if (!ValidationUtility.IsValidMethodName(methodName))
        {
            throw new ArgumentException("Method name must start with a letter.");
        }

        // Proceed with gRPC invocation
        InvokeGrpcMethod(serviceId, methodName);
    }

    private void InvokeGrpcMethod(string service, string method) { /* Implementation */ }
}
```

**Example 2: Sanitizing User Input for Logging**
This example demonstrates the sanitization logic verified by `SanitizeInput_WithHtmlTags_EscapesAllSpecialCharacters`, ensuring that user-provided data is safe before being rendered or logged.

```csharp
using GrpcWebBridge.Utilities;

public class AuditLogger
{
    public void LogUserAction(string userInput)
    {
        // Escapes HTML tags to prevent injection in logs or UI
        string safeInput = ValidationUtility.SanitizeInput(userInput);
        
        // Safe to write to output
        Console.WriteLine($"User action recorded: {safeInput}");
    }
}
```

## Notes

*   **Edge Cases**: The test suite specifically targets boundary conditions such as emails with missing domains and method names initiating with numeric characters. Implementations relying on these utilities should anticipate that strings containing only whitespace, empty strings, or null values (if not handled internally) may also result in validation failures, though specific tests for those cases are not explicitly listed in this class.
*   **Thread Safety**: As `ValidationUtilityTests` consists of stateless test methods verifying pure functions or static utilities, the underlying validation logic is expected to be thread-safe. No shared mutable state is initialized or modified within the test methods themselves, allowing tests to be executed in parallel without risk of race conditions.
*   **Assertion Behavior**: All methods return `void`. Failure conditions are not returned as boolean values but are instead signaled by the testing framework throwing an assertion exception. This implies that the test runner must catch these exceptions to report failures accurately.
