#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.BackgroundWorkers;
using GrpcWebBridge.Data;
using GrpcWebBridge.Integration;
using GrpcWebBridge.Services;
using GrpcWebBridge.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

// Register correlation ID management
services.AddCorrelationIdManager();

        // Register data access
        services.AddSingleton<IServiceRepository, ServiceRepository>();
        services.AddSingleton<GrpcConnectionManager>();

        // Register hosted services for background tasks
        services.AddHostedService<StreamCleanupService>();
services.AddHostedService<StreamingWorkerSupervisor>();

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

        var allowedOrigins = options?.Configuration.AllowedOrigins ?? ["*"];
        services.AddCors(corsOptions => corsOptions.AddPolicy("AllowGrpcWeb",
            policy =>
            {
                foreach (var origin in allowedOrigins)
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
    /// Adds authentication configuration with customisable JWT bearer options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureJwtBearer">
    ///   Optional delegate to configure <see cref="JwtBearerOptions"/> — authority, audience,
    ///   issuer signing key, token validation parameters, etc.  When omitted a minimal default
    ///   is registered so the pipeline can start; callers <b>must</b> supply an authority or
    ///   signing-key configuration before the app reaches production.
    /// </param>
    public static IServiceCollection AddGrpcWebBridgeAuthentication(
        this IServiceCollection services,
        Action<JwtBearerOptions>? configureJwtBearer = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Sensible defaults — callers should override Authority / TokenValidationParameters
                // via the configureJwtBearer delegate.
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = false,
                    RequireSignedTokens = false
                };

                configureJwtBearer?.Invoke(options);
            });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Registers Prometheus metric definitions for the bridge.
    /// Call <c>app.MapMetrics()</c> (from prometheus-net.AspNetCore) in the pipeline to expose
    /// the <c>/metrics</c> scrape endpoint consumed by Prometheus or compatible agents.
    /// </summary>
    public static IServiceCollection AddGrpcWebBridgePrometheus(
        this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        // Trigger static field initialisation so the metrics are registered with the
        // default Prometheus registry before any request arrives.
        _ = BridgePrometheusMetrics.RequestsTotal;
        _ = BridgePrometheusMetrics.RequestDuration;
        _ = BridgePrometheusMetrics.ActiveStreams;
        _ = BridgePrometheusMetrics.StreamErrorsTotal;

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry distributed tracing for the gRPC-Web bridge.
    /// Instruments ASP.NET Core request handling and exposes the bridge's own
    /// <see cref="BridgeActivitySource"/> so that proxy operations (protocol
    /// translation, authentication, streaming) appear as child spans.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="serviceName">
    ///   Resource name reported to the tracing backend. Defaults to <c>"grpc-web-bridge"</c>.
    /// </param>
    /// <param name="instanceName">
    ///   Optional instance identifier attached to every span as <c>bridge.instance</c>.
    ///   Useful in multi-instance deployments.
    /// </param>
    /// <param name="configureBuilder">
    ///   Optional delegate to customise the <see cref="TracerProviderBuilder"/> — for example
    ///   to add an OTLP or Zipkin exporter instead of the default console one.
    /// </param>
    public static IServiceCollection AddGrpcWebBridgeTracing(
        this IServiceCollection services,
        string serviceName = "grpc-web-bridge",
        string? instanceName = null,
        Action<TracerProviderBuilder>? configureBuilder = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        var resolvedInstance = string.IsNullOrWhiteSpace(instanceName) ? "default" : instanceName;

        services.AddSingleton(sp =>
            new TracingService(sp.GetRequiredService<ILogger<TracingService>>(), resolvedInstance));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: BridgeActivitySource.Version)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["bridge.instance"] = resolvedInstance
                }))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(BridgeActivitySource.Name)
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.RecordException = true;
                        opts.Filter = ctx =>
                            // Exclude metrics scrapes and health probes from traces to reduce noise
                            !ctx.Request.Path.StartsWithSegments("/metrics") &&
                            !ctx.Request.Path.StartsWithSegments("/health");
                    });

                configureBuilder?.Invoke(tracing);
            });

        return services;
    }
}

// StreamCleanupService lives in BackgroundWorkers/StreamCleanupService.cs
// alongside the other hosted workers.
