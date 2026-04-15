# Contributing to grpc-web-bridge

Thank you for considering contributing to grpc-web-bridge!

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Docker (optional, for container testing)

## Building Locally

```bash
# Restore dependencies
dotnet restore

# Build in Release mode
dotnet build --configuration Release

# Build in Debug mode (default)
dotnet build
```

## Running Tests

```bash
# Run all tests
dotnet test --verbosity normal

# Run with detailed output and save results
dotnet test --verbosity normal --logger "trx;LogFileName=test-results.trx"

# Run a specific test project
dotnet test tests/grpc-web-bridge.Tests/
```

## Running Benchmarks

```bash
dotnet run --project benchmarks/grpc-web-bridge.Benchmarks/ --configuration Release
```

## Pull Request Guidelines

1. **Fork** the repository and create a branch from `main`.
2. **Write tests** for any new functionality or bug fix.
3. **Ensure all tests pass** before submitting: `dotnet test`.
4. **Follow the existing code style** — the `.editorconfig` enforces formatting rules.
5. **Provide XML documentation** for all public classes and methods.
6. Keep pull requests focused — one feature or fix per PR.
7. Update `CHANGELOG.md` with a brief description of your change.

## Code Style

- 4-space indentation, Allman brace style.
- Use `var` where the type is obvious.
- File-scoped namespace declarations.
- `using` directives outside the namespace.
- All public APIs must have XML doc comments (`///`).

## Reporting Issues

Use [GitHub Issues](https://github.com/sarmkadan/grpc-web-bridge/issues) and include:
- Steps to reproduce
- Expected vs. actual behavior
- .NET version and OS
- Relevant logs or stack traces

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).