# Scene Lighting

> Craft reference for the `craft-game-floors` skill. **Load:** immediately after the first scene exists in any 3D build — or on first touching a 3D scene still lit by the template's defaults. Post-effect mechanics live in `urp-postprocessing` — this reference says *which* values; that skill says *how*. Sibling links resolve to the skill directory. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The opinion

Light tells the scene's story. The un-opinionated default — one white directional at noon over flat gray ambient — is why agent-built games look like documents. Every 3D scene built under these floors (the paved default — and any scene this skill touches) gets the baseline below before gameplay authoring continues. The values in this reference supersede the project baseline's and `urp-postprocessing`'s own starting points; that skill remains the mechanics and debugging reference for the Volume framework itself.

## The key light

- One directional, **warm-tinted from the palette** ([art-direction](art-direction.md); toward `#FFF2DC`, never pure white), intensity ~1.1–1.3.
- Elevation 35–50°, azimuth angled across the play axis so faces get light/shadow variation (never straight down, never behind camera).
- Soft shadows on; shadow distance sized to the playable space, not the pipeline default (URP ships 50 m; Built-in 150 m).

## The ambient answer (the half everyone skips)

Environment lighting → **Gradient**: sky = cool tint from the palette, equator = neutral mid, ground = dark warm. This is what keeps shadow sides alive instead of dead gray. For toon styles the ambient is doing the "bounce" the ramp can't.

## The global Volume (via `urp-postprocessing`)

One global [Volume](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page.md) asset, always — and **first, check the camera**: a script-created camera's `UniversalAdditionalCameraData` defaults `renderPostProcessing` to false (template cameras ship with it on), and the whole Volume silently does nothing until it is enabled.

**[Bloom](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/effect-list/bloom.md) needs HDR here** — with SDR's 0–1 colors the threshold values below never fire (the skill's own trap note; the Bloom page doesn't state it). Confirm HDR on the active URP asset (and not overridden off on the camera); a build that must stay SDR runs Threshold 0.9 with Intensity halved.

Baseline starting values:

| Override | Start | Why |
|---|---|---|
| Tonemapping | ACES (Neutral for flat toon looks) | free depth and rolloff |
| Bloom | intensity 0.25–0.4, threshold 1.1 (HDR on) | glow on accents/pickups only |
| Vignette | 0.2–0.25 | frames the action |
| Color Adjustments | saturation +8, contrast +5 | counters engine-default flatness |

Interiors: consider palette-tinted fog for depth. Skip AO on toon styles (fights the bands); consider it on stylized-lit ones.

## Per-mood variants (worked)

Starting points keyed to the worked bibles in [art-direction](art-direction.md) — tune from the matching row, never from zero.

| Mood | Key light | Tonemapping | Bloom | Vignette | Color Adjustments | Extras |
|---|---|---|---|---|---|---|
| Cozy interior | warm `#FFE8C8`, 1.15, elev 38° | ACES | 0.3 @ 1.1 | 0.25 | sat +8, contrast +5 | White Balance temp +10; faint palette-tinted fog |
| Bright arcade | cool dim `#CFE6FF`, 0.6, elev 60° | Neutral | 0.8 @ 0.9 | 0.18 | sat +15, contrast +10 | emissive materials carry the light; ambient from the bounds color |
| Moody sci-fi | cool hard `#BFD2E6`, 1.3, elev 55° | ACES | 0.35 @ 1.2 | 0.3 | sat −5, contrast +12 | White Balance temp −8; fog tinted from the shadow value |

## Verify

**This verify is URP-typed** — establish the pipeline first (`GraphicsSettings.currentRenderPipeline`; the router's mid-flow check does this). On Built-in or HDRP, apply this file's light-story opinions (key, ambient, mood) but skip the URP checks and name the pipeline gap in the report — `UniversalAdditionalCameraData` and the URP Volume types don't exist there, and `urp-postprocessing` stops by design when URP is inactive. Under URP, machine checks first (via `unity-cli` eval): the camera's `UniversalAdditionalCameraData.renderPostProcessing` reads true, and the global Volume's profile reports all four overrides present and active. Then capture with the Volume enabled and disabled and diff the two PNGs — a mean per-channel delta that rounds to zero means the settings are too timid; raise until the diff is unmistakable, then pull back one notch. Keep each tuning eval short: a timed-out `eval` may still have executed, so long sweeps go through `run_script` instead.

## Expansion scope

Baked GI / light-probe workflows, time-of-day rigs, per-scene Volume blending, mobile/web budget variants, HDRP parity.
