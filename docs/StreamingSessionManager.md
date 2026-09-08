# StreamingSessionManager

`StreamingSessionManager` is the thread-safe registry for logical streaming sessions. It creates sessions, maintains the bidirectional relationship between session IDs and stream IDs, provides active-session snapshots and diagnostic summaries, and closes every associated stream when a session ends. `IStreamingSessionManager` exposes the same operations as an injectable service contract.

## API

### Session Lifecycle Methods

#### `CreateSession`
`public StreamingSession CreateSession(string? userId = null, string? clientOrigin = null, string? authContextId = null, Dictionary<string, string>? metadata = null)`

Creates and registers a new `StreamingSession`. The optional arguments initialize the session's user, client origin, authentication context, and metadata. When `metadata` is `null`, the session receives an empty dictionary. The returned session has an automatically generated ID and UTC creation and activity timestamps.

#### `CloseSessionAsync`
`public Task<bool> CloseSessionAsync(string sessionId, GrpcStatusCode finalStatus = GrpcStatusCode.Ok, CancellationToken cancellationToken = default)`

Removes the identified session from the active registry, removes each of its reverse stream mappings, and asks the bidirectional streaming engine to close all associated streams concurrently with the supplied final status. Returns `true` after all close operations complete. Returns `false` without closing streams when the session is not registered. Cancellation is passed to each stream close operation.

### Stream Association Methods

#### `AssociateStream`
`public bool AssociateStream(string sessionId, string streamId)`

Associates `streamId` with an active session and records the reverse stream-to-session mapping. Returns `false` if `sessionId` is not registered; otherwise returns `true`. Associating a stream updates the session's last-activity timestamp. If the stream ID already belongs to another session, its reverse mapping is replaced, but the earlier session's stream-ID set is not modified automatically.

#### `DisassociateStream`
`public void DisassociateStream(string streamId)`

Removes the reverse mapping for `streamId` and removes the stream from the mapped session when that session is still active. It does nothing when no reverse mapping exists. Removing a mapped stream from an active session updates that session's last-activity timestamp.

### Lookup and Enumeration Methods

#### `GetSessionForStream`
`public StreamingSession? GetSessionForStream(string streamId)`

Resolves the session associated with `streamId`. Returns `null` when the stream has no reverse mapping or when its mapped session is no longer registered.

#### `GetSession`
`public StreamingSession? GetSession(string sessionId)`

Returns the active session registered under `sessionId`, or `null` when no such session exists.

#### `GetActiveSessions`
`public IReadOnlyCollection<StreamingSession> GetActiveSessions()`

Returns a point-in-time collection containing the currently registered sessions. The collection itself is a snapshot; the returned `StreamingSession` objects remain the live session instances.

#### `GetSessionSummaries`
`public IReadOnlyList<SessionSummary> GetSessionSummaries()`

Returns a point-in-time diagnostic projection of every active session. A single UTC timestamp is captured for the call and used to calculate each session's `IdleSeconds` value.

### Records

#### `SessionSummary`
`public sealed record SessionSummary(string SessionId, string? UserId, string? ClientOrigin, int StreamCount, DateTime CreatedAt, double IdleSeconds)`

Immutable diagnostic record produced by `GetSessionSummaries`.

- `SessionId`: Unique session identifier.
- `UserId`: Authenticated user identifier, or `null` for an anonymous session.
- `ClientOrigin`: Client connection origin, or `null` when unavailable.
- `StreamCount`: Number of streams associated with the session when the summary is created.
- `CreatedAt`: UTC timestamp at which the session was created.
- `IdleSeconds`: Seconds elapsed between the session's most recent stream-level activity and the summary operation's captured UTC time.

## Usage

### Example 1: Creating a session and associating streams

```csharp
public sealed class ConnectionHandler(IStreamingSessionManager sessions)
{
    public string OpenConnection(string userId, string origin)
    {
        var session = sessions.CreateSession(
            userId: userId,
            clientOrigin: origin,
            metadata: new Dictionary<string, string>
            {
                ["transport"] = "grpc-web"
            });

        sessions.AssociateStream(session.SessionId, "stream-1");
        sessions.AssociateStream(session.SessionId, "stream-2");

        return session.SessionId;
    }
}
```

### Example 2: Looking up and closing a session

```csharp
var session = sessionManager.GetSessionForStream("stream-1");
if (session is not null)
{
    await sessionManager.CloseSessionAsync(
        session.SessionId,
        GrpcStatusCode.Ok,
        cancellationToken);
}
```

### Example 3: Reading diagnostic summaries

```csharp
IReadOnlyList<SessionSummary> summaries = sessionManager.GetSessionSummaries();

foreach (var summary in summaries)
{
    Console.WriteLine(
        $"{summary.SessionId}: {summary.StreamCount} stream(s), " +
        $"idle for {summary.IdleSeconds:F1} seconds");
}
```

## Notes

- **Interface and implementation**: Applications can depend on `IStreamingSessionManager`; `StreamingSessionManager` supplies the documented implementation.
- **Thread safety**: Concurrent dictionaries protect the session registry and reverse stream mappings. Each `StreamingSession` independently protects its stream-ID set.
- **Closure visibility**: `CloseSessionAsync` removes a session before awaiting its stream closures, so lookups and active-session enumeration stop returning it while teardown is in progress.
- **Close failures**: Exceptions or cancellation from an engine close operation propagate from `CloseSessionAsync`; the session has already been removed from the registry at that point.
- **Identifier handling**: These methods do not perform explicit null, empty-string, or whitespace validation. Dictionary operations determine behavior for supplied keys.
