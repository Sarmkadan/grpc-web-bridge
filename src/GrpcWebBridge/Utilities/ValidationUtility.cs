#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using System.Text.RegularExpressions;

namespace GrpcWebBridge.Utilities;

/// <summary>
/// Validation utilities for common request validation scenarios.
/// Provides methods for validating strings, emails, URLs, IPs, and other inputs.
/// </summary>
public static class ValidationUtility
{
    // Common regex patterns
    private static readonly Regex EmailRegex = new(
        @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex UrlRegex = new(
        @"^https?://[-A-Za-z0-9+&@#/%?=~_|!:,.;]*[-A-Za-z0-9+&@#/%=~_|]$",
        RegexOptions.Compiled);

    private static readonly Regex GrpcMethodNameRegex = new(
        @"^[a-zA-Z][a-zA-Z0-9_]*$",
        RegexOptions.Compiled);

    private static readonly Regex ServiceIdRegex = new(
        @"^[a-zA-Z0-9_\-\.]+$",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates that a string is not null or empty.
    /// Trims whitespace for comparison.
    /// </summary>
    public static (bool Valid, string? Error) ValidateNotEmpty(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return (false, $"{fieldName} cannot be null or empty");

        return (true, null);
    }

    /// <summary>
    /// Validates string length constraints.
    /// </summary>
    public static (bool Valid, string? Error) ValidateStringLength(
        string? value,
        string fieldName,
        int minLength = 0,
        int maxLength = int.MaxValue)
    {
        if (string.IsNullOrEmpty(value))
            return (false, $"{fieldName} cannot be null or empty");

        if (value.Length < minLength)
            return (false, $"{fieldName} must be at least {minLength} characters long");

        if (value.Length > maxLength)
            return (false, $"{fieldName} cannot exceed {maxLength} characters");

        return (true, null);
    }

    /// <summary>
    /// Validates an email address format.
    /// Uses regex pattern matching.
    /// </summary>
    public static (bool Valid, string? Error) ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return (false, "Email cannot be null or empty");

        if (!EmailRegex.IsMatch(email))
            return (false, "Email format is invalid");

        if (email.Length > 254) // RFC 5321
            return (false, "Email is too long");

        return (true, null);
    }

    /// <summary>
    /// Validates a URL format.
    /// </summary>
    public static (bool Valid, string? Error) ValidateUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return (false, "URL cannot be null or empty");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return (false, "URL format is invalid");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return (false, "URL must use HTTP or HTTPS scheme");

        return (true, null);
    }

    /// <summary>
    /// Validates an IP address (IPv4 or IPv6).
    /// </summary>
    public static (bool Valid, string? Error) ValidateIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return (false, "IP address cannot be null or empty");

        if (!IPAddress.TryParse(ipAddress, out _))
            return (false, "IP address format is invalid");

        return (true, null);
    }

    /// <summary>
    /// Validates a gRPC service ID format.
    /// Service IDs must contain only alphanumeric characters, underscores, hyphens, and dots.
    /// </summary>
    public static (bool Valid, string? Error) ValidateServiceId(string? serviceId)
    {
        if (string.IsNullOrWhiteSpace(serviceId))
            return (false, "Service ID cannot be null or empty");

        if (serviceId.Length > 255)
            return (false, "Service ID cannot exceed 255 characters");

        if (!ServiceIdRegex.IsMatch(serviceId))
            return (false, "Service ID contains invalid characters");

        return (true, null);
    }

    /// <summary>
    /// Validates a gRPC method name format.
    /// Method names must start with a letter and contain only alphanumeric characters and underscores.
    /// </summary>
    public static (bool Valid, string? Error) ValidateMethodName(string? methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
            return (false, "Method name cannot be null or empty");

        if (methodName.Length > 255)
            return (false, "Method name cannot exceed 255 characters");

        if (!GrpcMethodNameRegex.IsMatch(methodName))
            return (false, "Method name must start with a letter and contain only alphanumeric characters and underscores");

        return (true, null);
    }

    /// <summary>
    /// Validates an integer is within a range.
    /// </summary>
    public static (bool Valid, string? Error) ValidateRange(
        int value,
        string fieldName,
        int minValue = int.MinValue,
        int maxValue = int.MaxValue)
    {
        if (value < minValue)
            return (false, $"{fieldName} cannot be less than {minValue}");

        if (value > maxValue)
            return (false, $"{fieldName} cannot exceed {maxValue}");

        return (true, null);
    }

    /// <summary>
    /// Validates that an enumerable is not null or empty.
    /// </summary>
    public static (bool Valid, string? Error) ValidateNotEmpty<T>(
        IEnumerable<T>? collection,
        string fieldName)
    {
        if (collection is null || !collection.Any())
            return (false, $"{fieldName} cannot be null or empty");

        return (true, null);
    }

    /// <summary>
    /// Validates that a dictionary contains all required keys.
    /// </summary>
    public static (bool Valid, string? Error) ValidateRequiredKeys(
        Dictionary<string, object>? dict,
        string fieldName,
        params string[] requiredKeys)
    {
        if (dict is null)
            return (false, $"{fieldName} cannot be null");

        var missingKeys = requiredKeys.Where(k => !dict.ContainsKey(k)).ToList();
        if (missingKeys.Any())
            return (false, $"{fieldName} is missing required keys: {string.Join(", ", missingKeys)}");

        return (true, null);
    }

    /// <summary>
    /// Validates that a value matches a pattern.
    /// </summary>
    public static (bool Valid, string? Error) ValidatePattern(
        string? value,
        string fieldName,
        Regex pattern,
        string patternDescription = "")
    {
        if (string.IsNullOrEmpty(value))
            return (false, $"{fieldName} cannot be null or empty");

        if (!pattern.IsMatch(value))
        {
            var desc = string.IsNullOrEmpty(patternDescription) ? "specified pattern" : patternDescription;
            return (false, $"{fieldName} does not match {desc}");
        }

        return (true, null);
    }

    /// <summary>
    /// Validates a JWT token format (basic validation).
    /// Checks for three dot-separated Base64 segments.
    /// </summary>
    public static (bool Valid, string? Error) ValidateJwtFormat(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return (false, "JWT token cannot be null or empty");

        var parts = token.Split('.');
        if (parts.Length != 3)
            return (false, "JWT token must contain exactly three parts separated by dots");

        // Basic Base64 validation
        try
        {
            foreach (var part in parts)
            {
                var padding = (4 - (part.Length % 4)) % 4;
                var base64 = part.PadRight(part.Length + padding, '=');
                Convert.FromBase64String(base64);
            }
        }
        catch
        {
            return (false, "JWT token contains invalid Base64 encoding");
        }

        return (true, null);
    }

    /// <summary>
    /// Sanitizes user input to prevent injection attacks.
    /// Escapes HTML special characters.
    /// </summary>
    public static string SanitizeInput(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;");
    }
}
