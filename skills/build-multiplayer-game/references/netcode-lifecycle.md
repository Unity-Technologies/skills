# Game sessions and lifecycle

## Contents

- [Start and connect](#start-and-connect)
- [GameObject-layer rules](#gameobject-layer-rules)
- [World creation and transport driver selection](#world-creation-and-transport-driver-selection)
- [Prefabs and spawning](#prefabs-and-spawning)
- [Scene and content state](#scene-and-content-state)
- [Canonical game-session state](#canonical-game-session-state)
- [Join, drop, leave, and rejoin](#join-drop-leave-and-rejoin)
- [Failure triage](#failure-triage)
- [Error strings](#error-strings)
- [Unhappy-path tests](#unhappy-path-tests)
- [Proving a build is current](#proving-a-build-is-current)

This reference covers the handoff from a connection (a Multiplayer Services session
or a direct connect) to gameplay and the join/drop/rejoin lifecycle. Creating the session, matchmaking, and hosting come
first; the session step of the workflow covers them.

Identity splits three ways. Account identity (sign-in, authentication) belongs to
`build-live-game`. Session membership and choices made before the game session exists,
carried as session player properties, belong to the session layer. The slot and team inside the
game session, and anything derived from them, belong here.

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

## Start and connect

For the GameObject ghost layer, the static `Netcode` class starts, inspects and stops
game sessions. Read the Netcode `ghostobject/ghostobject-connections` page for its
connect, listen, host, role, connection-event and shutdown APIs before writing that
code. `Netcode.StartAsHost` also takes an optional host-world-mode argument that the
page does not show; confirm its signature in the installed source before passing it.

When the session's default network handler (`WithRelayNetwork`/`WithDirectNetwork`) already starts Netcode, the session
creates the worlds and connects: skip the `Netcode` start calls and the automatic
bootstrap's world, which by default already listens (and, in binary worlds,
self-connects) on `ClientServerBootstrap.AutoConnectPort` (7979). Otherwise start
Netcode here.

Single world is the default host mode for `Netcode.StartAsHost`, and the only one
GhostObjects support (Netcode `ghostobject/ghostobject-connections` page); pass
`NetcodeConfig.HostWorldMode.BinaryWorlds` only for direct ECS ghosts. A host started
by the session's default network handler instead reuses
any bootstrap-created worlds, or creates separate client and server worlds when none
exist. So for GhostObjects, start the host with `Netcode.StartAsHost` and use the
session only for membership and connection details. To host or join a GhostObject game
over Relay, assign a Relay `INetworkStreamDriverConstructor` to
`NetworkStreamReceiveSystem.DriverConstructor` before calling `StartAsHost` or `Connect`
(Netcode `networking-using-relay` page). For direct ECS ghosts, disable the
automatic bootstrap before starting the session and treat that host as binary worlds.

The connection defaults are tuned for tolerance, not for a join screen:
`MaxConnectAttempts: 60` with `ConnectTimeoutMS: 1000` on the generated
`NetcodeConfig.asset` means a mistyped or dead direct-IP address takes about a
minute to fail. Any game with a manual IP entry field should lower the attempt
count, and should verify the resulting failure time.

Ghost replication starts on its own: `NetcodeConfig.AutomaticStateReplication`
defaults to true, so Netcode adds `NetworkStreamInGame` on both sides once a
connection is fully connected. Don't add `NetworkStreamInGame` yourself, because
Netcode already adds it. To hold replication back until
a peer is approved or its content is ready, follow *Control ghost replication* on the
Netcode `ghostobject/ghostobject-connections` page, and enable replication from one
place that both the host-local path and the remote path reach, so neither side is
missed.

For direct ECS ghosts, read the Netcode `client-server-worlds` and
`network-connection` pages for creating worlds and requesting listen or connect
before writing that code.

Every process in a game session (host, server, or client) must run with
`Application.runInBackground` enabled, or its simulation stalls on focus loss and
peers time out; a stalled host freezes every client. Check it rather than assuming
the template's value. Set it as described in `https://docs.unity3d.com/Packages/com.unity.netcode@<VERSION>/manual/network-connection.html`
(replace `<VERSION>` with the `com.unity.netcode` major.minor from
`Packages/packages-lock.json`, or read the package's `Documentation~/network-connection.md`
when that version is not published)
(`runInBackground: 1` in `ProjectSettings/ProjectSettings.asset` is the project-wide
form). Netcode's `WarnAboutApplicationRunInBackground` system logs an error when it
is disabled; suppress it by setting `SuppressApplicationRunInBackgroundWarning` on each
world's `NetDebug` singleton (an instance property, not a static) only when stalling on focus loss is a deliberate, handled choice (e.g. mobile, where
the OS suspends the app regardless and the reconnect flow must cope anyway).

Recommended order:

1. Resolve packages and confirm the APIs you will use in the installed source before writing gameplay. Enable
   Run in Background project-wide (or decide and document how focus loss is
   handled).
2. Complete the session step first: initialize identity/services, create or join
   the session, and acquire connection details.
3. Register ghost prefabs on client and server (order matters only when
   `GhostPrefabLoadingTimeout` is 0; see *Prefabs and spawning*).
4. Create the selected client/server/host worlds.
5. Bind the transport, listen/connect, and approve the peer before gameplay spawn
   and input (replication itself starts automatically unless
   `AutomaticStateReplication` is off).
6. Assign each joining participant's slot and derived identity on the authority
   and replicate it.
7. Synchronize scene/content state, then spawn the player and begin simulation.

## GameObject-layer rules

- **Bootstrap exclusivity.** Automatic world creation and manual `StartAsHost`/`Connect` are alternatives; combining them makes `IsServer` true before any game session exists.
- **Game session first, ghost state second.** Never access `GhostField<T>.Value` when no game session or Netcode world is running, and write it only where `CanWriteState` is true. (On a client, `Awake` already sees replicated values; on the server it sees prefab values — Netcode `ghostobject/ghostobject-replicate` page.) Gate per-frame simulation on game-session-active and ghost-linked state.
- **Shut down symmetrically.** Call `Netcode.Shutdown()` on leave, host loss and `OnDestroy`. Repeated identical per-frame exceptions mean a loop is not gated on game-session state.

Writing ghost state through the contract (`PredictionUpdate`, `CanWriteState`, `PredictionGatherInput`) and writing predicted values into restored storage are covered in *GhostObject state*, *Input* and *Prediction and replay* in [syncing-gameplay.md](syncing-gameplay.md).

## World creation and transport driver selection

Two defaults interact to break every "join a remote host" flow in a
default-configured build:

1. The automatic `ClientServerBootstrap` creates a host world (single world, the
   default) or a ServerWorld and a ClientWorld (binary worlds) at startup whenever `RequestedPlayType` is `ClientAndServer` —
   which is every normal (non-client-only, non-server-only) build, including
   instances that will only ever join. Under single world, a connect attempt from
   that host world throws `You cannot call Connect on a NetworkStreamDriver in a
   server or host world.`
2. Under binary worlds, the default `IPCAndSocketDriverConstructor` selects the client driver when
   the client world is created and again on each `Connect`: if `RequestedPlayType` is
   not Client, the network simulator is off, and a non-host ServerWorld exists in the
   same process, it registers the **IPC** driver (in-process, loopback-only) instead
   of UDP. A remote address passed to `Netcode.Connect` is then silently rewritten to
   loopback. This surfaces only as a warning in Player.log — never as an error or a
   failed return value — so the client appears to connect (or times out) identically
   for every address typed in.

Rule: **a join-only client must have no server or host world in its process when it
connects.** The auto-created server or host world serves no purpose for it, and by
default it listens on `AutoConnectPort` (7979): `Netcode.Connect`
and `Netcode.StartAsHost` both lazily create the worlds they need
(`Runtime/GameObjectLayer/Netcode.cs`), so hosting still works without it.

Mitigations, in preference order. Read the named page before applying one, and again
if it fails:

1. A custom `ClientServerBootstrap` that creates only the local world and sets
   `AutoConnectPort` to 0, so nothing connects before the player picks a role
   (*Customize the bootstrapping flow* on the Netcode `client-server-worlds` page, and
   the Netcode `network-connection` page). The `Netcode` facade then creates
   client/server worlds on demand for host and join. Wrong variant: returning true
   from `Initialize` without creating any world fails an Entities assertion
   (`World.DefaultGameObjectInjectionWorld` must be set).
2. Disable the automatic bootstrap on the `NetcodeConfig`, or for one scene with
   `OverrideAutomaticNetcodeBootstrap` (*Override bootstrapping for a single scene* on
   the Netcode `client-server-worlds` page), then create worlds explicitly.
3. Build join-only clients with the client-only Netcode Client Target (Netcode
   `project-settings` page), which creates only a ClientWorld.
4. Dispose the unused auto-created server or host world before the first client connect.

Verify the selected transport from the client's log after connecting (Player.log,
or a virtual player's console). The driver-registration lines
(`[DefaultDriverConstructor.RegisterClientUdpDriver]` versus `RegisterClientIpcDriver`)
are Debug-level, below Netcode's default log level, so set the log level to Debug
first (read the Netcode `testing/logging` page for how); otherwise their absence proves nothing. The warning `IPC
interfaces only support loopback address` appears at the default level and is the
smoking gun that the remote address was discarded.

## Prefabs and spawning

`GhostObject` prefabs register automatically when their asset loads. For addressable
or dynamically loaded content, or to change GhostObject settings from code first,
follow *Register a prefab yourself* on the Netcode `ghostobject/ghostobject-replicate`
page. Enable **Skip Automatic Prefab Registration** on the prefab asset in the
Inspector, because the value is copied at import and setting it at runtime does
nothing.

Prefab order between client and server matters only when
`NetcodeConfig.GhostPrefabLoadingTimeout` is 0; at the default (-1) or a positive
value, clients load prefabs in any order. The options are under *Control ghost
replication* on the Netcode `ghostobject/ghostobject-connections` page.

Spawn authoritatively with ordinary `Object.Instantiate(registeredPrefab)`; remote
replicas come from snapshots. For a client predicted spawn, follow *Spawn an object
from prediction* on the Netcode `ghostobject/ghostobject-replicate` page, and also
check the connection is in game and the ghost collection is initialized, because
rollback otherwise creates duplicates. Destroy the GameObject, not its linked entity.

High-churn gameplay should be designed carefully: a deterministic projectile is
often a spawn event plus local flight and an authoritative hit, rather than a
networked object for every frame of travel. On a single-world host that pattern
needs one extra guard — the host receives its own spawn event for an object it
already owns authoritatively, so gate the local derivation on `IsServer`. See the
note under *Common object plan* in [syncing-gameplay.md](syncing-gameplay.md).

## Scene and content state

Every peer must load compatible content before simulation. A mismatched ghost
registry, prefab layout, or build version can reject a connection or desynchronize
serialization.

Direct ECS ghosts have established SubScene, scene-section, and prespawn flows.
The GameObject layer has no scene-management API equivalent to NGO's
`NetworkSceneManager` (no `Netcode.SceneManager`), and a `GhostObject` authored
straight into a scene without a prefab is rejected. Read the installed package's
`Documentation~/ghostobject/` pages before using any scene flow.

For a GhostObject project:

- prefer a persistent bootstrap scene and dynamically registered/spawned gameplay
  prefabs;
- coordinate scene transitions through explicit authoritative state plus a
  tested local loading flow;
- gate simulation until every required peer reports compatible content and ready
  state;
- use direct ECS SubScenes for network scene content when that workflow fits;
- do not claim late-join scene correctness until it has been tested in builds.

## Canonical game-session state

Model match flow as compact replicated state: phase enum, authoritative end tick,
participants (each with its authority-assigned slot, team, and whatever identity the
game derives from the slot), score, and ownership. Late joiners receive current state
rather than replaying old events. Keep collections bounded and transfer data that
outlives a game session to a backend. Lifecycle transitions that lock or unlock
participant control (countdown end, match end, respawn) are part of that state and are applied on every
client from the replicated value, never from a local timer or a host-only callback.

Maintain one writer for each state value. On a single-world host both role checks
are true, with the consequences listed under *Single-world details* in
[architecture.md](architecture.md).

- `IsClient` does not imply non-authoritative execution on a single-world host.

## Join, drop, leave, and rejoin

**Join**

- Validate build/content version, capacity, ticket, and rules before the joiner's
  gameplay spawn and input (and before enabling ghost replication, if
  `AutomaticStateReplication` is off).
- Assign each participant's slot and derived identity on the authority as part
  of approval, and replicate it before enabling input. One connection may carry
  several participants (a shared-screen lobby joining a remote host), so the
  join request carries the participant count and each participant's local
  choices (character, vehicle, name; one session player may carry several
  participants' choices); the authority allocates one slot per participant,
  confirms or substitutes each choice, renumbers any locally numbered
  participants, re-derives what depends on the slot (color, spawn, HUD
  position), and clients render what was replicated.
- Load compatible content, synchronize state, spawn the player, then enable input.
- Make initialization idempotent; repeated connection notifications must not grant
  duplicate avatars or inventory.

**Leave versus drop**

- A deliberate leave releases the slot immediately.
- A timeout usually preserves player state for a bounded grace period.
- Record enough identity outside the transient connection entity to match a
  reconnecting player to held state.

**Owned objects**

For every category choose transfer, despawn, or freeze for the grace period. Never
leave ownership behavior implicit. A player avatar usually despawns or freezes;
shared vehicles/structures usually transfer to authority.

**Rejoin**

- Restore state rather than granting new state.
- Make repeated reconnect attempts idempotent.
- Expire held state and slots predictably after the grace period.

**Host loss**

- A listen server ends when its host leaves unless host migration is designed.
- Host migration is experimental. If a page asks for a scripting define, add it only
  if the installed source checks that define, because a define nothing checks changes
  nothing. It requires
  service-side host election/state storage plus a new Relay allocation and client
  reconnection, with auto connect disabled. Read the Netcode
  `host-migration/host-migration` page and its subpages first, and confirm in the
  installed source that it works with your host mode before promising it to a
  GhostObject (single-world) project. Verify the installed package's data-size/region/platform limits,
  child-ghost and prespawn behavior, detection time, allocation failure, and known
  migration defects before promising continuity.
- Single-world host is not automatically equivalent to binary-world migration.
- Dedicated authority avoids player-host migration.

## Failure triage

Check in this order:

1. Editor and resolved package version; the GhostObject-layer APIs confirmed in its source.
2. Compile errors from bridge source generation.
3. Client/server build and ghost registration mismatch.
4. Transport bound before connect/listen and service credentials still valid.
5. Client "connects" to itself, or the connect behaves identically for every
   address: check the client's log for `IPC interfaces only support loopback` — an
   in-process ServerWorld (usually the auto-created bootstrap world) forced the
   IPC driver and the remote address was never dialed. See
   [World creation and transport driver selection](#world-creation-and-transport-driver-selection).
6. Host shows doubled ghosts or a trailing duplicate while clients look correct.
   Two causes, one per host mode: in **binary worlds** the authoritative instance
   and its client-world replica coexist and presentation isn't gated to `IsClient`
   instances; in **single world** there is no replica, so the duplicate is
   application-created by a handler that runs because both roles are true. Causes,
   fixes, and the `IsServer` rule: [architecture.md](architecture.md).
   Check every `[RPC(SendDirection.ServerToClient)]` handler, `Netcode.RPC.Query<T>`
   consumer and event callback for an unguarded local spawn or state change.
7. With `NetcodeConfig.AutomaticStateReplication` off: connection approved but
   `EnableGhostReplication(true)` never called on one side (the client's or the
   server's connection).
8. World/host mode differs from the assumed architecture.
9. Content not ready before player spawn.
10. Timeout/drop interpreted as a deliberate leave.
11. NGO project with Unity Netcode's automatic bootstrap still on. NGO 3.x
    installs `com.unity.netcode`, whose bootstrap can open port 7979 in every
    process and break an NGO join. `netcode list_worlds` returning worlds in an
    NGO-only project means it is running; turn it off with `netcode.config set`.
12. Static state left from an earlier Play session. With domain reload disabled
    (Enter Play Mode Options), `NetworkStreamReceiveSystem.DriverConstructor` and
    `ClientServerBootstrap.AutoConnectPort` keep values set in a previous session: a
    Multiplayer Services session leaves its Relay driver constructor behind, so a
    later direct host accepts no connections and reports nothing. Set
    `DriverConstructor` to `null` (the default) and restore `AutoConnectPort`, or
    restart the Editor.

## Error strings

| Exact string | Root cause | Resolution |
|---|---|---|
| `You cannot call Connect on a NetworkStreamDriver in a server or host world.` | A join-only client's automatic bootstrap created a server or host world | Turn the automatic bootstrap off, or use a client-only bootstrap or build target ([World creation and transport driver selection](#world-creation-and-transport-driver-selection)) |
| `IPC interfaces only support loopback address` (warning) | An in-process server world forced the IPC driver, so the remote address was rewritten to loopback | As above; confirm the socket driver at Debug log level |
| `Failed to bind UDP socket because the address is already in use` | Another process already listens on the port (7979): two `ClientAndServer` instances, or Unity Netcode's bootstrap in an NGO project | Turn that bootstrap off in NGO projects; start Unity Netcode roles explicitly with `Netcode.StartAsHost` and `Netcode.Connect` |
| `Listen is not allowed if playmode is set to Client` | Play Mode Type is `Client`; the main Editor and its clones share it | `netcode.playmode set --type ClientAndServer`, then start the roles explicitly |
| `'<name>' has a GhostObject but was authored directly into a scene. Ghosts must be prefabs.` | A GhostObject placed in a scene, not as a prefab instance | Make it a prefab and instantiate it at runtime |
| `SessionException: Starting network failed` with `AssertionException: Should only be called from standalone clients` | The default network handler ran on a single-world host world | GhostObject: start the host with `Netcode.StartAsHost`. Direct ECS: turn the automatic bootstrap off and set `AutoConnectPort` to 0 |
| `There are still connections present` … `Cannot assign the NetworkDriverStore` | The handler reused a client world that had already auto-connected | Set `ClientServerBootstrap.AutoConnectPort` to 0 before the session starts |
| `error CS0103: The name '<Type>' does not exist in the current context` (from `script eval`) | An unqualified type outside `UnityEngine` | Fully qualify the type, or add a `using` line |
| `error CS0234: The type or namespace name '<member>' does not exist in the namespace '<Project>.Netcode'` | A namespace segment named `Netcode` hides the `Netcode` facade | Rename that namespace segment |

## Unhappy-path tests

Run these in Multiplayer Play Mode, with the main Editor and its clones as the
processes, unless a test says final testing (see the verification
gate's *Final testing* section). Use standalone players only when MPPM
is unavailable.

1. Kill a client process and reconnect during and after the grace period.
2. Kill the host and verify the promised end/migration behavior.
3. Join during active gameplay and verify complete world state.
4. Rejoin twice and confirm one avatar/state grant.
5. Final testing: connect a deliberately mismatched build and show a clear rejection.
6. Load/transition content with a slow client.
7. Join from a second process on a **non-loopback** address and confirm from both
   processes' logs that the socket driver was selected. In-process host+client
   rides the IPC path and proves nothing about real network connectivity. A
   Multiplayer Play Mode virtual player is a separate process, so a join from it
   to the main Editor over relay or the machine's own LAN address takes the
   socket driver and meets this bar with no extra hardware, scriptably. A join
   from a second machine belongs to final testing.
8. Compare the host's rendered view against a client's during active play.
   Doubled geometry, a trailing second avatar/ball, or duplicated GUI on the
   host means presentation isn't gated to client instances. A regression test
   for a bug found here counts only once it has been observed to fail with the
   fix removed **and a peer connected** — a solo in-process test takes only
   the authority path and can pass in both directions.
9. Repeat under latency, jitter, and loss with the in-Editor emulator in the
   clones; repeat over the real Relay/transport path in final testing.
10. Join from two processes that each made a local choice before connecting.
    Confirm the authority's assignment wins: no two participants share a slot
    or anything the game derives from one, and each client shows the
    replicated identity, not the one it chose before the game session.
11. Run a complete game session across two processes and compare it feature by
    feature with a single-process game session: anything missing on a client is
    either replicated or recorded as a deferral with its reason.

The measurement loop is in the performance reference.

## Proving a build is current

Player-build verification must be checkable, not asserted. Unity does not
rewrite the `.exe` when only managed assemblies change, and a directory's
timestamp does not move when files inside it change, so both things a reviewer
inspects stay frozen at the first build. Compare the last-modified time of the
built game code itself (`<Product>_Data/Managed/Assembly-CSharp.dll` under Mono,
`GameAssembly` under IL2CPP) against the newest source change, and confirm the
EditorBuildSettings scene list is non-empty before building. Don't change player
settings or add startup logging to prove it, because those ship with the game.
