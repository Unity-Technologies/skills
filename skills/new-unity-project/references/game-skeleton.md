# Scaffolding the game skeleton

Create a minimal but runnable structure matched to the concept from Step 1. Write plain
`.cs`/asset files to disk; Unity generates `.meta` files and imports on the next launch (Step 8).

Keep it small — a skeleton, not a game. Enough to open, press Play, and build on.

## Folder layout

Create these under `Assets/` (Unity creates `.meta` files on import):

```
Assets/
  Editor/
    ProjectBootstrap/  # the bootstrap scripts (PackageInstaller, ProjectSaver, SceneScaffolder)
  Scripts/
    Runtime/           # gameplay code (compiled into the game)
    Editor/            # game-specific editor tooling (custom inspectors, etc.)
  Scenes/
  Prefabs/
  Art/
  Audio/
  UI/
  Settings/
```

The Step-6/8 bootstrap scripts live in `Assets/Editor/ProjectBootstrap/` (see
[package-manager-api.md](package-manager-api.md)); `Assets/Scripts/Editor/` is for game-specific
editor tooling you add later.

## Assembly definition

Put an `.asmdef` at `Assets/Scripts/Runtime/<ProjectName>.Runtime.asmdef` so runtime code compiles
into a named assembly (faster iteration, clean references). Reference `Unity.InputSystem` etc. as
needed by the installed packages.

```json
{
  "name": "<ProjectName>.Runtime",
  "rootNamespace": "<ProjectName>",
  "references": [],
  "autoReferenced": true
}
```

Add package references (e.g. `"Unity.InputSystem"`) to `references` only once the corresponding
package is installed, or leave `references` empty and add them when a script needs them.

## GameManager bootstrap

A single entry point that persists across scenes. `Assets/Scripts/Runtime/GameManager.cs`:

```csharp
using UnityEngine;

namespace <ProjectName>
{
    // Central bootstrap: lives from the first scene, survives scene loads.
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            Debug.Log("<ProjectName> booted.");
            // TODO: initialize systems, load the first gameplay scene, etc.
        }
    }
}
```

## One genre-appropriate gameplay script

Add exactly one starter script that matches the core loop, so Play does something. Examples:

**Platformer / action — `PlayerController.cs`:**

```csharp
using UnityEngine;

namespace <ProjectName>
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float jumpForce = 12f;
        Rigidbody2D _rb;

        void Awake() => _rb = GetComponent<Rigidbody2D>();

        void Update()
        {
            // linearVelocity is the Unity 6 name; on pre-6 LTS use `velocity` instead.
            var x = Input.GetAxisRaw("Horizontal");
            _rb.linearVelocity = new Vector2(x * moveSpeed, _rb.linearVelocity.y);
            if (Input.GetButtonDown("Jump"))
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        }
    }
}
```

> **Input handling:** this sample uses the legacy `UnityEngine.Input` API for brevity. If the
> project installs `com.unity.inputsystem` **and** Active Input Handling is set to "Input System
> Package (New)" only, these `Input.*` calls throw at runtime — set Active Input Handling to
> "Both" (Project Settings > Player), or rewrite the reads using `UnityEngine.InputSystem`
> (e.g. `Keyboard.current`) and reference the `Unity.InputSystem` assembly in the asmdef.

For other genres, scaffold the equivalent single controller:

| Genre | Starter script | Responsibility |
|---|---|---|
| Puzzle / match | `BoardController` | Holds the grid, handles a tap/swap, checks matches |
| Top-down / twin-stick | `PlayerController` | Move on X/Y, aim toward cursor |
| Idle / clicker | `ResourceManager` | Tick a resource on an interval, respond to a click |
| RPG / adventure | `PlayerController` + `DialogueTrigger` | Movement + a stub interaction |
| Racing | `VehicleController` | Throttle/steer a Rigidbody |

Keep each to one clear responsibility. Match namespace and coding style to the rest of the project.

## Initial scene

Create `Assets/Scenes/Main.unity` with a `GameManager` object (carrying the `GameManager`
component) and register it as scene 0 in **Build Settings** so the project can build.

Do it fully headless with a bootstrap method. Write
`Assets/Editor/ProjectBootstrap/SceneScaffolder.cs` (synchronous — run it the same way as
`ProjectSaver`, see [package-manager-api.md](package-manager-api.md)):

```csharp
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectBootstrap
{
    public static class SceneScaffolder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        // Invoke with: -executeMethod ProjectBootstrap.SceneScaffolder.CreateMainScene
        public static void CreateMainScene()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var manager = new GameObject("GameManager");
            // Attach the GameManager MonoBehaviour by type name so this compiles without a ref.
            var type = System.Type.GetType("<ProjectName>.GameManager, <ProjectName>.Runtime");
            if (type != null) manager.AddComponent(type);
            else Debug.LogWarning("[SceneScaffolder] GameManager type not found yet.");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log($"[SceneScaffolder] Created {ScenePath} and registered it as scene 0.");
            EditorApplication.Exit(0);
        }
    }
}
```

Run it **after** the package install and the runtime scripts exist (so `GameManager` is
compiled). Add a `Player` object with the starter controller on first open, or extend this
method. Templates already provide a camera and light via `NewSceneSetup.DefaultGameObjects`.

## Save & meta files

After writing these files, run the project once so Unity imports them and writes `.meta` files
(Step 8). Every `.cs` and asset MUST be committed together with its `.meta`.
