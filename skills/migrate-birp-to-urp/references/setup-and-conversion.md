# Setup and Conversion

Choosing the migration path, setting up URP, converting materials, and triaging shaders, post-processing, and camera effects.

Links to other reference files point to files in this `references/` folder. Any other relative paths resolve to the skill directory.

## Contents

- 4. Choose the Migration Path
- 5. URP Setup Workflow
- 6. Conversion Workflow
- 7. Shader and Material Triage
- 8. Post-Processing, Cameras, and Rendering-Effect Triage

## 4. Choose the Migration Path

Use the correct path for the project:

### Path A: Standard 3D Built-in to URP
- Use this for normal 3D Built-in projects moving to standard URP.

### Path B: Built-in 2D to URP 2D
- Use this when the project is primarily 2D and the user expects URP 2D lighting or renderer behavior.
- Do not treat this as the same workflow as standard URP.

### Path C: Targeted Material-Only Conversion
- Use this only when the project is already on URP and the user wants selected materials converted.

### Path D: Troubleshooting an Existing Migration
- Use this when the project is already on URP or the migration has already been attempted.
- Prioritize the highest-impact breakages instead of re-running the entire migration blindly.

## 5. URP Setup Workflow

Follow this exact setup order:

1. **Ensure URP is installed.**
   - Installing URP can trigger package refresh, compilation, and domain reload. Treat this as a phase boundary.
   - Do not spin-wait indefinitely inside a `script eval` call on `UnityEditor.PackageManager.Client.Add`. Request installation, then verify through `Packages/manifest.json`, package listing, or the presence of URP types/assets after Unity finishes refreshing.
   - If the package install causes the `script eval` call to time out or return nothing, resume in a fresh turn after Unity finishes compiling. Do not restart the whole migration or reinstall URP; inspect the partial state and continue from there.
2. **Create the required URP asset and renderer asset** if they do not already exist.
3. **Assign the URP asset in Graphics settings** and in all relevant Quality levels.
4. **For 2D projects,** create and assign the correct 2D Renderer asset before conversion.
5. **If a URP asset already exists,** inspect and reuse it when appropriate instead of creating duplicates automatically.
6. **Do not continue** until URP is actually the active render pipeline for the target configuration.

## 6. Conversion Workflow

For actual conversion work, follow [migration-workflow.md](migration-workflow.md).

1. **Choose the correct Render Pipeline Converter path:**
   - `Built-in Render Pipeline to URP`
   - `Built-in Render Pipeline 2D to URP 2D`
2. **Initialize converters and inspect the candidate changes** before converting.
3. **For standard Built-in to URP migration,** prefer the applicable converters described in the reference:
   - `Rendering Settings`
   - `Material Upgrade`
   - `Animation Clip Converter`
   - `Read-only Material Converter`
   - `Post-processing Stack v2 Converter`
4. **For selected-material conversion,** use the targeted material conversion workflow instead of converting the whole project.
5. **Review warnings and failures before converting.**
6. **Run the conversion only after the user confirms the project is safe to change.**
7. **Verify which shader each material actually landed on** — do not assume the converter chose the 3D
   target. `MaterialUpgrader.FetchAllUpgradersForPipeline` returns the 2D provider set alongside the 3D
   one, and both claim `Standard` at equal priority, so on a 3D project a plain `Standard` material can
   silently convert to `Universal Render Pipeline/2D/Mesh2D-Lit-Default` instead of
   `Universal Render Pipeline/Lit`. Nothing errors and the material does not go magenta, so this is
   invisible unless you read the shader name back — measured on 6000.5.8f1.

   Read every converted material's `shader.name` afterwards and confirm it matches the intended target
   from the mapping table. If any landed on a `2D/` shader in a 3D project, restore those materials from
   the rollback point and convert them with a 3D-filtered upgrader list, or with the manual pattern in
   [implementation-patterns.md](implementation-patterns.md).
7. **After conversion, verify the saved project state.**
   - Re-open or re-query representative assets instead of trusting command output alone.
   - Save both assets and open scenes after scene-level edits. Changes to scene components, camera data, active lighting settings, reflection probes, and PPv2 enable states are not proven by `AssetDatabase.SaveAssets()` alone.
   - Confirm the URP asset is assigned in Graphics and intended Quality levels.
   - For Quality levels, validate by re-querying `QualitySettings.GetRenderPipelineAssetAt(i)` or by reading the saved `customRenderPipeline` entries in `ProjectSettings/QualitySettings.asset`. If they remain `{fileID: 0}`, the Quality levels are not explicitly assigned.
   - Do not call a guessed quality API such as `QualitySettings.SetRenderPipelineAssetAt`; in Unity versions where that API is unavailable, switch to each target level with `QualitySettings.SetQualityLevel(index)`, set `QualitySettings.renderPipeline = urpAsset`, then restore the original level and verify the saved `customRenderPipeline` values.
   - Confirm converted materials actually reference URP shaders.
   - Confirm representative materials that had source albedo textures now have non-null `_BaseMap` values. This check must compare against a pre-conversion material snapshot; do not use post-conversion `_MainTex` as the source of truth.
   - Confirm particle/VFX materials no longer reference Built-in particle shader IDs or legacy shader names; use URP particle shaders such as `Universal Render Pipeline/Particles/Unlit` where appropriate. If material names or paths include `Particle`, `Fog`, `Smoke`, `Steam`, `VFX`, `Additive`, or similar effect terms, preserve transparent/additive behavior rather than defaulting them to URP Lit.
   - Confirm vegetation materials preserve cutout/alpha clipping, render face intent, textures, tint, normals, and expected wind/billboard behavior. If grass/tree cards become solid or static because their specialized shader behavior was lost, material conversion is partial.
   - If PPv2 conversion was attempted, confirm saved URP Volume profiles contain persistent non-null `components` entries and the scene/camera wiring uses URP-compatible components.
   - If baked lighting exists, preserve the old Lighting Data and lightmaps as reference data, then verify whether they still produce acceptable URP visuals. If a new Lighting Settings asset is created, verify the saved scene actually references it after saving/reloading; the asset existing on disk is not enough. In an actual generic migration, attempt rebaking/refreshing when stale baked data or reflection probes affect parity; if that cannot finish in the current turn, report the migration as partial and resume from the lighting/probe phase later.
   - Do not claim "refreshed reflection probes" from intent. Verify new or updated probe outputs, changed saved reflection-probe assets, or successful probe-render tool output. If the existing EXR files remain unchanged, say probes were preserved as reference or still need refresh.
   - If any verification fails, report the item as incomplete/manual follow-up, not as migrated.

## 7. Shader and Material Triage

When materials are magenta, shaders fail to compile, or visuals drift heavily, use [custom-shader-triage.md](custom-shader-triage.md) first.

1. **Identify affected materials and shaders.**
   - Inspect the exact material shader assignments, not just scene symptoms.
2. **Read Console and Inspector errors before editing shader code.**
3. **Classify the shader case:**
   - supported Built-in shaders that the converter should handle
   - simple custom shaders that might be ported safely
   - complex custom shaders that need a scoped migration plan
   - foliage/vegetation shaders that require cutout, two-sided leaves, billboards, terrain detail rendering, or wind behavior
   - package-owned or Asset Store shaders that may require maintainer documentation, scoped custom porting, or manual follow-up
4. **For complex shader situations,** use [complex-shader-situations.md](complex-shader-situations.md).
5. **Prefer the smallest safe fix.**
   - Restore rendering on representative materials first.
   - Validate the result before scaling the fix to more assets.
6. **Never bulk search-and-replace shader code across the whole project** unless the mapping is explicit, tested, and scoped.

## 8. Post-Processing, Cameras, and Rendering-Effect Triage

Built-in projects often rely on more than material conversion.

1. **If PPv2 is present,** inspect the converter output and validate the resulting URP volumes, profiles, and camera behavior.
   - Inventory existing `PostProcessVolume`, `PostProcessLayer`, and PPv2 `PostProcessProfile` assets before conversion.
   - Prefer the URP `Post-processing Stack v2 Converter` when available instead of hand-building equivalent profiles from memory.
   - After conversion, verify that saved URP `VolumeProfile` assets contain persistent override components, not an empty `components: []` profile or dangling `{fileID: 0}` component references.
   - If creating a profile by script, `VolumeProfile.Add<T>()` only creates a component object; for persistent profile assets, also call `AssetDatabase.AddObjectToAsset(component, profile)`, mark the profile and component dirty, save assets, reload the asset, and count saved non-null components before reporting success. If the saved profile has `{fileID: 0}` component entries, recreate or repair the profile before claiming migration success.
   - Verify representative scenes contain the intended `Volume` objects and that target cameras have `UniversalAdditionalCameraData` with post-processing enabled when required.
   - If the source scene still only serializes a PPv2 `sharedProfile` reference and no URP `Volume` references the new `VolumeProfile`, PPv2 migration is incomplete even if a URP profile asset exists on disk.
   - If an active URP `Volume` references an empty profile, treat that as incomplete scene wiring, not as active post-processing. Repair the profile or report PPv2 as partial.
   - Check whether old `PostProcessVolume` / `PostProcessLayer` components remain. If they remain, explain whether they are intentionally retained, harmless legacy leftovers, or unresolved migration work.
   - If old PPv2 components are retained only as reference while a URP Volume is active, disable the old PPv2 component/layer for URP visual validation to avoid double post-processing. Do not delete it until parity is accepted or the user approves cleanup.
   - Do not remove PPv2 packages, components, or profiles as a cleanup step until a verified URP replacement exists or the user accepts that those effects will be dropped/manual follow-up.
   - During an actual migration, do not leave common PPv2 parity as a vague manual task if the source profile is available. Create persistent URP overrides for common mappable effects such as Bloom, Color Adjustments/Tonemapping, Vignette, and Depth of Field, then verify the saved profile has non-null components.
   - It is acceptable to mark unsupported or non-equivalent effects as manual follow-up, such as PPv2 Screen Space Reflections or Ambient Occlusion that should become a renderer feature / SSAO setup instead of a direct Volume override.
   - If URP Volume overrides cannot be created reliably, document the PPv2 effects and their URP equivalents instead of claiming they were migrated.
2. **Do not treat transient tool success as post-processing success.**
   - A command log that says "Added Bloom" is not enough. Re-read the saved `VolumeProfile` asset or inspect the scene after saving/reloading.
   - If the saved profile is empty, say the PPv2 setup was documented or partially prepared, not migrated.
3. **If grep finds `OnRenderImage`,** treat it as a custom full-screen effect case.
   - In URP, custom full-screen effects should move toward `ScriptableRenderPass`, a Renderer Feature, or URP custom post-processing instead of staying on the Built-in image-effect path.
4. **If grep finds `RenderWithShader` or `SetReplacementShader`,** treat it as a replacement-shader case.
   - These effects often need a deliberate URP renderer-feature or custom-pass strategy.
5. **If an effect depends on scene color, depth, or normals,** verify the URP-compatible path rather than assuming the Built-in approach still applies.
6. **If custom cameras were stacking effects in Built-in,** validate camera output explicitly after migration instead of assuming parity.
7. **For advanced shader/effect troubleshooting questions, provide concrete replacement patterns.**
   - For `GrabPass`, mention `_CameraOpaqueTexture` / Scene Color, the required URP asset setting, and the limitation that transparent ordering can differ from Built-in.
   - For `OnRenderImage`, mention `ScriptableRendererFeature` plus `ScriptableRenderPass`. When showing a Unity 6-style template, use the reference pattern with a temporary `RTHandle` and `Blitter.BlitCameraTexture`.
   - Never recommend `Blitter.BlitCameraTexture(cmd, source, source, material, pass)` or other source-to-source blits as the main `OnRenderImage` replacement. If you are not going to show the safer temporary-target pattern, omit the code sample and explain the architecture instead.
   - For Surface Shaders, state that `#pragma surface` has no direct URP equivalent and choose Shader Graph or a URP HLSL vertex/fragment rewrite based on effect complexity.
   - Do not stop at "rewrite it"; give the user a practical first porting step and a validation target.
