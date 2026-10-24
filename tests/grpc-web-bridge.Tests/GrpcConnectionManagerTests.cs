#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Data;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>Unit tests for <see cref="GrpcConnectionManager"/> covering channel caching, metrics, closing and disposal.</summary>
public sealed class GrpcConnectionManagerTests
{
    private static GrpcConnectionManager CreateManager() =>
        new(NullLogger<GrpcConnectionManager>.Instance);

    private static GrpcService CreateService(string fullName, string endpoint = "localhost", int port = 5000) =>
        new() { Name = fullName.Split('.')[^1], FullName = fullName, Endpoint = endpoint, Port = port };

    [Fact]
    public async Task NullInputs_ThrowArgumentNullException()
    {
        Action act = () => new GrpcConnectionManager(null!);
        act.Should().Throw<ArgumentNullException>();

        await using var manager = CreateManager();

        manager.Invoking(m => m.GetOrCreateChannel(null!)).Should().Throw<ArgumentNullException>();
        await manager.Awaiting(m => m.CloseChannelAsync(null!)).Should().ThrowAsync<ArgumentNullException>();
        await manager.Awaiting(m => m.TestConnectionAsync(null!)).Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetOrCreateChannel_SameService_CreatesOnceAndCountsReuse()
    {
        await using var manager = CreateManager();
        var service = CreateService("demo.v1.Greeter");

        var first = manager.GetOrCreateChannel(service);
        var metricsAfterCreate = manager.GetMetrics(service.FullName);
        var second = manager.GetOrCreateChannel(service);

        second.Should().BeSameAs(first);
        manager.ActiveConnectionCount.Should().Be(1);
        metricsAfterCreate.Should().NotBeNull();
        metricsAfterCreate!.ServiceName.Should().Be("demo.v1.Greeter");
        metricsAfterCreate.Address.Should().Be("http://localhost:5000");
        manager.GetMetrics(service.FullName)!.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateChannel_InvalidEndpoint_ThrowsServiceRegistrationException()
    {
        await using var manager = CreateManager();
        var service = CreateService("demo.v1.Broken", endpoint: "bad host");

        manager.Invoking(m => m.GetOrCreateChannel(service))
            .Should().Throw<ServiceRegistrationException>();
        manager.ActiveConnectionCount.Should().Be(0);

        // TestConnectionAsync swallows the same failure and reports it as unavailable.
        (await manager.TestConnectionAsync(service)).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown.Service")]
    public async Task GetChannelAndGetMetrics_BlankOrUnknownName_ReturnsNull(string? name)
    {
        await using var manager = CreateManager();
        manager.GetOrCreateChannel(CreateService("demo.v1.Greeter"));

        manager.GetChannel(name!).Should().BeNull();
        manager.GetMetrics(name!).Should().BeNull();
    }

    [Fact]
    public async Task CloseChannelAsync_ExistingService_RemovesChannelAndMetrics()
    {
        await using var manager = CreateManager();
        var service = CreateService("demo.v1.Greeter");
        manager.GetOrCreateChannel(service);

        await manager.CloseChannelAsync(service);
        await manager.CloseChannelAsync(CreateService("never.Created"));

        manager.ActiveConnectionCount.Should().Be(0);
        manager.GetChannel(service.FullName).Should().BeNull();
        manager.GetMetrics(service.FullName).Should().BeNull();
    }

    [Fact]
    public async Task CloseAllChannelsAsync_MultipleServices_ClearsEverything()
    {
        await using var manager = CreateManager();
        manager.GetOrCreateChannel(CreateService("demo.v1.Alpha", port: 5001));
        manager.GetOrCreateChannel(CreateService("demo.v1.Beta", port: 5002));
        manager.ActiveConnectionCount.Should().Be(2);

        await manager.CloseAllChannelsAsync();

        manager.ActiveConnectionCount.Should().Be(0);
        manager.GetChannel("demo.v1.Alpha").Should().BeNull();
        manager.GetChannel("demo.v1.Beta").Should().BeNull();
    }

    [Fact]
    public async Task TestConnectionAsync_FreshChannel_ReturnsTrue()
    {
        await using var manager = CreateManager();

        // A new channel is Idle until the first RPC, so it counts as available without any network I/O.
        (await manager.TestConnectionAsync(CreateService("demo.v1.Greeter"))).Should().BeTrue();
        manager.ActiveConnectionCount.Should().Be(1);
    }

    [Fact]
    public async Task DisposeAsync_ActiveChannels_ClosesAllAndIsIdempotent()
    {
        var manager = CreateManager();
        manager.GetOrCreateChannel(CreateService("demo.v1.Greeter"));

        await manager.DisposeAsync();
        await manager.DisposeAsync();

        manager.ActiveConnectionCount.Should().Be(0);
        manager.GetChannel("demo.v1.Greeter").Should().BeNull();
    }
}
