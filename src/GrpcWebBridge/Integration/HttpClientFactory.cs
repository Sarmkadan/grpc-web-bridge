#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using GrpcWebBridge.Domain.Exceptions;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Configuration options for HTTP client factory.
/// </summary>
public sealed class HttpClientFactoryOptions
{
    /// <summary>
    /// Gets or sets the request timeout in milliseconds.
    /// </summary>
    public int RequestTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the maximum number of connections per server.
    /// </summary>
    public int MaxConnectionsPerServer { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether to use cookies.
    /// </summary>
    public bool UseCookies { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow automatic redirection.
    /// </summary>
    public bool AllowAutoRedirect { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to allow insecure HTTPS connections.
    /// </summary>
    public bool AllowInsecureHttps { get; set; }

    /// <summary>
    /// Gets or sets the pooled connection lifetime in milliseconds.
    /// After this duration, the underlying SocketsHttpHandler will be rotated to
    /// prevent DNS staleness and ensure connection freshness.
    /// Default is 2 minutes (120000 ms).
    /// </summary>
    public int PooledConnectionLifetimeMs { get; set; } = 120000;
}

/// <summary>
/// HTTP client factory managing pooled HTTP clients.
/// Provides clients for external service communication with proper configuration.
/// Implements connection pooling and health monitoring.
/// </summary>
public sealed class HttpClientFactory : IDisposable
{
    private readonly ConcurrentDictionary<string, HttpClient> _clients;
    private readonly ILogger<HttpClientFactory> _logger;
    private readonly HttpClientFactoryOptions _options;
    private readonly object _handlerRotationLock = new object();
    private DateTime _lastHandlerRotation;
    private bool _disposed;

    // Single shared handler used by all HttpClient instances
    private PooledHandler _sharedHandler;

    public HttpClientFactory(ILogger<HttpClientFactory> logger, HttpClientFactoryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _clients = new ConcurrentDictionary<string, HttpClient>();
        _logger = logger;
        _options = options ?? new HttpClientFactoryOptions();
        _lastHandlerRotation = DateTime.UtcNow;

        // Create the shared handler once
        _sharedHandler = new PooledHandler(_options, "shared");

        // Configure default HTTP client handler
        ConfigureDefaultHandler();
    }

    /// <summary>
    /// Gets or creates an HTTP client for a specific endpoint.
    /// </summary>
    /// <param name="name">The name of the client.</param>
    /// <returns>An HTTP client instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
    /// <exception cref="ConfigurationException">Thrown if name is empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the factory has been disposed.</exception>
    public HttpClient GetClient(string name = "default")
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (_disposed)
            throw new ObjectDisposedException(nameof(HttpClientFactory), "The factory has been disposed and cannot create new clients.");

        // Check if handlers need rotation based on connection lifetime
        CheckHandlerRotation();

        return _clients.GetOrAdd(name, _ =>
        {
            _logger.LogDebug("Creating new HTTP client: Name={Name}", name);
            return CreateConfiguredClient(name);
        });
    }

    /// <summary>
    /// Creates a new configured HTTP client with timeouts and handlers.
    /// </summary>
    /// <param name="name">The client name.</param>
    /// <returns>A configured HTTP client.</returns>
    private HttpClient CreateConfiguredClient(string name)
    {
        var handler = GetSharedHandler();

        var client = new HttpClient(handler.Handler, disposeHandler: false)
        {
            Timeout = _options.RequestTimeoutMs > 0
                ? TimeSpan.FromMilliseconds(_options.RequestTimeoutMs)
                : TimeSpan.FromSeconds(30)
        };

        // Add default headers
        client.DefaultRequestHeaders.Add("User-Agent", $"GrpcWebBridge/{typeof(HttpClientFactory).Assembly.GetName().Version}");

        return client;
    }

    /// <summary>
    /// Returns the shared pooled handler.
    /// </summary>
    private PooledHandler GetSharedHandler()
    {
        return _sharedHandler;
    }

    /// <summary>
    /// Checks if handlers need to be rotated based on pooled connection lifetime.
    /// When rotating, also disposes any existing HttpClient instances that were
    /// using the old handlers to avoid socket exhaustion or using disposed handlers.
    /// </summary>
    private void CheckHandlerRotation()
    {
        if (_options.PooledConnectionLifetimeMs <= 0)
            return;

        var lifetime = TimeSpan.FromMilliseconds(_options.PooledConnectionLifetimeMs);
        var now = DateTime.UtcNow;
        var timeSinceLastRotation = now - _lastHandlerRotation;

        if (timeSinceLastRotation >= lifetime)
        {
            lock (_handlerRotationLock)
            {
                // Double‑check after acquiring lock
                if ((now - _lastHandlerRotation) >= lifetime)
                {
                    _logger.LogInformation("Rotating HTTP handlers due to connection lifetime expiry: Lifetime={Lifetime}, Elapsed={Elapsed}",
                        lifetime, timeSinceLastRotation);

                    // Dispose old shared handler
                    _sharedHandler.Dispose();

                    // Create a new shared handler
                    _sharedHandler = new PooledHandler(_options, "shared");

                    // Dispose any HttpClients that were using the old handler
                    foreach (var clientEntry in _clients)
                    {
                        clientEntry.Value?.Dispose();
                    }
                    _clients.Clear();

                    _lastHandlerRotation = now;
                }
            }
        }
    }

    /// <summary>
    /// Registers a pre-configured client.
    /// </summary>
    /// <param name="name">The client name.</param>
    /// <param name="client">The HTTP client to register.</param>
    /// <exception cref="ArgumentException">Thrown if name is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown if client is null.</exception>
    public void RegisterClient(string name, HttpClient client)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(client);

        _clients.AddOrUpdate(name, client, (_, old) =>
        {
            old?.Dispose();
            return client;
        });

        _logger.LogInformation("HTTP client registered: Name={Name}", name);
    }

    /// <summary>
    /// Gets or creates a client for a specific base address.
    /// </summary>
    /// <param name="baseUri">The base URI for the client.</param>
    /// <returns>An HTTP client configured with the base address.</returns>
    /// <exception cref="ConfigurationException">Thrown if baseUri is null or empty.</exception>
    public HttpClient GetClientForUri(string baseUri)
    {
        if (string.IsNullOrEmpty(baseUri))
            throw new ConfigurationException(nameof(baseUri), "Base URI cannot be null or empty");

        try
        {
            var uri = new Uri(baseUri);
            var client = GetClient(baseUri);
            if (client.BaseAddress is null)
            {
                client.BaseAddress = uri;
            }

            return client;
        }
        catch (UriFormatException ex)
        {
            throw new ConfigurationException(nameof(baseUri), baseUri, "Invalid URI format")
                .WithContext("uriFormat", baseUri)
                .WithInnerException(ex);
        }
    }

    /// <summary>
    /// Sends a GET request and returns response content as string.
    /// </summary>
    /// <param name="uri">The request URI.</param>
    /// <param name="clientName">Optional client name.</param>
    /// <returns>The response content as a string.</returns>
    /// <exception cref="ConfigurationException">Thrown if URI is invalid.</exception>
    /// <exception cref="GrpcWebBridgeException">Thrown if the request fails.</exception>
    public async Task<string> GetAsync(string uri, string? clientName = null)
    {
        if (string.IsNullOrEmpty(uri))
            throw new ConfigurationException(nameof(uri), "URI cannot be null or empty");

        try
        {
            var client = GetClient(clientName ?? "default");
            var response = await client.GetAsync(uri).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP GET request failed: URI={URI}, StatusCode={StatusCode}", uri, ex.StatusCode);
            throw new ConfigurationException(nameof(uri), uri, $"HTTP GET request failed with status {ex.StatusCode}")
                .WithInnerException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET request failed: URI={URI}", uri);
            throw new GrpcWebBridgeException($"GET request to {uri} failed: {ex.Message}", "HTTP_REQUEST_FAILED")
                .WithInnerException(ex);
        }
    }

    /// <summary>
    /// Sends a POST request with JSON body.
    /// </summary>
    /// <param name="uri">The request URI.</param>
    /// <param name="payload">The JSON payload to send.</param>
    /// <param name="clientName">Optional client name.</param>
    /// <returns>The response content as a string.</returns>
    /// <exception cref="ConfigurationException">Thrown if URI is invalid or payload is null.</exception>
    /// <exception cref="GrpcWebBridgeException">Thrown if the request fails.</exception>
    public async Task<string> PostJsonAsync(string uri, object payload, string? clientName = null)
    {
        if (string.IsNullOrEmpty(uri))
            throw new ConfigurationException(nameof(uri), "URI cannot be null or empty");

        if (payload is null)
            throw new ConfigurationException(nameof(payload), "Payload cannot be null");

        try
        {
            var client = GetClient(clientName ?? "default");
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(uri, content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP POST request failed: URI={URI}, StatusCode={StatusCode}", uri, ex.StatusCode);
            throw new ConfigurationException(nameof(uri), uri, $"HTTP POST request failed with status {ex.StatusCode}")
                .WithInnerException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST request failed: URI={URI}", uri);
            throw new GrpcWebBridgeException($"POST request to {uri} failed: {ex.Message}", "HTTP_REQUEST_FAILED")
                .WithInnerException(ex);
        }
    }

    /// <summary>
    /// Sends a request with custom configuration.
    /// </summary>
    /// <param name="uri">The request URI.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="content">Optional request content.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="clientName">Optional client name.</param>
    /// <returns>The HTTP response message.</returns>
    /// <exception cref="ConfigurationException">Thrown if URI or method is invalid.</exception>
    /// <exception cref="GrpcWebBridgeException">Thrown if the request fails.</exception>
    public async Task<HttpResponseMessage> SendAsync(
        string uri,
        HttpMethod method,
        HttpContent? content = null,
        Dictionary<string, string>? headers = null,
        string? clientName = null)
    {
        if (string.IsNullOrEmpty(uri))
            throw new ConfigurationException(nameof(uri), "URI cannot be null or empty");

        if (method is null)
            throw new ConfigurationException(nameof(method), "HTTP method cannot be null");

        try
        {
            var client = GetClient(clientName ?? "default");
            var request = new HttpRequestMessage(method, uri)
            {
                Content = content
            };

            if (headers is not null)
            {
                foreach (var (key, value) in headers)
                {
                    request.Headers.Add(key, value);
                }
            }

            var response = await client.SendAsync(request).ConfigureAwait(false);
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request failed: URI={URI}, Method={Method}, StatusCode={StatusCode}", uri, method, ex.StatusCode);
            throw new ConfigurationException(nameof(uri), uri, $"HTTP {method} request failed with status {ex.StatusCode}")
                .WithInnerException(ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request failed: URI={URI}, Method={Method}", uri, method);
            throw new GrpcWebBridgeException($"{method} request to {uri} failed: {ex.Message}", "HTTP_REQUEST_FAILED")
                .WithInnerException(ex);
        }
    }

    /// <summary>
    /// Removes a registered client.
    /// </summary>
    /// <param name="name">The client name to remove.</param>
    /// <returns>True if the client was found and removed; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
    public bool RemoveClient(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_clients.TryRemove(name, out var client))
        {
            client?.Dispose();
            _logger.LogInformation("HTTP client removed: Name={Name}", name);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets list of registered client names.
    /// </summary>
    /// <returns>A list of registered client names.</returns>
    public List<string> GetRegisteredClientNames()
    {
        return _clients.Keys.ToList();
    }

    private void ConfigureDefaultHandler()
    {
        ServicePointManager.DefaultConnectionLimit = _options.MaxConnectionsPerServer;
        ServicePointManager.ReusePort = true;
    }

    /// <summary>
    /// Disposes the factory and all registered clients.
    /// Note: Disposes only the clients managed by this factory.
    /// Clients registered via RegisterClient are disposed by the caller.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if the factory has already been disposed.</exception>
    public void Dispose()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HttpClientFactory), "The factory has already been disposed.");

        _disposed = true;

        // Dispose the shared handler first (it's a shared resource)
        _sharedHandler?.Dispose();

        // Dispose all clients managed by this factory
        foreach (var client in _clients.Values)
        {
            client?.Dispose();
        }

        _clients.Clear();
    }

    /// <summary>
    /// Internal class to manage pooled SocketsHttpHandler instances.
    /// </summary>
    private sealed class PooledHandler : IDisposable
    {
        public SocketsHttpHandler Handler { get; }
        private readonly HttpClientFactoryOptions _options;
        private bool _disposed;

        public PooledHandler(HttpClientFactoryOptions options, string handlerName)
        {
            _options = options;
            Handler = CreateHandler(handlerName);
        }

        private SocketsHttpHandler CreateHandler(string handlerName)
        {
            return new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = _options.UseCookies,
                AllowAutoRedirect = _options.AllowAutoRedirect,
                MaxConnectionsPerServer = _options.MaxConnectionsPerServer,
                PooledConnectionLifetime = TimeSpan.FromMilliseconds(_options.PooledConnectionLifetimeMs),
                PooledConnectionIdleTimeout = TimeSpan.FromMilliseconds(_options.PooledConnectionLifetimeMs),
                ConnectTimeout = TimeSpan.FromSeconds(10),
                EnableMultipleHttp2Connections = true,
                SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = _options.AllowInsecureHttps
                        ? (sender, certificate, chain, errors) => true
                        : null
                }
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Handler.Dispose();
        }
    }
}
