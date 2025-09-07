#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Data;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Configuration;

/// <summary>
/// Service for configuring startup services and seed data
/// </summary>
public sealed class StartupConfiguration
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupConfiguration> _logger;

    public StartupConfiguration(IServiceProvider serviceProvider, ILogger<StartupConfiguration> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes the bridge with default services
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing gRPC-Web Bridge startup configuration");

        try
        {
            // Register sample services
            await RegisterDefaultServicesAsync();

            _logger.LogInformation("Startup configuration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup configuration");
            throw;
        }
    }

    /// <summary>
    /// Registers default sample services
    /// </summary>
    private async Task RegisterDefaultServicesAsync()
    {
        var repository = _serviceProvider.GetRequiredService<IServiceRepository>();

        // Sample Service 1: UserService
        var userService = new GrpcService(
            name: "UserService",
            packageName: "example.api",
            endpoint: "localhost",
            port: 50051);

        userService.Description = "Service for user management operations";
        userService.UseTls = false;

        var getUserMethod = new GrpcMethod(
            name: "GetUser",
            fullName: "/example.api.UserService/GetUser",
            type: MethodType.Unary,
            inputMessage: "GetUserRequest",
            outputMessage: "UserResponse");

        getUserMethod.Description = "Retrieves user by ID";
        getUserMethod.AddInputParameter(new MethodParameter("user_id", "string", 1, true));
        userService.AddMethod(getUserMethod);

        var listUsersMethod = new GrpcMethod(
            name: "ListUsers",
            fullName: "/example.api.UserService/ListUsers",
            type: MethodType.ServerStreaming,
            inputMessage: "ListUsersRequest",
            outputMessage: "UserResponse");

        listUsersMethod.Description = "Streams all users";
        userService.AddMethod(listUsersMethod);

        await repository.AddAsync(userService);
        _logger.LogInformation("Registered default service: {ServiceName}", userService.FullName);

        // Sample Service 2: OrderService
        var orderService = new GrpcService(
            name: "OrderService",
            packageName: "example.api",
            endpoint: "localhost",
            port: 50052);

        orderService.Description = "Service for order management and processing";
        orderService.UseTls = false;

        var createOrderMethod = new GrpcMethod(
            name: "CreateOrder",
            fullName: "/example.api.OrderService/CreateOrder",
            type: MethodType.Unary,
            inputMessage: "CreateOrderRequest",
            outputMessage: "OrderResponse");

        createOrderMethod.Description = "Creates a new order";
        createOrderMethod.AddInputParameter(new MethodParameter("customer_id", "string", 1, true));
        createOrderMethod.AddInputParameter(new MethodParameter("items", "Item", 2, true));
        orderService.AddMethod(createOrderMethod);

        var trackOrderMethod = new GrpcMethod(
            name: "TrackOrder",
            fullName: "/example.api.OrderService/TrackOrder",
            type: MethodType.ServerStreaming,
            inputMessage: "TrackOrderRequest",
            outputMessage: "OrderStatusUpdate");

        trackOrderMethod.Description = "Streams order status updates";
        orderService.AddMethod(trackOrderMethod);

        await repository.AddAsync(orderService);
        _logger.LogInformation("Registered default service: {ServiceName}", orderService.FullName);
    }

    /// <summary>
    /// Validates system configuration
    /// </summary>
    public void ValidateConfiguration(GrpcWebBridgeOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        try
        {
            options.Validate();
            _logger.LogInformation("Configuration validation passed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration validation failed");
            throw;
        }
    }

    /// <summary>
    /// Gets system information
    /// </summary>
    public SystemInfo GetSystemInfo()
    {
        var options = _serviceProvider.GetRequiredService<GrpcWebBridgeOptions>();

        return new SystemInfo
        {
            InstanceId = options.Configuration.InstanceId,
            InstanceName = options.Configuration.InstanceName ?? "default",
            Environment = options.Configuration.Environment,
            Version = "1.0.0",
            StartTime = DateTime.UtcNow,
            MaxStreamCount = options.Configuration.MaxStreamCount,
            MaxMessageSize = options.Configuration.MaxMessageSize
        };
    }
}

/// <summary>
/// System information model
/// </summary>
public sealed class SystemInfo
{
    public string? InstanceId { get; set; }
    public string? InstanceName { get; set; }
    public string? Environment { get; set; }
    public string? Version { get; set; }
    public DateTime StartTime { get; set; }
    public int MaxStreamCount { get; set; }
    public int MaxMessageSize { get; set; }
}
