#nullable enable
using System;
using System.Collections.Generic;

namespace GrpcWebBridge.Services
{
    /// <summary>
    /// Registry snapshot DTO
    /// </summary>
    public sealed class RegistrySnapshot
    {
        public int TotalServiceCount { get; set; }
        public Dictionary<string, DateTime> ServiceRegistrationTimestamps { get; set; } = new();
    }
}
