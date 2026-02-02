using System;
using System.Collections.Generic;
using System.Linq;

namespace GrpcWebBridge.Domain.Models
{
    /// <summary>
    /// Extension methods for <see cref="GrpcServiceDescriptor"/> operations.
    /// </summary>
    public static class GrpcServiceDescriptorExtensions
    {
        /// <summary>
        /// Gets a display-friendly name combining package and service name.
        /// </summary>
        /// <param name="descriptor">The service descriptor</param>
        /// <returns>Formatted display name</returns>
        /// <exception cref="ArgumentNullException">When descriptor is null</exception>
        public static string GetDisplayName(this GrpcServiceDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            return $"{descriptor.PackageName}.{descriptor.Name}";
        }

        /// <summary>
        /// Determines if the service endpoint uses secure transport.
        /// </summary>
        /// <param name="descriptor">The service descriptor</param>
        /// <returns>True if TLS is enabled and port indicates HTTPS</returns>
        /// <exception cref="ArgumentNullException">When descriptor is null</exception>
        public static bool IsSecureEndpoint(this GrpcServiceDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            return descriptor.UseTls && 
                   (descriptor.Port == 443 || 
                    descriptor.Port == 8443);
        }

        /// <summary>
        /// Gets all streaming methods (client/server) in the service.
        /// </summary>
        /// <param name="descriptor">The service descriptor</param>
        /// <returns>ReadOnly collection of streaming methods</returns>
        /// <exception cref="ArgumentNullException">When descriptor is null</exception>
        public static IEnumerable<MethodDescriptor> GetStreamingMethods(this GrpcServiceDescriptor descriptor)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            return descriptor.Methods.Where(m => m.IsClientStreaming || m.IsServerStreaming);
        }

        /// <summary>
        /// Finds a method by name with case-insensitive comparison.
        /// </summary>
        /// <param name="descriptor">The service descriptor</param>
        /// <param name="methodName">Name of the method to find</param>
        /// <returns>Matching method descriptor or null</returns>
        /// <exception cref="ArgumentNullException">When arguments are null</exception>
        public static MethodDescriptor? GetMethodByName(this GrpcServiceDescriptor descriptor, string methodName)
        {
            ArgumentNullException.ThrowIfNull(descriptor);
            ArgumentException.ThrowIfNullOrEmpty(methodName);
            
            return descriptor.Methods
                .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
