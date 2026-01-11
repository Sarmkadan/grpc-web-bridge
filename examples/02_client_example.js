// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

/**
 * gRPC-Web Client Example
 *
 * Demonstrates how to create a web client that communicates through
 * the gRPC-Web Bridge.
 */

// Install dependencies:
// npm install grpc-web

const grpc = require('@grpc/grpc-js');

/**
 * Simple Unary RPC Example
 *
 * This example shows how to make a simple request-response call
 * to a backend service through the bridge.
 */
async function unaryRpcExample() {
    console.log('═══════════════════════════════════════════════════════════════');
    console.log('Unary RPC Example');
    console.log('═══════════════════════════════════════════════════════════════');

    const BRIDGE_URL = 'http://localhost:5000';
    const SERVICE = 'UserService';
    const METHOD = 'GetUser';

    try {
        // Build the request URL
        const url = `${BRIDGE_URL}/api/bridge/${SERVICE}/${METHOD}`;

        // Create request payload
        const request = {
            id: 42,
            includeProfile: true
        };

        // Make the request
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                // Add JWT token if authentication is enabled
                'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
            },
            body: JSON.stringify(request)
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const result = await response.json();
        console.log('✓ Response received:', result);
        console.log('  User:', result.name, result.email);

    } catch (error) {
        console.error('✗ Error:', error.message);
    }
}

/**
 * Server Streaming Example
 *
 * Demonstrates receiving multiple messages from the server
 */
async function serverStreamingExample() {
    console.log('═══════════════════════════════════════════════════════════════');
    console.log('Server Streaming Example');
    console.log('═══════════════════════════════════════════════════════════════');

    const BRIDGE_URL = 'http://localhost:5000';
    const SERVICE = 'UserService';
    const METHOD = 'ListUsers';

    try {
        const url = `${BRIDGE_URL}/api/bridge/${SERVICE}/${METHOD}`;

        const request = {
            pageSize: 10,
            offset: 0
        };

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
            },
            body: JSON.stringify(request)
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        // For streaming responses, read the body as text and parse lines
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            buffer += decoder.decode(value, { stream: true });
            const lines = buffer.split('\n');
            buffer = lines.pop(); // Keep incomplete line in buffer

            for (const line of lines) {
                if (line.trim()) {
                    const message = JSON.parse(line);
                    console.log('✓ Received message:', message);
                }
            }
        }

        console.log('✓ Stream completed');

    } catch (error) {
        console.error('✗ Error:', error.message);
    }
}

/**
 * Client Streaming Example
 *
 * Demonstrates sending multiple messages to the server
 */
async function clientStreamingExample() {
    console.log('═══════════════════════════════════════════════════════════════');
    console.log('Client Streaming Example');
    console.log('═══════════════════════════════════════════════════════════════');

    const BRIDGE_URL = 'http://localhost:5000';
    const SERVICE = 'MetricsService';
    const METHOD = 'UploadMetrics';

    try {
        const url = `${BRIDGE_URL}/api/bridge/${SERVICE}/${METHOD}`;

        // Prepare messages
        const metrics = [
            { name: 'cpu_usage', value: 75.5, timestamp: new Date().toISOString() },
            { name: 'memory_usage', value: 85.2, timestamp: new Date().toISOString() },
            { name: 'disk_usage', value: 92.1, timestamp: new Date().toISOString() }
        ];

        // Create request body with multiple messages
        const body = metrics.map(m => JSON.stringify(m)).join('\n');

        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-ndjson',
                'Authorization': 'Bearer YOUR_JWT_TOKEN_HERE'
            },
            body: body
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const result = await response.json();
        console.log('✓ Metrics uploaded:', result.count, 'metrics recorded');

    } catch (error) {
        console.error('✗ Error:', error.message);
    }
}

/**
 * Error Handling Example
 *
 * Demonstrates proper error handling with retries and backoff
 */
async function withRetry(fn, maxRetries = 3, baseDelay = 1000) {
    for (let attempt = 0; attempt < maxRetries; attempt++) {
        try {
            return await fn();
        } catch (error) {
            if (attempt === maxRetries - 1) throw error;

            // Exponential backoff
            const delay = baseDelay * Math.pow(2, attempt);
            console.log(`⚠ Attempt ${attempt + 1} failed, retrying in ${delay}ms...`);
            await new Promise(resolve => setTimeout(resolve, delay));
        }
    }
}

async function errorHandlingExample() {
    console.log('═══════════════════════════════════════════════════════════════');
    console.log('Error Handling Example');
    console.log('═══════════════════════════════════════════════════════════════');

    try {
        const result = await withRetry(async () => {
            const response = await fetch('http://localhost:5000/api/services');
            if (!response.ok) {
                const text = await response.text();
                throw new Error(`HTTP ${response.status}: ${text}`);
            }
            return response.json();
        });

        console.log('✓ Request succeeded:', result);

    } catch (error) {
        console.error('✗ Error after retries:', error.message);
    }
}

/**
 * Authentication Example
 *
 * Shows how to obtain and use JWT tokens
 */
async function authenticationExample() {
    console.log('═══════════════════════════════════════════════════════════════');
    console.log('Authentication Example');
    console.log('═══════════════════════════════════════════════════════════════');

    // In a real application, obtain token from your auth provider
    const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...';

    try {
        const response = await fetch('http://localhost:5000/api/services', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.status === 401) {
            console.error('✗ Authentication failed: Invalid or expired token');
            return;
        }

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const result = await response.json();
        console.log('✓ Authenticated request succeeded:', result);

    } catch (error) {
        console.error('✗ Error:', error.message);
    }
}

/**
 * Metadata & Headers Example
 *
 * Demonstrates custom headers and metadata
 */
async function headersExample() {
    console.log('═══════════════════════════════════════════════════════════════');
    console.log('Headers & Metadata Example');
    console.log('═══════════════════════════════════════════════════════════════');

    try {
        const response = await fetch('http://localhost:5000/api/bridge/UserService/GetUser', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer YOUR_TOKEN_HERE',
                'X-Request-ID': 'req-12345', // Correlation ID
                'X-Custom-Header': 'custom-value',
                'Accept-Encoding': 'gzip' // Request compression
            },
            body: JSON.stringify({ id: 42 })
        });

        console.log('✓ Response status:', response.status);
        console.log('✓ Response headers:');
        response.headers.forEach((value, key) => {
            console.log(`  ${key}: ${value}`);
        });

        const result = await response.json();
        console.log('✓ Response body:', result);

    } catch (error) {
        console.error('✗ Error:', error.message);
    }
}

// Main execution
async function main() {
    console.log('\n');
    console.log('gRPC-Web Bridge - JavaScript Client Examples');
    console.log('');

    // Run examples
    try {
        await unaryRpcExample();
        console.log('\n');

        await errorHandlingExample();
        console.log('\n');

        await authenticationExample();
        console.log('\n');

        await headersExample();

    } catch (error) {
        console.error('Fatal error:', error);
        process.exit(1);
    }

    console.log('\n');
    console.log('═══════════════════════════════════════════════════════════════');
    console.log('Examples completed!');
    console.log('═══════════════════════════════════════════════════════════════');
}

main();
