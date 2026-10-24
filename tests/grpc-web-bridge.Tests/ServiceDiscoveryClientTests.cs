#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using System.Net.Sockets;
using System.Text;
using FluentAssertions;
using GrpcWebBridge.Integration;
using GrpcWebBridge.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>Unit tests for <see cref="ServiceDiscoveryClient"/> against a loopback HttpListener stub.</summary>
public sealed class ServiceDiscoveryClientTests : IDisposable
{
    private const string UnreachableUrl = "http://127.0.0.1:1";

    private readonly HttpClientFactory _httpFactory = new HttpClientFactory(NullLogger<HttpClientFactory>.Instance);
    private HttpListener? _listener;
    private Task? _serverLoop;

    [Fact]
    public async Task RegisterServiceAsync_NullInstance_ThrowsArgumentNullException()
    {
        var client = CreateClient(UnreachableUrl);

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.RegisterServiceAsync(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Methods_WithNullOrEmptyId_ThrowArgumentException(string? value)
    {
        var client = CreateClient(UnreachableUrl);

        await Assert.ThrowsAsync<ArgumentException>(() => client.DeregisterServiceAsync(value!));
        await Assert.ThrowsAsync<ArgumentException>(() => client.DiscoverServicesAsync(value!));
        await Assert.ThrowsAsync<ArgumentException>(() => client.SendHeartbeatAsync(value!));
    }

    [Fact]
    public async Task Operations_WhenDiscoveryServiceUnreachable_ReturnFailureValues()
    {
        var client = CreateClient(UnreachableUrl);

        (await client.RegisterServiceAsync(NewInstance("svc-1", "orders"))).Should().BeFalse();
        (await client.DeregisterServiceAsync("svc-1")).Should().BeFalse();
        (await client.SendHeartbeatAsync("svc-1")).Should().BeFalse();
        (await client.DiscoverServicesAsync("orders")).Should().BeEmpty();
        (await client.GetHealthyInstanceAsync("orders")).Should().BeNull();
        client.GetCachedServices().Should().BeEmpty();
    }

    [Fact]
    public async Task RegisterServiceAsync_WhenAccepted_CachesInstance_AndClearCacheEmptiesIt()
    {
        var client = CreateClient(StartDiscoveryStub());

        (await client.RegisterServiceAsync(NewInstance("svc-1", "orders"))).Should().BeTrue();
        client.GetCachedServices().Should().ContainSingle(s => s.Id == "svc-1" && s.Name == "orders");

        client.ClearCache();
        client.GetCachedServices().Should().BeEmpty();
    }

    [Fact]
    public async Task DeregisterAndHeartbeat_ForKnownAndUnknownIds_ReturnTrueThenFalse()
    {
        var client = CreateClient(StartDiscoveryStub());
        await client.RegisterServiceAsync(NewInstance("svc-1", "orders"));

        (await client.SendHeartbeatAsync("svc-1")).Should().BeTrue();
        (await client.SendHeartbeatAsync("missing")).Should().BeFalse();
        (await client.DeregisterServiceAsync("svc-1")).Should().BeTrue();
        client.GetCachedServices().Should().BeEmpty();
        (await client.DeregisterServiceAsync("missing")).Should().BeFalse();
    }

    [Fact]
    public async Task DiscoverServicesAsync_ReturnsMatchingInstances_OrEmptyListForUnknownName()
    {
        var client = CreateClient(StartDiscoveryStub());

        (await client.DiscoverServicesAsync("orders")).Select(i => i.Id).Should().BeEquivalentTo("o-1", "o-2");
        (await client.DiscoverServicesAsync("unknown")).Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetHealthyInstanceAsync_PrefersUpInstance_AndFallsBackToFirstWhenAllDown()
    {
        var client = CreateClient(StartDiscoveryStub());

        (await client.GetHealthyInstanceAsync("orders"))?.Id.Should().Be("o-1");
        (await client.GetHealthyInstanceAsync("legacy"))?.Id.Should().Be("l-1");
        (await client.GetHealthyInstanceAsync("unknown")).Should().BeNull();
    }

    [Fact]
    public async Task StartAutoRefresh_FiresRefreshTimer_AndStopIsIdempotent()
    {
        var client = CreateClient(UnreachableUrl);

        client.StartAutoRefresh(TimeSpan.FromMilliseconds(20));
        client.StartAutoRefresh(TimeSpan.FromMilliseconds(20));

        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (LastRefreshTime(client) == DateTime.MinValue && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }

        LastRefreshTime(client).Should().NotBe(DateTime.MinValue);
        client.StopAutoRefresh();
        client.StopAutoRefresh();
    }

    public void Dispose()
    {
        _listener?.Close();
        _serverLoop?.Wait(TimeSpan.FromSeconds(5));
        _httpFactory.Dispose();
    }

    private static ServiceDiscoveryClient CreateClient(string discoveryUrl) => new(
        new HttpClientFactory(NullLogger<HttpClientFactory>.Instance),
        NullLogger<ServiceDiscoveryClient>.Instance,
        new ServiceDiscoveryOptions { DiscoveryServiceUrl = discoveryUrl });

    private static DateTime LastRefreshTime(ServiceDiscoveryClient client)
    {
        var stats = client.GetStatistics();
        return (DateTime)stats.GetType().GetProperty("lastRefreshTime")!.GetValue(stats)!;
    }

    private static ServiceInstance NewInstance(string id, string name, string status = "UP") => new()
    {
        Id = id, Name = name, Host = "127.0.0.1", Port = 5000, Status = status
    };

    private static List<ServiceInstance> Lookup(string? name) => name switch
    {
        "orders" => new List<ServiceInstance> { NewInstance("o-1", "orders"), NewInstance("o-2", "orders", "DOWN") },
        "legacy" => new List<ServiceInstance> { NewInstance("l-1", "legacy", "DOWN") },
        _ => new List<ServiceInstance>()
    };

    private string StartDiscoveryStub()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        var listener = _listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        _serverLoop = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                try
                {
                    Respond(await listener.GetContextAsync());
                }
                catch (Exception)
                {
                    break;
                }
            }
        });

        return $"http://127.0.0.1:{port}";
    }

    private static void Respond(HttpListenerContext context)
    {
        var (status, body) = (context.Request.HttpMethod, context.Request.Url!.AbsolutePath) switch
        {
            ("POST", "/services") => (200, "{}"),
            ("GET", "/services") => (200, JsonUtility.Serialize(Lookup(context.Request.QueryString["name"]))),
            ("DELETE", "/services/svc-1") => (200, string.Empty),
            ("PUT", "/services/svc-1/health") => (200, string.Empty),
            _ => (404, string.Empty)
        };

        var bytes = Encoding.UTF8.GetBytes(body);
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = bytes.Length;
        context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        context.Response.Close();
    }
}
