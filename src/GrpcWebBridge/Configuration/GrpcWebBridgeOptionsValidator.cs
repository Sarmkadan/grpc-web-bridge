#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain.Models;
using Microsoft.Extensions.Options;

namespace GrpcWebBridge.Configuration;

/// <summary>
/// Validates <see cref="GrpcWebBridgeOptions"/> when the host starts (see <c>ValidateOnStart</c>).
/// Every failing property is reported in a single result so one startup attempt shows all problems.
/// </summary>
public sealed class GrpcWebBridgeOptionsValidator : IValidateOptions<GrpcWebBridgeOptions>
{
    private const string ConfigurationPath = nameof(GrpcWebBridgeOptions.Configuration);

    /// <summary>
    /// Validates the bridge options and collects all failures.
    /// </summary>
    /// <param name="name">The options instance name (unused; the bridge registers a single unnamed instance).</param>
    /// <param name="options">The options to validate.</param>
    /// <returns>A successful result, or a failed result listing every invalid property.</returns>
    public ValidateOptionsResult Validate(string? name, GrpcWebBridgeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var config = options.Configuration;
        var failures = new List<string>();

        if (config.MaxMessageSize <= 0)
        {
            failures.Add(Failure(nameof(BridgeConfiguration.MaxMessageSize), $"must be greater than 0 (was {config.MaxMessageSize})"));
        }

        ValidateOrigins(config, failures);

        if (config.StreamIdleTimeoutSeconds <= 0)
        {
            failures.Add(Failure(nameof(BridgeConfiguration.StreamIdleTimeoutSeconds), $"must be greater than 0 (was {config.StreamIdleTimeoutSeconds})"));
        }

        if (config.StreamHeartbeatIntervalSeconds <= 0)
        {
            failures.Add(Failure(nameof(BridgeConfiguration.StreamHeartbeatIntervalSeconds), $"must be greater than 0 (was {config.StreamHeartbeatIntervalSeconds})"));
        }

        if (config.DefaultTimeoutMilliseconds <= 0)
        {
            failures.Add(Failure(nameof(BridgeConfiguration.DefaultTimeoutMilliseconds), $"must be greater than 0 (was {config.DefaultTimeoutMilliseconds})"));
        }

        // CORS cannot be both switched off and given origins to allow.
        if (!config.EnableCors && config.AllowedOrigins is { Count: > 0 })
        {
            failures.Add(Failure(nameof(BridgeConfiguration.EnableCors),
                $"is false but {nameof(BridgeConfiguration.AllowedOrigins)} lists {config.AllowedOrigins.Count} origin(s); " +
                "mutually exclusive: disable CORS or clear AllowedOrigins"));
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateOrigins(BridgeConfiguration config, List<string> failures)
    {
        const string property = nameof(BridgeConfiguration.AllowedOrigins);

        if (config.AllowedOrigins is null)
        {
            failures.Add(Failure(property, "must not be null"));
            return;
        }

        for (var i = 0; i < config.AllowedOrigins.Count; i++)
        {
            var origin = config.AllowedOrigins[i];

            if (string.IsNullOrWhiteSpace(origin))
            {
                failures.Add(Failure($"{property}[{i}]", "must not be empty or whitespace"));
            }
            else if (origin != "*" && !IsValidOrigin(origin))
            {
                failures.Add(Failure($"{property}[{i}]",
                    $"'{origin}' is not a valid origin; expected '*' or scheme://host[:port] using http or https"));
            }
        }
    }

    /// <summary>
    /// An origin is exactly <c>scheme://host[:port]</c>: no path, query, fragment, user info or surrounding whitespace.
    /// </summary>
    private static bool IsValidOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        return string.Equals(origin, $"{uri.Scheme}://{uri.Authority}", StringComparison.OrdinalIgnoreCase);
    }

    private static string Failure(string property, string message) =>
        $"{ConfigurationPath}.{property} {message}";
}
