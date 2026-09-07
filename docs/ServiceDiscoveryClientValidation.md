# ServiceDiscoveryClientValidation

`ServiceDiscoveryClientValidation` provides extension methods for validating a `ServiceInstance` before it is used with the [ServiceDiscoveryClient](ServiceDiscoveryClient.md). The methods collect validation problems, return a Boolean result, or throw when an instance is invalid.

Add the integration namespace to make the extension methods available:

```csharp
using GrpcWebBridge.Integration;
```

## Validation rules

`ServiceInstance` values are checked according to these rules:

| Property | Rule |
| --- | --- |
| `Id` | Must not be `null`, empty, or whitespace. |
| `Name` | Must not be `null`, empty, or whitespace. |
| `Host` | Must not be `null`, empty, or whitespace. |
| `Port` | Must be from `0` through `65535`, inclusive. The tested boundary values `0` and `65535` are valid. |
| `Status` | Must not be `null`, empty, or whitespace. After trimming whitespace, it must equal `UP`, `DOWN`, `MAINTENANCE`, `OUT_OF_SERVICE`, or `UNKNOWN`, ignoring case. |
| `Metadata` | May be `null` or empty. If supplied, every key and value must be non-empty and non-whitespace. Validation stops after the first invalid metadata entry, so at most one metadata problem is returned. |
| `RegisteredAt` | Must not equal `default(DateTime)`. |
| `LastHeartbeat` | May be `null`. If supplied, it must not equal `default(DateTime)` and must not be more than five minutes ahead of the current UTC time. |

Validation does not stop after an invalid property. It collects problems across the instance, apart from the first-error behavior within `Metadata`.

## Public methods

### `Validate`

```csharp
public static IReadOnlyList<string> Validate(this ServiceInstance value)
```

Returns a read-only list of human-readable problems. The list is empty when the instance satisfies every rule. A `null` instance causes an `ArgumentNullException`.

```csharp
var instance = new ServiceInstance
{
    Id = "",
    Name = "Orders",
    Host = "localhost",
    Port = 70000,
    Status = "READY",
    RegisteredAt = default
};

IReadOnlyList<string> problems = instance.Validate();

// problems contains messages for Id, Port, Status, and RegisteredAt.
foreach (var problem in problems)
{
    Console.WriteLine(problem);
}
```

A valid instance returns no problems. Status matching is case-insensitive and ignores surrounding whitespace:

```csharp
var instance = new ServiceInstance
{
    Id = "orders-1",
    Name = "Orders",
    Host = "localhost",
    Port = 5000,
    Status = "  up  ",
    RegisteredAt = DateTime.UtcNow.AddMinutes(-1),
    LastHeartbeat = DateTime.UtcNow.AddSeconds(-30),
    Metadata = null
};

IReadOnlyList<string> problems = instance.Validate();
// problems.Count == 0
```

### `IsValid`

```csharp
public static bool IsValid(this ServiceInstance value)
```

Returns `true` when `Validate()` returns no problems and `false` otherwise. Because this method delegates to `Validate()`, a `null` instance causes an `ArgumentNullException`.

```csharp
if (!instance.IsValid())
{
    foreach (var problem in instance.Validate())
    {
        Console.WriteLine(problem);
    }
}
```

Use `IsValid` when only a Boolean decision is needed. Use `Validate` when callers need the individual problem messages.

### `EnsureValid`

```csharp
public static void EnsureValid(this ServiceInstance value)
```

Returns normally for a valid instance. It throws:

- `ArgumentNullException` when the instance is `null`.
- `ArgumentException` when validation finds one or more problems. The exception message starts with `ServiceInstance is not valid. Problems:` and includes each collected problem on a separate bulleted line. Its parameter name is `value`.

```csharp
var instance = new ServiceInstance
{
    Id = "orders-1",
    Name = "Orders",
    Host = "localhost",
    Port = 5000,
    Status = "UP",
    RegisteredAt = DateTime.UtcNow.AddMinutes(-1)
};

instance.EnsureValid(); // Does not throw.
```

For invalid input, catch `ArgumentException` only when the caller can recover from invalid registration data:

```csharp
try
{
    instance.EnsureValid();
}
catch (ArgumentException exception)
{
    Console.WriteLine(exception.Message);
}
```

Prefer `Validate` instead of parsing the exception message when individual problems need to be handled programmatically.
