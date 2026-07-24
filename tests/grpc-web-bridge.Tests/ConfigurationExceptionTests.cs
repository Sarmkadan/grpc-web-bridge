using System;
using GrpcWebBridge.Domain.Exceptions;
using Xunit;

namespace GrpcWebBridge.Tests;

public class ConfigurationExceptionTests
{
    [Fact]
    public void DefaultConstructor_ShouldInitializePropertiesToNull()
    {
        var ex = new ConfigurationException();

        Assert.Null(ex.ConfigurationKey);
        Assert.Null(ex.ConfigurationValue);
        Assert.Null(ex.InnerException);
        Assert.NotNull(ex.Message); // base message may be empty but not null
    }

    [Fact]
    public void MessageConstructor_ShouldSetMessage()
    {
        const string message = "Configuration error occurred";
        var ex = new ConfigurationException(message);

        Assert.Equal(message, ex.Message);
        Assert.Null(ex.ConfigurationKey);
        Assert.Null(ex.ConfigurationValue);
    }

    [Fact]
    public void MessageAndInnerExceptionConstructor_ShouldSetInnerException()
    {
        const string message = "Configuration error with inner";
        var inner = new InvalidOperationException("inner");
        var ex = new ConfigurationException(message, inner);

        Assert.Equal(message, ex.Message);
        Assert.Same(inner, ex.InnerException);
        Assert.Null(ex.ConfigurationKey);
        Assert.Null(ex.ConfigurationValue);
    }

    [Fact]
    public void ConfigurationKeyConstructor_ShouldSetKeyAndMessage()
    {
        const string key = "MaxConnections";
        const string message = "must be positive";
        var ex = new ConfigurationException(key, message);

        Assert.Equal($"Configuration '{key}' error: {message}", ex.Message);
        Assert.Equal(key, ex.ConfigurationKey);
        Assert.Null(ex.ConfigurationValue);
    }

    [Fact]
    public void ConfigurationKeyAndValueConstructor_ShouldSetKeyValueAndMessage()
    {
        const string key = "MaxConnections";
        const string value = "-1";
        const string message = "must be positive";
        var ex = new ConfigurationException(key, value, message);

        Assert.Equal($"Configuration '{key}' with value '{value}' error: {message}", ex.Message);
        Assert.Equal(key, ex.ConfigurationKey);
        Assert.Equal(value, ex.ConfigurationValue);
    }

    [Fact]
    public void ToString_ShouldIncludeConfigKeyAndValueWhenSet()
    {
        const string key = "Timeout";
        const string value = "0";
        const string message = "must be > 0";
        var ex = new ConfigurationException(key, value, message);

        var result = ex.ToString();

        Assert.Contains($"ConfigKey: {key}", result);
        Assert.Contains($"ConfigValue: {value}", result);
    }

    [Fact]
    public void ToString_ShouldOmitConfigKeyAndValueWhenNullOrEmpty()
    {
        var ex = new ConfigurationException();

        var result = ex.ToString();

        // Base ToString should be present, but no extra config info
        Assert.DoesNotContain("ConfigKey:", result);
        Assert.DoesNotContain("ConfigValue:", result);
    }

    [Fact]
    public void ToString_ShouldOmitEmptyConfigKeyAndValue()
    {
        var ex = new ConfigurationException(string.Empty, string.Empty, "msg");

        var result = ex.ToString();

        Assert.DoesNotContain("ConfigKey:", result);
        Assert.DoesNotContain("ConfigValue:", result);
    }
}
