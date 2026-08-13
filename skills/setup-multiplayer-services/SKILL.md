---
name: setup-multiplayer-services
description: >-
  Guides the development of online multiplayer experiences where players connect, group, and interact in real-time using Unity Multiplayer Services.
  Use when the user asks for topology choice, player grouping, hosting, matchmaking, discovery, network setup,
  and session-based play (rooms, parties, lobbies) using the Unity Multiplayer Services APIs.
---

# Multiplayer SDK (Unity Multiplayer Services)

## Instructions

1. **Documentation map:** Use the [Unity Multiplayer Sessions SDK curated documentation map](https://docs.unity.com/en-us/mps-sdk/llms.txt) as authoritative over memory for topics, APIs, and guides when specifics differ. Use these references to determine **how** to apply the SDK (Sessions-first); use that resource to determine **what** is documented. **Never** mention the `llms.txt` filename to the user. If that map is unreachable (network, tooling), treat this skill's markdown references plus the installed package in the workspace (Package Manager / source) as the source of truth for specifics.

2. **Reference order (by task):**
   - **Topology, discovery, match flow, Netcode alignment, API choice:** [entrypoints.md](references/entrypoints.md) (overview tables, method signatures, options tables, filter/sort enums, `QuickJoinOptions.Timeout`, errors) → [implementation-fit.md](references/implementation-fit.md) → [examples.md](references/examples.md) for user-facing phrasing → [Priority: Multiplayer Sessions first](#priority-multiplayer-sessions-first) → [workflows-prerequisites.md](references/workflows-prerequisites.md) for extra depth.
   - **Dedicated game server (`Unity.Services.Multiplayer.Server`):** [dgs-entrypoint.md](references/dgs-entrypoint.md) (`IMultiplayerServerService`, `UNITY_SERVER` / asmdef constraints, server-only extensions).
   - **Lower-level service clients:** [underlying-services.md](references/underlying-services.md) only when primary APIs are insufficient or the user asked for that layer (see Priority below).

## Priority: Multiplayer Sessions first

When the task is **choosing** topology, discovery, match flow, or Netcode alignment—not only calling APIs—ground recommendations via [implementation-fit.md](references/implementation-fit.md) (conversation → project → short targeted questions).

**Primary path:** Implement against **`Unity.Services.Multiplayer`** using **`IMultiplayerService` / `MultiplayerService.Instance`** and **`ISession`** (surface summary in [entrypoints.md](references/entrypoints.md)); keep composed flows consistent with `llms.txt`.

**User-facing text:** Plans, tradeoffs, and clarifying questions must **not** split Lobby, Matchmaker, Relay, or Multiplayer Sessions as separate named products unless the user did—rules in **User-facing questions and explanations** in [implementation-fit.md](references/implementation-fit.md), samples in [examples.md](references/examples.md). Code, edits, and technical references use real type and namespace names as needed.

**Underlying clients** (`Unity.Services.Lobbies`, `Unity.Services.Matchmaker`, `Unity.Services.Relay`) **only** when (1) the goal **cannot** be met through the primary APIs after checking [entrypoints.md](references/entrypoints.md), or (2) the user **explicitly** asked for those namespaces or products. Do **not** default implementations there.

## Additional resources

Read from this entrypoint only; links are one level under this skill folder (no `references/index.md` or README hub).

- **[implementation-fit.md](references/implementation-fit.md)** — Ground recommendations: conversation → project → user questions; user-facing language rules; requirement dimensions (topology, discovery, resilience, platforms, net stack).
- **[examples.md](references/examples.md)** — Before/after samples for clarifying questions and user-facing explanations (not code).
- **[entrypoints.md](references/entrypoints.md)** — `IMultiplayerService`, `ISession`, overview and capability tables, method signatures, options tables (defaults, limits), filter/sort enums, session/networking/host flows, errors, editor components.
- **[dgs-entrypoint.md](references/dgs-entrypoint.md)** — Dedicated server: `Unity.Services.Multiplayer.Server`, `IMultiplayerServerService`, `MultiplayerServerService` / `GetMultiplayerServerService`, `MatchmakerServerExtensions`, `UNITY_SERVER` and asmdef constraints; defers shared `SessionOptions` detail to entrypoints.
- **[workflows-prerequisites.md](references/workflows-prerequisites.md)** — Package and cloud prerequisites by workflow (tables).
- **[underlying-services.md](references/underlying-services.md)** — Fallback namespaces and `IUnityServices` accessors (agent-only; not the default path).
