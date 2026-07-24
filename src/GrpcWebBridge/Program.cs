#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Grpc.AspNetCore.Web;
using GrpcWebBridge.Configuration;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Endpoints;
using GrpcWebBridge.Integration;
using GrpcWebBridge.Middleware;
using GrpcWebBridge.Services;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/grpc-web-bridge-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

var services = builder.Services;

// Register gRPC services
services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = 4 * 1024 * 1024; // 4MB
    options.MaxSendMessageSize = 4 * 1024 * 1024;
});

// Register gRPC-Web bridge services
services.AddGrpcWebBridge(options =>
{
    options.WithDevelopment()
        .WithMaxStreamCount(10000)
        .WithCompression(true, 6)
        .WithSwagger(true)
        .WithCors(true)
        .AddAllowedOrigins("*");
});

// Add Swagger/OpenAPI
services.AddGrpcWebBridgeSwagger(
    title: "gRPC-Web Bridge API",
    version: "1.0.0");

// Add CORS
services.AddGrpcWebBridgeCors();

// Add authentication — configure JWT bearer options to match your identity provider.
// Example: set Authority to your OIDC/OAuth2 server, or supply a signing key directly.
services.AddGrpcWebBridgeAuthentication(jwt =>
{
    // jwt.Authority = "https://your-identity-provider.example.com";
    // jwt.TokenValidationParameters.ValidAudience = "grpc-web-bridge";
    // Override with your settings via environment variables or appsettings.json.
});

// Add Prometheus metrics (opt-in; exposes /metrics for Prometheus scraping)
services.AddGrpcWebBridgePrometheus();

// Add OpenTelemetry distributed tracing
services.AddGrpcWebBridgeTracing(
    serviceName: "grpc-web-bridge",
    instanceName: builder.Configuration["GrpcWebBridge:InstanceName"],
    configureBuilder: tracing =>
    {
        // Console exporter is useful for development; replace with OTLP/Zipkin in production.
        if (builder.Environment.IsDevelopment())
            tracing.AddConsoleExporter();
    });

// Add controllers for REST endpoints
services.AddControllers();

// Add RequestContextManager for ambient context tracking
services.AddRequestContextManager();

var app = builder.Build();

// Configure middleware
// Error handling goes first so every exception below it becomes a proper JSON error response.
app.UseErrorHandling();
app.UseGrpcWebContentTypeValidation();
app.UseRequestLogging();
app.UseRouting();
app.UseCorrelationId();
app.UseRequestContext(); // Add RequestContext middleware for ambient context tracking
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.UseCors("AllowGrpcWeb");
app.UseAuthentication();
app.UseAuthorization();

// Map controller routes (REST API controllers)
app.MapControllers();

// Expose /metrics endpoint for Prometheus scraping.
app.MapMetrics();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map reflection endpoints
app.MapGrpcReflectionEndpoints();

// Health check endpoints
app.MapHealthEndpoints();

// Legacy health check endpoint (kept for backward compatibility)
app.MapGet("/health", async (ServiceRegistry registry, StreamingService streaming) =>
{
    var uptime = DateTime.UtcNow - HealthEndpoints.GetStartupTime();

    var response = new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        uptime = uptime.ToString("c"),
        uptime_seconds = (int)uptime.TotalSeconds,
        services = registry.RegisteredServiceCount,
        activeStreams = streaming.ActiveStreamCount,
        version = "1.0.0"
    };

    return Results.Ok(response);
})
.WithName("Health Check")
.WithOpenApi();

// Service list endpoint
app.MapGet("/api/services", async (ServiceRegistry registry) =>
{
    var services = registry.ListServices().Select(s => new
    {
        s.Id,
        s.Name,
        s.FullName,
        s.Endpoint,
        s.Port,
        s.Status,
        methodCount = s.Methods.Count,
        s.CreatedAt
    });

    return Results.Ok(services);
})
.WithName("List Services")
.WithOpenApi();

// Service details endpoint
app.MapGet("/api/services/{serviceId}", async (string serviceId, ServiceRegistry registry) =>
{
    var service = registry.ListServices().FirstOrDefault(s => s.Id == serviceId);
    if (service is null)
        return Results.NotFound(new { error = "Service not found" });

    var details = new
    {
        service.Id,
        service.Name,
        service.FullName,
        service.Endpoint,
        service.Port,
        service.Status,
        service.Description,
        methods = service.Methods.Select(m => new
        {
            m.Name,
            m.FullName,
            m.Type,
            m.InputMessageType,
            m.OutputMessageType,
            m.IsDeprecated
        }),
        service.CreatedAt,
        service.UpdatedAt
    };

    return Results.Ok(details);
})
.WithName("Get Service Details")
.WithOpenApi();

// Stream statistics endpoint
app.MapGet("/api/streams", (StreamingService streaming) =>
{
    var streamIds = streaming.GetAllStreamIds();
    var stats = streamIds.Select(id =>
    {
        try
        {
            return streaming.GetStreamStatistics(id);
        }
        catch
        {
            return null;
        }
    })
    .Where(s => s is not null)
    .ToList();

    return Results.Ok(new
    {
        activeStreamCount = stats.Count,
        streams = stats
    });
})
.WithName("Stream Statistics")
.WithOpenApi();

try
{
    Log.Information("Starting gRPC-Web Bridge server...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
