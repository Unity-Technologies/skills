## Effect Reference

**Bloom** (`Bloom`) — Glow on bright areas. Best with HDR rendering enabled on the URP Asset; works in SDR if `threshold` < 1.
- `threshold` (MinFloatParameter, default 0.9, min 0) — brightness cutoff below which pixels don't bloom.
- `intensity` (MinFloatParameter, default 0, min 0) — strength of the bloom effect.
- `scatter` (ClampedFloatParameter, default 0.7, 0–1) — spread/diffusion of the glow.
- `clamp` (MinFloatParameter, default 65472, min 0) — clamps pixel intensity to prevent artifacts.
- `tint` (ColorParameter, default white) — color tint applied to bloom.
- `highQualityFiltering` (BoolParameter, default false) — uses bicubic upsampling for smoother bloom.
- `downscale` (BloomDownscaleMode: `Half`, `Quarter`) — resolution for bloom computation.
- `maxIterations` (ClampedIntParameter, default 6, 2–8) — number of blur passes.

**Tonemapping** (`Tonemapping`) — Maps HDR to display range. Requires HDR.
- `mode` (TonemappingMode: `None`, `Neutral`, `ACES`) — `ACES` for cinematic, `Neutral` for minimal color shift.

**Color Adjustments** (`ColorAdjustments`) — **WARNING: Class is `ColorAdjustments`, NOT `ColorGrading`.**
- `postExposure` (FloatParameter, default 0) — exposure adjustment in EV units. Unbounded.
- `contrast` (ClampedFloatParameter, default 0, -100–100).
- `colorFilter` (ColorParameter, default white) — multiplied against the final color.
- `hueShift` (ClampedFloatParameter, default 0, -180–180) — shifts all hues.
- `saturation` (ClampedFloatParameter, default 0, -100–100).

**Depth of Field** (`DepthOfField`)
- `mode` (DepthOfFieldMode: `Off`, `Gaussian`, `Bokeh`).
- Gaussian mode: `gaussianStart` (MinFloatParameter, default 10, min 0), `gaussianEnd` (MinFloatParameter, default 30, min 0), `gaussianMaxRadius` (ClampedFloatParameter, default 1, 0.5–1.5).
- Bokeh mode: `focusDistance` (MinFloatParameter, default 10, min 0.1), `focalLength` (ClampedFloatParameter, default 50, 1–300), `aperture` (ClampedFloatParameter, default 5.6, 1–32), `bladeCount` (ClampedIntParameter, default 5, 3–9), `bladeCurvature` (ClampedFloatParameter, default 1, 0–1), `bladeRotation` (ClampedFloatParameter, default 0, -180–180).

**Vignette** (`Vignette`)
- `intensity` (ClampedFloatParameter, default 0, 0–1).
- `smoothness` (ClampedFloatParameter, default 0.2, 0.01–1).
- `color` (ColorParameter, default black).
- `center` (Vector2Parameter, default (0.5, 0.5)).
- `rounded` (BoolParameter, default false) — constrains vignette to circular shape.

**Motion Blur** (`MotionBlur`)
- `mode` (MotionBlurMode: `CameraOnly`, `CameraAndObjects`). `CameraAndObjects` requires per-object motion vectors.
- `quality` (MotionBlurQuality: `Low`, `Medium`, `High`).
- `intensity` (ClampedFloatParameter, default 0, 0–1).
- `clamp` (ClampedFloatParameter, default 0.05, 0–0.2) — maximum velocity in screen fraction.

**Film Grain** (`FilmGrain`)
- `type` (FilmGrainLookup: `Thin1`, `Thin2`, `Medium1`–`Medium6`, `Large01`, `Large02`, `Custom`).
- `intensity` (ClampedFloatParameter, default 0, 0–1).
- `response` (ClampedFloatParameter, default 0.8, 0–1).
- `texture` (NoInterpTextureParameter, default null) — required when `type` is `Custom`.

**Chromatic Aberration** (`ChromaticAberration`)
- `intensity` (ClampedFloatParameter, default 0, 0–1). Keep subtle (0.05–0.15) for realism.

**Split Toning** (`SplitToning`)
- `shadows` (ColorParameter, default grey).
- `highlights` (ColorParameter, default grey).
- `balance` (ClampedFloatParameter, default 0, -100–100).

**Lens Distortion** (`LensDistortion`)
- `intensity` (ClampedFloatParameter, default 0, -1–1). Negative = barrel, positive = pincushion.
- `xMultiplier` (ClampedFloatParameter, default 1, 0–1).
- `yMultiplier` (ClampedFloatParameter, default 1, 0–1).
- `center` (Vector2Parameter, default (0.5, 0.5)).
- `scale` (ClampedFloatParameter, default 1, 0.01–5).

**White Balance** (`WhiteBalance`)
- `temperature` (ClampedFloatParameter, default 0, -100–100). Negative = cool, positive = warm.
- `tint` (ClampedFloatParameter, default 0, -100–100). Negative = green, positive = magenta.

**Panini Projection** (`PaniniProjection`) — Only useful with camera FOV > 90.
- `distance` (ClampedFloatParameter, default 0, 0–1).
- `cropToFit` (ClampedFloatParameter, default 1, 0–1).

**Lift Gamma Gain** (`LiftGammaGain`) — Each is `Vector4Parameter` (RGBA where A is intensity offset, defaults to (1,1,1,0)).
- `lift` — affects shadows.
- `gamma` — affects midtones.
- `gain` — affects highlights.

**Shadows Midtones Highlights** (`ShadowsMidtonesHighlights`) — Each `Vector4Parameter`, defaults to (1,1,1,0).
- `shadows`, `midtones`, `highlights`.
- Zone boundaries: `shadowsStart` (default 0), `shadowsEnd` (default 0.3), `highlightsStart` (default 0.55), `highlightsEnd` (default 1). All `MinFloatParameter`, min 0.

**Color Curves** (`ColorCurves`) — All `TextureCurveParameter`.
- `master`, `red`, `green`, `blue` — default linear (0,0)→(1,1).
- `hueVsHue`, `hueVsSat`, `satVsSat`, `lumVsSat` — default empty, neutral at 0.5.

**Channel Mixer** (`ChannelMixer`) — All `ClampedFloatParameter`, -200–200.
- `redOutRedIn` (default 100), `redOutGreenIn` (default 0), `redOutBlueIn` (default 0).
- `greenOutRedIn` (default 0), `greenOutGreenIn` (default 100), `greenOutBlueIn` (default 0).
- `blueOutRedIn` (default 0), `blueOutGreenIn` (default 0), `blueOutBlueIn` (default 100).
