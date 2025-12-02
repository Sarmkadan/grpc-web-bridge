#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================
//
// ASP.NET Core Dependency Injection integration example
// Demonstrates how to wire the gRPC-Web Bridge client into ASP.NET DI
// Shows configuration, options pattern, and proper service lifecycle management

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GrpcWebBridge.Examples
{
    /// <summary>
    /// Configuration options for gRPC-Web Bridge client
    /// </summary>
    public sealed class GrpcWebBridgeOptions
    {
        /// <summary>
        /// URL of the gRPC-Web Bridge server
        /// </summary>
        public string BridgeUrl { get; set; } = "http://localhost:5000";

        /// <summary>
        /// Optional JWT token for authentication
        /// </summary>
        public string? JwtToken { get; set; }

        /// <summary>
        /// HTTP client timeout in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Enable detailed logging
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = false;

        /// <summary>
        /// Maximum number of concurrent connections
        /// </summary>
        public int MaxConnections { get; set; } = 100;

        /// <summary>
        /// Enable automatic health checks
        /// </summary>
        public bool EnableHealthChecks { get; set; } = true;

        /// <summary>
        /// Health check interval in seconds
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 30;
    }

    /// <summary>
    /// Configuration options validator
    /// </summary>
    public sealed class GrpcWebBridgeOptionsValidator
    {
        public static void Validate(GrpcWebBridgeOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.BridgeUrl))
            {
                throw new ArgumentException("Bridge URL is required", nameof(options.BridgeUrl));
            }

            if (options.TimeoutSeconds <= 0)
            {
                throw new ArgumentException("Timeout must be positive", nameof(options.TimeoutSeconds));
            }

            if (options.MaxConnections <= 0)
            {
                throw new ArgumentException("Max connections must be positive", nameof(options.MaxConnections));
            }
        }
    }

    /// <summary>
    /// Extension methods for configuring gRPC-Web Bridge in ASP.NET Core
    /// </summary>
    public static class GrpcWebBridgeServiceCollectionExtensions
    {
        /// <summary>
        /// Add gRPC-Web Bridge client to the service collection
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Configuration section containing GrpcWebBridge options</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddGrpcWebBridgeClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            // Bind options from configuration
            services.Configure<GrpcWebBridgeOptions>(configuration.GetSection("GrpcWebBridge"));

            // Register the options validator as singleton
            services.AddSingleton<GrpcWebBridgeOptionsValidator>();

            // Register the bridge client as singleton (recommended for production)
            services.AddSingleton<GrpcWebBridgeClient>();

            // Register the advanced client as scoped (if using scoped services)
            services.AddScoped<AdvancedGrpcWebBridgeClient>();

            // Register HTTP client factory for named clients
            services.AddHttpClient("GrpcWebBridge", (serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<GrpcWebBridgeOptions>>().Value;
                client.BaseAddress = new Uri(options.BridgeUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Add("User-Agent", "gRPC-Web-Bridge-.NET-Integration");

                if (!string.IsNullOrEmpty(options.JwtToken))
                {
                    client.DefaultRequestHeaders.Add(
                        "Authorization",
                        $"Bearer {options.JwtToken}");
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                MaxConnectionsPerServer = 100,
                AllowAutoRedirect = true
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            // Register background health monitor
            services.AddHostedService<GrpcWebBridgeHealthMonitor>();

            return services;
        }

        /// <summary>
        /// Add gRPC-Web Bridge client with explicit options
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Action to configure options</param>
        /// <returns>Service collection for method chaining</returns>
        public static IServiceCollection AddGrpcWebBridgeClient(
            this IServiceCollection services,
            Action<GrpcWebBridgeOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            // Configure options
            services.Configure(configureOptions);

            // Register services
            services.AddSingleton<GrpcWebBridgeOptionsValidator>();
            services.AddSingleton<GrpcWebBridgeClient>();
            services.AddScoped<AdvancedGrpcWebBridgeClient>();
            services.AddHttpClient("GrpcWebBridge");
            services.AddHostedService<GrpcWebBridgeHealthMonitor>();

            return services;
        }
    }

    /// <summary>
    /// Basic gRPC-Web Bridge client for DI integration
    /// </summary>
    public sealed class GrpcWebBridgeClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GrpcWebBridgeClient> _logger;
        private readonly GrpcWebBridgeOptions _options;

        /// <summary>
        /// Initialize the client
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory</param>
        /// <param name="logger">Logger</param>
        /// <param name="options">Configuration options</param>
        public GrpcWebBridgeClient(
            IHttpClientFactory httpClientFactory,
            ILogger<GrpcWebBridgeClient> logger,
            IOptions<GrpcWebBridgeOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("GrpcWebBridge");
            _logger = logger;
            _options = options.Value;

            _logger.LogInformation(
                "gRPC-Web Bridge client initialized for {BridgeUrl}",
                _options.BridgeUrl);
        }

        /// <summary>
        /// Check if bridge is healthy
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/health").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return false;
            }
        }

        /// <summary>
        /// Make a simple RPC call
        /// </summary>
        public async Task<T?> CallServiceAsync<T>(
            string serviceName,
            string methodName,
            object request)
        {
            try
            {
                var endpoint = $"/api/bridge/{serviceName}/{methodName}";
                var response = await _httpClient.PostAsJsonAsync(endpoint, request).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogError("RPC call failed: {Error}", error);
                    return default;
                }

                return await response.Content.ReadAsAsync<T>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RPC call failed");
                return default;
            }
        }
    }

    /// <summary>
    /// Advanced gRPC-Web Bridge client with additional features
    /// </summary>
    public sealed class AdvancedGrpcWebBridgeClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AdvancedGrpcWebBridgeClient> _logger;
        private readonly GrpcWebBridgeOptions _options;

        public AdvancedGrpcWebBridgeClient(
            IHttpClientFactory httpClientFactory,
            ILogger<AdvancedGrpcWebBridgeClient> logger,
            IOptions<GrpcWebBridgeOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("GrpcWebBridge");
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Get bridge metrics
        /// </summary>
        public async Task<BridgeMetrics?> GetMetricsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/metrics").ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsAsync<BridgeMetrics>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get metrics");
                return null;
            }
        }

        /// <summary>
        /// Register a service dynamically
        /// </summary>
        public async Task<bool> RegisterServiceAsync(
            string serviceName,
            string grpcAddress,
            bool enableHealthCheck = false)
        {
            try
            {
                var request = new
                {
                    serviceName,
                    address = grpcAddress,
                    healthCheck = enableHealthCheck
                };

                var response = await _httpClient.PostAsJsonAsync(
                        "/api/services/register",
                        request)
                    .ConfigureAwait(false);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register service {Service}", serviceName);
                return false;
            }
        }
    }

    /// <summary>
    /// Background service for monitoring bridge health
    /// </summary>
    public sealed class GrpcWebBridgeHealthMonitor : BackgroundService
    {
        private readonly ILogger<GrpcWebBridgeHealthMonitor> _logger;
        private readonly GrpcWebBridgeClient _bridgeClient;
        private readonly GrpcWebBridgeOptions _options;
        private Timer? _healthCheckTimer;

        public GrpcWebBridgeHealthMonitor(
            ILogger<GrpcWebBridgeHealthMonitor> logger,
            GrpcWebBridgeClient bridgeClient,
            IOptions<GrpcWebBridgeOptions> options)
        {
            _logger = logger;
            _bridgeClient = bridgeClient;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.EnableHealthChecks)
            {
                _logger.LogInformation("Health monitoring disabled");
                return;
            }

            _logger.LogInformation(
                "Starting health monitoring (every {Interval}s)",
                _options.HealthCheckIntervalSeconds);

            // Initial health check
            await CheckHealthAsync().ConfigureAwait(false);

            // Setup periodic health checks
            _healthCheckTimer = new Timer(
                async _ => await CheckHealthAsync().ConfigureAwait(false),
                null,
                TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds),
                TimeSpan.FromSeconds(_options.HealthCheckIntervalSeconds));
        }

        private async Task CheckHealthAsync()
        {
            try
            {
                var isHealthy = await _bridgeClient.IsHealthyAsync().ConfigureAwait(false);

                if (isHealthy)
                {
                    _logger.LogDebug("Bridge is healthy");
                }
                else
                {
                    _logger.LogWarning("Bridge health check failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check threw exception");
            }
        }

        public override void Dispose()
        {
            _healthCheckTimer?.Dispose();
            base.Dispose();
        }
    }

    /// <summary>
    /// ASP.NET Core application demonstrating DI integration
    /// </summary>
    public sealed class IntegrationExampleProgram
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== gRPC-Web Bridge - ASP.NET Core DI Integration Example ===\n");

            // Build the host
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Load configuration from appsettings.json
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables("GRPC_WEB_BRIDGE_");
                    config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Add gRPC-Web Bridge client with configuration
                    services.AddGrpcWebBridgeClient(hostContext.Configuration);

                    // Register your application services
                    services.AddScoped<DemoService>();

                    // Register background workers
                    services.AddHostedService<DemoWorker>();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .Build();

            try
            {
                // Start the application
                Console.WriteLine("Starting ASP.NET Core application...");

                await host.StartAsync().ConfigureAwait(false);

                Console.WriteLine("✓ Application started successfully");
                Console.WriteLine($"  Bridge URL: {host.Services.GetRequiredService<IOptions<GrpcWebBridgeOptions>>().Value.BridgeUrl}");
                Console.WriteLine($"  Health checks: {host.Services.GetRequiredService<IOptions<GrpcWebBridgeOptions>>().Value.EnableHealthChecks}");

                // Wait for shutdown
                await host.WaitForShutdownAsync().ConfigureAwait(false);

                Console.WriteLine("\nApplication stopped gracefully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Application failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                await host.StopAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                host.Dispose();
            }
        }
    }

    /// <summary>
    /// Example service demonstrating bridge client usage
    /// </summary>
    public sealed class DemoService
    {
        private readonly GrpcWebBridgeClient _bridgeClient;
        private readonly ILogger<DemoService> _logger;

        public DemoService(
            GrpcWebBridgeClient bridgeClient,
            ILogger<DemoService> logger)
        {
            _bridgeClient = bridgeClient;
            _logger = logger;
        }

        public async Task<string?> GetDataFromBridgeAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching data from bridge for id {Id}", id);

                var result = await _bridgeClient.CallServiceAsync<object>(
                        "ExampleService",
                        "GetData",
                        new { id })
                    .ConfigureAwait(false);

                return result?.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get data from bridge");
                return null;
            }
        }
    }

    /// <summary>
    /// Background worker demonstrating bridge client usage
    /// </summary>
    public sealed class DemoWorker : BackgroundService
    {
        private readonly ILogger<DemoWorker> _logger;
        private readonly GrpcWebBridgeClient _bridgeClient;
        private readonly DemoService _demoService;
        private readonly IHostApplicationLifetime _lifetime;

        public DemoWorker(
            ILogger<DemoWorker> logger,
            GrpcWebBridgeClient bridgeClient,
            DemoService demoService,
            IHostApplicationLifetime lifetime)
        {
            _logger = logger;
            _bridgeClient = bridgeClient;
            _demoService = demoService;
            _lifetime = lifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Demo worker started");

            try
            {
                // Wait for application fully started
                await Task.Delay(2000, stoppingToken).ConfigureAwait(false);

                // Example: Make an RPC call
                var result = await _demoService.GetDataFromBridgeAsync(42).ConfigureAwait(false);

                if (result != null)
                {
                    _logger.LogInformation("Worker received data: {Data}", result);
                }
                else
                {
                    _logger.LogWarning("Worker received null data (service may not exist)");
                }

                // Example: Get metrics
                var metrics = await _bridgeClient.IsHealthyAsync().ConfigureAwait(false);
                _logger.LogInformation("Bridge health: {Healthy}", metrics);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Worker cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker failed");
            }
            finally
            {
                _lifetime.StopApplication();
            }
        }
    }
}
