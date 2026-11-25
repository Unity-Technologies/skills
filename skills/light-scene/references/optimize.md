# Optimize lighting for a platform

Cut the runtime cost of lighting for a stated target platform, changing the cheapest settings first and saying what each one costs in appearance.

Relative paths in this file, such as `references/terms.md`, resolve to the skill's root folder, not to this file's folder or the project. Other workflows are named by workflow, such as "the bake workflow"; the skill's workflow table gives each one's file.

## Contents

- Success criteria
- Important for this workflow
- Project conventions
- Confirm this is the right workflow
- Find out what is actually running
- Change in cost-effectiveness order
- Constraints
- Anti-hallucination constraints
- Terms
- Worked example
- Verify
- Report

## Success criteria

- The pipeline asset the target platform actually resolves to is the one read and edited, not the one the Editor happens to be using.
- Changes are made in cost-effectiveness order, one at a time, each re-read after writing.
- The visual cost of each change is stated so the art team can evaluate it.
- Lights are not deleted; realtime shadow casters and limits are reduced instead, and baked lights are recognised as already free.
- No frame-time gain is claimed that was not measured.

## Important for this workflow

- Do only what's asked. Pipeline assets and Quality Settings are shared by every scene in the project, so a change here is never scene-local. You MUST say which scenes are affected before editing one.
- WAIT for the user to name the target platform and an acceptable quality loss. Every change here trades appearance for cost, and without a target there is no way to judge the trade.

## Project conventions

Unity's manual describes what each setting does. It cannot tell you which quality tier THIS project ships to which platform. Before changing any value:

1. Search the project for its own documentation on target platforms, performance budgets and quality tiers - for example a `Docs` folder or a technical specification - and read it.
2. Read every quality level and which pipeline asset each one resolves to. A project often has a tuned mobile asset that nothing actually references, which means the mobile build is silently using desktop settings.
3. Where a project convention and this workflow disagree, the project wins. Name the document you followed in your response.
4. Check what else references a pipeline asset before editing it. Editing the wrong one changes every scene and every platform.

Do this because the most valuable finding here is usually a wiring mistake, not a value that needs lowering.

## 1. Confirm this is the right workflow

| Request | Handle here? |
|---|---|
| Lighting is too expensive, or the frame rate drops on a platform | Yes |
| Tune lighting for mobile, or for a specific quality tier | Yes |
| A scene has no lighting yet | No - use the set-up workflow |
| A scene looks wrong or shadows are missing | No - use the diagnose workflow |
| The user wants a different mood or time of day | No - use the style workflow |
| Bake lighting to reduce realtime cost | Partly - decide the strategy here, then hand the bake to the bake workflow |

## 2. Find out what is actually running

Change nothing during this step. Report what you find before proposing any change.

1. If the request does not name the target platform and the quality loss the user accepts, ask for both and WAIT for the answer, because the rest of this section reads that platform's settings.
2. List every quality level and the pipeline asset each resolves to, per [Set the render pipeline asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/srp-setting-render-pipeline-asset.md).
3. Check for a pipeline asset that exists but is referenced by nothing. That is a common and expensive mistake: a tuned mobile asset that no quality level points at means the mobile build ships desktop settings.
4. For the asset the target platform actually resolves to, read shadow distance, cascade count, shadow resolution, soft shadow setting, additional-light limit, render scale and rendering path.
5. Read the scene's realtime light count and how many cast shadows.

## 3. Change in cost-effectiveness order

Work down this list in order, because the early entries usually give up the least visible quality for what they cost; stopping early is a valid outcome. The order is guidance, not a measurement, so never describe a change in your report as saving frame time, or as the biggest saving.

| Order | Change | What it costs you |
|---|---|---|
| 1 | Point the target platform's quality level at the correct pipeline asset | Nothing, if one was mis-wired. This is a fix, not a trade |
| 2 | Reduce shadow distance | Distant shadows disappear |
| 3 | Reduce cascade count | Shadow quality at distance. Pairs naturally with a shorter shadow distance |
| 4 | Soft Shadows to Hard | Shadow edges harden. Significant on tile-based mobile, per [Optimize shadow rendering in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/shadows-in-urp/shadows-optimization.md) |
| 5 | Reduce shadow resolution | Shadow crispness everywhere |
| 6 | Lower the additional-light limit, or stop secondary lights casting realtime shadows: switch a light that never moves or changes from Mixed to Baked with its shadows on and re-bake, or turn off additional-light shadows on the target tier's pipeline asset | Moving objects lose those lights' realtime shadows and highlights, and lights past the limit are downgraded |
| 7 | Move static lighting to baked | Iteration time, and a bake becomes required. Run it as the bake workflow describes |
| 8 | Reduce render scale | Overall image sharpness. Affects everything, not just lighting |

When the user has named the target platform and the loss they accept, make the first change in this table that stays inside this scene in the same pass, and list the changes to shared assets for approval, each with its visual cost from the table. If the limit the user asked for is already met on their target, say so and still make that change, because they asked for the cost to come down.

After each change, re-read the value rather than assuming the write applied, and state what it cost visually.

## 4. Constraints

| Rule | Reason |
|---|---|
| NEVER edit a pipeline asset without first saying which quality levels and scenes use it | These assets are project-wide, and the change will reach every scene |
| NEVER tune the asset the editor happens to be using | The editor may resolve to a different quality level than the target platform |
| ALWAYS state the appearance cost of each change | An optimisation the art team cannot evaluate will be reverted |
| Change one setting at a time and re-read it | Several changes at once make it impossible to attribute the saving |
| NEVER set a Mixed light's Shadow Type to None to save shadow cost | On a quality level that renders additional-light shadows, the light also loses the shadows the bake stored for it, so its light spills into areas that were shadowed. The next bake gives it no shadowmask channel, so every tier loses them. Switch a light that never moves or changes to Baked with its shadows on instead, or reduce shadows on the target tier's pipeline asset |
| NEVER switch a light to Baked while anything changes it at runtime | A Baked light gives no realtime direct light, so a script or animation that changes its intensity, colour or enabled state stops having any visible effect. Search the project's scripts and animation clips for anything that drives the light first, and keep a changing light Mixed or Realtime |
| Do not claim a performance gain you have not measured | You can state what a setting costs in principle; you cannot state the frame time without profiling |
| Say when a change is written to disk | `SerializedObject.ApplyModifiedProperties` on a pipeline or quality asset marks it dirty, and the Editor saves it on exit even if the scene is never saved. That is the right API for an approved change and the wrong one for an experiment |

## 5. Anti-hallucination constraints

| Rule | Reason |
|---|---|
| Read the resolved pipeline asset, not the Graphics Settings default | A quality level can override it, so the default is often not what runs |
| Do not quote frame-time numbers for a setting | Cost depends on scene, resolution and hardware, and an invented figure is worse than none |
| Say which on-disk assets a change touches before making it | `LightProbes.bakedProbes` writes to `LightingData.asset` the moment it is assigned, and `SerializedObject.ApplyModifiedProperties` on a pipeline, quality or Volume asset marks it dirty so the Editor saves it on exit. Declining to save the scene undoes neither |

## 6. Terms

Terms this workflow uses in a specific sense - key light, ambient, Contribute GI, light mode and the rest - are defined in `references/terms.md`.

## 7. Worked example

Requested: "Lighting is killing our frame rate on Android."

1. Read the quality levels. There is one level, "High", with no custom pipeline asset, so it inherits the desktop asset from Graphics Settings.
2. There is a mobile pipeline asset in the project that **nothing references**. Report this first: the Android build is running desktop lighting settings, and no amount of value tuning fixes that.
3. Ask whether the mobile asset is the intended target and what quality loss is acceptable. WAIT.
4. Wire the mobile quality level to the mobile asset. Re-read to confirm which asset now resolves.
5. Compare the two assets and report the differences that now apply - shadow distance 200 to 50, resolution 2048 to 1024, Soft to Hard shadows, render scale 1.0 to 0.8 - as appearance changes the art team should review.
6. Stop there and let them look, rather than tuning further in the same pass.

## Verify

If the user asked you to prove it works or you suspect a specific fault, do these after the Final step's console check and capture:

1. Read back which pipeline asset each quality level resolves to, and confirm the target platform's level points where you intended.
2. Capture with the target platform's quality level active, because the Editor's current level may resolve to a different pipeline asset than the one you edited.
3. If you changed a light rather than a pipeline asset, also capture with each other quality level that renders this scene active, because a light change that is invisible on the target tier can show on another.
4. Make the quality level that was active before these captures active again, because the Editor stores its active level in the project's quality settings, which the user did not ask you to change.

You MUST NOT claim a frame-rate improvement you have not measured. State what you changed and what it costs in appearance, and ask for a profiled comparison on the target device.

If a check fails, go back and reread the relevant documentation from `references/topic-map.md`. The most common mistake is tuning an asset the target platform does not resolve to.

## Report

Include these in your final report:

- Which pipeline asset each quality level resolved to before and after.
- Any asset that was mis-wired or unreferenced.
- Each setting you changed or propose to change, and what it costs visually.
- The documentation pages you read, by URL.
- That the saving needs profiling on the target device to be confirmed.
