# StartupConfiguration

`StartupConfiguration` encapsulates the initialization, validation, and runtime metadata for a gRPC-web bridge service instance. It holds environment-specific settings, operational limits such as maximum stream count and message size, and provides a mechanism to asynchronously prepare the instance for use. The type is designed to be created, validated, and then initialized before the bridge begins accepting traffic.

## API

### public StartupConfiguration

Default constructor. Creates a new instance with unset properties and default values for operational limits. The instance must be populated and `ValidateConfiguration` called before `InitializeAsync` is invoked.

### public async Task InitializeAsync

Performs asynchronous startup logic required before the configuration can be considered ready. This may include loading external resources, establishing connections, or warming internal caches. Returns a `Task` that completes when initialization finishes. Throws `InvalidOperationException` if the configuration has not been validated or if required properties are missing. May throw platform-specific exceptions if underlying I/O or network operations fail.

### public void ValidateConfiguration

Synchronously checks that all mandatory properties are set and that operational limit values fall within acceptable ranges. Throws `ArgumentException` or `ArgumentOutOfRangeException` when a property is null, empty, or exceeds defined boundaries. Must be called before `InitializeAsync`.

### public SystemInfo GetSystemInfo

Returns a `SystemInfo` object containing aggregated runtime identification details derived from the current configuration. The returned value includes instance identity, environment, version, and start time. Does not throw.

### public string? InstanceId

Gets or sets a unique identifier for this bridge instance. Null until assigned. Must be non-null and non-empty for validation to succeed.

### public string? InstanceName

Gets or sets a human-readable name for the instance. Null until assigned. Must be non-null and non-empty for validation to succeed.

### public string? Environment

Gets or sets the deployment environment name (e.g., `Production`, `Staging`). Null until assigned. Must be non-null and non-empty for validation to succeed.

### public string? Version

Gets or sets the version string of the running bridge. Null until assigned. Must be non-null and non-empty for validation to succeed.

### public DateTime StartTime

Gets or sets the timestamp marking when the instance began its startup sequence. Defaults to `DateTime.MinValue`. Validation may require this to be set to a reasonable recent value.

### public int MaxStreamCount

Gets or sets the maximum number of concurrent gRPC streams allowed. Defaults to zero. Validation requires a positive value within an implementation-defined upper bound.

### public int MaxMessageSize

Gets or sets the maximum size in bytes of a single gRPC message. Defaults to zero. Validation requires a positive value within an implementation-defined upper bound.

## Usage

### Example 1: Basic setup and initialization

```csharp
var config = new StartupConfiguration
{
    InstanceId = "bridge-01",
    InstanceName = "Primary Bridge",
    Environment = "Production",
    Version = "2.1.0",
    StartTime = DateTime.UtcNow,
    MaxStreamCount = 100,
    MaxMessageSize = 4 * 1024 * 1024 // 4 MB
};

config.ValidateConfiguration();
await config.InitializeAsync();

var sysInfo = config.GetSystemInfo();
Console.WriteLine($"Instance {sysInfo.InstanceId} ready in {sysInfo.Environment}");
```

### Example 2: Conditional validation and error handling

```csharp
var config = new StartupConfiguration();
config.InstanceId = Guid.NewGuid().ToString("N");
config.InstanceName = "Staging Bridge";
config.Environment = "Staging";
config.Version = "2.2.0-preview";
config.StartTime = DateTime.UtcNow;
config.MaxStreamCount = 50;
config.MaxMessageSize = 2 * 1024 * 1024;

try
{
    config.ValidateConfiguration();
    await config.InitializeAsync();
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Configuration invalid: {ex.Message}");
    throw;
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Initialization failed: {ex.Message}");
    throw;
}
```

## Notes

- `ValidateConfiguration` must always be called before `InitializeAsync`; skipping validation causes `InitializeAsync` to throw `InvalidOperationException`.
- All string properties (`InstanceId`, `InstanceName`, `Environment`, `Version`) are null by default and must be explicitly assigned before validation passes.
- `MaxStreamCount` and `MaxMessageSize` default to zero, which is always invalid. Set them to positive values appropriate for the deployment.
- `StartTime` should typically be set to `DateTime.UtcNow` at the moment the instance is created; using local time or very old values may cause validation warnings or failures depending on implementation policy.
- This type is not thread-safe. Property assignment, validation, and initialization should occur on a single thread during startup before the instance is shared across threads.
- Once `InitializeAsync` completes successfully, the configuration is considered immutable for the lifetime of the bridge process. Changing properties after initialization has undefined behavior.
