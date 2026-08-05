# Changelog — unity-cli skill

All notable changes to the `unity-cli` skill documentation are recorded here. The
skill documents the published [`unity` CLI](https://public-cdn.cloud.unity3d.com/hub/prod/cli/);
each entry notes the CLI version the skill was aligned to.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased] — aligned to CLI `1.0.0-beta.3` (2026-07-24)

Tracks the CLI's `1.0.0-beta.3` release. The CLI's own `[Unreleased]` changes (detached command jobs — `unity command --detach`, `unity job wait/status/cancel` — and live in-terminal progress for `unity command`) are intentionally **not** documented yet: they aren't in the shipped `1.0.0-beta.3` binary.

### Added

- **`unity editors running`** — list running Editor instances and the project each has open (version + PID; cross-platform; an empty list is exit 0).
- **`unity projects size [project]`** — on-disk footprint by top-level folder (`-a, --all`; `--json` emits raw bytes).
- **`unity run --command <name>`** — execute a registered `[CliCommand]` Editor command headlessly (arguments after `--` parsed against its `[CliArg]` schema; requires `com.unity.pipeline`).
- **`unity install --list-components`** — list an editor's available modules and exit (a drop-in alias for `unity modules list <version>`).
- **`unity bug` non-interactive flags** — `--title`, `--description`, `--steps` (repeatable), `--reproducibility <first-time|sometimes|always>`, `--email`.
- **`unity shell`** — command-history persistence (↑/↓; secret-bearing flag values masked on disk), tab completion, session context/defaults (`use project|org`, `set format|verbose|banner`, `unset`, `context`), and the `--protocol ndjson` machine/agent mode.
- Environment variables **`UNITY_NO_CONSENT_PROMPT`** (suppress the first-run consent prompt without recording a choice) and **`UNITY_NO_CRASH_REPORT`** (disable anonymous crash/error reporting).
- Global **`--json`** shorthand (accepted on every command) in the global-flags table.
- OSC 9;4 taskbar progress note for `unity install` on interactive terminals.
- **Driving a running Editor** — three patterns: **persistent headless** (launch the Editor binary in `-batchmode` *without* `-quit`; it stays resident and serves the Pipeline API — drive it with `unity command`/`list` --project-path), **warm/interactive** (`unity open`, which registers with `unity status` as `ready`), and **one-shot CI** (`unity run --command <name>` boots a batch Editor, runs one command, exits). Notes that a bare `unity run` is *not* persistent (batch runs to completion and exits) and — verified — that a batch-mode Editor serves commands but is **not** listed by `unity status`. Closes a gap where the Connected Editors section assumed a running Editor without saying how to get one.
- **Authoring custom `[CliCommand]` tools** — `[CliCommand]` / `[CliArg]` in the `Unity.Pipeline.Commands` namespace (assembly `Unity.Pipeline`), with `MainThreadRequired` / `RuntimeOnly` as **named properties on `[CliCommand]`** (not separate attributes); worked example, and hot-registration via `unity command recompile` → `unity list`.
- **Editor-side `eval` / `eval_file`** — noted the runtime-discoverable production path via `unity command eval` / `unity command eval_file`, discovered from the connected Editor.
- **Live-Editor control surfaced up front** — the skill `description` now advertises controlling a running/connected Editor (create/modify GameObjects, edit scenes, inspect the hierarchy, run C#) so agents pick the skill for scene/GameObject prompts, and a new top-of-skill **"Drive a running Unity Editor"** quickstart shows the minimal `unity status` → `unity command` path ahead of the install steps.
- **Production live commands + curated command list** — clarified that the whole `unity command <name>` / `com.unity.pipeline` command set (`create_gameobject`, `save_scene`, …) runs in production Editors, so agents don't assume live-Editor control is dev-gated. Added a curated quick-reference of the common built-in scene/GameObject commands, noting `unity command --format json` remains the authoritative catalog.
- **Scene / GameObject / asset workflow** — a new Common workflows entry makes `unity status` the first move for any scene or object task and, when an Editor is connected, prefers live `unity command` calls over file edits. Adds a strong anti-pattern block against hand-editing `.unity` / `.prefab` / `.asset` YAML while a live Editor is reachable (error-prone fileIDs/GUIDs, invisible until reimport, can silently target the wrong scene), with an explicit "only edit files when no Editor is reachable" fallback.

### Changed

- **`unity upgrade`** — documented Linux AppImage in-place updates and the apt/rpm repositories (GPG-signed rpm); the background "update available" notice is now package-manager-aware (suggests the owning manager's upgrade command) rather than always suppressed on package-managed installs.
- **`unity analytics`** — expanded the events recorded when opted in (registered command names only, never arguments/paths/project names; editor uninstalls; project open/create; self-upgrade/uninstall; shell/mcp/doctor/bug), noted that `opt-in`/`opt-out` now permanently answer the first-run prompt, and documented the separate anonymous Sentry crash-reporting pathway.
- **`unity language --set`** accepts BCP-47 / locale / bare-language / bare-region spellings (resolved case-insensitively when unambiguous); catalog shared with the Hub.
- **`unity projects`** path resolution documented as tolerant of casing, separator direction, and trailing slash (verified against real filesystem identity).
- Terminal-hardening note extended to Commander usage errors, the `bug` log-archive warning, and `projects add`/`remove` tsv output; noted that an invalid `--proxy` now fails with exit 2; `UNITY_PROJECT_PATH` now honored by `status` and the cloud commands.
- Refreshed the latest-version note to `1.0.0-beta.3`.

### Security

- Added `SECURITY.md` documenting the skill's powerful-by-design capabilities (local Editor control and C# evaluation, official-CDN install) and the safeguards around them (local-user-context execution, trusted-input-only machine mode, HTTPS official CDN).
- Clarified that driving a live Editor and running C# happen entirely on the local machine in the user's own account — not remote access — and added a trusted-input warning to `unity shell --protocol ndjson` machine mode.
- Removed internal development-only command documentation from the public skill; the production Editor-side C# evaluation via `unity command eval` remains documented.

## CLI `1.0.0-beta.2` (2026-07-21)

Tracks the CLI's move to 1.0 versioning (`1.0.0-beta.1` re-baseline) and `1.0.0-beta.2`. The CLI's own `[Unreleased]` changes at the time (e.g. the universal `--json` shorthand) were intentionally not documented in this section — they weren't in the shipped `1.0.0-beta.2` binary (they shipped in `1.0.0-beta.3`, documented above).

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

- **`--instance <host:port>` removed** from `unity command`, `unity mcp` — the CLI discovers running Editors itself; target via the project directory or `--project-path`.
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
- Dropped some no-longer-existent command wrappers.

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

- **Corrected command availability.** Commands previously presented as generally
  available were regrouped; several are not part of the published CLI's `--help`.
  (`pipeline`, `command`, and `status` were later promoted to production in
  `0.1.0-beta.8`.)
- `unity templates edit` expanded with its full editable-field flag set and the
  "at least one field required" rule.
- Refreshed the latest-version note to `0.1.0-beta.7`.

## CLI `0.1.0-beta.6` — prior baseline

The previous skill revision documented CLI `0.1.0-beta.6`: Unity Cloud
(`unity cloud …`), proxy support (`unity config proxy`, `--proxy`,
`--proxy-disable`), analytics consent (`unity analytics …`), custom templates
(`templates create|edit|delete|location`, `--type`), `unity status`,
and build versioning (`--versioning-strategy`,
`--build-version`).
