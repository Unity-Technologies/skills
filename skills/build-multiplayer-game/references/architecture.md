# Architecture

## Contents

- [Ghost runtime and authoring](#ghost-runtime-and-authoring)
- [Authority](#authority)
- [Client-host modes](#client-host-modes)
- [Practical recommendations](#practical-recommendations)
- [NGO upgrade path](#ngo-upgrade-path)
- [Requirement conflicts](#requirement-conflicts)

Use this reference for authoring, authority, host topology, and moving Netcode for
GameObjects (NGO) subsystems to ghosts when they need prediction. Make the NGO versus
Unity Netcode choice first, in workflow step 3.

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

## Ghost runtime and authoring

`com.unity.netcode` is the runtime for games that need prediction, rollback, lag
compensation or high scale. It has two authoring surfaces on one replication
pipeline: GhostObject for GameObject/MonoBehaviour teams and direct ECS ghosts for
ECS-native teams or the highest scale. Read the Netcode `ghostobject/ghostobject-intro`
page for the GhostObject building blocks and the `ghostobject/ghostobject-use` page to
choose between the surfaces. Use NGO (`NetworkObject`, `NetworkBehaviour`,
`NetworkVariable`, RPCs) instead for a GameObject game with no prediction, rollback or
lag-compensation need, for distributed authority, or for an existing NGO project.

Confirm each GameObject-layer API in the installed `com.unity.netcode` source
(`Runtime/GameObjectLayer/`) before relying on it.

## Authority

Choose dedicated server or client-hosted with the Netcode `choose-network-topology`
page: dedicated for ranked, competitive or cheat-sensitive play; client-hosted for
trusted small sessions, which then need a host-loss design. Turn-based or async
gameplay needs no realtime netcode, because a backend owns canonical state.

Server authority still requires validation. Clients submit input or requests;
the authority checks ownership, rate, range, resources, and game rules. A server
that accepts client-reported outcomes is not secure.

## Client-host modes

Two listen-server arrangements exist; single world is the default. Which one a host
gets depends on what starts it: the automatic bootstrap reads **Host World Mode
Selection** on `NetcodeConfig`; `Netcode.StartAsHost` uses its `hostWorldMode`
argument (default single world); the Multiplayer Services default handler creates
separate client and server worlds when none exist. GhostObjects support single world
only (Netcode `ghostobject/ghostobject-connections` page), so binary worlds are for
direct ECS ghosts.

Use single world for a client-hosted ghost game. Before choosing binary worlds for a
direct ECS game, compare the modes on the Netcode `choose-network-topology` page and
the *Limitations and considerations* section of the `single-world-host-mode` page.
A single-world host can't simulate lag on its own objects, so test latency from a
second process; a Multiplayer Play Mode clone counts.

Binary-worlds host (direct ECS ghosts only, because GhostObjects don't support
binary worlds): the host process holds the authoritative server-world ghost and its
client-world replica. Draw presentation only from the client world, or the host
renders everything twice, visible as a second, trailing object because the replica
is interpolated.

That remedy doesn't apply to a single-world host, which has no replica. Host
duplicates happen in both modes for different reasons — read the single-world
consequences below before concluding that doubled geometry on a host does not
apply to your mode.

### Single-world details

- Both client and server roles are true in the host world (Netcode
  `single-world-host-mode` page). Almost every single-world host defect follows
  from that one fact, so the consequences are spelled out here and the rest of the
  skill links back to this list.
- The host's own connection is synthetic (the same page describes it). It raises
  no connection events and is absent from
  `NetcodeWorld.AllConnections`. Player counting that was correct on a
  binary-worlds server (internal IPC client + remotes) undercounts by one on a
  single-world host — count the host explicitly when its local connection is
  missing from the list.
- All host ghosts are authoritative, and the client receive/spawn/update pipeline
  early-outs on `IsHost()`. There is **no local replica** in this mode — so a
  doubled object on a single-world host is *application-created*, not a replica.
  For transform presentation, set **Full Tick Smoothing** to `Interpolate` on the ghost
  prefab (`BaseGhostSettings.FullTickSmoothing`), because it defaults to
  disabled; smooth other visual values separately.
- **`ServerToClient` remotes are delivered to the host itself.** A remote method
  body executes on the receiving *role*, and on a single-world host both roles are
  the same process and world, so the call dispatches into the host's own handler
  (a local passthrough that skips serialization; read the same page before writing
  a custom RPC serializer).
  Any handler with a visible or stateful side effect (spawning a visual proxy,
  playing a one-shot, applying damage, deforming terrain) must be idempotent with
  the authoritative path or skip its mirror work when `IsServer` is also true. A handler that *mirrors* an
  authoritative action into local presentation draws two overlapping objects on
  the host while remote clients see one.
- **`IsServer` and `IsClient` are both true here.** An `if (IsServer) … else …`
  pair takes only the server branch on the host, so host-side client work never
  runs. Write independent `if (IsServer)` and `if (IsClient)` blocks so the host
  runs both (Netcode `ghostobject/ghostobject-prediction` page), and test as a host
  as well as a dedicated server.
- **`CanWriteState` is true on the host** for interpolated-only ghosts as well as
  predicted ones, because the server retains `PredictedGhost`. Convenient for
  authoritative writes, and another reason mirror code needs its own guard.
- **The host is never replayed.** The rollback systems and the prediction history
  system early-out on `IsHost()` and the prediction group runs once per tick, so a
  single-world host always takes the authority branch. Client-only prediction
  defects — compounding deltas, missing reconciliation, non-idempotent replay —
  are structurally unreachable in one process, and host-side profiling understates
  prediction cost. Prediction correctness requires a second process; see
  *Verifying prediction* in the performance reference.
- Client systems see every player's input in the combined world; filter host-local
  input with `GhostOwnerIsLocal` as the same page describes.

## Practical recommendations

| Game profile | Recommendation |
| --- | --- |
| Two-to-four-player trusted co-op or party game, no responsiveness need | NGO + listen server |
| Two-to-four-player trusted action co-op that needs instant own-movement | GhostObject + single-world host; direct ECS ghosts if it needs binary-worlds features |
| Casual or social game that must survive host departure, no dedicated server | NGO distributed authority |
| Co-op that must survive host departure and needs prediction | Dedicated authority, or experimental host migration on direct ECS ghosts with a binary-worlds host (Netcode `binary-worlds-host-mode` page) after accepting its limitations; not available to GhostObjects |
| Competitive action | Dedicated server + direct ECS ghosts or verified GhostObject authoring |
| Existing GameObject game adding multiplayer | NGO, unless it needs prediction; then GhostObject, with the rewrite cost stated |
| Existing NGO game | Keep stable NGO code; migrate measured bottlenecks or new bounded systems |
| Large entity-heavy simulation | Direct ECS ghosts + dedicated server + relevance/importance |
| Turn-based/async | Backend state; no realtime netcode unless presentation truly needs it |

## NGO upgrade path

Use this when an NGO game later needs prediction, rollback or lag compensation in
part of its gameplay. Treat migration as a sequence of bounded system replacements,
not a rewrite:

1. Profile and select a subsystem that benefits from prediction, rollback,
   snapshot compression, relevance, or high object counts.
2. Define its one authority and canonical state independently of either API.
3. Map NGO identity, ownership, state, RPCs, spawn/despawn, references, scenes,
   and reconnect behavior to the ghost design.
4. Move a complete boundary such as movement, vehicles, projectiles, or a class
   of replicated actors. Never let NGO and ghosts write the same state.
5. Both packages use the `Unity.Netcode` namespace; in a project with both, qualify
   ambiguous types and keep each stack in its own assembly. NGO 3.x can also put a
   `NetworkObject` and a GhostObject on one prefab (a hybrid object): NGO then drives
   the connection through a single-world Netcode world, and only the transform,
   Rigidbody and GhostBehaviour state are snapshot-replicated and predicted;
   `NetworkVariable`s and RPCs are not rolled back. That integration is experimental,
   needs `HostWorldModeSelection` set to single world, and excludes distributed
   authority, in-scene placed objects, pooling and client-authoritative motion. Read
   NGO's `ghostobjects` and `upgrade-guide` pages before proposing it, and test
   interoperation in the installed build.
6. Retain NGO where it still meets the need. Remove it only after no retained
   system depends on it.

Good migration candidates have measurable network/CPU cost or need prediction.
Low-frequency UI/game-session glue is usually not worth moving first.

## Requirement conflicts

| Conflict | Resolution |
| --- | --- |
| Cheat-safe + no server cost | Fund dedicated authority for protected modes, or drop the cheat-safe claim |
| Host must leave + single-world/listen host | Prefer dedicated authority; host migration is experimental, best-effort, and documented for binary-worlds hosts only, so not for GhostObjects |
| Dedicated parity + minimum host overhead | Choose binary parity or single-world efficiency; they are different code paths |
| Hundreds of relevant objects + no relevance work | Reduce scope or implement relevance/importance and measure |
| Twitch physics + weak client hardware | Narrow predicted bodies, lower rate, or relax responsiveness |
| Persistent world + player-hosted only | Add a canonical backend/server or abandon persistence guarantees |

Identify which of security, cost, latency, or scope cannot move, then state the
tradeoff rather than promising incompatible requirements.
