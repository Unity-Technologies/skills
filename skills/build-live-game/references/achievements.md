# Achievements Reference

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Data Models](#data-models)
- [API Notes](#api-notes)
  - [Namespace Aliases](#namespace-aliases-required)
  - [Access Class Options](#access-class-options-reference)
  - [Bucket Selection](#bucket-selection)
  - [Error Handling](#error-handling)
- [Load Definitions from Remote Config](#load-definitions-from-remote-config)
- [Load Player Records from Cloud Save](#load-player-records-from-cloud-save)
- [Save Player Records (Direct Write)](#save-player-records-direct-write)
- [Server-Authoritative Writes via Cloud Code](#server-authoritative-writes-via-cloud-code)
- [Full Client Implementation](#full-client-implementation)
- [Cloud Code Module](#cloud-code-module)
  - [AchievementModule.cs](#achievementmodulecs)
  - [Server-Side Model](#server-side-model)
- [Cloud Resources](#cloud-resources)
- [Deployment Checklist](#deployment-checklist)
- [Official Unity Reference](#official-unity-reference)
- [Validation](#validation)

---

## Architecture Overview

Achievements combine Remote Config (static definitions), Cloud Save (per-player progress), and Cloud Code (server-authoritative writes). An Access Control policy locks down direct client writes in production.

### Service Mapping

| Concern | Service | Key / Endpoint |
|---|---|---|
| Achievement definitions | Remote Config | `"achievements"` key (JSON array) |
| Player records | Cloud Save (Protected or Public) | `"achievements"` key (JSON array) |
| Server-authoritative writes | Cloud Code Module | `AchievementModule` |
| Block direct writes | Access Control | `.ac` policy denying player Cloud Save writes |

### Data Flow

```
Remote Config                        Cloud Save
  "achievements" key                   "achievements" key
  ┌─────────────────┐                 ┌─────────────────┐
  │ AchievementDef- │    client       │ AchievementRe-  │
  │ inition[]       │───reads────────>│ cord[]          │
  │                 │                 │                 │
  │ Id              │                 │ Id              │
  │ Title           │                 │ Unlocked        │
  │ Description     │                 │ ProgressCount   │
  │ IsHidden        │                 │                 │
  │ ProgressTarget  │                 └────────┬────────┘
  └─────────────────┘                          │
                                      two write paths
                                ┌──────────────┴──────────────┐
                                │                             │
                         Direct Write                  Trusted Write
                       (dev / prototype)              (production)
                                │                             │
                                ▼                             ▼
                        Cloud Save                     Cloud Code
                    PublicWriteAccess-             "AchievementModule"
                    ClassOptions                          │
                                                          ▼
                                                   Cloud Save
                                               SetProtectedItemAsync
                                              (server token bypass)
```

**Key principle:** In production, route all writes through the Cloud Code module. The Access Control `.ac` file denies direct player writes to the Protected bucket, so only the server can mutate achievement records.

---

## Data Models

### AchievementDefinition (client-side)

Stored in Remote Config as a JSON array under the `"achievements"` key. Read-only from the client.

```csharp
using System;

[Serializable]
public class AchievementDefinition
{
    public string Id;
    public string Title;
    public string Description;
    public bool IsHidden;
    public int ProgressTarget;  // <= 1 = single unlock, > 1 = multi-stage
}
```

| Field | Type | Description |
|---|---|---|
| `Id` | `string` | Unique identifier (e.g. `"first_win"`) |
| `Title` | `string` | Display name shown to the player |
| `Description` | `string` | What the player must do |
| `IsHidden` | `bool` | If true, hide until unlocked (secret achievements) |
| `ProgressTarget` | `int` | Target count for multi-stage achievements; 0 or 1 = single unlock |

### AchievementRecord (client-side)

Stored in Cloud Save as a JSON array under the `"achievements"` key.

```csharp
using System;

[Serializable]
public class AchievementRecord
{
    public string Id;
    public bool Unlocked;
    public int ProgressCount;
}
```

| Field | Type | Description |
|---|---|---|
| `Id` | `string` | Matches `AchievementDefinition.Id` |
| `Unlocked` | `bool` | True once the achievement is fully earned |
| `ProgressCount` | `int` | Current progress toward `ProgressTarget` |

---

## API Notes

### Namespace Aliases (Required)

Cloud Save has identically named option classes at different namespace levels. Always use these
aliases to avoid ambiguity:

```csharp
using PlayerLoadOptions   = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using PlayerSaveOptions   = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
using PlayerDeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;
```

Without these aliases, the compiler will report ambiguous type references.

### Access Class Options Reference

| Class | Read | Write | Use case |
|---|---|---|---|
| `DefaultWriteAccessClassOptions` / `DefaultReadAccessClassOptions` | Owner only | Owner only | Private player data |
| `PublicReadAccessClassOptions(playerId)` / `PublicWriteAccessClassOptions` | Anyone | Owner + server | Achievements, scores, public profiles |
| `ProtectedReadAccessClassOptions` | Owner only | Server only (Cloud Code) | Anti-cheat, server-awarded state |

### Bucket Selection

The read access class **must** match the bucket that was written to. Cloud Save has three
separate buckets — Default, Public, and Protected. Data written to one bucket is **invisible**
when reading from another.

- **Trusted path** (Cloud Code `SetProtectedItemAsync`) — data lands in the **Protected** bucket — read with `ProtectedReadAccessClassOptions()`
- **Direct path** (`PublicWriteAccessClassOptions`) — data lands in the **Public** bucket — read with `PublicReadAccessClassOptions(playerId)`

> **Common mistake:** Switching from direct writes to trusted writes (or vice versa) without
> updating the read access class. The data appears to vanish — it's still there, just in
> the other bucket.

### Error Handling

Cloud Code and Cloud Save errors surface as typed exceptions on the client:

```csharp
try
{
    await CloudCodeService.Instance.CallModuleEndpointAsync(
        "AchievementModule", "UnlockAchievement",
        new Dictionary<string, object> { { "achievementId", "first_win" } });
}
catch (CloudCodeException ex)
{
    // ex.Reason: CloudCodeExceptionReason (e.g. ScriptError for module-thrown exceptions)
    Debug.LogError($"[Achievements] Cloud Code error: {ex.Reason} — {ex.Message}");
}
catch (CloudSaveException ex)
{
    // ex.Reason: CloudSaveExceptionReason
    // A 403 on the Protected bucket means the client attempted a direct write —
    // all writes must go through the Cloud Code module when Access Control is deployed.
    Debug.LogError($"[Achievements] Cloud Save error: {ex.Reason} — {ex.Message}");
}
```

---

## Load Definitions from Remote Config

Fetch the `"achievements"` Remote Config key and deserialize the JSON array into a list of definitions.

```csharp
using Unity.Services.RemoteConfig;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

struct UserAttributes {}
struct AppAttributes {}

async Task<List<AchievementDefinition>> LoadDefinitionsAsync()
{
    var result = await RemoteConfigService.Instance.FetchConfigsAsync(
        new UserAttributes(), new AppAttributes());

    var token = result.config["achievements"];
    return token.ToObject<List<AchievementDefinition>>();
}
```

---

## Load Player Records from Cloud Save

**CRITICAL -- bucket selection:** The read access class must match the bucket that was written to.

- **Trusted path (production):** The Cloud Code module writes to the **Protected** bucket. The client reads with `ProtectedReadAccessClassOptions()`.
- **Direct path (dev only):** The client writes to the **Public** bucket. The client reads with `PublicReadAccessClassOptions(playerId)`.

If you read from the wrong bucket, the data will appear empty.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models.Data.Player;
using PlayerLoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;

async Task<List<AchievementRecord>> LoadRecordsAsync(string playerId, bool useTrustedWrites)
{
    // Read from whichever bucket was written to -- Protected (trusted) or Public (direct).
    ReadAccessClassOptions accessClass = useTrustedWrites
        ? new ProtectedReadAccessClassOptions()
        : new PublicReadAccessClassOptions(playerId);

    var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
        new HashSet<string> { "achievements" },
        new PlayerLoadOptions(accessClass));

    if (!result.TryGetValue("achievements", out var item))
        return new List<AchievementRecord>();

    return JsonConvert.DeserializeObject<List<AchievementRecord>>(item.Value.GetAsString());
}
```

**`ProtectedReadAccessClassOptions`** — reads the current player's own Protected data (no player ID parameter; always reads self).

**`PublicReadAccessClassOptions(playerId)`** — reads another player's Public data. Pass `AuthenticationService.Instance.PlayerId` for the current player.

---

## Save Player Records (Direct Write)

Direct writes use the **Public** access class. This path is for development and prototyping only -- in production, deploy an Access Control policy to block direct player writes and route everything through Cloud Code.

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using PlayerSaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

async Task SaveRecordsAsync(List<AchievementRecord> records)
{
    var json = JsonConvert.SerializeObject(records);
    await CloudSaveService.Instance.Data.Player.SaveAsync(
        new Dictionary<string, SaveItem>
        {
            { "achievements", new SaveItem(json, null) }
        },
        new PlayerSaveOptions(new PublicWriteAccessClassOptions()));
}
```

---

## Server-Authoritative Writes via Cloud Code

In production, route all mutations through Cloud Code module endpoints. The module uses its service token to write to the Protected bucket, bypassing Access Control restrictions that block the player client.

### Unlock an Achievement

```csharp
await CloudCodeService.Instance.CallModuleEndpointAsync(
    "AchievementModule", "UnlockAchievement",
    new Dictionary<string, object> { { "achievementId", achievementId } });
```

### Update Achievement Progress

```csharp
await CloudCodeService.Instance.CallModuleEndpointAsync(
    "AchievementModule", "UpdateAchievementProgress",
    new Dictionary<string, object>
    {
        { "achievementId", achievementId },
        { "count", newCount }
    });
```

### Reset Achievements

```csharp
await CloudCodeService.Instance.CallModuleEndpointAsync(
    "AchievementModule", "ResetAchievements",
    new Dictionary<string, object>());
```

### Delete / Reset Player Records (Direct)

```csharp
await CloudSaveService.Instance.Data.Player.DeleteAsync(
    "achievements",
    new PlayerDeleteOptions(new PublicWriteAccessClassOptions()));
```

---

## Full Client Implementation

Complete `AchievementManager` MonoBehaviour with dual-path support (trusted vs direct writes).

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using PlayerSaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
using PlayerLoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using PlayerDeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class AchievementManager : MonoBehaviour
{
    // Set to true to route writes through Cloud Code (required if Access Control is deployed).
    // Set to false for direct Cloud Save writes (development only).
    [SerializeField] bool m_UseTrustedWrites = true;

    public List<AchievementDefinition> Definitions { get; private set; } = new();
    public List<AchievementRecord> Records { get; private set; } = new();

    public event Action<AchievementDefinition> AchievementUnlocked;
    public event Action Loaded;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        await LoadAsync();
        Loaded?.Invoke();
    }

    // -- Load ------------------------------------------------------------------

    public async Task LoadAsync()
    {
        Definitions = await LoadDefinitionsAsync();
        Records = await LoadRecordsAsync(AuthenticationService.Instance.PlayerId);
    }

    struct UserAttributes {}
    struct AppAttributes {}

    async Task<List<AchievementDefinition>> LoadDefinitionsAsync()
    {
        var result = await RemoteConfigService.Instance.FetchConfigsAsync(
            new UserAttributes(), new AppAttributes());

        var token = result.config["achievements"];
        return token.ToObject<List<AchievementDefinition>>();
    }

    async Task<List<AchievementRecord>> LoadRecordsAsync(string playerId)
    {
        // Read from whichever bucket was written to -- Protected (trusted) or Public (direct).
        ReadAccessClassOptions accessClass = m_UseTrustedWrites
            ? new ProtectedReadAccessClassOptions()
            : new PublicReadAccessClassOptions(playerId);

        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
            new HashSet<string> { "achievements" },
            new PlayerLoadOptions(accessClass));

        if (!result.TryGetValue("achievements", out var item))
            return new List<AchievementRecord>();

        return JsonConvert.DeserializeObject<List<AchievementRecord>>(item.Value.GetAsString());
    }

    // -- Unlock ----------------------------------------------------------------

    public async Task UnlockAsync(string achievementId)
    {
        var def = Definitions.FirstOrDefault(d => d.Id == achievementId);
        if (def == null)
        {
            Debug.LogWarning($"[Achievements] Unknown achievement: {achievementId}");
            return;
        }

        var record = GetOrCreateRecord(achievementId);
        if (record.Unlocked)
            return;

        if (m_UseTrustedWrites)
        {
            // Server validates and writes -- bypasses Access Control restrictions.
            await CloudCodeService.Instance.CallModuleEndpointAsync(
                "AchievementModule", "UnlockAchievement",
                new Dictionary<string, object> { { "achievementId", achievementId } });
        }
        else
        {
            record.Unlocked = true;
            await SaveRecordsAsync(Records);
        }

        record.Unlocked = true;
        AchievementUnlocked?.Invoke(def);
        Debug.Log($"[Achievements] Unlocked: {def.Title}");
    }

    // -- Progress (multi-stage) ------------------------------------------------

    public async Task AddProgressAsync(string achievementId, int amount)
    {
        var def = Definitions.FirstOrDefault(d => d.Id == achievementId);
        if (def == null || def.ProgressTarget <= 1)
            return;

        var record = GetOrCreateRecord(achievementId);
        if (record.Unlocked)
            return;

        var newCount = record.ProgressCount + amount;

        if (m_UseTrustedWrites)
        {
            await CloudCodeService.Instance.CallModuleEndpointAsync(
                "AchievementModule", "UpdateAchievementProgress",
                new Dictionary<string, object>
                {
                    { "achievementId", achievementId },
                    { "count", newCount }
                });
        }
        else
        {
            record.ProgressCount = newCount;
            await SaveRecordsAsync(Records);
        }

        record.ProgressCount = newCount;

        if (record.ProgressCount >= def.ProgressTarget)
            await UnlockAsync(achievementId);
    }

    // -- Reset -----------------------------------------------------------------

    public async Task ResetAsync()
    {
        await CloudSaveService.Instance.Data.Player.DeleteAsync(
            "achievements",
            new PlayerDeleteOptions(new PublicWriteAccessClassOptions()));

        Records.Clear();
    }

    // -- Helpers ---------------------------------------------------------------

    AchievementRecord GetOrCreateRecord(string id)
    {
        var record = Records.FirstOrDefault(r => r.Id == id);
        if (record == null)
        {
            record = new AchievementRecord { Id = id };
            Records.Add(record);
        }
        return record;
    }

    async Task SaveRecordsAsync(List<AchievementRecord> records)
    {
        var json = JsonConvert.SerializeObject(records);
        await CloudSaveService.Instance.Data.Player.SaveAsync(
            new Dictionary<string, SaveItem>
            {
                { "achievements", new SaveItem(json, null) }
            },
            new PlayerSaveOptions(new PublicWriteAccessClassOptions()));
    }
}
```

---

## Cloud Code Module

The `AchievementModule` reads and writes to the **Protected** Cloud Save bucket using its service token, bypassing Access Control restrictions imposed on the player client.

> **Module scaffolding — follow [cloud-code.md — Module Creation](cloud-code.md#module-creation).**
> Replace `MyModule` / `MyModuleCCM` with `AchievementModule` / `AchievementsCCM` throughout.
> You **must** create every file listed there — missing any one will cause deployment to fail:
>
> - [ ] `.sln` (generate fresh GUIDs)
> - [ ] `.csproj` (net9.0, CloudCode.Apis + CloudCode.Core)
> - [ ] `ModuleSetup.cs` (registers `GameApiClient`)
> - [ ] `Properties/PublishProfiles/FolderProfile.pubxml` — **without this file, deployment fails with "Failed to retrieve main project"**
> - [ ] `Assets/CloudCode/AchievementModule.ccmr` (points to the `.sln`)

### AchievementModule.cs

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

// Each public method decorated with [CloudCodeFunction] becomes a callable endpoint.
// IExecutionContext and IGameApiClient are injected automatically by the framework.
public class AchievementModule
{
    const string AchievementsKey = "achievements";

    [CloudCodeFunction("UnlockAchievement")]
    public async Task<AchievementRecord> UnlockAchievement(
        IExecutionContext context, IGameApiClient client, string achievementId)
    {
        var records = await LoadRecordsAsync(context, client);

        var record = records.FirstOrDefault(r => r.Id == achievementId);
        if (record == null)
        {
            record = new AchievementRecord { Id = achievementId, Unlocked = true };
            records.Add(record);
        }
        else
        {
            record.Unlocked = true;
        }

        await SaveRecordsAsync(context, client, records);
        return record;
    }

    [CloudCodeFunction("UpdateAchievementProgress")]
    public async Task<AchievementRecord> UpdateAchievementProgress(
        IExecutionContext context, IGameApiClient client, string achievementId, int count)
    {
        var records = await LoadRecordsAsync(context, client);

        var record = records.FirstOrDefault(r => r.Id == achievementId);
        if (record == null)
        {
            record = new AchievementRecord { Id = achievementId, ProgressCount = count };
            records.Add(record);
        }
        else
        {
            record.ProgressCount += count;
        }

        await SaveRecordsAsync(context, client, records);
        return record;
    }

    [CloudCodeFunction("ResetAchievements")]
    public async Task ResetAchievements(IExecutionContext context, IGameApiClient client)
    {
        // Uses ServiceToken so the write bypasses Access Control restrictions on the player client.
        await client.CloudSaveData.DeleteProtectedItemAsync(
            context, context.ServiceToken, AchievementsKey,
            context.ProjectId, context.PlayerId!);
    }

    async Task<List<AchievementRecord>> LoadRecordsAsync(
        IExecutionContext context, IGameApiClient client)
    {
        var result = await client.CloudSaveData.GetProtectedItemsAsync(
            context, context.ServiceToken, context.ProjectId,
            context.PlayerId, new List<string> { AchievementsKey });

        var value = result.Data?.Results?.FirstOrDefault()?.Value?.ToString();
        if (string.IsNullOrEmpty(value))
            return new List<AchievementRecord>();

        return JsonConvert.DeserializeObject<List<AchievementRecord>>(value);
    }

    async Task SaveRecordsAsync(
        IExecutionContext context, IGameApiClient client, List<AchievementRecord> records)
    {
        var json = JsonConvert.SerializeObject(records);
        var body = new SetItemBody(AchievementsKey, json);
        await client.CloudSaveData.SetProtectedItemAsync(
            context, context.ServiceToken, context.ProjectId, context.PlayerId, body);
    }
}
```

### Server-Side Model

Must match client-side field names exactly for JSON round-trip. Server-side uses properties
(not fields) for serialization compatibility with `Newtonsoft.Json`.

```csharp
public class AchievementRecord
{
    public string Id { get; set; }
    public bool Unlocked { get; set; }
    public int ProgressCount { get; set; }
}
```

---

## Cloud Resources

### Package Setup -- `manifest.json`

Add these entries to your Unity project's `Packages/manifest.json` dependencies:

```json
{
  "dependencies": {
    "com.unity.services.authentication": "3.6.1",
    "com.unity.services.cloudcode": "2.10.2",
    "com.unity.services.cloudsave": "3.4.0",
    "com.unity.services.core": "1.16.0",
    "com.unity.services.deployment": "1.7.2",
    "com.unity.remote-config": "4.2.5",
    "com.unity.services.tooling": "1.4.1"
  }
}
```

The `com.unity.services.tooling` package is required for `.ac` file deployment. The `com.unity.services.deployment` package provides the Deployment Window. `Newtonsoft.Json` is provided as a transitive dependency and does not need an explicit entry.

### Remote Config -- `Assets/Config/Achievements.rc`

Right-click in the Project window → **Assets > Create > Services > Remote Config**. Name the file `Achievements`. Replace the generated contents with:

```json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json",
  "entries": {
    "achievements": [
      {
        "Id": "first_win",
        "Title": "First Win",
        "Description": "Win your first match.",
        "IsHidden": false,
        "ProgressTarget": 0
      },
      {
        "Id": "kill_100_enemies",
        "Title": "Centurion",
        "Description": "Defeat 100 enemies.",
        "IsHidden": false,
        "ProgressTarget": 100
      },
      {
        "Id": "secret_ending",
        "Title": "???",
        "Description": "Find the secret ending.",
        "IsHidden": true,
        "ProgressTarget": 0
      }
    ]
  },
  "types": {
    "achievements": "JSON"
  }
}
```

**Rules:**

- `Id` must be unique across all achievements.
- `ProgressTarget <= 1` means single-unlock (call `UnlockAsync`).
- `ProgressTarget > 1` means multi-stage (call `AddProgressAsync`).
- `IsHidden = true` — show obfuscated title/description until unlocked.

Deploy via **Services > Deployment** — the Deployment Window auto-discovers `.rc` files under `Assets/`.

### Cloud Code Module

> Create the full module solution using the file table and templates in
> [Cloud Code Module](#cloud-code-module) above.
> All scaffolding files (`.sln`, `.csproj`, `ModuleSetup.cs`, `FolderProfile.pubxml`) are
> **required** — the Deployment Window will fail without them. See
> [cloud-code.md — Module Creation](cloud-code.md#module-creation) for the generic templates;
> replace `MyModule` / `MyModuleCCM` with `AchievementModule` / `AchievementsCCM`.

### Access Control -- `Assets/Config/DenyPlayerCloudSaveWrites.ac`

Right-click in the Project window → **Assets > Create > Services > Access Control**. Name the file `DenyPlayerCloudSaveWrites`. Replace the generated contents with:

```json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/project-access-policy.schema.json",
  "Statements": [
    {
      "Sid": "DenyPlayerCloudSaveWrites",
      "Action": [
        "Write"
      ],
      "Effect": "Deny",
      "Principal": "Player",
      "Resource": "urn:ugs:cloud-save:*",
      "Version": "1.0.0"
    }
  ]
}
```

**Effect:** With this policy active, `AchievementManager` must have `m_UseTrustedWrites = true`. Direct Cloud Save writes from the player client will return `403`.

**Without this policy:** `m_UseTrustedWrites = false` works fine — suitable for development or games where anti-cheat is not a concern.

Deploy via **Services > Deployment** alongside your other files.

---

## Deployment Checklist

Deploy all cloud resources via **Services > Deployment** before testing:

- [ ] All packages added to `Packages/manifest.json` with correct minimum versions
- [ ] `Assets/Config/Achievements.rc` -- Remote Config achievement definitions
- [ ] `Assets/Config/DenyPlayerCloudSaveWrites.ac` -- Access Control policy blocking direct player writes
- [ ] Cloud Code module scaffolded with **all required files**: `.sln`, `.csproj`, `ModuleSetup.cs`, `FolderProfile.pubxml`, `.ccmr` (see file table in [Cloud Code Module](#cloud-code-module))
- [ ] `AchievementModule.cs` added to the module project with `UnlockAchievement`, `UpdateAchievementProgress`, `ResetAchievements` functions
- [ ] Environment selected in the Deployment Window dropdown
- [ ] All resources deployed via the Deployment Window
- [ ] Project linked in **Edit > Project Settings > Services**

---

## Official Unity Reference

- **Achievements Building Block (Asset Store):** The official [Unity Building Block — Achievements](https://assetstore.unity.com/packages/essentials/tutorial-projects/unity-building-block-achievements-341918) uses the same Remote Config + Cloud Save + Cloud Code pattern described here. Import from the Asset Store for ready-made UI, Cloud Code module, `.ac` / `.rc` / `.ccmr` cloud resources, and generated client bindings.
- **Unity Cloud Save documentation:** https://docs.unity.com/ugs/en-us/manual/cloud-save/manual
- **Unity Remote Config documentation:** https://docs.unity.com/ugs/en-us/manual/remote-config/manual
- **Unity Cloud Code documentation:** https://docs.unity.com/ugs/en-us/manual/cloud-code/manual
- **Access Control documentation:** https://docs.unity.com/ugs/en-us/manual/access-control/manual

---

## Validation

After implementing achievements, verify:

1. **Compilation:** The project compiles without errors. Ensure namespace aliases (`PlayerLoadOptions`, `PlayerSaveOptions`, `PlayerDeleteOptions`) are present.
2. **Initialization order:** `UnityServices.InitializeAsync()` then `AuthenticationService.Instance.SignInAnonymouslyAsync()` then service calls.
3. **Access class match:** If `m_UseTrustedWrites` is true, reads use `ProtectedReadAccessClassOptions()`. If false, reads use `PublicReadAccessClassOptions(playerId)`. Mismatched access classes return empty data.
4. **Cloud Code module builds:** The `.sln` at `AchievementsCCM/AchievementModule.sln` targets `net9.0` and publishes to `linux-x64`.
5. **Server-side model uses properties:** The `AchievementRecord` class in the Cloud Code module uses `{ get; set; }` properties (not fields) for JSON serialization compatibility with `Newtonsoft.Json`.
6. **Access Control deployed:** The `.ac` file denies direct player writes to Cloud Save. Without this, the trusted-write path has no enforcement advantage.
7. **Remote Config key matches:** The `.rc` file key (`"achievements"`) matches the key used in `result.config["achievements"]` on the client and `AchievementsKey` in the module.
8. **All cloud resources deployable:** The `.rc`, `.ac`, and `.ccmr` files are in the `Assets/` folder hierarchy and appear in the Deployment Window.
