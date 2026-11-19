---
name: optimize-web
description: Optimizes Unity WebGL and WebGPU builds for smaller downloads, faster loading, and smoother browser performance. Use when asked to shrink a web build, speed up its load, or fix stutter, memory or battery problems in the browser.
---
Audit and apply the Player Settings, compression, stripping, and server setup that make a Unity Web build smaller and faster.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` skill for each step. Use `unity-pipeline` too when it is available.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- If the changes affect runtime behavior, add `Debug.Log` lines that will prove the actual scene behaviour (not just your changes) is now working - you'll read these when you enter Play mode to validate later. For example, log a GameObject's Transform scale if your fix is meant to make sure it changes size.
- Avoid long explanations.

## Full topic map

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `<editor-install>/Editor/Data/Documentation/en/Manual` folder. Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/`.

For the packages this skill uses (`com.unity.cloud.ktx`, `com.unity.addressables`, `com.unity.web.stripping-tool`), the docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/<package-name>@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. If that folder has no `.md` files, use `https://docs.unity3d.com/Packages/<package-name>@<VERSION>/manual/` instead.

Prefer `WebFetch` over `WebSearch` — faster and lands on the exact reference. Only fetch what you need. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **docs.unity.com**: The Unity version, eg `6000.6`

Read the Unity major.minor from `ProjectSettings/ProjectVersion.txt` (for example `6000.0` from `6000.0.58f1`, because the full build number gives a 404), and each package's major.minor from `Packages/packages-lock.json`.

- [Recommended Player settings to optimize your Web build](https://docs.unity.com/en-us/engine/<VERSION>/manual/platform-specific/webgl/building-distribution/web-optimization/player.md) — local `web-optimization-player.html`: release values for compression, stripping, exceptions and memory.
- [Use C# code to enable optimization settings](https://docs.unity.com/en-us/engine/<VERSION>/manual/platform-specific/webgl/building-distribution/web-optimization/c-sharp.md) — local `web-optimization-c-sharp.html`: editor script that applies the recommended settings in one step.
- [Remove unused resources from your Web build](https://docs.unity.com/en-us/engine/<VERSION>/manual/platform-specific/webgl/building-distribution/web-optimization/remove-resources.md) — local `web-optimization-remove-resources.html`: removing unused packages and stripping shader variants.
- [Distribution size and code stripping](https://docs.unity.com/en-us/engine/<VERSION>/manual/platform-specific/webgl/building-distribution/distributionsize-codestripping.md) — local `webgl-distributionsize-codestripping.html`: what drives Web build size and how stripping works.
- [Deploy a Web application](https://docs.unity.com/en-us/engine/<VERSION>/manual/platform-specific/webgl/building-distribution/deploying.md) — local `webgl-deploying.html`: server compression headers, MIME types and Decompression Fallback.
- [Memory in Unity Web](https://docs.unity.com/en-us/engine/<VERSION>/manual/platform-specific/webgl/develop/memory.md) — local `webgl-memory.html`: Wasm heap size, memory growth and browser memory limits.

### API reference

The API is `UnityEditor.PlayerSettings`, `UnityEditor.PlayerSettings.WebGL` and `UnityEditor.WebGL.UserBuildSettings`, part of the Unity Engine. It is versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

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

The KTX API (`KtxUnity`) is part of the `com.unity.cloud.ktx` package. It is versioned by package version, not Unity version, so check the project's installed package version before relying on a specific class or method.

1. Read the installed package version from `Packages/packages-lock.json`.
2. Fetch and cache `https://docs.unity3d.com/Packages/com.unity.cloud.ktx@<version>/xrefmap.yml` (replace `<version>` with the installed package version, e.g. `3.4`). This file maps every public API member's fully qualified name to its docs page `href`.
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

## Procedure

1. Get the project context: the Unity version from `ProjectSettings/ProjectVersion.txt`, and the versions of the packages you need (`com.unity.cloud.ktx`, `com.unity.addressables`, `com.unity.web.stripping-tool`) from `Packages/packages-lock.json`. If there's no project, assume Unity `6000.0`. For any package not listed in `packages-lock.json`, assume the latest version in that package's documentation. Preserve every version you do find — only default the values that are actually missing. Name each assumption in your report and carry on. If the user hasn't said what to optimize and there's no project to read, ask what they want to improve.
2. Read the docs pages that cover the task, from the [Full topic map](#full-topic-map).
3. Set up `script eval` as described in [Running C# in the Editor](#running-c-in-the-editor). If no Editor is reachable or `script eval` is missing, say so, skip to step 5, then go straight to step 9. Mark build validation and runtime checks as unperformed in your report, and base your recommendations on the user's request and the docs from step 2.
4. Run [0. Pre-Flight](#0-pre-flight) and [1. Assess Current State](#1-assess-current-state). Their results are your baseline.
5. Match the request to a row in [2. Understand Request](#2-understand-request).
6. Create a restore point, then apply the changes from [3. Web Build Optimization Workflow](#3-web-build-optimization-workflow). If a symptom matches [5. Troubleshooting](#5-troubleshooting), follow that entry.
7. Run [4. Validation](#4-validation): rerun the Pre-Flight snippet and compare the values with your baseline.
8. Do the checks in [Final step](#final-step).
9. Give the [Final report](#final-report).

## Running C# in the Editor

Every step that reads or writes a Player Setting runs inside a live Editor through the Unity CLI. The `unity-cli` skill owns getting you there: installing the CLI, confirming a connected Editor, adding `com.unity.pipeline`, and telling an absent Editor apart from one stuck in Safe Mode. Follow it first.

- Confirm `script eval` appears in the command catalog, because its presence depends on the Pipeline package version. If it's missing, say so and skip the steps that read or write settings.
- Don't write Player Settings by editing `ProjectSettings/ProjectSettings.asset`, because the serialized names don't match the API names, several settings are per build target, and a hand-edited value can disagree with what the build uses. An unreachable Editor is a stop for the write steps.

Run C# with `unity command script eval --code '<snippet>'`. Its default timeout depends on the Pipeline version, so read it from the command's schema (`unity command --detail full --format json`) and pass `--timeout <seconds>` for a slow call.

Before you run any snippet, read [references/editor-scripting.md](references/editor-scripting.md). It has the Pre-Flight snippet that reads every audited setting, the API names that fail to compile when spelled the obvious way, how to apply settings with [scripts/WebOptimizer.cs](scripts/WebOptimizer.cs), and why you must save and verify from disk after every write.


## 0. Pre-Flight

1. Run the Pre-Flight snippet from [references/editor-scripting.md](references/editor-scripting.md#reading-the-settings-this-skill-audits). Read `UnityEditor.WebGL.UserBuildSettings.codeOptimization` in a separate call, because it only resolves when the Web build-support module is installed.
2. Check `activeBuildTarget` is `WebGL`. If not, warn the user.
3. Proceed only after you've recorded the compression, stripping, exception, memory, frame rate and player settings.

## 1. Assess Current State

1. **Check Build Report:** Instruct the user to open `Window > General > Build Report` after a build and identify the largest asset and code size contributors.
2. **Verify server configuration:** Ask the user to confirm whether the hosting server sends `Content-Encoding: br` (Brotli) or `Content-Encoding: gzip` headers, and whether `Content-Type: application/wasm` is set for `.wasm` files.
3. **Check frame rate and memory:** From the Pre-Flight values, `targetFrameRate` should be `-1` for Web (let the browser drive). Note `initialMemorySize` and `memoryGrowthMode`.
4. Report findings before making recommendations.


## 2. Understand Request

| User Says | Default Interpretation |
|-----------|----------------------|
| "build too large" / "download too slow" | Strip Engine Code on; Managed Stripping High; Disk Size + LTO; Brotli |
| "Decompression Fallback" / "slow startup" | Decompression Fallback off; fix server to send Content-Encoding |
| "stutter in Chrome" / "stutter in Safari" | Profile in browser DevTools; Safari caps at 60 fps |
| "excessive battery in browser" | `OnDemandRendering` on static screens; `targetFrameRate = -1` |
| "exceptions too large" | None for release; Wasm 2023 exceptions if browser baseline allows |
| "set up CDN" | Addressables remote groups + Brotli/Gzip on CDN |
| "WebAssembly 2023" | Enable when browser baseline supports it — smaller and faster |
| "memory growth slow" | Tune Initial Memory Size to peak estimate; use Geometric growth mode |
| "KTX" / "Basis Universal" / "texture formats unknown GPU" | KTX2 with Basis Universal; ETC1S for size, UASTC for quality |
| "strip unused code" / "remove unused packages" | Web Stripping Tool + remove unused packages + shader stripping |
| "quality settings for web" | Quality Level to Very Low or Low; lower quality = faster load |
| "shader variants too many" | Graphics settings: auto lightmap/fog modes; strip instancing + BRG variants; audit Always Included Shaders |
| "video not playing" / "audio issues" | Video: URL-only or StreamingAssets; Audio: no AudioEffects on Web, use Mono, compress |
| "profiler symbols" / "can't read Wasm stacks" | Embed profiling symbols via build processor or emscriptenArgs |
| "iOS crashes" / "Safari memory" | iOS memory limits; set Initial Memory Size high rather than growing; Gigacage 2GB limit pre-iOS 18 |

## 3. Web Build Optimization Workflow

**Always offer to generate the one-click optimization script.** Unity's web optimization docs provide a single editor menu script that applies all recommended release settings at once. Place it in `Assets/Editor/WebOptimizer.cs`, using [scripts/WebOptimizer.cs](scripts/WebOptimizer.cs) as the template. Adapt it to the project (for example keep exceptions if they use `try/catch`, or switch Brotli to Gzip for HTTP hosting), because it prevents settings from being missed.

For each area the request touches, read its section in [references/optimization-areas.md](references/optimization-areas.md) before you change anything. It covers the Player Settings audit table, compression and server configuration, exception handling, removing unused resources and shader variants, quality settings, frame rate, KTX / Basis Universal textures, streaming, profiling, memory, iOS Safari limits, video and audio, and canvas DPI.


## 4. Validation

1. Re-read the Player Settings with the Pre-Flight snippet from [references/editor-scripting.md](references/editor-scripting.md) (compression, stripping, exceptions, targetFrameRate).
2. Rebuild the player and compare Build Report file sizes with baseline.
3. Verify in at least Chrome and Safari (GC and JIT behavior differ).
4. Max **3 iterations** before asking the user for feedback.

## 5. Troubleshooting

If a symptom persists after the changes, find it in [references/troubleshooting.md](references/troubleshooting.md). It covers builds that stay large, Brotli needing Decompression Fallback, Safari-only stutter, slow memory growth, erratic frame rate, Firefox cache limits, and serving a build locally.


## Final step

Web settings only take effect in a browser build, so a clean compile and Play mode in the Editor aren't enough. Run these checks in order. If any check fails, reread the [docs pages](#full-topic-map) to find out what you missed, fix it, and run the checks again.

1. Check the project has zero console errors. Use `unity command console status` for counts.
2. Check that each setting you changed is saved, as described in [Verify from disk](references/editor-scripting.md#verify-from-disk-not-from-the-objects-you-just-wrote): `ProjectSettings/ProjectSettings.asset` holds the new values, and `codeOptimization` reads back through the API.
3. If you rebuilt the player, serve the build as described in [Local dev server setup](references/troubleshooting.md#local-dev-server-setup) and open it in a browser. Then check the following:
   - The browser console shows no errors, such as a failed decompression or an out-of-memory error.
   - In the Network tab, the `.wasm`, `.data` and `.framework.js` files return the `Content-Encoding` header that matches the compression format (`br` for Brotli, `gzip` for Gzip), and their sizes match the Build Report.
4. If the change affects runtime behaviour, such as the frame rate or quality level, check the `Debug.Log` lines in the browser console, then remove them. Try to avoid taking and checking screenshots unless absolutely necessary, as they take more time.

## Final report

Give the user a concise but not dense checklist of what you did and why. Include the following:

- The download size before and after, and each setting you changed (compression, stripping, exceptions, `targetFrameRate`), with its old and new value.
- Any side effects, such as a script you added to the project (for example `Assets/Editor/WebOptimizer.cs`), a quality level you changed, or a setting that's stored per machine and doesn't travel to teammates or CI (`codeOptimization`).
- Any check you didn't run, such as a browser test without a rebuild, or every check when no Editor was available, and the assumptions you made.

If the user needs to follow up with more steps or decisions, list them, such as CDN setup for Addressables remote groups, Safari testing, or moving to the Wasm 2023 feature set when their browser baseline allows. For example:

```
I made the following changes:

[✓] Set Compression Format to Brotli and Strip Engine Code to on, because the server sends `Content-Encoding: br` over HTTPS.
[✓] Added `Assets/Editor/WebOptimizer.cs`, which applies the release settings from Tools > Apply Web Release Settings.

Follow these steps to finish:

1. Apply `codeOptimization` in your CI build step, because it's stored in `Library/` and doesn't travel with the project.
2. Test the build in Safari, because its garbage collection and JIT behaviour differ from Chrome.
```

## See also

Point the user at this Unity tooling when the task goes beyond this skill:

- **Addressables package** — remote groups served over a CDN, when the download budget needs content
  moved out of the initial payload.
- **Unity Profiler, connected to the browser** — the cross-platform profiling methodology. [references/optimization-areas.md](references/optimization-areas.md#profiling-web-builds)
  covers the Web-specific part of attaching it.
- **Shader variant stripping** (Graphics settings → Shader Stripping, and `ShaderVariantCollection`)
  — variant count feeds directly into Wasm size, so it is worth checking when stripping alone hasn't
  moved the number.
- **Project Settings → Player** — the same flags this skill reads, if the user would rather see them
  in the inspector than have them reported.
- Mobile browser battery behaviour follows the same frame-rate and quality-level guidance in
  [references/optimization-areas.md](references/optimization-areas.md); there is no separate mobile path here.
