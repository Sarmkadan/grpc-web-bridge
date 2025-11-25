#nullable enable
using FluentAssertions;
using GrpcWebBridge.Streaming;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Edge-case tests for FlowControlOptions validation, presets, and boundary values.
/// </summary>
public sealed class FlowControlOptionsEdgeCaseTests
{
    [Fact]
    public void Validate_DefaultOptions_DoesNotThrow()
    {
        var options = new FlowControlOptions();
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroInitialWindowSize_Throws()
    {
        var options = new FlowControlOptions { InitialWindowSize = 0 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("InitialWindowSize");
    }

    [Fact]
    public void Validate_NegativeInitialWindowSize_Throws()
    {
        var options = new FlowControlOptions { InitialWindowSize = -1 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Validate_MaxWindowSizeLessThanInitial_Throws()
    {
        var options = new FlowControlOptions { InitialWindowSize = 100, MaxWindowSize = 50 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("MaxWindowSize");
    }

    [Fact]
    public void Validate_MaxWindowSizeEqualsInitial_DoesNotThrow()
    {
        var options = new FlowControlOptions { InitialWindowSize = 64, MaxWindowSize = 64 };
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroInboundChannelCapacity_Throws()
    {
        var options = new FlowControlOptions { InboundChannelCapacity = 0 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("InboundChannelCapacity");
    }

    [Fact]
    public void Validate_ZeroOutboundChannelCapacity_Throws()
    {
        var options = new FlowControlOptions { OutboundChannelCapacity = 0 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("OutboundChannelCapacity");
    }

    [Fact]
    public void Validate_BackpressureThresholdAboveOne_Throws()
    {
        var options = new FlowControlOptions { BackpressureThreshold = 1.01 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("BackpressureThreshold");
    }

    [Fact]
    public void Validate_BackpressureThresholdNegative_Throws()
    {
        var options = new FlowControlOptions { BackpressureThreshold = -0.1 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Validate_BackpressureThresholdAtBoundaries_DoesNotThrow()
    {
        var zero = new FlowControlOptions { BackpressureThreshold = 0.0 };
        var one = new FlowControlOptions { BackpressureThreshold = 1.0 };

        var act1 = () => zero.Validate();
        var act2 = () => one.Validate();

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroCreditReplenishmentBatch_Throws()
    {
        var options = new FlowControlOptions { CreditReplenishmentBatch = 0 };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Validate_NegativeMaxProducerWaitTime_Throws()
    {
        var options = new FlowControlOptions { MaxProducerWaitTime = TimeSpan.FromSeconds(-1) };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Validate_NullMaxProducerWaitTime_DoesNotThrow()
    {
        var options = new FlowControlOptions { MaxProducerWaitTime = null };
        var act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroAdaptiveAdjustmentInterval_Throws()
    {
        var options = new FlowControlOptions { AdaptiveAdjustmentInterval = TimeSpan.Zero };
        var act = () => options.Validate();
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void HighThroughputPreset_HasAdaptiveMode()
    {
        var preset = FlowControlOptions.HighThroughput;

        preset.Mode.Should().Be(FlowControlMode.Adaptive);
        preset.InitialWindowSize.Should().Be(256);
        preset.MaxWindowSize.Should().Be(1024);
        preset.InboundChannelCapacity.Should().Be(512);
    }

    [Fact]
    public void LowLatencyPreset_HasCreditWindowMode()
    {
        var preset = FlowControlOptions.LowLatency;

        preset.Mode.Should().Be(FlowControlMode.CreditWindow);
        preset.InitialWindowSize.Should().Be(16);
        preset.MaxWindowSize.Should().Be(64);
        preset.CreditReplenishmentBatch.Should().Be(4);
    }

    [Fact]
    public void HighThroughputPreset_Validates()
    {
        var act = () => FlowControlOptions.HighThroughput.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void LowLatencyPreset_Validates()
    {
        var act = () => FlowControlOptions.LowLatency.Validate();
        act.Should().NotThrow();
    }
}
