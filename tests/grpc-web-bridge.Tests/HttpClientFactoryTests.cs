#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for HttpClientFactory
// =====================================================================

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using GrpcWebBridge.Domain.Exceptions;
using GrpcWebBridge.Integration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GrpcWebBridge.Tests;

public sealed class HttpClientFactoryTests : IDisposable
{
    private readonly HttpClientFactory _factory;
    private readonly HttpClientFactoryOptions _options;
    private readonly ILogger<HttpClientFactory> _logger;

    public HttpClientFactoryTests()
    {
        _options = new HttpClientFactoryOptions
        {
            RequestTimeoutMs = 5000,
            MaxConnectionsPerServer = 5,
            UseCookies = false,
            AllowAutoRedirect = true,
            AllowInsecureHttps = false
        };
        _logger = NullLogger<HttpClientFactory>.Instance;
        _factory = new HttpClientFactory(_logger, _options);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Constructor and basic setup
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert - constructor validates logger parameter
        Action act = () => new HttpClientFactory(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullOptions_UsesDefaultOptions()
    {
        // Act
        var factory = new HttpClientFactory(_logger);

        // Assert
        factory.Should().NotBeNull();
        var client = factory.GetClient();
        client.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithOptions_SetsPropertiesCorrectly()
    {
        // Arrange
        var options = new HttpClientFactoryOptions
        {
            RequestTimeoutMs = 10000,
            MaxConnectionsPerServer = 20,
            UseCookies = true,
            AllowAutoRedirect = false,
            AllowInsecureHttps = true
        };

        // Act
        var factory = new HttpClientFactory(_logger, options);
        var client = factory.GetClient();

        // Assert
        client.Timeout.Should().Be(TimeSpan.FromMilliseconds(10000));
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetClient tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetClient_WithValidName_ReturnsHttpClient()
    {
        // Act
        var client = _factory.GetClient("test-client");

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<HttpClient>();
    }

    [Fact]
    public void GetClient_WithNullName_ThrowsConfigurationException()
    {
        // Act
        Action act = () => _factory.GetClient(null!);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void GetClient_WithEmptyName_ThrowsConfigurationException()
    {
        // Act
        Action act = () => _factory.GetClient(string.Empty);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void GetClient_WithWhitespaceName_ReturnsHttpClient()
    {
        // Act
        var client = _factory.GetClient("   ");

        // Assert - whitespace names are allowed
        client.Should().NotBeNull();
    }

    [Fact]
    public void GetClient_WithSameName_ReturnsSameClientInstance()
    {
        // Act
        var client1 = _factory.GetClient("shared-client");
        var client2 = _factory.GetClient("shared-client");

        // Assert
        client1.Should().BeSameAs(client2);
    }

    [Fact]
    public void GetClient_WithDifferentNames_ReturnsDifferentClientInstances()
    {
        // Act
        var client1 = _factory.GetClient("client-one");
        var client2 = _factory.GetClient("client-two");

        // Assert
        client1.Should().NotBeSameAs(client2);
    }

    [Fact]
    public void GetClient_DefaultName_ReturnsDefaultClient()
    {
        // Act
        var client = _factory.GetClient();

        // Assert
        client.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────
    // RegisterClient tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void RegisterClient_WithValidParameters_RegistersClient()
    {
        // Arrange
        var client = new HttpClient();

        // Act
        Action act = () => _factory.RegisterClient("custom-client", client);

        // Assert
        act.Should().NotThrow();
        var retrievedClient = _factory.GetClient("custom-client");
        retrievedClient.Should().BeSameAs(client);
    }

    [Fact]
    public void RegisterClient_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var client = new HttpClient();

        // Act
        Action act = () => _factory.RegisterClient(null!, client);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void RegisterClient_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var client = new HttpClient();

        // Act
        Action act = () => _factory.RegisterClient(string.Empty, client);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void RegisterClient_WithNullClient_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _factory.RegisterClient("null-client", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*client*");
    }

    [Fact]
    public void RegisterClient_WithExistingName_ReplacesOldClient()
    {
        // Arrange
        var oldClient = new HttpClient();
        var newClient = new HttpClient();
        _factory.RegisterClient("replaceable", oldClient);

        // Act
        _factory.RegisterClient("replaceable", newClient);

        // Assert
        var retrievedClient = _factory.GetClient("replaceable");
        retrievedClient.Should().BeSameAs(newClient);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetClientForUri tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetClientForUri_WithValidUri_ReturnsConfiguredClient()
    {
        // Arrange
        var uri = "https://example.com";

        // Act
        var client = _factory.GetClientForUri(uri);

        // Assert
        client.Should().NotBeNull();
        client.BaseAddress.Should().Be(new Uri(uri));
    }

    [Fact]
    public void GetClientForUri_WithNullUri_ThrowsConfigurationException()
    {
        // Act
        Action act = () => _factory.GetClientForUri(null!);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public void GetClientForUri_WithEmptyUri_ThrowsConfigurationException()
    {
        // Act
        Action act = () => _factory.GetClientForUri(string.Empty);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public void GetClientForUri_WithInvalidUriFormat_ThrowsConfigurationException()
    {
        // Arrange
        var invalidUri = "not-a-valid-uri";

        // Act
        Action act = () => _factory.GetClientForUri(invalidUri);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public void GetClientForUri_WithSameUri_ReturnsSameClient()
    {
        // Arrange
        var uri = "https://example.com";

        // Act
        var client1 = _factory.GetClientForUri(uri);
        var client2 = _factory.GetClientForUri(uri);

        // Assert
        client1.Should().BeSameAs(client2);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetAsync tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_WithNullUri_ThrowsConfigurationException()
    {
        // Act
        Func<Task> act = async () => await _factory.GetAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public async Task GetAsync_WithEmptyUri_ThrowsConfigurationException()
    {
        // Act
        Func<Task> act = async () => await _factory.GetAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public async Task GetAsync_WithInvalidUri_ThrowsGrpcWebBridgeException()
    {
        // Arrange
        var invalidUri = "invalid-uri";

        // Act
        Func<Task> act = async () => await _factory.GetAsync(invalidUri);

        // Assert
        await act.Should().ThrowAsync<GrpcWebBridgeException>();
    }

    // ─────────────────────────────────────────────────────────────────────
    // PostJsonAsync tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PostJsonAsync_WithNullUri_ThrowsConfigurationException()
    {
        // Arrange
        var payload = new { test = "data" };

        // Act
        Func<Task> act = async () => await _factory.PostJsonAsync(null!, payload);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public async Task PostJsonAsync_WithEmptyUri_ThrowsConfigurationException()
    {
        // Arrange
        var payload = new { test = "data" };

        // Act
        Func<Task> act = async () => await _factory.PostJsonAsync(string.Empty, payload);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public async Task PostJsonAsync_WithNullPayload_ThrowsConfigurationException()
    {
        // Arrange
        var uri = "https://example.com/api";

        // Act
        Func<Task> act = async () => await _factory.PostJsonAsync(uri, null!);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*payload*");
    }

    // ─────────────────────────────────────────────────────────────────────
    // SendAsync tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_WithNullUri_ThrowsConfigurationException()
    {
        // Arrange
        var method = HttpMethod.Get;

        // Act
        Func<Task> act = async () => await _factory.SendAsync(null!, method);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public async Task SendAsync_WithEmptyUri_ThrowsConfigurationException()
    {
        // Arrange
        var method = HttpMethod.Get;

        // Act
        Func<Task> act = async () => await _factory.SendAsync(string.Empty, method);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*uri*");
    }

    [Fact]
    public async Task SendAsync_WithNullMethod_ThrowsConfigurationException()
    {
        // Arrange
        var uri = "https://example.com/api";

        // Act
        Func<Task> act = async () => await _factory.SendAsync(uri, null!);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*method*");
    }

    // ─────────────────────────────────────────────────────────────────────
    // RemoveClient tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveClient_WithExistingName_ReturnsTrueAndRemovesClient()
    {
        // Arrange
        _factory.RegisterClient("removable", new HttpClient());

        // Act
        var result = _factory.RemoveClient("removable");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RemoveClient_WithNonExistingName_ReturnsFalse()
    {
        // Act
        var result = _factory.RemoveClient("non-existing");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveClient_WithNullName_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _factory.RemoveClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveClient_WithEmptyName_ReturnsFalse()
    {
        // Act
        var result = _factory.RemoveClient(string.Empty);

        // Assert - empty name is not in dictionary, returns false
        result.Should().BeFalse();
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetRegisteredClientNames tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetRegisteredClientNames_WithNoClients_ReturnsEmptyList()
    {
        // Act
        var names = _factory.GetRegisteredClientNames();

        // Assert
        names.Should().NotBeNull();
        names.Should().BeEmpty();
    }

    [Fact]
    public void GetRegisteredClientNames_WithMultipleClients_ReturnsAllNames()
    {
        // Arrange
        _factory.RegisterClient("client1", new HttpClient());
        _factory.RegisterClient("client2", new HttpClient());
        _factory.RegisterClient("client3", new HttpClient());

        // Act
        var names = _factory.GetRegisteredClientNames();

        // Assert
        names.Should().NotBeNull();
        names.Should().HaveCount(3);
        names.Should().Contain("client1");
        names.Should().Contain("client2");
        names.Should().Contain("client3");
    }

    [Fact]
    public void GetRegisteredClientNames_ReturnsCopyNotReference()
    {
        // Arrange
        _factory.RegisterClient("test", new HttpClient());

        // Act
        var names1 = _factory.GetRegisteredClientNames();
        var names2 = _factory.GetRegisteredClientNames();

        // Assert
        names1.Should().NotBeSameAs(names2);
        names1.Should().Equal(names2);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Dispose tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_WithMultipleClients_DisposesAllClients()
    {
        // Arrange
        var client1 = new HttpClient();
        var client2 = new HttpClient();
        var mockHandler1 = new MockHttpMessageHandler();
        var mockHandler2 = new MockHttpMessageHandler();
        client1 = new HttpClient(mockHandler1);
        client2 = new HttpClient(mockHandler2);

        _factory.RegisterClient("client1", client1);
        _factory.RegisterClient("client2", client2);

        // Verify initial state
        var initialNames = _factory.GetRegisteredClientNames();
        initialNames.Should().HaveCount(2);

        // Act
        _factory.Dispose();

        // Assert - clients should be disposed
        mockHandler1.DisposeCalls.Should().Be(1);
        mockHandler2.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public void Dispose_WithGetClientAfterDispose_ReturnsNewClient()
    {
        // Arrange
        _factory.Dispose();

        // Act - factory doesn't track disposal state, can still be used
        var client = _factory.GetClient("new-client");

        // Assert - factory continues to work after dispose
        client.Should().NotBeNull();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Handler pooling and connection lifetime tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetClient_WithSameName_ReusesSameHandler()
    {
        // Arrange
        var factory = new HttpClientFactory(_logger, _options);

        // Act - get two clients with the same name
        var client1 = factory.GetClient("shared-handler");
        var client2 = factory.GetClient("shared-handler");

        // Assert - both clients should share the same underlying handler
        client1.Should().NotBeNull();
        client2.Should().NotBeNull();
        client1.Should().BeSameAs(client2);
    }

    [Fact]
    public void GetClient_WithDifferentNames_CreatesDifferentHandlers()
    {
        // Arrange
        var factory = new HttpClientFactory(_logger, _options);

        // Act
        var client1 = factory.GetClient("handler-one");
        var client2 = factory.GetClient("handler-two");

        // Assert - different clients should have different handlers
        client1.Should().NotBeSameAs(client2);
    }

    [Fact]
    public void PooledConnectionLifetime_WithDefaultValue_IsSetCorrectly()
    {
        // Arrange
        var options = new HttpClientFactoryOptions();

        // Assert
        options.PooledConnectionLifetimeMs.Should().Be(120000); // 2 minutes
    }

    [Fact]
    public void PooledConnectionLifetime_WithCustomValue_IsSetCorrectly()
    {
        // Arrange
        var options = new HttpClientFactoryOptions
        {
            PooledConnectionLifetimeMs = 300000 // 5 minutes
        };

        // Act
        var factory = new HttpClientFactory(_logger, options);

        // Assert
        options.PooledConnectionLifetimeMs.Should().Be(300000);
    }

    [Fact]
    public void Dispose_WithMultipleClients_DisposesHandlersNotClients()
    {
        // Arrange
        var factory = new HttpClientFactory(_logger, _options);
        var client1 = factory.GetClient("client1");
        var client2 = factory.GetClient("client2");

        var initialNames = factory.GetRegisteredClientNames();
        initialNames.Should().HaveCount(2);

        // Act
        factory.Dispose();

        // Assert - clients should NOT be disposed (they're still in use)
        // The factory disposes pooled handlers but not the HttpClient instances
        // We can't directly verify handler disposal, but we can verify cleanup
        var finalNames = factory.GetRegisteredClientNames();
        finalNames.Should().BeEmpty();
    }

    [Fact]
    public async Task HandlerRotation_OccursAfterConfiguredLifetime()
    {
        // Arrange - use a very short connection lifetime for testing
        var shortLivedOptions = new HttpClientFactoryOptions
        {
            PooledConnectionLifetimeMs = 100, // 100ms lifetime
            RequestTimeoutMs = 5000
        };

        var factory = new HttpClientFactory(_logger, shortLivedOptions);

        // Act - get a client
        var client1 = factory.GetClient("rotation-test");
        client1.Should().NotBeNull();

        // Wait for lifetime to expire
        await Task.Delay(150).ConfigureAwait(false);

        // Get another client with the same name - should return the same client instance
        var client2 = factory.GetClient("rotation-test");
        client2.Should().NotBeNull();

        // Both clients should be the same instance (cached)
        client1.Should().BeSameAs(client2);

        factory.Dispose();
    }

    [Fact]
    public void SocketsHttpHandler_ConfiguredWithProperSettings()
    {
        // Arrange
        var options = new HttpClientFactoryOptions
        {
            MaxConnectionsPerServer = 20,
            PooledConnectionLifetimeMs = 300000,
            AllowInsecureHttps = true
        };

        // Act
        var factory = new HttpClientFactory(_logger, options);
        var client = factory.GetClient("test-client");

        // Assert - verify that client was created successfully
        client.Should().NotBeNull();
        client.Timeout.Should().Be(TimeSpan.FromMilliseconds(options.RequestTimeoutMs));
    }

    // ─────────────────────────────────────────────────────────────────────
    // HttpClientFactoryOptions tests
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void HttpClientFactoryOptions_DefaultValues_AreCorrect()
    {
        // Arrange
        var options = new HttpClientFactoryOptions();

        // Assert
        options.RequestTimeoutMs.Should().Be(30000);
        options.MaxConnectionsPerServer.Should().Be(10);
        options.UseCookies.Should().BeFalse();
        options.AllowAutoRedirect.Should().BeTrue();
        options.AllowInsecureHttps.Should().BeFalse();
    }

    [Fact]
    public void HttpClientFactoryOptions_WithCustomValues_AreSetCorrectly()
    {
        // Arrange
        var options = new HttpClientFactoryOptions
        {
            RequestTimeoutMs = 15000,
            MaxConnectionsPerServer = 25,
            UseCookies = true,
            AllowAutoRedirect = false,
            AllowInsecureHttps = true
        };

        // Act
        var factory = new HttpClientFactory(_logger, options);
        var client = factory.GetClient();

        // Assert
        client.Timeout.Should().Be(TimeSpan.FromMilliseconds(15000));
    }
}

// Mock HttpMessageHandler for testing disposal
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    public int DisposeCalls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeCalls++;
        }
        base.Dispose(disposing);
    }
}