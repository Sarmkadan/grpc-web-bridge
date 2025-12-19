# HttpClientFactory

The `HttpClientFactory` provides a centralized mechanism for managing, configuring, and reusing `HttpClient` instances within the `grpc-web-bridge` project. By consolidating client lifecycle management and configuration settings—such as timeouts, security policies, and connection limits—it promotes efficient network resource utilization and ensures consistent HTTP communication patterns across the application.

## API

### Methods

*   **`HttpClientFactory()`**
    Initializes a new instance of the `HttpClientFactory` class with default configuration settings.

*   **`HttpClient GetClient(string name)`**
    Retrieves a registered `HttpClient` instance by its identifier.
    *   `name`: The unique identifier of the client.
    *   Returns: The `HttpClient` instance.
    *   Throws: `KeyNotFoundException` if no client is registered with the specified name.

*   **`void RegisterClient(string name, HttpClient client)`**
    Registers an `HttpClient` instance to be managed by the factory under the given name.
    *   `name`: The unique identifier for the client.
    *   `client`: The `HttpClient` instance to manage.

*   **`HttpClient GetClientForUri(Uri uri)`**
    Retrieves an `HttpClient` instance appropriate for the specified target URI.
    *   `uri`: The target URI.
    *   Returns: An `HttpClient` instance configured for this destination.

*   **`async Task<string> GetAsync(string name, string uri)`**
    Performs an asynchronous GET request using the client identified by `name`.
    *   `name`: The client identifier.
    *   `uri`: The target URI string.
    *   Returns: The response content as a string.

*   **`async Task<string> PostJsonAsync(string name, string uri, object data)`**
    Performs an asynchronous POST request with serialized JSON content using the client identified by `name`.
    *   `name`: The client identifier.
    *   `uri`: The target URI string.
    *   `data`: The object to be serialized.
    *   Returns: The response content as a string.

*   **`async Task<HttpResponseMessage> SendAsync(string name, HttpRequestMessage message)`**
    Sends an `HttpRequestMessage` using the specified registered client.
    *   `name`: The client identifier.
    *   `message`: The `HttpRequestMessage` to send.
    *   Returns: The `HttpResponseMessage`.

*   **`bool RemoveClient(string name)`**
    Removes a registered `HttpClient` instance from the factory.
    *   `name`: The identifier to remove.
    *   Returns: `true` if the client was found and removed, `false` otherwise.

*   **`List<string> GetRegisteredClientNames()`**
    Returns a list of the names of all currently registered `HttpClient` instances.
    *   Returns: A list of client identifiers.

*   **`void Dispose()`**
    Releases all resources used by the factory, including any underlying connection pools associated with managed clients.

### Properties

*   **`int RequestTimeoutMs`**
    Gets or sets the timeout in milliseconds for network requests.
*   **`int MaxConnectionsPerServer`**
    Gets or sets the maximum number of concurrent connections allowed per server endpoint.
*   **`bool UseCookies`**
    Gets or sets whether the underlying client handler should manage cookies.
*   **`bool AllowAutoRedirect`**
    Gets or sets whether the underlying client handler should automatically follow HTTP redirects.
*   **`bool AllowInsecureHttps`**
    Gets or sets whether the underlying client handler permits insecure HTTPS connections, such as those with self-signed certificates.

## Usage

### Registering and Using a Client
```csharp
var factory = new HttpClientFactory();
var client = new HttpClient();
factory.RegisterClient("api-client", client);

// Perform a GET request using the registered client
string response = await factory.GetAsync("api-client", "https://api.example.com/data");
```

### Configuring Factory Behavior and JSON POST
```csharp
var factory = new HttpClientFactory
{
    RequestTimeoutMs = 5000,
    AllowAutoRedirect = false
};

var data = new { Name = "Test", Value = 123 };
// Performs POST with JSON serialization
string result = await factory.PostJsonAsync("json-client", "https://api.example.com/submit", data);
```

## Notes

*   **Thread Safety**: The `HttpClientFactory` is designed to be thread-safe for registering and retrieving clients. Multiple threads may concurrently request or utilize registered clients.
*   **Disposal**: The `Dispose` method must be called to properly release the underlying `HttpClientHandler` and connection pools associated with the factory and all its managed clients. Failure to dispose of the factory when no longer needed may result in socket exhaustion or memory leaks.
*   **Configuration Scope**: Property changes (e.g., `RequestTimeoutMs`, `AllowInsecureHttps`) apply to the configuration of the factory. Whether these changes affect already-initialized clients depends on the implementation details of those clients and their handlers. For consistent behavior, configure these properties before registering clients.
