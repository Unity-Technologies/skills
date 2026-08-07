---
name: 2d-pixel-perfect
description: Sets up, diagnoses, and fixes pixel perfect 2D rendering in Unity projects. Use when working on any retro-style or pixel art 2D game.
---

Set up, diagnose, and fix pixel perfect 2D rendering in Unity projects.

---

## ⚠️ There Are Two Completely Separate Implementations

Pixel perfect rendering in Unity is **not one system** — it is two separate, incompatible implementations, one per render pipeline. **Always detect the pipeline before writing or diagnosing any code.** 

| | URP | Built-in |
|---|---|---|
| **Component** | `UnityEngine.Rendering.Universal.PixelPerfectCamera` | `UnityEngine.U2D.PixelPerfectCamera` |
| **Package** | Built into URP — no extra install | `com.unity.2d.pixel-perfect` v6.0.0+ |
| **API style** | Enums (`gridSnapping`, `cropFrame`) | Booleans (`pixelSnapping`, `upscaleRT`, `cropFrameX/Y`) |

**Do not install `com.unity.2d.pixel-perfect` in a URP project.**

**→ Always call `DetectPipeline()` first** (`references/pipeline-detection.cs`), then branch your setup, diagnostics, and fixes based on the result.

---

## When NOT to use this skill

- **HD 2D or high-resolution 2D games** — pixel snapping and point filtering will make smooth art look wrong
- **UI-only scenes** — use Canvas Scaler instead
- **HDRP projects** — Pixel Perfect Camera is not supported

---

## Critical Reminders

⚠️ **Detect the render pipeline first** — URP and Built-in use different Pixel Perfect Camera components that are not interchangeable.
⚠️ **Filter Mode = Point is the #1 fix** — bilinear filtering is Unity's default and is almost always the cause of blurry sprites.
⚠️ **Anti-Aliasing must be disabled** — in Quality Settings and on the camera. AA actively blurs pixel edges.

## Key Principles

### 1. Pipeline Detection & Camera Component Selection

See the two-path comparison table at the top of this file. Assembly name note: the standalone Built-in package installs into a `Runtime/` folder, but the asmdef `"name"` field is `Unity.2D.PixelPerfect` — no `Runtime` suffix. HDRP is unsupported — see the HDRP fallback section under Common Issues.

**→ Code: `references/pipeline-detection.cs`** — `DetectPipeline()`, `GetPixelPerfectCameraType()`, and migration mismatch check.

### 2. Diagnostic-First Approach

Always diagnose before making changes. Report findings, then fix only what is broken.

### 3. Work in the correct scope

Default to scene scope. Scan project-wide only when the user explicitly requests it.

---

## Diagnostic Checklist

**Sprite import settings:**
- [ ] Filter Mode = `Point (no filter)` on all in-scope sprites
- [ ] Mip Maps = disabled
- [ ] Compression = `None` / Uncompressed
- [ ] PPU consistent across all sprites in scene
- [ ] Sprite pivots set to Custom / Pixels mode — a center pivot on an odd-dimension sprite (e.g. 15×15) lands at 7.5px, causing 0.5px misalignment

**→ Code: `references/sprite-settings.cs`** — `GetImporter()` and `FixSpriteImportSettings()`.

**Editor snap settings:**
- [ ] Grid Size = `1 / assetsPPU` on all axes (e.g. PPU 16 → 0.0625, PPU 100 → 0.01)
- [ ] Grid Snapping enabled in the Grid and Snap overlay
- [ ] To snap existing GameObjects: select them → Align Selected → All Axes

**Camera setup:**
- [ ] Camera projection = Orthographic
- [ ] Pixel Perfect Camera component present and correct type for pipeline
- [ ] `allowHDR`, `allowMSAA`, `allowDynamicResolution` all `false`
- [ ] Scene view shows two green bounding boxes on the camera gizmo — solid = visible area, dotted = reference resolution

**→ Code: `references/camera-setup-urp.cs`** — full URP camera + PP Camera configuration.
**→ Code: `references/camera-setup-builtin.cs`** — Built-in standalone configuration.

**Project quality settings:**
- [ ] Anti-Aliasing = 0 in Quality Settings
- [ ] Anisotropic Filtering = Disabled

---

## API Reference

Full property/method tables, `GridSnapping` and `CropFrame` enum values, and recommended configurations:
**→ `references/api-reference.md`** — read this when writing or reviewing camera setup code.

Quick enum summary:

**`GridSnapping`**: `None` · `PixelSnapping` (standard) · `UpscaleRenderTexture` (authentic low-res; incompatible with post-processing and UI text)

**`CropFrame`**: `None` · `Pillarbox` · `Letterbox` · `Windowbox` (safest default) · `StretchFill`

---

## Reference Resolution

Choose before building any assets. Never change after asset production starts.

| Reference resolution | 1080p | 1440p | 4K |
|---|---|---|---|
| 320 × 180 | 6× | 8× | 12× |
| 480 × 270 | 4× | ~5.3× | 8× |
| 640 × 360 | 3× | 4× | 6× |

320×180 is the safest general choice. For screens with no integer fit (e.g. 1366×768), use `cropFrame = Windowbox` to add black bars rather than stretching to a fractional scale.

---

## Migration & Compatibility

### URP project with the Built-in standalone component

**Symptom**: `DetectPipeline()` returns URP but camera has `UnityEngine.U2D.PixelPerfectCamera`. Symptoms are subtle because the standalone component has `ENABLE_URP` conditional code.

**Fix**:
1. Remove `com.unity.2d.pixel-perfect` from Package Manager
2. Remove `UnityEngine.U2D.PixelPerfectCamera` from each camera
3. Add `UnityEngine.Rendering.Universal.PixelPerfectCamera`
4. Reconfigure — booleans (`pixelSnapping`, `upscaleRT`, `cropFrameX/Y`) become enums (`gridSnapping`, `cropFrame`)

**→ Detection code: `references/pipeline-detection.cs`** (bottom of file).

### Old URP namespace (pre-Unity 2022 / URP pre-13.x)

**Symptom**: Compiler errors referencing `UnityEngine.Experimental.Rendering.Universal`.

**Fix**: Replace `using UnityEngine.Experimental.Rendering.Universal;` with `using UnityEngine.Rendering.Universal;`. Update any assembly-qualified type strings. The `[MovedFrom]` attribute handles serialization automatically — components on GameObjects survive the upgrade.

---

## Common Issues & Solutions

### Blurry sprites
**Fix**: Set Filter Mode to Point on all in-scope sprites, disable Mip Maps, disable AA in Quality Settings.
**→ Code: `references/sprite-settings.cs`**

### Tilemap gaps between tiles
Work through in order — workarounds like negative cell gap or PPU = 31.99 break when the camera moves.

| # | Check | Fix |
|---|---|---|
| 1 | Sprite Atlas with Tight Packing off, Padding ≥ 4, Sprite Packer Mode enabled? | Enable Sprite Packer Mode in Editor settings; on the atlas set Padding ≥ 4 and turn Tight Packing off |
| 2 | Mipmaps disabled on tileset textures and atlas? | Disable Generate Mip Maps |
| 3 | AA = 0, MSAA off on camera? | Disable AA globally |
| 4 | Compression = None? | RGBA 32-bit uncompressed |
| 5 | All tile sprites have even pixel dimensions? | Odd dimensions cause 0.5px grid offset |
| 6 | PPU = tile pixel width? (16×16 → PPU 16) | PPU mismatch leaves physical gaps |
| 7 | Gaps only during camera movement after all above pass? | Use PP Camera pixel snapping; do not use `cellGap = -0.01f` |

### Cinemachine conflict
**Cause**: Both Cinemachine and the Pixel Perfect Camera write to orthographic size every frame.

**Fix**: Add `CinemachinePixelPerfect` extension via the Add Extension dropdown on each Virtual Camera. Do not add it via `AddComponent` in code.

**Known limitations**:
- Camera blends between virtual cameras are not pixel-perfect during transitions
- `UpscaleRenderTexture` reduces valid pixel-perfect ortho sizes, which may cause framing to deviate
- Target Group + Framing Transposer causes visible choppiness (no fix available)

### Post-processing blur with `upscaleRT`
**Cause**: Post-processing runs after the PP Camera upscales the render texture.

**Simple fix**: Disable `upscaleRT`. Post-processing then runs at native screen resolution.

**Advanced fix (Unity 6 URP)**: Inject a `ScriptableRendererFeature2D` at `RenderPassEvent2D.AfterRenderingPostProcessing`. Use 2D-specific base classes — `ScriptableRendererFeature` (3D base class) is silently ignored in a URP 2D renderer.

### UI text blurry with `upscaleRT`
**Status**: Known Unity bug, declined to fix (still present Unity 6, 2025).

**Root causes**: (A) Canvas renders into the low-res buffer and is upscaled with the scene. (B) TMP's SDF gradient threshold is miscalibrated at low reference resolutions.

**Fixes in order of reliability**:
1. `Screen Space - Overlay` on Canvas — bypasses the camera, renders at native resolution
2. Dedicated UI camera with no PP Camera component, `Screen Space - Camera` mode
3. Unity 6 only: `Font Material → Debug Settings → Sharpness = 1` (mitigates B, not A)

### Physics / render desync (micro-stutter)
**Cause**: Physics runs at a fixed timestep; interpolated positions produce fractional values that snap to different pixels each frame.

**Fix**:
- Enable `Rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate` on physics-driven sprites
- Set `Time.fixedDeltaTime = 1f / 60f` to match target frame rate
- Keep camera tracking in `LateUpdate`, not `FixedUpdate`

### Non-integer scaling / pixel decimation
**Cause**: Screen resolution is not a clean integer multiple of the reference resolution.

**Fix**: Choose a reference resolution from the table above. Use `cropFrame = Windowbox` when no integer fit exists.

### Missing URP 2D Renderer
**Fix**:
1. `Assets > Create > Rendering > URP 2D Renderer Data`
2. Assign it to your URP Asset under Renderer List
3. Requires `com.unity.render-pipelines.universal` 12.0+

### HDRP fallback
Pixel Perfect Camera is unsupported in HDRP.

**Built-in / Unity 5.x**: Use `RenderTexture` + `Graphics.Blit` with `FilterMode.Point`.

**Unity 6 URP**: Use `ScriptableRendererFeature2D` + `ScriptableRenderPass2D` injected at `RenderPassEvent2D.AfterRendering` with `AddRasterRenderPass`. Note: `OnRenderImage` and `Graphics.Blit` are incompatible with Unity 6's render graph.
