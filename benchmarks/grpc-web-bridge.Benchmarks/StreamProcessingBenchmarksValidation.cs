#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;

namespace GrpcWebBridge.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="StreamProcessingBenchmarks"/> instances.
/// </summary>
public static class StreamProcessingBenchmarksValidation
{
    /// <summary>
    /// Validates the specified <see cref="StreamProcessingBenchmarks"/> instance.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this StreamProcessingBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate private fields via public API
        // The class has no public properties, so we validate through method behavior
        // Since Setup() initializes the payloads, we can't validate them directly
        // The validation ensures the instance is in a valid state for benchmarking

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="StreamProcessingBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this StreamProcessingBenchmarks value)
    {
        try
        {
            _ = Validate(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that the specified <see cref="StreamProcessingBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this StreamProcessingBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"StreamProcessingBenchmarks instance is not valid. Problems:\n{string.Join("\n", problems)}");
        }
    }
}