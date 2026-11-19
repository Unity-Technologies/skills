# Migration Gates

The phase contract for generic migration requests and the success gate to pass before calling a migration complete.

Links to other reference files point to files in this `references/` folder. Any other relative paths resolve to the skill directory.

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

- Use the detailed [Execution Regression Checklist](implementation-patterns.md#execution-regression-checklist) for actual migration or repair work.
- Verify saved scene references before status claims: URP Volume/profile, Quality assignments, Lighting Settings/Data, URP asset/renderer, and reflection-probe settings.
- Preserve material source data from a pre-conversion snapshot, then restore `_BaseMap`, `_BaseColor`, texture scale/offsets, and relevant maps after shader changes.
- Validate representative visuals for PPv2, foliage, particles/effects, baked lighting, probes, and exposure before final wording.
- Report exact phase status. `Phase complete` is allowed for a passed phase; project-level `complete` is allowed only when every success gate passes.
