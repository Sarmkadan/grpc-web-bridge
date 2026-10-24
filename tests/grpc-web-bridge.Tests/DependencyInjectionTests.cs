#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using FluentAssertions;
using GrpcWebBridge.Configuration;
using GrpcWebBridge.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>Verifies the public <c>AddGrpcWebBridge*</c> extension methods register the expected configuration.</summary>
public sealed class DependencyInjectionTests
{
    /// <summary>Omitting options registers a default singleton whose <c>IOptions</c> view shares its configuration.</summary>
    [Fact]
    public void AddGrpcWebBridge_WithoutOptions_RegistersDefaultOptionsSingleton()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddGrpcWebBridge();
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<GrpcWebBridgeOptions>();
        var validated = provider.GetRequiredService<IOptions<GrpcWebBridgeOptions>>().Value;

        // Assert
        options.Configuration.AllowedOrigins.Should().Equal("*");
        validated.Configuration.Should().BeSameAs(options.Configuration);
    }

    /// <summary>The delegate overload applies its changes to the registered options.</summary>
    [Fact]
    public void AddGrpcWebBridge_ConfigureDelegate_AppliesToRegisteredOptions()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddGrpcWebBridge(o => o.Configuration.MaxMessageSize = 4096);
        using var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<GrpcWebBridgeOptions>();
        var validated = provider.GetRequiredService<IOptions<GrpcWebBridgeOptions>>().Value;

        // Assert
        options.Configuration.MaxMessageSize.Should().Be(4096);
        validated.Configuration.MaxMessageSize.Should().Be(4096);
    }

    /// <summary>Swagger and Prometheus helpers return the same collection; Swagger adds services.</summary>
    [Fact]
    public void AddGrpcWebBridgeSwaggerAndPrometheus_ReturnSameCollection_AndRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var countBefore = services.Count;

        // Act
        var swaggerResult = services.AddGrpcWebBridgeSwagger();
        var prometheusResult = services.AddGrpcWebBridgePrometheus();

        // Assert
        swaggerResult.Should().BeSameAs(services);
        prometheusResult.Should().BeSameAs(services);
        services.Count.Should().BeGreaterThan(countBefore);
    }

    /// <summary>
    /// Pins the current behaviour: the default <c>["*"]</c> origin combined with credentials is rejected
    /// when the CORS policy is built. Revisit if the default changes.
    /// </summary>
    [Fact]
    public void AddGrpcWebBridgeCors_DefaultWildcardOrigin_ThrowsWhenPolicyIsBuilt()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddGrpcWebBridgeCors();
        using var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<CorsOptions>>().Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*wildcard*credentials*");
    }

    /// <summary>Explicit origins restrict the <c>AllowGrpcWeb</c> policy to exactly those origins.</summary>
    [Fact]
    public void AddGrpcWebBridgeCors_ExplicitOrigins_RestrictsPolicyToListedOrigins()
    {
        // Arrange
        var options = new GrpcWebBridgeOptions();
        options.Configuration.AllowedOrigins = ["https://app.example.com"];
        var services = new ServiceCollection();
        services.AddGrpcWebBridgeCors(options);
        using var provider = services.BuildServiceProvider();

        // Act
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>().Value.GetPolicy("AllowGrpcWeb");

        // Assert
        policy.Should().NotBeNull();
        policy!.AllowAnyOrigin.Should().BeFalse();
        policy.Origins.Should().ContainSingle().Which.Should().Be("https://app.example.com");
    }

    /// <summary>The JWT delegate is applied and the default token validation flags are kept.</summary>
    [Fact]
    public void AddGrpcWebBridgeAuthentication_ConfigureDelegate_AppliesToJwtBearerOptions()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddGrpcWebBridgeAuthentication(jwt => jwt.Audience = "bridge-api");
        using var provider = services.BuildServiceProvider();

        // Act
        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        // Assert
        jwt.Audience.Should().Be("bridge-api");
        jwt.TokenValidationParameters.ValidateLifetime.Should().BeTrue();
        jwt.TokenValidationParameters.ValidateIssuer.Should().BeFalse();
    }

    /// <summary>
    /// Ensures a blank instance name tags spans as <c>"default"</c> and the tracer builder delegate runs.
    /// </summary>
    [Fact]
    public void AddGrpcWebBridgeTracing_BlankInstanceName_TagsDefaultAndInvokesConfigureBuilder()
    {
        // Arrange
        var builderConfigured = false;
        var services = new ServiceCollection().AddLogging();
        services.AddGrpcWebBridgeTracing(instanceName: "   ", configureBuilder: _ => builderConfigured = true);
        using var provider = services.BuildServiceProvider();

        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BridgeActivitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        // Act
        using var tracerProvider = provider.GetRequiredService<TracerProvider>();
        var activity = provider.GetRequiredService<TracingService>().StartAuthenticationActivity("Bearer");

        // Assert
        builderConfigured.Should().BeTrue();
        activity.Should().NotBeNull();
        activity!.GetTagItem(BridgeActivitySource.TagBridgeInstance).Should().Be("default");
    }

    /// <summary>Every extension rejects a null collection; the delegate overload rejects a null delegate.</summary>
    [Fact]
    public void AllExtensions_NullArguments_ThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;
        Action<GrpcWebBridgeOptions>? configure = null;
        var calls = new Action[]
        {
            () => services!.AddGrpcWebBridge(),
            () => services!.AddGrpcWebBridge(new GrpcWebBridgeOptions()),
            () => services!.AddGrpcWebBridgeSwagger(),
            () => services!.AddGrpcWebBridgeCors(),
            () => services!.AddGrpcWebBridgeAuthentication(),
            () => services!.AddGrpcWebBridgePrometheus(),
            () => services!.AddGrpcWebBridgeTracing()
        };

        // Act & Assert
        foreach (var call in calls)
        {
            call.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("services");
        }

        var real = new ServiceCollection();
        var act = () => real.AddGrpcWebBridge(configure!);
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("configureOptions");
    }
}
