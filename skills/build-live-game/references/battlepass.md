# Battle Pass Blueprint

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Free Track vs Premium Track](#free-track-vs-premium-track)
- [Multiple Concurrent Passes](#multiple-concurrent-passes)
- [Data Models](#data-models)
- [API Notes](#api-notes)
  - [Namespace Aliases](#namespace-aliases-required)
  - [ProtectedReadAccessClassOptions](#protectedreadaccessclassoptions)
  - [Why tiersJson Is Passed from the Client](#why-tiersjson-is-passed-from-the-client)
  - [Source Whitelist Enforcement](#source-whitelist-enforcement)
  - [Error Handling](#error-handling)
- [Cloud Code Module Functions](#cloud-code-module-functions)
- [Load Pass Definitions from Remote Config](#load-pass-definitions-from-remote-config)
- [Load Player Progress](#load-player-progress)
- [Client to Cloud Code Calls](#client-to-cloud-code-calls)
- [Full Client Implementation](#full-client-implementation)
- [BattlePassTester](#battlepasstester)
- [Cloud Code Module Implementation](#cloud-code-module-implementation)
  - [BattlePassModule.cs](#battlepassmodulecs)
  - [Server-Side Models](#server-side-models-inside-battlepassmodulecs)
- [Cloud Resources](#cloud-resources)
- [Deployment Checklist](#deployment-checklist)
- [Validation](#validation)
- [Asset Store & Sample Projects](#asset-store--sample-projects)

---

## Architecture Overview

| Concern | Service | Key |
|---|---|---|
| Pass definitions | Remote Config | `"battle_passes"` |
| Per-player progress | Cloud Save (Protected) | `"battle_pass_progress"` |
| Award XP | Cloud Code | `BattlePassModule.AwardXp` |
| Claim reward | Cloud Code | `BattlePassModule.ClaimReward` |
| Purchase premium | Cloud Code | `BattlePassModule.PurchasePremium` |

```
Remote Config                Cloud Save (Protected)           Cloud Code Module
"battle_passes"              "battle_pass_progress"           BattlePassModule
       |                            |                               |
       v                            v                               v
BattlePassDefinition[]       { passId: Progress }            AwardXp
  PassId, Name,              Server-writes only               ClaimReward
  StartDate, EndDate,                                         PurchasePremium
  Tiers[]
       |                            ^                               |
       |                            |                               |
       +------- client reads -------+------- server writes ---------+
```

**Key principle:** The client reads pass definitions from Remote Config and player progress from Cloud Save (Protected bucket). All mutations -- awarding XP, claiming rewards, purchasing premium -- go through the Cloud Code module, which is the only writer to the Protected bucket.

---

## Free Track vs Premium Track

Every tier in a battle pass has **two parallel reward tracks**:

- **Free track** -- available to all players who reach the tier.
- **Premium track** -- visible to all players but **locked** unless the player has purchased the premium pass (`HasPremium = true`).

Premium rewards are displayed in the UI alongside free rewards so non-premium players can see what they are missing, encouraging upgrade. The Cloud Code module enforces the lock: `ClaimReward` with `rewardType = "premium"` throws if `HasPremium` is false.

---

## Multiple Concurrent Passes

The system supports multiple battle passes running simultaneously:

- Each pass has a unique `PassId` (e.g. `"season_03"`, `"easter_2026"`).
- Passes may overlap in time. A player can have active progress in several passes at once.
- Progress and premium status are tracked **independently per pass**. The Cloud Save key `"battle_pass_progress"` stores a `Dictionary<string, BattlePassProgress>` keyed by `PassId`.
- **Date filtering is client-side.** The client fetches all pass definitions from Remote Config and filters to those whose `StartDate <= now <= EndDate`.
- Expired passes remain in the progress dictionary. The client simply stops showing them. Clean up stale entries via a scheduled Cloud Code trigger if desired.

---

## Data Models

### BattlePassDefinition (client-side, from Remote Config)

```csharp
[Serializable]
public class BattlePassDefinition
{
    public string PassId;
    public string Name;
    public string StartDate;    // ISO 8601, e.g. "2026-04-01T00:00:00Z"
    public string EndDate;      // ISO 8601
    public List<BattlePassTier> Tiers;
}
```

### BattlePassTier (client-side, from Remote Config)

```csharp
[Serializable]
public class BattlePassTier
{
    public int TierNumber;
    public int XpRequired;      // cumulative XP to reach this tier (not delta from previous)
    public string FreeRewardId;
    public string PremiumRewardId;
}
```

### BattlePassProgress (client-side and server-side, from Cloud Save Protected)

```csharp
[Serializable]
public class BattlePassProgress
{
    public int CurrentXp;
    public int CurrentTier;
    public bool HasPremium;
    public List<string> ClaimedRewards;     // e.g. "tier_1_free", "tier_2_premium"
}
```

One entry per pass, keyed by `PassId` in the progress dictionary stored at Cloud Save key `"battle_pass_progress"`.

### BattlePassTierThreshold (server-side helper, passed from client via tiersJson)

```csharp
[Serializable]
public class BattlePassTierThreshold
{
    public int TierNumber;
    public int XpRequired;
}
```

---

## API Notes

### Namespace Aliases (Required)

Cloud Save has multiple types named `LoadOptions`, `SaveOptions`, and `DeleteOptions` across
different namespaces. Always declare these aliases at the top of any file that uses Cloud Save:

```csharp
using PlayerLoadOptions   = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using PlayerSaveOptions   = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;
using PlayerDeleteOptions = Unity.Services.CloudSave.Models.Data.Player.DeleteOptions;
```

Without these aliases, the compiler will report ambiguous type references.

### ProtectedReadAccessClassOptions

Use `ProtectedReadAccessClassOptions` when loading player progress. This reads the current
player's own Protected data — no player ID parameter is required; it always reads the signed-in
player's data.

> **Common mistake:** Reading from the wrong access class (e.g. using default `LoadOptions`
> instead of `ProtectedReadAccessClassOptions`) will return no data, because the progress
> is stored in the Protected bucket by the Cloud Code module.

### Why tiersJson Is Passed from the Client

Remote Config is not accessible from inside Cloud Code modules. The client serializes the tier
thresholds from the loaded `BattlePassDefinition` and sends them as the `tiersJson` parameter.
The server uses these thresholds to recalculate the player's current tier after awarding XP.

Tier thresholds are config data and are **not security-sensitive** — the anti-cheat protection
is on the `source` whitelist and the `amount` validation. A tampered `tiersJson` could only
cause the player's tier to be recalculated differently, which is bounded by their actual XP.

### Source Whitelist Enforcement

The Cloud Code module maintains a server-side whitelist of allowed XP source strings:

```csharp
static readonly HashSet<string> ValidSources = new()
{
    "match_complete",
    "daily_login",
    "quest_complete"
};
```

Any `AwardXp` call with an unrecognized source throws an exception. Add or remove entries as
needed for your game. The whitelist prevents clients from inventing arbitrary XP sources.

### Error Handling

Cloud Code and Cloud Save errors surface as typed exceptions on the client:

```csharp
try
{
    var progress = await AwardXpAsync(passId, 100, "match_complete");
}
catch (CloudCodeException ex)
{
    // ex.Reason: CloudCodeExceptionReason (e.g. ScriptError for module-thrown exceptions)
    Debug.LogError($"[BattlePass] Cloud Code error: {ex.Reason} — {ex.Message}");
}
catch (CloudSaveException ex)
{
    // ex.Reason: CloudSaveExceptionReason
    // A 403 on the Protected bucket means the client attempted a direct write —
    // all writes must go through the Cloud Code module.
    Debug.LogError($"[BattlePass] Cloud Save error: {ex.Reason} — {ex.Message}");
}
```

---

## Cloud Code Module Functions

| Function | Parameters | Behavior |
|---|---|---|
| `AwardXp` | `passId`, `amount`, `source`, `tiersJson` | Validates `source` against a whitelist (`ValidSources`), rejects non-positive amounts, adds XP to the pass progress, recalculates the current tier from the client-supplied `tiersJson`, saves to Cloud Save Protected |
| `ClaimReward` | `passId`, `tierNumber`, `rewardType` | Validates the player has reached the tier, validates premium ownership if `rewardType` is `"premium"`, checks reward is not already claimed, records the claim as `"tier_N_type"` (e.g. `"tier_2_premium"`), saves to Cloud Save Protected |
| `PurchasePremium` | `passId` | Sets `HasPremium = true` on the pass progress. **Idempotent** -- returns immediately if already purchased without error |

All three functions return the updated `BattlePassProgress` for the given pass.

---

## Load Pass Definitions from Remote Config

```csharp
struct UserAttributes {}
struct AppAttributes {}

async Task<List<BattlePassDefinition>> LoadActivePassesAsync()
{
    var result = await RemoteConfigService.Instance.FetchConfigsAsync(
        new UserAttributes(), new AppAttributes());

    var token = result.config["battle_passes"];
    if (token == null) return new List<BattlePassDefinition>();

    var allPasses = token.ToObject<List<BattlePassDefinition>>();

    var now = DateTime.UtcNow;
    return allPasses
        .Where(p =>
            DateTime.Parse(p.StartDate, null, DateTimeStyles.RoundtripKind) <= now &&
            DateTime.Parse(p.EndDate,   null, DateTimeStyles.RoundtripKind) >= now)
        .ToList();
}
```

- `FetchConfigsAsync` downloads all Remote Config entries.
- `result.config["battle_passes"]` returns a `JToken` for the JSON array stored under that key.
- Date filtering is performed client-side by comparing `StartDate` and `EndDate` against `DateTime.UtcNow`.

---

## Load Player Progress

```csharp
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using PlayerLoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;

async Task<Dictionary<string, BattlePassProgress>> LoadProgressAsync()
{
    var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
        new HashSet<string> { "battle_pass_progress" },
        new PlayerLoadOptions(new ProtectedReadAccessClassOptions()));

    if (!result.TryGetValue("battle_pass_progress", out var item))
        return new Dictionary<string, BattlePassProgress>();

    return JsonConvert.DeserializeObject<Dictionary<string, BattlePassProgress>>(
        item.Value.GetAsString())
        ?? new Dictionary<string, BattlePassProgress>();
}
```

**Important:** Use `ProtectedReadAccessClassOptions` because the Cloud Code module writes to the Protected bucket. Reading from the wrong access class will return no data.

> **Stale entries:** Expired pass entries remain in the dictionary indefinitely but are inert —
> the client ignores any PassId not present in the current active pass list.

---

## Client to Cloud Code Calls

### AwardXpAsync

```csharp
public async Task<BattlePassProgress> AwardXpAsync(string passId, int amount, string source)
{
    var pass = ActivePasses.FirstOrDefault(p => p.PassId == passId)
        ?? throw new InvalidOperationException(
            $"Pass '{passId}' is not in the active passes list. Call LoadAsync first.");

    var progress = await CloudCodeService.Instance.CallModuleEndpointAsync<BattlePassProgress>(
        "BattlePassModule", "AwardXp",
        new Dictionary<string, object>
        {
            { "passId",    passId                               },
            { "amount",    amount                               },
            { "source",    source                               },
            { "tiersJson", JsonConvert.SerializeObject(pass.Tiers) }
        });

    Progress[passId] = progress;
    return progress;
}
```

The `tiersJson` parameter is the serialized tier list from the locally loaded pass definition. The server uses it to recalculate the player's tier after adding XP, without needing its own Remote Config fetch.

### ClaimRewardAsync

```csharp
public async Task<BattlePassProgress> ClaimRewardAsync(
    string passId, int tierNumber, string rewardType)
{
    var progress = await CloudCodeService.Instance.CallModuleEndpointAsync<BattlePassProgress>(
        "BattlePassModule", "ClaimReward",
        new Dictionary<string, object>
        {
            { "passId",      passId      },
            { "tierNumber",  tierNumber  },
            { "rewardType",  rewardType  }
        });

    Progress[passId] = progress;
    return progress;
}
```

### PurchasePremiumAsync

```csharp
public async Task<BattlePassProgress> PurchasePremiumAsync(string passId)
{
    var progress = await CloudCodeService.Instance.CallModuleEndpointAsync<BattlePassProgress>(
        "BattlePassModule", "PurchasePremium",
        new Dictionary<string, object> { { "passId", passId } });

    Progress[passId] = progress;
    return progress;
}
```

---

## Full Client Implementation

`BattlePassManager` is a `MonoBehaviour` that initializes services, loads pass definitions and player progress, and exposes methods for XP awards, reward claims, and premium purchases. Attach it to a GameObject in your scene.

```csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using PlayerLoadOptions = Unity.Services.CloudSave.Models.Data.Player.LoadOptions;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;

public class BattlePassManager : MonoBehaviour
{
    public List<BattlePassDefinition> ActivePasses { get; private set; } = new();
    public Dictionary<string, BattlePassProgress> Progress { get; private set; } = new();

    public event Action Loaded;

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            await LoadAsync();
            Debug.Log($"BattlePassManager: Loaded {ActivePasses.Count} passes.");
            Loaded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogError($"BattlePassManager: Error during loading: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public async Task LoadAsync()
    {
        ActivePasses = await LoadActivePassesAsync();
        Progress     = await LoadProgressAsync();
    }

    struct UserAttributes {}
    struct AppAttributes {}

    async Task<List<BattlePassDefinition>> LoadActivePassesAsync()
    {
        var result = await RemoteConfigService.Instance.FetchConfigsAsync(
            new UserAttributes(), new AppAttributes());

        var token = result.config["battle_passes"];
        if (token == null) return new List<BattlePassDefinition>();

        var allPasses = token.ToObject<List<BattlePassDefinition>>();

        var now = DateTime.UtcNow;
        return allPasses
            .Where(p =>
                DateTime.Parse(p.StartDate, null, DateTimeStyles.RoundtripKind) <= now &&
                DateTime.Parse(p.EndDate,   null, DateTimeStyles.RoundtripKind) >= now)
            .ToList();
    }

    async Task<Dictionary<string, BattlePassProgress>> LoadProgressAsync()
    {
        var result = await CloudSaveService.Instance.Data.Player.LoadAsync(
            new HashSet<string> { "battle_pass_progress" },
            new PlayerLoadOptions(new ProtectedReadAccessClassOptions()));

        if (!result.TryGetValue("battle_pass_progress", out var item))
            return new Dictionary<string, BattlePassProgress>();

        return JsonConvert.DeserializeObject<Dictionary<string, BattlePassProgress>>(
            item.Value.GetAsString())
            ?? new Dictionary<string, BattlePassProgress>();
    }

    public async Task<BattlePassProgress> AwardXpAsync(string passId, int amount, string source)
    {
        var pass = ActivePasses.FirstOrDefault(p => p.PassId == passId)
            ?? throw new InvalidOperationException(
                $"Pass '{passId}' is not in the active passes list. Call LoadAsync first.");

        var progress = await CloudCodeService.Instance.CallModuleEndpointAsync<BattlePassProgress>(
            "BattlePassModule", "AwardXp",
            new Dictionary<string, object>
            {
                { "passId",    passId                               },
                { "amount",    amount                               },
                { "source",    source                               },
                { "tiersJson", JsonConvert.SerializeObject(pass.Tiers) }
            });

        Progress[passId] = progress;
        return progress;
    }

    public async Task<BattlePassProgress> ClaimRewardAsync(
        string passId, int tierNumber, string rewardType)
    {
        var progress = await CloudCodeService.Instance.CallModuleEndpointAsync<BattlePassProgress>(
            "BattlePassModule", "ClaimReward",
            new Dictionary<string, object>
            {
                { "passId",      passId      },
                { "tierNumber",  tierNumber  },
                { "rewardType",  rewardType  }
            });

        Progress[passId] = progress;
        return progress;
    }

    public async Task<BattlePassProgress> PurchasePremiumAsync(string passId)
    {
        var progress = await CloudCodeService.Instance.CallModuleEndpointAsync<BattlePassProgress>(
            "BattlePassModule", "PurchasePremium",
            new Dictionary<string, object> { { "passId", passId } });

        Progress[passId] = progress;
        return progress;
    }
}
```

---

## BattlePassTester

Attach to the same GameObject as `BattlePassManager` in your test scene. Wire `m_Manager` via
the Inspector, or let it fall back to `GetComponent`.

```csharp
using System.Text;
using UnityEngine;

/// <summary>
/// Attach alongside BattlePassManager to run a quick in-Editor smoke test.
/// On load: logs all active pass states.
/// Then: awards 100 XP to the first active pass via Cloud Code and logs the result.
/// </summary>
public class BattlePassTester : MonoBehaviour
{
    [SerializeField] BattlePassManager m_Manager;

    void Start()
    {
        if (m_Manager == null)
            m_Manager = GetComponent<BattlePassManager>();
        m_Manager.Loaded += OnLoaded;
    }

    async void OnLoaded()
    {
        LogState("Initial state");

        if (m_Manager.ActivePasses.Count == 0)
        {
            Debug.Log("[BattlePassTester] No active passes — nothing to award.");
            return;
        }

        var firstPass = m_Manager.ActivePasses[0];
        Debug.Log($"[BattlePassTester] Awarding 100 XP to '{firstPass.PassId}' " +
            $"(source: match_complete) ...");

        var result = await m_Manager.AwardXpAsync(firstPass.PassId, 100, "match_complete");

        Debug.Log($"[BattlePassTester] After award — " +
            $"XP={result.CurrentXp}, Tier={result.CurrentTier}");
    }

    void LogState(string label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[BattlePassTester] {label} — {m_Manager.ActivePasses.Count} active pass(es):");

        foreach (var pass in m_Manager.ActivePasses)
        {
            m_Manager.Progress.TryGetValue(pass.PassId, out var progress);
            sb.AppendLine($"  [{pass.PassId}] {pass.Name}: " +
                $"XP={progress?.CurrentXp ?? 0}, " +
                $"Tier={progress?.CurrentTier ?? 1}, " +
                $"Premium={progress?.HasPremium ?? false}, " +
                $"Claimed={progress?.ClaimedRewards?.Count ?? 0}");
        }

        Debug.Log(sb.ToString());
    }
}
```

---

## Cloud Code Module Implementation

> **Module scaffolding — follow [cloud-code.md — Module Creation](cloud-code.md#module-creation).**
> Replace `MyModule` / `MyModuleCCM` with `BattlePassModule` / `BattlePassCCM` throughout.
> You **must** create every file listed there — missing any one will cause deployment to fail:
>
> - [ ] `.sln` (generate fresh GUIDs)
> - [ ] `.csproj` (net9.0, CloudCode.Apis + CloudCode.Core)
> - [ ] `ModuleSetup.cs` (registers `GameApiClient`)
> - [ ] `Properties/PublishProfiles/FolderProfile.pubxml` — **without this file, deployment fails with "Failed to retrieve main project"**
> - [ ] `Assets/CloudCode/BattlePassModule.ccmr` (points to the `.sln`)

### BattlePassModule.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

public class BattlePassModule
{
    const string ProgressKey = "battle_pass_progress";

    static readonly HashSet<string> ValidSources = new()
    {
        "match_complete",
        "daily_login",
        "quest_complete"
    };

    // ── AwardXp ──────────────────────────────────────────────────────────────

    [CloudCodeFunction("AwardXp")]
    public async Task<BattlePassProgress> AwardXp(
        IExecutionContext context, IGameApiClient client,
        string passId, int amount, string source, string tiersJson)
    {
        if (!ValidSources.Contains(source))
            throw new Exception(
                $"Invalid XP source: '{source}'. Allowed: {string.Join(", ", ValidSources)}");

        if (amount <= 0)
            throw new Exception("XP amount must be positive.");

        var allProgress = await LoadProgressAsync(context, client);

        if (!allProgress.TryGetValue(passId, out var progress))
            progress = new BattlePassProgress { CurrentTier = 1, ClaimedRewards = new List<string>() };

        progress.CurrentXp += amount;

        var tiers = JsonConvert.DeserializeObject<List<BattlePassTierThreshold>>(tiersJson)
            ?? throw new Exception("tiersJson could not be deserialized.");
        progress.CurrentTier = CalculateTier(progress.CurrentXp, tiers);

        allProgress[passId] = progress;
        await SaveProgressAsync(context, client, allProgress);

        return progress;
    }

    // ── ClaimReward ──────────────────────────────────────────────────────────

    [CloudCodeFunction("ClaimReward")]
    public async Task<BattlePassProgress> ClaimReward(
        IExecutionContext context, IGameApiClient client,
        string passId, int tierNumber, string rewardType)
    {
        if (rewardType != "free" && rewardType != "premium")
            throw new Exception(
                $"Invalid rewardType: '{rewardType}'. Must be 'free' or 'premium'.");

        var allProgress = await LoadProgressAsync(context, client);

        if (!allProgress.TryGetValue(passId, out var progress))
            throw new Exception($"No progress found for pass '{passId}'. Award XP first.");

        if (progress.CurrentTier < tierNumber)
            throw new Exception(
                $"Tier {tierNumber} not yet reached (current tier: {progress.CurrentTier}).");

        if (rewardType == "premium" && !progress.HasPremium)
            throw new Exception("Premium pass not owned. Purchase premium first.");

        var rewardKey = $"tier_{tierNumber}_{rewardType}";
        if (progress.ClaimedRewards.Contains(rewardKey))
            throw new Exception($"Reward '{rewardKey}' already claimed.");

        progress.ClaimedRewards.Add(rewardKey);
        allProgress[passId] = progress;
        await SaveProgressAsync(context, client, allProgress);

        return progress;
    }

    // ── PurchasePremium ──────────────────────────────────────────────────────

    [CloudCodeFunction("PurchasePremium")]
    public async Task<BattlePassProgress> PurchasePremium(
        IExecutionContext context, IGameApiClient client, string passId)
    {
        var allProgress = await LoadProgressAsync(context, client);

        if (!allProgress.TryGetValue(passId, out var progress))
            progress = new BattlePassProgress { CurrentTier = 1, ClaimedRewards = new List<string>() };

        if (progress.HasPremium)
            return progress;   // idempotent -- already owns premium

        progress.HasPremium = true;
        allProgress[passId] = progress;
        await SaveProgressAsync(context, client, allProgress);

        return progress;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    static int CalculateTier(int currentXp, List<BattlePassTierThreshold> tiers)
    {
        var reached = tiers
            .Where(t => currentXp >= t.XpRequired)
            .OrderByDescending(t => t.TierNumber)
            .FirstOrDefault();

        return reached?.TierNumber ?? 1;
    }

    async Task<Dictionary<string, BattlePassProgress>> LoadProgressAsync(
        IExecutionContext context, IGameApiClient client)
    {
        var result = await client.CloudSaveData.GetProtectedItemsAsync(
            context, context.ServiceToken, context.ProjectId,
            context.PlayerId!, new List<string> { ProgressKey });

        var value = result.Data?.Results?.FirstOrDefault()?.Value?.ToString();
        if (string.IsNullOrEmpty(value))
            return new Dictionary<string, BattlePassProgress>();

        return JsonConvert.DeserializeObject<Dictionary<string, BattlePassProgress>>(value)
            ?? new Dictionary<string, BattlePassProgress>();
    }

    async Task SaveProgressAsync(
        IExecutionContext context, IGameApiClient client,
        Dictionary<string, BattlePassProgress> progress)
    {
        var json = JsonConvert.SerializeObject(progress);
        var body = new SetItemBody(ProgressKey, json);
        await client.CloudSaveData.SetProtectedItemAsync(
            context, context.ServiceToken, context.ProjectId, context.PlayerId!, body);
    }
}
```

### Server-Side Models (inside BattlePassModule.cs)

```csharp
public class BattlePassProgress
{
    public int CurrentXp { get; set; }
    public int CurrentTier { get; set; }
    public bool HasPremium { get; set; }
    public List<string> ClaimedRewards { get; set; } = new();
}

public class BattlePassTierThreshold
{
    public int TierNumber { get; set; }
    public int XpRequired { get; set; }
}
```

---

## Cloud Resources

### Remote Config `.rc` File

Place at `Assets/BattlePasses.rc`. Contains pass definitions as a JSON array under the `"battle_passes"` key. Example with two concurrent passes:

```json
{
  "$schema": "https://ugs-config-schemas.unity3d.com/v1/remote-config.schema.json",
  "entries": {
    "battle_passes": [
      {
        "PassId": "season_03",
        "Name": "Season 3",
        "StartDate": "2024-01-01T00:00:00Z",
        "EndDate": "2026-06-30T23:59:59Z",
        "Tiers": [
          { "TierNumber": 1, "XpRequired": 0,    "FreeRewardId": "item_bronze_badge",      "PremiumRewardId": "currency_gold_500" },
          { "TierNumber": 2, "XpRequired": 500,  "FreeRewardId": "item_xp_boost",          "PremiumRewardId": "item_rare_skin_fragment" },
          { "TierNumber": 3, "XpRequired": 1200, "FreeRewardId": "item_silver_badge",      "PremiumRewardId": "item_legendary_skin" }
        ]
      },
      {
        "PassId": "easter_2026",
        "Name": "Easter Event 2026",
        "StartDate": "2026-04-01T00:00:00Z",
        "EndDate": "2026-04-21T23:59:59Z",
        "Tiers": [
          { "TierNumber": 1, "XpRequired": 0,    "FreeRewardId": "item_egg_basket",     "PremiumRewardId": "item_golden_egg" },
          { "TierNumber": 2, "XpRequired": 300,  "FreeRewardId": "item_bunny_ears",     "PremiumRewardId": "item_bunny_mount" }
        ]
      }
    ]
  },
  "types": {
    "battle_passes": "JSON"
  }
}
```

**Rules:**

- `PassId` must be unique across all passes and stable over time — it is the key used in player
  progress records. Never rename a `PassId` after launch.
- `XpRequired` is **cumulative** XP to reach the tier, not a delta from the previous tier.
  Tier 1 must always have `XpRequired: 0` so it is immediately reachable.
- `FreeRewardId` / `PremiumRewardId` are arbitrary strings. The client reads these IDs and maps
  them to display names/icons locally.
- Date filtering happens **client-side** after fetch. Add passes to the file before their
  `StartDate` — they will be ignored by the client until the start time is reached.

### Cloud Code Module

> Create the full module solution using the file table and templates in
> [Cloud Code Module Implementation](#cloud-code-module-implementation) above.
> All scaffolding files (`.sln`, `.csproj`, `ModuleSetup.cs`, `FolderProfile.pubxml`) are
> **required** — the Deployment Window will fail without them. See
> [cloud-code.md — Module Creation](cloud-code.md#module-creation) for the generic templates;
> replace `MyModule` / `MyModuleCCM` with `BattlePassModule` / `BattlePassCCM`.

### Access Control

**No Access Control (`.ac`) file is needed for the battle pass.** The Cloud Save Protected bucket is inherently server-only for writes -- only Cloud Code (using `IGameApiClient` with the service token) can write to it. Clients can read Protected data but cannot modify it.

> **Contrast with achievements:** The achievements skill offered a direct-write dev path using
> the Public bucket, which required an explicit Access Control policy to lock down in production.
> Battle pass has no direct-write path, so there is nothing to lock down.

### `manifest.json` Entries

Ensure these packages are listed in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unity.services.authentication": "3.6.1",
    "com.unity.services.cloudcode": "2.10.2",
    "com.unity.services.cloudsave": "3.4.0",
    "com.unity.services.core": "1.16.0",
    "com.unity.services.deployment": "1.7.2",
    "com.unity.remote-config": "4.2.5",
    "com.unity.services.tooling": "1.4.1",
    "com.unity.nuget.newtonsoft-json": "3.2.2"
  }
}
```

`com.unity.nuget.newtonsoft-json` is required for `JsonConvert` used in serialization of tier data and progress.

---

## Deployment Checklist

- [ ] All packages added to `Packages/manifest.json`
- [ ] Remote Config `.rc` file created with `"battle_passes"` entry and at least one active pass
- [ ] Cloud Code module scaffolded with **all required files**: `.sln`, `.csproj`, `ModuleSetup.cs`, `FolderProfile.pubxml`, `.ccmr` (see file table in [Cloud Code Module Implementation](#cloud-code-module-implementation))
- [ ] `BattlePassModule.cs` added to the module project with `AwardXp`, `ClaimReward`, `PurchasePremium` functions
- [ ] Environment selected in **Services > Deployment** settings
- [ ] Deploy all via the Deployment Window: select `BattlePasses.rc` and `BattlePassModule.ccmr`, click **Deploy**
- [ ] Project linked in **Edit > Project Settings > Services**

---

## Validation

After implementing the battle pass feature, verify:

1. **Project compiles** without errors. Both the Unity project and the `BattlePassCCM/` .NET project should build cleanly.
2. **Initialization order** is correct: `UnityServices.InitializeAsync()` then `AuthenticationService.Instance.SignInAnonymouslyAsync()` then service calls.
3. **Protected bucket reads** use `ProtectedReadAccessClassOptions` -- using the wrong access class returns no data.
4. **Cloud Code module name** matches the `.ccmr` filename: `"BattlePassModule"` in `CallModuleEndpointAsync` matches `BattlePassModule.ccmr`.
5. **No direct client writes** to `"battle_pass_progress"` -- all mutations go through Cloud Code.
6. **tiersJson is serialized** from the locally loaded pass definition before calling `AwardXp`.
7. **Date filtering** uses `DateTime.UtcNow` with `DateTimeStyles.RoundtripKind` for consistent UTC parsing.
8. **Cloud resources** are present and deployable: `BattlePasses.rc` and `BattlePassModule.ccmr` appear in the Deployment Window.
9. **PurchasePremium is idempotent** -- calling it when `HasPremium` is already true returns the existing progress without error.
10. **ClaimReward validates** both tier reach and premium ownership before recording the claim.

---

## Asset Store & Sample Projects

- **Use Case Samples — Battle Pass:** The official [Use Case Samples](https://github.com/Unity-Technologies/com.unity.services.samples.use-cases) repository includes a complete Battle Pass sample with seasonal reward tiers, free/premium tracks, time-limited rewards, and server-authoritative progression via Cloud Code. Clone the repo and open the "Battle Pass" scene for a working reference.
- **Use Case Samples — Virtual Shop:** The same repository includes a Virtual Shop sample demonstrating Economy-based in-game stores, currency management, and inventory — patterns commonly paired with a Battle Pass.
