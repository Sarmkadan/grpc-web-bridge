# IStreamingSessionManager

Namespace: `GrpcWebBridge.Streaming`

Thread-safe manager for `StreamingSession` instances. Groups one or more related gRPC streams under a single logical session tied to a client connection.

## Typical workflow

1. The transport layer calls `CreateSession` when a new client connection is established, capturing the client's identity and origin.
2. For each gRPC call on that connection, `AssociateStream` links the stream ID to the session, and `GetSessionForStream` resolves the owning session for a given stream.
3. On connection close, `CloseSessionAsync` tears down all associated streams through the `IBidirectionalStreamingEngine` and removes the session from the registry.

## Interface members

### Properties

#### `ActiveSessionCount`

```csharp
int ActiveSessionCount { get; }
```

Returns the total number of sessions currently active.

### Methods

#### `CreateSession`

```csharp
StreamingSession CreateSession(
    string? userId = null,
    string? clientOrigin = null,
    string? authContextId = null,
    Dictionary<string, string>? metadata = null);
```

Creates and registers a new `StreamingSession`.

| Parameter | Description |
|---|---|
| `userId` | Optional user identifier from the authentication context. |
| `clientOrigin` | Optional client origin (IP, host, user-agent hash, etc.). |
| `authContextId` | Optional identifier of the resolved authentication context. |
| `metadata` | Optional key-value metadata attached at the session level. |

Returns the newly created `StreamingSession`.

#### `AssociateStream`

```csharp
bool AssociateStream(string sessionId, string streamId);
```

Associates a stream with an existing session and establishes the reverse mapping required for stream-to-session lookup.

Returns `true` when the association was established successfully; `false` when the session was not found.

Throws `ArgumentException` when `sessionId` or `streamId` is null or empty.

#### `DisassociateStream`

```csharp
void DisassociateStream(string streamId);
```

Removes a stream from its associated session and clears the reverse mapping. Safe to call even when the stream has no session mapping.

Throws `ArgumentException` when `streamId` is null or empty.

#### `GetSessionForStream`

```csharp
StreamingSession? GetSessionForStream(string streamId);
```

Returns the session that owns the specified stream, or `null` when no mapping exists.

#### `GetSession`

```csharp
StreamingSession? GetSession(string sessionId);
```

Returns a session by its identifier, or `null` if not registered.

#### `GetActiveSessions`

```csharp
IReadOnlyCollection<StreamingSession> GetActiveSessions();
```

Returns a point-in-time snapshot of all currently active sessions.

#### `CloseSessionAsync`

```csharp
Task<bool> CloseSessionAsync(
    string sessionId,
    GrpcStatusCode finalStatus = GrpcStatusCode.Ok,
    CancellationToken cancellationToken = default);
```

Closes all streams associated with the specified session via the `IBidirectionalStreamingEngine`, then removes the session from the registry.

| Parameter | Description |
|---|---|
| `sessionId` | Identifier of the session to close. |
| `finalStatus` | gRPC status code attached to each stream closed during teardown. Defaults to `GrpcStatusCode.Ok`. |
| `cancellationToken` | Token to abandon individual stream close waits. |

Returns `true` when the session was found and removed; `false` when the session did not exist.

Throws `ArgumentException` when `sessionId` is null or empty.

#### `GetSessionSummaries`

```csharp
IReadOnlyList<SessionSummary> GetSessionSummaries();
```

Returns a diagnostic summary of all active sessions, including stream counts and idle durations. Each `SessionSummary` record contains:

- `SessionId` - unique session identifier
- `UserId` - authenticated user identifier, or `null` for anonymous sessions
- `ClientOrigin` - client connection origin, or `null` when unavailable
- `StreamCount` - number of streams currently associated with the session
- `CreatedAt` - UTC timestamp at which the session was created
- `IdleSeconds` - seconds elapsed since the most recent stream-level activity

## Contract expectations

- All methods must be safe for concurrent use from multiple threads.
- Session IDs are auto-generated (`Guid.NewGuid().ToString("N")`), so callers should not fabricate them.
- `AssociateStream` / `DisassociateStream` maintain a bidirectional mapping: the session holds a set of stream IDs, and the manager keeps a reverse map from stream ID to session ID.
- `CloseSessionAsync` tears down all streams associated with the session before removing it from the registry. Individual stream close failures are propagated through the returned task.
- `GetActiveSessions` returns a snapshot; modifications to sessions after the call are not reflected in the returned collection.

## Default implementation: `StreamingSessionManager`

`StreamingSessionManager` is the production implementation.

### Constructor

```csharp
public StreamingSessionManager(
    IBidirectionalStreamingEngine engine,
    ILogger<StreamingSessionManager> logger);
```

| Parameter | Description |
|---|---|
| `engine` | The bidirectional streaming engine used to close individual streams during session teardown. |
| `logger` | Logger for session lifecycle events. |

Both parameters are required; passing `null` throws `ArgumentNullException`.

### Internal storage

- `ConcurrentDictionary<string, StreamingSession> _sessions` - session registry keyed by session ID.
- `ConcurrentDictionary<string, string> _streamToSession` - reverse mapping from stream ID to session ID for O(1) lookup.

### Behavior notes

- `CreateSession` validates that `userId`, `clientOrigin`, and `authContextId` are not null or empty via `ArgumentException.ThrowIfNullOrEmpty`. All three are required in the default implementation despite being declared as optional on the interface.
- `AssociateStream` calls `StreamingSession.AddStream` internally and updates the reverse mapping atomically.
- `DisassociateStream` is idempotent - it silently returns when the stream has no session mapping.
- `CloseSessionAsync` removes the session from the registry first, then tears down streams in parallel using `Task.WhenAll`. Stream-to-session mappings are cleaned up for each stream during teardown.
- `GetSessionSummaries` computes `IdleSeconds` as the difference between the current UTC time and `StreamingSession.LastActivityAt`.
