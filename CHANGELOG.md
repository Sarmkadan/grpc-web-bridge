## [2.0.2] - 2026-05-21

### Fixed
- Fix deadline propagation in nested gRPC calls causing premature timeouts
- Added regression test for the fix
### Added
- Add bidirectional streaming with backpressure and flow control
- Docker support with multi-stage builds
- Health check endpoints (/health, /health/ready)
- Integration test suite with xUnit
- Migration guide from v1.x
### Changed
- Upgraded to .NET 10.0
- Modern C# features (records, primary constructors)
- Improved API consistency
### Fixed
- Various edge cases found through testing