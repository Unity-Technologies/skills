---
name: android-add-adaptive-performance
description: Implements Unity Adaptive Performance for Android (Unity >= 6.0 / 6000.0.0) by
  handling hardware thermal/power signals and mapping them to quality tiers with dynamic
  graphics and simulation quality adjustments. Use when the user asks about adaptive
  performance, thermal throttling, dynamic quality scaling, FPS drops on Android, or
  optimizing Android game performance.
required_editor_version: ">=6000.0.0"
required_packages:
  com.unity.adaptiveperformance: ">=4.0.0"
---

## Quick Start

Adds a full Adaptive Performance integration to a Unity Android project. Creates two MonoBehaviours (`AdaptivePerformanceSignalManager` and `AdaptiveQualityAdapter`), optionally configures URP quality settings, and bootstraps the system into the user's chosen scene.

## Critical Rules

- Do not make any subjective calls — ask the user when in doubt
- Follow steps in strict order; never jump ahead
- STOP at every `WAIT` checkpoint and await the user's response before continuing
- Do not install any packages
- Do not add the bootstrap GameObject until all scripts are written and in place
- Use `AdaptivePerformanceIntegration` as the namespace for all generated code
- Do not create placeholder files when creating folders

## Workflow

### Step 1: Gather Required Information

Do not output any code until all answers are collected.

1. Attempt to detect the graphics pipeline (URP / HDRP / Built-in). If there is any doubt or you cannot determine it, ask the user directly.
2. Ask the user how many quality tiers they want. Default is 3 (Best / Medium / Low). Determine appropriate tier names yourself based on their answer.
3. Ask whether quality should snap back to Best when conditions normalize, or use a sticky downgrade (stay at the lowest tier reached for the session, preventing oscillation).

**WAIT for the user to answer all three questions before proceeding.**

### Step 2: Create URP Quality Settings (skip entirely if not URP)

- Create `mobile_adaptive` and `mobile_max` quality settings, configured to affect Android only.
- For each, create a matching URP Render Pipeline Asset under `Assets/AdaptivePerformanceManager/Settings/`:
  - `urp_mobile_adaptive` — Enable Adaptive Performance: **ON**
  - `urp_mobile_max` — Enable Adaptive Performance: **OFF**

Tell the user what was created, then continue.

### Step 3: Generate the Signal Handler Script

Copy `AdaptivePerformanceSignalManager` from `resources/AdaptivePerformanceSignalManager.cs` into `Assets/AdaptivePerformanceManager/`. Adjust tier count and names to match the user's answers from Step 1.

Tell the user the script has been added, then continue.

### Step 4: Generate the Quality Adapter Script

Copy `AdaptiveQualityAdapter` from `resources/AdaptiveQualityAdapter.cs` into `Assets/AdaptivePerformanceManager/`. Apply all scalers — never skip any. Add a `// HIGH VISUAL IMPACT` comment above any scaler line that may visually disrupt gameplay (leave the code intact).

For FPS targets, use display Hz divisors:
- 60 Hz displays: 60, 30 FPS
- 90 Hz displays: 90, 45, 30 FPS
- 120 Hz displays: 120, 60, 40, 30 FPS

See `resources/ADAPTIVE_PERFORMANCE_PLAN.md` for the full scaler table and URP-specific scalers.

Tell the user both scripts are ready, then continue.

### Step 5: Add Bootstrap GameObject

Ask the user which scene to place the `AdaptivePerformanceSignalManager` GameObject in.

**WAIT for the user to respond before continuing.**

1. Add the GameObject to the specified scene.
2. Attach `AdaptivePerformanceSignalManager` and `AdaptiveQualityAdapter` components to it and configure all references.
3. Save the scene.

### Step 6: Produce a Final Checklist

List everything that was done, then list what the user must do manually:

- Go to **Project Settings → Adaptive Performance** and enable "Enable Adaptive Performance" if not already checked.
- In the Providers section, check **Android Provider**.
- If new Quality Settings were added, review and adjust each one's settings and verify the URP Render Pipeline Assets are configured correctly.
- `AdaptivePerformanceSignalManager.cs` is the primary script to customize tier logic.
- Optional: `AdaptiveLayerCulling` via `Camera.main.layerCullDistances` is available but requires detailed per-project setup by the user.
- Optional: Post-processing `VolumeProfiles` in scene Volumes are not controlled by this integration and can be tuned manually for additional adaptive gains.

## Detailed References

- **Full implementation plan and scaler tables:** [resources/ADAPTIVE_PERFORMANCE_PLAN.md](resources/ADAPTIVE_PERFORMANCE_PLAN.md)
- **Signal handler template:** [resources/AdaptivePerformanceSignalManager.cs](resources/AdaptivePerformanceSignalManager.cs)
- **Quality adapter template:** [resources/AdaptiveQualityAdapter.cs](resources/AdaptiveQualityAdapter.cs)
