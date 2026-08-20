---
name: create-multiplayer-game
description: >-
  Stand up a working multiplayer game of a chosen genre (co-op / competitive / event-driven) by
  adapting Unity's official Multiplayer getting-started sample for that genre as a template — not by
  writing netcode from scratch. Use when the user wants to create, scaffold, or bootstrap a
  multiplayer game, add multiplayer to a project, or build a co-op / competitive Unity game with
  Netcode for GameObjects and Multiplayer Services. Co-op is implemented; competitive and
  event-driven are stubs. Consume-only: it adapts an existing sample, it does not generate a new
  sample package.
---

# create-multiplayer-game

Build a multiplayer game by learning from, then adapting, the official **getting-started sample**
for the chosen genre. The sample is the source of truth — this skill teaches the *shape* and the
*procedure*; it deliberately carries **no** copied code, asset contents, or version numbers, because
those live in the sample and change with it.

## Core principle — read the live sample, never assume

Everything volatile (exact dependency versions, asset field values, the current file set) **must be
read from the sample at run time**. Do not trust any specific value written in this file or recalled
from memory — discover the sample first (Step 0) and read it. This is what keeps the skill correct
when the sample changes, and portable across machines (resolve by package **name**, never a
hardcoded path).

## Genre routing

The Multiplayer Center defines exactly three genres (`Unity.Multiplayer.Center.Common.GameGenre`,
in the **`com.unity.multiplayer.center`** package):

| `GameGenre` | Targets | Sample package | Status |
|---|---|---|---|
| `Casual` (0) | Casual / **co-op** | `com.unity.multiplayer.getting-started.casual` | ✅ implemented below |
| `Competitive` (1) | Competitive | `com.unity.multiplayer.getting-started.competitive` | ⏳ stub — same procedure, dedicated-server topology |
| `EventDriven` (2) | Turn-Based / Event-Driven | *(none yet — planned)* | ⏳ planned; backend/Cloud-Code, **no NGO** |

Ask the user which genre if unstated. For competitive, follow the same steps against its package
(read that sample's building blocks live; do not assume they match co-op).

## Step 0 — Ensure packages, then discover & read (always first)

1. **Ensure the Multiplayer Center + Quickstart content are in the project.** The samples are
   delivered by the `com.unity.multiplayer.center.quickstart` content package (which depends on
   `com.unity.multiplayer.center`). If they aren't installed, **add the latest from UPM** — e.g. add
   `com.unity.multiplayer.center.quickstart` via the Package Manager (add by name) or the project
   `Packages/manifest.json` — then import the chosen genre's sample from that package's Samples.
2. **Read the genre know-how**, which ships in the quickstart package at
   **`Documentation~/genres.md`** (present in the package cache of an installed quickstart; also
   linked from `Documentation~/index.md`, and pointed to by the package's `AGENTS.md`). It names each
   genre's sample and the Center's per-genre package recommendations, and links into the sample.
3. **Read the Center's genre config** for the *why* (topology + recommended package tiers), from the
   `com.unity.multiplayer.center` package (resolve by name):
   `Common/GameGenre.cs`, `Editor/MultiplayerCenterWindow/PackagesRecommendations.asset`,
   `Editor/MultiplayerCenterWindow/GameGenre.asset`.
4. **Read the sample**, in order: `package.json` (current pinned deps), `README.md`, then the runtime
   `.cs` and the asset inventory. Resolve the sample by package name; its Samples live in the
   quickstart's `Samples~/`, and the source lives in the samples repo.
5. Treat what you read (packages + sample) as **authoritative over anything in this file**.

## Co-op concept spine (the durable model)

A casual co-op game is **session-based drop-in co-op over Relay** (players' connection info
obfuscated from each other), replicated with **Netcode for GameObjects**, **distributed-authority
capable**, and **configured by ScriptableObjects rather than bootstrap code**. Its building blocks
(read each from the live sample for specifics):

1. **Session definition** — a `Unity.Services.Multiplayer` `MultiplayerSession` ScriptableObject
   (session type + lifecycle UnityEvents). *Sample: `CasualMultiplayerSession.asset`.*
2. **Session connector** — a `SessionConnector` ScriptableObject that **creates or joins by a shared
   session ID**, with create options (max players) and network settings (Relay); references the
   session SO. *Sample: `CreateOrJoinSessionConnector.asset`.*
3. **Network prefab registration** — an NGO `NetworkPrefabsList` that registers the player prefab.
   *Sample: `NetworkPrefabsList.asset`.*
4. **Player prefab** — `CharacterController` + `PlayerInput` + `NetworkTransform` + a movement
   `NetworkBehaviour` written to work in **both** client-server and distributed-authority (authority
   gated on `NetworkTransform.CanCommitToTransform`). *Sample: `PlayerPrefab.prefab`,
   `Runtime/PlayerMovementController.cs`.*
5. **Scene** — a NetworkManager-driven scene that wires the connector + prefab list.
   *Sample: `Example.unity`.*
6. **Input** — an Input System asset with a `Move` action. *Sample: `InputSystem_Actions.inputactions`.*
7. **Local test harness** — an **MPPM** `OrchestratedScenario` that launches a second virtual player
   and connects both. Editor-only; not runtime game logic. *Sample: `PlayerConnection.asset`.*

Dependencies (co-op currently: Netcode for GameObjects, Multiplayer Services, Multiplayer Play Mode,
Input System) are **pinned to exact versions in the sample's `package.json`** — read them there; do
not hardcode.

## Procedure — co-op

1. **Discover & read** the sample (Step 0).
2. **Confirm the target**: an existing Unity project on the sample's `unity` version (read it live),
   with a **linked Unity project/org** so Multiplayer Services + Relay work.
3. **Add dependencies** matching the sample's pinned set (from its `package.json`).
4. **Bring in the connection ScriptableObjects** (session + connector). Adapt the session ID, session
   type, and max-players to the game; **keep the Relay network settings**. Do not replace this with
   hand-written session-bootstrap code — the pattern is config-by-asset.
5. **Player prefab**: register the game's player prefab in a `NetworkPrefabsList`; ensure it has
   `NetworkObject` + `NetworkTransform` + a movement/gameplay `NetworkBehaviour`. Start from the
   sample's movement controller and adapt the mechanics, **preserving the authority gating**.
6. **Scene**: add a NetworkManager and wire the connector so the game creates/joins a session on
   start.
7. **Input**: add a `Move` action (and the game's other actions) via the Input System.
8. **Verify** (below).

## Gotchas

- **Versions are pinned exactly** in the sample's `package.json` — mirror them; read live.
- **Connection is configured by ScriptableObjects, not code** — edit the assets.
- **`PlayerConnection.asset` is an MPPM editor scenario**, not runtime config — use it (or a copy)
  for local testing; don't ship it as game logic.
- **Assets carry GUIDs/`.meta`** — import/adapt them; never hand-recreate a prefab/scene from prose.
- **Relay needs a linked project/org** — a purely local project won't obtain a Relay allocation.

## Verify

Run **Multiplayer Play Mode** with a 2-virtual-player `OrchestratedScenario` (start from the
sample's): both players connect to one session, each moves independently, and motion replicates
across instances. Prefer this over asserting success from compile-only checks.

## Maintenance notes

- Volatile specifics are intentionally **absent** here — genre know-how ships in the quickstart
  package's `Documentation~/genres.md` (versions with the samples in the same repo), and the
  per-genre package tiers/topology live in the Center package's config assets
  (`PackagesRecommendations.asset`, `GameGenre.asset`). Read them in Step 0.
- The samples + quickstart ship in the `com.unity.multiplayer.center.quickstart` package; the genre
  config (genres + recommendations) lives in the `com.unity.multiplayer.center` package. Resolve
  everything by package name, not a checkout path.
