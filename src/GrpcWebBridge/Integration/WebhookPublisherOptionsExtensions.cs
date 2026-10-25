#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace GrpcWebBridge.Integration;

/// <summary>
/// Validation and cloning helpers for <see cref="WebhookPublisherOptions"/>.
/// </summary>
public static class WebhookPublisherOptionsExtensions
{
    /// <summary>
    /// Validates the options and throws if any setting is out of range.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <see cref="WebhookPublisherOptions.MaxRetries"/> or
    /// <see cref="WebhookPublisherOptions.FailureThresholdForDisable"/> is negative.
    /// </exception>
    public static void Validate(this WebhookPublisherOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.MaxRetries),
                options.MaxRetries,
                "Maximum webhook retries cannot be negative.");
        }

        if (options.FailureThresholdForDisable < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options.FailureThresholdForDisable),
                options.FailureThresholdForDisable,
                "Failure threshold for disabling a subscription cannot be negative.");
        }
    }

    /// <summary>
    /// Creates a detached copy of the options. Changes to the copy do not affect the original.
    /// </summary>
    /// <param name="options">The options to copy.</param>
    /// <returns>A new <see cref="WebhookPublisherOptions"/> with the same settings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
    public static WebhookPublisherOptions Clone(this WebhookPublisherOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new WebhookPublisherOptions
        {
            MaxRetries = options.MaxRetries,
            FailureThresholdForDisable = options.FailureThresholdForDisable
        };
    }
}
