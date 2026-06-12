# Rewarded Ads API Reference

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

Rewarded ads are video ads that users choose to watch in exchange for in-app rewards. They're the highest-earning ad format and provide excellent user experience when implemented correctly.

## Key Characteristics

- **User-initiated**: Users explicitly choose to watch
- **Win-win**: Users get valuable rewards, developers get revenue
- **Best for**: Extra lives, hints, currency, power-ups, skipping wait times

## Implementation Pattern

### Basic Rewarded Ad Implementation

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class RewardedAdManager : MonoBehaviour
{
    private LevelPlayRewardedAd rewardedAd;
    private string adUnitId = "YOUR_REWARDED_AD_UNIT_ID";

    void Start()
    {
        // Create the rewarded ad object using constructor
        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        // Register event listeners
        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdDisplayed += OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;
        rewardedAd.OnAdClicked += OnAdClicked;
        rewardedAd.OnAdInfoChanged += OnAdInfoChanged;   // Fires when the loaded ad swaps to a higher-paying network

        // Load the ad
        LoadAd();
    }

    void OnDestroy()
    {
        // Unregister event listeners
        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoaded -= OnAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
            rewardedAd.OnAdDisplayed -= OnAdDisplayed;
            rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            rewardedAd.OnAdRewarded -= OnAdRewarded;
            rewardedAd.OnAdClosed -= OnAdClosed;
            rewardedAd.OnAdClicked -= OnAdClicked;
            rewardedAd.OnAdInfoChanged -= OnAdInfoChanged;
        }
    }

    public void LoadAd()
    {
        Debug.Log("Loading rewarded ad...");
        rewardedAd.LoadAd();
    }

    public void ShowAd()
    {
        if (rewardedAd.IsAdReady())
        {
            Debug.Log("Showing rewarded ad");
            rewardedAd.ShowAd();
        }
        else
        {
            Debug.LogWarning("Rewarded ad is not ready yet");
        }
    }

    // Event Callbacks
    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad loaded successfully");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Rewarded ad failed to load: {error.ErrorMessage}");
        // Retry loading after a delay
        Invoke(nameof(LoadAd), 30f);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad displayed");
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Rewarded ad failed to display: {error.ErrorMessage}");
        // Load a new ad
        LoadAd();
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"User earned reward: {reward.Amount} {reward.Name}");
        // Grant the reward to the user
        GrantReward(reward);
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad closed");
        // Load the next ad
        LoadAd();
    }

    private void OnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad clicked");
    }

    private void OnAdInfoChanged(LevelPlayAdInfo adInfo)
    {
        // Optional: log or update analytics with the latest revenue estimate
        Debug.Log($"Rewarded ad info changed - network: {adInfo.AdNetwork}, revenue: ${adInfo.Revenue}");
    }

    private void GrantReward(LevelPlayReward reward)
    {
        // Implement your reward logic here
        Debug.Log($"Granting {reward.Amount} {reward.Name} to the user");
    }
}
```

### Advanced: Multiple Rewarded Ad Placements

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;
using System.Collections.Generic;

public class MultiPlacementRewardedAdManager : MonoBehaviour
{
    // Single ad unit ID, multiple placements
    private string adUnitId = "YOUR_REWARDED_AD_UNIT_ID";
    private LevelPlayRewardedAd rewardedAd;

    // Track which placement is currently being shown
    private string currentPlacement;

    void Start()
    {
        // Create single rewarded ad object
        rewardedAd = new LevelPlayRewardedAd(adUnitId);

        // Register callbacks
        rewardedAd.OnAdLoaded += OnAdLoaded;
        rewardedAd.OnAdLoadFailed += OnAdLoadFailed;
        rewardedAd.OnAdRewarded += OnAdRewarded;
        rewardedAd.OnAdClosed += OnAdClosed;
        rewardedAd.OnAdDisplayFailed += OnAdDisplayFailed;

        // Load ad
        rewardedAd.LoadAd();
    }

    void OnDestroy()
    {
        if (rewardedAd != null)
        {
            rewardedAd.OnAdLoaded -= OnAdLoaded;
            rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
            rewardedAd.OnAdRewarded -= OnAdRewarded;
            rewardedAd.OnAdClosed -= OnAdClosed;
            rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        }
    }

    // Public methods for different placements
    public void ShowAdForHints()
    {
        ShowAdWithPlacement("hints");
    }

    public void ShowAdForExtraLives()
    {
        ShowAdWithPlacement("extra_lives");
    }

    public void ShowAdForCoins()
    {
        ShowAdWithPlacement("bonus_coins");
    }

    private void ShowAdWithPlacement(string placementName)
    {
        if (rewardedAd.IsAdReady())
        {
            // Check if placement is capped
            if (LevelPlayRewardedAd.IsPlacementCapped(placementName))
            {
                Debug.LogWarning($"Placement '{placementName}' is capped");
                return;
            }

            currentPlacement = placementName;
            Debug.Log($"Showing rewarded ad with placement: {placementName}");
            rewardedAd.ShowAd(placementName: placementName);
        }
        else
        {
            Debug.LogWarning("Rewarded ad is not ready");
        }
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad loaded");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Rewarded ad load failed: {error.ErrorMessage}");
        Invoke(nameof(LoadAd), 30f);
    }

    private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
    {
        Debug.Log($"User rewarded: {reward.Amount} {reward.Name} for placement: {currentPlacement}");

        // Grant placement-specific rewards
        switch (currentPlacement)
        {
            case "hints":
                GrantHints(reward);
                break;
            case "extra_lives":
                GrantExtraLives(reward);
                break;
            case "bonus_coins":
                GrantCoins(reward);
                break;
        }
    }

    private void OnAdClosed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Rewarded ad closed");
        currentPlacement = null;
        LoadAd();
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Rewarded ad display failed: {error.ErrorMessage}");
        currentPlacement = null;
        LoadAd();
    }

    private void LoadAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.LoadAd();
        }
    }

    // Reward granting methods
    private void GrantHints(LevelPlayReward reward)
    {
        Debug.Log("Granting hints to player");
        // Your hint logic here
    }

    private void GrantExtraLives(LevelPlayReward reward)
    {
        Debug.Log("Granting extra lives to player");
        // Your lives logic here
    }

    private void GrantCoins(LevelPlayReward reward)
    {
        Debug.Log("Granting bonus coins to player");
        // Your coins logic here
    }
}
```

## API Reference

### Constructor

#### `new LevelPlayRewardedAd(string adUnitId)`
Create a rewarded ad object.

**Parameters:**
- `adUnitId`: The ad unit identifier from your LevelPlay dashboard

**Returns:** `LevelPlayRewardedAd` object

**Usage:**
```csharp
LevelPlayRewardedAd rewardedAd = new LevelPlayRewardedAd("your_ad_unit_id");
```

**Important:** Call this only after `LevelPlay.Init()` has completed successfully (in the `OnInitSuccess` callback).

#### `new LevelPlayRewardedAd(string adUnitId, Config config)`
Create a rewarded ad object with custom configuration.

**Parameters:**
- `adUnitId`: The ad unit identifier
- `config`: Optional configuration object

**Usage:**
```csharp
var configBuilder = new LevelPlayRewardedAd.Config.Builder();
configBuilder.SetBidFloor(1.0); // Minimum bid price in USD
var config = configBuilder.Build();
LevelPlayRewardedAd rewardedAd = new LevelPlayRewardedAd("your_ad_unit_id", config);
```

### Configuration Builder

#### `LevelPlayRewardedAd.Config.Builder()`
Create a configuration builder for rewarded ads.

**Returns:** `LevelPlayRewardedAd.Config.Builder` object

**Usage:**
```csharp
var configBuilder = new LevelPlayRewardedAd.Config.Builder();
```

**Set minimum bid price:**
```csharp
configBuilder.SetBidFloor(1.0); // Minimum bid price in USD ($1.00 CPM)
```

**When to use:** Set a minimum bid price in USD for ad requests.

#### `Build()`
Build the configuration object.

**Returns:** `LevelPlayRewardedAd.Config` object

**Usage:**
```csharp
var config = configBuilder.Build();
```

### Core Methods

#### `LoadAd()`
Load a rewarded ad.

**Usage:**
```csharp
rewardedAd.LoadAd();
```

**When to call:**
- After SDK initialization completes
- After showing an ad (to load the next one)
- After a load or display failure

#### `ShowAd()`
Show the rewarded ad without a placement name.

**Usage:**
```csharp
if (rewardedAd.IsAdReady())
{
    rewardedAd.ShowAd();
}
```

**Important:** Always check `IsAdReady()` before calling `ShowAd()`.

#### `ShowAd(placementName: string)`
Show the rewarded ad with a named placement for analytics tracking.

**Parameters:**
- `placementName`: Placement name configured in LevelPlay dashboard

**Usage:**
```csharp
rewardedAd.ShowAd(placementName: "extra_lives");
```

**When to use:** When tracking multiple placements within the same ad unit for analytics.

#### `GetReward(string placementName = null)`
Get the reward configuration for a placement.

**Parameters:**
- `placementName` (optional): The placement name to query. If null, returns default reward.

**Returns:** `LevelPlayReward` object with Name and Amount

**Usage:**
```csharp
// Get default reward
LevelPlayReward defaultReward = rewardedAd.GetReward();
Debug.Log($"Default reward: {defaultReward.Amount} {defaultReward.Name}");

// Get placement-specific reward
LevelPlayReward placementReward = rewardedAd.GetReward("extra_lives");
Debug.Log($"Extra lives reward: {placementReward.Amount} {placementReward.Name}");
```

**When to use:** To display reward information to users before they watch the ad (e.g., "Watch ad to earn 5 coins").

#### `IsAdReady()`
Check if a rewarded ad is loaded and ready to show.

**Returns:** `bool` - true if ready, false otherwise

**Usage:**
```csharp
if (rewardedAd.IsAdReady())
{
    // Ad is ready to show
    rewardedAd.ShowAd();
}
else
{
    // Ad not ready, maybe disable the button
}
```

**Best practice:** Check this before showing ad UI buttons to users.

#### `LevelPlayRewardedAd.IsPlacementCapped(string placementName)` (Static)
Check if a placement has reached its capping limit.

**Parameters:**
- `placementName`: The placement name to check

**Returns:** `bool` - true if capped, false otherwise

**Usage:**
```csharp
if (LevelPlayRewardedAd.IsPlacementCapped("extra_lives"))
{
    Debug.Log("Extra lives placement is capped");
}
```

**When to use:** Before showing an ad to check if the placement frequency cap is reached.

### Events

All events are properties of the `LevelPlayRewardedAd` object.

**Threading:** All ad callbacks run on the Unity main thread, so you can safely call Unity APIs (update UI, access GameObjects, etc.) directly in these callbacks. This is different from `LevelPlay.OnImpressionDataReady` which runs on a background thread.

#### `OnAdLoaded`
Fired when a rewarded ad is successfully loaded.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
rewardedAd.OnAdLoaded += (adInfo) =>
{
    Debug.Log("Rewarded ad loaded");
    // Enable UI button, etc.
};
```

#### `OnAdLoadFailed`
Fired when a rewarded ad fails to load.

**Signature:** `event Action<LevelPlayAdError>`

**Usage:**
```csharp
rewardedAd.OnAdLoadFailed += (error) =>
{
    Debug.LogWarning($"Load failed: {error.ErrorMessage}");
    // Retry loading
};
```

**Best practice:** Implement retry logic with exponential backoff.

#### `OnAdDisplayed`
Fired when a rewarded ad is displayed on screen.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
rewardedAd.OnAdDisplayed += (adInfo) =>
{
    Debug.Log("Rewarded ad displayed");
    // Optional: Pause game
};
```

#### `OnAdDisplayFailed`
Fired when a rewarded ad fails to display.

**Signature:** `event Action<LevelPlayAdInfo, LevelPlayAdError>`

**Usage:**
```csharp
rewardedAd.OnAdDisplayFailed += (adInfo, error) =>
{
    Debug.LogError($"Display failed: {error.ErrorMessage}");
    // Load a new ad
    rewardedAd.LoadAd();
};
```

#### `OnAdRewarded`
Fired when the user has earned the reward.

**Signature:** `event Action<LevelPlayAdInfo, LevelPlayReward>`

**Parameters:**
- `adInfo`: Information about the ad
- `reward`: The reward object containing `Name` and `Amount`

**Usage:**
```csharp
rewardedAd.OnAdRewarded += (adInfo, reward) =>
{
    Debug.Log($"User earned: {reward.Amount} {reward.Name}");
    // Grant the reward to the user
    GrantReward(reward);
};
```

**Critical:** This is where you grant rewards to the user. Don't wait for `OnAdClosed` - users may close before the ad finishes.

#### `OnAdClosed`
Fired when the rewarded ad is closed.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
rewardedAd.OnAdClosed += (adInfo) =>
{
    Debug.Log("Rewarded ad closed");
    // Resume game, load next ad
    rewardedAd.LoadAd();
};
```

**Best practice:** Load the next ad immediately in this callback.

#### `OnAdClicked`
Fired when the user clicks on the rewarded ad.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
rewardedAd.OnAdClicked += (adInfo) =>
{
    Debug.Log("Rewarded ad clicked");
    // Optional analytics tracking
};
```

#### `OnAdInfoChanged`
Fired when ad information changes, such as when a new highest-paying ad becomes available.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
rewardedAd.OnAdInfoChanged += (adInfo) =>
{
    Debug.Log($"Ad info changed - New ad from: {adInfo.AdNetwork}");
    Debug.Log($"Estimated revenue: ${adInfo.Revenue}");
};
```

**Important:** This event indicates that a new ad with potentially different revenue has become available. The SDK automatically selects the highest-paying ad when you call `ShowAd()`, but this event lets you know when the ad info has been updated. This is particularly important for revenue optimization as it signals when a better-paying ad has loaded.

**When it fires:**
- After a new ad loads with different properties than the previous one
- When the waterfall selects a different ad network
- When ad auction results change

**Why it matters:** The updated `LevelPlayAdInfo` contains the latest revenue estimates and network information, which directly impacts your monetization. Always use the most recent `adInfo` when logging or analyzing ad performance.

**If you're using ILRD** (`references/ilrd-api.md`): the `LevelPlayImpressionData` you receive in `OnImpressionDataReady` already contains the final revenue value, so `OnAdInfoChanged` is mostly useful for in-Editor debugging of the waterfall. Most publishers can leave it as a logging hook.

## Data Types

### LevelPlayReward

Represents the reward earned by the user.

**Properties:**
- `Name` (string): The reward name (e.g., "coins", "life")
- `Amount` (int): The reward amount (e.g., 10, 1)

**Usage:**
```csharp
private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
{
    Debug.Log($"Reward: {reward.Amount} {reward.Name}");

    if (reward.Name == "coins")
    {
        playerCoins += reward.Amount;
    }
}
```

### LevelPlayAdInfo

Contains information about the ad.

**Properties:**
- `AdId` (string): Unique identifier for this specific ad instance
- `AdUnitId` (string): The ad unit identifier
- `AdUnitName` (string): The ad unit name
- `AdSize` (LevelPlayAdSize): The ad size (may be null; banner-relevant)
- `AdFormat` (string): The ad format (e.g., "REWARDED")
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

**Always keep a rewarded ad ready:**
```csharp
// Load immediately after init
void OnInitSuccess(LevelPlayConfiguration config)
{
    rewardedAd = new LevelPlayRewardedAd(adUnitId);
    rewardedAd.OnAdLoaded += OnAdLoaded;
    rewardedAd.OnAdClosed += OnAdClosed;
    rewardedAd.LoadAd();
}

// Reload immediately after showing
void OnAdClosed(LevelPlayAdInfo adInfo)
{
    rewardedAd.LoadAd();
}
```

### UI Integration

**Show/hide buttons based on ad availability:**
```csharp
public Button watchAdButton;

void Update()
{
    // Enable button only if ad is ready
    watchAdButton.interactable = rewardedAd != null && rewardedAd.IsAdReady();
}

void OnAdLoaded(LevelPlayAdInfo adInfo)
{
    watchAdButton.interactable = true;
}

void OnAdLoadFailed(LevelPlayAdError error)
{
    watchAdButton.interactable = false;
}
```

### Reward Granting

**Grant rewards in `OnAdRewarded`, not `OnAdClosed`:**
```csharp
// ✅ CORRECT
private void OnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward)
{
    GrantReward(reward); // Grant immediately
}

// ❌ WRONG - user may close ad early
private void OnAdClosed(LevelPlayAdInfo adInfo)
{
    // Don't grant reward here!
}
```

**Note:** `OnAdRewarded` and `OnAdClosed` are asynchronous — `OnAdRewarded` may fire after `OnAdClosed`. Do not write logic in `OnAdClosed` that assumes the reward has already been granted.

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
    rewardedAd.LoadAd();
}
```

### Multiple Placements

**Use placements to track different reward contexts:**
- Create placements in LevelPlay dashboard (e.g., "hints", "extra_lives")
- Use `ShowAd(placementName: "name")` to track separately
- Check `IsPlacementCapped()` before showing
- Handle rewards differently based on placement in `OnAdRewarded`

### Memory Management

**Always unsubscribe from events:**
```csharp
void OnDestroy()
{
    if (rewardedAd != null)
    {
        rewardedAd.OnAdLoaded -= OnAdLoaded;
        rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
        rewardedAd.OnAdDisplayed -= OnAdDisplayed;
        rewardedAd.OnAdDisplayFailed -= OnAdDisplayFailed;
        rewardedAd.OnAdRewarded -= OnAdRewarded;
        rewardedAd.OnAdClosed -= OnAdClosed;
        rewardedAd.OnAdClicked -= OnAdClicked;
        rewardedAd.OnAdInfoChanged -= OnAdInfoChanged;
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
    if (!rewardedAd.IsAdReady())
    {
        rewardedAd.LoadAd();
    }
}
```

**Why this fails:** If the ad is still loading, calling `LoadAd()` again triggers error 1037: "Cannot load while another load is in progress."

**Solution:** Track load state manually and only call `LoadAd()` once:
```csharp
private bool isLoadingAd = false;

void Start()
{
    LoadRewardedAd();
}

void LoadRewardedAd()
{
    if (isLoadingAd) return; // Prevent duplicate loads
    
    isLoadingAd = true;
    rewardedAd.LoadAd();
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

### Issue: Reward not granted to user

**Possible causes:**
- Granting reward in `OnAdClosed` instead of `OnAdRewarded`
- User closes ad before completion
- Reward logic has bugs

**Solution:**
Always grant rewards in `OnAdRewarded`, which fires when the user has earned the reward (typically after watching enough of the ad).

### Issue: Ad not loading

**Possible causes:**
- SDK not initialized before creating ad object
- No internet connection
- Ad inventory not available in test region
- Incorrect ad unit ID

**Solutions:**
- Create ad object only after `OnInitSuccess` callback
- Check network connectivity
- Verify ad unit ID in LevelPlay dashboard
- Test on real device with good connection

### Issue: Button always disabled

**Possible causes:**
- Not checking `IsAdReady()` correctly
- Ad failed to load and no retry logic
- Event subscriptions not working

**Solutions:**
- Implement `OnAdLoaded` callback to enable button
- Add retry logic in `OnAdLoadFailed`
- Debug log all callbacks to verify they fire

### Issue: Multiple reward grants

**Possible causes:**
- `OnAdRewarded` callback subscribed multiple times
- Not tracking if reward already granted

**Solutions:**
- Unsubscribe in `OnDestroy()`
- Only subscribe once in initialization
- Use flag to track if reward granted for this ad

## Testing Checklist

- [ ] Ad loads successfully after SDK initialization
- [ ] `IsAdReady()` returns true when ad is loaded
- [ ] Ad displays correctly when `ShowAd()` is called
- [ ] `OnAdRewarded` fires and reward is granted
- [ ] UI button enables when ad is ready
- [ ] UI button disables when ad is not ready
- [ ] Ad reloads automatically after being shown
- [ ] Retry logic works when ad fails to load
- [ ] Multiple placements track correctly (if using)
- [ ] Placement capping works (if configured)
- [ ] Memory leaks prevented (events unsubscribed)
- [ ] Tested on multiple devices and network conditions
