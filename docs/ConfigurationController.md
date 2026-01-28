# ConfigurationController

The `ConfigurationController` is an ASP.NET Core controller responsible for managing application settings exposed via gRPC-Web. It provides endpoints to retrieve, update, validate, and reset configuration values while ensuring thread-safe access to shared settings.

## API

### `public ConfigurationController`

The default constructor initializes the controller with the application's configuration service and logger.

### `public IActionResult GetConfiguration()`

Retrieves the current application configuration as a dictionary of key-value pairs.

- **Returns**: An `IActionResult` containing a `Dictionary<string, object>` with the current settings. Returns HTTP 200 OK on success.
- **Throws**: `InvalidOperationException` if the configuration service is unavailable.

### `public IActionResult UpdateConfiguration()`

Updates the application configuration with the provided settings.

- **Returns**: An `IActionResult` indicating success (HTTP 200 OK) or failure (HTTP 400 Bad Request with error details).
- **Throws**: `ArgumentNullException` if the input settings are null. `InvalidOperationException` if the update operation fails.

### `public async Task<IActionResult> ValidateConfiguration()`

Validates the current configuration against predefined rules asynchronously.

- **Returns**: A `Task<IActionResult>` resolving to HTTP 200 OK if validation passes, or HTTP 400 Bad Request with validation errors if it fails.
- **Throws**: `InvalidOperationException` if the validation service is unavailable.

### `public IActionResult ResetConfiguration()`

Resets the application configuration to its default values.

- **Returns**: An `IActionResult` indicating success (HTTP 200 OK) or failure (HTTP 500 Internal Server Error).
- **Throws**: `InvalidOperationException` if the reset operation fails.

### `public Dictionary<string, object> Settings`

Gets a read-only snapshot of the current configuration settings.

- **Returns**: A `Dictionary<string, object>` containing the current settings. The dictionary is a snapshot and does not reflect subsequent changes.

## Usage

### Retrieving Configuration

```csharp
var controller = new ConfigurationController(configService, logger);
var result = controller.GetConfiguration();
if (result is OkObjectResult okResult)
{
    var settings = okResult.Value as Dictionary<string, object>;
    Console.WriteLine($"Current settings: {string.Join(", ", settings.Keys)}");
}
```

### Updating Configuration

```csharp
var controller = new ConfigurationController(configService, logger);
var newSettings = new Dictionary<string, object>
{
    { "timeout", 30 },
    { "retries", 3 }
};
var result = controller.UpdateConfiguration(newSettings);
if (result is BadRequestObjectResult badRequest)
{
    Console.WriteLine($"Update failed: {badRequest.Value}");
}
```

## Notes

- The `Settings` property returns a snapshot of the configuration at the time of access. Subsequent modifications to the underlying configuration will not be reflected until the next call to `GetConfiguration()`.
- All public methods are thread-safe due to internal locking mechanisms, but frequent calls to `UpdateConfiguration()` or `ResetConfiguration()` may impact performance under high concurrency.
- Validation in `ValidateConfiguration()` is performed asynchronously but does not block the caller. Long-running validations may delay responses if not handled properly by the caller.
- The controller assumes the configuration service is stateless and thread-safe. If the service is not thread-safe, external synchronization may be required.
