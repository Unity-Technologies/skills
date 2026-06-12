---
name: levelplay-unity-integration
description: Use when integrating LevelPlay (IronSource) ads into a Unity project — installing the Ads Mediation package, resolving native dependencies, initializing the SDK, or implementing rewarded ads, interstitials, or banners. Also use for LevelPlay-related errors, privacy compliance (GDPR, CCPA, COPPA), iOS setup (ATT, SKAdNetwork), or impression-level revenue tracking (ILRD).
---

# LevelPlay Unity package/SDK Integration

The C# scripts generated in this skill are MonoBehaviour files for the user to save to their project, not for inline execution.

Follow the integration workflow sequentially, one step at a time. Ask only the questions for the current step — do not gather information for future steps in advance. Wait for the user's response at each checkpoint before proceeding.

LevelPlay is Unity's ad mediation platform: it connects your game to multiple ad networks simultaneously and runs a unified auction across multiple ad networks and bidders to maximize competition for each impression. This guide walks you through the full integration: installing the SDK, configuring dependencies for Android and iOS, initializing LevelPlay in your project, and implementing rewarded, interstitial, and banner ads. If you already have part of this set up, you can skip ahead to the relevant step.

## Integration Workflow

### 1. Verify Unity Environment

Check that the user is working in a Unity project by verifying Assets/ and ProjectSettings/ directories exist. If not in a Unity project, instruct the user to navigate to their Unity project directory. If those directories are not found but the user believes they are in the right place, ask: "It looks like you may not be at your project root — can you navigate to the top-level folder of your Unity project and confirm you can see Assets/ and ProjectSettings/ there?"

### 2. Understand Business Goals

Before implementing ad units, determine the user's optimization priorities to recommend the appropriate ad unit strategy. Ask:

**"What's your primary optimization goal?"**
- **Revenue-focused**: Maximize ad revenue and impression opportunities
- **UX-focused**: Prioritize gameplay flow and user satisfaction  
- **Balanced**: Optimize for both revenue and UX
- **Not sure yet**: Default to Balanced and proceed. At Step 8, briefly note you're using Balanced since they were unsure, and invite them to indicate a different preference now that they've seen the format options.

Record this answer for later strategy recommendation in Step 8.

### 3. Install LevelPlay SDK via UPM

**If the SDK is already installed:** Ask the user to verify 'Ads Mediation' appears under 'Packages: In Project' in the Package Manager. If it does, proceed directly to Step 4.

Guide through installing the LevelPlay Unity package using Unity Package Manager:

1. Open Unity → Window > Package Manager
2. Select Unity Registry dropdown or Services tab
3. Search for "Ads Mediation" package
4. Review package description to confirm it's the LevelPlay SDK
5. Click Install button
6. Wait for package to download and import

When you install the package, you may see a prompt to install Mobile Dependency Resolver — click **Import** if it appears. This is covered in more detail in the next step.

Verify the package appears under "Packages: In Project" in Package Manager after installation.

**Network Manager:** Access **Ads Mediation > Network Manager** at any time to install additional ad network adapters and check for SDK and adapter updates.

For iOS builds, note that SKAdNetwork configuration will be needed later (reference `references/ios-setup.md` when ready for iOS builds).

### 4. Resolve Native Dependencies (Critical)

**Critical for Android/iOS builds**: LevelPlay requires native dependency resolution. Without this, code compiles in Unity Editor but fails during platform builds with gradle (Android) or CocoaPods (iOS) errors.

**Platform checkpoint — ask before proceeding:** "Which platform(s) are you targeting — iOS, Android, or both?" Record this. It determines which dependency resolution steps apply here, whether ATT is required (Step 6.5), and which testing steps are relevant (Step 10).

LevelPlay requires native Android/iOS libraries that Unity's package manager alone doesn't handle. A dependency manager bridges this gap. If you've never added one to your project, don't worry; we'll walk you through it.

**Check for existing dependency manager:**

Ask: "Do you have a dependency manager like Mobile Dependency Resolver (MDR), Unity External Dependency Manager (UEDM), or External Dependency Manager for Unity (EDM4U) installed? Check your Assets folder for these tools."

**If unsure**: Check Assets folder in Unity for folders named 'Mobile Dependency Resolver', 'External Dependency Manager', or 'EDM4U'. If you see any folder with these names, answer 'Yes'. If not, answer 'No'.

**If they HAVE a dependency manager:**

**For Android:**
- With newer versions of Mobile Dependency Resolver (shipped with the Ads Mediation package): Dependencies auto-resolve on build (no manual action needed). If unsure, try building first — if it fails, manually resolve via the Android Resolver menu.
- With older MDR or other managers: Go to Assets > [Your Dependency Manager] > Android Resolver > Resolve
- Example paths:
  - MDR: `Assets > Mobile Dependency Resolver > Android Resolver > Resolve`
  - EDM4U: `Assets > External Dependency Manager > Android Resolver > Resolve`
- Menu paths may vary depending on dependency manager version. Look for 'Android Resolver' under your dependency manager's menu.

**For iOS:**
- All dependency managers require manual CocoaPods installation:
  - MDR: `Assets > Mobile Dependency Resolver > iOS Resolver > Install Cocoapods`
  - EDM4U: `Assets > External Dependency Manager > iOS Resolver > Install Cocoapods`
  - UEDM: Similar path under Unity External Dependency Manager

Ask: "Have you run the dependency resolution for your target platform(s)?"

**If targeting both Android and iOS**, complete dependency resolution for both platforms before proceeding.

**If they DON'T have a dependency manager:**

If the user didn't see the Mobile Dependency Resolver prompt during installation (see Step 3), restart Unity Editor — the prompt may appear after restart. If it still doesn't appear, they may already have a dependency manager installed.

When prompted:
1. Click **Import** on the prompt to install Mobile Dependency Resolver
2. After installation:
   - **Android**: Dependencies will auto-resolve on build (newer MDR versions)
   - **iOS**: Go to Assets > Mobile Dependency Resolver > iOS Resolver > Install Cocoapods

**Alternative**: Install another dependency manager like EDM4U if preferred (search for installation instructions in their documentation).

**Note**: Unity is transitioning to Unity External Dependency Manager (UEDM). If available in your Unity version, prefer UEDM over MDR.

**Verification:**

After resolution, verify:
- **Android**: Gradle dependencies in `Assets/Plugins/Android/`
- **iOS**: Podfile references or CocoaPods installation confirmation in console

**If dependency resolution fails**: Check the Unity console for specific error messages and share them for troubleshooting — or see the **Common Issues and Solutions** section below for gradle and CocoaPods error guidance.

**Android Custom Main Gradle Template (Older LevelPlay Versions):**

For older LevelPlay Unity package versions, manually enable the Custom Main Gradle Template:
1. Go to Edit > Project Settings > Player
2. Select Android tab
3. Expand Publishing Settings
4. Under Build, check Custom Main Gradle Template

In newer LevelPlay Unity package versions (with newer Mobile Dependency Resolver), this is enabled automatically by default.

**Android API 33+ Requirement:**

If targeting Android API level 33 or higher, declare the AD_ID permission in AndroidManifest.xml:

```xml
<uses-permission android:name="com.google.android.gms.permission.AD_ID"/>
```

This permission is required for advertising ID access on Android 13+.

**If you skip this step and target Android API 33+:** advertising ID access will fail on Android 13+ devices.

### 5. Get App Key and Ad Unit IDs

Before initializing LevelPlay, collect credentials from the LevelPlay dashboard.

**Dashboard:** https://platform.ironsrc.com/

**New to LevelPlay?** Set up your app and ad units first:
- [Add your app](https://docs.unity.com/en-us/grow/levelplay/platform/get-started/add-app)
- [Create ad units](https://docs.unity.com/en-us/grow/levelplay/platform/get-started/ad-units)

**App Key:** In the dashboard, go to **Apps** in the left sidenav → find your app → copy the alphanumeric string displayed under the app title.

**Ad Unit IDs:** Go to **Ad units** in the left sidenav → select your app → copy the ID for each format you plan to implement (Rewarded, Interstitial, Banner).

**Note:** You need your App Key now for initialization (Step 7). Ad Unit IDs are only needed at Step 9 — if you haven't decided which ad formats to implement yet, just copy your App Key for now and return here after Step 8.

Keep both accessible — you'll need them in the next steps.

### 6. Configure AdMob Keys (If Using AdMob Network)

**When to use**: Only if using AdMob as a mediation network adapter in LevelPlay.

If using AdMob, configure platform-specific app keys in Unity Editor:

**Access**: Ads Mediation > Developer Settings > LevelPlay Mediation Settings

**Configuration:**
- **Android App Key**: AdMob Android app key
- **iOS App Key**: AdMob iOS app key

This configuration is required for AdMob to work as a mediation network in LevelPlay.

**Troubleshooting**: If you don't see the 'Ads Mediation' menu in Unity Editor, verify the Ads Mediation package is installed (Step 3) and restart Unity Editor.

### 6.5. Privacy & Regulation Settings (If Required)

> **Note:** This skill provides technical integration guidance, including for LevelPlay's privacy APIs. It is not legal advice, and it does not determine which laws apply to your app — that depends on your users, your data practices, and your distribution. Consult your own legal counsel, and refer to [Regulation Advanced Settings for Unity](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/regulation-advanced-settings) for the authoritative LevelPlay documentation.

Ask the user: "Do you need to configure privacy settings for GDPR, CCPA/CPRA (or certain state privacy consumer acts), or for child-directed apps?"

**If YES to any:**

Privacy settings must be configured **BEFORE** SDK initialization. See `references/privacy-settings.md` for complete implementation guide.

**Quick examples:**

**GDPR:**

**SDK 9.5.0+** — global consent boolean:
```csharp
using Unity.Services.LevelPlay;

// true = user has granted consent, false = user has not consented
LevelPlayPrivacySettings.SetGDPRConsent(true);
```

**SDK 9.4.x** — per-network consent dictionary (check your SDK version in Network Manager):
```csharp
using Unity.Services.LevelPlay;
using System.Collections.Generic;

// Add an entry for each ad network you have installed
LevelPlayPrivacySettings.SetGDPRConsents(new Dictionary<string, bool> {
    { "UnityAds", true },
    { "IronSource", true }
    // See references/privacy-settings.md for the full network key list
});
```

If neither API compiles, your Unity package/SDK may be below 9.4.0 — upgrade via **Ads Mediation > Network Manager**.

**CCPA (SDK 9.4.0+):**
```csharp
using Unity.Services.LevelPlay;

LevelPlayPrivacySettings.SetCCPA(true); // User opted out of data sale
```

**COPPA (SDK 9.4.0+):**
```csharp
using Unity.Services.LevelPlay;

LevelPlayPrivacySettings.SetCOPPA(true); // Child-directed app
```

The CCPA and COPPA APIs above also require SDK 9.4.0+. If either fails to compile, upgrade your Unity package/SDK via **Ads Mediation > Network Manager**.

Call these BEFORE `LevelPlay.Init()` in Step 7.

For complete implementation with UI, consent management, and combined regulations, see `references/privacy-settings.md`.

**For iOS builds — required regardless of privacy regulations above:** Also implement App Tracking Transparency (ATT) before proceeding to Step 7. Apple requires ATT authorization before your app tracks users or accesses the device's advertising identifier on iOS 14.5+. Request ATT authorization before calling `LevelPlay.Init()` — this is both an Apple platform requirement and necessary for personalized ads (which also affects fill rate). See `references/ios-setup.md` for the ATT implementation code.

**If NO privacy regulations and not targeting iOS:** Skip this step and proceed to Step 7.

### 7. Initialize LevelPlay SDK

**Installation checkpoint:**

Before providing initialization code, confirm:

**If the user confirmed they are not using AdMob, omit the Step 6 item from the confirmation below.**

If following the sequential workflow and Steps 3 and 4 have already been confirmed in this conversation, skip those items — only ask about Step 5 and Step 6 (if AdMob).

"Please confirm these are working correctly:
- Step 3: Do you see the 'Ads Mediation' package in Unity Package Manager under 'Packages: In Project'?
- Step 4: Have you run dependency resolution for your target platform(s) without errors?
- Step 5: Do you have your App Key copied from the LevelPlay dashboard?
- Step 6 (only if using AdMob): Have you configured AdMob keys in Unity Editor settings?

Verify these are working before proceeding."

**If they answer NO or are unsure:**
- Missing Step 3: Code will show `CS0246` namespace errors → Direct to Step 3
- Missing Step 4: Code compiles but Android/iOS builds will fail → Direct to Step 4
- Missing Step 5: They won't have credentials to initialize → Direct to Step 5
- Do not provide C# code until they confirm all steps are complete

**If they answer YES:**
- **Optional — Analytics: ILRD Wiring.** Ask this question verbatim — do not summarize or rephrase it: "Do you use an analytics or attribution platform (Firebase, AppsFlyer, Adjust, Singular, or custom backend) that needs ad revenue data? If yes, the init script will include a logging stub for Impression Level Revenue (ILRD) — 3 lines of code, no analytics platform setup required yet. (Yes / No / Not sure — defaults to yes)" Record the answer.
- Proceed with initialization code.

LevelPlay SDK must be initialized before loading or showing any ads. Initialization should happen early in the application lifecycle.

**Ask how they want to handle initialization. Present all four options exactly as listed — do not condense or omit any:**
1. Create a new dedicated script for LevelPlay initialization
2. Add to an existing initialization/manager script they already have
3. Create a new LevelPlay script that your existing manager calls
4. Just show me the initialization code — I'll decide how to integrate it

#### Option 1: Creating a New Script

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

**If the user answered Yes or Not Sure to ILRD in Step 7**, also subscribe before Init. Add these lines inside `Start()` (before `LevelPlay.Init(appKey)`):

```csharp
// MUST be registered BEFORE LevelPlay.Init() to avoid losing early impressions.
// Callback fires on a BACKGROUND thread — see references/ilrd-api.md for
// thread-safe forwarding patterns and a Firebase example.
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

**Next:** Once you confirm the log fires after your first ad impression, read `references/ilrd-api.md` to wire it up to your actual analytics platform. Note: ILRD callbacks do not fire with mock ads in the Unity Editor — you'll need a device build to verify this log fires (see Step 10).

#### Option 2: Adding to Existing Script

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

**If the user answered Yes or Not Sure to ILRD in Step 7**, add the same ILRD subscription code shown in Option 1 above: subscribe before `LevelPlay.Init(...)`, add the stub handler method, and unsubscribe in `OnDestroy()`.

Replace `"YOUR_APP_KEY_HERE"` with your actual App Key from Step 5 (the alphanumeric string copied from the LevelPlay dashboard).

**Note:** If your existing script doesn't already persist across scenes, add `DontDestroyOnLoad(gameObject);` to its `Awake()` method to prevent re-initialization when loading new scenes.

Before testing, double-check that you've replaced the placeholder with your actual App Key. The App Key should be an alphanumeric string, NOT 'YOUR_APP_KEY_HERE'.

#### Option 3: Separate Script Called by Manager

If you want to keep ads code in its own script but have your existing manager control initialization:

**1. Create `LevelPlayInitializer.cs` using the complete class code from Option 1**

**2. In existing manager script (e.g., `GameManager.cs`), add:**
```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Awake()
    {
        // Add LevelPlay initialization as a component
        gameObject.AddComponent<LevelPlayInitializer>();

        // ... your other initialization code
    }
}
```

This keeps ad code isolated in `LevelPlayInitializer.cs` while `GameManager` controls when it initializes.

**Note:** `LevelPlayInitializer.Awake()` calls `DontDestroyOnLoad(gameObject)`, which will mark this entire GameObject — including your GameManager and any other components on it — as persistent across scenes. Ensure this is compatible with your scene management strategy.

**If the user answered Yes or Not Sure to ILRD in Step 7**, add the ILRD subscription to `LevelPlayInitializer.cs` as shown in Option 1 above. No changes needed in `GameManager`.

Before testing, ensure you've entered your actual App Key in the Unity Inspector's App Key field on the LevelPlayInitializer component (see Option 1's After Creating steps above).

#### Option 4: Just Show Me the Code

Provide the complete initialization code from Option 1 as a standalone snippet, without instructions for attaching to a GameObject or setting up the Inspector. Include a note: "This is the full initialization class — save it as `LevelPlayInitializer.cs`, attach it to a persistent GameObject in your first scene, and set the App Key field in the Inspector."

**Key points:**
- Always register `OnInitSuccess` and `OnInitFailed` callbacks before calling `Init()`
- Call initialization early in app lifecycle (in `Awake()` or `Start()` of first scene)
- Only initialize once. If initialization script is in first scene and you have multiple scenes, add `DontDestroyOnLoad(gameObject);` in `Awake()` to prevent re-initialization when loading new scenes
- Wait for `OnInitSuccess` before creating ad objects

If initialization fails repeatedly, see the Common Issues section for troubleshooting. Common causes include incorrect App Key, no internet connection, or missing package dependencies.

For more advanced initialization options (user ID, consent management, etc.), see `references/initialization-api.md`.

### 8. Recommend Ad Unit Strategy

Based on the optimization goal identified in Step 2, recommend an ad unit strategy.

**Recall the user's optimization goal from Step 2.** If the conversation has been long or the answer is unclear, confirm: "Earlier you mentioned your optimization goal. To confirm, are you primarily focused on revenue, user experience, or a balance of both?"

**Map the answer to the appropriate strategy:**
- User focused on **maximizing revenue, impressions, or monetization** → Use **Revenue-Focused Strategy**
- User focused on **user experience, retention, or avoiding annoyance** → Use **UX-Focused Strategy**
- User wants to **balance both** or mentioned **both revenue and UX** → Use **Balanced Strategy**
- User answered **"Not sure yet"** in Step 2 → Use **Balanced Strategy**. After presenting the recommendation, add: "Since you weren't sure of your goal earlier, I've gone with the Balanced approach — if you'd prefer to lean more toward revenue or user experience now that you've seen the options, just say so."
- If unclear, ask: "Would you prioritize revenue, user experience, or a balance of both?"

#### Revenue-Focused Strategy

**Goal**: Maximize ad revenue and impression opportunities

**Recommended ad units:**
- **Rewarded ads**: Primary monetization driver
  - Implement in multiple high-value moments
  - Use for premium rewards, extra lives, bonus content
  - Load ads proactively to ensure availability

- **Interstitial ads**: Secondary revenue source
  - Show at natural transition points (level complete, game over)
  - Frequency cap: Every 3-5 minutes of gameplay (see `references/interstitial-api.md`)

- **Banner ads**: Persistent revenue during gameplay
  - Show during core gameplay loops
  - Use smart positioning to avoid UI conflicts

**Revenue lever: bid floor (optional)**

Once ads are live, bid floors let you set a minimum bid price per ad unit — raising your average eCPM at the cost of lower fill rate. You'll be prompted in Step 9 to configure this or skip and add it later.

**Implementation priority**: Rewarded → Interstitial → Banner

The next step will ask you to choose which ad formats to implement from this priority list.

#### UX-Focused Strategy

**Goal**: Maintain excellent user experience while monetizing thoughtfully

**Recommended ad units:**
- **Rewarded ads**: Primary and often only ad format
  - User-initiated only (explicit opt-in)
  - High-value rewards that feel generous
  - No forced ads, ever

- **Interstitial ads**: Optional, sparingly used
  - Only at major session boundaries (e.g., exiting to main menu)
  - Never interrupt active gameplay

- **Banner ads**: Generally avoided or minimized
  - If used, only during menu screens, not gameplay
  - Small, non-intrusive sizes

**Implementation priority**: Rewarded only, or Rewarded → (optional) Interstitial

The next step will ask you to choose which ad formats to implement from this priority list.

#### Balanced Strategy

**Goal**: Optimize both revenue and user satisfaction

**Recommended ad units:**
- **Rewarded ads**: Core monetization
  - 2-3 strategic placements in high-engagement moments
  - Rewards feel valuable but not exploitative

- **Interstitial ads**: Moderate usage
  - At natural breakpoints (level transitions, session ends)
  - Frequency cap: Every 5-7 minutes (see `references/interstitial-api.md`)

- **Banner ads**: Selective placement
  - Show in menus or low-attention moments
  - Hide during intense gameplay

**Implementation priority**: Rewarded → Interstitial → Banner (selective)

The next step will ask you to choose which ad formats to implement from this priority list. If the user wants to implement in a different order than recommended, accommodate that preference.

### 9. Implement Ad Units

**Important: Read Best Practices First**

Before implementing ad code, read the **Best Practices** section below (after Step 10). It contains essential patterns for loading strategy, error handling, memory management, and placement strategy. These patterns should be incorporated into all ad implementations.

**Implementation checkpoint:**

Before implementing ad units, confirm:

"Before providing ad implementation code, please confirm:
- Did you complete SDK initialization in Step 7?
- Did you receive the 'LevelPlay SDK initialized successfully' log message in your Unity console?

Verify initialization is working before proceeding with ad units."

**If they answer NO or are unsure:**
- Direct back to Step 7 to complete initialization first
- Do not provide ad implementation code until initialization is confirmed working

**If they answer YES**, proceed with ad implementation.

**Ad format checkpoint — ask before generating any code:** "Which ad formats do you want to implement? Rewarded, Interstitial, Banner, or a combination?" Only implement the formats the user selects. They can add more formats later using the 'Adding More Ad Formats Later' section.

Based on the chosen strategy, implement the appropriate ad formats.

**Implementation flexibility**: Implement formats one at a time or all at once. Choose which formats you want to implement now, and you can always add more formats later (see 'Adding More Ad Formats Later' section below).

**First, ask the user how they want to organize the ad code. Do not generate any code until they have answered:**

"How would you like to structure your ad implementation?"

1. **Separate manager scripts for each ad format** - Create individual scripts like `RewardedAdManager.cs`, `InterstitialAdManager.cs`, `BannerAdManager.cs` (good for larger projects, clear separation of concerns)

2. **One unified AdManager script** - Create a single `AdManager.cs` that handles all ad formats (simpler, everything in one place)

3. **Just show me the code snippets** - Provide implementation code without wrapping it in specific files, so you can integrate it however you prefer

4. **I already have ad manager code** - Review and help fix/update existing implementation

Based on their answer, adapt your response accordingly.

**Then present the optional bid floor feature (skip for Option 4 — review existing code instead):**

Present bid floor ranges only for the formats the user is implementing in this session. Reference starting ranges: Rewarded: $0.50–$2.00 | Interstitial: $0.20–$1.00 | Banner: $0.05–$0.20. Include only the ranges for formats being implemented.

"**Optional — Advanced: Bid Floors**

Most publishers skip this initially and add it once they have real dashboard data. You can safely skip now and return to it later.

If you'd like to set bid floors now: a bid floor sets a minimum bid price (USD) per ad unit — it raises your average eCPM at the cost of lower fill rate. Starting ranges:
[ranges for formats being implemented]

Reply with values per format, or just say 'skip' — you can add them any time."

**Record the answer per format.** Wire `Config.Builder().SetBidFloor(...)` into the ad construction for any format where a value was provided. Formats marked 'skip' use the basic constructor.

**If they choose Option 4 (existing code):**
- Ask: "Please share your existing ad manager code for review"
- Wait for them to provide the code
- Analyze the existing implementation:
  - Check if they're using current LevelPlay Ad Unit API (LevelPlayRewardedAd, LevelPlayInterstitialAd, LevelPlayBannerAd)
  - Identify if they're using deprecated IronSource.Agent APIs
  - Check for proper callback registration/unsubscription
  - Look for missing error handling or memory leaks
- Provide specific guidance:
  - If using deprecated APIs: "You're using the old IronSource.Agent API. Here's how to migrate to the new LevelPlay Ad Unit API:"
  - If using current APIs with issues: "Your implementation looks good but I noticed [specific issues]. Here's how to fix them:"
  - If implementation is correct: "Your implementation looks solid. Which additional ad formats would you like to add?"
- Offer to either:
  - Provide fixes as code snippets to integrate into existing files
  - Suggest refactoring if the code has structural issues
- When adding new formats after reviewing existing code, present the bid floor prompt scoped to those new formats only, confirm whether to match their existing code organization pattern or use a new one, then follow the same implementation guidelines as Options 1–3 above.

For each ad format, follow the implementation guidelines in detailed references:

- **Rewarded ads**: See `references/rewarded-api.md`
- **Interstitial ads**: See `references/interstitial-api.md`
- **Banner ads**: See `references/banner-api.md`

#### General Implementation Pattern

All ad formats follow a similar lifecycle:

1. **Load**: Request an ad from LevelPlay
2. **Listen**: Register callbacks for ad events (loaded, failed, shown, clicked, closed)
3. **Check readiness**: Verify ad is ready before showing
4. **Show**: Display the ad to the user
5. **Handle callbacks**: Respond to user interactions and ad lifecycle events

#### Code Generation Guidelines

**If the user mentions they have existing code**, ask to see it before providing implementation guidance. This allows you to provide targeted fixes rather than generating new code from scratch.

Adapt output based on the user's chosen organization approach:

**Option 1: Separate manager scripts**
- Create complete, production-ready `.cs` files for each ad format
- Name them clearly: `RewardedAdManager.cs`, `InterstitialAdManager.cs`, `BannerAdManager.cs`
- Include full class structure with proper namespaces
- Each manager handles one ad format completely

**Option 2: Unified AdManager**
- Create a single `AdManager.cs` file
- Include methods and callbacks for all requested ad formats in one class
- Use clear method naming to distinguish between formats (e.g., `LoadRewardedAd()`, `LoadInterstitial()`)
- Keep code organized with regions or comments separating each ad format

**Option 3: Code snippets only**
- Provide focused code blocks without full class wrappers
- Clearly label each snippet (e.g., "Rewarded Ad Initialization", "Interstitial Event Callbacks")
- Explain where/how to integrate each snippet
- Note any dependencies between snippets

**Regardless of chosen approach, always include:**
- All manager classes must inherit from `MonoBehaviour` — required for Unity lifecycle methods (`Start()`, `OnDestroy()`) and to attach the script to a GameObject
- After generating any manager script, instruct the user to attach it to a persistent GameObject in their scene (the same one as the initializer, with `DontDestroyOnLoad`)
- For banner and interstitial managers, call `DestroyAd()` in `OnDestroy()` so ads are destroyed and memory is freed when the manager is destroyed
- Proper event subscription and unsubscription (to avoid memory leaks)
- Null checks and defensive programming
- Debug logs for troubleshooting
- Clear variable names that match LevelPlay conventions
- Error handling and graceful degradation

**Bid floor handling (per-format):**
- If the user provided a bid floor value for a format, wrap construction in `Config.Builder().SetBidFloor(value).Build()` and pass the config to the constructor.
- If the user said 'skip' for that format, use the basic constructor (`new LevelPlayRewardedAd(adUnitId)`).
- Apply per-format: a publisher may set a floor on rewarded but skip banner.

Example (with bid floor):
```csharp
var config = new LevelPlayRewardedAd.Config.Builder()
    .SetBidFloor(0.80)
    .Build();
rewardedAd = new LevelPlayRewardedAd(adUnitId, config);
```

Example (skipped):
```csharp
rewardedAd = new LevelPlayRewardedAd(adUnitId);
```

**Impression Level Revenue Tracking:** If the user answered Yes or Not Sure to ILRD in Step 7, the ILRD callback was wired up in the init script. See `references/ilrd-api.md` to forward the data to the analytics platform (Firebase, AppsFlyer, Adjust, Singular, or custom backend).

If the user said "No" to ILRD and wants to add it now, see `references/ilrd-api.md` — subscribe to `LevelPlay.OnImpressionDataReady` **before** the existing `LevelPlay.Init()` call.

### 10. Testing and Validation

LevelPlay provides two validation approaches for different stages of development:

#### Early Development: Mock Ads in Unity Editor

For early iteration and callback testing, use **mock ads** in Unity Editor. Unity automatically provides mock ads when you press Play in the Editor - no special configuration needed.

**How it works:**

When you press Play in Unity Editor, Unity automatically provides mock ads - no special configuration needed. Mock ads work with ANY App Key and Ad Unit ID values (including dummy values like "test" or "editor"). However, **recommend using real App Key and real Ad Unit IDs** from the LevelPlay dashboard so you don't forget to update them before building to device.

**Example setup (works in both Editor and device builds):**

The same initialization code you wrote in Step 7 works in both Editor (with mock ads) and device builds (with real ads). For example:

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

**Mock ad callback behavior:**

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
- `LevelPlay.OnImpressionDataReady` (ILRD) - No impression data generated in Editor

This means you can test your happy-path flow in Editor, but must test error handling on real devices.

**Note on SDK initialization in the Editor:** `LevelPlay.OnInitSuccess` may not fire in all SDK configurations when running in the Unity Editor. If your initialization callback doesn't trigger and your ad objects never load as a result, try creating them directly in `Start()` after calling `LevelPlay.Init()` rather than waiting for the callback — mock ads will appear even without `OnInitSuccess` firing.

**Mock ads limitations:**
- Don't simulate network latency or failures
- Don't test real ad network behavior
- Don't validate reward logic server-side
- Placeholder UI instead of real ad creatives
- Error callbacks never fire

**Best for**: Early development, rapid iteration on ad logic, callback testing

#### Integration Validation: LevelPlay Test Suite (Recommended)

The **Test Suite** is the primary method for comprehensive validation. It tests your integration with real ad networks on device.

**What Test Suite validates:**
- All ad formats (Rewarded, Interstitial, Banner) with real ads
- SDK initialization with production App Key
- All callbacks fire correctly in production environment
- Real ad network behavior, latency, and edge cases
- Ad rendering and user interaction flows

**Before running the Test Suite:**
- **Unity Ads is pre-installed** — the Ads Mediation package includes the Unity Ads adapter by default, so you have at least one network ready without any additional setup. For ads to fill on device, verify your LevelPlay dashboard has active instances configured for your ad units.
- **Enable Development Build** in **Build Profiles** (called **Build Settings** in Unity versions before Unity 6) before building to device. Without it, SDK console output won't be visible, making it very difficult to diagnose issues if something doesn't work as expected.

**Setup (requires device build):**

Add these two lines to your existing `LevelPlayInitializer.cs` — do not create a new file or replace your existing initializer:

1. At the top of `Start()`, before `LevelPlay.Init(appKey)`:
```csharp
LevelPlay.SetMetaData("is_test_suite", "enable");
```

2. Inside your `OnInitSuccess` callback:
```csharp
LevelPlay.LaunchTestSuite();
```

**Important**: Remove both lines before your production release. Test Suite should only be used during development and testing.

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

**Testing workflow:**
1. Add Test Suite code (SetMetaData before Init, LaunchTestSuite after OnInitSuccess)
2. Build to Android or iOS device
3. Run the app on device
4. Test Suite UI launches automatically
5. Follow on-screen instructions to test each ad format
6. Verify all ad formats load and callbacks fire correctly

**Best for**: Integration validation before production, final testing with real ad networks

#### Production Release Checklist

Before releasing to production:

- [ ] Test Suite validation completed successfully on device
- [ ] All ad formats load correctly (Rewarded, Interstitial, Banner if implemented)
- [ ] All callbacks fire as expected
- [ ] App Key and ad unit IDs verified correct for production
- [ ] Tested on multiple devices (different screen sizes, OS versions)
- [ ] **iOS-specific requirements completed** (if targeting iOS):
  - [ ] SKAdNetwork IDs configured in Info.plist (see `references/ios-setup.md`)
  - [ ] App Tracking Transparency (ATT) framework implemented (see `references/ios-setup.md`)
  - [ ] iOS privacy manifest configured if required
  - [ ] Tested on physical iOS device (not just simulator)
- [ ] **Android-specific requirements completed** (if targeting Android):
  - [ ] Google Play Services dependencies resolved (Step 4 completed)
  - [ ] AD_ID permission added to AndroidManifest.xml if targeting API 33+ (see Step 4)
  - [ ] Tested on physical Android device
- [ ] Tested with real ads in production environment
- [ ] Ad frequency capping implemented (if using interstitials)
- [ ] Error handling works correctly (test with airplane mode - ads should fail gracefully without crashing or blocking gameplay)

## Adding More Ad Formats Later

If you've already integrated some ad formats and want to add more:

1. **Skip to Step 9** - You don't need to repeat the initial setup steps. Before proceeding, verify your existing initialization still works by checking the Unity console for the 'LevelPlay SDK initialized successfully' log.
2. **Choose the additional formats** you want to implement
3. **Follow the same organization pattern** you used before:
   - If you created separate manager scripts, create a new manager script for the new format
   - If you used a unified AdManager, add the new format's code to your existing AdManager class
   - If you used code snippets, integrate new snippets following the same pattern
4. **Follow the same implementation guidelines** from Step 9 for the new ad format
5. **Test the new format** following Step 10 testing guidelines

**Example**: If you initially implemented only Rewarded ads using separate manager scripts, and now want to add Interstitial ads:
- Create `InterstitialAdManager.cs` following the same structure as your `RewardedAdManager.cs`
- Follow the interstitial implementation guidelines from `references/interstitial-api.md`
- Test the interstitial ads in Unity Editor and on device

Your existing ad formats will continue to work while you add new ones. You can add formats incrementally without disrupting your current implementation.

## Best Practices

### Loading Strategy

**Rewarded Ads:**
- Load immediately after SDK initialization (load proactively means load early and keep ads preloaded)
- Reload immediately after showing or when ad fails to load
- Always keep a rewarded ad ready to show

**Interstitial Ads:**
- Load proactively before natural break points
- Maintain a loaded interstitial for opportunistic moments
- Reload after showing

**Banner Ads:**
- Load when entering scenes where banners will be displayed
- Destroy banners when leaving those scenes to free memory

### Placement Strategy

Align ad placements with user engagement moments:

- **High engagement** (post-victory, level up): Best for rewarded ads
- **Natural transitions** (level complete, menu navigation): Good for interstitials
- **Low attention** (waiting periods, menus): Acceptable for banners

### Error Handling

Always handle ad load failures gracefully:

```csharp
private void OnAdLoadFailed(LevelPlayAdError error)
{
    Debug.LogWarning($"Ad failed to load: {error.ErrorMessage}");

    // Retry loading after a delay
    Invoke(nameof(LoadAd), 30f); // Retry in 30 seconds

    // Provide fallback UX if ad was requested by user
    // Don't leave users stuck waiting for an ad that won't load
}
```

### Memory Management

Properly unsubscribe from events to prevent memory leaks:

```csharp
void OnDestroy()
{
    // Unsubscribe from all LevelPlay events
    if (rewardedAd != null)
    {
        rewardedAd.OnAdLoaded -= OnAdLoaded;
        rewardedAd.OnAdLoadFailed -= OnAdLoadFailed;
        // ... unsubscribe from all other events
    }
}
```

## Common Issues and Solutions

**Note**: This section addresses issues that can occur during integration. If you haven't started the integration yet, begin with Step 1: Verify Environment.

### Issue: CS0246 - Type or namespace 'Unity.Services.LevelPlay' not found

**Root cause**: Ads Mediation package not installed in Unity project

**Symptoms:**
- Compiler errors: `The type or namespace name 'LevelPlay' could not be found`
- Compiler errors: `The type or namespace name 'Unity.Services.LevelPlay' could not be found`
- Red underlines in Unity Editor on all LevelPlay code

**Solutions:**
1. Stop providing code immediately
2. Ask: "Can you confirm the Ads Mediation package is installed? Check Window > Package Manager and verify 'Ads Mediation' appears under 'Packages: In Project'"
3. If not installed, direct to Step 3 to install via Unity Package Manager
4. Have them restart Unity Editor after installation (important!)
5. Verify installation by checking that `using Unity.Services.LevelPlay;` no longer shows errors
6. Only resume code generation after confirmation and verification

**Prevention**: Always verify package installation at Step 7 checkpoint before generating any code.

### Issue: Android gradle build fails / iOS build fails with dependency errors

**Root cause**: Native dependencies not resolved

**Symptoms:**
- **Android**: Gradle build errors mentioning missing dependencies or classes
- **iOS**: CocoaPods errors, missing frameworks, or linker errors
- Code compiles perfectly in Unity Editor but fails during platform build
- Build succeeds in Editor but crashes immediately on device

**Solutions:**
1. Verify you have a dependency manager installed (Mobile Dependency Resolver, Unity External Dependency Manager, or EDM4U)
2. Check your project's Assets folder for dependency manager tools
3. Run dependency resolution:
   - **Android (newer MDR versions)**: Should auto-resolve on build. If failing, manually resolve via Assets > Mobile Dependency Resolver > Android Resolver > Resolve
   - **Android (older/other managers)**: Assets > [Your Dependency Manager] > Android Resolver > Resolve
   - **iOS (all managers)**: Assets > [Your Dependency Manager] > iOS Resolver > Install Cocoapods
4. Verify resolution:
   - **Android**: Check `Assets/Plugins/Android/` for gradle files
   - **iOS**: Look for Podfile or CocoaPods confirmation in console
5. If you don't have a dependency manager, restart Unity - you should see a prompt to install Mobile Dependency Resolver
6. Rebuild for your target platform after resolution

**Prevention**: Complete Step 4 (dependency resolution) before building for Android/iOS.

### Issue: Ads not loading

**Possible causes:**
- SDK not initialized before loading ads
- Incorrect App Key
- Ad object created before initialization completes
- Network connectivity issues
- Ad inventory not available in test region

**Solutions:**
- Verify `LevelPlay.Init()` is called and `OnInitSuccess` fires before creating ad objects
- Create ad objects only after `OnInitSuccess` callback
- Check App Key matches LevelPlay dashboard
- Test on real devices with active internet connection
- Enable test mode in LevelPlay dashboard for guaranteed test ads (Note: dashboard test mode is separate from mock ads in the Unity Editor — it enables real test ads on device)

### Issue: Callbacks not firing

**Possible causes:**
- Events registered after SDK initialization
- Missing event subscriptions
- Script destroyed before callbacks execute

**Solutions:**
- Register callbacks before calling `Init()`
- Verify all callbacks are subscribed (check with Debug.Log statements)
- Use persistent GameObject with DontDestroyOnLoad if needed

### Issue: Platform-specific build errors

**iOS:**
- Ensure SKAdNetwork IDs are configured in Info.plist
- Verify ATT is implemented correctly (see `references/ios-setup.md`)
- Check Xcode build settings for required frameworks

**Android:**
- Verify Google Play Services is included
- Check AndroidManifest.xml for required permissions
- Ensure Gradle dependencies are resolved

## When to Read Detailed References

Read specific references based on what the user is implementing:

- **`references/rewarded-api.md`**: When implementing rewarded ads
- **`references/interstitial-api.md`**: When implementing interstitial ads
- **`references/banner-api.md`**: When implementing banner ads
- **`references/initialization-api.md`**: When user ID tracking, segmentation, consent management, or advanced SDK configuration options are needed
- **`references/ios-setup.md`**: When targeting iOS builds
- **`references/best-practices.md`**: When user asks for optimization guidance or troubleshooting
- **`references/privacy-settings.md`**: When GDPR, CCPA, or COPPA compliance is needed
- **`references/ilrd-api.md`**: When wiring ILRD to an analytics platform

## Examples

**Note**: These examples show abbreviated workflows for illustration. In practice, follow all steps 1–10 in order to ensure proper integration.

### Example 1: Revenue-Focused Game

**User request:** "I want to maximize ad revenue in my casual puzzle game"

**Response approach:**
1. Verify Unity project (Step 1)
2. Ask about optimization goal (Step 2)
3. Verify SDK installed (Step 3)
4. Verify dependencies resolved (Step 4)
5. Verify they have App Key (Step 5)
6. Verify AdMob config if needed (Step 6)
7. Verify SDK initialized (Step 7)
8. Recommend revenue-focused strategy (Step 8)
9. Ask about code organization preference (Step 9)
10. Generate appropriate code structure (Step 9)
11. Guide through testing (Step 10)

### Example 2: UX-Focused Game

**User request:** "I want to add optional rewarded ads for extra lives without annoying players"

**Response approach:**
1. Verify Unity project (Step 1)
2. Ask about optimization goal (Step 2)
3. Verify SDK installed (Step 3)
4. Verify dependencies resolved (Step 4)
5. Verify they have App Key (Step 5)
6. Verify AdMob config if needed (Step 6)
7. Verify SDK initialized (Step 7)
8. Recommend UX-focused strategy (Step 8): Rewarded only, user-initiated
9. Ask about code organization preference (Step 9)
10. Provide implementation with proper patterns (Step 9)
11. Guide through testing (Step 10)

### Example 3: Existing Project Integration

**User request:** "I have an existing GameManager script and want to add interstitial ads between levels"

**Response approach:**
1. Verify Unity project (Step 1)
2. Ask about optimization goal (Step 2)
3. Verify SDK installed (Step 3)
4. Verify dependencies resolved (Step 4)
5. Verify they have App Key (Step 5)
6. Verify AdMob config if needed (Step 6)
7. Verify SDK initialized (Step 7)
8. Recommend balanced strategy (Step 8)
9. Ask to see existing GameManager.cs, then provide code snippets (Step 9)
10. Guide through testing (Step 10)

