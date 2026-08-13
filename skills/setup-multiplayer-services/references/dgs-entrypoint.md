## Table of Contents

- [Overview](#overview)
- [Build and assembly constraints](#build-and-assembly-constraints-unity_server)
- [`IMultiplayerServerService` capabilities](#imultiplayerserverservice-capabilities)
- [Method signatures](#method-signatures)
- [Matchmaker server extensions](#matchmaker-server-extensions-matchmakerserverextensions)
- [Session options (shared types)](#session-options-shared-types)
- [Errors](#errors)

## Overview

Dedicated Game Server (DGS) session entrypoints live in the **`Unity.Services.Multiplayer.Server`** assembly only. They complement the client **`IMultiplayerService`** surface documented in **`entrypoints.md`**.

| Topic | Details |
|--------|---------|
| **Assembly** | **`Unity.Services.Multiplayer.Server`** |
| **Service access** | **`MultiplayerServerService.Instance`** (static) or **`unityServices.GetMultiplayerServerService()`** via **`Unity.Services.Core.UnityServicesExtensions`** after Services initialization on a server build. |
| **Core type** | **`IMultiplayerServerService`** — create and resolve **server** sessions; async methods return **`IServerSession`** (session handle for dedicated server; types from the main **`Unity.Services.Multiplayer`** assembly). |

## Build and assembly constraints (`UNITY_SERVER`)

The **`Unity.Services.Multiplayer.Server`** assembly is compiled only when **`UNITY_SERVER`** or **`ENABLE_UCS_SERVER`** is defined (see the package **`Unity.Services.Multiplayer.Server.asmdef`** **`defineConstraints`**).

Any **game or tool code** that references **`Unity.Services.Multiplayer.Server`** must satisfy one of the following:

| Approach | What to do |
|----------|------------|
| **Scripting define** | Wrap references (types, calls, `using` that pulls server-only APIs) in **`#if UNITY_SERVER`** … **`#endif`** (or a define that implies the same server build), so non-server targets do not compile that code. |
| **Assembly Definition** | In the **`.asmdef`** of the assembly that references **`Unity.Services.Multiplayer.Server`**, set **`defineConstraints`** to include **`UNITY_SERVER`** so the dependent assembly is not built for client-only targets. |

Use one or both so client/player builds never require the Server assembly to be present or linked incorrectly.

## `IMultiplayerServerService` capabilities

| Area | What to use |
|------|-------------|
| **Create session** | **`CreateSessionAsync(SessionOptions)`** — new server session from options. |
| **Create or join by id** | **`CreateSessionAsync(string sessionId, SessionOptions)`** — server session with a chosen session id (create if missing, join if present per SDK behavior). |
| **Create from matchmaker** | **`CreateMatchSessionAsync(string matchId, SessionOptions)`** — server session tied to a Matchmaker match id; uses matchmaker configuration on options when applicable. |

> **`GetSessionAsync`** exists on **`IMultiplayerServerService`** for package-internal use and is **`internal`** in the SDK source; treat the three **`Create*`** methods above as the supported public server entry surface for session creation from game code.

## Method signatures

```csharp
// Creates a new dedicated-server session. Returns IServerSession. Throws SessionException on failure.
Task<IServerSession> CreateSessionAsync(SessionOptions sessionOptions)

// Creates or joins a server session using an explicit session id.
Task<IServerSession> CreateSessionAsync(string sessionId, SessionOptions sessionOptions)

// Creates a server session from a Matchmaker match id and session options.
Task<IServerSession> CreateMatchSessionAsync(string matchId, SessionOptions sessionOptions)
```

## Matchmaker server extensions (`MatchmakerServerExtensions`)

All members below are declared in **`Unity.Services.Multiplayer.Server`** (`MatchmakerServerExtensions`).

```csharp
// Configure backfill behavior on SessionOptions before create/match session.
T WithBackfillingConfiguration<T>(this T options, bool enable, bool automaticallyRemovePlayers,
    bool autoStart, int playerConnectionTimeout, int backfillingLoopInterval) where T : SessionOptions

// Start backfilling on a matchmade session (server / session handle).
Task StartBackfillingAsync(this ISession session)

// Stop backfilling on a matchmade session.
Task StopBackfillingAsync(this ISession session)
```

## Session options (shared types)

**`SessionOptions`** and related lobby/network fields are defined in **`Unity.Services.Multiplayer`**, not in the Server assembly. For property tables, fluent **`SessionOptionsExtensions`**, and networking helpers, use **`entrypoints.md`** — apply the same options when calling **`IMultiplayerServerService`** create APIs on dedicated servers.

## Errors

Async methods on **`IMultiplayerServerService`** throw **`SessionException`** on failure (same family as the client **`IMultiplayerService`** session APIs).
