---
name: unity-pipeline
description: Controls a Unity Editor or dev Player through the com.unity.pipeline package to compile, run tests, evaluate C#, method-reload code, enter Play Mode and capture screenshots. Make sure to use this skill when the user asks to drive, test, debug or inspect a running Unity Editor or Player from the command line.
---

# Unity Pipeline (agent control)

Invoke commands with `unity command <group> <name> [args]`. `unity command` lists the command
groups, `unity command <group>` one group's commands, and `--query <term>` filters by name,
description or tag. Two servers exist: **Editor** (`7800-7849`, auto-starts with the editor) and
**Runtime** (`7900-7949`, only in a dev Player build).

## Targeting the Editor

**Always pass `--project-path <project>`, or run `unity` from inside the project folder.** A
project folder holds exactly one Editor, so this is unambiguous, needs no other flag, and reads the
Editor's port and token straight from the project, with no process scan. An MPPM clone is its own
project: `--project-path <mainProject>/Library/VP/<vpId>`.

Don't use `--port`, `--pid` or `--instance` unless the user asks for them. Without
`--project-path` they search every running Editor: on Unity 6 (6000.x) that is a scan of every
process, a second or more on each call; only Unity 7 Editors answer it without a scan. The CLI
warns on stderr when a call had to scan.

## Before you start

- If the package is not installed, install it first ([1. Install & verify](#1-install--verify)).
- Before any other workflow or diagnosis, open and read the [Topic map](#topic-map) pages for your
  task, and name the files you read. Your training knowledge about Unity and this package may be
  out of date, incorrect, or for the wrong version.
- Read only `.md` files in the installed package's `Documentation~` folder, or the live pages for
  its version, as the [Topic map](#topic-map) describes.
- Create a restore point you can roll back to if your changes fail, such as a git commit or stash.
- Do only what's asked. Don't change unrelated assets or files. Avoid long explanations.
- Never leave verification code in the user's script. Never build logging or detection they didn't
  ask for.

## 1. Install & verify

```bash
unity pipeline install            # install into current project (or --project-path)
unity pipeline editors            # list Editor instances and their Pipeline status
unity status --until-ready        # wait until the editor server is ready
unity command editor status       # confirm the editor server answers
```

An Editor that was already open picks up the package only on its next refresh, usually when its
window regains focus. Until then `unity status` reports `STATUS_PIPELINE_LOAD_PENDING`; if that
persists, ask the user to switch to the Unity Editor window, then re-run `unity status --until-ready`.

## 2. Autonomous edit loop (Editor)

This is the core agent workflow: keep the editor alive, change code, recompile, test.

```bash
# 1. Keep the editor ticking when unfocused or minimized; otherwise Unity throttles update and
#    compile. It is on by default, but a session that set it explicitly keeps its own value.
unity command editor set_auto_tick --enabled true

# 2. Edit C# source files on disk normally.

# 3. Recompile (async: triggers a domain reload, then poll until done).
unity command script recompile
unity command script recompile_status    # repeat until "completed" or "up_to_date"
#   Tolerate connection errors while the domain reload is in flight — that is expected.
#   If script recompile_status reports failed=true, read its "errors" array. Fix errors in code
#   you wrote; for errors in the user's code, report them and ask before editing. Tests run on a
#   failed compile use the last assemblies that built, so their results don't cover the change.

# 4. (Optional) List available tests without running them.
unity command test list --mode editor            # mode: all | editor | playmode

# 5. Run tests (filter to keep it fast).
unity command test run --mode editor --filter MyFixture.MyTest
```

`test run` modes: `editor` (default) | `playmode` | `all` (both need `--async-tests true`). `filter-type`:
`testName` | `assembly` | `category`. Use `--async-tests true` and poll `unity command test status` for long
runs and any run that may reload the domain (a reload drops a synchronous run's request; abort with `test cancel`).
A synchronous run returns one record per test, failures included, with its message and stack trace. For an
async run, `unity command test status --detail failures` returns only the failed tests' results.

**Reading exit codes.** They separate "rewrite the invocation" from "the Editor failed", which
is the distinction this loop depends on:

| Exit | Meaning | What to do |
|------|---------|------------|
| `2` | Bad arguments — a misspelled flag, a wrong type, too many positionals. Nothing ran. | Fix the command line and retry. The error names the problem and often suggests the right flag. |
| `6` | The command ran and failed, or no Editor could be reached. | Read the error. If it says no Pipeline instance was found, the Editor isn't reachable: wait with `unity status --until-ready` (or check `unity pipeline editors` for Safe Mode), then retry. Otherwise retrying the same invocation will not help. |

Do not retry an exit 2 unchanged, and do not rewrite a command line on an exit 6.

## 3. Runtime method reload

Change gameplay code in a **running** game with no domain reload. The game must be live: enter
Editor Play Mode (`unity command editor.playmode start`) or run a dev Player with the runtime
Pipeline driver enabled (Project Settings > Pipeline > Runtime). Two backends:

- `script.methodreload reload` loads a compiled assembly: Editor Play Mode and Mono desktop dev
  builds only. The reloaded body may touch only **public** members of its type.
- `script.methodreload reload_editor` and `reload_player` run the edited IL through an
  interpreter, in the Editor or in connected dev Players, **including IL2CPP**. They accept a C#
  subset, and `reload_player` needs the Editor running.

1. **Before entering Play Mode**, tag the method `[MethodReload]` (`using Unity.Pipeline.MethodReload;`,
   assembly `Unity.Pipeline.Attributes`, auto-referenced) and recompile. The reload hook is
   compiled into the method, so tagging it while the game runs has no effect: exit Play Mode,
   recompile, and enter it again. A tagged method is `public`, instance or static, and returns
   `void` or `IEnumerator` (a running coroutine keeps its old body; new `StartCoroutine` calls get
   the new one).
2. With the game running, edit the method body on disk.
3. Apply (re-run to iterate):
   ```bash
   unity command script.methodreload reload --filename Assets/Spinner.cs
   unity command script.methodreload reload_player Assets/Scripts/Player.cs   # device, IL2CPP too
   ```
   `--pdb` on `reload` emits a source-mapped PDB so IDE breakpoints bind (compiles unoptimized).

`unity command script.methodreload status` shows active overrides.

## 4. Bulk construction: `script run` (the builder pattern)

For bulk work — creating many objects, wiring fields, generating content — put the code in a
**versioned project script** and run a named static entry point with `script run`. No domain
reload, no code carried through the protocol; iterating costs an in-memory compile (< ~2s), not
a 15–20s recompile.

```bash
# 1. Write the builder OUTSIDE Assets/ (so the write triggers no asset import / domain reload),
#    e.g. AgentScripts/Build.cs:  public static class Build { public static int All() { ... } }
# 2. Run it — relative paths resolve against the project root (the parent of Assets/):
unity command script run --file AgentScripts/Build.cs --entry Build.All
# 3. Iterate: edit the file, re-run. Useful extras:
unity command script run --file AgentScripts/Build.cs --dry-run   # compile-only check: diagnostics, nothing loaded or executed
unity command script run --file AgentScripts/Build.cs --entry Build.All --args '[3, "Green"]'
```

Rules of thumb:

- **Code goes in files on disk.** Never ship multi-line escaped C# strings through `script eval`:
  write the file, then run it with `script run` (a named entry point, with arguments) or
  `script eval --file` (a script: `using` lines, declarations, `await`; see §5).
- Compiles see the project's **active editor defines** (`UNITY_EDITOR`, version/platform symbols);
  `--defines` appends extra symbols on top.
- Entry points may be `async Task`/`Task<T>` — they are awaited asynchronously (the editor keeps
  pumping, so awaits resuming on Unity's context work naturally) and `Task<T>.Result` is returned.
  The wait is bounded by `--timeout` (seconds, default 60); on expiry the task keeps running detached.
- Runtime exceptions come back with `file:line` mapped to your source (a source-mapped PDB is
  always emitted for executing runs; they compile unoptimized).
- `--mode hotpatch` instead applies `[MethodReload]` in-place method replacements (delegates to
  `script.methodreload reload`); `entry`/`args`/`dry-run` are rejected there and `references`/`defines` don't apply.

## 5. C# eval (Editor or dev Player)

`script eval` compiles C# as a script: `using` lines, top-level methods and types, and `await`. The
value of `return x;` or of a trailing expression is the result. It targets the Editor by default;
add `--runtime <process name>` to run it in a dev Player. Put anything longer than a line or two in
a file.

```bash
unity command script eval "return 2 + 2;"
unity command script eval "return UnityEngine.Time.timeScale;" --runtime MyGame
# A .cs file on disk. Keep it outside Assets/, where Unity would compile it as project source:
unity command script eval --file AgentScripts/Scratch.cs
unity command script eval --file AgentScripts/Scratch.cs --dry-run          # compile only
unity command script eval "return EditorSceneManager.GetActiveScene().name;" --auto-import   # adds missing usings
unity command script eval "return UnityEditor.LogEntries.GetCount();" --internals             # Editor internal APIs
```

- `await Wait.Until(() => condition)` waits without blocking the Editor; `Wait.For(asyncOperation)`
  and `Wait.ForImport(assetPath)` cover operations and imports. Submit a long wait with `--detach`,
  then `unity job wait <job-id>`.
- A failed eval exits 6 with an `errorCode`:

  | `errorCode` | Cause | Resolution |
  |-------------|-------|------------|
  | `COMPILATION_ERROR` | The code doesn't compile; each error has its line and column. | Fix the code at those lines; `--auto-import` adds missing usings. |
  | `RUNTIME_EXCEPTION` | The code threw; the result has the exception and stack trace. | Fix the failing line the stack trace names. |
  | `TIMEOUT` | The code ran past `--timeout`. | Raise `--timeout`, or submit with `--detach` and `unity job wait`. |
  | `INVALID_COMMAND_ARGS` | The arguments are invalid or conflict. | Fix the command line. |

## 6. Authoring scenes, GameObjects, prefabs and assets

The `scene`, `gameobject`, `component`, `prefab` and `asset` groups edit content directly, with no
script. Objects are addressed by a handle (globalId, path, guid, instanceId or hierarchy path), and
each result returns one to chain into the next call. Scene edits are undoable and blocked in Play
Mode. For many objects, use `gameobject create_many` or a `script run` builder (§4).

```bash
unity command scene get_hierarchy                                   # the active scene's objects
unity command gameobject create --name Enemy --primitive cube
unity command component add --target <handle> --type Rigidbody
unity command component set_properties --target <handle> --type Rigidbody --properties '{"m_Mass":2}'
unity command prefab create --source <handle> --path Prefabs/Enemy
unity command scene save
```

## 7. Play Mode and multiplayer

```bash
unity command editor.playmode start    # enter Play Mode (also pause, stop)
```

`editor.playmode start` runs the main Editor's open scene: **one** instance, never Multiplayer Play
Mode (MPPM) virtual players, even if configured.

**MPPM scenarios.** Run a scenario with the scenario commands, never with `editor.playmode start`,
which fights MPPM's play-mode guard:

```bash
unity command editor.playmode list_scenarios
unity command editor.playmode set_active_scenario --path Assets/Settings/PlayMode/2Players.asset  # only while Idle
unity command editor.playmode start_scenario
unity command editor.playmode get_scenario_status     # poll: players launch asynchronously
unity command editor.playmode stop_scenario
```

- They need Unity 6000.3+ and the `com.unity.multiplayer.playmode` package; otherwise they are
  compiled out. Run `unity command editor.playmode` and check `start_scenario` is listed first.
- Each virtual player (clone) is a **separate Unity project** at `<project>/Library/VP/<vpId>` with
  its own server. Wait for its `get_scenario_status` entry to show `reachable: true` (`Launching`
  is slow, `UnexpectedlyStopped` is dead), then address it with that entry's `projectPath`. There
  is no forwarding command: set auto tick and run tests on the clone itself.
  ```bash
  unity command --project-path <mainProject>/Library/VP/<vpId> test run --mode playmode --async-tests true
  unity command --project-path <mainProject>/Library/VP/<vpId> test status   # repeat until done
  ```

**Multiplayer Roles** (client/server stripping, Unity 6000.6+). Nothing is stripped until roles are
enabled with `settings.multiplayer_roles set`. Assign roles with the `multiplayer.role` commands,
not by reflecting over `EditorMultiplayerRolesManager` in `script eval`; run
`unity command multiplayer.role` to check the group is listed.

## 8. Screen capture (Editor)

Capture a view to a PNG to check a visual result.

```bash
unity command capture game_view --output-path Screenshots/hud.png   # what the Game window shows, incl. overlay UI
unity command capture scene_view --max-resolution 512             # the scene only (camera render), inline, longest edge capped
unity command capture editor_window --window Inspector --output-path Screenshots/inspector.png   # any open Editor window
```

Without `--output-path` the image comes back inline (base64). A relative path resolves from the
project root. `editor_window` and `scene_view --source screen` briefly front a background tab and
don't work in batchmode.

## Gotchas

- **A stuck command may mean a modal dialog is open**, not a hang — a dialog blocks the main
  thread until dismissed. If a command runs long, check `unity command editor status` (answers
  instantly even when blocked); `status: "blocked_by_dialog"` means stop retrying and tell the
  human what's blocking (its `dialog.title`/`message`/`buttons`) — it can't be clicked over CLI.
  Some dialogs (`EditorUtility.DisplayDialog`) aren't detected yet: `editor status` itself then
  times out. Treat that as the same case and ask the human to look at the Editor.
- **Player-only commands need a dev Player.** `console write`, `runtime set_time_scale`, `runtime status`, etc. hit
  the Runtime server, which does not run in the Editor.
- **Target a specific instance** when more than one is running: `--project-path <path>` selects an
  Editor, `--runtime <process name>` or `--runtime-path <port file>` selects a dev Player.

## Final checks

1. Check that the project has zero console errors:
   ```bash
   unity command console status              # groundTruth.consoleErrors must be 0, compilationFailed false
   unity command console get --level error   # read any errors it reports
   ```
2. If you fixed or suspect a runtime fault, enter Play Mode once (`unity command editor.playmode start`),
   check the behavior with `unity command script eval`, then stop Play Mode. Add no logging and take
   no screenshots to do this.
3. Otherwise, with zero errors, stop there unless the user asked for proof that the change works.
4. If the fix doesn't work, go back and reread the relevant [Topic map](#topic-map) pages before you
   try again, rather than retrying from memory.

## Final report

End with a short report to the user:

- What you changed, by file, and the commands that verified it (test results, console status).
- Side effects: auto tick left on, Play Mode or a scenario still running, files written outside
  `Assets/` (such as `AgentScripts/` or screenshots), method-reload overrides still active.
- Anything the user must do or decide next, such as a failure you could not fix or a dialog only
  they can dismiss.

## Topic map

The Pipeline package ships its documentation in its `Documentation~` folder: for a registry install
`<project>/Library/PackageCache/com.unity.pipeline@*/Documentation~/`, for a local or embedded
package the folder next to its `package.json`. The same pages are published at
`https://docs.unity3d.com/Packages/com.unity.pipeline@<VERSION>/`. Only read what you need.

Prefer the local copy: it matches the installed package. For the live pages, fetch the exact URL
rather than searching, e.g.
`https://docs.unity3d.com/Packages/com.unity.pipeline@<VERSION>/connectivity.md`, where
`<VERSION>` is the major.minor of `com.unity.pipeline` in `Packages/packages-lock.json`, e.g. `@1.0`.

| Topic | Page |
|-------|------|
| Connecting to an Editor or Player, ports, `--project-path` | `connectivity.md` |
| Dev Player and Runtime server setup | `runtime-setup.md` |
| Method reload (`[MethodReload]`, `script.methodreload`, both backends) | `method-reload.md` |
| `script run`, `script eval`, `script recompile` | `commands/scripts.md` |
| Compiling and running tests | `commands/build-and-compilation.md` |
| `editor status`, `editor set_auto_tick`, modal dialogs | `commands/editor-lifecycle-and-observability.md` |
| Scenes, GameObjects and components, prefabs, assets | `commands/scenes.md`, `commands/gameobjects-and-components.md`, `commands/prefabs.md`, `commands/assets-and-files.md` |
| Play Mode scenarios, virtual players, clones | `commands/multiplayer-play-mode.md` |
| Multiplayer Roles | `commands/multiplayer-roles.md` |
| Waiting on a condition server-side (`job wait`) | `commands/wait.md` |
| Screen capture | `commands/capture.md` |
| `console get` / `console status`, Runtime commands | `commands/runtime.md` |
