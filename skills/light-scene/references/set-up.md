# Set up scene lighting

Build a lighting setup for a URP scene from nothing, in the order Unity expects, asking for the intended look rather than guessing it.

Relative paths in this file, such as `references/terms.md`, resolve to the skill's root folder, not to this file's folder or the project. Other workflows are named by workflow, such as "the bake workflow"; the skill's workflow table gives each one's file.

## Contents

- Success criteria
- Important for this workflow
- Project conventions
- Confirm this is the right workflow
- Gather project context
- Ask for the three things you cannot infer
- Adding a single light to a scene that is already lit
- Build the setup in order
- Anti-hallucination constraints
- Terms
- Worked example
- Verify
- Report

## Success criteria

- The environment is set before the first light, and exactly one directional light acts as key.
- The key casts shadows; a reflection probe exists if anything in view is metallic or smooth.
- If any light is Baked or Mixed the bake has run and lightmaps exist; otherwise the setup is stated to be fully realtime.
- Intensity was calibrated by rendering and looking at every area the user will see, interiors included, not copied from a sibling scene.
- The three questions the setup depends on were either answered in the prompt or asked once, together, before any value was chosen.

## Important for this workflow

- Do only what's asked. You MUST NOT change unrelated assets or files, because pipeline and quality settings are shared across every scene in the project.
- A component cannot be destroyed while another on the same GameObject declares `[RequireComponent]` on it, and Unity refuses without throwing an exception, so a `script eval` snippet that tries it still appears to succeed. URP attaches `UniversalAdditionalLightData` to every Light, so removing a Light means removing that first. Re-read the count afterwards: a destroy that quietly did nothing looks identical to one that worked.
- WAIT for the user to state the intended look before choosing values. Time of day, mood and colour temperature are art decisions with no correct default, and guessing wastes the work.

## Project conventions

Unity's manual describes how lighting works. It cannot tell you what THIS project has already
decided. Before changing any value:

1. Search the project for its own documentation on lighting and art direction - a `Docs`
   folder, a style guide, a technical design document - and read it.
2. Open a sibling scene that is already lit and read its rig: light count, colour, intensity and
   mode, ambient source, fog, and any bound Volume. Read its shadow and bake policy too - they
   decide each other, because a baked scene pays no runtime cost for shadows and can afford far
   more shadow casters than a realtime rig. Match the project's structure unless you have a
   stated reason not to.
3. Where a project convention and this workflow disagree, the project wins. Name the document you
   followed in your response.
4. If the project documents a scene-lighting ownership rule, obey it. A scene with no lights is
   sometimes deliberate, and "fixing" it can silently break the arrangement.

**Light mode, shadow policy and bake policy are conventions** - copy them. **Intensity, colour
and range are not**: they follow this scene's own materials, sky and mood, so take a light's
colour from what emits it HERE - a fixture's emissive material, the sky, a screen - not from a
sibling lit for another time of day.

Do this because a setup that is correct in general can still be wrong for a project that has
already decided these things - the most common reason this work gets rejected.

## 1. Confirm this is the right workflow

| Request | Handle here? |
|---|---|
| A scene has no lighting, or only a default light, and needs a setup | Yes |
| Add or adjust one light in an already-lit scene - a lamp, fire, torch or other practical | Yes |
| A scene is already lit and looks wrong | No - use the diagnose workflow |
| The setup exists and the user wants a different mood or time of day | No - use the style workflow |
| Run or configure a bake on an existing setup | No - use the bake workflow |
| Tune lighting cost for a specific platform | No - use the optimize workflow |
| The sky, environment reflections or fog need setting up or repairing | No - use the environment workflow |
| A material renders magenta, or a custom shader receives no light | No - use the materials workflow |

## 2. Gather project context

Change nothing during this step.

1. Read the Unity version and the active render pipeline asset.
2. Check whether the current quality level overrides the pipeline asset, per [Set the render pipeline asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/srp-setting-render-pipeline-asset.md), because a project can render through a different pipeline asset per quality level.
3. Name the active pipeline. The steps below are written for URP; on Built-in or HDRP apply the substitutions in `references/pipeline-notes.md` as you go - the shape of the work is the same, the places the settings live are not.
4. Find existing lights with the query `t:Light`, and read the scene's Environment Lighting Source and Intensity Multiplier.
5. If the scene already has a considered setup and the request is not to add one light, hand off to the diagnose workflow: a second setup on top of an existing one produces conflicting shadows. A request to add one light continues at section 4.

## 3. Ask for the three things you cannot infer

Ask these together, in one message, and WAIT for the answer. Each one changes the values you would choose, so guessing means redoing the work.

| Question | Why it changes the setup |
|---|---|
| What is the intended look - time of day, indoor or outdoor, mood? | Determines key light direction, colour temperature and intensity. There is no default. |
| Is the level geometry static, or does it move? | Static geometry can be baked; moving geometry cannot, and needs probes instead. |
| What is the target platform? | Mobile and tile-based platforms pay a real cost for Soft Shadows and extra realtime lights, per [Optimize shadow rendering in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/shadows-in-urp/shadows-optimization.md). |

If the user says to just pick something, choose a neutral daylight exterior, say so explicitly, and tell them which values to change to move it.

## 4. Adding a single light to a scene that is already lit

A request for one light is not a request to relight the scene. Change nothing else.

1. Read the existing rig first. A new light has to sit inside the established key, fill and ambient, not compete with it.
2. Choose the type from what the light represents, per [Light component types](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/lighting.md), and, outside HDRP, use an area light only in a baked scene, because that page says area lights can only be baked.
3. Set range before intensity. Range decides what the light touches; intensity only how strongly. Compensating for a wrong range with intensity gives a light too bright at the source and still dark at the edge.
4. Match the project's palette rather than inventing a colour; a practical is usually warmer than the key. Set warmth as a colour temperature in kelvin and put any visible fixture geometry on a layer the camera renders, both per the constraints in section 6.
5. Check the additional-light budget first, per [Lights flicker or disappear](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/ts-lights-flicker-disappear.md): a light past the limit is silently downgraded or dropped, and gets reported as a bug. Shadows follow the light mode: a **Baked or Mixed** practical keeps its shadows on, because the bake stores its shadows on static geometry in the lightmaps or the shadowmask, and turning them off loses those too; only a **Realtime** practical goes without shadows by default, turned on only to describe form. Read the scene's policy before throttling: a realtime shadow budget imposed on a baked scene silently deletes most of its shadows.
6. If the scene is baked, match the surrounding rig's mode and bake afterwards - do not leave it half-applied.

## 5. Build the setup in order

Work through [Lighting configuration workflow](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-configuration-workflow.md) and follow its order. Later stages depend on earlier ones, so a stage done out of order usually has to be redone.

1. **Environment first.** Set the Environment Lighting Source and Intensity Multiplier, or confirm that the current values suit the look, before you add or change any light - including a default light the scene already has. Ambient sets the floor that every light adds on top of, and judging a light against a black ambient gives the wrong intensity.
2. **One key light.** Add a single directional light as the sun or main source. Use exactly one, because a second directional light casts a second set of shadows and reads as a rendering error rather than a lighting choice.
3. **Shadows on the key light.** Enable shadows and check the result against [Shadow configuration](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-configuration.md). On a mobile target prefer Hard Shadows unless the user asked for soft ones.
4. **Secondary lights only where they do work.** Add point or spot lights only for features the key light cannot reach; the additional-light budget in section 4 applies.
5. **Reflection probe if anything is glossy.** Metallic and smooth surfaces render black or wrong without one, per [Reflection Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections/reflection-probes.md).
6. **Decide light modes, then probes.** Use the matrix in [Choose a light mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/choose.md). If geometry is static and baked quality is wanted, set it to contribute GI, choose Baked or Mixed, then give moving objects probes so they still receive that light, following the bake workflow section 3, which reads the pipeline asset's probe system before it chooses between [Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/use.md) and a [Light Probe Group](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/light-probes.md), because a Light Probe Group does nothing while that asset enables Adaptive Probe Volumes.
7. **Finish the bake if the setup depends on one.** Until a bake runs, the scene renders only the realtime half of the setup. Read what this Unity version bakes without being asked in the [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md) and the [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md).

   Decide which of these you are in, and say which:

   - **Restoring a baked scene, or lighting static geometry you set to Baked or Mixed.** Run the bake; it is part of the work. Call the synchronous `Lightmapping.Bake()`, because `BakeAsync()` returns before anything is baked, and check that it returns `true`, because a failed bake leaves any earlier lightmaps in place.

     NEVER skip the bake because you cannot save the scene: the bake writes its lightmaps and `LightingData.asset` as **assets**, live the moment it finishes, and only the scene's own reference to that asset needs the save. Bake, then say what still needs saving.
   - **If the project's authored settings are too slow, bake at draft quality rather than skipping it** - a few texels per unit and one bounce resolves in seconds and still gives real shadows, GI and probe reflections. If the scene already has lightmaps, ask before you bake the draft, because it replaces them. Put the draft values in a separate lighting settings asset, so the project's own settings stay as they were for the final bake. Say it is a draft and name the production settings.
   - **Nothing in the scene can contribute GI yet**, such as a level with no static geometry so far. Do not bake, because there is nothing for the bake to work on; say that the setup needs a bake once static geometry is in.
   - **Entirely realtime.** Nothing to bake; say so.

After each stage, re-read the values you wrote rather than assuming the write applied.

### Calibrate intensity against the render

Intensity is the value you are most likely to get wrong and the one a state read cannot check -
every number looks plausible in the Inspector. Treat your first intensity as a guess and correct
it from the image.

1. Render once the setup is in place, and judge the mid-tones rather than the highlights. A scene
   is too dark when surfaces that should read white read grey and shadows carry no detail.
2. Raise the key light and the environment together, then render again. Indirect bounce scales
   with the key, so a weak key leaves the scene flat however many practicals you add.
3. Say which intensities you measured and which you guessed, and name the first one to tune.

### When a grade tuned for another time of day hides the result

A scene's Volumes can carry exposure and white balance tuned for the look it had before, such as a night grade. Under a new daytime setup that grade can leave whole areas wrong however the lights are set - typically interiors that stay black. Check every area the user will look at, not only the main view. If one still reads wrong after calibration because of a grade:

1. Read the exposure and white balance of each Volume profile that applies there.
2. Copy that profile to a new asset for this scene, assign the copy to the scene's Volume, and set exposure and white balance in the copy for the new look. Leave the original untouched, because other scenes may share it.
3. Render the area again and calibrate against it.
4. Report the copy, the values you changed and why, and that the original is unchanged, so the user can keep either.

Do not raise light intensities to fight a grade, because the lights then read wrong as soon as the grade is fixed.

## 6. Anti-hallucination constraints

| Rule | Reason |
|---|---|
| NEVER rely on Auto Generate to produce baked lighting | It was removed in Unity 6, per the [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md); run the bake explicitly and save the lighting data asset it writes |
| Do not add a second directional light to raise brightness | Raise the existing light's intensity or the environment contribution instead; a second sun produces conflicting shadows |
| Do not treat "Baked Global Illumination is enabled" as "the scene is baked" | The first is a setting; only a completed bake produces lightmaps |
| NEVER set a Volume override parameter's `.value` without setting `.overrideState = true` first | A parameter whose `overrideState` is false is ignored entirely: the profile displays the value and the render does not change. The result looks like a pipeline fault, which it is not |
| Before creating any visible object, read the rendering camera's `cullingMask` and put the object on the layer of the thing it belongs to | NEVER assume the Default layer renders. In a project that culls it the object exists, is enabled, has bounds, and never draws - and a state read looks perfect |
| For warm or cool light prefer `useColorTemperature = true` and `colorTemperature` in kelvin over a hand-picked RGB | A temperature names the source the user described, and with a temperature in use the light's `color` is a filter that tints it, per the [URP Light component reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/light-component.md), so read both when checking warmth |
| For Volume scripting beyond setting a value, follow the `urp-postprocessing` skill | Edit `volume.sharedProfile` (the asset, so it persists), not `volume.profile` (a runtime clone that is never saved), and an override added in editor code needs `AssetDatabase.AddObjectToAsset` to survive a reload - otherwise the grade shows in the Inspector and vanishes when the asset reloads |

## 7. Terms

Terms this workflow uses in a specific sense - key light, ambient, Contribute GI, light mode and the rest - are defined in `references/terms.md`.

## 8. Worked example

Requested: "This level has no lighting at all, can you set it up? It's an outdoor daytime scene, the terrain is static, PC only."

1. Context: `PC_RPAsset` active, no override; `t:Light` returns zero lights; ambient is Color at 0.02.
2. The three questions are answered in the request; do not ask again.
3. Environment first: source to Skybox, Intensity Multiplier to 1.
4. Key light: one directional, warm white, pitched down ~50 degrees, intensity near the URP default.
5. Shadows: enable Soft Shadows, since the target is PC rather than mobile.
6. Glossy surfaces present: add a reflection probe over the play area.
7. Terrain is static and quality is wanted: contribute GI, key light Mixed, add light probes for movers.
8. Run the bake, at draft quality if the authored settings are slow, and say which you ran.
9. Verify: re-read every value, confirm zero console errors, then render and look at the result.

## Verify

Because lighting is judged by eye, state which values you set and ask the user to confirm the look. You MUST NOT claim the result matches their intent from state reads alone.

If a check fails, go back and reread the relevant documentation from `references/topic-map.md` for what you missed. A stage completed out of order is a more common cause than a wrong value.

## Report

Include these in your final report:

- What the scene had before.
- Each stage you completed, in order, and the values you set.
- The documentation pages you read, by URL.
- Whether a bake is still required, and that it is a separate step.
- Which values the user is most likely to want to adjust to move the look.
