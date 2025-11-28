#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Middleware;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>
/// Unit tests for <see cref="ClientRateLimit"/> — the sliding-window rate tracking
/// component used by <see cref="RateLimitingMiddleware"/>.
/// </summary>
public sealed class ClientRateLimitTests
{
    // ─────────────────────────────────────────────────────────────────────
    // AllowRequest
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllowRequest_BelowLimit_ReturnsTrue()
    {
        var limit = new ClientRateLimit();

        // 5 requests per second, window = 1 s → threshold = 5
        for (int i = 0; i < 5; i++)
        {
            limit.AllowRequest(5, 1).Should().BeTrue($"request {i + 1} is within the limit");
        }
    }

    [Fact]
    public void AllowRequest_AtLimitBoundary_ReturnsFalseForExtraRequest()
    {
        var limit = new ClientRateLimit();

        // Fill up to the maximum allowed
        for (int i = 0; i < 10; i++)
            limit.AllowRequest(10, 1);

        // The next request should be rejected
        limit.AllowRequest(10, 1).Should().BeFalse("limit is exactly exhausted");
    }

    [Fact]
    public void AllowRequest_WithGenerousLimits_AllowsManyRequests()
    {
        var limit = new ClientRateLimit();

        // 100 req/sec over a 5-second window → 500 total allowed
        for (int i = 0; i < 500; i++)
            limit.AllowRequest(100, 5).Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────
    // GetRequestCount
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void GetRequestCount_AfterSeveralRequests_ReturnsCorrectCount()
    {
        var limit = new ClientRateLimit();

        limit.AllowRequest(100, 10);
        limit.AllowRequest(100, 10);
        limit.AllowRequest(100, 10);

        limit.GetRequestCount(10).Should().Be(3);
    }

    [Fact]
    public void GetRequestCount_OnFreshInstance_ReturnsZero()
    {
        var limit = new ClientRateLimit();
        limit.GetRequestCount(60).Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────
    // IsStale
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsStale_FreshInstance_IsStaleForNonZeroTimeout()
    {
        // A brand-new instance has never been used, so _lastRequestTime = default.
        // That timestamp is far in the past, so it should be stale.
        var limit = new ClientRateLimit();
        limit.IsStale(TimeSpan.FromSeconds(1)).Should().BeTrue(
            "a client that has never sent a request is considered stale");
    }

    [Fact]
    public void IsStale_AfterRecentRequest_ReturnsFalse()
    {
        var limit = new ClientRateLimit();
        limit.AllowRequest(100, 60); // records _lastRequestTime = now

        limit.IsStale(TimeSpan.FromMinutes(5)).Should().BeFalse(
            "a request was made just now, well within the stale threshold");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Thread safety
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void AllowRequest_ConcurrentAccess_DoesNotThrowOrCorruptState()
    {
        var limit = new ClientRateLimit();
        int allowed = 0;

        Parallel.For(0, 200, _ =>
        {
            if (limit.AllowRequest(1000, 60))
                Interlocked.Increment(ref allowed);
        });

        allowed.Should().BeLessThanOrEqualTo(1000);
        limit.GetRequestCount(60).Should().BeLessThanOrEqualTo(1000);
    }
}
