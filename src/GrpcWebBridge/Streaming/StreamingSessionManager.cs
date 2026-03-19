// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using GrpcWebBridge.Domain;
using Microsoft.Extensions.Logging;

namespace GrpcWebBridge.Streaming;

/// <summary>
/// Represents a logical session that groups one or more related gRPC streams
/// originating from a single client connection.
/// <para>
/// Sessions carry client-level context — authentication identifier, connection origin,
/// and arbitrary metadata — that applies uniformly across every stream opened within
/// the session's lifetime.  Stream association is bidirectional: the session holds a
/// set of stream IDs, and the <see cref="StreamingSessionManager"/> maintains a
/// reverse mapping from stream ID back to session ID for O(1) lookup.
/// </para>
/// </summary>
public sealed class StreamingSession
{
    private readonly HashSet<string> _streamIds = [];
    private readonly object _lock = new();

    /// <summary>Unique identifier for this session, auto-generated at construction.</summary>
    public string SessionId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>Optional user identifier extracted from the authentication context.</summary>
    public string? UserId { get; init; }

    /// <summary>Optional client origin — IP address, host name, or connection identifier.</summary>
    public string? ClientOrigin { get; init; }

    /// <summary>Identifier of the authentication context associated with this session.</summary>
    public string? AuthContextId { get; init; }

    /// <summary>UTC timestamp at which this session was created.</summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the most recent stream-level activity in this session.</summary>
    public DateTime LastActivityAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Arbitrary session-level metadata for extensibility (e.g. user-agent, tenant ID).
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = [];

    /// <summary>Read-only snapshot of stream IDs currently associated with this session.</summary>
    public IReadOnlyCollection<string> StreamIds
    {
        get { lock (_lock) { return [.. _streamIds]; } }
    }

    /// <summary>Returns <c>true</c> when the session has no associated streams.</summary>
    public bool IsEmpty
    {
        get { lock (_lock) { return _streamIds.Count == 0; } }
    }

    /// <summary>Number of streams currently associated with this session.</summary>
    public int StreamCount
    {
        get { lock (_lock) { return _streamIds.Count; } }
    }

    /// <summary>
    /// Associates a stream identifier with this session.
    /// </summary>
    /// <param name="streamId">Stream identifier to add.</param>
    /// <returns><c>true</c> if newly added; <c>false</c> if already present.</returns>
    internal bool AddStream(string streamId)
    {
        lock (_lock)
        {
            LastActivityAt = DateTime.UtcNow;
            return _streamIds.Add(streamId);
        }
    }

    /// <summary>
    /// Removes a stream identifier from this session.
    /// </summary>
    /// <param name="streamId">Stream identifier to remove.</param>
    /// <returns><c>true</c> if the stream was found and removed; <c>false</c> otherwise.</returns>
    internal bool RemoveStream(string streamId)
    {
        lock (_lock)
        {
            LastActivityAt = DateTime.UtcNow;
            return _streamIds.Remove(streamId);
        }
    }

    /// <summary>Records activity by updating <see cref="LastActivityAt"/> to the current UTC time.</summary>
    internal void Touch() => LastActivityAt = DateTime.UtcNow;
}

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
public sealed class StreamingSessionManager
{
    private readonly ConcurrentDictionary<string, StreamingSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _streamToSession = new();
    private readonly IBidirectionalStreamingEngine _engine;
    private readonly ILogger<StreamingSessionManager> _logger;

    /// <summary>Total number of sessions currently active.</summary>
    public int ActiveSessionCount => _sessions.Count;

    /// <summary>
    /// Initialises the manager.
    /// </summary>
    /// <param name="engine">
    /// The bidirectional streaming engine used to close individual streams during
    /// session teardown.
    /// </param>
    /// <param name="logger">Logger for session lifecycle events.</param>
    public StreamingSessionManager(
        IBidirectionalStreamingEngine engine,
        ILogger<StreamingSessionManager> logger)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates and registers a new <see cref="StreamingSession"/>.
    /// </summary>
    /// <param name="userId">Optional user identifier (from authentication context).</param>
    /// <param name="clientOrigin">Optional client origin (IP, host, user-agent hash, etc.).</param>
    /// <param name="authContextId">Optional identifier of the resolved authentication context.</param>
    /// <param name="metadata">Optional key-value metadata attached at the session level.</param>
    /// <returns>The newly created <see cref="StreamingSession"/>.</returns>
    public StreamingSession CreateSession(
        string? userId = null,
        string? clientOrigin = null,
        string? authContextId = null,
        Dictionary<string, string>? metadata = null)
    {
        var session = new StreamingSession
        {
            UserId = userId,
            ClientOrigin = clientOrigin,
            AuthContextId = authContextId,
            Metadata = metadata ?? []
        };

        _sessions[session.SessionId] = session;

        _logger.LogInformation(
            "Session {SessionId} created — user={UserId}, origin={Origin}.",
            session.SessionId,
            userId ?? "(anonymous)",
            clientOrigin ?? "(unknown)");

        return session;
    }

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
    public bool AssociateStream(string sessionId, string streamId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            _logger.LogWarning(
                "AssociateStream: session '{SessionId}' not found for stream '{StreamId}'.",
                sessionId, streamId);
            return false;
        }

        session.AddStream(streamId);
        _streamToSession[streamId] = sessionId;

        _logger.LogDebug(
            "Stream {StreamId} associated with session {SessionId}.",
            streamId, sessionId);

        return true;
    }

    /// <summary>
    /// Removes a stream from its associated session and clears the reverse mapping.
    /// Safe to call even when the stream has no session mapping.
    /// </summary>
    /// <param name="streamId">Stream identifier to disassociate.</param>
    public void DisassociateStream(string streamId)
    {
        if (!_streamToSession.TryRemove(streamId, out var sessionId))
            return;

        if (_sessions.TryGetValue(sessionId, out var session))
            session.RemoveStream(streamId);

        _logger.LogDebug(
            "Stream {StreamId} disassociated from session {SessionId}.",
            streamId, sessionId);
    }

    /// <summary>
    /// Returns the session that owns the specified stream, or <c>null</c> when
    /// no mapping exists.
    /// </summary>
    /// <param name="streamId">Stream identifier to look up.</param>
    public StreamingSession? GetSessionForStream(string streamId) =>
        _streamToSession.TryGetValue(streamId, out var sessionId)
            ? _sessions.GetValueOrDefault(sessionId)
            : null;

    /// <summary>
    /// Returns a session by its identifier, or <c>null</c> if not registered.
    /// </summary>
    /// <param name="sessionId">Session identifier.</param>
    public StreamingSession? GetSession(string sessionId) =>
        _sessions.GetValueOrDefault(sessionId);

    /// <summary>
    /// Returns a point-in-time snapshot of all currently active sessions.
    /// </summary>
    public IReadOnlyCollection<StreamingSession> GetActiveSessions() =>
        [.. _sessions.Values];

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
    public async Task<bool> CloseSessionAsync(
        string sessionId,
        GrpcStatusCode finalStatus = GrpcStatusCode.Ok,
        CancellationToken cancellationToken = default)
    {
        if (!_sessions.TryRemove(sessionId, out var session))
        {
            _logger.LogDebug("CloseSessionAsync: session '{SessionId}' not found.", sessionId);
            return false;
        }

        var streamIds = session.StreamIds.ToArray();

        _logger.LogInformation(
            "Closing session {SessionId} — tearing down {Count} stream(s) with status {Status}.",
            sessionId, streamIds.Length, finalStatus);

        await Task.WhenAll(streamIds.Select(async id =>
        {
            _streamToSession.TryRemove(id, out _);
            await _engine.CloseStreamAsync(id, finalStatus, cancellationToken);
        }));

        _logger.LogInformation(
            "Session {SessionId} closed — {Count} stream(s) torn down.",
            sessionId, streamIds.Length);

        return true;
    }

    /// <summary>
    /// Returns a diagnostic summary of all active sessions, including stream counts
    /// and idle durations.
    /// </summary>
    public IReadOnlyList<SessionSummary> GetSessionSummaries()
    {
        var now = DateTime.UtcNow;
        return [.. _sessions.Values.Select(s => new SessionSummary(
            s.SessionId,
            s.UserId,
            s.ClientOrigin,
            s.StreamCount,
            s.CreatedAt,
            (now - s.LastActivityAt).TotalSeconds))];
    }
}

/// <summary>
/// Lightweight diagnostic record describing an active session at a point in time.
/// </summary>
/// <param name="SessionId">Unique session identifier.</param>
/// <param name="UserId">Authenticated user identifier, or <c>null</c> for anonymous sessions.</param>
/// <param name="ClientOrigin">Client connection origin, or <c>null</c> when unavailable.</param>
/// <param name="StreamCount">Number of streams currently associated with the session.</param>
/// <param name="CreatedAt">UTC timestamp at which the session was created.</param>
/// <param name="IdleSeconds">Seconds elapsed since the most recent stream-level activity.</param>
public sealed record SessionSummary(
    string SessionId,
    string? UserId,
    string? ClientOrigin,
    int StreamCount,
    DateTime CreatedAt,
    double IdleSeconds);
