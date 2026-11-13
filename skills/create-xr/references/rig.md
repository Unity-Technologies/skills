# Rig procedure

Relative paths in this file resolve to the skill directory.

## Table of Contents

- [Docs](#docs)
- [1. Pre-flight](#1-pre-flight)
- [2. Prerequisites](#2-prerequisites)
- [3. Place the rig](#3-place-the-rig)
- [4. Configure](#4-configure)
- [5. Validate](#5-validate)
- [6. For the final report](#6-for-the-final-report)
- [Rule](#rule)

Add a configured XR rig to the open scene: import the XR Interaction Toolkit Starter Assets rig prefab, place it as a linked prefab instance, and configure input, interactions, and locomotion from the user's answers. Requires the setup procedure first: if `status.sh` hasn't run in this session, run it now, and run setup if it reads todo. Read `references/rig-pitfalls.md` first.

## Docs

- `com.unity.xr.interaction.toolkit`: `samples-starter-assets.md` (the rig prefabs), `near-far-interactor.md`, `locomotion.md`, `teleportation.md`, `continuous-movement.md`, `snap-turning.md`, `interaction-layers.md` (the Teleport layer).
- `com.unity.xr.hands` (hand tracking): `project-setup/install-xrhands.md`, `openxr-features/metahandtrackingaim.md`; XR Interaction Toolkit `samples-hands-interaction-demo.md`.

## 1. Pre-flight

Only surface these to the user if something is wrong.

1. **Open scene.** If no scene is open, open the project's first scene in the build list, or ask the user which scene to use.
2. **Existing rig.** If the scene already contains an XR Origin, do not modify it — even if it looks like an unmodified Starter Assets rig, and even if the user asks you to change their rig (e.g., "add teleport to my rig"). Don't offer to edit it. Ask: replace it (deactivate the existing rig and place a new one), or put the new rig in a new scene. Only deactivate the existing rig; don't rename or change it. If the names need to differ, rename the new rig. Replacing breaks any references the user's objects hold to the old rig — say so.
3. **Active Input Handling.** If it is set to the old Input Manager only, set it to Both (recipe in rig-recipes.md) and tell the user the Editor must restart before the rig receives input.
4. **XR Interaction Toolkit.** Setup installs it. If it is missing (e.g., the project was set up without this skill), install it with the shared helper (skips it if already installed, waits for the reload, stops on a blocking dialog):

   ```
   source "<SKILL_DIR>/scripts/_eval.sh" && install_package com.unity.xr.interaction.toolkit
   ```
5. **Versions.** Read the installed XR Interaction Toolkit version. Sample versions must match it.

## 2. Prerequisites

1. **If hand tracking was chosen:** setup has already installed XR Hands and enabled the OpenXR hand features (`setup.sh --hands`). If `com.unity.xr.hands` is not installed — e.g., setup ran earlier without `--hands` — re-run setup with the same devices plus `--hands`; it skips everything already done.
2. **TMP Essentials, before the samples.** Starter Assets requires them, and importing the samples first opens the TMP importer window. If `Assets/TextMesh Pro` does not exist, import them with the recipe and wait until the folder exists.
3. **Import samples** with the sample import recipe — only these, matched by exact display name:
   - XR Interaction Toolkit **Starter Assets** (always)
   - XR Hands **HandVisualizer** and XR Interaction Toolkit **Hands Interaction Demo** (hand tracking only)

   If a sample is already imported for a different package version, ask before updating it.
4. Wait for the recompile to finish before continuing.
5. **Simulator — only if the user asks for it.** Import the XRI **XR Interaction Simulator** sample (exact name) with the sample import recipe, then turn on XRI's "Use XR Interaction Simulator in scenes" setting with the simulator recipe. It then appears automatically in Editor Play mode only — tell the user. Never use the XR Device Simulator.

## 3. Place the rig

1. **Find the prefab** in the imported sample folder with the recipe — the folder path includes the version, so do not hardcode it:
   - Controllers only → **XR Origin (XR Rig)**
   - With hand tracking → **XR Origin Hands (XR Rig)**
2. **Place it** with the Pipeline's prefab-instantiate command. Confirm it is a linked prefab instance.
3. **The scene's own Main Camera.** Target it by hierarchy path `/Main Camera` — the rig has its own child named "Main Camera". Use the template camera recipe:
   - **Untouched template camera** (only Transform, Camera, AudioListener, URP camera data; no children): disable it and tell the user.
   - **Customized camera:** ask before disabling it.
4. Save the scene.

## 4. Configure

Every change is a prefab override. Use the Pipeline's command for setting component properties (it writes serialized fields by name and accepts `null` to clear a reference); fall back to the `eval` recipe if the catalog has none. Target components by hierarchy path, e.g. `--target '{"hierarchyPath":"/XR Origin (XR Rig)/Camera Offset/Right Controller"}' --type ControllerInputActionManager`; for the hands rig the root is `XR Origin Hands (XR Rig)`. See `references/rig-structure.md` for where each component lives and how locomotion works.

### Interactions (each controller)

| Not chosen | Change |
|---|---|
| Near | Near-Far Interactor: near casting off |
| Far | Near-Far Interactor: far casting off |
| Poke | Disable the Poke Interactor GameObject |

**With hand tracking, apply the same choices to each hand** (`Camera Offset/Left Hand`, `Camera Offset/Right Hand`), with two differences:

- **No poke on a hand:** disable the `XRPokeInteractor` component on the hand's Poke Interactor — not its GameObject, which the hand re-activates (rig-pitfalls.md).
- **Far or poke off on a hand:** also disable the hand's `PokeGestureDetector`, or far casting turns back on after a poke gesture.

### Hand tracking only

Hide the controllers and stop their input from moving the user:

1. On the rig root's `XRInputModalityManager`, clear `m_LeftController` and `m_RightController` (`null`).
2. Deactivate the `Left Controller` and `Right Controller` GameObjects.
3. Set the controller inputs on the locomotion providers to Unused (`m_InputSourceMode` = 0) — with the controllers inactive, nothing else turns their stick actions off, so a picked-up controller would still move and turn the user:
   - `Locomotion/Move` `DynamicMoveProvider`: `m_LeftHandMoveInput`, `m_RightHandMoveInput`
   - `Locomotion/Turn` `SnapTurnProvider` and `ContinuousTurnProvider`: `m_LeftHandTurnInput`, `m_RightHandTurnInput`
   - `Locomotion/Jump` `JumpProvider`: `m_JumpInput`

   These are nested fields: use the dotted path, e.g. `m_LeftHandMoveInput.m_InputSourceMode`. Keep the providers enabled. Skip the locomotion and turning tables below.

### Locomotion (each controller's `ControllerInputActionManager`)

`m_SmoothMotionEnabled` sets each thumbstick's job: `true` = move and strafe (no turning); `false` = teleport (push forward) and turn (left/right).

| Movement | Left `m_SmoothMotionEnabled` | Right `m_SmoothMotionEnabled` | Also |
|---|---|---|---|
| Teleport | false | false | — |
| Continuous | true | false | Right: clear `m_TeleportMode` and `m_TeleportModeCancel`; disable `TeleportationProvider` |
| Both (prefab default) | true | false | — |
| Stationary | false | false | Both: clear `m_TeleportMode` and `m_TeleportModeCancel`; disable `TeleportationProvider` and the Move provider |

Unless the user asks otherwise, Stationary keeps turning.

**Continuous with no turning:** the right stick has no other job, so make it a move stick too — right `m_SmoothMotionEnabled` = true, in addition to the Continuous row's changes.

### Turning (both controllers)

Set the turn type on both controllers, even a move stick, so it holds if the user later switches a stick to teleport and turn.

- **Snap:** `m_SmoothTurnEnabled` = false
- **Continuous:** `m_SmoothTurnEnabled` = true
- **None:** disable `SnapTurnProvider` and `ContinuousTurnProvider`

Leave the Climb, Gravity, Jump, and Grab Move providers as the prefab has them. Save the scene.

## 5. Validate

1. **Rig read-back.** Run the rig read-back recipe and compare against the user's answers: the rig is linked, exactly one active camera and AudioListener, per-controller move/turn values and teleport input references, interactors and providers on or off as expected. In edit mode the Teleport Interactor shows as active — this is expected; it switches off when Play mode starts.
2. **Starter Assets project validation items.** TMP Essentials must be present (step 2). If teleport is on, interaction layer 31 must be named `Teleport` — set it with the recipe in rig-recipes.md if it is not.

## 6. For the final report

- **If far and poke are both off**, warn that world-space UI cannot be used without one of them.
- List every override this step made (the skill's final report covers how to revert them).
- **If the scene had nothing but the template objects and the rig**, offer example content to try the rig: a floor to teleport and move on, objects to grab, and a button to poke. For hand tracking only, describe the floor as just a floor: hands can't teleport or move on it. Add it only if the user says yes (recipe in rig-recipes.md).

## Rule

Start from the Starter Assets prefab and change it only by overrides: never build a rig from components, unpack the prefab, or remove components — disable or change them instead, so the user can revert every change.
