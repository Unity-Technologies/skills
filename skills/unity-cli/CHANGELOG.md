# Changelog — unity-cli skill

All notable changes to the `unity-cli` skill documentation are recorded here. The
skill documents the published [`unity` CLI](https://public-cdn.cloud.unity3d.com/hub/prod/cli/);
each entry notes the CLI version the skill was aligned to.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased] — aligned to CLI `1.0.0-beta.2` (2026-07-21)

Tracks the CLI's move to 1.0 versioning (`1.0.0-beta.1` re-baseline) and `1.0.0-beta.2`. The CLI's own `[Unreleased]` changes (e.g. the universal `--json` shorthand) are intentionally **not** documented yet — they aren't in the shipped `1.0.0-beta.2` binary.

### Added

- **`unity shell`** — interactive REPL that boots the CLI once and runs many commands in a warm process (enter commands without the `unity` prefix; `exit` / `quit` / Ctrl-D to leave).
- **`unity list`** — top-level discovery of a connected Editor's registered tools (name, description, group, parameter schema); introspection-only companion to `unity command`.
- **`unity diagnose proxy`** — redacted, paste-safe proxy diagnostic report for support (`--json`; a copy is written to the logs dir).
- **`unity pipeline upgrade`**, **`unity pipeline list-versions`**, and **`unity pipeline install --package-version <v>`** — upgrade the Pipeline package only when the registry is newer, list all published versions, and pin a specific version. Documented that the flag is `--package-version` (not `--version`, which collides with the global `-V, --version`), and the multi-editor selection behavior.
- **`unity editor module remove` / `unity editors module remove`** — remove installed modules by id (`-m`, repeatable; `-y`, `-a`).
- **`unity install-modules`** `--reinstall`, `-f` / `--force`, and `--retries <n>` (env `UNITY_INSTALL_RETRIES`); **`--no-elevate`** (env `UNITY_NO_ELEVATE`, Windows) on `install` / `install-modules`.
- Global **`--log-proxy` / `--no-log-proxy`** (env `UNITY_LOG_PROXY`) — per-request redacted proxy logging.
- **`unity doctor`** environment health checks (PATH presence, `unity`-binary shadowing, Windows long-path support).
- Exit code **`143`** (SIGTERM) in the exit-code table.

### Changed

- **`--instance <host:port>` removed** from `unity command`, `unity mcp` (and dev-only `eval`) — the CLI discovers running Editors itself; target via the project directory or `--project-path`.
- **Exit codes** — the `cloud` / `auth` commands map an auth failure to `3` and any other operational failure to `6` (previously `1`); `unity build` interrupts exit `130` (SIGINT) / `143` (SIGTERM).
- **`unity license`** recognizes service-account sessions (`status` reports "Signed in: yes (service account)"); `activate` default/`--personal` fail up front for service accounts, pointing to the unattended modes; `return` now returns serial-activated licenses too, with per-license partial results.
- **`unity install` / `install-modules`** continue past a failed item and report a per-item result (✓/✗/·), with an `items[]` breakdown in NDJSON.
- **`unity upgrade`** detects package-manager installs (points at the owning manager instead of self-replacing); the "update available" notice is suppressed there.
- **`unity analytics`** first-run prompt now requires an explicit `y`/`n` (Enter re-asks); **`unity language`** dropped the regional variants Spanish (Latin America), French (Canada), and Portuguese (Portugal).
- Refreshed the latest-version note to `1.0.0-beta.2`; noted the move to 1.0 versioning at `1.0.0-beta.1`.

## CLI `0.1.0-beta.8` (2026-06-25)

### Added

- **MCP server** — `unity mcp` (built-in Model Context Protocol stdio server
  exposing a connected Editor's commands as tools) and
  `unity mcp configure <client>` (one-step config for 16 AI clients: `claude`,
  `claude-code`, `cursor`, `vscode`, `vscode-insiders`, `copilot-cli`,
  `windsurf`, `cline`, `codex`, `kiro`, `trae`, `openclaw`, `antigravity`,
  `zed`, `continue`, `inspect`; with `--list`, `--local`, `--project-path`,
  `--yes`, `--dry-run`).
- **`unity editors upgrade [editor]`** — upgrade an installed editor to the
  newest f-channel patch in its `major.minor` line, carrying modules over;
  `--all`, `--replace` (`--remove-old`), `--dry-run` (`--check`), `--no-modules`,
  `--module`, `--architecture`, `--yes`, `--accept-eula`. Documented the
  explicit `editors list` subcommand and the new "Upgrade to" column on
  `editors --installed`.
- **`unity config update-check`** and the `UNITY_NO_UPDATE_CHECK` env var, plus
  the background "update available" notice.
- `unity command screenshot` example (a command forwarded to the Editor).

### Changed

- **`pipeline`, `command`, and `status` promoted from development-only to
  production.** They now talk to any running Editor, and the Pipeline package
  (`com.unity.pipeline`) resolves from the **Unity UPM registry** into
  `Packages/manifest.json` — no internal-network clone or SSH. Moved into a new
  "Connected Editors" section; dropped `--ssh` / `--install-samples` /
  `--install-tests` from `pipeline install`; corrected the `command` aliases to
  `cmd`, `request`.
- **Auth:** the CLI and the Hub now store sign-in credentials **separately**
  (previously a shared keyring session).
- **`unity license list`** now reports a clear error and a non-zero exit when
  the licensing client is unavailable (previously an empty list).
- **`unity bug`** collects the same diagnostic system information as the Hub bug
  reporter (including GPU details).
- Refreshed the latest-version note to `0.1.0-beta.8`.

### Removed

- **`unity implode`** — removed (use `unity self-uninstall`).
- Dropped the no-longer-existent `editor play/stop/pause` wrappers. `eval`,
  `cloud-pipeline`, and `collab` remain documented as development-only.

## CLI `0.1.0-beta.7` (2026-06-17)

### Added

- **License management** (`unity license`) — `list`, `status`, `activate`
  (`--serial` / `--personal` / `--floating` / `--file` / `--generate-request`,
  mutually exclusive modes), `return`, and `server list|status`. Documented the
  expected exit codes (`4` when no license / floating server is configured).
- **`unity hub install`** — bootstrap Unity Hub from the CLI, with
  `--force`, `--headless` (Windows), `--architecture`, `--hub-version`, and
  `--skip-signature-check`; documented SHA-512 + code-signature fail-closed
  verification.
- **`unity test`** — run EditMode/PlayMode tests via the Editor's built-in test
  runner, with `--mode`, `--filter`, `--output`, `--editor-version`,
  `--editor-path`, `--architecture`, `--allow-install`, and `--timeout`
  (`UNITY_TEST_TIMEOUT`).
- **`unity editors path <version>`** — print an installed editor's directory
  (local, offline); clarified its distinction from `editors install-path`.
- **Projects source control & cloud** — `unity projects clone`,
  `projects link cloud|vcs`, `projects unlink cloud|vcs` (`--unlink-workspace`),
  and the full source-control flag set on `projects create` / `link vcs`
  (`--vcs`, `--git-namespace`, `--git-repo`, `--git-visibility`,
  `--git-default-branch`, `--git-token` / `--git-token-stdin`,
  `--no-initial-commit`, `--git-lfs`, `--vcs-region`). Also `projects create
  --cloud` / `--cloud-project`, and `--template` accepting a `.tgz`/directory.
- **`unity build` Android signing & export** — `--android-export-type`,
  `--android-keystore-base64`, `--android-keystore-password`,
  `--android-key-alias`, `--android-key-alias-password`,
  `--android-target-sdk-version`, `--android-symbol-type`,
  `--android-version-code`.
- New env vars `UNITY_TEST_TIMEOUT` and `UNITY_CLOUD_ORG`; new exit code `4`
  (precondition not met).
- Notes on the branded landing-surface header, the CLI's own `cli-log.json`,
  shared keyring sign-in with Hub, manifest-driven per-module install commands,
  partial-download self-heal, and terminal output hardening.

### Changed

- **Corrected availability of development-only commands.** `pipeline`,
  `command` (`cmd`), `eval`, `editor play/stop/pause`, `status`,
  `cloud-pipeline`, and `collab` are hidden in production builds (registered only
  when `HUB_ENV=development`) and are **absent from the published CLI's
  `--help`**. They were previously presented as generally available; they are now
  grouped under a clearly marked "Development-only commands" section.
- Documented `unity cloud-pipeline` and `unity collab` command groups (new,
  development-only).
- `unity templates edit` expanded with its full editable-field flag set and the
  "at least one field required" rule.
- Refreshed the latest-version note to `0.1.0-beta.7`.

## CLI `0.1.0-beta.6` — prior baseline

The previous skill revision documented CLI `0.1.0-beta.6`: Unity Cloud
(`unity cloud …`), proxy support (`unity config proxy`, `--proxy`,
`--proxy-disable`), analytics consent (`unity analytics …`), custom templates
(`templates create|edit|delete|location`, `--type`), `unity eval`,
`unity status`, and build versioning (`--versioning-strategy`,
`--build-version`).
