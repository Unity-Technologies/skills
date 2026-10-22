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

## Performance Notes
- Take your time to do this thoroughly.
- Quality is more important than speed.

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
3. Set up `eval` as described in [Running C# in the Editor](#running-c-in-the-editor). If no Editor is reachable or `eval` is missing, say so, skip to step 5, then go straight to step 9. Mark build validation and runtime checks as unperformed in your report, and base your recommendations on the user's request and the docs from step 2.
4. Run [0. Pre-Flight](#0-pre-flight) and [1. Assess Current State](#1-assess-current-state). Their results are your baseline.
5. Match the request to a row in [2. Understand Request](#2-understand-request).
6. Create a restore point, then apply the changes from [3. Web Build Optimization Workflow](#3-web-build-optimization-workflow). If a symptom matches [5. Troubleshooting](#5-troubleshooting), follow that entry.
7. Run [4. Validation](#4-validation): rerun the Pre-Flight snippet and compare the values with your baseline.
8. Do the checks in [Final step](#final-step).
9. Give the [Final report](#final-report).

## Running C# in the Editor

Every step below that reads or writes a Player Setting runs inside a live Editor through the Unity
CLI. **The `unity-cli` skill owns getting you there** — installing the CLI, confirming a connected
Editor, adding the project's `com.unity.pipeline` package, telling a genuinely absent Editor apart
from one stuck in Safe Mode, and discovering the Editor's command catalog. Follow it first; don't
re-derive any of it here.

Two things it can't know for you:

- **You need `eval` in particular**, not just a reachable Editor. Confirm it appears in the catalog.
  Its presence depends on the Pipeline package version, not on the CLI, so a healthy install can
  still lack it — if it's missing, say so and skip the steps that read or write settings.
- **You can read Player Settings from `ProjectSettings/ProjectSettings.asset` in a pinch, but do not
  write them that way.** The serialized names don't match the API names, several of these settings
  are per-build-target, and a hand-edited value silently disagrees with what the build actually
  uses. An unreachable Editor is a stop for the write steps.

Run C# with `unity command eval --code '<snippet>'`. `unity command` defaults to a 30 second
timeout.

### Passing C# to `eval`

`eval` compiles a **statement block, not a file**, so write the code without `using` directives and
with fully qualified types. If the code doesn't compile, look up the error here:

| Error | Cause | Fix |
| --- | --- | --- |
| `CS0210` | A `using` directive, such as `using UnityEditor;`, which the compiler reads as a resource-disposal statement | Remove the directive and qualify the types |
| `CS0246` | An unqualified type, such as `PlayerSettings` | Use the full name, such as `UnityEditor.PlayerSettings` |
| `CS0104` | A bare `Object`, which is ambiguous with `object` | Use `UnityEngine.Object` |

### Reading the settings this skill audits

One call returns the whole Pre-Flight picture. Verified against Unity 6000.5.7f1:

```csharp
var target = UnityEditor.Build.NamedBuildTarget.WebGL;
var w = new System.Collections.Generic.List<string>();
w.Add($"activeBuildTarget={UnityEditor.EditorUserBuildSettings.activeBuildTarget}");
w.Add($"compressionFormat={UnityEditor.PlayerSettings.WebGL.compressionFormat}");
w.Add($"decompressionFallback={UnityEditor.PlayerSettings.WebGL.decompressionFallback}");
w.Add($"stripEngineCode={UnityEditor.PlayerSettings.stripEngineCode}");
w.Add($"managedStrippingLevel={UnityEditor.PlayerSettings.GetManagedStrippingLevel(target)}");
w.Add($"il2cppCodeGeneration={UnityEditor.PlayerSettings.GetIl2CppCodeGeneration(target)}");
w.Add($"apiCompatibilityLevel={UnityEditor.PlayerSettings.GetApiCompatibilityLevel(target)}");
w.Add($"exceptionSupport={UnityEditor.PlayerSettings.WebGL.exceptionSupport}");
w.Add($"debugSymbolMode={UnityEditor.PlayerSettings.WebGL.debugSymbolMode}");
w.Add($"dataCaching={UnityEditor.PlayerSettings.WebGL.dataCaching}");
w.Add($"wasm2023={UnityEditor.PlayerSettings.WebGL.wasm2023}");
w.Add($"initialMemorySize={UnityEditor.PlayerSettings.WebGL.initialMemorySize}");
w.Add($"maximumMemorySize={UnityEditor.PlayerSettings.WebGL.maximumMemorySize}");
w.Add($"memoryGrowthMode={UnityEditor.PlayerSettings.WebGL.memoryGrowthMode}");
w.Add($"targetFrameRate={UnityEngine.Application.targetFrameRate}");
w.Add($"vSyncCount={UnityEngine.QualitySettings.vSyncCount}");
return string.Join("\n", w);
```

**Three API names to get right**, because the obvious spellings do not exist and fail to compile:

| Setting | Correct form | Does NOT exist |
|---|---|---|
| Managed stripping level | `PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL)` | `PlayerSettings.managedStrippingLevel` |
| Wasm code optimization | `UnityEditor.WebGL.UserBuildSettings.codeOptimization` | `PlayerSettings.WebGL.codeOptimization`, `PlayerSettings.WebGL.optimizationLevel` |
| IL2CPP code generation | `PlayerSettings.GetIl2CppCodeGeneration(NamedBuildTarget.WebGL)` | a bare property |

`UserBuildSettings` lives in the WebGL build-support module, so it only resolves when that module is
installed. Read it in a separate call from the rest, and treat a resolution failure as "the Web
module isn't installed" rather than as a bad snippet.

**`codeOptimization` is the one setting here that does not live in the project file.** It persists to
`Library/EditorUserBuildSettings.asset`, and `Library/` is gitignored by every standard Unity
`.gitignore`, so this value is per-machine and does not travel: teammates and CI do not inherit it.
Two consequences. Read it back through the API and do not look for it in
`ProjectSettings/ProjectSettings.asset` — it is absent there even on a successful apply, so its
absence is not a failure. And if the release build runs in CI, apply it there as a build step rather
than assuming the repository carries it.

### Applying the settings

Most of the writes in this skill are a single batch, and
[scripts/WebOptimizer.cs](scripts/WebOptimizer.cs) already is that batch. It declares a class
with a `[MenuItem]`, so it is a **project file, not `eval` input** — a class declaration cannot be
flattened into a statement block. Save it under `Assets/Editor/`, let Unity compile, then invoke it
in one line:

```csharp
UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Apply Web Release Settings");
```

Keep its `using` directives; they are correct in a file. For one-off changes — a single quality
level, a frame-rate flip — an inline `eval` statement is fine.

### Verify from disk, not from the objects you just wrote

**Applying a setting and reading it back in the same session proves nothing.** Player Settings are
in-memory objects until something saves them, so every read-back returns the value you just
assigned whether or not it ever reached
`ProjectSettings/ProjectSettings.asset`. A run that skips the save reports success, and when the
Editor session ends the whole change is gone. This was observed, not theorised: a run applied
everything, read back Brotli / High / None, said it was done, and the file on disk never changed.

So after any write:

1. **Save.** `UnityEditor.AssetDatabase.SaveAssets()`. `WebOptimizer.cs` now does this itself; an
   inline `eval` write has to do it explicitly.
2. **Read the values back and report them.** Not "applied successfully" but the actual values, so the
   user can see what is stored. `WebOptimizer.cs` logs all nine.
3. **For anything per-build-target, name the target you read.** Several of these settings exist once
   per target, so a value can be correct for one target and unset for the one being built.

The same trap exists on the file-editing route in reverse: a hand-edited
`ProjectSettings.asset` reads back fine while the running Editor and the build still use the old
value. Verifying after a save and a reimport is what catches both.

**Do not try to reproduce this in batch mode.** A batch Editor invoked with `-quit` saves settings on
exit, so an unsaved write persists anyway and the run looks correct. Both a saving and a non-saving
version of the script pass under `-quit`. The bug only appears in a live Editor session, which is
where it was found. Concluding from a green batch run that the save is unnecessary is the wrong
conclusion from a test that cannot see the defect.

## 0. Pre-Flight

1. **Confirm Web build target:** Read, with the Pre-Flight snippet above, `EditorUserBuildSettings.activeBuildTarget` — must be `WebGL`; if not, warn the user.
2. **Read compression and stripping settings:** Read `compressionFormat`, `decompressionFallback`, `stripEngineCode` and the managed stripping level with the Pre-Flight snippet above. Note the stripping level is `PlayerSettings.GetManagedStrippingLevel(NamedBuildTarget.WebGL)` — there is no `PlayerSettings.managedStrippingLevel` property.
3. **Read exception and optimization settings:** Read `PlayerSettings.WebGL.exceptionSupport` from the Pre-Flight snippet above. For the wasm code optimization level use `UnityEditor.WebGL.UserBuildSettings.codeOptimization` — the `PlayerSettings.WebGL.codeOptimization` and `optimizationLevel` spellings do not exist and will not compile.
4. **Read frame rate settings:** Read, with the Pre-Flight snippet above, `Application.targetFrameRate` and `QualitySettings.vSyncCount`.
5. **Read additional player settings:** Read, with the Pre-Flight snippet above, `PlayerSettings.WebGL.dataCaching`, `PlayerSettings.WebGL.debugSymbolMode`, `PlayerSettings.WebGL.maximumMemorySize`, and `PlayerSettings.GetApiCompatibilityLevel`.
6. Proceed only after you've confirmed the compression, stripping, frame rate, and player settings.

## 1. Assess Current State

1. **Check Build Report:** Instruct the user to open `Window > General > Build Report` after a build and identify the largest asset and code size contributors.
2. **Verify server configuration:** Ask the user to confirm whether the hosting server sends `Content-Encoding: br` (Brotli) or `Content-Encoding: gzip` headers, and whether `Content-Type: application/wasm` is set for `.wasm` files.
3. **Check frame rate config:** Confirm, with the Pre-Flight snippet above, `Application.targetFrameRate` — should be `-1` for Web (let the browser drive).
4. **Check memory settings:** Read, with the Pre-Flight snippet above, `PlayerSettings.WebGL.initialMemorySize` and `PlayerSettings.WebGL.memoryGrowthMode`.
5. Report findings before making recommendations.

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

### IMPORTANT: One-click optimization script

**Always offer to generate this script for the user.** Unity's official web optimization docs provide a single editor menu script that applies all recommended release settings at once. Place in `Assets/Editor/WebOptimizer.cs` — see [scripts/WebOptimizer.cs](scripts/WebOptimizer.cs) for the template.

Adapt the script to the user's project needs (e.g. keep exceptions if they use `try/catch`, switch Brotli to Gzip for HTTP hosting). This script is the single most impactful action for a new web project — it prevents settings from being missed.

### Player Settings audit

Verify and set these values through `eval`:

| Setting | Release recommendation |
|---|---|
| **Compression Format** | **Brotli** (HTTPS hosting); Gzip for HTTP |
| **Decompression Fallback** | **Off** when server is correctly configured |
| **Strip Engine Code** | **On** |
| **Managed Stripping Level** | **High** (release) / Medium (dev) |
| **Code Optimization** | **Disk Size with LTO** (release) / Build Times (dev) |
| **WebAssembly Language Features** | **2023** if browser baseline allows |
| **Enable Exceptions** | **None** (smallest); Explicitly Thrown Only if `try/catch` required |
| **Initial Memory Size** | Tune to peak estimate; too small causes expensive growth |
| **Memory Growth Mode** | **Geometric** |
| **API Compatibility Level** | **.NET Standard 2.1** — smaller than .NET Framework |
| **IL2CPP Code Generation** | **Optimize Size** — smaller Wasm at slight runtime cost |
| **Debug Symbols** | **Off** for release; on for development builds only |
| **Data Caching** | **On** — caches asset data in browser IndexedDB for faster repeat loads |
| **Strip Unused Mesh Components** | **On** — removes unused vertex attributes |
| **Maximum Memory Size** | **2048 MB** default; up to 4096 for complex 3D (Firefox and Chrome < 119 have issues above 2048) |
| **vSyncCount** | 0 (browser handles pacing) |
| **targetFrameRate** | -1 (use `requestAnimationFrame`) |

### Compression and server configuration

| Compression | Use when | Notes |
|---|---|---|
| **Brotli** | HTTPS or localhost | Best ratio; browsers accept only over secure contexts |
| **Gzip** | HTTP delivery, legacy CDNs | Universal |
| **None** | Local dev / file:// | Largest payload; do not ship |

Configure the server to:
- Serve `.br` files with `Content-Encoding: br`.
- Serve `.gz` files with `Content-Encoding: gzip`.
- Set `Content-Type: application/wasm` for `.wasm`, `application/javascript` for `.js`.
- Enable HTTP/2 or HTTP/3 to parallelize chunk fetches.

If the host cannot inject `Content-Encoding`: set **Decompression Fallback = On** as a fallback only — it adds ~150 KB JS and slows startup.

### Exception handling

| Setting | Build size | Use |
|---|---|---|
| **None** | Smallest | Release builds where uncaught exceptions are acceptable |
| **Explicitly Thrown Only** | Modest | Default for projects that catch exceptions |
| **Full** | Largest, slowest | Rarely needed; avoid for release |

Wasm 2023 introduces a cheaper exception model; switching from Explicitly Thrown Only (legacy) to Wasm exceptions reduces both size and cost when browser targets support it.

### Remove unused resources

Three categories to audit for build size reduction:

**1. Unused packages** — Check `Packages/manifest.json` and the Package Manager **In Project** and **Built-in** views. Remove or disable packages the project does not use. The Input System package is a significant size contributor if unused.

**2. Shader stripping** — Configure in `Edit > Project Settings > Graphics`:

| Setting | Recommendation |
|---|---|
| **Lightmap Modes** | Automatic (strips unused lightmap shader variants) |
| **Fog Modes** | Automatic (strips unused fog shader variants) |
| **Instancing Variants** | Strip Unused |
| **Batch Renderer Group Variants** | Strip All (if BRGs are not used) |
| **Always Included Shaders** | Audit and remove any shaders the project does not reference |

Test after stripping — make sure you didn't remove any referenced shaders.

**3. Web Stripping Tool** (`com.unity.web.stripping-tool`) — Analyzes the WebAssembly binary and identifies unused Unity engine submodules (e.g. 3D graphics in a 2D-only game). Install via Package Manager, profile the build, then configure which submodules to exclude. Can yield substantial size reductions beyond what Managed Stripping Level achieves alone.

### Quality settings for Web

Lower quality levels reduce load time and improve runtime performance. Set via `Edit > Project Settings > Quality`:

- Use **Very Low** or **Low** as the default Web quality level.
- Set it with `eval`: `UnityEngine.QualitySettings.SetQualityLevel(0, true);` where 0 = Very Low.
- Consider creating a Web-specific quality level that disables features unnecessary in-browser (real-time shadows, post-processing effects, high particle counts).

### Frame rate on Web

- Set it with `eval`: `UnityEngine.Application.targetFrameRate = -1;` — let the browser use `requestAnimationFrame`.
- Note: **Safari caps at 60 fps** in WebGL; high-refresh targets do not apply.
- Use `OnDemandRendering.renderFrameInterval` to drop to 5–10 fps on static/idle screens to save battery.

### KTX / Basis Universal textures

KTX2 with Basis Universal supercompression ships a single texture file that transcodes at load time to the optimal GPU format for the browser's device (BC7 on desktop, ASTC on mobile, ETC2 on older Android). This avoids shipping separate texture variants for each GPU family — critical for Web where the target hardware is unknown.

| Topic | Guidance |
|---|---|
| **Package** | Install `com.unity.cloud.ktx` (KtxUnity) via Package Manager |
| **When to use** | Runtime-loaded textures via Addressables or asset bundles served to unknown GPU targets |
| **When NOT to use** | Textures baked into the player build — Unity already selects the correct format at build time |
| **Supercompression** | Use **ETC1S** for smallest size (lossy, good for diffuse/albedo); **UASTC** for higher quality (near-lossless, better for normals/UI) |
| **Encoding** | Encode offline with `toktx` or `basisu` CLI; do not encode at runtime |
| **Linear data** | Set `--assign_oetf linear` when encoding normal maps, masks, or data textures to avoid incorrect sRGB conversion |
| **Mip maps** | Generate mips at encode time (`--genmipmap`) — browser-side mip generation is expensive |
| **Loading** | Use `KtxTexture.LoadFromStreamingAssets` or load bytes via UnityWebRequest and call `KtxTexture.LoadFromBytes` |
| **Memory** | Transcoded textures are standard GPU textures; memory cost equals the target format, not the KTX2 file size |
| **Orientation** | Always include `--lower_left_maps_to_s0t0` to match Unity's UV convention |

**`toktx` CLI examples:** See [scripts/toktx-examples.sh](scripts/toktx-examples.sh) for commands covering albedo (ETC1S), normals/detail (UASTC), ICC profile errors, and linear data.

### Streaming on Web

- Use Addressables with **remote groups** hosted on a CDN with Brotli / Gzip.
- Avoid bundling the entire game into the initial download; stream levels on demand.
- Target < 30 MB initial download for "instant play"; level data follows.
- For streamed textures targeting mixed GPU hardware, prefer KTX2 bundles over per-platform variants — one bundle serves all browsers.

### Profiling Web builds

| Tool | Use | Notes |
|---|---|---|
| **Chrome DevTools > Performance** | CPU flamegraph; main-thread analysis | Default first stop for WebGL hitches; inspect Wasm call stacks |
| **Chrome DevTools > Memory** | Heap snapshot; allocation timeline | Find JS/Wasm memory leaks; compare snapshots before/after scene load |
| **Firefox Profiler** | Cross-platform; shareable URLs; native + Wasm view | Better Wasm symbolication than Chrome in some cases; shareable profile URLs for team review |
| **Safari Web Inspector** | iOS Safari and macOS Safari debugging | Required for Safari-specific issues; WebGL/Wasm runtime differs from Chromium |
| **Unity Profiler over WebSocket** | Connect to a development build; standard markers | Use for Unity-side markers (GC, rendering, scripts); does not capture browser-side overhead |

**Symptom → tool quick reference:**

| Symptom | First-line tool | Second-line tool |
|---|---|---|
| WebGL hitch / stutter | Chrome DevTools > Performance | Firefox Profiler |
| Memory climbing over time | Chrome DevTools > Memory | Unity Memory Profiler (WebSocket) |
| Slow initial load | Chrome DevTools > Network | Build Report Inspector |
| Safari-only rendering issue | Safari Web Inspector | Compare with Chrome DevTools |

**Embedding profiling symbols** — browser profilers show mangled Wasm function names by default. To get readable C# method names in Chrome/Firefox flamegraphs, either enable `Player Settings > Publishing > Debug Symbols` for dev builds, or add a build processor:

```csharp
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class WebProfilingBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        PlayerSettings.SetAdditionalIl2CppArgs("--compiler-flags=--profiling-funcs");
    }
}
```

**Emscripten built-in profilers** — enable one at a time via `PlayerSettings.WebGL.emscriptenArgs`:

| Flag | What it shows |
|---|---|
| `--cpuprofiler` | CPU profiler overlay in browser |
| `--memoryprofiler` | Visual memory map (white=allocated unused, pink=stack, blue=dynamic, green=fragmented) |
| `--threadprofiler` | Thread activity profiler |

**GPU debugging** — No Frame Debugger support on Web. Use [Spector.js](https://spector.babylonjs.com/) as a browser-based alternative — it captures draw calls and WebGL state.

**Firefox `about:memory`** — type `about:memory` as a URL in Firefox, click Measure to see per-tab breakdown: WASM code size, WASM heap, .data file, web audio. Watch for WASM heap > 300 MB (crash risk, especially on iOS Safari).

Editor Play Mode does not represent browser runtime; always measure in browser. Chrome and Safari GC and JIT behavior differ — test both.

### Web memory directives

- Disable **Read/Write Enabled** on textures and meshes — it duplicates data into the WASM heap.
- Reduce `.data` file size by moving assets to Addressables or AssetBundles.
- Use compressed texture formats (KTX2/Basis) to reduce both download and decoded memory cost.

### iOS Safari memory limits

- **iOS < 18:** WebContent process limit ~1.5 GB. WASM memory (Gigacage) capped at 2 GB. Typed arrays share this pool. On iPhone X (iOS 16) heap growth caps at ~512 MB, but setting Initial Memory Size to 512 MB–1.5 GB upfront works.
- **iOS 18+:** Limits largely lifted; iPhone 11 can allocate ~4 GB.
- On iOS, set **Initial Memory Size** to the target peak rather than relying on growth — Safari handles large upfront allocations better than incremental growth.
- WASM heap > 300 MB risks crashes on older iOS; target < 200 MB for broad compatibility.

### Video and audio on Web

- **Video:** Playback only works from a URL (server with CORS enabled) or from StreamingAssets. On iOS the server must support HTTP range requests for streaming. Use browser-compatible formats (MP4/H.264).
- **Audio:** AudioEffects (mixer effects) require compute shaders — **not available on WebGL**. Mixers and MixerGroups work for volume control only. Set audio to **Mono** to improve loading. If `about:memory` shows web audio > 100 MB, audio is likely uncompressed — switch to Vorbis.

### Canvas and DPI

If the canvas is scaled up it takes the new resolution. Use `devicePixelRatio` in the web template to offset DPI scaling and avoid rendering at unnecessarily high resolution.

## 4. Validation

1. Re-read the Player Settings with the Pre-Flight snippet (compression, stripping, exceptions, targetFrameRate).
2. Rebuild the player and compare Build Report file sizes with baseline.
3. Verify in at least Chrome and Safari (GC and JIT behavior differ).
4. Max **3 iterations** before asking the user for feedback.

## 5. Troubleshooting

### Build still large after enabling Strip Engine Code

1. Is **Managed Stripping Level** set to Medium or Low? → Set to High for release.
2. Are plug-ins using reflection to access engine modules that would otherwise be stripped? → Add a `link.xml` to preserve needed symbols.
3. Is **Exceptions** set to Full? → Full adds the largest code overhead; switch to None or Explicitly Thrown Only.

### Brotli not working — Decompression Fallback required

1. Is the server sending `Content-Encoding: br`? → Without this header the browser won't decompress; the fallback JS decompressor is then needed.
2. Is the build hosted over HTTP (not HTTPS)? → Brotli requires a secure context; degrade to Gzip for HTTP hosting.

### Stutter in Safari but not Chrome

1. Does the project set `Application.targetFrameRate = 60`? → On Safari WebGL this conflicts with browser pacing; set to `-1`.
2. Are there shaders that behave differently on Safari's WebGL implementation? → Test on device; Safari's WebGL/Wasm runtime differs from Chromium — some GLSL constructs are handled differently.

### Memory growth slow path triggered

1. Is **Initial Memory Size** too small for the project's peak? → Wasm memory growth requires a full buffer copy; set Initial Memory Size to a realistic peak estimate.
2. Is **Memory Growth Mode** set to Linear? → Switch to **Geometric** for saner growth curve.

### Frame rate set to 60 but browser runs erratically

1. Is `Application.targetFrameRate = 60` set in code? → On Web this conflicts with `requestAnimationFrame` browser pacing. Set to `-1`.
2. Is `vSyncCount` non-zero? → Set to 0; the browser handles pacing.

### Firefox cache rejecting large files

Firefox limits individual cache entries via `browser.cache.disk.max_entry_size`. If the build exceeds this (default ~50 MB), assets won't cache. Solution: use Addressables to split into bundles < 51 MB, or instruct users to increase the setting in `about:config`.

### Local dev server setup

For testing builds locally with proper MIME types:

```bash
# Python (HTTP)
python -m http.server 55553 -d path/to/build

# Node.js (install serve-handler)
npx serve path/to/build -l 3001
```

For Brotli testing, use HTTPS — Brotli requires a secure context. Generate a self-signed cert with OpenSSL for local testing.

## Final step

Web settings only take effect in a browser build, so a clean compile and Play mode in the Editor aren't enough. Run these checks in order. If any check fails, reread the [docs pages](#full-topic-map) to find out what you missed, fix it, and run the checks again.

1. Check the project has zero console errors.
2. Check that each setting you changed is saved, as described in [Verify from disk](#verify-from-disk-not-from-the-objects-you-just-wrote): `ProjectSettings/ProjectSettings.asset` holds the new values, and `codeOptimization` reads back through the API.
3. If you rebuilt the player, serve the build as described in [Local dev server setup](#local-dev-server-setup) and open it in a browser. Then check the following:
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
- **Unity Profiler, connected to the browser** — the cross-platform profiling methodology. Section 3
  covers the Web-specific part of attaching it.
- **Shader variant stripping** (Graphics settings → Shader Stripping, and `ShaderVariantCollection`)
  — variant count feeds directly into Wasm size, so it is worth checking when stripping alone hasn't
  moved the number.
- **Project Settings → Player** — the same flags this skill reads, if the user would rather see them
  in the inspector than have them reported.
- Mobile browser battery behaviour follows the same frame-rate and quality-level guidance in
  Sections 3 and 4; there is no separate mobile path here.
