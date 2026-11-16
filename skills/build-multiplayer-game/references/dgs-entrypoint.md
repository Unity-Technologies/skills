# Dedicated game server entrypoint

## Contents

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in. Project and package paths in code spans are relative to the user's project or the package root.

- [Overview](#overview)
- [Build and assembly constraints](#build-and-assembly-constraints-unity_server)
- [`IMultiplayerServerService` capabilities](#imultiplayerserverservice-capabilities)
- [API reference](#api-reference)
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

Default to `#if UNITY_SERVER` around the calls. Use the assembly definition instead when a whole assembly is server-only, because then no `#if` is needed in its files. Either way, client and player builds must not need the Server assembly.

## `IMultiplayerServerService` capabilities

| Area | What to use |
|------|-------------|
| **Create session** | **`CreateSessionAsync(SessionOptions)`** — new server session from options. |
| **Create or join by id** | **`CreateSessionAsync(string sessionId, SessionOptions)`** — server session with a chosen session id (create if missing, join if present per SDK behavior). |
| **Create from matchmaker** | **`CreateMatchSessionAsync(string matchId, SessionOptions)`** — server session tied to a Matchmaker match id; uses matchmaker configuration on options when applicable. |

> **`GetSessionAsync`** exists on **`IMultiplayerServerService`** for package-internal use and is **`internal`** in the SDK source; treat the three **`Create*`** methods above as the supported public server entry surface for session creation from game code.

## API reference

The three `Create*` methods above take `SessionOptions` and return `IServerSession`. Read [game server hosting support](https://docs.unity.com/en-us/mps-sdk/game-server-hosting-support.md) before writing server code, and the [Multiplayer Services package scripting API](https://docs.unity3d.com/Packages/com.unity.services.multiplayer@<VERSION>) for exact signatures.

## Matchmaker server extensions (`MatchmakerServerExtensions`)

All members below are declared in **`Unity.Services.Multiplayer.Server`** (`MatchmakerServerExtensions`).

`WithBackfillingConfiguration` on `SessionOptions` configures backfill before create or match session, and `StartBackfillingAsync` / `StopBackfillingAsync` on a matchmade `ISession` control it. Read [Matchmaker backfill](https://docs.unity.com/en-us/mps-sdk/matchmaking/mm-backfill.md) for the parameters.

## Session options (shared types)

**`SessionOptions`** and related lobby/network fields are defined in **`Unity.Services.Multiplayer`**, not in the Server assembly. For property tables, fluent **`SessionOptionsExtensions`**, and networking helpers, use **`entrypoints.md`** — apply the same options when calling **`IMultiplayerServerService`** create APIs on dedicated servers.

## Errors

Async methods on `IMultiplayerServerService` throw `SessionException` on failure, the same as the client session APIs. Read `SessionException`'s `SessionError` value, then resolve it:

| `SessionError` | Root cause | Resolution |
| --- | --- | --- |
| `InvalidParameter` | A parameter is missing or malformed, for example `SessionOptions.MaxPlayers` left at `0` or an empty match id | 1. Print the options and match id you passed. 2. Set `MaxPlayers` above 0 and pass the match id from the allocation unchanged. 3. Retry. |
| `NotAuthorized` or `Forbidden` | The server is not signed in, or its credentials lack permission for the project or environment | 1. Confirm Unity Services initialized and the server signed in with a service account (`ServerAuthenticationService.Instance.SignInWithServiceAccountAsync`) before the call; backfill also needs the service account to have the Matchmaker Backfill Admin role. 2. Check the server's project and environment match the matchmaker's. 3. Retry. |
| `SessionNotFound` or `AllocationNotFound` | The match id does not name a live allocation, often because it expired or came from another environment | 1. Log the match id the server received. 2. Create the match session as soon as the allocation arrives. 3. If it still fails, rerun the matchmaking flow and use the new id. |
| `RateLimitExceeded` | Too many session calls in a short time | 1. Remove retry loops without a delay. 2. Back off and retry once. |

For other values, read the `lobby-error-messages` page in the Multiplayer Services docs.
