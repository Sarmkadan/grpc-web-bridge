# ServiceRepositoryTests

Unit tests for `ServiceRepository` that verify CRUD operations and request handling for gRPC-web bridge services.

## API

### `ServiceRepositoryTests`
Constructor that initializes the test fixture with a fresh `ServiceRepository` instance and test data.

### `AddAsync_WithNewService_ReturnsTrueAndStoresService`
Verifies that adding a new service succeeds and persists the service in the repository.

- **Parameters**: `Service service` – The service to add.
- **Return value**: `Task<bool>` – `true` if the service was added, `false` otherwise.
- **Throws**: May throw if the service is `null` or invalid.

### `AddAsync_WithDuplicateServiceId_ReturnsFalse`
Ensures that adding a service with a duplicate ID fails and does not modify the repository.

- **Parameters**: `Service service` – The service with a duplicate ID.
- **Return value**: `Task<bool>` – `false` indicating the operation failed.
- **Throws**: May throw if the service is `null`.

### `GetByFullNameAsync_WithExistingService_ReturnsService`
Confirms that retrieving a service by its full name returns the correct service.

- **Parameters**: `string fullName` – The full name of the service to retrieve.
- **Return value**: `Task<Service>` – The matching service, or `null` if not found.
- **Throws**: May throw if `fullName` is `null` or empty.

### `DeleteAsync_WithExistingService_ReturnsTrueAndRemoves`
Validates that deleting an existing service succeeds and removes it from the repository.

- **Parameters**: `string fullName` – The full name of the service to delete.
- **Return value**: `Task<bool>` – `true` if the service was deleted, `false` otherwise.
- **Throws**: May throw if `fullName` is `null` or empty.

### `CountAsync_ReturnsCorrectCount`
Checks that the repository returns the correct number of services.

- **Return value**: `Task<int>` – The current count of services.
- **Throws**: Never throws.

### `UpdateAsync_WithExistingService_UpdatesAndReturnsTrue`
Ensures that updating an existing service modifies it and returns success.

- **Parameters**: `Service service` – The updated service.
- **Return value**: `Task<bool>` – `true` if the service was updated, `false` otherwise.
- **Throws**: May throw if the service is `null` or invalid.

### `ExistsAsync_WithNonExistentFullName_ReturnsFalse`
Confirms that checking for a non-existent service returns `false`.

- **Parameters**: `string fullName` – The full name to check.
- **Return value**: `Task<bool>` – `false` indicating the service does not exist.
- **Throws**: May throw if `fullName` is `null` or empty.

### `AddRequestAsync_WithValidRequest_ReturnsTrue`
Verifies that adding a valid request succeeds and persists it.

- **Parameters**: `ServiceRequest request` – The request to add.
- **Return value**: `Task<bool>` – `true` if the request was added, `false` otherwise.
- **Throws**: May throw if the request is `null` or invalid.

### `GetByIdAsync_WithNonExistentId_ReturnsNull`
Ensures that retrieving a non-existent service by ID returns `null`.

- **Parameters**: `string id` – The service ID to retrieve.
- **Return value**: `Task<Service>` – `null` indicating no service was found.
- **Throws**: May throw if `id` is `null` or empty.

### `GetByPackageAsync_ReturnsServicesForPackage`
Confirms that retrieving services by package returns the correct subset.

- **Parameters**: `string package` – The package name to filter by.
- **Return value**: `Task<IEnumerable<Service>>` – The services matching the package.
- **Throws**: May throw if `package` is `null` or empty.
- **Throws**: Never throws if no services match.

## Usage
