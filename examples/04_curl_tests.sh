#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# cURL Test Examples for gRPC-Web Bridge
# These examples show how to test the bridge using cURL

set -e

# Configuration
BRIDGE_URL="${BRIDGE_URL:-http://localhost:5000}"
BRIDGE_SECURE_URL="${BRIDGE_SECURE_URL:-https://localhost:5001}"
API_KEY="${API_KEY:-your-api-key-here}"
JWT_TOKEN="${JWT_TOKEN:-your-jwt-token-here}"

# Color output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Helper function to test endpoints
test_endpoint() {
    local name=$1
    local method=$2
    local url=$3
    local data=$4

    echo ""
    echo -e "${YELLOW}Testing:${NC} $name"
    echo "URL: $url"

    if [ -z "$data" ]; then
        response=$(curl -s -X "$method" "$url" \
            -H "Content-Type: application/json" \
            -w "\n%{http_code}")
    else
        response=$(curl -s -X "$method" "$url" \
            -H "Content-Type: application/json" \
            -d "$data" \
            -w "\n%{http_code}")
    fi

    http_code=$(echo "$response" | tail -n 1)
    body=$(echo "$response" | head -n -1)

    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        echo -e "${GREEN}✓ Status: $http_code${NC}"
        echo "Response: $body" | jq '.' 2>/dev/null || echo "Response: $body"
    else
        echo -e "${RED}✗ Status: $http_code${NC}"
        echo "Response: $body"
    fi
}

# Test with authentication
test_endpoint_with_auth() {
    local name=$1
    local method=$2
    local url=$3
    local data=$4
    local auth_type=$5  # "jwt" or "apikey"

    echo ""
    echo -e "${YELLOW}Testing:${NC} $name (with $auth_type)"
    echo "URL: $url"

    if [ "$auth_type" = "jwt" ]; then
        auth_header="Authorization: Bearer $JWT_TOKEN"
    else
        auth_header="X-API-Key: $API_KEY"
    fi

    if [ -z "$data" ]; then
        response=$(curl -s -X "$method" "$url" \
            -H "Content-Type: application/json" \
            -H "$auth_header" \
            -w "\n%{http_code}")
    else
        response=$(curl -s -X "$method" "$url" \
            -H "Content-Type: application/json" \
            -H "$auth_header" \
            -d "$data" \
            -w "\n%{http_code}")
    fi

    http_code=$(echo "$response" | tail -n 1)
    body=$(echo "$response" | head -n -1)

    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        echo -e "${GREEN}✓ Status: $http_code${NC}"
        echo "Response:" && echo "$body" | jq '.' 2>/dev/null || echo "$body"
    else
        echo -e "${RED}✗ Status: $http_code${NC}"
        echo "Response: $body"
    fi
}

main() {
    echo "═══════════════════════════════════════════════════════════════"
    echo "gRPC-Web Bridge - cURL Test Suite"
    echo "═══════════════════════════════════════════════════════════════"
    echo ""
    echo "Bridge URL: $BRIDGE_URL"
    echo "Secure URL: $BRIDGE_SECURE_URL"
    echo ""

    # 1. Health Check Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "1. HEALTH CHECK TESTS"
    echo "════════════════════════════════════════════════════════════════"

    test_endpoint "Health Status" "GET" "$BRIDGE_URL/health"
    test_endpoint "Liveness Probe" "GET" "$BRIDGE_URL/health/live"
    test_endpoint "Readiness Probe" "GET" "$BRIDGE_URL/health/ready"

    # 2. Service Management Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "2. SERVICE MANAGEMENT TESTS"
    echo "════════════════════════════════════════════════════════════════"

    test_endpoint "List Services" "GET" "$BRIDGE_URL/api/services"

    test_endpoint "Register Service" "POST" "$BRIDGE_URL/api/services/register" \
        '{
            "serviceName": "TestService",
            "address": "grpc://localhost:50051",
            "healthCheck": false,
            "metadata": {
                "version": "1.0.0",
                "environment": "test"
            }
        }'

    test_endpoint "Get Service Details" "GET" "$BRIDGE_URL/api/services/TestService"

    # 3. Stream Management Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "3. STREAM MANAGEMENT TESTS"
    echo "════════════════════════════════════════════════════════════════"

    test_endpoint "List Active Streams" "GET" "$BRIDGE_URL/api/streams"

    # 4. Metrics Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "4. METRICS TESTS"
    echo "════════════════════════════════════════════════════════════════"

    test_endpoint "Get Metrics" "GET" "$BRIDGE_URL/api/metrics"

    # 5. Configuration Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "5. CONFIGURATION TESTS"
    echo "════════════════════════════════════════════════════════════════"

    test_endpoint "Get Configuration" "GET" "$BRIDGE_URL/api/configuration"

    # 6. RPC Call Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "6. RPC CALL TESTS"
    echo "════════════════════════════════════════════════════════════════"

    test_endpoint "Unary RPC Call" "POST" "$BRIDGE_URL/api/bridge/TestService/GetData" \
        '{"id": 42}'

    test_endpoint "Server Streaming" "POST" "$BRIDGE_URL/api/bridge/TestService/StreamData" \
        '{"count": 10}'

    test_endpoint "Client Streaming" "POST" "$BRIDGE_URL/api/bridge/TestService/UploadData" \
        '{"data": "test"}'

    # 7. Error Handling Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "7. ERROR HANDLING TESTS"
    echo "════════════════════════════════════════════════════════════════"

    test_endpoint "Invalid Service" "POST" "$BRIDGE_URL/api/bridge/NonExistent/Method" \
        '{}'

    test_endpoint "Bad Request" "POST" "$BRIDGE_URL/api/bridge/TestService/GetData" \
        'invalid json'

    test_endpoint "Non-existent Endpoint" "GET" "$BRIDGE_URL/api/nonexistent"

    # 8. Authentication Tests (if enabled)
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "8. AUTHENTICATION TESTS"
    echo "════════════════════════════════════════════════════════════════"

    test_endpoint_with_auth "Authenticated Request (JWT)" "GET" \
        "$BRIDGE_URL/api/services" "" "jwt"

    test_endpoint_with_auth "Authenticated Request (API Key)" "GET" \
        "$BRIDGE_URL/api/services" "" "apikey"

    # 9. Headers and Metadata Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "9. HEADERS AND METADATA TESTS"
    echo "════════════════════════════════════════════════════════════════"

    echo ""
    echo -e "${YELLOW}Testing:${NC} Request with custom headers"
    curl -v \
        -X POST "$BRIDGE_URL/api/bridge/TestService/GetData" \
        -H "Content-Type: application/json" \
        -H "X-Request-ID: req-12345" \
        -H "X-Correlation-ID: corr-12345" \
        -H "User-Agent: gRPC-Web-Bridge-Test" \
        -d '{"id": 42}' 2>&1 | head -20

    # 10. Performance Tests
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "10. PERFORMANCE TESTS"
    echo "════════════════════════════════════════════════════════════════"

    echo ""
    echo -e "${YELLOW}Testing:${NC} Response time benchmark"

    start_time=$(date +%s%N)
    curl -s -X GET "$BRIDGE_URL/health" > /dev/null
    end_time=$(date +%s%N)

    elapsed=$((($end_time - $start_time) / 1000000))
    echo "Response time: ${elapsed}ms"

    # Summary
    echo ""
    echo "════════════════════════════════════════════════════════════════"
    echo "Test Suite Completed"
    echo "════════════════════════════════════════════════════════════════"
    echo ""
    echo "Next steps:"
    echo "1. Verify all health checks pass"
    echo "2. Register your backend services"
    echo "3. Make RPC calls to test functionality"
    echo "4. Monitor metrics and logs"
    echo ""
}

# Run if called directly
if [ "${BASH_SOURCE[0]}" = "${0}" ]; then
    main "$@"
fi
