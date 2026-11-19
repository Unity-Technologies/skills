# Assess Current Migration State

How to inspect what already exists before proposing conversion.

Links to other reference files point to files in this `references/` folder. Any other relative paths resolve to the skill directory.

## 1. Assess Current Migration State

Before making any changes, **inspect what already exists**:

1. **Inspect pipeline state and assignment points:**
   - Run both render-pipeline detection snippets from [references/eval-execution.md](eval-execution.md) — the Graphics-settings one and the per-quality-level one.
   - If the project has Quality levels, inspect which Render Pipeline Asset each level uses before assuming the project is fully switched.
2. **Inventory migration surfaces:** Use `script eval` with `UnityEditor.AssetDatabase.FindAssets` or equivalent asset queries to inventory:
   - materials, including particle/VFX materials that often use built-in particle shaders
   - vegetation materials, including grass, tree, terrain-detail, SpeedTree, billboard, leaf-card, cutout, and wind-driven foliage materials
   - shaders
   - scenes and important prefabs
   - URP assets and renderer assets, if any
   - post-processing profiles / volume profiles
   - Lighting Settings assets, Lighting Data assets, lightmaps, light probes, reflection probes, and mixed/baked lights
3. **Scan for rendering risk markers:** Use available project-file search, `AssetDatabase.FindAssets`, or a short `script eval` file scan to search the project for rendering-specific patterns such as:
   - `PostProcessLayer`, `PostProcessVolume`, `UnityEngine.Rendering.PostProcessing`
   - `OnRenderImage(`, `RenderWithShader`, `SetReplacementShader`
   - `#pragma surface`, `GrabPass`, `CGPROGRAM`
   - `CommandBuffer`, custom shader include paths, or package shader namespaces
   - vegetation markers such as `Nature/`, `SpeedTree`, `TreeCreator`, `Grass`, `Foliage`, `_Cutoff`, `_AlphaClip`, `_Cull`, billboard, or wind keywords
4. **If PPv2 is installed but grep finds nothing,** inspect open scenes and profile assets through Unity APIs by component/type. Scene YAML and serialized package references can be missed by text search.
5. **Determine project shape:** Decide whether the project is primarily 3D, primarily 2D, or mixed.
6. **Report Findings:** Summarize the current state before proposing conversion. Example:
   - "The project is still on Built-in, has no URP asset assigned, contains PPv2 references, and includes several custom shaders using surface-shader syntax."
