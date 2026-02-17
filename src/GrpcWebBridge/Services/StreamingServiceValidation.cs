#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;
using GrpcWebBridge.Domain;

namespace GrpcWebBridge.Services;

/// <summary>
/// Provides validation helpers for <see cref="StreamingService"/> instances
/// </summary>
public static class StreamingServiceValidation
{
    /// <summary>
    /// Validates a StreamingService instance and returns a list of validation problems
    /// </summary>
    /// <param name="value">The StreamingService instance to validate.</param>
    /// <returns>An empty list if valid, or a list of human-readable problems if invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this StreamingService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate internal streams dictionary
        if (value.ActiveStreamCount < 0)
        {
            problems.Add("Active stream count cannot be negative");
        }

        // Validate that all streams are valid
        foreach (var streamId in value.GetAllStreamIds())
        {
            var stream = value.GetStream(streamId);
            if (stream is not null)
            {
                var streamProblems = stream.Validate();
                problems.AddRange(streamProblems);
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a StreamingService instance is valid
    /// </summary>
    /// <param name="value">The StreamingService instance to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(this StreamingService? value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures a StreamingService instance is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The StreamingService instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if value contains validation problems.</exception>
    public static void EnsureValid(this StreamingService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"StreamingService validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}

/// <summary>
/// Provides validation helpers for Stream instances
/// </summary>
public static class StreamValidation
{
    /// <summary>
    /// Validates a Stream instance and returns a list of validation problems
    /// </summary>
    /// <param name="value">The Stream instance to validate.</param>
    /// <returns>An empty list if valid, or a list of human-readable problems if invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this Stream? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate StreamId
        if (string.IsNullOrWhiteSpace(value.StreamId))
        {
            problems.Add("StreamId cannot be null or whitespace");
        }

        // Validate MethodType
        if (!Enum.IsDefined(typeof(MethodType), value.MethodType))
        {
            problems.Add($"MethodType has invalid value: {value.MethodType}");
        }

        // Validate State
        if (!Enum.IsDefined(typeof(StreamState), value.State))
        {
            problems.Add($"State has invalid value: {value.State}");
        }

        // Validate MessageCount
        if (value.MessageCount < 0)
        {
            problems.Add("MessageCount cannot be negative");
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            problems.Add("CreatedAt cannot be default(DateTime)");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(1))
        {
            problems.Add("CreatedAt cannot be in the future");
        }

        // Validate LastActivityTime
        if (value.LastActivityTime == default)
        {
            problems.Add("LastActivityTime cannot be default(DateTime)");
        }
        else if (value.LastActivityTime > DateTime.UtcNow.AddMinutes(1))
        {
            problems.Add("LastActivityTime cannot be in the future");
        }
        else if (value.LastActivityTime < value.CreatedAt)
        {
            problems.Add("LastActivityTime cannot be before CreatedAt");
        }

        // Validate FinalStatus
        if (value.FinalStatus.HasValue && !Enum.IsDefined(typeof(GrpcStatusCode), value.FinalStatus.Value))
        {
            problems.Add($"FinalStatus has invalid value: {value.FinalStatus.Value}");
        }

        // Validate FinalMessage
        if (value.FinalMessage is { Length: > 0 } && string.IsNullOrWhiteSpace(value.FinalMessage))
        {
            problems.Add("FinalMessage cannot be whitespace");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a Stream instance is valid
    /// </summary>
    /// <param name="value">The Stream instance to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(this Stream? value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures a Stream instance is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The Stream instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if value contains validation problems.</exception>
    public static void EnsureValid(this Stream? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Stream validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}

/// <summary>
/// Provides validation helpers for StreamStatistics instances
/// </summary>
public static class StreamStatisticsValidation
{
    /// <summary>
    /// Validates a StreamStatistics instance and returns a list of validation problems
    /// </summary>
    /// <param name="value">The StreamStatistics instance to validate.</param>
    /// <returns>An empty list if valid, or a list of human-readable problems if invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    public static IReadOnlyList<string> Validate(this StreamStatistics? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate StreamId
        if (string.IsNullOrWhiteSpace(value.StreamId))
        {
            problems.Add("StreamId cannot be null or whitespace");
        }

        // Validate MessageCount
        if (value.MessageCount < 0)
        {
            problems.Add("MessageCount cannot be negative");
        }

        // Validate QueuedMessageCount
        if (value.QueuedMessageCount < 0)
        {
            problems.Add("QueuedMessageCount cannot be negative");
        }

        // Validate State
        if (!Enum.IsDefined(typeof(StreamState), value.State))
        {
            problems.Add($"State has invalid value: {value.State}");
        }

        // Validate CreatedAt
        if (value.CreatedAt == default)
        {
            problems.Add("CreatedAt cannot be default(DateTime)");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(1))
        {
            problems.Add("CreatedAt cannot be in the future");
        }

        // Validate LastActivityTime
        if (value.LastActivityTime == default)
        {
            problems.Add("LastActivityTime cannot be default(DateTime)");
        }
        else if (value.LastActivityTime > DateTime.UtcNow.AddMinutes(1))
        {
            problems.Add("LastActivityTime cannot be in the future");
        }

        // Validate DurationSeconds
        if (value.DurationSeconds < 0)
        {
            problems.Add("DurationSeconds cannot be negative");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a StreamStatistics instance is valid
    /// </summary>
    /// <param name="value">The StreamStatistics instance to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValid(this StreamStatistics? value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures a StreamStatistics instance is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The StreamStatistics instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentException">Thrown if value contains validation problems.</exception>
    public static void EnsureValid(this StreamStatistics? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"StreamStatistics validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}
