// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Frequently Asked Questions

Quick answers to common questions about gRPC-Web Bridge.

## General Questions

### Q: What is gRPC-Web and why do I need a bridge?

**A:** gRPC-Web is a protocol that allows web browsers to call gRPC services. Browsers don't support HTTP/2, which gRPC requires, so gRPC-Web uses HTTP/1.1 instead. The bridge translates between these protocols:
- Clients (browsers, mobile) use gRPC-Web over HTTP/1.1
- Backend services use native gRPC over HTTP/2
- The bridge in between translates both directions

### Q: Can I use this with my existing gRPC services?

**A:** Yes! The bridge works with any standard gRPC service. No changes to your backend needed.

### Q: What's the performance overhead?

**A:** Minimal. The bridge is optimized for throughput:
- Connection pooling reduces gRPC channel creation
- Message buffering is bounded
- Compression is optional and configurable
- Typical overhead: 1-5% latency

### Q: Does it work with gRPC services in different languages?

**A:** Yes. The bridge works with gRPC services written in any language (.NET, Go, Java, Python, etc.).

### Q: Can I use this for service-to-service communication?

**A:** The bridge is optimized for client-to-service. For service-to-service, use direct gRPC connections for better performance.

## Installation & Setup

### Q: What .NET versions are supported?

**A:** .NET 10 (latest). The bridge uses modern .NET features and depends on ASP.NET Core 10.

### Q: Can I run this on .NET 8 or 9?

**A:** No. The project requires .NET 10 for C# 14 features. If you need older .NET support, you could fork and adapt, but we maintain only .NET 10.

### Q: How do I install dependencies?

**A:** Just run `dotnet restore`. NuGet packages are automatically downloaded.

### Q: Can I self-host without Docker?

**A:** Yes. Use `dotnet run` or `dotnet publish` to create a standalone executable for any platform.

### Q: What ports does it use by default?

**A:** 
- HTTP: 5000
- HTTPS: 5001
Configure in `appsettings.json` under `Kestrel.Endpoints`

## Configuration

### Q: How do I configure multiple backend services?

**A:** Register each service via the configuration endpoint:
```bash
curl -X POST http://localhost:5000/api/services/register \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName": "UserService",
    "address": "grpc://user-service:50051"
  }'
```

### Q: Can I add services at runtime without restart?

**A:** Yes! Use the `/api/services/register` endpoint. Services are registered dynamically.

### Q: How do I secure my configuration?

**A:** Use environment variables:
```bash
export GrpcWebBridge__RequireAuthentication=true
export GrpcWebBridge__MaxStreamCount=1000
dotnet run
```

### Q: What's the maximum message size?

**A:** Default 4 MB. Increase with:
```json
{
  "GrpcWebBridge": {
    "MaxMessageSize": 8388608
  }
}
```

### Q: Can I enable compression?

**A:** Yes. It's enabled by default:
```json
{
  "GrpcWebBridge": {
    "CompressResponses": true,
    "CompressionLevel": 6
  }
}
```

## Authentication & Security

### Q: How do I enable authentication?

**A:** Set `RequireAuthentication: true` in config, then configure JWT or API keys.

### Q: How do I use JWT tokens?

**A:** 
1. Configure JWT issuer and audience
2. Include token in Authorization header: `Authorization: Bearer <token>`
3. Bridge validates token signature and expiration

### Q: What JWT algorithms are supported?

**A:** HS256, RS256, ES256, and other standard algorithms. Algorithm is determined by the token's `alg` header.

### Q: Can I use API keys instead of JWT?

**A:** Yes. Configure API keys in your settings or implement a custom handler.

### Q: How do I implement custom authentication?

**A:** Implement `IAuthenticationHandler` and register it in dependency injection.

### Q: Is HTTPS required?

**A:** Recommended for production. Configure certificates in `appsettings.json`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "https": {
        "Url": "https://0.0.0.0:5001",
        "Certificate": {
          "Path": "/path/to/cert.pfx",
          "Password": "password"
        }
      }
    }
  }
}
```

## Streaming

### Q: What streaming modes are supported?

**A:**
1. **Unary**: Single request, single response (standard RPC)
2. **Server Streaming**: Single request, multiple responses
3. **Client Streaming**: Multiple requests, single response
4. **Bidirectional**: Multiple requests, multiple responses

All are fully supported.

### Q: How do I handle stream errors?

**A:** The client receives errors on the stream:
```javascript
stream.on('error', (err) => {
  console.error('Stream error:', err.code, err.message);
  // Implement retry logic
});
```

### Q: What happens if a stream is idle?

**A:** Idle streams are closed after `StreamIdleTimeoutSeconds` (default 300s). Heartbeats are sent every `StreamHeartbeatIntervalSeconds` (default 30s) to keep streams alive.

### Q: Can I set per-request timeouts?

**A:** Use the default timeout in config, or extend via metadata headers in requests.

### Q: How do I handle large file uploads?

**A:** Use client streaming with appropriate `MaxMessageSize`:
```csharp
public override async Task<UploadResponse> UploadFile(
    IAsyncStreamReader<FileChunk> requestStream,
    ServerCallContext context)
{
    long totalBytes = 0;
    await foreach (var chunk in requestStream.ReadAllAsync())
    {
        totalBytes += chunk.Data.Length;
        // Process chunk
    }
    return new UploadResponse { BytesReceived = totalBytes };
}
```

## Deployment

### Q: Should I use Docker or native installation?

**A:** Docker is recommended for production. It ensures consistency across environments.

### Q: How do I scale horizontally?

**A:** Run multiple bridge instances behind a load balancer. Each instance maintains its own connection pool to backend services.

### Q: Does the bridge maintain state?

**A:** No persistent state. It's stateless and horizontally scalable. Stream state is in-memory and lost on restart.

### Q: Can I deploy on Kubernetes?

**A:** Yes. See deployment guide for complete K8s manifests with health checks and autoscaling.

### Q: What are the resource requirements?

**A:** 
- Minimum: 256 MB RAM, 250m CPU
- Recommended: 512 MB RAM, 500m CPU
- For high throughput: 1 GB+ RAM, multiple CPUs

### Q: How do I monitor the bridge?

**A:** 
- Health endpoint: `GET /health`
- Metrics endpoint: `GET /api/metrics`
- Logs via Serilog (console, file, external services)
- Integrate with Prometheus/Grafana

## Debugging

### Q: How do I enable debug logging?

**A:** Update `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "GrpcWebBridge": "Debug"
    }
  }
}
```

### Q: How do I see request details?

**A:** Enable request logging middleware - it logs all incoming requests and responses with headers and body size.

### Q: Can I trace requests across services?

**A:** The bridge uses correlation IDs. Track via `X-Correlation-ID` header.

### Q: What do error codes mean?

**A:** Standard HTTP/gRPC codes:
- 200: Success
- 400: Bad request (invalid parameters)
- 401: Unauthenticated (no token)
- 403: Forbidden (insufficient permissions)
- 408: Timeout
- 429: Rate limited
- 500: Internal server error
- 503: Service unavailable

## Performance

### Q: How many concurrent streams can it handle?

**A:** Configured by `MaxStreamCount` (default 10,000). Actual limit depends on available memory and CPU.

### Q: What's the latency overhead?

**A:** Typically 1-5ms for protocol translation, plus gRPC round-trip time.

### Q: Should I enable compression for all responses?

**A:** Only for responses > ~1KB. Compression has CPU overhead. It's automatic and configurable per request.

### Q: How do I optimize for high throughput?

**A:**
1. Increase `MaxStreamCount`
2. Tune buffer sizes
3. Use connection pooling
4. Enable compression for large messages
5. Run multiple bridge instances with load balancing

## Troubleshooting

### Q: How do I fix "connection refused" errors?

**A:** 
1. Verify backend service is running
2. Check firewall rules
3. Verify service address in configuration
4. Check network connectivity

### Q: Why are my requests timing out?

**A:**
1. Check backend service performance
2. Increase `DefaultTimeoutMilliseconds` if needed
3. Check network latency
4. Monitor backend service logs

### Q: How do I diagnose memory leaks?

**A:** 
1. Monitor memory usage via metrics endpoint
2. Check for idle streams: `GET /api/streams`
3. Enable debug logging
4. Check for unhandled exceptions
5. Review stream cleanup logs

### Q: Why is compression not working?

**A:**
1. Verify `CompressResponses: true`
2. Check message size (compression overhead for small messages)
3. Verify client supports gzip
4. Check `CompressionLevel` setting

## Advanced Questions

### Q: Can I add custom business logic?

**A:** The architecture supports extension points:
- Custom middleware
- Custom formatters
- Custom authentication handlers
- Event subscriptions

### Q: Can I use this with WebSockets?

**A:** No. gRPC-Web uses HTTP/1.1 streaming, not WebSockets.

### Q: Can I cache responses?

**A:** Yes. The bridge includes a cache manager. Configuration available for caching service responses.

### Q: Can I publish events from the bridge?

**A:** Yes. Use the EventBus for internal events, or configure webhook publishing for external events.

### Q: Can I integrate with observability tools?

**A:** Yes. The bridge exports metrics and supports distributed tracing via correlation IDs. Integrate with Prometheus, Jaeger, Datadog, etc.

## Contributing & Support

### Q: How do I report bugs?

**A:** Open an issue on GitHub with:
- .NET version
- Bridge version
- Configuration (sanitized)
- Steps to reproduce
- Error logs

### Q: How do I request features?

**A:** Open a GitHub discussion or issue describing the use case and desired behavior.

### Q: Can I contribute code?

**A:** Yes! Fork the repo, follow the contribution guidelines, and submit a PR.

### Q: What's the support policy?

**A:** The project is maintained by Vladyslav Zaiets. Community contributions are welcome.

## License & Legal

### Q: What license is this under?

**A:** MIT License. See LICENSE file for details.

### Q: Can I use this commercially?

**A:** Yes. MIT license permits commercial use.

### Q: Can I modify and redistribute?

**A:** Yes, with attribution per the MIT license.

### Q: What about dependencies?

**A:** All dependencies are compatible with commercial use. Check their licenses if concerned.
