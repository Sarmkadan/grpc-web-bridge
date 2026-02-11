# GrpcWebBridgeOptionsTests

Unit tests for the `GrpcWebBridgeOptions` class, verifying configuration behavior for gRPC-Web bridge settings including environment-specific defaults, origin restrictions, stream limits, and compression settings.

## API

### `Constructor_WithEnvironment_SetsEnvironment`
Verifies that the constructor initializes the environment setting correctly when provided via constructor parameter. No parameters; no return value. Does not throw.

### `WithDevelopment_SetsDevelopmentDefaults`
Ensures that calling `WithDevelopment()` applies default development-time settings such as disabled compression, unrestricted origins, and default max stream count. No parameters; no return value. Does not throw.

### `WithProduction_SetsProductionDefaults`
Validates that `WithProduction()` configures production-safe defaults including enabled compression, restricted origins, and a conservative max stream count. No parameters; no return value. Does not throw.

### `WithTesting_SetsTestingDefaults`
Confirms that `WithTesting()` sets testing-specific defaults such as disabled compression and unrestricted origins for integration testing scenarios. No parameters; no return value. Does not throw.

### `WithMaxStreamCount_ValidValue_SetsCount`
Tests that providing a positive integer to `WithMaxStreamCount()` updates the internal max stream limit to the specified value. Parameters: `count` (int) — the maximum number of concurrent streams allowed. No return value. Does not throw.

### `WithMaxStreamCount_Zero_ThrowsArgumentException`
Ensures that passing zero or a negative value to `WithMaxStreamCount()` results in an `ArgumentException` being thrown. Parameters: `count` (int) — the invalid stream count. No return value. Throws: `ArgumentException` if `count <= 0`.

### `AddAllowedOrigins_WithValidOrigins_AddsToConfiguration`
Checks that calling `AddAllowedOrigins()` with a non-empty collection of origin strings correctly appends those origins to the allowed origins list. Parameters: `origins` (IEnumerable<string>) — collection of origin URIs to allow. No return value. Does not throw.

### `WithCompression_ValidLevel_EnablesCompression`
Verifies that setting a valid compression level via `WithCompression()` enables or disables compression based on the provided level. Parameters: `level` (CompressionLevel) — the compression level to apply. No return value. Does not throw.

## Usage
