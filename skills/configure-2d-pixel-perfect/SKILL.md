---
name: configure-2d-pixel-perfect
description: Sets up, diagnoses, and fixes pixel perfect 2D rendering with PixelPerfectCamera in URP or Built-in. Use when a retro-style or pixel art 2D game looks blurry, jittery, or misaligned. Not for HD 2D or high-resolution art.
---

Set up, diagnose, and fix pixel perfect 2D rendering in Unity projects.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files. Avoid long explanations. This is because unrequested changes are hard for the user to review and can break other parts of their project.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. This is because it clutters their project with code they didn't request.

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

Read the Unity version from `ProjectSettings/ProjectVersion.txt` and package versions from `Packages/packages-lock.json`.

### Unity Manual pages

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder (`<editor-install>/Editor/Data/Documentation/en/Manual/`).

Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/`. Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`
- **docs.unity.com**: The product version, eg `7000.0`

Each page lists its local file, then its online page:

- **Add a pixel perfect camera (URP)** — `urp/2d-pixelperfect-prep-sprites.html`, `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/2d-urp/2d-pixelperfect/prep-sprites.md`: preparing sprites and adding the camera.
- **Configure a pixel perfect camera (URP)** — `urp/2d-pixelperfect-configure.html`, `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/2d-urp/2d-pixelperfect/configure.md`: reference resolution, grid snapping, and upscaling.
- **Pixel Perfect Camera reference (URP)** — `urp/2d-pixelperfect-ref.html`, `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/2d-urp/2d-pixelperfect/ref.md`: every URP Pixel Perfect Camera property.
- **Renderer 2D asset reference** — `urp/2DRendererData-overview.html`, `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/2d-urp/2drenderer-data-overview.md`: creating and assigning the Renderer 2D asset.
- **Sprite texture import settings** — `texture-type-sprite.html`, `https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/textures/textures-reference/texture-type-sprite.md`: Filter Mode, Pixels Per Unit, mipmaps, compression.

### Package manual pages

The docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/<package-name>@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time.

If that folder has no `.md` files, use the online pages at `https://docs.unity3d.com/Packages/<package-name>@<VERSION>/manual/` instead, because some packages don't ship their docs.

- **`com.unity.2d.pixel-perfect`** — `Documentation~/index.md`: the Built-in Pixel Perfect Camera package.
- **`com.unity.cinemachine`** — `Documentation~/CinemachinePixelPerfect.md`: the Cinemachine Pixel Perfect extension.
- **`com.unity.render-pipelines.high-definition`** — `Documentation~/Custom-Pass.md`: HDRP custom passes, for the HDRP fallback.

## Workflow

1. Detect the render pipeline with `DetectPipeline()` from `references/pipeline-detection.cs`, because URP and Built-in use different components.
2. Run the [Diagnostic Checklist](#diagnostic-checklist) on the in-scope scene, then check [Common Issues & Solutions](#common-issues--solutions) for the user's symptom.
3. Report what you found to the user before you change anything, because they might not want every finding fixed.
4. Create a restore point, then fix only what's broken.
5. Run the [Final step](#final-step), then report back as described in [Final report](#final-report).

---

## ⚠ There Are Two Completely Separate Implementations

Pixel perfect rendering in Unity is **not one system** — it is two separate, incompatible implementations, one per render pipeline. **Always detect the pipeline before writing or diagnosing any code.** 

| | URP | Built-in |
|---|---|---|
| **Component** | `UnityEngine.Rendering.Universal.PixelPerfectCamera` | `UnityEngine.U2D.PixelPerfectCamera` |
| **Package** | Built into URP — no extra install | `com.unity.2d.pixel-perfect` v6.0.0+ |
| **API style** | Enums (`gridSnapping`, `cropFrame`) | Booleans (`pixelSnapping`, `upscaleRT`, `cropFrameX/Y`) |

**Do not install `com.unity.2d.pixel-perfect` in a URP project**, because URP already includes its own Pixel Perfect Camera, and the standalone component behaves differently under URP in ways that are hard to diagnose (see [Migration & Compatibility](#migration--compatibility)).

**→ Always call `DetectPipeline()` first** (`references/pipeline-detection.cs`), then branch your setup, diagnostics, and fixes based on the result.

---

## When NOT to use this skill

- **HD 2D or high-resolution 2D games** — pixel snapping and point filtering will make smooth art look wrong
- **UI-only scenes** — use Canvas Scaler instead
- **HDRP projects** — Pixel Perfect Camera is not supported

---

## Critical Reminders

⚠ **Detect the render pipeline first** — URP and Built-in use different Pixel Perfect Camera components that are not interchangeable.
⚠ **Filter Mode = Point is the #1 fix** — bilinear filtering is Unity's default and is almost always the cause of blurry sprites.
⚠ **Anti-Aliasing must be disabled** — in Quality Settings and on the camera. AA actively blurs pixel edges.

## Key Principles

### 1. Pipeline Detection & Camera Component Selection

See the two-path comparison table at the top of this file. Assembly name note: the standalone Built-in package installs into a `Runtime/` folder, but the asmdef `"name"` field is `Unity.2D.PixelPerfect` — no `Runtime` suffix. HDRP is unsupported — see the HDRP fallback section under Common Issues.

**→ Code: `references/pipeline-detection.cs`** — `DetectPipeline()`, `GetPixelPerfectCameraType()`, and migration mismatch check.

### 2. Diagnostic-First Approach

Always diagnose before making changes. Report findings, then fix only what is broken, because changing settings that already work can hide the real cause of the symptom.

### 3. Work in the correct scope

Default to scene scope. Scan project-wide only when the user explicitly requests it, because project-wide fixes change assets in scenes the user didn't ask about.

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

Property and enum tables, and recommended configurations: **→ `references/api-reference.md`** — read this when writing or reviewing camera setup code.

### Package APIs

The APIs are `UnityEngine.Rendering.Universal` (`com.unity.render-pipelines.universal`, including the URP `PixelPerfectCamera` and `ScriptableRendererFeature2D`), `UnityEngine.U2D` (`com.unity.2d.pixel-perfect`, the Built-in `PixelPerfectCamera`), and `Unity.Cinemachine` (`com.unity.cinemachine`). They are versioned by package version, not Unity version (e.g. `@17.3`, `@3.1`), so check the project's installed package version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the installed package version.
2. Fetch and cache `https://docs.unity3d.com/Packages/<package-name>@<version>/xrefmap.yml` (replace `<version>` with the installed package version, e.g. `17.3`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/UnityEngine.Rendering.Universal.PixelPerfectCamera.html` => `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/api/UnityEngine.Rendering.Universal.PixelPerfectCamera.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file. If a class such as `ScriptableRendererFeature2D` isn't in it, that package version doesn't have the class.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions (e.g. `UnityEngine.Experimental.Rendering.Universal` moved to `UnityEngine.Rendering.Universal`).

### Engine APIs

The APIs are `TextureImporter`, `Rigidbody2D`, `Graphics.Blit`, and `UnityEngine.U2D.PixelPerfectRendering`, part of the Unity Engine. They are versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/rigidbody/addforce.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector3/ctor.md`), and indexers use `item` (e.g. `unityengine/vector3/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector3.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Rigidbody.AddForce.html'` => `https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html`).

Always use the docs for the project's Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

---

## Reference Resolution

Choose before building any assets. Never change after asset production starts, because every sprite, tile, and layout is sized for that pixel grid, and a new resolution means reworking existing art.

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
| 7 | Gaps only during camera movement after all above pass? | Use PP Camera pixel snapping; do not use `cellGap = -0.01f`, because it makes tiles overlap and the gaps come back when the camera moves |

### Cinemachine conflict
**Cause**: Both Cinemachine and the Pixel Perfect Camera write to orthographic size every frame.

**Fix**: Add `CinemachinePixelPerfect` extension via the Add Extension dropdown on each Virtual Camera. Do not add it via `AddComponent` in code, because a component added at runtime isn't saved with the scene and is lost when Play mode ends.

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

## Final step

Do the following checks:

- Check the project has zero console errors (using `console status`). Stop there unless the user asked for proof or you suspect a specific fault, because further checks cost time.
- If the change affects runtime behaviour and you suspect a fault, enter Play mode once and read the world through `unity command script eval` (for example the Pixel Perfect Camera's `pixelRatio` or a sprite's position). Don't add logging and don't take screenshots, because they take more time and leave code in the project.

If either check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Return these sections in order:

1. **Project**: the Unity version, render pipeline, and package versions.
2. **Findings**: each problem you found and why it causes the symptom.
3. **Changes**: each setting or file you changed, with old and new values.
4. **Verification**: the results of the final step checks.
5. **Follow-up**: anything the user must do or decide, and the doc pages you read.
