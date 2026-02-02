#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace GrpcWebBridge.BackgroundWorkers;

/// <summary>
/// Provides validation helpers for <see cref="HealthCheckWorker"/> instances.
/// Validates configuration, timing parameters, and operational state.
/// </summary>
public static class HealthCheckWorkerValidation
{
    private const int MinimumCheckIntervalSeconds = 5;
    private const int MinimumTimeoutMs = 100;
    private const int MinimumInitialDelaySeconds = 0;
    private const int MaximumCheckIntervalSeconds = 300;
    private const int MaximumTimeoutMs = 30000;
    private const int MaximumInitialDelaySeconds = 300;

    /// <summary>
    /// Validates the health check worker configuration and state.
    /// </summary>
    /// <param name="value">The health check worker to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthCheckWorker? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate timing configuration from the worker's options
        if (value.GetType().GetProperty("Options")?.GetValue(value) is HealthCheckOptions options)
        {
            ValidateCheckIntervalSeconds(options.CheckIntervalSeconds, problems);
            ValidateCheckTimeoutMs(options.CheckTimeoutMs, problems);
            ValidateInitialDelaySeconds(options.InitialDelaySeconds, problems);
        }

        // Validate statistics
        try
        {
            _ = value.GetStatistics();
        }
        catch
        {
            problems.Add("GetStatistics() method failed or threw an exception");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the health check worker configuration and state are valid.
    /// </summary>
    /// <param name="value">The health check worker to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this HealthCheckWorker? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the health check worker configuration and state are valid.
    /// </summary>
    /// <param name="value">The health check worker to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the worker is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this HealthCheckWorker? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                "HealthCheckWorker configuration or state is invalid. Problems:\n" +
                string.Join("\n", problems),
                nameof(value));
        }
    }

    private static void ValidateCheckIntervalSeconds(int value, List<string> problems)
    {
        if (value <= 0)
        {
            problems.Add(
                $"CheckIntervalSeconds must be positive, but was {value}.");
        }
        else if (value < MinimumCheckIntervalSeconds)
        {
            problems.Add(
                $"CheckIntervalSeconds must be at least {MinimumCheckIntervalSeconds} seconds, but was {value}.");
        }
        else if (value > MaximumCheckIntervalSeconds)
        {
            problems.Add(
                $"CheckIntervalSeconds must not exceed {MaximumCheckIntervalSeconds} seconds, but was {value}.");
        }
    }

    private static void ValidateCheckTimeoutMs(int value, List<string> problems)
    {
        if (value <= 0)
        {
            problems.Add(
                $"CheckTimeoutMs must be positive, but was {value}.");
        }
        else if (value < MinimumTimeoutMs)
        {
            problems.Add(
                $"CheckTimeoutMs must be at least {MinimumTimeoutMs} milliseconds, but was {value}.");
        }
        else if (value > MaximumTimeoutMs)
        {
            problems.Add(
                $"CheckTimeoutMs must not exceed {MaximumTimeoutMs} milliseconds, but was {value}.");
        }
    }

    private static void ValidateInitialDelaySeconds(int value, List<string> problems)
    {
        if (value < MinimumInitialDelaySeconds)
        {
            problems.Add(
                $"InitialDelaySeconds cannot be negative, but was {value}.");
        }
        else if (value > MaximumInitialDelaySeconds)
        {
            problems.Add(
                $"InitialDelaySeconds must not exceed {MaximumInitialDelaySeconds} seconds, but was {value}.");
        }
    }
}