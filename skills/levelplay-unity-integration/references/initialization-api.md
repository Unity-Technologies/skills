# LevelPlay SDK Initialization API

## Contents
- [Overview](#overview)
- [Code Organization Options (Step 7)](#code-organization-options-step-7)
- [Version-Aware ILRD Init Wiring (Step 7)](#version-aware-ilrd-init-wiring-step-7)
- [Basic Initialization](#basic-initialization)
- [Advanced Initialization](#advanced-initialization)
- [API Reference](#api-reference)
- [Initialization Timing](#initialization-timing)
- [Best Practices](#best-practices)
- [Testing](#testing)
- [Migration from IronSource.* APIs](#migration-from-ironsource-apis)
- [Error Code Reference](#error-code-reference)

## Overview

Proper SDK initialization is critical for LevelPlay to function correctly. This reference covers basic initialization, advanced options, and best practices.

## Code Organization Options (Step 7)

Step 7 of SKILL.md asks the user how they want to handle initialization and presents four options. Full
implementation for each option follows. Present the four option labels exactly as written in SKILL.md;
use the code here once the user has chosen.

### Option 1: Creating a New Script

**If you completed ATT in Step 6.5 (iOS):** Use the `LevelPlayInitializer.cs` from `references/ios-setup.md` (Part 3) instead of the template below — it already includes the `IEnumerator Start()` coroutine required for ATT and the `DontDestroyOnLoad` setup. Skip this code block and continue from the ILRD wiring below.

If creating a new script (e.g., `LevelPlayInitializer.cs`), use this complete class:

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class LevelPlayInitializer : MonoBehaviour
{
    [SerializeField] private string appKey;

    void Awake()
    {
        // Persist across scene loads so ads stay initialized
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Register initialization callbacks
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        // Initialize the SDK with your App Key
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialized successfully");
        // SDK is now ready to load ads
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay initialization failed: {error.ErrorMessage}");
    }

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }
}
```

**File location**: Save as `Assets/Scripts/LevelPlayInitializer.cs` (or `Assets/Scripts/Ads/LevelPlayInitializer.cs` if you have an Ads subfolder)

**After creating:**
1. Attach script to a GameObject in your first scene
2. In Unity Inspector, find the "App Key" field
3. Paste your App Key from Step 5 into that field

### Option 2: Adding to Existing Script

If adding to an existing script (e.g., `GameManager.cs`):

**1. Add namespace at top of file:**
```csharp
using Unity.Services.LevelPlay;
```

**2. In existing Start() or Awake() method, add initialization:**
```csharp
void Start()
{
    // Register initialization callbacks
    LevelPlay.OnInitSuccess += OnInitSuccess;
    LevelPlay.OnInitFailed += OnInitFailed;

    // Initialize the SDK - REPLACE with your actual App Key from Step 5
    LevelPlay.Init("YOUR_APP_KEY_HERE");

    // ... your other existing Start() code
}
```

**3. Add callback methods to class:**
```csharp
private void OnInitSuccess(LevelPlayConfiguration config)
{
    Debug.Log("LevelPlay SDK initialized successfully");
    // SDK is ready - you can now create ad objects
}

private void OnInitFailed(LevelPlayInitError error)
{
    Debug.LogError($"LevelPlay initialization failed: {error.ErrorMessage}");
}
```

**4. In existing OnDestroy() (or create if it doesn't exist), add:**
```csharp
void OnDestroy()
{
    // Unregister callbacks
    LevelPlay.OnInitSuccess -= OnInitSuccess;
    LevelPlay.OnInitFailed -= OnInitFailed;
}
```

Replace `"YOUR_APP_KEY_HERE"` with your actual App Key from Step 5 (the alphanumeric string copied from the LevelPlay dashboard).

**Note:** If your existing script doesn't already persist across scenes, add `DontDestroyOnLoad(gameObject);` to its `Awake()` method to prevent re-initialization when loading new scenes.

Before testing, double-check that you've replaced the placeholder with your actual App Key. The App Key should be an alphanumeric string, NOT 'YOUR_APP_KEY_HERE'.

### Option 3: Separate Script Referenced by Manager

If you want to keep ads code in its own script that your existing manager references:

**1. Create `LevelPlayInitializer.cs` using the complete class code from Option 1.**

**2. Add the `LevelPlayInitializer` component to a GameObject in the Unity Editor** (at design time — not at runtime). Use a dedicated persistent GameObject (e.g. an "Ads" object in your first scene), and set its **App Key** field in the Inspector, exactly as in Option 1's "After creating" steps.

**3. Reference it from your existing manager** via a serialized field, so `GameManager` can coordinate with the initializer without creating it at runtime:
```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Assign the LevelPlayInitializer GameObject in the Inspector.
    // Enter your App Key on the LevelPlayInitializer component itself (see step 2),
    // NOT here — that keeps your ad credentials in one place where publishers expect them.
    [SerializeField] private LevelPlayInitializer levelPlayInitializer;

    void Awake()
    {
        // ... your other initialization code.
        // LevelPlayInitializer initializes itself in its own Start();
        // GameManager just holds a reference to it via the field above.
    }
}
```

This keeps ad code isolated in `LevelPlayInitializer.cs` while giving `GameManager` a reference to it.

> **Why not `gameObject.AddComponent<LevelPlayInitializer>()` at runtime?** The initializer reads its App Key from a `[SerializeField]` Inspector field. A component added at runtime never receives that Inspector value, so the App Key would be empty and initialization would fail. Attaching the component in the Editor (step 2) is what makes the App Key field available to fill in.

**Note:** `LevelPlayInitializer.Awake()` calls `DontDestroyOnLoad(gameObject)`, so the GameObject you attach it to persists across scenes. Put it on a dedicated Ads GameObject rather than on your `GameManager`, unless you intend `GameManager` to persist too.

### Option 4: Just Show Me the Code

Provide the complete initialization code from Option 1 as a standalone snippet, without instructions for attaching to a GameObject or setting up the Inspector. Include a note: "This is the full initialization class — save it as `LevelPlayInitializer.cs`, attach it to a persistent GameObject in your first scene, and set the App Key field in the Inspector."

**Key points (all options):**
- Always register `OnInitSuccess` and `OnInitFailed` callbacks before calling `Init()`
- Call initialization early in app lifecycle (in `Awake()` or `Start()` of first scene)
- Only initialize once. If initialization script is in first scene and you have multiple scenes, add `DontDestroyOnLoad(gameObject);` in `Awake()` to prevent re-initialization when loading new scenes
- Wait for `OnInitSuccess` before creating ad objects

If initialization fails repeatedly, see `references/troubleshooting.md`. Common causes include incorrect App Key, no internet connection, or missing package dependencies.

## Version-Aware ILRD Init Wiring (Step 7)

**If the user answered Yes or Not Sure to the ILRD question in Step 7**, wire up the impression-revenue
callback. **The correct approach depends on the user's SDK version** (check in **Ads Mediation > Network
Manager**):

**SDK 9.5.0+ (current): do NOT add anything to the initializer here.** In 9.5.0+, ILRD is delivered per
ad instance via the `OnAdImpressionDataReady` event on each ad object — it is wired when you create each
ad in Step 9 (see `references/ilrd-api.md`). The global `LevelPlay.OnImpressionDataReady` event still
exists but is **deprecated in 9.5.0+ and generates a compiler warning** — do not use it on 9.5.0+.

**SDK 9.4.x and earlier:** subscribe to the global `LevelPlay.OnImpressionDataReady` event before
`LevelPlay.Init()`. Add these lines inside `Start()` (before `LevelPlay.Init(appKey)`):

```csharp
// SDK 9.4.x and earlier only. MUST be registered BEFORE LevelPlay.Init() to avoid
// losing early impressions. Callback fires on a BACKGROUND thread — see
// references/ilrd-api.md for thread-safe forwarding patterns and a Firebase example.
LevelPlay.OnImpressionDataReady += OnImpressionDataReady;
```

Add this stub method to the class:

```csharp
private void OnImpressionDataReady(LevelPlayImpressionData impressionData)
{
    // See references/ilrd-api.md for full implementation.
    // For now, just log so you can verify it fires:
    Debug.Log($"ILRD: {impressionData.AdNetwork} / {impressionData.AdFormat} / ${impressionData.Revenue}");
}
```

And unsubscribe in `OnDestroy()`:

```csharp
LevelPlay.OnImpressionDataReady -= OnImpressionDataReady;
```

**iOS path (SDK 9.4.x and earlier only):** if you used the `LevelPlayInitializer.cs` from
`references/ios-setup.md` Part 3, note that `Start()` is a coroutine with **no `LevelPlay.Init(appKey)`
call inside it** — initialization happens in `InitializeLevelPlay()`. Place the
`LevelPlay.OnImpressionDataReady += ...` line inside `InitializeLevelPlay()`, immediately **before**
`LevelPlay.Init(appKey)`, so it registers before the first impression.

For Option 2 (existing script) and Option 3 (separate script referenced by a manager), apply the same
version-aware wiring: on 9.4.x and earlier add the global subscription/stub/unsubscribe shown above; on
9.5.0+ add nothing here (ILRD is wired per ad instance in Step 9). For Option 3, the wiring goes in
`LevelPlayInitializer.cs`, with no changes in `GameManager`.

**Next:** Once you confirm the log fires after your first ad impression, read `references/ilrd-api.md` to
wire it up to your actual analytics platform. Note: ILRD callbacks do not fire with mock ads in the
Unity Editor — you'll need a device build to verify this log fires (see Step 10).

## Basic Initialization

### Minimum Required Setup

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class LevelPlayInitializer : MonoBehaviour
{
    [SerializeField] private string appKey = "YOUR_APP_KEY";

    void Start()
    {
        // Register initialization callbacks
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        // Initialize SDK
        LevelPlay.Init(appKey);
    }

    void OnDestroy()
    {
        // Unregister callbacks
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialized successfully");
        // SDK is ready - you can now create ad objects
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay initialization failed: {error.ErrorMessage}");
    }
}
```

**Key points:**
- Register `OnInitSuccess` and `OnInitFailed` callbacks before calling `Init()`
- The `appKey` is obtained from your LevelPlay dashboard
- Call `Init()` as early as possible (ideally in the first scene)
- Only create ad objects after `OnInitSuccess` fires

## Advanced Initialization

### With User ID

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class AdvancedInitializer : MonoBehaviour
{
    [SerializeField] private string appKey = "YOUR_APP_KEY";

    void Start()
    {
        // Register callbacks
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        // Initialize SDK with optional user ID
        string userId = "user_12345"; // Your internal user ID
        LevelPlay.Init(appKey, userId);
    }

    void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay initialized successfully");
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"Init failed: {error.ErrorMessage}");
    }
}
```

**When to use:** When you need to track users across sessions or implement server-side verification for rewards.

### Complete Production-Ready Example

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class ProductionLevelPlayInitializer : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private string appKey = "YOUR_APP_KEY";

    void Awake()
    {
        // Ensure this object persists across scenes
        DontDestroyOnLoad(gameObject);

        // Initialize as early as possible
        InitializeLevelPlay();
    }

    private void InitializeLevelPlay()
    {
        // Register initialization callbacks
        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;

        // Initialize SDK with optional user ID
        if (HasUserId())
        {
            string userId = GetUserId();
            LevelPlay.Init(appKey, userId);
            Debug.Log($"LevelPlay SDK initialization started with user ID: {userId}");
        }
        else
        {
            LevelPlay.Init(appKey);
            Debug.Log("LevelPlay SDK initialization started");
        }
    }

    void OnDestroy()
    {
        // Unregister callbacks
        LevelPlay.OnInitSuccess -= OnInitSuccess;
        LevelPlay.OnInitFailed -= OnInitFailed;
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay SDK initialization completed");

        // SDK is ready - create ad objects here
        CreateAdObjects();
    }

    private void OnInitFailed(LevelPlayInitError error)
    {
        Debug.LogError($"LevelPlay initialization failed: {error.ErrorMessage}");

        // Optionally retry initialization after delay
        Invoke(nameof(RetryInitialization), 5f);
    }

    private void RetryInitialization()
    {
        Debug.Log("Retrying LevelPlay initialization...");
        LevelPlay.Init(appKey);
    }

    private bool HasUserId()
    {
        // Check if user is logged in and has an ID
        return !string.IsNullOrEmpty(PlayerPrefs.GetString("UserID", ""));
    }

    private string GetUserId()
    {
        // Get your internal user ID
        return PlayerPrefs.GetString("UserID", "");
    }

    private void CreateAdObjects()
    {
        // Create your ad objects here after init succeeds
        // Example:
        // rewardedAd = new LevelPlayRewardedAd("rewarded_ad_unit_id");
        // interstitialAd = new LevelPlayInterstitialAd("interstitial_ad_unit_id");
        Debug.Log("Creating ad objects...");
    }
}
```

## API Reference

### Core Methods

#### `LevelPlay.Init(string appKey, string userId = null)`
Initialize the LevelPlay SDK.

**Parameters:**
- `appKey`: Your application key from LevelPlay dashboard
- `userId` (optional): Your internal user ID for tracking and server-side reward verification

**Usage:**
```csharp
// Basic initialization
LevelPlay.Init("your_app_key");

// With user ID
LevelPlay.Init("your_app_key", "user_12345");
```

**Important:** Register `OnInitSuccess` and `OnInitFailed` callbacks before calling `Init()`.

**When to use user ID:**
- Server-side reward verification
- User-level analytics
- Cross-session tracking

#### `LevelPlay.SetSegment(LevelPlaySegment segment)`
Set user segmentation data for improved ad targeting and reporting.

**Parameters:**
- `segment`: A `LevelPlaySegment` object containing user segment information

**Usage:**
```csharp
// Create a segment with user data
var segment = new LevelPlaySegment();
segment.SegmentName = "high_spenders";
segment.Level = 25;
segment.UserCreationDate = 1609459200000; // Unix timestamp in milliseconds (Jan 1, 2021)
segment.IapTotal = 49.99;
segment.IsPaying = 1; // 0 or 1

// Set custom parameters
segment.SetCustom("vip_tier", "gold");
segment.SetCustom("gameplay_hours", "150");

// Apply segment before or after initialization
LevelPlay.SetSegment(segment);
```

**When to call:**
- Can be called before or after SDK initialization
- Update whenever user segment changes (level up, first purchase, etc.)
- Helps LevelPlay optimize ad delivery and reporting

**LevelPlaySegment properties:**
- `SegmentName` (string): Name of the segment (e.g., "high_spenders", "casual_players")
- `Level` (int): User's current level in the game
- `UserCreationDate` (long): Unix timestamp of when user created their account
- `IapTotal` (double): Total amount spent on in-app purchases
- `IsPaying` (int): Whether the user has made any purchases (0 or 1)

**LevelPlaySegment methods:**
- `SetCustom(string key, string value)`: Add custom key-value parameters for advanced segmentation

**Custom parameter limits:**
- Maximum 5 custom parameters per segment

**Best practices:**
- Update segments when significant user milestones occur
- Use meaningful segment names that describe user behavior
- Keep custom parameters descriptive and consistent
- Stay within the 5 custom parameter limit per segment

#### `LevelPlay.SetPauseGame(bool pause)`
Pause Unity 3D game activities (except ad callbacks) during ad presentation.

**Platform:** iOS only (SDK 8.5.0+)

**Parameters:**
- `pause`: Set to `true` to pause game activities during ads

**Usage:**
```csharp
LevelPlay.SetPauseGame(true);
```

**Notes:**
- iOS only
- Can be called once per session, before or after SDK initialization
- Game resumes automatically when ad closes
- Affects Rewarded and Interstitial ads

#### `LevelPlay.SetMetaData(string key, params string[] values)`
Set metadata for SDK configuration.

**Parameters:**
- `key`: The metadata key
- `values`: Array of values

**Use Cases:**

**1. Server-to-Server Rewarded Callbacks (SDK 8.11.0+)**

Pass custom parameters for rewarded ad callbacks:

```csharp
LevelPlay.SetMetaData("LevelPlay_Rewarded_Server_Params", new [] { "key1=value1", "key2=value2" });
```

**2. Test Suite Activation**

Enable the LevelPlay Test Suite:

```csharp
LevelPlay.SetMetaData("is_test_suite", "enable");
LevelPlay.Init(appKey);
```

**Note:** For Test Suite, must be called before `Init()`.

#### `LevelPlay.SetDynamicUserId(string userId)`
Set a dynamic user ID that can be changed during the session for server-side rewarded ad verification.

**Parameters:**
- `userId`: The dynamic user ID to set

**Returns:** `bool` - true if successfully set, false otherwise

**Usage:**
```csharp
string newUserId = "user_67890";
bool success = LevelPlay.SetDynamicUserId(newUserId);
if (success)
{
    Debug.Log($"Dynamic user ID set to: {newUserId}");
}
```

**When to use:**
- Server-side reward verification for rewarded ads
- When user ID changes mid-session (e.g., user logs in after guest play)
- Must be set before showing rewarded ads that require verification

**Important:** This is different from the userId passed to `Init()`. The dynamic user ID can be changed during the session and is used specifically for server-to-server reward callbacks.

#### `LevelPlay.LaunchTestSuite()`
Launch the LevelPlay Test Suite for testing ad integrations on device.

**Usage:**
```csharp
void OnInitSuccess(LevelPlayConfiguration config)
{
    Debug.Log("SDK initialized");
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Launch Test Suite for testing
    LevelPlay.LaunchTestSuite();
    #endif
}
```

**Requirements:**
- SDK must be initialized before calling this method
- Test Suite must be enabled via metadata before initialization:
  ```csharp
  LevelPlay.SetMetaData("is_test_suite", "enable");
  LevelPlay.Init(appKey);
  ```

**When to use:**
- Testing ad networks on real devices
- Verifying ad fill and display for different networks
- Debugging ad integration issues
- QA testing before release

**Notes:**
- Opens a UI overlay with test controls
- Allows testing each ad network individually
- Only available on device builds (not Editor)
- Should be removed or disabled in production builds

#### `LevelPlay.SetAdaptersDebug(bool enabled)`
Enable or disable debug logging for ad network adapters.

**Parameters:**
- `enabled`: Set to `true` to enable adapter debug logs, `false` to disable

**Usage:**
```csharp
void Awake()
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    // Enable adapter debug logs for development
    LevelPlay.SetAdaptersDebug(true);
    #endif
    
    LevelPlay.OnInitSuccess += OnInitSuccess;
    LevelPlay.Init(appKey);
}
```

**When to use:**
- Debugging ad network integration issues
- Investigating ad load failures
- Understanding adapter behavior
- Development and QA testing

**Notes:**
- Increases console log verbosity
- Helps diagnose network-specific issues
- Should be disabled in production builds
- Can be called before or after SDK initialization

#### `LevelPlay.SetNetworkData(string networkKey, string networkData)`
Set custom data for specific ad networks.

**Parameters:**
- `networkKey`: The ad network identifier (e.g., "UnityAds", "AdMob")
- `networkData`: Custom data string for the network

**Usage:**
```csharp
// Set custom network-specific configuration
LevelPlay.SetNetworkData("UnityAds", "customValue");
LevelPlay.SetNetworkData("AdMob", "extraConfig");
```

**When to use:**
- Passing custom configuration to specific ad networks
- Advanced network-specific setup
- When network adapter requires additional data

**Notes:**
- Call before SDK initialization
- Consult individual network adapter documentation for supported data
- Not commonly needed for standard integrations

### Events

#### `LevelPlay.OnInitSuccess`
Fired when SDK initialization completes successfully.

**Signature:** `event Action<LevelPlayConfiguration>`

**Usage:**
```csharp
LevelPlay.OnInitSuccess += (config) =>
{
    Debug.Log("SDK ready");
    Debug.Log($"Unity Version: {LevelPlay.UnityVersion}");
    Debug.Log($"Plugin Version: {LevelPlay.PluginVersion}");
    // Create ad objects here
};
```

**Note:** `UnityVersion` and `PluginVersion` are static properties on the `LevelPlay` class, not on `LevelPlayConfiguration`.

#### `LevelPlay.OnInitFailed`
Fired when SDK initialization fails.

**Signature:** `event Action<LevelPlayInitError>`

**Usage:**
```csharp
LevelPlay.OnInitFailed += (error) =>
{
    Debug.LogError($"Init failed: Code {error.ErrorCode}, Message: {error.ErrorMessage}");
    // Optionally retry
};
```

**LevelPlayInitError properties:**
- `error.ErrorCode`: Numeric error code
- `error.ErrorMessage`: Description of the initialization error

## Initialization Timing

### Early Initialization (Recommended)

Initialize in your first scene, as early as possible:

```csharp
void Awake()
{
    DontDestroyOnLoad(gameObject);

    LevelPlay.OnInitSuccess += OnInitSuccess;
    LevelPlay.OnInitFailed += OnInitFailed;
    LevelPlay.Init(appKey);
}
```

**Why:** Ads start loading immediately, maximizing availability when needed.

### Delayed Initialization (Advanced)

In some cases, you might delay initialization:

```csharp
void Start()
{
    // Wait for user to complete onboarding first
    StartCoroutine(InitializeAfterOnboarding());
}

IEnumerator InitializeAfterOnboarding()
{
    yield return new WaitUntil(() => HasCompletedOnboarding());

    // Now initialize
    LevelPlay.OnInitSuccess += OnInitSuccess;
    LevelPlay.OnInitFailed += OnInitFailed;
    LevelPlay.Init(appKey);
}
```

**When to use:**
- Waiting for user consent decisions
- Completing critical onboarding first
- Reducing initial app load time

**Trade-off:** Ads won't be ready as quickly.

## Best Practices

1. **Initialize early**: Call `Init()` in your first scene's `Awake()` or `Start()`
2. **Persist the initializer**: Use `DontDestroyOnLoad()` to prevent re-initialization
3. **Register callbacks first**: Always register `OnInitSuccess` and `OnInitFailed` before calling `Init()`
4. **Create ads after init**: Only create ad objects after `OnInitSuccess` fires
5. **Set user ID if available**: Helps with analytics and server-side verification
6. **Initialize only once**: Don't re-initialize the SDK across scenes
7. **Handle failures**: Implement `OnInitFailed` to catch and log initialization errors

## Testing

### Development Checklist
- [ ] SDK initializes successfully (check logs)
- [ ] `OnInitSuccess` callback fires
- [ ] Ads can be created after initialization
- [ ] User ID is set correctly (if used)
- [ ] Initialization happens early in app lifecycle
- [ ] Error handling works (`OnInitFailed` implemented)

### Common Issues

**Issue: Ads not loading after initialization**
- Check that App Key is correct
- Verify ad objects are created **after** `OnInitSuccess` fires
- Check console for initialization errors
- Enable test mode in LevelPlay dashboard

**Issue: Initialization fails**
- Verify App Key matches LevelPlay dashboard exactly
- Check internet connectivity
- Look at `LevelPlayInitError.ErrorMessage` for specific error
- Ensure LevelPlay SDK package is properly installed

**Issue: User ID not being tracked**
- Verify user ID is passed to `Init(appKey, userId)` as the second parameter
- Check that user ID is non-empty and unique
- Verify format meets LevelPlay requirements (alphanumeric, max 64 chars)

**Issue: Ads not ready when needed**
- Initialize earlier in the app lifecycle
- Create ad objects immediately in `OnInitSuccess`
- Load ads immediately after creating ad objects

## Migration from IronSource.* APIs

> **Full migration guide:** for SDK upgrade paths, per-format ad unit API migration, the migration completeness checklist, Unity Ads migration, and Maven Central build failures, see `references/migration-sdk-9.md`. The summary below covers initialization only.

If you're migrating from deprecated IronSource APIs:

**Old (Deprecated):**
```csharp
IronSource.Agent.validateIntegration();
IronSource.Agent.init(appKey);
```

**New (Current):**
```csharp
LevelPlay.OnInitSuccess += OnInitSuccess;
LevelPlay.OnInitFailed += OnInitFailed;
LevelPlay.Init(appKey);
```

**Key differences:**
- Init errors are reported via `OnInitFailed`; for integration validation use the LevelPlay Test Suite (`LaunchTestSuite()`)
- Must register callbacks before calling `Init()`
- Ad objects created explicitly after initialization (not automatic background loading)

## Error Code Reference

When ad operations fail, `LevelPlayAdError` contains an `ErrorCode` property with one of these values:

> **Note:** Code **508** is an initialization/mediation-level error, surfaced via `LevelPlayInitError` in the `OnInitFailed` callback — not via a per-ad `LevelPlayAdError` (hence "N/A" in the Ad Formats column). All other codes below are per-ad errors delivered through `LevelPlayAdError` in the ad load/show failure callbacks.

| Code | Ad Formats | Description |
|------|-----------|-------------|
| **508** | N/A | Init failure of mediation/Network, Calling Demand Only API in non Demand Only mode, Calling non Demand Only API in Demand Only mode |
| **509** | Interstitial, Rewarded | Show Fail: No ads to show |
| **510** | Interstitial, Rewarded, Banner | Load Fail: Server response failed |
| **520** | Interstitial, Rewarded | Show Fail: No internet connection; ShouldTrackNetworkState is enabled |
| **524** | Interstitial, Rewarded | Show Fail: Placement has reached its limit as defined per pace or capping limit |
| **526** | Interstitial, Rewarded | Show Fail: Ad unit has reached its daily cap per session |
| **604** | Banner | Can't load because the placement is capped |
| **605** | Banner | Unexpected exception while loading the banner |
| **606** | Banner | No banner fill on all the networks on the first load |
| **1007** | Interstitial, Rewarded | Auction Fail: Auction request did not contain all required information |
| **1022** | Rewarded | Show Fail: Cannot show a rewarded ad while another rewarded ad is showing |
| **1023** | Rewarded | Show Fail: Show called when there are no available ads to show |
| **1035** | Interstitial | Empty Waterfall |
| **1036** | Interstitial | Show Fail: Cannot show an interstitial while another interstitial is showing |
| **1037** | Interstitial | Load Fail: Cannot load an interstitial while another interstitial is loading |

**Usage in error callbacks:**
```csharp
private void OnAdLoadFailed(LevelPlayAdError error)
{
    Debug.LogWarning($"Ad load failed: Code {error.ErrorCode}, Message: {error.ErrorMessage}");

    // Handle specific errors
    if (error.ErrorCode == 510)
    {
        // Server response failed - retry with backoff
    }
    else if (error.ErrorCode == 520)
    {
        // No internet connection - wait for connectivity
    }
}
```
