#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Test suite for <see cref="GrpcWebBridgeOptionsValidator"/>.
/// </summary>
public sealed class GrpcWebBridgeOptionsValidatorTests
{
    private readonly GrpcWebBridgeOptionsValidator _validator = new();

    /// <summary>
    /// Ensures that the default options pass validation.
    /// </summary>
    [Fact]
    public void Validate_WithDefaultOptions_Succeeds()
    {
        // Act
        var result = _validator.Validate(null, new GrpcWebBridgeOptions());

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Ensures that a non-positive max message size is rejected.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveMaxMessageSize_Fails(int size)
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.MaxMessageSize = size;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Configuration.MaxMessageSize");
    }

    /// <summary>
    /// Ensures that empty or whitespace origins are rejected.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceOrigin_Fails(string origin)
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.AllowedOrigins = [origin];

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Configuration.AllowedOrigins[0]").And.Contain("empty or whitespace");
    }

    /// <summary>
    /// Ensures that origins which are not bare <c>scheme://host[:port]</c> URLs are rejected.
    /// </summary>
    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com")]
    [InlineData("example.com")]
    [InlineData("https://example.com/")]
    [InlineData("https://example.com/app")]
    [InlineData("https://example.com?x=1")]
    [InlineData(" https://example.com")]
    [InlineData("https://user@example.com")]
    public void Validate_WithInvalidOriginUrl_Fails(string origin)
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.AllowedOrigins = [origin];

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Configuration.AllowedOrigins[0]").And.Contain("not a valid origin");
    }

    /// <summary>
    /// Ensures that the wildcard and well-formed origins are accepted.
    /// </summary>
    [Theory]
    [InlineData("*")]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:4200")]
    [InlineData("HTTPS://Example.com")]
    public void Validate_WithValidOrigin_Succeeds(string origin)
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.AllowedOrigins = [origin];

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Ensures that a negative stream idle timeout is rejected.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeStreamIdleTimeout_Fails()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.StreamIdleTimeoutSeconds = -5;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Configuration.StreamIdleTimeoutSeconds").And.Contain("must be greater than 0");
    }

    /// <summary>
    /// Ensures that a negative stream heartbeat interval is rejected.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeStreamHeartbeatInterval_Fails()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.StreamHeartbeatIntervalSeconds = -5;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Configuration.StreamHeartbeatIntervalSeconds").And.Contain("must be greater than 0");
    }

    /// <summary>
    /// Ensures that a negative default gRPC call timeout is rejected.
    /// </summary>
    [Fact]
    public void Validate_WithNegativeDefaultTimeout_Fails()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.DefaultTimeoutMilliseconds = -5;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Configuration.DefaultTimeoutMilliseconds").And.Contain("must be greater than 0");
    }

    /// <summary>
    /// Ensures that disabling CORS while origins are still listed is rejected.
    /// </summary>
    [Fact]
    public void Validate_WithCorsDisabledAndOriginsListed_Fails()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions().WithCors(false);

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Configuration.EnableCors").And.Contain("mutually exclusive");
    }

    /// <summary>
    /// Ensures that disabling CORS is accepted once no origins are listed.
    /// </summary>
    [Fact]
    public void Validate_WithCorsDisabledAndNoOrigins_Succeeds()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions().WithCors(false);
        options.Configuration.AllowedOrigins = [];

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    /// <summary>
    /// Ensures that every failing property is reported in one result, not only the first one found.
    /// </summary>
    [Fact]
    public void Validate_WithSeveralInvalidProperties_ReportsAllFailures()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions().WithCors(false);
        options.Configuration.MaxMessageSize = 0;
        options.Configuration.StreamIdleTimeoutSeconds = -1;
        options.Configuration.DefaultTimeoutMilliseconds = 0;
        options.Configuration.AllowedOrigins = ["", "not a url"];

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().HaveCount(6);
        result.FailureMessage.Should().Contain("Configuration.MaxMessageSize")
            .And.Contain("Configuration.StreamIdleTimeoutSeconds")
            .And.Contain("Configuration.DefaultTimeoutMilliseconds")
            .And.Contain("Configuration.AllowedOrigins[0]")
            .And.Contain("Configuration.AllowedOrigins[1]")
            .And.Contain("Configuration.EnableCors");
    }

    /// <summary>
    /// Ensures that a null origin list is reported rather than throwing.
    /// </summary>
    [Fact]
    public void Validate_WithNullAllowedOrigins_Fails()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.AllowedOrigins = null!;

        // Act
        var result = _validator.Validate(null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Contain("Configuration.AllowedOrigins").And.Contain("must not be null");
    }

    /// <summary>
    /// Ensures that a null options instance is rejected with <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Validate_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _validator.Validate(null, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
