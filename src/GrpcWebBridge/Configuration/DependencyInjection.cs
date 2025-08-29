// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Data;
using GrpcWebBridge.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GrpcWebBridge.Configuration;

/// <summary>
/// Extension methods for dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds all gRPC-Web bridge services to the dependency injection container
    /// </summary>
    public static IServiceCollection AddGrpcWebBridge(
        this IServiceCollection services,
        GrpcWebBridgeOptions? options = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // Register configuration
        options ??= new GrpcWebBridgeOptions();
        services.AddSingleton(options);

        // Register core services
        services.AddSingleton<ProtocolTranslationService>();
        services.AddSingleton<StreamingService>();
        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<ServiceRegistry>();

        // Register data access
        services.AddSingleton<IServiceRepository, ServiceRepository>();
        services.AddSingleton<GrpcConnectionManager>();

        // Register hosted services for background tasks
        services.AddHostedService<StreamCleanupService>();

        return services;
    }

    /// <summary>
    /// Adds gRPC-Web bridge with custom configuration
    /// </summary>
    public static IServiceCollection AddGrpcWebBridge(
        this IServiceCollection services,
        Action<GrpcWebBridgeOptions> configureOptions)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configureOptions is null)
            throw new ArgumentNullException(nameof(configureOptions));

        var options = new GrpcWebBridgeOptions();
        configureOptions(options);

        return AddGrpcWebBridge(services, options);
    }

    /// <summary>
    /// Adds OpenAPI/Swagger documentation
    /// </summary>
    public static IServiceCollection AddGrpcWebBridgeSwagger(
        this IServiceCollection services,
        string title = "gRPC-Web Bridge API",
        string version = "1.0.0")
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddOpenApi();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(version, new()
            {
                Title = title,
                Version = version,
                Description = "gRPC-Web bridge server for .NET - protocol translation, streaming support, authentication middleware, Swagger docs",
                Contact = new()
                {
                    Name = "Vladyslav Zaiets",
                    Url = new Uri("https://sarmkadan.com")
                },
                License = new()
                {
                    Name = "MIT",
                    Url = new Uri("https://opensource.org/licenses/MIT")
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Adds CORS configuration for gRPC-Web
    /// </summary>
    public static IServiceCollection AddGrpcWebBridgeCors(
        this IServiceCollection services,
        GrpcWebBridgeOptions? options = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        options ??= new GrpcWebBridgeOptions();

        services.AddCors(options => options.AddPolicy("AllowGrpcWeb",
            policy =>
            {
                foreach (var origin in options.Configuration.AllowedOrigins)
                {
                    if (origin == "*")
                    {
                        policy.AllowAnyOrigin();
                    }
                    else
                    {
                        policy.WithOrigins(origin);
                    }
                }

                policy.AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }));

        return services;
    }

    /// <summary>
    /// Adds authentication configuration
    /// </summary>
    public static IServiceCollection AddGrpcWebBridgeAuthentication(
        this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", options =>
            {
                options.Authority = "https://sarmkadan.com";
                options.TokenValidationParameters = new()
                {
                    ValidateAudience = false
                };
            });

        services.AddAuthorization();

        return services;
    }
}

/// <summary>
/// Background service for cleaning up idle streams
/// </summary>
public class StreamCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StreamCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public StreamCleanupService(IServiceProvider serviceProvider, ILogger<StreamCleanupService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_cleanupInterval);

        _logger.LogInformation("Stream cleanup service started");

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var streamingService = _serviceProvider.GetRequiredService<StreamingService>();
                streamingService.CleanupIdleStreams();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stream cleanup");
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stream cleanup service stopping");
        return base.StopAsync(cancellationToken);
    }
}
