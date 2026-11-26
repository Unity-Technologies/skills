# Fix material lighting response

Repair URP materials and shaders that render magenta or fail to respond to light, by identifying whether the shader is absent, wrong for the pipeline, or missing the passes and keywords lighting needs, and verify the repair by reading project state.

Relative paths in this file, such as `references/terms.md`, resolve to the skill's root folder, not to this file's folder or the project. Other workflows are named by workflow, such as "the bake workflow"; the skill's workflow table gives each one's file.

## Contents

- Success criteria
- Important for this workflow
- Rules for this workflow
- Project conventions
- Confirm this is the right workflow
- Gather project context
- Separate the three failure shapes
- Magenta or pink materials
- A custom shader receives no light or no shadows
- The shader is right but the material still looks wrong
- Anti-hallucination constraints
- Terms
- Worked example
- Verify
- Report

## Success criteria

- No magenta or pink surface remains, verified from the render.
- Materials on Built-in shaders are moved to their URP equivalents through Unity's own upgrader; custom shaders are repaired, not replaced.
- No mesh or material is deleted and recreated to hide the fault.
- A surface that stays dark for a lighting or environment reason is handed to the right skill with the reason stated, rather than "fixed" in the material.
- The user is told which shaders changed and which were left for them to decide.

## Important for this workflow

- Do only what's asked. You MUST NOT change unrelated assets or files, because materials and shaders are shared across scenes and an unrelated edit is hard for the user to spot.
- NEVER recreate a mesh, material or terrain to fix how it renders. The asset is almost always intact and only its shader reference is broken, and recreating it destroys the user's authored values.
- Read the console before forming any theory. A magenta surface usually has a shader compile error printed with the file and line, and guessing past it wastes the user's time.

## Rules for this workflow

NEVER recreate a mesh, material, terrain or prefab to fix how it renders. INSTEAD repair the shader reference, because the asset holds authored work that recreating it destroys.

NEVER replace a custom shader with a built-in one to make the symptom go away. INSTEAD name what the custom shader is missing and ask, because someone wrote that shader for a reason.

NEVER change lighting, ambient or exposure to compensate for a material that renders wrong. INSTEAD fix the material, because compensating in the scene breaks every other object in it.

NEVER edit a material or shader shared by other scenes without saying so first. INSTEAD name the asset, name what else uses it, and ask.

## Project conventions

Unity's manual describes how shaders and materials work. It cannot tell you what THIS project has
already decided. Before changing any value:

1. Search the project for its own documentation on rendering, shaders and art direction - for
   example a `Docs` folder, a style guide, or a technical design document - and read what it says
   about which shaders are approved and whether custom shaders are maintained in-house.
2. Open a sibling material that renders correctly and read which shader it uses, its surface type
   and its keyword state. Match the project's existing structure unless you have a stated reason
   not to.
3. Where a project convention and this workflow disagree, the project wins. Name the document you
   followed in your response.
4. If the project ships its own shader library, a custom shader is someone's authored work, not a
   mistake. Repair it rather than replacing it with a built-in shader, unless the user asks for
   the replacement.

Copying a sibling material is not the same as agreeing with it. Read its values, then judge them
against the asset in front of you - a surface type that suits opaque geometry will break a
material that needs alpha clipping.

Do this because a setup that is correct in general can still be wrong for a project that has
already decided these things - the most common reason this work gets rejected.

## 1. Confirm this is the right workflow

| Request | Handle here? |
|---|---|
| Materials render magenta, pink or purple | Yes |
| A custom or third-party shader receives no light, or casts no shadow | Yes |
| One object stays flat or dark while the rest of the scene lights correctly | Yes |
| Metallic or smoothness has no visible effect on one material | Yes |
| The whole scene is dark, black or washed out | No - use the diagnose workflow |
| The skybox is missing, or nothing reflects the sky at all | No - use the environment workflow |
| A scene has no lighting at all yet | No - use the set-up workflow |
| The user wants to author a new shader for a look they describe | No - this workflow repairs lighting response, it does not design shaders |
| The whole project is moving from the Built-in Render Pipeline to URP | No - that is `migrate-birp-to-urp`; come back here for any material still wrong afterwards |

If a single material is wrong, the fault is almost certainly in that material or its shader. If every material is wrong, check the pipeline and the scene first - see section 3.

## 2. Gather project context

Change nothing during this step.

1. Read the Unity version, the active render pipeline asset, and whether the current quality level overrides it.
2. Read the console and collect every shader compile error and warning verbatim. Do not summarise them away - the file and line are the fastest route to the cause.
3. Identify the affected renderers and, for each, the material and the shader asset it points to.
4. For each shader, establish which pipeline it was written for: a built-in `Standard` or `Legacy` shader, an HDRP shader, a URP shader, or a Shader Graph asset with a target set.
5. Check the project's package list for a render pipeline package that was removed or changed, because a removed package leaves materials pointing at shaders that no longer exist.
6. Report what exists before proposing any change, so the user can correct a wrong assumption early.

## 3. Separate the three failure shapes

Magenta and unlit are different faults with different fixes. Name which one you have before acting.

| Shape | What it looks like | Where the fix lives |
|---|---|---|
| Shader not found or not compiled | Solid magenta or pink, often with a console error | Section 4 |
| Shader found but not lighting-aware | Renders its texture, but flat - no shadows, no response to lights | Section 5 |
| Shader correct, material or renderer wrong | Lights partly, but one aspect is missing | Section 6 |

A magenta surface is Unity's error shader. It is not a lighting result and no lighting setting will change it. You MUST NOT adjust lights, ambient or exposure while any material is magenta, because it hides the real fault and changes the scene for everything else.

## 4. Magenta or pink materials

1. If the console shows a compile error, fix that first. The error names the file and line.
2. If there is no error, read the shader the material points at. A missing reference means the shader asset is gone - usually with the package that provided it.
3. For materials written for the Built-in Render Pipeline, use the [Render Pipeline Converter](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/rp-converter.md) rather than reassigning shaders by hand. Converting in bulk preserves property mappings that a manual swap loses.
4. For materials from a removed HDRP package, reassign each material as [Convert shaders from HDRP to URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/migrating-from-hdrp/convert-hdrp-shaders-to-urp.md) describes, say plainly that this is done by hand, and tell the user which properties could not be carried across.
5. For a Shader Graph asset, read its active target. A graph targeting a different pipeline compiles to nothing usable and renders magenta.
6. If the material renders correctly in the editor and magenta in a build, the shader was stripped. Check whether it is referenced only by name or from a `Resources` load.

NEVER recreate the mesh, terrain or prefab. The geometry is intact; only the shader reference is broken.

## 5. A custom shader receives no light or no shadows

Work these in order, because a missing pass makes the keywords below it irrelevant.

1. **Pass tags.** Read the renderer's rendering path first, then check the shader has a lighting pass for that path, using the tags [URP ShaderLab pass tags](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/writing-custom-shaders-urp/use-built-in-shader-methods/urp-shaderlab-pass-tags.md) lists for it. Never call a pass missing before you have checked that page's tags for the path, because more than one tag is valid.
2. **Shadow casting.** If the object casts no shadow, check for the shadow caster pass that page lists before you touch the renderer's Cast Shadows setting, because that setting has no effect without the pass.
3. **Shadow receiving.** Check the shader declares the shadow keywords [Use shadows in a custom URP shader](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/writing-custom-shaders-urp/use-built-in-shader-methods/shadows.md) lists, because a missing one raises no error: the shader compiles and drops shadows.
4. **Additional lights.** If only the directional light affects the surface, check the additional-light keywords [Render additional lights in a shader in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/custom-lighting/use-built-in-shader-methods-additional-lights-fplus.md) lists.
5. **Depth passes.** If the surface disappears when a depth prepass or SSAO is on, check for the depth passes the URP ShaderLab pass tags page lists.
6. **Meta pass.** If the surface adds nothing to a bake, check for a `Meta` pass, per [Meta pass](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-birp/meta-pass.md). Unity reports a missing one as a named warning about a fallback material being used, so search the console for the material name before theorising.

For a third-party shader you did not write, read its source before editing it, and tell the user you are modifying a vendor asset that an update will overwrite.

## 6. The shader is right but the material still looks wrong

| Symptom | Work these first |
|---|---|
| Point lights have no effect, directional works | The per-object additional-light limit per [Light limits in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/light-limits-in-urp.md), and whether the renderer is Forward or Forward+ |
| Lights work at a distance and drop out up close, or flicker | [Lights flicker or disappear](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/ts-lights-flicker-disappear.md) |
| Metallic or smoothness has no visible effect | There is no environment reflection to be metallic against. Hand this to the environment workflow rather than raising the values |
| Diffuse lighting works, specular does not | [Missing specular response](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/ts-missing-spec-response.md) |
| Transparent surface receives no shadow | Surface Type and Receive Shadows, per the [Lit shader reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/lit-shader.md) |
| Emissive material lights nothing around it | [Emissive materials not rendering](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/ts-emissive-mats-not-rendering.md) |
| Alpha clipping is enabled but the surface stays opaque | Read the material's Surface Type and its alpha clip threshold, and confirm the shader declares the alpha-clip keyword |

## 7. Anti-hallucination constraints

| Rule | Reason |
|---|---|
| Read the shader source or graph before describing what it does | A shader's name says nothing about which passes it declares |
| NEVER state a shader property, keyword or pass tag name from memory | These change between URP versions, and a wrong keyword fails silently rather than erroring |
| Distinguish a shader that fails to compile from one that compiles and is not lighting-aware | The first is magenta and the second is flat, and the fixes share nothing |
| NEVER report a material as fixed without reading back its shader assignment | A failed assignment leaves the old shader in place with no error |

## 8. Terms

Terms this workflow uses in a specific sense - key light, ambient, Contribute GI, light mode and the rest - are defined in `references/terms.md`.

## 9. Worked example

Reported: "the buildings render pink after we removed the HDRP package. Diagnose first, then repair only the confirmed broken materials."

1. Context: Unity 6000.6, `PC_RPAsset` active, no quality-level override.
2. Console shows no compile errors, so this is a missing shader rather than a broken one.
3. The affected materials point at shader references that no longer resolve. The package list confirms the HDRP package is gone.
4. Report that the meshes and material values are intact and only the shader references are broken, because the user's instruction not to recreate anything depends on that being true.
5. Say plainly that each material is reassigned by hand, per the conversion page, and that some properties will not carry across.
6. Ask once which URP shader family the buildings should use, listing the options and what each costs, because that is a look decision and not a fault.
7. Assign the chosen shader, then read each material back to confirm the assignment applied.
8. Verify: zero console errors, then read each material's shader name, capture the Game view and look at it. Play mode is not needed for a shader assignment.

## Verify

If the user asked you to prove it works or you suspect a specific fault, do these after the Final step's console check and capture:

1. Confirm no shader compile errors remain in the console.

Because the reported symptom is visual, state which values you verified and ask the user to confirm the appearance. You MUST NOT claim a visual result is correct from state reads alone.

If a check fails, go back and reread the relevant documentation from `references/topic-map.md` for what you missed. The cause is more often a missing pass or keyword than a wrong property value.

## Report

Include these in your final report:

- Which of the three failure shapes this was, and the evidence for it.
- Every material and shader you changed, and what you deliberately left alone.
- The documentation pages you read, by URL.
- Any property that could not be carried across a pipeline change, named individually.
- What you verified from state, and what still needs the user's eyes.
