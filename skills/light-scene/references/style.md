# Style scene lighting

Move a working lighting setup to a different mood or time of day, changing look rather than repairing fault.

Relative paths in this file, such as `references/terms.md`, resolve to the skill's root folder, not to this file's folder or the project. Other workflows are named by workflow, such as "the bake workflow"; the skill's workflow table gives each one's file.

## Contents

- Success criteria
- Important for this workflow
- Project conventions
- Confirm this is the right workflow
- Establish the target before touching anything
- Change in this order
- Constraints that keep a mood pass reviewable
- Anti-hallucination constraints
- Terms
- Worked example
- Verify
- Report

## Success criteria

- The mood came from key colour and angle, then ambient, then shadows, then tonemapping, then grading - in that order.
- No light was added or removed, and no intensity was raised more than 20% without saying so.
- Every Volume parameter that was meant to take effect has `overrideState` on, and the render was checked to confirm the grade landed.
- A documented palette's values are kept where one exists.
- If the scene's lights are Baked or Mixed, the bake was re-run so the new mood is not sitting under stale daylight lightmaps.

## Important for this workflow

- Do only what's asked. You MUST NOT change unrelated assets or files. Volume profiles and pipeline assets are frequently shared between scenes, so check what else references one before editing it.
- WAIT for a reference before choosing values. Mood is an art decision with no correct default. An image, a named time of day, or a documented palette is the difference between one pass and five.

## Project conventions

Unity's manual describes how the controls work. It cannot tell you what look THIS project is aiming at, and that is the whole subject of this workflow. Before changing any value:

1. Search the project for its own art direction - a style guide, visual rules, a colour palette, reference images. Read it. A documented palette outranks your judgement of what looks good.
2. Open a sibling scene that already has the intended look and read its values: key colour and angle, ambient, fog, and the Volume profile it uses.
   Copy its **mode, shadow and bake policy and Volume structure**. Do NOT copy its intensity or colour:
   those follow this scene's own materials, sky and mood, and a sibling lit for another time of day
   will look careful while being wrong. Judge them against this scene's render instead.
3. Where a project convention and this workflow disagree, the project wins. Name the document you followed in your response.
4. If the project's documentation contradicts itself or marks something unresolved, do NOT silently pick a side. Say which choice you made and flag the open question.

Do this because a mood change judged only by taste is unreviewable, while one traceable to the project's own palette can be argued with.

## 1. Confirm this is the right workflow

| Request | Handle here? |
|---|---|
| Make the scene feel like sunset, night, overcast, golden hour | Yes |
| Warm up, cool down, or match a reference or art style | Yes |
| The scene has no lighting at all | No - use the set-up workflow |
| The scene looks broken, blown out, or shadows are missing | No - use the diagnose workflow |
| Configure or troubleshoot a bake with no mood change | No - use the bake workflow |
| The skybox is missing, will not change, or nothing reflects the sky | No - use the environment workflow |

A useful test: if the current look is a *fault*, it is not this workflow. This workflow assumes the setup works and the intent has changed.

## 2. Establish the target before touching anything

Change nothing during this step.

1. Read the current key light colour, intensity and angle; the ambient source and colour; fog; and every Volume override in effect.
2. Establish the target. Ask for a reference image, a named time of day, or point at the project's documented palette. WAIT for the answer.
3. Check tonemapping first. If tonemapping is None with HDR on, values above 1 clip to white and no amount of colour work will read correctly. Say so before proceeding, because it changes what every later value does.
4. Report the current values and the target you are working to, so the user can correct the brief before you spend a pass on it.
5. If the user asks to restore an earlier mood and no reference, sibling scene or documented palette records it, say the original is unrecoverable, then offer an approximation from their description - and never present that approximation as a restore.

## 3. Change in this order

Mood comes mostly from the first two steps. Working bottom-up from post-processing produces a scene that looks graded rather than lit.

1. **Key light colour and angle.** Time of day is mostly sun elevation. A low sun gives long shadows and warm light; a high sun gives short shadows and neutral light. Change the angle before reaching for a colour filter.
   Set warmth with `useColorTemperature = true` and `colorTemperature` in kelvin rather than an
   RGB guess, so the light reads as the source the mood describes.
2. **Ambient.** Set the shadow colour and the overall cool or warm balance. Warm key against cool ambient reads as daylight; both warm reads as interior or sunset.
3. **Shadow softness and strength.** Overcast is soft and low-contrast; midday is hard and high-contrast. Shadow strength below 1 lifts blacks without touching the light.
4. **Tonemapping.** Set it before grading, because it determines how highlights roll off.
5. **Colour grading.** Last, and sparingly. It corrects the image; it does not light the scene.
   Every Volume parameter you mean to take effect needs `overrideState = true` as well as a
   value; a parameter with it off is ignored and the profile still displays your number. Set the
   state, then re-render and confirm the grade landed before moving on.
6. **Fog, bloom, vignette.** Atmosphere, last of all, and only if the project uses them.
7. **Re-bake if the scene is baked.** If the scene carries lightmaps and you moved or recoloured a Baked or Mixed sun, re-bake as part of this pass with the scene's own lighting settings, because under the old lightmaps the bounce light still describes the previous time of day and the mood reads as a tint over daylight. Say which files the bake writes - `LightingData.asset`, the lightmaps and the reflection probes in the scene folder - before running it. If those settings would make the bake too slow for this pass, ask before baking at draft quality instead, because a draft replaces the production lightmaps. If the user agrees, call the result a preview and leave the scene's own settings in place for the final bake. Hand off to the bake workflow only when the user asks about bake configuration itself.

After each change, re-read the affected value rather than assuming the write applied.

### Calibrate the sun angle against the render

A low sun is the whole point of an evening look, and it is the change most likely to be silently
undone by the building: geometry that was irrelevant at noon blocks a sun at 15 degrees, and the
main surfaces fall into flat shade. That is not visible in any state read - the rotation you set
is the rotation you get, and the scene still looks wrong.

Treat your first sun elevation as a guess and correct it from the image.

1. Render, then check whether direct light actually reaches the surfaces the shot is about -
   the floor, the wall the camera faces. Look for a cast shadow pattern; if nothing casts, no
   direct light is arriving.
2. If the main surfaces are in flat shade, raise the elevation in steps and re-render until
   light lands, then stop at the lowest angle that still lights them.
3. NEVER compensate for a blocked sun by raising intensity, warming the ambient or pushing the
   grade. Those brighten the shade without restoring the direction, and the result reads as
   overcast tinted orange rather than as evening.
4. Say which elevation you tried, which you settled on, and why.

## 4. Constraints that keep a mood pass reviewable

| Rule | Reason |
|---|---|
| NEVER raise key intensity to make a scene feel brighter in mood terms | Intensity past the tonemapping range clips detail to white; use exposure or ambient instead. If the Volume has no Tonemapping override (mode None) there is no range at all - anything above 1.0 clips - so read that first |
| NEVER edit a shared Volume profile or pipeline asset without saying so | One profile often serves several scenes, and the change will appear in all of them |
| ALWAYS keep a documented palette's values when one exists | A mood pass that ignores the palette will be reverted |
| NEVER use pure black or pure white if the project's rules forbid them | Many stylised projects cap both ends deliberately |
| Change one layer at a time and re-read it | Mood work compounds, and a wrong value found late costs the whole pass |

## 5. Anti-hallucination constraints

| Rule | Reason |
|---|---|
| Read every value before changing it | Mood values are almost never at their defaults |
| Do not claim a colour reads warm or cool without reading the resolved ambient | Ambient mode changes how a colour is applied, so the swatch alone does not tell you the result |
| NEVER set a Volume override parameter's `.value` without setting `.overrideState = true` first | A parameter whose `overrideState` is false is ignored entirely: the profile displays the value and the render does not change. The result looks like a pipeline fault, which it is not |
| For warm or cool light prefer `useColorTemperature = true` and `colorTemperature` in kelvin over a hand-picked RGB | A temperature names the source the user described, and with a temperature in use the light's `color` is a filter that tints it, per the [URP Light component reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/light-component.md), so read both when checking warmth |
| For Volume scripting beyond setting a value, follow the `urp-postprocessing` skill | Edit `volume.sharedProfile` (the asset, so it persists), not `volume.profile` (a runtime clone that is never saved), and an override added in editor code needs `AssetDatabase.AddObjectToAsset` to survive a reload - otherwise the grade shows in the Inspector and vanishes when the asset reloads |

## 6. Terms

Terms this workflow uses in a specific sense - key light, ambient, Contribute GI, light mode and the rest - are defined in `references/terms.md`.

## 7. Worked example

Requested: "Make this level feel like late afternoon instead of midday."

1. Read current state: key light white at intensity 1.4, pitch 60 degrees, ambient a neutral grey, tonemapping Neutral, no colour filter.
2. The project's style guide documents a warm golden-hour key of `#FFF5CC`. Use it rather than inventing a colour, and say so.
3. Drop the sun pitch from 60 to about 15 degrees. That alone lengthens shadows roughly fourfold and is most of the effect.
4. Warm the key to the documented colour; leave intensity alone, because the mood change is angle and hue, not brightness.
5. Cool the ambient slightly so the warm key reads as warm by contrast.
6. Soften shadows a little and set shadow strength below 1 so blacks are not crushed.
7. Leave grading untouched; it was already correct.
8. Re-read every value, then ask the user to confirm the look against their reference.

## Verify

Mood is judged only by eye. State which values you set and against which reference, and ask the user to confirm. You MUST NOT claim the look matches their intent from state reads alone.

If the look is wrong, go back and reread the relevant documentation from `references/topic-map.md`. A mood that will not resolve is more often tonemapping or ambient than the key light everyone reaches for first.

## Report

Include these in your final report:

- The target you worked to, and where it came from.
- Each layer you changed, in order, with the before and after values.
- The documentation pages you read, by URL.
- Any shared asset you touched, and what else references it.
- The one or two values the user is most likely to want to nudge.
