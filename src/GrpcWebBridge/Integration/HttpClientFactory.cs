#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Net;
using GrpcWebBridge.Domain.Exceptions;

namespace GrpcWebBridge.Integration;

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

    public HttpClientFactory(ILogger<HttpClientFactory> logger, HttpClientFactoryOptions? options = null)
    {
        _clients = new ConcurrentDictionary<string, HttpClient>();
        _logger = logger;
        _options = options ?? new HttpClientFactoryOptions();

        // Configure default HTTP client handler
        ConfigureDefaultHandler();
    }

    /// <summary>
    /// Gets or creates an HTTP client for a specific endpoint.
    /// </summary>
    public HttpClient GetClient(string name = "default")
    {
        if (string.IsNullOrEmpty(name))
            throw new ConfigurationException(nameof(name), "HTTP client name cannot be null or empty");

        return _clients.GetOrAdd(name, _ =>
        {
            _logger.LogDebug("Creating new HTTP client: Name={Name}", name);
            return CreateConfiguredClient(name);
        });
    }

    /// <summary>
    /// Creates a new configured HTTP client with timeouts and handlers.
    /// </summary>
    private HttpClient CreateConfiguredClient(string name)
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseCookies = _options.UseCookies,
            AllowAutoRedirect = _options.AllowAutoRedirect,
            MaxConnectionsPerServer = _options.MaxConnectionsPerServer,
            ServerCertificateCustomValidationCallback = _options.AllowInsecureHttps
                ? (msg, cert, chain, errors) => true
                : null
        };

        var client = new HttpClient(handler, disposeHandler: true)
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
    /// Registers a pre-configured client.
    /// </summary>
    public void RegisterClient(string name, HttpClient client)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Client name cannot be null or empty", nameof(name));

        if (client is null)
            throw new ArgumentNullException(nameof(client));

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
    public bool RemoveClient(string name)
    {
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
    public List<string> GetRegisteredClientNames()
    {
        return _clients.Keys.ToList();
    }

    private void ConfigureDefaultHandler()
    {
        ServicePointManager.DefaultConnectionLimit = _options.MaxConnectionsPerServer;
        ServicePointManager.ReusePort = true;
    }

    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            client?.Dispose();
        }

        _clients.Clear();
    }
}

/// <summary>
/// Configuration options for HTTP client factory.
/// </summary>
public sealed class HttpClientFactoryOptions
{
    public int RequestTimeoutMs { get; set; } = 30000;
    public int MaxConnectionsPerServer { get; set; } = 10;
    public bool UseCookies { get; set; } = false;
    public bool AllowAutoRedirect { get; set; } = true;
    public bool AllowInsecureHttps { get; set; } = false;
}
