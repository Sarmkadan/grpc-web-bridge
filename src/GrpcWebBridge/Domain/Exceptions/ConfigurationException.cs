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
    /// <summary>
    /// Gets or sets the configuration key that caused the exception.
    /// </summary>
    public string? ConfigurationKey { get; set; }

    /// <summary>
    /// Gets or sets the configuration value that caused the exception.
    /// </summary>
    public string? ConfigurationValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class.
    /// </summary>
    public ConfigurationException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ConfigurationException(string message) : base(message, "CONFIGURATION_ERROR")
    {
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ConfigurationException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = "CONFIGURATION_ERROR";
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified configuration key and error message.
    /// </summary>
    /// <param name="configurationKey">The configuration key that caused the exception.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ConfigurationException(string configurationKey, string message)
        : base($"Configuration '{configurationKey}' error: {message}", "CONFIG_INVALID")
    {
        ConfigurationKey = configurationKey;
        GrpcStatus = GrpcStatusCode.InvalidArgument;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a specified configuration key, configuration value, and error message.
    /// </summary>
    /// <param name="configurationKey">The configuration key that caused the exception.</param>
    /// <param name="configurationValue">The configuration value that caused the exception.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
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