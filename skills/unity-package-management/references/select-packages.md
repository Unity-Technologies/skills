# Selecting packages

Turn a game concept — genre, look, target platforms, monetization — into a concrete package
list, then install it via the C# PackageManager Client API (see the main `SKILL.md`).

**Principle:** install what the concept actually needs, not everything. A hyper-casual 2D
prototype needs far less than a 3D multiplayer RPG. Prefer packages already provided by the
chosen template (the URP templates `com.unity.template.urp-blank` / `universal-2d` already
include the render pipeline, Input System, uGUI/TextMeshPro, etc.) — only add what's missing.
Don't pin exact versions unless a minimum is required; `Client.Add` without a version resolves
the latest compatible release.

The tables below are a starting point, not the whole registry. **Search the registry** to
discover packages beyond this list, confirm an id exists, or check available versions before
installing — see [Discovering and verifying packages](#discovering-and-verifying-packages).

## Discovering and verifying packages

Two ways to search, depending on whether the Editor is involved:

**In-Editor — the PackageManager Client API (preferred).** `Client.SearchAll()` returns every
package available in the project's configured registries (the Unity registry plus any scoped
registries), each with all its versions and metadata — this is what the Package Manager
window's search filters over. `Client.Search("<id>")` inspects a single package. Use the
ready-to-run `PackageSearch` script in the main `SKILL.md` to discover candidates and verify
ids/versions before building the install list.

**Terminal — query the npm-compatible registry directly** (no Editor needed) to confirm a
**known** id exists and list its versions:

```bash
# Full metadata for one package: versions{}, dist-tags.latest, description, dependencies.
# -f makes curl fail (non-zero) on HTTP errors — e.g. a 404 for a bad id — instead of piping
# an error page into python; -L follows redirects.
curl -fsSL https://packages.unity.com/com.unity.cinemachine | python3 -m json.tool | head -40

# Just the latest published version
curl -fsSL https://packages.unity.com/com.unity.cinemachine \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['dist-tags']['latest'])"
```

Note: the registry supports fetching a **known** package id, but **not** free-text search over
HTTP (the npm `-/v1/search` endpoint is not available — it 404s). For keyword discovery, use
`Client.SearchAll()` in-Editor, the Package Manager window, or the
[Unity package documentation](https://docs.unity3d.com/Manual/pack-keys.html).

## Foundation (almost every project)

| Need | Package | Notes |
|---|---|---|
| Modern input | `com.unity.inputsystem` | Preferred over the legacy Input Manager. |
| Text / UI | `com.unity.ugui` | uGUI + TextMeshPro (bundled). UI Toolkit ships with the Editor. Use one of these for HUDs and menus — never `OnGUI`. |
| Camera framing | `com.unity.cinemachine` | Great for almost any 3D and many 2D games. |
| Testing | `com.unity.test-framework` | Enables `unity test`; usually already present. |
| Large/streamed assets | `com.unity.addressables` | Add when the game has many assets or needs content updates. |

## Render pipeline (set by the template — do not add or swap it here)

The pipeline is decided by the template chosen in the **`unity-cli`** bootstrap step (default:
`com.unity.template.urp-blank` for 3D, `com.unity.template.universal-2d` for 2D — both URP).
Installing `com.unity.render-pipelines.universal` into a Built-in template project does **not**
switch pipelines: no URP asset gets assigned, and materials render pink. To change pipeline after
creation use the **`migrate-birp-to-urp`** skill instead. The rows below are for reading a
`manifest.json`, not for adding packages:

| Choice | Package | Notes |
|---|---|---|
| **URP** (Universal) | `com.unity.render-pipelines.universal` | Default for all 2D/3D, mobile, and WebGL. Ships in the URP templates. |
| **HDRP** (High-Definition) | `com.unity.render-pipelines.high-definition` | High-fidelity PC/console only; `com.unity.template.hdrp-blank`. Not for mobile/WebGL. |
| **Built-in** | (none) | Legacy. Its templates are deprecated from Unity 6.5 and removed in 6.7; use only when the user explicitly asks. |

## By dimension & look

| Look | Packages |
|---|---|
| **2D** (any) | `com.unity.2d.feature` (sprites, tilemap, animation, pixel-perfect bundle) |
| **2D pixel-perfect** | `com.unity.2d.pixel-perfect` (included in the 2D feature set) |
| **3D navigation** | `com.unity.ai.navigation` (NavMesh for AI/pathfinding) |
| **Cutscenes / sequencing** | `com.unity.timeline` |
| **No-code logic** | `com.unity.visualscripting` |

## By genre (starting points, combine with the above)

| Genre | Typical additions |
|---|---|
| Platformer / action | Input System, Cinemachine, 2D feature (if 2D), AI Navigation (if 3D) |
| Puzzle / match / card | Input System, uGUI/TextMeshPro, 2D feature (if 2D), Timeline (juice) |
| Top-down / twin-stick | Input System, Cinemachine, 2D feature (if 2D), AI Navigation (if 3D) |
| RPG / adventure | Input System, Cinemachine, AI Navigation, Addressables, Timeline |
| Racing / physics | Input System, Cinemachine; Physics is built in |
| Idle / hyper-casual | Input System, uGUI/TextMeshPro, 2D feature (if 2D) — keep it lean |
| Multiplayer (any) | `com.unity.netcode.gameobjects` + Multiplayer Services → see **build-live-game** |

## By target platform

Platform support is mostly Editor **modules** (installed with `unity install --module …`, see
the **`unity-cli`** skill), not packages. Package-wise:

| Platform | Consider |
|---|---|
| Mobile (iOS/Android) | Keep dependencies lean; a URP template, never HDRP; Addressables for download size; monetization below |
| WebGL | A URP template (not HDRP); small footprint; avoid heavy packages |
| Desktop / Console | URP template by default; `hdrp-blank` only for a high-fidelity target |

## By monetization — install now, integrate via the dedicated skill

| Goal | Package | Integration skill |
|---|---|---|
| In-app purchases | `com.unity.purchasing` | **implement-in-app-purchases** |
| Ads / mediation | `com.unity.services.levelplay` | **levelplay-unity-integration** |
| Accounts, cloud save, economy, remote config, leaderboards, analytics | see the UGS package table | **build-live-game** |

Install the package(s) here so the manifest is complete, but do the actual wiring by invoking
the matching skill. For the full UGS package/version matrix (`com.unity.services.core`,
`authentication`, `cloudsave`, `cloudcode`, `economy`, `remote-config`, `analytics`, etc.),
read the **build-live-game** skill.
