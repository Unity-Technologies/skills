# Built-in to URP Migration Workflow

Use this workflow to follow the official Unity migration flow and identify when to slow down, branch, or stop.

## Table of Contents

- [Official References](#official-references)
- [Safety First](#safety-first)
- [Project Preparation](#project-preparation)
- [Phased Migration Model](#phased-migration-model)
- [Standard 3D Path: Built-in Render Pipeline to URP](#standard-3d-path-built-in-render-pipeline-to-urp)
- [What Each Converter Does](#what-each-converter-does)
- [Targeted Material-Only Path](#targeted-material-only-path)
- [2D Path: Built-in Render Pipeline 2D to URP 2D](#2d-path-built-in-render-pipeline-2d-to-urp-2d)
- [2D Lighting Note](#2d-lighting-note)
- [Baked Lighting, Lightmaps, And Reflection Probes](#baked-lighting-lightmaps-and-reflection-probes)
- [Manual Follow-Up Hot Spots](#manual-follow-up-hot-spots)
- [When To Stop And Escalate](#when-to-stop-and-escalate)

## Official References

- [Upgrading from the Built-in Render Pipeline to URP](https://docs.unity3d.com/Manual/urp/upgrading-from-birp.html)
- [Convert assets using the Render Pipeline Converter](https://docs.unity3d.com/Manual/urp/features/rp-converter.html)
- [Upgrade material assets to URP or HDRP](https://docs.unity3d.com/Manual/upgrade-material.html)
- [URP asset](https://docs.unity3d.com/Manual/urp/urp-asset-and-renderer.html)
- [Prepare and upgrade sprites for 2D lighting in URP](https://docs.unity3d.com/Manual/urp/PrepShader.html)
- [Set up a render pipeline](https://docs.unity3d.com/Manual/render-pipelines-set-up.html)
- [Update believable visuals in URP and HDRP](https://docs.unity3d.com/Manual/BestPracticeMakingBelievableVisuals0.html)

## Safety First

- Render Pipeline Converter changes are one-way. Run conversion only after the user has confirmed a backup, branch, or rollback point.
- Installing URP and assigning a URP asset can also leave the project in a partial magenta state before materials are converted. If rollback safety is not confirmed, stop before package install or Graphics/Quality assignment, not after.
- If the user has confirmed rollback safety or says this is a disposable copy, treat that confirmation as covering the full migration pass. Do not ask again before each converter unless a new risky decision appears.
- Inspect warnings before and after conversion instead of treating converter output as success by default.
- If the project is also moving to a new Unity version, the safer order is: engine upgrade first, render-pipeline migration second.

## Project Preparation

Before conversion:

1. Inspect whether the project is truly still on Built-in or already partially on URP.
2. Identify likely migration hot spots:
   - custom shaders
   - Asset Store rendering packages
   - Post-processing Stack v2
   - image effects or camera callbacks
   - replacement shaders or special camera rendering paths
   - multiple quality levels with different graphics expectations
   - baked lighting, lightmaps, light probes, and reflection probes
3. If rendering plug-ins or packages are clearly Built-in-specific, do not assume they will survive the migration unchanged.

Do this even when the user gives a short prompt like "upgrade this project to URP." The skill, not the user, is responsible for discovering these common migration surfaces.

## Phased Migration Model

Use phases for real project upgrades. A generic "migrate this project to URP" prompt starts or resumes the next phase; it does not require finishing every fragile subsystem in one response.

| Phase | Goal | Completion gate |
| --- | --- | --- |
| Phase 0: Inspect and plan | Detect pipeline state, representative scene, rollback safety, PPv2, materials, Quality levels, lighting/probes, custom render code | Findings are reported and rollback safety is confirmed before mutation |
| Phase 1: URP setup and materials | Install/reuse URP, create/assign URP asset/renderer, assign Graphics/Quality, convert supported opaque and particle/effect materials | Saved Graphics/Quality and material shader assignments verify correctly |
| Phase 2: Post-processing and cameras | Convert PPv2 to persistent URP Volumes/profiles, wire cameras, disable legacy PPv2 for validation, classify unsupported PPv2 effects | Saved URP profile has non-null expected overrides, scene references it, legacy PPv2 is disabled or clearly preserved for comparison |
| Phase 3: Lighting and probes | Resolve stale Built-in lighting data, configure URP lighting/probe settings, rebake/refresh where feasible | Saved scene lighting/probe state is verified, or the phase is explicitly partial/blocked |
| Phase 4: Final validation | Save/reload/re-query, capture representative scenes, check Console, report complete/incomplete/manual items | Every success gate passes before using "complete" |

At the end of each phase, report:

- `Completed`: saved-state evidence for that phase.
- `Incomplete`: specific failed checks and whether they are repairable.
- `Manual follow-up`: unsupported or intentionally deferred items.
- `Next phase`: the exact phase to run next.

Continue into the next phase only when Unity remains stable, the next phase does not introduce a new costly/risky operation, and the user has already authorized that scope. Prefer a clear phase boundary over a rushed final answer.

## Standard 3D Path: Built-in Render Pipeline to URP

Use this path for normal 3D Built-in projects moving to URP.

For a generic "migrate/upgrade to URP" request, treat the following as part of the standard phased path unless inspection proves they are absent: URP asset/renderer setup, Graphics and Quality assignment, material conversion, particle/VFX material verification, PPv2-to-URP Volume verification, baked-lighting/lightmap/probe verification, representative scene capture, saved-state re-query, and a final complete/incomplete/manual report.

Do not downgrade common migration surfaces into manual follow-up just because the user used a short prompt. Discover PPv2, stale baked lighting, lightmaps, reflection probes, particle materials, and Quality-level assignments. A generic migration can be called complete only after these surfaces are either verified in saved project state or precisely reported as unsupported/manual edge cases. Until then, report the current phase as complete or partial instead of reporting the whole migration as complete.

Before step 1, confirm rollback safety. If no backup, branch, archive, or disposable copy is confirmed, do not install URP or assign a URP asset. Explain that those steps can immediately make supported Built-in materials render magenta until material conversion runs.

Before and after pipeline assignment, deliberately open or inspect the representative scene(s). Do not infer that baked lighting, PPv2, reflection probes, or lightmaps are absent from a default/test scene.

1. Ensure URP is installed.
   - Package installation is a phase boundary. It may trigger package refresh, compilation, and domain reload.
   - Do not rely on a single `PackageManager.Client.Add` tool call to complete the rest of the migration. If the chat/tool call stops or returns `null`, wait for Unity to finish compiling, then resume by inspecting the partial project state.
   - If `Packages/manifest.json` already contains `com.unity.render-pipelines.universal`, do not reinstall URP; continue from asset setup and validation.
2. Create the URP asset and renderer asset if they do not already exist.
3. Assign the URP asset in Graphics settings. Assign it in relevant Quality levels as well.
   - If the Console reports "Default Renderer is missing", do not stop at setup. Re-query the saved URP asset's renderer list/default index, validate a fresh scene/camera render after save/reload, and either repair the renderer asset or mark the migration partial.
4. Open `Window > Rendering > Render Pipeline Converter`.
5. Select `Built-in Render Pipeline to URP`.
6. Initialize converters and inspect the result set.
7. Run the converters that apply to the project:
   - `Rendering Settings`
   - `Material Upgrade`
   - `Animation Clip Converter`
   - `Read-only Material Converter`
   - `Post-processing Stack v2 Converter`
8. Re-open representative scenes and validate before doing broad cleanup.
9. Verify saved assets after conversion. Do not rely only on converter or command logs.
   - If the migration changed scene objects, camera components, PPv2 enable states, active Lighting Settings, or reflection probes, save the open scene as well as project assets. `AssetDatabase.SaveAssets()` does not prove scene changes persisted.
10. For Quality levels, verify saved `customRenderPipeline` entries or re-query `QualitySettings.GetRenderPipelineAssetAt(i)`. A tool log that says Quality levels were assigned is not enough.
11. If a script must assign Quality-level URP assets, follow URP's converter pattern: cache the current quality level, call `QualitySettings.SetQualityLevel(index)` for each target level, set `QualitySettings.renderPipeline = urpAsset`, restore the original level, save, and verify the saved `customRenderPipeline` entries. Do not use guessed APIs such as `QualitySettings.SetRenderPipelineAssetAt`.
12. If representative scenes use baked lighting, verify the baked-lighting state separately. Do not treat a clean compile or material conversion as proof that lightmaps and probes look correct in URP.
13. Before the final response, run a success gate:
   - Saved Graphics and relevant Quality levels point at the intended URP asset.
   - The saved URP asset has a valid default renderer and current validation is not blocked by renderer errors.
   - Saved material assets, including particles/fog/smoke/VFX materials, use URP-compatible shaders or are listed as unresolved custom/package shader cases. Materials that had source textures/colors still have mapped URP textures/colors; white/gray untextured output is not a successful material conversion.
   - If PPv2 existed, the saved URP Volume profile has persistent non-null `components` entries and the scene references the URP `Volume`; `components: []` means incomplete.
   - If baked/mixed lighting, Enlighten/realtime-GI settings, Lighting Data, lightmaps, light probes, or reflection probes existed, lighting/probe repair has been attempted or the migration is explicitly partial because that phase is still running/incomplete.
   - If a new URP `.lighting` asset was created, the saved scene's `m_LightingSettings` reference points to that asset after save/reload. Creating the asset and calling `Lightmapping.lightingSettings = target` is not sufficient evidence.
   - A representative scene was inspected or captured after saving/reloading.
   - If the representative capture is materially too dark, blown out, or high contrast, post-processing wiring and exposure/tonemapping/ambient balance have been verified and adjusted, or visual parity is explicitly partial.
   - The final status wording matches the gate result. Say "complete" only when every required item passes. Say "partial" when URP setup/material conversion succeeded but PPv2, baked lighting, probes, or saved-state validation still need repair.

If only Phase 1 is complete, say "Phase 1 complete" and list Phase 2/3/4 as next work. Do not label the whole migration complete from Phase 1 evidence.

## What Each Converter Does

- `Rendering Settings`: creates URP assets and maps Built-in settings to URP equivalents.
- `Material Upgrade`: converts supported Built-in materials. It does not solve custom shader compatibility.
- Particle and VFX materials need explicit verification. If they still point at built-in particle shaders or built-in shader IDs after conversion, migrate them to URP particle shaders such as `Universal Render Pipeline/Particles/Unlit` where appropriate.
- Do not blindly force particle, fog, smoke, steam, decal, additive, VFX, or transparent effect materials to `Universal Render Pipeline/Lit`. Preserve transparent/additive behavior where possible, and validate off-screen/inactive particle systems by asset inspection rather than only by the current camera.
- `Animation Clip Converter`: runs after material conversion and helps when animation clips affect material properties or Post-processing Stack v2 properties.
- `Read-only Material Converter`: handles built-in read-only materials such as `Default-Diffuse` and can take longer because it indexes the project.
- `Post-processing Stack v2 Converter`: converts PPv2 volumes, profiles, and related camera data to URP equivalents and can also take longer because it indexes the project.

## Post-Processing Verification

PPv2 conversion is successful only if the saved project state proves it.

After any PPv2 migration attempt:

1. Re-open or re-query the converted scene.
2. Inspect old `PostProcessVolume`, `PostProcessLayer`, and `PostProcessProfile` usage.
3. Inspect the resulting URP `VolumeProfile` asset. A profile with `components: []` is empty and should not be reported as migrated. A profile with `components` entries that point to `{fileID: 0}` is also invalid and should not be reported as migrated.
   - If the profile was created by script, `VolumeProfile.Add<T>()` must be paired with `AssetDatabase.AddObjectToAsset(component, profile)` for persistent assets. Mark both the component and profile dirty, save assets, reload it from `AssetDatabase`, and verify the non-null component count persisted.
   - If the saved profile contains `{fileID: 0}` entries where Bloom, Tonemapping, Vignette, or Depth Of Field should be, treat that as broken persistence. Repair or recreate the profile before claiming PPv2 parity.
4. Confirm representative cameras have URP camera data and post-processing enabled when the visual target depends on it.
5. Confirm the scene has the intended URP `Volume` setup, or explicitly document that the PPv2 setup still needs manual migration.
   - If the scene still serializes only the old PPv2 `sharedProfile` and no URP `Volume` references the new `VolumeProfile`, PPv2 migration is incomplete even if a URP profile asset exists.
   - If a scene URP `Volume` references a profile with `components: []`, the scene has empty URP post-processing plumbing, not migrated post-processing. Repair the profile or mark PPv2 as incomplete.
6. If PPv2 effects include Screen Space Reflections, call out that URP has no direct equivalent in the same built-in PPv2 form. Recommend reflection probes, screen-space reflection alternatives, or a custom/third-party solution when required.
7. For common mappable PPv2 effects, create persistent URP overrides instead of leaving a vague manual parity task. Typical mappings include Bloom -> `UnityEngine.Rendering.Universal.Bloom`, Color Grading/exposure -> `ColorAdjustments` plus `Tonemapping`, Vignette -> `Vignette`, and Depth Of Field -> `DepthOfField`.
8. It is acceptable to mark unsupported or structurally different effects as manual follow-up, such as PPv2 Screen Space Reflections or Ambient Occlusion that should be handled through reflection probes, SSAO renderer features, or custom/third-party rendering.
9. If old PPv2 components remain as a reference while a URP Volume is active, disable the old PPv2 layer/component during URP visual validation to avoid double post-processing. Do not delete it until the saved URP replacement has been verified and the user approves cleanup.
10. Do not uninstall PPv2 or remove `PostProcessLayer` / `PostProcessVolume` components until the saved URP replacement has been verified, unless the user explicitly accepts effect loss or manual follow-up.

If a command log says effects were added but the saved profile is empty, report the result as a failed or incomplete post-processing migration and do not claim success. If legacy PPv2 is still active while an empty URP profile exists, do not claim URP post-processing is active; state that the old PPv2 setup is preserved but the URP replacement is incomplete.

## Material And Particle Verification

After material conversion:

1. Re-query material shader assignments on disk or through `AssetDatabase`.
2. Confirm standard opaque materials use URP-compatible shaders, usually `Universal Render Pipeline/Lit`.
3. Confirm particle, fog, smoke, steam, decal, VFX, and transparent effect materials are not left on Built-in particle shaders.
4. Do not rely only on "no magenta in the current camera" as proof. Some particle systems may be off-screen, inactive, or visually subtle.
5. If a material is custom, package-owned, or effect-heavy, classify it as manual follow-up instead of forcing it to URP Lit.
6. For vegetation, grass, tree, terrain-detail, SpeedTree, billboard, leaf-card, and wind-driven materials, verify more than the shader name. Preserve alpha cutoff/clipping, face/culling intent, textures, tint, normals, and expected wind/billboard behavior where possible. If foliage becomes solid opaque cards, static when it was wind-driven, or visually loses its specialized behavior, report the foliage set as partial/manual instead of calling material conversion complete.
7. For any converter or manual shader-assignment fallback, prove source material data survived from a pre-conversion snapshot. Cache `_MainTex`, `_Color`, normal, metallic/specular, emission, alpha/cutoff, texture tiling/offset, and source shader before conversion, then verify representative converted assets have non-null `_BaseMap`/expected textures and non-default color values when the source did.
8. Do not run a "repair" that copies `_MainTex` to `_BaseMap` after conversion unless the value comes from the pre-conversion snapshot. Once a shader has changed, `_MainTex` may already be null/default; copying it later can silently preserve the broken state.
9. Do not edit immutable package materials under `Packages/` or `Library/PackageCache`. If those assets appear in the conversion set, classify them as package-owned/read-only and use a local material copy or scoped custom replacement instead.
10. If a representative capture shows broad white/gray untextured objects after conversion, or if snapshot comparison shows source materials had albedo textures but converted materials have null `_BaseMap`, treat the material phase as incomplete even if the materials are no longer magenta.

If rollback safety was already confirmed, continue repairing ordinary material, renderer, PPv2, lighting, or probe failures instead of asking the user whether to continue. Ask only when a new destructive cleanup choice, unsupported custom shader strategy, long-running phase boundary, or repeated validation failure requires the user's decision.

## Partial Migration Resume Checklist

If a migration stops after installing URP, after a domain reload, after a phase boundary, or after a long-running bake:

1. Re-check whether URP is now installed in `Packages/manifest.json`.
2. Check whether a URP asset and renderer asset exist and are valid.
3. Verify Graphics settings and every relevant Quality level point to the intended URP asset.
4. Re-query material shader assignments, including particle/fog/smoke/VFX materials. If supported Built-in shader GUIDs or names still dominate after URP is assigned, material conversion is the immediate next incomplete phase; do not stop at "URP setup complete" when the scene is still magenta.
5. Inspect PPv2 state and URP Volume profiles. Empty `components: []` profiles are incomplete.
6. Inspect baked lighting/lightmap state. If old lightmaps blow out the scene, preserve them as reference, clear active baked data, rebake under URP, and refresh reflection probes.
   - If a new URP-compatible Lighting Settings asset exists, verify the saved scene actually references it. A scene that still references the old Enlighten/realtime-GI `.lighting` asset is not fully repaired.
   - If old `LightingData.asset` remains assigned and old reflection-probe EXRs are unchanged, do not say lighting or probes were refreshed. Report them as preserved reference data or partial until a URP bake/probe refresh completes.
7. If a previous run ended at "would you like me to continue?" but rollback safety was already confirmed, resume from the first incomplete item instead of restarting or asking again.
8. Capture a representative scene and report exactly which items are complete, incomplete, or manual follow-up.
9. Name the next phase instead of restarting. For example: "Resume at Phase 2: Post-processing and cameras" or "Resume at Phase 3: Lighting and probes."

## Targeted Material-Only Path

Use this only when the user wants selected materials converted instead of a full migration.

1. Confirm the project is already on URP.
2. Select the Built-in materials in the Project window.
3. Use `Edit > Rendering > Materials > Convert Selected Built-in Materials to URP`.
4. If a material still errors in the Inspector or stays magenta, treat it as a shader triage case rather than retrying blindly.
5. Validate one or two representative scenes after conversion because selected material upgrades can still create visual drift.

## 2D Path: Built-in Render Pipeline 2D to URP 2D

Use this path when the project is primarily 2D and the target is URP 2D.

1. Ensure URP is installed.
2. Create and assign a 2D Renderer asset.
3. Open `Window > Rendering > Render Pipeline Converter`.
4. Select `Built-in Render Pipeline 2D to URP 2D`.
5. Use `Material and Material Reference Upgrade`.
6. If the user wants 2D lighting, verify that sprites and materials are using URP-compatible lit materials after conversion.

## 2D Lighting Note

For 2D lighting in URP, Unity can assign `Sprite-Lit-Default` when sprites are dragged into the scene. Existing project materials still need to be upgraded if they should react to 2D lights.

## Baked Lighting, Lightmaps, And Reflection Probes

Baked lighting does not become visually correct just because the project compiles after URP setup.

For scenes with baked or mixed lighting:

1. Preserve existing Lighting Data, lightmaps, light probes, and reflection probes as reference data until visual parity is accepted.
2. Inventory `Lightmapping.lightingSettings`, scene `LightmapSettings`, baked/mixed lights, light probes, reflection probes, and the scene's Lighting Data asset.
3. Check whether the migrated scene is overexposed or too dark because both old PPv2 and new URP Volume effects are active. For URP validation, disable legacy PPv2 components/layers but do not delete them.
4. Review URP Volume exposure, tonemapping, bloom, and color adjustments before changing light intensities.
5. If the scene is still too dark, blown out, or high contrast after PPv2/URP Volume migration, run an exposure-balance check before the final report. Verify renderer `PostProcessData`, camera `renderPostProcessing`, `volumeLayerMask`, active Volume weight/priority/layer, and non-empty saved Volume overrides before changing values. Then tune exposure, tonemapping, lift/gamma/gain, bloom, ambient fill, additional-light limits, and shadows in bounded steps with captures between attempts.
6. If clearing baked data makes the scene stop being blown out, treat the previous lightmaps/Lighting Data as stale active data for URP. Keep them as reference/rollback evidence, but do not tune final URP lighting against them.
7. If parity matters, clear/rebake lighting under URP and refresh reflection probes after URP assets, renderer features, volumes, and quality settings are finalized when the user has permitted changes. If the bake is too long or interrupted, report a partial migration and resume from this phase later rather than claiming completion.
8. When assigning new URP-compatible `LightingSettings`, remember that the active lighting-settings reference is scene state. Mark the settings and active scene dirty, save the scene, reload or re-query, and verify the saved scene references the intended asset before reporting it as active. `AssetDatabase.SaveAssets()` alone does not prove the scene's `m_LightingSettings` reference changed.
9. If the saved scene still references the old Enlighten/realtime-GI `.lighting` asset, the lighting phase is incomplete even if a new URP lighting settings asset exists on disk.
10. Report baked lighting as one of: preserved as reference, stale active bake cleared, visually checked and acceptable, rebaked/refreshed under URP, exposure-balanced with remaining bake risk, or manual follow-up. Do not report it as fully migrated from compile success alone.
11. Do not claim visual parity is preserved while old Built-in `LightingData.asset` and old reflection-probe EXRs remain active without a URP rebake/probe refresh or explicit saved-state visual verification.
12. Do not claim reflection probes were refreshed from intent alone. Verify successful probe-render output, changed probe assets, or a saved-state visual check. Unchanged old EXR files mean probe refresh is still partial/manual.

## Manual Follow-Up Hot Spots

Even when the converter succeeds, these areas often still need attention:

- custom shaders and package shaders
- PPv2 and fullscreen image effects
- replacement-shader camera workflows
- baked lighting, lightmaps, light probes, reflection probes, and shadow tuning
- quality-level assignments
- 2D lit materials and sprite workflows
- visual parity around tone mapping, exposure, and light falloff

For real project upgrades, explicitly verify:

- material shader assignments on disk
- particle/VFX material shader assignments
- Graphics settings and each relevant Quality level
- URP renderer features that were added
- persistent URP Volume profile components
- representative scene camera data and active legacy post-processing components
- baked-lighting state and whether a URP rebake/probe refresh is still required

## When To Stop And Escalate

Stop and explain the blocker instead of guessing when:

- custom shaders drive important visuals
- complex shader situations such as surface shaders, `GrabPass`, replacement shaders, or fullscreen camera callbacks are involved
- package-owned shaders are involved and the safe fix is unclear
- converter results contain unresolved warnings or failures
- baked lighting is important to the scene and no URP bake/visual parity check has been performed; for a generic full migration, this means the migration is partial, not complete
- the project mixes 2D and 3D requirements and the target renderer choice is unclear
- the user has not confirmed rollback safety for irreversible conversion
