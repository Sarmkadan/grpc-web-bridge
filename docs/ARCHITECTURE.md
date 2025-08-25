// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# gRPC-Web Bridge Architecture

Comprehensive guide to the internal architecture and design of gRPC-Web Bridge.

## System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Request Flow                              │
└─────────────────────────────────────────────────────────────────┘

Client (gRPC-Web)
        │
        ▼ HTTP/1.1 + gRPC-Web Format
┌─────────────────────────────────────────────────────────────────┐
│ ASP.NET Core Pipeline                                           │
│  1. ErrorHandlingMiddleware       (Exception catching)          │
│  2. RequestLoggingMiddleware       (Request logging)            │
│  3. RateLimitingMiddleware         (Rate limit checks)          │
│  4. CORS Middleware                (Cross-origin support)       │
│  5. Authentication Middleware      (Token validation)           │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼ Validated Request
┌─────────────────────────────────────────────────────────────────┐
│ BridgeController                                                │
│  - Parse request headers and body                               │
│  - Extract service and method name                              │
│  - Validate parameters                                          │
│  - Route to ProtocolTranslationService                          │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼ Protocol Translation
┌─────────────────────────────────────────────────────────────────┐
│ ProtocolTranslationService                                      │
│  - Deserialize gRPC-Web message                                 │
│  - Convert to gRPC protocol                                     │
│  - Add service routing metadata                                 │
│  - Pass to StreamingService                                     │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼ Streaming Management
┌─────────────────────────────────────────────────────────────────┐
│ StreamingService                                                │
│  - Create stream context                                        │
│  - Set up message buffering                                     │
│  - Configure timeout/heartbeat                                  │
│  - Connect to GrpcConnectionManager                             │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼ Connection Management
┌─────────────────────────────────────────────────────────────────┐
│ GrpcConnectionManager                                           │
│  - Check connection pool                                        │
│  - Reuse or create gRPC channel                                 │
│  - Execute backend RPC call                                     │
│  - Return response/stream                                       │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼ Backend gRPC Service
        Backend Service (gRPC)
        │
        ▼ Response
┌─────────────────────────────────────────────────────────────────┐
│ StreamingService (Response)                                     │
│  - Buffer response messages                                     │
│  - Serialize to gRPC-Web format                                 │
│  - Compress if needed                                           │
│  - Stream to client                                             │
└─────────────────────────────────────────────────────────────────┘
        │
        ▼ HTTP/1.1 Response
Client receives gRPC-Web response
```

## Layer Architecture

### 1. **Presentation Layer** (Controllers)

Responsibility: Handle HTTP requests and responses

**Components**:
- `BridgeController` - Main RPC routing
- `HealthCheckController` - Health/readiness probes
- `MetricsController` - Performance metrics
- `ConfigurationController` - Dynamic configuration

**Key Methods**:
```csharp
[HttpPost("/{service}/{method}")]
public async Task<IActionResult> RouteGrpcCall(
    string service, string method, [FromBody] object request)
```

**Patterns**:
- RESTful routing convention
- Automatic content negotiation
- Custom formatter support (JSON, CSV, XML)

### 2. **Middleware Layer**

Responsibility: Cross-cutting concerns

**Stack Order** (top to bottom):
1. **ErrorHandlingMiddleware** - Exception to response conversion
2. **RequestLoggingMiddleware** - Structured request/response logging
3. **RateLimitingMiddleware** - Per-client request rate limiting
4. **CORS** - Cross-origin validation
5. **Authentication** - Token/API key validation
6. **Authorization** - Role-based access control

**Key Features**:
- Async execution pipeline
- Exception aggregation
- Context propagation
- Performance monitoring

### 3. **Service Layer** (Business Logic)

#### ProtocolTranslationService

Converts between gRPC and gRPC-Web formats.

**Responsibilities**:
- Parse gRPC-Web request format
- Deserialize message payload
- Convert to native gRPC message
- Serialize response back to gRPC-Web
- Handle compression (gzip/deflate)

**Message Flow**:
```
gRPC-Web Request
    ↓ (HTTP binary frame)
ProtobufUtility.Deserialize
    ↓
GrpcRequest object
    ↓ (native gRPC)
Backend Service
    ↓
GrpcResponse object
    ↓
ResponseFormatter (JSON/CSV/XML)
    ↓ (HTTP response)
gRPC-Web Response
```

#### StreamingService

Manages stream lifecycle and message buffering.

**Stream Types**:
1. **Unary**: Single request, single response
2. **ServerStreaming**: Single request, multiple responses
3. **ClientStreaming**: Multiple requests, single response
4. **BidirectionalStreaming**: Multiple requests, multiple responses

**Key Features**:
- In-memory message queue (bounded)
- Heartbeat generation for idle streams
- Automatic cleanup on timeout
- Backpressure handling
- Error state propagation

**Stream State Machine**:
```
Created
   ↓ (Request received)
Active
   ├─→ Idle (no activity)
   │    └─→ Heartbeat
   │         └─→ Active (if reset)
   │         └─→ Closed (if timeout)
   ├─→ Error (exception)
   │    └─→ Closed (send error)
   └─→ Completed (all messages sent)
        └─→ Closed
```

#### AuthenticationService

Validates and processes security credentials.

**Supported Methods**:
1. **JWT Bearer Token**
   - Parse Authorization header
   - Validate signature
   - Check expiration
   - Extract claims

2. **API Key**
   - Extract from header
   - Lookup in keystore
   - Validate permissions

3. **Custom Schemes**
   - Pluggable handlers
   - Claim-based authorization

**AuthenticationContext**:
```csharp
public class AuthenticationContext
{
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string[]? Roles { get; set; }
    public Dictionary<string, object>? Claims { get; set; }
    public bool IsAuthenticated { get; set; }
}
```

#### ServiceRegistry

Dynamic service discovery and registration.

**Features**:
- Manual service registration
- Health check monitoring
- Metadata caching
- Instance tracking
- Load balancing ready

**Service Model**:
```csharp
public class GrpcService
{
    public string ServiceName { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }
    public ServiceStatus Status { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

### 4. **Data Access Layer**

#### GrpcConnectionManager

Connection pooling and lifecycle management.

**Features**:
- Channel pooling (per-service)
- Connection reuse
- Graceful shutdown
- Connection health monitoring

**Connection Pool Strategy**:
```
Service Address
    ↓
Pool Lookup
    ├─→ Found: Reuse channel
    ├─→ Not found: Create new
    │    └─→ Add to pool
    ├─→ Unhealthy: Replace
    └─→ Idle timeout: Close
```

#### ServiceRepository

Service metadata persistence.

**Operations**:
- Register service
- Update service status
- Query services
- Delete service
- Get service by ID

### 5. **Cross-cutting Concerns**

#### Caching

**Cache Manager**:
- In-memory caching
- Cache invalidation
- TTL management
- Hit/miss tracking

**Cached Items**:
- Service metadata
- JWT token claims
- Compression buffers
- Response templates

#### Logging

**Serilog Integration**:
- Structured logging with properties
- Request/response correlation
- Performance metrics
- Error tracing

**Log Levels**:
- **Debug**: Detailed flow, parameters
- **Information**: Key events, service registration
- **Warning**: Recoverable errors, timeouts
- **Error**: Failures, exceptions
- **Critical**: System failures

#### Metrics

**Collected Metrics**:
- Request count, latency, errors
- Stream count, message throughput
- Cache hit rate, compression ratio
- Connection pool utilization
- Memory usage

## Domain Models

### Request Models

**GrpcRequest**:
```csharp
public class GrpcRequest
{
    public string ServiceName { get; set; }
    public string MethodName { get; set; }
    public byte[] Payload { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public CompressionType? Compression { get; set; }
    public TimeSpan Timeout { get; set; }
}
```

**MethodParameter**:
```csharp
public class MethodParameter
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
}
```

### Response Models

**GrpcResponse**:
```csharp
public class GrpcResponse
{
    public byte[] Payload { get; set; }
    public int StatusCode { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string? ErrorMessage { get; set; }
    public CompressionType? Compression { get; set; }
}
```

**StreamMessage**:
```csharp
public class StreamMessage
{
    public string StreamId { get; set; }
    public byte[] Content { get; set; }
    public int SequenceNumber { get; set; }
    public bool IsLast { get; set; }
    public DateTime Timestamp { get; set; }
}
```

## Extension Points

### 1. Custom Middleware

Add custom middleware for specialized concerns:

```csharp
public class CustomMiddleware
{
    private readonly RequestDelegate _next;
    
    public CustomMiddleware(RequestDelegate next) => _next = next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Pre-processing
        await _next(context);
        // Post-processing
    }
}

// Register in Program.cs
app.UseMiddleware<CustomMiddleware>();
```

### 2. Custom Authentication

Implement `IAuthenticationHandler`:

```csharp
public class CustomAuthHandler : IAuthenticationHandler
{
    public Task<AuthenticationContext> AuthenticateAsync(
        HttpContext context)
    {
        // Custom logic
        return Task.FromResult(new AuthenticationContext { ... });
    }
}
```

### 3. Custom Formatters

Add response formatters:

```csharp
public class CustomFormatter : IResponseFormatter
{
    public string ContentType => "application/custom";
    
    public byte[] Format(GrpcResponse response)
    {
        // Custom serialization
        return CustomSerialize(response);
    }
}
```

### 4. Custom Event Handlers

Subscribe to bridge events:

```csharp
public class EventBus
{
    public event EventHandler<RequestProcessedEventArgs>? RequestProcessed;
    public event EventHandler<StreamClosedEventArgs>? StreamClosed;
}
```

## Performance Considerations

### Memory Management

- Bounded message queues (prevent unbounded growth)
- Stream pooling to reduce allocations
- Zero-copy where possible
- Automatic cleanup of idle streams

### Connection Pooling

- Reuse gRPC channels (expensive to create)
- Per-service channel pool
- Health-based eviction
- Connection timeout management

### Compression

- Configurable compression level
- Automatic format detection
- Deferred compression (only for large payloads)
- Cache compressed responses

### Request Handling

- Async/await throughout
- Connection pooling
- Request batching support
- Timeout management

## Security Architecture

### Authentication Flow

```
Request → Extract Credentials → Validate → Create AuthContext → Route
              ↓                    ↓
          JWT Extractor      Signature Check
          API Key Extractor  Expiration Check
          Custom Handler     Permission Check
```

### Authorization

- Role-based access control (RBAC)
- Claim-based authorization
- Per-method authorization attributes
- Service-level authorization policies

### Data Protection

- TLS for transport (HTTPS)
- Message encryption ready
- Secure credential handling
- CORS validation

## Concurrency Model

- Fully async/await based
- No blocking operations
- Lock-free where possible
- Bounded task concurrency
- Graceful degradation under load

## Failure Modes

### Graceful Degradation

1. **Service Unavailable**: Return 503 with retry info
2. **Rate Limited**: Return 429 with backoff guidance
3. **Timeout**: Abort stream, return timeout error
4. **Authentication Failure**: Return 401/403
5. **Protocol Error**: Return 400 with details

### Recovery Strategies

- Auto-reconnect for connections
- Request retry with exponential backoff
- Circuit breaker for failing services
- Graceful shutdown with drain period

## Monitoring Integration

### Metrics Export Ready

- Prometheus-compatible metrics
- Custom metric collection
- Performance baselines
- Trend analysis support

### Tracing Support

- Request correlation IDs
- Distributed tracing headers (W3C)
- Span generation per operation
- Service dependency tracking

## Future Enhancements

1. **gRPC Load Balancing**: Client-side load balancing
2. **Circuit Breaker**: Automatic failure handling
3. **Caching Layer**: Response caching strategies
4. **API Versioning**: Service version management
5. **Webhooks**: Event-driven integrations
6. **GraphQL Support**: GraphQL to gRPC translation
