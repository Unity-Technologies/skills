# Placeholder Assets

> Craft reference for the `craft-game-floors` skill. **Load:** whenever the build needs an asset it doesn't have, before reaching for external content. Silhouette/proportion rules live in [art-direction](art-direction.md); the audio floor reference owns placeholder sound. Sibling links resolve to the skill directory. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The doctrine: primitive but proud

A placeholder that follows the art bible is *style*; a placeholder that ignores it is *missing content*. The player can't tell a deliberate flat-shaded cube-chair from programmer art unless the color, proportion, or seam gives it away. This reference removes the giveaways. Nothing in frame is ever magenta, default-gray, or default-font.

## Meshes

- **Compound primitives first** — characters and props assembled from tapered, tilted primitives per [art-direction](art-direction.md)'s rules (its worked fox build is the template to imitate), palette-tinted per part via material or vertex color.
- **[ProBuilder](https://docs.unity3d.com/Packages/com.unity.probuilder@<VERSION>/manual/index.html) for architecture** — add `com.unity.probuilder` when rooms need trim, baseboards, window frames, door reveals, or furniture beyond stacked cubes; the linked manual carries the authoring mechanics (editor-script mesh API included). Through `unity-cli`, short edits fit `eval`; anything longer goes through `run_script` — a timed-out `eval` may still have executed, so mesh mutations through it are not safe to blindly retry.
- **Bevel cheat** — a slight scale-inset second material band or ProBuilder bevel on edges catches the toon ramp and reads as manufactured, not extruded.
- **Scaled-primitive colliders lie** — a non-uniformly scaled primitive keeps its original collider shape limits: a squashed cylinder's capsule collider clamps its height to twice its radius and becomes an invisible dome that walls characters off. Strip or replace colliders on squashed primitives (a BoxCollider, or a fresh mesh collider).

## Textures (sparingly — toon styles are mostly flat)

Generate `Texture2D` in code when a surface needs interest: stripes, checkers, dot grids, low-octave value noise — always palette-tinted, always subtle (10–15% value variation). A rug, a picture frame's art, a window's sky card. Point-filter small textures for crisp toon looks.

## UI icons and type

- **Icons**: draw simple geometric glyphs (paw, star, gear, speaker) into a `Texture2D`/Sprite from code — circles, capsules, and rotated rects compose almost any HUD icon at 64px.
- **Type**: the default font is a giveaway. Style TMP's default aggressively — bold weight, +2–5% character spacing, palette-colored with a darker palette outline (0.15–0.2) — so it reads as chosen. Never ship untinted white Arial-look text. Font-asset mechanics (weights, outline and underlay setup, fallback atlases, SDF settings) live in `optimize-text-mesh-pro`; this reference owns only the styling opinion.

## The escalation ladder

1. Primitives + palette (free, always first) → 2. ProBuilder + procedural textures (minutes) → 3. **the generator slot** — external asset generation and store content are a product decision above this reference's pay grade; when that lands, it replaces rungs, not the doctrine.

## Verify

The done gate's palette-discipline axis is the hard check, run as a machine scan before any capture (via `unity-cli` eval): walk every `Renderer` for a `sharedMaterial` named `Default-Material`, every TMP component for the stock `LiberationSans SDF` font asset left unstyled, and `RenderSettings.skybox` for the template sky — all three lists must come back empty, and any hit names its object. Then the taste check: screenshot a prop close-up — if it reads as "unfinished" rather than "stylized", the fix is proportion or color, not more geometry.

## Expansion scope

First-party asset generation (text-to-3D, textures, sprites), Asset Store pull-and-restyle flows, style-transfer onto imported models, LOD/optimization of generated content — the strategic half this reference deliberately leaves open.
