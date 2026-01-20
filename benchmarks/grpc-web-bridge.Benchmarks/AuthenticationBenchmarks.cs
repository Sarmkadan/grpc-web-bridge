#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using GrpcWebBridge.Domain;
using GrpcWebBridge.Domain.Models;
using GrpcWebBridge.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace GrpcWebBridge.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public sealed class AuthenticationBenchmarks
{
    private AuthenticationService _service = null!;

    private string _validBearerHeader = null!;
    private string _invalidBearerHeader = null!;
    private string _cachedContextId = null!;
    private string _missingContextId = null!;

    // Minimal valid JWT (header.payload.signature) – not signature-verified in the tested path
    private const string SampleJwt =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9" +
        ".eyJzdWIiOiJ1c2VyLTEyMyIsIm5hbWUiOiJWbGFkeXNsYXYgWmFpZXRzIiwiaWF0IjoxNzAwMDAwMDAwLCJleHAiOjk5OTk5OTk5OTl9" +
        ".SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

    [GlobalSetup]
    public void Setup()
    {
        _service = new AuthenticationService(NullLogger<AuthenticationService>.Instance);

        _validBearerHeader = $"Bearer {SampleJwt}";
        _invalidBearerHeader = "Basic dXNlcjpwYXNz";

        // Pre-populate cache via API key path (no JWT validation required)
        var ctx = _service.AuthenticateApiKey("api-key-bench-001", "bench-user");
        _cachedContextId = ctx.Id;
        _missingContextId = Guid.NewGuid().ToString("N");
    }

    [Benchmark(Description = "ExtractBearerToken — valid Bearer header")]
    public string? ExtractBearerToken_Valid() =>
        _service.ExtractBearerToken(_validBearerHeader);

    [Benchmark(Description = "ExtractBearerToken — non-Bearer header")]
    public string? ExtractBearerToken_Invalid() =>
        _service.ExtractBearerToken(_invalidBearerHeader);

    [Benchmark(Description = "ExtractBearerToken — null header")]
    public string? ExtractBearerToken_Null() =>
        _service.ExtractBearerToken(null);

    [Benchmark(Description = "GetCachedContext — cache hit")]
    public AuthenticationContext? GetCachedContext_Hit() =>
        _service.GetCachedContext(_cachedContextId);

    [Benchmark(Description = "GetCachedContext — cache miss")]
    public AuthenticationContext? GetCachedContext_Miss() =>
        _service.GetCachedContext(_missingContextId);

    [Benchmark(Description = "AuthenticateApiKey — full path")]
    public AuthenticationContext AuthenticateApiKey() =>
        _service.AuthenticateApiKey($"api-key-{Guid.NewGuid():N}", "bench-user");

    [Benchmark(Description = "ValidateContext — authenticated context")]
    public bool ValidateContext()
    {
        var ctx = _service.GetCachedContext(_cachedContextId)!;
        return _service.ValidateContext(ctx);
    }
}
