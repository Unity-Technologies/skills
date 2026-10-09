# Play-mode verification loop

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and the general "drive a running Editor" workflow. All global
flags (`--format json`, `--non-interactive`, `--yes`, `--proxy`, ...) apply to every command below.
[integration-advanced.md](integration-advanced.md) covers the Pipeline command surface generally;
this page is the recipe for one specific loop: enter Play mode, confirm the game is actually
running (not frozen), look at it, read its console, and tune it, all without leaving the CLI.

## Why this loop, and not `editor_play` + a screenshot

A screenshot renders on demand: it can capture the last frame a frozen Game view drew and look
exactly like a live game. Treat "Play mode entered" and "a screenshot came back" as setup, not
proof. The loop below adds one cheap, server-side check (`wait_for` on `Time.frameCount`) that
actually proves frames are advancing before you trust anything else you observe.

**Command names, parameters, and defaults below are the connected Editor's, not the CLI's own**:
they come from the project's `com.unity.pipeline` package and can change between package versions.
Confirm the exact set for your project with `unity command` / `unity list` before relying on any of
this, the same caution [integration-advanced.md](integration-advanced.md#available-in-production--the-common-live-commands)
already gives for `eval`.

**Put pipeline-specific flags after a `--` fence.** `unity command` recognizes a small set of its
own flags (`--project-path`, `--runtime`, `--runtime-path`, `--timeout`, `--detach`,
`--result-only`, plus the global flags) anywhere in the tail, not just at the start, so a
same-named Editor parameter (`eval`'s own `timeout`, in **milliseconds**) is swallowed by the CLI's
`--timeout` (**seconds**) unless it comes after `--`. Every multi-flag example below uses the
fence; a single positional argument (like a bare `eval` call at its default timeout) does not need
one.

## The recipe

### 1. Confirm a connected Editor and discover its commands

```bash
unity status --format json          # look for state "ready"
unity command --format json         # the full catalog, with each command's parameter schema
```

If nothing is reachable, rule out Safe Mode and a sandboxed shell before concluding the Editor is
down: see [integration-advanced.md → Recovering from Safe Mode](integration-advanced.md#recovering-from-safe-mode-connection-fails-because-of-compile-errors)
and [integration-advanced.md → Sandboxed agent tooling can hide a running Editor](integration-advanced.md#sandboxed-agent-tooling-can-hide-a-running-editor).

### 2. Keep the Editor ticking while it is unfocused

Unity throttles `EditorApplication.update` when the Editor window does not have focus, which is
the common case for an agent driving it headlessly or alongside other work. On an unfocused
Editor, entering Play mode can leave the game genuinely stuck at frame 1 while `unity status` still
reports it as playing, because the per-frame loop that would advance it is the thing being
throttled. `com.unity.pipeline` ships a command for exactly this, `set_autotick`, which forces a
tick at a throttled rate even while unfocused:

```bash
unity command set_autotick          # enable=true, interval_ms=16 (~60 Hz), persist=true by default
```

Call it once per Editor session, before `editor_play`. `persist=true` (the default) survives a
domain reload, so you do not need to call it again after a recompile in the same session; pass
`-- --persist false` for a one-off setting that should revert after the next unrelated recompile
instead of sticking for the rest of the session.

This is the confirmed, currently available fix for the freeze, not a temporary stand-in: it is a
real Pipeline command with its own doc entry (`Documentation~/commands/editor-lifecycle-and-observability.md`),
not something a future package release removes the need for. The CLI does not call it
automatically before `editor_play`: it sends only the bare `editor_play` command, so call it
yourself.

The player loop has a second switch: the project's Run In Background Player Setting
(`Application.runInBackground`). On macOS and Windows, with it off, Unity pauses the game while the
Editor is unfocused, and `set_autotick` can't undo that pause. The setting ships in builds, so
turn it on explicitly with the Pipeline command, which records the project's value and keeps it out
of builds:

```bash
unity command editor_set_run_in_background                      # on for this Editor session
unity command editor_set_run_in_background -- --enable false    # put the project's value back
```

The override doesn't touch the project's saved settings, so it lasts until you revert it or the
Editor restarts. After a restart, run the command again.

Alternatively, a project can turn on **Ensure editor runs in background in PlayMode** under
Project Settings > Pipeline > Editor, which applies it only while playing. Check that your package
has the command with `unity command --query editor_set_run_in_background --format json`. On an older
package, set it with `eval` before `editor_play`, and put the project's value back after
`editor_stop`, because nothing keeps it out of builds there:

```bash
unity command eval "return UnityEngine.Application.runInBackground;"   # note the project's value
unity command eval "UnityEngine.Application.runInBackground = true;"
```

### 3. Enter Play mode, then wait for the Pipeline server before the next command

```bash
unity command editor_play
unity status --until-ready --project-path <path> --format json
```

`editor_play` drops the Pipeline listener for a few seconds while the domain reloads. Sending the
next command immediately can hit `EDITOR_NOT_READY` or simply queue behind the reconnect. `unity
status --until-ready` is the documented fix, not a hand-rolled polling loop: it keeps checking
until a matching Editor reports `ready`, then returns. Full detail in
[integration-advanced.md → status](integration-advanced.md#status--live-state-of-connected-editors).

### 4. Confirm the game is actually advancing, not frozen at frame 1

A `wait_for` on `Time.frameCount` with `op: changed` proves the frame counter has moved since the
wait started, server-side, with no client polling loop:

```bash
unity command wait_for -- \
  --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' \
  --timeout_s 10
```

`op: changed` needs no `value`: it is met the first time the member differs from the value observed
when the wait was armed. A `met: true` result with `framesObserved` > 0 is your proof; `timedOut:
true` with the frame count unmoved is the frozen-at-frame-1 symptom this whole step exists to
catch, and step 2 (`set_autotick` and `runInBackground`) is the fix to check first.

A package that has `editor_set_run_in_background` also reports the player loop in
`unity status --format json` while in Play mode: `frameCount` and `playerLoopTicking` on the
instance row. `playerLoopTicking: false` outside a pause is the same frozen symptom, and it costs
one read instead of a wait. Keep the `wait_for` above as the proof; the status fields are a quick
first check, and an older package omits them. `timeout_s` defaults to 30 if omitted;
10 seconds above is a shortened budget, since a ticking Editor should advance the frame count within
a couple of frames of the wait being armed.

`wait_for` is one `/api/exec` request that holds the exec queue for its own duration, so a
synchronous call like the one above is only for a wait you expect to resolve in a few seconds. For
anything longer, or a condition that depends on a command you still need to issue, add `--async
true` and poll the result instead of blocking on it:

```bash
unity command wait_for -- --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' \
  --timeout_s 120 --async true          # returns { "waitId": "..." } immediately
unity command wait_status -- --wait_id <waitId>
```

`wait_for`'s own `--async` is unrelated to `unity command`'s `--detach`: the former is the
connected Editor evaluating the condition in the background and reporting through `wait_status` /
`wait_cancel`; the latter is the CLI's own client-side job tracking (`unity job status` /
`unity job wait`) for a command that runs long on the Editor's main thread. Don't conflate the two.

`wait_for` also accepts a `findType`/`target` + instance member path instead of a static one (for
example a gameplay manager's own state field), and an `on_met.capture` follow-up that captures the
game view in the same frame the condition first holds, atomically, when a client-side "wait, then
screenshot" loop would risk missing a short-lived state. Discover the exact shape (and whether your
project's package version has it at all) with `unity command --query wait_for --format json`
rather than assuming it: `wait_for`/`wait_status`/`wait_cancel` are a comparatively new addition to
`com.unity.pipeline`.

### 5. Capture the game view without bloating the response

```bash
unity command capture_game_view -- --source screen --save_path Screenshots/playtest.png
```

Pass `save_path` and the result is a file path, not an inline base64 image, which keeps an agent's
tool result small. The path is **project-relative** (the package's own docs give
`Screenshots/foo.png` as the example); nothing in the package documentation states a restriction to
paths under `Assets/`, so do not assume one. When you do need the image inline instead of, or as
well as, the file (for example to look at it yourself in this turn), add `--include_inline_image
true` and use `--max_resolution` to cap the inline copy's longest edge without touching the
resolution of the saved file:

```bash
unity command capture_game_view -- --max_resolution 512   # inline only, downscaled, no save_path
```

`source=screen` captures the composited game view (including Screen Space - Overlay UI such as
HUDs); `source=camera` renders a single camera off-screen instead and never sees overlay canvases.
Pass whichever you mean explicitly: the command's default value for `source` has changed between
package versions (`com.unity.pipeline` 0.8.0-exp.1 switched the default from `camera` to `screen`),
so relying on the default is one more thing to go stale as the project's package updates.

### 6. Read the console for exceptions before trusting a clean-looking capture

```bash
unity command console -- --tail 50 --level error
```

`level` is a minimum-severity floor (`log` | `warn` | `error`, default `log`), not an exact match, so
`--level error` returns errors and exceptions only. Prefer `console_status` (no arguments) when you
only need the compile-failure flag and entry counts, not the entries themselves, to poll cheaply
while something else runs.

Older `com.unity.pipeline` documentation names this command `get_console_logs`; the currently
shipping command is `console` (with `console_status` and `clear_console` alongside it), which is a
case in point for confirming names against `unity command` rather than trusting any cached
documentation, this page included.

### 7. Tune the running game with `eval`, not edit, recompile, and re-enter Play mode

Once Play mode is confirmed live, prefer evaluating C# against the running instance over leaving
Play mode to edit a script, recompiling, and re-entering: it is one round trip instead of a
domain reload plus a fresh Play mode entry, and it does not lose whatever state you were verifying.

```bash
unity command eval "UnityEngine.Time.timeScale = 0.5f;"
```

A bare positional string is `eval`'s `code` parameter at its default 5-second timeout. Override the
timeout with the fully-named form (`timeout` is milliseconds on `eval`, not the seconds `unity
command --timeout` takes, which is exactly the collision the `--` fence in this page's intro
exists for):

```bash
unity command eval -- --code 'return UnityEngine.Time.frameCount;' --timeout 15000
```

Reach for `create_script` → `unity recompile` → `attach_script` (documented in
[integration-advanced.md → Authoring custom `[CliCommand]` tools](integration-advanced.md#authoring-custom-clicommand-tools))
only when the change genuinely needs a new type or a persistent script asset. For a one-off runtime
tweak inside a live Play mode session, `eval` is cheaper and keeps the loop in this page intact.

### 8. Exit Play mode when you are done

```bash
unity command editor_stop
```

## Summary: the ordered commands

```bash
unity status --format json
unity command set_autotick
unity command editor_set_run_in_background
unity command editor_play
unity status --until-ready --project-path <path> --format json
unity command wait_for -- --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' --timeout_s 10
unity command capture_game_view -- --source screen --save_path Screenshots/playtest.png
unity command console -- --tail 50 --level error
unity command eval "<C# to inspect or tune the running game>"
unity command editor_stop
unity command editor_set_run_in_background -- --enable false
```
