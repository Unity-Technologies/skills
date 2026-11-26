# Terms as this skill uses them

Users, the Editor and the scripting API name some lighting settings differently. Use this mapping when you read or write a setting, and read the linked page for what the setting does, because the page matches the project's Unity version.

| Users and the workflows say | Editor label | API |
|---|---|---|
| [Ambient](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md) | Environment Lighting Source and Intensity Multiplier | `RenderSettings.ambientMode`, `RenderSettings.ambientIntensity` |
| [Environment reflections](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-reference/lighting-window.md) | Environment Reflections Source and Intensity Multiplier | `RenderSettings.defaultReflectionMode`, `RenderSettings.reflectionIntensity` |
| [Light mode](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting-light-sources/components/configuring/light-modes/choose.md) | Light Mode | `Light.lightmapBakeType`, which exists only in the Editor |
| [Contribute GI](https://docs.unity.com/en-us/engine/<VERSION>/manual/working-with-gameobjects/gameobject-fundamentals/static-objects.md) | Static: Contribute GI | `UnityEditor.StaticEditorFlags.ContributeGI` |
| [Shadow type](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/lighting/light-component.md) | Shadow Type | `Light.shadows` |
| [Lighting Data Asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/lighting-overview/direct-and-indirect-lighting/lightmap-data/lightmap-snapshot.md) | Lighting Data Asset | `UnityEditor.Lightmapping.lightingDataAsset` |
| [Resolved pipeline asset](https://docs.unity.com/en-us/engine/<VERSION>/manual/render-pipelines/choose-a-render-pipeline/srp-setting-render-pipeline-asset.md) | Render Pipeline Asset, per quality level | `UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline` |

Terms in this skill's own sense:

- **Key light**: the light the viewer reads as the main source, usually the one directional light. Its colour and angle carry most of the mood.
- **Resolved pipeline asset**: the pipeline asset that actually renders after the active quality level's override, which is often not the Graphics Settings default.
- **Baked GI enabled**: a setting, not evidence of a bake. Only lightmaps and a Lighting Data Asset show that a bake ran.
- **Normalised time**: one value from 0 to 1 for the position in a lighting cycle, from which every other value in the cycle is derived.
