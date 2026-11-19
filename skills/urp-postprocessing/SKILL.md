---
name: urp-postprocessing
description: Sets up, configures, and debugs URP post-processing with the Volume framework. Use when asked to add or tune effects such as bloom, tonemapping, depth of field, or to fix Volume overrides that don't display.
required_packages:
  com.unity.render-pipelines.universal: ">=14.0.0"
---

Help the user set up, configure, and debug post-processing effects using URP's Volume framework.

**Goal: The user should have a working visual result with zero console errors after setup.**

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Run C# with `unity command script eval --code '<snippet>'`. Don't use `using` directives or unqualified types in the snippet. The compiler reads using `UnityEngine` and a bare AssetDatabase, Volume, or Object causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

## Step 1

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

## Full topic map

Where the URP manual lives depends on the Unity version, so check the version first. Read the Unity version from `ProjectSettings/ProjectVersion.txt` (for example `6000.0` from `6000.0.58f1`), and each package's major.minor from `Packages/packages-lock.json` (for example `17.0` from `17.0.4`).

**Unity 6000.0 or later:** if the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder. Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/`.

**Earlier than Unity 6000.0:** the docs pages are local markdown conversions of the URP manual, stored at `<project-folder>/Library/PackageCache/com.unity.render-pipelines.universal@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. Some page names differ from the list below, so find each page by its title. If that folder has no `.md` files, use the online pages at `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@<VERSION>/manual/` instead, because some package versions don't ship their docs.

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`
- **docs.unity.com**: The product version, eg `7000.0`

Before fetching, check that `<VERSION>` is listed as a `## Version` heading in https://docs.unity.com/en-us/engine/llms.txt. If it isn't, use the docs.unity3d.com Manual instead. The two sites use different page names, so don't reuse the paths below. Instead, find each page by its title in `https://docs.unity3d.com/<VERSION>/Documentation/Manual/docdata/toc.js`, then fetch `https://docs.unity3d.com/<VERSION>/Documentation/Manual/<link>.html`, where `<link>` is the `link` value next to the title.

- [Add post-processing in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/add-post-processing.md) — local `urp/add-post-processing.html`: enabling post-processing on a camera and adding a Volume.
- [Understand volumes in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volumes.md) — local `urp/Volumes.html`: Global and Local Volumes and how they blend.
- [Set up a volume in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/set-up-a-volume.md) — local `urp/set-up-a-volume.html`: creating a Volume and setting its mode.
- [Create a Volume Profile in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volume-profile.md) — local `urp/Volume-Profile.html`: creating and assigning Volume Profiles.
- [Configure Volume Overrides in URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volume-overrides.md) — local `urp/VolumeOverrides.html`: adding overrides and enabling their properties.
- [Volume component reference for URP](https://docs.unity.com/en-us/engine/<VERSION>/manual/post-processing-and-full-screen-effects/urp/volumes-landing-page/volume-component-reference.md) — local `urp/volume-component-reference.html`: Volume component properties.

### API reference

The API is `UnityEngine.Rendering.Universal` (effects such as `Bloom`, and `UniversalAdditionalCameraData`), part of the `com.unity.render-pipelines.universal` package, and `UnityEngine.Rendering` (`Volume`, `VolumeProfile`, `VolumeComponent`), part of the `com.unity.render-pipelines.core` package. It is versioned by package version, not Unity version (e.g. `@14.0`, `@17.0`), so check the project's installed package versions before relying on a specific class or method.

1. Read the installed package versions from `Packages/packages-lock.json`.
2. For each package, fetch and cache `https://docs.unity3d.com/Packages/<package-name>@<version>/xrefmap.yml` (replace `<version>` with the installed package version, e.g. `17.0`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/UnityEngine.Rendering.Universal.Bloom.html` => `https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.Bloom.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions.

## Step 2. Prerequisite: an Editor you can run C# in

Volume profiles, `VolumeParameter.overrideState`, and the camera's post-processing flags are
Editor/runtime object state — the checks and edits below all run C# inside a live Editor.

**The `unity-cli` skill owns getting you there** — installing the CLI, confirming a connected
Editor, adding the project's `com.unity.pipeline` package, telling a genuinely absent Editor
apart from one stuck in Safe Mode, and discovering the Editor's command catalog. Follow it
first; don't re-derive any of it here. You need `script eval` in particular, not just a reachable
Editor: its presence depends on the Pipeline package version, not on the CLI. If it's
missing, say so and stop.

Run C# through the connected Editor with the `script eval` command. Discover its parameter shape
from `unity command --format json` rather than assuming one — the inline form is
`unity command script eval --code '<snippet>'`, and some Pipeline versions also register
`script eval_file` for running a snippet from a file. **Check the catalog before reaching for
`script eval_file`; it is frequently absent.** `script eval`'s default timeout depends on the Pipeline version, so read it from the command's schema (`unity command --detail full --format json`) and pass `--timeout <seconds>` for a slow call.

### Passing C# to `script eval`

`script eval` compiles a **statement block, not a file**. Two consequences, both of which cause a
compile error rather than a warning:

- **No `using` directives.** The compiler reads `using UnityEngine;` as a resource-disposal
  statement and rejects it (`CS0210`).
- **Types must be fully qualified.** A bare `AssetDatabase` or `Volume` does not resolve
  (`CS0246` / `CS0103`), and a bare `Object` is ambiguous with `object` (`CS0104`).

Where a snippet below is written as a file — with usings, for readability, or because it is
meant to be saved into the project — qualify the types before passing it to `script eval`.

## Step 3. Pre-Flight Checks

Before configuring any effect, **verify all checks**. Fix failures first.

1. **URP is the active render pipeline** — If not, inform the user and stop.
2. **HDR is enabled on the URP Asset** — Required for Tonemapping. Bloom works best with HDR; in SDR it still works but `threshold` must be < 1.
3. **Camera has post-processing enabled** — `renderPostProcessing` must be `true` (defaults to `false`). Camera Stacking: only a `CameraRenderType.Base` camera (or the last `Overlay` in the stack) should enable post-processing. Also verify the Renderer's PostProcessData asset is not null — if it is, the post-process pass won't exist.
4. **The Volume's GameObject layer is in the Camera's Volume Layer Mask** — `volumeLayerMask` defaults to layer 0 "Default" only. The Volume's `GameObject.layer` must be included, otherwise the camera ignores it.
5. **Volume exists with `enabled = true`, a valid Profile, and at least one override** — The `Volume` component must be enabled, have a non-null `profile` (or `sharedProfile`), and at least one `VolumeComponent` with `overrideState = true` on its properties.

### Pre-Flight Check Snippet

Run this to verify the setup programmatically:

```csharp
// `script eval` compiles a statement block, not a file: no `using` directives are
// allowed, so every type is fully qualified.
var report = new System.Text.StringBuilder();

// 1. Check URP is active — a hard stop, so throw: it fails `script eval` loudly
var urpAsset = UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset;
if (urpAsset == null)
    throw new System.Exception("URP is not the active render pipeline.");

// 2. Check HDR
if (!urpAsset.supportsHDR)
    report.AppendLine("Warning: HDR is disabled on the URP Asset. Tonemapping won't work; Bloom requires threshold < 1.");

// 3. Check camera post-processing
var cam = UnityEngine.Camera.main;
if (cam == null)
    throw new System.Exception("No Main Camera found.");
if (!cam.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var camData))
    throw new System.Exception("Missing UniversalAdditionalCameraData on camera. Is URP active?");
if (!camData.renderPostProcessing)
    report.AppendLine("Warning: Post-processing is disabled on the camera. Enable via camData.renderPostProcessing = true.");

// 4. Check volume layer mask
var volumes = UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(UnityEngine.FindObjectsSortMode.None);
foreach (var vol in volumes)
{
    if (!vol.enabled) { report.AppendLine($"Warning: Volume '{vol.name}' is disabled."); continue; }
    if ((camData.volumeLayerMask & (1 << vol.gameObject.layer)) == 0)
        report.AppendLine($"Warning: Volume '{vol.name}' on layer {vol.gameObject.layer} is not in camera's volumeLayerMask.");
    // 5. Check profile and overrides
    var profile = vol.sharedProfile;
    if (profile == null) { report.AppendLine($"Warning: Volume '{vol.name}' has no profile assigned."); continue; }
    if (profile.components.Count == 0)
        report.AppendLine($"Warning: Volume '{vol.name}' profile has no overrides.");
}

// Return the findings: logs land in the Editor console, the returned value comes back to you
return report.Length == 0 ? "Post-processing setup looks correct." : report.ToString();
```

## Step 4. Volume Setup

Effects are added as **VolumeComponent overrides** on a **VolumeProfile** (a `ScriptableObject`).

**Global Volume** (most common): GameObject with `Volume` component, `isGlobal = true`, `profile` assigned. Affects every camera whose `volumeLayerMask` includes the Volume's layer.

**Local Volume (optional, but takes precedence)**: GameObject with trigger `Collider` + `Volume` component, `isGlobal = false`. Properties:
- `priority` (float) — higher values override lower when volumes overlap.
- `blendDistance` (float) — outer distance in world units to start blending from (0 = no blend, instant transition at collider boundary).
- `weight` (float, 0–1) — scales the volume's overall influence.

## Step 5. Post-Processing Effects

All effects are `VolumeComponent` subclasses added as overrides on a `VolumeProfile` via `profile.Add<T>()`. Check existence with `profile.Has<T>()` or `profile.TryGet<T>(out var t)`. Remove with `profile.Remove<T>()`.

Every property is a `VolumeParameter`. You **must** set `overrideState = true` before setting `value`, otherwise the Volume system ignores it.

When configuring a specific effect, load the full API reference:
- [references/effect-reference.md](references/effect-reference.md) — All VolumeComponent properties by effect (Bloom, Tonemapping, ColorAdjustments, DepthOfField, Vignette, MotionBlur, FilmGrain, ChromaticAberration, SplitToning, LensDistortion, WhiteBalance, PaniniProjection, LiftGammaGain, ShadowsMidtonesHighlights, ColorCurves, ChannelMixer)

For code templates:
- [references/code-templates.md](references/code-templates.md) — Global Volume setup, camera post-processing, and profile modification templates

## Step 6. Anti-Hallucination Rules

### Required Usings

These apply when you write a `.cs` file into the project. **A snippet passed to `script eval` cannot
carry them** — qualify the types instead (see "Passing C# to `script eval`" above).

```csharp
using UnityEngine.Rendering;           // Volume, VolumeProfile, VolumeComponent, VolumeParameter
using UnityEngine.Rendering.Universal;  // Bloom, Tonemapping, ColorAdjustments, UniversalRenderPipeline, etc.
```

### Wrong → Correct API Mapping

| WRONG | CORRECT |
|-------|---------|
| `PostProcessVolume` | `Volume` (from `UnityEngine.Rendering`) |
| `PostProcessLayer` | `UniversalAdditionalCameraData.renderPostProcessing` (bool) |
| `UnityEngine.Rendering.PostProcessing` | `UnityEngine.Rendering.Universal` |
| `profile.GetSetting<T>()` | `profile.TryGet<T>(out var t)` (returns bool) |
| `profile.AddSettings<T>()` | `profile.Add<T>()` (returns T; throws if already exists — check `profile.Has<T>()` first) |
| `volume.sharedProfile` (to modify at runtime) | `volume.profile` (auto-clones the asset into an instance) |
| `VolumeManager.instance.stack.GetComponent<T>()` | `volume.profile.TryGet<T>(out var t)` |

### Key Facts
- **`overrideState = true`** is required on every `VolumeParameter` you set. The volume system skips parameters where `overrideState` is `false`. This is the #1 scripting mistake.
- **`sharedProfile`** = returns the asset directly (edits persist to disk). **`profile`** = auto-clones into an instance if needed (safe for runtime edits). Check with `volume.HasInstantiatedProfile()`.
- **`profile.Add<T>(bool overrides = false)`** — pass `true` to auto-enable `overrideState` on all parameters of the added component.

## Step 7. Debugging Checklist

When post-processing isn't working, check in order:

1. `cam.TryGetComponent<UniversalAdditionalCameraData>(out var data)` succeeds and `data.renderPostProcessing` is `true`?
2. Volume exists in scene with a non-null `profile` (or `sharedProfile`) assigned?
3. Overrides added via `profile.Add<T>()` AND `overrideState = true` on each property you set?
4. Volume's `GameObject.layer` is included in camera's `data.volumeLayerMask`? (Default mask is layer 0 "Default" only.)
5. `volume.isGlobal = true` (for global), or camera is inside the Volume's trigger `Collider` (for local)?
6. Camera `data.renderType` is `CameraRenderType.Base`, not `Overlay`? (Overlay cameras composite onto the Base camera's output.)
7. `UniversalRenderPipeline.asset.supportsHDR` is `true`? Required for Bloom and Tonemapping.
8. Viewing in **Game view**? Scene view has a separate post-processing toggle in its toolbar.

## Step 8. Common Recipes

Format: Effect property=value. Bloom values are threshold/intensity/scatter.

**Cinematic (Film):** Tonemapping mode=ACES, ColorAdjustments contrast=15 saturation=-10, Bloom threshold=0.9 intensity=0.5 scatter=0.7, Vignette intensity=0.3 smoothness=0.4, FilmGrain type=Medium1 intensity=0.2

**Stylized/Vibrant:** Tonemapping mode=Neutral, ColorAdjustments saturation=20 contrast=10, Bloom threshold=0.8 intensity=1.5 scatter=0.6, SplitToning highlights=warm shadows=cool

**Horror/Dark:** ColorAdjustments postExposure=-0.5 saturation=-30 contrast=20, Vignette intensity=0.5 smoothness=0.3 color=dark-red, FilmGrain type=Large01 intensity=0.4, ChromaticAberration intensity=0.15

**Clean/Mobile:** Tonemapping mode=Neutral, ColorAdjustments postExposure=0.2, Bloom threshold=1.0 intensity=0.3 (subtle). Avoid FilmGrain, MotionBlur, DepthOfField on mobile.

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Give the user a concise but not dense checklist of what you did and why, including any incidental side effects (for example, settings you toggled to test something, or temporary files you created). If the user needs to follow up with more steps or decisions, list them. After setup, report to user:

```
Post-Processing Setup Complete
- Volume: [Global/Local] on "[GameObject Name]"
- Profile: [Asset Path]
- Effects: [List with key property=value pairs]
- Camera: [Name] — renderPostProcessing=true, volumeLayerMask includes layer [N]

View results in Game view (not Scene view).
Undo all changes with Edit > Undo (Ctrl+Z).
```
