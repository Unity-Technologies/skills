# Choosing the netcode stack

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

Two stacks sync gameplay. Pick one per project, or per bounded subsystem in an existing project, before choosing authoring or writing code.

- **NGO** (`com.unity.netcode.gameobjects`): `NetworkObject`, `NetworkBehaviour`, `NetworkVariable`, RPCs. State changes replicate from the authority, and remote copies interpolate.
- **Unity Netcode** (`com.unity.netcode`): ghosts with snapshot delta compression, client prediction and rollback, lag compensation, importance and relevance. It has two authoring layers, GhostObject and direct ECS; see [ghost-authoring.md](ghost-authoring.md). GhostObjects support single-world hosts only. If the installed package has no public `GhostObject` type, use direct ECS ghosts (see the fallback order in ghost-authoring.md).

NGO 3.x depends on `com.unity.netcode` and installs it with the Entities packages, so read NGO's `package.json` for the minimum Editor (keep NGO 2.x on older Editors) and target platforms that support Burst and Entities (NGO `upgrade-guide` page). A project that only uses NGO therefore still resolves `com.unity.netcode`; that is not a ghost project. Turn that package's automatic bootstrap off in an NGO project (`netcode.config set --field enable_client_server_bootstrap --value DisableAutomaticBootstrap`), because otherwise it can create Unity Netcode worlds on port 7979 in every process, MPPM clones included, and an NGO client's join then fails.

NGO has no rollback-and-replay prediction loop and no server-side rewind. Before recommending NGO for anything latency-sensitive, read its `advanced-topics/client-anticipation` and `learn/dealing-with-latency` pages, because they state what NGO leaves for the project to build.

## Decide in this order

1. **Existing stack.** Decide it from the direct dependencies in `manifest.json` and the project's assets and scripts (`NetworkManager`/`NetworkObject` versus `GhostObject`/`GhostAuthoringComponent`), not from `packages-lock.json`. Keep that stack for new work, because both stacks use the `Unity.Netcode` namespace and two parallel net models double the lifecycle and test cost. To move an NGO subsystem that now needs prediction, follow *NGO upgrade path* in [architecture.md](architecture.md). NGO 3.x hybrid objects (a `NetworkObject` and a GhostObject on one prefab) are experimental, need a single-world host, and exclude distributed authority, in-scene placement, pooling and client-authoritative motion; read NGO's `ghostobjects` page before proposing them.
2. **Explicit request.** If the user names a stack, use it. If they ask for prediction, rollback, reconciliation, lag compensation or instant own-movement on a laggy connection, use Unity Netcode, because NGO cannot provide these without the project building them.
3. **Requirements table.** Otherwise, apply the first row below that the game matches.
4. **Ask.** If no row matches, recommend NGO and ask one question in game terms, in the same reply: "Should your own character react instantly even on a laggy connection, or is a short delay fine?". "Instantly" means Unity Netcode.
5. **Cannot ask.** In a run with no user to answer, decide from clues in the request (genre, "physics", "hundreds of units", "competitive", "party game"). If nothing points either way, choose NGO.

| The game needs | Stack | Why |
|---|---|---|
| Competitive or twitch action: shooter, fighting, racing with contact | Unity Netcode, dedicated server | Hit registration and responsive input need prediction and lag compensation |
| Server-side hit rewind | Unity Netcode | NGO has no rewind implementation; Netcode's `server-rewind` page covers it |
| Predicted physics between players (pushing, vehicles colliding) | Unity Netcode | NGO simulates physics only on the authority; other instances are kinematic, and no rollback corrects them |
| Many relevant objects or players, or a tight bandwidth budget | Unity Netcode | Delta-compressed snapshots, importance and relevance are built in |
| ECS-native project or team | Unity Netcode, direct ECS | Matches the existing workflow |
| No dedicated server, the match must survive the host leaving, casual or social play | NGO, distributed authority | Distributed authority exists only in NGO; its docs say it does not suit competitive games that need a predictive motion model |
| Existing GameObject game with significant Rigidbody, Animator or multi-scene content, and no prediction need | NGO | NGO's network components cover that content as is; Unity Netcode means converting to Unity Physics or accepting GhostObject's limits (Netcode `ghostobject/` pages) |
| Low-end mobile CPU budget with simple gameplay | NGO | Prediction re-simulates several ticks per frame and multiplies CPU cost |
| Trusted co-op or party game, a few players, no responsiveness requirement | NGO, listen server | Least code and setup for the need |
| Turn-based or async play | Neither | Hand off to `build-live-game` |

## Cost of choosing Unity Netcode for existing content

When Unity Netcode wins for an existing GameObject game, report the rewrite cost under Risks, because the user is trading content they already have for prediction:

- Direct ECS predicted physics runs on Unity Physics (`com.unity.physics`), not the GameObject physics engine. `Rigidbody`, `Collider` and joint setups are converted and re-tuned. Read the Netcode `physics` page for its limits before promising parity.
- GhostObject keeps GameObject physics but replays the whole default physics scene on every predicted tick, so existing client-only physics such as debris moves to a separate physics scene. Count that move in the rewrite cost. Read the Netcode `ghostobject/ghostobject-physics` page, and the installed package source, for setup and limits before promising parity.

## Record the choice

Write the chosen stack and the requirement that decided it in the decision record (README or `NETWORKING.md`), with the installed package version. A later change can then tell whether that requirement still holds before it adds or moves systems.
