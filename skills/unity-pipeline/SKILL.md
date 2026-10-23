---
name: unity-pipeline
description: Drives a Unity Editor or dev Player via the unity-pipeline package. Compile, run tests, run C# scripts, method-reload code, check Editor status, enter Play Mode, capture screenshots. Use when an agent must control a running Unity instance.
---

# Unity Pipeline (agent control)

Invoke commands with `unity command <group> <name> [args]`. Run `unity command` with no
argument to list the command groups an instance exposes, and `unity command <group>` to list
the commands in one group. Two servers exist: **Editor** (`7800-7849`, auto-starts with
the editor) and **Runtime** (`7900-7949`, only in a dev Player build). `unity command --query <term>`
filters the listing by name, description or tag.

## Before you start

- Create a restore point you can roll back to if your changes fail, such as a commit or a stash of
  the project's working tree.
- Read the project's Editor version from `ProjectSettings/ProjectVersion.txt` (`m_EditorVersion`),
  because some commands and parameters below differ by Unity version. Use it to pick the matching
  steps wherever this skill names a version.
- Do only what's asked. Don't change unrelated assets or files. Avoid long explanations.
- Before you go forward with a workflow or diagnosis, open and read the [Topic map](#topic-map)
  page(s) that cover the commands you intend to run. Name the file(s) you read in your response. This
  is because your training knowledge about Unity might be out of date, incorrect, or for the wrong
  Unity version, and Pipeline command names and parameters change between package versions.
- When a change affects runtime behaviour, add `Debug.Log` lines that you will read back in Play Mode
  to verify the behaviour (see [Final checks](#final-checks)).

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
unity command editor set_auto_tick --enable true

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

`test run` modes: `all` | `editor` | `playmode`. `filter_type`: `testName` | `assembly` |
`category`. For long runs use `--async_tests true` and poll `unity command test status`
(abort with `unity command test cancel`).

> **Known caveat:** when any selected test *fails*, `test run` may surface an opaque
> result instead of the failure details. Re-run a narrower `--filter`, or inspect the
> editor's Test Runner / logs to get the real failure.

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
   Add `--pdb` to make it debuggable — emits a portable PDB mapped to your source so breakpoints in
   the original file bind (attach the IDE + enable Editor Attaching; compiles unoptimized):
   ```bash
   unity command script.methodreload reload --filename Assets/Spinner.cs --pdb
   ```

Constraints: `void` instance methods only; **public** members only; debugging requires `--pdb`
(the default emits no symbols).

`unity command script.methodreload status` shows active overrides. `script.methodreload reload` options:
`--timeout <seconds>` (default 60), `--assemblyDir <dir>` (persist DLLs instead of in-memory);
`script.methodreload cleanup --assemblyDir <dir>` clears old DLLs.

The CLI's `--timeout` (seconds) is the command's timeout: the Pipeline writes it into the command's
own `timeout` parameter, so there is no separate value to pass:

```bash
unity command script.methodreload reload --filename Assets/Spinner.cs --timeout 120
```

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
unity command script run --file AgentScripts/Build.cs --dry_run true   # compile-only check: diagnostics, nothing loaded or executed
unity command script run --file AgentScripts/Build.cs --entry Build.All --args '[3, "Green"]'
```

Rules of thumb:

- **Code goes in files on disk via `script run`; `script eval` is for genuinely ad-hoc one-liners.**
  Never ship multi-line escaped C# strings through `script eval` — write the file, run the entry.
- Compiles see the project's **active editor defines** (`UNITY_EDITOR`, version/platform symbols);
  `--defines` appends extra symbols on top.
- Entry points may be `async Task`/`Task<T>` — they are awaited asynchronously (the editor keeps
  pumping, so awaits resuming on Unity's context work naturally) and `Task<T>.Result` is returned.
  The wait is bounded by `--timeout` (seconds, default 60); on expiry the task keeps running detached.
- Runtime exceptions come back with `file:line` mapped to your source (a source-mapped PDB is
  always emitted for executing runs; they compile unoptimized).
- `--mode hotpatch` instead applies `[MethodReload]` in-place method replacements (delegates to
  `script.methodreload reload`); `entry`/`args`/`dry_run` are rejected there and `references`/`defines` don't apply.

## 5. Quick C# eval (Editor or dev Player)

For genuinely ad-hoc one-liners only — anything longer belongs in a file run via `script run`.
`script eval` targets the Editor by default; add `--runtime <process name>` to run it in a dev Player.

```bash
unity command script eval "return 2 + 2;"
unity command script eval "return UnityEngine.Time.timeScale;" --runtime MyGame
# Or evaluate a .cs file on disk. Keep it outside Assets/, where Unity would compile it as project source:
unity command script eval_file AgentScripts/Scratch.cs
```

## 6. Play Mode vs. Multiplayer Play Mode (MPPM) scenarios

These are two different things — do not conflate them.

### Single-player: normal Play Mode (main Editor only)

```bash
unity command editor.playmode start    # enter Play Mode
unity command editor.playmode pause    # toggle pause
unity command editor.playmode stop     # exit Play Mode
```

`editor.playmode start` just flips the main Editor into Play Mode running the **open scene**. It
controls **one** instance and does **not** launch MPPM virtual players, even if the project has
them configured. Use it for single-instance testing and to make runtime code reload live.

### Multiplayer: Play Mode Scenarios (main Editor + virtual players)

A **Play Mode Scenario** is an asset (an `OrchestratedScenario`, e.g.
`Assets/Settings/PlayMode/2Players.asset`) describing the main Editor plus one or more virtual
players. Each virtual player (clone) is a **separate Unity project** at
`<project>/Library/VP/<vpId>` with its **own Library, port, and Pipeline server** — not a second
window of the main Editor. `editor.playmode start` does **not** run a scenario; use the scenario commands
(they drive MPPM's `PlayModeScenarioManager`, which enters Play Mode itself):

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
unity command editor.playmode remove_player --id <playerId> --path Assets/Settings/PlayMode/MyScenario.asset --confirm true
```

For `remove_player --id`, check the Editor version you read in [Before you start](#before-you-start):
on 6000.5 and later pass the player's GUID; on 6000.3 and 6000.4 pass the player's name.

The `editor.playmode` scenario commands need **Unity 6000.3+** *and* the `com.unity.multiplayer.playmode`
package, so check the Editor version and `Packages/manifest.json` first. Without both they are compiled out and calling one fails as an unknown command, so run
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
unity command --project-path <mainProject>/Library/VP/<vpId> editor set_auto_tick --enable true
unity command --project-path <mainProject>/Library/VP/<vpId> test run --mode playmode
```

`unity pipeline list` also reports clones, named by player ("Player 2") against the project they're
playing. There is no forwarding command — talk to the clone. That way you get its full command
surface and its own status, and nothing queues behind the main Editor.

## 7. Screen capture (Editor)

Capture a view to a PNG to check a visual result.

```bash
unity command capture screenshot --view game                      # or --view scene; returns the PNG path
unity command capture game_view --source screen --save_path Screenshots/hud.png   # what the Game window shows, incl. overlay UI
unity command capture scene_view --max_resolution 512             # inline base64 image, longest edge capped
```

`capture game_view` and `capture scene_view` return the image inline as base64 unless `--save_path` is
set; add `--include_inline_image true` to get both. `capture game_view --source camera --camera <name>`
renders one camera instead and misses Screen Space - Overlay UI. Without `--output`, `capture screenshot`
writes under `<project>/Temp/pipeline-screenshots/`. If the Editor version is 6000.7 or later,
`capture editor_element` and `capture runtime_element` capture a single UI Toolkit element by
`--selector`. On earlier versions these commands don't exist, so capture the whole view instead.

## Gotchas

- **`editor set_auto_tick` first.** Without it, recompile and tests can hang while the editor is
  unfocused. The package's watchdog relies on the tick loop staying alive.
- **A stuck command may mean a modal dialog is open**, not a hang — a dialog blocks the main
  thread until dismissed. If a command runs long, check `unity command editor status` (answers
  instantly even when blocked); `status: "blocked_by_dialog"` means stop retrying and tell the
  human what's blocking (its `dialog.title`/`message`/`buttons`) — it can't be clicked over CLI.
- **Method reload needs the game running.** `script.methodreload reload` applies to a live
  game — enter Editor Play Mode (`unity command editor.playmode start`) first, or run a dev Player.
- **`editor.playmode start` ≠ a scenario.** It runs only the main Editor's open scene; it never
  launches MPPM virtual players. For a multiplayer session set/start a Play Mode Scenario with
  `editor.playmode set_active_scenario` + `editor.playmode start_scenario` (see §6) — never drive a scenario with
  `editor.playmode start` (it fights MPPM's play-mode guard) — then drive the clones with `editor.playmode list_instances`
  and `--project-path`.
- **Player-only commands need a dev Player.** `console write`, `runtime set_time_scale`, `runtime status`, etc. hit
  the Runtime server, which does not run in the Editor.
- **Async commands poll.** `script recompile`→`script recompile_status`, `test run --async_tests`→
  `test status`. Never assume completion from the trigger call's response.
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

Give the user a short summary:

- What you did: the commands you ran, the files you changed, and the test or check results.
- Side effects you left behind: Play Mode still running, `editor set_auto_tick` still enabled, a
  scenario left active, files written under `AgentScripts/` or `Screenshots/`, method reload overrides
  still applied.
- Follow-ups the user needs to do: anything blocked by a modal dialog, failures you could not fix,
  and the restore point to roll back to.

## Topic map

The Pipeline package ships its documentation in its `Documentation~` folder. For a registry install
that is `<project>/Library/PackageCache/com.unity.pipeline@*/Documentation~/`; the same pages are
published at `https://docs.unity3d.com/Packages/com.unity.pipeline@<VERSION>/`. Only read what you need.

Prefer the local `Documentation~` copy, because it matches the installed package exactly. If you
use the live pages, prefer fetching the page directly (for example `WebFetch`) over searching (for
example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only
search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **Package on docs.unity3d.com**: The `com.unity.pipeline` major and minor version from
  `Packages/packages-lock.json`, eg `@0.9`.

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
