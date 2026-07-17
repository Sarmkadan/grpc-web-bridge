#nullable enable

using System.Globalization;

namespace GrpcWebBridge.Integration;

/// <summary>
/// Provides validation helpers for <see cref="ServiceInstance"/> instances.
/// Validates all public properties for null values, empty strings, out-of-range values,
/// and default/invalid dates based on the semantic meaning of each property.
/// </summary>
public static class ServiceDiscoveryClientValidation
{
    /// <summary>
    /// Validates the specified <see cref="ServiceInstance"/> instance.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ServiceInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Id
        if (string.IsNullOrWhiteSpace(value.Id))
        {
            problems.Add("ServiceInstance.Id cannot be null, empty, or whitespace.");
        }

        // Validate Name
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("ServiceInstance.Name cannot be null, empty, or whitespace.");
        }

        // Validate Host
        if (string.IsNullOrWhiteSpace(value.Host))
        {
            problems.Add("ServiceInstance.Host cannot be null, empty, or whitespace.");
        }

        // Validate Port (should be a valid port number: 0-65535)
        if (value.Port < 0 || value.Port > 65535)
        {
            problems.Add($"ServiceInstance.Port must be between 0 and 65535, but was {value.Port}.");
        }

        // Validate Status
        if (string.IsNullOrWhiteSpace(value.Status))
        {
            problems.Add("ServiceInstance.Status cannot be null, empty, or whitespace.");
        }
        else if (!IsValidStatus(value.Status))
        {
            problems.Add($"ServiceInstance.Status must be a valid status (e.g., 'UP', 'DOWN', 'MAINTENANCE', 'OUT_OF_SERVICE', 'UNKNOWN'), but was '{value.Status}'.");
        }

        // Validate Metadata (optional but must be valid if provided)
        if (value.Metadata is not null)
        {
            foreach (var kvp in value.Metadata)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    problems.Add("ServiceInstance.Metadata contains a key that is null, empty, or whitespace.");
                    break;
                }

                if (string.IsNullOrWhiteSpace(kvp.Value))
                {
                    problems.Add("ServiceInstance.Metadata contains a value that is null, empty, or whitespace.");
                    break;
                }
            }
        }

        // Validate RegisteredAt (should not be default DateTime)
        if (value.RegisteredAt == default)
        {
            problems.Add("ServiceInstance.RegisteredAt cannot be default(DateTime).");
        }

        // Validate LastHeartbeat (should be null or a valid past date)
        if (value.LastHeartbeat.HasValue)
        {
            if (value.LastHeartbeat.Value == default)
            {
                problems.Add("ServiceInstance.LastHeartbeat cannot be default(DateTime) when set.");
            }
            else if (value.LastHeartbeat.Value > DateTime.UtcNow.AddMinutes(5))
            {
                problems.Add($"ServiceInstance.LastHeartbeat cannot be in the future (was {value.LastHeartbeat.Value:yyyy-MM-dd HH:mm:ss}).");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ServiceInstance"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ServiceInstance value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ServiceInstance"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this ServiceInstance value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ServiceInstance is not valid. Problems:\n- {string.Join("\n- ", problems)}",
                nameof(value));
        }
    }

    /// <summary>
    /// Checks if the given status string is a valid service status.
    /// </summary>
    /// <param name="status">The status to validate.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="status"/> is null.</exception>
    private static bool IsValidStatus(string status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var normalized = status.Trim().ToUpperInvariant();
        return normalized is "UP" or "DOWN" or "MAINTENANCE" or "OUT_OF_SERVICE" or "UNKNOWN";
    }
}