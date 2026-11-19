# Web build optimization areas

Release recommendations for each area of a Unity Web build. Read the matching docs page from the skill's topic map before you apply a value.

Links in this file resolve to the skill directory's `references/` folder, the folder this file is in, so `../scripts/` is the skill's `scripts/` folder. Project paths in code spans are relative to the user's project.

## Contents

- [Player Settings audit](#player-settings-audit)
- [Compression and server configuration](#compression-and-server-configuration)
- [Exception handling](#exception-handling)
- [Remove unused resources](#remove-unused-resources)
- [Quality settings for Web](#quality-settings-for-web)
- [Frame rate on Web](#frame-rate-on-web)
- [KTX / Basis Universal textures](#ktx--basis-universal-textures)
- [Streaming on Web](#streaming-on-web)
- [Profiling Web builds](#profiling-web-builds)
- [Web memory directives](#web-memory-directives)
- [iOS Safari memory limits](#ios-safari-memory-limits)
- [Video and audio on Web](#video-and-audio-on-web)
- [Canvas and DPI](#canvas-and-dpi)

## Player Settings audit

Verify and set these values through `script eval`:

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

## Compression and server configuration

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

## Exception handling

| Setting | Build size | Use |
|---|---|---|
| **None** | Smallest | Release builds where uncaught exceptions are acceptable |
| **Explicitly Thrown Only** | Modest | Default for projects that catch exceptions |
| **Full** | Largest, slowest | Rarely needed; avoid for release |

Wasm 2023 introduces a cheaper exception model; switching from Explicitly Thrown Only (legacy) to Wasm exceptions reduces both size and cost when browser targets support it.

## Remove unused resources

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

## Quality settings for Web

Lower quality levels reduce load time and improve runtime performance. Set via `Edit > Project Settings > Quality`:

- Use **Very Low** or **Low** as the default Web quality level.
- Set it with `script eval`: `UnityEngine.QualitySettings.SetQualityLevel(0, true);` where 0 = Very Low.
- Consider creating a Web-specific quality level that disables features unnecessary in-browser (real-time shadows, post-processing effects, high particle counts).

## Frame rate on Web

- Set it with `script eval`: `UnityEngine.Application.targetFrameRate = -1;` — let the browser use `requestAnimationFrame`.
- Note: **Safari caps at 60 fps** in WebGL; high-refresh targets do not apply.
- Use `OnDemandRendering.renderFrameInterval` to drop to 5–10 fps on static/idle screens to save battery.

## KTX / Basis Universal textures

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

**`toktx` CLI examples:** See [scripts/toktx-examples.sh](../scripts/toktx-examples.sh) for commands covering albedo (ETC1S), normals/detail (UASTC), ICC profile errors, and linear data.

## Streaming on Web

- Use Addressables with **remote groups** hosted on a CDN with Brotli / Gzip.
- Avoid bundling the entire game into the initial download; stream levels on demand.
- Target < 30 MB initial download for "instant play"; level data follows.
- For streamed textures targeting mixed GPU hardware, prefer KTX2 bundles over per-platform variants — one bundle serves all browsers.

## Profiling Web builds

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

## Web memory directives

- Disable **Read/Write Enabled** on textures and meshes — it duplicates data into the WASM heap.
- Reduce `.data` file size by moving assets to Addressables or AssetBundles.
- Use compressed texture formats (KTX2/Basis) to reduce both download and decoded memory cost.

## iOS Safari memory limits

- **iOS < 18:** WebContent process limit ~1.5 GB. WASM memory (Gigacage) capped at 2 GB. Typed arrays share this pool. On iPhone X (iOS 16) heap growth caps at ~512 MB, but setting Initial Memory Size to 512 MB–1.5 GB upfront works.
- **iOS 18+:** Limits largely lifted; iPhone 11 can allocate ~4 GB.
- On iOS, set **Initial Memory Size** to the target peak rather than relying on growth — Safari handles large upfront allocations better than incremental growth.
- WASM heap > 300 MB risks crashes on older iOS; target < 200 MB for broad compatibility.

## Video and audio on Web

- **Video:** Playback only works from a URL (server with CORS enabled) or from StreamingAssets. On iOS the server must support HTTP range requests for streaming. Use browser-compatible formats (MP4/H.264).
- **Audio:** AudioEffects (mixer effects) require compute shaders — **not available on WebGL**. Mixers and MixerGroups work for volume control only. Set audio to **Mono** to improve loading. If `about:memory` shows web audio > 100 MB, audio is likely uncompressed — switch to Vorbis.

## Canvas and DPI

If the canvas is scaled up it takes the new resolution. Use `devicePixelRatio` in the web template to offset DPI scaling and avoid rendering at unnecessarily high resolution.
