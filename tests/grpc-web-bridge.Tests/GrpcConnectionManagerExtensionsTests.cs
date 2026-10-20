#nullable enable

using FluentAssertions;
using GrpcWebBridge.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class GrpcConnectionManagerExtensionsTests : IAsyncDisposable
{
    private readonly GrpcConnectionManager _manager =
        new(NullLogger<GrpcConnectionManager>.Instance);

    public async ValueTask DisposeAsync() => await _manager.DisposeAsync();

    /// <summary>
    /// Injects metrics into the manager's private <c>_metrics</c> dictionary via reflection,
    /// mirroring what <see cref="GrpcConnectionManagerExtensions"/> itself does to read them.
    /// </summary>
    private void SeedMetrics(params (string key, ConnectionMetrics metrics)[] entries)
    {
        var field = typeof(GrpcConnectionManager)
            .GetField("_metrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var dict = (Dictionary<string, ConnectionMetrics>)field.GetValue(_manager)!;
        foreach (var (key, metrics) in entries)
            dict[key] = metrics;
    }

    // --- Empty manager ---

    [Fact]
    public void GetAllMetrics_EmptyManager_ReturnsEmpty()
    {
        _manager.GetAllMetrics().Should().BeEmpty();
    }

    [Fact]
    public void GetActiveConnectionCount_EmptyManager_ReturnsZero()
    {
        _manager.GetActiveConnectionCount().Should().Be(0);
    }

    [Fact]
    public void GetTotalRequestCount_EmptyManager_ReturnsZero()
    {
        _manager.GetTotalRequestCount().Should().Be(0);
    }

    [Fact]
    public void GetAverageConnectionDuration_EmptyManager_ReturnsZero()
    {
        _manager.GetAverageConnectionDuration().Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void GetMostRecentlyUsed_EmptyManager_ReturnsNull()
    {
        _manager.GetMostRecentlyUsed().Should().BeNull();
    }

    [Fact]
    public void GetOldestConnection_EmptyManager_ReturnsNull()
    {
        _manager.GetOldestConnection().Should().BeNull();
    }

    [Fact]
    public void GetMostActiveConnection_EmptyManager_ReturnsNull()
    {
        _manager.GetMostActiveConnection().Should().BeNull();
    }

    [Fact]
    public void GetHighestThroughputConnection_EmptyManager_ReturnsNull()
    {
        _manager.GetHighestThroughputConnection().Should().BeNull();
    }

    // --- Null input ---

    [Fact]
    public void GetAllMetrics_NullManager_ThrowsArgumentNullException()
    {
        GrpcConnectionManager? nullManager = null;
        var act = () => nullManager!.GetAllMetrics();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetTotalBytesSent_NullManager_ThrowsArgumentNullException()
    {
        GrpcConnectionManager? nullManager = null;
        var act = () => nullManager!.GetTotalBytesSent();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetTotalBytesReceived_NullManager_ThrowsArgumentNullException()
    {
        GrpcConnectionManager? nullManager = null;
        var act = () => nullManager!.GetTotalBytesReceived();
        act.Should().Throw<ArgumentNullException>();
    }

    // --- Seeded data ---

    [Fact]
    public void GetAllMetrics_WithConnections_ReturnsAll()
    {
        SeedMetrics(
            ("svc1", new ConnectionMetrics { ServiceName = "Svc1", RequestCount = 5 }),
            ("svc2", new ConnectionMetrics { ServiceName = "Svc2", RequestCount = 3 }));

        _manager.GetAllMetrics().Should().HaveCount(2);
    }

    [Fact]
    public void GetTotalRequestCount_WithConnections_SumsRequests()
    {
        SeedMetrics(
            ("a", new ConnectionMetrics { RequestCount = 10 }),
            ("b", new ConnectionMetrics { RequestCount = 7 }));

        _manager.GetTotalRequestCount().Should().Be(17);
    }

    [Fact]
    public void GetTotalBytesSent_WithConnections_SumsBytes()
    {
        SeedMetrics(
            ("a", new ConnectionMetrics { BytesSent = 100 }),
            ("b", new ConnectionMetrics { BytesSent = 250 }));

        _manager.GetTotalBytesSent().Should().Be(350);
    }

    [Fact]
    public void GetTotalBytesReceived_WithConnections_SumsBytes()
    {
        SeedMetrics(
            ("a", new ConnectionMetrics { BytesReceived = 400 }),
            ("b", new ConnectionMetrics { BytesReceived = 600 }));

        _manager.GetTotalBytesReceived().Should().Be(1000);
    }

    [Fact]
    public void GetMostRecentlyUsed_ReturnsConnectionWithLatestLastUsedAt()
    {
        var older = new ConnectionMetrics { ServiceName = "Old", LastUsedAt = DateTime.UtcNow.AddMinutes(-10) };
        var newer = new ConnectionMetrics { ServiceName = "New", LastUsedAt = DateTime.UtcNow };
        SeedMetrics(("old", older), ("new", newer));

        _manager.GetMostRecentlyUsed()!.ServiceName.Should().Be("New");
    }

    [Fact]
    public void GetOldestConnection_ReturnsConnectionWithEarliestCreatedAt()
    {
        var oldest = new ConnectionMetrics { ServiceName = "Ancient", CreatedAt = DateTime.UtcNow.AddHours(-5) };
        var recent = new ConnectionMetrics { ServiceName = "Recent", CreatedAt = DateTime.UtcNow };
        SeedMetrics(("old", oldest), ("new", recent));

        _manager.GetOldestConnection()!.ServiceName.Should().Be("Ancient");
    }

    [Fact]
    public void GetMostActiveConnection_ReturnsConnectionWithHighestRequestCount()
    {
        SeedMetrics(
            ("low", new ConnectionMetrics { ServiceName = "Low", RequestCount = 2 }),
            ("high", new ConnectionMetrics { ServiceName = "High", RequestCount = 99 }));

        _manager.GetMostActiveConnection()!.ServiceName.Should().Be("High");
    }

    [Fact]
    public void GetHighestThroughputConnection_ReturnsByTotalBytes()
    {
        SeedMetrics(
            ("small", new ConnectionMetrics { ServiceName = "Small", BytesSent = 10, BytesReceived = 10 }),
            ("big", new ConnectionMetrics { ServiceName = "Big", BytesSent = 500, BytesReceived = 500 }));

        _manager.GetHighestThroughputConnection()!.ServiceName.Should().Be("Big");
    }
}
