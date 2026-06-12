# Banner Ads API Reference

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

Banner ads are rectangular ads that occupy a portion of the screen. They can be displayed at the top or bottom of the screen and remain visible while users interact with your app.

## Key Characteristics

- **Persistent**: Remain on screen until explicitly destroyed
- **Always visible**: Can distract from gameplay if poorly positioned
- **Best for**: Menu screens, waiting periods, low-attention moments

## Implementation Pattern

### Basic Banner Implementation

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class BannerAdManager : MonoBehaviour
{
    private LevelPlayBannerAd bannerAd;
    private string adUnitId = "YOUR_BANNER_AD_UNIT_ID";

    void Start()
    {
        // Create the banner ad object using constructor
        bannerAd = new LevelPlayBannerAd(adUnitId);

        // Register event listeners
        bannerAd.OnAdLoaded += OnAdLoaded;
        bannerAd.OnAdLoadFailed += OnAdLoadFailed;
        bannerAd.OnAdDisplayed += OnAdDisplayed;
        bannerAd.OnAdDisplayFailed += OnAdDisplayFailed;
        bannerAd.OnAdClicked += OnAdClicked;
        bannerAd.OnAdExpanded += OnAdExpanded;
        bannerAd.OnAdCollapsed += OnAdCollapsed;
        bannerAd.OnAdLeftApplication += OnAdLeftApplication;

        // Load and show banner
        LoadBanner();
    }

    void OnDestroy()
    {
        // Unregister event listeners
        if (bannerAd != null)
        {
            bannerAd.OnAdLoaded -= OnAdLoaded;
            bannerAd.OnAdLoadFailed -= OnAdLoadFailed;
            bannerAd.OnAdDisplayed -= OnAdDisplayed;
            bannerAd.OnAdDisplayFailed -= OnAdDisplayFailed;
            bannerAd.OnAdClicked -= OnAdClicked;
            bannerAd.OnAdExpanded -= OnAdExpanded;
            bannerAd.OnAdCollapsed -= OnAdCollapsed;
            bannerAd.OnAdLeftApplication -= OnAdLeftApplication;
        }

        // Destroy banner
        DestroyBanner();
    }

    public void LoadBanner()
    {
        Debug.Log("Loading banner ad...");
        bannerAd.LoadAd();
    }

    public void ShowBanner()
    {
        Debug.Log("Showing banner ad");
        bannerAd.ShowAd();
    }

    public void HideBanner()
    {
        Debug.Log("Hiding banner ad");
        bannerAd.HideAd();
    }

    public void DestroyBanner()
    {
        if (bannerAd != null)
        {
            Debug.Log("Destroying banner ad");
            bannerAd.DestroyAd();
            bannerAd = null;
        }
    }

    // Event Callbacks
    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner ad loaded");
        // Banner is ready, can call ShowAd() if needed
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Banner ad load failed: {error.ErrorMessage}");
        // Retry loading after delay
        Invoke(nameof(LoadBanner), 60f);
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner ad displayed");
    }

    private void OnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error)
    {
        Debug.LogError($"Banner ad display failed: {error.ErrorMessage}");
        // Retry loading
        Invoke(nameof(LoadBanner), 60f);
    }

    private void OnAdClicked(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner ad clicked");
    }

    private void OnAdExpanded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner ad expanded");
        // Optionally pause game if banner expands to full screen
    }

    private void OnAdCollapsed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner ad collapsed");
        // Resume game if paused
    }

    private void OnAdLeftApplication(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner ad left application");
    }
}
```

### Banner with Custom Configuration

Use the Config Builder pattern to customize banner size, position, and behavior:

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class CustomBannerAdManager : MonoBehaviour
{
    private LevelPlayBannerAd bannerAd;
    private string adUnitId = "YOUR_BANNER_AD_UNIT_ID";

    void Start()
    {
        // Create config using Builder pattern
        var configBuilder = new LevelPlayBannerAd.Config.Builder();

        // Set banner size (UPPERCASE!)
        configBuilder.SetSize(LevelPlayAdSize.LARGE); // 320x90

        // Set position on screen
        configBuilder.SetPosition(LevelPlayBannerPosition.BottomCenter);

        // Auto-display when loaded
        configBuilder.SetDisplayOnLoad(true);

        // Respect safe area (Android only)
        configBuilder.SetRespectSafeArea(true);

        // Build the config
        var bannerConfig = configBuilder.Build();

        // Create banner with config
        bannerAd = new LevelPlayBannerAd(adUnitId, bannerConfig);

        // Register callbacks
        bannerAd.OnAdLoaded += OnAdLoaded;
        bannerAd.OnAdLoadFailed += OnAdLoadFailed;
        bannerAd.OnAdDisplayed += OnAdDisplayed;

        // Load banner
        bannerAd.LoadAd();
    }

    void OnDestroy()
    {
        if (bannerAd != null)
        {
            bannerAd.OnAdLoaded -= OnAdLoaded;
            bannerAd.OnAdLoadFailed -= OnAdLoadFailed;
            bannerAd.OnAdDisplayed -= OnAdDisplayed;
            bannerAd.DestroyAd();
        }
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Custom banner loaded");
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Banner load failed: {error.ErrorMessage}");
    }

    private void OnAdDisplayed(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Custom banner displayed");
    }
}
```

### Advanced: Context-Aware Banner Management

Show banners only in appropriate scenes:

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.LevelPlay;

public class ContextAwareBannerManager : MonoBehaviour
{
    private LevelPlayBannerAd bannerAd;
    private string adUnitId = "YOUR_BANNER_AD_UNIT_ID";

    // Scenes where banner should be shown
    private string[] bannerEnabledScenes = { "MainMenu", "LevelSelect", "Shop" };

    private bool isBannerLoaded = false;

    void Start()
    {
        // Make this manager persist across scenes
        DontDestroyOnLoad(gameObject);

        // Create banner ad
        bannerAd = new LevelPlayBannerAd(adUnitId);

        // Register callbacks
        bannerAd.OnAdLoaded += OnAdLoaded;
        bannerAd.OnAdLoadFailed += OnAdLoadFailed;

        // Register for scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Load banner
        bannerAd.LoadAd();

        // Check initial scene
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        // Unregister callbacks
        if (bannerAd != null)
        {
            bannerAd.OnAdLoaded -= OnAdLoaded;
            bannerAd.OnAdLoadFailed -= OnAdLoadFailed;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Destroy banner
        if (bannerAd != null)
        {
            bannerAd.DestroyAd();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (ShouldShowBannerInScene(scene.name))
        {
            ShowBanner();
        }
        else
        {
            HideBanner();
        }
    }

    private bool ShouldShowBannerInScene(string sceneName)
    {
        foreach (string enabledScene in bannerEnabledScenes)
        {
            if (sceneName == enabledScene)
            {
                return true;
            }
        }
        return false;
    }

    private void ShowBanner()
    {
        if (isBannerLoaded && bannerAd != null)
        {
            bannerAd.ShowAd();
        }
    }

    private void HideBanner()
    {
        if (bannerAd != null)
        {
            bannerAd.HideAd();
        }
    }

    private void OnAdLoaded(LevelPlayAdInfo adInfo)
    {
        Debug.Log("Banner ad loaded");
        isBannerLoaded = true;

        // Show banner if we're in an enabled scene
        if (ShouldShowBannerInScene(SceneManager.GetActiveScene().name))
        {
            ShowBanner();
        }
    }

    private void OnAdLoadFailed(LevelPlayAdError error)
    {
        Debug.LogWarning($"Banner ad load failed: {error.ErrorMessage}");
        isBannerLoaded = false;
        Invoke(nameof(LoadBanner), 60f);
    }

    private void LoadBanner()
    {
        if (bannerAd != null)
        {
            isBannerLoaded = false; // Reset flag when loading
            bannerAd.LoadAd();
        }
    }
}
```

## API Reference

### Constructor

#### `new LevelPlayBannerAd(string adUnitId)`
Create a banner ad object with default configuration.

**Parameters:**
- `adUnitId`: The ad unit identifier from your LevelPlay dashboard

**Returns:** `LevelPlayBannerAd` object

**Usage:**
```csharp
LevelPlayBannerAd bannerAd = new LevelPlayBannerAd("your_ad_unit_id");
```

**Default behavior:**
- Size: Standard banner (320x50)
- Position: Bottom center
- DisplayOnLoad: true (banner shows automatically when loaded)

**Important:** Call this only after `LevelPlay.Init()` has completed successfully.

#### `new LevelPlayBannerAd(string adUnitId, Config config)`
Create a banner ad object with custom configuration.

**Parameters:**
- `adUnitId`: The ad unit identifier
- `config`: Configuration object built using Config.Builder()

**Usage:**
```csharp
var configBuilder = new LevelPlayBannerAd.Config.Builder();
configBuilder.SetSize(LevelPlayAdSize.LARGE);
configBuilder.SetPosition(LevelPlayBannerPosition.TopCenter);
var config = configBuilder.Build();

LevelPlayBannerAd bannerAd = new LevelPlayBannerAd("your_ad_unit_id", config);
```

### Configuration Builder

#### `LevelPlayBannerAd.Config.Builder()`
Create a configuration builder for customizing banner behavior.

**Methods:**

**`SetSize(LevelPlayAdSize size)`**
Set the banner size.

**Usage:**
```csharp
configBuilder.SetSize(LevelPlayAdSize.BANNER); // 320x50
configBuilder.SetSize(LevelPlayAdSize.LARGE); // 320x90
configBuilder.SetSize(LevelPlayAdSize.MEDIUM_RECTANGLE); // 300x250
configBuilder.SetSize(LevelPlayAdSize.LEADERBOARD); // 728x90
configBuilder.SetSize(LevelPlayAdSize.CreateAdaptiveAdSize()); // Adaptive
configBuilder.SetSize(LevelPlayAdSize.CreateCustomBannerSize(300, 150)); // Custom size
```

**`SetPosition(LevelPlayBannerPosition position)`**
Set the banner position on screen.

**Usage:**
```csharp
configBuilder.SetPosition(LevelPlayBannerPosition.TopCenter);
configBuilder.SetPosition(LevelPlayBannerPosition.BottomCenter);
// Also: TopLeft, TopRight, BottomLeft, BottomRight
```

**`SetDisplayOnLoad(bool displayOnLoad)`**
Whether to automatically show banner when loaded.

**Usage:**
```csharp
configBuilder.SetDisplayOnLoad(true); // Auto-show (default)
configBuilder.SetDisplayOnLoad(false); // Manual show
```

**`SetRespectSafeArea(bool respectSafeArea)`**
Whether to respect device safe area (Android only).

**Usage:**
```csharp
configBuilder.SetRespectSafeArea(true); // Avoid notches/cutouts
```

**`SetPlacementName(string placementName)`**
Set the placement name for analytics tracking.

**Usage:**
```csharp
configBuilder.SetPlacementName("main_menu_banner");
```

**When to use:** When tracking multiple banner placements for analytics and reporting.

**Set minimum bid price:**
```csharp
configBuilder.SetBidFloor(1.0); // Minimum bid price in USD ($1.00 CPM)
```

**When to use:** Set a minimum bid price in USD for ad requests to ensure revenue floor.

**`Build()`**
Build the configuration object.

**Usage:**
```csharp
var config = configBuilder.Build();
```

### Core Methods

#### `LoadAd()`
Load a banner ad.

**Usage:**
```csharp
bannerAd.LoadAd();
```

**Note:** After loading succeeds, you can call `ShowAd()` to display it (unless `SetDisplayOnLoad(true)` was used).

#### `ShowAd()`
Show the banner ad.

**Usage:**
```csharp
bannerAd.ShowAd();
```

**Note:** Banner must be loaded first.

#### `HideAd()`
Hide the banner ad without destroying it.

**Usage:**
```csharp
bannerAd.HideAd();
```

**When to use:** Temporarily hide banner (e.g., during gameplay) while keeping it loaded for quick re-display.

#### `DestroyAd()`
Destroy the banner ad and free resources.

**Usage:**
```csharp
bannerAd.DestroyAd();
```

**When to use:** When leaving a scene where banner was shown, or when switching to a different banner size.

**Note on IsAdReady():** Unlike Rewarded and Interstitial ads, Banner ads do NOT have an `IsAdReady()` method. Banners are ready to show immediately after `OnAdLoaded` fires. Simply call `ShowAd()` in the `OnAdLoaded` callback or afterward.

#### `PauseAutoRefresh()`
Pause automatic banner refresh.

**Usage:**
```csharp
bannerAd.PauseAutoRefresh();
```

**When to use:** When banner is not visible but still loaded (e.g., during gameplay).

#### `ResumeAutoRefresh()`
Resume automatic banner refresh.

**Usage:**
```csharp
bannerAd.ResumeAutoRefresh();
```

**When to use:** When banner becomes visible again after pausing.

### Banner Sizes

Available banner sizes from `LevelPlayAdSize` (note: **UPPERCASE**):

| Size | Description | Typical Dimensions |
|------|-------------|-------------------|
| `LevelPlayAdSize.BANNER` | Standard banner | 320x50 |
| `LevelPlayAdSize.LARGE` | Large banner | 320x90 |
| `LevelPlayAdSize.MEDIUM_RECTANGLE` | Medium rectangle | 300x250 |
| `LevelPlayAdSize.LEADERBOARD` | Leaderboard banner | 728x90 |
| `LevelPlayAdSize.CreateAdaptiveAdSize()` | Adaptive banner | Adapts to screen width |
| `LevelPlayAdSize.CreateCustomBannerSize(int width, int height)` | Custom size | Custom dimensions |

**Usage:**
```csharp
// Standard banner (most common)
LevelPlayAdSize.BANNER

// Large banner
LevelPlayAdSize.LARGE

// Medium rectangle
LevelPlayAdSize.MEDIUM_RECTANGLE

// Leaderboard
LevelPlayAdSize.LEADERBOARD

// Adaptive banner (method call)
LevelPlayAdSize.CreateAdaptiveAdSize()

// Custom size
LevelPlayAdSize.CreateCustomBannerSize(300, 150)
```

**Recommendation:** Use `BANNER` (320x50) for most cases. Use `CreateAdaptiveAdSize()` for adaptive sizing across devices.

**LevelPlayAdSize Properties (SDK 8.8.0+):**

After setting a banner size, you can query the actual dimensions:

```csharp
LevelPlayAdSize adSize = LevelPlayAdSize.BANNER;
int width = adSize.Width;   // e.g., 320
int height = adSize.Height; // e.g., 50
```

**Properties:**
- `Width` (int): Banner width in pixels
- `Height` (int): Banner height in pixels

**When to use:** When calculating UI layout offsets or positioning other UI elements around the banner.

### Banner Positions

Available positions from `LevelPlayBannerPosition`:

| Position | Description |
|----------|-------------|
| `TopLeft` | Top-left corner |
| `TopCenter` | Top-center |
| `TopRight` | Top-right corner |
| `CenterLeft` | Center-left |
| `Center` | Center |
| `CenterRight` | Center-right |
| `BottomLeft` | Bottom-left corner |
| `BottomCenter` | Bottom-center (most common) |
| `BottomRight` | Bottom-right corner |

**Usage:**
```csharp
LevelPlayBannerPosition.BottomCenter
LevelPlayBannerPosition.TopCenter
LevelPlayBannerPosition.Center
// etc.
```

**Additional:** Custom positioning can be achieved using a Vector2 constructor.

**Recommendation:** `BottomCenter` is less intrusive and more commonly used.

### Events

All events are properties of the `LevelPlayBannerAd` object.

**Threading:** All ad callbacks run on the Unity main thread, so you can safely call Unity APIs (update UI, access GameObjects, etc.) directly in these callbacks. This is different from `LevelPlay.OnImpressionDataReady` which runs on a background thread.

#### `OnAdLoaded`
Fired when a banner ad is loaded.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
bannerAd.OnAdLoaded += (adInfo) =>
{
    Debug.Log("Banner loaded");
    bannerAd.ShowAd();
};
```

#### `OnAdLoadFailed`
Fired when a banner ad fails to load.

**Signature:** `event Action<LevelPlayAdError>`

**Usage:**
```csharp
bannerAd.OnAdLoadFailed += (error) =>
{
    Debug.LogWarning($"Banner load failed: {error.ErrorMessage}");
};
```

#### `OnAdDisplayed`
Fired when banner ad is displayed.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
bannerAd.OnAdDisplayed += (adInfo) =>
{
    Debug.Log("Banner displayed");
};
```

#### `OnAdDisplayFailed`
Fired when banner ad fails to display.

**Signature:** `event Action<LevelPlayAdInfo, LevelPlayAdError>`

**Usage:**
```csharp
bannerAd.OnAdDisplayFailed += (adInfo, error) =>
{
    Debug.LogError($"Banner display failed: {error.ErrorMessage}");
};
```

#### `OnAdClicked`
Fired when user clicks on the banner.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
bannerAd.OnAdClicked += (adInfo) =>
{
    Debug.Log("Banner clicked");
};
```

#### `OnAdExpanded`
Fired when banner ad expands to full screen (e.g., after click).

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
bannerAd.OnAdExpanded += (adInfo) =>
{
    Debug.Log("Banner expanded");
    // Pause game if needed
};
```

#### `OnAdCollapsed`
Fired when expanded banner collapses back.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
bannerAd.OnAdCollapsed += (adInfo) =>
{
    Debug.Log("Banner collapsed");
    // Resume game if paused
};
```

#### `OnAdLeftApplication`
Fired when banner click causes user to leave the application.

**Signature:** `event Action<LevelPlayAdInfo>`

**Usage:**
```csharp
bannerAd.OnAdLeftApplication += (adInfo) =>
{
    Debug.Log("Banner left application");
};
```

## Data Types

### LevelPlayAdInfo

Contains information about the ad.

**Properties:**
- `AdId` (string): Unique identifier for this specific ad instance
- `AdUnitId` (string): The ad unit identifier
- `AdUnitName` (string): The ad unit name
- `AdSize` (LevelPlayAdSize): The ad size (may be null; banner-relevant)
- `AdFormat` (string): The ad format (e.g., "BANNER")
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

### Loading

- Load banners when entering scenes where they'll be displayed
- Destroy banners when leaving those scenes to free memory
- Only one banner per ad unit can be active at a time
- If you need to change size/position, destroy and recreate

### Placement

- **Bottom position** is generally less intrusive than top
- **Avoid during gameplay** - show in menus, waiting screens
- **Consider UI overlap** - ensure banner doesn't cover important UI
- **Test on various screen sizes** - especially important for mobile

### Visibility Management

- Use `HideAd()` / `ShowAd()` for temporary visibility changes
- Use `DestroyAd()` when permanently done with banner (frees memory)
- Hide banners during gameplay for UX-focused strategy
- Show banners persistently for revenue-focused strategy

### Auto-Refresh Management

- Use `PauseAutoRefresh()` when banner is hidden
- Use `ResumeAutoRefresh()` when banner is shown again

**Example:**
```csharp
void OnGameplayStart()
{
    bannerAd.HideAd();
    bannerAd.PauseAutoRefresh();
}

void OnGameplayEnd()
{
    bannerAd.ShowAd();
    bannerAd.ResumeAutoRefresh();
}
```

### Memory Management

- Always call `DestroyAd()` in `OnDestroy()` or when leaving scene
- Don't keep banners loaded in scenes where they're not displayed
- Unsubscribe from events properly

```csharp
void OnDestroy()
{
    if (bannerAd != null)
    {
        bannerAd.DestroyAd();
    }
}
```

### UX Considerations

- Banners can be distracting during gameplay
- Best practice: Show in menus, hide during gameplay
- For revenue-focused: Show persistently but position carefully
- Test with actual users to gauge impact on experience

## Common Issues

### Issue: Repeated LoadAd() calls

**Cause:** Calling `LoadAd()` repeatedly in `Update()` or other high-frequency loops.

**Common mistake:**
```csharp
void Update()
{
    // WRONG - Don't call LoadAd() repeatedly!
    if (!bannerAd.IsAdReady())
    {
        bannerAd.LoadAd();
    }
}
```

**Why to avoid:** While banner ads don't throw errors for repeated loads like interstitial/rewarded ads do, calling `LoadAd()` multiple times per second wastes resources and can cause unexpected behavior.

**Solution:** Call `LoadAd()` once after creating the banner:
```csharp
void Start()
{
    bannerAd = new LevelPlayBannerAd(adUnitId);
    bannerAd.OnAdLoaded += OnAdLoaded;
    bannerAd.OnAdLoadFailed += OnAdLoadFailed;
    bannerAd.LoadAd(); // Call once
}

void OnAdLoadFailed(LevelPlayAdError error)
{
    // Optionally retry after delay
    Invoke(nameof(RetryLoad), 5f);
}

void RetryLoad()
{
    bannerAd.LoadAd();
}
```

**Note:** Banner ads auto-refresh based on platform settings, so you typically only need to call `LoadAd()` once at initialization.

### Issue: Banner overlaps UI elements

**Solutions:**
- Calculate banner height and offset UI elements
- Use `Canvas.offsetMin` or `RectTransform.anchoredPosition` to create spacing
- Test on different screen sizes
- Use `SetRespectSafeArea(true)` to avoid notches

### Issue: Banner not showing

**Possible causes:**
- SDK not initialized before creating banner
- Banner not loaded before calling `ShowAd()`
- Banner is hidden with `HideAd()`
- `SetDisplayOnLoad(false)` used without manual `ShowAd()`

**Solutions:**
- Create banner only after `OnInitSuccess` callback
- Ensure `LoadAd()` is called and `OnAdLoaded` fires before `ShowAd()`
- Check banner visibility state

### Issue: Banner shows in wrong scenes

**Solutions:**
- Implement scene-based visibility management
- Destroy banner when leaving scenes where it shouldn't appear
- Use `SceneManager.sceneLoaded` event for automatic management

### Issue: Wrong banner size

**Possible causes:**
- Using lowercase enum values (e.g., `.Banner` instead of `.BANNER`)
- Not using Config Builder to set size

**Solutions:**
- Always use UPPERCASE: `LevelPlayAdSize.BANNER`, not `.Banner`
- Use Config Builder to explicitly set size:
```csharp
var config = new LevelPlayBannerAd.Config.Builder()
    .SetSize(LevelPlayAdSize.LARGE)
    .Build();
```

## Testing Checklist

- [ ] Banner loads and displays correctly
- [ ] Banner appears in correct position (top/bottom)
- [ ] Banner size is appropriate for screen
- [ ] Banner doesn't overlap important UI
- [ ] Banner hides/shows correctly
- [ ] Banner destroys properly when leaving scene
- [ ] Banner works on different screen sizes/orientations
- [ ] Retry logic works if banner fails to load
- [ ] Auto-refresh pauses/resumes correctly
- [ ] Safe area respected on devices with notches (Android)
