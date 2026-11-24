# Changelog

## Contents

- [v0.13.0 — 2026-11-23 — Skill quality checklist updates](#v0130--2026-11-23--skill-quality-checklist-updates)
- [v0.12.0 — 2026-11-19 — App and ad units through the Management API](#v0120--2026-11-19--app-and-ad-units-through-the-management-api)
- [v0.11.0 — 2026-11-19 — Project-aware placement proposals](#v0110--2026-11-19--project-aware-placement-proposals)
- [v0.10.0 — 2026-08-21 — Workflow spine, and a hard install gate](#v0100--2026-08-21--workflow-spine-and-a-hard-install-gate)
- [v0.9.0 — 2026-08-17 — SDK 9.x migration support](#v090--2026-08-17--sdk-9x-migration-support)
- [v0.8.0 — 2026-08-05 — Version-aware ILRD (SDK 9.5.0), rewarded load lifecycle, and improved activation](#v080--2026-08-05--version-aware-ilrd-sdk-950-rewarded-load-lifecycle-and-improved-activation)
- [v0.7.0 — 2026-06-12 — Initial public beta release](#v070--2026-06-12--initial-public-beta-release)

## v0.13.0 — 2026-11-23 — Skill quality checklist updates

The skill follows the Unity skills checklist more closely: it reads the LevelPlay docs before acting, checks and reports its work at the end, and drops repeated text.

**Added:**
- "Read the documentation first" and "Documentation topic map" sections, with the page list in `references/docs-topic-map.md`, so the agent checks the current LevelPlay docs before it acts. For privacy questions, the agent links the Regulation Advanced Settings page instead of summarizing it.
- Guardrail and restore point lines at the top of the skill.
- Added a final step: the agent checks for zero console errors and, only when it suspects a fault, reads Play mode state through `script eval`.
- A "Final report" section: what the agent changed, any side effects, and what the user still needs to do.
- `SECURITY.md`: read-only C# in the Editor through `script eval` as an accepted risk, with its mitigation.

**Changed:**
- The description follows the "Make sure to use this skill when..." pattern, and covers creating the LevelPlay app and ad units, and adding ads from a single network.
- Step 8 skips the approval question when every placement is deferred.
- The project profile treats Unity 6 template identifiers as defaults, not as Android or iOS setup, and rechecks each cited method before the readout.
- Links the agent fetches in `references/management-api.md` and `references/ios-setup.md` use the Markdown version of each page.
- Step 3, Step 9, and `references/dependency-resolution.md` give one instruction per step.
- `references/testing-and-validation.md` now explains what to do when mock ads don't run as expected in the **Device Simulator**.
- Evals: close-call trigger queries replace unrelated ones, two trigger queries become multi-step tasks, an AdMob-only request is a should-trigger query, editor eval 3 covers existing ad code, and a new eval checks that the legal notice appears word for word.

**Removed:**
- The introduction paragraph, a repeated note in Step 5, and a repeated example under "Adding More Ad Formats Later".

## v0.12.0 — 2026-11-19 — App and ad units through the Management API

The agent can find or create the user's LevelPlay app and ad units through the LevelPlay Management API, as an alternative to the dashboard.

**Added:**
- `references/management-api.md`: credentials read only from the `LEVELPLAY_SECRET_KEY` and `LEVELPLAY_REFRESH_TOKEN` environment variables, never pasted, printed or written to the project; the auth, list and create calls with `curl`; the answers that are not errors (an empty account answers 404) and the error handling; and a procedure that lists what exists, reuses it, and creates only what is missing after the user confirms a table of what will be created, because the API cannot delete what it creates.
- `SECURITY.md`: the authenticated network calls and the undeletable resources as accepted risks, with their mitigations.

**Changed:**
- The create calls send their JSON body from a file outside the project (`--data-binary @file`, checked with `json.tool` first) instead of a quoted `-d` argument, so a name taken from the project can never be read as part of the shell command. `SECURITY.md` lists it under Mitigations.
- Step 5 offers the API path when the credentials are set, and asks which path the user wants. Either path gives the App Key at Step 5 and the ad unit ids after Step 8 approval.
- Step 7's checkpoint skips the App Key question when the key came through the API, and Step 8 runs the reference's ad unit part on approval when the user chose the API path.
- Evals: a trigger query for creating the app and ad units through the API replaces the near-duplicate ad unit IDs query, and a functional eval covers credential handling and the confirmation before creating.


## v0.11.0 — 2026-11-19 — Project-aware placement proposals

The skill reads the game before it proposes ads, and proposes placements in the game's own code rather than a generic format list.

**Added:**
- `references/project-inference.md`: how to profile the project from its files before proposing ads, in a budgeted order: game type, session length, key moments cited as `Class.Method()` at `file:line`, the buttons a rewarded placement needs, economy, existing monetization, and target platforms. The agent shows a seven-row readout, and asks at most three questions when the project has no game loop yet.
- An **Audience** row in the profile, asked with the goal question rather than inferred, because whether a game is child-directed is the developer's determination. The answer rules formats out in Step 8, makes the `SetCOPPA(true)` call in Step 6.5 required, and sets the `coppa` flag on any app created through the API.
- Existing ad placements are audited against the placement guards, not inherited: a forced ad that pays a reward, or a rewarded ad with no button, is reported as found and as needing a change, and Step 8 proposes the conversion at the same moment.
- `references/placement-strategy.md`: how to turn the profile and the optimization goal into a placement proposal (format, moment, rule, code location), with the guards that always apply. A rewarded ad is hooked only to a button the player presses, a missing button is named as one the user adds rather than invented, and the proposal carries no revenue, eCPM or fill figures. Interstitial caps follow the ranges in `best-practices.md`.
- A "Child-directed and mixed-audience games" section: banner off on every screen, rewarded off by default, no interstitial at session start, and "no ads" as a valid proposal for a free-play toy.
- The mid-action guard now covers events the game raises on its own (a timer, a year-end or wave tick, a room generating): not a transition unless the game already stops play behind a screen there.
- The proposal's Moment column names the action that fires the ad (a press, a screen closing, a scene load), never a screen the player is still reading.
- The agent never states what a regulation permits, requires or forbids; it points to `references/privacy-settings.md` for the API and leaves the determination to the developer.
- A legal notice, shown word for word before the profile table the first time the profile appears.
- Privacy settings are named, never explained: the Step 6.5 question, the Quick Reference table, the GDPR/CCPA/COPPA intros and the code samples in `references/privacy-settings.md` no longer describe what a law is or where it applies; they point to LevelPlay's Regulation Advanced Settings page, and the samples' region checks become `ShouldRequestGDPRConsent()` / `ShouldApplyCCPA()` that the developer's own logic decides. "Children" is no longer defined as under 13.
- The button search also looks for a screen's own buttons (Restart, Retry, Return, Next) in scripts, prefabs and scenes, because a lose or results screen that no script shows by name is found through the buttons on it.
- An optional `#if UNITY_EDITOR || DEVELOPMENT_BUILD` guard around the two Test Suite lines in `testing-and-validation.md`, so a forgotten removal is compiled out of release builds. Removing the lines before release is still required.

**Changed:**
- The changelog moves to `references/CHANGELOG.md` and the README is removed, following the repository's skill layout (supplemental files live under `references/`).
- Step 2 is now "Profile the Project, then Confirm the Goal": the agent reads the project, shows the readout, then asks the existing goal question with Balanced pre-filled.
- Step 8 is now "Propose Placements": the goal-to-strategy mapping is unchanged, and it ends with a placement table the user approves or edits before Step 9.
- Step 9 skips the ad-format question when the approved proposal already answers it, and wires each approved placement with one call at its cited location.
- Evals cover the profile-then-proposal flow, the no-game-loop case, the child-directed case, an existing forced ad that pays a reward, and an automatic tick that is not an interstitial moment.

## v0.10.0 — 2026-08-21 — Workflow spine, and a hard install gate

Moves reference material out of the skill file and makes the SDK install a verified step rather than an assumed one.

**Changed:**
- The skill file is now the workflow spine only, going from about 1,100 lines to 435. The dependency-resolution, testing-and-validation and troubleshooting material that was inlined in it moves into `references/`. Nothing was dropped; it is read on demand instead. The skill file is loaded in full on every invocation while a reference file is read only when a step links it, so this is a saving on every run that does not need the detail.
- The reference set grows from nine files to twelve: `dependency-resolution.md`, `testing-and-validation.md` and `troubleshooting.md` are now separate files.

**Added:**
- **A hard install-verification gate at Step 3.** No LevelPlay code is written until `com.unity.services.levelplay` is confirmed present in `Packages/packages-lock.json`, read from the project rather than taken from the Package Manager window or an earlier turn. The package is easy to believe is installed: its display name is **Ads Mediation** while the recorded id is `com.unity.services.levelplay`, two similarly named packages are the wrong ones, and the install prompts for a second package partway through. Code written before the id resolves fails with `CS0246` on every LevelPlay symbol, which reads as a code problem rather than an install problem. The gate also distinguishes "the install never happened" from "Unity has not resolved it yet", because the fix differs.
- The deprecated-APIs section now states explicitly that `SetGDPRConsents(Dictionary)` is **not** deprecated on SDK 9.4.x, where it is the correct call, and only becomes `[Obsolete]` on 9.5.0+. It is kept out of the deprecated list rather than listed with a caveat, so it cannot be read the wrong way round.

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
- Reframed the guidance so explicit, publisher-triggered loading is the default, with eager preloading documented as an optional pattern. Applies to `references/rewarded-api.md` and the loading-strategy guidance in the skill file.

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
