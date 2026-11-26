# Sessions

Relative paths in this file resolve to the skill directory, the folder above `references/`.

A session is a detached host that owns one scene and outlives the CLI call that made it. Run `unity transform session --help` for the commands this build has.

A session's name (`session new --name`) uses letters, digits, `.`, `_` and `-`, can't start with `ses_`, and must be unique among running sessions: `session new` refuses a taken name with `INVALID_COMMAND_ARGS` (exit 2). Every command that takes a session accepts the id or the name.

## Which session a command uses

1. `--session <id>`, when given.
2. Else the session `TRANSFORM_SESSION` names. A session it names that isn't running is `SESSION_NOT_FOUND`, never a fallback to the active one.
3. Else the active session: the one `session new` last started, or the one `session use <id>` picked. When it closes, the newest session still running becomes active; when its host dies, the next command warns (in `warnings`) before it uses another, so check `data.sessionId` against the session you expect. `session info` with no `--session` shows which one it is, and `session list` marks it.
4. Else `NO_SESSION` (exit 4).

A session that `--session` names and isn't running is `SESSION_NOT_FOUND` (exit 6), and the message says whether it was closed or its host exited; `session logs --session <id> --all` still reads its log.

Every result names the session it used in `data.sessionId`, and a person sees `Using session <id>` on stderr.

## Who creates and closes sessions

| Command | Creates a session | Closes it |
|---|---|---|
| `session new` | Yes, and makes it active unless `TRANSFORM_SESSION` is set | No |
| `run file` / `run code` without `--session` | Only with `--isolated`, or when no session is running at all and `TRANSFORM_SESSION` is unset | Only the one it created |
| `run file` / `run code` with `--session` | No | No |
| Every other command | No: it needs a running session | No |
| `session close --session <id>` | No | That session |

An isolated session is never active, so a loop of `run file … --isolated` over many files can't mix scenes or leave hosts behind: its host also stops if the `run` is killed, and `session close --all` closes one whose `run` is gone (it skips one whose `run` still goes). `--isolated` with `--session` is refused (exit 2). One run that loops over several files shares one scene, so clear it between files or run once per file.

## Agents side by side

The active session is shared by every process of the same user, so it can change under an agent that relies on it. Launch each agent with `TRANSFORM_SESSION=<name>` in its environment, or pass `--session <id>` on every call. Environment variables are per process, so each agent keeps its own. While `TRANSFORM_SESSION` is set, `session new` leaves the active session as it is. `~/.transform/.env` doesn't set it, because every process shares that file.

A session runs one command at a time; a command sent while another runs queues behind it. A command that waits says so on stderr (`Session <id> is busy running …`). `session reset` gives up after 30 seconds and `ui open` / `ui close` after 10 with `BRIDGE_TIMEOUT`; a request whose CLI gave up or was stopped leaves the queue and never runs. `session list` shows such a session as `busy`, with the command it runs and for how long (`running` in `--json`), because `info` and `ping` still answer. A `busy` with no command means the host is alive but didn't answer `info` in time (a long SDK call, or a window opening); `unreachable` is kept for a host the CLI can't connect to. `busy` means a command has not returned, not that it is making progress: a hung script stays `busy`. `session close` without `--force` stops a session that doesn't answer within about 5 seconds, because it is still running a command, and that command doesn't finish (`data.terminated: true`, with a warning); one that answers gets up to 5 more seconds to exit; `--force` stops it at once. Closing discards the scene, the running command and the queue, and a busy session can't export first. So if your own session shows `busy`, watch `session logs --session <id> --follow`, and close it only once it has clearly hung. Leave a busy session you don't own alone.

## Where a session runs

A session runs in `~/.transform`, so a relative path inside a script (`pxz.io.importScene("model.step")`) resolves there. Write absolute paths in `pxz` calls. A path given to a command, such as `scene import .\model.step` or `run file .\fix.py`, resolves where you run the command. A script's own arguments, after `--` (`run file .\convert.py -- --input C:\in\a.step`), reach it as typed, so pass absolute paths there too. An `argparse` script runs unchanged, as `__main__` with `__file__` set, and `sys.exit(0)` succeeds. A `SIGINT` the script sends (`signal.raise_signal`, `_thread.interrupt_main`) doesn't stop it or the session: the script runs to its end, then fails with `SCRIPT_FAILED` and a `KeyboardInterrupt`. Put every CLI option, `--json` included, before the `--` (`run file x.py --session <job> --json -- --input C:\in\a.step`), because everything after it is the script's `sys.argv[1:]`.

## Starting over and keeping a scene

- To try something again on the same model, such as another decimate ratio, run `scene undo --to "scene import"` instead of resetting, because it keeps the import and importing a large file again is slow (the Undo section of scene-editing.md).
- To start over on a new model in the same session, run `session reset`, because it clears the scene, its materials and the script variables. Run `scene clear` instead only when a later script needs variables an earlier one set, because it deletes the occurrences and resets the scene root (its name, transform, visibility, material and metadata), and keeps the materials and the script variables.
- To keep the scene past `session close`, save it with `scene export <file.pxz>` (always the whole scene: `--occurrence` is refused for `.pxz`) and load it later with `scene open <file.pxz>`. Warn the user before `scene open`, because it replaces the whole scene and clears the undo history, even when the file fails to open.

## The UI

`ui open` builds the SDK's interactive UI, a real window on the session's scene, and costs about 450 MB; `ui close` destroys it. `session new --ui` opens it at startup. Whoever is at the UI can edit the scene and run operations in it, and the session's commands see those changes. `session list` and `session info` report the session's `mode` as `headless` or `ui`. A session starts headless and needs no GPU or display; `scene screenshot` renders offscreen. A failed attach leaves the session headless and still running scripts; `session info` reports why in `uiError`. Only `ui open` needs the SDK's `pxzui` and write access to the SDK folder (pxzui writes `callback_manager.py` there as it opens), so a read-only SDK folder or a registered build without its UI breaks `ui open`, never the session's start. A downloaded build always has its UI; a registered one whose `pxzui` the session can't find (in `<sdk>/PixyzUI/`, or a `PixyzUI` / `UI` folder near the SDK) takes `session new --pxzui <folder holding pxzui>`.

- Don't open the UI just to take a screenshot, because `scene screenshot` renders offscreen.
- While the UI is open, run `scene info` again before relying on an earlier reading, because the user can edit the scene and run operations in it.
- If `ui open` reports the session is still headless, read `uiError` from `session info` and keep working headless without respawning, because a failed attach leaves the session fully usable.
- If the user refers to "these parts" or "what I picked" in the UI, run `selection get` for their ids. Use `selection set <id...>` to show the user parts you found.

When every Vulkan device renders on the CPU, as inside WSL, where Ubuntu 22.04 has only `llvmpipe`, `ui open` refuses with `UI_ATTACH_FAILED` and the session stays headless, because a window on that renderer crashed the session host. Screenshots still work there but take up to a minute, and the CLI waits for them and warns that the render ran on the CPU; `session info` reports the renderer as `softwareRenderer`. To look at the scene from WSL, use the Windows build (`transform.exe`), which takes `/mnt/c/...` paths as `C:\...`. So when `ui open` refuses for this reason, keep working headless, tell the user to look on a machine with a GPU, and don't treat a slow screenshot as a hang.

On a Windows laptop with an integrated and a discrete GPU, a window on the integrated GPU crashed the session host, so the session's `python.exe` has to be set to High performance in Settings > System > Display > Graphics. `session new` sets it for the CLI's own Python (under `~/.transform/runtimes`) when that `python.exe` has no setting yet, and says so on stderr; it never changes an interpreter other programs share. When it has another one, which the CLI leaves alone, or the session predates the CLI that sets it, `ui open` refuses with `UI_ATTACH_FAILED`; the message names the `python.exe`, and the setting takes effect for a new session.

On macOS with SDK 2026.6 or earlier, building the window crashed the session host, so `ui open` refuses with `UI_ATTACH_FAILED` and `session new --ui` starts headless with `uiError` set; the session keeps its scene. Keep working headless, and don't retry.
