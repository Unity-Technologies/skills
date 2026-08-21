---
name: levelplay-unity-integration
description: Integrates the LevelPlay Mediation SDK via the Ads Mediation UPM package. Use when a developer asks about adding ads to a Unity game, implementing rewarded, interstitial, or banner ads, setting up ad mediation, configuring ad networks, installing or updating the Ads Mediation package, troubleshooting LevelPlay namespace errors, resolving Android gradle or iOS CocoaPods dependency issues for ads, configuring ATT or privacy settings for ad compliance, tracking impression-level revenue (ILRD), initializing the LevelPlay SDK, or setting up ad unit IDs. Also use when a developer wants to monetize their Unity game with ads, asks how to get started with LevelPlay, ads, or mediation, or needs help with any part of the LevelPlay integration workflow including platform-specific setup for iOS or Android. Also use when upgrading the LevelPlay or IronSource SDK version, migrating from deprecated IronSource.Agent APIs, or migrating a game from Unity Ads to LevelPlay.
---

# LevelPlay Unity package/SDK Integration

Base editor-side checks on the actual project rather than assumptions — read the project files, or ask the user to confirm in the Editor. The C# scripts generated in this skill are MonoBehaviour files for the user to save to their project, not for inline execution.

This skill covers the LevelPlay integration path only; it does not cover other mediation SDKs. If the user explicitly asks about alternatives, acknowledge that alternatives exist and point them to those vendors' own documentation — do not describe, characterize, or make claims about competitor products.

Follow the steps and provide only the files and configurations described in this skill. Do not proactively add steps, create files, or make recommendations based on general knowledge. If the user asks a question outside the scope of this skill, check the skill and reference files first to confirm it is not covered. If it is not, use general knowledge to respond, but do not incorporate additional steps or files into the integration workflow as a result.

Follow the integration workflow sequentially, one step at a time. Ask only the questions for the current step — do not gather information for future steps in advance. Wait for the user's response at each checkpoint before proceeding.

LevelPlay is Unity's ad mediation platform: it connects your game to multiple ad networks simultaneously and runs a unified auction across multiple ad networks and bidders to maximize competition for each impression. This guide walks you through the full integration: installing the SDK, configuring dependencies for Android and iOS, initializing LevelPlay in your project, and implementing rewarded, interstitial, and banner ads. If you already have part of this set up, you can skip ahead to the relevant step.

**This SKILL.md is the workflow spine. It keeps the decisions, checkpoints, and exact questions; longer code, full API detail, and edge cases live in `references/` and are linked from the relevant step. Read the linked reference when you reach that step — do not answer from general knowledge instead.**

## Integration Workflow

### 0. New Integration or Migration?

Ask: "Are you starting a new LevelPlay integration, migrating an existing one (from an older SDK version or from Unity Ads), or troubleshooting an existing setup?"

- **New integration**: proceed to Step 1.
- **Migration** (SDK upgrade, replacing IronSource.Agent APIs, migrating from Unity Ads, or fixing a Maven Central Android build failure): Read `references/migration-sdk-9.md`. Ask which of the five scenarios applies — A = SDK upgrade, B = init API migration, C = ad unit API migration, D = Maven Central build failure, E = Unity Ads migration — then follow the matching scenario. After applying all code changes, work through the Migration Completeness Checklist (section C5 of the reference) — it catches requirements that a line-by-line translation misses because the legacy code had no equivalent line. Then ask the user to check the Unity console for compilation errors, and fix any that appear before presenting results. Do not block or keep retrying if you cannot see the console: list the files you changed, say what to look for, and continue.
- **Troubleshooting or adding to an existing setup** (ATT, GDPR, ILRD, Test Suite, build errors on a fresh integration, or adding a feature to an already-working integration): Identify what the user needs and go directly to the relevant step or reference from "When to Read Detailed References."

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

**If the SDK looks already installed:** do not take that on trust, and do not ask the user to read
the Package Manager window for you. Read `Packages/packages-lock.json` and look for
`com.unity.services.levelplay`. If it is there, say which version resolved and proceed to Step 4.
If it is not, it is not installed, whatever the conversation so far has assumed: continue with the
install below.

Guide through installing the LevelPlay Unity package using Unity Package Manager:

1. Open Unity → Window > Package Manager
2. Select Unity Registry dropdown or Services tab
3. In the Package Manager search bar, type **Ads Mediation**
4. Confirm the package name matches exactly: the correct package is titled **Ads Mediation**. Do not install either of these packages:
   - **Ads IAP Mediation Adaptor** (a separate in-app purchases package, not the LevelPlay SDK)
   - **Advertisement Legacy** (a deprecated package, not compatible with the current LevelPlay integration)
5. Click Install button
6. Wait for package to download and import

When you install the package, you may see a prompt to install Mobile Dependency Resolver — click **Import** if it appears. This is covered in more detail in the next step.

**Then verify it resolved, by reading the project rather than by asking.** The package id is
`com.unity.services.levelplay` (its Package Manager display name is **Ads Mediation**; the id is
what the project files record). Check both files:

- **`Packages/manifest.json`** lists what the project *asks for*. `com.unity.services.levelplay`
  must appear under `dependencies`.
- **`Packages/packages-lock.json`** records what Unity actually *resolved*. The same id must appear
  here too, with a concrete version. This is the file that answers "did it install", and it is the
  one to trust.

Both are plain JSON in the project, so this check needs no Editor, no CLI, and nothing from the
user. Read them.

> **This is a hard gate, not a formality.** Do not write, generate, or paste a single line of
> LevelPlay code until `com.unity.services.levelplay` is present in `packages-lock.json`. Skipping
> ahead produces code that looks correct, compiles nowhere, and fails with `CS0246` on every
> LevelPlay symbol. If the id is missing from `manifest.json`, the install never happened. If it is
> in `manifest.json` but not `packages-lock.json`, Unity has not resolved it yet: the Editor may
> still be importing, or resolution failed. Say which of the two you found, and stop.
>
> **If you added the id to `manifest.json` yourself and no Editor has run since, the lock file will
> not show it yet. That is expected, not a failure.** Never write the entry into
> `packages-lock.json` yourself: that file is Unity's resolution output, hand-editing it is what the
> migration guide forbids, and an entry you wrote is a false "resolved" signal rather than a passed
> gate. Ask the user to open the Unity Editor so resolution runs, then re-read the file. If no
> Editor is available at all, say so and stop there rather than manufacturing the evidence.

Report the resolved version you found. Do not report "installed" on the strength of the Package
Manager window, a previous turn, or a user's recollection.

**Network Manager:** Access **Ads Mediation > Network Manager** at any time to install additional ad network adapters and check for SDK and adapter updates.

For iOS builds, note that SKAdNetwork configuration will be needed later (reference `references/ios-setup.md` when ready for iOS builds).

### 4. Resolve Native Dependencies (Critical)

**Critical for Android/iOS builds**: LevelPlay requires native dependency resolution. Without this, code compiles in Unity Editor but fails during platform builds with gradle (Android) or CocoaPods (iOS) errors.

**Platform checkpoint — ask before proceeding:** "Which platform(s) are you targeting — iOS, Android, or both?" Record this. It determines which dependency resolution steps apply here, whether ATT is required (Step 6.5), and which testing steps are relevant (Step 10).

**Set the active build target now.** Switch the project's active build target to Android or iOS via **File ▸ Build Profiles** (called **Build Settings** before Unity 6) → **Switch Platform**. This is required before any testing: LevelPlay only runs on Android/iOS targets, so even mock ads in the Editor (Step 10) do nothing while the target is Standalone/PC/Mac.

**Resolve dependencies for the target platform(s).** LevelPlay requires native Android/iOS libraries that Unity's package manager alone doesn't handle; a dependency manager (MDR, UEDM, or EDM4U) bridges this gap. The full procedure — checking for an existing dependency manager, resolving on Android vs iOS, installing one if the user has none, verification, and the older-version Custom Main Gradle Template — is in **`references/dependency-resolution.md`**. Walk the user through it now, and **if targeting both Android and iOS, complete resolution for both before proceeding.**

Ask: "Have you run dependency resolution for your target platform(s) without errors?"

**Android API 33+ (Android 13+):** declare the AD_ID permission in AndroidManifest.xml:

```xml
<uses-permission android:name="com.google.android.gms.permission.AD_ID"/>
```

Without it, advertising ID access fails on Android 13+ devices. Details in `references/dependency-resolution.md`.

**If dependency resolution fails**, see `references/troubleshooting.md` for gradle and CocoaPods error guidance.

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

Privacy settings must be configured **BEFORE** SDK initialization. See `references/privacy-settings.md` for the complete implementation guide (UI, consent management, combined regulations, and the full network key list).

**GDPR — the correct API depends on the user's SDK version** (check in **Ads Mediation > Network Manager**):

**SDK 9.5.0+** — global consent boolean:
```csharp
using Unity.Services.LevelPlay;

// true = user has granted consent, false = user has not consented
LevelPlayPrivacySettings.SetGDPRConsent(true);
```

**SDK 9.4.x** — per-network consent dictionary (this is the CURRENT API on 9.4.x, not legacy — it only becomes `[Obsolete]` on 9.5.0+; do not mislabel it as deprecated):
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

If neither API compiles, your Unity package/SDK may be below 9.4.0 (legacy) — recommend upgrading via **Ads Mediation > Network Manager**. If the user cannot upgrade, the legacy `LevelPlay.SetConsent(bool)` API is documented in `references/privacy-settings.md`.

**CCPA (SDK 9.4.0+):**
```csharp
LevelPlayPrivacySettings.SetCCPA(true); // User opted out of data sale
```

**COPPA (SDK 9.4.0+):**
```csharp
LevelPlayPrivacySettings.SetCOPPA(true); // Child-directed app
```

If CCPA or COPPA fails to compile, upgrade your Unity package/SDK via **Ads Mediation > Network Manager**. Call all of these BEFORE `LevelPlay.Init()` in Step 7.

**For iOS builds — required regardless of privacy regulations above:** Also implement App Tracking Transparency (ATT) before proceeding to Step 7. Apple requires ATT authorization before your app tracks users or accesses the device's advertising identifier on iOS 14.5+. Request ATT authorization before calling `LevelPlay.Init()` — this is both an Apple platform requirement and necessary for personalized ads (which also affects fill rate). See `references/ios-setup.md` for the ATT implementation code.

**If NO privacy regulations and not targeting iOS:** Skip this step and proceed to Step 7.

### 7. Initialize LevelPlay SDK

**Installation checkpoint:**

**First, re-read `Packages/packages-lock.json` and confirm `com.unity.services.levelplay` is there.**
Do this every time you reach this point, even if Step 3 already passed earlier in the conversation.
It costs one file read, and it is the only item here you can settle without the user. An earlier
turn saying the package was installed is not evidence that it is: this check exists because the
install step is the one most often skipped, and the resulting code fails with `CS0246` on every
LevelPlay symbol. If the id is absent, go back to Step 3 and do not write initialization code.

Then confirm the remaining prerequisites with the user, which are the ones no file can answer.
**If the user confirmed they are not using AdMob, omit the Step 6 item.** If Step 4 was already
confirmed in this conversation, skip that item and ask only about Step 5 and Step 6 (if AdMob).

"Please confirm these are working correctly:
- Step 4: Have you run dependency resolution for your target platform(s) without errors?
- Step 5: Do you have your App Key copied from the LevelPlay dashboard?
- Step 6 (only if using AdMob): Have you configured AdMob keys in Unity Editor settings?

Verify these are working before proceeding."

**If the package check failed or they answer NO or are unsure:**
- Package id absent from `packages-lock.json`: code will show `CS0246` namespace errors → Direct to Step 3. This one you established yourself; do not ask the user to overrule it.
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
3. Create a new LevelPlay script that your existing manager references
4. Just show me the initialization code — I'll decide how to integrate it

**Full code for each option is in `references/initialization-api.md` (Code Organization Options).** Behavior that must not change:
- **Option 1 (new script):** If ATT was set up in Step 6.5 (iOS), use the `LevelPlayInitializer.cs` from `references/ios-setup.md` Part 3 (the `IEnumerator Start()` coroutine variant) instead of the plain template.
- **Option 4 (just the code):** provide the complete Option 1 initialization class as a standalone snippet — do NOT create files or add Inspector/GameObject setup steps — with the note: "save it as `LevelPlayInitializer.cs`, attach it to a persistent GameObject in your first scene, and set the App Key field in the Inspector."

**ILRD wiring (if the user answered Yes or Not Sure).** The correct approach depends on the SDK version (check in **Ads Mediation > Network Manager**):
- **SDK 9.5.0+ (current):** add nothing to the initializer — ILRD is delivered per ad instance via `OnAdImpressionDataReady`, wired when each ad is created in Step 9. The global `LevelPlay.OnImpressionDataReady` event is **deprecated on 9.5.0+ and generates a compiler warning** — do not use it.
- **SDK 9.4.x and earlier:** subscribe to the global `LevelPlay.OnImpressionDataReady` event **before** `LevelPlay.Init()`, add a logging stub, and unsubscribe in `OnDestroy()`. On the iOS coroutine initializer, place the subscription inside `InitializeLevelPlay()` immediately before `LevelPlay.Init(appKey)`.

The exact wiring code for each option (including the iOS coroutine placement) is in `references/initialization-api.md` (Version-Aware ILRD Init Wiring). ILRD callbacks do not fire with mock ads — a device build is needed to verify (see Step 10). For advanced options (user ID, segmentation, consent management), see `references/initialization-api.md`.

### 8. Recommend Ad Unit Strategy

Based on the optimization goal identified in Step 2, recommend an ad unit strategy.

**Recall the user's optimization goal from Step 2.** If the conversation has been long or the answer is unclear, confirm: "Earlier you mentioned your optimization goal. To confirm, are you primarily focused on revenue, user experience, or a balance of both?"

**Map the answer to a strategy** and give a brief recommendation (full detail, benchmarks, and placement guidance are in `references/best-practices.md` under "Ad Format Strategy by Goal"):

- **Revenue-focused** → **Revenue Strategy.** Rewarded (primary monetization, multiple high-value moments) → Interstitial (secondary; at transitions; frequency cap 3–5 min) → Banner (persistent during gameplay). Bid floors are an optional revenue lever configured in Step 9. Implementation priority: **Rewarded → Interstitial → Banner**.
- **UX-focused** → **UX Strategy.** Rewarded only, user-initiated (explicit opt-in), high-value rewards, **no forced ads, ever**. Interstitials optional/sparingly at session boundaries only; banners generally avoided or menu-only. Implementation priority: **Rewarded only, or Rewarded → (optional) Interstitial**.
- **Balanced** → **Balanced Strategy.** Rewarded (2–3 strategic placements) → Interstitial (moderate; natural breakpoints; frequency cap 5–7 min) → Banner (selective; menus/low-attention). Implementation priority: **Rewarded → Interstitial → Banner (selective)**.
- **"Not sure yet"** (from Step 2) → use the **Balanced Strategy**, then add: "Since you weren't sure of your goal earlier, I've gone with the Balanced approach — if you'd prefer to lean more toward revenue or user experience now that you've seen the options, just say so."
- If still unclear, ask: "Would you prioritize revenue, user experience, or a balance of both?"

The next step asks which ad formats to implement from this priority list. If the user wants a different order than recommended, accommodate that preference.

### 9. Implement Ad Units

**Read `references/best-practices.md` first** — its "Code Generation Guidelines (Step 9)" section carries the general ad lifecycle, the per-organization-approach code-gen rules, the always-include requirements (MonoBehaviour, `DestroyAd()` in `OnDestroy()`, the placement-capping show-path check (when placements are used), event unsubscription, null checks, error handling), and the bid-floor wiring examples. Incorporate those patterns into all ad implementations.

**Implementation checkpoint:**

"Before providing ad implementation code, please confirm:
- Did you complete SDK initialization in Step 7?
- Did you receive the 'LevelPlay SDK initialized successfully' log message in your Unity console?

Verify initialization is working before proceeding with ad units."

**If they answer NO or are unsure:** direct back to Step 7 and do not provide ad implementation code until initialization is confirmed working.

**Ad format checkpoint — ask before generating any code:** "Which ad formats do you want to implement? Rewarded, Interstitial, Banner, or a combination?" Only implement the formats the user selects. They can add more formats later using the 'Adding More Ad Formats Later' section.

**First, ask the user how they want to organize the ad code. Do not generate any code until they have answered:**

"How would you like to structure your ad implementation?"

1. **Separate manager scripts for each ad format** - Create individual scripts like `RewardedAdManager.cs`, `InterstitialAdManager.cs`, `BannerAdManager.cs` (good for larger projects, clear separation of concerns)

2. **One unified AdManager script** - Create a single `AdManager.cs` that handles all ad formats (simpler, everything in one place)

3. **Just show me the code snippets** - Provide implementation code without wrapping it in specific files, so you can integrate it however you prefer

4. **I already have ad manager code** - Review and help fix/update existing implementation

Based on their answer, adapt your response accordingly (see the code-gen guidelines in `references/best-practices.md`).

**If the user already has ad code (e.g., an existing manager script), ask to see it before generating any** — so you can provide targeted fixes rather than new code from scratch. This applies regardless of which organization option they picked (Option 4 is specifically for reviewing existing code, but the same "show me your code first" applies whenever the user mentions they already have some).

**Then present the optional bid floor feature (skip for Option 4 — review existing code instead):**

Present bid floor ranges only for the formats the user is implementing in this session. Reference starting ranges: Rewarded: $0.50–$2.00 | Interstitial: $0.20–$1.00 | Banner: $0.05–$0.20. Include only the ranges for formats being implemented.

"**Optional — Advanced: Bid Floors**

Most publishers skip this initially and add it once they have real dashboard data. You can safely skip now and return to it later.

If you'd like to set bid floors now: a bid floor sets a minimum bid price (USD) per ad unit — it raises your average eCPM at the cost of lower fill rate. Starting ranges:
[ranges for formats being implemented]

Reply with values per format, or just say 'skip' — you can add them any time."

**Record the answer per format.** Wire `Config.Builder().SetBidFloor(...)` into the ad construction for any format where a value was provided; formats marked 'skip' use the basic constructor (see the bid-floor examples in `references/best-practices.md`).

**If they choose Option 4 (existing code):**
- Ask: "Please share your existing ad manager code for review" and wait for it.
- Analyze the implementation: whether they use the current LevelPlay Ad Unit API (LevelPlayRewardedAd, LevelPlayInterstitialAd, LevelPlayBannerAd), whether they use **deprecated IronSource.Agent APIs**, proper callback registration/unsubscription, and missing error handling or memory leaks.
- Provide specific guidance:
  - If using deprecated APIs: "You're using the old IronSource.Agent API. Here's how to migrate to the new LevelPlay Ad Unit API:" (full migration detail in `references/migration-sdk-9.md` — Scenario B for init, Scenario C per ad format including the C5 completeness checklist)
  - If using current APIs with issues: "Your implementation looks good but I noticed [specific issues]. Here's how to fix them:"
  - If implementation is correct: "Your implementation looks solid. Which additional ad formats would you like to add?"
- Offer fixes as code snippets or suggest refactoring. When adding new formats after review, present the bid floor prompt scoped to those new formats only, confirm whether to match their existing organization pattern or use a new one, then follow the same guidelines as Options 1–3.

For each ad format, follow the implementation guidelines in the detailed references:

- **Rewarded ads**: See `references/rewarded-api.md`
- **Interstitial ads**: See `references/interstitial-api.md`
- **Banner ads**: See `references/banner-api.md`

**Impression Level Revenue Tracking (version-aware):** See `references/ilrd-api.md` to forward impression data to the analytics platform (Firebase, AppsFlyer, Adjust, Singular, or custom backend).
- **SDK 9.5.0+ (current):** subscribe to each ad object's `OnAdImpressionDataReady` event right after you create it (and unsubscribe in `OnDestroy()`). Add this to every ad manager you generate — it is the correct ILRD path in 9.5.0+.
- **SDK 9.4.x and earlier:** ILRD uses the single global `LevelPlay.OnImpressionDataReady` event, wired in the init script (Step 7). If the user answered Yes/Not Sure in Step 7, it is already wired. If they said "No" and want it now, subscribe to `LevelPlay.OnImpressionDataReady` **before** the existing `LevelPlay.Init()` call.

### 10. Testing and Validation

LevelPlay provides two validation approaches for different stages of development. **Full detail — setup, callback-behavior tables, the Test Suite initializer template, and the iOS coroutine placement — is in `references/testing-and-validation.md`. Read it when the user is testing.**

**Early Development: Mock Ads in Unity Editor.** For rapid iteration and callback testing. Pressing Play in the Editor provides mock ads automatically — but **only if the active build target is Android or iOS** (Standalone/PC/Mac returns no ads; this is the most common "no ads in Editor" cause — see Step 4). Mock ads work with any App Key/Ad Unit ID, but recommend real credentials so they aren't forgotten. Mock ads fire most callbacks (OnAdLoaded/Displayed/Rewarded/Closed) but NOT failure, click, or impression/ILRD callbacks — so test error handling on device. Details and the full callback table: `references/testing-and-validation.md`.

**Integration Validation: LevelPlay Test Suite (Recommended).** The primary method for comprehensive validation against real ad networks on device. Key rules that must not change:
- `LevelPlay.SetMetaData("is_test_suite", "enable");` **before** `LevelPlay.Init()`
- `LevelPlay.LaunchTestSuite();` inside `OnInitSuccess`
- **Requires a device build** (does not work in the Editor); enable **Development Build** so SDK logs are visible; use the production App Key.
- **Remove both lines before production release.**
- iOS coroutine initializer: put `SetMetaData` as the first line inside `InitializeLevelPlay()` before `Init` (not in `Start()`).

Add the two lines to the existing `LevelPlayInitializer.cs` (don't create a new file). The full setup, the standalone template for users without an initializer, and the testing workflow are in `references/testing-and-validation.md`.

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

**Example**: If you initially implemented only Rewarded ads using separate manager scripts, and now want to add Interstitial ads: create `InterstitialAdManager.cs` following the same structure as your `RewardedAdManager.cs`, follow the interstitial guidelines from `references/interstitial-api.md`, and test in Editor and on device. Your existing ad formats keep working while you add new ones.

## Best Practices

Before implementing ad code, read `references/best-practices.md`. It covers loading strategy (per format), placement strategy, error handling and graceful degradation, memory management, frequency management, and common mistakes to avoid — incorporate these patterns into all ad implementations.

## Common Issues and Solutions

If the user reports a problem, route to the matching issue in `references/troubleshooting.md` and follow it (stop generating code where that guidance says to). Do not wait for the user to open the reference — surface the fix directly. If they haven't started integration yet, begin with Step 1.

| Symptom | Likely root cause | Action |
|---|---|---|
| `CS0246` on `Unity.Services.LevelPlay`; red underlines on all LevelPlay code | Ads Mediation package not installed | Stop giving code; check `Packages/packages-lock.json` for `com.unity.services.levelplay`; install (Step 3); restart Editor; then resume. See troubleshooting.md. |
| Android gradle / iOS build fails with dependency errors; compiles in Editor but fails at build | Native dependencies not resolved | Resolve dependencies (Step 4 / dependency-resolution.md); verify `Assets/Plugins/Android/`; rebuild. See troubleshooting.md. |
| Ads not loading | SDK not initialized, wrong App Key, ad created before init, or no connectivity | Confirm `OnInitSuccess` fires before creating ads; check App Key; test on device. See troubleshooting.md. |
| Callbacks not firing | Events registered after init, missing subscriptions, or script destroyed | Register callbacks before `Init()`; verify subscriptions; use a persistent GameObject. See troubleshooting.md. |
| Platform-specific build errors (iOS SKAdNetwork/ATT/frameworks; Android Play Services/manifest/gradle) | Platform setup incomplete | See troubleshooting.md and `references/ios-setup.md`. |
| Android build fails resolving `com.ironsource.sdk` dependencies from `android-sdk.is.com` (worked before; nothing changed) | Dependencies moved to Maven Central; the old is.com repository was shut down | Follow Scenario D in `references/migration-sdk-9.md`: delete the stale dependency XMLs, reinstall via Network Manager, verify no is.com references remain. |

## When to Read Detailed References

Read specific references based on what the user is doing:

- **`references/dependency-resolution.md`**: Resolving native dependencies (Step 4), or gradle/CocoaPods build failures
- **`references/initialization-api.md`**: Step 7 init code-organization options and ILRD init wiring; also user ID, segmentation, consent management, advanced config
- **`references/privacy-settings.md`**: GDPR, CCPA, or COPPA compliance (incl. legacy `SetConsent` and the full network key list)
- **`references/ios-setup.md`**: iOS builds — ATT, SKAdNetwork, the iOS coroutine initializer
- **`references/rewarded-api.md`** / **`references/interstitial-api.md`** / **`references/banner-api.md`**: Implementing each ad format (Step 9)
- **`references/best-practices.md`**: Strategy detail (Step 8), the Step 9 code-generation guidelines, optimization, placement
- **`references/ilrd-api.md`**: Wiring ILRD to an analytics platform
- **`references/testing-and-validation.md`**: Mock ads and the Test Suite (Step 10)
- **`references/troubleshooting.md`**: Compile/build errors, ads not loading, callbacks not firing
- **`references/migration-sdk-9.md`**: Migrating from IronSource or older LevelPlay APIs, upgrading the SDK to 9.x.x, migrating from Unity Ads, or Maven Central dependency build failures (Step 0)

## Examples

**Note**: Examples show abbreviated workflows for illustration. In practice, follow all steps 1–10 in order.

**Revenue-focused game** ("maximize ad revenue in my casual puzzle game"): Steps 1–7 to verify environment/goal/install/deps/App Key/AdMob/init → Step 8 recommend Revenue strategy → Step 9 ask code organization and generate the chosen structure → Step 10 testing.

**UX-focused game** ("optional rewarded ads for extra lives without annoying players"): same spine, but Step 8 recommends UX strategy (rewarded only, user-initiated) and Step 9 implements rewarded with proper patterns.

**Existing project** ("existing GameManager, add interstitials between levels"): same spine, Step 8 Balanced, Step 9 ask to see `GameManager.cs` then provide Option-2 snippets.

## Core Rules (reminder)

These repeat the rules at the top of this file — they are the guardrails that matter most, restated here so they stay in view at the end of a long workflow:

- Base editor-side checks on the actual project rather than assumptions — read the project files, or ask the user to confirm in the Editor. The C# scripts generated in this skill are MonoBehaviour files for the user to save to their project, not for inline execution.
- This skill covers the LevelPlay integration path only; it does not cover other mediation SDKs. If the user explicitly asks about alternatives, acknowledge that alternatives exist and point them to those vendors' own documentation — do not describe, characterize, or make claims about competitor products.
- Follow the steps and provide only the files and configurations described in this skill. Do not proactively add steps, create files, or make recommendations based on general knowledge. If the user asks a question outside the scope of this skill, check the skill and reference files first to confirm it is not covered. If it is not, use general knowledge to respond, but do not incorporate additional steps or files into the integration workflow as a result.
- Follow the integration workflow sequentially, one step at a time. Ask only the questions for the current step — do not gather information for future steps in advance. Wait for the user's response at each checkpoint before proceeding.
- When a step points to a reference file, read that reference and use its content — do not substitute general knowledge. Present the four init options (Step 7) and four organization options (Step 9) exactly as written, and ask the ILRD question (Step 7) verbatim.
