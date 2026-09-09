#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Services;

namespace GrpcWebBridge.BackgroundWorkers;

/// <summary>
/// Background service for cleaning up idle streams.
/// Runs on a fixed 5-minute timer and delegates the actual sweep to
/// <see cref="StreamingService.CleanupIdleStreams"/>.
/// </summary>
public class StreamCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StreamCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamCleanupService"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve the streaming service.</param>
    /// <param name="logger">The logger used to record cleanup service activity and errors.</param>
    public StreamCleanupService(IServiceProvider serviceProvider, ILogger<StreamCleanupService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs the periodic stream cleanup loop until cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">The token that signals when the cleanup loop should stop.</param>
    /// <returns>A task that represents the lifetime of the cleanup loop.</returns>
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

    /// <summary>
    /// Logs that the cleanup service is stopping and stops the background service.
    /// </summary>
    /// <param name="cancellationToken">The token that indicates the stop operation should no longer be graceful.</param>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stream cleanup service stopping");
        return base.StopAsync(cancellationToken);
    }
}
