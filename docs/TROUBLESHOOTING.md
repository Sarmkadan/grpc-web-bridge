// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Troubleshooting Guide

Comprehensive troubleshooting guide for common issues with gRPC-Web Bridge.

## Table of Contents

- [Startup Issues](#startup-issues)
- [Connectivity Issues](#connectivity-issues)
- [Authentication Issues](#authentication-issues)
- [Streaming Issues](#streaming-issues)
- [Performance Issues](#performance-issues)
- [Deployment Issues](#deployment-issues)
- [Debugging Techniques](#debugging-techniques)

## Startup Issues

### Bridge won't start on ports 5000/5001

**Symptoms**: Port already in use error

**Diagnosis**:
```bash
# Check what's using the ports
lsof -i :5000
lsof -i :5001
```

**Solutions**:
1. Use different ports in appsettings.json:
```json
{
  "Kestrel": {
    "Endpoints": {
      "http": {"Url": "http://0.0.0.0:6000"},
      "https": {"Url": "https://0.0.0.0:6001"}
    }
  }
}
```

2. Stop the process using the port:
```bash
kill -9 <PID>
```

3. Use environment variables:
```bash
ASPNETCORE_URLS=http://0.0.0.0:6000 dotnet run
```

### .NET SDK not found

**Symptoms**: "dotnet: command not found"

**Solution**:
```bash
# Verify .NET is installed
dotnet --version

# Install .NET 10 if needed
# Visit: https://dotnet.microsoft.com/download/dotnet/10.0

# Or use:
curl https://dot.net/v1/dotnet-install.sh | bash
```

### SSL/TLS certificate errors

**Symptoms**: Certificate validation failed, HTTPS won't start

**Diagnosis**:
```bash
# Check certificate file exists
ls -la /path/to/certificate.pfx

# Check certificate expiration
openssl pkcs12 -in certificate.pfx -passin pass:password \
  -noout -info -nokeys | grep "subject=\|issuer=\|notAfter="
```

**Solutions**:
1. For development, use ASPNET_ENVIRONMENT=Development
2. Generate a self-signed certificate:
```bash
openssl genrsa -out server.key 2048
openssl req -new -x509 -key server.key -out server.crt -days 365
openssl pkcs12 -export -out server.pfx -inkey server.key -in server.crt
```

3. In development, disable certificate validation:
```csharp
if (env.IsDevelopment())
{
    handler.ServerCertificateCustomValidationCallback = 
        (message, cert, chain, errors) => true;
}
```

## Connectivity Issues

### Can't connect to backend service

**Symptoms**: "Unable to connect to gRPC service", Connection refused

**Diagnosis**:
```bash
# Test network connectivity
curl -v grpc://backend-service:50051

# Check DNS resolution
nslookup backend-service
dig backend-service

# Test with netcat
nc -zv backend-service 50051

# Check firewall
sudo ufw status
sudo iptables -L -n
```

**Solutions**:
1. Verify backend service is running:
```bash
# SSH into backend host
ssh user@backend-service

# Check if gRPC port is listening
netstat -tlnp | grep 50051
```

2. Check network connectivity:
```bash
# From bridge host
ping backend-service
traceroute backend-service
```

3. Check firewall rules:
```bash
# Allow port in firewall
sudo ufw allow 50051
```

4. Verify service address in registration:
```bash
curl http://localhost:5000/api/services | jq '.services[] | {name, address}'
```

### CORS errors in browser

**Symptoms**: Browser blocks request with CORS error

**Error Example**:
```
Access to XMLHttpRequest at 'http://localhost:5000/api/...' 
from origin 'http://localhost:3000' has been blocked by CORS policy
```

**Solutions**:
1. Update allowed origins in appsettings.json:
```json
{
  "GrpcWebBridge": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://app.example.com"
    ]
  }
}
```

2. For development, allow all origins (NOT for production):
```json
{
  "GrpcWebBridge": {
    "AllowedOrigins": ["*"]
  }
}
```

3. Test CORS without browser (to isolate the issue):
```bash
curl -X POST http://localhost:5000/api/bridge/Service/Method \
  -H "Content-Type: application/json" \
  -d '{}' \
  -v
```

### DNS resolution failures

**Symptoms**: "Unable to resolve hostname"

**Diagnosis**:
```bash
# Test DNS
nslookup grpc-service.default.svc.cluster.local  # K8s
nslookup grpc-service.example.com                 # External

# Check /etc/resolv.conf
cat /etc/resolv.conf
```

**Solutions**:
1. Use IP address instead of hostname (temporary):
```bash
curl -X POST http://localhost:5000/api/services/register \
  -d '{"address": "grpc://192.168.1.100:50051"}'
```

2. Fix DNS (permanent):
```bash
# Add to /etc/hosts for local testing
echo "192.168.1.100 grpc-service" | sudo tee -a /etc/hosts

# Or configure DNS server
sudo nano /etc/resolv.conf
```

## Authentication Issues

### "Unauthorized (401)" errors

**Symptoms**: Authentication failed, invalid token

**Diagnosis**:
```bash
# Check token presence
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5000/api/services

# Decode JWT token
node -e "console.log(JSON.parse(Buffer.from('$TOKEN'.split('.')[1], 'base64').toString()))"
```

**Solutions**:
1. Verify token is valid:
```bash
# Get new token from auth provider
AUTH_TOKEN=$(curl -X POST https://auth.example.com/token \
  -d 'grant_type=client_credentials&client_id=...&client_secret=...' \
  -s | jq -r '.access_token')

# Use token in requests
curl -H "Authorization: Bearer $AUTH_TOKEN" http://localhost:5000/api/services
```

2. Check token expiration:
```bash
# Token expires in this many seconds
jwt_exp=$(node -e "console.log(\$(node -e \"console.log(JSON.parse(Buffer.from('$TOKEN'.split('.')[1], 'base64').toString()).exp)\")*1000)")
current_time=$(date +%s000)
echo "Token expired: $(node -e \"console.log($jwt_exp < $current_time)\")"
```

### "Forbidden (403)" errors

**Symptoms**: Authenticated but insufficient permissions

**Solutions**:
1. Check user roles/permissions:
```bash
# Decode token to see claims
node -e "console.log(JSON.parse(Buffer.from('$TOKEN'.split('.')[1], 'base64').toString()))"
```

2. Verify required roles are present
3. Check authorization policies in configuration

### API key not working

**Symptoms**: 401/403 with API key

**Diagnosis**:
```bash
# Test with API key
curl -H "X-API-Key: your-key" http://localhost:5000/api/services

# Check configuration
curl http://localhost:5000/api/configuration | \
  jq '.authentication.apiKeyHeader'
```

**Solutions**:
1. Verify API key header name matches configuration
2. Check API key is registered in system
3. Ensure API key has required permissions

## Streaming Issues

### Streams timeout immediately

**Symptoms**: Stream closes after starting

**Diagnosis**:
```bash
# Check stream idle timeout
curl http://localhost:5000/api/configuration | \
  jq '.streamIdleTimeoutSeconds'

# Monitor stream state
watch -n 1 'curl -s http://localhost:5000/api/streams | jq ".activeStreams | length"'
```

**Solutions**:
1. Increase idle timeout:
```json
{
  "GrpcWebBridge": {
    "StreamIdleTimeoutSeconds": 600
  }
}
```

2. Ensure heartbeats are being sent:
```json
{
  "GrpcWebBridge": {
    "StreamHeartbeatIntervalSeconds": 15
  }
}
```

### Stream message ordering issues

**Symptoms**: Messages arrive out of order

**Diagnosis**:
- Check client logs for message sequence numbers
- Verify backend service sends in order

**Solutions**:
1. Implementation preserves order - issue likely in client
2. Enable debug logging to track message flow:
```json
{
  "Logging": {
    "LogLevel": {
      "GrpcWebBridge.Services.StreamingService": "Debug"
    }
  }
}
```

### Stream buffer overflow

**Symptoms**: "Stream buffer exceeded"

**Solutions**:
1. Increase max message size:
```json
{
  "GrpcWebBridge": {
    "MaxMessageSize": 8388608
  }
}
```

2. Implement client-side buffering/backpressure
3. Reduce message frequency if possible

## Performance Issues

### High CPU usage

**Symptoms**: CPU utilization very high (>90%)

**Diagnosis**:
```bash
# Monitor CPU per process
top -p $(pgrep -f "dotnet GrpcWebBridge")

# Check active streams count
curl http://localhost:5000/api/metrics | jq '.activeStreams'

# Check request latency
curl http://localhost:5000/api/metrics | jq '.p99LatencyMs'
```

**Solutions**:
1. Reduce active streams limit:
```json
{
  "GrpcWebBridge": {
    "MaxStreamCount": 1000
  }
}
```

2. Enable rate limiting:
```json
{
  "RateLimiting": {
    "Enabled": true,
    "RequestsPerSecond": 100
  }
}
```

3. Add more CPU resources
4. Scale horizontally with load balancing

### High memory usage

**Symptoms**: Memory usage grows unbounded

**Diagnosis**:
```bash
# Monitor memory
watch -n 5 'free -h && echo "---" && ps aux | grep dotnet'

# Check for stream leaks
curl http://localhost:5000/api/streams | jq '.activeStreams | length'
```

**Solutions**:
1. Check for idle streams that aren't timing out:
```json
{
  "GrpcWebBridge": {
    "StreamIdleTimeoutSeconds": 300
  }
}
```

2. Monitor and cleanup orphaned streams
3. Increase available memory
4. Check backend services for leaks

### Slow response times

**Symptoms**: High latency, slow requests

**Diagnosis**:
```bash
# Check backend service latency
time curl -X POST http://localhost:5000/api/bridge/Service/Method \
  -d '{}' \
  -s > /dev/null

# Check metrics
curl http://localhost:5000/api/metrics | \
  jq '{p50: .p50LatencyMs, p95: .p95LatencyMs, p99: .p99LatencyMs}'
```

**Solutions**:
1. Check backend service performance
2. Enable compression for large responses:
```json
{
  "GrpcWebBridge": {
    "CompressResponses": true
  }
}
```

3. Increase connection pool size
4. Check network latency to backend

## Deployment Issues

### Docker container won't start

**Symptoms**: Container exits immediately

**Diagnosis**:
```bash
# Check Docker logs
docker logs grpc-web-bridge

# Check Docker build output
docker build -t test . --progress=plain

# Inspect image
docker inspect grpc-web-bridge:latest
```

**Solutions**:
1. Verify base image exists:
```bash
docker pull mcr.microsoft.com/dotnet/aspnet:10.0
```

2. Check Dockerfile syntax
3. Ensure all dependencies are included

### Kubernetes pod won't start

**Symptoms**: Pod stuck in Pending, CrashLoopBackOff

**Diagnosis**:
```bash
# Check pod status
kubectl describe pod grpc-web-bridge-xxx -n grpc-bridge

# Check logs
kubectl logs grpc-web-bridge-xxx -n grpc-bridge
kubectl logs grpc-web-bridge-xxx -n grpc-bridge --previous

# Check resource availability
kubectl top nodes
kubectl top pods -n grpc-bridge
```

**Solutions**:
1. Increase resource requests if nodes are full
2. Check volume mounts exist
3. Verify secrets/configmaps exist:
```bash
kubectl get secrets -n grpc-bridge
kubectl get configmaps -n grpc-bridge
```

## Debugging Techniques

### Enable debug logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Warning"
    }
  }
}
```

### Request logging with curl

```bash
# Verbose output with headers
curl -v -X POST http://localhost:5000/api/services \
  -H "Content-Type: application/json" \
  -d '...'

# Save request and response
curl -D headers.txt -X POST http://localhost:5000/api/services \
  -H "Content-Type: application/json" \
  -d '...' -o response.json
```

### Monitor in real-time

```bash
# Watch metrics
watch -n 1 'curl -s http://localhost:5000/api/metrics | jq'

# Watch streams
watch -n 1 'curl -s http://localhost:5000/api/streams | jq ".activeStreams | length"'

# Watch logs
tail -f /var/log/grpc-web-bridge/application*.log
```

### Performance profiling

```bash
# Enable profiling (if available)
export DOTNET_PerfMapEnabled=1
export DOTNET_StackSizeBytes=1048576

# Run with profiling
dotnet run
```

### Distributed tracing

Enable correlation IDs:
```bash
curl -X POST http://localhost:5000/api/bridge/Service/Method \
  -H "X-Correlation-ID: trace-12345" \
  -d '{}'
```

## Getting Help

If you can't resolve the issue:

1. **Check the logs**: Enable debug logging and review
2. **Review configuration**: Verify appsettings.json is correct
3. **Test connectivity**: Verify all services are reachable
4. **Search existing issues**: GitHub issues may have solutions
5. **Create a detailed issue**: Include logs, configuration (sanitized), and reproduction steps

## Additional Resources

- [FAQ](FAQ.md)
- [Deployment Guide](DEPLOYMENT.md)
- [Architecture Guide](ARCHITECTURE.md)
- [GitHub Issues](https://github.com/sarmkadan/grpc-web-bridge/issues)
