# Quality Settings Map

Use this map to explain where common Built-in quality settings end up after URP migration.

## Official References

- [Convert Built-in quality settings to URP](https://docs.unity3d.com/Manual/urp/birp-onboarding/quality-presets.html)
- [Find Built-in quality settings in URP](https://docs.unity3d.com/Manual/urp/birp-onboarding/quality-settings-location.html)
- [Universal Render Pipeline asset](https://docs.unity3d.com/Manual/urp/urp-asset-and-renderer.html)
- [Change how lights fade to match the Built-In Render Pipeline](https://docs.unity3d.com/Manual/urp/birp-onboarding/birp-light-falloff-in-urp.html)

## Common Mappings

| Built-in concept | URP location |
| --- | --- |
| Render Pipeline Asset | `Project Settings > Quality > Rendering > Render Pipeline Asset` |
| Default pipeline assignment | `Project Settings > Graphics > Render Pipeline Asset` |
| MSAA | `URP Asset > Quality > Anti-aliasing (MSAA)` |
| Camera anti-aliasing | `Camera > Rendering > Anti-aliasing` |
| Main light shadows | `URP Asset > Lighting > Main Light > Cast Shadows` |
| Additional light shadows | `URP Asset > Lighting > Additional Lights > Cast Shadows` |
| Main light shadow resolution | `URP Asset > Lighting > Main Light > Shadow Resolution` |
| Additional light shadow atlas and tiers | `URP Asset > Lighting > Additional Lights > Shadow Atlas Resolution` and `Shadow Resolution Tiers` |
| Shadow distance | `URP Asset > Shadows > Max Distance` |
| Shadow cascades | `URP Asset > Shadows > Cascade Count` and split controls |
| HDR | `URP Asset > Quality > HDR` |
| Depth texture | `URP Asset > Rendering > Depth Texture` |
| Opaque texture | `URP Asset > Rendering > Opaque Texture` |
| Real-time reflection probes | `Project Settings > Quality > Rendering > Real-time Reflection Probes` |
| Resolution scaling | `Project Settings > Quality > Rendering > Resolution Scaling Fixed DPI Factor` and `URP Asset > Quality > Render Scale` |
| LOD cross fade | `URP Asset > Quality > LOD Cross Fade` |
| Baked GI / lightmaps | Scene Lighting Settings, `LightmapSettings`, Lighting Data asset; verify visually and rebake under URP when parity matters |
| Reflection probe bake data | Scene reflection probes; refresh/rebake after URP lighting and post-processing settings are finalized |

## Guidance

- Many settings that used to live only in Built-in quality settings are split between Project Settings and the URP asset.
- Treat the official URP values as a starting point, not a promise of identical performance or visuals.
- After migration, verify the user's important quality levels explicitly instead of assuming Low and High presets still behave the same way.
- A Quality level with no custom render pipeline asset may fall back to Graphics settings. That can be acceptable, but state this explicitly instead of claiming that Quality was fully assigned.
- For full-project migrations, prefer assigning the intended URP asset to all relevant runtime Quality levels unless the project deliberately relies on Graphics settings fallback.
- When validating on disk, look for each Quality level's saved `customRenderPipeline` entry. `{fileID: 0}` means that level has no explicit pipeline asset, even if Graphics Settings has a URP asset.
- If using scripts or serialized project settings to assign Quality assets, verify by re-querying after saving. Do not trust a guessed serialized field name or a command log that says assignment happened.
- In Unity versions without a direct per-index setter, use the converter-style assignment pattern: store `QualitySettings.GetQualityLevel()`, call `QualitySettings.SetQualityLevel(index)`, set `QualitySettings.renderPipeline = urpAsset`, repeat for each target level, restore the original level, then verify saved `customRenderPipeline` references. Do not invent APIs such as `QualitySettings.SetRenderPipelineAssetAt`.
- Light falloff and overall scene brightness can differ between Built-in and URP. Treat that as a tuning step, not automatically a failed migration.
- Baked GI, lightmaps, light probes, and reflection probes are visual-parity risks. Preserve existing baked data as reference, but recommend a URP rebake/refresh when the scene depends on baked lighting.
- If a migrated scene is extremely bright or washed out, check for double post-processing first: retained PPv2 components/layers plus an active URP Volume can stack exposure, bloom, tonemapping, or color grading during validation.
- If a migrated scene is too dark, too flat, or still high contrast after material and PPv2 migration, treat exposure balance as part of visual validation. Verify URP post-processing wiring first, then tune Volume exposure/tonemapping/lift-gamma-gain, ambient fill, additional-light limits, and shadow settings with representative captures.
- If clearing baked data makes the scene stop being washed out, classify the old Built-in lightmaps or Lighting Data as stale active data. Preserve them as reference, but clear active baked data and rebake under URP before final tuning.
- If a new Lighting Settings asset is created for URP baked GI, verify the saved scene references it after saving/reloading. A `.lighting` asset with baked GI enabled is not active if the scene still points to the old Enlighten/realtime-GI settings asset.
- Do not claim baked lighting parity from compile success, material conversion, or URP asset assignment alone.

## Validation Checklist

After migration or troubleshooting:

1. Confirm the correct URP asset is assigned in Graphics settings.
2. Confirm each relevant Quality level points to the expected URP asset.
   - If it does not, report whether this is intentional fallback behavior or an incomplete migration item.
3. Compare shadow distance, cascades, and shadow resolution against the user's expected look.
4. If fullscreen effects depend on them, verify whether depth texture and opaque texture are enabled.
5. If the user reports blurrier or sharper visuals, review MSAA, render scale, and camera anti-aliasing.
6. If lighting looks "off," review light falloff, post-processing, active PPv2/URP volumes, and ambient/environment settings before rewriting content.
7. For baked-lighting scenes, inspect Lighting Settings, `LightmapSettings`, baked/mixed lights, light probes, reflection probes, and Lighting Data assets.
8. If old lightmaps are causing severe overexposure, clear active baked data before final URP validation.
9. If visual parity matters, rebake lighting under URP and refresh reflection probes after URP renderer, Quality, and Volume settings are finalized.
10. After assigning new Lighting Settings, save the scene and verify the scene reference, not only the settings asset contents.
11. If the final capture is too dark or too bright, verify renderer `PostProcessData`, camera `renderPostProcessing`, camera volume layer mask, active Volume weight/priority/layer, and saved VolumeProfile overrides before changing lighting.
12. When exposure balance is required, prefer bounded changes to `ColorAdjustments.postExposure`, contrast, saturation, `Tonemapping`, `LiftGammaGain`, Bloom, ambient sky color/intensity, additional-light count, and shadow settings. If extreme values are only compensating for missing GI or an unfinished bake, report lighting parity as partial.
