#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics.CodeAnalysis;

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
        var optionsField = value.GetType().GetField("_options", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (optionsField?.GetValue(value) is HealthCheckOptions options)
        {
            ValidateCheckIntervalSeconds(options.CheckIntervalSeconds, problems);
            ValidateCheckTimeoutMs(options.CheckTimeoutMs, problems);
            ValidateInitialDelaySeconds(options.InitialDelaySeconds, problems);
        }

        // Validate statistics - log any errors for debugging
        try
        {
            _ = value.GetStatistics();
        }
        catch (Exception ex)
        {
            problems.Add($"GetStatistics() method failed: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the health check worker configuration and state are valid.
    /// </summary>
    /// <param name="value">The health check worker to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Null is validated by ArgumentNullException.ThrowIfNull")]
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
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Null is validated by ArgumentNullException.ThrowIfNull")]
    public static void EnsureValid(this HealthCheckWorker? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"HealthCheckWorker configuration or state is invalid. Problems:{Environment.NewLine}" +
                string.Join(Environment.NewLine, problems),
                nameof(value));
        }
    }

    /// <summary>
    /// Validates that CheckIntervalSeconds is within acceptable range.
    /// </summary>
    /// <param name="value">The check interval in seconds.</param>
    /// <param name="problems">List to accumulate validation problems.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="problems"/> is null.</exception>
    private static void ValidateCheckIntervalSeconds(int value, [DisallowNull] List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);

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

    /// <summary>
    /// Validates that CheckTimeoutMs is within acceptable range.
    /// </summary>
    /// <param name="value">The check timeout in milliseconds.</param>
    /// <param name="problems">List to accumulate validation problems.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="problems"/> is null.</exception>
    private static void ValidateCheckTimeoutMs(int value, [DisallowNull] List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);

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

    /// <summary>
    /// Validates that InitialDelaySeconds is within acceptable range.
    /// </summary>
    /// <param name="value">The initial delay in seconds.</param>
    /// <param name="problems">List to accumulate validation problems.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="problems"/> is null.</exception>
    private static void ValidateInitialDelaySeconds(int value, [DisallowNull] List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(problems);

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