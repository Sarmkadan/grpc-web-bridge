#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Configuration;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcWebBridgeOptionsTests
{
    [Fact]
    public void Constructor_WithEnvironment_SetsEnvironment()
    {
        // Act
        var options = new GrpcWebBridgeOptions("Staging");

        // Assert
        options.Configuration.Environment.Should().Be("Staging");
    }

    [Fact]
    public void WithDevelopment_SetsDevelopmentDefaults()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();

        // Act
        options.WithDevelopment();

        // Assert
        options.Configuration.Environment.Should().Be("Development");
        options.Configuration.EnableSwagger.Should().BeTrue();
        options.Configuration.EnableLogging.Should().BeTrue();
        options.Configuration.AllowedOrigins.Should().Contain("*");
    }

    [Fact]
    public void WithProduction_SetsProductionDefaults()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();

        // Act
        options.WithProduction();

        // Assert
        options.Configuration.Environment.Should().Be("Production");
        options.Configuration.RequireAuthentication.Should().BeTrue();
        options.Configuration.CompressResponses.Should().BeTrue();
        options.Configuration.AllowedOrigins.Should().BeEmpty();
    }

    [Fact]
    public void WithTesting_SetsTestingDefaults()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();

        // Act
        options.WithTesting();

        // Assert
        options.Configuration.Environment.Should().Be("Testing");
        options.Configuration.EnableSwagger.Should().BeTrue();
        options.Configuration.EnableMetrics.Should().BeFalse();
        options.Configuration.CompressResponses.Should().BeFalse();
    }

    [Fact]
    public void WithMaxStreamCount_ValidValue_SetsCount()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();

        // Act
        options.WithMaxStreamCount(500);

        // Assert
        options.Configuration.MaxStreamCount.Should().Be(500);
    }

    [Fact]
    public void WithMaxStreamCount_Zero_ThrowsArgumentException()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();

        // Act
        var act = () => options.WithMaxStreamCount(0);

        // Assert
        act.Should().Throw<ArgumentException>().WithParameterName("maxCount");
    }

    [Fact]
    public void AddAllowedOrigins_WithValidOrigins_AddsToConfiguration()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();

        // Act
        options.AddAllowedOrigins("https://example.com", "https://app.example.com");

        // Assert
        options.Configuration.AllowedOrigins.Should().Contain("https://example.com");
        options.Configuration.AllowedOrigins.Should().Contain("https://app.example.com");
    }

    [Fact]
    public void WithCompression_ValidLevel_EnablesCompression()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();

        // Act
        options.WithCompression(true, 5);

        // Assert
        options.Configuration.CompressResponses.Should().BeTrue();
        options.Configuration.CompressionLevel.Should().Be(5);
    }
}
