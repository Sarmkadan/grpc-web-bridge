#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

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

        // Validate that the instance has been properly initialized
        // The Setup() method should have been called to initialize payloads
        // We can't directly access private fields, so we validate through behavior

        // Check if payloads are initialized by attempting to use them
        try
        {
            // This will throw if Setup() wasn't called or if payloads are null
            _ = value.ReadStreamToEnd_1KB();
            _ = value.ReadStreamToEnd_64KB();
            _ = value.ReadStreamToEnd_1MB();
            _ = value.CopyStreamChunked_1KB();
            _ = value.CopyStreamChunked_64KB();
            _ = value.CopyStreamChunked_1MB();
            _ = value.StreamToBase64_1KB();
        }
        catch (NullReferenceException)
        {
            problems.Add("StreamProcessingBenchmarks instance has uninitialized payloads. Ensure Setup() was called before benchmarking.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException and not ArgumentException)
        {
            problems.Add($"StreamProcessingBenchmarks instance failed initialization check: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="StreamProcessingBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this StreamProcessingBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        return problems.Count == 0;
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