# LevelPlay SDK 9.x.x Migration Guide

## Contents
- [Overview](#overview)
- [Scenario A: Upgrade SDK to 9.x.x](#scenario-a-upgrade-sdk-to-9xx)
- [Scenario B: Migrate Init API (IronSource → LevelPlay)](#scenario-b-migrate-init-api)
- [Scenario C: Migrate Ad Unit APIs](#scenario-c-migrate-ad-unit-apis)
  - [C1: Rewarded](#c1-rewarded-ads) · [C2: Interstitial](#c2-interstitial-ads) · [C3: Banner](#c3-banner-ads) · [C4: ILRD Handler](#c4-impression-data-ilrd-handler) · [C5: Completeness Checklist](#c5-migration-completeness-checklist)
- [Scenario D: Maven Central Migration](#scenario-d-maven-central-migration)
- [Scenario E: Migrate from Unity Ads to LevelPlay](#scenario-e-migrate-from-unity-ads-to-levelplay)
- [API Reference: Legacy vs. New](#api-reference-legacy-vs-new)

---

## Overview

SDK 9.0.0 introduced breaking changes: the new `LevelPlay.Init()` API replaces `IronSource.Agent.init()`, and ad unit IDs (Ad Unit API) replace the older placement-based IronSource APIs for rewarded, interstitial, and banner ads.

**Minimum SDK version for new Ad Unit APIs**: 8.4.0 (required in 9.0.0+)

---

## Scenario A: Upgrade SDK to 9.x.x

**Before touching anything: inventory, confirm, then delete.** The steps below delete folders, which wipes settings and installed adapters. Complete these in order:

1. **Record Developer Settings values** (app keys, app IDs) from **Ads Mediation > Developer Settings** — they are lost during upgrade. Ask the user to save them and wait for their confirmation before proceeding.
2. **Record the installed network adapters** (check **Ads Mediation > Network Manager**, or list the `IS*AdapterDependencies.xml` files under `Assets > LevelPlay > Editor`) so the same set can be reinstalled after the upgrade.
3. **List exactly what will be deleted and why** (deleting Mobile Dependency Resolver is safe — the new package re-ships it), and get the user's explicit confirmation before deleting anything. Never delete folders unannounced.

### A1: Upgrading via .unitypackage

1. Delete `Assets > LevelPlay` folder and all contents
2. Delete `Assets > Mobile Dependency Resolver` if present
3. Download the [latest Unity Plugin](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/package-integration)
4. Import the .unitypackage file
5. Continue with **Post-Upgrade Verification** below — adapter reinstall and settings re-entry happen there

### A2: Upgrading via UPM (Ads Mediation package)

1. Delete `Assets > LevelPlay` or `Assets > IronSource` folder and all contents
2. Delete `Assets > Mobile Dependency Resolver` if present
3. Open **Window > Package Manager**
4. Select **Unity Registry** → find **Ads Mediation** → click **Update**
5. Continue with **Post-Upgrade Verification** below — adapter reinstall and settings re-entry happen there

### A3: Switching from .unitypackage to UPM

1. Delete `Assets > LevelPlay` or `Assets > IronSource` folder and all contents
2. Delete `Assets > Mobile Dependency Resolver` if present
3. Open **Window > Package Manager**
4. Select **Unity Registry** → find **Ads Mediation** → click **Install**
5. Continue with **Post-Upgrade Verification** below — adapter reinstall and settings re-entry happen there

**Note on package file edits (A2/A3):** when changing package versions by editing files, modify only `Packages/manifest.json`. Never hand-edit `Packages/packages-lock.json` — Unity regenerates it from the manifest, and a corrupted lock file can break the Editor.

### Post-Upgrade Verification

After upgrading:

1. Reinstall the recorded network adapters via **Ads Mediation > Network Manager** and re-enter the recorded Developer Settings values. Do not repeat the backup warning at this point — it is only useful before deletion.
2. Only if the project switched from .unitypackage to UPM (A3): remove the `LEVELPLAY_DEPENDENCIES_INSTALLED` entry from **Project Settings > Player > Scripting Define Symbols**. Both distributions use this define, so do NOT remove it in any other situation. Removing it once after the switch is a safe reset: the stale flag from the old install would otherwise skip the UPM package's dependency verification, and the UPM installer re-verifies and re-adds the define automatically.
3. Check for deprecated API warnings in the Unity console. Then:
   - Migrate Init API → see [Scenario B](#scenario-b-migrate-init-api)
   - Migrate ad unit code → see [Scenario C](#scenario-c-migrate-ad-unit-apis)
   - Maven Central (Scenario D) applies ONLY if the project previously used a .unitypackage older than 7.9.0, or its dependency XMLs under `Assets/LevelPlay/Editor` still reference `android-sdk.is.com`. Fresh UPM installs never need it — if it does not apply, do not bring it up.

---

## Scenario B: Migrate Init API

Replaces the legacy `IronSource.Agent.init()` call with the new `LevelPlay.Init()` API.

### Namespace Change

```csharp
// Old (8.x): the classic IronSource classes (IronSource.Agent, IronSourceEvents, ...)
// live in the GLOBAL namespace — no using directive needed or possible for them.
// The 8.x LevelPlay ad-unit classes (LevelPlayRewardedAd, ...) live in:
using com.unity3d.mediation;

// New
using Unity.Services.LevelPlay;
```

### Initialization Code

**Old (deprecated):**
```csharp
IronSourceEvents.onSdkInitializationCompletedEvent += OnInitSuccess;
IronSource.Agent.setUserId("userId");
IronSource.Agent.validateIntegration();
IronSource.Agent.init(appKey);
```

**New:**
```csharp
LevelPlay.OnInitSuccess += OnInitSuccess;
LevelPlay.OnInitFailed += OnInitFailed;
LevelPlay.Init(appKey);              // basic
LevelPlay.Init(appKey, "userId");    // with optional user ID
```

### Callback Signatures

| Old | New |
|-----|-----|
| `void onSdkInitializationCompletedEvent()` | `void OnInitSuccess(LevelPlayConfiguration config)` |
| *(no failure callback)* | `void OnInitFailed(LevelPlayInitError error)` |

### Complete Migration Example

**Before:**
```csharp
void Start()
{
    IronSourceEvents.onSdkInitializationCompletedEvent += SdkInitializationCompleted;
    IronSource.Agent.setUserId("user_123");
    IronSource.Agent.init(appKey);
}

void SdkInitializationCompleted()
{
    Debug.Log("IronSource initialized");
    // load ads here
}
```

**After:**
```csharp
void Start()
{
    LevelPlay.OnInitSuccess += OnInitSuccess;
    LevelPlay.OnInitFailed += OnInitFailed;
    LevelPlay.Init(appKey, "user_123");
}

void OnInitSuccess(LevelPlayConfiguration config)
{
    Debug.Log("LevelPlay initialized");
    // create and load ad objects here
}

void OnInitFailed(LevelPlayInitError error)
{
    Debug.LogError($"Init failed: {error.ErrorMessage}");
}

void OnDestroy()
{
    LevelPlay.OnInitSuccess -= OnInitSuccess;
    LevelPlay.OnInitFailed -= OnInitFailed;
}
```

---

## Scenario C: Migrate Ad Unit APIs

The new Ad Unit API uses per-instance objects (`LevelPlayRewardedAd`, `LevelPlayInterstitialAd`, `LevelPlayBannerAd`) with Ad Unit IDs from the LevelPlay dashboard. The old `IronSource.Agent.*` static methods are deprecated.

**Get Ad Unit IDs**: LevelPlay dashboard → **Setup > Ad Units**

### C1: Rewarded Ads

**Old (deprecated):**
```csharp
// Register listeners via interface
IronSourceRewardedVideoEvents.onAdAvailableEvent += OnAdAvailable;
IronSourceRewardedVideoEvents.onAdUnavailableEvent += OnAdUnavailable;
IronSourceRewardedVideoEvents.onAdOpenedEvent += OnAdOpened;
IronSourceRewardedVideoEvents.onAdClosedEvent += OnAdClosed;
IronSourceRewardedVideoEvents.onAdRewardedEvent += OnAdRewarded;
IronSourceRewardedVideoEvents.onAdShowFailedEvent += OnAdShowFailed;
IronSourceRewardedVideoEvents.onAdClickedEvent += OnAdClicked;

// Load / Show
if (IronSource.Agent.isRewardedVideoAvailable())
    IronSource.Agent.showRewardedVideo();
```

**New:**
```csharp
private LevelPlayRewardedAd rewardedAd;

// Create after OnInitSuccess
rewardedAd = new LevelPlayRewardedAd(adUnitId);
rewardedAd.OnAdLoaded += OnAdLoaded;
rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
rewardedAd.OnAdDisplayed += OnAdDisplayed;
rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
rewardedAd.OnAdRewarded += OnAdRewarded;
rewardedAd.OnAdClosed += OnAdClosed;
rewardedAd.OnAdClicked += OnAdClicked;       // optional
rewardedAd.OnAdInfoChanged += OnAdInfoChanged; // optional

// Do NOT call rewardedAd.LoadAd() here automatically.
// Unlike the legacy IronSource rewarded video (where the SDK managed loading internally),
// the MADU API requires an explicit LoadAd() call. Add a dedicated load trigger — for
// example a UI button, a scene entry method, or a gameplay event — and call LoadAd() from
// there. If the legacy code had auto-load-on-init behavior, replace it with an explicit
// trigger rather than carrying it forward.
//
// Example explicit trigger:
// public void OnPlayerReachesRewardedOpportunity() { rewardedAd.LoadAd(); }
//
// If the game needs an ad preloaded at all times, calling LoadAd() immediately after
// subscribing events (and again in OnAdClosed) is valid — but treat it as a conscious
// choice, not a default carryover from legacy behavior.

void ShowRewardedAd(string placementName = null)
{
    // Check IsAdReady() before showing. If the game uses dashboard placements, also
    // check IsPlacementCapped(placementName) — showing a capped placement fails.
    if (rewardedAd.IsAdReady() && !LevelPlayRewardedAd.IsPlacementCapped(placementName))
        rewardedAd.ShowAd(placementName);
}
```

**Rewarded event + reward handling:**
```csharp
void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
{
    // Old: placement.GetRewardName(), placement.GetRewardAmount()
    // New:
    Debug.Log($"Reward: {reward.Name} x {reward.Amount}");
    GrantReward(reward.Name, reward.Amount);
}

// OnAdDisplayFailed — note the correct parameter types: LevelPlayAdInfo + LevelPlayAdError.
// Using LevelPlayAdDisplayInfoError (which existed in 8.x but was removed in 9.x) will cause a CS0246 compile error.
void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
{
    Debug.LogError($"Rewarded ad failed to display: {error.ErrorMessage}");
}
```

**Key API mapping:**

| Old | New |
|-----|-----|
| `IronSource.Agent.loadRewardedVideo()` | `rewardedAd.LoadAd()` |
| `IronSource.Agent.showRewardedVideo()` | `rewardedAd.ShowAd()` |
| `IronSource.Agent.isRewardedVideoAvailable()` | `rewardedAd.IsAdReady()` |
| `IronSource.Agent.isRewardedVideoPlacementCapped(name)` | `LevelPlayRewardedAd.IsPlacementCapped(name)` |
| `placement.GetRewardName()` | `reward.Name` |
| `placement.GetRewardAmount()` | `reward.Amount` |

---

### C2: Interstitial Ads

**Old (deprecated):**
```csharp
IronSourceInterstitialEvents.onAdReadyEvent += OnAdReady;
IronSourceInterstitialEvents.onAdLoadFailedEvent += OnAdLoadFailed;
IronSourceInterstitialEvents.onAdOpenedEvent += OnAdOpened;       // → OnAdDisplayed
IronSourceInterstitialEvents.onAdClosedEvent += OnAdClosed;
IronSourceInterstitialEvents.onAdShowFailedEvent += OnAdShowFailed; // → OnAdDisplayFailed
IronSourceInterstitialEvents.onAdClickedEvent += OnAdClicked;

IronSource.Agent.loadInterstitial();
if (IronSource.Agent.isInterstitialReady())
    IronSource.Agent.showInterstitial();
```

**New:**
```csharp
private LevelPlayInterstitialAd interstitialAd;

// Create after OnInitSuccess
interstitialAd = new LevelPlayInterstitialAd(adUnitId);
interstitialAd.OnAdLoaded += OnAdLoaded;
interstitialAd.OnAdLoadFailed += OnAdLoadFailed;
interstitialAd.OnAdDisplayed += OnAdDisplayed;
interstitialAd.OnAdDisplayFailed += OnAdDisplayFailed;
interstitialAd.OnAdClicked += OnAdClicked;
interstitialAd.OnAdClosed += OnAdClosed;
interstitialAd.OnAdInfoChanged += OnAdInfoChanged;

interstitialAd.LoadAd();

void ShowInterstitialAd(string placementName = null)
{
    // Check IsAdReady() before showing. If the game uses dashboard placements, also
    // check IsPlacementCapped(placementName) — showing a capped placement fails.
    if (interstitialAd.IsAdReady() && !LevelPlayInterstitialAd.IsPlacementCapped(placementName))
        interstitialAd.ShowAd(placementName);
}

void OnDestroy()
{
    interstitialAd?.DestroyAd();
}
```

**Key API mapping:**

| Old | New |
|-----|-----|
| `IronSource.Agent.loadInterstitial()` | `interstitialAd.LoadAd()` |
| `IronSource.Agent.showInterstitial()` | `interstitialAd.ShowAd()` |
| `IronSource.Agent.isInterstitialReady()` | `interstitialAd.IsAdReady()` |
| `IronSource.Agent.isInterstitialPlacementCapped(name)` | `LevelPlayInterstitialAd.IsPlacementCapped(name)` |
| `onAdReadyEvent` | `OnAdLoaded` |
| `onAdOpenedEvent` | `OnAdDisplayed` |
| `onAdShowFailedEvent` | `OnAdDisplayFailed` |
| `onAdShowSucceededEvent` | *(removed)* |

---

### C3: Banner Ads

**Old (deprecated):**
```csharp
IronSourceBannerEvents.onAdLoadedEvent += OnAdLoaded;
IronSourceBannerEvents.onAdLoadFailedEvent += OnAdLoadFailed;
IronSourceBannerEvents.onAdClickedEvent += OnAdClicked;
IronSourceBannerEvents.onAdScreenPresentedEvent += OnAdScreenPresented; // → OnAdExpanded
IronSourceBannerEvents.onAdScreenDismissedEvent += OnAdScreenDismissed; // → OnAdCollapsed

IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
IronSource.Agent.destroyBanner();
```

**New:**
```csharp
private LevelPlayBannerAd bannerAd;

// Create after OnInitSuccess.
// Default constructor (1-arg) uses BANNER size and BottomCenter position.
// Use this form when migrating a legacy call that used BANNER + BOTTOM — no need to re-specify defaults.
bannerAd = new LevelPlayBannerAd(adUnitId);

// Use Config.Builder only when specifying non-default size, position, or safe area:
// var config = new LevelPlayBannerAd.Config.Builder()
//     .SetSize(LevelPlayAdSize.LARGE)
//     .SetPosition(LevelPlayBannerPosition.TopCenter)
//     .SetRespectSafeArea(true)
//     .Build();
// bannerAd = new LevelPlayBannerAd(adUnitId, config);

bannerAd.OnAdLoaded += OnAdLoaded;
bannerAd.OnAdLoadFailed += OnAdLoadFailed;
bannerAd.OnAdDisplayed += OnAdDisplayed;
bannerAd.OnAdDisplayFailed += OnAdDisplayFailed;
bannerAd.OnAdClicked += OnAdClicked;
bannerAd.OnAdCollapsed += OnAdCollapsed;
bannerAd.OnAdExpanded += OnAdExpanded;
bannerAd.OnAdLeftApplication += OnAdLeftApplication;

bannerAd.LoadAd();
bannerAd.ShowAd();

// HideAd() vs DestroyAd() — these are not equivalent (see note below).
bannerAd.HideAd();
bannerAd.PauseAutoRefresh();
bannerAd.ResumeAutoRefresh();

void OnDestroy()
{
    bannerAd?.DestroyAd();
}
```

**HideAd() vs DestroyAd() — choose the right one when migrating `destroyBanner()`:**

The legacy `IronSource.Agent.destroyBanner()` maps to two different new APIs depending on intent:

- `bannerAd.HideAd()` — hides the banner but keeps the instance alive. Use this for a visibility toggle (e.g., hiding during gameplay and re-showing later with `ShowAd()`). No need to recreate the object or reload.
- `bannerAd.DestroyAd()` — tears down the instance entirely. Use this only for permanent lifecycle cleanup (e.g., `OnDestroy()` / `OnDisable()`). To show a banner again after calling `DestroyAd()`, a new `LevelPlayBannerAd` object must be created and `LoadAd()` called again.

When migrating, ask: was the legacy `destroyBanner()` call being used to temporarily hide the banner, or to permanently remove it? If temporarily hiding, use `HideAd()`. If in doubt, prefer `HideAd()` for visibility toggling and reserve `DestroyAd()` for `OnDestroy()`/`OnDisable()`.

**Banner size mapping:**

| Old (`IronSourceBannerSize`) | New (`LevelPlayAdSize`) | Dimensions (dp) |
|------------------------------|-------------------------|-----------------|
| `BANNER` | `LevelPlayAdSize.BANNER` | 320 × 50 |
| `LARGE` | `LevelPlayAdSize.LARGE` | 320 × 90 |
| `RECTANGLE` | `LevelPlayAdSize.MEDIUM_RECTANGLE` | 300 × 250 |
| `SMART` | `LevelPlayAdSize.CreateAdaptiveAdSize()` | Auto-adjusting |

**Adaptive size (recommended):**
```csharp
// The 9.x constructor only accepts (adUnitId, Config) — passing a LevelPlayAdSize
// directly does not compile. Set the size through Config.Builder:
var adaptiveConfig = new LevelPlayBannerAd.Config.Builder()
    .SetSize(LevelPlayAdSize.CreateAdaptiveAdSize())
    .Build();
bannerAd = new LevelPlayBannerAd(adUnitId, adaptiveConfig);
```

**Key API mapping:**

| Old | New |
|-----|-----|
| `IronSource.Agent.loadBanner(size, pos)` | `bannerAd.LoadAd()` (size/position in constructor) |
| `IronSource.Agent.destroyBanner()` | `bannerAd.HideAd()` (visibility toggle) or `bannerAd.DestroyAd()` (lifecycle teardown) |
| `IronSource.Agent.displayBanner()` | `bannerAd.ShowAd()` |
| `IronSource.Agent.hideBanner()` | `bannerAd.HideAd()` |
| `onAdScreenPresentedEvent` | `OnAdExpanded` |
| `onAdScreenDismissedEvent` | `OnAdCollapsed` |

### C4: Impression Data (ILRD) Handler

Migrate the event subscription first, then the handler.

**Subscription — the event name changes, and the correct new event depends on the SDK version:**

**Old (deprecated):**
```csharp
IronSourceEvents.onImpressionDataReadyEvent += ImpressionDataReadyEvent;
```

**New — SDK 9.4.x and earlier** (single global event, subscribe before `LevelPlay.Init()`):
```csharp
LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;
```

**New — SDK 9.5.0+** (per ad instance, subscribe when each ad object is created; the global event still exists but is deprecated and generates a compiler warning):
```csharp
rewardedAd.OnAdImpressionDataReady += ImpressionDataReadyEvent;
interstitialAd.OnAdImpressionDataReady += ImpressionDataReadyEvent;
bannerAd.OnAdImpressionDataReady += ImpressionDataReadyEvent;
```

The new global event has NO "Event" suffix: `LevelPlay.OnImpressionDataReadyEvent` has never existed and will not compile. Beware: the 8.x SDK's own deprecation message on the legacy event misnames the replacement as `LevelPlay.OnImpressionDataReadyEvent` — do not follow that message; the correct member is `LevelPlay.OnImpressionDataReady`. Unsubscribe in `OnDestroy()` using the same event names. For forwarding the data to an analytics platform, see `references/ilrd-api.md`.

**Handler — preserve both log lines and apply the property rename:**

**Old:**
```csharp
void ImpressionDataReadyEvent(IronSourceImpressionData impressionData)
{
    Debug.Log("ImpressionDataReadyEvent ToString(): " + impressionData.ToString());
    Debug.Log("ImpressionDataReadyEvent allData: " + impressionData.allData);
}
```

**New:**
```csharp
void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
{
    Debug.Log("ImpressionDataReadyEvent ToString(): " + impressionData.ToString());
    // allData renamed to AllData (camelCase to PascalCase)
    Debug.Log("ImpressionDataReadyEvent AllData: " + impressionData.AllData);
}
```

**Key property rename:**

| Old | New |
|-----|-----|
| `impressionData.allData` | `impressionData.AllData` |

Both log lines should be preserved. Migrations that keep only the `ToString()` line silently drop the raw impression-data dump, which may break analytics code that reads `allData` directly.

### C5: Migration Completeness Checklist

A faithful line-by-line translation is not enough. Several 9.x requirements have no counterpart line in the legacy code, so translating only what is there will silently miss them. After migrating, verify each item against the new code:

- [ ] **Show paths check `IsAdReady()`, plus `IsPlacementCapped(placementName)` when the game uses dashboard placements** — the legacy code only checked availability. If placements are in use, add the capping check (showing a capped placement fails); if the game does not use placements, note that and move on.
- [ ] **A rewarded load trigger exists and is publisher-controlled** — the legacy SDK auto-loaded rewarded video internally, so legacy code has no load call to translate; the migration must ADD one (for example a Load Rewarded Video button mirroring the interstitial's, or a scene-entry call). Without it the rewarded ad can never become ready. No auto-load in `OnInitSuccess` and no auto-reload in `OnAdClosed` unless the publisher deliberately chooses a preload pattern — when repairing or choosing, ask which the publisher prefers.
- [ ] **Version APIs map one-to-one** — `IronSource.unityVersion()` → `LevelPlay.UnityVersion`, and `IronSource.pluginVersion()` → `LevelPlay.PluginVersion`. They return different values; do not swap or merge them.
- [ ] **Logging is preserved, not expanded** — keep the legacy code's log lines (renamed as needed), but do not add new log statements the legacy code did not have.
- [ ] **ILRD subscription uses the correct event name, and both handler log lines survive** — `onImpressionDataReadyEvent` becomes `LevelPlay.OnImpressionDataReady` on SDK 9.4.x and earlier, or per-instance `OnAdImpressionDataReady` on 9.5.0+; `LevelPlay.OnImpressionDataReadyEvent` does not exist. `allData` is renamed to `AllData` (see C4).
- [ ] **`destroyBanner()` intent resolved** — `HideAd()` for a visibility toggle, `DestroyAd()` only in `OnDestroy()`/`OnDisable()` (see C3).
- [ ] **`IronSource.Agent.onApplicationPause(isPaused)` is removed, not replaced.** The legacy API forwarded Unity's pause/resume to the native SDK; in 9.x the SDK fetches pause/resume from the app lifecycle itself, so there is no equivalent to call. `LevelPlay.OnApplicationPause` does not exist and will not compile. `LevelPlay.SetPauseGame(bool)` is a different feature (pausing the game while an ad is showing) — it is not a substitute. If the Unity `OnApplicationPause` override contained only this call, delete the whole override and tell the user why.
- [ ] **No invented APIs** — every LevelPlay member the migrated code uses should appear in this guide or in the installed package. If unsure whether a member exists, verify before using it; a plausible-sounding name that does not exist costs the user a compile-fix cycle.

---

## Scenario D: Maven Central Migration

LevelPlay SDK Android dependencies moved from `is.com` to Maven Central. This migration was required by June 30, 2025. If your project still references `is.com` repositories, Android builds are currently failing. Follow the steps below to fix it.

### Check If Migration Is Needed

1. Go to `Assets > LevelPlay > Editor`
2. Open any dependency XML (e.g., `IronSourceSDKDependencies.xml`)
3. Look for `https://android-sdk.is.com/` inside `<repository>` tags

**If found** → Manual migration required (see below)  
**If not found** → Already migrated, no action needed

### Detect Old Format
```xml
<!-- OLD - needs migration -->
<androidPackage spec="com.ironsource.sdk:mediationsdk:7.9.0">
  <repositories>
    <repository>https://android-sdk.is.com/</repository>
  </repositories>
</androidPackage>
```

### Expected New Format
```xml
<!-- NEW - correct -->
<androidPackage spec="com.unity3d.ads-mediation:mediation-sdk:x.x.x">
</androidPackage>
```

### Migration Steps

**For LevelPlay Package:**
1. Go to `Assets > LevelPlay > Editor`
2. Delete `IronSourceSDKDependencies.xml` and all `IS*AdapterDependencies.xml` files
3. Open **Ads Mediation > Network Manager** → reinstall LevelPlay SDK and all required adapters
4. Verify XML files now use `com.unity3d.ads-mediation` (no `is.com` references)

**For Ad Quality SDK:**
1. Go to `Assets > LevelPlay > Editor`
2. Delete `IronSourceAdQualityDependencies.xml`
3. Download the Maven Central version (Ad Quality 7.24.0+) and place it in `Assets > LevelPlay > Editor`
4. Verify the file references `com.unity3d.ads-mediation:adquality-sdk`

**Minimum versions required:**
- LevelPlay Unity Package: **7.9.0+**
- Ad Quality SDK: **7.19.2+**

---

## Scenario E: Migrate from Unity Ads to LevelPlay

For projects using the `Advertisement Legacy` Unity package.

> **Note**: Since April 1, 2026, direct Unity Ads integration may deliver reduced ad performance. Migrating to LevelPlay is now strongly recommended.

### Step 1: Install LevelPlay Package

1. **Window > Package Manager** → Packages: Unity Registry → search **Ads Mediation** → **Install**
2. If prompted to install Mobile Dependency Resolver, click **Import**
3. Do **not** remove Advertisement Legacy yet — keep both until migration is complete

### Step 2: Configure LevelPlay Dashboard

1. Log in at [platform.ironsrc.com](https://platform.ironsrc.com/partners/identity/login)
2. Add your app → create ad units (Banner, Interstitial, Rewarded)
3. Copy your **App Key** and **Ad Unit IDs**

### Step 3: Replace Initialization Code

**Replace:**
```csharp
Advertisement.Initialize(_gameId, _testMode, this);
void OnInitializationComplete() { /* load ads */ }
void OnInitializationFailed(UnityAdsInitializationError error, string message) { }
```

**With:**
```csharp
using Unity.Services.LevelPlay;

LevelPlay.OnInitSuccess += OnInitSuccess;
LevelPlay.OnInitFailed += OnInitFailed;
LevelPlay.Init("YOUR_APP_KEY");   // use appKey, not gameId; test mode not supported

void OnInitSuccess(LevelPlayConfiguration config)
{
    // Create ad objects here
}

void OnInitFailed(LevelPlayInitError error)
{
    Debug.LogError($"Init failed: {error.ErrorMessage}");
}
```

**Test mode:** if the legacy code passed `testMode` to `Advertisement.Initialize`, tell the user explicitly that `LevelPlay.Init()` has no test-mode parameter — do not drop the flag silently. The equivalents are the LevelPlay Test Suite on a device build, or enabling test mode in the LevelPlay dashboard.

### Step 4: Replace Ad Implementation

Use the Ad Unit API examples from [Scenario C](#scenario-c-migrate-ad-unit-apis) for each format.

**Unity Ads → LevelPlay API mapping:**

| Use Case | Unity Ads | LevelPlay |
|----------|-----------|-----------|
| Initialization ID | `gameId` | `appKey` |
| Initialize | `Advertisement.Initialize()` | `LevelPlay.Init()` |
| Init Success | `OnInitializationComplete()` | `LevelPlay.OnInitSuccess` |
| Init Failed | `OnInitializationFailed()` | `LevelPlay.OnInitFailed` |
| Is Initialized | `Advertisement.isInitialized` | Not supported |
| Set MetaData | `Advertisement.SetMetaData()` | `LevelPlay.SetMetaData(key, value)` |
| Banner Load | `Advertisement.Banner.Load()` | `bannerAd.LoadAd()` |
| Banner Show | `Advertisement.Banner.Show()` | `bannerAd.ShowAd()` |
| Banner Hide | `Advertisement.Banner.Hide()` | `bannerAd.HideAd()` |
| Rewarded/Interstitial Load | `Advertisement.Load(placementId, listener)` | `ad.LoadAd()` |
| Rewarded/Interstitial Show | `Advertisement.Show(placementId, listener)` | `ad.ShowAd()` |
| Ad Loaded | `OnUnityAdsAdLoaded()` | `OnAdLoaded` event on instance |
| Ad Failed to Load | `OnUnityAdsFailedToLoad()` | `OnAdLoadFailed` event on instance |
| Ad Show Start | `OnUnityAdsShowStart()` | `OnAdDisplayed` event on instance |
| Ad Show Complete | `OnUnityAdsShowComplete()` | `OnAdClosed` event on instance |
| Ad Show Failed | `OnUnityAdsShowFailure()` | `OnAdDisplayFailed` event on instance |
| Ad Clicked | `OnUnityAdsShowClick()` | `OnAdClicked` event on instance |
| Plugin Version | `Advertisement.version` | `LevelPlay.PluginVersion` |

**Key differences:**
- LevelPlay uses **instance-based** ad objects instead of static methods
- Uses **App Key** instead of Game ID
- Events are subscribed per instance, not via global interfaces
- **No test mode parameter** in `LevelPlay.Init()`
- Banner position/size configured in constructor, not via separate API call

### Step 5: Remove Advertisement Legacy Package

Only after verifying all ads work correctly:

1. **Window > Package Manager** → Packages: In Project → select **Advertisement Legacy** → **Remove**

⚠️ Do not remove until all ad implementations are tested and working.

---

## API Reference: Legacy vs. New

### Initialization

| | Legacy (IronSource) | New (LevelPlay) |
|--|---------------------|-----------------|
| Namespace | global namespace (classic IronSource classes) / `com.unity3d.mediation` (8.x ad-unit classes) | `Unity.Services.LevelPlay` |
| Init method | `IronSource.Agent.init(appKey)` | `LevelPlay.Init(appKey)` |
| User ID | `IronSource.Agent.setUserId(id)` | `LevelPlay.Init(appKey, userId)` |
| Success callback | `onSdkInitializationCompletedEvent` | `LevelPlay.OnInitSuccess` |
| Failure callback | *(none)* | `LevelPlay.OnInitFailed` |
| Validate | `IronSource.Agent.validateIntegration()` | `LevelPlay.ValidateIntegration()` |
| Test Suite | `IronSource.Agent.launchTestSuite()` | `LevelPlay.LaunchTestSuite()` |
| Unity version | `IronSource.unityVersion()` | `LevelPlay.UnityVersion` (not `PluginVersion`) |
| Plugin version | `IronSource.pluginVersion()` | `LevelPlay.PluginVersion` (not `UnityVersion`) |
| App pause notify | `IronSource.Agent.onApplicationPause(isPaused)` | *(removed — handled internally in 9.x; delete the call, see checklist C5)* |

### Rewarded

| | Legacy | New |
|--|--------|-----|
| Load | `IronSource.Agent.loadRewardedVideo()` | `rewardedAd.LoadAd()` |
| Show | `IronSource.Agent.showRewardedVideo()` | `rewardedAd.ShowAd()` |
| Is ready | `IronSource.Agent.isRewardedVideoAvailable()` | `rewardedAd.IsAdReady()` |
| Placement capped | `IronSource.Agent.isRewardedVideoPlacementCapped(name)` | `LevelPlayRewardedAd.IsPlacementCapped(name)` |

### Interstitial

| | Legacy | New |
|--|--------|-----|
| Load | `IronSource.Agent.loadInterstitial()` | `interstitialAd.LoadAd()` |
| Show | `IronSource.Agent.showInterstitial()` | `interstitialAd.ShowAd()` |
| Is ready | `IronSource.Agent.isInterstitialReady()` | `interstitialAd.IsAdReady()` |
| Placement capped | `IronSource.Agent.isInterstitialPlacementCapped(name)` | `LevelPlayInterstitialAd.IsPlacementCapped(name)` |

### Banner

| | Legacy | New |
|--|--------|-----|
| Load | `IronSource.Agent.loadBanner(size, pos)` | `bannerAd.LoadAd()` |
| Destroy | `IronSource.Agent.destroyBanner()` | `bannerAd.HideAd()` (visibility toggle) or `bannerAd.DestroyAd()` (lifecycle teardown) — see C3 |
| Show | `IronSource.Agent.displayBanner()` | `bannerAd.ShowAd()` |
| Hide | `IronSource.Agent.hideBanner()` | `bannerAd.HideAd()` |

### Impression Data (ILRD)

| | Legacy | New |
|--|--------|-----|
| Event | `IronSourceEvents.onImpressionDataReadyEvent` | SDK 9.4.x and earlier: `LevelPlay.OnImpressionDataReady` (before `Init()`) — SDK 9.5.0+: `OnAdImpressionDataReady` on each ad instance |
| Payload type | `IronSourceImpressionData` | `LevelPlayImpressionData` |
| Raw data property | `impressionData.allData` | `impressionData.AllData` |
