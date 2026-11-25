# Animate scene lighting

Drive a URP scene's lighting over time, choosing between realtime and blended-baked approaches on what the scene actually contains.

Relative paths in this file, such as `references/terms.md`, resolve to the skill's root folder, not to this file's folder or the project. Other workflows are named by workflow, such as "the bake workflow"; the skill's workflow table gives each one's file.

## Contents

- Success criteria
- Important for this workflow
- Project conventions
- Confirm this is the right workflow
- Establish the design before writing code
- Choose the approach from what the scene contains
- Build it in this order
- Constraints
- Anti-hallucination constraints
- Terms
- Worked example
- Verify
- Report

## Success criteria

- One normalised time value drives the sun, the ambient and the sky together; none of them has its own clock.
- Motion is proven from two readings taken a moment apart that differ, not from a single reading.
- Ambient and environment updates run on a timer or threshold, never every frame.
- A scene with static lightmaps and a moving sun is reported as a design conflict before any code is written.
- The user is told what the cycle changes and which value to adjust to change its pace.

## Important for this workflow

- Do only what's asked. You MUST NOT change unrelated assets or files, because this workflow writes runtime scripts as well as scene data.
- WAIT for the user to say how long a full cycle should take and whether it must survive a save or reload. Those two answers decide the whole design, and guessing means rewriting it.

## Project conventions

Unity's manual describes the mechanisms. It cannot tell you how THIS project already handles time, state and lighting ownership. Before writing anything:

1. Search the project for its own documentation on lighting, scene composition and game state - for example a `Docs` folder or a technical design document - and read it. A project that documents which scene owns lighting will break if a cycle writes to the wrong one.
2. Check whether the project already has a time-of-day or game-state system. Adding a second clock is worse than driving the existing one.
3. Where a project convention and this workflow disagree, the project wins. Name the document you followed in your response.
4. Check where the new script belongs. Projects usually have a documented folder and namespace convention for runtime scripts.

Do this because an animated-lighting system touches runtime code, scene ownership and update order, and a scene-level view cannot see any of those.

## 1. Confirm this is the right workflow

| Request | Handle here? |
|---|---|
| A day-night cycle, a moving sun, a flicker the user asks for, or other lighting that changes over time | Yes |
| Blending between two baked lighting states | Yes |
| A scene has no lighting yet | No - use the set-up workflow first; you cannot animate what does not exist |
| A single fixed mood or time of day | No - use the style workflow |
| A one-off bake | No - use the bake workflow |
| A light that flickers or disappears when it should not | No - use the diagnose workflow |
| The skybox does not change when the lighting state does | No - use the environment workflow |

## 2. Establish the design before writing code

Change nothing during this step.

1. Read the existing lighting: lights, ambient source, skybox type, and whether the scene has lightmaps.
2. For a cycle, ask how long a full cycle should take, and whether the time of day must persist across a save or scene reload. WAIT for both answers. For a flicker, the only questions are its speed and strength: take them from the request, or use a firelight default and name it in your report, because neither changes the approach.
3. Ask whether the geometry is static. This decides the approach more than anything else.
4. Check whether a procedural or texture skybox is in use. A texture skybox will not follow a moving sun, so a day-night cycle needs either a procedural sky or a blend between two skies.

## 3. Choose the approach from what the scene contains

| The scene has | Use | Why |
|---|---|---|
| Realtime lighting only, no lightmaps | Drive the directional light and ambient directly from script | Simplest, and nothing baked can go stale |
| Static geometry, and the user wants baked quality that still changes | Adaptive Probe Volumes, blending [Lighting Scenarios](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/probe-volumes-change-lighting-at-runtime/bakedifferentlightingsetups.md) or using [sky occlusion](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/probe-volumes-change-lighting-at-runtime/skyocclusion.md) | Lightmaps cannot change at runtime, and these let baked indirect light follow a time of day |
| Existing lightmaps and a moving sun | Stop and explain the conflict | Static lightmaps encode one sun position; they cannot follow a moving one, and the scene will look wrong at every other time of day |

If the third row applies, say so plainly before writing anything. It is a design conflict, not a bug, and the user has to choose between baked quality and a moving sun.

## 4. Build it in this order

1. **Drive the sun.** Rotate the directional light and set its colour and intensity from a single normalised time value. One value driving everything keeps the cycle coherent.
2. **Move ambient with it.** Update the ambient source or its colours in the same step, then call the dynamic GI environment update so the change takes effect. A sun that sets over a fixed daylight ambient looks broken.
3. **Move the sky with it.** Assign the sun to the render settings so a procedural sky tracks it, or blend between sky materials.
4. **Blend baked data, if used.** Set up lighting scenarios per [Use Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/use.md) and blend between them, rather than rebaking at runtime.
5. **Make the update cheap.** Lighting does not need updating every frame. Update on a timer or on a threshold change in the time value, because ambient and environment updates are not free.
6. **Drive it from a clock that actually advances where it will be judged.** `Update()` and
   coroutines run in Play mode. In Edit mode they run only on `[ExecuteAlways]` components and
   only while the Editor loop is ticking, and under a headless or batch Editor they do not run at
   all; `EditorApplication.update` has the same limit. A flicker driven from any of these renders
   as a frozen frame in a still capture. If the environment offers an explicit time or tick
   callback, drive the animation from that; otherwise enter Play mode to verify, and say which
   you did.

After each stage, re-read the affected values rather than assuming the write applied.

## 5. Constraints

| Rule | Reason |
|---|---|
| NEVER update ambient or environment lighting every frame | The environment update has real cost, and lighting changes are imperceptible at frame granularity |
| NEVER rebake at runtime | Baking is an editor operation; blend prebaked scenarios instead |
| ALWAYS drive everything from one time value | Separate clocks for sun, ambient and sky drift out of step |
| Write the cycle so a given time value always produces the same lighting | A cycle that accumulates state cannot be resumed after a save or scene reload |
| NEVER animate lighting in a scene the project says does not own its lighting | The change will be overridden, or it will override another scene's setup |
| NEVER verify motion from one reading | Two readings a moment apart that differ are the evidence something moved; one reading cannot tell a running cycle from a frozen one |

## 6. Anti-hallucination constraints

| Rule | Reason |
|---|---|
| Confirm whether the project uses Adaptive Probe Volumes or the older Light Probes before writing probe code | They are different systems with different APIs, and the pipeline asset says which is active |
| Do not assume a skybox tracks the sun | Only some skybox shaders do; a texture sky will not move |
| Before creating any visible object, read the rendering camera's `cullingMask` and put the object on the layer of the thing it belongs to | NEVER assume the Default layer renders. In a project that culls it the object exists, is enabled, has bounds, and never draws - and a state read looks perfect |
| NEVER claim lights animate from a single reading | Read the sun's rotation twice, a moment apart, in Play mode and compare. Identical values mean nothing moved, whatever the code says |

## 7. Terms

Terms this workflow uses in a specific sense - key light, ambient, Contribute GI, light mode and the rest - are defined in `references/terms.md`.

## 8. Worked example

Requested: "Add a day-night cycle to this level."

1. Read state: one directional light, procedural skybox, ambient source Skybox, no lightmaps, geometry mostly static.
2. Ask for cycle length and whether time persists across a reload. The user says ten minutes, and yes it must persist.
3. No lightmaps exist, so realtime driving is viable and the baked-versus-moving-sun conflict does not arise. Say so.
4. Write one component holding a normalised time value, derived from persisted elapsed time so a reload resumes correctly rather than restarting at dawn.
5. From that value: rotate the sun, set its colour warm at the horizon and neutral at noon, scale intensity, and update ambient in the same step.
6. Assign the sun to render settings so the procedural sky's sun disc tracks it.
7. Update on a timer rather than every frame, and say why.
8. Verify by reading the sun angle and resolved ambient through `script eval` at several time values, and confirm the same value always gives the same lighting.

## Verify

A cycle that never advances is this workflow's most common fault, so always treat it as the suspected fault the Final step names, and do these after its console check and capture:

1. Enter Play mode once and read the sun's rotation and the resolved ambient through `script eval` twice, a moment apart. Values that differ show the cycle advances; also check that the sun angle, ambient and sky stay in step, and that the same time value reproduces the same lighting. Take no screenshots in Play mode.
2. Confirm the update is not running every frame.
3. If the user asked to see the cycle, set the time value in Edit mode to dawn, noon and dusk, capture each and look at them, then set it back to its starting value, because the captures are only a test.

Because the result is judged by eye over time, state which values you verified and ask the user to watch a full cycle. You MUST NOT claim the transition looks right from state reads alone.

If a check fails, go back and reread the relevant documentation from `references/topic-map.md`. A cycle that looks wrong at dusk but right at noon is usually ambient failing to move with the sun.

## Report

Include these in your final report:

- Which approach you chose and what in the scene decided it.
- Where the new script lives and what drives it.
- How the time value persists across a reload.
- The documentation pages you read, by URL.
- Any conflict between baked lighting and a moving sun that the user still has to resolve.
