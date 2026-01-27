using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GrpcWebBridge.Domain.Exceptions
{
    /// <summary>
    /// Extensions for <see cref="ServiceRegistrationException"/>.
    /// </summary>
    public static class ServiceRegistrationExceptionExtensions
    {
        /// <summary>
        /// Gets a human-readable string representation of the exception with service details.
        /// </summary>
        /// <param name="exception">The <see cref="ServiceRegistrationException"/> instance.</param>
        /// <returns>A string representation of the exception.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static string ToDetailedString(this ServiceRegistrationException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return $"{exception.Message} (Service: {exception.ServiceName}, Endpoint: {exception.ServiceEndpoint})";
        }

        /// <summary>
        /// Tries to extract the service endpoint from the exception message.
        /// </summary>
        /// <param name="exception">The <see cref="ServiceRegistrationException"/> instance.</param>
        /// <param name="endpoint">The extracted endpoint, or null if not found.</param>
        /// <returns>true if the endpoint was extracted; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exception"/> is null.</exception>
        public static bool TryExtractEndpoint(this ServiceRegistrationException exception, out string? endpoint)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (string.IsNullOrEmpty(exception.ServiceEndpoint))
            {
                endpoint = null;
                return false;
            }

            endpoint = exception.ServiceEndpoint;
            return true;
        }

        /// <summary>
        /// Combines multiple <see cref="ServiceRegistrationException"/> instances into a single exception.
        /// </summary>
        /// <param name="exceptions">The sequence of <see cref="ServiceRegistrationException"/> instances.</param>
        /// <returns>A new <see cref="AggregateException"/> containing all the original exceptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="exceptions"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="exceptions"/> is empty.</exception>
        public static AggregateException CombineExceptions(IEnumerable<ServiceRegistrationException> exceptions)
        {
            ArgumentNullException.ThrowIfNull(exceptions);
            if (!exceptions.Any())
            {
                throw new ArgumentException("At least one exception is required.", nameof(exceptions));
            }

            return new AggregateException(exceptions);
        }
    }
}
