# Named SDKs

Relative paths in this file resolve to the skill directory, the folder above `references/`.

Every SDK the CLI knows has a name, and the name is what you type. Run `unity transform sdk --help` and `unity transform sdk <command> --help` for the flags this build takes.

## Scopes

| Scope | Command | Lifetime |
|---|---|---|
| Global default | `unity transform sdk use <name>` | Until changed |
| One session | `unity transform session new --sdk <name>` | That host's life |
| One run | `unity transform run file x.py --isolated --sdk <name>` | The session the run creates (`--isolated`, or none running), closed after it; in a running session `--sdk` is ignored with a warning |

## Get an SDK

1. Run `unity transform sdk status --json` to see what is installed.
2. If the user wants the newest published build, run `unity transform sdk install`. It downloads, verifies, names the build after its version and makes it the default.
3. If the user already has an SDK folder (a local build, for example), run `unity transform sdk install --path <dir> --name <name>`. It registers the folder in place and copies nothing, so a dev build keeps tracking its own rebuilds. `<dir>` is the SDK root or its `bin/`; a folder with no `pxz.pyd` / `pxz.so` fails with `SDK_INVALID` (exit 2). The name can't be a version (such as `2026.5.0.0`) or `latest`, because a download claims those: use `2026.5.0.0-local` or `dev`.
4. Run `unity transform sdk status --json` again and check the name appears with the expected path, and its Python runtime is `ready`. A `missing` runtime comes with the command that sets it up without downloading the SDK again.

`sdk install` makes the new SDK the default; `--no-use` keeps the current default. Pass `--no-use` when the user only wants to try or compare a build, because every later session started without `--sdk` would otherwise run it. `sdk status` checks for a newer published build every time it runs (`updateAvailable`, `latest` in `data`); the other commands read a cache refreshed at most once a day. The CLI's own downloads go through `HTTPS_PROXY` / `HTTP_PROXY` / `ALL_PROXY` and honor `NO_PROXY`, read from the process environment only, not `~/.transform/.env`.

## Rules

- A string that could be a name or a path is read as a name first, because a stray `./dev` folder must not hijack the `dev` SDK.
- `sdk api` reads `--sdk <name>`, else the SDK of the session `--session` names, else the default SDK, so pass `--session <id>` to read the build your session runs. It needs no session and never waits on a busy one. `sdk api <module>` lists a module. `sdk api <module.name> <module.name> ...` answers several names in full from one start of the SDK, so look a script's calls up together. `sdk guide` is the same text for every build; with `--session <id>` it also names that session's SDK version.
- `sdk install` and `sdk use` don't re-point running sessions, because a host keeps its SDK for life. Close and recreate the sessions they name, otherwise you are testing the old build.
- `sdk uninstall` on a `managed` SDK deletes its files; on a `linked` SDK it only forgets the name. Check the kind in `sdk status` before uninstalling. Neither is removed while a session uses it, nor the default without `--force` (`SDK_IN_USE`).

## Licenses

Downloading a build doesn't license it, and a session checks its license as it starts. One license covers every SDK build on the machine, so a new build needs no license step.

If a session fails at startup with `NO_LICENSE`, run `unity transform license status --json` and tell the user what it reports. Both fixes change their machine, so ask first:

1. Usually: a floating license from the organization's license server, `unity transform license server set <host:port>` (FlexLM by default; `--type pixyz` or `--type unity` for others), then `unity transform license server status` to check it gives a seat: `reachable: true, licensed: true` if it does, `licensed: false` with the SDK's reason (exit 0) if it answered with no seat, `LICENSE_SERVER_UNREACHABLE` (exit 6) if it can't be reached. `license server unset` goes back to the installed license: a session's server replaces the machine's license file, and `unset` puts back the copy kept in `~/.transform/license-backup`.
2. Otherwise: a license file the user received, installed with `unity transform license activate <path>` (a `.lic`, or a `.bin` activation file; the path is positional). It needs administrator rights, a UAC prompt or `sudo`, so with prompts forbidden, or under `--json` on macOS and Linux, it fails: the user runs it themselves from an elevated terminal, or after `sudo -v` on macOS and Linux. Never copy a license file into the SDK's license folder by hand.

## Validate a local SDK build

1. Register each build under its own name, with `--no-use` so the user's default SDK stays as it was: `unity transform sdk install --path <dir-a> --name before --no-use` and `unity transform sdk install --path <dir-b> --name after --no-use`.
2. Create one session per build: `unity transform session new --name a --sdk before` and `unity transform session new --name b --sdk after`.
3. Run the same steps against both with explicit `--session`, and compare `scene info --json` output.
