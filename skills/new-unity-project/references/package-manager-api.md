# Installing packages with the C# PackageManager Client API

Install packages programmatically with `UnityEditor.PackageManager.Client` and run the script
headless. Do **not** hand-edit `Packages/manifest.json`.

## The `-quit` problem — why NOT `unity run` for the installer

`Client.Add` / `Client.AddAndRemove` are **asynchronous**: they return a `Request` that only
completes on later `EditorApplication.update` ticks (the UPM child process marshals its result
back on the Editor's main-loop pump — a blocking `while(!req.IsCompleted)` busy-wait deadlocks
it). So the Editor must **stay alive** after `-executeMethod` returns, until the request finishes.

`unity run` **cannot** be used here: it always injects `-quit` (see the `unity-cli` skill —
"Reserved flags … The command adds them itself: `-batchmode`, `-quit`, …"). With `-quit`, the
Editor quits the instant `Install()` returns, before UPM resolves — packages never install and
the callback never runs.

**Solution:** for the async installer, launch the **Editor binary directly** in `-batchmode`
**without** `-quit`. The Editor stays alive, `EditorApplication.update` keeps ticking, the poll
callback runs, and it calls `EditorApplication.Exit(code)` itself when done — which both quits
and sets the process exit code.

(Purely *synchronous* bootstrap methods — the savers below — finish before returning, so those
*can* use `unity run`.)

## The installer script

Write this to `Assets/Editor/ProjectBootstrap/PackageInstaller.cs`. It must live under an
`Editor/` folder (or an Editor-only assembly) because it uses `UnityEditor`.

```csharp
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProjectBootstrap
{
    // Installs a fixed set of packages via the PackageManager Client API, headless-safe.
    public static class PackageInstaller
    {
        // EDIT this list to match the package selection (see select-packages.md).
        static readonly string[] PackagesToAdd =
        {
            "com.unity.inputsystem",
            "com.unity.cinemachine",
            "com.unity.render-pipelines.universal",
            // "com.unity.package@1.2.3"  // pin a version with @ when a skill requires a minimum
        };

        const double TimeoutSeconds = 600; // UPM resolution + downloads can be slow

        static AddAndRemoveRequest _request;
        static double _deadline;

        // Invoke with: -executeMethod ProjectBootstrap.PackageInstaller.Install  (NO -quit)
        public static void Install()
        {
            if (PackagesToAdd == null || PackagesToAdd.Length == 0)
            {
                Debug.Log("[PackageInstaller] Nothing to install.");
                EditorApplication.Exit(0);
                return;
            }

            Debug.Log($"[PackageInstaller] Adding: {string.Join(", ", PackagesToAdd)}");
            _request = Client.AddAndRemove(packagesToAdd: PackagesToAdd);
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
                Debug.Log($"[PackageInstaller] Installed: {string.Join(", ", names)}");
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

`AddAndRemove` installs the whole set in a single UPM resolution pass, which is faster and less
error-prone than one `Client.Add` per package. To also *remove* packages (e.g. drop a template's
unwanted default), pass `packagesToRemove:`.

## Run it headless (direct Editor invocation, no `-quit`)

Resolve the Editor binary from the version, then run it in batch mode. The script owns quitting
via `EditorApplication.Exit`, so do **not** pass `-quit`:

```bash
VERSION="<version>"          # e.g. 6000.0.47f1
PROJECT="<project-path>"

# Install directory of that editor (Hub layout)
ED=$(unity editors path "$VERSION" --format json | python3 -c "import sys,json;print(json.load(sys.stdin)['data']['path'])")

# Resolve the executable per-OS (handles both "dir containing Unity.app" and the ".app" itself)
case "$(uname)" in
  Darwin) if [ -d "$ED/Unity.app" ]; then UNITY_BIN="$ED/Unity.app/Contents/MacOS/Unity";
          elif [[ "$ED" == *.app ]]; then UNITY_BIN="$ED/Contents/MacOS/Unity";
          else UNITY_BIN="$ED/Unity"; fi ;;
  Linux)  UNITY_BIN="$ED/Unity" ;;
  *)      UNITY_BIN="$ED/Unity.exe" ;;   # Windows (Git Bash / MSYS); use Unity.exe in PowerShell
esac

"$UNITY_BIN" -batchmode -projectPath "$PROJECT" \
  -executeMethod ProjectBootstrap.PackageInstaller.Install -logFile -
echo "Exit code: $?"   # 0 = success, 1 = UPM error, 2 = timeout
```

`-logFile -` streams the Editor log (including the `[PackageInstaller]` lines) to stdout so you
can watch resolution progress and read any UPM error. If `unity editors path` output shape
differs on your build, get the directory from `unity editors --installed --format json` instead.

## Verify

```bash
# Every requested id should appear as a dependency
cat "<project-path>/Packages/manifest.json"
```

Confirm the run exited `0` and each package from the list is present in `manifest.json`.

## The synchronous bootstrap methods (Steps 7–8)

These finish before returning, so they're safe to run via `unity run` (its injected `-quit` is
harmless — they also call `EditorApplication.Exit` for a clean exit code). Write them to
`Assets/Editor/ProjectBootstrap/`.

**Import + save the AssetDatabase (Step 8)** — generates `.meta` files before committing.
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

Merely opening the project once (`unity open "<project-path>"`) also imports and generates
`.meta` files — use `SaveAll` when you want it headless in a script or CI. A `SceneScaffolder`
method (see [game-skeleton.md](game-skeleton.md)) is also synchronous and runs the same way.

## Notes

- These editor scripts are a bootstrap convenience. Leave them in `Assets/Editor/ProjectBootstrap/`
  (they do nothing unless invoked) or delete them after setup — your call; mention it to the user.
- If a package fails to resolve, `_request.Error.message` is logged; read it and check the
  package id/version against the Unity registry. `unity logs --level error` surfaces Editor logs.
