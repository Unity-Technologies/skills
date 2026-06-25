# Services APIs Reference

## Table of Contents

- [Overview](#overview)
- [When to Use This Package](#when-to-use-this-package)
- [Four Client Types](#four-client-types)
- [IAdminClient](#iadminclient)
  - [Service Area Properties](#iadminclient-service-area-properties)
  - [Authentication](#iadminclient-authentication)
  - [ICloudSaveDataAdminApi](#icloudsavedataadminapi)
- [IGameClient](#igameclient)
- [IServerClient](#iserverclient)
- [ITrustedClient](#itrustedclient)
- [Code Templates](#code-templates)
  - [Set Up an Admin Client](#set-up-an-admin-client)
  - [Write Game Data (Custom Data) from the Editor](#write-game-data-custom-data-from-the-editor)
  - [Write Player Data from the Editor](#write-player-data-from-the-editor)
  - [Set Up a Game Client](#set-up-a-game-client-player-device)
  - [Set Up a Server Client](#set-up-a-server-client)

---

## Overview

Unified entry point for Unity Gaming Services client types. Provides auto-generated REST clients for all UGS services through a single dependency.

- **Package:** `com.unity.services.apis`
- **Namespace:** `Unity.Services.Apis`
- **Assembly:** `Unity.Services.Apis`
- **Availability:** `IAdminClient` is editor-only (`#if UNITY_EDITOR`). Game/Server/Trusted clients work at runtime.

---

## When to Use This Package

> **CRITICAL — all admin API interactions go through `com.unity.services.apis`.**
> Any editor tool, Deployment Window integration, or server-side script that needs to read
> or write UGS data with elevated privileges must use this package. There is no other
> supported path for admin-level operations from within the Unity Editor.

Use cases for `IAdminClient`:

- **Deployment Window custom providers** — deploy commands that write to Cloud Save, Economy, Leaderboards, etc.
- **Editor tools** — custom inspectors or windows that read/write game-wide data
- **Build pipelines** — pre-build steps that push config data to UGS

Use `IGameClient` for runtime player-facing code. Use `IServerClient` / `ITrustedClient` for dedicated game servers.

---

## Four Client Types

| Client | Interface | Context | Auth |
|---|---|---|---|
| Admin | `IAdminClient` | Editor tools, deployment integrations | Service account (key ID + secret) |
| Game | `IGameClient` | Player device at runtime | Player sign-in (anonymous, Unity, password) |
| Server | `IServerClient` | Dedicated game servers | Server key |
| Trusted | `ITrustedClient` | Servers needing admin-level player data access | Service account |

---

## IAdminClient

Used for backend tooling, editor extensions, Deployment Window integrations, and any operation that requires elevated privileges. **Editor-only** — guarded by `#if UNITY_EDITOR || ENABLE_RUNTIME_ADMIN_APIS`.

### IAdminClient Authentication

```csharp
using Unity.Services.Apis;

var adminClient = UnityServicesApiClient.AdminClient;
adminClient.SetServiceAccount(keyId, keySecret);
```

Service account credentials are created in the Unity Dashboard under **Project Settings > Service Accounts**. The key ID and secret are passed at runtime — do not hard-code them.

### IAdminClient Service Area Properties

| Property | Type | Service |
|---|---|---|
| `CloudSaveData` | `ICloudSaveDataAdminApi` | Cloud Save — player data, custom (game-wide) data, all access classes |
| `CloudSaveFiles` | `ICloudSaveFilesAdminApi` | Cloud Save — file storage |
| `CloudCodeModules` | `ICloudCodeModulesAdminApi` | Cloud Code — C# modules |
| `CloudCodeScripts` | `ICloudCodeScriptsAdminApi` | Cloud Code — JS scripts |
| `Economy` | `IEconomyAdminApi` | Economy — config and player data |
| `RemoteConfig` | `IConfigsAdminApi` | Remote Config |
| `RemoteConfigSchemas` | `ISchemasAdminApi` | Remote Config schemas |
| `GameOverrides` | `IGameOverridesAdminApi` | Game Overrides |
| `Leaderboards` | `ILeaderboardsAdminApi` | Leaderboards |
| `Environment` | `IEnvironmentAdminApi` | Environment management |
| `Logs` | `ILogsAdminApi` | Logs |
| `PlayerAuthentication` | `IPlayerAuthenticationAdminApi` | Player auth management |
| `PlayerPolicy` | `IPlayerPolicyAdminApi` | Player access policies |
| `ProjectPolicy` | `IProjectPolicyAdminApi` | Project access policies |
| `Scheduler` | `ISchedulerAdminApi` | Scheduler |
| `ServiceAuthentication` | `IServiceAuthenticationAdminApi` | Service auth |
| `Triggers` | `ITriggersAdminApi` | Triggers |

### ICloudSaveDataAdminApi

This is the key interface for reading and writing Cloud Save data with admin privileges. All methods require `projectId` and `environmentId`.

#### Player Data (all access classes)

| Method | Description |
|---|---|
| `GetItems(projectId, envId, playerId, keys?, after?)` | Get player items (Default bucket) |
| `SetItem(projectId, envId, playerId, body)` | Set a player item |
| `SetItemBatch(projectId, envId, playerId, body)` | Set multiple player items |
| `DeleteItem(projectId, envId, playerId, key, writeLock?)` | Delete a player item |
| `DeleteItems(projectId, envId, playerId)` | Delete all player items |
| `GetKeys(projectId, envId, playerId, after?)` | List player keys |

Same pattern for **Public** (`GetPublicItems`, `SetPublicItem`, etc.) and **Protected** (`GetProtectedItems`, `SetProtectedItem`, etc.).

#### Custom Data (game-wide)

Custom Data is game-wide key-value storage not tied to any player. The `customId` parameter is a namespace (1-50 chars, alphanumeric/underscores/hyphens).

| Method | Description |
|---|---|
| `GetCustomItems(projectId, envId, customId, keys?, after?)` | Read game-wide items |
| `SetCustomItem(projectId, envId, customId, body)` | Write a game-wide item |
| `SetCustomItemBatch(projectId, envId, customId, body)` | Write multiple game-wide items |
| `DeleteCustomItem(projectId, envId, customId, key, writeLock?)` | Delete a game-wide item |
| `DeleteCustomItems(projectId, envId, customId)` | Delete all items in a custom namespace |
| `GetCustomKeys(projectId, envId, customId, after?)` | List keys in a custom namespace |

Same pattern for **Private Custom** (`GetPrivateCustomItems`, `SetPrivateCustomItem`, etc.).

> **This is how you write game data from the editor.** The client SDK's `ICustomDataService`
> is read-only — only `IAdminClient.CloudSaveData` can write to Custom Data.

---

## IGameClient

Created via `UnityServicesApiClient.GameClient` after player authentication. Runtime only.

| Property | Type | Service |
|---|---|---|
| `CloudSaveData` | `ICloudSaveDataApi` | Cloud Save player data |
| `CloudSaveFiles` | `ICloudSaveFilesApi` | Cloud Save files |
| `CloudCode` | `ICloudCodeApi` | Cloud Code invocation |
| `EconomyConfiguration` | `IEconomyConfigurationApi` | Economy config |
| `EconomyCurrencies` | `IEconomyCurrenciesApi` | Currency balances |
| `EconomyInventory` | `IEconomyInventoryApi` | Player inventory |
| `EconomyPurchases` | `IEconomyPurchasesApi` | Purchases |
| `Leaderboards` | `ILeaderboardsApi` | Leaderboard scores |
| `Lobby` | `ILobbyApi` | Lobby management |
| `RemoteConfig` | `IRemoteConfigSettingsApi` | Remote Config |
| `PlayerAuthentication` | `IPlayerAuthenticationApi` | Auth management |
| `PlayerNames` | `IPlayerNamesApi` | Display names |
| `FriendsRelationships` | `IFriendsRelationshipsApi` | Friend lists |
| `FriendsPresence` | `IFriendsPresenceApi` | Presence |
| `RelayAllocations` | `IRelayAllocationsApi` | Relay |
| `QosDiscovery` | `IQosDiscoveryApi` | QoS |
| `Analytics` | `IAnalyticsApi` | Analytics |

Most developers use the high-level SDKs (`CloudSaveService.Instance`, `EconomyService.Instance`) instead of `IGameClient` directly.

---

## IServerClient

Used from dedicated game servers. Authenticated with a server key or service account.

```csharp
var serverClient = UnityServicesApiClient.ServerClient;
await serverClient.SignInFromServer();
```

| Property | Type | Service |
|---|---|---|
| `CloudSaveData` | `ICloudSaveDataApi` | Cloud Save player data |
| `CloudSaveFiles` | `ICloudSaveFilesApi` | Cloud Save files |
| `CloudCode` | `ICloudCodeApi` | Cloud Code |
| `EconomyConfiguration` | `IEconomyConfigurationApi` | Economy config |
| `EconomyCurrencies` | `IEconomyCurrenciesApi` | Currencies |
| `EconomyInventory` | `IEconomyInventoryApi` | Inventory |
| `EconomyPurchases` | `IEconomyPurchasesApi` | Purchases |
| `Leaderboards` | `ILeaderboardsApi` | Leaderboards |
| `Lobby` | `ILobbyApi` | Lobbies |
| `PlayerNames` | `IPlayerNamesApi` | Display names |

---

## ITrustedClient

Elevated server client with admin-equivalent access to player data. Use when a dedicated server needs to read or modify any player's data with elevated privileges.

```csharp
var trustedClient = UnityServicesApiClient.TrustedClient;
trustedClient.SetServiceAccount(keyId, keySecret);
await trustedClient.SignInWithServiceAccount(projectId, environmentId);
```

Same service area properties as `IServerClient`, plus `PlayerAuthentication` and `MultiplayAllocations`/`MultiplayFleets`.

---

## Code Templates

### Set Up an Admin Client

```csharp
using Unity.Services.Apis;

// Admin client is editor-only. Guard with #if UNITY_EDITOR in runtime assemblies.
var adminClient = UnityServicesApiClient.AdminClient;
adminClient.SetServiceAccount("your-key-id", "your-key-secret");
```

### Write Game Data (Custom Data) from the Editor

This is the pattern for Deployment Window integrations that push JSON into Cloud Save Custom Data.

```csharp
using Unity.Services.Apis;
using Unity.Services.Apis.Admin.CloudSave;

var adminClient = UnityServicesApiClient.AdminClient;
adminClient.SetServiceAccount(keyId, keySecret);

string projectId = CloudProjectSettings.projectId;
string environmentId = "your-environment-id";
string customId = "game-config";  // namespace for your game-wide data

// Write a single item
var body = new SetItemBody("level_data", levelDataJson);
await adminClient.CloudSaveData.SetCustomItem(projectId, environmentId, customId, body);

// Write multiple items in a batch
var batchBody = new SetItemBatchBody(new List<SetItemBatchBodyItem>
{
    new SetItemBatchBodyItem("enemies", enemiesJson),
    new SetItemBatchBodyItem("weapons", weaponsJson)
});
await adminClient.CloudSaveData.SetCustomItemBatch(projectId, environmentId, customId, batchBody);
```

### Write Player Data from the Editor

```csharp
// Write to a specific player's Protected bucket
var body = new SetItemBody("achievements", achievementsJson);
await adminClient.CloudSaveData.SetProtectedItem(
    projectId, environmentId, playerId, body);
```

### Set Up a Game Client (Player Device)

```csharp
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Apis;

async Task SetupGameClient()
{
    await UnityServices.InitializeAsync();
    await AuthenticationService.Instance.SignInAnonymouslyAsync();

    // Game client is automatically authenticated via the signed-in player
    var client = UnityServicesApiClient.GameClient;
    var scores = await client.Leaderboards.GetScoresAsync("WEEKLY_LB");
}
```

### Set Up a Server Client

```csharp
using Unity.Services.Apis;

// serverAuthToken provided by the Multiplay server environment
var serverClient = UnityServicesApiClient.ServerClient;
await serverClient.SignInFromServer();

await serverClient.Lobby.CreateOrJoinLobbyAsync(...);
```
