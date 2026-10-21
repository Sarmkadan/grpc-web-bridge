#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Configuration;
using GrpcWebBridge.Streaming;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Verifies that the host fails fast on invalid <see cref="GrpcWebBridgeOptions"/> and starts on valid ones.
/// </summary>
public sealed class GrpcWebBridgeOptionsStartupValidationTests
{
    /// <summary>
    /// Ensures that starting a host with invalid options throws before the host is running,
    /// and that the exception lists every failing property.
    /// </summary>
    [Fact]
    public async Task StartAsync_WithInvalidOptions_FailsFastListingEveryFailure()
    {
        // Arrange
        using var host = BuildHost(options =>
        {
            options.Configuration.MaxMessageSize = 0;
            options.Configuration.StreamIdleTimeoutSeconds = -1;
            options.Configuration.AllowedOrigins = ["", "not a url"];
        });

        // Act
        var act = () => host.StartAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<OptionsValidationException>();
        exception.Which.Message.Should().Contain("Configuration.MaxMessageSize")
            .And.Contain("Configuration.StreamIdleTimeoutSeconds")
            .And.Contain("Configuration.AllowedOrigins[0]")
            .And.Contain("Configuration.AllowedOrigins[1]");
    }

    /// <summary>
    /// Ensures that the host starts and stops cleanly when the options are valid.
    /// </summary>
    [Fact]
    public async Task StartAsync_WithValidOptions_Starts()
    {
        // Arrange
        using var host = BuildHost(options => options.WithDevelopment().WithCompression(true, 6));

        // Act
        var act = async () =>
        {
            await host.StartAsync();
            await host.StopAsync();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static IHost BuildHost(Action<GrpcWebBridgeOptions> configureOptions) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // StreamingWorkerSupervisor needs EventBus, which only AddBidirectionalStreaming registers.
                services.AddBidirectionalStreaming();
                services.AddGrpcWebBridge(configureOptions);
            })
            .Build();
}
