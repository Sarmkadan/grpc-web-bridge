#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# Basic Setup Example: Starting the bridge and registering a service
# This demonstrates the simplest usage pattern

set -e

echo "═══════════════════════════════════════════════════════════════"
echo "gRPC-Web Bridge: Basic Setup Example"
echo "═══════════════════════════════════════════════════════════════"

# Configuration
BRIDGE_URL="http://localhost:5000"
SERVICE_NAME="ExampleService"
SERVICE_ADDRESS="grpc://localhost:50051"

# 1. Check if bridge is running
echo ""
echo "1. Checking bridge health..."
if curl -s "$BRIDGE_URL/health" > /dev/null; then
    echo "✓ Bridge is running"
else
    echo "✗ Bridge is not running. Start it with: dotnet run"
    echo "  From: src/GrpcWebBridge/"
    exit 1
fi

# 2. Check current services
echo ""
echo "2. Listing registered services..."
curl -s "$BRIDGE_URL/api/services" | jq '.services // []' || echo "[]"

# 3. Register the service
echo ""
echo "3. Registering service: $SERVICE_NAME"
curl -X POST "$BRIDGE_URL/api/services/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"serviceName\": \"$SERVICE_NAME\",
    \"address\": \"$SERVICE_ADDRESS\",
    \"healthCheck\": false
  }"
echo ""

# 4. Verify service was registered
echo ""
echo "4. Verifying service registration..."
SERVICES=$(curl -s "$BRIDGE_URL/api/services")
echo "$SERVICES" | jq "." || echo "Service registration completed"

# 5. Check active streams
echo ""
echo "5. Checking active streams..."
curl -s "$BRIDGE_URL/api/streams" | jq '.activeStreams // []' || echo "No active streams"

# 6. Get metrics
echo ""
echo "6. Bridge metrics..."
curl -s "$BRIDGE_URL/api/metrics" | jq "." || echo "Metrics endpoint available"

echo ""
echo "═══════════════════════════════════════════════════════════════"
echo "✓ Setup complete!"
echo ""
echo "Next steps:"
echo "1. Ensure your gRPC service is running on $SERVICE_ADDRESS"
echo "2. Make requests to: $BRIDGE_URL/api/bridge/$SERVICE_NAME/<method>"
echo "3. Monitor via Swagger: $BRIDGE_URL/swagger"
echo "═══════════════════════════════════════════════════════════════"
