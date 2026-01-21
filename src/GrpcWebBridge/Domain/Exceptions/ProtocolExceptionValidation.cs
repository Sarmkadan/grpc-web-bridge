using System;
using System.Collections.Generic;
using System.Globalization;

namespace GrpcWebBridge.Domain.Exceptions;

public static class ProtocolExceptionValidation
{
    /// <summary>
    /// Validates the specified <see cref="ProtocolException"/> instance.
    /// </summary>
    /// <param name="value">The protocol exception to validate.</param>
    /// <returns>A read-only list of validation errors; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ProtocolException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (string.IsNullOrEmpty(value.SourceFormat))
        {
            errors.Add($"SourceFormat is required.");
        }

        if (string.IsNullOrEmpty(value.TargetFormat))
        {
            errors.Add($"TargetFormat is required.");
        }

        if (value.RequestId is not null && string.IsNullOrEmpty(value.RequestId))
        {
            errors.Add($"RequestId must be null or a non-empty string.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ProtocolException"/> instance is valid.
    /// </summary>
    /// <param name="value">The protocol exception to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ProtocolException value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ProtocolException"/> instance is valid.
    /// </summary>
    /// <param name="value">The protocol exception to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing a list of validation errors.</exception>
    public static void EnsureValid(this ProtocolException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"ProtocolException is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}