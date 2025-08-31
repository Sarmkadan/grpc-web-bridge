// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Contributing to gRPC-Web Bridge

Thank you for your interest in contributing to gRPC-Web Bridge! This document provides guidelines and instructions for contributing.

## Code of Conduct

Be respectful and professional in all interactions. Harassment, discrimination, and hostile behavior are not tolerated.

## Getting Started

### Prerequisites

- .NET 10 SDK or later
- Git
- Visual Studio 2022, VS Code, or Rider

### Setup Development Environment

```bash
# Clone the repository
git clone https://github.com/sarmkadan/grpc-web-bridge.git
cd grpc-web-bridge

# Restore packages and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Start development server
cd src/GrpcWebBridge
dotnet run
```

## Development Workflow

### 1. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
# or for bug fixes:
git checkout -b fix/your-bug-description
```

Use descriptive branch names:
- `feature/` for new features
- `fix/` for bug fixes
- `docs/` for documentation
- `test/` for tests
- `refactor/` for refactoring

### 2. Make Your Changes

Follow these guidelines:

**Code Style**:
- Use C# naming conventions (PascalCase for public members, camelCase for private)
- Use nullable reference types (`#nullable enable`)
- Keep methods focused and under 50 lines
- Write self-documenting code with clear intent

**Comments**:
- Only comment the "why", not the "what"
- Explain non-obvious logic
- Update comments when behavior changes
- No commented-out code blocks

**Commits**:
- Commit logically related changes together
- Write clear, descriptive commit messages
- Reference issues when relevant: "Fixes #123"
- Use imperative mood: "Add feature" not "Added feature"

### 3. Testing

```bash
# Run all tests
dotnet test

# Run specific test file
dotnet test --filter ClassName=MyTest

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**Test Requirements**:
- New features must have tests
- Bug fixes should include regression tests
- Tests should be independent and idempotent
- Use meaningful test names: `DescribeWhatIsBeingTested`

### 4. Code Quality

```bash
# Format code
dotnet format

# Check formatting
dotnet format --verify-no-changes

# Run static analysis
dotnet build /p:EnforceCodeStyleInBuild=true
```

**Requirements**:
- No compiler warnings
- Pass code style checks
- No obvious code smells
- Maintain or improve test coverage

### 5. Documentation

Update documentation for:
- New features
- API changes
- Configuration options
- Architecture changes

Update these files as needed:
- `README.md` - Overview and quick start
- `docs/ARCHITECTURE.md` - Design details
- `docs/API_REFERENCE.md` - API endpoints
- `CHANGELOG.md` - Version history

### 6. Create a Pull Request

```bash
# Push your branch
git push origin feature/your-feature-name

# Create PR on GitHub
# - Clear title describing changes
# - Description explaining "why"
# - Link related issues
# - Screenshots for UI changes
```

**PR Requirements**:
- Passes all CI checks
- Code review approval
- Tests passing
- Documentation updated

## Submitting Issues

### Bug Reports

Include:
- Clear title
- Detailed description
- Steps to reproduce
- Expected vs actual behavior
- Environment (OS, .NET version, etc.)
- Logs or error messages
- Minimal reproduction code

### Feature Requests

Include:
- Clear title
- Use case description
- Why you need this feature
- Proposed solution (if any)
- Alternative solutions

### Discussion Topics

Use GitHub Discussions for:
- Questions
- Architecture discussions
- Design decisions
- General feedback

## Review Process

### What to Expect

1. **Initial Review** (1-2 days)
   - Check for obvious issues
   - Request changes if needed
   - Ask questions for clarity

2. **Code Review** (1-3 days)
   - Review logic and implementation
   - Check for bugs and edge cases
   - Suggest improvements
   - Verify tests

3. **Approval & Merge**
   - Address review feedback
   - Rebase on main if needed
   - Merge when approved

### Review Checklist

Contributors should ensure:
- [ ] Code compiles without warnings
- [ ] Tests pass locally
- [ ] Tests added/updated
- [ ] Documentation updated
- [ ] Code is formatted
- [ ] Commit messages are clear
- [ ] No sensitive data in code
- [ ] No breaking changes (or documented)

Reviewers verify:
- [ ] Tests are adequate
- [ ] Logic is sound
- [ ] Code is maintainable
- [ ] Performance impact acceptable
- [ ] Documentation is complete
- [ ] No security issues

## Coding Standards

### File Structure

```
src/GrpcWebBridge/
├── Controllers/          # HTTP request handlers
├── Services/            # Business logic
├── Domain/              # Models and entities
├── Data/                # Data access
├── Middleware/          # Pipeline middleware
├── Extensions/          # Extension methods
├── Configuration/       # Setup and config
├── Integration/         # External integrations
└── Utilities/           # Helper functions
```

### Naming Conventions

| Item | Convention | Example |
|------|-----------|---------|
| Classes | PascalCase | `UserService` |
| Methods | PascalCase | `GetUserAsync` |
| Properties | PascalCase | `MaxStreamCount` |
| Private fields | camelCase with `_` | `_logger` |
| Constants | PascalCase | `DefaultTimeout` |
| Interfaces | PascalCase with `I` prefix | `ILogger` |

### Documentation Standards

**Method Documentation**:
```csharp
/// <summary>
/// Translates a gRPC request to gRPC-Web format.
/// </summary>
/// <param name="request">The gRPC request to translate</param>
/// <returns>The translated gRPC-Web response</returns>
/// <exception cref="ProtocolException">Thrown if translation fails</exception>
public async Task<GrpcWebResponse> TranslateAsync(GrpcRequest request)
{
    // Implementation
}
```

**Class Documentation**:
```csharp
/// <summary>
/// Manages protocol translation between gRPC and gRPC-Web.
/// </summary>
/// <remarks>
/// This service handles the core bridge functionality including
/// message serialization, compression, and format conversion.
/// </remarks>
public class ProtocolTranslationService
{
    // Implementation
}
```

## Security

### Reporting Security Issues

Do not create public GitHub issues for security vulnerabilities. Email security details to the maintainers.

### Security Guidelines

When contributing security-related code:
- Don't commit secrets or credentials
- Use secure APIs for cryptography
- Validate all inputs
- Handle sensitive data carefully
- Don't trust external input

## Performance

### Guidelines

- Profile before optimizing
- Prefer readability over micro-optimizations
- Use async/await for I/O operations
- Minimize allocations in hot paths
- Cache expensive computations

### Testing Performance

```bash
# Run benchmarks (if available)
dotnet run --configuration Release --project benchmarks/
```

## Documentation

### Writing Documentation

- Use clear, simple language
- Include code examples
- Add tables for references
- Keep it up-to-date
- Link to related docs

### Documentation Files

- `README.md` - Project overview
- `docs/GETTING_STARTED.md` - Quick start
- `docs/ARCHITECTURE.md` - Technical details
- `docs/DEPLOYMENT.md` - Production setup
- `docs/FAQ.md` - Common questions

## Release Process

### Version Numbers

Follow [Semantic Versioning](https://semver.org/):
- MAJOR: Breaking changes
- MINOR: New features (backward compatible)
- PATCH: Bug fixes (backward compatible)

Format: `vX.Y.Z` (e.g., `v1.2.0`)

### Changelog Format

```markdown
## [1.2.0] - 2024-12-15

### Added
- New feature description

### Changed
- Change description

### Fixed
- Bug fix description

### Security
- Security fix description

### Deprecated
- Deprecated feature

### Removed
- Removed feature
```

## Questions?

- Check [FAQ](docs/FAQ.md)
- Review [existing issues](https://github.com/sarmkadan/grpc-web-bridge/issues)
- Join discussions on GitHub
- Ask maintainers directly

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

---

**Thank you for contributing to gRPC-Web Bridge!** Your efforts help make this project better for everyone.
