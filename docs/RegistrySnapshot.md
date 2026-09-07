# RegistrySnapshot

The `RegistrySnapshot` class represents a point-in-time snapshot of the service registry, containing the total count of registered services and their registration timestamps.

## Namespace
`GrpcWebBridge.Services`

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `TotalServiceCount` | `int` | The total number of services currently registered in the service registry. |
| `ServiceRegistrationTimestamps` | `Dictionary<string, DateTime>` | A dictionary mapping service full names (e.g., `package.ServiceName`) to their UTC registration timestamps. |

## Methods

### `ToJson()`
Returns a JSON representation of the snapshot with indentation for readability.

**Returns**
- `string`: JSON string representing the snapshot.

**Example**
```csharp
var snapshot = serviceRegistry.GetRegistrySnapshot();
string json = snapshot.ToJson();
```

### `ToString()`
Returns a human-readable string representation of the snapshot.

**Returns**
- `string`: Formatted string showing the total service count and registration timestamps.

**Example**
```csharp
var snapshot = serviceRegistry.GetRegistrySnapshot();
string description = snapshot.ToString();
// Output: RegistrySnapshot { TotalServiceCount = 2, ServiceRegistrationTimestamps = { [grpc.example.ServiceA: 2026-09-07T10:00:00Z], [grpc.example.ServiceB: 2026-09-07T10:05:00Z] } }
```

## ServiceRegistry Methods

### `GetRegistrySnapshot()`
Creates and returns a new `RegistrySnapshot` instance containing the current state of the service registry.

**Returns**
- `RegistrySnapshot`: A snapshot of the service registry.

**Thread Safety**
This method is thread-safe and uses a lock on the internal services dictionary.

### `GetRegistrySnapshotJson()`
Returns the registry snapshot as a formatted JSON string.

**Returns**
- `string`: JSON representation of the registry snapshot.

**Example**
```csharp
string json = serviceRegistry.GetRegistrySnapshotJson();
```

## Sample JSON

The following is an example of the JSON produced by `GetRegistrySnapshotJson()` or `RegistrySnapshot.ToJson()` when two services are registered:

```json
{
  "TotalServiceCount": 2,
  "ServiceRegistrationTimestamps": {
    "grpc.example.ServiceA": "2026-09-07T10:00:00Z",
    "grpc.example.ServiceB": "2026-09-07T10:05:00Z"
  }
}
```

Note: The actual timestamps will reflect the UTC time when each service was registered.