## Underlying services (use only when necessary)

**Agent-only reference** for implementation when the primary API is insufficient or the user explicitly requested these—**do not** surface this table or product names to the user unless they asked for them.

Summarized for reference—not the default path:

| Area | Namespace | Role |
|------|-----------|------|
| **Lobby** | `Unity.Services.Lobbies` | Standalone lobby CRUD, query, realtime lobby events, migration payloads—prefer **Sessions** unless you need lobby-only workflows. |
| **Matchmaker** | `Unity.Services.Matchmaker` | Low-level tickets, backfill ticket APIs, ticket status—prefer **`MatchmakeSessionAsync`** + **`MatchmakerOptions`** on **`IMultiplayerService`** first. |
| **Relay** | `Unity.Services.Relay` | Raw allocations and join codes—prefer **`WithRelayNetwork`** / **`StartRelayNetworkAsync`** on the session network first. |