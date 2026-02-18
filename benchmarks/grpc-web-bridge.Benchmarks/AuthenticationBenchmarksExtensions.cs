#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using System.Diagnostics.CodeAnalysis;

namespace GrpcWebBridge.Benchmarks;

/// <summary>
/// Extension methods for <see cref="AuthenticationBenchmarks"/> to provide additional benchmarking utilities.
/// </summary>
public static class AuthenticationBenchmarksExtensions
{
    /// <summary>
    /// Creates a pre-configured <see cref="AuthenticationBenchmarks"/> instance with common setup.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>A configured <see cref="AuthenticationBenchmarks"/> instance ready for benchmarking.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static AuthenticationBenchmarks WithCommonSetup(this AuthenticationBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        benchmarks.Setup();
        return benchmarks;
    }

    /// <summary>
    /// Extracts the Bearer token from the Authorization header and validates it.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="bearerHeader">The Bearer header string (format: "Bearer {token}").</param>
    /// <returns>
    /// The extracted and validated token if successful, or null if extraction fails or validation returns false.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static string? ExtractAndValidateBearerToken(this AuthenticationBenchmarks benchmarks, string? bearerHeader)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        if (string.IsNullOrEmpty(bearerHeader))
        {
            return null;
        }

        var token = benchmarks._service.ExtractBearerToken(bearerHeader);

        if (token is null)
        {
            return null;
        }

        var context = benchmarks._service.AuthenticateBearer(token);
        return context is not null && benchmarks._service.ValidateContext(context) ? token : null;
    }

    /// <summary>
    /// Measures the round-trip time for authenticating an API key and validating the resulting context.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="apiKey">The API key to authenticate.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The authentication context if successful, null otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="apiKey"/> or <paramref name="userId"/> is null or empty.</exception>
    public static AuthenticationContext? MeasureApiKeyAuthentication(
        this AuthenticationBenchmarks benchmarks,
        string apiKey,
        string userId)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var context = benchmarks._service.AuthenticateApiKey(apiKey, userId);
        return benchmarks._service.ValidateContext(context) ? context : null;
    }

    /// <summary>
    /// Gets a benchmark-ready authentication context with the specified ID.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="contextId">The context ID to retrieve.</param>
    /// <returns>
    /// The authentication context if found and valid, null otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="contextId"/> is null or empty.</exception>
    public static AuthenticationContext? GetContextOrDefault(
        this AuthenticationBenchmarks benchmarks,
        string contextId)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(contextId);

        var context = benchmarks._service.GetCachedContext(contextId);
        return context is not null && benchmarks._service.ValidateContext(context) ? context : null;
    }
}
