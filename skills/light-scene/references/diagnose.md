# Diagnose scene lighting

Diagnose a URP scene whose lighting looks wrong, repair the whole setup rather than the named symptom, and verify the repair by reading project state.

Relative paths in this file, such as `references/terms.md`, resolve to the skill's root folder, not to this file's folder or the project. Other workflows are named by workflow, such as "the bake workflow"; the skill's workflow table gives each one's file.

## Contents

- Success criteria
- Important for this workflow
- Project conventions
- Confirm this is the right workflow
- Gather project context
- Narrow down where the problem lives
- Complete the setup, not the symptom
- Work the known causes before escalating
- Anti-hallucination constraints
- Terms
- Worked example
- Verify
- Report

## Success criteria

- The one cause is named, and the fix changes only what was broken: the moved value in a regression, or the earliest missing stage in an unfinished setup.
- No authored value that was not the symptom is changed; anything else that looks wrong is reported and left.
- Every claimed mechanism is tested against the render before it is stated - a "clips to white" is shown by counting clipped pixels, not asserted.
- Brightness, shadows or the reported symptom are visibly restored, and the render is inspected to confirm it.
- Any shared asset the fix touches is named as shared before it is edited.

## Important for this workflow

- Do only what's asked. You MUST NOT change unrelated assets or files, because lighting settings are shared across scenes and an unrelated edit is hard for the user to spot.
- A component cannot be destroyed while another on the same GameObject declares `[RequireComponent]` on it, and Unity refuses without throwing an exception, so a `script eval` snippet that tries it still appears to succeed. URP attaches `UniversalAdditionalLightData` to every Light, so removing a Light means removing that first. Re-read the count afterwards: a destroy that quietly did nothing looks identical to one that worked.
- WAIT for the user's decision whenever a repair needs a look or an intent they have not stated, because there is no correct value to guess.

## Project conventions

Unity's manual describes how lighting works. It cannot tell you what THIS project has already
decided. Before changing any value:

1. Search the project for its own documentation on lighting, scene composition and art
   direction - for example a `Docs` folder, a style guide, or a technical design document - and
   read what it says about lighting.
2. Open a sibling scene that is already lit and read its rig: how many lights, their colour,
   intensity and mode, the ambient source, whether fog is used, and whether a Volume is bound.
   Read its shadow and bake policy too: how many lights cast shadows, and whether the scene is
   lit by a bake. Those two decide each other - a baked scene pays no runtime cost for shadows,
   so it can afford far more shadow-casting lights than a realtime rig, and copying a realtime
   rig's shadow budget into a baked scene silently throws most of its shadows away.
   Match the project's existing structure unless you have a stated reason not to.
3. Where a project convention and this workflow disagree, the project wins. Name the document you
   followed in your response.
4. If the project documents a scene-lighting ownership rule, obey it. A scene with no lights is
   sometimes deliberate, and "fixing" it can silently break the arrangement.

Copying a sibling scene is not the same as agreeing with it. Read its values, then judge them
against the scene in front of you - an intensity tuned for textured geometry can clip flat
blockout albedos.

Do this because a setup that is correct in general can still be wrong for a project that has
already decided these things - the most common reason this work gets rejected.

## 1. Confirm this is the right workflow

| Request | Handle here? |
|---|---|
| A scene looks wrong, or shadows are missing or blocky | Yes |
| A scene has no lighting yet and needs some | No - use the set-up workflow |
| Run or configure a bake, with no problem reported | No - use the bake workflow |
| The setup is correct and the user wants a different look | No - use the style workflow |
| Materials render magenta, or a custom shader receives no light | No - use the materials workflow |
| The skybox, environment reflections or fog are the problem | No - use the environment workflow |

### Match the effort to what was asked

Decide this before gathering anything, because it determines how much gathering is warranted.

| What the user asked | What to deliver |
|---|---|
| A question, with no change requested - "why is X like this", "dont change anything" | Read only what is needed to name the cause, answer at the length they asked for, and change nothing. Do not render the scene to answer a question about a setting you can read |
| A symptom that appeared after a change they name - "since i changed the sun" | A regression. Find what moved, put it back, and stop. See section 4 |
| A symptom with no known trigger, or a scene that was never finished | A full diagnosis. Work sections 3 to 6 |

## 2. Gather project context

Change nothing during this step.

1. Read the Unity version and the active render pipeline asset.
2. Check whether the current quality level overrides the pipeline asset, per [Set the render pipeline asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/srp-setting-render-pipeline-asset.md). Do this because a project can render through a different pipeline asset per quality level, so the Graphics Settings default is not always what is active.
3. Name the active pipeline. The steps below are written for URP; on Built-in or HDRP apply the substitutions in `references/pipeline-notes.md` as you go - the shape of the work is the same, the places the settings live are not.
4. Find every light in the open scene using the query `t:Light`.
5. In a single call, read each light's type, intensity, colour, shadow mode, range and light mode, the scene's Environment Lighting Source and Intensity Multiplier, and each Volume in effect - the scene's own and, on URP, the two default Volumes every scene has, per [Understand volumes in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes.md) - with its Color Adjustments Post Exposure and its Tonemapping mode, and their `overrideState`. Use one call because each round trip is slow, and read the Volumes here because exposure, not a light, is a common cause of a scene that is dark everywhere, and it can sit in a default Volume when the scene has none.
6. Report what exists before proposing any change, so the user can correct a wrong assumption early.

## 3. Narrow down where the problem lives

1. Find out whether the problem appears in the Scene view, the Game view or a build. If the user has not said, capture the Game view first and ask only if the capture does not settle it, because a capture answers the question faster than the user can.
2. If it appears only in the Scene view, check the Scene view's own View Options, because those toggles affect the Scene view alone and not the built game.
3. If it appears only in the Game view, inspect the active camera and any post-processing volume, because tonemapping and colour grading change the image without changing the lighting. Check whether a Tonemapping override exists at all: with mode None, anything above 1.0 clips hard to white, so a scene that reads blown out may be correctly lit and merely uncompressed.
   Read each override's `overrideState` as well as its value: an override that is switched off
   is not in effect however plausible its number looks, and one that is on with a large value can
   be the whole fault.
4. If it appears in both views, do NOT conclude the cause is a light. Check the URP asset and any global Volume first, because shadow distance, cascade count, additional-light limits and a Volume with a default layer mask all affect both views and are neither a camera nor a scene light.
5. If it appears only in a build, go to section 5.

## 4. Complete the setup, not the symptom

A lighting problem usually spans shadows, baking and exposure together, so a repair that addresses only the named symptom tends to leave the scene wrong.

### Unless it is a regression

When the user names the change they made, the scene worked before it and the rest of this section
does not apply. Undo the damage; do not improve the scene.

1. Identify the one thing that moved. Everything else is authored and deliberate.
2. Restore only that. If the original value is unrecoverable - the scene on disk no longer holds
   it and the project has no version control - say so plainly, then take a replacement from the
   project's own sibling scenes rather than from Unity's defaults.
3. Values you believe are wrong but that were NOT part of the reported change: report them and
   leave them. Offer to fix them separately.

   The one exception: a value that is **zero** while the project's authored value and every
   sibling scene are non-zero, and that sits alongside the reported symptom - an ambient
   intensity of 0 next to a missing skybox, say - is part of the damage, not a tuning choice.
   Restore it and say why. Left alone, it changes nothing visible until the next bake, which it then makes wrong.

NEVER change a value that is neither the reported symptom nor demonstrably broken. A value that
differs from Unity's default is usually one somebody tuned deliberately.

1. Work through [Lighting configuration workflow](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-configuration-workflow.md) and identify every stage that is missing or unapplied.
2. Say which on-disk assets a fix will touch before making it. `LightProbes.bakedProbes` writes
   to `LightingData.asset` the moment it is assigned, and `SerializedObject.ApplyModifiedProperties`
   on a pipeline, quality or Volume asset marks it dirty so the Editor saves it on exit. Declining
   to save the scene undoes neither. Prefer an in-memory change when the user asked only to see a
   result.
3. Fix the earliest missing stage first, because later stages depend on it. A bake means nothing until light modes and ContributeGI flags are right.
4. After each change, re-read the affected value rather than assuming the write applied.
5. NEVER report a bake as done until `UnityEditor.Lightmapping.Bake()` has returned `true` and you have read the lightmap count from project state, because a failed bake leaves the previous result in place.

## 5. Work the known causes before escalating

| Symptom | Work these first | Escalate only when |
|---|---|---|
| Editor crashes during Generate Lighting | Switch baking backend per [Choose a light baking backend](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/configure/progressive-lightmapper.md), then lower lightmap resolution and atlas size, update GPU drivers, and clear the GI cache per [No baked global illumination](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/troubleshooting/ts-no-baked-global-illum.md) | Every backend has been tried and the crash reproduces. Report it as an engine issue and stop |
| Lighting correct in the editor, wrong in a build | Treat this as a setup problem, not a build problem. Check that the scene has a committed lighting data asset from an explicit bake, per the [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md), that it is in Build Settings, and quality-level differences | The setup is verified complete and the divergence still reproduces |
| Screen goes black at some viewing angles and renders at others | Read the camera's far clip plane, the fog end distance and whether occlusion culling data is baked for this scene, in that order, because a light cannot make a view direction black. Fix the one that is wrong and re-render from the failing angle | All three read sane and the blackout still reproduces from the same angle |

You MUST NOT skip the first column, because escalating a fixable case wastes the user's time.

## 6. Anti-hallucination constraints

| Rule | Reason |
|---|---|
| Read every value before changing it | Lighting values are frequently non-default and cannot be inferred |
| Distinguish Baked Global Illumination being enabled from a bake having been run | The first is a setting; only the second produces lightmaps |
| Read the active pipeline asset and its quality-level override, rather than inferring the pipeline from folder names | Only the resolved asset determines behaviour |
| NEVER set a Volume override parameter's `.value` without setting `.overrideState = true` first | A parameter whose `overrideState` is false is ignored entirely: the profile displays the value and the render does not change. The result looks like a pipeline fault, which it is not |
| Say which on-disk assets a change touches before making it | `LightProbes.bakedProbes` writes to `LightingData.asset` the moment it is assigned, and `SerializedObject.ApplyModifiedProperties` on a pipeline, quality or Volume asset marks it dirty so the Editor saves it on exit. Declining to save the scene undoes neither |
| For Volume scripting beyond setting a value, follow the `urp-postprocessing` skill | Edit `volume.sharedProfile` (the asset, so it persists), not `volume.profile` (a runtime clone that is never saved), and an override added in editor code needs `AssetDatabase.AddObjectToAsset` to survive a reload - otherwise the grade shows in the Inspector and vanishes when the asset reloads |

## 7. Terms

Terms this workflow uses in a specific sense - key light, ambient, Contribute GI, light mode and the rest - are defined in `references/terms.md`.

## 8. Worked example

Reported: "My scene is completely black when I press play."

1. Context: Unity 6000.6, `PC_RPAsset` active, no quality-level override.
2. `t:Light` returns zero lights. Environment Lighting Source is Color at an Intensity Multiplier of 0.02.
3. Report: the scene has no lights at all, and ambient is almost black.
4. Fix the earliest missing stage - a light source - by adding a directional light angled down, with shadows enabled and a sun-like intensity.
5. Re-read the light to confirm the write applied.
6. Do NOT raise the Intensity Multiplier as the fix, because that hides a scene with no working light rather than repairing it.
7. Verify: zero console errors, then capture the Game view and look at it. Adding a light is not runtime behaviour, so Play mode is not needed.

## Verify

Because the reported symptom is visual, state which values you verified and ask the user to confirm the appearance. You MUST NOT claim a visual result is correct from state reads alone.

If a check fails, go back and reread the relevant documentation from `references/topic-map.md` for what you missed. The cause is more often a skipped stage of the configuration workflow than a wrong value.

## Report

Include these in your final report:

- What the lighting state was before the change.
- Which stages of the configuration workflow were missing, and what completed them.
- The documentation pages you read, by URL.
- What you verified from state, and what still needs the user's eyes.
- Anything escalated as an engine issue.
