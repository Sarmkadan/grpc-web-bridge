using Xunit;
using FluentAssertions;
using GrpcWebBridge.Domain.Models;

public class BridgeConfigurationTests
{
    [Fact]
    public void SetServiceDefault_NullServiceName_ThrowsArgumentException()
    {
        // Arrange
        var config = new BridgeConfiguration("env");

        // Act and Assert
        Assert.Throws<ArgumentException>(() => config.SetServiceDefault(null, "defaultValue"));
    }

    [Fact]
    public void AddAllowedOrigin_NullOrigin_ThrowsArgumentException()
    {
        // Arrange
        var config = new BridgeConfiguration("env");

        // Act and Assert
        Assert.Throws<ArgumentException>(() => config.AddAllowedOrigin(null));
    }
}
