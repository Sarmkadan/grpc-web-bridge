using System;
using System.Text.Json;
using GrpcWebBridge.Domain.Models;
using Xunit;

namespace GrpcWebBridge.Tests;

public class BridgeConfigurationJsonExtensionsTests
{
    [Fact]
    public void ToJson_HappyPath_ReturnsJsonString()
    {
        // Arrange
        var configuration = new BridgeConfiguration();
        var expectedJson = "{\"Environment\":\"dev\",\"InstanceName\":\"instance1\",\"InstanceId\":\"instance1\"}";

        // Act
        var actualJson = BridgeConfigurationJsonExtensions.ToJson(configuration);

        // Assert
        Assert.Equal(expectedJson, actualJson);
    }

    [Fact]
    public void ToJson_NullConfiguration_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => BridgeConfigurationJsonExtensions.ToJson(null));
    }

    [Fact]
    public void FromJson_HappyPath_ReturnsConfiguration()
    {
        // Arrange
        var json = "{\"Environment\":\"dev\",\"InstanceName\":\"instance1\",\"InstanceId\":\"instance1\"}";
        var expectedConfiguration = new BridgeConfiguration { Environment = "dev", InstanceName = "instance1", InstanceId = "instance1" };

        // Act
        var actualConfiguration = BridgeConfigurationJsonExtensions.FromJson(json);

        // Assert
        Assert.Equal(expectedConfiguration, actualConfiguration);
    }

    [Fact]
    public void FromJson_NullJson_ThrowsArgumentException()
    {
        // Act and Assert
        Assert.Throws<ArgumentException>(() => BridgeConfigurationJsonExtensions.FromJson(null));
    }

    [Fact]
    public void TryFromJson_HappyPath_ReturnsTrue()
    {
        // Arrange
        var json = "{\"Environment\":\"dev\",\"InstanceName\":\"instance1\",\"InstanceId\":\"instance1\"}";
        var expectedConfiguration = new BridgeConfiguration { Environment = "dev", InstanceName = "instance1", InstanceId = "instance1" };

        // Act
        var result = BridgeConfigurationJsonExtensions.TryFromJson(json, out var actualConfiguration);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedConfiguration, actualConfiguration);
    }

    [Fact]
    public void TryFromJson_NullJson_ReturnsFalse()
    {
        // Act
        var result = BridgeConfigurationJsonExtensions.TryFromJson(null, out _);

        // Assert
        Assert.False(result);
    }
}
