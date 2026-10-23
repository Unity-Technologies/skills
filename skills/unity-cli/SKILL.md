---
name: unity-cli
description: Controls the Unity Editor and projects from the command line, driving a running Editor to edit scenes and assets or run C#. Use when asked to install or manage Editors, licenses, or projects, set up version control, build, test, configure the Unity MCP server, or run any unity command.
allowed-tools:
  - Bash
---

# Unity CLI

**Always use the latest version of the Unity CLI unless you are told to use a
specific version.** Newer releases regularly add commands and output fields
these docs describe, so an outdated CLI fails in ways that read like the docs
being wrong. Check with `unity self-update --check --format json`.

## Find a command: search, don't grep help

**Never filter `unity --help` or a group's help with `grep`.** Help is long, and a grep over it drops the match or the context around it. Use the CLI's own search:

```bash
unity commands --grep license                 # built-in commands, plugins and the connected Editor's Pipeline commands
unity command --query screenshot              # the same search
```

`unity commands --grep <pattern>` and `unity command --query <term>` are one search. It covers the CLI's built-in commands, the plugin catalog (installed or not) and the Pipeline commands of the live connected Editor, best match first. Plain words each match in any order; regex characters make it a regular expression. Each row says how to run it (`invoke`): `unity projects link cloud`, `unity command create_gameobject`, `unity <id>` for an installed plugin, or `unity plugin install <id>` for one not yet installed. In `--format json` each row also carries `kind` (`command`, `plugin`, `pipeline`), `path`, `description` and `matchedOn` (`name`, `description` or `option`), so act on `invoke` rather than parsing the table.

- Use `unity command --query <term>` to search a specific Editor (`--project-path <project>`) or a Player (`--runtime <pattern>`, e.g. `unity command --query screenshot --runtime MyGame`).
- `commands --grep` waits at most 3 seconds for the Editor; `command --query` waits for it. When the Editor was not searched, a last line says why (no Editor, no answer in time, several Editors) and what to run. Wait for the Editor first with `unity status --until-ready --project-path <project>`: before it connects, no Pipeline row is not proof nothing exists.

Fields and flags: [integration-advanced.md](references/integration-advanced.md#querying-the-command-list).

## Drive a running Unity Editor (if one is open)

**If a Unity Editor is open on this machine, this CLI can control it live** — create and modify GameObjects, edit scenes and assets, inspect the hierarchy, and run arbitrary C# — through the project's **Pipeline** package (`com.unity.pipeline`). This runs entirely on your local machine, in your own user account, against your own open Editor: it is not remote access and grants no privilege you don't already have at your own terminal. When an Editor is available, drive it instead of hand-editing scene or asset files.

```bash
unity status                    # confirm a connected Editor (look for state "ready")
unity command                   # list the commands the Editor exposes
unity command editor.playmode start   # run one — e.g. enter Play mode
# Run arbitrary C# — e.g. add a GameObject named "Joe" — when the Editor exposes script eval:
unity command script eval 'new UnityEngine.GameObject("Joe");'
```

> **Verifying a running game, not just editing a scene?** Entering Play mode is setup, not proof:
> an unfocused Editor or a locked desktop can freeze at frame 1 while `unity status` still reports it
> as playing, and a screenshot can render a frozen frame that looks alive, or a flat color. The
> ordered recipe for playing, confirming the game is actually advancing, capturing the view, reading
> the console, and tuning it live with `script eval` is in
> [playmode-verification-loop.md](references/playmode-verification-loop.md).

### More than one Editor open? Pass `--project-path`

Every Editor-driving command takes `--project-path <path>`. **Pass it whenever more than one Editor may be running** — without it the CLI targets the Editor whose project contains the current directory, so the target follows the shell's cwd:

```bash
unity command editor.playmode start --project-path /path/to/MyProject
```

To pin one Editor when several share a project (worktrees, duplicate projects, Multiplayer Play Mode players), `unity command`, `list`, `job` and `mcp` also take `--port <number>`, `--pid <number>` or `--instance <name>` (the MPPM instance name or `vpId`); a pin that matches no running Editor exits 12 with `EDITOR_NOT_RUNNING`. A `unity status` instance's `project` field is what `--project-path` takes. For `unity command`/`list`/`job`/`mcp`, matching no running project fails with `AMBIGUOUS_EDITOR` and lists the candidates. [Details](references/integration-advanced.md#targeting-one-of-several-running-editors).

**Working in several worktrees or agent sessions? One Editor per worktree.** Make each worktree with `unity vcs git worktree add <branch> --into <path>`, so you choose where it lands. `--into` is optional: `--format json` reports the real worktree and project paths, and only the human output masks the account name in a home-directory path. The Unity project is `<path>` when it sits at the repository root, and `<path>/<its folder>` when it doesn't. Open that project's own Editor with `unity open <project>`. Then pin every call that reaches the Editor to that project: pass `--project-path <full project path>`, or prefix the call with the variable, as in `UNITY_PROJECT_PATH=<full project path> unity command editor status`. Most agent tools run each command in a fresh shell, so an `export` or a bare assignment in one call never reaches the next one. `unity open`, `status`, `command`, `list`, `job` and `mcp` read the variable when you pass no path. `recompile`, `close` and `mcp configure` don't read it: pass the path to those. Without the variable, a bare `unity open` opens the project of the current directory, which is still your source checkout. Target that Editor by the same full path every time, including `unity status --until-ready --project-path <project>`. A path that exists matches only that project, while a bare name also matches a sibling whose path contains it. Never drive an Editor whose project is not your own checkout (or one of its Multiplayer Play Mode clones). An Editor open on a sibling worktree answers every command just like yours. A command sent there edits that checkout's scenes, and its Play mode, recompiles and imports run against that checkout's code. If your own Editor isn't open, open it. If it has gone away, tell the user rather than borrowing the one that is running.

Requires the project's `com.unity.pipeline` package (Unity 6.0+) — add it once with `unity pipeline install`. Full details — launching a headless Editor to drive, `unity list` tool discovery, and authoring custom `[CliCommand]` tools — are in [integration-advanced.md](references/integration-advanced.md).

The package also ships a deeper `unity-pipeline` agent skill, invisible to clients inside `Library/PackageCache` — in a project with the package, run `unity skill install <client> --local` once to mirror it beside this skill.

> **Can't connect / commands time out? Check for Safe Mode first.** When a project has C# compile errors, the Editor boots into **Safe Mode**, where the Pipeline package doesn't load, so `unity command`, `unity list`, and `unity recompile` can't connect at all (`unity status` doesn't connect for this: it reads the Editor's log and reports Safe Mode). Note what that means for `unity recompile` specifically: it reports errors you introduce into an Editor that is **already running**, but an Editor that *started* with broken code never loads the package, so there is nothing to ask and it exits `7` rather than reporting the errors. Don't fall back to blind file-editing: run `unity status` (it reports Safe Mode and lists the compile errors, `safeMode.compileErrors` under `--format json`), then fix the errors: the Editor leaves Safe Mode once they compile. Full recovery loop in [integration-advanced.md → Recovering from Safe Mode](references/integration-advanced.md#recovering-from-safe-mode-connection-fails-because-of-compile-errors).

> **Running as a sandboxed coding agent and `unity status` reports no instances?** A restrictive sandbox can hide an Editor that is genuinely running from this CLI's view of it — don't treat that alone as proof the Editor is down. Full detail in [integration-advanced.md → Sandboxed agent tooling can hide a running Editor](references/integration-advanced.md#sandboxed-agent-tooling-can-hide-a-running-editor).

> **A call timed out, or the Editor's lifecycle failed? Tell the user, don't silently work around it.** First rule out [Safe Mode](references/integration-advanced.md#recovering-from-safe-mode-connection-fails-because-of-compile-errors), a sandbox, and an Editor that is still importing or loading. Read `errors[0].code` under `--format json`, and give a slow import a longer `--timeout`. If it is still one of these, report it:
>
> - a command that times out
> - an Editor that never reaches `ready` under `unity status --until-ready` (exit 6)
> - a connection that drops mid-command
> - an Editor that quits or restarts under you
> - a `unity open` that never produces an Editor
>
> Don't quietly retry in a loop, switch to another Editor, or fall back to hand-editing scene and asset files. Say what you ran, what failed and what you already tried. Then offer to file it with `unity bug --area pipeline-package` (`--area editor` when the Editor itself quit or never started), and ask first: `unity bug` sends logs to Unity, so never run it on your own initiative.

## Install the CLI (if not already installed)

First check if the CLI is available:

```bash
which unity && unity --version
```

If not found, install it:

**macOS / Linux**
```bash
curl -fsSL https://unity.com/install.sh | bash
```

**Windows (PowerShell)**
```powershell
irm https://unity.com/install.ps1 | iex
```

After installing, open a new shell so `unity` is on PATH, then verify with `unity --version`. If the install script fails or the binary is still not found, tell the user and stop; if the command itself fails with a permissions error or crash, the installation may be broken — suggest re-running the install script.

---

## Global flags

These work on every command:

| Flag | Description |
|---|---|
| `--format <fmt>` | Output format: `human` (default), `json`, `tsv`, `ndjson`, `github`. Also via `UNITY_FORMAT` env var. |
| `--json` | Global shorthand for `--format json`, accepted on every command (e.g. `unity status --json`, `unity doctor --json`). Passing it with a different `--format` (e.g. `--json --format tsv`) is a usage error (exit 2); `--json` overrides `UNITY_FORMAT`. |
| `--no-banner` | Suppress the branded header — use in scripts |
| `--no-header` | Leave the header row out of `tsv` tables. The header row is the column names, which are the same keys the `json` rows use (`version`, `upgradeTo`, ...). An empty `tsv` result is the header row alone, so with `--no-header` it is no output at all. |
| `--no-pager` | Turn off paging. Governs both pagers: the external one over the long listings (`unity command`, `releases`, `editors`, `changelog`, `logs`) and the interactive one in `unity projects list`. Also via `UNITY_NO_PAGER` (presence-based — any value, including `0`, disables it). |
| `--non-interactive` | Disable all interactive prompts — use in CI |
| `--quiet` | Hide notices, warnings and progress in human and tsv output. Errors and the exit code stay. json output never changes, and ndjson output does not either, except that a few listings still drop their rows (see [Notes](#notes)). |
| `--verbose` | Print full error details (stack trace + cause chain) on failure. Also via `UNITY_VERBOSE`. |
| `--proxy <url>` | HTTP/HTTPS/SOCKS/PAC proxy URL for this invocation. Also via `UNITY_PROXY`. Takes precedence over standard `HTTPS_PROXY`/`HTTP_PROXY`/`ALL_PROXY` env vars and the persisted `proxy.json` setting. |
| `--proxy-disable` | Disable proxy for this invocation, ignoring all sources (env vars, persisted config, system settings). |
| `--log-proxy` | Log one redacted entry per outbound request to `proxy-request.json` — for reproducing proxy issues. Also via `UNITY_LOG_PROXY=1` or the `proxyRequestLogging` setting. |
| `--no-log-proxy` | Opt a single invocation out of proxy request logging when it's enabled globally. |
| `--color <auto\|always\|never>` | Control colored output for this invocation, overriding `NO_COLOR`/`FORCE_COLOR` and TTY auto-detection. Governs every ANSI-emitting surface (help, tables, spinners, errors), not just `human` output. |
| `--no-color` | Shorthand for `--color never`. Whichever of `--color`/`--no-color` appears last on the line wins. |

**Always use `--format json` when you need to parse output programmatically.** If you read `tsv` instead, select columns by header name, not position: a command's `tsv` columns keep their names and order, and a flag that adds information (such as `editors --long`) only appends columns at the end. Failures in `tsv` go to stderr, so stdout holds only data.

`--accelerator <host:port>` and `--no-accelerator` are **not** root globals — they are accepted only on `run`, `test` and `build`, and only after the command name. See [build-run-test.md](references/build-run-test.md).

**`unity projects list` is the only command that pages IN-PROCESS.** It shows 10 projects per screen and waits for a keypress between screens, and only when stdout is a terminal. Paging is off for redirected stdout, under `--format json` and `--format ndjson`, and under `--all`, `--watch`, or `--no-pager` / `UNITY_NO_PAGER`.

**Not every machine format bypasses that one.** Only `json` and `ndjson` get their own non-interactive rendering; on a terminal, `--format tsv` and `--format github` fall through to the human table and page like `human` does — so `--format tsv` on a TTY yields neither TSV nor unpaged output. Redirect stdout (the usual case for a machine format) or pass `--no-pager`. Note this is the **opposite** of the external pager below, which is `human`-only: the two mechanisms differ here, and `projects list` is the surprising one.

**The long listings page through an external pager, like `git log`.** `unity command` (the bare listing), `unity releases`, `unity editors`, `unity changelog`, and `unity logs` pipe human output through `less -RFX` on a terminal; `-F` quits by itself when the output fits one screen. `$UNITY_PAGER` then `$PAGER` override the choice and run through a shell, so `PAGER="less -S"` works; a blank value is ignored rather than treated as an opt-out. Unlike `projects list`'s pager this one is **`human`-only**, and it never engages for redirected stdout, any machine format (`json`, `tsv`, `ndjson`, `github`), `--quiet`, `TERM=dumb`, the streaming modes (`editors --watch`, `logs --follow`), a named `unity command <name>`, or inside `unity shell`. A broken pager costs the paging, not the output: a `$PAGER` naming something that is not there is resolved before anything spawns, and one that spawns and then dies has its output reprinted to the terminal, decided from the pager's exit status (a clean exit is a normal `q` and discards; a failure status reprints). A pager that exits 0 without reading (`PAGER=true`) is indistinguishable from `q`, so its output is lost. One that merely *waits* is not broken; the CLI waits with it.

A branded Unity header (logo, wordmark, CLI version) renders on the landing surfaces — bare `unity`, `unity --help` / `-h`, `unity help`, and above the first-run consent prompt. It's shown only on a TTY, prints at most once, and degrades to compact, uncolored text on narrow terminals, without Unicode, or under `NO_COLOR`. Piped output is unaffected. Use `--no-banner` to suppress it in scripts. Bare `unity` prints usage and exits 0.

## Environment variables

All CLI env vars use the `UNITY_` prefix. A CLI flag always overrides the corresponding env var.

| Variable | Mirrors flag | Description |
|---|---|---|
| `UNITY_FORMAT` | `--format` | Output format (`human`, `json`, `tsv`, `ndjson`, `github`). `HUB_FORMAT` is a deprecated alias. |
| `UNITY_EDITOR_VERSION` | `--editor-version` | Editor version (e.g. `2023.3.0f1`, `latest`, `lts`). |
| `UNITY_ARCHITECTURE` | `--architecture` | Chip architecture (`x86_64`, `arm64`). |
| `UNITY_PROJECT_PATH` | path argument | Project path when none is passed. Honored by `open` (and `projects open`), the Editor-connecting commands `status`, `command`, `list`, `job` and `mcp`, plus `pipeline install`/`upgrade` and the `collaboration` commands. Not read by `recompile`, `close` (and `projects close`), or `mcp configure`. |
| `UNITY_QUIET` | `--quiet` | Suppress non-essential output. |
| `UNITY_VERBOSE` | `--verbose` | Show full error details on failure. |
| `UNITY_NON_INTERACTIVE` | `--non-interactive` | Disable interactive prompts. |
| `UNITY_NO_BANNER` | `--no-banner` | Suppress the branded banner. |
| `UNITY_NO_PAGER` | `--no-pager` | Turn off paging — both the external pager over the long listings and `unity projects list`'s interactive one. Presence-based: any value counts, including `0`. |
| `UNITY_PAGER` | — | The pager to use for the long listings, overriding `$PAGER` and the `less -RFX` default. Runs through a shell, so flags work (`less -S`). A blank value is ignored, not an opt-out. |
| `PAGER` | — | Same as `UNITY_PAGER`, consulted only when that is unset or blank. |
| `LESS` / `LV` / `LESSCHARSET` / `MORE` | — | Passed to the pager only when you have not set them, defaulting to `FRX`, `-c`, `utf-8`, and `FRX`. `LESSCHARSET` keeps multi-byte glyphs readable where the locale does not declare UTF-8; `MORE` exists because `more` on macOS/BSD is `less` under another name and reads `$MORE`, so without it `PAGER=more` waits for a keypress even for one line. |
| `UNITY_RUN_TIMEOUT` | `--timeout` | Timeout for `unity run` in seconds. |
| `UNITY_TEST_TIMEOUT` | `--timeout` | Timeout for `unity test` in seconds. |
| `UNITY_CLOUD_ORG` | `--cloud-org` | Active Unity Cloud organization id or name for a single call. |
| `UNITY_CLOUD_PROJECT` | `--cloud-project` | Cloud project ID or name (a service account needs the ID); used by Cloud Build inventory. Pipeline Automation inventory is organization-scoped. |
| `UNITY_NO_CLOUD` | `--no-cloud` | Decline the Unity Cloud link on `projects create` and `projects upgrade`. Presence-based: any value counts, including `0`. On `projects create` and `projects upgrade`, an explicit `--cloud`, `--cloud-project`, `--cloud-org` or `--coppa` wins over it. `projects create --vcs uvcs` is refused with it set unless one of those flags overrides it. |
| `UNITY_SERVICE_ACCOUNT_ID` | — | Service account client ID for non-interactive (CI) auth. |
| `UNITY_SERVICE_ACCOUNT_SECRET` | — | Service account client secret for non-interactive (CI) auth. |
| `UNITY_PROXY` | `--proxy` | HTTP/HTTPS/SOCKS/PAC proxy URL. Takes precedence over `HTTPS_PROXY`/`HTTP_PROXY`/`ALL_PROXY` and the persisted `proxy.json` setting. |
| `UNITY_NO_UPDATE_CHECK` | — | Disable the background "update available" check (see `unity config update-check`). |
| `UNITY_NO_AGENT_HINTS` | — | Stop suggesting Unity's agent plugin or skill when an AI agent runs `unity`: Claude Code's install prompt, the `unity setup codex` / `unity setup grok` tip, and the `unity skill install` tip (an `AGENT_PLUGIN_AVAILABLE` entry in the envelope's `notifications` under `--format json`). Presence-based: any value counts. A tip asks you to get the user's approval before running its command. |
| `UNITY_NO_CONSENT_PROMPT` | — | Suppress the one-time first-run analytics consent prompt *without* recording a choice — for wrapper scripts on an interactive terminal that must never absorb the prompt. Analytics stay off until you run `unity analytics opt-in`. Unlike `UNITY_NON_INTERACTIVE`, it changes nothing else about command behavior. |
| `UNITY_NO_CRASH_REPORT` | — | Disable anonymous crash/error reporting (Sentry) entirely. |
| `UNITY_LOG_PROXY` | `--log-proxy` | Log one redacted entry per outbound request to `proxy-request.json`. Truthy values: `1`, `true`. |
| `UNITY_ACCELERATOR` | `--accelerator` | Unity Accelerator endpoint (`host:port`). Outranks the persisted `accelerator.json`; `--accelerator` outranks it. |
| `UNITY_NO_ELEVATE` | `--no-elevate` | Windows: skip the elevated (UAC) install helper for `install` / `install-modules`, so the install service runs unelevated. The Editor's NSIS installer still asks for elevation on demand if Windows requires it for your account — an administrator token always does; a standard user never does. |
| `UNITY_INSTALL_RETRIES` | `--retries` (`install-modules` only) | Number of times `install` and `install-modules` retry an editor or module download whose transfer or validation fails. `0` disables retries; `unity install` has no `--retries` flag, so set the variable there. |
| `UNITY_NO_AUTH_BROKER` | — | Skip the resident auth broker and read credentials directly from the OS keyring. By default every command that needs a token goes through a broker that starts on demand and exits after two idle minutes (see [auth-license-cloud.md](references/auth-license-cloud.md)). |
| `UNITY_CLI_HOME` | — | Install root for the install script and `unity self-install`, on every platform including Windows: the binary lands in `<UNITY_CLI_HOME>/bin` instead of the default location. |
| `UNITY_NO_EDITOR_IDENTITY_SERVER` | — | Disable the background identity helper that `unity open` starts to answer the Editor’s sign-in lookups when no Hub is running (see [projects-templates.md](references/projects-templates.md)). Presence-based. |
| `UNITY_EXPERIMENTAL_FEX_EMU` | none | Linux arm64 only, experimental and unsupported: run the x86_64 Editor under FEX-Emu. `1`, `true` or `yes` turns it on, and it takes effect only when FEX-Emu and an x86_64 RootFS are installed (`unity doctor` checks). C# script compilation is known to crash under FEX-Emu. See [editors-install.md](references/editors-install.md). |

**CI service account auth:** Set both `UNITY_SERVICE_ACCOUNT_ID` and `UNITY_SERVICE_ACCOUNT_SECRET` to skip the browser OAuth flow — this keeps the secret out of the process argument list and shell history. These map to the `--client-id` / `--secret-from-stdin` inputs of `unity auth login`, but reading the credentials from the environment isn't a full login: it doesn't run the interactive flow or persist credentials to the keyring.

## Getting help

Append `-h` or `--help` to any command or subcommand, at any level: `unity --help`, `unity projects create --help`.

Not sure which command? [Search, don't grep help](#find-a-command-search-dont-grep-help). `unity --help` and the unknown-command error end with that pointer too. Each search ends by naming the source it skipped and the command that searches it (`data.hint` in JSON): follow it before concluding nothing exists.

## Exit codes

Branch on the exit code, then on `errors[0].code` in the json or ndjson envelope. **Retry** says
whether running the same command again, unchanged, can succeed. `unity commands --json` carries
this table as `exitCodes`, with the error codes that pick each exit. `6` covers every failure no
more specific code names; a missing project, an Editor that isn't running, an unknown Editor
command and a timeout have their own codes (`11` to `14`).

<!-- exit-codes:begin (checked against the CLI's exit-code table) -->
| Code | Name | Retry | Meaning |
|---|---|---|---|
| 0 | `SUCCESS` | no | The command succeeded. |
| 1 | `ERROR` | no | An unexpected or unsupported state. |
| 2 | `BAD_ARGS` | no | A usage error: an unknown command or option, or a bad value. |
| 3 | `AUTH_ERROR` | no | Not signed in, the session expired, or permission was denied. |
| 4 | `NEEDS_CONFIG` | no | Setup is missing: a required tool, setting or link. |
| 6 | `COMMAND_FAILED` | no | The command failed for a reason no more specific code names. |
| 7 | `NETWORK_UNREACHABLE` | yes | A required service could not be reached. |
| 8 | `TESTS_FAILED` | no | The tests ran, and some failed. |
| 9 | `INSTALL_LOCKED` | yes | Another process holds the install lock. |
| 10 | `READY_TIMEOUT` | yes | The Editor did not become ready in time. |
| 11 | `NOT_FOUND` | no | What the command names does not exist: a project, a release, a module. |
| 12 | `EDITOR_UNREACHABLE` | yes | No running Editor answered: none is running, none matches, or it refused the connection. |
| 13 | `REMOTE_COMMAND_FAILED` | no | The Editor answered, but it has no command by that name. |
| 14 | `TIMEOUT` | yes | An operation ran out of time: an Editor request, an HTTP request, a batch run. |
| 130 | `INTERRUPTED` | yes | Interrupted by Ctrl-C (SIGINT). |
| 141 | `OUTPUT_CLOSED` | no | The reader of stdout closed the pipe. |
| 143 | `TERMINATED` | yes | Terminated by SIGTERM. |
<!-- exit-codes:end -->

`8` is `unity test` only: the tests ran and some failed. A run that never completed exits `6`
and a timed-out one `14`, so CI can retry those and never blindly re-run a failing test. `9`:
earlier items of the same install may already be in place (`data.completedUids`). `10` is
`unity open --until-ready` only; the Editor is still running.

## Commands

The full per-command reference — syntax, flags, and examples — lives in grouped files under
[`references/`](references/). **Read the file for the command group you need**; all the global
flags, environment variables, and exit codes above apply throughout. Every command also supports
`-h` / `--help` (see [Getting help](#getting-help)).

| Commands | Reference file |
|---|---|
| `auth` (login / logout / status / list / switch / default / consumers / revoke), `license` (activate / return / server), `cloud` (org / project) | [auth-license-cloud.md](references/auth-license-cloud.md) |
| `pipeline cloud-build` (targets incl. groups / builds / project / tooling), `pipeline automation` (apps / pipelines / jobs / automations / bots / profiles / templates, plus `apps versions`, `pipelines versions`, `jobs stats`) | [cloud-automation.md](references/cloud-automation.md) |
| `editors` (list / running / add / default / path / install-path / info / upgrade / prune / verify / module), `install`, `uninstall`, `modules`, `install-modules`, `terms` (accept / status) | [editors-install.md](references/editors-install.md) |
| `projects` (list / create / new / clone / open / link / require / upgrade / export / import / pin / size / clean / exec; its help reads "Create, open, upgrade, and link local Unity projects (Unity Cloud, GitHub/GitLab)", so linking a folder to Unity Cloud is `projects link cloud`, not `cloud`), `open`, `close`, `releases`, `templates` (list / info / create / pack / delete), `assets` (`inspect` / `export`) | [projects-templates.md](references/projects-templates.md) |
| `config` (proxy / update-check / accelerator / get / set / list / unset / resolve), `context` (save / use / list / current / delete), `hub install` | [config-hub.md](references/config-hub.md) |
| `run`, `test`, `build` (+ `build run`), `recompile`, `watch` (`test`) | [build-run-test.md](references/build-run-test.md) |
| `logs`, `doctor`, `env`, `version`, `cache`, `ci init`, `analytics`, `changelog`, `docs` (+ `toc`), `language`, `completion`, `bug`, `self-update`, `self-uninstall`, `diagnose proxy`, `diagnose accelerator`, `diagnose update` | [diagnostics-maintenance.md](references/diagnostics-maintenance.md). To find, cite and version-match Unity docs: [docs-workflow.md](references/docs-workflow.md) |
| `mcp` (+ `configure`), `setup claude` / `codex` / `grok` / `antigravity`, `skill` (install / refresh / show), `plugin` (install / remove / upgrade / list / changelog), local `pipeline` (install / upgrade / list / list-versions), `command` / `commands` / `status` / `list`, `job` (status / wait / cancel), answering an open modal dialog (`command editor.dialog list` / `answer` / `dismiss`), `shell` | [integration-advanced.md](references/integration-advanced.md) |
| `vcs` — `setup` / `status` / `sync` / `switch` / `doctor` / `providers` / `merge-setup` / `conflicts` / `explain` / `resolve` / `diff` / `blame` / `summarize` / `affected` / `hooks`, `vcs git` (`migrate-lfs` / `worktree`), `vcs uvcs` (`locks` / `changesets` / `review`) | [version-control.md](references/version-control.md) |
| `collaboration` (alias `collab`) — `annotations` / `attachments` / `thumbnail` / `reactions` / `read` / `subscribe` / `jira` | [collaboration.md](references/collaboration.md) |

## Common workflows

### Inspect cloud builds or Pipeline Automation resources

Read [cloud-automation.md](references/cloud-automation.md) for every read-only
command in both groups, context/authentication, per-command filters and sorting,
output fields, pagination (including the leaves that refuse `--page`/`--limit`
rather than ignoring them), errors, and redaction. `pipeline cloud-build` reads Build Automation;
`pipeline automation` reads Pipeline Automation. Neither needs a running Editor
or the local Pipeline package. Use numeric organization IDs for service accounts.
These commands don't trigger builds/jobs, fetch logs/artifacts, or change
configuration.

JSON/NDJSON return full API-shaped results under `data`, as Collab does; NDJSON
has one terminal result, without item frames. Preserve native fields and free-form
metadata, subject to the reference's bounded secret protections and public-API
redaction assumption. Table projections remain separate. Explicit local build
targets retain `_local`; missing targets aren't local. These conventions apply
to follow-up cloud-automation work too.

### Edit a scene, GameObject, or asset — `unity status` first

**Before editing any scene, GameObject, prefab, or asset, run `unity status` to detect a connected Editor.** If one is reachable, drive it with live commands instead of touching project files — the Editor applies changes to the *actual active scene* and keeps its in-memory state in sync.

```bash
unity status                       # is an Editor connected? (look for state "ready")
unity command                      # discover the scene/GameObject commands THIS Editor exposes
# then drive it with the commands it lists — for example, if your Editor exposes them:
unity command gameobject create   # act on the live, active scene
unity command scene save          # persist the active scene
```

Commands are grouped and invoked as `unity command <group> <name>`. Their names are defined by the Editor, so run `unity command` (or `unity list`) to see the exact set — don't assume a name.

> **Never hand-edit `.unity`, `.prefab`, or `.asset` YAML while a live Editor is reachable.** Raw-file edits are:
> - **error-prone** — fileIDs and GUIDs are assigned by hand and easy to get wrong;
> - **invisible** to the running Editor until a reimport, so the change silently fails to take effect; and
> - **prone to hitting the wrong file** — e.g. writing to `SampleScene.unity` while the Editor's active scene is actually `Demo2.unity`, producing valid-looking YAML that changes nothing the user sees.

**Rule out two false negatives before concluding no Editor is reachable — both look identical to a genuinely closed Editor, and both are easy to get wrong under time pressure:**

- **Safe Mode.** If an Editor *is* running for this project but `unity command` won't connect, it may be stuck in **Safe Mode** from a compile error rather than genuinely absent. Run `unity status`: if it reports Safe Mode (`STATUS_SAFE_MODE`, with the compile errors), editing the C# source to fix the compile errors *is* the correct move, not a fallback. See [integration-advanced.md → Recovering from Safe Mode](references/integration-advanced.md#recovering-from-safe-mode-connection-fails-because-of-compile-errors).
- **A sandboxed agent shell.** If your own shell commands run inside a restrictive sandbox — the normal case for a coding agent like this one — the sandbox can hide a genuinely running Editor from `unity status` the same way. This applies to **every** scene/GameObject/prefab/asset task that reaches this preflight, not only ones that obviously need a live Editor: a task you could otherwise finish without any CLI involvement (e.g. generating an asset through ordinary Editor APIs) can still get funneled into "no Editor" here and derailed. Don't treat "no instances" as proof the Editor is down, and don't quietly improvise a third path — like driving a separate headless Editor process to approximate what a live connection would have done — as a substitute for a disclosed file edit. Say plainly that your sandbox may be blocking your view of a real Editor, and ask whether one is actually open before falling back. Full detail: [integration-advanced.md → Sandboxed agent tooling can hide a running Editor](references/integration-advanced.md#sandboxed-agent-tooling-can-hide-a-running-editor).

Only fall back to editing files directly once you've ruled out both of the above — and say so explicitly ("no live Editor detected, editing the file directly").

### Generate an asset, texture, sprite, image, or 3D model: check for the `ai` catalog plugin

**Before approximating a "generate a texture/sprite/image/3D model for my project" request some
other way, check whether the Unity AI Generators plugin (`ai`) is installed.** It is Unity's own
catalog plugin for exactly this class of request, and using it is almost always the better answer.
The same three-step pattern below (recognize the intent, check the catalog, ask before installing)
applies to any catalog plugin the CLI ships, not only this one; `unity plugin list` always
reflects the full catalog, installed or not, so it is how you discover what else is available too.

1. **Check whether it's installed.**
   ```bash
   unity plugin list --format json
   ```
   Find the entry whose `id` is `"ai"` and read its `installed` field (`true` / `false`).

2. **Not installed? Tell the user, then ask, never install silently.** Say plainly that Unity
   ships an AI Generators plugin that covers this, and wait for a yes before running:
   ```bash
   unity plugin install ai
   ```
   A refusal is a normal answer: fall back to whatever you would otherwise have done, and don't
   ask again in the same conversation.

   `ai` is alpha, and during alpha it's gated to Unity staff on Unity's internal network. An
   ordinary user's `unity plugin install ai` can fail with a **sign-in** message ("Unity AI
   Generators is currently limited to Unity staff. Sign in with a Unity staff account…") or, once
   past that, a plain **download failure** if the network it needs isn't reachable from where
   you're running. Neither is a bug in the request: report the message and fall back, the same as
   any other refusal.

3. **Pick up its agent skill in the same session.** A catalog plugin can ship its own agent skill
   inside its payload (`ai` does), and installing the plugin does not, by itself, make that skill
   visible to you; it has to be mirrored the same way this skill itself is:
   ```bash
   unity skill install <client>     # e.g. claude-code, first time this session
   unity skill refresh              # already mirrored earlier? re-render it fresh
   ```
   `unity plugin install`'s own last line already tells you which one applies: it points at
   `unity skill install` right after installing a copy that ships a skill, and at
   `unity skill refresh` once one is already mirrored but the plugin's copy just changed. Confirm
   where it landed with `unity skill list --format json` (look for the row whose `skill` field
   names the plugin's own skill, e.g. `unity-ai`, and read its `path`), then read that file before
   driving the plugin's commands. Don't guess its command surface from this skill, which only
   documents `unity` itself.

**A command invoked before any of this** (`unity ai …` typed straight, or suggested from memory)
**fails with a clear, actionable message** naming the exact install command (`unity plugin install
ai`), in both human and machine (`--format json` / `--format ndjson`) output, under the stable
error code `TOOL_NOT_INSTALLED`. Read that message rather than guessing why the command did
nothing; the same shape (`TOOL_UNSUPPORTED_PLATFORM`, `TOOL_RUNTIME_NOT_INSTALLED`) covers the
two other reasons a catalog tool can't run.

### Bootstrap a new project from scratch

> For a **guided** end-to-end experience — concept questions, installing the Editor in the
> background while you plan, package selection, and monetization handoff — use the
> **`new-unity-project`** skill. This section is the raw CLI recipe that skill builds on; use it
> directly when you just want the commands.

Take an idea to a running, version-controlled project using only the CLI. Decide the **target
platforms first** — they determine which Editor modules you install in step 2. You can add
modules later (`unity install-modules`), but a project can't build for a platform until that
platform's module is installed, so it's simplest to decide up front.

```bash
# 1. Confirm the CLI works and you're signed in and licensed (see references/auth-license-cloud.md).
unity --version
unity auth status --format json      # if signed out:      unity auth login
unity license status --format json   # if none active:      unity license activate

# 2. Pick and install an Editor with the modules your target platforms need.
#    Default to the latest LTS (most stable, ~2 years of patches). Reach for a Tech-stream
#    release (--stream tech) only for a feature not yet in LTS; treat --stream beta/alpha as
#    evaluation-only, never for a project you intend to ship. A deadline argues for LTS.
#    (lts / latest aliases work almost everywhere a version is accepted — `templates` is the
#     exception; see step 3.)
unity releases --stream lts --limit 5 --format json
unity install lts --module android --module ios --yes --accept-eula   # add --module webgl, etc.
unity editors --installed --format json                               # confirm it landed

# 3. List the real template ids this Editor offers — don't guess them — and pick by RENDER
#    PIPELINE, not just by 2D/3D. Default to the URP templates:
#      3D → com.unity.template.urp-blank      ("Universal 3D")
#      2D → com.unity.template.universal-2d   ("Universal 2D": URP + the 2D packages)
#    com.unity.template.3d and com.unity.template.2d are the Built-in Render Pipeline templates
#    (displayName "… (Built-In Render Pipeline)"): deprecated from Unity 6.5, gone in 6.7. Use
#    them only when the user explicitly asks for Built-in. Confirm the pick with the JSON
#    `renderPipeline` field — it is blank for universal-2d on current releases, so match that
#    one by id.
#    NOTE: `templates` does NOT resolve the lts / latest aliases — unlike `install` and
#    `projects create`, it passes --editor straight through and rejects anything that is not a
#    concrete 6000.x.y. Use the version you just installed (read it from `editors --installed`).
unity templates list --editor <6000.x.y> --type core --format json

# 4. Create the project. The first positional arg is the NAME; --path sets the parent directory.
#    All options supplied, so it won't prompt; add --non-interactive in CI.
#    (To publish it to a remote in the same step, use the source-control forms below instead.)
unity projects create "MyGame" --path ~/UnityProjects \
  --editor-version lts --template com.unity.template.urp-blank

# 5. Add the Pipeline package BEFORE the first open. `unity command`, `unity status` and
#    `unity command script eval` all need it, and templates don't include it. The Editor reads
#    Packages/manifest.json when it loads the project, so installing first means the package
#    is live from the first open.
unity pipeline install --project-path ~/UnityProjects/MyGame

# 6. Open the project and wait until its Editor is ready to take commands. Pass the project's
#    full path: a path that exists matches that project's Editor only, so an Editor open on a
#    sibling checkout such as ~/UnityProjects/MyGame-lighting can't end the wait.
cd ~/UnityProjects/MyGame
unity open .
unity status --until-ready --project-path ~/UnityProjects/MyGame --format json
```

**Then build the scene in the live Editor, not in batch mode.** Create GameObjects, wire
components, and set asset references with `unity command script eval` (or the Editor's own `unity command`
tools) against the running Editor, and read the result back the same way. Each step can be
checked before the next one. A script run through `unity run -- -executeMethod` is the
**fallback** for when no Editor can stay open (CI, a headless build box). It can't see what it
produced: a reference saved as null, such as a `UIDocument` with no `PanelSettings`, still
reports success. If you do use batch mode, open the result in a live Editor and inspect it before
calling the work done. How to get an Editor to drive:
[integration-advanced.md → Getting an Editor to drive](references/integration-advanced.md#getting-an-editor-to-drive).

**Source control — let the user choose.** The CLI publishes the new project to a fresh remote in
one step for any provider. **Always pass tokens on stdin** (`--git-token-from-stdin`) so secrets never
land in shell history or the process list. Pick based on the project — don't default to one:

- **Git — GitHub / GitLab** (`--vcs github` / `--vcs gitlab`). Ubiquitous. For asset-heavy games
  add **Git LFS** (`--git-lfs`) so large binaries don't bloat history.
- **Unity Version Control — UVCS** (`--vcs uvcs`). Unity's own VCS, built for large binary game
  assets: it handles them natively (**no LFS needed**) and supports file locking — often the
  better fit for art-heavy projects or larger teams. Auth uses your Unity sign-in; `--vcs-region`
  selects the region.

```bash
# Git (GitHub) — drop --git-lfs if the game isn't asset-heavy. Add --no-initial-commit if you
# want to add packages/assets BEFORE the first commit (see the new-unity-project flow).
unity projects create "MyGame" --path ~/UnityProjects \
  --editor-version lts --template com.unity.template.urp-blank \
  --vcs github --git-namespace my-org --git-repo my-game \
  --git-visibility private --git-default-branch main --git-token-from-stdin --git-lfs

# Unity Version Control (UVCS) — handles binaries natively, so no LFS:
unity projects create "MyGame" --path ~/UnityProjects \
  --editor-version lts --template com.unity.template.urp-blank \
  --vcs uvcs --git-namespace my-org --git-repo my-game --vcs-region <region>
```

Feed the token to `--git-token-from-stdin` from a secret store, never a literal — e.g.
`… --git-token-from-stdin <<<"$GIT_TOKEN"` where `$GIT_TOKEN` comes from your CI/secret manager
(UVCS uses your Unity sign-in, so no token is needed).

**Working with a UVCS workspace day to day: a few wrapped reads, everything else straight through
to `cm`.** The split is deliberate and worth teaching, because guessing wrong wastes a user's time:

- **`unity vcs uvcs <verb>`** wraps the reads that **join `cm`'s data to your project** —
  `locks` (who holds a lock, *and which locks cover files you have already changed*),
  `changesets`, and `review`. Those joins are the thing `cm` cannot do for you, and they come in a
  stable envelope, so prefer them whenever something *parses* the output.
- **`unity uvcs <args>`** forwards the whole command line to `cm` verbatim, `--help` and
  `--format` included. That is the supported route, not a workaround: `cm` owns and versions this
  vocabulary, so wrapping it would pin a paraphrase that goes stale. Reach for it for **partial
  checkout**, **shelves**, and **taking or releasing a lock**, and when a human reads the output.

```bash
unity vcs uvcs locks                       # who holds what, and what collides with your changes
unity uvcs lock list                       # the raw listing, cm's own flags and output
unity uvcs partial update /Assets/Levels   # cm's own vocabulary, unchanged
unity uvcs shelve -c "wip: lighting pass"
```

Every verb, flag and trap: [version-control.md](references/version-control.md).

`unity cm <args>` is the same passthrough under cm's own name. Both need the `cm` client; install
it with `unity plugin install plastic` if a command says it is missing.

**Beyond setup, the `vcs` group covers the whole day-2 loop** — `status`, `sync`, `switch`,
`merge-setup`, `conflicts` / `explain` / `resolve`, `diff`, `blame`, `summarize`, `affected`,
`hooks`, `doctor`, `providers` — and the Unity semantics are the reason to reach for it over raw
`git`. Full reference, with the flags and the traps:
[version-control.md](references/version-control.md).

**Git tokens belong to the user's credential manager, not the CLI.** When no token flag or env var
is given, the CLI asks `git credential fill` and uses whatever the configured helper returns; it
stores nothing it is passed or told. Don't suggest the CLI can save a Git token, and don't reach for
a token flag when the user already has a working credential helper. If they want a different token
per organization, that is `git config --global credential.useHttpPath true` plus a multi-account
helper such as [Git Credential Manager](https://github.com/git-ecosystem/git-credential-manager).
The CLI passes the full repo URL so the helper can discriminate, but it never installs or
reconfigures a helper. `UNITY_GITHUB_TOKEN` / `UNITY_GITLAB_TOKEN` are one token per provider, so a
CI job spanning several orgs should pass `--git-token-from-stdin` per invocation instead. See
[references/projects-templates.md](references/projects-templates.md) for the full
source-control flag set. For a purely local Git repository instead, initialize git with a
Unity-appropriate ignore so the multi-GB `Library/` and other generated folders are never committed:

```bash
cd ~/UnityProjects/MyGame
git init -b main
# Download (do not pipe to a shell) a maintained Unity .gitignore:
curl -fsSL https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -o .gitignore

# Asset-heavy game? Keep large binaries out of git history with Git LFS:
git lfs install
git lfs track "*.psd" "*.fbx" "*.wav" "*.mp3" "*.png"   # adjust to your asset types
git add .gitattributes

git add -A
git status                             # sanity-check: Library/ Temp/ obj/ Build/ must NOT be staged
git commit -m "Initial Unity project: MyGame"
git ls-files | grep -c '^Library/'     # must print 0
```

**What the CLI does and doesn't cover.** The CLI handles editor, project, and source control.
It has no top-level package command. To add UPM (Unity Package Manager) packages beyond the
template, use the **`unity-package-management`** skill: it runs the Pipeline `package add`
command through `unity command`, with a headless C# script as the fallback. For monetization/backend, hand off to the dedicated skills: `implement-in-app-purchases`
(IAP), `levelplay-unity-integration` (ads), or `build-live-game` (accounts, cloud save,
economy, remote config, leaderboards). Once steps 5 and 6 above are done, work in the open
Editor.

**Installed the Pipeline package while the Editor was already open?** The Editor picks up the
manifest change only when it next refreshes. Until then `unity status` reports
`STATUS_PIPELINE_LOAD_PENDING` (or `STATUS_NO_INSTANCES` on CLI releases before that code
existed). An Editor that is still opening or importing reports the same code until it finishes,
so wait for that first (`unity status --until-ready`). The CLI can't trigger the refresh itself:
if the Editor has finished opening, ask the user to switch to the Unity Editor window, then
re-run `unity status --until-ready`. Installing before the first open (step 5) avoids this
entirely.

### Find and install a missing editor

```bash
# 1. Check what's installed
unity editors --installed --format json

# 2. Browse available LTS versions
unity releases --lts --limit 5 --format json

# 3. Install
unity install 6000.0.47f1 --yes --accept-eula
```

### Open a project with the correct editor

```bash
# 1. Check the project's required editor version
unity projects info /path/to/MyProject --format json
# Look at "editorVersion" in the result

# 2. Confirm that editor is installed
unity editors --installed --format json

# 3. Open (warns if the editor version is missing)
unity open /path/to/MyProject
```

### CI: activate a license, then build

```bash
# 1. Activate a license without signing in; a service account cannot activate one.
#    A perpetual or legacy serial: store it as the CI secret UNITY_LICENSE_SERIAL
#    (the name `unity ci init` generates) and pass it to the step, e.g. in GitHub
#    Actions `env: { UNITY_LICENSE_SERIAL: ${{ secrets.UNITY_LICENSE_SERIAL }} }`.
#    A serial tied to a subscription needs a signed-in user, so for those seats use
#    --floating, or the offline --generate-request / --file path (see "Headless /
#    fleet license activation" in references/auth-license-cloud.md).
unity license activate --serial "$UNITY_LICENSE_SERIAL"   # or: unity license activate --floating

# 2. Only if the job also runs cloud commands: export UNITY_SERVICE_ACCOUNT_ID and
#    UNITY_SERVICE_ACCOUNT_SECRET from the CI secret store. Every later command reads
#    them directly, with no `unity auth login` and nothing written to a credential
#    store (which a headless runner may not have).

# 3. Build
unity build /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --target StandaloneLinux64 \
  --execute-method Builder.PerformBuild \
  --allow-install
echo "Exit code: $?"

# 4. Return the license when done (serial or assigned seats)
unity license return --yes
```

### CI: headless build

Prefer the dedicated `unity build` command (handles batch mode, logging, and CI flags):

```bash
unity build /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --target StandaloneLinux64 \
  --execute-method Builder.PerformBuild \
  --allow-install
echo "Exit code: $?"
```

Or use `unity run` (batch mode is automatic — never pass `-batchmode`/`-quit`):

```bash
unity run /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --allow-install \
  -- -executeMethod Builder.PerformBuild -logFile build.log
echo "Exit code: $?"
```

Writing scripts for a player build? Keep each `MonoBehaviour` alone in a `.cs` file named after its class. Several in one file can run fine in the Editor and Play Mode, yet the built player crashes at startup saying a scene file such as `level0` is corrupted. See [build-run-test.md](references/build-run-test.md#build).

### CI: run tests and publish results

```bash
unity test /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --mode EditMode \
  --report-format junit \
  --output ./test-results.xml \
  --allow-install \
  --timeout 600
case $? in
  0) echo "All tests passed" ;;
  8) echo "Tests failed — report to developers, do not retry" ;;
  *) echo "Run did not complete — infrastructure failure, safe to retry" ;;
esac
```

Exit `8` means the run finished and reported failing tests; any other non-zero code means it never produced a verdict. Under `--format json` the same split is `errors[0].code`: `TESTS_FAILED` versus `TEST_RUN_ERROR` / `TEST_TIMED_OUT`.

`--report-format junit` makes `--output` a JUnit-schema report, which GitHub Actions and GitLab ingest as native test results with no converter step. It is written even when tests fail. Drop the flag for the NUnit3 default, or use `--report-format nunit,junit` to get both from one run. Add `--coverage` to collect coverage via the Unity Code Coverage package — it warns and carries on if the project doesn't have the package. See [build-run-test.md](references/build-run-test.md).

**Iterating on a change, not gating a pipeline?** Run the cheapest test that can fail for the change. Try EditMode first. Use `--filter`, `--affected` or `--rerun-failed` while you work. Run the full suite on the final code, once per mode, before you hand the work back. Never re-run a suite on code that has not changed, unless the run exited `6` with no verdict. Keep the NUnit default for these runs, because `--affected` and `--rerun-failed` cannot read the JUnit-only report the CI example above writes. [build-run-test.md](references/build-run-test.md#run-the-cheapest-test-that-proves-the-change) has the rule, and which report each flag reads and writes. `unity test` refuses a project that is open in an Editor: don't close the user's Editor to make room, and don't start a test run while they are in Play Mode.

### Debug the CLI

```bash
# Check auth + installed editors + recent errors in one command
unity doctor --format json

# Follow live logs during an install
unity logs --follow --level info
```

---

## Notifications

A `--format json` or `--format ndjson` envelope may carry a `notifications`
array: advisories about the user's environment rather than about the command
you ran. **When one is present, tell the user** — they have no other way to see
it, because the human-facing equivalent is a terminal banner that never reaches
you.

The key is absent when there is nothing to report, so read it as
`envelope.notifications ?? []`.

```jsonc
{
  "code": "CLI_UPDATE_AVAILABLE",
  "message": "A new version of the Unity CLI is available: 1.0.0-beta.8 → 1.0.0-beta.9",
  "data": { "current": "1.0.0-beta.8", "latest": "1.0.0-beta.9" },
  "remediation": { "command": "unity self-update", "requiresUserApproval": true }
}
```

Branch on `code`; `message` is localized and meant for display. `data` is
shaped per `code`.

`PLUGIN_UPDATE_AVAILABLE` means an installed plugin has a newer version.
`data` is `{ "plugin": "<id>", "current": "…", "latest": "…" }`, and the
remediation is `unity plugin upgrade <id>`. There is one entry per outdated
plugin.

`remediation` says how the notification gets resolved. It may be absent, which
means the notification is informational and there is nothing to run.

`FEX_EMU_EXPERIMENTAL` means the command ran, or will run, the x86_64 Editor
through FEX-Emu on Linux arm64. Tell the user it is experimental and
unsupported and that C# script compilation is known to crash there.

**`requiresUserApproval: true` means do not run `remediation.command`
yourself.** Surface the notification and the command, and let the user decide.

---

## Notes

- `--non-interactive` and `--yes` together suppress all prompts — use both in CI.
- A short flag means one thing on every command: `-y` is `--yes` wherever `--yes` exists, and `-a` is `--architecture`. `unity projects list` and `unity projects size` spell "every project" `--all`; write that, not `-a`. An old spelling the CLI still accepts (`-a` there, `--project` on `status`, `--template-version`, `--hub-version`, `--package-version`, `--list-components`, `--install-dir`, `--childModules`, `--wait-timeout`, `--git-token-stdin`, `--group_by` on `command`, `--local-space` on `collaboration annotations`, `--project-id` and `--organization-id` on `collaboration`, `-v` on `projects list`, `--since` on `releases` and `vcs git migrate-lfs`) prints a warning naming the spelling to use; so does `--verbose` on `editors` and `projects list`, which still shows every column but now spells it `--long`; in `--format json` it is in the envelope's `warnings`. Use the new spelling, so the warning never lands in a log you parse.
- `--timeout` takes a whole number of seconds written as digits (`--timeout 600`) on every command, and so do `UNITY_BUILD_TIMEOUT`, `UNITY_RUN_TIMEOUT` and `UNITY_TEST_TIMEOUT`. A unit, fraction or exponent (`30s`, `5m`, `1.5`, `1e3`) is a usage error (exit `2`), so convert minutes to seconds yourself.
- `--format json` always produces machine-readable output; prefer it over parsing human text. Error envelopes are pretty-printed with the same 2-space indent as success envelopes.
- **`success` is `true` exactly when the exit code is `0`.** A status command whose answer is no (`unity auth status` signed out, `unity license status` with no license, `unity status` with no ready Editor) is a failure: `success: false`, an `errors[0].code` naming the answer, and its report still in `data`. A listing that finds nothing (`unity editors running` with no Editor) succeeds with an empty list.
- **Read failures from stdout, not stderr.** A failed command still writes a complete document to stdout: under `--format json` an envelope with `success: false` and a populated `errors` array (`errors[0].code` is the stable token to branch on); under `--format ndjson` the usual terminal `{"type":"result","success":false,…}` frame. **Every ndjson stream ends with exactly one terminal `{"type":"result"}` frame, on success too**, so a stream without one was truncated. Table-shaped listings (`unity editors`, `unity editors upgrade --check`, `unity templates list`, `unity releases`, `unity projects list`, `unity context list`, `unity auth list`, `unity modules list`, `unity list`, `unity doctor`, `unity cloud org list`, `unity cloud project list`) write one row per line first, with no `type` field on the rows, then a terminal frame whose `data` is `{"count": N}`. A single-record answer (`unity version`, `unity language`, `unity config proxy`, `unity cache info` and the like) is that frame alone, its `data` the same as `--format json`; `unity env` writes its rows, then the frame. `--quiet` never changes json output. It never changes ndjson output either, except that `unity editors upgrade`, `unity editors module list`, `unity install-modules`, `unity build --list-targets`, `unity build --list-profiles` and `unity env` still drop their rows under `--quiet` (the terminal frame stays), a known bug; leave `--quiet` off when you need those rows. A warning in either format appears only in `warnings`, never on stderr. A `--watch` listing, which only ends on Ctrl-C, writes no terminal frame. A few commands (`unity install-modules --list` among them) still end a successful ndjson stream without one, which is a known bug. **Branch on `success`, never on `data`** — `data` is usually `null` on a failure, but not always: a partial `unity editors add` failure carries a row per path, and an ambiguous `unity auth switch` carries `data.candidates` for you to disambiguate with. Check `success` and the exit code — never treat empty stdout as a failure signal, and do not parse stderr, which carries only human diagnostics in these formats. If stdout is empty on a non-zero exit, that is a known bug in that command rather than a shape you should code against.
- `unity <version> [path]` is a shorthand for `unity open [path] --editor-version <version>`. Works with `lts`, `latest`, or a full version string like `6000.0.47f1`.
- The CLI supports kubectl-style plugins: any `unity-<name>` binary on PATH is callable as `unity <name>`.
- Terminal output is hardened against control-character / escape-sequence injection from server-provided values (project titles, editor versions, module names) — C0 controls and non-SGR escape sequences are stripped from table/list/tree output, and now also from Commander usage errors, the `unity bug` log-archive warning, and `unity projects add`/`remove` machine (tsv) output, while SGR color/style codes are preserved.
- The CLI reports anonymous crashes and errors via Sentry to help fix bugs (no IP address or hostname; home-directory paths and token-like values scrubbed before send), aligned with the Unity Hub. Opting in to analytics additionally attaches an anonymized machine id; opted-out users stay fully anonymous. Set `UNITY_NO_CRASH_REPORT` to disable reporting entirely. Separately again, every run sends one anonymous `cli telemetry` usage ping regardless of analytics/consent state — see [diagnostics-maintenance.md](references/diagnostics-maintenance.md#analytics--usagetelemetry-consent).
- `unity --version` prints the version you have. `unity self-update --check --format json` says whether a newer one exists on your installed channel (a beta follows betas). `unity self-update --check --channel stable --format json` checks for a stable release, for example when moving off a beta once GA ships.
- **Always use the latest version of the Unity CLI unless you are told to use a specific version.** A newer CLI regularly adds commands and output fields these docs describe, so an outdated one fails in ways that read like the docs being wrong.
- As of `0.1.0-beta.8` the CLI checks in the background for a newer version and prints an unobtrusive "update available" notice (interactive sessions only; never delays a command). Turn it off with `unity config update-check off` or the `UNITY_NO_UPDATE_CHECK` env var.
- The same check also covers plugins the CLI installed (`unity plugin install`): when one has a newer version, a notice names it and suggests `unity plugin upgrade`, at most once a day. The same two switches turn it off.
- **Under `--format json` and `--format ndjson` that same notice reaches you as a `notifications` array on the envelope** — see [Notifications](#notifications) below. Over MCP it arrives once per session, as prose in the server's `initialize` `instructions`.
- Outbound HTTP from every CLI command honors the resolved proxy (see `unity config proxy`). An invalid `--proxy` value (malformed URL or unsupported scheme) fails with a usage error (exit 2) instead of being silently ignored. Inspect what the CLI actually resolved with `unity env --format json` or `unity doctor --format json` — both surface the active proxy URL, its source, and auth source.
