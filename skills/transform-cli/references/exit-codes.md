# Exit codes and the JSON envelope

Relative paths in this file resolve to the skill directory, the folder above `references/`.

The exit codes are the Unity CLI's, so `unity transform` and `transform` agree:

| Code | Means | Examples (`errors[].code`) |
|---|---|---|
| `0` | Success | |
| `1` | The CLI could not do its own job | `SESSION_START_FAILED`, `HOST_EXITED`, `BRIDGE_UNREACHABLE`, `BRIDGE_TIMEOUT`, `SESSION_REGISTRY_INVALID`, `CONFIG_INVALID`, `FILESYSTEM_ERROR`, `INTERNAL_ERROR` |
| `2` | Bad arguments | `INVALID_COMMAND_ARGS` (also a file or skill copy in the way that `--force` replaces: ask the user before adding it), `SDK_INVALID` (a folder given as an SDK that isn't one) |
| `3` | A credential was rejected | `AUTH_FAILED` |
| `4` | Something must be set up first | `NO_SESSION`, `NO_SDK`, `NO_RUNTIME`, `MISSING_LIBRARY`, `NO_LICENSE`, `SDK_SHARED_MEMORY_LEFT`, `SDK_UNSUPPORTED`, `SESSION_OUTDATED`, `WORKFLOW_EMPTY_SCENE` |
| `6` | The operation failed | `SCRIPT_FAILED`, `COMMAND_FAILED`, `SESSION_NOT_FOUND`, `NO_GPU`, `WORKFLOW_BROKEN` and the others below |
| `7` | A service could not be reached, or the SDK's instance limit is full; a retry may help | `NETWORK_UNREACHABLE`, `SDK_INSTANCE_LIMIT` |
| `130` | Stopped by Ctrl+C | `INTERRUPTED` |
| `143` | Stopped by SIGTERM (macOS and Linux) | `TERMINATED` |

The other exit-6 codes, by what they are about:

| Area | Codes |
|---|---|
| Sessions | `SESSION_ISOLATED` (`session use` on a session `run --isolated` owns), `AMBIGUOUS_SESSION` (a name several running sessions carry: pass one of the ids in `data.candidates`), `SESSIONS_RUNNING` (`upgrade` while sessions run) |
| Files and the scene | `FILE_NOT_FOUND` (a script, model or license file the command needs), `OCCURRENCE_NOT_FOUND`, `MATERIAL_NOT_FOUND`, `STEP_NOT_FOUND` (`scene undo --to` / `scene redo --to` names no step in that stack: pick one from `data.candidates`, newest first) |
| The UI | `UI_ATTACH_FAILED`: the UI didn't open and the session is still headless and usable |
| SDKs | `SDK_NOT_FOUND`, `SDK_IN_USE` (a running session uses it, or it is the default), `API_NOT_FOUND` (`sdk api <module>.<name>` names nothing: try `data.candidates`, or with several names each `data.notFound[].candidates`), `VERSION_NOT_FOUND`, `DOWNLOAD_FAILED`, `CHECKSUM_MISMATCH`, `RUNTIME_SETUP_FAILED` |
| Licensing and the machine | `LICENSE_SERVER_UNREACHABLE`, `DOCTOR_FAILED` (`doctor --ci` found a blocking problem) |
| Workflows | `WORKFLOW_NOT_FOUND`, `WORKFLOW_INVALID`, `WORKFLOW_BROKEN` |
| The install | `UPGRADE_UNSUPPORTED` (`upgrade` can't update this install: `data.upgradeWith` names what can, `unity plugin upgrade transform` under the Unity CLI or `git pull` in a source checkout, and is `null` where no build is published for the platform), `SKILL_INSTALL_UNSUPPORTED` (use `unity skill install`: `data.installCommands` gives the command for each client asked for, `--local` for the project) |

## The envelope

Under `--json` (or `--format json`, the Unity CLI's spelling) every command prints one envelope on stdout, success or failure, and nothing a script should read on stderr:

```json
{ "success": false, "command": "run", "data": { "output": "Exception: ValueError: boom\n..." },
  "errors": [{ "code": "SCRIPT_FAILED", "message": "Exception: ValueError: boom" }], "warnings": [] }
```

- Switch on `errors[].code`, never on the message, because the wording changes between releases.
- A failure keeps what helps fix it in `data`: a failed script's output and traceback in `data.output`, the running sessions in `data.candidates` when the one named doesn't exist, the materials of that name in `data.candidates` when a material name matches several, or the closest command in `data.suggestion` and the others at that level in `data.candidates` for a mistyped one (a command that closes, resets, clears, deletes, removes or uninstalls something, or activates a license or upgrades the CLI, only for a single typo of its name; a removed spelling suggests its replacement, which can be in another group).
- `command` is the command group that ran (`run`, `scene`), and `null` when the command line names none that exists.
- Advisories go in `warnings`, never on stderr.
- A command stopped by Ctrl+C or SIGTERM prints the `INTERRUPTED` or `TERMINATED` envelope under `--json`. On macOS and Linux it then dies by the signal, so a shell reports `130` or `143` and a parent that reads the wait status (Python's `subprocess`, Node's `child_process`) sees the signal; on Windows, Ctrl+C exits `130`. An isolated session the command started is closed first; other sessions keep running.

## Acting on a failure

| Exit | Code (`errors[0].code`) | Cause | Action |
|---|---|---|---|
| `6` | `SCRIPT_FAILED` | The script raised inside the SDK; its traceback is in `data.output`. | Fix the Python. Don't restart the session, because it is healthy. |
| `6` | `NO_GPU` | The operation renders and the machine has no graphics driver. | Skip the step and say so in the report. |
| `6` | `SESSION_NOT_FOUND` | The session named isn't running; the message says whether it was closed or its host exited. | If its host exited, read `session logs --session <id> --all` for why. If you created it, create it again and re-import; otherwise tell the user. Don't switch to one of `data.candidates`, because they may be other jobs'. |
| `1` | `HOST_EXITED`, `SESSION_START_FAILED`, `BRIDGE_UNREACHABLE` | The host or its bridge failed. | Read `unity transform session logs --session <id> --all`, then run `unity transform doctor --json`. |
| `2` | `INVALID_COMMAND_ARGS` | The arguments are wrong for this build. | Read `<group> <command> --help` and fix the arguments. |
| `4` | `NO_LICENSE` | The SDK has no license to run with. | Run `license status --json` (exit 0 with `licensed: false` and the SDK's reason in `invalidReasons`) and tell the user. Ask before `license server set <host:port>` (a floating license, the usual way) or `license activate <path>` (a license file), because both change the machine's license. |
| `4` | `SDK_SHARED_MEMORY_LEFT` | Shared-memory objects an earlier SDK process left are in the way (on macOS, usually root's, left by the license installers an earlier `license activate` ran under `sudo`); the license itself may be fine. | Don't run `license activate`. Tell the user: once no session (`unity transform session list`) and no other program using the SDK runs, the command the message names removes them (it needs root, so ask first), or restarting the machine does. |
| `6` | `LICENSE_SERVER_UNREACHABLE` | The floating license server set can't be reached (no connection to its host and port); the SDK's reason is in the message. A server that answered and gave no seat is `NO_LICENSE` instead, and `license server status` answers it with exit 0, `reachable: true` and `licensed: false`. | Tell the user the reason. Ask before `license server set <host:port>` with the right address or `license server unset`, because both change the machine's license. |
| `4` | `NO_SESSION`, `NO_SDK`, `NO_RUNTIME`, `MISSING_LIBRARY` | Something must be set up first. | Do the setup the message names, such as starting a session or installing an SDK. |
| `6` | `UPGRADE_UNSUPPORTED` | `upgrade` can't update this install; `data.upgradeWith` names what can. | Tell the user to run it (`unity plugin upgrade transform` under the Unity CLI). Don't retry `upgrade` or replace the install another way. |
| `7` | `NETWORK_UNREACHABLE` | A service could not be reached. | Retry once, then tell the user. |
| `7` | `SDK_INSTANCE_LIMIT` | The SDK already runs as many times on this machine as its license allows; the license itself is fine. | Close a session you started and no longer need, then retry. Don't close sessions you didn't start, because they may be other jobs'; tell the user instead. Don't touch the license. |
