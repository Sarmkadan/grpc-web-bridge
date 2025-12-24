# BridgeConfiguration
The `BridgeConfiguration` type is used to configure the behavior of a gRPC web bridge, allowing for customization of various settings such as logging, security, and performance. This configuration object provides a centralized way to manage the bridge's settings, making it easier to adapt the bridge to different environments and use cases.

## API
The `BridgeConfiguration` type has the following public members:
* `InstanceId`: a string representing the unique identifier of the bridge instance.
* `InstanceName`: a nullable string representing the name of the bridge instance.
* `Environment`: a string representing the environment in which the bridge is running.
* `EnableLogging`: a boolean indicating whether logging is enabled for the bridge.
* `EnableSwagger`: a boolean indicating whether Swagger support is enabled for the bridge.
* `EnableMetrics`: a boolean indicating whether metrics collection is enabled for the bridge.
* `EnableCors`: a boolean indicating whether CORS support is enabled for the bridge.
* `RequireAuthentication`: a boolean indicating whether authentication is required for the bridge.
* `MaxStreamCount`: an integer representing the maximum number of streams allowed for the bridge.
* `StreamIdleTimeoutSeconds`: an integer representing the idle timeout in seconds for streams.
* `StreamHeartbeatIntervalSeconds`: an integer representing the heartbeat interval in seconds for streams.
* `MaxMessageSize`: an integer representing the maximum size of messages allowed for the bridge.
* `DefaultTimeoutMilliseconds`: an integer representing the default timeout in milliseconds for the bridge.
* `CompressResponses`: a boolean indicating whether responses should be compressed.
* `CompressionLevel`: an integer representing the compression level for responses.
* `AllowedOrigins`: a list of strings representing the allowed origins for CORS requests.
* `AllowedMethods`: a list of strings representing the allowed HTTP methods for the bridge.
* `CustomHeaders`: a dictionary of strings representing custom headers that should be included in responses.
* `ServiceDefaults`: a dictionary of objects representing default settings for services.
* `CreatedAt`: a `DateTime` representing the time at which the bridge configuration was created.

## Usage
Here are two examples of using the `BridgeConfiguration` type:
```csharp
// Example 1: Creating a basic bridge configuration
var config = new BridgeConfiguration
{
    InstanceId = "my-bridge",
    Environment = "dev",
    EnableLogging = true,
    MaxStreamCount = 10,
    DefaultTimeoutMilliseconds = 30000
};

// Example 2: Creating a more advanced bridge configuration with custom headers and CORS settings
var advancedConfig = new BridgeConfiguration
{
    InstanceId = "my-advanced-bridge",
    Environment = "prod",
    EnableSwagger = true,
    EnableCors = true,
    AllowedOrigins = new List<string> { "https://example.com" },
    AllowedMethods = new List<string> { "GET", "POST" },
    CustomHeaders = new Dictionary<string, string> { { "X-Custom-Header", "custom-value" } }
};
```

## Notes
When using the `BridgeConfiguration` type, note that some settings may have implications for security, performance, or compatibility. For example, enabling CORS support may introduce security risks if not properly configured. Additionally, setting the `MaxStreamCount` too low may lead to performance issues under heavy load. The `BridgeConfiguration` type is not thread-safe, so care should be taken to ensure that instances are not shared across multiple threads. It is also important to note that the `CreatedAt` property represents the time at which the configuration was created, and may not reflect any subsequent changes to the configuration.
