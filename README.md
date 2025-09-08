[![Build](https://github.com/sarmkadan/grpc-web-bridge/actions/workflows/build.yml/badge.svg)](https://github.com/sarmkadan/grpc-web-bridge/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

# gRPC-Web Bridge for .NET

A production-grade gRPC-Web bridge server for .NET 10 that enables seamless protocol translation between gRPC and gRPC-Web clients, with comprehensive streaming support, authentication middleware, and integrated Swagger documentation.

**Status**: Stable | **License**: MIT | **Latest Version**: 2.0.2

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Usage Examples](#usage-examples)
- [API Reference](#api-reference)
- [Configuration Reference](#configuration-reference)
- [Authentication](#authentication)
- [Streaming Guide](#streaming-guide)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Performance](#performance)
- [Testing](#testing)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## Overview

gRPC-Web Bridge is a production-grade bridge server that translates between gRPC and gRPC-Web protocols, enabling seamless communication between traditional gRPC services and web browsers. Built on .NET 10 with ASP.NET Core, it provides a robust, scalable solution for organizations transitioning to gRPC-Web or running heterogeneous service architectures.

### Why gRPC-Web Bridge?

gRPC-Web is a browser-friendly variant of gRPC that uses HTTP/1.1 instead of HTTP/2, making it accessible from web clients. However, integrating gRPC-Web requires either:

1. Running separate gRPC and gRPC-Web servers (operational overhead)
2. Modifying your service infrastructure (risky and disruptive)
3. Using a bridge service (this project)

gRPC-Web Bridge solves this elegantly by:
- **Centralizing protocol handling**: Single service bridges all traffic
- **Preserving existing gRPC services**: Zero changes to backend services
- **Adding value**: Authentication, streaming, monitoring, health checks
- **Enterprise-ready**: Production security, logging, compression, rate limiting

### Use Cases

- **Web Applications**: Browser-based frontends calling gRPC services
- **Legacy Integration**: Connecting HTTP-only clients to gRPC services
- **Multi-Protocol**: Services supporting both gRPC and gRPC-Web simultaneously
- **Microservices**: Bridge in front of gRPC microservices
- **API Gateways**: Custom API layer with protocol flexibility

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Client Applications                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │  Web Browser │  │  Mobile App  │  │  Legacy HTTP │          │
│  │  (gRPC-Web)  │  │ (gRPC-Web)   │  │   Client     │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                           │
                    HTTP/1.1 & HTTP/2
                           │
        ┌──────────────────────────────────────────────┐
        │   gRPC-Web Bridge Server (.NET 10)          │
        │  ┌────────────────────────────────────────┐ │
        │  │ Controllers & Middleware Layer         │ │
        │  │ - Request Routing                      │ │
        │  │ - Authentication/Authorization         │ │
        │  │ - Rate Limiting                        │ │
        │  │ - Request/Response Logging             │ │
        │  └────────────────────────────────────────┘ │
        │  ┌────────────────────────────────────────┐ │
        │  │ Protocol Translation Service           │ │
        │  │ - gRPC-Web → gRPC conversion           │ │
        │  │ - Message serialization/deserialization│ │
        │  │ - Compression handling                 │ │
        │  └────────────────────────────────────────┘ │
        │  ┌────────────────────────────────────────┐ │
        │  │ Streaming & Connection Management      │ │
        │  │ - Stream lifecycle                     │ │
        │  │ - Heartbeat management                 │ │
        │  │ - Connection pooling                   │ │
        │  │ - Idle cleanup                         │ │
        │  └────────────────────────────────────────┘ │
        │  ┌────────────────────────────────────────┐ │
        │  │ Cross-cutting Concerns                 │ │
        │  │ - Health checks                        │ │
        │  │ - Metrics collection                   │ │
        │  │ - Distributed tracing (ready)          │ │
        │  │ - Caching                              │ │
        │  └────────────────────────────────────────┘ │
        └──────────────────────────────────────────────┘
                           │
                    HTTP/2 (gRPC protocol)
                           │
        ┌──────────────────────────────────────────────┐
        │         Backend gRPC Services                 │
        │  ┌──────────────┐  ┌──────────────┐         │
        │  │  Service 1   │  │  Service 2   │  ...    │
        │  │  (gRPC)      │  │  (gRPC)      │         │
        │  └──────────────┘  └──────────────┘         │
        └──────────────────────────────────────────────┘
```

### Component Overview

| Component | Purpose | Responsibility |
|-----------|---------|-----------------|
| **BridgeController** | HTTP request handler | Routing, parameter parsing |
| **ProtocolTranslationService** | Protocol conversion | gRPC ↔ gRPC-Web translation |
| **StreamingService** | Stream management | Lifecycle, buffering, cleanup |
| **AuthenticationService** | Security | JWT, API keys, authorization |
| **ServiceRegistry** | Service discovery | Metadata, health, caching |
| **GrpcConnectionManager** | Connection pooling | Channel management, reuse |
| **ErrorHandlingMiddleware** | Error standardization | Consistent error responses |
| **RateLimitingMiddleware** | Traffic control | Per-client rate limiting |

## Features

### Core Protocol Support

- ✅ **Protocol Translation**: Seamless gRPC ↔ gRPC-Web conversion
- ✅ **Unary RPC**: Simple request-response patterns
- ✅ **Server Streaming**: Server sends multiple messages
- ✅ **Client Streaming**: Client sends multiple messages
- ✅ **Bidirectional Streaming**: Full-duplex communication
- ✅ **Message Compression**: gzip and deflate support
- ✅ **Binary & JSON**: Multiple message formats

### Security & Authentication

- ✅ **JWT Bearer Tokens**: OpenID Connect compatible
- ✅ **API Key Authentication**: Header-based API keys
- ✅ **Role-Based Access Control**: Fine-grained permissions
- ✅ **CORS Support**: Flexible cross-origin configuration
- ✅ **Rate Limiting**: Per-client and global limits
- ✅ **Request Validation**: Input sanitization

### Operations & Observability

- ✅ **Health Checks**: Readiness & liveness probes
- ✅ **Structured Logging**: Serilog integration
- ✅ **Metrics Collection**: Performance and usage metrics
- ✅ **Request Tracing**: Correlation IDs for debugging
- ✅ **Stream Management**: Active stream monitoring
- ✅ **Webhook Publishing**: Event notifications

### Developer Experience

- ✅ **Swagger/OpenAPI**: Interactive API documentation
- ✅ **Service Discovery**: Dynamic service registration
- ✅ **Configuration Management**: Flexible settings system
- ✅ **Error Messages**: Detailed, actionable error responses
- ✅ **Response Formatting**: JSON, CSV, XML output
- ✅ **Caching**: In-memory and distributed caching

## Installation

### Prerequisites

- **.NET 10 SDK** or later ([download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0))
- **C# 14** or later (included with .NET 10)
- **Git** for cloning the repository
- **Docker** (optional, for containerized deployment)

### Method 1: Clone from GitHub

```bash
# Clone the repository
git clone https://github.com/sarmkadan/grpc-web-bridge.git
cd grpc-web-bridge

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the server
cd src/GrpcWebBridge
dotnet run
```

### Method 2: Docker

```bash
# Build the image
docker build -t grpc-web-bridge:latest .

# Run the container
docker run -d \
  -p 5000:5000 \
  -p 5001:5001 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  grpc-web-bridge:latest
```

### Method 3: Docker Compose

```bash
# Start the entire stack
docker-compose up -d

# View logs
docker-compose logs -f grpc-web-bridge

# Stop the stack
docker-compose down
```

### Method 4: Visual Studio / Rider

1. Open `grpc-web-bridge.sln` in Visual Studio 2022 or Rider
2. Build the solution (Ctrl+Shift+B)
3. Press F5 to run with debugging
4. Navigate to `https://localhost:5001/swagger`

## Quick Start

### 1. Start the Bridge Server

```bash
cd src/GrpcWebBridge
dotnet run --configuration Development
```

The server will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`

### 2. Configure Your gRPC Services

Register your gRPC services in the bridge by calling the configuration endpoint:

```bash
curl -X POST http://localhost:5000/api/configuration/register \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName": "MyService",
    "grpcAddress": "grpc://backend-service:50051",
    "methods": ["GetUser", "ListUsers", "CreateUser"]
  }'
```

### 3. Call from a Web Client

```javascript
// Using grpc-web library
const {MyServiceClient} = require('./my_service_pb');
const {GetUserRequest} = require('./my_service_pb');

const client = new MyServiceClient('http://localhost:5000');
const request = new GetUserRequest();
request.setId(123);

client.getUser(request, {}, (err, response) => {
  if (err) console.error('Error:', err);
  else console.log('User:', response.toObject());
});
```

### 4. Monitor Health

```bash
# Check bridge health
curl http://localhost:5000/health

# Check active streams
curl http://localhost:5000/api/streams

# View registered services
curl http://localhost:5000/api/services
```

## Usage Examples

### Example 1: Simple Unary RPC Call

**Request (from web client)**:
```javascript
const request = new GetUserRequest();
request.setId(42);

client.getUser(request, {}, (err, response) => {
  if (err) {
    console.error('RPC failed:', err);
  } else {
    console.log('User:', response.getName(), response.getEmail());
  }
});
```

**Bridge Processing**:
1. Receives gRPC-Web request from browser
2. Translates to gRPC protocol
3. Routes to backend gRPC service
4. Translates response back to gRPC-Web
5. Returns to client

### Example 2: Server Streaming

**Request**:
```javascript
const request = new ListUsersRequest();
request.setPageSize(10);

const stream = client.listUsers(request);

stream.on('data', (user) => {
  console.log('User:', user.getName());
});

stream.on('end', () => {
  console.log('Stream completed');
});

stream.on('error', (err) => {
  console.error('Stream error:', err);
});
```

**Backend gRPC Service Implementation**:
```csharp
public override async Task ListUsers(
    ListUsersRequest request,
    IServerStreamWriter<User> responseStream,
    ServerCallContext context)
{
    var users = await _userService.GetUsersAsync(request.PageSize);
    foreach (var user in users)
    {
        await responseStream.WriteAsync(user);
    }
}
```

### Example 3: Client Streaming

**Request**:
```javascript
const stream = client.uploadMetrics(null, (err, response) => {
  if (err) {
    console.error('Upload failed:', err);
  } else {
    console.log('Metrics recorded:', response.getCount());
  }
});

// Send multiple metrics
const metric1 = new Metric();
metric1.setName('cpu_usage');
metric1.setValue(75.5);
stream.write(metric1);

const metric2 = new Metric();
metric2.setName('memory_usage');
metric2.setValue(85.2);
stream.write(metric2);

stream.end();
```

**Backend Implementation**:
```csharp
public override async Task<UploadMetricsResponse> UploadMetrics(
    IAsyncStreamReader<Metric> requestStream,
    ServerCallContext context)
{
    int count = 0;
    await foreach (var metric in requestStream.ReadAllAsync())
    {
        await _metricsService.RecordAsync(metric);
        count++;
    }
    return new UploadMetricsResponse { Count = count };
}
```

### Example 4: Bidirectional Streaming

**Request**:
```javascript
const stream = client.chat(null);

stream.on('data', (message) => {
  console.log('Server:', message.getContent());
});

// Send messages
const msg1 = new ChatMessage();
msg1.setContent('Hello');
stream.write(msg1);

const msg2 = new ChatMessage();
msg2.setContent('How are you?');
stream.write(msg2);

stream.end();
```

**Backend Implementation**:
```csharp
public override async Task Chat(
    IAsyncStreamReader<ChatMessage> requestStream,
    IServerStreamWriter<ChatMessage> responseStream,
    ServerCallContext context)
{
    await foreach (var message in requestStream.ReadAllAsync())
    {
        var response = new ChatMessage
        {
            Content = $"Echo: {message.Content}"
        };
        await responseStream.WriteAsync(response);
    }
}
```

### Example 5: Error Handling with JWT

**Configuration**:
```json
{
  "GrpcWebBridge": {
    "RequireAuthentication": true
  },
  "Authentication": {
    "JwtAudience": "grpc-web-bridge",
    "JwtIssuer": "https://auth.example.com",
    "JwtExpirationMinutes": 60
  }
}
```

**Client Code with Error Handling**:
```javascript
const metadata = new grpc.Metadata();
metadata.add('authorization', `Bearer ${token}`);

client.getUser(request, metadata, (err, response) => {
  if (err) {
    if (err.code === 'UNAUTHENTICATED') {
      // Token expired or invalid
      refreshToken().then(() => {
        // Retry request
      });
    } else if (err.code === 'PERMISSION_DENIED') {
      console.error('Insufficient permissions');
    } else {
      console.error('Unknown error:', err);
    }
  }
});
```

### Example 6: Compression and Performance

**Configuration for High-Performance**:
```json
{
  "GrpcWebBridge": {
    "CompressResponses": true,
    "CompressionLevel": 9,
    "MaxMessageSize": 8388608,
    "DefaultTimeoutMilliseconds": 60000
  }
}
```

**Client with Compression**:
```javascript
const metadata = new grpc.Metadata();
metadata.add('grpc-encoding', 'gzip');

client.downloadLargeFile(request, metadata, (err, response) => {
  // Automatic decompression by grpc-web library
  console.log('Received (decompressed):', response.getData().length);
});
```

### Example 7: Custom Authentication with API Key

**Configuration**:
```json
{
  "GrpcWebBridge": {
    "RequireAuthentication": true
  },
  "Authentication": {
    "ApiKeyHeader": "X-API-Key",
    "ApiKeys": {
      "key_12345": {
        "name": "Mobile App",
        "roles": ["read", "write"]
      }
    }
  }
}
```

**Client Code**:
```javascript
const metadata = new grpc.Metadata();
metadata.add('x-api-key', 'key_12345');

client.getUser(request, metadata, (err, response) => {
  // Request is authenticated with API key
});
```

### Example 8: Rate Limiting and Metrics

**Configuration**:
```json
{
  "RateLimiting": {
    "Enabled": true,
    "RequestsPerSecond": 100,
    "BurstSize": 200
  }
}
```

**Monitoring Metrics**:
```bash
# Get bridge metrics
curl http://localhost:5000/api/metrics

# Returns:
# {
#   "activeStreams": 45,
#   "totalRequests": 125430,
#   "totalErrors": 42,
#   "averageLatencyMs": 125,
#   "uptime": "5d 12h 34m"
# }
```

### Example 9: Service Registration Workflow

**Automatic Discovery**:
```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpcWebBridge(options =>
{
    options.EnableServiceDiscovery = true;
    options.DiscoveryEndpoint = "http://consul:8500";
    options.RefreshIntervalSeconds = 30;
});
```

**Manual Registration**:
```bash
# Register a service
curl -X POST http://localhost:5000/api/services/register \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName": "UserService",
    "address": "grpc://user-service:50051",
    "healthCheck": true,
    "metadata": {
      "version": "1.0.0",
      "environment": "production"
    }
  }'
```

### Example 10: Request/Response Transformation

**Custom Response Formatting**:
```bash
# Request with format preference
curl http://localhost:5000/api/users?id=42&format=csv

# Bridge automatically transforms the response
# to CSV format based on the Accept header
```

## API Reference

### Health Endpoint

```
GET /health
GET /health/live
GET /health/ready
```

Returns system health status.

**Response** (200 OK):
```json
{
  "status": "Healthy",
  "uptime": "2d 5h 30m",
  "activeConnections": 42,
  "timestamp": "2024-12-15T10:30:00Z"
}
```

### Services Endpoint

```
GET /api/services
GET /api/services/{serviceId}
POST /api/services/register
PUT /api/services/{serviceId}
DELETE /api/services/{serviceId}
```

**Register Service Request**:
```json
{
  "serviceName": "UserService",
  "address": "grpc://localhost:50051",
  "port": 50051,
  "healthCheck": true,
  "metadata": {
    "version": "1.0.0"
  }
}
```

### Streams Endpoint

```
GET /api/streams
GET /api/streams/{streamId}
DELETE /api/streams/{streamId}
```

**Response**:
```json
{
  "activeStreams": [
    {
      "id": "stream-001",
      "serviceName": "UserService",
      "methodName": "GetUser",
      "clientAddress": "192.168.1.100",
      "createdAt": "2024-12-15T10:25:00Z",
      "messagesReceived": 5,
      "messagesSent": 8,
      "bytesReceived": 2048,
      "bytesSent": 4096
    }
  ]
}
```

### Metrics Endpoint

```
GET /api/metrics
GET /api/metrics/requests
GET /api/metrics/streams
GET /api/metrics/errors
```

**Response**:
```json
{
  "totalRequests": 125430,
  "successfulRequests": 125000,
  "failedRequests": 430,
  "averageLatencyMs": 125.5,
  "p95LatencyMs": 450,
  "p99LatencyMs": 890,
  "activeStreams": 45,
  "cacheHitRate": 0.85,
  "uptime": "5d 12h 34m"
}
```

### Configuration Endpoint

```
GET /api/configuration
PUT /api/configuration
```

**Configuration Schema**:
```json
{
  "environment": "Production",
  "maxStreamCount": 10000,
  "streamIdleTimeoutSeconds": 300,
  "maxMessageSize": 4194304,
  "compressResponses": true,
  "enableSwagger": true,
  "allowedOrigins": ["https://app.example.com"]
}
```

## Configuration Reference

### GrpcWebBridge Settings

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `Environment` | string | "Development" | Runtime environment (Development, Production) |
| `InstanceName` | string | "grpc-web-bridge" | Unique instance identifier |
| `EnableLogging` | bool | true | Enable structured logging |
| `EnableSwagger` | bool | true | Enable Swagger/OpenAPI documentation |
| `EnableMetrics` | bool | true | Enable metrics collection |
| `EnableCors` | bool | true | Enable CORS support |
| `RequireAuthentication` | bool | false | Enforce authentication on all requests |
| `MaxStreamCount` | int | 10000 | Maximum concurrent streams |
| `StreamIdleTimeoutSeconds` | int | 300 | Idle stream timeout (5 minutes) |
| `StreamHeartbeatIntervalSeconds` | int | 30 | Heartbeat interval for keepalive |
| `MaxMessageSize` | int | 4194304 | Max message size (4 MB) |
| `DefaultTimeoutMilliseconds` | int | 30000 | Default RPC timeout (30 seconds) |
| `CompressResponses` | bool | true | Enable response compression |
| `CompressionLevel` | int | 6 | gzip compression level (1-9) |
| `AllowedOrigins` | string[] | ["*"] | CORS allowed origins |
| `AllowedMethods` | string[] | ["GET", "POST", ...] | Allowed HTTP methods |

### Authentication Settings

```json
{
  "Authentication": {
    "JwtAudience": "grpc-web-bridge",
    "JwtIssuer": "https://auth.example.com",
    "JwtExpirationMinutes": 60,
    "ApiKeyHeader": "X-API-Key",
    "ValidateTokenSignature": true,
    "ValidateLifetime": true
  }
}
```

### Kestrel Endpoints

```json
{
  "Kestrel": {
    "Endpoints": {
      "http": {
        "Url": "http://0.0.0.0:5000"
      },
      "https": {
        "Url": "https://0.0.0.0:5001",
        "Certificate": {
          "Path": "/etc/ssl/certs/server.pfx",
          "Password": "password"
        }
      }
    }
  }
}
```

### Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "GrpcWebBridge": "Debug"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```

## Authentication

### JWT Bearer Token

```csharp
// 1. Configure JWT in appsettings.json
{
  "Authentication": {
    "JwtIssuer": "https://auth.example.com",
    "JwtAudience": "grpc-web-bridge"
  }
}

// 2. Include token in requests
const metadata = new grpc.Metadata();
metadata.add('authorization', `Bearer ${jwtToken}`);
client.getUser(request, metadata, callback);

// 3. Bridge validates token and extracts claims
// Token is available in AuthenticationContext
```

### API Key

```csharp
// Configure API keys
{
  "Authentication": {
    "ApiKeyHeader": "X-API-Key"
  }
}

// Client includes API key in header
curl -H "X-API-Key: your-api-key" http://localhost:5000/api/users
```

### Custom Authentication Scheme

Implement `IAuthenticationHandler` for custom authentication:

```csharp
public class CustomAuthHandler : IAuthenticationHandler
{
    public Task<AuthenticationContext> AuthenticateAsync(
        HttpContext context)
    {
        // Custom authentication logic
        var token = context.Request.Headers["X-Custom-Token"];
        // Validate and return AuthenticationContext
    }
}
```

## Streaming Guide

### Unary Streaming (Server Response)

Ideal for: Single request, paginated responses

```csharp
public override async Task GetEvents(
    GetEventsRequest request,
    IServerStreamWriter<Event> responseStream,
    ServerCallContext context)
{
    var events = await _eventService.GetAsync(request.Filter);
    foreach (var evt in events)
    {
        await responseStream.WriteAsync(evt);
        // Heartbeat every 10 messages
        if (evt.Index % 10 == 0)
        {
            await Task.Delay(100);
        }
    }
}
```

### Client Streaming (Multiple Requests)

Ideal for: Bulk uploads, log aggregation

```csharp
public override async Task<UploadResponse> UploadBatch(
    IAsyncStreamReader<Item> requestStream,
    ServerCallContext context)
{
    int count = 0;
    await foreach (var item in requestStream.ReadAllAsync(
        context.CancellationToken))
    {
        await _itemService.SaveAsync(item);
        count++;
    }
    return new UploadResponse { Count = count };
}
```

### Bidirectional Streaming (Chat)

Ideal for: Real-time collaboration, live updates

```csharp
public override async Task Chat(
    IAsyncStreamReader<Message> requestStream,
    IServerStreamWriter<Message> responseStream,
    ServerCallContext context)
{
    await foreach (var message in requestStream.ReadAllAsync(
        context.CancellationToken))
    {
        var response = ProcessMessage(message);
        await responseStream.WriteAsync(response);
    }
}
```

### Best Practices

1. **Handle Context Cancellation**: Always check `context.CancellationToken`
2. **Implement Heartbeats**: Send periodic heartbeats for long-running streams
3. **Buffer Management**: Don't accumulate too many messages in memory
4. **Error Recovery**: Gracefully handle network interruptions
5. **Monitoring**: Track stream metrics for debugging

## Deployment

### Docker Deployment

**Dockerfile**:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GrpcWebBridge.dll"]
```

**Docker Compose**:
```yaml
version: '3.8'
services:
  grpc-web-bridge:
    build: .
    ports:
      - "5000:5000"
      - "5001:5001"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:5000;https://+:5001
    volumes:
      - ./certs:/app/certs:ro
```

### Kubernetes Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: grpc-web-bridge
spec:
  replicas: 3
  selector:
    matchLabels:
      app: grpc-web-bridge
  template:
    metadata:
      labels:
        app: grpc-web-bridge
    spec:
      containers:
      - name: bridge
        image: grpc-web-bridge:latest
        ports:
        - containerPort: 5000
          name: http
        - containerPort: 5001
          name: https
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5000
          initialDelaySeconds: 5
          periodSeconds: 5
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

### Production Checklist

- [ ] Enable HTTPS with valid certificates
- [ ] Set `RequireAuthentication: true`
- [ ] Configure JWT issuer and audience
- [ ] Set up centralized logging (Serilog)
- [ ] Enable metrics collection and monitoring
- [ ] Configure rate limiting
- [ ] Set up health check endpoints
- [ ] Enable CORS only for trusted origins
- [ ] Use environment-specific `appsettings.Production.json`
- [ ] Set up log aggregation (ELK, Splunk, Datadog)
- [ ] Monitor performance metrics (latency, errors, throughput)
- [ ] Plan for graceful shutdown
- [ ] Test failover scenarios

## Troubleshooting

### Common Issues

#### "Unable to connect to gRPC service"

**Cause**: Backend service unreachable

```bash
# Check network connectivity
curl -v grpc://backend-service:50051

# Verify DNS resolution
nslookup backend-service

# Check firewall rules
ufw allow 50051
```

**Solution**: Verify backend service is running and accessible

#### "Authentication failed (401)"

**Cause**: Invalid or expired JWT token

```bash
# Debug JWT token
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/services

# Check token expiration
node -e "console.log(JSON.parse(Buffer.from('$TOKEN'.split('.')[1], 'base64').toString()))"
```

**Solution**: Refresh token or use valid credentials

#### "Stream exceeded max message size"

**Cause**: Message larger than configured limit

**Solution**: Increase `MaxMessageSize` in configuration

```json
{
  "GrpcWebBridge": {
    "MaxMessageSize": 8388608
  }
}
```

#### "Rate limit exceeded"

**Cause**: Too many requests in short time

**Solution**: Implement exponential backoff in client

```javascript
async function withRetry(fn, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      return await fn();
    } catch (err) {
      if (err.code !== 'RESOURCE_EXHAUSTED') throw err;
      await new Promise(r => setTimeout(r, Math.pow(2, i) * 1000));
    }
  }
}
```

#### "High memory usage"

**Cause**: Stream memory leaks or large messages

**Solution**:
1. Check `StreamIdleTimeoutSeconds` setting
2. Monitor active streams: `GET /api/streams`
3. Enable stream cleanup worker
4. Review message sizes

#### "CORS errors in browser"

**Cause**: Origin not allowed

**Solution**: Update `AllowedOrigins` in configuration

```json
{
  "GrpcWebBridge": {
    "AllowedOrigins": [
      "https://app.example.com",
      "https://admin.example.com"
    ]
  }
}
```

### Debug Logging

Enable debug logging for troubleshooting:

```json
{
  "Logging": {
    "LogLevel": {
      "GrpcWebBridge": "Debug",
      "GrpcWebBridge.Services": "Debug"
    }
  }
}
```

## Performance

Benchmarks are located in `benchmarks/grpc-web-bridge.Benchmarks/` and use [BenchmarkDotNet](https://benchmarkdotnet.org/) 0.14.

```bash
# Run all benchmarks (Release mode required)
dotnet run --project benchmarks/grpc-web-bridge.Benchmarks -c Release

# Run a specific class
dotnet run --project benchmarks/grpc-web-bridge.Benchmarks -c Release -- --filter "*Protocol*"
```

### Sample results — .NET 10, x64, Linux (Intel Core i7-1260P)

#### Protocol Translation

| Method | Description | Mean | Allocated |
|--------|-------------|-----:|----------:|
| `TranslateMetadata_Small` | 5 headers | 312 ns | 480 B |
| `TranslateMetadata_Large` | 50 headers | 2.81 μs | 4.12 KB |
| `ConvertProtobufToJson_256B` | 256 B payload | 618 ns | 792 B |
| `ConvertJsonToProtobuf_Base64` | base64-wrapped payload | 541 ns | 368 B |
| `TranslateGrpcToHttp_Passthrough` | Protobuf passthrough | 198 ns | 96 B |
| `TranslateGrpcToHttp_Convert` | Protobuf → JSON | 724 ns | 864 B |

#### Stream Processing

| Method | Description | Mean | Allocated |
|--------|-------------|-----:|----------:|
| `ReadStreamToEnd_1KB` | 1 KB stream | 4.2 μs | 1.05 KB |
| `ReadStreamToEnd_64KB` | 64 KB stream | 18.7 μs | 64.1 KB |
| `ReadStreamToEnd_1MB` | 1 MB stream | 287 μs | 1.00 MB |
| `CopyStreamChunked_1KB` | 1 KB copy | 3.8 μs | 0 B* |
| `CopyStreamChunked_64KB` | 64 KB copy | 16.1 μs | 0 B* |
| `CopyStreamChunked_1MB` | 1 MB copy | 261 μs | 0 B* |
| `StreamToBase64_1KB` | 1 KB → Base64 | 9.6 μs | 2.37 KB |

*Buffer rented from `ArrayPool<byte>.Shared` — not counted as managed-heap allocation.

#### Authentication

| Method | Description | Mean | Allocated |
|--------|-------------|-----:|----------:|
| `ExtractBearerToken_Valid` | valid Bearer header | 68 ns | 0 B* |
| `ExtractBearerToken_Invalid` | non-Bearer header | 31 ns | 0 B |
| `ExtractBearerToken_Null` | null header | 8 ns | 0 B |
| `GetCachedContext_Hit` | ConcurrentDictionary hit | 42 ns | 0 B |
| `GetCachedContext_Miss` | ConcurrentDictionary miss | 39 ns | 0 B |
| `AuthenticateApiKey` | full auth path | 1.24 μs | 816 B |
| `ValidateContext` | validate cached context | 94 ns | 0 B |

*Token returned via `ReadOnlySpan<char>.ToString()` — allocation only when token is non-empty.

### Key optimizations

- **`ArrayPool<byte>.Shared`** in `StreamUtility` — temporary I/O buffers are rented and returned rather than heap-allocated per call, eliminating allocations in the stream hot path.
- **`Memory<T>` overloads** — all `ReadAsync`/`WriteAsync` calls use `Memory<byte>` overloads to avoid extra copies through the older `byte[], int, int` API surface.
- **Cached `JsonSerializerOptions`** in `ProtocolTranslationService` — options object is created once at class load; previously a new instance was constructed on every `ConvertProtobufToJson` call.
- **`JsonDocument.Parse(ReadOnlyMemory<byte>)`** — JSON parsing in `ConvertJsonToProtobuf` now works directly on the raw byte buffer, removing the intermediate `UTF-8 → string` allocation.
- **`string.Create` + `Span<char>.ToLowerInvariant`** in `TranslateMetadata` — metadata keys are lowercased in-place inside a single allocated string, removing the extra copy produced by `string.ToLowerInvariant()`.
- **`ConcurrentDictionary`** in `AuthenticationService` — replaces `lock + Dictionary` for the context cache, eliminating the lock contention cost on the read path.
- **`ReadOnlySpan<char>` in `ExtractBearerToken`** — the bearer prefix check and whitespace trim work on a span slice; no substring allocation unless a valid token is found.

## Testing

```bash
# Run the full test suite
dotnet test

# Run with coverage report
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Run a specific project
dotnet test tests/grpc-web-bridge.Tests/

# Run benchmarks (Release mode required)
dotnet run --project benchmarks/grpc-web-bridge.Benchmarks -c Release
```

Tests are organized under `tests/grpc-web-bridge.Tests/` and cover authentication, validation, protocol translation, and stream lifecycle. The `benchmarks/` folder contains BenchmarkDotNet micro-benchmarks for hot paths.

## Related Projects

Part of a collection of .NET libraries and tools. See more at [github.com/sarmkadan](https://github.com/sarmkadan).

### Integration Examples

**Registering the bridge in an ASP.NET Core host:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpcWebBridge(options =>
{
    options.RequireAuthentication = true;
    options.MaxStreamCount = 5000;
    options.AllowedOrigins = new[] { "https://app.example.com" };
});
var app = builder.Build();
app.UseGrpcWebBridge();
app.Run();
```

**Forwarding authentication headers to downstream gRPC services:**

```csharp
public class TokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _ctx;
    public TokenForwardingHandler(IHttpContextAccessor ctx) => _ctx = ctx;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var auth = _ctx.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(auth))
            request.Headers.TryAddWithoutValidation("Authorization", auth);
        return base.SendAsync(request, ct);
    }
}
```

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Setup

```bash
# Clone and setup
git clone https://github.com/sarmkadan/grpc-web-bridge.git
cd grpc-web-bridge

# Build
dotnet build

# Run tests
dotnet test

# Format code
dotnet format

# Static analysis
dotnet analyze
```

### Code Style

- Follow C# naming conventions
- Use nullable reference types (`#nullable enable`)
- Document public APIs with XML comments
- Write unit tests for new features
- Keep methods focused and under 50 lines

## License

MIT License - Copyright (c) 2025 Vladyslav Zaiets

See [LICENSE](LICENSE) file for details.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**

[Portfolio](https://sarmkadan.com) | [GitHub](https://github.com/Sarmkadan) | [Telegram](https://t.me/sarmkadan)
