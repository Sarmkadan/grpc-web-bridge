# StreamingSession

`StreamingSession` represents a single logical session within the gRPC-Web bridge, tracking identity, metadata, and the set of active streams associated with that session. It provides lifecycle management for sessions—creation, stream association/disassociation, retrieval, and orderly closure—along with a lightweight summary projection for enumeration.

## API

### Properties

#### `SessionId`
`public string SessionId`

Globally unique identifier for this session. Immutable after creation.

#### `UserId`
`public string? UserId`

Optional user identifier bound to the session. May be `null` when the session is anonymous or pre-authentication.

#### `ClientOrigin`
`public string? ClientOrigin`

Optional origin identifier (e.g., IP address, hostname, or logical client tag) from which the session was initiated. May be `null` when origin tracking is disabled or unavailable.

#### `AuthContextId`
`public string? AuthContextId`

Optional identifier linking this session to an authentication context. May be `null` when no authentication context has been established.

#### `CreatedAt`
`public DateTime CreatedAt`

UTC timestamp when the session was created. Immutable.

#### `LastActivityAt`
`public DateTime LastActivityAt`

UTC timestamp of the most recent activity on any stream belonging to this session. Updated automatically when associated streams exhibit activity.

#### `Metadata`
`public Dictionary<string, string> Metadata`

Arbitrary key-value pairs attached to the session. Callers may read and mutate this dictionary directly. Keys and values are non-null strings.

### Instance Methods

#### `AssociateStream`
`public bool AssociateStream` *(method, not property)*

Registers a stream with this session. Returns `true` if the stream was newly associated; `false` if the stream was already tracked by this session. Throws `ArgumentNullException` when the stream argument is `null`. Throws `InvalidOperationException` when the session has been closed.

#### `DisassociateStream`
`public void DisassociateStream` *(method, not property)*

Removes a stream from this session’s tracking set. Does nothing if the stream was not associated. Throws `ArgumentNullException` when the stream argument is `null`.

#### `CloseSessionAsync`
`public async Task<bool> CloseSessionAsync()`

Initiates orderly shutdown of the session: disassociates all active streams, marks the session as closed, and releases resources. Returns `true` if the session was successfully closed; `false` if the session was already closed. Awaiting the returned task ensures all disassociation side effects have completed.

### Static / Manager-Level Members

#### `StreamingSessionManager`
`public StreamingSessionManager` *(static property or field on containing type)*

Exposes the singleton manager instance responsible for session creation, lookup, and enumeration. All session-level operations below are accessed through this manager.

#### `CreateSession`
`public StreamingSession CreateSession` *(method on StreamingSessionManager)*

Creates a new session with a unique `SessionId`, sets `CreatedAt` and `LastActivityAt` to the current UTC time, and returns the session object. Optional parameters allow setting `UserId`, `ClientOrigin`, and initial metadata. Throws `InvalidOperationException` if the manager has been disposed or shut down.

#### `GetSessionForStream`
`public StreamingSession? GetSessionForStream` *(method on StreamingSessionManager)*

Returns the session currently associated with the given stream, or `null` if the stream is not associated with any session. Throws `ArgumentNullException` when the stream argument is `null`.

#### `GetSession`
`public StreamingSession? GetSession` *(method on StreamingSessionManager)*

Looks up a session by its `SessionId`. Returns the session if it exists and is active; returns `null` when the session ID is unknown or the session has been closed. Throws `ArgumentNullException` when `sessionId` is `null`.

#### `GetActiveSessions`
`public IReadOnlyCollection<StreamingSession> GetActiveSessions` *(method on StreamingSessionManager)*

Returns a snapshot of all currently active (non-closed) sessions. The returned collection is safe to enumerate and does not reflect subsequent mutations. Never returns `null`; returns an empty collection when no sessions are active.

#### `GetSessionSummaries`
`public IReadOnlyList<SessionSummary> GetSessionSummaries` *(method on StreamingSessionManager)*

Returns a lightweight, read-only list of summaries for all active sessions. Each summary contains a subset of session fields suitable for display or telemetry. Never returns `null`.

#### `SessionSummary`
`public sealed record SessionSummary`

Immutable record containing a projection of session state: `SessionId`, `UserId`, `ClientOrigin`, `CreatedAt`, `LastActivityAt`, and a count of currently associated streams. Instances are produced exclusively by `GetSessionSummaries`.

## Usage

### Example 1: Creating a session, associating streams, and closing

```csharp
// Obtain the manager instance
var manager = StreamingSession.StreamingSessionManager;

// Create a session for an authenticated user
var session = manager.CreateSession(
    userId: "user-42",
    clientOrigin: "192.168.1.10",
    metadata: new Dictionary<string, string> { ["role"] = "admin" }
);

// Associate incoming streams
bool added = session.AssociateStream(requestStream);
session.AssociateStream(responseStream);

// Later, when the client disconnects
bool closed = await session.CloseSessionAsync();
if (closed)
{
    Console.WriteLine($"Session {session.SessionId} closed successfully.");
}
```

### Example 2: Monitoring active sessions via summaries

```csharp
var manager = StreamingSession.StreamingSessionManager;

// Periodic health check or dashboard query
IReadOnlyList<StreamingSession.SessionSummary> summaries = manager.GetSessionSummaries();

foreach (var summary in summaries)
{
    Console.WriteLine(
        $"Session: {summary.SessionId}, User: {summary.UserId ?? "anonymous"}, " +
        $"Streams: {summary.ActiveStreamCount}, LastActivity: {summary.LastActivityAt:O}"
    );
}

// Identify stale sessions (no activity for over 5 minutes)
var staleThreshold = DateTime.UtcNow.AddMinutes(-5);
var staleSessions = summaries
    .Where(s => s.LastActivityAt < staleThreshold)
    .Select(s => s.SessionId);

foreach (var id in staleSessions)
{
    var session = manager.GetSession(id);
    if (session != null)
    {
        await session.CloseSessionAsync();
    }
}
```

## Notes

- **Closed session behavior**: Once `CloseSessionAsync` completes successfully, the session is permanently closed. Subsequent calls to `AssociateStream` throw `InvalidOperationException`. `GetSession` and `GetSessionForStream` return `null` for closed sessions. `Metadata` remains readable but mutations have no effect on bridge behavior.
- **Metadata thread safety**: The `Metadata` dictionary is not synchronized internally. Concurrent reads and writes from multiple threads must be guarded by the caller. All other session state transitions (creation, stream association/disassociation, closure) are safe for concurrent use.
- **`LastActivityAt` updates**: This timestamp is advanced by activity on associated streams. The exact definition of “activity” depends on the stream implementation and may include reads, writes, or heartbeat signals. It does not advance automatically with wall-clock time.
- **Manager disposal**: When the `StreamingSessionManager` is disposed or the host shuts down, `CreateSession` throws `InvalidOperationException`. Existing sessions should be closed beforehand; any remaining sessions are orphaned and their `CloseSessionAsync` may no longer complete normally.
- **`GetActiveSessions` vs `GetSessionSummaries`**: `GetActiveSessions` returns full session objects suitable for direct manipulation. `GetSessionSummaries` returns lightweight records intended for read-only telemetry or display without exposing mutable state or stream references.
- **Null arguments**: All methods accepting reference-type arguments throw `ArgumentNullException` when passed `null`. This applies to `AssociateStream`, `DisassociateStream`, `GetSessionForStream`, and `GetSession`.
