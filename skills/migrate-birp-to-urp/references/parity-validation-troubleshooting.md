# Parity, Validation, and Troubleshooting

Quality and lighting parity review, validation after each pass, and the troubleshooting decision tree.

Links to other reference files point to files in this `references/` folder. Any other relative paths resolve to the skill directory.

## Contents

- 9. Quality, Lighting, and Visual Parity Review
- 10. Validation
- 11. Troubleshooting Decision Tree

## 9. Quality, Lighting, and Visual Parity Review

Use [quality-settings-map.md](quality-settings-map.md) when the user mentions visual mismatch, shadows, performance, or quality settings.

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

1. **Capture the scene:** Capture the Scene View or a specific camera on a representative scene — see [capturing-the-editor.md](capturing-the-editor.md).
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
5. If it is a custom or package shader, move into [custom-shader-triage.md](custom-shader-triage.md) and, when needed, [complex-shader-situations.md](complex-shader-situations.md).

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
2. If so, use [complex-shader-situations.md](complex-shader-situations.md) and choose a Scene Color / Renderer Feature / custom-pass approach.

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
