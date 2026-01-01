# AuthenticationService

The `AuthenticationService` class provides a centralized mechanism for authenticating and authorizing gRPC requests within the `grpc-web-bridge` project. It supports multiple authentication schemes (bearer token, API key, custom logic), caches authentication contexts for performance, and offers methods to validate contexts and check role-based authorization. The service is designed to be used in server-side request pipelines to produce consistent authentication failure responses.

## API

### `public AuthenticationService()`

Initializes a new instance of the `AuthenticationService`.  
**Parameters:** None.  
**Return value:** A new `AuthenticationService` instance.  
**Throws:** None.

### `public AuthenticationContext AuthenticateBearer(string token)`

Authenticates a request using a bearer token.  
**Parameters:**  
- `token` – The bearer token string to validate.  
**Return value:** An `AuthenticationContext` representing the result of authentication.  
**Throws:** `ArgumentNullException` if `token` is `null`.

### `public AuthenticationContext AuthenticateApiKey(string apiKey)`

Authenticates a request using an API key.  
**Parameters:**  
- `apiKey` – The API key string to validate.  
**Return value:** An `AuthenticationContext` representing the result of authentication.  
**Throws:** `ArgumentNullException` if `apiKey` is `null`.

### `public AuthenticationContext AuthenticateCustom(object credentials)`

Authenticates a request using custom credentials.  
**Parameters:**  
- `credentials` – An object containing the custom authentication data.  
**Return value:** An `AuthenticationContext` representing the result of authentication.  
**Throws:** `ArgumentNullException` if `credentials` is `null`.

### `public bool ValidateContext(AuthenticationContext context)`

Validates whether the given authentication context is still valid (e.g., not expired, not revoked).  
**Parameters:**  
- `context` – The `AuthenticationContext` to validate.  
**Return value:** `true` if the context is valid; otherwise `false`.  
**Throws:** `ArgumentNullException` if `context` is `null`.

### `public bool AuthorizeRole(AuthenticationContext context, string role)`

Checks whether the authenticated principal associated with the context has the specified role.  
**Parameters:**  
- `context` – The `AuthenticationContext` to check.  
- `role` – The role name to verify.  
**Return value:** `true` if the principal has the role; otherwise `false`.  
**Throws:** `ArgumentNullException` if `context` or `role` is `null`.

### `public bool AuthorizeAnyRole(AuthenticationContext context, params string[] roles)`

Checks whether the authenticated principal has at least one of the specified roles.  
**Parameters:**  
- `context` – The `AuthenticationContext` to check.  
- `roles` – One or more role names to verify.  
**Return value:** `true` if the principal has any of the given roles; otherwise `false`.  
**Throws:** `ArgumentNullException` if `context` or `roles` is `null`.

### `public AuthenticationContext? GetCachedContext(string key)`

Retrieves a previously cached authentication context by its cache key.  
**Parameters:**  
- `key` – The cache key used when the context was stored.  
**Return value:** The cached `AuthenticationContext`, or `null` if no context is found for the given key.  
**Throws:** `ArgumentNullException` if `key` is `null`.

### `public GrpcResponse CreateAuthFailureResponse(string message)`

Creates a standard gRPC response indicating authentication failure.  
**Parameters:**  
- `message` – A human-readable error message describing the failure.  
**Return value:** A `GrpcResponse` object configured with the appropriate failure status and message.  
**Throws:** `ArgumentNullException` if `message` is `null`.

### `public string? ExtractBearerToken(Metadata headers)`

Extracts a bearer token from the incoming gRPC request metadata.  
**Parameters:**  
- `headers` – The `Metadata` collection from the gRPC request.  
**Return value:** The bearer token string if found; otherwise `null`.  
**Throws:** `ArgumentNullException` if `headers` is `null`.

## Usage

### Example 1: Bearer token authentication with role authorization

```csharp
public GrpcResponse HandleRequest(ServerCallContext context)
{
    var authService = new AuthenticationService();
    string? token = authService.ExtractBearerToken(context.RequestHeaders);
    
    if (token == null)
        return authService.CreateAuthFailureResponse("Missing bearer token.");
    
    var authContext = authService.AuthenticateBearer(token);
    if (!authService.ValidateContext(authContext))
        return authService.CreateAuthFailureResponse("Token expired or invalid.");
    
    if (!authService.AuthorizeRole(authContext, "admin"))
        return authService.CreateAuthFailureResponse("Insufficient permissions.");
    
    // Proceed with authorized request...
    return new GrpcResponse { Status = StatusCode.OK };
}
```

### Example 2: API key authentication with caching

```csharp
public GrpcResponse ProcessApiKeyRequest(string apiKey, ServerCallContext context)
{
    var authService = new AuthenticationService();
    
    // Attempt to retrieve cached context
    var cached = authService.GetCachedContext(apiKey);
    if (cached != null && authService.ValidateContext(cached))
    {
        // Use cached context for authorization
        if (!authService.AuthorizeAnyRole(cached, "reader", "writer"))
            return authService.CreateAuthFailureResponse("Access denied.");
        return new GrpcResponse { Status = StatusCode.OK };
    }
    
    // Authenticate fresh
    var authContext = authService.AuthenticateApiKey(apiKey);
    if (!authService.ValidateContext(authContext))
        return authService.CreateAuthFailureResponse("Invalid API key.");
    
    // Authorization
    if (!authService.AuthorizeAnyRole(authContext, "reader", "writer"))
        return authService.CreateAuthFailureResponse("Access denied.");
    
    return new GrpcResponse { Status = StatusCode.OK };
}
```

## Notes

- **Null handling:** All public methods throw `ArgumentNullException` when required parameters are `null`. Always validate inputs before calling.
- **Thread safety:** The `AuthenticationService` class is not guaranteed to be thread-safe. If shared across multiple concurrent requests, external synchronization (e.g., a lock or dedicated instance per request) should be used.
- **Caching behavior:** `GetCachedContext` returns `null` if no context has been cached for the given key. The caching strategy (e.g., expiration, eviction) is implementation-defined and may vary.
- **Custom authentication:** The `AuthenticateCustom` method accepts an `object` parameter; the concrete type and validation logic depend on the registered custom authentication handler.
- **Authorization methods** (`AuthorizeRole`, `AuthorizeAnyRole`) rely on the roles present in the `AuthenticationContext`. If the context does not contain role information, these methods return `false`.
- **Failure responses:** `CreateAuthFailureResponse` returns a `GrpcResponse` that should be returned directly to the client. The exact status code and message format are determined by the underlying gRPC framework configuration.
