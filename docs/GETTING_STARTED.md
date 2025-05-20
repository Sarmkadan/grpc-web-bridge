# Getting Started with gRPC-Web Bridge

A step-by-step guide to get up and running with gRPC-Web Bridge in 5 minutes.

## Prerequisites

- .NET 10 SDK or later
- Basic understanding of gRPC and HTTP
- A gRPC backend service (or we'll create one)

## Step 1: Clone the Repository

```bash
git clone https://github.com/sarmkadan/grpc-web-bridge.git
cd grpc-web-bridge
```

## Step 2: Build and Run the Bridge

```bash
# Navigate to the project
cd src/GrpcWebBridge

# Restore packages and build
dotnet build

# Run the server
dotnet run
```

You should see output like:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

## Step 3: Verify Installation

Open your browser and navigate to:
```
http://localhost:5000/swagger
```

You should see the Swagger UI with available endpoints.

## Step 4: Check Health

```bash
curl http://localhost:5000/health
```

Expected response:
```json
{
  "status": "Healthy",
  "activeConnections": 0,
  "uptime": "0d 0h 0m"
}
```

## Step 5: Register a Backend Service

Create a test gRPC service or use an existing one. Register it with the bridge:

```bash
curl -X POST http://localhost:5000/api/services/register \
  -H "Content-Type: application/json" \
  -d '{
    "serviceName": "TestService",
    "address": "grpc://localhost:50051",
    "healthCheck": false
  }'
```

## Step 6: Make Your First Request

Create a file `test_request.js`:

```javascript
// Node.js example with grpc-web
const {TestServiceClient} = require('./test_service_pb');
const {TestRequest} = require('./test_service_pb');

const client = new TestServiceClient('http://localhost:5000');
const request = new TestRequest();
request.setId(1);

client.test(request, {}, (err, response) => {
  if (err) {
    console.error('Error:', err);
  } else {
    console.log('Success:', response);
  }
});
```

Or using `curl`:

```bash
curl -X POST http://localhost:5000/api/bridge/TestService/Test \
  -H "Content-Type: application/json" \
  -d '{"id": 1}'
```

## Next Steps

1. **Authentication**: Read [AUTHENTICATION.md](AUTHENTICATION.md)
2. **Streaming**: Check [STREAMING.md](STREAMING.md)
3. **Configuration**: Review [CONFIGURATION.md](CONFIGURATION.md)
4. **Deployment**: See [DEPLOYMENT.md](DEPLOYMENT.md)

## Common Issues

### Bridge won't start
- Ensure .NET 10 is installed: `dotnet --version`
- Check if ports 5000/5001 are available
- Try a different port: `dotnet run -- --urls "http://0.0.0.0:6000"`

### Can't connect to backend service
- Verify backend service is running
- Check firewall rules
- Ensure correct service address in registration

### CORS errors
- Update `AllowedOrigins` in `appsettings.json`
- Test with `curl` first (no CORS)

## Need Help?

- Check logs: Watch the console output for errors
- Enable debug logging in `appsettings.json`:
  ```json
  {
    "Logging": {
      "LogLevel": {
        "GrpcWebBridge": "Debug"
      }
    }
  }
  ```
- Open an issue on GitHub

## What's Next?

Now that you have the bridge running:

1. **Read the API Reference**: Full endpoint documentation
2. **Explore Examples**: See real-world usage patterns
3. **Configure Services**: Register your actual gRPC services
4. **Set Up Authentication**: Secure your endpoints with JWT
5. **Deploy**: Move to production with Docker

Happy gRPC-Web bridging! 🚀
