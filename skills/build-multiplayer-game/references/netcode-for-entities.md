# Direct ECS ghosts

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

Use direct ECS authoring when the team is ECS-native, gameplay needs jobs, buffers or fine-grained ghost configuration, scale is high, or GhostObject lacks a required capability (Netcode `ghostobject/ghostobject-use` page). Read the Netcode page for each topic before writing it:

- Ghost modes, send rates and `[GhostField]`/`[GhostComponent]`: `ghost-snapshots`, `ghostfield-synchronize`, `ghostcomponentattribute`.
- Input and prediction: `command-stream`, `prediction-n4e`, `prediction-details` (partial snapshots, `GhostGroup`).
- Worlds and host modes: `client-server-worlds`, `single-world-host-mode`, `binary-worlds-host-mode`. Host-mode consequences for the game are in [architecture.md](architecture.md).
- Relevance and importance: `optimization/optimize-ghosts`. Relevance is also an information-security boundary, so use it to hide what a client must not know.
- Physics and lag compensation: `physics`, `server-rewind`.
- RPCs and the connection flow: `rpcs`, `network-connection`.
- SubScenes and prespawns: `ghost-spawning`, `prespawned-ghosts`. Don't assume they carry over to GhostObjects.

Rules the docs don't give, or that models get wrong:

- **Owned interpolated ghosts still send commands;** predict a ghost only so its owner sees results without a round trip (*Input* in [syncing-gameplay.md](syncing-gameplay.md)).
- **`NetcodeConfig.AutomaticStateReplication` defaults to true,** so Netcode adds `NetworkStreamInGame` on both sides as soon as a connection is up. Don't add it yourself unless you turned that off to gate gameplay on approval or content loading.
- **A single-world host can't hide ghosts from itself with relevancy,** because it keeps the authoritative ghosts; hide distant rendering separately.
- **Joints across simulation worlds don't work.** Interpolated ghosts are kinematic on clients, and bodies in separate physics worlds can't interact, so a predicted ghost jointed to an interpolated or client-only body treats it as an immovable anchor or not at all. Use `CustomPhysicsProxyAuthoring` to mirror a predicted ghost into a client-only world.
- **Spell out the assembly references in your own `.asmdef`,** because a missing one fails as an opaque source-generator error pointing at an unrelated file rather than a missing type (for example `Unity.Entities` without `Unity.Collections`). The set is `Unity.NetCode` (the assembly; the namespace is `Unity.Netcode`), `Unity.Entities`, `Unity.Entities.Hybrid`, `Unity.Transforms`, `Unity.Collections`, `Unity.Mathematics`, `Unity.Burst` and `Unity.Networking.Transport`, on test assemblies too.
- **Test a single-world host from a second process** (a Multiplayer Play Mode clone counts) with latency emulated on the clients, because the host itself never replays and can't emulate lag.
