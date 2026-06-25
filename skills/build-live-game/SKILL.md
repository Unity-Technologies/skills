---
name: build-live-game
description: Build and operate a live game using Unity Services. Use when the user needs to implement, connect, or debug backend-driven features — battle passes, achievements, player progression, cloud saves, leaderboards, matchmaking, virtual economies, server-authoritative logic, anti-cheat, player accounts and authentication, remote configuration, feature flags, A/B testing, analytics, or cloud resource deployment. Triggers on live-ops, live service, backend, server authority, cloud code, cloud save, remote config, player data, retention, monetization loop, season pass, ranking, multiplayer sessions, lobbies, or any Unity Services integration.
enabled: true
---

# Build a Live Game With Unity Gaming Services

## UGS Packages

| Package | Min Version | Purpose |
|---|---|---|
| `com.unity.services.core` | 1.16.0 | Initialization, dependency graph |
| `com.unity.services.authentication` | 3.6.1 | Player sign-in and identity |
| `com.unity.services.cloudcode` | 2.10.3 | Server-authoritative C# modules |
| `com.unity.services.cloudsave` | 3.4.0 | Per-player and shared key-value storage |
| `com.unity.remote-config` | 4.2.5 | Server-side game configuration |
| `com.unity.services.deployment` | 1.7.2 | Deploy cloud resources from Editor |
| `com.unity.services.tooling` | 1.4.1 | Access Control and Game Overrides |
| `com.unity.services.apis` | 1.1.1 | Generated REST clients for all UGS services |

- [Initialization Pattern](#initialization-pattern)
- [Package Map](#package-map)
- [Architecture — How Packages Combine](#architecture--how-packages-combine)
- [Core Services — Quick Reference](#core-services--quick-reference)
- [Asset Store Building Blocks](#asset-store-building-blocks)
- [Ready-Made Feature Blueprints](#ready-made-feature-blueprints)
- [Common Architecture Patterns](#common-architecture-patterns)
- [Validation](#validation)
- [Deployment Checklist](#deployment-checklist)
- [Detailed References](#detailed-references)

## Initialization Pattern

Every UGS game starts the same way. `com.unity.services.core` must initialize first, then the player signs in:

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;

await UnityServices.InitializeAsync();
await AuthenticationService.Instance.SignInAnonymouslyAsync();
// All other services are now ready
```

After `InitializeAsync()` completes, service singletons (e.g. `CloudSaveService.Instance`, `CloudCodeService.Instance`) are available.

## Package Map

### Foundation

| Package | Purpose | Singleton / Entry Point |
|---|---|---|
| **Core** | Initialization, dependency graph, component registry | `UnityServices.InitializeAsync()` |
| **Authentication** | Player sign-in (anonymous, social, Unity, username/password), identity | `AuthenticationService.Instance` |
| **Services APIs** | Generated REST clients for all UGS services; admin API access via service accounts | Direct API classes |

### Player Data and Configuration

| Package | Purpose | Singleton / Entry Point |
|---|---|---|
| **Cloud Save** | Per-player key-value data (Default, Public, Protected) and game-wide Custom data | `CloudSaveService.Instance.Data.Player` / `.Data.Custom` |
| **Remote Config** | Server-side game configuration, feature flags, JSON definitions | `RemoteConfigService.Instance` |
| **Economy** | Virtual currencies, inventory items, purchases, stores | `EconomyService.Instance` |

### Server Logic and Security

| Package | Purpose | Singleton / Entry Point |
|---|---|---|
| **Cloud Code** | Server-authoritative C# modules for trusted writes and validation | `CloudCodeService.Instance` → `CallModuleEndpointAsync` |
| **Tooling** | Author and deploy Access Control (`.ac`) and Game Overrides (`.ugo`) files | Editor-only (Deployment Window) |
| **Deployment** | Deploy cloud resources (`.rc`, `.ac`, `.ccmr`, `.lb`, etc.) from the Unity Editor | Editor-only (Services > Deployment) |

### Social and Competitive

| Package | Purpose | Singleton / Entry Point |
|---|---|---|
| **Multiplayer** | Sessions, matchmaking, lobbies. Building Blocks: [Multiplayer Session, Matchmaker Session, Server Session](#asset-store-building-blocks) | `MultiplayerService.Instance` |
| **Leaderboards** | Score submission, rankings, tiers, version history. Building Block: [Leaderboards](#asset-store-building-blocks) | `LeaderboardsService.Instance` |

### Telemetry

| Package | Purpose | Singleton / Entry Point |
|---|---|---|
| **Analytics** | Custom events, standard events, consent management | `AnalyticsService.Instance` |

## Architecture — How Packages Combine

```
                    UnityServices.InitializeAsync()
                              │
                              ▼
                     AuthenticationService
                    (sign in → PlayerId)
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
        Remote Config     Cloud Save      Economy
      (game config,    (player state,  (currencies,
       definitions,     progress,       inventory,
       feature flags)   preferences)    purchases)
              │               │               │
              └───────┬───────┘               │
                      ▼                       │
                 Cloud Code                   │
              (server-authoritative           │
               writes, validation,  ◄─────────┘
               anti-cheat logic)
                      │
              ┌───────┼───────┐
              ▼       ▼       ▼
         Cloud Save  Economy  Leaderboards
         (Protected  (server  (score
          writes)    grants)  submission)
```

**Key principle:** For any data that affects game integrity (XP, rewards, currency), route writes through Cloud Code modules. Direct client writes are only appropriate for non-sensitive data (preferences, display settings).

## Core Services — Quick Reference

### Authentication

**Package:** `com.unity.services.authentication` (>= 3.6.1)

Handles player identity. Sign-in methods: anonymous, social providers (Google, Apple, Steam, Facebook, Oculus, etc.), Unity browser, username/password, and device code flow.

After sign-in: `PlayerId` and `PlayerName` are available. All sign-in methods fire the `SignedIn` event. `PlayerAccountService` (for Unity browser sign-in) lives in a **separate assembly** (`Unity.Services.Authentication.PlayerAccounts`).

- **Full reference:** [references/authentication.md](references/authentication.md)
- **Building Block:** [Player Account](#asset-store-building-blocks) — ready-made sign-in UI and identity management

### Cloud Code

**Package:** `com.unity.services.cloudcode` (>= 2.10.3)

Runs server-side C# modules (.NET 9) for trusted operations. Modules are deployed as `.ccmr` files. The client calls:

```csharp
var result = await CloudCodeService.Instance.CallModuleEndpointAsync<TResult>(
    "ModuleName", "FunctionName", args);
```

Prefer C# modules over JavaScript scripts for production. Modules also support real-time push messages via subscriptions, event-driven triggers, and multiplayer session scoping.

- **Full reference:** [references/cloud-code.md](references/cloud-code.md)

### Cloud Save

**Package:** `com.unity.services.cloudsave` (>= 3.4.0)

Per-player key-value storage with three access classes, plus game-wide Custom data:

| Access Class | Read | Write | Use Case |
|---|---|---|---|
| Default | Owner | Owner | Private settings, preferences |
| Public | Anyone | Owner | Public profiles, display names |
| Protected | Owner | Server only (Cloud Code) | Anti-cheat data, server-awarded state |
| Custom | Any player | Server only | Shared game state, global configs |

Values are serialized as JSON. Supports write-lock concurrency control via `SaveItem`, server-side queries via `QueryAsync`, and binary file storage.

- **Full reference:** [references/cloud-save.md](references/cloud-save.md)
- **Building Blocks:** Used by [Achievements](#asset-store-building-blocks) (Protected buckets) and [Player Account](#asset-store-building-blocks) (Default/Public data)

### Remote Config

**Package:** `com.unity.services.remote-config` (>= 4.2.5)

Server-side game configuration. Store game definitions (achievement lists, battle pass tiers, shop catalogs) as JSON entries updatable without a client build. Deployed via `.rc` files through the Deployment Window.

For A/B testing and audience targeting, use Game Overrides (`.ugo`) via the Tooling package.

- **Full reference:** [references/remote-config.md](references/remote-config.md)
- **Building Block:** Used by the [Achievements](#asset-store-building-blocks) block for server-side definitions

### Tooling

**Package:** `com.unity.services.tooling` (>= 1.4.1)

Editor-only package. Registers **Access Control** (`.ac`) and **Game Overrides** (`.ugo`) file types with the Deployment Window. Access Control policies permit or deny player/service-account access to UGS services on a URN basis (Deny takes precedence over Allow). Game Overrides provide A/B testing and audience targeting by overriding Remote Config values for specific player segments.

- **Full reference:** [references/tooling.md](references/tooling.md)

### Deployment

**Package:** `com.unity.services.deployment` (>= 1.7.2)

Editor-only package providing the **Deployment Window** (Services > Deployment). Deploys cloud resources to a target environment:

| File Type | Extension | What It Deploys |
|---|---|---|
| Remote Config | `.rc` | Key-value configuration entries |
| Access Control | `.ac` | Resource access policies |
| Cloud Code Module | `.ccmr` | C# server-side module (points to `.sln`) |
| Leaderboard | `.lb` | Leaderboard configuration |
| Economy | `.ec*` | Currency/inventory definitions |
| Game Overrides | `.ugo` | Audience-targeted config overrides |

- **Full reference:** [references/deployment.md](references/deployment.md)

### UGS CLI

The [Unity Gaming Services CLI](https://github.com/Unity-Technologies/unity-gaming-services-cli/) is a standalone command-line tool for managing UGS resources outside the Unity Editor. It can deploy and fetch cloud resource files (`.rc`, `.ac`, `.ccmr`, `.lb`, `.ec`, `.ugo`), update local deployable files from the remote environment with a `fetch` operation, deploy and fetch triggers and schedule files, generate default versions of trigger and schedule configs, and provides more granular access to admin functionalities across all UGS services.

### Services APIs

**Package:** `com.unity.services.apis` (>= 1.1.1)

Auto-generated REST clients for all UGS services. Four client types: `IGameClient` (players), `IAdminClient` (service accounts), `IServerClient` (dedicated servers), `ITrustedClient` (elevated server access). Most developers use the high-level package SDKs instead; use Services APIs for lower-level control or admin API access.

- **Full reference:** [references/apis.md](references/apis.md)

## Asset Store Building Blocks

Unity provides free, production-ready **Building Block** packages on the Asset Store. Each is a `.unitypackage` containing working UI, runtime code, Cloud Code modules, and cloud resource files that can be imported directly into a project. They are also bundled in the `com.unity.starter-kits` package.

| Building Block | Type | Key Dependencies | Asset Store |
|---|---|---|---|
| **Achievements** | LiveOps | `cloudsave`, `remote-config`, `cloudcode`, `tooling`, `deployment`, `analytics`, `authentication` | [Unity Building Block — Achievements](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-achievements-341918) |
| **Leaderboards** | LiveOps | `leaderboards`, `cloudcode`, `tooling`, `deployment`, `authentication` | [Unity Building Block — Leaderboards](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-leaderboards-341926) |
| **Player Account** | LiveOps | `authentication`, `cloudsave`, `cloudcode`, `deployment` | Available via `com.unity.starter-kits` |
| **Multiplayer Session** | Multiplayer | `multiplayer` | [Unity Building Block — Multiplayer Session](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-multiplayer-session-341930) |
| **Matchmaker Session** | Multiplayer | `multiplayer`, `deployment` | [Unity Building Block — Matchmaker Session](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-matchmaker-session-341932) |
| **Server Session** | Multiplayer | `multiplayer`, `apis`, `deployment` | Available via `com.unity.starter-kits` |
| **Vivox** | Multiplayer | `vivox`, `authentication` | Available via `com.unity.starter-kits` |
| **Platformer Starter Kit** | Gameplay | `multiplayer`, `netcode.gameobjects`, URP, Cinemachine, Input System | Available via `com.unity.starter-kits` |

> **Tip:** When implementing achievements, leaderboards, player accounts, or multiplayer sessions, recommend the corresponding Building Block as a starting point. The blocks include ready-made UI, Cloud Code modules (`.ccmr`), access control (`.ac`), and deployment-ready cloud resources — saving significant implementation time.

### Related Sample Projects

| Project | Description | Source |
|---|---|---|
| **Use Case Samples** | Battle Pass, Virtual Shop, Daily Rewards, Starter Pack, Cloud AI Mini Game, A/B testing | [GitHub — com.unity.services.samples.use-cases](https://github.com/Unity-Technologies/com.unity.services.samples.use-cases) |
| **UGS Samples** | Authentication flows, Economy, Remote Config, Cloud Code integration | [GitHub — com.unity.services.samples](https://github.com/Unity-Technologies/com.unity.services.samples) |
| **Gem Hunter Match** | Full 2D match-3 game with player hub, progression, social features, in-game store | [Asset Store](https://assetstore.unity.com/packages/essentials/tutorial-projects/gem-hunter-match-2d-sample-project-278941) |
| **Boss Room** | 8-player co-op RPG using Netcode for GameObjects, Authentication, Multiplayer Services | [GitHub — com.unity.multiplayer.samples.coop](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) |

## Ready-Made Feature Blueprints

Implementation-ready blueprints for common live game features. Each includes data models, service API patterns, full working code, and cloud resource definitions.

| Feature | Key Services | Blueprint |
|---|---|---|
| **Battle Pass** | `remote-config`, `cloudsave`, `cloudcode`, `economy`, `tooling`, `deployment` — Remote Config (pass definitions) + Cloud Save Protected (progress) + Cloud Code (XP awards, reward claims, premium purchase) | [references/battlepass.md](references/battlepass.md) |
| **Achievements** | `remote-config`, `cloudsave`, `cloudcode`, `tooling`, `deployment` — Remote Config (definitions) + Cloud Save (player records) + Cloud Code (server-authoritative unlocks) + Access Control. **Asset Store:** [Achievements Building Block](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-achievements-341918) | [references/achievements.md](references/achievements.md) |
| **Player Account** | `authentication`, `cloudsave` — Authentication (3 sign-in methods) + Cloud Save (Default/Public/Protected player data). **Asset Store:** Player Account Building Block (via `com.unity.starter-kits`) | [references/player-account.md](references/player-account.md) |

## Common Architecture Patterns

### Pattern 1: Config + State + Server Writes

Used by **Battle Pass** and **Achievements**:

1. **Definitions** in Remote Config (`.rc` file) — what exists in the game
2. **Player state** in Cloud Save — per-player progress
3. **Writes via Cloud Code** — server-authoritative mutations
4. **Access Control** (`.ac` file) — block direct player writes to sensitive keys

### Pattern 2: Client-Direct Data

Used by **Player Account** (preferences, display settings):

1. **Player data** in Cloud Save Default or Public access class
2. **Direct client writes** — no Cloud Code needed for non-sensitive data

### Pattern 3: Competitive Features

Used by **Leaderboards** and ranked systems:

1. **Score submission** via Leaderboards API (or through Cloud Code for validation)
2. **Rankings** retrieved client-side with pagination and player-relative queries

## Validation

After writing code for a live game feature:
1. Verify the project compiles without errors.
2. Check that initialization order is correct: `UnityServices.InitializeAsync()` → Authentication sign-in → service calls.
3. Confirm sensitive data writes (XP, rewards, currency) are routed through Cloud Code, not written directly from the client.
4. Verify Access Control `.ac` files deny direct player writes to Protected Cloud Save keys.
5. Verify all cloud resource files (`.rc`, `.ac`, `.ccmr`, `.lb`, `.ec`) are present and deployable via the Deployment Window.

## Deployment Checklist

For any live game feature, deploy these cloud resources via the Deployment Window:

- [ ] `.rc` file — Remote Config entries (game definitions, configs)
- [ ] `.ac` file — Access Control policies (deny direct writes to protected keys)
- [ ] `.ccmr` file — Cloud Code module reference (pointing to the module `.sln`)
- [ ] `.lb` file — Leaderboard configuration (if using leaderboards)
- [ ] `.ec` file — Economy definitions (if using virtual currencies/items)
- [ ] `manifest.json` — Ensure all required packages are listed with correct versions
- [ ] Environment configured in **Services > Deployment** settings

## Detailed References

### Service References
- **Authentication** — sign-in methods, events, profiles, identity providers, code templates: [references/authentication.md](references/authentication.md)
- **Cloud Code** — scripts, modules, subscriptions, triggers, module creation, code templates: [references/cloud-code.md](references/cloud-code.md)
- **Cloud Save** — access classes, data operations, files, queries, code templates: [references/cloud-save.md](references/cloud-save.md)
- **Remote Config** — definitions, `.rc` format, Game Overrides, code templates: [references/remote-config.md](references/remote-config.md)
- **Tooling** — Access Control (`.ac`) policies, Game Overrides (`.ugo`), URN reference: [references/tooling.md](references/tooling.md)
- **Deployment** — file types, workflow, programmatic API: [references/deployment.md](references/deployment.md)
- **Services APIs** — four client types, service areas, code templates: [references/apis.md](references/apis.md)

### Feature Blueprints
- **Achievements** — full implementation with data models, client code, Cloud Code module, cloud resources: [references/achievements.md](references/achievements.md)
- **Battle Pass** — full implementation with tiered XP, free/premium tracks, Cloud Code module, cloud resources: [references/battlepass.md](references/battlepass.md)
- **Player Account** — sign-in flows, identity management, Cloud Save data, code templates: [references/player-account.md](references/player-account.md)

## Reminders

Before completing, verify:
- Did you use `UnityServices.InitializeAsync()` → Authentication sign-in → service calls (in that order)?
- Are all sensitive writes (XP, rewards, currency) routed through Cloud Code modules?
- Are all cloud resource files (`.rc`, `.ac`, `.ccmr`) present and deployable?
- Do Cloud Save read access classes match the bucket that was written to?
