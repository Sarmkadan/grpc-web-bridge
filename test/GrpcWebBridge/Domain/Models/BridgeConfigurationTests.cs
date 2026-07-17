using Xunit;
using FluentAssertions;
using GrpcWebBridge.Domain.Models;

/// <summary>
/// Tests for the <see cref="BridgeConfiguration"/> class.
/// </summary>
public class BridgeConfigurationTests
{
    /// <summary>
    /// Verifies that calling <see cref="BridgeConfiguration.SetServiceDefault(string, string)"/> with a null service name
    /// throws an <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void SetServiceDefault_NullServiceName_ThrowsArgumentException()
    {
        // Arrange
        var config = new BridgeConfiguration("env");

        // Act and Assert
        Assert.Throws<ArgumentException>(() => config.SetServiceDefault(null, "defaultValue"));
    }

    /// <summary>
    /// Verifies that calling <see cref="BridgeConfiguration.AddAllowedOrigin(string)"/> with a null origin
    /// throws an <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public void AddAllowedOrigin_NullOrigin_ThrowsArgumentException()
    {
        // Arrange
        var config = new BridgeConfiguration("env");

        // Act and Assert
        Assert.Throws<ArgumentException>(() => config.AddAllowedOrigin(null));
    }
}
