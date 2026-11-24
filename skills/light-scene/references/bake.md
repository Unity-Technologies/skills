# Bake scene lighting

Prepare a URP scene for baked lighting, run the bake, and prove from project state that it produced output.

Relative paths in this file, such as `references/terms.md`, resolve to the skill's root folder, not to this file's folder or the project. Other workflows are named by workflow, such as "the bake workflow"; the skill's workflow table gives each one's file.

## Contents

- Success criteria
- Important for this workflow
- Project conventions
- Confirm this is the right workflow
- Check the scene can be baked at all
- Prepare in dependency order
- Run the bake
- Work the known causes before escalating
- Anti-hallucination constraints
- Terms
- Worked example
- Verify
- Report

## Success criteria

- Contribute GI flags, light modes and lightmap UVs are verified before the bake, and a bake that cannot produce output is not run.
- The bake actually runs: `Lightmapping.Bake()` returns `true`, and the lightmap count is read back from project state afterwards - never assumed from the absence of errors.
- Lightmaps exist for every Baked or Mixed light, or the user is told exactly which lights still contribute nothing and why.
- The user is told which files the bake wrote and that the scene must be saved and the lighting folder committed for the result to survive.
- No shared asset is written to disk without saying so first.

## Important for this workflow

- Do only what's asked. You MUST NOT change unrelated assets or files, because lightmap settings and pipeline assets are shared across scenes.
- `UnityEditor.Lightmapping.BakeAsync()` returns immediately and bakes in the background, so any code after it runs against an unbaked scene. If your script needs the bake finished before it reports, captures or verifies, call the synchronous `Bake()` instead, check that it returns `true`, and say which you used.
- WAIT for the user before starting a bake on a large scene. A bake can take minutes to hours and blocks the editor, so it is not a step to run speculatively.

## Project conventions

Unity's manual describes how baking works. It cannot tell you what THIS project has already decided. Before changing any value:

1. Search the project for its own documentation on lighting and technical constraints - for example a `Docs` folder, a style guide, or a technical design document - and read what it says about baking. Some projects state that lighting is realtime by policy.
2. Open a sibling scene that is already baked and read its settings: lightmap resolution, lightmapper backend, light modes, and whether probes are used.
3. Where a project convention and this workflow disagree, the project wins. Name the document you followed in your response.
4. If the project has never baked anything, treat that as a decision until the user says otherwise, and confirm before introducing the first lightmaps. Baked output is large binary data that lands in version control.

Do this because baking has project-wide consequences - import settings, repository size, iteration time - that a scene-level view cannot see.

## 1. Confirm this is the right workflow

| Request | Handle here? |
|---|---|
| Run a bake, configure lightmaps, add light probes | Yes |
| A bake produced nothing, or the scene looks unchanged after baking | Yes |
| A scene has no lighting at all and needs a setup | No - use the set-up workflow first |
| A lit scene looks wrong and no bake is involved | No - use the diagnose workflow |
| The user wants a different mood or time of day | No - use the style workflow |
| A material renders magenta, or a shader has no Meta pass | No - use the materials workflow |

## 2. Check the scene can be baked at all

Change nothing during this step. Report what you find, because steps 2 to 4 decide what section 3 must fix before a bake can produce anything.

1. Read the Unity version and confirm the active pipeline is URP, including any per-quality-level override.
2. Count renderers whose static flags include Contribute GI. **If none are marked, nothing will bake.** A scene of entirely moving objects has nothing to bake, and the answer is probes rather than lightmaps.
3. Check each static mesh has lightmap UVs by reading the mesh itself, `mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord1)`, not the model's import settings, because a model can bring its own lightmap UVs with Generate Lightmap UVs off. Only a mesh without them needs that setting turned on, which re-imports the model.
4. Read the light modes of every light. Realtime lights contribute nothing to a bake.
5. Read the current lightmap count and whether a Lighting Data Asset exists.

Hold the bake, not the workflow. If the user asked you to configure or repair baking, fix what steps 2 to 4 found in section 3, then bake. Stop the workflow only when nothing in the scene can contribute GI, because then lightmaps are not the answer, and say that probes are. Never run a bake before its prerequisites are in place, because it cannot produce output.

## 3. Prepare in dependency order

Each stage below depends on the one above it, so a stage done out of order usually has to be redone.

1. Mark the static geometry to contribute GI. Only geometry that never moves.
2. Set light modes per [Choose a light mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/choose.md), and never Baked for a light that anything changes at runtime, because a change to a Baked light has no visible effect.
3. Set lightmap resolution and parameters per [Lightmap Parameters](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/class-lightmap-parameters.md). Start low. A first bake exists to prove the setup, not to look final.
4. Probes for moving objects, because moving objects receive nothing from lightmaps. First read the probe system the resolved pipeline asset enables - on URP, `lightProbeSystem` on the `UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset`, shown as Light Probe System - because it decides which probes work, whatever the scene contains. Then look for existing volumes with `UnityEngine.Object.FindAnyObjectByType<UnityEngine.Rendering.ProbeVolume>()`.
   - If the asset enables Adaptive Probe Volumes, use [Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/use.md), and add a volume if the scene has none.
   - If it enables Light Probe Groups, use a [Light Probe Group](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/light-probes.md), or offer to switch the asset to Adaptive Probe Volumes. Ask before switching, because the pipeline asset is shared and the switch changes every scene's bake.
   - On the Built-in Render Pipeline, use a Light Probe Group.

   NEVER add a `LightProbeGroup` while the asset enables Adaptive Probe Volumes, because it does nothing there and a script that adds one still appears to succeed.
5. Add a [Reflection Probe](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections/reflection-probes.md) only if a surface is metallic or smooth. Matte surfaces gain nothing from one.

## 4. Run the bake

1. Tell the user the bake is about to run and roughly how long you expect it to take. WAIT if the scene is large.
2. Choose a backend per [Choose a light baking backend](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/configure/progressive-lightmapper.md). Use the backend that page recommends for the project's Unity version.
3. Run the bake explicitly, because the [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md) lists what no longer bakes on its own.
4. NEVER decline to bake because the scene cannot be saved, because the bake's lighting data and lightmaps are assets that are live the moment it finishes, and only the scene's reference to them needs the save. Bake, then say plainly that the scene must be saved and its lighting folder committed for the result to survive a reopen or reach a build.
5. NEVER report a bake as done until `UnityEditor.Lightmapping.Bake()` has returned `true` for this bake and you have read the lightmap count back from project state, because a failed bake leaves the previous result in place, so the count alone can come from an earlier bake.
6. Assigning `LightProbes.bakedProbes` writes to `LightingData.asset` on disk immediately, and it
   is not undone by declining to save the scene. Say so before touching probe data by script, and
   re-read the asset's size afterwards, because a "restored" probe array can come back different
   from the original.

## 5. Work the known causes before escalating

| Symptom | Work these first | Escalate only when |
|---|---|---|
| Bake finishes but produces zero lightmaps | Contribute GI flags, light modes, lightmap UVs, then the steps in [No baked global illumination](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/troubleshooting/ts-no-baked-global-illum.md) | All of those are correct and the bake is still empty |
| Mixed lights look baked, with no realtime shadows | Work the steps in [Mixed lights render as baked](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/lighting-mode/ts-mixed-lights-render-as-baked.md) | Every step on that page checks out and it still demotes |
| The editor crashes during Generate Lighting | Switch backend per [Choose a light baking backend](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/configure/progressive-lightmapper.md), then lower lightmap resolution and atlas size, update GPU drivers, and clear the GI cache per [No baked global illumination](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/troubleshooting/ts-no-baked-global-illum.md) | Every backend has been tried and it still reproduces |
| Surfaces turn magenta after the bake | The material's shader has no usable `Meta` pass, so the bake produced Unity's error shader for it. Hand the material to the materials workflow; do NOT adjust lights, ambient or lightmap settings, because none of them changes an error shader | Never - this is a material fault, not a bake fault |

You MUST NOT skip the first column, because escalating a fixable case wastes the user's time.

## 6. Anti-hallucination constraints

| Rule | Reason |
|---|---|
| Treat "Baked Global Illumination is enabled" and "the scene is baked" as different facts | The first is a setting; only a completed bake produces lightmaps |
| NEVER assume a mesh has lightmap UVs, or that it lacks them | Read the mesh's second UV channel: the import setting only says whether Unity generates one, and without it a lightmap cannot be applied |
| After every bake, check that `Lightmapping.Bake()` returned `true`, then read the lightmap count | The return value says whether this bake succeeded and the count shows what it produced; neither is enough alone, because a failed bake leaves the previous lightmaps in place |
| Say which on-disk assets a change touches before making it | `LightProbes.bakedProbes` writes to `LightingData.asset` the moment it is assigned, and `SerializedObject.ApplyModifiedProperties` on a pipeline, quality or Volume asset marks it dirty so the Editor saves it on exit. Declining to save the scene undoes neither |
| Confirm whether the project uses Adaptive Probe Volumes or Light Probe Groups before any probe work | The resolved pipeline asset decides it - on URP, its `lightProbeSystem` - not the scene's contents, because a project can enable Adaptive Probe Volumes before any `ProbeVolume` exists |

## 7. Terms

Terms this workflow uses in a specific sense - key light, ambient, Contribute GI, light mode and the rest - are defined in `references/terms.md`.

## 8. Worked example

Requested: "Bake the lighting for this level."

1. Context: Unity 6000.6, URP active, no quality-level override.
2. Pre-flight: 140 renderers, **none** marked Contribute GI. Lightmap count 0, no Lighting Data Asset. Both lights are Realtime.
3. Report that as it stands the bake would produce nothing, and say why.
4. Ask which geometry is genuinely static, rather than marking all 140 renderers.
5. Mark the confirmed static geometry, set the key light to Mixed, leave moving objects unmarked.
6. Check the static models have lightmap UVs. Two do not, so flag that fixing them re-imports the model.
7. Set a low lightmap resolution for the first bake and say it is a proving run, not a final one.
8. Ask before running, then run explicitly.
9. Read back: `Bake()` returned `true`, the lightmap count is now 3, and the Lighting Data Asset is present. Report those facts rather than saying "baked".

## Verify

If the user asked you to prove it works or you suspect a specific fault, do these after the Final step's console check and capture:

1. Confirm that `Lightmapping.Bake()` returned `true` for this bake, then read the lightmap count and the presence of a Lighting Data Asset from project state.
2. Note a dynamic object's position, move it through the probed area in Edit mode and capture again, then put it back at the noted position whether or not the check passed, because the move is only a test. Probes apply in Edit mode too, so a moving object that stays unlit means the probe data did not bake.

Because baked lighting is judged by eye, state which values you verified and ask the user to confirm the appearance. You MUST NOT claim a bake looks correct from state reads alone.

If a check fails, go back and reread the relevant documentation from the topic map's bake section for what you missed. An empty bake is far more often a missing Contribute GI flag or a missing lightmap UV than a wrong lightmapper setting.

## Report

Include these in your final report:

- What could and could not be baked, and why.
- What you marked static and what you deliberately left dynamic.
- The lightmap count and Lighting Data Asset state before and after.
- The documentation pages you read, by URL.
- Anything that needs a model re-import or a project-wide decision.
