---
name: build-multiplayer-game
description: >-
  Builds Unity online multiplayer, from matchmaking to gameplay sync. Make sure to use this skill when the user asks to add online play, host or join games, or fix lag, desync or connection failures. Not for voice chat or backend features.
allowed-tools:
  - WebFetch
  - WebSearch
---

# Build multiplayer game

The **session layer** (Unity Multiplayer Services) creates, finds and connects the match; **netcode** synchronizes gameplay inside it. A **game session** is the running netcode world, from `StartAsHost`/`Connect` until `Shutdown`; a lobby or session before it is not one, so no ghost may be touched yet.

## Before you start

Before a workflow or diagnosis, read the doc pages from [Documentation](#documentation) that cover it, and name the files you read, because your training knowledge may be for the wrong version.

Prefer `WebFetch` over `WebSearch`, and fetch only what you need. Replace `<VERSION>` with the package's major.minor resolved in `Packages/packages-lock.json` (e.g. `7.2`); docs.unity.com URLs are unversioned. If that version's page is not published (404), read the installed package's `Documentation~` copy, never an older version's page, because older pages describe a different API. If neither exists, read the package source and report the page missing.

Run Editor commands via the `unity-cli` skill. Fully qualify types in `script eval` snippets (or add `using` lines), because an unqualified type outside `UnityEngine` fails with CS0103.

Do only what's asked. Don't change unrelated assets or files. Avoid long explanations. Never leave verification code in the user's script. Never build logging or detection they didn't ask for. Create a restore point you can roll back to if your changes fail.

## Documentation

**Multiplayer Services.** The [documentation map](https://docs.unity.com/en-us/mps-sdk/llms.txt) is authoritative over memory; don't mention `llms.txt` to the user. If it is unreachable, use the installed `com.unity.services.multiplayer` source. Pages (as `.md`):

- `create-session`, `join-session`, `matchmake-session`, `matchmake-lobby` (quick join)
- `session-networking`, `session-host-migration`, `game-server-hosting-support` (dedicated servers, backfill)
- `build-with-netcode-for-entities` (how the default network handler creates worlds), `lobby-error-messages`

**Netcode packages.** For `<package>` below, read `<project>/Library/PackageCache/<package>@*/Documentation~/` `.md` files first (`"source": "builtin"`: the Editor install's `Resources/PackageManager/BuiltInPackages/<package>/`; before install, the version Package Manager would resolve). Otherwise fetch `https://docs.unity3d.com/Packages/<package>@<VERSION>/manual/<page>.html`.

Unity Netcode (`com.unity.netcode`):

- Topology and host modes: `choose-network-topology`, `client-hosted`, `binary-worlds-host-mode`, `single-world-host-mode`
- Ghosts, prediction, physics, lag compensation: `ghosts`, `prediction-n4e`, `physics`, `server-rewind`
- Host migration (experimental), bandwidth, test tools: `host-migration/host-migration`, `optimizations`, `testing/debugging`
- GhostObject layer and when to use it: `ghostobject/ghostobject-intro`, `ghostobject-use`, `ghostobject-get-started`, `ghostobject-setup`, `ghostobject-hybrid`
- GhostObject state, input, RPCs, connections, physics: `ghostobject/ghostobject-replicate`, `ghostobject-prediction`, `ghostobject-rpcs`, `ghostobject-connections`, `ghostobject-physics`

NGO (`com.unity.netcode.gameobjects`):

- Identity, state, messages: `components/core/networkobject`, `basics/networkvariable`, `advanced-topics/message-system/rpc`
- Transform, animation, physics sync: `components/helper/networktransform`, `components/helper/networkanimator`, `components/helper/networkrigidbody`, `advanced-topics/physics`
- Scenes and topologies: `basics/scenemanagement/scene-management-overview`, `terms-concepts/network-topologies`, `terms-concepts/distributed-authority`
- Latency: `advanced-topics/client-anticipation`, `learn/dealing-with-latency`
- NGO 3.x and hybrid objects (experimental): `upgrade-guide`, `ghostobjects`
- The `netcode.gameobjects` CLI commands: `unity-cli-commands`

## Glossary

| Term | Meaning |
|---|---|
| Unity Netcode | `com.unity.netcode` (formerly Netcode for Entities): ghosts with prediction and rollback, authored as GhostObjects or direct ECS ghosts |
| GhostObject layer | `GhostObject`/`GhostBehaviour`/`GhostField<T>`: GameObject authoring over ghosts |
| Direct ECS ghosts | `GhostAuthoringComponent` and `[GhostField]` on ECS components |
| NGO | Netcode for GameObjects: `NetworkObject`/`NetworkBehaviour`/`NetworkVariable`, no rollback-and-replay prediction |

## Invariants

1. **Sessions first.** Implement session flows as step 4 routes them. Use the underlying `Lobbies`, `Matchmaker` or `Relay` clients only when the user asks or sessions can't do it, because sessions compose them.
2. **User-facing language.** Describe flows in the user's words (host, join by code); name Lobby, Matchmaker, Relay or Sessions only when the user did.
3. **Choose the stack by requirement** (step 3) and record it with that requirement in the decision record, because later changes must know whether it still holds.
4. **Raw Unity Transport is not a gameplay layer** without an explicit, recorded user requirement, because it leaves replication, prediction and lifecycle to the project.
5. **Recommend, then ask.** State assumptions and recommend a design; ask only decisive questions, in game terms, in the same reply. Block only when every assumption is unsafe.
6. **Ranked or cheat-sensitive play needs a trusted dedicated server**, because player-hosted authority cannot be cheat-safe.
7. **One writer per value.** Clients send requests or inputs; the authority validates and decides, because two writers diverge and cannot be reconciled.
8. **Participant identity comes from the authority.** It assigns slot, team and spawn point on join, not local join or device order. A pre-session choice (character, loadout) is a request carried into the join, such as a session player property, which the authority confirms or reconciles.
9. **Parity with the single-process game is the bar.** Make each feature work across machines, or record it as not yet done, with the reason.
10. **Replicate causes and compact state, not presentation.** Derive animation, VFX, audio and UI locally, because causes carry the information.
11. **Persistent or late-join data is state; one-shot effects are remotes.** Current state, not past events, is the source of truth.
12. **Name platforms by category only** (desktop, mobile, console), never a specific console or platform holder, because those names must not appear in output.

## Scope

Voice or text chat belongs to `setup-vivox-voice-chat`; persistence, progression, cloud save, leaderboards, economy, analytics and turn-based or async play belong to `build-live-game`.

## Workflow

### 1. Inspect the project

1. Read `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json` and `Packages/packages-lock.json`, because the installed versions decide which APIs exist.
2. List multiplayer packages, networking scripts and server build targets.
3. Decide the existing stack from `manifest.json` direct dependencies and the project's scripts (step 1 of [choosing-netcode.md](references/choosing-netcode.md)), not from `packages-lock.json`, because NGO 3.x always pulls in `com.unity.netcode`.
4. For a ghost project, confirm each API and limit you rely on in the installed package source, because they differ between package builds.

### 2. Determine requirements

Infer from the conversation, then the project, then ask. A trusted game of at most four players defaults to a listen server with no interview; ask only step 3's responsiveness question, when nothing else decides the stack.

| Requirement | Decision affected |
|---|---|
| Player count, relevant objects per client | CPU, bandwidth, relevance |
| Cheat risk; who runs the match; hosting budget | Listen server vs dedicated |
| How players join: code, browse, auto-pair | Session APIs |
| Host-loss and reconnect needs | Topology, migration, reconnect |
| Instant own movement on lag; hit registration; physics between players | Stack (prediction, lag compensation), tick rate |
| Existing stack, Rigidbody, Animator or multi-scene content; GameObject or ECS workflow | Stack and rewrite cost |
| Match, persistent or drop-in flow | Scene state, late join |
| Choices made before the game session | Join requests (invariant 8) |
| Desktop, mobile or console | Performance ceiling, prediction CPU cost |

Propose four budgets rather than asking: players, relevant objects, tick and send rate, per-client bandwidth.

### 3. Choose authority, topology and netcode stack

Pick one of each.

**Stack.** Apply the order in [choosing-netcode.md](references/choosing-netcode.md), then state the stack and the requirement that decided it:
1. Keep the existing stack.
2. Otherwise follow the request: a stack the user names; else prediction, rollback or lag compensation (or instant response on a laggy connection, or fair hits) means Unity Netcode, because NGO has no rollback-and-replay loop.
3. Otherwise apply its requirements table.
4. Otherwise recommend NGO and ask the one responsiveness question in game terms; with no user to ask, decide from clues in the request, and with none, choose NGO.

**Authority and topology:**

- Cheat-sensitive or ranked: dedicated, server-authoritative (invariants 6, 7).
- Trusted short game session: client-hosted listen server, because it needs no server hosting.
- Trusted game session that must survive host departure: dedicated authority, because no player's exit ends it; use experimental host migration only if the user explicitly accepts its limits.
- Casual or social game, no dedicated server, must survive host departure, no prediction need: NGO distributed authority, because it has no single host to lose.
- No realtime simulation: no realtime netcode; defer to backend services.

A client-hosted ghost game defaults to single-world host mode (lowest host cost). GhostObjects support single world only, so binary worlds (host prediction switching, host migration) are for direct ECS ghosts; read `binary-worlds-host-mode` first. State the host mode and why. Tradeoffs: [architecture.md](references/architecture.md).

### 4. Set up session and connectivity

Check packages and cloud setup in [workflows-prerequisites.md](references/workflows-prerequisites.md) before coding. Carry the step 3 decision in; don't re-decide it.

Before you write session code, check that the project is linked to a Unity Cloud project, because sessions and Relay need its project ID. Player sign-in at runtime is separate; it is in the workflow prerequisites. If it is not linked, make the restore point, then follow [link-cloud.md](references/link-cloud.md).

Create, join and create-or-join, client or dedicated server, use session connectors where [session-connectors.md](references/session-connectors.md) allows. Otherwise:

| Goal | Session API |
|---|---|
| Create, join by code | `CreateSessionAsync`, `JoinSessionByCodeAsync` |
| Browse open games | `QuerySessionsAsync` on indexed session properties |
| Quick join | `MatchmakeSessionAsync` + `QuickJoinOptions`; set `CreateSession` to create when nothing matches; no `Timeout` unless asked |
| Queue matchmaking | `MatchmakeSessionAsync` + `MatchmakerOptions` and a deployed queue |
| Dedicated server, backfill | `MultiplayerServerService.Instance` under `UNITY_SERVER`; `WithBackfillingConfiguration` |
| Host migration (only if step 3 chose it), reconnect | `WithHostMigration`; `ReconnectToSessionAsync` |
| Connectivity | `WithRelayNetwork` across home networks, `WithDirectNetwork` for published addresses |

APIs and options: [entrypoints.md](references/entrypoints.md); server rows: [dgs-entrypoint.md](references/dgs-entrypoint.md). Phrase user questions per [implementation-fit.md](references/implementation-fit.md) and [examples.md](references/examples.md). Use [underlying-services.md](references/underlying-services.md) only as invariant 1 allows.

Pick one owner of netcode start-up per game session, because two starters duplicate worlds. The default network handler (`WithRelayNetwork`/`WithDirectNetwork`) connects or listens, and starts NGO whenever `com.unity.netcode.gameobjects` is installed, otherwise Unity Netcode (never on WebGL):

| Stack | Who starts it | Don't |
|---|---|---|
| NGO | The default handler | Call `NetworkManager` `StartHost`/`StartClient`, or leave Unity Netcode's bootstrap on, because NGO 3.x installs it and it can take port 7979 |
| Direct ECS ghosts | The default handler | Call `Netcode.Connect`/`StartAsHost`, leave the automatic bootstrap on, or leave `ClientServerBootstrap.AutoConnectPort` above 0, because the handler reuses bootstrap worlds and fails on a client world that auto-connected |
| GhostObject | You, with `Netcode.StartAsHost` (for Relay, see netcode-lifecycle.md); the session only supplies membership and connection details | Host through the default handler (it creates separate client and server worlds), or install NGO (its handler runs instead) |

If the installed packages' docs don't confirm the default handler for your stack, or packages aren't installed yet, start the stack yourself and say so. A custom `INetworkHandler` owns start-up itself. With no network option, the host starts the session network later (`session-networking`).

### 5. Choose authoring

- NGO: registered `NetworkObject` prefabs, `NetworkVariable` for state, RPCs for one-shot requests. Read the NGO pages first.
- Unity Netcode: read [ghost-authoring.md](references/ghost-authoring.md). A GameObject project uses GhostObject; an ECS-native or maximum-scale one uses direct ECS ghosts. **Declare every `GhostBehaviour` in its own file named after the class**, because a second class in a shared file saves onto the prefab as a missing script without warning. Never name a namespace segment `Netcode`, because it hides the `Netcode` facade in other files too.
- Existing NGO game that now needs prediction: keep stable systems; move only the subsystems that need prediction to ghosts, across clear authority and state boundaries (*NGO upgrade path* in [architecture.md](references/architecture.md)). Read NGO's `ghostobjects` page before proposing experimental hybrid objects.

### 6. Design replication

For every gameplay state, decide: writer; input or request sent; replicated state; predicted or interpolated (NGO: interpolated or anticipated only); local presentation; relevance and send rate; spawn, despawn, owner-drop, late-join and reconnect behavior.

Before writing GhostObject code, apply *GameObject-layer rules* in [netcode-lifecycle.md](references/netcode-lifecycle.md). Ghost rules in [syncing-gameplay.md](references/syncing-gameplay.md) and netcode-lifecycle.md are for Unity Netcode only; invariants 7, 8, 10 and 11 hold for NGO too.

### 7. Integrate and verify

1. Before writing gameplay networking code, satisfy the implementation gate in [verification-gate.md](references/verification-gate.md).
2. Confirm zero console errors with `unity command console status`.
3. For implementation work, verify in one Multiplayer Play Mode scenario per [multiplayer-play-mode.md](references/multiplayer-play-mode.md): the main Editor hosts and a virtual player joins, because a virtual player is a separate process and proves replication without a build. This includes games whose rules run in backend services: give each player its own instance, not two identities in one process. Run `unity command` first to list the installed packages' command groups. Then, reading state with commands, not logging or screenshots:
   1. Before `start_scenario`, run the stack's `audit` and fix what it reports, because clones must not receive its project writes.
   2. Drive input on each instance (the host and the clone) with `script eval` calls to the game's own methods (move each player, trigger the core gameplay event), let gameplay advance, then confirm the result on both instances with `netcode.ghost get` or `netcode.gameobjects list_objects` (NGO `NetworkVariable` values: `script eval`), because unchanging state proves the connection, not the gameplay.
   3. Repeat with lag set on the clone by `netcode.simulator set` (NGO: `netcode.gameobjects.simulator set`), because prediction and smoothing problems only show under latency.
4. Build players only for the gate's final-testing step, or when MPPM can't launch a clone, because builds are slow to iterate on.
5. Netcode work is done only when the gate's completion check passes on that cross-process evidence. Session-only work is done when the gate's session create, join and read-back evidence exists. Diagnosis stops at zero console errors unless the user asked for proof or you suspect a specific fault; then enter Play mode once to chase it, reading the world through `script eval`, no logging or screenshots.
6. If it fails, reread the docs and the matching reference before another fix.

## Example

Request: "GameObject project. Four friends, trusted co-op party game, join with a code." No stack is installed and nothing needs prediction, so: listen server, no interview, NGO with `NetworkVariable` state, and Host and Join session connectors on Relay. The report ends "Tell me if your own character must react instantly on a laggy connection, or if the host may leave mid-match".

Counter-example: "Online arena shooter; players say shots miss on high ping." Hit registration needs lag compensation, so Unity Netcode on a dedicated server; the report says NGO was ruled out for lacking rollback and server-side rewind.

## Reference map

| Task | Read |
|---|---|
| Topology, host mode, NGO migration | [architecture.md](references/architecture.md) |
| NGO vs Unity Netcode, rewrite cost | [choosing-netcode.md](references/choosing-netcode.md) |
| GhostObject vs direct ECS | [ghost-authoring.md](references/ghost-authoring.md) |
| State, input, prediction, remotes, physics, animation | [syncing-gameplay.md](references/syncing-gameplay.md) |
| Connect, worlds, spawn, scenes, rejoin, triage, error strings | [netcode-lifecycle.md](references/netcode-lifecycle.md) |
| Direct ECS ghosts, rollback, relevance | [netcode-for-entities.md](references/netcode-for-entities.md) |
| Lag, bandwidth, budgets, profiling | [performance-and-scale.md](references/performance-and-scale.md) |
| Implementation gate and completion check | [verification-gate.md](references/verification-gate.md) |
| Host + client or dedicated-server verification in MPPM | [multiplayer-play-mode.md](references/multiplayer-play-mode.md) |

## Final report

Use this unless the request is narrower:

```text
Assumptions: <inferred requirements and the four budgets>
Package state: <versions and the APIs confirmed in installed source, with file citations>
Recommendation: <authority/topology> + <NGO, GhostObject or direct ECS> + <host mode if relevant>, with the requirement that decided each
Session flow: <how players create, find and join, in the user's words>
Replication: <input, state, prediction/interpolation (and restored storage when predicting), local presentation, relevance>
Lifecycle: <identity/slot assignment, spawn, late join, disconnect/rejoin, host loss>
Risks: <package or design limits, rewrite cost of the chosen stack, or "none">
Verification: <checks run and the processes they ran in, or the gaps>
Side effects: <files, packages, settings or scenes changed beyond the request, and the Cloud project linked (ID, created or reused), or "none">
Follow-ups for you: <what the user must do or decide>
Tell me if <decisive assumption> differs and I will revise this.
```
