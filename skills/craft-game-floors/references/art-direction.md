# Art Direction

> Craft reference for the `craft-game-floors` skill. **Load:** before the first material, light, or UI element exists — in the creation flow, that is the start of the build (the paved default path); in an ad-hoc session, load it first whenever no art bible or `Palette` source exists yet, because every other floor depends on this one.

## The art bible — three lines, written first

Before the first material, write and print:

1. **Style statement** — one sentence ("Cel-shaded storybook interior: soft toon bands, chunky outlines, warm afternoon light").
2. **Palette** — 5–6 hex values + one accent, each named for its job (floor, walls, character-primary, character-secondary, props, shadow, accent/XP). Derive from the prompt's mood; keep saturation in one family.
3. **Mood** — the light source story ("late sun through windows, cool shadow bounce").

## Three worked bibles — imitate, then vary

Copy the closest one and re-derive the hexes from the prompt's own nouns; never ship these verbatim twice.

**Hearthside** (cozy interior)

- Style: "Cel-shaded storybook café: soft toon bands, chunky outlines, everything slightly rounded."
- Palette: floor `#8A6F4D` honeyed oak · walls `#E8D9BE` cream plaster · character-primary `#C96F4A` terracotta · character-secondary `#F4E7CE` oat · props `#5E7B5A` sage · shadow `#6B7A8F` cool slate · accent `#F2B134` amber.
- Mood: late sun through west windows; cool blue-gray shadow bounce; lamps warm the corners.

**Neon Ricochet** (bright arcade)

- Style: "Flat-shaded neon arcade: saturated shapes on near-black courts; everything that scores, glows."
- Palette: floor `#141A2E` ink navy · bounds `#253057` midnight · character-primary `#19D3F0` electric cyan · character-secondary `#F037A5` hot magenta · props `#7A5CF0` violet · shadow `#0B0F1E` abyss · accent `#F7E04B` laser yellow.
- Mood: no sun — the pieces are the light source; dim cool key, emissives and glow carry the frame.

**Derelict Signal** (moody sci-fi)

- Style: "Grounded sci-fi interior: desaturated steel broken by one signal color, hard light through grates."
- Palette: floor `#3A3F46` deck gunmetal · walls `#2A2E35` hull plate · character-primary `#C8CCD4` worn ceramic · character-secondary `#5A6472` slate · props `#4E5A50` olive drab · shadow `#1C2026` void blue · accent `#FF6A2B` signal orange.
- Mood: one hard cool key through ceiling grates; the accent reads as emergency lighting; shadows stay deep but blue, never black.

## Palette as the single source

Author one `Palette.cs` (static Color fields from the hex values) or a ScriptableObject. **Every** color in the project reads from it: materials, fog, light tints, particles, UI, text. The test is absolute: `Color.gray`, default Lit material, and default skybox never appear in a capture.

## Style is a system, not a shader on the hero

Whatever the style is, it covers *everything in frame*: character, walls, props, floor, sky, UI. The proven failure mode is a cel-shaded character in a default-lit room — the game reads as broken exactly at the seam. When the bible picks a toon pass, the pass applies frame-wide.

## Primitive characters that still read

Agent-built characters are assembled from primitives. Rules that make them charming instead of sad:

- **Silhouette first** — the character must be identifiable filled flat black. Exaggerate the identifying features (ears, tail, snout) ~30% past realistic.
- **Taper and tilt** — scale limb primitives asymmetrically (0.8× at the far end) and tilt joints a few degrees; nothing organic is axis-aligned.
- **Two-tone minimum** — body primary + belly/face secondary from the palette, plus dark accents (paw tips, ear interiors).
- **Nothing is statuary** — an idle character breathes: a 2–3 line sine bob on the body and a slow tail sway. Cheap, transformative.

## Worked character: Scout, the fox courier

A complete build to imitate. Parent chest, head, legs, and tail to the body; snout, nose, and ears to the head. Scales are local (x, y, z) against a body ~1 unit long; rotations in degrees; palette roles come from the active bible.

| # | Part | Primitive | Scale | Placement and tilt | Palette role |
|---|---|---|---|---|---|
| 1 | Body (root) | Sphere | 0.50, 0.42, 0.62 | egg lying along +Z | character-primary |
| 2 | Chest | Sphere | 0.34, 0.30, 0.30 | embedded front-under of body | character-secondary |
| 3 | Head | Sphere | 0.32, 0.30, 0.32 | above body front; pitch down 6 | character-primary |
| 4 | Snout | Sphere | 0.14, 0.10, 0.20 | head front; pitch up 4 | character-secondary |
| 5 | Nose | Cube | 0.05 uniform | snout tip; yaw 45 (diamond) | shadow |
| 6 | Ears ×2 | Cube | 0.10, 0.18, 0.04 | head top; roll ±12 outward | primary; inner face shadow |
| 7 | Legs ×4 | Capsule | 0.09, 0.16, 0.09 | under body corners; tilt 3–5 off vertical | character-primary |
| 8 | Paw tips ×4 | Sphere | 0.072 uniform | leg ends (the 0.8× taper) | shadow |
| 9 | Tail | Capsule | 0.12, 0.30, 0.12 | body rear; pitch back 55 | character-primary |
| 10 | Tail tip | Sphere | 0.10 uniform | tail end | character-secondary |

Idle: sine-bob the body ±2% of its height at ~0.6 Hz; sway the tail ±8° at ~0.3 Hz. The 0.18-tall ears are the ~30% silhouette exaggeration — keep it.

## Environment floor

No bare planes. Every room gets: floor material distinct from walls, baseboard/trim geometry at wall joins, 2–3 props scaled against the character, and one light-story element (window, lamp).

## Verify

Machine check first, capture second:

- Eval (via `unity-cli`): walk every `Renderer` and assert no `sharedMaterial` named `Default-Material`, and that `RenderSettings.skybox` is a palette-derived material, not the template sky. Any hit names its object. **Scope the fixes by the ask**: hits on the surface the request authorized get fixed before capturing (a full-look pass authorizes everything; a title-screen ask authorizes the title backdrop); hits outside that scope are never edited uninvited — report them as follow-up recommendations instead.
- Grep the project scripts for literal `Color.` constants outside the `Palette` source; a hit means the single source is being bypassed.
- Capture the character beside a doorway: scale and palette read together. A gray or default-lit surface in the frame fails the bible, not the capture.

## Expansion scope

Reference palettes per genre/mood beyond these three, texture and detail passes, integration with generated 2D/3D assets, style-transfer onto imported models, per-platform color calibration.
