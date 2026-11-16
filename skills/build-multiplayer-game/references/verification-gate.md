# Verification gate

## Contents

- [Hard implementation gate](#hard-implementation-gate)
- [Verify in this order](#verify-in-this-order)
- [Completion check](#completion-check)

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

## Hard implementation gate

For a new realtime multiplayer project, the primary implementation is NGO or Netcode
for Entities, chosen per [choosing-netcode.md](choosing-netcode.md). Do not implement
Mirror, a raw `NetworkDriver` gameplay protocol, or any other networking runtime unless
the user requires it. Before writing any gameplay networking code, record all of
the following in the project (README or a `NETWORKING.md` decision record), each item
citing its source file with quoted lines, not assertions. Items 2, 4 and 6 apply to
Unity Netcode only; NGO skips them and takes item 5's NGO branch.

1. Installed Unity, the chosen netcode package (`com.unity.netcode` or
   `com.unity.netcode.gameobjects`), and `com.unity.transport` versions, quoted
   from `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, and
   `Packages/packages-lock.json`.
2. The authoring-layer APIs you will use (`GhostObject`, `GhostBehaviour`,
   `GhostField<T>`, or the direct ECS types), each confirmed in the installed
   package source with the grepped path.
3. The selected stack, topology and authority model, and the requirement that decided
   the stack.
4. For every predicted value, **the restored storage it lives in**: the
   `GhostField<T>`/`[GhostField]`-carrying component that rollback rewinds. A row
   naming a plain MonoBehaviour field, a static, or a component with no ghost fields
   is a defect before it is written (see *Prediction and replay* in
   [syncing-gameplay.md](syncing-gameplay.md)); an unfilled row means the
   value is not yet designed.
5. A minimal probe of **the authoring path that was chosen**, run in Play mode (a
   Multiplayer Play Mode scenario; a development build only when MPPM is unavailable),
   with command output as its evidence. The chosen stack and authoring layer pick the branch:
   - *GameObject authoring* (GhostObject): the compile
     probe's exact compiler output recorded (delete the probe afterwards), plus a runtime
     probe: game-session start, registered-prefab spawn, one `GhostField` read.
   - *Direct ECS authoring* (an ECS-native project, or an installed package with no GhostObject layer): the
     equivalent runtime probe: game-session start, spawn of a `GhostAuthoringComponent`
     prefab, one `[GhostField]` read. Equivalent evidence, not a lighter bar:
     recorded compiler output for an absent `GhostObject` is not a probe of the path
     being implemented.
   - *NGO*: game-session start, spawn of a `NetworkObject` prefab registered with the
     `NetworkManager`, one `NetworkVariable` read.

   Read the field with `netcode.ghost get` (Unity Netcode) or `script eval`
   (NGO, because `get_object` doesn't show `NetworkVariable` values), and run the stack's `audit`
   command before the probe. A compile-only probe does not validate initialization or
   lifecycle on any branch.
6. Either a GhostObject/GhostBehaviour implementation, or a documented direct-ECS path
   naming what routed the project there: an absent GameObject authoring layer, a
   reproduced GhostObject defect, an unimplementable requirement, or an ECS-native
   project chosen up front.

If any item is missing, stop and ask for approval before implementing. Approval may only
select a stack per [choosing-netcode.md](choosing-netcode.md), a path in the
[ghost authoring fallback order](ghost-authoring.md), or record an explicit user requirement
for raw Transport. Do not substitute raw Unity Transport because it is smaller, stable,
or more convenient.

Every cited verification output must postdate the newest source file it claims to verify;
record every failed or truncated verification attempt alongside the successful one.
Every design claim in the record (tick and snapshot rates, replicated fields, lifecycle
behavior) must name the config field or code symbol that implements it, and every
numeric budget must be asserted in a test; see the `ClientServerTickRate` trap in
[performance-and-scale.md](performance-and-scale.md).

Before multiplayer testing, verify Run in Background is enabled, or that focus loss is
deliberately handled, per *Start and connect* in
[netcode-lifecycle.md](netcode-lifecycle.md).

## Verify in this order

Write gameplay code only after the design is agreed.

- **Agreement.** "Agreed" means the final report has been emitted with its package-probe
  fields filled from the gate's cited file evidence. A straightforward trusted-game-session
  default needs no explicit user reply; cheat-sensitive or ranked designs require
  explicit user approval.
- **Probe outcome routes the fallback.** A failed GhostObject probe, or an absent layer,
  moves a new project to direct ECS ghosts (and to the direct-ECS branch of item 5), not
  to NGO, because the requirement that chose Unity Netcode still holds.
- **Function across processes, in Multiplayer Play Mode.** Verify gameplay with a
  scripted host-plus-client scenario via [multiplayer-play-mode.md](multiplayer-play-mode.md):
  the main Editor hosts and a virtual player joins. This is the default; it needs no build.
- **Cross-process connect on a real address.** The minimal connectivity probe must
  include a connect from a second process, with evidence for the chosen stack:
  - *Unity Netcode:* the client's driver-selection lines quoted from its log (a
    virtual player's console, or Player.log). Those lines are Debug-level, so set the
    clone's Netcode log level to Debug before the run (`netcode.debug set_log_level`);
    at the default level their absence proves nothing. Quote the clone's
    `netcode get_connection_state` output alongside them. Whenever a non-host ServerWorld
    exists in the client's process (binary-worlds `ClientAndServer`), the default
    bootstrap plus driver constructor otherwise routes "remote" connects to the
    in-process IPC loopback, with only a warning (`IPC interfaces only support loopback
    address`) in the log (see *World creation and transport driver selection* in
    [netcode-lifecycle.md](netcode-lifecycle.md)). A virtual player joining the main
    Editor qualifies when the clone has no server world of its own: the IPC driver
    cannot cross a process boundary, so a cross-process connect that succeeds used the
    socket driver. A relay connect already dials a non-loopback address; for a direct
    connect, use the machine's LAN address, not loopback.
  - *NGO:* quote the clone's `netcode.gameobjects status` (`isConnectedClient: true`, its
    `localClientId` and transport) and the host's `netcode.gameobjects list_clients`
    showing that client with an RTT. NGO's transport has no in-process IPC driver, so a
    client the host lists from another process connected over the network. For a direct
    connect, use the machine's LAN address; a Relay connect shows `RelayUnityTransport`.
- **Test under adversity, also in Multiplayer Play Mode.** Run the clones under latency,
  jitter, and packet loss with the in-Editor emulator throughout development, not in a
  late pass: `netcode.simulator set` on each clone for Unity Netcode, or
  `netcode.gameobjects.simulator set` for NGO (see *Network conditions in clones* in
  [multiplayer-play-mode.md](multiplayer-play-mode.md) and
  [performance-and-scale.md](performance-and-scale.md)). Editor profiling numbers are
  indicative; final numbers come from final testing.
- **A complete game session, not a few seconds of motion.** The start signal reaches every
  client (control unlocks everywhere), interactions between participants show on every
  screen, and the game session ends with the same result everywhere. A participant that never
  leaves the start is the classic sign that an authoritative lifecycle event was applied
  locally on the host and never replicated.
- **Final testing: player builds.** Build players once the work passes in Multiplayer Play
  Mode, and only for what Editor clones cannot show, because the MPPM and NGO docs place
  builds there and they are slow to iterate on: server-only (`UNITY_SERVER`) code,
  build stripping and the scene list, target platforms and devices, release-build
  network conditions, a multi-machine run over the real transport or Relay path,
  rejection of a mismatched build, and any shippable build the user asked for. With MPPM
  available, add the builds to the scenario as local instances
  (`add_player --type local`), per [multiplayer-play-mode.md](multiplayer-play-mode.md).
  Report whatever final testing was not run as a gap, not as a failure of the change.
- **MPPM unavailable.** When MPPM is not installed or its clone never launches, build
  and launch standalone host and client players by hand instead, outside any scenario,
  per [multiplayer-play-mode.md](multiplayer-play-mode.md).

## Completion check

Before declaring the implementation done, check the following. For a ghost or NGO implementation, all of them apply. For session-only work (no gameplay
network), skip items 1, 2, 4 and 7, because they need a netcode stack or a gameplay event, and cover
item 3's evidence with the session create, join and read-back, each read with
`script eval` on the creating and joining instances, which are separate MPPM players, not
two identities in one process.

1. `Packages/manifest.json` contains the chosen netcode package: `com.unity.netcode`
   or `com.unity.netcode.gameobjects`.
2. A grep of `Assets` hits the chosen stack's types (`GhostBehaviour`, `GhostObject`,
   `GhostAuthoringComponent`, `[GhostField]`, or `GhostPrefabCreation` for Netcode for
   Entities; `NetworkBehaviour`, `NetworkObject` or `NetworkVariable` for NGO), and at least one matched type is reachable
   at runtime (referenced by a scene, prefab, or SubScene, or an auto-registered
   system/bootstrap that executes). Orphaned authoring classes and compile-only probes
   do not count.
3. Runtime evidence exists: a game session (host start, a cross-process client
   connect, **and at least one gameplay event (spawn, shot, impact, score change)
   driven across that already-connected game session**) shown by command output from
   each process (connection state per *Cross-process connect on a real address* above, and the event's
   state read with the commands in item 4), quoted in the decision record and run after
   the last source change. Don't add logging to produce it. A connect-only check, a solo
   host, and any single-process run are blind to host-role bugs.
4. **That event's resulting state is recorded on both peers and confirmed to match**:
   object counts and any mutated shared world state, read on each instance with
   `netcode.ghost get` (Unity Netcode) or `script eval` (NGO `NetworkVariable`
   values), not "it looked right". This is
   unhappy-path test 8 in [netcode-lifecycle.md](netcode-lifecycle.md), promoted into
   the gate because it catches host-role divergence empirically.
5. If the project predicts anything, prediction is verified **in two processes** per
   *Verifying prediction* in [performance-and-scale.md](performance-and-scale.md), with
   `netcode.profiler get_rollback_metrics` from a client instance as evidence. A
   single-world host is never replayed, so single-process tests only take the
   authority path.
6. If player-build verification is claimed, the EditorBuildSettings scene list is
   non-empty and the build verifiably postdates the last source change (see *Proving a
   build is current* in [netcode-lifecycle.md](netcode-lifecycle.md)).
7. Parity is accounted for: every gameplay feature the single-process build has is either
   exercised across processes in the cited evidence or named in the decision record as a
   deferral with its reason. A feature disabled only for networked play without such an
   entry is a defect.

A regression test is not one until it has been observed to fail with the fix removed.
For host-role and prediction bugs, see the note under unhappy-path test 8 in
[netcode-lifecycle.md](netcode-lifecycle.md).

For a ghost or NGO implementation, zero hits for the chosen stack's types, or missing
runtime evidence, is a failed completion check: record it in the decision record as a
gap, not as done. Session-only work fails only when its session create, join or read-back
evidence is missing.
