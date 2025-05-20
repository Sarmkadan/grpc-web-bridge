// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Performance Tuning Guide

Optimize gRPC-Web Bridge for maximum throughput and minimal latency.

## Profiling & Diagnostics

### Enable Performance Counters

```json
{
  "Monitoring": {
    "EnablePerformanceCounters": true,
    "SampleRate": 0.1
  }
}
```

### Measure Baseline Performance

```bash
# Benchmark health endpoint
wrk -t4 -c100 -d30s http://localhost:5000/health

# Benchmark RPC call (100 concurrent connections, 30 seconds)
wrk -t4 -c100 -d30s -s script.lua http://localhost:5000/api/bridge/Service/Method

# Load test with apachebench
ab -n 10000 -c 100 http://localhost:5000/health

# Using hey (modern alternative)
hey -n 10000 -c 100 http://localhost:5000/health
```

### Monitor During Load

```bash
# Watch real-time metrics
watch -n 1 'curl -s http://localhost:5000/api/metrics | jq'

# Monitor system resources
top -p $(pgrep -f "dotnet")
htop
iostat 1 10
vmstat 1 10

# Check network
netstat -an | grep ESTABLISHED | wc -l
ss -s
```

## Configuration Optimization

### Stream Management

**Increase stream count** for high-concurrency scenarios:
```json
{
  "GrpcWebBridge": {
    "MaxStreamCount": 10000
  }
}
```

**Optimize heartbeat interval** (smaller = more overhead, larger = longer detection):
```json
{
  "GrpcWebBridge": {
    "StreamHeartbeatIntervalSeconds": 20
  }
}
```

### Memory Optimization

**Reduce buffer sizes** if memory is constrained:
```json
{
  "GrpcWebBridge": {
    "MaxMessageSize": 2097152
  }
}
```

**Enable cache expiration**:
```json
{
  "GrpcWebBridge": {
    "CacheSettings": {
      "Enabled": true,
      "DefaultTtlSeconds": 60,
      "MaxCacheSize": 500
    }
  }
}
```

### Compression Settings

**Optimize compression level** (1-9, higher = more CPU):
```json
{
  "GrpcWebBridge": {
    "CompressionLevel": 4
  }
}
```

**Compress only large responses** (not in config, but best practice):
- Enable compression for responses > 1KB
- Disable for small responses
- Use streaming for very large responses

### Connection Pooling

**Optimize pool size** (default manages automatically):
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConnections": 1000,
      "MaxRequestBodySize": 4194304
    }
  }
}
```

### Timeout Tuning

**Increase timeout for slow backends**:
```json
{
  "GrpcWebBridge": {
    "DefaultTimeoutMilliseconds": 60000
  }
}
```

**Reduce timeout for quick failure detection**:
```json
{
  "GrpcWebBridge": {
    "DefaultTimeoutMilliseconds": 10000
  }
}
```

## Resource Allocation

### CPU Tuning

```bash
# Set thread pool size
export DOTNET_ThreadPool_MinThreads=50
export DOTNET_ThreadPool_MaxThreads=200

# Pin to specific cores (Linux)
taskset -cp 0-7 $(pgrep -f "dotnet")

# Use performance governor
sudo cpupower frequency-set -g performance
```

### Memory Management

```bash
# Set heap size
export DOTNET_HeapCount=4
export DOTNET_HeapAffinitizeMask=0xFF

# Enable tiered compilation
export DOTNET_TieredCompilation=1
export DOTNET_TieredCompilation_Tier0=100
export DOTNET_TieredCompilation_TierRegex=100
```

### Networking

```bash
# Increase socket buffer sizes (Linux)
sysctl -w net.core.rmem_max=134217728
sysctl -w net.core.wmem_max=134217728
sysctl -w net.ipv4.tcp_rmem="4096 87380 67108864"
sysctl -w net.ipv4.tcp_wmem="4096 65536 67108864"

# Increase backlog
sysctl -w net.core.somaxconn=65535
sysctl -w net.ipv4.tcp_max_syn_backlog=65535
```

## Caching Strategy

### Enable Response Caching

```json
{
  "GrpcWebBridge": {
    "CacheSettings": {
      "Enabled": true,
      "DefaultTtlSeconds": 300,
      "MaxCacheSize": 1000
    }
  }
}
```

### Cache Invalidation

Implement TTL-based expiration:
- Short TTL for frequently-changing data
- Long TTL for stable data
- Manual invalidation for critical updates

### Cache Metrics

Monitor cache effectiveness:
```bash
curl http://localhost:5000/api/metrics | jq '.cacheHitRate'
```

Target: >70% hit rate for optimal performance

## Streaming Optimization

### Server Streaming

**Best for**: Large result sets, real-time updates

```csharp
public override async Task GetEvents(
    GetEventsRequest request,
    IServerStreamWriter<Event> responseStream,
    ServerCallContext context)
{
    // Send messages efficiently
    foreach (var evt in await _service.GetEventsAsync())
    {
        await responseStream.WriteAsync(evt);
    }
}
```

### Buffering Strategy

Implement bounded queue:
```csharp
var channel = Channel.CreateBounded<Message>(
    new BoundedChannelOptions(1000)
    {
        FullMode = BoundedChannelFullMode.Wait
    });

// Backpressure: waits when queue is full
await channel.Writer.WriteAsync(message);
```

### Message Batching

Reduce overhead by batching small messages:
```csharp
var batch = messages
    .Take(100)
    .ToList();

await responseStream.WriteAsync(new BatchedResponse 
{ 
    Messages = batch 
});
```

## Load Balancing

### Multiple Instances

Deploy 3-5 instances behind load balancer:
```yaml
# Example with round-robin
upstream bridge {
    server instance1:5000;
    server instance2:5000;
    server instance3:5000;
}

server {
    location / {
        proxy_pass http://bridge;
    }
}
```

### Session Affinity

For stateful connections, enable sticky sessions (if needed):
```nginx
upstream bridge {
    ip_hash;  # Route same client to same server
    server instance1:5000;
    server instance2:5000;
}
```

## Monitoring Performance

### Key Metrics to Track

| Metric | Healthy | Warning | Critical |
|--------|---------|---------|----------|
| Latency (P95) | <100ms | 100-500ms | >500ms |
| Latency (P99) | <500ms | 500ms-1s | >1s |
| Error Rate | <0.1% | 0.1-1% | >1% |
| CPU Usage | <70% | 70-85% | >85% |
| Memory Usage | <80% | 80-90% | >90% |
| Active Streams | <80% | 80-95% | >95% |
| Cache Hit Rate | >70% | 50-70% | <50% |

### Grafana Dashboards

Create dashboards for:
- Request latency distribution
- Error rate by service
- Resource utilization
- Stream count trends
- Cache effectiveness

### Alerting Rules

Configure alerts for:
- High latency (P95 > 1s)
- High error rate (> 1%)
- High CPU/memory (> 85%)
- Stream count approaching limit
- Cache misses increasing

## Optimization Checklist

- [ ] Compressed responses enabled for large payloads
- [ ] Caching enabled with appropriate TTL
- [ ] Connection pooling configured
- [ ] Appropriate stream limits set
- [ ] Rate limiting enabled
- [ ] Timeouts tuned for expected latency
- [ ] Load balancing implemented
- [ ] Monitoring dashboards created
- [ ] Alert thresholds set
- [ ] Baseline performance measured
- [ ] Peak load tested
- [ ] Graceful degradation verified

## Common Issues & Solutions

### High Latency

**Causes**:
- Backend service slow
- Network issues
- Resource constraints

**Solutions**:
1. Profile backend service
2. Check network latency
3. Increase resources
4. Enable caching

### High Memory Usage

**Causes**:
- Stream accumulation
- Large messages
- Memory leak

**Solutions**:
1. Check for stream leaks
2. Reduce message size
3. Enable compression
4. Implement backpressure

### High CPU Usage

**Causes**:
- High compression level
- Many concurrent streams
- Inefficient code

**Solutions**:
1. Reduce compression level
2. Implement rate limiting
3. Profile hot paths
4. Add resources

### Connection Exhaustion

**Causes**:
- Too many concurrent requests
- Connection leaks
- Connection pool too small

**Solutions**:
1. Enable rate limiting
2. Fix connection leaks
3. Increase pool size
4. Use load balancing

## Performance Targets

Aim for production SLAs:
- **Availability**: 99.9% (monthly)
- **Latency**: <100ms P95
- **Throughput**: 1000+ requests/second
- **Error Rate**: <0.1%
- **Resource Usage**: <85% peak

## Tools for Benchmarking

### Load Testing
- `wrk` - HTTP benchmarking tool
- `hey` - HTTP load generator
- `Apache JMeter` - Full test suite
- `k6` - Modern load testing
- `Locust` - Python-based load testing

### Profiling
- `dotnet trace` - Event tracing
- `dotnet dump` - Memory dumps
- `BenchmarkDotNet` - Microbenchmarks
- Profiler.NET - .NET profiler

### Monitoring
- Prometheus - Metrics collection
- Grafana - Visualization
- Jaeger - Distributed tracing
- ELK Stack - Log aggregation

## Further Reading

- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/performance)
- [gRPC Performance](https://grpc.io/docs/guides/performance-best-practices/)
- [Async/Await Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
