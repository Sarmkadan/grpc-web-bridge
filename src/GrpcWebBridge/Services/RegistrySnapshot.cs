#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace GrpcWebBridge.Services
{
    /// <summary>
    /// Registry snapshot DTO
    /// </summary>
    public sealed class RegistrySnapshot
    {
        /// <summary>
        /// Total number of services in the registry.
        /// </summary>
        public int TotalServiceCount { get; set; }

        /// <summary>
        /// Timestamps of when each service was registered.
        /// </summary>
        public Dictionary<string, DateTime> ServiceRegistrationTimestamps { get; set; } = new();

        /// <summary>
        /// Converts the snapshot to a JSON string.
        /// </summary>
        /// <returns>A JSON representation of the snapshot.</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        public override string ToString()
        {
            return $"RegistrySnapshot {{ TotalServiceCount = {TotalServiceCount}, ServiceRegistrationTimestamps = {ServiceRegistrationTimestamps} }}";
        }
    }
}
