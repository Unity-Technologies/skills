# Session connectors

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

The session authoring components run the create and join flows without session code: a `MultiplayerSession`
asset holds the live session, a `SessionConnector` asset performs one operation (`Create`, `Join` by code or
id, or `CreateOrJoin`), and a `SessionConnectorBehaviour` runs it when the player signs in or when the game
calls `Execute()`. Read [Create sessions in the Editor](https://docs.unity.com/en-us/mps-sdk/scriptableobject-sessions.md)
before setting them up.

Use the Session API instead when any of these holds:

- The stack is GhostObject (see [Network](#network)).
- Step 3 chose distributed authority or host migration, because a connector starts only a None, Direct or Relay
  network and sets no host migration.
- The flow has no gameplay netcode (a lobby or a metadata-only session), because a connector's network starts
  netcode.
- The user asked for code.
- The project's session code already runs the flow, because a connector next to that code creates or joins a
  second session.

## Contents

- [Set up](#set-up)
- [Wire the UI](#wire-the-ui)
- [Network](#network)
- [Dedicated server](#dedicated-server)
- [Verify](#verify)

## Set up

On the main Editor, in Edit Mode. List each group first (`unity command services.core`, `services.authentication`,
`services.multiplayer`), because the commands depend on the installed package versions. Search the project's
code first: skip `add_services_initialization_component` when it already calls `UnityServices.InitializeAsync`,
and `add_player_authentication_component` when it already signs the player in (an `AuthenticationService` sign-in
call, or a `PlayerAuthentication` component in a scene), because signing in twice fails:

```bash
unity command services.core add_services_initialization_component            # initializes Unity Services in Play Mode
unity command services.authentication add_player_authentication_component    # signs the player in anonymously
unity command services.multiplayer create_session_handle --path Assets/Multiplayer/Session
unity command services.multiplayer create_session_connector --path Assets/Multiplayer/Host --connector-type Create --multiplayer-session Assets/Multiplayer/Session
unity command services.multiplayer create_session_connector --path Assets/Multiplayer/Join --connector-type Join --multiplayer-session Assets/Multiplayer/Session
unity command services.multiplayer configure_session_connector --path Assets/Multiplayer/Host --network Relay   # saves the asset
unity command services.multiplayer add_session_connector_behaviour --game-object "Host Game" --session-connector Assets/Multiplayer/Host
unity command services.multiplayer add_session_connector_behaviour --game-object "Join Game" --session-connector Assets/Multiplayer/Join
unity command scene save                                                      # the two component commands leave the scene unsaved
```

`create_session_connector` always makes a new asset, so a second run on the same path adds `Host 1.asset`.
Change an existing connector with `configure_session_connector`, which sets only the arguments given. A
`CreateOrJoin` connector needs `configure_session_connector --session-id <id>`. Never give a Join connector a
`--join-code`, because the game sets the code at join time (see [Wire the UI](#wire-the-ui)).

If a group lacks its command, the installed package predates it:

- No `add_services_initialization_component` or `add_player_authentication_component`: add
  `Unity.Services.Core.Components.ServicesInitialization` and
  `Unity.Services.Authentication.Components.PlayerAuthentication` with `component add`, set the latter's
  `SignInAnonymously` to `true` with `component set_field` (it defaults to `false`), then `scene save`.
- No `configure_session_connector`: set the network as [Network](#network) says. Use the Session API for a
  `CreateOrJoin` flow, because its session id then has no command.
- No `create_session_connector`: use the Session API instead (step 4 of the skill), because the connector's
  settings live on an asset and `component set_field` does not write assets to disk, so Multiplayer Play Mode
  clones would not see them.

## Wire the UI

- Leave **Execute On Sign In** off for anything a button starts, and call the behaviour's `Execute()` from the
  button: a UGUI `Button.onClick` entry in the Inspector, or `button.clicked += hostBehaviour.Execute;` in the
  UI Toolkit screen's controller.
- Join by code: set the code on the connector right before every `Execute()`, with
  `joinBehaviour.SessionConnector.WithJoinByCode(codeField.value)`. Never rely on a code already on the asset,
  because a connector keeps its settings when Play Mode exits, so it holds whatever code was set last.
- React to the result through the connector's `SuccessfulExecution` (the `ISession`: show `Code` to the host,
  switch screens) and `FailedExecution` (the error message), in the Inspector or in code.
- Turn **Execute On Sign In** on only for a flow with no button, such as a `CreateOrJoin` connector that puts
  every tester in one session.

## Network

A connector's network starts the netcode stack through the default network handler, so use connectors only
where step 4 of the skill gives start-up to the default handler (NGO, direct ECS ghosts). A GhostObject game
uses the Session API.

A new connector's network is Direct on `127.0.0.1`: it connects Multiplayer Play Mode instances on one machine
and nothing else. For players on different networks, configure the Host connector with `--network Relay`, as
[Set up](#set-up) does. Relay also connects Multiplayer Play Mode instances, so keep it for local testing.

When the group has no `configure_session_connector`, set the network with
`component set_field --target Assets/Multiplayer/Host.asset --field m_SessionNetworkSettings.m_Network --value '"Relay"'`,
then save it with `unity command script eval --code "UnityEditor.AssetDatabase.SaveAssets(); return true;"`,
because `set_field` leaves the asset changed only in memory.

## Dedicated server

The server counterpart creates the session when a dedicated server starts: a `ServerSessionConnector` asset
creates it, publishes the connector's session properties (for example the ones clients quick-join against) and
starts its Direct or Relay network, and a `ServerSessionConnectorBehaviour` runs it once the server is
authorized. It only creates sessions; backfill and sessions allocated by the Matchmaker stay on
`MultiplayerServerService` ([dgs-entrypoint.md](dgs-entrypoint.md)).

```bash
unity command services.multiplayer create_session_handle --path Assets/Multiplayer/ServerSession
unity command services.multiplayer create_server_session_connector --path Assets/Multiplayer/Server --multiplayer-session Assets/Multiplayer/ServerSession
unity command services.multiplayer add_server_session_connector_behaviour --game-object "Server Session" --session-connector Assets/Multiplayer/Server   # runs on authorization by default
```

- The behaviour never signs the server in. Sign in from server code, with `SignInFromServerAsync` on a hosted
  server, or `SignInWithServiceAccountAsync` with a service account allowed to create sessions on a local or CI
  server. Read the key and secret from environment variables that the host or CI secret store sets, never
  from command-line arguments, because shell history and process listings expose those. Never put them in a
  serialized asset or in source, because those ship in builds.
- It acts only where the multiplayer role includes Server: a Dedicated Server build, or an instance Multiplayer
  Roles doesn't mark Client only. Without the Multiplayer Roles API (Unity 6000.5 or older without the
  Dedicated Server package) every instance acts, so keep the behaviour out of client scenes there.
- A new server connector's Direct network listens on all interfaces and publishes this machine's first LAN
  address, which reaches the local network only. For internet hosting, set the publish address to the server's
  public or allocated address in the connector's network settings.
- To accept players only once the game server is ready, set the connector's network type to None and call the
  behaviour's `StartNetwork()` when ready (`StopNetwork()` to close it).
- Clients find the session through the Session API (quick join filters on the published properties) or a Join
  connector with the session id.

## Verify

Verify in Multiplayer Play Mode per [multiplayer-play-mode.md](multiplayer-play-mode.md): a game whose flows run
on connectors needs no test hook class. `run_session_connector` runs a behaviour's connector on an instance in
Play Mode and returns the session. Its `--join-code`, `--join-session-id` and `--session-id` are set on the
connector first, as a button sets them, so pass the code on every join run. It raises the connector's
`SuccessfulExecution` and `FailedExecution`, so the game's own listeners react as they would to a button.
