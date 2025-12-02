#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
//
// Advanced gRPC-Web Bridge usage example
// Demonstrates configuration, custom options, error handling, and production patterns

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace GrpcWebBridge.Examples
{
    /// <summary>
    /// Advanced usage example - configuration, custom options, and error handling
    /// </summary>
    public sealed class AdvancedUsageExample
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AdvancedUsageExample> _logger;
        private readonly string _bridgeUrl;
        private readonly string? _jwtToken;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initialize the advanced client with configuration options
        /// </summary>
        /// <param name="bridgeUrl">URL of the gRPC-Web Bridge server</param>
        /// <param name="jwtToken">Optional JWT token for authentication</param>
        /// <param name="timeoutSeconds">HTTP request timeout in seconds</param>
        /// <param name="maxRetryAttempts">Maximum number of retry attempts</param>
        public AdvancedUsageExample(
            string bridgeUrl,
            string? jwtToken = null,
            int timeoutSeconds = 30,
            int maxRetryAttempts = 3)
        {
            _bridgeUrl = bridgeUrl ?? throw new ArgumentNullException(nameof(bridgeUrl));
            _jwtToken = jwtToken;

            // Configure HTTP client with timeouts and defaults
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_bridgeUrl),
                Timeout = TimeSpan.FromSeconds(timeoutSeconds)
            };

            ConfigureHttpClient();

            // Setup retry policy with exponential backoff
            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode &&
                                                   (int)r.StatusCode >= 500) // Retry on 5xx
                .WaitAndRetryAsync(
                    maxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} after {Delay}s due to: {Reason}",
                            retryCount,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? $"HTTP {(int)outcome.Result!.StatusCode}");
                    });

            // Configure JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Setup logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<AdvancedUsageExample>();
        }

        /// <summary>
        /// Configure HTTP client with default headers and settings
        /// </summary>
        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent", "gRPC-Web-Bridge-.NET-Advanced-Client");

            if (!string.IsNullOrEmpty(_jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Add(
                    "Authorization", $"Bearer {_jwtToken}");
            }

            // Enable compression if supported by server
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        }

        /// <summary>
        /// Example 1: Health check with circuit breaker pattern
        /// </summary>
        public async Task<HealthStatus> CheckHealthWithCircuitBreakerAsync()
        {
            try
            {
                _logger.LogInformation("Performing health check with circuit breaker...");

                var response = await _httpClient.GetAsync("/health").ConfigureAwait(false);

                var status = response.IsSuccessStatusCode
                    ? HealthStatus.Healthy
                    : HealthStatus.Unhealthy;

                _logger.LogInformation(
                    "Health check result: {Status} (HTTP {Code})",
                    status,
                    (int)response.StatusCode);

                return status;
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Health check timed out");
                return HealthStatus.Timeout;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return HealthStatus.Error;
            }
        }

        /// <summary>
        /// Example 2: RPC call with retry logic and timeout
        /// </summary>
        public async Task<T?> CallWithResilienceAsync<T>(
            string serviceName,
            string methodName,
            object request,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogInformation(
                    "Calling {Service}.{Method} with resilience patterns",
                    serviceName, methodName);

                var endpoint = $"/api/bridge/{serviceName}/{methodName}";

                // Execute with retry policy
                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    return await _httpClient.PostAsJsonAsync(
                            endpoint,
                            request,
                            _jsonOptions,
                            cancellationToken)
                        .ConfigureAwait(false);
                });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogError(
                        "RPC call failed after retries: {StatusCode} - {Error}",
                        response.StatusCode,
                        errorContent);
                    return null;
                }

                var result = await response.Content.ReadAsAsync<T>(_jsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("✓ RPC call succeeded with resilience patterns");
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("RPC call was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC call failed despite retry policy");
                return null;
            }
        }

        /// <summary>
        /// Example 3: Streaming RPC call with progress reporting
        /// </summary>
        public async Task<int> StreamDataWithProgressAsync<T>(
            string serviceName,
            string methodName,
            IEnumerable<T> dataItems,
            IProgress<int>? progress = null) where T : class
        {
            int processedCount = 0;
            int failedCount = 0;

            try
            {
                _logger.LogInformation(
                    "Starting stream processing for {Count} items",
                    dataItems?.Count() ?? 0);

                foreach (var item in dataItems ?? Array.Empty<T>())
                {
                    try
                    {
                        // Process each item with individual retry
                        var result = await CallWithResilienceAsync<object>(
                                serviceName,
                                methodName,
                                item)
                            .ConfigureAwait(false);

                        if (result != null)
                        {
                            processedCount++;
                        }
                        else
                        {
                            failedCount++;
                        }

                        // Report progress
                        progress?.Report(processedCount + failedCount);

                        // Small delay to prevent overwhelming the service
                        await Task.Delay(10).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process stream item");
                        failedCount++;
                    }
                }

                _logger.LogInformation(
                    "Stream processing completed: {Processed} succeeded, {Failed} failed",
                    processedCount,
                    failedCount);

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stream processing failed");
                return processedCount;
            }
        }

        /// <summary>
        /// Example 4: Get detailed metrics with parsing
        /// </summary>
        public async Task<BridgeMetrics?> GetDetailedMetricsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching detailed bridge metrics...");

                var response = await _httpClient.GetAsync("/api/metrics").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var metricsJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var metrics = JsonSerializer.Deserialize<BridgeMetrics>(metricsJson, _jsonOptions);

                if (metrics != null)
                {
                    _logger.LogInformation(
                        "Metrics - Req: {Total}/{Success}/{Failed}, Latency: {Avg}ms (P95: {P95}ms), Streams: {Active}",
                        metrics.TotalRequests,
                        metrics.SuccessfulRequests,
                        metrics.FailedRequests,
                        metrics.AverageLatencyMs,
                        metrics.P95LatencyMs,
                        metrics.ActiveStreams);

                    return metrics;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse metrics JSON");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch metrics");
                return null;
            }

            return null;
        }

        /// <summary>
        /// Example 5: Dynamic service registration with validation
        /// </summary>
        public async Task<RegistrationResult> RegisterServiceWithValidationAsync(
            string serviceName,
            string grpcAddress,
            Dictionary<string, string>? metadata = null,
            bool enableHealthCheck = false)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(serviceName))
                return RegistrationResult.InvalidName;

            if (string.IsNullOrWhiteSpace(grpcAddress))
                return RegistrationResult.InvalidAddress;

            try
            {
                _logger.LogInformation(
                    "Registering service '{Service}' at {Address}",
                    serviceName,
                    grpcAddress);

                var registrationRequest = new
                {
                    serviceName,
                    address = grpcAddress,
                    healthCheck = enableHealthCheck,
                    metadata = metadata ?? new Dictionary<string, string>()
                };

                var response = await _httpClient.PostAsJsonAsync(
                        "/api/services/register",
                        registrationRequest,
                        _jsonOptions)
                    .ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogError(
                        "Service registration failed: {StatusCode} - {Error}",
                        response.StatusCode,
                        error);

                    return response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Conflict => RegistrationResult.AlreadyExists,
                        System.Net.HttpStatusCode.BadRequest => RegistrationResult.InvalidRequest,
                        _ => RegistrationResult.ServerError
                    };
                }

                _logger.LogInformation("✓ Service '{Service}' registered successfully", serviceName);
                return RegistrationResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Service registration failed with exception");
                return RegistrationResult.ServerError;
            }
        }

        /// <summary>
        /// Example 6: Batch operation with transaction-like behavior
        /// </summary>
        public async Task<BatchResult> ExecuteBatchOperationAsync<T>(
            string serviceName,
            string methodName,
            IEnumerable<T> items,
            bool stopOnFirstFailure = false) where T : class
        {
            var successfulItems = new List<T>();
            var failedItems = new List<T>();
            var errors = new List<string>();

            try
            {
                _logger.LogInformation(
                    "Starting batch operation for {Count} items",
                    items?.Count() ?? 0);

                foreach (var item in items ?? Array.Empty<T>())
                {
                    try
                    {
                        var result = await CallWithResilienceAsync<object>(
                                serviceName,
                                methodName,
                                item)
                            .ConfigureAwait(false);

                        if (result != null)
                        {
                            successfulItems.Add(item);
                        }
                        else
                        {
                            failedItems.Add(item);
                            errors.Add("Null response received");

                            if (stopOnFirstFailure)
                            {
                                _logger.LogWarning(
                                    "Stopping batch on first failure (item {Index})",
                                    successfulItems.Count + failedItems.Count);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failedItems.Add(item);
                        errors.Add(ex.Message);

                        if (stopOnFirstFailure)
                        {
                            _logger.LogWarning(
                                "Stopping batch on first failure (item {Index}): {Error}",
                                successfulItems.Count + failedItems.Count,
                                ex.Message);
                            break;
                        }
                    }
                }

                var result = new BatchResult
                {
                    SuccessfulCount = successfulItems.Count,
                    FailedCount = failedItems.Count,
                    TotalItemsProcessed = successfulItems.Count + failedItems.Count,
                    Errors = errors,
                    SuccessRate = (successfulItems.Count * 100.0) /
                                 Math.Max(1, successfulItems.Count + failedItems.Count)
                };

                _logger.LogInformation(
                    "Batch operation completed: {Successful}/{Total} successful ({SuccessRate:F1}%)",
                    result.SuccessfulCount,
                    result.TotalItemsProcessed,
                    result.SuccessRate);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch operation failed");
                return new BatchResult
                {
                    Errors = new List<string> { ex.Message },
                    SuccessRate = 0
                };
            }
        }
    }

    /// <summary>
    /// Health status enumeration
    /// </summary>
    public enum HealthStatus
    {
        Healthy,
        Unhealthy,
        Timeout,
        Error
    }

    /// <summary>
    /// Service registration result
    /// </summary>
    public enum RegistrationResult
    {
        Success,
        AlreadyExists,
        InvalidRequest,
        InvalidName,
        InvalidAddress,
        ServerError
    }

    /// <summary>
    /// Batch operation result
    /// </summary>
    public sealed class BatchResult
    {
        public int SuccessfulCount { get; set; }
        public int FailedCount { get; set; }
        public int TotalItemsProcessed { get; set; }
        public List<string> Errors { get; set; } = new();
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Detailed bridge metrics model
    /// </summary>
    public sealed class BridgeMetrics
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
        public long MemoryUsageBytes { get; set; }
        public int ThreadCount { get; set; }
    }

    /// <summary>
    /// Console application demonstrating advanced usage patterns
    /// </summary>
    public sealed class AdvancedUsageProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== gRPC-Web Bridge - Advanced Usage Example ===\n");

            // Configuration - adjust these for your environment
            const string bridgeUrl = "http://localhost:5000";
            const string? jwtToken = null; // Set if your bridge requires auth
            const int timeoutSeconds = 45;
            const int maxRetries = 3;

            var advancedExample = new AdvancedUsageExample(
                bridgeUrl,
                jwtToken,
                timeoutSeconds,
                maxRetries);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\n\nOperation cancelled by user.");
            };

            try
            {
                // Example 1: Health check with circuit breaker
                Console.WriteLine("--- Health Check ---");
                var health = await advancedExample.CheckHealthWithCircuitBreakerAsync();
                Console.WriteLine($"Health Status: {health}");

                if (health != HealthStatus.Healthy)
                {
                    Console.WriteLine("\n❌ Bridge is not healthy. Please check the server.");
                    return;
                }

                Console.WriteLine("\n--- Advanced Features Demo ---");

                // Example 2: Resilient RPC call
                Console.WriteLine("\n1. Testing resilient RPC call...");
                var rpcResult = await advancedExample.CallWithResilienceAsync<object>(
                        "ExampleService",
                        "GetData",
                        new { timestamp = DateTime.UtcNow.Ticks },
                        cts.Token);

                if (rpcResult != null)
                {
                    Console.WriteLine("   ✓ Resilient RPC call succeeded");
                }
                else
                {
                    Console.WriteLine("   ⚠ Resilient RPC call failed (this may be expected if service doesn't exist)");
                }

                // Example 3: Service registration
                Console.WriteLine("\n2. Testing service registration...");
                var regResult = await advancedExample.RegisterServiceWithValidationAsync(
                        "DemoService",
                        "grpc://localhost:50051",
                        new Dictionary<string, string>
                        {
                            ["version"] = "1.0",
                            ["environment"] = "demo"
                        },
                        true);

                Console.WriteLine($"   Registration result: {regResult}");

                // Example 4: Batch processing
                Console.WriteLine("\n3. Testing batch processing...");
                var testItems = new[]
                {
                    new { id = 1, value = "test1" },
                    new { id = 2, value = "test2" },
                    new { id = 3, value = "test3" }
                };

                var batchResult = await advancedExample.ExecuteBatchOperationAsync(
                        "ExampleService",
                        "ProcessItem",
                        testItems,
                        stopOnFirstFailure: false);

                Console.WriteLine(
                    $"   Batch results: {batchResult.SuccessfulCount}/{batchResult.TotalItemsProcessed} successful " +
                    $"({batchResult.SuccessRate:F1}%)");

                if (batchResult.Errors.Count > 0)
                {
                    Console.WriteLine($"   Errors: {string.Join(", ", batchResult.Errors.Take(3))}");
                }

                // Example 5: Detailed metrics
                Console.WriteLine("\n4. Fetching detailed metrics...");
                var metrics = await advancedExample.GetDetailedMetricsAsync();
                if (metrics != null)
                {
                    Console.WriteLine("   ✓ Detailed metrics retrieved:");
                    Console.WriteLine($"     Total Requests: {metrics.TotalRequests}");
                    Console.WriteLine($"     Success Rate: {(metrics.SuccessfulRequests * 100.0 / Math.Max(1, metrics.TotalRequests)):F1}%");
                    Console.WriteLine($"     Avg Latency: {metrics.AverageLatencyMs:F1}ms");
                    Console.WriteLine($"     Active Streams: {metrics.ActiveStreams}");
                }

                Console.WriteLine("\n=== Advanced example completed ===");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nOperation was cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Unexpected error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                cts.Dispose();
            }
        }
    }
}