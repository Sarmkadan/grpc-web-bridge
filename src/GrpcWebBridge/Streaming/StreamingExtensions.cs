// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Extension methods for registering the bidirectional streaming subsystem —
/// engine, session manager, adaptive controller, and diagnostics service —
/// into an <see cref="IServiceCollection"/>.
/// </summary>
public static class StreamingExtensions
{
    /// <summary>
    /// Registers the core bidirectional streaming infrastructure using the supplied
    /// <see cref="FlowControlOptions"/>.
    /// <para>
    /// Services registered (all as singletons unless noted):
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="FlowControlOptions"/> — shared configuration record.</item>
    ///   <item>
    ///     <see cref="IBidirectionalStreamingEngine"/> → <see cref="BidirectionalStreamingEngine"/>
    ///     — central stream lifecycle manager.
    ///   </item>
    ///   <item>
    ///     <see cref="StreamingSessionManager"/> — groups streams into logical client sessions.
    ///   </item>
    ///   <item>
    ///     <see cref="EventBus"/> — application event bus (registered via
    ///     <c>TryAddSingleton</c>; skipped when already registered by the host).
    ///   </item>
    ///   <item>
    ///     <see cref="AdaptiveFlowController"/> (hosted service) — registered only when
    ///     <see cref="FlowControlOptions.Mode"/> is <see cref="FlowControlMode.Adaptive"/>.
    ///   </item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="options">
    /// Flow-control configuration. When <c>null</c>, <see cref="FlowControlOptions"/>
    /// property defaults are used. Validation is run before registration.
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddBidirectionalStreaming(
        this IServiceCollection services,
        FlowControlOptions? options = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        options ??= new FlowControlOptions();
        options.Validate();

        // TryAdd prevents double-registration when the host calls this method more than once
        // or when the application composes multiple feature modules.
        services.TryAddSingleton(options);

        services.TryAddSingleton<EventBus>();

        services.TryAddSingleton<IBidirectionalStreamingEngine>(sp =>
            new BidirectionalStreamingEngine(
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetRequiredService<FlowControlOptions>(),
                sp.GetService<EventBus>()));

        services.TryAddSingleton<StreamingSessionManager>();

        if (options.Mode == FlowControlMode.Adaptive)
            services.AddHostedService<AdaptiveFlowController>();

        return services;
    }

    /// <summary>
    /// Registers the core bidirectional streaming infrastructure, deriving the
    /// <see cref="FlowControlOptions"/> from a functional transform applied to the
    /// default configuration.
    /// <para>
    /// Example:
    /// <code>
    /// services.AddBidirectionalStreaming(opts => opts with
    /// {
    ///     Mode = FlowControlMode.Adaptive,
    ///     InitialWindowSize = 128
    /// });
    /// </code>
    /// </para>
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">
    /// A function that receives the default <see cref="FlowControlOptions"/> and returns
    /// a configured instance. The idiomatic approach is to use a <c>with</c> expression.
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configure"/> is <c>null</c>.
    /// </exception>
    public static IServiceCollection AddBidirectionalStreaming(
        this IServiceCollection services,
        Func<FlowControlOptions, FlowControlOptions> configure)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        var options = configure(new FlowControlOptions());
        return AddBidirectionalStreaming(services, options);
    }

    /// <summary>
    /// Registers <see cref="StreamDiagnosticsService"/> as a hosted background service that
    /// periodically emits aggregate metrics and flags high-backpressure or zero-activity streams.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="diagnosticsInterval">
    /// Interval between collection passes. Defaults to <c>60 seconds</c>.
    /// </param>
    /// <param name="staleThreshold">
    /// Streams with zero messages since last collection are flagged after this idle duration.
    /// Defaults to <c>5 minutes</c>.
    /// </param>
    /// <param name="backpressureWarnThreshold">
    /// Backpressure-event ratio above which a per-stream warning is logged.
    /// Defaults to <c>0.10</c> (10 %).
    /// </param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddStreamingDiagnostics(
        this IServiceCollection services,
        TimeSpan? diagnosticsInterval = null,
        TimeSpan? staleThreshold = null,
        double backpressureWarnThreshold = 0.10)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton(new StreamDiagnosticsOptions
        {
            CollectionInterval = diagnosticsInterval ?? TimeSpan.FromSeconds(60),
            StaleStreamThreshold = staleThreshold ?? TimeSpan.FromMinutes(5),
            BackpressureWarnThreshold = backpressureWarnThreshold
        });

        services.AddHostedService<StreamDiagnosticsService>();

        return services;
    }

    /// <summary>
    /// Convenience method that registers both the core streaming infrastructure and
    /// the diagnostics service in a single call, using the high-throughput preset.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddHighThroughputBidirectionalStreaming(
        this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        return services
            .AddBidirectionalStreaming(FlowControlOptions.HighThroughput)
            .AddStreamingDiagnostics();
    }

    /// <summary>
    /// Convenience method that registers both the core streaming infrastructure and
    /// the diagnostics service in a single call, using the low-latency preset.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The original <paramref name="services"/> for chaining.</returns>
    public static IServiceCollection AddLowLatencyBidirectionalStreaming(
        this IServiceCollection services)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        return services
            .AddBidirectionalStreaming(FlowControlOptions.LowLatency)
            .AddStreamingDiagnostics(diagnosticsInterval: TimeSpan.FromSeconds(30));
    }
}
