#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace GrpcWebBridge.Domain.Exceptions;

/// <summary>
/// Extension methods for <see cref="ConfigurationException"/> to provide fluent validation and common operations
/// </summary>
public static class ConfigurationExceptionExtensions
{
    /// <summary>
    /// Creates a new ConfigurationException with the specified message, preserving existing configuration properties
    /// </summary>
    /// <param name="exception">The source exception</param>
    /// <param name="message">The new error message</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException WithMessage(this ConfigurationException exception, string message)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var newException = new ConfigurationException(message)
        {
            ConfigurationKey = exception.ConfigurationKey,
            ConfigurationValue = exception.ConfigurationValue
        };

        return newException;
    }

    /// <summary>
    /// Creates a new ConfigurationException with the specified configuration key, preserving existing message and value
    /// </summary>
    /// <param name="exception">The source exception</param>
    /// <param name="configurationKey">The configuration key to set</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException WithKey(this ConfigurationException exception, string configurationKey)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var newException = new ConfigurationException(configurationKey, exception.ConfigurationValue ?? string.Empty, GetInnerMessage(exception))
        {
            HelpLink = exception.HelpLink
        };

        return newException;
    }

    /// <summary>
    /// Creates a new ConfigurationException with the specified configuration value, preserving existing message and key
    /// </summary>
    /// <param name="exception">The source exception</param>
    /// <param name="configurationValue">The configuration value to set</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException WithValue(this ConfigurationException exception, string configurationValue)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var newException = new ConfigurationException(exception.ConfigurationKey ?? string.Empty, configurationValue, GetInnerMessage(exception))
        {
            HelpLink = exception.HelpLink
        };

        return newException;
    }

    /// <summary>
    /// Creates a new ConfigurationException with both key and value, preserving the original message
    /// </summary>
    /// <param name="exception">The source exception</param>
    /// <param name="configurationKey">The configuration key</param>
    /// <param name="configurationValue">The configuration value</param>
    /// <returns>A new ConfigurationException instance</returns>
    public static ConfigurationException WithKeyValue(this ConfigurationException exception, string configurationKey, string configurationValue)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var newException = new ConfigurationException(configurationKey, configurationValue, GetInnerMessage(exception))
        {
            HelpLink = exception.HelpLink
        };

        return newException;
    }

    /// <summary>
    /// Checks if the exception contains a configuration key that matches the specified key (case-insensitive)
    /// </summary>
    /// <param name="exception">The source exception</param>
    /// <param name="key">The key to check for</param>
    /// <returns>True if the key matches or is null/empty</returns>
    public static bool HasKey(this ConfigurationException exception, string key)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        if (string.IsNullOrEmpty(key))
            return true;

        return string.Equals(exception.ConfigurationKey, key, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a formatted error message that includes all configuration details
    /// </summary>
    /// <param name="exception">The source exception</param>
    /// <returns>A formatted error message string</returns>
    public static string GetFormattedMessage(this ConfigurationException exception)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        var builder = new StringBuilder();
        builder.AppendLine(exception.Message);

        if (!string.IsNullOrEmpty(exception.ConfigurationKey))
        {
            builder.Append("  Key: ");
            builder.AppendLine(exception.ConfigurationKey);
        }

        if (!string.IsNullOrEmpty(exception.ConfigurationValue))
        {
            builder.Append("  Value: ");
            builder.AppendLine(exception.ConfigurationValue);
        }

        return builder.ToString().Trim();
    }

    /// <summary>
    /// Safely extracts the inner exception message if available, otherwise returns the outer message
    /// </summary>
    /// <param name="exception">The source exception</param>
    /// <returns>The inner exception message or outer message</returns>
    private static string GetInnerMessage(ConfigurationException exception)
    {
        if (exception == null)
            return string.Empty;

        if (exception.InnerException != null)
            return exception.InnerException.Message;

        return exception.Message;
    }
}