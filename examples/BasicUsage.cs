#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
//
// Minimal gRPC-Web Bridge usage example
// Demonstrates the simplest possible integration with the bridge

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Examples
{
    /// <summary>
    /// Basic usage example - minimal setup and first call
    /// </summary>
    public sealed class BasicUsageExample
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BasicUsageExample> _logger;
        private readonly string _bridgeUrl;

        /// <summary>
        /// Initialize the basic client
        /// </summary>
        /// <param name="bridgeUrl">URL of the gRPC-Web Bridge server</param>
        public BasicUsageExample(string bridgeUrl)
        {
            _bridgeUrl = bridgeUrl ?? throw new ArgumentNullException(nameof(bridgeUrl));

            // Create HTTP client with minimal configuration
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_bridgeUrl)
            };

            // Setup logger (console output for this example)
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information);
            });
            _logger = loggerFactory.CreateLogger<BasicUsageExample>();
        }

        /// <summary>
        /// Example 1: Check if bridge is running
        /// </summary>
        public async Task<bool> CheckBridgeHealthAsync()
        {
            try
            {
                _logger.LogInformation("Checking if gRPC-Web Bridge is running...");

                var response = await _httpClient.GetAsync("/health").ConfigureAwait(false);
                var isHealthy = response.IsSuccessStatusCode;

                _logger.LogInformation(isHealthy
                    ? "✓ Bridge is healthy and running"
                    : "✗ Bridge is not responding");

                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to bridge");
                return false;
            }
        }

        /// <summary>
        /// Example 2: Make a simple unary RPC call
        /// </summary>
        /// <param name="serviceName">Name of the gRPC service</param>
        /// <param name="methodName">Name of the gRPC method</param>
        /// <param name="requestData">Request payload as anonymous object</param>
        public async Task<object?> MakeSimpleCallAsync(
            string serviceName,
            string methodName,
            object requestData)
        {
            try
            {
                _logger.LogInformation(
                    "Making call: {Service}.{Method}",
                    serviceName, methodName);

                // Construct the bridge endpoint URL
                var endpoint = $"/api/bridge/{serviceName}/{methodName}";

                // Make POST request with JSON payload
                var response = await _httpClient.PostAsJsonAsync(
                    endpoint,
                    requestData).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogError(
                        "Call failed: {StatusCode} - {Error}",
                        response.StatusCode,
                        error);
                    return null;
                }

                // Read and return the response
                var result = await response.Content.ReadAsAsync<object>().ConfigureAwait(false);
                _logger.LogInformation("✓ Call succeeded");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC call failed");
                return null;
            }
        }

        /// <summary>
        /// Example 3: Get bridge metrics
        /// </summary>
        public async Task<string?> GetBridgeMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching bridge metrics...");

                var response = await _httpClient.GetAsync("/api/metrics").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var metrics = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogInformation("✓ Metrics retrieved");

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metrics");
                return null;
            }
        }
    }

    /// <summary>
    /// Simple console application demonstrating basic usage
    /// </summary>
    public sealed class BasicUsageProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== gRPC-Web Bridge - Basic Usage Example ===\n");

            // Replace with your bridge URL
            const string bridgeUrl = "http://localhost:5000";

            var example = new BasicUsageExample(bridgeUrl);

            try
            {
                // Example 1: Check health
                var isHealthy = await example.CheckBridgeHealthAsync();
                if (!isHealthy)
                {
                    Console.WriteLine("\n❌ Bridge is not available. Please start the bridge server first.");
                    return;
                }

                Console.WriteLine("\n--- Bridge is ready! ---\n");

                // Example 2: Make a simple call
                // Replace with actual service/method names from your setup
                var result = await example.MakeSimpleCallAsync(
                    "ExampleService",  // Replace with your service name
                    "GetData",         // Replace with your method name
                    new { id = 123 }); // Replace with your request data

                if (result != null)
                {
                    Console.WriteLine($"\n✓ RPC call returned: {result}");
                }

                // Example 3: Get metrics
                var metrics = await example.GetBridgeMetricsAsync();
                if (metrics != null)
                {
                    Console.WriteLine("\n--- Bridge Metrics ---");
                    Console.WriteLine(metrics.Substring(0, Math.Min(200, metrics.Length)) + "...");
                }

                Console.WriteLine("\n=== Example completed successfully ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
