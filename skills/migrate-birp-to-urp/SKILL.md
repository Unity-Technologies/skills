---
name: migrate-birp-to-urp
description: Plans, executes, and troubleshoots Unity projects moving from the Built-in Render Pipeline (BiRP/BIRP/Built-in RP) to the Universal Render Pipeline (URP). Use when the user asks to upgrade, convert, switch, or migrate a project, scene, material, or shader to URP/Universal Render Pipeline; fix pink or magenta materials after URP; convert Built-in materials/shaders; move a 2D project to URP 2D; review lighting, quality, post-processing, baked lightmaps, or reflection probes after URP; or diagnose visual problems after a render-pipeline migration.
---
Classify the request, inspect the current project state, choose the correct migration path, and validate the Built-in to URP migration outcome carefully.

## Mandatory Execution Reference

For any actual migration or repair pass, read [references/implementation-patterns.md](references/implementation-patterns.md) before the first `eval` call that edits project settings, materials, post-processing, lighting, probes, or scenes. This is mandatory even for short prompts such as "migrate this project to URP" when rollback safety is already confirmed.

If that resource cannot be loaded, use the embedded rules in this SKILL.md and lower your confidence: do not claim PPv2 conversion, lighting/probe refresh, or migration completion from intent or tool logs alone.

## Critical Default

For simple requests like "migrate this Built-in project to URP" or "move this project to URP", start or resume the safe migration workflow rather than jumping straight to conversion.

A generic upgrade prompt must not require the user to mention every migration risk. Inspect and handle common Built-in dependencies such as PPv2, baked lighting/lightmaps, reflection probes, particle/fog/smoke materials, Quality levels, camera post-processing, and representative scene validation.

Default result is phase-based:

1. Detect whether the project is Built-in, partially migrated, already URP, or HDRP.
2. Classify the request as standard 3D URP, URP 2D, selected-material conversion, planning-only, or troubleshooting.
3. Inspect migration risks before recommending changes: opaque materials, particle/VFX materials, custom shaders, post-processing, camera effects, baked lighting/lightmaps/reflection probes, Quality levels, and representative scenes.
4. Stop before any pipeline-mutating work until the user confirms a rollback point such as a branch, backup, archive, or disposable copy. Pipeline-mutating work includes installing URP, assigning a URP asset in Graphics/Quality settings, running converters, and editing materials or scenes.
5. Choose the next migration phase, execute that phase, save/re-query its outputs, and report its phase status.
6. Treat custom shaders, `GrabPass`, Surface Shaders, `OnRenderImage`, replacement shaders, and package-owned shaders as scoped follow-up work, not automatic converter work.
7. Validate representative scenes, Console output, camera rendering, lighting/baked GI state, materials, post-processing persistence, and quality-level assignments before declaring full migration success.

Default phases:

- Phase 0: Inspect and plan. Detect pipeline state, representative scenes, rollback safety, PPv2, material/shader risks, Quality levels, lighting/lightmap/probe state, and custom render code.
- Phase 1: URP setup and supported material conversion. Install/reuse URP, create/assign URP assets in Graphics/Quality, convert supported opaque and particle/effect materials, and verify no supported materials remain on Built-in shaders.
- Phase 2: Post-processing and camera migration. Create persistent URP Volume profiles, wire scene Volumes/camera post-processing, disable legacy PPv2 for URP validation, and classify unsupported PPv2 effects such as SSR.
- Phase 3: Lighting, lightmaps, and reflection probes. Resolve stale Built-in lighting state, configure URP lighting/probe settings, rebake/refresh when feasible, or clearly mark lighting/probes partial.
- Phase 4: Final validation and report. Save/reload/re-query project state, capture representative scenes, check Console, and report complete/incomplete/manual items.

One turn may complete multiple phases if the project is small and Unity remains stable, but do not force all phases into one response. Prefer an honest phase boundary over an over-claimed "complete" result.

Ask configuration questions only when the project state or user intent leaves a real decision unresolved. If the user says "do not modify files", stay in audit/planning mode and do not perform conversion or asset edits.

### Generic Upgrade Contract

When the user gives a generic migration request and permits changes, drive the next phase from the project state:

1. Do not ask the user to enumerate known Built-in features before inspection. Discover them.
2. If PPv2, baked lighting, reflection probes, particle/fog/smoke materials, or multiple Quality levels exist, include them in the migration automatically.
3. Prefer phase completion over all-in-one completion. Each phase should save/re-query its own outputs and end with `Phase complete`, `Phase partial`, or `Blocked`.
4. If Unity compilation, package installation, domain reload, or a long lighting bake interrupts the pass, report the interruption as a phase boundary and resume from saved partial state in the next turn rather than starting over.
5. Do not rely on the user writing a detailed checklist prompt to get correct behavior. The detailed checklist is for benchmarking or stress testing; normal user prompts should still trigger this phased contract.
6. A generic migration request gives permission to inspect and plan, but it does not prove rollback safety. If no rollback point is confirmed, stop before package install or URP assignment and ask for confirmation. Do not leave the project in a magenta partial state just to reach the backup question.
7. If rollback safety is already confirmed or the user says the project is a disposable copy, continue within the current phase without asking again for routine work.
8. At the end of a phase, give a concise next-phase prompt or say what phase should run next. Ask for confirmation before the next phase only when it introduces a new costly or risky operation, such as a long lighting bake, deletion/cleanup of legacy assets, or a custom shader rewrite.
9. If rollback safety was confirmed and ordinary work inside the active phase remains incomplete, do not ask "would you like me to continue?" Continue repairing that phase until its gate passes, a tool/domain-reload boundary interrupts execution, or a genuinely new risky decision appears.

### Generic Migration Success Gate

Before saying a generic migration is "successful", "complete", or "fully migrated", verify saved project state, not just tool logs:

1. Graphics settings and every relevant Quality level point to the intended URP asset.
2. The URP asset has a valid default renderer in saved state, and current Console output does not contain unresolved render-pipeline errors such as "Default Renderer is missing". If a renderer error appears after assignment, re-query the saved renderer list/default index, clear stale Console output when possible, trigger a fresh scene/camera validation, and continue repair instead of stopping at "URP setup complete".
3. Supported Built-in materials, including particle/fog/smoke materials, have URP-compatible shaders or are explicitly listed as unresolved custom/package shader cases. Textured/colorized source materials must preserve their source maps/colors; a URP shader assignment with missing `_BaseMap` or all-white `_BaseColor` is incomplete if the source `_MainTex` or `_Color` had content. Particle, fog, smoke, steam, decal, VFX, additive, and transparent effect materials should not be blindly forced to `Universal Render Pipeline/Lit`; prefer URP particle/effect shaders such as `Universal Render Pipeline/Particles/Unlit` when appropriate.
4. If the source scene used PPv2, a saved URP `VolumeProfile` exists with persistent non-null override components, the representative scene references it through a URP `Volume`, and legacy PPv2 is disabled for URP visual validation. A saved profile with `components: []`, `{fileID: 0}` component refs, or a scene that still only references the old `PostProcessProfile` is incomplete.
5. If the representative scene uses baked/mixed lighting, an Enlighten/realtime-GI Lighting Settings asset, a Lighting Data asset, lightmaps, light probes, or reflection probes, do not leave routine lighting/probe repair as manual follow-up during a generic full migration. Attempt URP-compatible Lighting Settings assignment, clear/rebake or resume a bake when needed, refresh reflection probes when feasible, then save/reload and verify the scene references the intended lighting state. Creating a `.lighting` asset and calling `Lightmapping.lightingSettings = target` plus `AssetDatabase.SaveAssets()` is not enough; mark/save the scene, reload/re-query, and compare the saved scene `m_LightingSettings` reference or asset GUID. If a long bake or tool interruption prevents this, call the migration partial and resume from that phase.
6. A representative scene has been opened or selected deliberately before migration validation; do not infer baked-lighting or PPv2 absence from a default/test scene. The scene has been captured or inspected after saving/reloading, and Console output has been checked for render-pipeline, shader, or renderer-feature errors.
7. Final wording must match the gate result. Use "complete" only when every gate passes. Use "partial" when URP is set up but PPv2 Volume persistence, legacy PPv2 disablement, lighting bake/probe refresh, or saved-state validation remains. Do not start the final response with "successfully migrated" if any required gate is partial.

Complete-report checklist:

- If the old PPv2 profile contains active `DepthOfField`, the new URP profile must contain a saved `DepthOfField` override or the final report must call it omitted/manual.
- If the old PPv2 profile contains active `AmbientOcclusion`, configure a URP SSAO renderer feature when feasible, or mark AO as manual/partial.
- If the old PPv2 profile contains active `ScreenSpaceReflections`, list SSR as unsupported/manual unless a URP renderer-feature/custom replacement was actually added and validated.
- If the scene still serializes the old Lighting Settings GUID or a non-zero old `m_LightingDataAsset`, lighting is partial.
- If the scene has reflection probes and the URP asset still serializes `m_ReflectionProbeBlending: 0` or `m_ReflectionProbeBoxProjection: 0`, reflections are partial unless intentionally disabled and reported.
- If any checklist item is false, do not use "complete", "fully migrated", or "fully functional on URP" in the final answer.

Unsupported features can remain manual, but name them precisely. Examples include PPv2 Screen Space Reflections needing a URP renderer-feature/custom/third-party replacement, complex custom shader ports, `GrabPass`, replacement shaders, or package-owned rendering code. If any success-gate item fails, continue repairing when allowed or report a partial migration with incomplete items; do not present the project as fully migrated.

Regression guard summary:

- Use the detailed [Execution Regression Checklist](references/implementation-patterns.md#execution-regression-checklist) for actual migration or repair work.
- Verify saved scene references before status claims: URP Volume/profile, Quality assignments, Lighting Settings/Data, URP asset/renderer, and reflection-probe settings.
- Preserve material source data from a pre-conversion snapshot, then restore `_BaseMap`, `_BaseColor`, texture scale/offsets, and relevant maps after shader changes.
- Validate representative visuals for PPv2, foliage, particles/effects, baked lighting, probes, and exposure before final wording.
- Report exact phase status. `Phase complete` is allowed for a passed phase; project-level `complete` is allowed only when every success gate passes.

## Execution path: running C# in the Editor

Every C# step in this skill runs inside a live Editor through the Unity CLI. **The `unity-cli`
skill owns getting you there** — installing the CLI, confirming a connected Editor, adding the
project's `com.unity.pipeline` package, telling a genuinely absent Editor apart from one stuck in
Safe Mode, and discovering the Editor's command catalog. Follow it first; don't re-derive any of
it here.

Two things it can't know for you:

- **You need `eval` in particular**, not just a reachable Editor. Confirm it appears in the
  catalog. Its presence depends on the Pipeline package version, not on the CLI, so a healthy
  install can still lack it — if it's missing, say so and stop.
- **A render-pipeline migration is not safely authorable blind.** Assigning a URP asset, converting
  materials, and rebaking lighting all need a live Editor. An unreachable Editor is a stop, not a
  cue to hand-edit `ProjectSettings/GraphicsSettings.asset`.

Run C# with `unity command eval --code '<snippet>'`. `unity command` defaults to a 30 second
timeout, which matters here: installing URP triggers a package refresh and domain reload that will
outlast it. Treat that as a phase boundary rather than raising the timeout.

### Passing C# to `eval`

`eval` compiles a **statement block, not a file**. Three consequences, all of which cause a compile
error rather than a warning:

- **No `using` directives.** The compiler reads `using UnityEngine;` as a resource-disposal
  statement and rejects it (`CS0210`).
- **Types must be fully qualified.** A bare `GraphicsSettings` or `Volume` does not resolve
  (`CS0246` / `CS0103`), and a bare `Object` is ambiguous with `object` (`CS0104`).
- **Extension methods are unavailable**, because they resolve through `using`. Two that this skill
  would otherwise reach for: `camera.GetUniversalAdditionalCameraData()` becomes
  `camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>()`, and LINQ
  calls must be written statically — `System.Linq.Enumerable.FirstOrDefault(sequence, predicate)`
  rather than `sequence.FirstOrDefault(predicate)`.

### Two execution modes — pick by the snippet's shape

The references carry both shapes, and they are not interchangeable:

- **Statement-shaped** (a bare sequence of statements, like the detection snippets above) — pass
  straight to `eval`, fully qualified, with no `using` lines.
- **Class- or method-shaped** (anything declaring a `class`, a `static` method, or a `[MenuItem]`,
  such as the material-snapshot pattern) — these are **project files, not `eval` input**. A class
  declaration cannot be flattened into a statement block. Save the snippet under
  `Assets/Editor/`, let Unity compile it, then invoke its entry point through a one-line `eval`
  call. Keep the `using` directives in that file; they are correct there.

For a multi-step migration the script route is the more reliable one anyway: it survives the domain
reloads that URP installation and material conversion trigger, whereas a long `eval` payload does
not.

### Detecting the active render pipeline

```csharp
var rp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline;
var qrp = UnityEngine.QualitySettings.renderPipeline;
return $"graphics={(rp == null ? "NULL (Built-in)" : rp.GetType().Name + ":" + rp.name)}, "
     + $"activeQualityLevel={(qrp == null ? "inherits Graphics" : qrp.GetType().Name + ":" + qrp.name)}";
```

A `UniversalRenderPipelineAsset` means URP; `HDRenderPipelineAsset` means HDRP; `NULL` on both
means Built-in. **Check the per-quality-level assignment too** — a project can be switched in
Graphics settings while a Quality level still points at a different asset, or at none:

```csharp
var names = UnityEngine.QualitySettings.names;
var rows = new System.Collections.Generic.List<string>();
for (int i = 0; i < names.Length; i++)
{
    var a = UnityEngine.QualitySettings.GetRenderPipelineAssetAt(i);
    rows.Add($"{i}:{names[i]}={(a == null ? "inherits Graphics" : a.name)}");
}
return string.Join(", ", rows);
```

If `GetRenderPipelineAssetAt` is unavailable in the project's Unity version, read
`ProjectSettings/QualitySettings.asset` instead rather than switching levels at runtime —
`SetQualityLevel` mutates project state.

## 0. Pre-Flight: Pipeline Detection and Reference Loading

Before doing anything else, you **must determine the active render pipeline and migration mode**:

1. **Detect Pipeline:** Run the render-pipeline detection snippet from the execution-path section above.
   - If `currentRenderPipeline` or `defaultRenderPipelineAsset` references a `UniversalRenderPipelineAsset` -> **URP**.
   - If no render pipeline asset is assigned -> **Built-in**.
   - If it references an `HDRenderPipelineAsset` -> **HDRP**. Explain that this skill only covers Built-in to URP migration. Basic comparison advice is fine, but do not drive an HDRP migration with this skill.
2. **Load Base Reference:** Read [references/migration-workflow.md](references/migration-workflow.md).
3. **Load Shader References When Relevant:** If the request mentions materials, shaders, magenta materials, rendering errors, image effects, or custom rendering, also read:
   - [references/custom-shader-triage.md](references/custom-shader-triage.md)
   - [references/complex-shader-situations.md](references/complex-shader-situations.md) when inspection or project-file search finds advanced shader/effect patterns
4. **Load Quality Reference When Relevant:** If the request mentions shadows, lighting, baked lighting, lightmaps, reflection probes, quality settings, visual mismatch, or performance after migration, also read [references/quality-settings-map.md](references/quality-settings-map.md).
5. **Load Implementation Patterns When Executing:** If the request permits actual migration changes, post-processing conversion, baked-lighting/probe repair, or resume from a partial migration, also read [references/implementation-patterns.md](references/implementation-patterns.md).
6. **Classify the Request** as one of:
   - Full-project Built-in to URP migration
   - Built-in 2D to URP 2D migration
   - Selected material-only conversion
   - Planning / explanation only
   - Troubleshooting after a previous migration
7. **If the project is already on URP,** switch to troubleshooting mode instead of re-running setup blindly.
8. **Proceed** only after the pipeline and migration path are clear.

## 1. Assess Current Migration State

Before making any changes, **inspect what already exists**:

1. **Inspect pipeline state and assignment points:**
   - Run both render-pipeline detection snippets from the execution-path section above — the Graphics-settings one and the per-quality-level one.
   - If the project has Quality levels, inspect which Render Pipeline Asset each level uses before assuming the project is fully switched.
2. **Inventory migration surfaces:** Use `eval` with `UnityEditor.AssetDatabase.FindAssets` or equivalent asset queries to inventory:
   - materials, including particle/VFX materials that often use built-in particle shaders
   - vegetation materials, including grass, tree, terrain-detail, SpeedTree, billboard, leaf-card, cutout, and wind-driven foliage materials
   - shaders
   - scenes and important prefabs
   - URP assets and renderer assets, if any
   - post-processing profiles / volume profiles
   - Lighting Settings assets, Lighting Data assets, lightmaps, light probes, reflection probes, and mixed/baked lights
3. **Scan for rendering risk markers:** Use available project-file search, `AssetDatabase.FindAssets`, or a short `eval` file scan to search the project for rendering-specific patterns such as:
   - `PostProcessLayer`, `PostProcessVolume`, `UnityEngine.Rendering.PostProcessing`
   - `OnRenderImage(`, `RenderWithShader`, `SetReplacementShader`
   - `#pragma surface`, `GrabPass`, `CGPROGRAM`
   - `CommandBuffer`, custom shader include paths, or package shader namespaces
   - vegetation markers such as `Nature/`, `SpeedTree`, `TreeCreator`, `Grass`, `Foliage`, `_Cutoff`, `_AlphaClip`, `_Cull`, billboard, or wind keywords
4. **If PPv2 is installed but grep finds nothing,** inspect open scenes and profile assets through Unity APIs by component/type. Scene YAML and serialized package references can be missed by text search.
5. **Determine project shape:** Decide whether the project is primarily 3D, primarily 2D, or mixed.
6. **Report Findings:** Summarize the current state before proposing conversion. Example:
   - "The project is still on Built-in, has no URP asset assigned, contains PPv2 references, and includes several custom shaders using surface-shader syntax."

## 2. Gather Requirements

Determine what the user actually wants. If the request is ambiguous, ask.

Use these defaults for common requests:

| User says | Default interpretation |
|-----------|------------------------|
| "Upgrade this project to URP" | Full-project migration |
| "Move this 2D project to URP" | Built-in 2D to URP 2D migration |
| "Convert these materials" | Targeted material conversion |
| "My materials turned pink" | Post-migration shader/material troubleshooting |
| "Lighting looks wrong after URP" | Quality and visual parity troubleshooting |
| "Do not change anything yet" | Planning / audit only |

### Information to Gather

- **Scope:** full project, selected materials, or troubleshooting only
- **Target renderer:** standard URP or URP 2D
- **Validation targets:** which scenes, prefabs, or cameras matter most to the user
- **Risk tolerance:** whether limited manual follow-up is acceptable
- **Rendering dependencies:** custom shaders, Asset Store shaders, PPv2, baked lighting/lightmaps/probes, image effects, command buffers, replacement shaders, or multiple Quality levels
- **Execution permission:** planning only vs actual project changes

## 3. Safety Gate

Before any pipeline-mutating step:

1. **Confirm rollback safety.**
   - NEVER install URP, assign a URP asset, run the Render Pipeline Converter, edit materials, or edit migration scene state before the user confirms a backup, branch, archive, disposable copy, or rollback point.
   - If rollback safety is missing, do not partially switch the project to URP. Stop in planning mode and ask for the rollback confirmation first.
2. **Separate engine-upgrade risk from pipeline-upgrade risk.**
   - If the project is also moving to a new Unity version, recommend doing the engine upgrade first and the pipeline migration second. Do not treat both as a single blind operation.
3. **If rollback safety is confirmed,** treat it as covering the whole migration pass. Do not ask again before each converter; proceed through setup, material conversion, PPv2 migration, lighting/probe work, save/reload verification, and final reporting unless a new risky decision appears.
4. **If the user already authorized a full disposable migration,** do not ask for permission to continue after routine setup or recoverable errors. Continue with the next incomplete migration phase. Ask only when a new destructive cleanup decision appears, a package/domain reload stops execution, or repeated repair attempts hit the validation iteration limit.
4. **If the project is already partially migrated,** identify whether rollback safety was previously confirmed. If yes, resume from the first incomplete item. If no, report the partial state and ask before making additional changes.

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
   - Do not spin-wait indefinitely inside an `eval` call on `UnityEditor.PackageManager.Client.Add`. Request installation, then verify through `Packages/manifest.json`, package listing, or the presence of URP types/assets after Unity finishes refreshing.
   - If the package install causes the `eval` call to time out or return nothing, resume in a fresh turn after Unity finishes compiling. Do not restart the whole migration or reinstall URP; inspect the partial state and continue from there.
2. **Create the required URP asset and renderer asset** if they do not already exist.
3. **Assign the URP asset in Graphics settings** and in all relevant Quality levels.
4. **For 2D projects,** create and assign the correct 2D Renderer asset before conversion.
5. **If a URP asset already exists,** inspect and reuse it when appropriate instead of creating duplicates automatically.
6. **Do not continue** until URP is actually the active render pipeline for the target configuration.

## 6. Conversion Workflow

For actual conversion work, follow [references/migration-workflow.md](references/migration-workflow.md).

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

When materials are magenta, shaders fail to compile, or visuals drift heavily, use [references/custom-shader-triage.md](references/custom-shader-triage.md) first.

1. **Identify affected materials and shaders.**
   - Inspect the exact material shader assignments, not just scene symptoms.
2. **Read Console and Inspector errors before editing shader code.**
3. **Classify the shader case:**
   - supported Built-in shaders that the converter should handle
   - simple custom shaders that might be ported safely
   - complex custom shaders that need a scoped migration plan
   - foliage/vegetation shaders that require cutout, two-sided leaves, billboards, terrain detail rendering, or wind behavior
   - package-owned or Asset Store shaders that may require maintainer documentation, scoped custom porting, or manual follow-up
4. **For complex shader situations,** use [references/complex-shader-situations.md](references/complex-shader-situations.md).
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

## 9. Quality, Lighting, and Visual Parity Review

Use [references/quality-settings-map.md](references/quality-settings-map.md) when the user mentions visual mismatch, shadows, performance, or quality settings.

1. **Review Graphics settings and each active Quality level.**
   - If a Quality level has no custom URP asset, state whether that level intentionally falls back to Graphics settings or still needs explicit assignment.
   - Do not claim all quality levels are migrated unless each relevant level has been inspected.
   - If using serialized project settings, the saved field is commonly `customRenderPipeline`; writing a guessed field such as `renderPipelineAsset` is not enough unless the saved asset proves the assignment.
   - If assigning through script, use the same pattern as URP's Render Settings converter: cache `QualitySettings.GetQualityLevel()`, call `QualitySettings.SetQualityLevel(index)` for each target level, assign `QualitySettings.renderPipeline = urpAsset`, then restore the original quality level and verify on disk.
2. **Check URP asset settings** that commonly affect parity:
   - shadows
   - shadow distance and cascades
   - MSAA
   - render scale
   - HDR, opaque texture, and depth texture settings when effects depend on them
3. **Do not promise identical lighting automatically.**
   - Built-in and URP can differ in light falloff, baked GI appearance, shadow tuning, reflection probe response, tonemapping, exposure, and post-processing behavior.
4. **For baked lighting scenes,** treat existing lightmaps and Lighting Data as reference material, not guaranteed-final URP output.
   - Inventory `Lightmapping.lightingSettings`, scene `LightmapSettings`, baked/mixed lights, light probes, reflection probes, and any existing `LightingDataAsset`.
   - Preserve baked data until the user has a visual reference or rollback point, but do not keep stale Built-in lightmaps active as the final URP lighting solution if they blow out or distort the scene.
   - If the scene looks blown out, too dark, or mismatched, first isolate post-processing/exposure by disabling legacy PPv2 during URP validation, then review URP Volume exposure/tonemapping/bloom before changing lights.
   - If clearing baked data makes the scene stop being blown out, identify the old lightmaps/Lighting Data as stale or incompatible active data. Then rebake under URP with the final URP asset, renderer, Volume, and Quality settings instead of tuning lights against the stale bake.
   - If creating or assigning URP-compatible `LightingSettings`, mark the lighting settings and active scene dirty, save the scene, reload or re-query, and verify the saved scene references the intended `.lighting` asset. `AssetDatabase.SaveAssets()` alone does not save the scene's `Lightmapping.lightingSettings` reference. Do not report "Baked GI enabled" if the saved scene still points at an Enlighten/realtime-GI settings asset.
   - When visual parity is part of the request and old `LightingData.asset` / reflection-probe EXRs remain from the Built-in bake, treat them as reference data until rebaked/refreshed under URP. Do not claim visual parity is preserved from old bake data alone.
   - Recommend clearing/rebaking lighting and refreshing reflection probes when visual parity matters. Do not claim baked lighting was successfully migrated unless a representative scene has been visually checked after URP setup, and preferably after a URP bake.
5. **For 2D lighting projects,** ensure sprites and tilemaps use URP-compatible lit materials where required.
6. **For performance regressions,** inspect whether the issue is coming from:
   - heavier URP asset settings
   - post-processing
   - extra shadow cost
   - custom shader ports or non-batched shaders
7. **When changing URP asset settings,** re-read the saved asset or query the property after saving before reporting values such as MSAA, additional-light limits, HDR, depth texture, or opaque texture.

## 10. Validation

After setup, conversion, or troubleshooting, validate the result:

1. **Capture the scene:** Capture the Scene View or a specific camera on a representative scene — see [references/capturing-the-editor.md](references/capturing-the-editor.md).
2. **Evaluate the result:**
   - no magenta materials unless unresolved custom shader blockers remain
   - main lighting and shadows behave as expected
   - baked GI/lightmaps, light probes, and reflection probes are acceptable or explicitly marked for rebake/refresh
   - cameras render expected content
   - post-processing or fullscreen effects still behave correctly
   - sprites, tilemaps, or 2D lights work when relevant
3. **Verify persistent project data, not only visual output.**
   - Inspect saved URP assets, renderer assets, scene references, material shader GUIDs/names, Quality settings, and Volume profiles.
   - Confirm the representative scene, not a default/test scene, was opened or otherwise inspected before concluding that baked lighting, PPv2, or reflection probes are absent.
   - For material migration, include particle/VFX materials in the verification. Remaining built-in particle shader IDs or legacy particle shader names mean material migration is incomplete.
   - For PPv2 migrations, saved URP `VolumeProfile` assets must contain the expected override components before reporting them as migrated.
   - If a tool log reports mapped post-processing but the saved profile reloads with `components: []`, override the tool log and report the migration as failed/incomplete.
   - For baked-lighting scenes, inspect Lighting Settings, Lighting Data/lightmap references, light probes, and reflection probes. Compilation success does not prove baked lighting parity. If old lightmaps cause overexposure, clear active baked data and rebake under URP before judging parity. After assigning new lighting settings, verify the saved scene reference, not only the existence of the new `.lighting` asset.
   - If old `LightingData.asset` remains assigned and reflection-probe EXRs were not regenerated or explicitly accepted after visual inspection, classify lighting/probes as preserved or partial rather than refreshed.
4. **Check Console output** with `Unity.GetConsoleLogs` for shader, render pipeline, or renderer-feature errors.
5. **Fix the highest-impact issue first,** then validate again.
6. **Repeat for at most 3 iterations** before reporting remaining blockers or asking the user how they want to proceed.

## 11. Troubleshooting Decision Tree

If the user reports a migration problem, follow this diagnostic flow:

### Project still behaves like Built-in after "migration"
1. Check whether a URP asset is assigned in Graphics settings.
2. Check whether the relevant Quality levels also point to a URP asset.
3. Verify that the active render pipeline is actually URP before troubleshooting anything else.

### Migration stopped after installing URP
1. Treat this as a package-refresh/domain-reload boundary, not a failed full migration by itself.
2. Re-check `Packages/manifest.json` and package state. If URP is installed, do not reinstall it.
3. Inspect for partial assets such as URP assets, renderer assets, converted materials, empty Volume profiles, and scene component changes.
4. If URP is installed/assigned but Built-in materials still dominate or the scene is magenta, material conversion is the immediate next incomplete item. Do not call URP setup complete and stop there if rollback safety has already been confirmed.
5. Continue from the first incomplete verification item: Graphics/Quality assignment, renderer asset validity, material conversion, PPv2-to-URP Volume migration, baked-lighting rebake, then final validation.
6. If the previous chat/export has no final response, report it as incomplete evidence and continue in a fresh chat/turn.

### Materials are magenta / bright pink
1. Check Console and Inspector for shader errors.
2. Confirm whether the material uses a supported Built-in shader, a custom shader, or a package-owned shader.
3. If it is a supported Built-in shader, review the converter path or targeted material conversion first.
4. If the project is already on URP and many supported Built-in materials remain, treat this as incomplete material conversion, not as final visual parity work.
5. If it is a custom or package shader, move into [references/custom-shader-triage.md](references/custom-shader-triage.md) and, when needed, [references/complex-shader-situations.md](references/complex-shader-situations.md).

### Scene is much darker, brighter, or just "wrong"
1. Compare Scene View and Game View captures.
2. Check for double post-processing first: retained PPv2 `PostProcessVolume` / `PostProcessLayer` plus an active URP `Volume` can overexpose or otherwise distort validation captures.
3. Review shadows, ambient/environment lighting, tone mapping, skybox, exposure, bloom, and post-processing.
4. Inspect baked lighting state: Lighting Settings, Lighting Data asset, lightmap references, mixed/baked lights, light probes, and reflection probes.
5. Review quality-level assignments and URP asset settings before rewriting content.
6. If clearing baked data fixes severe overexposure, treat the old lightmaps as stale active data: keep them only as reference/rollback evidence, then rebake under URP.
7. If the scene depends on baked lighting, recommend a URP rebake and reflection-probe refresh before claiming visual parity.
8. Remember that Built-in and URP light falloff can differ; treat that as a tuning task, not proof that conversion failed.

### Post-processing or fullscreen effects disappeared
1. Check whether PPv2 was converted and whether the target camera and volume setup are still valid.
2. Inspect saved URP `VolumeProfile` assets. Empty profiles mean PPv2 effects were not actually migrated.
3. Check whether old `PostProcessVolume` / `PostProcessLayer` components remain active in scenes.
4. Search for `OnRenderImage`, custom blit code, or replacement-shader camera effects.
5. Port the effect using a URP-compatible approach rather than trying to preserve the Built-in callback path unchanged.

### Transparent / refraction / distortion effects broke
1. Inspect whether the shader relied on `GrabPass` or a similar Built-in screen-copy workflow.
2. If so, use [references/complex-shader-situations.md](references/complex-shader-situations.md) and choose a Scene Color / Renderer Feature / custom-pass approach.

### 2D lights are not affecting sprites
1. Confirm the project is actually using the 2D Renderer.
2. Confirm existing sprite materials were upgraded to URP-compatible lit materials where needed.
3. Do not assume dragged-in new sprites prove old project materials are correct.

### Performance regressed after migration
1. Review render scale, shadows, additional lights, MSAA, post-processing, opaque/depth textures, and other URP asset settings.
2. Check whether custom shader ports lost batching compatibility or introduced extra passes.
3. Tune settings before assuming the only answer is a rollback.

### Custom shader compiles but visuals are still wrong
1. Determine whether the issue is simple parameter drift or a structural incompatibility.
2. If the shader came from a surface shader, `GrabPass`, replacement-shader workflow, or custom lighting path, treat it as a complex migration case.
3. Validate one representative material before rolling the approach out project-wide.

## 12. Core Guardrails

- Detect the active render pipeline first; for HDRP, explain the mismatch and offer only basic comparison guidance.
- Do not mutate the project before rollback safety is confirmed; instead stop in planning mode before URP install, asset assignment, converters, material edits, or scene edits.
- Do not treat tool logs or created assets as proof; instead save/reload/re-query Graphics, Quality, materials, Volumes, lighting, probes, Console, and representative scenes before status claims.
- Do not collapse partial phases into project-level completion; instead report `Phase complete`, `Partial migration`, and `Manual follow-up` based on the success gate.
- Do not bulk-rewrite fragile rendering code; instead use scoped mappings for custom shaders, `GrabPass`, `OnRenderImage`, replacement shaders, package-owned render code, and unsupported PPv2 effects.

## 13. Reporting Back

Summarize:

- which migration path was used
- whether the project is still Built-in, partially migrated, or fully on URP
- which converters ran
- what was fixed automatically
- what still needs manual work
- which scenes, materials, or cameras were validated
- what remains risky, especially around complex shaders, package effects, and visual parity
- whether post-processing was fully migrated, partially prepared, or only documented for manual follow-up
- whether Quality levels are explicitly assigned or intentionally relying on Graphics settings fallback
- whether particle/VFX materials were converted or still need URP particle-shader follow-up
- whether baked lighting/lightmaps/reflection probes were preserved as reference, verified visually, rebaked/refreshed, or left as manual follow-up
- whether exposure, tonemapping, ambient fill, and camera/Volume post-processing wiring were visually balanced or still remain partial

Use completion language conservatively. If the report contains any `Partial`, `Manual follow-up`, `not validated`, `may still need`, or unsupported-feature item, state that specific completed phases passed and call the overall migration partial/manual. Do not pair a project-level "complete" claim with manual follow-up bullets.

## References

- [Migration Workflow](references/migration-workflow.md)
- [Custom Shader Triage](references/custom-shader-triage.md)
- [Complex Shader Situations](references/complex-shader-situations.md)
- [Quality Settings Map](references/quality-settings-map.md)
- [Implementation Patterns](references/implementation-patterns.md)

When this skill is activated, proactively read [references/migration-workflow.md](references/migration-workflow.md). If the request involves materials, shaders, magenta materials, custom rendering, or fullscreen effects, also read [references/custom-shader-triage.md](references/custom-shader-triage.md) and [references/complex-shader-situations.md](references/complex-shader-situations.md). If the request involves visual mismatch, lighting, baked lighting, lightmaps, reflection probes, shadows, or performance after migration, also read [references/quality-settings-map.md](references/quality-settings-map.md). If the user permits actual migration changes or asks to continue/repair a partial migration, also read [references/implementation-patterns.md](references/implementation-patterns.md).
