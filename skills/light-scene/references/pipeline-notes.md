# Pipeline notes

The workflows are written for URP. On the Built-in Render Pipeline or HDRP the order of work is the same, but many settings live somewhere else and some values mean something else. This file sends each step to the page that documents it for that pipeline, and adds only the decisions those pages leave to you. Read the page for the step you are on before you change anything, because the facts belong to the pipeline and its version, not to this skill.

## Contents

- Built-in Render Pipeline
- High Definition Render Pipeline

## Built-in Render Pipeline

| When a workflow step works on | Read instead |
|---|---|
| A pipeline asset's shadow, light-limit or render-scale settings | [Quality settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-settings-reference/comp-manager-group/class-quality-settings.md) for each quality level, because Built-in has no pipeline asset, and [Rendering paths in the Built-in Render Pipeline](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/built-in-render-pipeline/built-in-rendering-paths.md) for the per-object light limit |
| Shadow distance or cascades | [Shadow distance](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-realtime/shadow-distance.md) and [Shadow cascades](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-realtime/shadow-cascades/shadow-cascades.md) |
| A Volume, exposure, tonemapping or a grade | [Post Processing Stack v2](https://docs.unity3d.com/Packages/com.unity.postprocessing@<VERSION>/manual/index.html) and its [Color Grading](https://docs.unity3d.com/Packages/com.unity.postprocessing@<VERSION>/manual/Color-Grading.html), if the project has that package |
| Probes for moving objects | [Light Probe Group](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/class-light-probe-group.md) |
| The lighting mode of a bake | [Lighting Mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/lighting-mode/lighting-mode.md) and the [render pipeline feature comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/feature-comparison.md) |
| A custom shader's passes | [Pass tags](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/reference/sl-reference/sl-sub-shader-pass/sl-pass-tags.md), [Surface shaders](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/writing-shaders-birp/writing-surface/sl-surface.md) and [Meta pass](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-birp/meta-pass.md) |
| Sky, ambient, reflections or fog | [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md), [Using skyboxes](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skyboxes-using.md) and [Reflection Probe component](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections/class-reflection-probe.md) |

Decisions on Built-in:

- If the project has no post-processing package, say so and build the look from lights and ambient, because there is no Volume to grade with.
- Map a URP or HDRP material to a Built-in shader by hand, and list what did not carry over, because no converter works in that direction.

## High Definition Render Pipeline

| When a workflow step works on | Read instead |
|---|---|
| Light intensity | [Physical light units](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Physical-Light-Units.html) and [Light component](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Light-Component.html) |
| Exposure | [Exposure](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Override-Exposure.html) |
| The sky, ambient or environment | [Visual Environment](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Override-Visual-Environment.html), [Create an HDRI sky](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/create-an-hdri-sky.html), [Create a physically based sky](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/create-a-physically-based-sky.html), [Create a gradient sky](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/create-a-gradient-sky.html) and [Environment lighting](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Environment-Lighting.html) |
| Shadow distance, cascades or resolution | [Shadows](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Override-Shadows.html) and [HDRP Asset](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/HDRP-Asset.html) |
| A feature that does nothing | [Frame Settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Frame-Settings.html) |
| Shadowmask, probes or reflections | [Shadowmask](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Lighting-Mode-Shadowmask.html), [Use Adaptive Probe Volumes](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/probevolumes-use.html), [Reflection Probe](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Reflection-Probe.html), [Planar Reflection Probe](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Planar-Reflection-Probe.html) and [Screen Space Reflection](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Override-Screen-Space-Reflection.html) |
| Fog, tonemapping or Volumes | [Fog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/fog.html), [Volumetric lighting](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Volumetric-Lighting.html), [Tonemapping](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Post-Processing-Tonemapping.html) and [Understand volumes](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/understand-volumes.html) |
| Render scale | [Dynamic resolution](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Dynamic-Resolution.html) |
| Magenta materials or a custom shader | [Convert a project from the Built-in Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/convert-project-from-built-in-render-pipeline.html) and [Lit material](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/lit-material.html) |

Decisions on HDRP:

- Set exposure before you judge any light, because under the wrong exposure correct lights read black or blown.
- Never copy an intensity from a URP scene, because HDRP measures light in physical units. Set each light from its role, using the units page.
- When a feature does nothing, check Frame Settings before you change the feature, because a camera can switch it off.
- Before a bake, check that the sky reaches the bake as the Environment lighting page describes, because otherwise the bake has no sky light.
