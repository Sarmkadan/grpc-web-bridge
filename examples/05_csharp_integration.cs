#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Examples
{
    /// <summary>
    /// gRPC-Web Bridge integration examples for .NET applications
    /// </summary>
    public sealed class GrpcWebBridgeClientExample
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GrpcWebBridgeClientExample> _logger;
        private readonly string _bridgeUrl;
        private readonly string? _jwtToken;

        public GrpcWebBridgeClientExample(
            string bridgeUrl,
            HttpClient httpClient,
            ILogger<GrpcWebBridgeClientExample> logger,
            string? jwtToken = null)
        {
            _bridgeUrl = bridgeUrl;
            _httpClient = httpClient;
            _logger = logger;
            _jwtToken = jwtToken;

            ConfigureHttpClient();
        }

        /// Configure the HTTP client with default headers
        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_bridgeUrl);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "gRPC-Web-Bridge-.NET-Client");

            if (!string.IsNullOrEmpty(_jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Add(
                    "Authorization",
                    $"Bearer {_jwtToken}");
            }
        }

        /// <summary>
        /// Example 1: Check bridge health
        /// </summary>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                _logger.LogInformation("Checking bridge health...");
                var response = await _httpClient.GetAsync("/health");
                var isHealthy = response.IsSuccessStatusCode;
                _logger.LogInformation($"Bridge health: {(isHealthy ? "Healthy" : "Unhealthy")}");
                return isHealthy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check health");
                return false;
            }
        }

        /// <summary>
        /// Example 2: List registered services
        /// </summary>
        public async Task<List<ServiceInfo>?> ListServicesAsync()
        {
            try
            {
                _logger.LogInformation("Listing registered services...");
                var response = await _httpClient.GetAsync("/api/services");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var services = JsonSerializer.Deserialize<List<ServiceInfo>>(json);

                _logger.LogInformation($"Found {services?.Count} services");
                return services;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list services");
                return null;
            }
        }

        /// <summary>
        /// Example 3: Register a service dynamically
        /// </summary>
        public async Task<bool> RegisterServiceAsync(
            string serviceName,
            string grpcAddress,
            bool enableHealthCheck = false)
        {
            try
            {
                _logger.LogInformation($"Registering service: {serviceName}");

                var request = new
                {
                    serviceName,
                    address = grpcAddress,
                    healthCheck = enableHealthCheck
                };

                var response = await _httpClient.PostAsJsonAsync(
                    "/api/services/register",
                    request);

                response.EnsureSuccessStatusCode();
                _logger.LogInformation($"Service {serviceName} registered successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to register service {serviceName}");
                return false;
            }
        }

        /// <summary>
        /// Example 4: Make a simple unary RPC call
        /// </summary>
        public async Task<T?> CallServiceAsync<T>(
            string serviceName,
            string methodName,
            object request) where T : class
        {
            try
            {
                _logger.LogInformation(
                    $"Calling {serviceName}.{methodName}");

                var url = $"/api/bridge/{serviceName}/{methodName}";
                var response = await _httpClient.PostAsJsonAsync(url, request);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        $"RPC call failed: {response.StatusCode} - {error}");
                    return null;
                }

                var result = await response.Content.ReadAsAsync<T>();
                _logger.LogInformation("RPC call succeeded");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    $"Failed to call {serviceName}.{methodName}");
                return null;
            }
        }

        /// <summary>
        /// Example 5: Get metrics from the bridge
        /// </summary>
        public async Task<MetricsInfo?> GetMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching metrics...");
                var response = await _httpClient.GetAsync("/api/metrics");
                response.EnsureSuccessStatusCode();

                var metrics = await response.Content.ReadAsAsync<MetricsInfo>();
                _logger.LogInformation(
                    $"Metrics - Active Streams: {metrics.ActiveStreams}, " +
                    $"Total Requests: {metrics.TotalRequests}, " +
                    $"Avg Latency: {metrics.AverageLatencyMs}ms");
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metrics");
                return null;
            }
        }

        /// <summary>
        /// Example 6: Monitor active streams
        /// </summary>
        public async Task<int> GetActiveStreamCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/streams");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var streams = JsonSerializer.Deserialize<StreamInfo>(json);
                var count = streams?.ActiveStreams.Count ?? 0;

                _logger.LogInformation($"Active streams: {count}");
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active streams");
                return 0;
            }
        }

        /// <summary>
        /// Example 7: Error handling with retry logic
        /// </summary>
        public async Task<T?> CallWithRetryAsync<T>(
            string serviceName,
            string methodName,
            object request,
            int maxRetries = 3) where T : class
        {
            var delay = 1000; // Start with 1 second

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var result = await CallServiceAsync<T>(
                        serviceName,
                        methodName,
                        request);

                    if (result is not null)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        $"Attempt {attempt + 1} failed, retrying in {delay}ms...");
                }

                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(delay);
                    delay *= 2; // Exponential backoff
                }
            }

            _logger.LogError(
                $"Failed after {maxRetries} attempts");
            return null;
        }

        /// <summary>
        /// Example 8: Batch operations with streaming
        /// </summary>
        public async Task<bool> ProcessBatchAsync<T>(
            string serviceName,
            string methodName,
            IEnumerable<T> items) where T : class
        {
            try
            {
                _logger.LogInformation(
                    $"Processing batch for {serviceName}.{methodName}");

                foreach (var item in items)
                {
                    var result = await CallServiceAsync<T>(
                        serviceName,
                        methodName,
                        item);

                    if (result is null)
                    {
                        _logger.LogWarning("Failed to process item");
                        continue;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch processing failed");
                return false;
            }
        }
    }

    /// <summary>
    /// Data models for bridge responses
    /// </summary>
    public sealed class ServiceInfo
    {
        public string ServiceName { get; set; } = "";
        public string Address { get; set; } = "";
        public string Status { get; set; } = "";
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public sealed class MetricsInfo
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageLatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public double P99LatencyMs { get; set; }
        public int ActiveStreams { get; set; }
        public double CacheHitRate { get; set; }
        public string Uptime { get; set; } = "";
    }

    public sealed class StreamInfo
    {
        public List<ActiveStream> ActiveStreams { get; set; } = new();
    }

    public sealed class ActiveStream
    {
        public string Id { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string MethodName { get; set; } = "";
        public string ClientAddress { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int MessagesReceived { get; set; }
        public int MessagesSent { get; set; }
        public long BytesReceived { get; set; }
        public long BytesSent { get; set; }
    }

    /// <summary>
    /// Dependency injection extension methods
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// Add gRPC-Web Bridge client to dependency injection
        public static IServiceCollection AddGrpcWebBridgeClient(
            this IServiceCollection services,
            string bridgeUrl,
            string? jwtToken = null)
        {
            services.AddHttpClient<GrpcWebBridgeClientExample>(
                client => client.BaseAddress = new Uri(bridgeUrl))
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            if (!string.IsNullOrEmpty(jwtToken))
            {
                services.AddSingleton(provider => new GrpcWebBridgeClientExample(
                    bridgeUrl,
                    provider.GetRequiredService<IHttpClientFactory>()
                        .CreateClient(nameof(GrpcWebBridgeClientExample)),
                    provider.GetRequiredService<ILogger<GrpcWebBridgeClientExample>>(),
                    jwtToken));
            }

            return services;
        }
    }

    /// <summary>
    /// Example usage in a console application
    /// </summary>
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            // Setup dependency injection
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddHttpClient();
            services.AddGrpcWebBridgeClient(
                "http://localhost:5000",
                jwtToken: "your-jwt-token-here");

            var provider = services.BuildServiceProvider();
            var logger = provider.GetRequiredService<ILoggerFactory>()
                .CreateLogger<Program>();

            var bridgeClient = provider.GetRequiredService<GrpcWebBridgeClientExample>();

            try
            {
                // Example 1: Check health
                var healthy = await bridgeClient.CheckHealthAsync();
                if (!healthy)
                {
                    logger.LogError("Bridge is not healthy");
                    return;
                }

                // Example 2: List services
                var services2 = await bridgeClient.ListServicesAsync();
                foreach (var service in services2 ?? new())
                {
                    logger.LogInformation($"Service: {service.ServiceName}");
                }

                // Example 3: Register a service
                await bridgeClient.RegisterServiceAsync(
                    "TestService",
                    "grpc://localhost:50051");

                // Example 4: Make an RPC call
                var result = await bridgeClient.CallServiceAsync<object>(
                    "TestService",
                    "GetData",
                    new { id = 42 });

                // Example 5: Get metrics
                var metrics = await bridgeClient.GetMetricsAsync();

                // Example 6: Call with retry
                var retryResult = await bridgeClient.CallWithRetryAsync<object>(
                    "TestService",
                    "GetData",
                    new { id = 42 });

                logger.LogInformation("Examples completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Example failed");
            }
        }
    }
}
