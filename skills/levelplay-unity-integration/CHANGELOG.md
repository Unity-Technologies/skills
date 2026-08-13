# Changelog

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
