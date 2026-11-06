---
name: optimize-text-mesh-pro
description: Optimizes TextMeshPro rendering, memory, and font setup, including font asset stacks, fallback atlases, SDF quality, and worldspace text. Use when the user asks about TextMeshPro font assets, CJK font fallbacks, or TMP memory, build size, quality or performance, not about translating text.
---

# Optimize TextMeshPro

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Don't use `using` directives or unqualified types in the `snippet` for `eval`. The compiler reads `using UnityEngine` and a bare `AssetDatabase`, `Volume`, or `Object` causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Never leave verification code in the user's script; never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

First, read the Unity version from `ProjectSettings/ProjectVersion.txt` and the `com.unity.ugui` or `com.unity.textmeshpro` version from `Packages/packages-lock.json`, because the doc URLs and some advice depend on them. For example, before you recommend Dynamic OS, check that `TMPro.AtlasPopulationMode.DynamicOS` exists in the installed package's [API reference](#api-reference), because older `com.unity.textmeshpro` versions don't have it.

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`

For TextMeshPro, if `com.unity.ugui` is 2.0 or later, TMP is part of uGUI and its docs are in the `com.unity.ugui` package; otherwise they are in the `com.unity.textmeshpro` package. Use the one listed in `Packages/packages-lock.json`.

## Glossary

Some names mean different things in the product, the UI and the API, so use these meanings:

- **TextMeshPro (TMP)**: the text system as a whole.
- **`TextMeshPro` component**: the 3D text component for text in the scene, without a Canvas.
- **`TextMeshProUGUI` component**: the UI text component that renders through a Canvas.
- **Dynamic** and **Dynamic OS**: two different Atlas Population Mode values. Dynamic uses the project's font file, and Dynamic OS uses a font on the device. Don't treat one as the other.

## Workflow

1. Read the Unity version and the `com.unity.ugui` or `com.unity.textmeshpro` version, as described above.
2. Read the doc pages that cover the area you'll work on, as described above.
3. Identify the symptom with the [Triage](#triage--identify-the-symptom-first) table, asking the user if they haven't described one.
4. Follow the matching section. Give advice by default, and change settings only when the user asks you to, because many of these settings change memory use or build size for the whole project. Before you change anything, create a restore point.
5. Run the [Final step](#final-step), then give the [Final report](#final-report).

## Triage — identify the symptom first

Before providing tips, identify which category the user's issue falls into. If the user has not described a specific symptom, ask: "Are you seeing a **memory/atlas bloat**, **visual quality**, **CPU/performance**, **build size**, or **localization/alignment** issue with TextMeshPro?"

| Symptom | Go To |
|---|---|
| Memory Profiler shows large or multiple TMP atlases | [Font Stack & Dynamic Fallbacks](#font-stack--dynamic-fallbacks), [Memory Profiler: Include Font Data](#memory-profiler-include-font-data) |
| Inconsistent glyph weight, fuzzy edges, visual quality | [Padding & Sampling Ratios](#padding--sampling-ratios), [Font Asset Scale](#font-asset-scale), [Atlas Render Mode: SDF16](#atlas-render-mode-sdf16) |
| CPU spikes during text updates or Canvas rebuilds | [AutoSize](#autosize), [Worldspace vs Canvas Text](#worldspace-vs-canvas-text) |
| Build size too large from shipped font files | [Dynamic OS Atlas Population](#dynamic-os-atlas-population) |
| Mixed Latin + CJK alignment looks off | [Font Normalization](#font-normalization) |
| Need multiple font styles (italic, outline, glow) | [Material Presets](#material-presets) |

---

## Core Rules

- **Main font = static asset with all glyphs baked in.** Add **dynamic** fallbacks to the font asset's own Fallback list for everything else, not the global list in TMP Settings, unless every font in the project should share them (see `FontAssetsFallback.md`). Keep dynamic atlas size at **512-1024** to bound peak memory.
- **Dynamic fallback fonts -> enable `Clear Dynamic Data On Build`.** Otherwise editor-baked glyphs ship in the player.
- **Keep Padding-to-Sampling-Point-Size ratio consistent across primary + fallback fonts.** Mismatch produces inconsistent glyph weight on the same line.
- **Latin sampling point size 70-90; CJK 36-50.** Different scripts need different sampling sizes for clean SDF.
- **Font asset Scale = 1.** Anything else (e.g., 0.9) breaks standard point-size math.
- **Disable AutoSize at runtime once layout is locked.** AutoSize is for design, not for live counters.
- **Worldspace text -> use `TextMeshPro`, not `TextMeshProUGUI`.** Canvas overhead in worldspace is not free.
- **Parent often-changing TMP UI to its own Canvas** to bound rebuild cost.
- **TMP material presets > duplicating font assets** for italic / bold / outline / glow variants of the same font.
- **For shipping multilingual builds on iOS/Android, evaluate `Atlas Population Mode = Dynamic OS`** to leverage system fonts and shrink the build.

---

## Font Stack & Dynamic Fallbacks

If the user reports memory bloat from TMP atlases, advise this font stack pattern:

```
Main font asset (static, all required Latin glyphs baked)
  -> Fallback 1: Dynamic font (atlas 512 or 1024) for CJK
  -> Fallback 2: Dynamic font for symbols / emoji
```

Don't use one dynamic font asset for every language, because its atlas keeps growing with every glyph the game uses. Check `Clear Dynamic Data On Build` on each dynamic fallback, as the Core Rules say (see `FontAssets.md`).

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

If the user reports point sizes not matching design specs, check the font asset Scale value. Some imported TMP font assets ship with `Scale = 0.9` instead of `1.0`. A non-1 Scale changes every rendered point size (see `FontAssetsProperties.md`). Advise the user to **set Scale = 1 on all font assets before adjusting padding ratios**.

---

## Sprite Assets

For the source texture settings a Sprite Asset needs, read `Sprites.md` (see the [Full topic map](#full-topic-map)). If the user reports slow loading times for TMP Sprite Assets on mobile, check the source texture's Texture Type, and suggest setting it back to **Default** once the Sprite Asset is created, because Sprite type keeps child sprite sub-objects that TMP doesn't use.

---

## AutoSize

If the user reports CPU spikes on text fields that change frequently (timers, counters, chat, dynamic player names), check whether `enableAutoSizing` is on. AutoSize resizes the text whenever the string changes, causing constant CPU spikes.

Advise: **disable AutoSize and hard-code the chosen point size** once layout is locked. Keep AutoSize on only for genuinely static labels that auto-fit on locale change.

---

## Atlas Render Mode: SDF16

If a static font with point size **72 or larger** looks unclear or has fuzzy edges, advise switching the **Atlas Render Mode** to **SDF16**. Higher precision SDF for big glyphs, at slightly more atlas memory.

---

## Font Normalization

If the user reports misaligned Latin + CJK text on the same line, compare the line metrics of the primary and fallback font assets, and advise adjusting the fallback's ascender and descender until they match the primary's. Read `FontAssetsLineMetrics.md` (see the [Full topic map](#full-topic-map)) for what each metric controls.

To show the lines in the scene, the `TMP_TextInfoDebugTool` component from TMP Examples & Extras draws them. Ask the user before importing Examples & Extras, because it adds unrelated example assets to the project.

---

## Material Presets

If the user needs multiple styles (italic, bold, outline, glow) of the same font, advise material presets instead of duplicating font assets. Presets share the same font texture but override shader parameters. For the Material Preset property on the text component, read `TMPObjectUIText.md`, and for the shader settings a preset can override, read `Shaders.md` (see the [Full topic map](#full-topic-map)).

---

## Dynamic OS Atlas Population

If the user is shipping multilingual builds and concerned about build size, advise evaluating **`Atlas Population Mode = Dynamic OS`**. Read the Dynamic OS font asset section of [Font assets](https://docs.unity.com/en-us/engine/<VERSION>/manual/uitoolkits/uielements/uie-work-with-text/uie-font-asset/uie-font-asset.md) for what it changes in the build. The TMP package manual doesn't cover Dynamic OS, but this page covers the font assets that TMP and UI Toolkit share.

At runtime, Dynamic OS looks for a device font with the same Family and Style name as the source font, so choose a source font whose name exists on each target platform. Recommended system fonts for CJK:

| Platform | Recommended system font |
|---|---|
| **Android** | NotoSans (covers Chinese, Japanese, Korean glyphs broadly). |
| **iOS** | PingFang for Simplified/Traditional Chinese. iOS uses **unique fonts per language** for CJK (different families for Chinese, Japanese, Korean) - check the fallback chain when shipping a single TMP setup across all three. |

---

## Memory Profiler: Include Font Data

If Memory Profiler shows unexpectedly large font asset sizes, check **Include Font Data** on the `.ttf` / `.otf` import settings. Read [TrueTypeFontImporter.includeFontData](https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityeditor/truetypefontimporter/includefontdata.md) for what it controls.

Keep it on for fonts that dynamic font assets use, because they generate glyphs from the font data at runtime. Don't turn it off just to make an Editor capture look like a device, because it changes what ships in the build. For build size, use static font assets or [Dynamic OS](#dynamic-os-atlas-population) instead.

---

## Worldspace vs Canvas Text

If the user has worldspace text (damage numbers, signs, holograms) using `TextMeshProUGUI`, advise switching to **`TextMeshPro`**. Worldspace Canvas is a known inefficiency.

If a `TextMeshProUGUI` element's `text` changes often (timers, counters, chat), advise **parenting it under a child GameObject with its own Canvas component**. Canvas rebuilds are scoped per-Canvas, so isolating the volatile field cuts rebuild cost on the rest of the UI.

---

## Full topic map

The docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.ugui@*/` (TMP pages in `Documentation~/TextMeshPro/`) or, for the older package, `<project-folder>/Library/PackageCache/com.unity.textmeshpro@*/` (TMP pages in `Documentation~/`). Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time.

If that folder has no .md files, because some packages don't ship their docs, use the online pages at `https://docs.unity3d.com/Packages/com.unity.ugui@<VERSION>/manual/TextMeshPro/<page>.html` or `https://docs.unity3d.com/Packages/com.unity.textmeshpro@<VERSION>/manual/<page>.html`, where `<page>` is the file name from the list below without `.md`. Use `.html`, because package docs on docs.unity3d.com don't publish a `.md` version of each page. Some older `com.unity.textmeshpro` versions publish only an index page; if a page is missing, use the `com.unity.ugui@2.0` page instead.

- **Text objects** — `TMPObjectUIText.md`: `TextMeshProUGUI` properties, including Auto Size and Material Preset
- **Font Assets** — `FontAssets.md`: overview of font assets, including Clear Dynamic Data on Build
  - Font Asset Properties — `FontAssetsProperties.md`: Scale, Atlas Population Mode, atlas size, font weights
  - Font Asset Creator — `FontAssetsCreator.md`: sampling point size, padding, render mode
  - Dynamic Fonts — `FontAssetsDynamicFonts.md`: how dynamic font assets add glyphs, and what they cost
  - Fallback Fonts — `FontAssetsFallback.md`: per-asset and global fallback chains
  - Line Metrics — `FontAssetsLineMetrics.md`: ascender, descender, baseline, for aligning mixed scripts
  - Signed Distance Fields — `FontAssetsSDF.md`: how SDF atlases render text
- **Sprites** — `Sprites.md`: creating Sprite Assets and the source texture settings
- **Shaders** — `Shaders.md`: TMP shaders and the material properties a preset overrides
- **Settings** — `Settings.md`: TMP Settings, default and global fallback fonts

### API reference

The API is the `TMPro` namespace (for example `TMP_FontAsset`, `TextMeshPro`, `TextMeshProUGUI`), part of the `com.unity.ugui` package (2.0 or later) or the older `com.unity.textmeshpro` package. It is versioned by package version, not Unity version (e.g. `@2.0`, `@3.0`), so check the project's installed package version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the installed package version, or read it from `Packages/packages-lock.json`.
2. Fetch and cache `https://docs.unity3d.com/Packages/<package-name>@<version>/xrefmap.yml` (replace `<package-name>` with `com.unity.ugui` or `com.unity.textmeshpro`, and `<version>` with the installed package version, e.g. `2.0`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/TMPro.TMP_FontAsset.html` => `https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/TMPro.TMP_FontAsset.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions.

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console_status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

If you changed settings, also read them back through the API, because an Inspector or script change can fail to save. For example: `TMP_FontAsset.atlasPopulationMode`, `m_ClearDynamicDataOnBuild` (through `SerializedObject`), the font asset's `faceInfo.scale`, `enableAutoSizing` on the text component, `TrueTypeFontImporter.includeFontData`, and `TextureImporter.textureType` for a sprite source texture. If a value doesn't match what you set, set it again and read it back.

If you changed settings to reduce memory or build size, compare a measurable result before and after, because a saved setting doesn't prove the problem is gone. For example, record the number and dimensions of `TMP_FontAsset.atlasTextures` for each font asset in the stack before your change, and read them again after. If the result didn't improve, reread the [docs pages](#full-topic-map), and say so in the final report rather than claiming a fix.

## Final report

Report the following, in this order:

1. The symptom category, and each setting you recommended or changed, with the current and recommended values.
2. The verification results from the final step, including the before and after measurements.
3. Any side effects of your changes, such as Examples & Extras assets you imported or other assets that use a font asset you changed.
4. Follow-up the user must do, such as testing Dynamic OS fonts on iOS and Android devices, or capturing a new Memory Profiler snapshot.
5. The doc pages you read.
