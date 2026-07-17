using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GrpcWebBridge.Domain.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="ServiceRegistrationException"/> to enhance error handling and diagnostics.
    /// </summary>
    public static class ServiceRegistrationExceptionExtensions
    {
        /// <summary>
        /// Gets a human-readable string representation of the exception with service details.
        /// </summary>
        /// <param name="exception">The <see cref="ServiceRegistrationException"/> instance.</param>
        /// <returns>A detailed string representation of the exception including service name and endpoint.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is <see langword="null"/>.</exception>
        public static string ToDetailedString(this ServiceRegistrationException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return string.Create(
                CultureInfo.InvariantCulture,
                stackalloc char[512],
                $"{exception.Message} (Service: {exception.ServiceName ?? "N/A"}, Endpoint: {exception.ServiceEndpoint ?? "N/A"})");
        }

        /// <summary>
        /// Attempts to extract the service endpoint from the exception.
        /// </summary>
        /// <param name="exception">The <see cref="ServiceRegistrationException"/> instance.</param>
        /// <param name="endpoint">When this method returns, contains the extracted endpoint if successful; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the endpoint was successfully extracted; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is <see langword="null"/>.</exception>
        public static bool TryExtractEndpoint(this ServiceRegistrationException exception, out string? endpoint)
        {
            ArgumentNullException.ThrowIfNull(exception);

            endpoint = exception.ServiceEndpoint;
            return !string.IsNullOrEmpty(endpoint);
        }

        /// <summary>
        /// Combines multiple <see cref="ServiceRegistrationException"/> instances into a single <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="exceptions">The sequence of <see cref="ServiceRegistrationException"/> instances to combine.</param>
        /// <returns>A new <see cref="AggregateException"/> containing all the original exceptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exceptions"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="exceptions"/> is empty or contains only <see langword="null"/> values.</exception>
        public static AggregateException CombineExceptions(IEnumerable<ServiceRegistrationException> exceptions)
        {
            ArgumentNullException.ThrowIfNull(exceptions);

            if (!exceptions.Any())
            {
                throw new ArgumentException("At least one exception is required.", nameof(exceptions));
            }

            var nonNullExceptions = exceptions.Where(ex => ex is not null).ToList();

            if (nonNullExceptions.Count == 0)
            {
                throw new ArgumentException("At least one non-null exception is required.", nameof(exceptions));
            }

            return new AggregateException(nonNullExceptions);
        }
    }
}