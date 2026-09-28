---
name: optimize-text-mesh-pro
description: >
  Covers TextMeshPro font stacks, dynamic fallback atlases, padding and
  sampling ratios, SDF16, AutoSize discipline, worldspace vs UGUI, and Memory
  Profiler font-data capture. Use when the user mentions TextMeshPro,
  Text Mesh Pro, TMP (TextMeshPro), font asset, dynamic atlas, TMP localization,
  CJK (Chinese, Japanese, Korean) fonts, font alignment across
  scripts, mixed western and eastern fonts, text rendering performance, profiler
  markers related to text generation or glyph rasterization, font fallback
  strategy, font normalization, multilingual or localized text rendering, SDF
  font quality, or text-related memory issues—not for UI Toolkit layout
  (unity-ui-toolkit) or non-TMP uGUI (unity-ui).
---

# Optimize TextMeshPro

## Triage — identify the symptom first

Before providing tips, identify which category the user's issue falls into. If the user has not described a specific symptom, ask: "Are you seeing a **memory/atlas bloat**, **visual quality**, **CPU/performance**, **build size**, or **localization/alignment** issue with TextMeshPro?"

| Symptom | Go To |
|---|---|
| Memory Profiler shows large or multiple TMP atlases | [Font Stack & Dynamic Fallbacks](#font-stack--dynamic-fallbacks), [Memory Profiler: Include Font Data](#memory-profiler-include-font-data) |
| Inconsistent glyph weight, fuzzy edges, visual quality | [Padding & Sampling Ratios](#padding--sampling-ratios), [Font Asset Scale](#font-asset-scale), [Atlas Render Mode: SDF16](#atlas-render-mode-sdf16) |
| CPU spikes during text updates or Canvas rebuilds | [AutoSize](#autosize), [Worldspace vs Canvas Text](#worldspace-vs-canvas-text) |
| Build size too large from shipped font files | [Dynamic OS Atlas Population](#dynamic-os-atlas-population-tmp-320-pre3) |
| Mixed Latin + CJK alignment looks off | [Font Normalization](#font-normalization) |
| Need multiple font styles (italic, outline, glow) | [Material Presets](#material-presets) |

---

## Core Rules

- **Main font = static asset with all glyphs baked in.** Add **dynamic** fallbacks via the Fallback list (or TMP Settings) for everything else. Keep dynamic atlas size at **512-1024** to bound peak memory.
- **Dynamic fallback fonts -> enable `Clear Dynamic Data On Build`.** Otherwise editor-baked glyphs ship in the player.
- **Keep Padding-to-Sampling-Point-Size ratio consistent across primary + fallback fonts.** Mismatch produces inconsistent glyph weight on the same line.
- **Latin sampling point size 70-90; CJK 36-50.** Different scripts need different sampling sizes for clean SDF.
- **Font asset Scale = 1.** Anything else (e.g., 0.9) breaks standard point-size math.
- **Disable AutoSize at runtime once layout is locked.** AutoSize is for design, not for live counters.
- **Worldspace text -> use `TextMeshPro`, not `TextMeshProUGUI`.** Canvas overhead in worldspace is not free.
- **Parent often-changing TMP UI to its own Canvas** to bound rebuild cost.
- **TMP material presets > duplicating font assets** for italic / bold / outline / glow variants of the same font.
- **For shipping multilingual builds on iOS/Android, evaluate `Atlas Population Mode = Dynamic OS`** (TMP 3.2.0-pre.3+) to leverage system fonts and shrink the build.

---

## Font Stack & Dynamic Fallbacks

If the user reports memory bloat from TMP atlases, advise this font stack pattern:

```
Main font asset (static, all required Latin glyphs baked)
  -> Fallback 1: Dynamic font (atlas 512 or 1024) for CJK
  -> Fallback 2: Dynamic font for symbols / emoji
```

**NEVER ship a dynamic fallback font asset without enabling `Clear Dynamic Data On Build`.** Every glyph baked while testing in the editor is included in the player build if this toggle is off.

---

## Padding & Sampling Ratios

If the user reports inconsistent stroke widths or glyph weight differences between primary and fallback fonts, check the padding-to-sampling-point-size ratio.

The ratio is `Padding / SamplingPointSize`. With Padding = 9 and Sampling Point Size = 90, ratio = **10%**.

- A primary font with one ratio and a fallback with a different ratio produces **inconsistent stroke widths** on the same line.
- Pick a ratio (10% is a safe default), apply it to all font assets in the chain.

Recommended sampling point sizes:

- **Latin scripts**: 70-90.
- **CJK scripts**: 36-50 (CJK glyphs are visually denser; smaller sampling sizes still produce clean SDF and save atlas memory).

---

## Font Asset Scale

If the user reports point sizes not matching design specs, check the font asset Scale value. Some imported TMP font assets ship with `Scale = 0.9` instead of `1.0`. The Scale value participates in the point-size-to-pixels math, so a non-1 scale produces non-standard point sizes. Advise the user to **set Scale = 1 on all font assets before adjusting padding ratios**.

---

## Sprite Assets

If the user reports slow loading times for TMP Sprite Assets on mobile, check the source texture's Texture Type. It must be set to **Default** (not Sprite). Sprite type creates child sub-objects that TMP doesn't use; Default avoids them.

---

## AutoSize

If the user reports CPU spikes on text fields that change frequently (timers, counters, chat, dynamic player names), check whether `enableAutoSizing` is on. AutoSize resizes the text whenever the string changes, causing constant CPU spikes.

Advise: **disable AutoSize and hard-code the chosen point size** once layout is locked. Keep AutoSize on only for genuinely static labels that auto-fit on locale change.

---

## Atlas Render Mode: SDF16

If a static font with point size **72 or larger** looks unclear or has fuzzy edges, advise switching the **Atlas Render Mode** to **SDF16**. Higher precision SDF for big glyphs, at slightly more atlas memory.

---

## Font Normalization

If the user reports misaligned Latin + CJK text on the same line, walk them through this procedure:

1. **Window -> TextMeshPro -> Settings -> Import TMP Example & Extras** (one-time per project).
2. Add the **`TMP_TextInfoDebugTool`** component to the TextMeshPro object displaying misaligned text.
3. Enable **ShowLines** toggle - the ascender, descender, and baseline render as overlays.
4. Mix Latin + CJK strings; if the lines diverge, **adjust ascender/descender on the TMP Font Asset** until they align.

> **Caveat**: importing TMP Examples & Extras has been observed to cause an infinite import loop on some project layouts. If it happens, close Unity and re-open - the import resolves on the second attempt.

---

## Material Presets

If the user needs multiple styles (italic, bold, outline, glow) of the same font, advise material presets instead of duplicating font assets. Presets share the same font texture but override shader parameters.

How to create:

1. Select a TMP Text GameObject.
2. In Inspector, find the **Material** section.
3. **Right-click the Material header -> Create Material Preset.**
4. Rename the new material and tweak settings.
5. On the TMP Text component, pick the preset from the **Material Preset dropdown**.

---

## Dynamic OS Atlas Population (TMP 3.2.0-pre.3)

If the user is shipping multilingual builds and concerned about build size, advise evaluating **`Atlas Population Mode = Dynamic OS`** (TMP 3.2.0-pre.3+):

- In Editor: still uses the source font from the project.
- In a player build: **the source font is not included**. At runtime, Unity searches the device for a font with the matching Family + Style name.

Recommended system fonts for CJK:

| Platform | Recommended system font |
|---|---|
| **Android** | NotoSans (covers Chinese, Japanese, Korean glyphs broadly). |
| **iOS** | PingFang for Simplified/Traditional Chinese. iOS uses **unique fonts per language** for CJK (different families for Chinese, Japanese, Korean) - check the fallback chain when shipping a single TMP setup across all three. |

Wins: build size shrinks (no shipped CJK font files) and memory drops (system font is shared with the OS).

---

## Memory Profiler: Include Font Data

If Memory Profiler shows unexpectedly large font asset sizes in the Editor, check whether **Include Font Data** is enabled on the `.ttf` / `.ttc` import settings. The Editor includes the source font file in the asset by default, but on device (especially with Dynamic OS), this cost is not paid.

To make Editor captures match device: on the font file -> deselect **Include Font Data** in the import settings. Memory Profiler will then show overhead **without** the underlying font file.

---

## Worldspace vs Canvas Text

If the user has worldspace text (damage numbers, signs, holograms) using `TextMeshProUGUI`, advise switching to **`TextMeshPro`**. Worldspace Canvas is a known inefficiency.

If a `TextMeshProUGUI` element's `text` changes often (timers, counters, chat), advise **parenting it under a child GameObject with its own Canvas component**. Canvas rebuilds are scoped per-Canvas, so isolating the volatile field cuts rebuild cost on the rest of the UI.

---

## Common Pitfalls

If the user's setup matches any of these, flag it:

- One giant dynamic font asset for all languages instead of static main + dynamic fallback - the dynamic atlas balloons.
- Inconsistent padding ratio across primary + fallback - same line of text looks like two fonts.
- Font asset Scale = 0.9 inherited from import - point sizes won't match design specs.
- Leaving AutoSize on for live counters - hidden CPU spikes.
- World-space `TextMeshProUGUI` inside a worldspace Canvas - extra rebuilds for no benefit; use `TextMeshPro`.
- Forgetting **Clear Dynamic Data On Build** on dynamic fallback fonts - editor-test glyphs ship in the player.
- Capturing Memory Profiler in Editor with Include Font Data on, then being surprised the on-device build is smaller.
- Sprite asset source texture set to Sprite type - mobile loading slows from extra child sub-objects.

---

## References

- TextMeshPro - Atlas Population Mode (Unity Manual): https://docs.unity3d.com/Packages/com.unity.textmeshpro@latest/manual/FontAssets.html
