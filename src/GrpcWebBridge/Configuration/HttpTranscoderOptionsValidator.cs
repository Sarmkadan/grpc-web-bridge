#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace GrpcWebBridge.Configuration;

/// <summary>
/// Validates <see cref="HttpTranscoderOptions"/> when the host starts (see <c>ValidateOnStart</c>).
/// Every failing rule is reported in a single result so one startup attempt shows all problems.
/// </summary>
public sealed class HttpTranscoderOptionsValidator : IValidateOptions<HttpTranscoderOptions>
{
    private const string OptionsPath = nameof(HttpTranscoderOptions);

    private static readonly HashSet<string> _allowedHttpMethods = new(StringComparer.Ordinal)
    {
        "GET", "PUT", "POST", "DELETE", "PATCH"
    };

    // A variable is a brace group with no braces inside, such as {id} or {name=books/*}.
    private static readonly Regex _variableGroup = new(@"\{[^{}]*\}", RegexOptions.CultureInvariant);

    /// <summary>
    /// Validates the transcoder options and collects all failures.
    /// </summary>
    /// <param name="name">The options instance name (unused; the transcoder registers a single unnamed instance).</param>
    /// <param name="options">The options to validate.</param>
    /// <returns>A successful result, or a failed result listing every violated rule.</returns>
    public ValidateOptionsResult Validate(string? name, HttpTranscoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        ValidateRoutePrefix(options.RoutePrefix, failures);

        if (options.JsonSerializerOptions is null)
        {
            failures.Add(Failure(nameof(HttpTranscoderOptions.JsonSerializerOptions), "must not be null"));
        }

        if (options.MaxRequestBodySize <= 0)
        {
            failures.Add(Failure(nameof(HttpTranscoderOptions.MaxRequestBodySize), $"must be greater than 0 (was {options.MaxRequestBodySize})"));
        }

        ValidateRoutes(options.Routes, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateRoutePrefix(string? prefix, List<string> failures)
    {
        const string property = nameof(HttpTranscoderOptions.RoutePrefix);

        if (prefix is null)
        {
            return;
        }

        if (!prefix.StartsWith('/'))
        {
            failures.Add(Failure(property, $"must start with '/' (was '{prefix}')"));
        }
        else if (prefix.EndsWith('/'))
        {
            failures.Add(Failure(property, $"must not end with '/' (was '{prefix}')"));
        }
    }

    private static void ValidateRoutes(List<HttpRouteBinding>? routes, List<string> failures)
    {
        const string property = nameof(HttpTranscoderOptions.Routes);

        if (routes is null)
        {
            failures.Add(Failure(property, "must not be null"));
            return;
        }

        // Key: "<VERB> <template with every variable replaced by {}>". Two routes with the same key cannot be told apart.
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var i = 0; i < routes.Count; i++)
        {
            var route = routes[i];
            var path = $"{property}[{i}]";

            if (route is null)
            {
                failures.Add(Failure(path, "must not be null"));
                continue;
            }

            var routeName = $"{route.ServiceName}/{route.MethodName}";

            if (!_allowedHttpMethods.Contains(route.HttpMethod))
            {
                failures.Add(Failure($"{path}.{nameof(HttpRouteBinding.HttpMethod)}",
                    $"must be one of GET, PUT, POST, DELETE, PATCH (was '{route.HttpMethod}' for {routeName})"));
            }

            if (!TryParseVariables(route.Template, out var variables, out var error))
            {
                failures.Add(Failure($"{path}.{nameof(HttpRouteBinding.Template)}",
                    $"'{route.Template}' for {routeName} is not a valid route template: {error}"));
                continue;
            }

            var requestFields = route.RequestFieldNames ?? [];
            foreach (var variable in variables)
            {
                // Nested paths such as {book.id} are matched on their top-level field only.
                var topLevelField = variable.Split('.')[0];
                if (!requestFields.Contains(topLevelField, StringComparer.Ordinal))
                {
                    failures.Add(Failure($"{path}.{nameof(HttpRouteBinding.Template)}",
                        $"variable '{{{variable}}}' for {routeName} does not match a request field"));
                }
            }

            var key = $"{route.HttpMethod} {_variableGroup.Replace(route.Template!, "{}")}";
            if (seen.TryGetValue(key, out var firstRoute))
            {
                failures.Add(Failure(path,
                    $"route '{route.HttpMethod} {route.Template}' for {routeName} duplicates {firstRoute}"));
            }
            else
            {
                seen[key] = $"'{route.HttpMethod} {route.Template}' for {routeName} at {property}[{i}]";
            }
        }
    }

    /// <summary>
    /// Checks that a route template is well formed and returns the variable names it declares.
    /// A variable name is the text before any <c>=</c> pattern, so <c>{name=books/*}</c> declares <c>name</c>.
    /// </summary>
    private static bool TryParseVariables(string? template, out List<string> variables, out string? error)
    {
        variables = [];
        error = null;

        if (string.IsNullOrEmpty(template))
        {
            error = "must not be empty";
            return false;
        }

        if (!template.StartsWith('/'))
        {
            error = "must start with '/'";
            return false;
        }

        var depth = 0;
        var groupStart = -1;

        for (var i = 0; i < template.Length; i++)
        {
            switch (template[i])
            {
                case '{' when depth > 0:
                    error = "contains a nested '{'";
                    return false;

                case '{':
                    depth = 1;
                    groupStart = i;
                    break;

                case '}' when depth == 0:
                    error = "contains an unmatched '}'";
                    return false;

                case '}':
                    depth = 0;
                    var body = template[(groupStart + 1)..i];
                    var equals = body.IndexOf('=');
                    var variable = equals >= 0 ? body[..equals] : body;

                    if (string.IsNullOrWhiteSpace(variable))
                    {
                        error = "contains a variable with an empty name";
                        return false;
                    }

                    if (variables.Contains(variable, StringComparer.Ordinal))
                    {
                        error = $"declares variable '{variable}' more than once";
                        return false;
                    }

                    variables.Add(variable);
                    break;
            }
        }

        if (depth != 0)
        {
            error = "contains an unclosed '{'";
            return false;
        }

        return true;
    }

    private static string Failure(string property, string message) =>
        $"{OptionsPath}.{property} {message}";
}
