---
name: unity-pipeline
description: Drives a running Unity Editor or dev Player through the unity-pipeline package — recompile, run tests, run C# scripts, method-reload code at runtime and audit the project. Use when an agent must control a live Unity instance or automate its tests.
---

# Unity Pipeline (agent control)

Invoke commands with `unity command <name> [args]`. Run `unity command` with no name to
list what an instance exposes. Two servers exist: **Editor** (`7800-7849`, auto-starts with
the editor) and **Runtime** (`7900-7949`, only in a dev Player build). `unity command --query <term>`
filters that list by name, description or tag.

## 1. Install & verify

```bash
unity pipeline install            # install into current project (or --project-path)
unity pipeline list               # confirm the editor instance + server are reachable
unity status --until-ready        # wait until the editor server is ready
unity command editor_status       # confirm the editor server answers
```

An Editor that was already open picks up the package only on its next refresh, usually when its
window regains focus. Until then `unity status` reports `STATUS_PIPELINE_LOAD_PENDING`; if that
persists, ask the user to switch to the Unity Editor window, then re-run `unity status --until-ready`.

## 2. Autonomous edit loop (Editor)

This is the core agent workflow: keep the editor alive, change code, recompile, test.

```bash
# 1. Keep the editor ticking even when unfocused/minimized. REQUIRED before headless work —
#    Unity otherwise throttles or stalls update/compile when it isn't the active app.
unity command set_autotick --enable true

# 2. Edit C# source files on disk normally.

# 3. Recompile (async: triggers a domain reload, then poll until done).
unity command recompile
unity command recompile_status    # repeat until "completed" or "up_to_date"
#   Tolerate connection errors while the domain reload is in flight — that is expected.
#   If recompile_status reports failed=true, read its "errors" array and fix before testing.

# 4. (Optional) List available tests without running them.
unity command list_tests --mode editor            # mode: all | editor | playmode

# 5. Run tests (filter to keep it fast).
unity command run_tests --mode editor --filter MyFixture.MyTest
```

`run_tests` modes: `all` | `editor` | `playmode`. `filter_type`: `testName` | `assembly` |
`category`. For long runs use `--async_tests true` and poll `unity command test_status`
(abort with `unity command cancel_tests`).

> **Known caveat:** when any selected test *fails*, `run_tests` may surface an opaque
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
enter Editor Play Mode (`unity command editor_play`) or run a dev Player. The runtime Pipeline driver (enabled via Project Settings > Pipeline > Runtime) auto-discovers tagged methods on `Awake` (no manual
registration). Mono only — Editor Play Mode and Mono desktop dev builds, not IL2CPP. The token
is auto-injected for local requests.

### `reload_file`

Edit the method body directly; no separate file, no boilerplate.

1. **Before entering Play Mode**, tag the method `[MethodReload]` on the MonoBehaviour and recompile.
   The reload hook is compiled into the method, so tagging it while the game runs has no effect:
   exit Play Mode, recompile, and enter it again.
2. With the game running, edit the method body on disk.
3. Apply (re-run to iterate):
   ```bash
   unity command reload_file --filename Assets/Spinner.cs
   ```
   Add `--pdb` to make it debuggable — emits a portable PDB mapped to your source so breakpoints in
   the original file bind (attach the IDE + enable Editor Attaching; compiles unoptimized):
   ```bash
   unity command reload_file --filename Assets/Spinner.cs --pdb
   ```

Constraints: `void` instance methods only; **public** members only; debugging requires `--pdb`
(the default emits no symbols).

`unity command methodreload_status` shows active overrides. `reload_file` options: `--timeout <ms>`
(default 30000), `--assemblyDir <dir>` (persist DLLs instead of in-memory);
`cleanup_methodreload --assemblyDir <dir>` clears old DLLs.

The CLI consumes `--timeout` itself, in seconds. To pass the package's `--timeout`, put the command's
arguments after `--`:

```bash
unity command reload_file -- --filename Assets/Spinner.cs --timeout 60000
```

## 4. Bulk construction: `run_script` (the builder pattern)

For bulk work — creating many objects, wiring fields, generating content — put the code in a
**versioned project script** and run a named static entry point with `run_script`. No domain
reload, no code carried through the protocol; iterating costs an in-memory compile (< ~2s), not
a 15–20s recompile.

```bash
# 1. Write the builder OUTSIDE Assets/ (so the write triggers no asset import / domain reload),
#    e.g. AgentScripts/Build.cs:  public static class Build { public static int All() { ... } }
# 2. Run it — relative paths resolve against the project root (the parent of Assets/):
unity command run_script --file AgentScripts/Build.cs --entry Build.All
# 3. Iterate: edit the file, re-run. Useful extras:
unity command run_script --file AgentScripts/Build.cs --dry_run true   # compile-only check: diagnostics, nothing loaded or executed
unity command run_script --file AgentScripts/Build.cs --entry Build.All --args '[3, "Green"]'
```

Rules of thumb:

- **Code goes in files on disk via `run_script`; `eval` is for genuinely ad-hoc one-liners.**
  Never ship multi-line escaped C# strings through `eval` — write the file, run the entry.
- Compiles see the project's **active editor defines** (`UNITY_EDITOR`, version/platform symbols);
  `--defines` appends extra symbols on top.
- Entry points may be `async Task`/`Task<T>` — they are awaited asynchronously (the editor keeps
  pumping, so awaits resuming on Unity's context work naturally) and `Task<T>.Result` is returned.
  The wait is bounded by `timeout_ms`; on expiry the task keeps running detached.
- Runtime exceptions come back with `file:line` mapped to your source (a source-mapped PDB is
  always emitted for executing runs; they compile unoptimized).
- `--mode hotpatch` instead applies `[MethodReload]` in-place method replacements (delegates to
  `reload_file`); `entry`/`args`/`dry_run` are rejected there and `references`/`defines` don't apply.

## 5. Quick C# eval (Editor or dev Player)

For genuinely ad-hoc one-liners only — anything longer belongs in a file run via `run_script`.
`eval` targets the Editor by default; add `--runtime <process name>` to run it in a dev Player.

```bash
unity command eval "return 2 + 2;"
unity command eval "return UnityEngine.Time.timeScale;" --runtime MyGame
# Or evaluate a .cs file on disk. Keep it outside Assets/, where Unity would compile it as project source:
unity command eval_file AgentScripts/Scratch.cs
```

## 6. Project audit (Editor)

Static-analysis scan via Project Auditor, producing a CSV of issues to fix. Trigger, poll, read.

```bash
unity command audit                              # optional: --categories Code,ProjectSetting --output my.csv
unity command audit_status                       # repeat until terminal status
```

`audit_status` is terminal on `completed` (with `csvPath` + `issueCount`), `failed`, `unavailable`, or
`interrupted` (a domain reload killed the scan — just re-run `audit`). Polling stays responsive while a
Code-category scan compiles assemblies and holds the main thread. One scan at a time: a second `audit`
returns `busy`. There is no cancel — stop polling to abandon a scan.

CSV columns: `Category, Severity, Areas, Description, RelativePath, Line, DescriptorId, Recommendation`
(diagnostics only, so every row is something to fix; `Recommendation` says how).

> **Requires Project Auditor plus its rules.** `unavailable` means the Editor has no Project Auditor,
> or it has no analysis rules — in a built-in-module Editor the rules live in the separate
> `com.unity.project-auditor-rules` package (`unity command package_add --identifier
> com.unity.project-auditor-rules --confirm true`). Read the `message` field; the command never reports
> an empty `completed` scan that would look like a clean project.

## Gotchas

- **`set_autotick` first.** Without it, recompile and tests can hang while the editor is
  unfocused. The package's watchdog relies on the tick loop staying alive.
- **A stuck command may mean a modal dialog is open**, not a hang — a dialog blocks the main
  thread until dismissed. If a command runs long, check `unity command editor_status` (answers
  instantly even when blocked); `status: "blocked_by_dialog"` means stop retrying and tell the
  human what's blocking (its `dialog.title`/`message`/`buttons`) — it can't be clicked over CLI.
- **Method reload needs the game running.** `reload_file` applies to a live
  game — enter Editor Play Mode (`unity command editor_play`) first, or run a dev Player.
- **Player-only commands need a dev Player.** `log`, `set_timescale`, `runtime_status`, etc. hit
  the Runtime server, which does not run in the Editor.
- **Async commands poll.** `recompile`→`recompile_status`, `run_tests --async_tests`→
  `test_status`. Never assume completion from the trigger call's response.
- **Target a specific instance** when more than one is running: `--project-path <path>` selects an
  Editor, `--runtime <process name>` or `--runtime-path <port file>` selects a dev Player.
