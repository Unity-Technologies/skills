## Table of Contents

- [Overview](#overview)
- [IMultiplayerService capabilities](#imultiplayerservice-capabilities)
- [Method signatures](#method-signatures)
- [Options reference](#options-reference)
- [Session configuration](#session-configuration-sessionoptions-joinsessionoptions-basesessionoptions)
- [Netcode with session options](#netcode-with-withnetwork-session-options)
- [Networking model](#networking-model-session-side)
- [Host / server session](#host--server-session-ihostsession-iserversession)
- [Matchmaking results](#matchmaking-results-on-a-session)
- [Errors and observation](#errors-and-observation)
- [Editor / glue](#editor--glue-unityservicesmultiplayercomponents)

## Overview

| Topic | Details |
|--------|---------|
| **Service access** | `MultiplayerService.Instance` (static) or `unityServices.GetMultiplayerService()` via `UnityServicesExtensions` after Services initialization. |
| **Core type** | **`ISession`** — session state, players, properties, role (host/member/server), **`Network`** (client), **`LeaveAsync`**, **`ReconnectAsync`**, **`RefreshAsync`**, **`SaveCurrentPlayerDataAsync`**; cast to **`IHostSession`** / **`IServerSession`** when hosting or dedicated server. |

### `IMultiplayerService` capabilities

| Area | What to use |
|------|-------------|
| **Session registry** | `Sessions` (read-only map); events `SessionAdded`, `SessionRemoved`, `AddingSessionStarted`, `AddingSessionFailed`. |
| **Create / join** | `CreateSessionAsync`, `CreateOrJoinSessionAsync`, `JoinSessionByIdAsync`, `JoinSessionByCodeAsync`, `ReconnectToSessionAsync`, `GetJoinedSessionIdsAsync`. |
| **Matchmaking into a session** | `MatchmakeSessionAsync` with **`QuickJoinOptions`** (filters, timeout, optional create) or **`MatchmakerOptions`** (queue, ticket attributes, player properties) + **`SessionOptions`**; optional `CancellationToken` on the `MatchmakerOptions` overload. |
| **Discovery** | `QuerySessionsAsync` + **`QuerySessionsOptions`** (filters, sort, skip/count, continuation token); **`QuerySessionsResults`** may **`StartPolling`** / **`StopPolling`**. |

### Method signatures

```csharp
// Creates a new session. Returns IHostSession (host-side). Throws SessionException on failure.
Task<IHostSession> CreateSessionAsync(SessionOptions sessionOptions)

// Joins session by ID, or creates it if it does not exist.
Task<ISession> CreateOrJoinSessionAsync(string sessionId, SessionOptions sessionOptions)

// Joins an existing session by its ID.
Task<ISession> JoinSessionByIdAsync(string sessionId, JoinSessionOptions sessionOptions = default)

// Joins an existing session via a human-readable join code.
Task<ISession> JoinSessionByCodeAsync(string sessionCode, JoinSessionOptions sessionOptions = default)

// Reconnects to a previously joined session after a disconnect.
Task<ISession> ReconnectToSessionAsync(string sessionId, ReconnectSessionOptions options = default)

// Finds and joins a session using Unity's matchmaker service. Supports cancellation.
Task<ISession> MatchmakeSessionAsync(MatchmakerOptions matchOptions, SessionOptions sessionOptions, CancellationToken cancellationToken = default)

// Finds a session using session filters with retries up to a timeout. Can optionally create a session if none is found.
Task<ISession> MatchmakeSessionAsync(QuickJoinOptions quickJoinOptions, SessionOptions sessionOptions)

// Browses available sessions matching the provided query options.
Task<QuerySessionsResults> QuerySessionsAsync(QuerySessionsOptions queryOptions)

// Returns IDs of all sessions the current player is already part of.
Task<List<string>> GetJoinedSessionIdsAsync()
```

### Options reference

#### `SessionOptions` _(create / create-or-join / matchmake)_

Inherits `BaseSessionOptions`.

| Property | Type | Default | Description |
|---|---|---|---|
| `Name` | `string` | new GUID | Session display name |
| `MaxPlayers` | `int` | `0` | Max players including host. Must be > 0 when creating |
| `IsLocked` | `bool` | `false` | Locked sessions reject new joins |
| `IsPrivate` | `bool` | `false` | Private sessions are hidden from queries and quick-join |
| `Password` | `string` | `null` | 8–64 char password required to join. Not readable back from the session |
| `SessionProperties` | `Dictionary<string, SessionProperty>` | empty | Custom game-specific session properties (e.g. `"map"`). Up to 20 total |
| `Type` | `string` _(from base)_ | new GUID | Client-side key identifying the session type |
| `PlayerProperties` | `Dictionary<string, PlayerProperty>` _(from base)_ | empty | Per-player properties (e.g. `"role"`). Up to 10 per player |

Fluent extensions (on `SessionOptionsExtensions`): `.WithRelayNetwork()`, `.WithDirectNetwork()`, `.WithDistributedAuthorityNetwork()`, `.WithNetworkHandler()`, `.WithHostMigration()`, `.WithPlayerName()`

---

#### `JoinSessionOptions` _(join by ID / join by code / quick-join fallback)_

Inherits `BaseSessionOptions`.

| Property | Type | Default | Description |
|---|---|---|---|
| `Password` | `string` | `null` | Password required if the session is password-protected |
| `Type` | `string` _(from base)_ | new GUID | Client-side session type key |
| `PlayerProperties` | `Dictionary<string, PlayerProperty>` _(from base)_ | empty | Per-player properties |

---

#### `ReconnectSessionOptions` _(reconnect)_

| Property | Type | Default | Description |
|---|---|---|---|
| `Type` | `string` | new GUID | Client-side session type key |

Fluent: `.WithNetworkHandler(INetworkHandler)` — disables default NGO/NfE integration.

---

#### `MatchmakerOptions` _(matchmake via Unity Matchmaker)_

| Property | Type | Default | Description |
|---|---|---|---|
| `QueueName` | `string` | `null` | Name of the Matchmaker queue |
| `TicketAttributes` | `Dictionary<string, object>` | empty | Attributes sent with the matchmaking ticket |
| `PlayerProperties` | `Dictionary<string, PlayerProperty>` | empty | Per-player properties forwarded to matchmaker |

---

#### `QuickJoinOptions` _(matchmake via filters)_

| Property | Type | Default | Description |
|---|---|---|---|
| `Filters` | `List<FilterOption>` | empty | Filters a session must satisfy to be joined |
| `Timeout` | `TimeSpan` | default | How long to retry before giving up and optionally creating a new session |
| `CreateSession` | `bool` | `false` | Create a new session if none is found within the timeout |

> Do not set `Timeout` unless explicitly requested.

---

#### `QuerySessionsOptions` _(query)_

| Property | Type | Default | Description |
|---|---|---|---|
| `Count` | `int` | `100` | Max results to return |
| `Skip` | `int` | `0` | Pagination offset |
| `FilterOptions` | `List<FilterOption>` | empty | Filters to narrow results |
| `SortOptions` | `List<SortOption>` | empty | Sort order for results |
| `ContinuationToken` | `string` | `null` | Token for fetching the next page |

---

#### `FilterOption` _(used in `QuickJoinOptions` and `QuerySessionsOptions`)_

Constructor: `FilterOption(FilterField field, string value, FilterOperation operation)`

| Enum | Values |
|------|--------|
| **`FilterField`** | `MaxPlayers`, `AvailableSlots`, `Name`, `Created` (RFC3339), `LastUpdated` (RFC3339), `IsLocked`, `HasPassword`, `StringIndex1–5`, `NumberIndex1–5` |
| **`FilterOperation`** | `Contains` _(Name only)_, `Equal`, `NotEqual`, `Less`, `LessOrEqual`, `Greater`, `GreaterOrEqual` |

---

#### `SortOption` _(used in `QuerySessionsOptions`)_

Constructor: `SortOption(SortOrder order, SortField field)`

| Enum | Values |
|------|--------|
| **`SortOrder`** | `Ascending`, `Descending` |
| **`SortField`** | `Name`, `MaxPlayers`, `AvailableSlots`, `CreationTime`, `LastUpdated`, `Id`, `StringIndex1–5`, `NumberIndex1–5` |

---

#### `AddingSessionOptions` _(event payload — read-only)_

| Property | Type | Description |
|---|---|---|
| `Type` | `string` | The session type passed to the create/join call |

---

### Session configuration (`SessionOptions`, `JoinSessionOptions`, `BaseSessionOptions`)

| Topic | Details |
|--------|---------|
| **Lobby-like fields** | Max players, name, password, locked/private flags, typed **`Type`**, **session** and **player** properties with **`VisibilityPropertyOptions`** (Public / Member / Private) and indexed slots (**`PropertyIndex`**) for query filters. |
| **Networking** (`SessionOptionsExtensions`) | **`WithRelayNetwork`**, **`WithDirectNetwork`** (listen/publish IP/port or **`DirectNetworkOptions`**), **`WithNetworkOptions`** (e.g. **`RelayProtocol`**), **`WithNetworkHandler`** for custom **`INetworkHandler`**. |
| **Host migration** | **`WithHostMigration`** + **`IMigrationDataHandler`**; on **`IHostSession`**: **`GetHostMigrationDataAsync`** / **`SetHostMigrationDataAsync`**. |
| **Player name** | **`WithPlayerName`** (visibility). |
| **Matchmaker backfill** | **`MatchmakerServerExtensions.WithBackfillingConfiguration`** on **`SessionOptions`**; on matchmade **`ISession`**: **`StartBackfillingAsync`** / **`StopBackfillingAsync`**. See **`llms.txt`** and package docs for hosting constraints. |

### Netcode with `With*Network*` session options

| Condition | Behavior |
|-----------|----------|
| **`SessionOptionsExtensions`** include gameplay networking (**`WithRelayNetwork`**, **`WithDirectNetwork`**, **`WithNetworkOptions`**, **`WithNetworkHandler`**, …) | **create / join / matchmake / reconnect** bring up **NGO** or **NFE** as configured—no separate Netcode start for that path. |

### Networking model (session side)

| Surface | Role |
|---------|------|
| **`IHostSessionNetwork`** | **`StartDirectNetworkAsync`**, **`StartRelayNetworkAsync`**, **`StopNetworkAsync`**; state and failure events; **`INetworkHandler`**. |
| **`IClientSessionNetwork`** | Client **`NetworkState`** and events; **`NetworkHandler`**. |
| **`NetworkConfiguration`** | UTP endpoints and Relay server data; **`NetworkType`**: Direct, Relay, **DistributedAuthority**; **`NetworkRole`**: Client, Server, Host. |

### Matchmaking results on a session

| API | Use when |
|-----|----------|
| **`MatchmakerExtensions.GetMatchmakingResults(ISession)`** | Stored matchmaking results are needed after a matchmade session exists. |

### Errors and observation

All async methods on **`IMultiplayerService`** throw **`SessionException`** on failure; **`SessionException`** exposes a specific session error type and message.

| Type | Role |
|------|------|
| **`SessionException`** / **`SessionError`** | Session and composed flows. |
| **`SessionObserver`** | Watch add/fail events for a given session **type**. |

### Editor / glue (`Unity.Services.Multiplayer.Components`)

| Item | Role |
|------|------|
| **`MultiplayerSession`** (ScriptableObject) | Holds **`ISession`** and UnityEvent groups (lifecycle, session, players). |
| **`SessionConnector`** / **`SessionConnectorBehaviour`** | Create or create-or-join flows (e.g. on sign-in). |
