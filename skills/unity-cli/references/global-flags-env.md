# Global flags and environment variables — unity-cli reference

Relative links in this file resolve to the skill directory's `references/` folder.

Part of the **`unity-cli`** skill. Read this when the main page points here.

### Contents

- Global flags
- Environment variables

## Global flags

These work on every command:

| Flag | Description |
|---|---|
| `--format <fmt>` | Output format: `human` (default), `json`, `tsv`, `ndjson`, `github`. Also via `UNITY_FORMAT` env var. |
| `--json` | Global shorthand for `--format json`, accepted on every command (e.g. `unity status --json`, `unity doctor --json`). Passing it with a different `--format` (e.g. `--json --format tsv`) is a usage error (exit 2); `--json` overrides `UNITY_FORMAT`. |
| `--no-banner` | Suppress the branded header — use in scripts |
| `--no-header` | Leave the header row out of `tsv` tables. The header row is the column names, which are the same keys the `json` rows use (`version`, `upgradeTo`, ...). An empty `tsv` result is the header row alone, so with `--no-header` it is no output at all. |
| `--no-pager` | Turn off paging. Governs both pagers: the external one over the long listings (`unity command`, `releases`, `editors`, `changelog`, `logs`) and the interactive one in `unity projects`. Also via `UNITY_NO_PAGER` (presence-based — any value, including `0`, disables it). |
| `--non-interactive` | Disable all interactive prompts — use in CI |
| `--quiet` | Hide notices, warnings and progress in human and tsv output. Errors, the exit code and the `tsv` rows stay, so a script that pipes with `--quiet` still gets its data. json output never changes, and ndjson output does not either, except that a few listings still drop their rows (see [Notes](notes.md)). |
| `--verbose` | Print full error details (stack trace + cause chain) on failure. Also via `UNITY_VERBOSE`. |
| `--proxy <url>` | HTTP/HTTPS/SOCKS/PAC proxy URL for this invocation. Also via `UNITY_PROXY`. Takes precedence over standard `HTTPS_PROXY`/`HTTP_PROXY`/`ALL_PROXY` env vars and the persisted `proxy.json` setting. |
| `--proxy-disable` | Disable proxy for this invocation, ignoring all sources (env vars, persisted config, system settings). |
| `--log-proxy` | Log one redacted entry per outbound request to `proxy-request.json` — for reproducing proxy issues. Also via `UNITY_LOG_PROXY=1` or the `proxyRequestLogging` setting. |
| `--no-log-proxy` | Opt a single invocation out of proxy request logging when it's enabled globally. |
| `--color <auto\|always\|never>` | Control colored output for this invocation, overriding `NO_COLOR`/`FORCE_COLOR` and TTY auto-detection. Governs every ANSI-emitting surface (help, tables, spinners, errors), not just `human` output. |
| `--no-color` | Shorthand for `--color never`. Whichever of `--color`/`--no-color` appears last on the line wins. |

**Always use `--format json` when you need to parse output programmatically.** If you read `tsv` instead, select columns by header name, not position: a command's `tsv` columns keep their names and order, and a flag that adds information (such as `editors --long`) only appends columns at the end. Failures in `tsv` go to stderr, so stdout holds only data. A single-record answer (`unity env`, `unity config proxy`, `unity doctor`, `unity editors default`, `unity install-path` and the like) is a `key<TAB>value` table instead, one row per field keyed by its `json` path (`values.editor`, `tools.0.id`), so `unity install-path` piped prints the path on its `path` row, not bare; `unity cache key` is one too, so a script that needs the bare key passes `--format human`.

**Every listing has one JSON shape.** Read the rows from `.data.items[]`. Beside them, `.data.total` is the full count (`null` when the service doesn't report one), `.data.nextPage` (pass it to the command's `--page`, `--skip` or `--offset`, whichever it takes: `--offset` for `collab annotations count --grouped`) or `.data.nextCursor` (pass it to `--next` or `--cursor`) is `null` on the last page, and `.data.partial` is `true` when the listing stopped before it finished. Under `--format ndjson` the rows stream one per line and the last line, `type: "result"`, carries `count` and the same paging fields. Some listings still carry their older keys too (`instances`, `entries`, `results`); prefer `items`.

`--accelerator <host:port>` and `--no-accelerator` are **not** root globals — they are accepted only on `run`, `test` and `build`, and only after the command name. See [build-run-test.md](build-run-test.md).

**`unity projects` is the only command that pages IN-PROCESS.** It shows 10 projects per screen and waits for a keypress between screens, and only when stdout is a terminal. Paging is off for redirected stdout, under every machine format (`json`, `ndjson`, `tsv`, `github`), and under `--all`, `--watch`, or `--no-pager` / `UNITY_NO_PAGER`.

**The long listings page through an external pager, like `git log`.** `unity command` (the bare listing), `unity releases`, `unity editors`, `unity changelog`, and `unity logs` pipe human output through `less -RFX` on a terminal; `-F` quits by itself when the output fits one screen. `$UNITY_PAGER` then `$PAGER` override the choice and run through a shell, so `PAGER="less -S"` works; a blank value is ignored rather than treated as an opt-out. Like `projects`'s pager this one is **`human`-only**, and it never engages for redirected stdout, any machine format (`json`, `tsv`, `ndjson`, `github`), `--quiet`, `TERM=dumb`, the streaming modes (`editors --watch`, `logs --follow`), a named `unity command <name>`, or inside `unity shell`. A broken pager costs the paging, not the output: a `$PAGER` naming something that is not there is resolved before anything spawns (a warning on stderr names it, and on Windows a bare name also finds `.com` programs such as `more`), and one that spawns and then dies has its output reprinted to the terminal, decided from the pager's exit status (a clean exit is a normal `q` and discards; a failure status reprints). A pager that exits 0 without reading (`PAGER=true`) is indistinguishable from `q`, so its output is lost. One that merely *waits* is not broken; the CLI waits with it.

A branded Unity header (logo, wordmark, CLI version) renders on the landing surfaces — bare `unity`, `unity --help` / `-h`, and `unity help`. It's shown only on a TTY, prints at most once, and degrades to compact, uncolored text on narrow terminals, without Unicode, or under `NO_COLOR`. Piped output is unaffected. Use `--no-banner` to suppress it in scripts. Bare `unity` prints usage and exits 0. The header's accent is brand blue (`#0F70CB` on a light background, `#6EB8FF` on a dark one), in 24-bit color where the terminal supports it and the nearest 256-color entry otherwise. The background comes from `UNITY_THEME`, then `COLORFGBG`, then, on the header only, a terminal query. The query runs only where the terminal can answer (Windows Terminal, VS Code, or a Unix terminal with `TERM` set), and waits at most 100 ms; no reply means dark. Git Bash is never queried. Subcommand help and the command listing never query the terminal: they use `UNITY_THEME`, then `COLORFGBG`, then dark. Where the header cannot query, set `UNITY_THEME=light` or `UNITY_THEME=dark` to choose the background yourself.

## Environment variables

All CLI env vars use the `UNITY_` prefix. A CLI flag always overrides the corresponding env var.

| Variable | Mirrors flag | Description |
|---|---|---|
| `UNITY_FORMAT` | `--format` | Output format (`human`, `json`, `tsv`, `ndjson`, `github`). `HUB_FORMAT` is a deprecated alias. |
| `UNITY_EDITOR_VERSION` | `--editor-version` | Editor version (e.g. `2023.3.0f1`, `latest`, `lts`). Read by every command that takes `--editor-version`, `build` included; `build` prints a notice when the variable chose its Editor. Not read by the `pipeline cloud-build` filters. |
| `UNITY_ARCHITECTURE` | `--architecture` | Chip architecture (`x86_64`, `arm64`). `build` names it in the same notice. |
| `UNITY_PROJECT_PATH` | `--project-path` | Project path when none is passed. Read by every command that takes `--project-path` (the Editor-connecting commands, `recompile`, `pipeline`, `collaboration`, `assets`, `auth default`, `templates create`/`pack`, `projects link`, `projects unlink vcs` and the `vcs` commands that act on a project) and by `open`. A path argument or `--project-path` wins over it. Not read by `close` (and `projects close`), `projects unlink cloud`, `mcp configure` or `ci init`. |
| `UNITY_QUIET` | `--quiet` | Suppress non-essential output. |
| `UNITY_VERBOSE` | `--verbose` | Show full error details on failure. |
| `UNITY_NON_INTERACTIVE` | `--non-interactive` | Disable interactive prompts. |
| `UNITY_NO_BANNER` | `--no-banner` | Suppress the branded banner. |
| `UNITY_THEME` | — | `light` or `dark` (any case): the terminal background the branded header assumes, so its accent reads on it. Wins over `COLORFGBG` and the header's terminal query, and is the fallback where that query cannot run (Git Bash, a legacy console, or `TERM` unset). Help and the command listing rely on it and `COLORFGBG`, never on a query. Any other value is ignored. |
| `UNITY_NO_PAGER` | `--no-pager` | Turn off paging — both the external pager over the long listings and `unity projects`'s interactive one. Presence-based: any value counts, including `0`. |
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
| `UNITY_CLI_ROLLBACK_KEEP` | — | How many previous CLI versions `unity self-update` keeps for `--rollback`, from 1 to 10 (default 3). |
| `UNITY_NO_AGENT_HINTS` | — | Stop suggesting Unity's agent plugin or skill when an AI agent runs `unity`: Claude Code's install prompt, the `unity setup codex` / `grok` / `antigravity` / `copilot` tip, and the `unity skill install` tip (an `AGENT_PLUGIN_AVAILABLE` entry in the envelope's `notifications` under `--format json`), plus the once-a-day `unity skill sync` notice when installed Unity skills are older than the CLI's pin (`AGENT_SKILLS_OUTDATED`). Presence-based: any value counts. A tip asks you to get the user's approval before running its command. |
| `UNITY_NO_CRASH_REPORT` | — | Disable anonymous crash/error reporting (Sentry) entirely. The Hub's "Send crash reports" setting, when off, also disables it. |
| `UNITY_NO_CLI_INVOKED_TELEMETRY` | — | Stop the anonymous once-per-run `cli telemetry` usage ping. Presence-based: any value counts. |
| `DO_NOT_TRACK` | — | The cross-tool [Console Do Not Track](https://consoledonottrack.com) setting. Any value but empty, `0` or `false` stops the `cli telemetry` usage ping and the upload of opt-in usage data. Licensing records are not affected. |
| `UGS_CLI_TELEMETRY_DISABLED` | — | The UGS CLI's telemetry switch, honored exactly like `DO_NOT_TRACK`. Any value but empty, `0` or `false` stops the `cli telemetry` usage ping and the upload of opt-in usage data. Licensing records are not affected. |
| `UNITY_LOG_PROXY` | `--log-proxy` | Log one redacted entry per outbound request to `proxy-request.json`. Truthy values: `1`, `true`. |
| `UNITY_ACCELERATOR` | `--accelerator` | Unity Accelerator endpoint (`host:port`). Outranks the persisted `accelerator.json`; `--accelerator` outranks it. |
| `UNITY_NO_ELEVATE` | `--no-elevate` | Windows: skip the elevated (UAC) install helper for `install` / `install-modules`, so the install service runs unelevated. The Editor's NSIS installer still asks for elevation on demand if Windows requires it for your account — an administrator token always does; a standard user never does. |
| `UNITY_INSTALL_RETRIES` | `--retries` (`install-modules` only) | Number of times `install` and `install-modules` retry an editor or module download whose transfer or validation fails. `0` disables retries; `unity install` has no `--retries` flag, so set the variable there. |
| `UNITY_NO_AUTH_BROKER` | — | Skip the resident auth broker and read credentials directly from the OS keyring. By default every command that needs a token goes through a broker that starts on demand and exits after two idle minutes (see [auth-license-cloud.md](auth-license-cloud.md)). |
| `UNITY_CLI_HOME` | — | Install root for the install script and `unity self-install`, on every platform including Windows: the binary lands in `<UNITY_CLI_HOME>/bin` instead of the default location. |
| `UNITY_NO_EDITOR_IDENTITY_SERVER` | — | Disable the background identity helper that `unity open` starts to answer the Editor’s sign-in lookups when no Hub is running (see [projects-templates.md](projects-templates.md)). Presence-based. |
| `UNITY_EXPERIMENTAL_FEX_EMU` | none | Linux arm64 only, experimental and unsupported: run the x86_64 Editor under FEX-Emu. `1`, `true` or `yes` turns it on, and it takes effect only when FEX-Emu and an x86_64 RootFS are installed (`unity doctor` checks). C# script compilation is known to crash under FEX-Emu. See [editors-install.md](editors-install.md). |

**CI service account auth:** Set both `UNITY_SERVICE_ACCOUNT_ID` and `UNITY_SERVICE_ACCOUNT_SECRET` to skip the browser OAuth flow — this keeps the secret out of the process argument list and shell history. These map to the `--client-id` / `--secret-from-stdin` inputs of `unity auth login`, but reading the credentials from the environment isn't a full login: it doesn't run the interactive flow or persist credentials to the keyring.

