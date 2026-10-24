# Contributing to gRPC-Web Bridge

Thanks for helping out. This guide covers local setup, the checks a change must pass, and how branches, commits and pull requests are named and reviewed. Project conventions (layout, DI, error handling, test naming) are in [CLAUDE.md](CLAUDE.md) and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Prerequisites

- **.NET SDK 10.0** (the solution targets `net10.0`). `global.json` pins `10.0.100` with `rollForward: latestMinor`, so any 10.0.x SDK at or above that version works.
- Git.
- Docker is optional and only needed for `docker build`.

Check your SDK with `dotnet --version`.

## Clone, restore, build, test

```bash
git clone https://github.com/sarmkadan/grpc-web-bridge.git
cd grpc-web-bridge
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Run these from the repository root, where `grpc-web-bridge.sln` lives. The `dotnet build` and `dotnet test` commands cover the whole solution, including the test and benchmark projects.

- To build or test only one project, pass its path, for example `dotnet test tests/grpc-web-bridge.Tests`.
- `make test` runs `dotnet test` inside `src/GrpcWebBridge`, which is not a test project, so use the commands above instead.

## Code style

- Formatting and analyzer rules are defined in [`.editorconfig`](.editorconfig): 4-space indentation, UTF-8 with BOM, trimmed trailing whitespace, a final newline, and `var` for built-in types.
- Run `dotnet format` before you commit. `dotnet format --verify-no-changes` (`make format-check`) checks without writing.
- Follow the file conventions in CLAUDE.md: a `#nullable enable` header, file-scoped namespaces that match the folder, and XML doc comments on public APIs.
- New behaviour needs tests in `tests/grpc-web-bridge.Tests`, one `<Type>Tests.cs` file per class under test. Test names follow `Method_Scenario_ExpectedResult`.

## Branch naming

Create branches from `main`, using a type prefix and a short kebab-case description:

```
feat/<short-description>
fix/<short-description>
refactor/<short-description>
docs/<short-description>
test/<short-description>
ci/<short-description>
```

Examples: `fix/validate-port-range-in-service-registration`, `docs/improve-api-documentation`.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/): `<type>(<optional scope>): <imperative summary>`.

- Types in use: `feat`, `fix`, `refactor`, `docs`, `test`, `style`, `chore`.
- Scope is optional and names the affected area, for example `feat(logging): ...` or `test(data): ...`.
- Keep the summary short and in the imperative mood, for example `fix: reject port numbers outside 1-65535`.
- Add a body when the reason for the change is not obvious from the diff.

## Pull request checklist

Open pull requests against `main`. Use the template in `.github/PULL_REQUEST_TEMPLATE.md`, and confirm each item before requesting review:

- [ ] `dotnet build --configuration Release` succeeds with no new warnings compared to `main`.
- [ ] `dotnet test --configuration Release` passes.
- [ ] New or changed behaviour has tests.
- [ ] `dotnet format` has been run and the diff contains only intended changes.
- [ ] The description states what changed and why, and notes any breaking change.
- [ ] Public API changes have XML doc comments and, where relevant, updated docs under `docs/`.

CI runs the secret scan, build and test on pull requests to `main`. Make sure the build and tests pass locally before pushing.
