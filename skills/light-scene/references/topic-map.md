# Documentation topic map

The pages each workflow relies on, grouped by workflow and then by stage. Every link carries `<VERSION>`: replace it with the project's Unity version, for example `6000.3`, before fetching. Open only the pages for the step you are about to do, and name the ones you read. URP pages are in the main manual; some are landing pages, so follow them down. HDRP pages are in the `com.unity.render-pipelines.high-definition` package manual; Built-in pages are in the main manual. Both are indexed in the last section.

## Contents

- Set up scene lighting
- Diagnose scene lighting
- Bake scene lighting
- Style scene lighting
- Configure environment lighting
- Fix material lighting response
- Optimize lighting for a platform
- Animate scene lighting
- Built-in and HDRP pages

## Set up scene lighting

- **Order and pipeline**
  - [Lighting configuration workflow](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-configuration-workflow.md) - the ordered stages. Read this first; it is the backbone of this workflow.
  - [Set the render pipeline asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/srp-setting-render-pipeline-asset.md) - how the active pipeline asset is chosen, including the per-quality-level override.
  - [Render pipeline feature comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/feature-comparison.md) - what differs across pipelines.
  - [Lighting in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting.md) - URP lighting behaviour and settings.
  - [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md) - what Unity 6 changed about baking and environment lighting, which decides how a new setup is finished.
- **Environment and light sources**
  - [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md) - Environment Lighting Source and Intensity Multiplier.
  - [Ambient light](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/lighting-ambient-light.md) - what ambient contributes.
  - [Light component types](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/lighting.md) - Point, Spot, Directional and Area lights and what each is for.
  - [Light sources](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources.md) - light components, ambient light and cookies.
  - [URP Light component](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/light-component.md) - the URP-specific light properties.
  - [Direct and indirect lighting](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting.md) - which contributions are direct and which require a bake.
- **Shadows**
  - [Shadow configuration](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-configuration.md) - shadow settings and how to configure them.
  - [Optimize shadow rendering in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/shadows-in-urp/shadows-optimization.md) - shadow cost, including Soft Shadows on tile-based mobile platforms.
  - [Lights flicker or disappear](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/ts-lights-flicker-disappear.md) - lights downgraded from per-pixel to per-vertex in Forward, and the additional-light limit.
- **Probes and baking**
  - [Choose a light mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/choose.md) - the Baked, Mixed and Realtime comparison matrix.
  - [Lightmapping](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/lightmapping.md) - what baking produces.
  - [Light Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/light-probes.md) - lighting dynamic objects from baked light.
  - [Use Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/use.md) - the Unity 6 probe system; check for it before adding a Light Probe Group.
  - [Reflection Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections/reflection-probes.md) - reflections, and why glossy surfaces need one.

## Diagnose scene lighting

- **Setup and pipeline**
  - [Lighting configuration workflow](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-configuration-workflow.md) - the ordered stages Unity expects lighting to be configured in.
  - [Render pipeline feature comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/feature-comparison.md) - the documented matrix of lighting, shadow, ambient and probe differences across pipelines.
  - [Set the render pipeline asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/srp-setting-render-pipeline-asset.md) - how the active pipeline asset is chosen, including the per-quality-level override.
  - [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md) - Environment Lighting Source and Intensity Multiplier.
  - [Ambient light](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/lighting-ambient-light.md) - what ambient light contributes.
  - [Lighting in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting.md) - URP lighting behaviour and settings.
- **Shadows**
  - [Shadow performance and failures](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-performance.md) - documented reasons realtime shadows fail to appear.
  - [Shadow cascades](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-realtime/shadow-cascades/shadow-cascades.md) - cascades and perspective aliasing, the usual cause of blocky directional shadows.
  - [URP shadow resolution](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/shadows-in-urp/shadow-resolution-urp.md) - URP shadow resolution settings.
  - [URP shadow troubleshooting](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/shadows-in-urp/shadows-troubleshooting-urp.md) - bias, shadow acne, peter-panning, light leaking.
  - [Optimize shadow rendering in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/shadows-in-urp/shadows-optimization.md) - shadow cost, including Soft Shadows on tile-based mobile platforms.
  - [Lights flicker or disappear](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/ts-lights-flicker-disappear.md) - lights downgraded from per-pixel to per-vertex in Forward.
- **Baking and global illumination**
  - [Bake lighting](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/bake.md) - running a bake.
  - [Choose a light mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/choose.md) - the Baked, Mixed and Realtime comparison matrix.
  - [Choose a light baking backend](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/configure/progressive-lightmapper.md) - the available baking backends and which one to use.
  - [No baked global illumination](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/troubleshooting/ts-no-baked-global-illum.md) - causes of missing baked GI, including backend, driver and GI cache steps.
  - [Mixed lights render as baked](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/lighting-mode/ts-mixed-lights-render-as-baked.md) - Mixed lights silently demoted to Baked.
  - [Emissive materials not rendering](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/ts-emissive-mats-not-rendering.md) - emissive materials not contributing light.
  - [Troubleshooting objects appearing unlit by Light Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/ts-objects-missing-lighting.md) - objects that render black with no probes to sample, including glossy surfaces with no reflection probe.
  - [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md) - what Unity 6 changed about baking and environment lighting.

## Bake scene lighting

- **Before the bake**
  - [Lightmapping](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/lightmapping.md) - what baking produces and what it needs.
  - [Choose a light mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/choose.md) - the Baked, Mixed and Realtime comparison matrix.
  - [Light modes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/light-modes.md) - what each mode contributes.
  - [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md) - what Unity 6 changed about baking and environment lighting.
- **Running the bake**
  - [Bake lighting](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/bake.md) - running a bake.
  - [Choose a light baking backend](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/configure/progressive-lightmapper.md) - the available backends and which one to use.
  - [GPU lightmapper](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/gpuprogressive-lightmapper.md) - GPU-specific requirements and limits.
  - [Lightmap Parameters](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/class-lightmap-parameters.md) - resolution and quality settings per object.
- **Probes**
  - [Light Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/light-probes.md) - lighting dynamic objects from baked light.
  - [Use Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/use.md) - the Unity 6 probe system; check for it before adding a Light Probe Group.
  - [Reflection Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections/reflection-probes.md) - reflections for glossy surfaces.
  - [Reflection probes in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/reflection-probes.md) - URP-specific behaviour.
- **When it goes wrong**
  - [Lightmapping troubleshooting](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/troubleshooting.md) - the index of bake failures.
  - [No baked global illumination](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/troubleshooting/ts-no-baked-global-illum.md) - causes of an empty bake, including backend, driver and GI cache steps.
  - [Mixed lights render as baked](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/lighting-mode/ts-mixed-lights-render-as-baked.md) - Mixed lights silently demoted.
  - [Lighting Data Asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmap-data/lightmap-snapshot.md) - what the bake writes and where.

## Style scene lighting

- **Light and environment**
  - [Light component types](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/lighting.md) - the light types and what each is for.
  - [URP Light component](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/light-component.md) - colour, temperature and intensity as URP exposes them.
  - [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md) - Environment Lighting Source and Intensity Multiplier.
  - [Ambient light](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/lighting-ambient-light.md) - what ambient contributes to mood.
  - [Using skyboxes](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skyboxes-using.md) - the sky as a light and colour source.
  - [Shadow configuration](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-configuration.md) - shadow softness and strength, which carry a lot of mood.
- **Grading and post-processing**
  - [Post-processing in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/integration-with-post-processing.md) - how URP applies post-processing.
  - [Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes.md) - global and local volumes, and how overrides blend.
  - [Tonemapping](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/effect-list/tonemapping.md) - highlight rolloff. Check this before touching anything else.
  - [Color Adjustments](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/effect-list/color-adjustments.md) - exposure, contrast, saturation, colour filter.
  - [Bloom](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/effect-list/bloom.md) - glow around bright areas.
  - [Vignette](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/effect-list/vignette.md) - edge darkening.

## Configure environment lighting

- **Sky and background**
  - [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md) - the Environment tab: Skybox Material, Sun Source, Environment Lighting Source and Intensity Multiplier, Environment Reflections, and Fog under Other Settings.
  - [Use a skybox](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skyboxes-using.md) - assigning a skybox per scene and per camera, and the Skybox component that overrides the scene.
  - [Skybox shaders](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skybox-shaders.md) - which shader a skybox material must use.
  - [Skybox material reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skybox-material-reference.md) - the properties of each skybox shader.
  - [Procedural skybox](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skybox-material-reference/shader-skybox-procedural.md), [Panoramic skybox](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skybox-material-reference/shader-skybox-panoramic.md), [Cubemap skybox](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skybox-material-reference/shader-skybox-cubemap.md), [6-sided skybox](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skybox-material-reference/shader-skybox-6sided.md) - the four skybox shader families and what each one needs.
  - [Panoramic video on a skybox](https://docs.unity.com/en-us/engine/<VERSION>/manual/video/player/panoramic/skybox.md) - driving a skybox from a video player.
  - [URP camera component reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/cameras/urp/camera-components-reference/camera-component-reference.md) - Background Type, and Render Type for camera stacking.
- **Ambient and reflections**
  - [Ambient light](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/lighting-ambient-light.md) - what ambient contributes and where its source is chosen.
  - [Reflections](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections.md) - how reflection sources combine.
  - [Reflection probes in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/reflection-probes.md) and [their introduction](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/reflection-probes/introduction.md) - probe types, baking, and realtime probes.
  - [Reflection probe troubleshooting](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/reflection-probes/troubleshooting.md) - documented reasons a probe produces no visible reflection.
  - [Reflection probe reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections/class-reflection-probe.md) - probe properties including box projection.
  - [Troubleshooting objects appearing unlit by Light Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/ts-objects-missing-lighting.md) - objects that render black with no probes to sample, including glossy surfaces with no reflection probe.
  - [Unity 6 upgrade guide](https://docs.unity.com/en-us/engine/<VERSION>/manual/upgrade-guides/upgrade-guide-unity6.md) - what Unity 6 changed about environment lighting, which decides what a scene shows before a bake.
- **Fog and volumes**
  - [Settings and keywords reference for stripping keywords in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/built-in/shaders-in-universalrp/shader-stripping/features-and-keywords.md) - the Fog row: the settings that strip fog from a build.
  - [Volumes in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes.md) and [Volume troubleshooting](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes-troubleshooting.md) - Volume priority, layer masks and blend distance.
  - [Emissive materials](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-birp/lighting-emissive-materials.md) - emissive surfaces as an environment contribution.

## Fix material lighting response

- **Pipeline migration and magenta materials**
  - [Render Pipeline Converter](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/rp-converter.md) - the supported way to convert built-in materials to URP in bulk.
  - [Upgrade a material](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/materials/upgrade-material.md) - what an upgrade does to a material and its properties.
  - [Upgrading from the Built-In Render Pipeline](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp.md) - the whole migration path.
  - [Render pipeline feature comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/feature-comparison.md) - the documented matrix of lighting, shadow, ambient and probe differences across pipelines.
  - [Shaders in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/built-in/shaders-in-universalrp.md) and [choosing one](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/built-in/shaders-in-universalrp/choose.md) - which URP shader replaces which built-in shader.
- **Custom shaders and lighting**
  - [URP ShaderLab pass tags](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/writing-custom-shaders-urp/use-built-in-shader-methods/urp-shaderlab-pass-tags.md) - the `LightMode` tags URP renders, per rendering path.
  - [Upgrade custom shaders from built-in to URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/birp-urp-custom-shader-upgrade-guide.md) - the per-feature mapping for hand-written shaders.
  - [Upgrading your shaders](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/introduction/installing-and-configuring-urp/upgrading-from-birp/upgrading-your-shaders.md) - version-to-version shader changes.
  - [Use shadows in a custom URP shader](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/writing-custom-shaders-urp/use-built-in-shader-methods/shadows.md) and [Render additional lights in a shader in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/custom-lighting/use-built-in-shader-methods-additional-lights-fplus.md) - the keywords a shader must declare to receive shadows and additional lights.
  - [Shader keywords](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/writing-custom/shader-writing/sl-multiple-program-variants/shader-keywords.md) - how keywords are enabled, and why an undeclared one silently does nothing.
  - [Built-in lighting methods in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/writing-custom-shaders-urp/use-built-in-shader-methods/lighting.md) and [additional lights in Forward+](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/custom-lighting/use-built-in-shader-methods-additional-lights-fplus.md) - the functions a custom shader calls to be lit.
  - [Meta pass](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-birp/meta-pass.md) - the pass a shader needs to contribute to baked global illumination.
- **Per-material behaviour**
  - [Lit shader](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/lit-shader.md), [Complex Lit shader](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/shader-complex-lit.md), [Unlit shader](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/unlit-shader.md) - what each responds to.
  - [Light limits in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/light-limits-in-urp.md) - the per-object additional-light limit, and how Forward+ changes it.
  - [Lights flicker or disappear](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/ts-lights-flicker-disappear.md) - lights downgraded from per-pixel to per-vertex in Forward.
  - [Missing specular response](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/ts-missing-spec-response.md) - surfaces that receive diffuse light but no specular.
  - [Troubleshooting objects appearing unlit by Light Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/ts-objects-missing-lighting.md) - objects that render black with no probes to sample, including glossy surfaces with no reflection probe.
  - [Emissive materials not rendering](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/ts-emissive-mats-not-rendering.md) - emissive materials not contributing light.

## Optimize lighting for a platform

- **Where the settings live**
  - [Set the render pipeline asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/srp-setting-render-pipeline-asset.md) - how the active asset is chosen, including the per-quality-level override. Read this first.
  - [Quality Settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-settings-reference/comp-manager-group/class-quality-settings.md) - the quality levels and what each controls.
  - [URP quality settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/urp-quality-settings.md) - the URP asset's own quality options.
  - [Universal Renderer](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/universal-render-pipeline/urp-reference/urp-universal-renderer.md) - rendering path, and what Forward, Forward+ and Deferred cost.
- **Shadow cost**
  - [Optimize shadow rendering in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/shadows-in-urp/shadows-optimization.md) - the main reference, including the cost of Soft Shadows on tile-based mobile platforms.
  - [Shadow cascades](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-realtime/shadow-cascades/shadow-cascades.md) - cascade count against shadow quality at distance.
  - [URP shadow resolution](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/shadows-in-urp/shadow-resolution-urp.md) - shadow map size.
  - [Shadow performance](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-performance.md) - general shadow cost and why shadows disappear.
- **Light cost**
  - [Lights flicker or disappear](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/ts-lights-flicker-disappear.md) - the additional-light limit, and lights downgraded to per-vertex.
  - [Choose a light mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/choose.md) - baked light is cheaper at runtime than realtime light.
  - [Optimize for better performance](https://docs.unity.com/en-us/engine/<VERSION>/manual/analysis/graphics-performance-profiling/in-urp/optimize-for-better-performance.md) - broader URP performance guidance.
  - [Rendering Debugger](https://docs.unity.com/en-us/engine/<VERSION>/manual/analysis/graphics-performance-profiling/in-urp/understand-performance/rendering-debugger.md) - inspecting what is actually being rendered.

## Animate scene lighting

- **What changes over time**
  - [Light component types](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/lighting.md) - the sun is a directional light; its angle is the clock.
  - [URP Light component](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/light-component.md) - the light properties you can drive.
  - [Ambient light](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/lighting-ambient-light.md) - ambient must move with the sun or dusk looks wrong.
  - [Lighting window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md) - Environment Lighting Source and Intensity Multiplier.
  - [Using skyboxes](https://docs.unity.com/en-us/engine/<VERSION>/manual/creating-environments/sky/skyboxes-using.md) - procedural skyboxes track a sun; texture skyboxes do not.
- **Baked lighting that changes**
  - [Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes.md) - the landing page for APV.
  - [Understand Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/concept.md) - what APV stores and how it differs from Light Probes.
  - [Use Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/use.md) - setting them up, including lighting scenarios.
  - [Display and adjust Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/showandadjust.md) - inspecting the result.
  - [Fix issues with Adaptive Probe Volumes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/probevolumes/fixissues.md) - when blending looks wrong.
  - [Light Probes](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/light-probes.md) - the older system, for comparison.
  - [Lightmapping](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmapping/baking-before-runtime/lightmapping.md) - why static lightmaps cannot follow a moving sun.

## Built-in and HDRP pages

The pages behind the pipeline notes. Manual links take the Unity version, as above. Package links take the installed package's version instead, such as `17.6` for HDRP; check the package version if unsure.

- **Built-in Render Pipeline**
  - [Quality settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity-editor/editor-settings-reference/comp-manager-group/class-quality-settings.md) - Pixel Light Count, shadow distance, cascades and resolution, per quality level; Built-in has no pipeline asset.
  - [Rendering paths in the Built-in Render Pipeline](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/built-in-render-pipeline/built-in-rendering-paths.md) - the Forward path's per-object light limit, against Deferred.
  - [Shadow distance](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-realtime/shadow-distance.md) and [Shadow cascades](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/shadows/shadow-realtime/shadow-cascades/shadow-cascades.md) - the main shadow-cost settings.
  - [Light Probe Group](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/light-probes/class-light-probe-group.md) - probes for moving objects, since Built-in has no Adaptive Probe Volumes.
  - [Lighting Mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/lighting-mode/lighting-mode.md) and [Render pipeline feature comparison](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/feature-comparison.md) - which Mixed lighting modes each pipeline supports.
  - [Meta pass](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-birp/meta-pass.md) - how a shader contributes to baked global illumination.
  - [Pass tags](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/reference/sl-reference/sl-sub-shader-pass/sl-pass-tags.md) and [Surface shaders](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/shaders/writing-shaders-birp/writing-surface/sl-surface.md) - what a Built-in lit shader needs.
  - [Reflection Probe component](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/reflections/class-reflection-probe.md) - reflection probes, which work as they do on URP.
  - [Post Processing Stack v2](https://docs.unity3d.com/Packages/com.unity.postprocessing@<VERSION>/manual/index.html) and its [Color Grading](https://docs.unity3d.com/Packages/com.unity.postprocessing@<VERSION>/manual/Color-Grading.html) - tonemapping and grading on Built-in, when the package is installed.
- **High Definition Render Pipeline**
  - [Physical light units](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Physical-Light-Units.html) - lux, lumen, candela and EV100; URP intensities do not transfer.
  - [Light component](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Light-Component.html) - HDRP's light properties.
  - [Exposure](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Override-Exposure.html) - the Volume override to set before judging any intensity.
  - [Visual Environment](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Override-Visual-Environment.html) - chooses the sky type and the ambient mode.
  - [Create an HDRI sky](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/create-an-hdri-sky.html), [Create a physically based sky](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/create-a-physically-based-sky.html) and [Create a gradient sky](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/create-a-gradient-sky.html) - the sky overrides.
  - [Environment lighting](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Environment-Lighting.html) - ambient light from the sky, and the Static Lighting Sky a bake needs.
  - [Shadows](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Override-Shadows.html) - the Volume override for cascade count and maximum distance.
  - [Frame Settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Frame-Settings.html) - per-camera switches that can turn lighting, shadows, fog or post-processing off.
  - [HDRP Asset](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/HDRP-Asset.html) - shadow atlases, quality tiers and what is compiled in.
  - [Shadowmask](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Lighting-Mode-Shadowmask.html) - the Shadowmask lighting mode in HDRP.
  - [Use Adaptive Probe Volumes](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/probevolumes-use.html) - probes and lighting scenarios in HDRP.
  - [Reflection Probe](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Reflection-Probe.html), [Planar Reflection Probe](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Planar-Reflection-Probe.html) and [Screen Space Reflection](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Override-Screen-Space-Reflection.html) - reflections in HDRP.
  - [Fog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/fog.html) and [Volumetric lighting](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Volumetric-Lighting.html) - fog as a Volume override, volumetric fog included.
  - [Tonemapping](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Post-Processing-Tonemapping.html) - HDRP's tonemapping modes.
  - [Understand volumes](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/understand-volumes.html) - the Volume framework HDRP shares with URP.
  - [Dynamic resolution](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/Dynamic-Resolution.html) - HDRP's equivalent of render scale.
  - [Convert a project from the Built-in Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/convert-project-from-built-in-render-pipeline.html) and [Lit material](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@<VERSION>/manual/lit-material.html) - upgrading materials to HDRP.
