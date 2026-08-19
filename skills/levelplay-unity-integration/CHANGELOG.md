# Changelog

## v0.9.0 — 2026-08-17 — SDK 9.x migration support

Adds guided migration to the LevelPlay 9.x SDK and the current Ad Unit (MADU) APIs.

**New:**
- "New Integration or Migration?" routing step at the start of the workflow
- Migration reference guide covering five scenarios: SDK upgrade (.unitypackage or UPM, including switching from .unitypackage to UPM), init API migration (IronSource.Agent to LevelPlay.Init), ad unit API migration (rewarded, interstitial, banner, and the ILRD handler), Maven Central dependency build failures, and Unity Ads (Advertisement Legacy) migration
- Upgrade safety flow: Developer Settings values and installed adapters are inventoried, and the user confirms, before any folder deletion; post-upgrade steps cover adapter reinstall, settings re-entry, and removal of the stale LEVELPLAY_DEPENDENCIES_INSTALLED scripting define when switching from .unitypackage to UPM
- Migration completeness checklist covering requirements that a line-by-line translation misses: placement capping checks when dashboard placements are used, an explicit rewarded load trigger, the version API mappings, correct ILRD event names, preserved logging, HideAd vs DestroyAd intent for legacy destroyBanner calls, and removal of onApplicationPause
- Compilation check after migration edits, with errors fixed before presenting results
- Skill description now also triggers on SDK upgrades, deprecated IronSource.Agent APIs, and Unity Ads migration

**Fixed:**
- Banner adaptive-size example used a constructor form that does not compile on any 9.x version; now configured through Config.Builder (verified against 9.0.0, 9.4.0, and 9.5.0 source)
- API mapping corrections: validateIntegration maps to LevelPlay.ValidateIntegration (not LaunchTestSuite); pluginVersion maps to LevelPlay.PluginVersion (distinct from UnityVersion); onApplicationPause is removed in 9.x with no replacement; the legacy ILRD subscription maps to LevelPlay.OnImpressionDataReady on SDK 9.4.x and earlier or per-instance OnAdImpressionDataReady on 9.5.0+
- Unity Ads migration now surfaces that LevelPlay.Init has no test-mode parameter (Test Suite or dashboard test mode are the equivalents) instead of dropping the flag silently
- Package edits during upgrades touch only manifest.json; packages-lock.json is never hand-edited
- Maven Central migration is mentioned only when the project actually needs it
- Corrected a consent-callback name mismatch in the privacy reference, and a banner troubleshooting example that called a method banners do not have

## v0.8.0 — 2026-08-05 — Version-aware ILRD (SDK 9.5.0), rewarded load lifecycle, and improved activation

Accuracy and activation updates reflecting current LevelPlay SDK behavior.

**Impression-level revenue (ILRD) — SDK 9.5.0 API change**
- ILRD now documents both delivery mechanisms: the single global `LevelPlay.OnImpressionDataReady` event (SDK 9.4.x and earlier) and the per-ad-instance `OnAdImpressionDataReady` events on each ad object (SDK 9.5.0+), which replace the global event.
- The global event still exists but is deprecated on SDK 9.5.0+ and generates a compiler warning.
- Updated the initialization step and the rewarded/interstitial/banner references to direct SDK 9.5.0+ users to the per-instance approach.

**Rewarded ad load lifecycle**
- Clarified that `LoadAd()` must be called explicitly; the SDK does not auto-manage rewarded loading (unlike the legacy IronSource API).
- Reframed the guidance so explicit, publisher-triggered loading is the default, with eager preloading documented as an optional pattern. Applies to `references/rewarded-api.md` and the loading-strategy guidance in `SKILL.md`.

**Description and activation**
- Reworked the skill description to increase activation on general ad and monetization requests, not only when a developer names LevelPlay.
- Added guidance at the top of the skill directing the agent to run it as an interactive, step-by-step workflow and use the reference files, rather than answering from general knowledge.

## v0.7.0 — 2026-06-12 — Initial public beta release

First release of the LevelPlay Unity integration skill, released as public beta.

**Features:**
- Step-by-step installation of the LevelPlay SDK using the Ads Mediation package in Unity Package Manager
- Native dependency resolution for Android and iOS
- SDK initialization with three code organization options
- Ad unit strategy recommendations based on business goals (revenue-focused, UX-focused, or balanced)
- Implementation guides for rewarded ads, interstitials, and banner ads
- Privacy compliance support (GDPR, CCPA, COPPA)
- iOS setup (App Tracking Transparency, SKAdNetwork)
- Impression-level revenue tracking (ILRD)
- Testing guidance using mock ads and the LevelPlay Test Suite

## Feedback

This skill is currently in beta. [Share your feedback here](https://docs.google.com/forms/d/e/1FAIpQLSe7WvWozJ67KjgOLglSBvLug8JdgEYk895nn_BHZs0HS_bWJA/viewform).
