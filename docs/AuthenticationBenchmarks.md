# AuthenticationBenchmarks

The `AuthenticationBenchmarks` class provides a set of performance benchmarking scenarios for the authentication pipeline within the `grpc-web-bridge` infrastructure. It is designed to measure the execution time and resource overhead associated with key authentication components, including Bearer token extraction, authentication context caching, API key validation, and context verification.

## API

- **`public void Setup()`**
  Initializes the state and prerequisite test data required for all subsequent benchmark scenarios. This method must be executed before calling any benchmark methods.

- **`public string? ExtractBearerToken_Valid()`**
  Benchmarks the performance of extracting a valid Bearer token from an `HttpRequest`. Returns the extracted token string if successful, otherwise returns `null`.

- **`public string? ExtractBearerToken_Invalid()`**
  Benchmarks the performance of processing an `HttpRequest` containing an incorrectly formatted or invalid Bearer token. Returns `null`.

- **`public string? ExtractBearerToken_Null()`**
  Benchmarks the performance of handling an `HttpRequest` where the Authorization header is missing or null. Returns `null`.

- **`public AuthenticationContext? GetCachedContext_Hit()`**
  Benchmarks the performance of retrieving an `AuthenticationContext` from the cache when the lookup key exists. Returns the `AuthenticationContext` object if found; otherwise, returns `null`.

- **`public AuthenticationContext? GetCachedContext_Miss()`**
  Benchmarks the performance of a cache miss scenario when attempting to retrieve an `AuthenticationContext`. Returns `null`.

- **`public AuthenticationContext AuthenticateApiKey()`**
  Benchmarks the execution path for authenticating a request using an API key. Returns the resulting `AuthenticationContext` upon successful authentication.

- **`public bool ValidateContext()`**
  Benchmarks the performance of validating an existing `AuthenticationContext` object to ensure it meets current security requirements. Returns `true` if the context is valid, otherwise `false`.

## Usage

### Example 1: Basic Execution in a Harness
```csharp
var benchmarks = new AuthenticationBenchmarks();

// Initialize the environment
benchmarks.Setup();

// Measure performance of successful token extraction
var token = benchmarks.ExtractBearerToken_Valid();
if (token != null)
{
    // Proceed with authentication...
}
```

### Example 2: Integration with BenchmarkDotNet
```csharp
[MemoryDiagnoser]
public class AuthPerformanceSuite
{
    private readonly AuthenticationBenchmarks _benchmarks = new();

    [GlobalSetup]
    public void Setup() => _benchmarks.Setup();

    [Benchmark]
    public void BenchmarkApiKeyAuthentication()
    {
        _benchmarks.AuthenticateApiKey();
    }
}
```

## Notes

- **Intended Use:** These methods are specifically designed for use with performance measurement frameworks, such as BenchmarkDotNet, to produce reliable micro-benchmarks.
- **Thread Safety:** The `AuthenticationBenchmarks` class is not thread-safe. It maintains an internal state initialized by the `Setup` method; concurrent calls to any of the benchmark methods may result in undefined behavior or inaccurate performance data.
- **Execution Order:** Always call `Setup` once before executing any other methods in the class.
- **Environmental Impact:** Performance results are highly dependent on the underlying hardware, framework version, and environment configuration. These benchmarks should be run on representative hardware to establish reliable baselines.
