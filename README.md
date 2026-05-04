# gRPC-Web Bridge for .NET

A production-grade gRPC-Web bridge server for .NET 10 that enables seamless protocol translation between gRPC and gRPC-Web clients, with comprehensive streaming support, authentication middleware, and integrated Swagger documentation.

## Features

- **Protocol Translation**: Seamless conversion between gRPC and gRPC-Web protocols
- **Streaming Support**: Full support for unary, client streaming, server streaming, and bidirectional streaming
- **Authentication & Authorization**: Bearer token authentication with JWT, API key support, and custom credential schemes
- **Service Registry**: Dynamic service discovery and registration with metadata caching
- **Connection Management**: Robust gRPC connection pooling and lifecycle management
- **Stream Management**: Idle stream detection, heartbeat support, and automatic cleanup
- **Compression**: Response compression with configurable levels
- **CORS Support**: Flexible cross-origin resource sharing configuration
- **Swagger/OpenAPI**: Auto-generated API documentation
- **Health Checks**: Built-in health status endpoints
- **Comprehensive Logging**: Structured logging with Serilog

## Technology Stack

- **.NET 10** - Latest .NET runtime
- **ASP.NET Core** - Web framework
- **gRPC** - Protocol implementation
- **Serilog** - Structured logging
- **JWT** - Token-based authentication
- **Swagger/OpenAPI** - API documentation

## Project Structure

```
grpc-web-bridge/
├── src/GrpcWebBridge/
│   ├── Domain/
│   │   ├── Models/          # Domain entities
│   │   ├── Exceptions/      # Custom exception types
│   │   ├── Constants.cs     # Global constants
│   │   └── Enums.cs         # Enumeration types
│   ├── Services/            # Business logic
│   ├── Data/                # Data access layer
│   ├── Configuration/       # Startup and configuration
│   ├── Extensions/          # Extension methods
│   ├── Program.cs           # Application entry point
│   ├── appsettings.json     # Configuration files
│   └── GrpcWebBridge.csproj # Project file
├── LICENSE                  # MIT License
├── README.md               # This file
└── .gitignore             # Git ignore patterns
```

## Getting Started

### Prerequisites

- .NET 10 SDK or later
- Visual Studio 2022 / VS Code / Rider
- Git

### Installation

1. Clone the repository:
```bash
git clone https://github.com/sarmkadan/grpc-web-bridge.git
cd grpc-web-bridge
```

2. Build the project:
```bash
cd src/GrpcWebBridge
dotnet build
```

3. Run the server:
```bash
dotnet run
```

The server will start on `http://localhost:5000` and `https://localhost:5001`.

### API Endpoints

#### Health Check
```
GET /health
```

Returns system health status and metrics.

#### List Services
```
GET /api/services
```

Lists all registered gRPC services.

#### Service Details
```
GET /api/services/{serviceId}
```

Retrieves detailed information about a specific service.

#### Stream Statistics
```
GET /api/streams
```

Gets statistics for active streams.

#### Swagger UI
```
http://localhost:5000/swagger
```

Interactive API documentation (available in Development environment).

## Configuration

Configure the bridge using `appsettings.json`:

```json
{
  "GrpcWebBridge": {
    "Environment": "Development",
    "MaxStreamCount": 10000,
    "StreamIdleTimeoutSeconds": 300,
    "MaxMessageSize": 4194304,
    "CompressResponses": true,
    "EnableSwagger": true,
    "AllowedOrigins": ["*"]
  }
}
```

## Core Components

### ProtocolTranslationService
Handles protocol conversion between gRPC, gRPC-Web, and other formats.

### StreamingService
Manages stream lifecycle, buffering, and message queueing.

### AuthenticationService
Provides JWT bearer token validation, API key authentication, and role-based authorization.

### ServiceRegistry
Maintains service discovery with metadata caching and health monitoring.

### GrpcConnectionManager
Manages gRPC channel pooling and connection lifecycle.

## Domain Models

- **GrpcService**: Service definition with metadata
- **GrpcMethod**: Method definition with parameter information
- **GrpcRequest**: Incoming gRPC request wrapper
- **GrpcResponse**: Outgoing gRPC response wrapper
- **StreamMessage**: Individual message in a stream
- **AuthenticationContext**: User authentication and authorization context
- **BridgeConfiguration**: Bridge-wide configuration settings

## Error Handling

The bridge provides custom exception types for different error scenarios:

- `GrpcWebBridgeException`: Base exception for all bridge operations
- `ServiceRegistrationException`: Service registration and discovery errors
- `StreamingException`: Stream lifecycle and message errors
- `ProtocolException`: Protocol translation and conversion errors

## Authentication

### Bearer Token
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### API Key
Configure and validate API keys through the AuthenticationService.

## Development

### Building
```bash
dotnet build
```

### Running Tests
```bash
dotnet test
```

### Code Style
The project follows C# naming conventions and uses nullable reference types (`#nullable enable`).

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Author

**Vladyslav Zaiets**
- Website: https://sarmkadan.com
- Email: rutova2@gmail.com

---

**Built with ❤️ for gRPC-Web enthusiasts**
