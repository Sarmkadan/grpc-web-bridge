# GrpcMethod

Represents a single gRPC method definition within the `grpc-web-bridge` infrastructure. It encapsulates the method’s identity, its request/response message types, the gRPC method type (unary, server streaming, client streaming, or bidirectional), metadata such as deprecation status and timeout, and the structured input/output parameters derived from the associated Protobuf message descriptors. The type serves as the canonical in-memory model for a gRPC method when bridging web clients to backend gRPC services.

## API

### Properties

- **`public string Name`**  
  The short name of the gRPC method as declared in the Protobuf service definition (e.g., `"GetUser"`). This is the unqualified method identifier.

- **`public string FullName`**  
  The fully qualified method name, typically including the package and service name (e.g., `"/package.ServiceName/GetUser"`). Used for routing and invocation.

- **`public MethodType Type`**  
  The gRPC method type enumeration value. Indicates whether the method is unary, client streaming, server streaming, or bidirectional streaming.

- **`public string InputMessageType`**  
  The fully qualified Protobuf message type name used as the request (input) message for this method.

- **`public string OutputMessageType`**  
  The fully qualified Protobuf message type name used as the response (output) message for this method.

- **`public bool IsDeprecated`**  
  Indicates whether the method is marked as deprecated in the Protobuf service definition. Consumers should treat deprecated methods with caution.

- **`public string? Description`**  
  An optional human-readable description of the method, typically sourced from Protobuf comments. May be `null` if no description is present.

- **`public int TimeoutMilliseconds`**  
  The default timeout for calls to this method, expressed in milliseconds. A value of `0` may indicate no explicit timeout is configured.

- **`public DateTime CreatedAt`**  
  The timestamp when this `GrpcMethod` instance was created in the bridge’s metadata store. Set at construction time.

- **`public DateTime? UpdatedAt`**  
  The timestamp of the last update to this `GrpcMethod` instance, if any. `null` when the instance has never been modified after creation.

### Constructors

- **`public GrpcMethod()`**  
  Parameterless constructor. Initializes a new, empty `GrpcMethod` instance with default values. `CreatedAt` is set to the current UTC time; `UpdatedAt` is `null`. All string properties are empty or `null`, and `TimeoutMilliseconds` defaults to `0`.

- **`public GrpcMethod(...)`**  
  Parameterized constructor (signature details omitted from the public surface shown, but present). Accepts values for the core identity and type fields and initializes the instance accordingly. `CreatedAt` is set to the current UTC time; `UpdatedAt` remains `null`.

### Methods

- **`public void AddInputParameter(...)`**  
  Adds a parameter definition to the method’s input (request) message descriptor. The exact parameter type is internal to the bridge but corresponds to a field in the Protobuf request message.  
  *Throws:* May throw if a parameter with the same identifier already exists or if the parameter definition is invalid.

- **`public void AddOutputParameter(...)`**  
  Adds a parameter definition to the method’s output (response) message descriptor. Mirrors `AddInputParameter` for the response side.  
  *Throws:* May throw on duplicate identifiers or invalid parameter definitions.

- **`public void RemoveInputParameter(...)`**  
  Removes a previously added input parameter by its identifier. If the parameter does not exist, the call may silently succeed or throw depending on internal strictness.  
  *Throws:* May throw if the specified parameter is not found.

- **`public void Validate()`**  
  Validates the integrity of the `GrpcMethod` instance. Checks that required fields (`Name`, `FullName`, `InputMessageType`, `OutputMessageType`) are populated, the `Type` is a recognized enum value, and the parameter collections are internally consistent.  
  *Throws:* Throws a validation exception (specific type internal to the bridge) if any constraint is violated.

- **`public override string ToString()`**  
  Returns a string representation of the method, typically the `FullName` or a combination of `FullName` and `Type`. Suitable for logging and diagnostics.

- **`public override bool Equals(object? obj)`**  
  Compares this instance to another object for equality. Two `GrpcMethod` instances are considered equal if they have the same `FullName` and `Type`, and their parameter collections are equivalent.  
  *Returns:* `true` if the objects represent the same gRPC method definition; otherwise `false`.

- **`public override int GetHashCode()`**  
  Returns a hash code based on the equality-determining fields (`FullName`, `Type`, and the parameter collections). Consistent with the overridden `Equals` method.

## Usage

### Example 1: Constructing and validating a unary method

```csharp
var method = new GrpcMethod
{
    Name = "GetUser",
    FullName = "/user.UserService/GetUser",
    Type = MethodType.Unary,
    InputMessageType = "user.GetUserRequest",
    OutputMessageType = "user.GetUserResponse",
    TimeoutMilliseconds = 5000,
    Description = "Retrieves a user by their identifier."
};

method.AddInputParameter(/* parameter definition for 'user_id' */);
method.AddOutputParameter(/* parameter definition for 'user' */);

method.Validate(); // throws if any required field is missing

Console.WriteLine(method.ToString()); // "/user.UserService/GetUser"
```

### Example 2: Handling deprecation and equality checks

```csharp
var oldMethod = new GrpcMethod
{
    Name = "ListUsers",
    FullName = "/user.UserService/ListUsers",
    Type = MethodType.ServerStreaming,
    InputMessageType = "user.ListUsersRequest",
    OutputMessageType = "user.User",
    IsDeprecated = true,
    UpdatedAt = DateTime.UtcNow
};

var newMethod = new GrpcMethod
{
    Name = "ListUsersV2",
    FullName = "/user.UserService/ListUsersV2",
    Type = MethodType.ServerStreaming,
    InputMessageType = "user.ListUsersV2Request",
    OutputMessageType = "user.User"
};

bool areSame = oldMethod.Equals(newMethod); // false

if (oldMethod.IsDeprecated)
{
    Console.WriteLine($"Warning: {oldMethod.Name} is deprecated.");
}
```

## Notes

- **Validation timing:** `Validate()` is not called automatically by constructors or property setters. Consumers must invoke it explicitly before using the instance in bridge operations. Failure to validate may lead to runtime errors during method invocation.
- **Parameter management:** `AddInputParameter` and `AddOutputParameter` rely on internal descriptor objects. Duplicate additions or removals of non-existent parameters can throw; the exact exception type depends on the bridge’s internal error handling policy. Always guard additions with existence checks if the parameter source is untrusted.
- **Equality semantics:** `Equals` and `GetHashCode` consider the parameter collections. Two instances with identical `FullName` and `Type` but different parameter sets are not equal. This is important when using `GrpcMethod` as a dictionary key or in hash-based collections.
- **Thread safety:** This type is not inherently thread-safe. Concurrent modifications to properties, parameter collections, or calls to `Validate()` from multiple threads without external synchronization may result in inconsistent state or race conditions. Instances intended for shared use should be treated as immutable after initial configuration and validation.
- **`UpdatedAt` handling:** The `UpdatedAt` property is not automatically set when properties or parameters change. Updating it is the responsibility of the code that mutates the instance. The parameterized constructor leaves `UpdatedAt` as `null`.
- **Timeout interpretation:** A `TimeoutMilliseconds` value of `0` is distinct from an unset timeout; it typically means “no explicit timeout” rather than “immediate timeout.” The bridge layer interprets this according to its own timeout resolution logic.
