#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Configuration;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Test suite for the <see cref="GrpcWebBridgeOptions"/> configuration builder.
/// </summary>
public sealed class GrpcWebBridgeOptionsTests
{
    /// <summary>
    /// Verifies that the constructor correctly assigns the provided environment string.
    /// </summary>
    [Fact]
    public void Constructor_WithEnvironment_SetsEnvironment()
    {
        // Act
        var options = new GrpcWebBridgeOptions("Staging");

        // Assert
        options.Configuration.Environment.Should().Be("Staging");
    }

    /// <summary>
    /// Ensures that calling <c>WithDevelopment</c> applies the development defaults
    /// (environment, Swagger, logging, and allowed origins).
    /// </summary>
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

    /// <summary>
    /// Ensures that calling <c>WithProduction</c> applies the production defaults
    /// (environment, authentication, compression, and allowed origins).
    /// </summary>
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

    /// <summary>
    /// Ensures that calling <c>WithTesting</c> applies the testing defaults
    /// (environment, Swagger, metrics, and compression settings).
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>WithMaxStreamCount</c> accepts a positive value and sets the
    /// <see cref="GrpcWebBridgeOptions.Configuration.MaxStreamCount"/> property.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>WithMaxStreamCount</c> throws an <see cref="ArgumentException"/>
    /// when the supplied count is zero.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>AddAllowedOrigins</c> correctly appends the provided origins
    /// to the configuration's <see cref="GrpcWebBridgeOptions.Configuration.AllowedOrigins"/> collection.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>WithCompression</c> enables compression and sets the
    /// compression level when called with valid arguments.
    /// </summary>
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
