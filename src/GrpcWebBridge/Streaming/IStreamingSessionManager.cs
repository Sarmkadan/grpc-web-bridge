#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using GrpcWebBridge.Domain;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Thread-safe manager for <see cref="StreamingSession"/> instances.
/// <para>
/// The typical workflow is:
/// </para>
/// <list type="number">
///   <item>
///     The transport layer calls <see cref="CreateSession"/> when a new client connection
///     is established, capturing the client's identity and origin.
///   </item>
///   <item>
///     For each gRPC call on that connection, <see cref="AssociateStream"/> links the
///     stream ID to the session, and <see cref="GetSessionForStream"/> allows any component
///     to resolve the owning session for a given stream.
///   </item>
///   <item>
///     On connection close, <see cref="CloseSessionAsync"/> tears down all associated
///     streams through the <see cref="IBidirectionalStreamingEngine"/> and removes the
///     session from the registry.
///   </item>
/// </list>
/// </summary>
public interface IStreamingSessionManager
{
    /// <summary>Total number of sessions currently active.</summary>
    int ActiveSessionCount { get; }

    /// <summary>
    /// Creates and registers a new <see cref="StreamingSession"/>.
    /// </summary>
    /// <param name="userId">Optional user identifier (from authentication context).</param>
    /// <param name="clientOrigin">Optional client origin (IP, host, user-agent hash, etc.).</param>
    /// <param name="authContextId">Optional identifier of the resolved authentication context.</param>
    /// <param name="metadata">Optional key-value metadata attached at the session level.</param>
    /// <returns>The newly created <see cref="StreamingSession"/>.</returns>
    StreamingSession CreateSession(
        string? userId = null,
        string? clientOrigin = null,
        string? authContextId = null,
        Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Associates a stream with an existing session and establishes the reverse mapping
    /// required for stream-to-session lookup.
    /// </summary>
    /// <param name="sessionId">Identifier of the target session.</param>
    /// <param name="streamId">Stream identifier to associate.</param>
    /// <returns>
    /// <c>true</c> when the association was established successfully;
    /// <c>false</c> when the session was not found.
    /// </returns>
    bool AssociateStream(string sessionId, string streamId);

    /// <summary>
    /// Removes a stream from its associated session and clears the reverse mapping.
    /// Safe to call even when the stream has no session mapping.
    /// </summary>
    /// <param name="streamId">Stream identifier to disassociate.</param>
    void DisassociateStream(string streamId);

    /// <summary>
    /// Returns the session that owns the specified stream, or <c>null</c> when
    /// no mapping exists.
    /// </summary>
    /// <param name="streamId">Stream identifier to look up.</param>
    StreamingSession? GetSessionForStream(string streamId);

    /// <summary>
    /// Returns a session by its identifier, or <c>null</c> if not registered.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    StreamingSession? GetSession(string sessionId);

    /// <summary>
    /// Returns a point-in-time snapshot of all currently active sessions.
    /// </summary>
    IReadOnlyCollection<StreamingSession> GetActiveSessions();

    /// <summary>
    /// Closes all streams associated with the specified session via the engine,
    /// then removes the session from the registry.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to close.</param>
    /// <param name="finalStatus">
    /// gRPC status code attached to each stream closed during teardown.
    /// Defaults to <see cref="GrpcStatusCode.Ok"/>.
    /// </param>
    /// <param name="cancellationToken">Token to abandon individual stream close waits.</param>
    /// <returns>
    /// <c>true</c> when the session was found and removed; <c>false</c> when the session
    /// did not exist.
    /// </returns>
    Task<bool> CloseSessionAsync(
        string sessionId,
        GrpcStatusCode finalStatus = GrpcStatusCode.Ok,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a diagnostic summary of all active sessions, including stream counts
    /// and idle durations.
    /// </summary>
    IReadOnlyList<SessionSummary> GetSessionSummaries();
}