#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace GrpcWebBridge.Domain.Models;

/// <summary>
/// Provides validation helpers for <see cref="GrpcService"/> instances
/// </summary>
public static class GrpcServiceValidation
{
    /// <summary>
    /// Validates a <see cref="GrpcService"/> instance and returns a list of validation problems.
    /// </summary>
    /// <param name="value">The service to validate</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable problems</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this GrpcService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate required string properties
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            problems.Add("Service Id cannot be null or whitespace");
        }
        else if (value.Id.Length != 32)
        {
            problems.Add("Service Id must be a 32-character GUID");
        }

        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("Service Name cannot be null or whitespace");
        }
        else if (value.Name.Length > 100)
        {
            problems.Add("Service Name cannot exceed 100 characters");
        }

        if (string.IsNullOrWhiteSpace(value.PackageName))
        {
            problems.Add("Service PackageName cannot be null or whitespace");
        }
        else if (value.PackageName.Length > 100)
        {
            problems.Add("Service PackageName cannot exceed 100 characters");
        }
        else if (!IsValidPackageName(value.PackageName))
        {
            problems.Add("Service PackageName must be a valid .NET package name (alphanumeric with dots)");
        }

        if (string.IsNullOrWhiteSpace(value.FullName))
        {
            problems.Add("Service FullName cannot be null or whitespace");
        }
        else if (value.FullName.Length > 200)
        {
            problems.Add("Service FullName cannot exceed 200 characters");
        }

        if (string.IsNullOrWhiteSpace(value.Endpoint))
        {
            problems.Add("Service Endpoint cannot be null or whitespace");
        }
        else if (value.Endpoint.Length > 100)
        {
            problems.Add("Service Endpoint cannot exceed 100 characters");
        }
        else if (!IsValidEndpoint(value.Endpoint))
        {
            problems.Add("Service Endpoint must be a valid hostname or IP address");
        }

        // Validate numeric properties
        if (value.Port <= 0 || value.Port > 65535)
        {
            problems.Add("Service Port must be between 1 and 65535");
        }

        // Validate enum values
        if (value.Status == ServiceStatus.Unknown)
        {
            problems.Add("Service Status cannot be Unknown");
        }

        // Validate date properties
        if (value.CreatedAt == default)
        {
            problems.Add("Service CreatedAt cannot be default DateTime");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("Service CreatedAt cannot be in the future");
        }

        if (value.UpdatedAt.HasValue)
        {
            if (value.UpdatedAt.Value == default)
            {
                problems.Add("Service UpdatedAt cannot be default DateTime");
            }
            else if (value.UpdatedAt.Value > DateTime.UtcNow.AddMinutes(5))
            {
                problems.Add("Service UpdatedAt cannot be in the future");
            }
            else if (value.UpdatedAt.Value < value.CreatedAt)
            {
                problems.Add("Service UpdatedAt cannot be earlier than CreatedAt");
            }
        }

        // Validate collections
        if (value.Metadata is null)
        {
            problems.Add("Service Metadata dictionary cannot be null");
        }
        else if (value.Metadata.Count > 100)
        {
            problems.Add("Service Metadata cannot contain more than 100 entries");
        }
        else
        {
            foreach (var kvp in value.Metadata)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    problems.Add("Service Metadata contains an entry with null or empty key");
                    break;
                }

                if (kvp.Key.Length > 100)
                {
                    problems.Add("Service Metadata key cannot exceed 100 characters");
                    break;
                }

                if (kvp.Value is not null && kvp.Value.Length > 1000)
                {
                    problems.Add("Service Metadata value cannot exceed 1000 characters");
                    break;
                }
            }
        }

        // Validate methods collection
        if (value.Methods is null)
        {
            problems.Add("Service Methods collection cannot be null");
        }
        else if (value.Methods.Count == 0)
        {
            problems.Add("Service must have at least one method");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="GrpcService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service to check</param>
    /// <returns>True if valid, otherwise false</returns>
    public static bool IsValid(this GrpcService? value) => value is not null && Validate(value).Count == 0;

    /// <summary>
    /// Ensures that a <see cref="GrpcService"/> instance is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The service to validate</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid, containing a list of problems</exception>
    public static void EnsureValid(this GrpcService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"GrpcService is invalid:{Environment.NewLine}- {
                string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }

    /// <summary>
    /// Validates that a string is a valid .NET package name.
    /// </summary>
    /// <param name="packageName">The package name to validate</param>
    /// <returns>True if valid, otherwise false</returns>
    private static bool IsValidPackageName(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;

        // .NET package names are alphanumeric with dots and dashes
        if (!packageName.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-'))
            return false;

        // Cannot start or end with dot or dash
        if (packageName.StartsWith('.') || packageName.StartsWith('-') || packageName.EndsWith('.') || packageName.EndsWith('-'))
            return false;

        // Cannot have consecutive dots or dashes
        if (packageName.Contains("..") || packageName.Contains("--"))
            return false;

        return true;
    }

    /// <summary>
    /// Validates that a string is a valid hostname or IP address.
    /// </summary>
    /// <param name="endpoint">The endpoint to validate</param>
    /// <returns>True if valid, otherwise false</returns>
    private static bool IsValidEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            return false;

        // Check if it's a valid IPv4 address
        if (IsValidIpAddress(endpoint))
            return true;

        // Check if it's a valid hostname
        return IsValidHostname(endpoint);
    }

    /// <summary>
    /// Validates that a string is a valid IPv4 address.
    /// </summary>
    /// <param name="ip">The IP address to validate</param>
    /// <returns>True if valid, otherwise false</returns>
    private static bool IsValidIpAddress(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return false;

        var parts = ip.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
            return false;

        return parts.All(part => int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num) && num >= 0 && num <= 255);
    }

    /// <summary>
    /// Validates that a string is a valid hostname.
    /// </summary>
    /// <param name="hostname">The hostname to validate</param>
    /// <returns>True if valid, otherwise false</returns>
    private static bool IsValidHostname(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname) || hostname.Length > 253)
            return false;

        // Hostname can contain letters, digits, hyphens, and dots
        // Cannot start or end with hyphen or dot
        // Each label (between dots) must be 1-63 characters
        var labels = hostname.Split('.');
        if (labels.Length == 0 || labels.Any(string.IsNullOrWhiteSpace))
            return false;

        return labels.All(label => label.Length > 0 && label.Length <= 63 &&
            !label.StartsWith('-') && !label.EndsWith('-') &&
            label.All(c => char.IsLetterOrDigit(c) || c == '-'));
    }
}