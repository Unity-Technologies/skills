---
name: build-live-game
description: Builds and operates a live game's backend with Unity Gaming Services. Make sure to use this skill when the user wants trusted server-side game logic, player progress, remote configuration, sign-in, achievements, battle passes or leaderboards.
---

# Build a Live Game With Unity Gaming Services

## Before You Start

1. Read the Editor version from `ProjectSettings/ProjectVersion.txt` and the installed UGS package versions from `Packages/packages-lock.json`. Pick the steps below that match (for example `.ccmu` or `.ccmr` modules), because the right workflow depends on the Editor and package versions.
2. Check whether the project is linked to a Unity project ID and environment (**Edit > Project Settings > Services**), and ask the user to link it if it is not.
3. Create a restore point you can roll back to if your changes fail: commit the project in version control. If there is no version control, copy the project folder instead.
4. Before you go forward with a workflow or diagnosis, open and read the documentation pages for the service you will use (see [Documentation Map](#documentation-map)) and the matching file under `references/`, and name the pages you read in your response. This is because your training knowledge about Unity Gaming Services might be out of date, incorrect, or for the wrong package version.
5. Do only what's asked. Don't change unrelated assets or files. Avoid long explanations.
6. When you change runtime behaviour, add `Debug.Log` lines at the points you need to verify (sign-in complete, module call returned, config fetched). You read them in Play mode during [Validation](#validation).

## Glossary

| Term | Meaning |
|---|---|
| **UGS** | Unity Gaming Services: the backend services (Authentication, Cloud Code, Cloud Save, Remote Config, Leaderboards and others). The Package Manager calls them "Unity Services" |
| **Building Block** | A free Asset Store package with UI, runtime code and cloud resource files for one feature. Its Cloud Code is legacy `.ccmr` |
| **Cloud Code module** | Server-side C# class library. Authored in the Unity project as a `.ccmu` asset on Unity 6000.5+ |
| **`.ccmr`** | Legacy module reference file that points at a C# solution built outside Unity |
| **Script** | Server-side JavaScript endpoint. Legacy: maintain existing ones, don't write new ones |
| **CloudBehaviour** | Experimental Cloud Code class that generates client bindings, synchronized state and push events |
| **Protected data** | Cloud Save player data that the player can read but only server code can write |
| **Custom data** | Cloud Save game-wide data, not tied to one player, written only by server code |
| **Cloud resource file** | A deployable config file (`.rc`, `.ac`, `.ccmu`, `.ccmr`, `.lb`, `.ugo`) that the Deployment Window pushes to a UGS environment |

## Documentation Map

Prefer `WebFetch` over `WebSearch`: it is faster and lands on the exact reference. Only fetch what you need. Pages on docs.unity.com are available as Markdown when the URL ends in `.md`. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: the installed package version as `major.minor`, for example `3.0`. Read it from `Packages/packages-lock.json` if unsure.
- **docs.unity.com**: the UGS product pages below are unversioned, so there is nothing to replace.

Pages to read before acting:

- Cloud Code C# modules: [Overview](https://docs.unity.com/en-us/cloud-code/modules/overview.md), [Get started](https://docs.unity.com/en-us/cloud-code/modules/getting-started.md), [Module structure](https://docs.unity.com/en-us/cloud-code/modules/how-to-guides/module-structure.md), [Error handling](https://docs.unity.com/en-us/cloud-code/modules/how-to-guides/error-handling.md), [Limits](https://docs.unity.com/en-us/cloud-code/modules/reference/limits.md)
- Cloud Code server authority: [Server authority](https://docs.unity.com/en-us/cloud-code/server-authority.md), [Cheat prevention](https://docs.unity.com/en-us/cloud-code/cheat-prevention.md), [UGS integration and access control](https://docs.unity.com/en-us/cloud-code/server-access-control.md)
- Authentication: [How Authentication works](https://docs.unity.com/en-us/authentication/how-authentication-works.md), [Sessions and cached players](https://docs.unity.com/en-us/authentication/session-management.md), [Common errors](https://docs.unity.com/en-us/authentication/common-errors.md)
- Cloud Save: [Player Data](https://docs.unity.com/en-us/cloud-save/concepts/player-data.md), [Game Data](https://docs.unity.com/en-us/cloud-save/concepts/game-data.md), [Cloud Save for Cloud Code](https://docs.unity.com/en-us/cloud-save/tutorials/cloud-code.md)
- Remote Config: [Remote Config files](https://docs.unity.com/en-us/remote-config/remote-config-files.md), [Code integration](https://docs.unity.com/en-us/remote-config/code-integration.md)
- Leaderboards: [Overview](https://docs.unity.com/en-us/leaderboards.md), [Unity SDK tutorial](https://docs.unity.com/en-us/leaderboards/tutorials/unity-sdk.md)
- Deployment Window: [Deployment package manual](https://docs.unity3d.com/Packages/com.unity.services.deployment@<VERSION>/manual/index.html)

## UGS Packages

| Package | Min Version | Purpose |
|---|---|---|
| `com.unity.services.core` | 1.16.0 | Initialization, dependency graph |
| `com.unity.services.authentication` | 3.6.1 | Player sign-in and identity |
| `com.unity.services.cloudcode` | 3.0.0 | Server-authoritative C# modules, authored in-project as `.ccmu`. The `2.10.x` line deploys legacy `.ccmr` only — see below |
| `com.unity.services.cloudsave` | 3.4.0 | Per-player and shared key-value storage |
| `com.unity.remote-config` | 4.2.5 | Server-side game configuration |
| `com.unity.services.deployment` | 1.7.2 | Deploy cloud resources from Editor |
| `com.unity.services.tooling` | 1.4.1 | Access Control and Game Overrides |
| `com.unity.services.apis` | 1.1.1 | Generated REST clients for all UGS services |

- [Before You Start](#before-you-start)
- [Glossary](#glossary)
- [Documentation Map](#documentation-map)
- [Initialization Pattern](#initialization-pattern)
- [Architecture — How Packages Combine](#architecture--how-packages-combine)
- [Core Services — Quick Reference](#core-services--quick-reference)
- [Common Architecture Patterns](#common-architecture-patterns)
- [Validation](#validation)
- [Detailed References](#detailed-references)
- [Final Report](#final-report)

## Initialization Pattern

Every UGS game starts the same way. `com.unity.services.core` must initialize first, then the player signs in:

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;

await UnityServices.InitializeAsync();
await AuthenticationService.Instance.SignInAnonymouslyAsync();
// All other services are now ready
```

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
        Remote Config     Cloud Save      Multiplayer
      (game config,    (player state,   (sessions,
       definitions,     progress,        matchmaking,
       feature flags)   preferences)     lobbies)
              │               │
              └───────┬───────┘
                      ▼
                 Cloud Code
              (server-authoritative
               writes, validation,
               anti-cheat logic)
                      │
              ┌───────┴───────┐
              ▼               ▼
         Cloud Save       Leaderboards
         (Protected       (score
          writes)          submission)
```

**Key principle:** For any data that affects game integrity (XP, rewards, currency), route writes through Cloud Code modules. Direct client writes are only appropriate for non-sensitive data (preferences, display settings).

## Core Services — Quick Reference

### Authentication

**Package:** `com.unity.services.authentication` (>= 3.6.1)

Handles player identity; [How Authentication works](https://docs.unity.com/en-us/authentication/how-authentication-works.md) covers the sign-in methods. After sign-in `PlayerId` is available and the `SignedIn` event fires. `PlayerAccountService` (Unity browser sign-in) lives in a **separate assembly**: `Unity.Services.Authentication.PlayerAccounts`.

- **Full reference:** [references/authentication.md](references/authentication.md)

### Cloud Code

**Package:** `com.unity.services.cloudcode` **`3.0.0`** for native `.ccmu` authoring (Unity 6000.5+); `>= 2.10.3` maintains legacy `.ccmr` modules. Before you author a `.ccmu`, bump a manifest that pins `2.10.x` to `3.0.0` or later.

Runs server-side C# modules (.NET 8; .NET 10 on Unity 6.7+) for trusted operations. Check the project's Editor version, then **author new modules as `.ccmu` on Unity 6000.5+**: create the module inside the Unity project with **Create > Services > Cloud Code Module Script**. Unity compiles it with the project and generates its client bindings at compile time. `.ccmr` is legacy: on 6000.5+ maintain existing ones only; on an older Editor, author new modules as `.ccmr`.
Call a `.ccmu` module from the client only through its generated bindings, never with `CallModuleEndpointAsync`. Use `CloudCodeService.Instance.CallModuleEndpointAsync` for legacy `.ccmr` modules only.
Write new JavaScript scripts only when the user asks for them; maintain existing ones.

- **Full reference:** [references/cloud-code-modules.md](references/cloud-code-modules.md)

**CloudBehaviours** (experimental) generate a module's client bindings — RPCs, synchronized state, push
events — from one C# class. Use them for shared match state, per-player secret state or a server-owned
timer; otherwise use a plain Cloud Code module. Requires `com.unity.services.cloudcode` `3.1.0-exp.1`+
and Unity 6000.5+. Check the installed package first: if CloudBehaviours are unavailable, use a plain
stateful module (`[StateScope]`, see the modules reference) instead of stopping, unless the user asks for
the experimental package.

- **Full reference:** [references/cloud-code-cloudbehaviours.md](references/cloud-code-cloudbehaviours.md)

### Cloud Save

**Package:** `com.unity.services.cloudsave` (>= 3.4.0)

Per-player key-value storage in three access classes (Default, Public, Protected), plus game-wide Custom data. Protected and Custom data can be written only by server code, so store anti-cheat and server-awarded state there. See [Player Data](https://docs.unity.com/en-us/cloud-save/concepts/player-data.md) and [Game Data](https://docs.unity.com/en-us/cloud-save/concepts/game-data.md) for the read and write rules of each class.

- **Full reference:** [references/cloud-save.md](references/cloud-save.md)

### Remote Config

**Package:** `com.unity.services.remote-config` (>= 4.2.5)

Server-side game configuration. Store game definitions (achievement lists, battle pass tiers, shop catalogs) as JSON entries updatable without a client build. Deployed via `.rc` files through the Deployment Window.

For A/B testing and audience targeting, use Game Overrides (`.ugo`) via the Tooling package.

- **Full reference:** [references/remote-config.md](references/remote-config.md)

### Tooling

**Package:** `com.unity.services.tooling` (>= 1.4.1)

Editor-only package. Registers **Access Control** (`.ac`) and **Game Overrides** (`.ugo`) file types with the Deployment Window. Use Access Control to deny direct player writes to protected keys; Deny takes precedence over Allow.

- **Full reference:** [references/tooling.md](references/tooling.md)

### Deployment

**Package:** `com.unity.services.deployment` (>= 1.7.2)

Editor-only package providing the **Deployment Window** (Services > Deployment). Deploys cloud resources to a target environment:

The Deployment Window deploys the cloud resource files: `.rc` (Remote Config), `.ac` (Access Control), `.ccmu` (in-project Cloud Code module, preferred), `.ccmr` (legacy module reference), `.lb` (Leaderboard) and `.ugo` (Game Overrides).

- **Full reference:** [references/deployment.md](references/deployment.md)

### UGS CLI

The UGS CLI ships as a Unity CLI plugin, run as `unity ugs <command>`, to deploy and fetch cloud resources outside the Editor. Install, auth/setup and which file types it deploys: [references/ugs-cli.md](references/ugs-cli.md). Plugin system: the **unity-cli** skill.

### Services APIs

**Package:** `com.unity.services.apis` (>= 1.1.1)

Auto-generated REST clients for all UGS services, in four client types (`IGameClient`, `IAdminClient`, `IServerClient`, `ITrustedClient`). Use the high-level package SDKs by default; use Services APIs only for admin API access or when no package SDK covers the call.

- **Full reference:** [references/apis.md](references/apis.md)

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

After writing code for a live game feature, run these checks in order. If one fails, fix the cause and re-run it before moving on; if the fix does not work, go back and reread the matching documentation pages and `references/` file.

1. Check the project compiles: read the Editor console (or the Editor log) and confirm zero compile errors.
2. Check the initialization order in the code: search for `UnityServices.InitializeAsync()` and confirm it runs before the sign-in call, and that sign-in completes before any service call.
3. Check that sensitive writes (XP, rewards, currency) go through Cloud Code: search the client code for Cloud Save `SaveAsync` calls on those keys and move any you find into a module. Also confirm each Cloud Save read uses the access class of the bucket that was written to, because a mismatched class returns no data.
4. Deploy the cloud resource files the feature uses (`.rc`, `.ac`, `.ccmu` or `.ccmr`, `.lb`; skip the types it does not use) to the active environment with the `Deploy.cs` script in `references/deployment.md`, and read its output: every file must report `Success`. A file reported as `not a deployable item`, or with an `Error` status, has failed. If the project does not have both packages the script needs, `com.unity.pipeline` and `com.unity.services.deployment.api`, ask the user to deploy from the Deployment Window and report each file's status.
5. Check Access Control by its effect: if the feature deploys an `.ac` policy that denies direct player writes, enter Play mode, run one direct client Cloud Save write to a key the policy covers (a temporary `SaveAsync` call), and confirm it fails with a `403`. Remove the temporary call afterwards. Skip this step when the feature has no `.ac` file.
6. Enter Play mode, run the feature once, and confirm the `Debug.Log` lines from [Before You Start](#before-you-start) appear in the order you expect and the console has zero errors.

## Detailed References

### Service References
- **Authentication** — sign-in methods, events, profiles, identity providers, code templates: [references/authentication.md](references/authentication.md)
- **Cloud Code** — choosing a model: [references/cloud-code.md](references/cloud-code.md); scripts and modules: [references/cloud-code-modules.md](references/cloud-code-modules.md); CloudBehaviours: [references/cloud-code-cloudbehaviours.md](references/cloud-code-cloudbehaviours.md)
- **Cloud Save** — access classes, data operations, files, queries, code templates: [references/cloud-save.md](references/cloud-save.md)
- **Remote Config** — definitions, `.rc` format, Game Overrides, code templates: [references/remote-config.md](references/remote-config.md)
- **Tooling** — Access Control (`.ac`) policies, Game Overrides (`.ugo`), URN reference: [references/tooling.md](references/tooling.md)
- **Deployment** — file types, workflow, programmatic API: [references/deployment.md](references/deployment.md)
- **UGS CLI** — `unity ugs` plugin install, auth/setup, headless deploy by file type: [references/ugs-cli.md](references/ugs-cli.md)
- **Services APIs** — four client types, service areas, code templates: [references/apis.md](references/apis.md)

### Feature Blueprints
- **Choosing a starting point** — this skill's blueprints, Asset Store Building Blocks and sample projects, and how to reuse their legacy Cloud Code: [references/blueprints-and-samples.md](references/blueprints-and-samples.md)
- **Achievements** — full implementation with data models, client code, Cloud Code module, cloud resources: [references/achievements.md](references/achievements.md)
- **Battle Pass** — implementation with tiered XP, free/premium tracks, Cloud Code module, cloud resources (premium purchase is an integration stub you must complete): [references/battlepass.md](references/battlepass.md)
- **Player Account** — sign-in flows, identity management, Cloud Save data, code templates: [references/player-account.md](references/player-account.md)

## Final Report

When the work is done, end your response with a short report:

1. What you created or changed: files, packages added to `Packages/manifest.json`, and cloud resource files.
2. The checks from [Validation](#validation) that you ran and their results, including any you could not run and why.
3. Follow-ups only the user can do, for example: deploy the cloud resources to the target environment, link the project to a Unity project ID and environment, complete a stubbed integration (such as battle pass `PurchasePremium`), or restore from the restore point you created if they reject the changes.
4. Which documentation pages you read.
