#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.BackgroundWorkers;

/// <summary>
/// Provides extension helpers for <see cref="HealthCheckOptions"/>: validation, cloning and timeout conversion.
/// </summary>
public static class HealthCheckOptionsExtensions
{
    private const int MinimumCheckIntervalSeconds = 5;
    private const int MinimumTimeoutMs = 100;
    private const int MinimumInitialDelaySeconds = 0;
    private const int MaximumCheckIntervalSeconds = 300;
    private const int MaximumTimeoutMs = 30000;
    private const int MaximumInitialDelaySeconds = 300;

    /// <summary>
    /// Validates the health check options against the accepted ranges.
    /// </summary>
    /// <param name="value">The health check options to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthCheckOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.CheckIntervalSeconds <= 0)
        {
            problems.Add($"CheckIntervalSeconds must be positive, but was {value.CheckIntervalSeconds}.");
        }
        else if (value.CheckIntervalSeconds < MinimumCheckIntervalSeconds)
        {
            problems.Add($"CheckIntervalSeconds must be at least {MinimumCheckIntervalSeconds} seconds, but was {value.CheckIntervalSeconds}.");
        }
        else if (value.CheckIntervalSeconds > MaximumCheckIntervalSeconds)
        {
            problems.Add($"CheckIntervalSeconds must not exceed {MaximumCheckIntervalSeconds} seconds, but was {value.CheckIntervalSeconds}.");
        }

        if (value.CheckTimeoutMs <= 0)
        {
            problems.Add($"CheckTimeoutMs must be positive, but was {value.CheckTimeoutMs}.");
        }
        else if (value.CheckTimeoutMs < MinimumTimeoutMs)
        {
            problems.Add($"CheckTimeoutMs must be at least {MinimumTimeoutMs} milliseconds, but was {value.CheckTimeoutMs}.");
        }
        else if (value.CheckTimeoutMs > MaximumTimeoutMs)
        {
            problems.Add($"CheckTimeoutMs must not exceed {MaximumTimeoutMs} milliseconds, but was {value.CheckTimeoutMs}.");
        }

        if (value.InitialDelaySeconds < MinimumInitialDelaySeconds)
        {
            problems.Add($"InitialDelaySeconds cannot be negative, but was {value.InitialDelaySeconds}.");
        }
        else if (value.InitialDelaySeconds > MaximumInitialDelaySeconds)
        {
            problems.Add($"InitialDelaySeconds must not exceed {MaximumInitialDelaySeconds} seconds, but was {value.InitialDelaySeconds}.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Creates a copy of the health check options with the same values.
    /// </summary>
    /// <param name="value">The health check options to clone.</param>
    /// <returns>A new <see cref="HealthCheckOptions"/> instance with copied values.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static HealthCheckOptions Clone(this HealthCheckOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new HealthCheckOptions
        {
            CheckIntervalSeconds = value.CheckIntervalSeconds,
            CheckTimeoutMs = value.CheckTimeoutMs,
            InitialDelaySeconds = value.InitialDelaySeconds
        };
    }

    /// <summary>
    /// Gets the per-check timeout as a <see cref="TimeSpan"/>, converted from <see cref="HealthCheckOptions.CheckTimeoutMs"/>.
    /// </summary>
    /// <param name="value">The health check options.</param>
    /// <returns>The check timeout as a <see cref="TimeSpan"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static TimeSpan GetCheckTimeout(this HealthCheckOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return TimeSpan.FromMilliseconds(value.CheckTimeoutMs);
    }
}
