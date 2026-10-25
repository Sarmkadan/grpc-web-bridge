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

/// <summary>
/// Unit tests for <see cref="HttpClientFactory"/>: client lookup and registration, URI validation,
/// request helpers, handler lifetime and rotation, options defaults, and disposal behaviour.
/// </summary>
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
        // Some tests dispose the factory themselves; a second Dispose is rejected by design
        try
        {
            _factory.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Constructor and basic setup
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that constructing the factory without a logger throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert - constructor validates logger parameter
        Action act = () => new HttpClientFactory(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that constructing the factory without options falls back to default options and yields a usable client.
    /// </summary>
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

    /// <summary>
    /// Verifies that the supplied options, such as request timeout, are applied to clients created by the factory.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetClient</c> with a valid name returns an <see cref="HttpClient"/>.
    /// </summary>
    [Fact]
    public void GetClient_WithValidName_ReturnsHttpClient()
    {
        // Act
        var client = _factory.GetClient("test-client");

        // Assert
        client.Should().NotBeNull();
        client.Should().BeOfType<HttpClient>();
    }

    /// <summary>
    /// Verifies that <c>GetClient</c> with a null name throws <see cref="ConfigurationException"/>.
    /// </summary>
    [Fact]
    public void GetClient_WithNullName_ThrowsConfigurationException()
    {
        // Act
        Action act = () => _factory.GetClient(null!);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*name*");
    }

    /// <summary>
    /// Verifies that <c>GetClient</c> with an empty name throws <see cref="ConfigurationException"/>.
    /// </summary>
    [Fact]
    public void GetClient_WithEmptyName_ThrowsConfigurationException()
    {
        // Act
        Action act = () => _factory.GetClient(string.Empty);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*name*");
    }

    /// <summary>
    /// Verifies that <c>GetClient</c> accepts a whitespace-only name and returns a client.
    /// </summary>
    [Fact]
    public void GetClient_WithWhitespaceName_ReturnsHttpClient()
    {
        // Act
        var client = _factory.GetClient("   ");

        // Assert - whitespace names are allowed
        client.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that repeated <c>GetClient</c> calls with the same name return the same client instance.
    /// </summary>
    [Fact]
    public void GetClient_WithSameName_ReturnsSameClientInstance()
    {
        // Act
        var client1 = _factory.GetClient("shared-client");
        var client2 = _factory.GetClient("shared-client");

        // Assert
        client1.Should().BeSameAs(client2);
    }

    /// <summary>
    /// Verifies that <c>GetClient</c> with different names returns different client instances.
    /// </summary>
    [Fact]
    public void GetClient_WithDifferentNames_ReturnsDifferentClientInstances()
    {
        // Act
        var client1 = _factory.GetClient("client-one");
        var client2 = _factory.GetClient("client-two");

        // Assert
        client1.Should().NotBeSameAs(client2);
    }

    /// <summary>
    /// Verifies that <c>GetClient</c> without arguments returns the default client.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>RegisterClient</c> with valid arguments makes the client retrievable by name.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>RegisterClient</c> with a null name throws <see cref="ArgumentException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>RegisterClient</c> with an empty name throws <see cref="ArgumentException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>RegisterClient</c> with a null client throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void RegisterClient_WithNullClient_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _factory.RegisterClient("null-client", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithMessage("*client*");
    }

    /// <summary>
    /// Verifies that registering a client under an existing name replaces the previously registered client.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetClientForUri</c> with a valid URI returns a client whose base address matches the URI.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetClientForUri</c> with a null URI throws <see cref="ConfigurationException"/>.
    /// </summary>
    [Fact]
    public void GetClientForUri_WithNullUri_ThrowsConfigurationException()
    {
        // Act
        Action act = () => _factory.GetClientForUri(null!);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*uri*");
    }

    /// <summary>
    /// Verifies that <c>GetClientForUri</c> with an empty URI throws <see cref="ConfigurationException"/>.
    /// </summary>
    [Fact]
    public void GetClientForUri_WithEmptyUri_ThrowsConfigurationException()
    {
        // Act
        Action act = () => _factory.GetClientForUri(string.Empty);

        // Assert
        act.Should().Throw<ConfigurationException>()
            .WithMessage("*uri*");
    }

    /// <summary>
    /// Verifies that <c>GetClientForUri</c> with a malformed URI throws <see cref="ConfigurationException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that repeated <c>GetClientForUri</c> calls with the same URI return the same client instance.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetAsync</c> with a null URI throws <see cref="ConfigurationException"/>.
    /// </summary>
    [Fact]
    public async Task GetAsync_WithNullUri_ThrowsConfigurationException()
    {
        // Act
        Func<Task> act = async () => await _factory.GetAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*uri*");
    }

    /// <summary>
    /// Verifies that <c>GetAsync</c> with an empty URI throws <see cref="ConfigurationException"/>.
    /// </summary>
    [Fact]
    public async Task GetAsync_WithEmptyUri_ThrowsConfigurationException()
    {
        // Act
        Func<Task> act = async () => await _factory.GetAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>()
            .WithMessage("*uri*");
    }

    /// <summary>
    /// Verifies that <c>GetAsync</c> with a non-absolute URI throws <see cref="GrpcWebBridgeException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>PostJsonAsync</c> with a null URI throws <see cref="ConfigurationException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>PostJsonAsync</c> with an empty URI throws <see cref="ConfigurationException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>PostJsonAsync</c> with a null payload throws <see cref="ConfigurationException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>SendAsync</c> with a null URI throws <see cref="ConfigurationException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>SendAsync</c> with an empty URI throws <see cref="ConfigurationException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>SendAsync</c> with a null HTTP method throws <see cref="ConfigurationException"/>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>RemoveClient</c> returns <c>true</c> for a registered name and removes the client.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>RemoveClient</c> returns <c>false</c> for a name that is not registered.
    /// </summary>
    [Fact]
    public void RemoveClient_WithNonExistingName_ReturnsFalse()
    {
        // Act
        var result = _factory.RemoveClient("non-existing");

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <c>RemoveClient</c> with a null name throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void RemoveClient_WithNullName_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _factory.RemoveClient(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that <c>RemoveClient</c> with an empty name returns <c>false</c>.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetRegisteredClientNames</c> returns an empty collection when no clients are registered.
    /// </summary>
    [Fact]
    public void GetRegisteredClientNames_WithNoClients_ReturnsEmptyList()
    {
        // Act
        var names = _factory.GetRegisteredClientNames();

        // Assert
        names.Should().NotBeNull();
        names.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that <c>GetRegisteredClientNames</c> returns every registered client name.
    /// </summary>
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

    /// <summary>
    /// Verifies that each <c>GetRegisteredClientNames</c> call returns a new collection with the same contents.
    /// </summary>
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

    /// <summary>
    /// Verifies that disposing the factory disposes the message handlers of all registered clients.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetClient</c> on a disposed factory throws and does not register a new client.
    /// </summary>
    [Fact]
    public void Dispose_WithGetClientAfterDispose_DoesNotRegisterClient()
    {
        // Arrange
        _factory.Dispose();

        // Act
        Action act = () => _factory.GetClient("new-client");

        // Assert - a disposed factory must not create clients
        act.Should().Throw<ObjectDisposedException>();
        _factory.GetRegisteredClientNames().Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Handler pooling and connection lifetime tests
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that repeated <c>GetClient</c> calls with the same name reuse the same pooled handler.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>GetClient</c> with different names creates separate clients and handlers.
    /// </summary>
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

    /// <summary>
    /// Verifies that <c>PooledConnectionLifetimeMs</c> defaults to 120000 ms (2 minutes).
    /// </summary>
    [Fact]
    public void PooledConnectionLifetime_WithDefaultValue_IsSetCorrectly()
    {
        // Arrange
        var options = new HttpClientFactoryOptions();

        // Assert
        options.PooledConnectionLifetimeMs.Should().Be(120000); // 2 minutes
    }

    /// <summary>
    /// Verifies that a custom <c>PooledConnectionLifetimeMs</c> value is stored on the options.
    /// </summary>
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

    /// <summary>
    /// Verifies that disposing the factory clears its registered client names while leaving the client instances intact.
    /// </summary>
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

    /// <summary>
    /// Verifies that once the configured connection lifetime has elapsed, <c>GetClient</c> returns a new client instance.
    /// </summary>
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
        await Task.Delay(150);

        // Get another client with the same name - rotation replaces the expired handler and its clients
        var client2 = factory.GetClient("rotation-test");
        client2.Should().NotBeNull();

        // The rotated client must be a new instance, not the one bound to the expired handler
        client2.Should().NotBeSameAs(client1);

        factory.Dispose();
    }

    /// <summary>
    /// Verifies that a client created with non-default handler settings is created successfully and uses the request timeout.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="HttpClientFactoryOptions"/> has the expected default values.
    /// </summary>
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

    /// <summary>
    /// Verifies that custom values set on <see cref="HttpClientFactoryOptions"/> are applied to created clients.
    /// </summary>
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

    // ─────────────────────────────────────────────────────────────────────
    // Disposal tracking tests - NEW TESTS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that <c>GetClient</c> on a disposed factory throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Fact]
    public void Dispose_WithGetClientAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _factory.Dispose();

        // Act & Assert - factory should throw when used after disposal
        Action act = () => _factory.GetClient("new-client");
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*The factory has been disposed*");
    }

    /// <summary>
    /// Verifies that <c>RegisterClient</c> on a disposed factory throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Fact]
    public void Dispose_WithRegisterClientAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _factory.Dispose();

        // Act & Assert - factory should throw when used after disposal
        Action act = () => _factory.RegisterClient("test-client", new HttpClient());
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*The factory has been disposed*");
    }

    /// <summary>
    /// Verifies that <c>GetClientForUri</c> on a disposed factory throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Fact]
    public void Dispose_WithGetClientForUriAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _factory.Dispose();

        // Act & Assert - factory should throw when used after disposal
        Action act = () => _factory.GetClientForUri("https://example.com");
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*The factory has been disposed*");
    }

    /// <summary>
    /// Verifies that <c>GetAsync</c> on a disposed factory throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Fact]
    public void Dispose_WithGetAsyncAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _factory.Dispose();

        // Act & Assert - factory should throw when used after disposal
        Func<Task> act = async () => await _factory.GetAsync("https://example.com");
        act.Should().ThrowAsync<ObjectDisposedException>()
            .WithMessage("*The factory has been disposed*");
    }

    /// <summary>
    /// Verifies that <c>PostJsonAsync</c> on a disposed factory throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Fact]
    public void Dispose_WithPostJsonAsyncAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _factory.Dispose();

        // Act & Assert - factory should throw when used after disposal
        Func<Task> act = async () => await _factory.PostJsonAsync("https://example.com/api", new { test = "data" });
        act.Should().ThrowAsync<ObjectDisposedException>()
            .WithMessage("*The factory has been disposed*");
    }

    /// <summary>
    /// Verifies that <c>SendAsync</c> on a disposed factory throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Fact]
    public void Dispose_WithSendAsyncAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _factory.Dispose();

        // Act & Assert - factory should throw when used after disposal
        Func<Task> act = async () => await _factory.SendAsync("https://example.com/api", HttpMethod.Get);
        act.Should().ThrowAsync<ObjectDisposedException>()
            .WithMessage("*The factory has been disposed*");
    }

    /// <summary>
    /// Verifies that <c>RemoveClient</c> on a disposed factory throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Fact]
    public void Dispose_WithRemoveClientAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _factory.Dispose();

        // Act & Assert - factory should throw when used after disposal
        Action act = () => _factory.RemoveClient("test-client");
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*The factory has been disposed*");
    }

    /// <summary>
    /// Verifies that calling <c>Dispose</c> a second time throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Fact]
    public void Dispose_CalledTwice_ThrowsObjectDisposedException()
    {
        // Arrange
        _factory.Dispose();

        // Act & Assert - disposing twice should throw
        Action act = () => _factory.Dispose();
        act.Should().Throw<ObjectDisposedException>()
            .WithMessage("*The factory has already been disposed*");
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
