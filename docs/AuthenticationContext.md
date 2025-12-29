# AuthenticationContext

`AuthenticationContext` is a container for authentication-related data in the `grpc-web-bridge` project. It encapsulates identity information, roles, claims, and metadata associated with an authenticated request or user session.

## API

### Properties

#### `public string Id`
A unique identifier for the authentication context. Useful for tracking or correlating authentication events.

#### `public AuthenticationScheme Scheme`
The authentication scheme used to authenticate the request (e.g., "Bearer", "ApiKey").

#### `public string? Token`
The raw authentication token, if available. May be `null` for contexts derived from non-token sources.

#### `public string? UserId`
The identifier of the authenticated user, if available.

#### `public string? Username`
The username of the authenticated user, if available.

#### `public List<string> Roles`
A list of roles assigned to the authenticated entity. Roles are case-sensitive strings.

#### `public Dictionary<string, string> Claims`
A collection of key-value pairs representing claims associated with the authenticated entity. Keys are case-sensitive.

#### `public DateTime? ExpiresAt`
The timestamp at which the authentication context expires, if applicable. May be `null` for contexts without expiration.

#### `public DateTime AuthenticatedAt`
The timestamp when the authentication context was created or last refreshed.

#### `public bool IsAuthenticated`
Indicates whether the context represents an authenticated entity (`true`) or an anonymous/unauthenticated one (`false`).

#### `public string? IpAddress`
The IP address from which the authentication request originated, if available.

#### `public Dictionary<string, object> CustomData`
A dictionary for storing arbitrary, custom data associated with the authentication context. Keys are case-sensitive.

### Constructors

#### `public AuthenticationContext()`
Initializes a new, empty authentication context with default values:
- `Id` is assigned a new GUID.
- `Roles` is initialized as an empty list.
- `Claims` is initialized as an empty dictionary.
- `CustomData` is initialized as an empty dictionary.
- `AuthenticatedAt` is set to the current UTC time.
- `IsAuthenticated` is set to `false`.

#### `public AuthenticationContext(AuthenticationScheme scheme, string? token, string? userId, string? username, DateTime? expiresAt, string? ipAddress)`
Initializes a new authentication context with the provided values:
- `Scheme` is set to the given `scheme`.
- `Token` is set to the given `token`.
- `UserId` is set to the given `userId`.
- `Username` is set to the given `username`.
- `ExpiresAt` is set to the given `expiresAt`.
- `IpAddress` is set to the given `ipAddress`.
- `Id` is assigned a new GUID.
- `Roles` is initialized as an empty list.
- `Claims` is initialized as an empty dictionary.
- `CustomData` is initialized as an empty dictionary.
- `AuthenticatedAt` is set to the current UTC time.
- `IsAuthenticated` is set to `true` if `userId` is non-null; otherwise, `false`.

### Methods

#### `public void AddRole(string role)`
Adds a role to the `Roles` list if it is not already present. The `role` parameter must not be `null` or whitespace; otherwise, the method does nothing.

#### `public bool HasRole(string role)`
Determines whether the context includes the specified role. Returns `true` if the role exists in the `Roles` list; otherwise, `false`. The `role` parameter must not be `null` or whitespace.

#### `public bool HasAnyRole(IEnumerable<string> roles)`
Determines whether the context includes any of the specified roles. Returns `true` if at least one role from the `roles` collection exists in the `Roles` list; otherwise, `false`. The `roles` parameter must not be `null`.

#### `public bool HasAllRoles(IEnumerable<string> roles)`
Determines whether the context includes all of the specified roles. Returns `true` if every role from the `roles` collection exists in the `Roles` list; otherwise, `false`. The `roles` parameter must not be `null`.

#### `public void AddClaim(string type, string value)`
Adds a claim with the specified `type` and `value` to the `Claims` dictionary. If a claim with the same `type` already exists, its value is overwritten. Neither `type` nor `value` may be `null` or whitespace.

#### `public string? GetClaim(string type)`
Retrieves the value of the claim with the specified `type`. Returns `null` if no such claim exists. The `type` parameter must not be `null` or whitespace.

## Usage

### Example 1: Creating and Populating an AuthenticationContext
