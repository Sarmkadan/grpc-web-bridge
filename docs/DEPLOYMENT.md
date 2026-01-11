// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Deployment Guide for gRPC-Web Bridge

Production deployment strategies for gRPC-Web Bridge across different environments.

## Pre-Deployment Checklist

### Security

- [ ] HTTPS enabled with valid certificates
- [ ] JWT secret keys configured
- [ ] API keys generated and stored securely
- [ ] CORS origins restricted to known domains
- [ ] Authentication enabled for all sensitive endpoints
- [ ] Rate limiting configured
- [ ] Firewall rules in place

### Configuration

- [ ] Environment set to "Production"
- [ ] Logging level set appropriately
- [ ] Metrics enabled for monitoring
- [ ] Health check endpoints accessible
- [ ] Backend service addresses verified
- [ ] Timeout values tuned
- [ ] Max stream count appropriate for workload

### Infrastructure

- [ ] Resource allocation (CPU, memory)
- [ ] Networking (IP, DNS, load balancer)
- [ ] Storage (if using persistent cache)
- [ ] Monitoring tools installed
- [ ] Log aggregation configured
- [ ] Backup strategy in place

### Testing

- [ ] Load testing completed
- [ ] Failover testing done
- [ ] Security testing passed
- [ ] Integration testing with backends
- [ ] Client compatibility verified

## Docker Deployment

### Simple Docker Image

**Dockerfile**:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000 5001

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001

ENTRYPOINT ["dotnet", "GrpcWebBridge.dll"]
```

### Build Image

```bash
# Build the Docker image
docker build -t grpc-web-bridge:1.2.0 .
docker tag grpc-web-bridge:1.2.0 grpc-web-bridge:latest

# Verify the image
docker images | grep grpc-web-bridge
```

### Run Container

```bash
docker run -d \
  --name grpc-web-bridge \
  -p 5000:5000 \
  -p 5001:5001 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://+:5000;https://+:5001" \
  -v /etc/ssl/certs:/app/certs:ro \
  -v /app/logs:/app/logs \
  --restart unless-stopped \
  grpc-web-bridge:latest
```

### Container Health Check

```bash
docker run -d \
  --name grpc-web-bridge \
  --health-cmd='curl -f http://localhost:5000/health || exit 1' \
  --health-interval=30s \
  --health-timeout=10s \
  --health-retries=3 \
  -p 5000:5000 \
  -p 5001:5001 \
  grpc-web-bridge:latest
```

## Docker Compose Deployment

### Full Stack with Backend

**docker-compose.yml**:
```yaml
version: '3.8'

services:
  grpc-web-bridge:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: grpc-web-bridge
    ports:
      - "5000:5000"
      - "5001:5001"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:5000;https://+:5001
      GrpcWebBridge__MaxStreamCount: 5000
      GrpcWebBridge__RequireAuthentication: "true"
    volumes:
      - ./certs:/app/certs:ro
      - ./config/appsettings.Production.json:/app/appsettings.Production.json:ro
      - ./logs:/app/logs
    depends_on:
      - grpc-backend
    networks:
      - grpc-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  grpc-backend:
    image: my-grpc-service:latest
    container_name: grpc-backend
    ports:
      - "50051:50051"
    environment:
      ASPNETCORE_URLS: http://+:50051
    networks:
      - grpc-network
    restart: unless-stopped

  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./config/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus
    networks:
      - grpc-network
    restart: unless-stopped

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"
    environment:
      GF_SECURITY_ADMIN_PASSWORD: admin
    volumes:
      - grafana-data:/var/lib/grafana
    depends_on:
      - prometheus
    networks:
      - grpc-network
    restart: unless-stopped

volumes:
  prometheus-data:
  grafana-data:

networks:
  grpc-network:
    driver: bridge
```

**Start the Stack**:
```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f grpc-web-bridge

# Stop all services
docker-compose down
```

## Kubernetes Deployment

### Namespace and RBAC

```bash
# Create dedicated namespace
kubectl create namespace grpc-bridge

# Create service account
kubectl create serviceaccount grpc-bridge -n grpc-bridge

# Create role and binding
kubectl apply -f - <<EOF
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: grpc-bridge
  namespace: grpc-bridge
rules:
- apiGroups: [""]
  resources: ["configmaps"]
  verbs: ["get", "list", "watch"]
EOF

kubectl create rolebinding grpc-bridge \
  --clusterrole=grpc-bridge \
  --serviceaccount=grpc-bridge:grpc-bridge \
  -n grpc-bridge
```

### ConfigMap for Configuration

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: grpc-web-bridge-config
  namespace: grpc-bridge
data:
  appsettings.Production.json: |
    {
      "GrpcWebBridge": {
        "Environment": "Production",
        "MaxStreamCount": 5000,
        "StreamIdleTimeoutSeconds": 300,
        "RequireAuthentication": true,
        "AllowedOrigins": [
          "https://app.example.com",
          "https://admin.example.com"
        ]
      },
      "Authentication": {
        "JwtIssuer": "https://auth.example.com",
        "JwtAudience": "grpc-web-bridge",
        "JwtExpirationMinutes": 60
      }
    }
```

### Deployment

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: grpc-web-bridge
  namespace: grpc-bridge
  labels:
    app: grpc-web-bridge
spec:
  replicas: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app: grpc-web-bridge
  template:
    metadata:
      labels:
        app: grpc-web-bridge
      annotations:
        prometheus.io/scrape: "true"
        prometheus.io/port: "5000"
        prometheus.io/path: "/metrics"
    spec:
      serviceAccountName: grpc-bridge
      securityContext:
        runAsNonRoot: true
        runAsUser: 1000
        fsGroup: 1000
      containers:
      - name: grpc-web-bridge
        image: grpc-web-bridge:1.2.0
        imagePullPolicy: IfNotPresent
        ports:
        - name: http
          containerPort: 5000
          protocol: TCP
        - name: https
          containerPort: 5001
          protocol: TCP
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5000;https://+:5001"
        - name: GrpcWebBridge__MaxStreamCount
          value: "5000"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: http
            scheme: HTTP
          initialDelaySeconds: 15
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health/ready
            port: http
            scheme: HTTP
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
        volumeMounts:
        - name: config
          mountPath: /app/config
          readOnly: true
        - name: certs
          mountPath: /app/certs
          readOnly: true
        securityContext:
          allowPrivilegeEscalation: false
          readOnlyRootFilesystem: false
          capabilities:
            drop:
            - ALL
      volumes:
      - name: config
        configMap:
          name: grpc-web-bridge-config
      - name: certs
        secret:
          secretName: grpc-web-bridge-tls
```

### Service

```yaml
apiVersion: v1
kind: Service
metadata:
  name: grpc-web-bridge
  namespace: grpc-bridge
spec:
  type: LoadBalancer
  ports:
  - port: 5000
    targetPort: http
    protocol: TCP
    name: http
  - port: 5001
    targetPort: https
    protocol: TCP
    name: https
  selector:
    app: grpc-web-bridge
```

### HorizontalPodAutoscaler

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: grpc-web-bridge
  namespace: grpc-bridge
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: grpc-web-bridge
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### Deploy to Kubernetes

```bash
# Apply ConfigMap
kubectl apply -f configmap.yaml

# Create TLS secret
kubectl create secret tls grpc-web-bridge-tls \
  --cert=certs/server.crt \
  --key=certs/server.key \
  -n grpc-bridge

# Apply deployment
kubectl apply -f deployment.yaml
kubectl apply -f service.yaml
kubectl apply -f hpa.yaml

# Verify deployment
kubectl get pods -n grpc-bridge
kubectl get services -n grpc-bridge
kubectl describe deployment grpc-web-bridge -n grpc-bridge
```

## SSL/TLS Configuration

### Generate Self-Signed Certificate

```bash
# Generate private key
openssl genrsa -out server.key 2048

# Generate certificate
openssl req -new -x509 -key server.key -out server.crt \
  -days 365 \
  -subj "/CN=grpc-web-bridge.example.com"

# Convert to PFX (for .NET)
openssl pkcs12 -export -out server.pfx \
  -inkey server.key \
  -in server.crt \
  -passout pass:password
```

### Configure in appsettings

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
          "Path": "/app/certs/server.pfx",
          "Password": "password"
        }
      }
    }
  }
}
```

## Load Balancing

### Nginx Configuration

```nginx
upstream grpc_web_bridge {
    server bridge1:5001;
    server bridge2:5001;
    server bridge3:5001;
}

server {
    listen 443 ssl http2;
    server_name api.example.com;

    ssl_certificate /etc/nginx/certs/server.crt;
    ssl_certificate_key /etc/nginx/certs/server.key;

    location / {
        grpc_pass grpcs://grpc_web_bridge;
        grpc_ssl_verify off;
        
        # Timeouts
        grpc_connect_timeout 60s;
        grpc_send_timeout 60s;
        grpc_recv_timeout 60s;
    }

    location /health {
        proxy_pass http://grpc_web_bridge;
    }
}
```

## Monitoring and Observability

### Prometheus Metrics

**prometheus.yml**:
```yaml
global:
  scrape_interval: 15s

scrape_configs:
- job_name: 'grpc-web-bridge'
  static_configs:
  - targets: ['localhost:5000']
  metrics_path: '/metrics'
```

### Grafana Dashboard

Create dashboard queries:
```
# Active streams
grpc_web_bridge_active_streams

# Request latency
histogram_quantile(0.95, rate(grpc_web_bridge_request_duration_seconds_bucket[5m]))

# Error rate
rate(grpc_web_bridge_request_errors_total[5m])

# Cache hit rate
rate(grpc_web_bridge_cache_hits_total[5m]) / rate(grpc_web_bridge_cache_requests_total[5m])
```

### Logging

**Production logging configuration**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "GrpcWebBridge": "Information"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```

## Graceful Shutdown

### Shutdown Handler

```csharp
var host = builder.Build();

var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(async () =>
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application is shutting down...");
    
    // Gracefully close active streams
    var streamService = host.Services.GetRequiredService<StreamingService>();
    await streamService.GracefulShutdownAsync();
    
    // Close connections
    var connManager = host.Services.GetRequiredService<GrpcConnectionManager>();
    await connManager.CloseAllConnectionsAsync();
});

await host.RunAsync();
```

## Rollout Strategy

### Blue-Green Deployment

```bash
# Deploy new version to green
kubectl apply -f deployment-green.yaml

# Test green environment
curl https://green.api.example.com/health

# Switch traffic to green
kubectl patch service grpc-web-bridge \
  -p '{"spec":{"selector":{"version":"green"}}}'

# Keep blue for quick rollback
```

### Canary Deployment

```yaml
apiVersion: networking.istio.io/v1beta1
kind: VirtualService
metadata:
  name: grpc-web-bridge
spec:
  hosts:
  - grpc-web-bridge
  http:
  - match:
    - uri:
        prefix: /
    route:
    - destination:
        host: grpc-web-bridge
        subset: v1
      weight: 90
    - destination:
        host: grpc-web-bridge
        subset: v2
      weight: 10
```

## Backup and Recovery

### Configuration Backup

```bash
# Backup configuration
kubectl get configmap grpc-web-bridge-config \
  -n grpc-bridge \
  -o yaml > backup-config.yaml

# Restore configuration
kubectl apply -f backup-config.yaml
```

### Data Backup

If using persistent storage:
```bash
kubectl get pvc -n grpc-bridge
kubectl create snapshot grpc-bridge-snapshot --source pvc/data
```

## Production Checklist

- [ ] Load testing completed and passed
- [ ] Security audit completed
- [ ] Certificate valid and not self-signed
- [ ] Monitoring dashboards created
- [ ] Alert rules configured
- [ ] Runbooks for common issues created
- [ ] Backup strategy tested
- [ ] Disaster recovery plan in place
- [ ] Team trained on operations
- [ ] Documentation updated
- [ ] Version tagged in git
- [ ] Change log updated
- [ ] Deployment runbook created

## Troubleshooting

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for common issues and solutions.
