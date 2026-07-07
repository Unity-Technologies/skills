# Selecting packages from the project brief

Turn the Step 1 concept and Step 2 platforms/monetization into a concrete package list, then
install it via the C# PackageManager Client API (see
[package-manager-api.md](package-manager-api.md)).

**Principle:** install what the concept actually needs, not everything. A hyper-casual 2D
prototype needs far less than a 3D multiplayer RPG. Prefer packages already provided by the
chosen template (URP templates already include the render pipeline, Input System, etc.) — only
add what's missing. Don't pin exact versions unless a skill requires a minimum; `Client.Add`
without a version resolves the latest compatible release.

## Foundation (almost every project)

| Need | Package | Notes |
|---|---|---|
| Modern input | `com.unity.inputsystem` | Preferred over the legacy Input Manager. |
| Text / UI | `com.unity.ugui` | uGUI + TextMeshPro (bundled). UI Toolkit ships with the Editor. |
| Camera framing | `com.unity.cinemachine` | Great for almost any 3D and many 2D games. |
| Testing | `com.unity.test-framework` | Enables `unity test`; usually already present. |
| Large/streamed assets | `com.unity.addressables` | Add when the game has many assets or needs content updates. |

## Render pipeline (pick one; usually set by the template)

| Choice | Package | Use when |
|---|---|---|
| **URP** (Universal) | `com.unity.render-pipelines.universal` | Default for most 2D/3D, mobile, and WebGL. Broadest platform reach. |
| **HDRP** (High-Definition) | `com.unity.render-pipelines.high-definition` | High-fidelity PC/console only. Not for mobile/WebGL. |
| **Built-in** | (none) | Simplest/legacy; fine for tiny prototypes. |

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
| Platformer / action | URP, Input System, Cinemachine, 2D feature (if 2D), AI Navigation (if 3D) |
| Puzzle / match / card | URP or 2D feature, Input System, uGUI/TextMeshPro, Timeline (juice) |
| Top-down / twin-stick | URP, Input System, Cinemachine, AI Navigation |
| RPG / adventure | URP, Input System, Cinemachine, AI Navigation, Addressables, Timeline |
| Racing / physics | URP, Input System, Cinemachine; Physics is built in |
| Idle / hyper-casual | 2D feature or URP, Input System, uGUI/TextMeshPro (keep it lean) |
| Multiplayer (any) | `com.unity.netcode.gameobjects` + Multiplayer Services → see **build-live-game** |

## By target platform (Step 2)

Platform support is mostly Editor **modules** (installed in Step 4), not packages. Package-wise:

| Platform | Consider |
|---|---|
| Mobile (iOS/Android) | Keep dependencies lean; URP over HDRP; Addressables for download size; monetization below |
| WebGL | URP (not HDRP); small footprint; avoid heavy packages |
| Desktop / Console | URP or HDRP depending on fidelity target |

## By monetization (Step 2) — install now, integrate via the dedicated skill

| Goal | Package | Integration skill |
|---|---|---|
| In-app purchases | `com.unity.purchasing` | **implement-in-app-purchases** |
| Ads / mediation | `com.unity.services.levelplay` | **levelplay-unity-integration** |
| Accounts, cloud save, economy, remote config, leaderboards, analytics | see the UGS package table | **build-live-game** |

Install the package(s) here so the manifest is complete, but do the actual wiring by invoking
the matching skill after the skeleton exists (Step 7). For the full UGS package/version matrix
(`com.unity.services.core`, `authentication`, `cloudsave`, `cloudcode`, `economy`,
`remote-config`, `analytics`, etc.), read the **build-live-game** skill.

## Output

Produce a deduplicated list of package IDs and pass it to the installer script in
[package-manager-api.md](package-manager-api.md). Read it back to the user before installing.
