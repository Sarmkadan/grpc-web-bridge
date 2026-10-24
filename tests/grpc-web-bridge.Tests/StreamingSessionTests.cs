#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using FluentAssertions;
using GrpcWebBridge.Streaming;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace GrpcWebBridge.Tests;

/// <summary>Unit tests for <see cref="StreamingSession"/>, exercised through <see cref="StreamingSessionManager"/>.</summary>
public sealed class StreamingSessionTests
{
    private static StreamingSessionManager CreateManager() =>
        new(Substitute.For<IBidirectionalStreamingEngine>(), NullLogger<StreamingSessionManager>.Instance);

    [Fact]
    public void Constructor_Defaults_HasUniqueHexIdAndEmptyContext()
    {
        var first = new StreamingSession();
        var second = new StreamingSession();

        first.SessionId.Should().MatchRegex("^[0-9a-f]{32}$");
        second.SessionId.Should().NotBe(first.SessionId);
        first.UserId.Should().BeNull();
        first.ClientOrigin.Should().BeNull();
        first.AuthContextId.Should().BeNull();
        first.Metadata.Should().BeEmpty();
        first.IsEmpty.Should().BeTrue();
        first.StreamCount.Should().Be(0);
        first.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        first.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        first.LastActivityAt.Should().BeOnOrAfter(first.CreatedAt);
    }

    [Fact]
    public void CreateSession_ValidArguments_RegistersSessionWithContext()
    {
        var manager = CreateManager();
        var metadata = new Dictionary<string, string> { ["tenant"] = "acme" };

        var session = manager.CreateSession("user-1", "10.0.0.5", "auth-9", metadata);

        session.UserId.Should().Be("user-1");
        session.ClientOrigin.Should().Be("10.0.0.5");
        session.AuthContextId.Should().Be("auth-9");
        session.Metadata.Should().ContainKey("tenant").WhoseValue.Should().Be("acme");
        manager.GetSession(session.SessionId).Should().BeSameAs(session);
        manager.ActiveSessionCount.Should().Be(1);
    }

    [Fact]
    public void CreateSession_NullMetadata_UsesEmptyDictionary()
    {
        var session = CreateManager().CreateSession("user-1", "10.0.0.5", "auth-9", metadata: null);

        session.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData(null, "origin", "auth")]
    [InlineData("", "origin", "auth")]
    [InlineData("user", null, "auth")]
    [InlineData("user", "", "auth")]
    [InlineData("user", "origin", null)]
    [InlineData("user", "origin", "")]
    public void CreateSession_NullOrEmptyIdentity_ThrowsArgumentException(
        string? userId, string? clientOrigin, string? authContextId)
    {
        var manager = CreateManager();

        Action act = () => manager.CreateSession(userId, clientOrigin, authContextId);

        act.Should().Throw<ArgumentException>();
        manager.ActiveSessionCount.Should().Be(0);
    }

    [Fact]
    public void AssociateStream_KnownSession_TracksStreamAndReverseMapping()
    {
        var manager = CreateManager();
        var session = manager.CreateSession("user-1", "10.0.0.5", "auth-9");

        manager.AssociateStream(session.SessionId, "s1").Should().BeTrue();
        manager.AssociateStream(session.SessionId, "s1").Should().BeTrue();

        session.StreamCount.Should().Be(1, "re-associating the same stream must not duplicate it");
        session.IsEmpty.Should().BeFalse();
        session.StreamIds.Should().ContainSingle().Which.Should().Be("s1");
        session.LastActivityAt.Should().BeOnOrAfter(session.CreatedAt);
        manager.GetSessionForStream("s1").Should().BeSameAs(session);

        manager.DisassociateStream("s1");

        session.StreamCount.Should().Be(0);
        session.IsEmpty.Should().BeTrue();
        manager.GetSessionForStream("s1").Should().BeNull();
    }

    [Fact]
    public void AssociateStream_UnknownSession_ReturnsFalseAndCreatesNoMapping()
    {
        var manager = CreateManager();

        manager.AssociateStream("no-such-session", "s1").Should().BeFalse();

        manager.GetSessionForStream("s1").Should().BeNull();
    }

    [Theory]
    [InlineData(null, "s1")]
    [InlineData("", "s1")]
    [InlineData("sess", null)]
    [InlineData("sess", "")]
    public void AssociateStream_NullOrEmptyArguments_ThrowsArgumentException(string? sessionId, string? streamId)
    {
        Action act = () => CreateManager().AssociateStream(sessionId!, streamId!);

        act.Should().Throw<ArgumentException>();
    }
}
