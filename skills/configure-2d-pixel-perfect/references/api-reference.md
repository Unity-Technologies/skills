# PixelPerfect Skill — Full API Reference

## URP `PixelPerfectCamera` (`UnityEngine.Rendering.Universal`)
API is identical since Unity 2023.2 through latest Unity 6 versions.

### Properties

| Property | Type | Access | Description |
|---|---|---|---|
| `assetsPPU` | `int` | get/set | Must match Pixels Per Unit on all sprites in scene |
| `refResolutionX` | `int` | get/set | Base resolution width |
| `refResolutionY` | `int` | get/set | Base resolution height |
| `cropFrame` | `CropFrame` | get/set | How to handle aspect ratio differences |
| `gridSnapping` | `GridSnapping` | get/set | Snapping mode — replaces old `pixelSnapping`/`upscaleRT` bools |
| `orthographicSize` | `float` | get only | PP Camera's calculated ortho size (different from `Camera.orthographicSize`) |
| `pixelRatio` | `int` | get only | Actual scale factor applied (e.g. 6 on 1080p with 320×180 ref res) |
| `requiresUpscalePass` | `bool` | get only | True when `gridSnapping = UpscaleRenderTexture` |

### Methods

| Method | Returns | Description |
|---|---|---|
| `RoundToPixel(Vector3)` | `Vector3` | Snaps world-space position to pixel grid. Adapts to current `gridSnapping`. |
| `CorrectCinemachineOrthoSize(float)` | `float` | Returns nearest pixel-perfect ortho size. Used by the Cinemachine extension. |

### Static

**`UnityEngine.U2D.PixelPerfectRendering.pixelSnapSpacing`** (static float) — world-space size of one screen pixel. Formula: `Camera.orthographicSize * 2f / Camera.pixelHeight`. Set manually in custom setups that bypass the PP Camera component (e.g. HDRP fallback, custom SRP).

---

## `GridSnapping` Enum

| Value | Old standalone equivalent | Behaviour |
|---|---|---|
| `None` | `pixelSnapping = false`, `upscaleRT = false` | No snapping. |
| `PixelSnapping` | `pixelSnapping = true` | Snaps SpriteRenderers at render-time. Transforms stay at float precision. |
| `UpscaleRenderTexture` | `upscaleRT = true` | Renders at ref res then upscales. Incompatible with post-processing and UI text. |

---

## `CropFrame` Enum

| Value | Old standalone equivalent | Behaviour |
|---|---|---|
| `None` | both false | No cropping. Scene stretches to fill screen. |
| `Pillarbox` | `cropFrameX = true, cropFrameY = false` | Black bars left and right. |
| `Letterbox` | `cropFrameX = false, cropFrameY = true` | Black bars top and bottom. |
| `Windowbox` | both true, `stretchFill = false` | Black bars all sides. Safest default. |
| `StretchFill` | both true, `stretchFill = true` | Fills screen maintaining aspect ratio. Enables component-level Filter Mode. |

**Filter Mode** (only active when `StretchFill` — separate from per-sprite texture Filter Mode):
- `Retro AA` — default. Integer upscale to nearest multiple, then bilinear to screen. Recommended.
- `Point` — nearest-neighbour all the way to screen. Can lose pixel-perfectness at non-integer scale factors.

---

## Recommended Configurations

| Use case | `gridSnapping` | `cropFrame` | Notes |
|---|---|---|---|
| Standard pixel art | `PixelSnapping` | `Windowbox` | Safe default. Compatible with post-processing and UI. |
| Authentic low-res look | `UpscaleRenderTexture` | `Windowbox` | No post-processing. Separate UI camera required. |
| Performance on 4K/HiDPI | `UpscaleRenderTexture` | `Windowbox` | Renders fewer pixels. Same UI caveat. |
| Full-screen stretch | `PixelSnapping` | `StretchFill` | Use `Retro AA` filter, not Point. |

---

## Standalone Built-in (`UnityEngine.U2D.PixelPerfectCamera`)
All booleans, no enums. No `RoundToPixel`, `CorrectCinemachineOrthoSize`, `requiresUpscalePass`, or `orthographicSize`.

| Property | Type | Constraint |
|---|---|---|
| `assetsPPU` | `int` | — |
| `refResolutionX` / `refResolutionY` | `int` | — |
| `pixelSnapping` | `bool` | Silently ignored when `upscaleRT = true` |
| `upscaleRT` | `bool` | When true, `pixelSnapping` has no effect |
| `cropFrameX` | `bool` | Black bars left/right |
| `cropFrameY` | `bool` | Black bars top/bottom |
| `stretchFill` | `bool` | Only functional when **both** `cropFrameX` and `cropFrameY` are `true` |
| `pixelRatio` | `int` readonly | Same semantics as URP `pixelRatio` |
