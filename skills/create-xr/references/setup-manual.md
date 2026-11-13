# Setup by hand

Relative paths in this file resolve to the skill directory.

Only when the setup scripts cannot be used. Read `references/eval-guide.md` first.

**Scripts:** step N below is `scripts/setup/N_*.sh` (e.g. step 4 = `4_openxr.sh`). For Quest, PC VR, and Android XR, run that script rather than doing the step by hand. Reuse the C# in the step's script where it applies.

## 1. Platform modules

Script: `1_modules.sh`

Each target platform needs its build support module; the device package's get-started page names it. If one is missing, stop. Offer `unity install-modules --editor-version <editor version> -m <module ids>` (IDs from `--list`; some include a version number) and ask first — it is a large download and the Editor may need a restart.

## 2. Packages

Script: `2_packages.sh`

**First, Active Input Handling.** If it is Input Manager (old) only, set it to Both before installing anything — the Input System otherwise opens a dialog that blocks the Editor. Takes effect after an Editor restart; tell the user.

Install one at a time with `source "<SKILL_DIR>/scripts/_eval.sh" && install_package <id>` (it skips installed packages).

Always install `com.unity.xr.management`, `com.unity.xr.openxr`, and `com.unity.inputsystem`. Add the device package: `com.unity.xr.meta-openxr` for Quest; `com.unity.xr.androidxr-openxr` for Android XR (it brings AR Foundation as a dependency).

Also install `com.unity.xr.interaction.toolkit`, and `com.unity.xr.hands` if the user wants hand tracking. The rig procedure imports the samples.

## 3. Build profiles

Script: `3_build_profile.sh`

Use the device's own Build Profile platform if the Editor lists one (Meta Quest), otherwise its base platform (Android, Windows). See the Unity Manual's `building-and-publishing/build-settings/build-profiles.md`.

1. **Create missing profiles** with the catalog's create-profile command (`--name <name> --platform <id from list-platforms>`), non-primary ones with `--activate false`. Never pass `--confirm true` — it overwrites a profile of the same name. No create command → ask the user to add it in **File > Build Profiles**.
2. **Activate the primary device's profile** (the first device named), unless already active:
   - Switch the build target first (`--target <BuildTarget> --confirm true`), then poll its status command until `"status":"completed"` with `"success":true`. Pipeline refuses to activate a profile that needs a different target.
   - New profile: create it with `--activate true`. Existing profile: there is no CLI command — activate it with `eval` (`BuildProfile.SetActiveBuildProfile`). Never recreate a profile to activate it.
   - Confirm `"isActive":true` in the profile list, then set `EditorUserBuildSettings.selectedBuildTargetGroup` to the active target's group — activating from code leaves it out of sync.

Quest-only needs no Windows profile — the Standalone OpenXR tab (Quest Link) works without one.

## 4. OpenXR

Script: `4_openxr.sh`

Use OpenXR unless the user asks for another plug-in. Settings are per tab: Android (Quest on-device, Android XR) and Standalone (PC VR, Quest Link). Enable the loader on each tab, then the device's support feature and controller profiles. Read the OpenXR package's `project-configuration.md` and `features/metaquest.md`, and for Android XR its package's `get-started/project-setup.md`. If the user wants hand tracking, enable the hand-tracking features here too.

## 5. Player settings

Script: `5_player_settings.sh`

Set them with `eval` naming the platform explicitly — never the Pipeline's player-settings command, which writes to the platform selected in the Build Profiles window, not the active one. Do this after step 3.

Use the values in the Meta OpenXR package's `get-started/project-settings.md` (Quest) or the Android XR package's `get-started/project-setup.md`.

After the last change, mark the active Build Profile dirty and call `AssetDatabase.SaveAssets()`: while a Build Profile is active, player settings live in the profile asset and are lost on restart unless it is saved.

## 6. Validation

Script: `6_validate.sh`

Read back the active profile, the OpenXR loader and features per tab, and the installed packages. Note the results for the final report.
