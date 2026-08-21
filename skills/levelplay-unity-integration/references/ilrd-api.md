# Impression Level Revenue (ILR) Integration

## Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Key Characteristics](#key-characteristics)
- [Implementation](#implementation)
- [API Reference](#api-reference)
- [Thread Safety](#thread-safety)
- [Integration with Third-Party Analytics](#integration-with-third-party-analytics)
- [Best Practices](#best-practices)
- [Common Issues](#common-issues)
- [Testing Checklist](#testing-checklist)

## Overview

Use the Impression Level Revenue (ILR) solution to track ad revenue at both device and impression levels, integrating with third-party analytics tools for deeper insights.

## Prerequisites

- LevelPlay SDK 7.0.3+ correctly integrated
- Refer to Unity Package integration guide

For more information about the Impression Level Revenue (ILR) SDK feature and pre-requisites, refer to the Impression-level revenue server-side API documentation.

## Key Characteristics

- **Real-time data**: Fires postbacks to inform you about displayed ads
- **Background thread**: Callback runs on a background thread, not the Unity main thread
- **Optional listener**: Provides information about all ad units
- **All ad formats**: Works with Rewarded, Interstitial, and Banner ads

## Implementation

ILRD is delivered differently depending on your SDK version. Check your version in **Ads Mediation > Network Manager**.

| SDK version | ILRD delivery | Where you subscribe |
|-------------|---------------|---------------------|
| **9.5.0+ (current)** | Per ad instance — `OnAdImpressionDataReady` on each ad object (`LevelPlayRewardedAd`, `LevelPlayInterstitialAd`, `LevelPlayBannerAd`) | When you create each ad |
| **9.4.x and earlier** | Single global event — `LevelPlay.OnImpressionDataReady` | Once, before `LevelPlay.Init()` |

On SDK 9.5.0+, the global `LevelPlay.OnImpressionDataReady` still exists but is **deprecated and generates a compiler warning** — use the per-instance `OnAdImpressionDataReady` event instead.

### SDK 9.5.0+ (current): Per-Instance Setup

Subscribe to `OnAdImpressionDataReady` on each ad object right after you create it — typically inside the ad manager classes from `rewarded-api.md` / `interstitial-api.md` / `banner-api.md`. There is no "before Init" ordering concern with the per-instance event; you subscribe when the ad is created.

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class RewardedAdManager : MonoBehaviour
{
    private LevelPlayRewardedAd rewardedAd;
    private string adUnitId = "YOUR_REWARDED_AD_UNIT_ID";

    void Start()
    {
        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        // ILRD (SDK 9.5.0+): subscribe per ad instance.
        // Fires on a BACKGROUND thread — do not call Unity APIs directly here.
        rewardedAd.OnAdImpressionDataReady += OnImpressionDataReady;

        // ... register other event listeners and call LoadAd() as usual
    }

    void OnDestroy()
    {
        if (rewardedAd != null)
        {
            rewardedAd.OnAdImpressionDataReady -= OnImpressionDataReady;
            // ... unsubscribe other events
        }
    }

    private void OnImpressionDataReady(LevelPlayImpressionData impressionData)
    {
        // Runs on a background thread — see "Integration with Third-Party Analytics".
        if (impressionData == null) return;
        Debug.Log($"ILRD - {impressionData.AdNetwork} / {impressionData.AdFormat} / ${impressionData.Revenue}");
    }
}
```

Repeat the same `OnAdImpressionDataReady` subscription on your interstitial and banner ad objects. Every ad format reports its own impressions through its own instance event.

### SDK 9.4.x and earlier (legacy): Global Event Setup

On older SDKs, use the single global `LevelPlay.OnImpressionDataReady` event, registered **before** `LevelPlay.Init()`:

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class ImpressionRevenueManager : MonoBehaviour
{
    void Start()
    {
        // Register impression data callback BEFORE SDK initialization
        // This event is triggered on a background thread, not the Unity main thread.
        LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;

        // Register initialization callbacks
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        // Initialize SDK
        LevelPlay.Init("YOUR_APP_KEY");
    }

    void OnDestroy()
    {
        // Unregister callbacks
        LevelPlay.OnImpressionDataReady -= ImpressionDataReadyEvent;
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }

    private void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
    {
        // IMPORTANT: This runs on a background thread, not the main thread
        // Do not call Unity APIs directly from here
        Debug.Log($"ILR - Ad Network: {impressionData.AdNetwork}");
        Debug.Log($"ILR - Revenue: ${impressionData.Revenue}");
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay initialized");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"Init failed: {error.ErrorMessage}");
    }
}
```

**Key points (global event — SDK 9.4.x and earlier only):**
- Declare the listener **BEFORE** initializing the LevelPlay SDK to avoid any loss of information
- Callback runs on a **background thread**
- Don't call Unity APIs directly in the callback
- On SDK 9.5.0+ this global event is deprecated — use the per-instance `OnAdImpressionDataReady` shown above instead

### Accessing Impression Data

You can refer to each field separately or get all information by using the AllData property:

```csharp
private void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
{
    string allData = impressionData.AllData;
    string adNetwork = impressionData.AdNetwork;
    double? revenue = impressionData.Revenue;
}
```

**Important:** The returned data might include null values. To avoid potential crashes, ensure that you add protections before assigning the data.

## API Reference

### Events

#### `adObject.OnAdImpressionDataReady` (SDK 9.5.0+ — current)

Per-instance event on each ad object (`LevelPlayRewardedAd`, `LevelPlayInterstitialAd`, `LevelPlayBannerAd`). Fired when that ad reports an impression with revenue data.

**Signature:** `event Action<LevelPlayImpressionData>`

**Usage:**
```csharp
// Subscribe after creating the ad object. Fires on a background thread.
rewardedAd.OnAdImpressionDataReady += ImpressionDataReadyEvent;
```

**Important:**
- Subscribe when you create the ad object; unsubscribe in `OnDestroy()`
- Callback runs on **background thread**
- Each ad format reports through its own instance event

#### `LevelPlay.OnImpressionDataReady` (SDK 9.4.x and earlier — deprecated in 9.5.0+)

Single global event. Fired when any impression occurs and revenue data is available.

**Signature:** `static event Action<LevelPlayImpressionData>`

**Usage:**
```csharp
// This event is triggered on a background thread, not the Unity main thread.
LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;
```

**Important:**
- Register **BEFORE** `LevelPlay.Init()`
- Callback runs on **background thread**
- Fires for all ad formats (Rewarded, Interstitial, Banner)
- **Deprecated in SDK 9.5.0+** (generates a compiler warning) — use the per-instance event above instead

### Data Type

#### `LevelPlayImpressionData`

Contains detailed information about an ad impression.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `AllData` | string/dictionary | All impression data as a structured object |
| `AuctionId` | string | Unique auction identifier |
| `MediationAdUnitName` | string | Mediation ad unit name |
| `MediationAdUnitId` | string | Mediation ad unit identifier |
| `AdFormat` | string | Ad format (e.g., "REWARDED", "INTERSTITIAL", "BANNER") |
| `AdNetwork` | string | Ad network that served the ad |
| `InstanceName` | string | Ad network instance name |
| `InstanceId` | string | Ad network instance identifier |
| `Country` | string | User's country code |
| `Placement` | string | Placement name where ad was shown |
| `Revenue` | double? | Estimated revenue in USD (nullable - check for null) |
| `Precision` | string | Revenue precision level |
| `Ab` | string | A/B test segment identifier |
| `SegmentName` | string | User segment name |
| `EncryptedCpm` | string | Encrypted CPM value |
| `ConversionValue` | number? | iOS SKAdNetwork conversion value (iOS only) |
| `CreativeId` | string | Creative identifier |

**Example:**
```csharp
private void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
{
    string allData = impressionData.AllData;
    string adNetwork = impressionData.AdNetwork;
    double? revenue = impressionData.Revenue;
}
```

**Note:** For the full list of available ILR data, including field description and types, refer to Impression-level revenue server-side API documentation.

## Thread Safety

**CRITICAL:** `OnImpressionDataReady` runs on a background thread. This means:

**❌ DO NOT:**
- Call Unity APIs directly (e.g., `GameObject.Find()`, `transform.position`)
- Access Unity components or game objects
- Update UI elements directly
- Call coroutines

**✅ DO:**
- Queue data for processing on main thread
- Use thread-safe operations
- Check for null values before using data

## Integration with Third-Party Analytics

### Firebase Analytics Example

The following example details how to integrate the Impression Level Revenue SDK API data with Google Analytics for Firebase. You can use it as-is or make any required changes to integrate with third-party reporting tools or your own proprietary optimization tools and databases.

**Important:** Ensure that the inside parameters aren't null.

```csharp
private void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
{
    Debug.Log("unity-script: ImpressionDataReadyEvent impressionData = " + impressionData);

    if (impressionData != null)
    {
        Firebase.Analytics.Parameter[] AdParameters = {
            new Firebase.Analytics.Parameter("ad_platform", "LevelPlay"),
            new Firebase.Analytics.Parameter("ad_source", impressionData.AdNetwork),
            new Firebase.Analytics.Parameter("ad_format", impressionData.AdFormat),
            new Firebase.Analytics.Parameter("ad_unit_name", impressionData.InstanceName),
            new Firebase.Analytics.Parameter("currency", "USD"),
            new Firebase.Analytics.Parameter("value", impressionData.Revenue ?? 0) // Add protection for null values
        };

        Firebase.Analytics.FirebaseAnalytics.LogEvent("custom_ad_impression", AdParameters);
    }
}
```

### Integration with Other Tools

After you implement the ImpressionDataListener, you can send the impression data to:
- Your own proprietary BI tools and data warehouses
- Third-party analytics platforms
- Attribution providers
- Custom backend services

## Best Practices

1. **Subscribe correctly for your SDK version**: On SDK 9.5.0+, subscribe to each ad's `OnAdImpressionDataReady` when you create the ad. On SDK 9.4.x and earlier, register the global `LevelPlay.OnImpressionDataReady` before calling `LevelPlay.Init()`
2. **Handle background thread**: Don't call Unity APIs directly in the callback
3. **Check for null values**: Revenue and other properties may be null - always check before using
4. **Protect against crashes**: Add null checks to avoid potential crashes
5. **Unregister on destroy**: Prevent memory leaks by unregistering in `OnDestroy()`

## Common Issues

### Issue: Callback never fires

**Possible causes:**
- **SDK 9.5.0+:** not subscribed to the ad instance's `OnAdImpressionDataReady`, or subscribed to a different ad object than the one shown
- **SDK 9.4.x and earlier:** global `LevelPlay.OnImpressionDataReady` not registered before `LevelPlay.Init()`
- SDK not initialized successfully
- No ads shown yet

**Solutions:**
- On 9.5.0+, subscribe to `OnAdImpressionDataReady` on the same ad object you load/show
- On 9.4.x and earlier, register the global callback before calling `Init()`
- Verify `OnInitSuccess` fires
- Show an ad and check if callback fires

### Issue: Unity APIs crash in callback

**Cause:** Calling Unity APIs from background thread

**Solution:** Don't call Unity APIs directly in the callback. Queue data for main thread processing if needed.

### Issue: Revenue is null

**Cause:** Some ad networks don't provide revenue data

**Solution:** Always check `impressionData.Revenue.HasValue` or use `impressionData.Revenue ?? 0` to provide a default value

## Testing Checklist

- [ ] Subscribed correctly for your SDK version (9.5.0+: per-instance `OnAdImpressionDataReady`; 9.4.x and earlier: global `OnImpressionDataReady` before `Init()`)
- [ ] Callback fires when ads are shown
- [ ] Revenue data is received (check for null)
- [ ] Null checks added to prevent crashes
- [ ] Analytics/backend integration works correctly
- [ ] No crashes from Unity API calls in callback
- [ ] Callback unregistered in `OnDestroy()`
- [ ] Tested with all ad formats (Rewarded, Interstitial, Banner)
