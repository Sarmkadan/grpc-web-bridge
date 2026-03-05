using FluentAssertions;
using GrpcWebBridge.Configuration;
using GrpcWebBridge.Controllers;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Tests for the <see cref="ConfigurationController"/>.
/// </summary>
public sealed class ConfigurationControllerTests
{
    private static ServiceRegistry CreateRegistry()
        => new(NullLogger<ServiceRegistry>.Instance);

    private static ConfigurationController CreateController(
        GrpcWebBridgeOptions? options = null,
        ServiceRegistry? registry = null)
    {
        options ??= new GrpcWebBridgeOptions();
        return new ConfigurationController(
            options,
            registry ?? CreateRegistry(),
            NullLogger<ConfigurationController>.Instance);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationController.GetConfiguration"/> returns an
    /// <see cref="OkObjectResult"/> with status code 200.
    /// </summary>
    [Fact]
    public void GetConfiguration_ReturnsOk()
    {
        var controller = CreateController();

        var result = controller.GetConfiguration();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Verifies that the configuration returned by
    /// <see cref="ConfigurationController.GetConfiguration"/> contains the expected
    /// properties, specifically the <c>Environment</c> property.
    /// </summary>
    [Fact]
    public void GetConfiguration_ContainsExpectedProperties()
    {
        var options = new GrpcWebBridgeOptions();
        options.Configuration.Environment = "Testing";
        var controller = CreateController(options: options);

        var result = (OkObjectResult)controller.GetConfiguration();
        var body = result.Value!;

        body.Should().NotBeNull();
        // Anonymous object testing
        var environmentProperty = body.GetType().GetProperty("data");
        environmentProperty.Should().NotBeNull();
        var data = environmentProperty!.GetValue(body)!;
        var envProperty = data.GetType().GetProperty("Environment");
        envProperty.Should().NotBeNull();
        envProperty!.GetValue(data).Should().Be("Testing");
    }

    /// <summary>
    /// Verifies that updating the configuration with valid settings returns an
    /// <see cref="OkObjectResult"/> with status code 200.
    /// </summary>
    [Fact]
    public void UpdateConfiguration_WithValidSettings_ReturnsOk()
    {
        var controller = CreateController();
        var request = new ConfigurationUpdateRequest
        {
            Settings = new Dictionary<string, object>
            {
                { "RequestsPerSecond", "500" },
                { "CompressResponses", "true" }
            }
        };

        var result = controller.UpdateConfiguration(request);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Verifies that updating the configuration with no settings returns a
    /// <see cref="BadRequestObjectResult"/>.
    /// </summary>
    [Fact]
    public void UpdateConfiguration_WithNoSettings_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new ConfigurationUpdateRequest { Settings = new Dictionary<string, object>() };

        var result = controller.UpdateConfiguration(request);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
