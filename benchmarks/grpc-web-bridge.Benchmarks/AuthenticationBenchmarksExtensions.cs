#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

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
    public static AuthenticationBenchmarks WithCommonSetup(this AuthenticationBenchmarks benchmarks)
    {
        benchmarks.Setup();
        return benchmarks;
    }

    /// <summary>
    /// Extracts the Bearer token and validates it in a single call.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="bearerHeader">The Bearer header string.</param>
    /// <returns>
    /// The extracted and validated token if successful, or null if extraction fails or validation returns false.
    /// </returns>
    public static string? ExtractAndValidateBearerToken(this AuthenticationBenchmarks benchmarks, string? bearerHeader)
    {
        var token = benchmarks.ExtractBearerToken_Valid();

        if (token is null)
        {
            return null;
        }

        var context = benchmarks.GetCachedContext_Hit();
        return context is not null && benchmarks.ValidateContext() ? token : null;
    }

    /// <summary>
    /// Measures the round-trip time for authenticating an API key and validating the resulting context.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="apiKey">The API key to authenticate.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>The authentication context if successful, null otherwise.</returns>
    public static AuthenticationContext? MeasureApiKeyAuthentication(
        this AuthenticationBenchmarks benchmarks,
        string apiKey,
        string userId)
    {
        var context = benchmarks.AuthenticateApiKey();
        return benchmarks.ValidateContext() ? context : null;
    }

    /// <summary>
    /// Gets a benchmark-ready authentication context with the specified ID.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <param name="contextId">The context ID to retrieve.</param>
    /// <returns>
    /// The authentication context if found and valid, null otherwise.
    /// </returns>
    public static AuthenticationContext? GetContextOrDefault(
        this AuthenticationBenchmarks benchmarks,
        string contextId)
    {
        var context = benchmarks.GetCachedContext_Hit();
        return context is not null && benchmarks.ValidateContext() ? context : null;
    }
}