#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Validation and cloning helpers for <see cref="MaxBodySizeOptions"/>.
/// </summary>
public static class MaxBodySizeOptionsExtensions
{
    /// <summary>
    /// Validates the options and throws if the configured limit is not usable.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="MaxBodySizeOptions.MaxRequestBodySizeBytes"/> is zero or negative.
    /// </exception>
    public static void Validate(this MaxBodySizeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxRequestBodySizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaxRequestBodySizeBytes),
                options.MaxRequestBodySizeBytes,
                "Maximum request body size must be a positive number of bytes.");
        }
    }

    /// <summary>
    /// Creates a detached copy of the options. Changes to the copy do not affect the original.
    /// </summary>
    /// <param name="options">The options to copy.</param>
    /// <returns>A new <see cref="MaxBodySizeOptions"/> with the same settings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public static MaxBodySizeOptions Clone(this MaxBodySizeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new MaxBodySizeOptions
        {
            MaxRequestBodySizeBytes = options.MaxRequestBodySizeBytes
        };
    }
}
