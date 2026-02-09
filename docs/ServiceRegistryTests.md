# ServiceRegistryTests

The `ServiceRegistryTests` class contains unit tests for validating the behavior of a service registry implementation in the `grpc-web-bridge` project. These tests verify core functionality such as service registration, retrieval, listing, unregistration, existence checks, and status updates, ensuring the registry correctly handles valid and invalid operations.

## API

### `RegisterService_WithValidGrpcService_AddsServiceToRegistry`
Verifies that registering a valid gRPC service adds it to the registry without errors.
- **Purpose**: Ensures successful registration of a well-formed service.
- **Parameters**: None (test context implicitly uses a mock or test service).
- **Return Value**: None (test assertion).
- **Throws**: Nothing under normal conditions.

### `RegisterService_WithDuplicateService_ThrowsServiceRegistrationException`
Ensures that attempting to register a service with a duplicate identifier throws a `ServiceRegistrationException`.
- **Purpose**: Validates duplicate service prevention.
- **Parameters**: None (test context implicitly uses a duplicate service).
- **Return Value**: None (test assertion).
- **Throws**: `ServiceRegistrationException` when a duplicate service is registered.

### `GetService_WithExistingServiceFullName_ReturnsService`
Confirms that retrieving a service by its full name returns the correct service instance.
- **Purpose**: Validates accurate service lookup.
- **Parameters**: None (test context implicitly uses an existing service full name).
- **Return Value**: None (test assertion verifies non-null return).
- **Throws**: Nothing under normal conditions.

### `GetService_WithNonExistingServiceFullName_ReturnsNull`
Verifies that querying a non-existent service returns `null`.
- **Purpose**: Ensures proper handling of missing services.
- **Parameters**: None (test context implicitly uses a non-existent service full name).
- **Return Value**: None (test assertion verifies `null` return).
- **Throws**: Nothing under normal conditions.

### `ListServices_ReturnsAllRegisteredServices`
Tests that listing services returns all registered services without omission or duplication.
- **Purpose**: Validates completeness of the service listing operation.
- **Parameters**: None.
- **Return Value**: None (test assertion verifies expected count and contents).
- **Throws**: Nothing under normal conditions.

### `UnregisterService_WithExistingService_ReturnsTrueAndRemoves`
Ensures that unregistering an existing service returns `true` and removes it from the registry.
- **Purpose**: Validates successful service removal.
- **Parameters**: None (test context implicitly uses an existing service).
- **Return Value**: None (test assertion verifies `true` return and absence post-removal).
- **Throws**: Nothing under normal conditions.

### `UnregisterService_WithNonExistingService_ReturnsFalse`
Confirms that attempting to unregister a non-existent service returns `false`.
- **Purpose**: Ensures proper handling of invalid unregistration attempts.
- **Parameters**: None (test context implicitly uses a non-existent service).
- **Return Value**: None (test assertion verifies `false` return).
- **Throws**: Nothing under normal conditions.

### `ServiceExists_WithExistingService_ReturnsTrue`
Verifies that checking for an existing service returns `true`.
- **Purpose**: Validates accurate existence checks.
- **Parameters**: None (test context implicitly uses an existing service).
- **Return Value**: None (test assertion verifies `true` return).
- **Throws**: Nothing under normal conditions.

### `UpdateServiceStatus_WithExistingService_UpdatesStatus`
Ensures that updating the status of an existing service succeeds and reflects the change.
- **Purpose**: Validates status modification functionality.
- **Parameters**: None (test context implicitly uses an existing service and new status).
- **Return Value**: None (test assertion verifies status update).
- **Throws**: Nothing under normal conditions.

## Usage

### Example 1: Registering and Retrieving a Service
