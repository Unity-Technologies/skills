---
name: unity-package-management
description: Use when adding, removing, upgrading, or discovering Unity (UPM) packages programmatically from outside the Editor — headless or CI package installs via the C# UnityEditor.PackageManager.Client API, verifying package ids/versions against the Unity registry, or choosing which packages a game needs by genre, platform, and monetization. The Unity CLI does not manage UPM packages, so this skill covers that gap. Triggers on "install a Unity package", "add com.unity.*", "set up packages headless/CI", "which packages for a <genre> game".
allowed-tools:
  - Bash
  - Read
  - Write
  - Edit
---

# Unity Package Management (headless, via the C# Client API)

Add, remove, upgrade, and discover UPM (Unity Package Manager) packages programmatically with
`UnityEditor.PackageManager.Client`, driven headless from the terminal or CI. Do **not**
hand-edit `Packages/manifest.json` — the Client API resolves dependencies and compatible
versions correctly, whereas manual edits routinely break resolution.

This complements the **`unity-cli`** skill (editor install, project creation, build/test): the
CLI has **no** package-management command, so all package work goes through the Editor's C# API.

## When to use

- Add / remove / upgrade one or more packages in an existing or freshly-created project.
- Set up a project's packages non-interactively in CI.
- Verify a package id exists, or find its available versions, before depending on it.
- Decide which packages a game actually needs — see
  [references/select-packages.md](references/select-packages.md).

## Choosing what to install

Install what the project actually needs, not everything; prefer packages the chosen template
already provides (URP templates already include the render pipeline, Input System, etc.). The
genre / look / platform / monetization → package mapping, plus how to search the registry, is
in [references/select-packages.md](references/select-packages.md). Produce a **deduplicated
list of package ids** and read it back to the user before installing.

## The `-quit` problem — why NOT `unity run` for installs

`Client.Add` / `Client.AddAndRemove` are **asynchronous**: they return a `Request` that only
completes on later `EditorApplication.update` ticks (the UPM child process marshals its result
back on the Editor's main-loop pump, so a blocking `while (!req.IsCompleted)` busy-wait
deadlocks it). The Editor must **stay alive** after `-executeMethod` returns, until the request
finishes.

`unity run` **cannot** be used for the installer: it always injects `-quit` (see the reserved
flags in the **`unity-cli`** skill). With `-quit`, the Editor quits the instant the method
returns — before UPM resolves — so packages never install and the callback never runs.

**Solution:** launch the **Editor binary directly** in `-batchmode` **without** `-quit`. The
Editor stays alive, `EditorApplication.update` keeps ticking, the poll callback runs, and it
calls `EditorApplication.Exit(code)` itself when done — which both quits and sets the process
exit code.

## The installer script

Write this to `Assets/Editor/ProjectBootstrap/PackageInstaller.cs`. It must live under an
`Editor/` folder (or an Editor-only assembly) because it uses `UnityEditor`.

```csharp
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    // Installs (and optionally removes) a fixed set of packages via the PackageManager
    // Client API, headless-safe.
    public static class PackageInstaller
    {
        // EDIT this list to match the package selection (see references/select-packages.md).
        static readonly string[] PackagesToAdd =
        {
            "com.unity.inputsystem",
            "com.unity.cinemachine",
            "com.unity.render-pipelines.universal",
            // "com.unity.package@1.2.3"  // pin a version with @ when a minimum is required
        };

        // Optionally drop packages in the same resolution pass (e.g. a template default you don't want).
        static readonly string[] PackagesToRemove = { };

        const double TimeoutSeconds = 600; // UPM resolution + downloads can be slow

        static AddAndRemoveRequest _request;
        static double _deadline;

        // Invoke with: -executeMethod ProjectBootstrap.PackageInstaller.Install  (NO -quit)
        public static void Install()
        {
            if (PackagesToAdd.Length == 0 && PackagesToRemove.Length == 0)
            {
                Debug.Log("[PackageInstaller] Nothing to do.");
                EditorApplication.Exit(0);
                return;
            }

            Debug.Log($"[PackageInstaller] Adding: {string.Join(", ", PackagesToAdd)}");
            _request = Client.AddAndRemove(packagesToAdd: PackagesToAdd, packagesToRemove: PackagesToRemove);
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null) return;

            if (!_request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("[PackageInstaller] Timed out waiting for UPM.");
                    EditorApplication.Exit(2);
                }
                return;
            }

            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
            {
                var names = _request.Result.Select(p => $"{p.name}@{p.version}");
                Debug.Log($"[PackageInstaller] Resolved: {string.Join(", ", names)}");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[PackageInstaller] Failed: {_request.Error?.message}");
                EditorApplication.Exit(1);
            }
        }
    }
}
```

`AddAndRemove` installs the whole set in a single UPM resolution pass — faster and less
error-prone than one `Client.Add` per package.

**Add / remove / upgrade with one script:**
- **Add**: list the id in `PackagesToAdd`.
- **Remove**: list the id in `PackagesToRemove`.
- **Upgrade / pin**: add the id with `@<version>` (e.g. `com.unity.cinemachine@2.9.7`). Without
  a version, resolution picks the latest compatible release.

## Discovering / verifying packages

To confirm an id exists or list its versions before adding it, search the registry. The
in-Editor `Client.SearchAll()` / `Client.Search("<id>")` calls are also async, so they use the
**same poll-and-`Exit` pattern and the same headless run** as the installer. Write
`Assets/Editor/ProjectBootstrap/PackageSearch.cs`:

```csharp
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    public static class PackageSearch
    {
        const double TimeoutSeconds = 120;
        static SearchRequest _request;
        static double _deadline;

        // Invoke with: -executeMethod ProjectBootstrap.PackageSearch.SearchAll  (NO -quit)
        public static void SearchAll()
        {
            _request = Client.SearchAll();                 // or Client.Search("com.unity.cinemachine")
            _deadline = EditorApplication.timeSinceStartup + TimeoutSeconds;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (_request == null) return;
            if (!_request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > _deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("[PackageSearch] Timed out.");
                    EditorApplication.Exit(2);
                }
                return;
            }
            EditorApplication.update -= Poll;

            if (_request.Status == StatusCode.Success)
            {
                foreach (var p in _request.Result.OrderBy(p => p.name))
                    Debug.Log($"[PackageSearch] {p.name}@{p.versions.latestCompatible}  {p.displayName}");
                Debug.Log($"[PackageSearch] {_request.Result.Length} packages found.");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[PackageSearch] Failed: {_request.Error?.message}");
                EditorApplication.Exit(1);
            }
        }
    }
}
```

`_request.Result` is a `PackageInfo[]`; each entry exposes `name`, `displayName`, `description`,
and `versions` (`.latest`, `.latestCompatible`, `.all`). For a terminal-only check without the
Editor (a **known** id, not free-text search), query the registry directly — see
[references/select-packages.md](references/select-packages.md#discovering-and-verifying-packages).

## Run it headless (direct Editor invocation, no `-quit`)

Resolve the Editor binary from the version, then run it in batch mode. The script owns quitting
via `EditorApplication.Exit`, so do **not** pass `-quit`:

```bash
VERSION="<version>"          # e.g. 6000.0.47f1 (or an installed version)
PROJECT="<project-path>"
METHOD="ProjectBootstrap.PackageInstaller.Install"   # or ...PackageSearch.SearchAll

# Install directory of that editor (Hub layout), via the unity CLI
ED=$(unity editors path "$VERSION" --format json | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['path'])")

# Resolve the executable per-OS (handles both "dir containing Unity.app" and the ".app" itself)
case "$(uname)" in
  Darwin) if [ -d "$ED/Unity.app" ]; then UNITY_BIN="$ED/Unity.app/Contents/MacOS/Unity";
          elif [[ "$ED" == *.app ]]; then UNITY_BIN="$ED/Contents/MacOS/Unity";
          else UNITY_BIN="$ED/Unity"; fi ;;
  Linux)  UNITY_BIN="$ED/Unity" ;;
  *)      UNITY_BIN="$ED/Unity.exe" ;;   # Windows (Git Bash / MSYS); use Unity.exe in PowerShell
esac

"$UNITY_BIN" -batchmode -projectPath "$PROJECT" -executeMethod "$METHOD" -logFile -
echo "Exit code: $?"   # 0 = success, 1 = UPM error, 2 = timeout
```

`-logFile -` streams the Editor log (including the `[PackageInstaller]` / `[PackageSearch]`
lines) to stdout so you can watch resolution progress and read any UPM error. If
`unity editors path` output shape differs on your build, get the directory from
`unity editors --installed --format json` instead.

## Verify

```bash
# Every requested id should appear as a dependency
cat "<project-path>/Packages/manifest.json"
```

Confirm the run exited `0` and each package from the list is present in `manifest.json`. If a
package fails to resolve, `_request.Error.message` is logged; read it and check the id/version
against the registry. The Editor's own log (including the `[PackageInstaller]` lines) is the
stdout you streamed with `-logFile -` above — read it there, not via `unity logs` (which shows
the CLI's own log, not the Editor's).

## Import & save headlessly (generate `.meta` files)

After a script or tool writes new `.cs`/asset files, Unity must **import** them so it generates
the `.meta` file each asset needs — and every `.cs`/asset MUST be committed together with its
`.meta`. Merely opening the project once (`unity open "<project-path>"`) imports and generates
them; use this method when you need it **headless** (in a script or CI).

Unlike the package installer, this is **synchronous** — it finishes before returning — so it's
safe to run via `unity run` (its injected `-quit` is harmless; the method also calls
`EditorApplication.Exit` for a clean exit code). Write
`Assets/Editor/ProjectBootstrap/ProjectSaver.cs`:

```csharp
using UnityEditor;
using UnityEngine;

namespace ProjectBootstrap
{
    public static class ProjectSaver
    {
        // Invoke with: -executeMethod ProjectBootstrap.ProjectSaver.SaveAll
        public static void SaveAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSaver] Assets imported and saved.");
            EditorApplication.Exit(0);
        }
    }
}
```

```bash
unity run "<project-path>" --editor-version <version> \
  -- -executeMethod ProjectBootstrap.ProjectSaver.SaveAll
```

## Notes

- These editor scripts are a bootstrap convenience. Leave them in
  `Assets/Editor/ProjectBootstrap/` (they do nothing unless invoked) or delete them after
  setup — your call; mention it to the user.
- All scripts live under `Editor/` because they use `UnityEditor`; they never ship in a build.
- Monetization / backend packages (`com.unity.purchasing`, `com.unity.services.levelplay`, the
  UGS packages) install through this same mechanism, but do the actual **integration** via the
  dedicated skills: **implement-in-app-purchases**, **levelplay-unity-integration**,
  **build-live-game**.
