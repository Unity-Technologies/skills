# LevelPlay Testing and Validation

Detailed guidance for the two validation approaches referenced from Step 10 of SKILL.md: mock ads in
the Unity Editor (early development) and the LevelPlay Test Suite (integration validation on device).
Read this when the user is testing their integration, asks how mock ads work, or is setting up the
Test Suite.

## Contents
- [Early Development: Mock Ads in Unity Editor](#early-development-mock-ads-in-unity-editor)
- [Mock Ad Callback Behavior](#mock-ad-callback-behavior)
- [Mock Ads Limitations](#mock-ads-limitations)
- [Integration Validation: LevelPlay Test Suite](#integration-validation-levelplay-test-suite)
- [Test Suite Setup](#test-suite-setup)
- [Test Suite Initializer Template](#test-suite-initializer-template)
- [Testing Workflow](#testing-workflow)

## Early Development: Mock Ads in Unity Editor

For early iteration and callback testing, use **mock ads** in Unity Editor. When you press Play in the
Editor, Unity provides mock ads automatically — but only if the active build target is Android or iOS
(see the requirement below).

> **Required: set the active build target to Android or iOS first.** Mock ads (and the LevelPlay SDK
generally) do **not** work while the active build target is Standalone/PC/Mac — which is the default
for new desktop projects. On a non-mobile target the SDK runs as an "unsupported platform" and returns
no ads, so `Init()` and ad loads will appear to do nothing in the Editor. Switch via **File ▸ Build
Profiles** (called **Build Settings** before Unity 6) → select **Android** or **iOS** → **Switch
Platform**, then press Play. If mock ads never appear in the Editor, this is the most likely cause.

**How it works:**

Once the build target is Android or iOS, pressing Play in the Editor provides mock ads automatically.
Mock ads work with ANY App Key and Ad Unit ID values (including dummy values like "test" or "editor").
However, **recommend using real App Key and real Ad Unit IDs** from the LevelPlay dashboard so you don't
forget to update them before building to device.

**Example setup (works in both Editor and device builds):**

The same initialization code you wrote in Step 7 works in both Editor (with mock ads) and device builds
(with real ads). For example:

```csharp
// Initialize with your actual App Key (replace "abc123..." with yours)
// Mock ads work with any value, but use your real key to avoid forgetting to update it later
LevelPlay.Init("abc123youractualappkey");
```

Example for rewarded ads (works in both Editor and device):
```csharp
// In OnInitSuccess callback:
// Use your real ad unit ID from LevelPlay dashboard (replace "12345..." with yours)
// Mock ads work with any value, but use your real IDs to avoid forgetting to update them later
LevelPlayRewardedAd rewardedAd = new LevelPlayRewardedAd("12345youractualadunitid");
rewardedAd.OnAdLoaded += OnAdLoaded;
rewardedAd.OnAdRewarded += OnAdRewarded;
rewardedAd.LoadAd();
```

**Key points:**
- **Mock ads work with any App Key/Ad Unit ID values** (you can even use "test" or "editor")
- **Recommended: Use your real credentials** from Step 5 to avoid forgetting to update them later
- **Mock ads appear automatically** when testing in Unity Editor
- **Real ads appear automatically** when building to device
- **Same code works everywhere** - no switching or conditional compilation needed
- **Android API 33+**: If targeting Android 13+ devices, verify you've added the AD_ID permission to AndroidManifest.xml (see Step 4)

**What mock ads validate:**
- Ad integration flow works correctly
- Most callbacks fire as expected (see callback behavior below)
- Ad loading, showing, and closing logic
- Ad positioning and layout (for banners)
- Basic ad logic and state management

## Mock Ad Callback Behavior

Mock ads in Unity Editor fire most callbacks, but not all:

**Callbacks that FIRE:**
- `OnAdLoaded` - Always fires after LoadAd()
- `OnAdDisplayed` - Fires when ShowAd() is called
- `OnAdRewarded` - Fires for rewarded ads (with test reward)
- `OnAdClosed` - Fires when mock ad is dismissed

**Callbacks that DON'T fire:**
- `OnAdLoadFailed` - Mock ads always succeed loading
- `OnAdDisplayFailed` - Mock ads always succeed showing
- `OnAdClicked` - Mock ads don't simulate user clicks
- `OnAdExpanded` / `OnAdCollapsed` - Banner expand/collapse not simulated
- `OnAdLeftApplication` - No real ad redirect in Editor
- `OnAdInfoChanged` - Mock ads don't update ad info dynamically
- Impression data events (ILRD — `OnAdImpressionDataReady` on 9.5.0+, or `LevelPlay.OnImpressionDataReady` on 9.4.x and earlier) - No impression data generated in Editor

This means you can test your happy-path flow in Editor, but must test error handling on real devices.

**Note on SDK initialization in the Editor:** `LevelPlay.OnInitSuccess` may not fire in all SDK
configurations when running in the Unity Editor. If your initialization callback doesn't trigger and
your ad objects never load as a result, try creating them directly in `Start()` after calling
`LevelPlay.Init()` rather than waiting for the callback — mock ads will appear even without
`OnInitSuccess` firing.

## Mock Ads Limitations
- Don't simulate network latency or failures
- Don't test real ad network behavior
- Don't validate reward logic server-side
- Placeholder UI instead of real ad creatives
- Error callbacks never fire

**Best for**: Early development, rapid iteration on ad logic, callback testing

## Integration Validation: LevelPlay Test Suite

The **Test Suite** is the primary method for comprehensive validation. It tests your integration with
real ad networks on device.

**What Test Suite validates:**
- All ad formats (Rewarded, Interstitial, Banner) with real ads
- SDK initialization with production App Key
- All callbacks fire correctly in production environment
- Real ad network behavior, latency, and edge cases
- Ad rendering and user interaction flows

**Before running the Test Suite:**
- **Unity Ads is pre-installed** — the Ads Mediation package includes the Unity Ads adapter by default, so you have at least one network ready without any additional setup. For ads to fill on device, verify your LevelPlay dashboard has active instances configured for your ad units.
- **Enable Development Build** in **Build Profiles** (called **Build Settings** in Unity versions before Unity 6) before building to device. Without it, SDK console output won't be visible, making it very difficult to diagnose issues if something doesn't work as expected.

## Test Suite Setup

**Setup (requires device build):**

Add these two lines to your existing `LevelPlayInitializer.cs` — do not create a new file or replace
your existing initializer:

1. Before `LevelPlay.Init(appKey)` — at the top of `Start()`:
```csharp
LevelPlay.SetMetaData("is_test_suite", "enable");
```

2. Inside your `OnInitSuccess` callback:
```csharp
LevelPlay.LaunchTestSuite();
```

> **iOS path (`references/ios-setup.md` Part 3 initializer):** `Start()` is a coroutine and
`LevelPlay.Init(appKey)` lives inside `InitializeLevelPlay()`, not `Start()`. Put
`LevelPlay.SetMetaData("is_test_suite", "enable");` as the **first line inside `InitializeLevelPlay()`,
before `LevelPlay.Init(appKey)`** — do NOT put it in `Start()` and do NOT put it after `Init`.
`LaunchTestSuite()` still goes in `OnInitSuccess`. `SetMetaData` must run before `Init` or the Test
Suite will silently never launch.

**Important**: Remove both lines before your production release. Test Suite should only be used during
development and testing.

## Test Suite Initializer Template

**Don't have a `LevelPlayInitializer.cs` yet?** Use this complete template:

```csharp
using UnityEngine;
using Unity.Services.LevelPlay;

public class LevelPlayInitializer : MonoBehaviour
{
    [SerializeField] private string appKey;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Enable Test Suite — REMOVE before production release
        LevelPlay.SetMetaData("is_test_suite", "enable");

        LevelPlay.OnInitSuccess += OnInitSuccess;
        LevelPlay.OnInitFailed += OnInitFailed;
        LevelPlay.Init(appKey);
    }

    private void OnInitSuccess(LevelPlayConfiguration config)
    {
        Debug.Log("LevelPlay initialized successfully");
        // Launch Test Suite — REMOVE before production release
        LevelPlay.LaunchTestSuite();
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

**After creating this script:**
1. Attach it to a GameObject in your first scene
2. In the Unity Inspector, find the "App Key" field
3. Paste your App Key from Step 5 into that field

**Key points:**
- `LevelPlay.SetMetaData("is_test_suite", "enable");` must be called **BEFORE** `LevelPlay.Init()` — if the Test Suite doesn't appear after launch, this is the most likely cause
- `LevelPlay.LaunchTestSuite();` is called **AFTER** successful initialization (in `OnInitSuccess`)
- **Requires device build** - Test Suite does not work in Unity Editor
- Use production App Key, not "editor"
- Build to Android or iOS device and run the app
- Test Suite UI will appear automatically after initialization

## Testing Workflow
1. Add Test Suite code (SetMetaData before Init, LaunchTestSuite after OnInitSuccess)
2. Build to Android or iOS device
3. Run the app on device
4. Test Suite UI launches automatically
5. Follow on-screen instructions to test each ad format
6. Verify all ad formats load and callbacks fire correctly

**Best for**: Integration validation before production, final testing with real ad networks
