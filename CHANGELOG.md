// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Changelog

All notable changes to gRPC-Web Bridge are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2024-12-15

### Added
- **Metrics Endpoint**: New `/api/metrics` endpoint for monitoring
- **Stream Statistics**: Detailed stream analytics and tracking
- **Configuration API**: Dynamic service registration endpoints
- **Response Formatting**: Support for CSV, XML, and JSON output formats
- **Webhook Publishing**: Event-driven integrations with external services
- **Health Check Endpoints**: `/health/live` and `/health/ready` for K8s
- **Service Discovery**: Automatic service registration support
- **Request Correlation**: X-Correlation-ID header tracking

### Changed
- **Connection Pooling**: Optimized gRPC channel reuse strategy
- **Streaming Performance**: Improved buffer management and backpressure
- **Authentication**: Enhanced JWT validation with claim extraction
- **Error Handling**: More detailed error messages and codes
- **Logging**: Structured Serilog integration with multiple sinks

### Fixed
- Fixed stream cleanup on timeout
- Resolved connection leaks in high-load scenarios
- Corrected CORS header handling for complex requests
- Fixed compression flag propagation in responses

### Security
- Added rate limiting middleware
- Implemented request validation
- Enhanced HTTPS configuration
- Added API key authentication support

### Deprecated
- Direct gRPC address configuration (use API endpoints instead)

## [1.1.0] - 2024-11-20

### Added
- **Bidirectional Streaming**: Full duplex communication support
- **Compression**: gzip and deflate response compression
- **CORS Support**: Flexible cross-origin resource sharing
- **API Documentation**: Swagger/OpenAPI integration
- **Custom Middleware**: Extension point for custom concerns
- **Service Registry**: Dynamic service management
- **Request Logging**: HTTP request/response logging

### Changed
- **Message Format**: Improved protobuf serialization
- **Stream Timeouts**: Configurable idle and heartbeat intervals
- **Response Codes**: More granular HTTP status codes
- **Configuration**: Moved to appsettings.json structure

### Fixed
- Fixed stream message ordering
- Resolved memory issues with large responses
- Corrected gRPC metadata handling
- Fixed TLS certificate loading

## [1.0.0] - 2024-10-15

### Added
- **Core gRPC-Web Bridge**: Protocol translation between gRPC and gRPC-Web
- **Unary RPC**: Simple request-response pattern
- **Server Streaming**: Multiple server responses
- **Client Streaming**: Multiple client requests
- **Authentication**: JWT bearer token support
- **Service Registry**: Service metadata management
- **Connection Management**: gRPC channel pooling
- **Async/Await**: Fully asynchronous request handling
- **Error Handling**: Custom exception types and middleware

### Features
- Protocol translation from gRPC-Web to gRPC
- Support for all four RPC patterns
- JWT token validation
- API key authentication
- Service discovery and registration
- Connection pooling with health checks
- Request/response logging
- Graceful shutdown support

## [0.9.0] - 2024-09-20 (Beta)

### Added
- Initial beta release
- Basic protocol translation
- Unary RPC support
- Simple authentication
- Health check endpoint
- Configuration management

### Known Issues
- Streaming support incomplete
- Performance optimization needed
- Documentation sparse

---

## Version Comparison

| Feature | 0.9.0 | 1.0.0 | 1.1.0 | 1.2.0 |
|---------|-------|-------|-------|-------|
| Unary RPC | ✓ | ✓ | ✓ | ✓ |
| Server Streaming | ✗ | ✓ | ✓ | ✓ |
| Client Streaming | ✗ | ✓ | ✓ | ✓ |
| Bidirectional Streaming | ✗ | ✗ | ✓ | ✓ |
| JWT Auth | ✓ | ✓ | ✓ | ✓ |
| API Keys | ✗ | ✗ | ✗ | ✓ |
| CORS | ✗ | ✗ | ✓ | ✓ |
| Compression | ✗ | ✗ | ✓ | ✓ |
| Swagger/OpenAPI | ✗ | ✗ | ✓ | ✓ |
| Metrics | ✗ | ✗ | ✗ | ✓ |
| Webhooks | ✗ | ✗ | ✗ | ✓ |
| Health Checks | ✓ | ✓ | ✓ | ✓ |
| Rate Limiting | ✗ | ✗ | ✗ | ✓ |
| Docker Support | ✗ | ✗ | ✓ | ✓ |
| Kubernetes Ready | ✗ | ✗ | ✗ | ✓ |

## Upgrade Guide

### From 1.1.0 to 1.2.0
- No breaking changes
- New endpoints available under `/api/metrics` and `/api/configuration`
- Metrics collection enabled by default
- Consider updating appsettings.json for new options

### From 1.0.0 to 1.1.0
- Breaking: Service registry API has changed
- New configuration structure required
- Recommend full redeployment

### From 0.9.0 to 1.0.0
- Complete rewrite of streaming support
- Configuration format updated
- All users should upgrade

## Maintenance

### Supported Versions
- **Current**: 1.2.0 (Production Ready)
- **LTS**: 1.1.0 (Bug fixes only, until 2025-11-20)
- **EOL**: 1.0.0 (No longer supported)

### Patch Schedule
- Security fixes: Released immediately
- Bug fixes: Released monthly
- Features: Released quarterly

## Contributors

Maintained by:
- **Vladyslav Zaiets** - Creator & Lead Developer

## License

MIT License - See LICENSE file for details
