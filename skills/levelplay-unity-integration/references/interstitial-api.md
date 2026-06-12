# Interstitial Ads API Reference

## Contents
- [Overview](#overview)
- [Key Characteristics](#key-characteristics)
- [Implementation Pattern](#implementation-pattern)
- [API Reference](#api-reference)
- [Data Types](#data-types)
- [Best Practices](#best-practices)
- [Common Issues](#common-issues)
- [Testing Checklist](#testing-checklist)

## Overview

Interstitial ads are full-screen video or static ads shown at natural transition points in your app. They provide good revenue while maintaining acceptable user experience when placed thoughtfully.

## Key Characteristics

- **Full-screen**: Takes over the entire screen
- **Skippable**: User can close after viewing
- **Best for**: Level transitions, game over, menu navigation, session breaks

## Implementation Pattern

### Basic Interstitial Implementation

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class InterstitialAdManager : MonoBehaviour
{
    private LevelPlayInterstitialAd interstitialAd;
    private string adUnitId = "YOUR_INTERSTITIAL_AD_UNIT_ID";

    void Start()
    {
        // Create the interstitial ad object using constructor
        interstitialAd = new LevelPlayInterstitialAd(adUnitId);

        // Register event listeners
        interstitialAd.OnAdLoaded += OnAdLoaded;
        interstitialAd.OnAdLoadFailed += OnAdLoadFailed;
        interstitialAd.OnAdDisplayed += OnAdDisplayed;
        interstitialAd.OnAdDisplayFailed += OnAdDisplayFailed;
        interstitialAd.OnAdClicked += OnAdClicked;
        interstitialAd.OnAdClosed += OnAdClosed;
        interstitialAd.OnAdInfoChanged += OnAdInfoChanged;   // Fires when the loaded ad updates after a new auction result

        // Load the ad
        LoadAd();
    }

    void OnDestroy()
    {
        // Unregister event listeners
        if (interstitialAd != null)
        {
            interstitialAd.OnAdLoaded -= OnAdLoaded;
            interstitialAd.OnAdLoadFailed -= OnAdLoadFailed;
            interstitialAd.OnAdDisplayed -= OnAdDisplayed;
            interstitialAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            interstitialAd.OnAdClicked -= OnAdClicked;
            interstitialAd.OnAdClosed -= OnAdClosed;
            interstitialAd.OnAdInfoChanged -= OnAdInfoChanged;
        }
    }

    public void LoadAd()
    {
        Debug.Log("Loading interstitial ad...");
        interstitialAd.LoadAd();
    }

    public void ShowAd()
    {
        if (interstitialAd.IsAdReady())
        {
            Debug.Log("Showing interstitial ad");
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("Interstitial ad is not ready yet");
        }
    }

    // Event Callbacks
    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial ad loaded successfully");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Interstitial ad failed to load: {error.ErrorMessage}");
        // Retry loading after a delay
        Invoke(nameof(LoadAd), 30f);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial ad displayed");
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Interstitial ad failed to display: {error.ErrorMessage}");
        // Load a new ad
        LoadAd();
    }

    private void OnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial ad clicked");
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial ad closed");
        // Load the next ad
        LoadAd();
    }

    private void OnAdInfoChanged(LevelPlayAdInfo adInfo)
    {
        // Optional: log or update analytics with the latest revenue estimate
        Debug.Log($"Interstitial ad info changed - network: {adInfo.AdNetwork}, revenue: ${adInfo.Revenue}");
    }
}
```

### Advanced: Frequency Capping

Implement time-based frequency capping to avoid showing ads too frequently:

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class FrequencyCappedInterstitialManager : MonoBehaviour
{
    private LevelPlayInterstitialAd interstitialAd;
    private string adUnitId = "YOUR_INTERSTITIAL_AD_UNIT_ID";

    // Frequency capping settings
    private float minTimeBetweenAds = 300f; // 5 minutes in seconds
    private float lastAdShownTime = -999f; // Time when last ad was shown

    void Start()
    {
        // Create interstitial ad object
        interstitialAd = new LevelPlayInterstitialAd(adUnitId);

        // Register callbacks
        interstitialAd.OnAdLoaded += OnAdLoaded;
        interstitialAd.OnAdLoadFailed += OnAdLoadFailed;
        interstitialAd.OnAdDisplayed += OnAdDisplayed;
        interstitialAd.OnAdClosed += OnAdClosed;
        interstitialAd.OnAdDisplayFailed += OnAdDisplayFailed;

        // Load first ad
        interstitialAd.LoadAd();
    }

    void OnDestroy()
    {
        if (interstitialAd != null)
        {
            interstitialAd.OnAdLoaded -= OnAdLoaded;
            interstitialAd.OnAdLoadFailed -= OnAdLoadFailed;
            interstitialAd.OnAdDisplayed -= OnAdDisplayed;
            interstitialAd.OnAdClosed -= OnAdClosed;
            interstitialAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        }
    }

    public void TryShowInterstitial()
    {
        // Check if enough time has passed since last ad
        float timeSinceLastAd = Time.time - lastAdShownTime;

        if (timeSinceLastAd < minTimeBetweenAds)
        {
            Debug.Log($"Frequency cap: {minTimeBetweenAds - timeSinceLastAd:F0}s until next ad");
            return;
        }

        // Check if ad is ready
        if (interstitialAd.IsAdReady())
        {
            interstitialAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("Interstitial not ready, loading...");
            interstitialAd.LoadAd();
        }
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial loaded");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Interstitial load failed: {error.ErrorMessage}");
        Invoke(nameof(LoadAd), 30f);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial displayed");
        lastAdShownTime = Time.time; // Record when ad was shown
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial closed");
        // Load next ad
        interstitialAd.LoadAd();
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Interstitial display failed: {error.ErrorMessage}");
        interstitialAd.LoadAd();
    }

    private void LoadAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.LoadAd();
        }
    }
}
```

### Advanced: Multiple Placements

Track different interstitial placements for analytics:

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class MultiPlacementInterstitialManager : MonoBehaviour
{
    private LevelPlayInterstitialAd interstitialAd;
    private string adUnitId = "YOUR_INTERSTITIAL_AD_UNIT_ID";

    // Placement names
    private const string PLACEMENT_LEVEL_COMPLETE = "level_complete";
    private const string PLACEMENT_GAME_OVER = "game_over";
    private const string PLACEMENT_MAIN_MENU = "main_menu";

    // Frequency capping per placement
    private float minTimeBetweenAds = 300f;
    private float lastAdTime = -999f;

    void Start()
    {
        interstitialAd = new LevelPlayInterstitialAd(adUnitId);

        interstitialAd.OnAdLoaded += OnAdLoaded;
        interstitialAd.OnAdLoadFailed += OnAdLoadFailed;
        interstitialAd.OnAdDisplayed += OnAdDisplayed;
        interstitialAd.OnAdClosed += OnAdClosed;
        interstitialAd.OnAdDisplayFailed += OnAdDisplayFailed;

        interstitialAd.LoadAd();
    }

    void OnDestroy()
    {
        if (interstitialAd != null)
        {
            interstitialAd.OnAdLoaded -= OnAdLoaded;
            interstitialAd.OnAdLoadFailed -= OnAdLoadFailed;
            interstitialAd.OnAdDisplayed -= OnAdDisplayed;
            interstitialAd.OnAdClosed -= OnAdClosed;
            interstitialAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        }
    }

    // Public methods for different placements
    public void ShowOnLevelComplete()
    {
        TryShowWithPlacement(PLACEMENT_LEVEL_COMPLETE);
    }

    public void ShowOnGameOver()
    {
        TryShowWithPlacement(PLACEMENT_GAME_OVER);
    }

    public void ShowOnMainMenu()
    {
        TryShowWithPlacement(PLACEMENT_MAIN_MENU);
    }

    private void TryShowWithPlacement(string placementName)
    {
        // Check frequency cap
        if (Time.time - lastAdTime < minTimeBetweenAds)
        {
            Debug.Log($"Frequency cap active for {placementName}");
            return;
        }

        // Check placement capping
        if (LevelPlayInterstitialAd.IsPlacementCapped(placementName))
        {
            Debug.Log($"Placement {placementName} is capped");
            return;
        }

        // Check if ad ready
        if (interstitialAd.IsAdReady())
        {
            Debug.Log($"Showing interstitial with placement: {placementName}");
            interstitialAd.ShowAd(placementName: placementName);
        }
        else
        {
            Debug.LogWarning("Interstitial not ready");
        }
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial loaded");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Interstitial load failed: {error.ErrorMessage}");
        Invoke(nameof(LoadAd), 30f);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial displayed");
        lastAdTime = Time.time;
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Interstitial closed");
        interstitialAd.LoadAd();
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Interstitial display failed: {error.ErrorMessage}");
        interstitialAd.LoadAd();
    }

    private void LoadAd()
    {
        if (interstitialAd != null)
        {
            interstitialAd.LoadAd();
        }
    }
}
```

## API Reference

### Constructor

#### `new LevelPlayInterstitialAd(string adUnitId)`
Create an interstitial ad object.

**Parameters:**
- `adUnitId`: The ad unit identifier from your LevelPlay dashboard

**Returns:** `LevelPlayInterstitialAd` object

**Usage:**
```csharp
LevelPlayInterstitialAd interstitialAd = new LevelPlayInterstitialAd("your_ad_unit_id");
```

**Important:** Call this only after `LevelPlay.Init()` has completed successfully (in the `OnInitSuccess` callback).

#### `new LevelPlayInterstitialAd(string adUnitId, Config config)`
Create an interstitial ad object with custom configuration.

**Parameters:**
- `adUnitId`: The ad unit identifier
- `config`: Optional configuration object

**Usage:**
```csharp
var configBuilder = new LevelPlayInterstitialAd.Config.Builder();
configBuilder.SetBidFloor(1.0); // Minimum bid price in USD
var config = configBuilder.Build();
LevelPlayInterstitialAd interstitialAd = new LevelPlayInterstitialAd("your_ad_unit_id", config);
```

### Configuration Builder

#### `LevelPlayInterstitialAd.Config.Builder()`
Create a configuration builder for interstitial ads.

**Returns:** `LevelPlayInterstitialAd.Config.Builder` object

**Usage:**
```csharp
var configBuilder = new LevelPlayInterstitialAd.Config.Builder();
```

**Set minimum bid price:**
```csharp
configBuilder.SetBidFloor(1.0); // Minimum bid price in USD ($1.00 CPM)
```

**When to use:** Set a minimum bid price in USD for ad requests.

#### `Build()`
Build the configuration object.

**Returns:** `LevelPlayInterstitialAd.Config` object

**Usage:**
```csharp
var config = configBuilder.Build();
```

### Core Methods

#### `LoadAd()`
Load an interstitial ad.

**Usage:**
```csharp
interstitialAd.LoadAd();
```

**When to call:**
- After SDK initialization completes
- After showing an ad (to load the next one)
- After a load or display failure

#### `ShowAd()`
Show the interstitial ad without a placement name.

**Usage:**
```csharp
if (interstitialAd.IsAdReady())
{
    interstitialAd.ShowAd();
}
```

**Important:** Always check `IsAdReady()` before calling `ShowAd()`.

#### `ShowAd(placementName: string)`
Show the interstitial ad with a named placement for analytics tracking.

**Parameters:**
- `placementName`: Placement name configured in LevelPlay dashboard

**Usage:**
```csharp
interstitialAd.ShowAd(placementName: "level_complete");
```

**When to use:** When tracking multiple placements within the same ad unit for analytics.

#### `IsAdReady()`
Check if an interstitial ad is loaded and ready to show.

**Returns:** `bool` - true if ready, false otherwise

**Usage:**
```csharp
if (interstitialAd.IsAdReady())
{
    // Ad is ready to show
    interstitialAd.ShowAd();
}
else
{
    // Ad not ready, load it
    interstitialAd.LoadAd();
}
```

#### `LevelPlayInterstitialAd.IsPlacementCapped(string placementName)` (Static)
Check if a placement has reached its capping limit.

**Parameters:**
- `placementName`: The placement name to check

**Returns:** `bool` - true if capped, false otherwise

**Usage:**
```csharp
if (LevelPlayInterstitialAd.IsPlacementCapped("level_complete"))
{
    Debug.Log("Level complete placement is capped");
}
```

**When to use:** Before showing an ad to check if the placement frequency cap is reached.

### Events

All events are properties of the `LevelPlayInterstitialAd` object.

**Threading:** All ad callbacks run on the Unity main thread, so you can safely call Unity APIs (update UI, access GameObjects, etc.) directly in these callbacks. This is different from `LevelPlay.OnImpressionDataReady` which runs on a background thread.

#### `OnAdLoaded`
Fired when an interstitial ad is successfully loaded.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
interstitialAd.OnAdLoaded += (adInfo) =>
{
    Debug.Log("Interstitial ad loaded");
};
```

#### `OnAdLoadFailed`
Fired when an interstitial ad fails to load.

**Signature:** `event Action<LevelPlayAdError>`

**Usage:**
```csharp
interstitialAd.OnAdLoadFailed += (error) =>
{
    Debug.LogWarning($"Load failed: {error.ErrorMessage}");
};
```

**Best practice:** Implement retry logic with exponential backoff.

#### `OnAdDisplayed`
Fired when an interstitial ad is displayed on screen.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
interstitialAd.OnAdDisplayed += (adInfo) =>
{
    Debug.Log("Interstitial ad displayed");
    // Optional: Track analytics
};
```

#### `OnAdDisplayFailed`
Fired when an interstitial ad fails to display.

**Signature:** `event Action<LevelPlayAdInfo, LevelPlayAdError>`

**Usage:**
```csharp
interstitialAd.OnAdDisplayFailed += (adInfo, error) =>
{
    Debug.LogError($"Display failed: {error.ErrorMessage}");
    // Load a new ad
    interstitialAd.LoadAd();
};
```

#### `OnAdClicked`
Fired when the user clicks on the interstitial ad.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
interstitialAd.OnAdClicked += (adInfo) =>
{
    Debug.Log("Interstitial ad clicked");
    // Optional analytics tracking
};
```

#### `OnAdClosed`
Fired when the interstitial ad is closed.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
interstitialAd.OnAdClosed += (adInfo) =>
{
    Debug.Log("Interstitial ad closed");
    // Resume game, load next ad
    interstitialAd.LoadAd();
};
```

**Best practice:** Load the next ad immediately in this callback.

#### `OnAdInfoChanged`
Fired when ad information changes, such as when a new winning ad becomes available after auction.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
interstitialAd.OnAdInfoChanged += (adInfo) =>
{
    Debug.Log($"Ad info changed - New ad from: {adInfo.AdNetwork}");
    Debug.Log($"Estimated revenue: ${adInfo.Revenue}");
};
```

**Important:** This event indicates that a new ad with potentially different revenue has become available. The SDK automatically selects the winning ad from the auction when you call `ShowAd()`, but this event lets you know when the ad info has been updated. This is particularly important for revenue optimization as it signals when a new auction winner has loaded.

**When it fires:**
- After a new ad loads with different properties than the previous one
- When the waterfall selects a different ad network
- When ad auction results change

**Why it matters:** The updated `LevelPlayAdInfo` contains the latest revenue estimates and network information, which directly impacts your monetization. Always use the most recent `adInfo` when logging or analyzing ad performance.

**If you're using ILRD** (`references/ilrd-api.md`): the `LevelPlayImpressionData` you receive in `OnImpressionDataReady` already contains the final revenue value, so `OnAdInfoChanged` is mostly useful for in-Editor debugging of the waterfall. Most publishers can leave it as a logging hook.

## Data Types

### LevelPlayAdInfo

Contains information about the ad.

**Properties:**
- `AdId` (string): Unique identifier for this specific ad instance
- `AdUnitId` (string): The ad unit identifier
- `AdUnitName` (string): The ad unit name
- `AdSize` (LevelPlayAdSize): The ad size (may be null; banner-relevant)
- `AdFormat` (string): The ad format (e.g., "INTERSTITIAL")
- `PlacementName` (string): Placement name where ad was shown
- `AuctionId` (string): Unique auction identifier
- `CreativeId` (string): Creative identifier
- `Country` (string): User's country code (ISO 3166-1)
- `Ab` (string): A/B test segment identifier
- `SegmentName` (string): User segment name
- `AdNetwork` (string): Ad network that served the ad
- `InstanceName` (string): Ad network instance name
- `InstanceId` (string): Ad network instance identifier
- `Revenue` (double?): Estimated revenue in USD (nullable - check for null)
- `Precision` (string): Revenue precision level
- `EncryptedCPM` (string): Encrypted CPM value

### LevelPlayAdError

Contains error information when ad operations fail.

**Properties:**
- `ErrorCode` (int): Numeric error code
- `ErrorMessage` (string): Human-readable error description
- `AdUnitId` (string): The ad unit identifier where the error occurred
- `AdId` (string): Unique identifier for the specific ad instance

## Best Practices

### Loading Strategy

**Keep an interstitial ready for opportunistic moments:**
```csharp
// Load after init
void OnInitSuccess(LevelPlayConfiguration config)
{
    interstitialAd = new LevelPlayInterstitialAd(adUnitId);
    interstitialAd.OnAdLoaded += OnAdLoaded;
    interstitialAd.OnAdClosed += OnAdClosed;
    interstitialAd.LoadAd();
}

// Reload after showing
void OnAdClosed(LevelPlayAdInfo adInfo)
{
    interstitialAd.LoadAd();
}
```

### Placement Strategy

**Show at natural transition points:**
- ✅ Level complete screen
- ✅ Game over screen
- ✅ Returning to main menu
- ✅ Between sessions
- ❌ During active gameplay
- ❌ Mid-level or mid-action

### Frequency Capping

**Implement time-based capping to avoid annoying users:**
```csharp
private float minTimeBetweenAds = 300f; // 5 minutes
private float lastAdTime = -999f;

public void TryShowInterstitial()
{
    if (Time.time - lastAdTime < minTimeBetweenAds)
    {
        Debug.Log("Too soon to show another ad");
        return;
    }

    if (interstitialAd.IsAdReady())
    {
        interstitialAd.ShowAd();
    }
}

private void OnAdDisplayed(LevelPlayAdInfo adInfo)
{
    lastAdTime = Time.time;
}
```

**Recommended frequencies:**
- **Revenue-focused**: 3-5 minutes
- **Balanced**: 5-7 minutes
- **UX-focused**: 7-10 minutes or never

### User Flow

**Never block user progress waiting for ads:**
```csharp
// ✅ CORRECT - Don't block flow
public void OnLevelComplete()
{
    // Continue to next level regardless of ad
    LoadNextLevel();

    // Try to show ad, but don't wait for it
    TryShowInterstitial();
}

// ❌ WRONG - Blocking user
public void OnLevelComplete()
{
    if (interstitialAd.IsAdReady())
    {
        interstitialAd.ShowAd();
        // Don't wait here to load next level!
    }
}
```

### Error Handling

**Handle ad load failures:**
```csharp
private void OnAdLoadFailed(LevelPlayAdError error)
{
    Debug.LogWarning($"Ad load failed: {error.ErrorMessage}");

    // Retry loading after a delay
    Invoke(nameof(RetryLoad), 30f);
}

private void RetryLoad()
{
    interstitialAd.LoadAd();
}
```

### Memory Management

**Always unsubscribe from events:**
```csharp
void OnDestroy()
{
    if (interstitialAd != null)
    {
        interstitialAd.OnAdLoaded -= OnAdLoaded;
        interstitialAd.OnAdLoadFailed -= OnAdLoadFailed;
        interstitialAd.OnAdDisplayed -= OnAdDisplayed;
        interstitialAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        interstitialAd.OnAdClicked -= OnAdClicked;
        interstitialAd.OnAdClosed -= OnAdClosed;
        interstitialAd.OnAdInfoChanged -= OnAdInfoChanged;
    }
}
```

## Common Issues

### Issue: Error 1037 (Load during load)

**Cause:** Calling `LoadAd()` repeatedly in `Update()` or other high-frequency loops before the previous load completes.

**Common mistake:**
```csharp
void Update()
{
    // WRONG - This will cause error 1037!
    if (!interstitialAd.IsAdReady())
    {
        interstitialAd.LoadAd();
    }
}
```

**Why this fails:** If the ad is still loading, calling `LoadAd()` again triggers error 1037: "Cannot load an interstitial while another interstitial is loading."

**Solution:** Track load state manually and only call `LoadAd()` once:
```csharp
private bool isLoadingAd = false;

void Start()
{
    LoadInterstitialAd();
}

void LoadInterstitialAd()
{
    if (isLoadingAd) return; // Prevent duplicate loads
    
    isLoadingAd = true;
    interstitialAd.LoadAd();
}

void OnAdLoaded(LevelPlayAdInfo adInfo)
{
    isLoadingAd = false;
}

void OnAdLoadFailed(LevelPlayAdError error)
{
    isLoadingAd = false;
    // Optionally retry after delay
}
```

### Issue: Interstitial showing too frequently

**Solution:** Implement frequency capping based on time or session count. Track when the last ad was shown and enforce a minimum interval.

### Issue: User flow blocked by ad loading

**Solution:** Never wait for ads to load or show before progressing the user. Load ads proactively and show them opportunistically.

### Issue: Ad not loading

**Possible causes:**
- SDK not initialized before creating ad object
- No internet connection
- Ad inventory not available
- Incorrect ad unit ID

**Solutions:**
- Create ad object only after `OnInitSuccess` callback
- Check network connectivity
- Verify ad unit ID in LevelPlay dashboard
- Test on real device with good connection

### Issue: Ad loading but not showing

**Possible causes:**
- Not checking `IsAdReady()` before calling `ShowAd()`
- Ad expired (loaded too long ago)

**Solutions:**
- Always check `IsAdReady()` before showing
- Load ads close to when they'll be shown (within a few minutes)

## Testing Checklist

- [ ] Ad loads successfully after SDK initialization
- [ ] `IsAdReady()` returns true when ad is loaded
- [ ] Ad displays correctly when `ShowAd()` is called
- [ ] Ad shows at appropriate transition points only
- [ ] Frequency capping works as expected
- [ ] User can progress regardless of ad status
- [ ] Ad reloads automatically after being shown
- [ ] Retry logic works when ad fails to load
- [ ] Multiple placements track correctly (if using)
- [ ] Placement capping works (if configured)
- [ ] Memory leaks prevented (events unsubscribed)
- [ ] Tested on multiple devices and network conditions
- [ ] Ad doesn't interrupt active gameplay
