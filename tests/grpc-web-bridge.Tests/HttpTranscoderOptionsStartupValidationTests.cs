#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Configuration;
using GrpcWebBridge.Streaming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Verifies that the host fails fast on invalid <see cref="HttpTranscoderOptions"/> and starts on valid ones.
/// </summary>
public sealed class HttpTranscoderOptionsStartupValidationTests
{
    /// <summary>
    /// Ensures that starting a host with invalid transcoder options throws before the host is running,
    /// and that the exception lists every failing rule.
    /// </summary>
    [Fact]
    public async Task StartAsync_WithInvalidHttpTranscoderOptions_FailsFastListingEveryFailure()
    {
        // Arrange
        using var host = BuildHost(options =>
        {
            options.RoutePrefix = "api/";
            options.MaxRequestBodySize = 0;
            options.Routes =
            [
                new HttpRouteBinding
                {
                    ServiceName = "items.v1.ItemService",
                    MethodName = "GetItem",
                    HttpMethod = "GET",
                    Template = "/v1/items/{itemId}",
                    RequestFieldNames = ["id"]
                }
            ];
        });

        // Act
        var act = () => host.StartAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<OptionsValidationException>();
        exception.Which.Message.Should().Contain("HttpTranscoderOptions.RoutePrefix")
            .And.Contain("HttpTranscoderOptions.MaxRequestBodySize")
            .And.Contain("HttpTranscoderOptions.Routes[0].Template");
    }

    /// <summary>
    /// Ensures that the host starts and stops cleanly when the transcoder options are valid.
    /// </summary>
    [Fact]
    public async Task StartAsync_WithValidHttpTranscoderOptions_Starts()
    {
        // Arrange
        using var host = BuildHost(options =>
        {
            options.RoutePrefix = "/api";
            options.Routes =
            [
                new HttpRouteBinding
                {
                    ServiceName = "items.v1.ItemService",
                    MethodName = "GetItem",
                    HttpMethod = "GET",
                    Template = "/v1/items/{id}",
                    RequestFieldNames = ["id"]
                }
            ];
        });

        // Act
        var act = async () =>
        {
            await host.StartAsync();
            await host.StopAsync();
        };

        // Assert
        await act.Should().NotThrowAsync();
    }

    private static IHost BuildHost(Action<HttpTranscoderOptions> configureTranscoder) =>
        Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // StreamingWorkerSupervisor needs EventBus, which only AddBidirectionalStreaming registers.
                services.AddBidirectionalStreaming();
                services.AddGrpcWebBridge();
                services.Configure(configureTranscoder);
            })
            .Build();
}
