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
        public int TotalServiceCount { get; set; }
        public Dictionary<string, DateTime> ServiceRegistrationTimestamps { get; set; } = new();

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
