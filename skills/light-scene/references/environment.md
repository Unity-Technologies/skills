# Configure environment lighting

Set up or repair the sky, ambient, reflections and fog of a URP scene, resolving which of the several places that can supply a background is actually supplying it, and verify the result by reading project state.

Relative paths in this file, such as `references/terms.md`, resolve to the skill's root folder, not to this file's folder or the project. Other workflows are named by workflow, such as "the bake workflow"; the skill's workflow table gives each one's file.

## Contents

- Success criteria
- Important for this workflow
- Rules for this workflow
- Project conventions
- Confirm this is the right workflow
- Gather project context
- Resolve which source is actually supplying the background
- Repair a sky that is missing or wrong
- Reflections
- Fog
- Anti-hallucination constraints
- Terms
- Worked example
- Verify
- Report

## Success criteria

- The source actually supplying the background - skybox material, camera clear colour, a Skybox component override, or a script - is identified before anything is changed.
- The skybox renders and also contributes ambient and reflection; the two paths are checked separately.
- Reflection probes have baked output or a working realtime state, verified from the render, not the Inspector.
- A value found at zero next to the reported fault is restored to the project's authored value, and the restoration is named.
- No Light component is changed; a sun that no longer suits the sky is reported, not adjusted.

## Important for this workflow

- Do only what's asked. You MUST NOT change unrelated assets or files, because environment settings are shared across scenes and an unrelated edit is hard for the user to spot.
- Leave every Light component as it is, because this workflow changes the environment only. If the sun's direction or colour no longer suits a new sky, say so and offer to change it.
- Environment settings live in several places that override each other - the scene's Lighting window, the camera, a Skybox component, and runtime script. Find which one is winning before you change any of them.
- WAIT for the user's decision whenever the choice is a look rather than a fault - which sky, how thick the fog - because there is no correct value to guess.

## Rules for this workflow

NEVER set the same background in two places to be sure it takes. INSTEAD fix the one that is winning, because a camera override and a scene value that disagree will diverge the next time either one is edited.

NEVER repeat an action that already failed with a larger value. INSTEAD stop and report what you tried, because raising skybox exposure or fog density to force a result hides the actual cause.

NEVER edit a skybox material, reflection probe or lighting settings asset shared by other scenes without saying so first. INSTEAD name the asset, name what else uses it, and ask.

## Project conventions

Unity's manual describes how environment lighting works. It cannot tell you what THIS project has
already decided. Before changing any value:

1. Search the project for its own documentation on lighting, scene composition and art
   direction - for example a `Docs` folder, a style guide, or a technical design document - and
   read what it says about skies, reflections and fog.
2. Open a sibling scene that already renders correctly and read its environment: which skybox
   material, the ambient source and intensity, the environment reflection source, whether fog is
   on and at what density, and how the camera's background is set. Match the project's existing
   structure unless you have a stated reason not to.
3. Where a project convention and this workflow disagree, the project wins. Name the document you
   followed in your response.
4. If the project loads skyboxes at runtime - from a `Resources` folder, a scriptable object or
   an addressable - the scene's authored skybox is not the one players see. Find the loading code
   before editing the scene, or your change will be overwritten on the first frame.

Copying a sibling scene is not the same as agreeing with it. Read its values, then judge them
against the scene in front of you - a fog density tuned for a corridor will erase an open
landscape.

Do this because a setup that is correct in general can still be wrong for a project that has
already decided these things - the most common reason this work gets rejected.

## 1. Confirm this is the right workflow

| Request | Handle here? |
|---|---|
| The skybox is missing, blue, black, blurry, or will not change | Yes |
| A surface does not reflect the sky, or a reflection probe does nothing | Yes |
| Fog is missing, or will not turn off | Yes |
| Ambient is the wrong source or intensity | Yes |
| The scene is dark or washed out and the sky is not implicated | No - use the diagnose workflow |
| A scene has no lighting at all yet | No - use the set-up workflow |
| A material renders magenta, or a custom shader ignores light | No - use the materials workflow |
| The sky is correct and the user wants a different mood or time of day | No - use the style workflow |
| Volumetric light shafts and god rays | Partly - see section 6 |

## 2. Gather project context

Change nothing during this step.

1. Read the Unity version and the active render pipeline asset.
2. Check whether the current quality level overrides the pipeline asset. Do this because a project can render through a different pipeline asset per quality level, so the Graphics Settings default is not always what is active, and realtime reflection probes are a per-quality-level toggle.
3. Name the active pipeline. The steps below are written for URP; on Built-in or HDRP apply the substitutions in `references/pipeline-notes.md` as you go - the shape of the work is the same, the places the settings live are not.
4. In a single call, read the scene's Skybox Material, Sun Source, Environment Lighting Source and Intensity Multiplier, Environment Reflections source and resolution, and the fog settings. Use one call because each round trip is slow.
5. Find every camera and every reflection probe in the scene.
6. Report what exists before proposing any change, so the user can correct a wrong assumption early.

## 3. Resolve which source is actually supplying the background

Several things can put pixels behind the geometry, and the one that wins is not always the one the user edited. Establish the winner before changing anything.

1. Read the camera's Background Type per [URP camera component reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/cameras/urp/camera-components-reference/camera-component-reference.md). If it is Solid Color, the scene's skybox material is irrelevant to the Game view no matter what it is set to. A flat blue background is usually this, not a missing skybox.
2. Read the camera's Render Type. An Overlay camera does not clear, so the background comes from the Base camera in the stack.
3. Check for a `Skybox` component on the camera. Per [Use a skybox](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skyboxes-using.md) it overrides the scene's Lighting window skybox for that camera only, which is the usual reason an edit in the Lighting window does nothing.
4. Check whether anything assigns `RenderSettings.skybox` from script. Search the project for that property. Runtime assignment beats the authored scene value.
5. Only after all four, read the scene's Skybox Material in the Lighting window.

State which of these is winning in your report. You MUST NOT change more than one of them to make the sky appear, because that leaves the scene with two sources of truth and the next person cannot tell which is intended.

## 4. Repair a sky that is missing or wrong

| Symptom | Work these first |
|---|---|
| Flat blue or flat colour background | Camera Background Type, then the Skybox component override, per section 3 |
| Skybox Material is assigned but nothing renders | Check the material's shader is a skybox shader per [Skybox shaders](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skybox-shaders.md), and that its texture or cubemap slots are filled per [Skybox material reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skybox-material-reference.md) |
| Sky renders black | Check the skybox shader's Exposure and Tint, and check exposure or tonemapping on a Volume before touching the sky |
| Sky changes but ambient and reflections do not | After assigning `RenderSettings.skybox` at runtime, call [`UnityEngine.DynamicGI.UpdateEnvironment()`](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/dynamicgi/updateenvironment.md), because the assignment alone does not refresh them |
| Sky is visible in the editor and absent in a build | Confirm the skybox material is referenced by an asset that ships, or add its shader to Always Included Shaders per [Graphics settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-settings-reference/comp-manager-group/class-graphics-settings.md), because a build strips a shader nothing in it references |
| Sky looks low resolution | Read the source texture's import settings - max size, compression and mip generation - before changing any skybox property |

The `DynamicGI.UpdateEnvironment` row is the most common cause of a skybox that changes while ambient and reflections stay on the old sky. Check it before you conclude the assignment failed.

## 5. Reflections

1. Read the Environment Reflections source in the [Lighting window](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md).
2. If the scene shows no environment reflection, check what this version still bakes by default, per the [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md), before you add a probe.
3. For each reflection probe, read its baked output, or for a realtime probe whether the active quality level enables Realtime Reflection Probes, per [Quality settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-settings-reference/comp-manager-group/class-quality-settings.md).
4. Before concluding reflections are broken, read the material's Smoothness and Metallic. A rough dielectric surface reflects almost nothing, and that is correct behaviour rather than a fault. Report it as such rather than raising probe intensity to force a result.
5. Before you offer Screen Space Reflections, check the [render pipeline feature comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/feature-comparison.md) for the project's version, because it says whether URP supports them. Where it does not, reflections come only from the skybox and reflection probes.
6. For a reflection that is correct in the middle of a room and wrong at its edges, read box projection on the probe per [Reflection probe reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections/class-reflection-probe.md).

Work [Reflection probe troubleshooting](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/reflection-probes/troubleshooting.md) before escalating, because most no-reflection reports are one of its documented rows.

## 6. Fog

1. Read fog in the Lighting window of each scene involved, per the [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md), because fog is set per scene: a project-wide belief that fog is off is usually one scene's value someone left on.
2. If fog appears in the Editor and not in a build, read the Fog row of [Settings and keywords reference for stripping keywords in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/built-in/shaders-in-universalrp/shader-stripping/features-and-keywords.md) and check the settings it names before changing fog values.
3. If the built-in materials fog and a custom material does not, hand that material to the materials workflow, because each shader applies fog itself.
4. If the user asks for volumetric fog or light shafts, read the Volumetrics rows of the [render pipeline feature comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/feature-comparison.md) for the project's version. Where URP has none, say plainly that this needs a custom renderer feature or a third-party asset, and offer the approximations that do exist rather than tuning fog density towards a result it cannot reach. You MUST NOT imply a built-in setting will produce volumetrics.

## 7. Anti-hallucination constraints

| Rule | Reason |
|---|---|
| Read every value before changing it | Environment values are frequently non-default and cannot be inferred |
| Distinguish the sky being rendered from the sky contributing ambient and reflection | These are separate paths, and one can be correct while the other is stale |
| NEVER claim a reflection probe works without reading its baked output or its realtime state | A probe with no baked output looks identical in the Inspector to one with output |
| NEVER set a Volume override parameter's `.value` without setting `.overrideState = true` first | A parameter whose `overrideState` is false is ignored entirely: the profile displays the value and the render does not change. The result looks like a pipeline fault, which it is not |
| After `DynamicGI.UpdateEnvironment()` in Edit mode, check the default reflection as well as the ambient | The skybox reflection can keep mirroring the old sky after the ambient has updated. If it does, set `defaultReflectionMode` to Custom with an explicit cubemap |
| Read a reflection probe's texture after `ReflectionProbe.RenderProbe()` in Edit mode | The call can leave the probe without a texture outside Play mode; `Camera.RenderToCubemap` from the probe's position is the fallback |
| For Volume scripting beyond setting a value, follow the `urp-postprocessing` skill | Edit `volume.sharedProfile` (the asset, so it persists), not `volume.profile` (a runtime clone that is never saved), and an override added in editor code needs `AssetDatabase.AddObjectToAsset` to survive a reload - otherwise the grade shows in the Inspector and vanishes when the asset reloads |

## 8. Terms

Terms this workflow uses in a specific sense - key light, ambient, Contribute GI, light mode and the rest - are defined in `references/terms.md`.

## 9. Worked example

Reported: "the screen is all blue, there is no skybox from Resources/Skyboxes".

1. Context: Unity 6000.6, `PC_RPAsset` active, no quality-level override.
2. Resolve the source: the main camera's Background Type is Solid Color, set to blue. The scene's Skybox Material is assigned correctly.
3. A project search finds a start-up script assigning `RenderSettings.skybox` from `Resources/Skyboxes` with no follow-up call.
4. Report both findings before changing anything, because the user believes there is one fault and there are two.
5. Fix the camera's Background Type to Skybox - the one change that makes the sky visible.
6. Add `UnityEngine.DynamicGI.UpdateEnvironment()` after the runtime assignment, so ambient and reflections match the sky that was loaded.
7. Re-read both values to confirm the writes applied.
8. Verify: zero console errors, then read `RenderSettings.skybox` by name and the camera's background type, capture the Game view and look at it. Play mode is needed only if a script sets the skybox at runtime, as in section 3's case.

## Verify

Because the reported symptom is visual, state which values you verified and ask the user to confirm the appearance. You MUST NOT claim a visual result is correct from state reads alone.

If a check fails, go back and reread the relevant documentation from `references/topic-map.md` for what you missed. The cause is more often a second source overriding the one you fixed than a wrong value.

## Report

Include these in your final report:

- Which source was actually supplying the background, and how you established it.
- What you changed, and what you deliberately left alone.
- The documentation pages you read, by URL.
- What you verified from state, and what still needs the user's eyes.
- Anything the pipeline cannot do, such as volumetric fog, stated plainly rather than approximated.
