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
