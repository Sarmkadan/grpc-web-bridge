using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace GrpcWebBridge.Integration
{
    /// <summary>
    /// Extension methods for <see cref="HttpClientFactory"/>.
    /// </summary>
    public static class HttpClientFactoryExtensions
    {
        /// <summary>
        /// Gets a client with a specific base address.
        /// </summary>
        /// <param name="factory">The <see cref="HttpClientFactory"/> instance.</param>
        /// <param name="baseAddress">The base address for the client.</param>
        /// <returns>A new <see cref="HttpClient"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is null.</exception>
        /// <exception cref="ConfigurationException">Thrown if <paramref name="baseAddress"/> is null or empty.</exception>
        public static HttpClient GetClientWithBaseAddress(this HttpClientFactory factory, string baseAddress)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrEmpty(baseAddress);

            var client = factory.GetClient();
            client.BaseAddress = new Uri(baseAddress);
            return client;
        }

        /// <summary>
        /// Sends a GET request with a specific timeout.
        /// </summary>
        /// <param name="factory">The <see cref="HttpClientFactory"/> instance.</param>
        /// <param name="requestUri">The request URI.</param>
        /// <param name="timeout">The request timeout.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the response.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is null.</exception>
        /// <exception cref="ConfigurationException">Thrown if <paramref name="requestUri"/> is null or empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is negative.</exception>
        public static async Task<HttpResponseMessage> SendGetAsyncWithTimeout(this HttpClientFactory factory, string requestUri, TimeSpan timeout)
        {
            ArgumentNullException.ThrowIfNull(factory);
            ArgumentException.ThrowIfNullOrEmpty(requestUri);
            ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);

            var client = factory.GetClient();
            client.Timeout = timeout;
            return await client.GetAsync(requestUri).ConfigureAwait(false);
        }
    }
}
