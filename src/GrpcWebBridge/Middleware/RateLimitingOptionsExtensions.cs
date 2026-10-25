#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Middleware;

/// <summary>
/// Extension methods for <see cref="RateLimitingOptions"/>.
/// </summary>
public static class RateLimitingOptionsExtensions
{
    /// <summary>
    /// Validates that the rate-limiting rates are positive and that the global limit
    /// is not stricter than the per-client limit.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="RateLimitingOptions.RequestsPerSecond"/> or
    /// <see cref="RateLimitingOptions.GlobalRequestsPerSecond"/> is not positive, or when
    /// <see cref="RateLimitingOptions.GlobalRequestsPerSecond"/> is less than
    /// <see cref="RateLimitingOptions.RequestsPerSecond"/>.
    /// </exception>
    public static void Validate(this RateLimitingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.RequestsPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.RequestsPerSecond,
                "RequestsPerSecond must be positive.");
        }

        if (options.GlobalRequestsPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.GlobalRequestsPerSecond,
                "GlobalRequestsPerSecond must be positive.");
        }

        if (options.GlobalRequestsPerSecond < options.RequestsPerSecond)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.GlobalRequestsPerSecond,
                "GlobalRequestsPerSecond must be greater than or equal to RequestsPerSecond.");
        }
    }

    /// <summary>
    /// Creates a shallow copy of the rate-limiting options. The <see cref="RateLimitingOptions.ExemptPaths"/>
    /// list is copied so that changes to the clone do not affect the original.
    /// </summary>
    /// <param name="options">The options to clone.</param>
    /// <returns>A new <see cref="RateLimitingOptions"/> instance with the same values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public static RateLimitingOptions Clone(this RateLimitingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new RateLimitingOptions
        {
            RequestsPerSecond = options.RequestsPerSecond,
            WindowSizeSeconds = options.WindowSizeSeconds,
            RetryAfterSeconds = options.RetryAfterSeconds,
            EnableGlobalLimit = options.EnableGlobalLimit,
            GlobalRequestsPerSecond = options.GlobalRequestsPerSecond,
            ExemptPaths = options.ExemptPaths.ToArray()
        };
    }
}
