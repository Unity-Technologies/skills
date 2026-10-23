# Play-mode verification loop

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and the general "drive a running Editor" workflow. All global
flags (`--format json`, `--non-interactive`, `--yes`, `--proxy`, ...) apply to every command below.
[integration-advanced.md](integration-advanced.md) covers the Pipeline command surface generally;
this page is the recipe for one specific loop: enter Play mode, confirm the game is actually
running (not frozen), look at it, read its console, and tune it, all without leaving the CLI.

## Why this loop, and not `editor.playmode start` + a screenshot

A screenshot renders on demand: it can capture the last frame a frozen Game view drew and look
exactly like a live game. Treat "Play mode entered" and "a screenshot came back" as setup, not
proof. The loop below adds one cheap, server-side check (`job wait` on `Time.frameCount`) that
actually proves frames are advancing before you trust anything else you observe.

**Command names, parameters, and defaults below are the connected Editor's, not the CLI's own**:
they come from the project's `com.unity.pipeline` package and can change between package versions.
Confirm the exact set for your project with `unity command` / `unity list` before relying on any of
this, the same caution [integration-advanced.md](integration-advanced.md#available-in-production--the-common-live-commands)
already gives for `script eval`.

**Put pipeline-specific flags after a `--` fence.** `unity command` recognizes a small set of its
own flags (`--project-path`, `--runtime`, `--runtime-path`, `--timeout`, `--detach`,
`--result-only`, plus the global flags) anywhere in the tail, not just at the start, so a
same-named Editor parameter (`script eval`'s own `timeout`, in **milliseconds**) is swallowed by the CLI's
`--timeout` (**seconds**) unless it comes after `--`. Every multi-flag example below uses the
fence; a single positional argument (like a bare `script eval` call at its default timeout) does not need
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

Then read the play state before you change anything: `unity command editor status` reports it, and
its `playMode` reads `stopped` when the Editor is not playing. If the Editor is already playing or
paused, that session is the user's. Reading it is fine (steps 4 to 6 only read). Leave the settings in
step 2, `editor.playmode start` in step 3, the pause and step recipe in step 4, the tuning in step 7 and
`editor.playmode stop` in step 8 alone, and tell the user what you found.

### 2. Keep the Editor ticking while it is unfocused

Unity throttles `EditorApplication.update` when the Editor window does not have focus, which is
the common case for an agent driving it headlessly or alongside other work. On an unfocused
Editor, entering Play mode can leave the game genuinely stuck at frame 1 while `unity status` still
reports it as playing, because the per-frame loop that would advance it is the thing being
throttled. `com.unity.pipeline` ships a command for exactly this, `editor set_auto_tick`, which forces a
tick at a throttled rate even while unfocused:

```bash
unity command editor set_auto_tick  # enable=true, interval_ms=16 (~60 Hz), persist=true by default
```

Call it once per Editor session, before `editor.playmode start`. `persist=true` (the default) survives a
domain reload, so you do not need to call it again after a recompile in the same session; pass
`-- --persist false` for a one-off setting that should revert after the next unrelated recompile
instead of sticking for the rest of the session.

This is the confirmed, currently available fix for the freeze, not a temporary stand-in: it is a
real Pipeline command with its own doc entry (`Documentation~/commands/editor-lifecycle-and-observability.md`),
not something a future package release removes the need for. The CLI does not call it
automatically before `editor.playmode start`: it sends only the bare `editor.playmode start` command,
so call it yourself.

The player loop has a second switch: the project's Run In Background Player Setting
(`Application.runInBackground`). On macOS and Windows, with it off, Unity pauses the game while the
Editor is unfocused, and `editor set_auto_tick` can't undo that pause. The setting ships in builds, so
turn it on explicitly with the Pipeline command, which records the project's value and keeps it out
of builds:

```bash
unity command editor.playmode set_run_in_background                      # on for this Editor session
unity command editor.playmode set_run_in_background -- --enable false    # put the project's value back
```

The override doesn't touch the project's saved settings, so it lasts until you revert it or the
Editor restarts. After a restart, run the command again.

Alternatively, a project can turn on **Ensure editor runs in background in PlayMode** under
Project Settings > Pipeline > Editor, which applies it only while playing. Check that your package
has the command with `unity command --query set_run_in_background --detail full --format json`. On an older
package, set it with `script eval` before `editor.playmode start`, and put the project's value back after
`editor.playmode stop`, because nothing keeps it out of builds there:

```bash
unity command script eval "return UnityEngine.Application.runInBackground;"   # note the project's value
unity command script eval "UnityEngine.Application.runInBackground = true;"
```

### 3. Enter Play mode, then wait for the Pipeline server before the next command

```bash
unity command editor.playmode start
unity status --until-ready --project-path <path> --format json
```

`editor.playmode start` drops the Pipeline listener for a few seconds while the domain reloads. Sending the
next command immediately can hit `EDITOR_NOT_READY` or simply queue behind the reconnect. `unity
status --until-ready` is the documented fix, not a hand-rolled polling loop: it keeps checking
until a matching Editor reports `ready`, then returns. Full detail in
[integration-advanced.md → status](integration-advanced.md#status--live-state-of-connected-editors).

### 4. Confirm the game is actually advancing, not frozen at frame 1

A `job wait` on `Time.frameCount` with `op: changed` proves the frame counter has moved since the
wait started, server-side, with no client polling loop:

```bash
unity command job wait -- \
  --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' \
  --timeout 10
```

`op: changed` needs no `value`: it is met the first time the member differs from the value observed
when the wait was armed. A `met: true` result with `framesObserved` > 0 is your proof; `timedOut:
true` with the frame count unmoved is the frozen-at-frame-1 symptom this whole step exists to
catch. On a session you started, step 2 (`editor set_auto_tick` and `runInBackground`) is the fix to check
first. On the user's session, leave step 2 alone and report the stall.

A package that has `editor.playmode set_run_in_background` also reports the player loop in
`unity status --format json` while in Play mode: `frameCount` and `playerLoopTicking` on the
instance row. `playerLoopTicking: false` outside a pause is the same frozen symptom, and it costs
one read instead of a wait. Keep the `job wait` above as the proof; the status fields are a quick
first check, and an older package omits them. `timeout_s` defaults to 30 if omitted;
10 seconds above is a shortened budget, since a ticking Editor should advance the frame count within
a couple of frames of the wait being armed.

`job wait` is one `/api/exec` request that holds the exec queue for its own duration, so a
synchronous call like the one above is only for a wait you expect to resolve in a few seconds. For
anything longer, or a condition that depends on a command you still need to issue, add `--async
true` and poll the result instead of blocking on it:

```bash
unity command job wait -- --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' \
  --timeout_s 120 --async true          # returns { "waitId": "..." } immediately
unity command job wait_status -- --wait_id <waitId>
```

`job wait`'s own `--async` is unrelated to `unity command`'s `--detach`: the former is the
connected Editor evaluating the condition in the background and reporting through `job wait_status` /
`job wait_cancel`; the latter is the CLI's own client-side job tracking (`unity job status` /
`unity job wait`) for a command that runs long on the Editor's main thread. Don't conflate the two,
or `unity job wait <job-id>` (the CLI's own command) with `unity command job wait` (the Pipeline's).

`job wait` also accepts a `findType`/`target` + instance member path instead of a static one (for
example a gameplay manager's own state field), and an `on_met.capture` follow-up that captures the
game view in the same frame the condition first holds, atomically, when a client-side "wait, then
screenshot" loop would risk missing a short-lived state. Discover the exact shape (and whether your
project's package version has it at all) with `unity command job --format json`
rather than assuming it: `job wait`/`job wait_status`/`job wait_cancel` are a comparatively new addition to
`com.unity.pipeline`.

**Still frozen with `editor set_auto_tick` and Run In Background on?** Check for a locked desktop. On Windows, a locked session can stop Play Mode from advancing even with Run In Background on, so every `job wait` times out and every observation reads frame 1. If nobody can unlock the machine, drive the frames yourself, but only on a session you started. That is one you entered with `editor.playmode start` from a stopped Editor, as step 1 showed. If the Editor was already playing, the session is the user's, so leave its settings alone too, and don't pause, step or stop it. A paused session is flat by design, so ask the user to resume it. If it is frozen, say so, name the likely causes (a locked desktop, or an unfocused Editor with Run In Background off), and ask the user to fix that. On your own session, pause it, then step it one frame per `script eval` call.

```bash
unity command script eval -- --code 'UnityEditor.EditorApplication.isPaused = true;'
unity command script eval -- --code 'UnityEditor.EditorApplication.Step();'     # one frame: repeat once per frame you need
unity command script eval -- --code 'return UnityEngine.Time.frameCount;'      # read the count in its own call
```

Each `Step()` advances one frame. Drive it from the host, one call per frame, because an `script eval` runs on the main thread and holds the player loop until it returns. Check that the count rose by exactly the number of steps. A frame is not a fixed span of game time. Read `UnityEngine.Time.time` before and after one step, then divide the time you need by the difference (one project measured 50 steps to a second, and yours can differ). If `Time.time` does not move because `Time.timeScale` is 0, no number of steps produces game time. Step by frame count for logic that runs per frame, and for anything timed, ask the user before you change `timeScale`, then put it back. Every step is an `script eval` call that the client may ask the user to approve, so keep the stepped window short and ask before a run that needs more than a few hundred steps. Set `UnityEditor.EditorApplication.isPaused = false;` when you are done, whatever the outcome. Stepping shows what happens frame by frame, not real-time timing or frame rate.

### 5. Capture the game view without bloating the response

```bash
unity command capture game_view -- --source screen --save_path Screenshots/playtest.png
```

Pass `save_path` and the result is a file path, not an inline base64 image, which keeps an agent's
tool result small. The path is **project-relative** (the package's own docs give
`Screenshots/foo.png` as the example); nothing in the package documentation states a restriction to
paths under `Assets/`, so do not assume one. When you do need the image inline instead of, or as
well as, the file (for example to look at it yourself in this turn), add `--include_inline_image
true` and use `--max_resolution` to cap the inline copy's longest edge without touching the
resolution of the saved file:

```bash
unity command capture game_view -- --max_resolution 512   # inline only, downscaled, no save_path
```

`source=screen` captures the composited game view (including Screen Space - Overlay UI such as
HUDs); `source=camera` renders a single camera off-screen instead and never sees Screen Space -
Overlay canvases. Pass whichever you mean explicitly: the command's default value for `source` has
changed between package versions (`com.unity.pipeline` 0.8.0-exp.1 switched the default from
`camera` to `screen`), so relying on the default is one more thing to go stale as the project's
package updates.

`source=screen` needs an unlocked desktop (reported on Windows): a `screen` capture taken while the session was locked came back as a solid cyan image with no error. When the machine is unattended and may be locked, use `--source camera`. Treat a single-color capture as suspect and check that the frame count is rising before you rely on it, since a fade to black is a legitimate frame. A camera capture shows overlay UI only if its Canvas uses Screen Space - Camera, because no camera draws a Screen Space - Overlay canvas. If the UI you need is on an Overlay canvas, read its state with `script eval` instead of looking for it in the image.

### 6. Read the console for exceptions before trusting a clean-looking capture

```bash
unity command console get -- --tail 50 --level error
```

`level` is a minimum-severity floor (`log` | `warn` | `error`, default `log`), not an exact match, so
`--level error` returns errors and exceptions only. Prefer `console status` (no arguments) when you
only need the compile-failure flag and entry counts, not the entries themselves, to poll cheaply
while something else runs.

Older `com.unity.pipeline` documentation names this command `get_console_logs`, and before command
groups it was `console` (with `console_status` and `clear_console` alongside it); the currently
shipping command is `console get` (with `console status` and `console clear`), which is a
case in point for confirming names against `unity command` rather than trusting any cached
documentation, this page included.

### 7. Tune the running game with `script eval`, not edit, recompile, and re-enter Play mode

Once Play mode is confirmed live, prefer evaluating C# against the running instance over leaving
Play mode to edit a script, recompiling, and re-entering: it is one round trip instead of a
domain reload plus a fresh Play mode entry, and it does not lose whatever state you were verifying.

```bash
unity command script eval "UnityEngine.Time.timeScale = 0.5f;"
```

A bare positional string is `script eval`'s `code` parameter at its default 5-second timeout. Override the
timeout with the fully-named form (`timeout` is milliseconds on `script eval`, not the seconds `unity
command --timeout` takes, which is exactly the collision the `--` fence in this page's intro
exists for):

```bash
unity command script eval -- --code 'return UnityEngine.Time.frameCount;' --timeout 15000
```

Reach for `script create` → `unity recompile` → `script attach` (documented in
[integration-advanced.md → Authoring custom `[CliCommand]` tools](integration-advanced.md#authoring-custom-clicommand-tools))
only when the change genuinely needs a new type or a persistent script asset. For a one-off runtime
tweak inside a live Play mode session, `script eval` is cheaper and keeps the loop in this page intact.

### 8. Exit Play mode when you are done

```bash
unity command editor.playmode stop
unity command editor.playmode set_run_in_background -- --enable false
unity command editor set_auto_tick -- --enable false
```

Stop only a session you started. One that was already playing when you read `editor status` in
step 1 is the user's. Then put back what step 2 changed: with `persist=true`, auto-tick otherwise
keeps the Editor ticking while unfocused for the rest of the session. Skip the `editor set_auto_tick`
revert if you'll keep driving the Editor, because an unfocused Editor stalls without it.

## Summary: the ordered commands

```bash
unity status --format json
unity command editor status   # already playing or paused? Then the session is the user's: read it, change nothing
unity command editor set_auto_tick
unity command editor.playmode set_run_in_background
unity command editor.playmode start
unity status --until-ready --project-path <path> --format json
unity command job wait -- --condition '{"member":"UnityEngine.Time.frameCount","op":"changed"}' --timeout_s 10
unity command capture game_view -- --source screen --save_path Screenshots/playtest.png   # --source camera when the machine may be locked
unity command console get -- --tail 50 --level error
unity command script eval "<C# to inspect or tune the running game>"
unity command editor.playmode stop
unity command editor.playmode set_run_in_background -- --enable false
unity command editor set_auto_tick -- --enable false   # skip if you'll keep driving the Editor
```
