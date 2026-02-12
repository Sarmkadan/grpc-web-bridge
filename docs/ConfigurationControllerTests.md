# ConfigurationControllerTests

Unit tests for the `ConfigurationController` in the `grpc-web-bridge` project, verifying the behavior of configuration retrieval and update endpoints. These tests ensure the controller correctly handles valid and invalid configuration payloads and returns appropriate HTTP status codes.

## API

### `GetConfiguration_ReturnsOk`
Verifies that the `GetConfiguration` endpoint returns an HTTP 200 OK response with a valid configuration payload.

- **Parameters**: None
- **Return Value**: `void` (asserts on HTTP 200 OK)
- **Throws**: No exceptions expected under normal test conditions

### `GetConfiguration_ContainsExpectedProperties`
Ensures the configuration payload returned by `GetConfiguration` includes all expected properties with non-null values.

- **Parameters**: None
- **Return Value**: `void` (asserts on property presence and non-nullity)
- **Throws**: No exceptions expected under normal test conditions

### `UpdateConfiguration_WithValidSettings_ReturnsOk`
Validates that the `UpdateConfiguration` endpoint accepts a valid configuration payload and returns HTTP 200 OK.

- **Parameters**: None (uses a predefined valid configuration object)
- **Return Value**: `void` (asserts on HTTP 200 OK)
- **Throws**: No exceptions expected under normal test conditions

### `UpdateConfiguration_WithNoSettings_ReturnsBadRequest`
Checks that the `UpdateConfiguration` endpoint rejects an empty or null configuration payload and returns HTTP 400 Bad Request.

- **Parameters**: None (uses an empty or null configuration object)
- **Return Value**: `void` (asserts on HTTP 400 Bad Request)
- **Throws**: No exceptions expected under normal test conditions

## Usage

### Example 1: Testing Valid Configuration Update
