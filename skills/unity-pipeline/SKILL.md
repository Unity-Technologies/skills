---
name: unity-pipeline
description: Control a Unity Editor or Player via the unity-pipeline package. Compile, run tests, eval, method-reload code, enter Play Mode, capture screenshots.
---

# Unity Pipeline (agent control)

Invoke commands with `unity command <group> <name> [args]`. `unity command` lists the command
groups, `unity command <group>` one group's commands, and `--query <term>` filters by name,
description or tag. Two servers exist: **Editor** (`7800-7849`, auto-starts with the editor) and
**Runtime** (`7900-7949`, only in a dev Player build).

## 1. Install & verify

```bash
unity pipeline install            # install into current project (or --project-path)
unity pipeline list               # confirm the editor instance + server are reachable
unity status --until-ready        # wait until the editor server is ready
unity command editor status       # confirm the editor server answers
```

An Editor that was already open picks up the package only on its next refresh, usually when its
window regains focus. Until then `unity status` reports `STATUS_PIPELINE_LOAD_PENDING`; if that
persists, ask the user to switch to the Unity Editor window, then re-run `unity status --until-ready`.

## 2. Autonomous edit loop (Editor)

This is the core agent workflow: keep the editor alive, change code, recompile, test.

```bash
# 1. Keep the editor ticking even when unfocused/minimized. REQUIRED before headless work —
#    Unity otherwise throttles or stalls update/compile when it isn't the active app.
unity command editor set_auto_tick --enabled true

# 2. Edit C# source files on disk normally.

# 3. Recompile (async: triggers a domain reload, then poll until done).
unity command script recompile
unity command script recompile_status    # repeat until "completed" or "up_to_date"
#   Tolerate connection errors while the domain reload is in flight — that is expected.
#   If script recompile_status reports failed=true, read its "errors" array and fix before testing.

# 4. (Optional) List available tests without running them.
unity command test list --mode editor            # mode: all | editor | playmode

# 5. Run tests (filter to keep it fast).
unity command test run --mode editor --filter MyFixture.MyTest
```

`filter-type`: `testName` | `assembly` | `category`. For long runs use `--async-tests true` and
poll `unity command test status` (abort with `test cancel`). When a selected test *fails*, `test run`
may return an opaque result: re-run a narrower `--filter`, or inspect the Test Runner / logs.

**Reading exit codes.** They separate "rewrite the invocation" from "the Editor failed", which
is the distinction this loop depends on:

| Exit | Meaning | What to do |
|------|---------|------------|
| `2` | Bad arguments — a misspelled flag, a wrong type, too many positionals. Nothing ran. | Fix the command line and retry. The error names the problem and often suggests the right flag. |
| `6` | The command ran and failed, or no Editor could be reached. | Read the error. If it says no Pipeline instance was found, the Editor isn't reachable: wait with `unity status --until-ready` (or check `unity pipeline list` for Safe Mode), then retry. Otherwise retrying the same invocation will not help. |

Do not retry an exit 2 unchanged, and do not rewrite a command line on an exit 6.

## 3. Runtime method reload

Change gameplay code in a **running** game with no domain reload. The game must be live:
enter Editor Play Mode (`unity command editor.playmode start`) or run a dev Player. The runtime Pipeline driver (enabled via Project Settings > Pipeline > Runtime) auto-discovers tagged methods on `Awake` (no manual
registration). Mono only — Editor Play Mode and Mono desktop dev builds, not IL2CPP. The token
is auto-injected for local requests.

### `script.methodreload reload`

Edit the method body directly; no separate file, no boilerplate.

1. **Before entering Play Mode**, tag the method `[MethodReload]` on the MonoBehaviour and recompile.
   The reload hook is compiled into the method, so tagging it while the game runs has no effect:
   exit Play Mode, recompile, and enter it again.
2. With the game running, edit the method body on disk.
3. Apply (re-run to iterate):
   ```bash
   unity command script.methodreload reload --filename Assets/Spinner.cs
   ```
   Add `--pdb` to debug it: it emits a portable PDB mapped to your source so breakpoints bind
   (attach the IDE + enable Editor Attaching; compiles unoptimized). Without it, no symbols.

Constraints: `void` instance methods only; **public** members only.

`unity command script.methodreload status` shows active overrides. `reload` options: `--timeout
<seconds>` (default 60; the CLI's `--timeout` is the command's own), `--assembly-dir <dir>` (persist
DLLs instead of in-memory); `script.methodreload cleanup --assembly-dir <dir>` clears old DLLs.

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
unity command script run --file AgentScripts/Build.cs --dry-run true   # compile-only check: diagnostics, nothing loaded or executed
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
- A failed eval exits 6 with an `errorCode`: `COMPILATION_ERROR` (each error with its line and
  column), `RUNTIME_EXCEPTION` (exception and stack trace), `TIMEOUT` or `INVALID_COMMAND_ARGS`.
- `script eval_file` is deprecated; use `script eval --file`.

## 6. Play Mode vs. Multiplayer Play Mode (MPPM) scenarios

These are two different things — do not conflate them.

### Single-player: normal Play Mode (main Editor only)

```bash
unity command editor.playmode start    # enter Play Mode
unity command editor.playmode pause    # toggle pause
unity command editor.playmode stop     # exit Play Mode
```

`editor.playmode start` runs the main Editor's **open scene** in Play Mode: **one** instance, never
MPPM virtual players, even if configured. Use it for single-instance testing and method reload.

### Multiplayer: Play Mode Scenarios (main Editor + virtual players)

A **Play Mode Scenario** is an asset (an `OrchestratedScenario`, e.g.
`Assets/Settings/PlayMode/2Players.asset`) describing the main Editor plus one or more virtual
players. Each virtual player (clone) is a **separate Unity project** at
`<project>/Library/VP/<vpId>` with its **own Library, port, and Pipeline server** — not a second
window of the main Editor. Run a scenario with the scenario commands (they drive MPPM's
`PlayModeScenarioManager`, which enters Play Mode itself), never with `editor.playmode start`,
which fights MPPM's play-mode guard:

```bash
unity command editor.playmode list_scenarios         # discover scenario assets
unity command editor.playmode list_players # players the active scenario declares
unity command editor.playmode set_active_scenario --path Assets/Settings/PlayMode/2Players.asset
unity command editor.playmode start_scenario        # run the active scenario
unity command editor.playmode get_scenario_status       # live scenario + play-mode state
unity command editor.playmode stop_scenario         # stop it
```

Authoring the asset (Edit Mode):

```bash
unity command editor.playmode create_scenario --name MyScenario # auto-adds the Main Editor player
unity command editor.playmode add_player --path Assets/Settings/PlayMode/MyScenario.asset --type clone --name "Player 2"
unity command editor.playmode remove_player --id <playerId> --path Assets/Settings/PlayMode/MyScenario.asset --confirm true  # id: GUID on 6000.5+, player name on 6000.3/6000.4
```

MPPM has four player slots, and slot 0 (`Player 1`) is always the Main Editor. A scenario therefore
holds at most **3 clones** (`Player 2`–`Player 4`): a 4-player scenario is the Main Editor plus 3 clones,
and a fourth clone is refused. For more players, add built players with `--type local`.

The `editor.playmode` scenario commands need **Unity 6000.3+** *and* the `com.unity.multiplayer.playmode`
package. Without both they are compiled out and calling one fails as an unknown command, so run
`unity command editor.playmode` and check that `start_scenario` and the other scenario commands are listed
before using them. The group itself always exists: `editor.playmode list_instances` needs neither
and always works. You may only `editor.playmode set_active_scenario` while state is **Idle**.

Player editors launch **asynchronously**, so after `editor.playmode start_scenario` poll
`editor.playmode get_scenario_status`. Its `players` array has one entry per player the scenario declares — not
MPPM's fixed four slots — and each entry carries both `state` (`NotLaunched` / `Launching` /
`Launched` / `UnexpectedlyStopped`) and, once that player's Editor is up, `reachable: true` with the
`port` and `projectPath` you need to drive it. Wait for `reachable`, then address it with
`--project-path`. Watch `state` to tell a slow launch (`Launching`) from a dead one
(`UnexpectedlyStopped`) — a missing descriptor alone cannot distinguish those.

### Discovering and driving the clones

The CLI folds a project's own clones into its main Editor, so a running scenario adds no
ambiguity by itself. But with another Unity project open, `unity command` with no `--project-path`
picks the Editor whose project contains the current directory (and fails as ambiguous outside
both). When more than one project is open, pass `--project-path <mainProject>` to the main Editor
commands above. Each clone runs its own server, so address one by its project directory:

```bash
# List every running instance (main + clones): role, port, pid, vpId, projectPath
unity command editor.playmode list_instances

# Talk to a clone directly, using the projectPath that listed it
unity command --project-path <mainProject>/Library/VP/<vpId> editor status
# Each clone is its own Editor: keep it ticking before running tests on it
unity command --project-path <mainProject>/Library/VP/<vpId> editor set_auto_tick --enabled true
unity command --project-path <mainProject>/Library/VP/<vpId> test run --mode playmode
```

`unity pipeline list` also reports clones, named by player ("Player 2") against the project they're
playing. There is no forwarding command — talk to the clone. That way you get its full command
surface and its own status, and nothing queues behind the main Editor.

### Multiplayer Roles (client / server stripping)

Multiplayer Roles decide what is stripped from a client or a server: the Editor runs as one role in
Play Mode, and each GameObject or component is kept for some roles and stripped from the others.
Use the `multiplayer.role` commands rather than reflecting over `EditorMultiplayerRolesManager` in
`script eval`:

```bash
unity command settings.multiplayer_roles get                    # enabled? safety checks, server stripping
unity command settings.multiplayer_roles set --settings '{"enabled":true}' --confirm true  # nothing is stripped until roles are enabled
unity command multiplayer.role get_active                       # Editor role, and whether roles are enabled
unity command multiplayer.role set_active --role server         # role the Editor plays as (Edit Mode only)
unity command multiplayer.role set_game_object --target <go> --role server                       # the GameObject
unity command multiplayer.role set_component --target <go> --type AudioSource --role client      # one of its components
unity command multiplayer.role set_component --target <component handle> --role client          # a component by handle
unity command multiplayer.role set_build --role server          # role the active build profile builds for
```

`role` is `client`, `server` or `client_and_server`. The group needs **Unity 6000.6+**; on older
Editors it is compiled out, so run `unity command multiplayer.role` to check it is listed.

## 7. Screen capture (Editor)

Capture a view to a PNG to check a visual result.

```bash
unity command capture game_view --output-path Screenshots/hud.png   # what the Game window shows, incl. overlay UI
unity command capture scene_view --max-resolution 512             # the scene only (camera render), inline, longest edge capped
unity command capture scene_view --source screen --output-path Screenshots/scene.png   # the Scene window with grid, gizmos, toolbars
unity command capture editor_window --window Inspector --output-path Screenshots/inspector.png   # any open Editor window
```

Without `--output-path` the image comes back inline (base64); `--include-inline-image true` gives
both. A relative path resolves from the project root (no `.meta` outside `Assets/`). `game_view`
opens a Game view if none is showing, in batchmode too. `game_view --source camera --camera <name>`
renders one camera; overlay UI is drawn on URP/HDRP (`--overlay auto|true|false`), not on Built-in.
`editor_window` and `scene_view --source screen` briefly front a background tab and don't work in
batchmode. `capture screenshot` is deprecated. On 6000.7+, `capture editor_element` /
`runtime_element` capture one UI Toolkit element by `--selector`.

## Gotchas

- **A stuck command may mean a modal dialog is open**, not a hang — a dialog blocks the main
  thread until dismissed. If a command runs long, check `unity command editor status` (answers
  instantly even when blocked); `status: "blocked_by_dialog"` means stop retrying and tell the
  human what's blocking (its `dialog.title`/`message`/`buttons`) — it can't be clicked over CLI.
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
2. If you changed runtime behaviour, enter Play Mode (`unity command editor.playmode start`), exercise
   the change, and read back the `Debug.Log` lines you added with `unity command console get`. Confirm
   they show the fix works, then stop Play Mode and remove those lines unless the user wants them kept.
3. If the fix doesn't work, go back and reread the relevant [Topic map](#topic-map) pages before you
   try again, rather than retrying from memory.

## Final report

End with a short report to the user:

- What you changed, by file, and the commands that verified it (test results, console status).
- Side effects: auto tick left on, Play Mode or a scenario still running, files written outside
  `Assets/` (such as `AgentScripts/` or screenshots), method-reload overrides still active.
- Anything the user must do or decide next, such as a failure you could not fix or a dialog only
  they can dismiss.

## Topic map

The Pipeline package ships its documentation in its `Documentation~` folder. For a registry install
that is `<project>/Library/PackageCache/com.unity.pipeline@*/Documentation~/`; the same pages are
published at `https://docs.unity3d.com/Packages/com.unity.pipeline@<VERSION>/`. Only read what you need.

Prefer the local copy: it matches the installed package. For the live pages, fetch the exact URL
rather than searching; `<VERSION>` is the major.minor of `com.unity.pipeline` in
`Packages/packages-lock.json`, e.g. `@0.9`.

| Topic | Page |
|-------|------|
| Connecting to an Editor or Player, ports, `--project-path` | `connectivity.md` |
| Dev Player and Runtime server setup | `runtime-setup.md` |
| Method reload (`[MethodReload]`, `script.methodreload`) | `method-reload.md` |
| `script run`, `script eval`, `script recompile` | `commands/scripts.md` |
| Compiling and running tests | `commands/build-and-compilation.md` |
| `editor status`, `editor set_auto_tick`, modal dialogs | `commands/editor-lifecycle-and-observability.md` |
| Play Mode scenarios, virtual players, clones | `commands/multiplayer-play-mode.md` |
| Screen capture | `commands/capture.md` |
| `console get` / `console status`, Runtime commands | `commands/runtime.md` |
