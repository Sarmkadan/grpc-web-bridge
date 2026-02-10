# AuthenticationServiceTests
The `AuthenticationServiceTests` class is designed to test the functionality of the authentication service in the `grpc-web-bridge` project. It provides a set of test methods to verify the correctness of authentication and authorization logic, including API key authentication, custom authentication, context validation, and role authorization.

## API
* `public AuthenticationServiceTests()`: The constructor for the `AuthenticationServiceTests` class.
* `public void AuthenticateApiKey_WithValidCredentials_ReturnsAuthenticatedContext()`: Tests the authentication of an API key with valid credentials. This method does not take any parameters and does not return a value. It is expected to throw no exceptions when valid credentials are provided.
* `public void AuthenticateApiKey_WithEmptyKey_ThrowsGrpcWebBridgeException()`: Tests the authentication of an API key with an empty key. This method does not take any parameters and does not return a value. It is expected to throw a `GrpcWebBridgeException` when an empty key is provided.
* `public void AuthenticateCustom_WithCredentials_AddsClaimsToContext()`: Tests the custom authentication with credentials. This method does not take any parameters and does not return a value. It is expected to add claims to the context when valid credentials are provided.
* `public void AuthenticateCustom_WithEmptyCredentials_ThrowsGrpcWebBridgeException()`: Tests the custom authentication with empty credentials. This method does not take any parameters and does not return a value. It is expected to throw a `GrpcWebBridgeException` when empty credentials are provided.
* `public void ValidateContext_WithNullContext_ReturnsFalse()`: Tests the validation of a null context. This method does not take any parameters and does not return a value. It is expected to return `false` when a null context is provided.
* `public void AuthorizeRole_WithContextHoldingMatchingRole_ReturnsTrue()`: Tests the authorization of a role with a context holding a matching role. This method does not take any parameters and does not return a value. It is expected to return `true` when a matching role is found.
* `public void ExtractBearerToken_WithBearerPrefix_ReturnsRawToken()`: Tests the extraction of a bearer token with a bearer prefix. This method does not take any parameters and does not return a value. It is expected to return the raw token when a bearer prefix is provided.
* `public void ExtractBearerToken_WithNullHeader_ReturnsNull()`: Tests the extraction of a bearer token with a null header. This method does not take any parameters and does not return a value. It is expected to return `null` when a null header is provided.

## Usage
The following examples demonstrate how to use the `AuthenticationServiceTests` class:
```csharp
// Example 1: Testing API key authentication
var authenticationServiceTests = new AuthenticationServiceTests();
authenticationServiceTests.AuthenticateApiKey_WithValidCredentials_ReturnsAuthenticatedContext();
```

```csharp
// Example 2: Testing custom authentication
var authenticationServiceTests = new AuthenticationServiceTests();
authenticationServiceTests.AuthenticateCustom_WithCredentials_AddsClaimsToContext();
```

## Notes
The `AuthenticationServiceTests` class is designed to be thread-safe, as it does not maintain any internal state. However, the test methods may throw exceptions if the authentication or authorization logic fails. It is recommended to handle these exceptions accordingly in the calling code. Additionally, the `GrpcWebBridgeException` is expected to be thrown when invalid credentials or empty keys are provided, and it is recommended to handle this exception to provide a meaningful error message to the user.
