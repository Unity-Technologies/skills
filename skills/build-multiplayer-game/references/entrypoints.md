# Session API entrypoints

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

Implement session flows against `MultiplayerService.Instance` and `ISession` (cast to `IHostSession` when hosting). Read the page for the call you're writing before using it, because defaults and limits change between package versions:

- [Create a session](https://docs.unity.com/en-us/mps-sdk/create-session.md) and [session configuration options](https://docs.unity.com/en-us/mps-sdk/config-options.md): `SessionOptions`, properties, visibility, indexed properties for queries.
- [Find and join a session](https://docs.unity.com/en-us/mps-sdk/join-session.md): join by id or code, `QuerySessionsAsync` filters and sorting, reconnect.
- [Matchmaking](https://docs.unity.com/en-us/mps-sdk/matchmake-session.md) and [Quick Join](https://docs.unity.com/en-us/mps-sdk/matchmake-lobby.md): `MatchmakerOptions`, `QuickJoinOptions`.
- [Session networking](https://docs.unity.com/en-us/mps-sdk/session-networking.md) and [manage the session network connection](https://docs.unity.com/en-us/mps-sdk/manage-session-network-connection.md): `WithRelayNetwork`, `WithDirectNetwork`, starting the network later from the host, custom `INetworkHandler`.
- [Host migration](https://docs.unity.com/en-us/mps-sdk/session-host-migration.md), [Session observer](https://docs.unity.com/en-us/mps-sdk/session-observer.md), [ScriptableObject sessions](https://docs.unity.com/en-us/mps-sdk/scriptableobject-sessions.md) (`MultiplayerSession`, `SessionConnector`), and [error messages](https://docs.unity.com/en-us/mps-sdk/lobby-error-messages.md) (`SessionException`, `SessionError`).
- The [Multiplayer Services scripting API](https://docs.unity3d.com/Packages/com.unity.services.multiplayer@<VERSION>) for exact signatures.

Rules the docs don't give, or that models get wrong:

- Set `SessionOptions.MaxPlayers` above 0 when creating a session, because the default of 0 is rejected.
- Leave `QuickJoinOptions.Timeout` unset unless the user asked for one, because an unrequested timeout changes how long quick join retries.
- Use `WithDistributedAuthorityNetwork()` only when the chosen stack is NGO and the topology is distributed authority, because distributed authority exists only in NGO.
- `WithNetworkHandler(INetworkHandler)` replaces the default netcode integration, so the custom handler owns netcode start-up itself.
- Who starts netcode with the default handler is decided in SKILL step 4; world creation and connect details are in [netcode-lifecycle.md](netcode-lifecycle.md).
