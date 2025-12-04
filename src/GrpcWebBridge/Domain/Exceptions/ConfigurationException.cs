#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Exception thrown when configuration validation fails
/// </summary>
public class ConfigurationException : GrpcWebBridgeException
{
    public string? ConfigurationKey { get; set; }
    public string? ConfigurationValue { get; set; }

    public ConfigurationException() : base() { }

    public ConfigurationException(string message) : base(message, "CONFIGURATION_ERROR")
    {
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    public ConfigurationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "CONFIGURATION_ERROR";
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    public ConfigurationException(string configurationKey, string message)
        : base($"Configuration '{configurationKey}' error: {message}", "CONFIG_INVALID")
    {
        ConfigurationKey = configurationKey;
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    public ConfigurationException(string configurationKey, string configurationValue, string message)
        : base($"Configuration '{configurationKey}' with value '{configurationValue}' error: {message}", "CONFIG_INVALID")
    {
        ConfigurationKey = configurationKey;
        ConfigurationValue = configurationValue;
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    public override string ToString()
    {
        var result = base.ToString();
        if (!string.IsNullOrEmpty(ConfigurationKey))
            result += $" | ConfigKey: {ConfigurationKey}";

        if (!string.IsNullOrEmpty(ConfigurationValue))
            result += $" | ConfigValue: {ConfigurationValue}";

        return result;
    }
}
