# Toon Shading

> Craft reference for the `craft-game-floors` skill. **Load:** when the art bible picks a cel/toon/anime style — and if no art bible exists yet, author one first via the look floor ([art-direction](art-direction.md)). Sibling links resolve to the skill directory. Confirm engine behavior against the linked documentation for the installed version before acting on it.

## The opinion

One toon shader family for the **whole frame**. Characters, walls, props, floor — all of it. A cel-shaded hero in a PBR room is the canonical broken look.

## The ramp (URP, Unity 6)

Two routes; pick by what's in the project:

- **Shader Graph route** — the lighting lives in a [Custom Function node](https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/manual/Custom-Function-Node.html), because Shader Graph exposes no main-light data as built-in nodes. Inside the function, call `GetMainLight()` and quantize `NdotL` into 2–3 hard bands via `smoothstep` with a narrow edge (0.02–0.05) so bands stay crisp but antialiased. Two requirements, mechanics on the linked pages:
  - Give the function a preview-safe branch per the Custom Function node page above (previews have no light data).
  - For cast shadows, pass `GetMainLight` a shadow coord and declare the three main-light shadow [keywords](https://docs.unity3d.com/Packages/com.unity.shadergraph@<VERSION>/manual/Keywords.html) (`_MAIN_LIGHT_SHADOWS`, `_MAIN_LIGHT_SHADOWS_CASCADE`, `_SHADOWS_SOFT` — Boolean, Global, Multi Compile), or `shadowAttenuation` reads 1 everywhere.
  - `shader-graph-create-custom-node` turns finished HLSL into a reflected function node (it needs `com.unity.shadergraph` 17.5 or newer); the graph wiring above stays this reference's recipe.
- **Hand-written HLSL route** — a single ~120-line URP shader covers it. Same quantized-NdotL core; add `_BaseColor`, `_ShadowTint`, `_Bands`.

Either way: **shadow bands are palette-tinted, never black** — sample the art bible's shadow color (usually a cooler, darker sibling of the base) — and main-light shadow attenuation multiplies into the band input on both routes so cast shadows band too.

## Outlines

- **Inverted hull** (default): a second pass/material extruding vertices along normals in clip space, front-face culled. Width in screen space (~2 px equivalent) so it doesn't collapse with distance; outline color = palette's darkest value, not black.
- **Renderer Feature** (whole-frame route): a fullscreen edge pass on depth+normals when you want environment edges too. Prefer this when the environment is mostly flat walls — hull outlines on giant planes look wrong. The feature is Render Graph code: run `validate-urp-render-graph-renderer-feature` over it before trusting it.

## The supporting cast

- **Skybox**: gradient from the palette (simple shader or tinted procedural) — the default blue procedural sky instantly breaks the style.
- **Fog**: if used, tinted from the palette.
- **Verify** (via `unity-cli`): machine scan first — eval over every renderer in frame asserting `sharedMaterial.shader.name` belongs to the toon family, with zero hits on `Universal Render Pipeline/Lit` (any hit names the object to convert) — then capture: bands visible on a curved surface, outlines uniform at two capture distances.

## Expansion scope

Ramp textures for painterly grades, rim/specular toon terms, face-shadow maps for characters, 2D-lit variant, VFX Graph toon integration, per-object outline masks.
