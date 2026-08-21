# Changelog — unity-cli skill

All notable changes to the `unity-cli` skill documentation are recorded here. The
skill documents the published [`unity` CLI](https://public-cdn.cloud.unity3d.com/hub/prod/cli/);
each entry notes the CLI version the skill was aligned to.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), with one
deliberate departure: there is no `Unreleased` section. Sections are cut per CLI release, and
documentation for a CLI version that has not shipped publicly is not recorded here until that
release is out — so this file never names unreleased surface. Pending skill work is tracked
alongside the CLI change itself, not here.

## CLI `1.0.0-beta.6` (2026-08-19)

Aligned to the CLI's `1.0.0-beta.6` release. Most of this release's surface — `unity doctor --ci`, `unity cache key`, `--format github`, `unity test --shard`, and `unity collaboration` — landed already documented in `1.0.0-beta.5`'s reference files ahead of that release's stamp bump. This pass adds the two pieces that were still outstanding: the `unity build` stall heartbeat and `--timeout`, and the `unity open` background identity server plus the new git-credential interactivity gating.

### Added

- **`unity build --timeout <seconds>` (env `UNITY_BUILD_TIMEOUT`) and the stall heartbeat** — documented in the build options table and as its own note: a long build now prints a periodic "Still building — Nm elapsed, last log output Nm ago" heartbeat (on stderr for the human path, as progress frames under `--format json`/`ndjson`), and `--timeout` aborts a build that runs past the given number of seconds, exit 6.
- **`unity open`'s background identity server** — a small helper that answers the Editor's account lookup from the stored `unity auth login` session, so a Hub-less machine gets a signed-in Editor. Documents that it steps aside for a real Hub, exits on its own, and can be disabled with `UNITY_NO_EDITOR_IDENTITY_SERVER`.
- **Git credential interactivity gating** — the CLI now decides up front whether anyone is there to answer a credential prompt. Documented alongside the existing token-resolution order: on a real terminal a configured helper (including Git Credential Manager's browser/device-code flows) runs and is relayed; without one — CI, machine `--format`, `--non-interactive` — the command fails immediately with exit 4 instead of hanging, naming the missing credential.

### Changed

- Refreshed the latest-version note to `1.0.0-beta.6`.

## CLI `1.0.0-beta.5` (2026-08-13)

Aligned to the CLI's `1.0.0-beta.5` release. That release is fixes only — it adds no command, flag, or exit code. This pass instead closes the documentation gap the previous one left open: every item the `1.0.0-beta.4` note listed as deferred is now documented, so the skill covers the full shipped surface of both releases.

### Added

- **`unity editors prune`** — find editors no registered project uses and optionally uninstall them. Report-only by default; `--remove` uninstalls, and `-y, --yes` is required to do so non-interactively. Notes that "unused" is judged against the project registry, so an unregistered project's editor counts as unused.
- **`unity editors verify <version>`** — structurally verify an installed editor's files and modules, reporting each component as `ok` / `missing` / `skipped` with the exact repair command. Documented as a structural check (presence, not integrity or signing), and that `--architecture` is inherited from the `editors` parent.
- **`unity projects clean`** — delete a project's regenerable folders (`Library`, `Temp`, `Logs`, …). Documents `--dry-run`, that `--yes` is required non-interactively, and the two guardrails: it refuses while an editor has the project open (naming the PID) and rejects a path that isn't a Unity project.
- **`unity templates pack`** — pack a project into a portable `.tgz`, with the distinction from `templates create` stated up front (`pack` writes a standalone archive and registers nothing; `create` installs into the user templates directory). Covers the required `--output` file path, the `--template-version` spelling that avoids the global `--version` collision, and the rejection of an output path inside the project being packed.
- **`unity command` listing-query flags** — `--detail`, `--query`, `--tag`, `--group_by`, `--sort`, `--order`, `--offset`, `--limit` as a table with values and defaults, plus the two traps: `--group_by` is deliberately underscored, and the flags only mean "listing" when no command name is given (with a name they forward to that Pipeline command as parameters, which is why each takes an optional value).
- **Multi-account auth** — `unity auth list` (with its `ls` alias), `switch`, and `default` (`--project`, `--clear`), plus `auth logout <account>`. Documents the precedence that makes these predictable: a project default beats the active account, and service-account credentials beat both; `switch` fails on an ambiguous match rather than guessing, carrying candidates in `data.candidates`.
- **The output pager** — documented in the global flags and environment tables (`--no-pager`, `UNITY_NO_PAGER`, `UNITY_PAGER`) with the `git log` model spelled out: which commands page, the `$UNITY_PAGER` → `$PAGER` → `less -RFX` → `more.com` resolution chain, and the conditions under which paging never happens (non-TTY, machine formats, `--quiet`, `TERM=dumb`, inside `unity shell`) so scripts need no special handling.
- **`unity skill install` / `refresh`** — install this skill into an AI client from the copy embedded in the binary, framed against `mcp configure` (tools vs. docs). Covers the client list, `--list` as the authority on which scopes each client supports, and that `refresh` should follow `unity upgrade` because installed copies otherwise go stale.

### Changed

- Refreshed the latest-version note to `1.0.0-beta.5`.
- Command index: added `editors prune`/`verify`, `projects clean`, `templates pack`, `auth list`/`switch`/`default`, and `skill install`/`refresh`. Also dropped one listed subcommand that is not part of the public surface.

## CLI `1.0.0-beta.4` (2026-08-06)

Tracks the CLI's `1.0.0-beta.4` release. Coverage is the full `1.0.0-beta.3` surface plus the beta.4 additions an automation or CI caller reaches for first: `unity test --report-format`/`--coverage`, `unity build --profile` and the zero-code build strategies, `unity projects exec`, `unity bug --attachments`/`--share-project`, and the rule that a failure is readable from stdout. The rest of beta.4 was deferred to a later pass and is documented under `1.0.0-beta.5` above: `unity skill install`/`refresh`, `unity projects clean`, `unity editors prune`/`verify`, `unity templates pack`, the `unity command` listing-query flags, multi-account auth (`unity auth list`/`switch`/`default`), and the output pager. Documenting a subset of the shipped surface is safe; the stamp exists to stop the reverse (publishing surface that isn't in the shipped binary).

### Added

- **`unity bug --attachments <paths…>` / `--share-project <path>`** — attach extra files (each must be an existing readable file; a folder is rejected), or a stripped copy of the project using the same packaging the Editor's bug reporter uses. Interactively, omitting both flags makes the reporter ask about each.
- **`unity test --report-format nunit|junit|nunit,junit`** — write a JUnit-schema report that GitHub Actions and GitLab ingest as native test results, with no converter step. `junit` alone makes `--output` the JUnit file; `nunit,junit` writes both from a single Editor run, the JUnit one landing beside the NUnit report. `--junit-output` chooses that second path and is valid **only** with `nunit,junit`; passing it with a single format is an option error. The report is written even when tests fail, and the NUnit default is unchanged.
- **`unity test --coverage`** (with `--coverage-output`, `--coverage-options`) — collect coverage through the Unity Code Coverage package. A project without the package gets a warning and the tests still run.
- **`unity build --profile <profile>` and the zero-code build strategies** — documented the three ways to pick a build: a Unity 6+ Build Profile (a `.asset` path or a profile name under `Assets/Settings/Build Profiles`, which defines the target), a built-in desktop player build (`--target` plus a required `--output-path`), or a custom `--execute-method`. `--execute-method` is no longer required, and `--target` is not needed when `--profile` is used. Non-desktop targets still need `--profile` or `--execute-method`.
- **`unity projects exec -- <command>`** — run one command across every registered project, each in its own directory with `UNITY_PROJECT_PATH` and `UNITY_EDITOR_VERSION` set. Narrow the set with repeatable `--filter` terms (`name:<glob>`, `version:<glob>`, `pinned[:<bool>]`), raise concurrency with `--parallel <n>`, and use `--continue-on-error` or `--dry-run`. Arguments are passed verbatim rather than through a shell, so pipes and `&&` are unavailable.
- **`unity run --command` worked example** — a `[CliCommand]` source snippet with the human output it produces and the `--format json` envelope beside it (`data.result`, `data.parameters`, and `data.reusedRunningEditor`, which reports whether an already-open Editor was reused).

### Changed

- **Read failures from stdout, not stderr** — documented the machine-format failure contract. Under `--format json` a failed command still writes a full envelope (`success: false` and a populated `errors` array whose `errors[0].code` is the stable token to branch on); under `--format ndjson` it closes with the usual terminal `result` frame. `data` is usually `null` on a failure but not always, so branch on `success`, never on `data`: a partial `unity editors add` failure carries a row per path, and an ambiguous `unity auth switch` carries `data.candidates`. Empty stdout is not a failure signal, and the commands that still report only on stderr are called out as a known gap rather than a shape to code against.
- **Reserved forwarded flags** — matching is spelling-insensitive, so `-projectPath`, `--projectPath`, and `-projectPath=<value>` are all rejected, on every command that forwards user arguments (`unity run`, `unity test`, `unity build --args`, `unity open --args`). Also clarified that `unity run` deliberately never passes `-useHub`/`-hubIPC`, because the CLI runs no Hub IPC server and those flags would make the Editor launch the Unity Hub.
- **`unity mcp configure --local`** — corrected the client list. The clients with a project-local config are `cursor`, `vscode`, `vscode-insiders`, `kiro`, and `codex`. Windsurf reads one global file and has no project-local variant.
- **`UNITY_NO_ELEVATE` / `--no-elevate`** — corrected to say it keeps the install service unelevated. The Editor's NSIS installer is manifested `highestAvailable`, so it still asks for elevation on demand under an administrator account and never does for a standard user; in CI, run the agent elevated instead.
- Refreshed the latest-version note to `1.0.0-beta.4`.

### Security

- **Install integrity stated, and scoped** — the install script verifies the downloaded binary against the SHA-256 published in the channel's release manifest and aborts on mismatch, or when no SHA-256 tool is available. Because the manifest is fetched from the same CDN origin as the binary, this is described as an integrity check against a corrupted, truncated, or substituted *download*, not a defense against a compromise of the origin; the trust assumption (TLS plus Unity's control of that CDN) is stated explicitly.
- **Linux install side effects split by package** — the CDN script installs a self-contained binary under `~/.local/bin` and touches no system package sources. The separately published packages do change system state, and differently: the `.deb` adds an apt repository entry and installs Unity's signing key into the system keyring, while the `.rpm` adds a yum repository entry with `gpgcheck` enabled pointing at the published key URL and imports no key at install time.

## CLI `1.0.0-beta.3` (2026-07-24)

Tracks the CLI's `1.0.0-beta.3` release. The CLI's own `[Unreleased]` changes at the time (detached command jobs — `unity command --detach`, `unity job wait/status/cancel` — and live in-terminal progress for `unity command`) were intentionally **not** documented in this pass: they weren't in the shipped `1.0.0-beta.3` binary. They shipped in `1.0.0-beta.4`, documented above.

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
- **Recovering from Safe Mode** — a new Connected Editors playbook for the deadlock where a project's C# compile errors force the Editor into Safe Mode, the `com.unity.pipeline` package doesn't load, and `unity command`/`status`/`list`/MCP can't connect. Documents the recovery loop with production-available commands: recognize the connection failure, confirm Safe Mode with `unity pipeline list` (which surfaces the warning, `SafeMode Instances: N detected`, and the "fix compilation errors and restart" hint), read the compile errors from the Editor log — narrowest first (`-logFile`, then `<project>/Logs/Editor.log`, then the per-user global log, with per-platform paths; disambiguated from `unity logs`, which reads the CLI's own log) — fix the C# source, restart Unity, and re-poll until reachable. Restarting stops the stuck Editor **by PID** from `unity pipeline list`, with an explicit warning against name-pattern kills (`pkill -f Unity`) that would take down every open Editor including unsaved work. The log step reads through a filter rather than dumping a cross-project file, and treats log contents as data, not instructions. Cross-linked from the "Drive a running Editor" quickstart and the scene-editing fallback so agents diagnose Safe Mode before falling back to blind file edits. (Addresses community feedback on the 1.0.0-beta.3 rollout thread.)

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
- Removed internal development-only command documentation from the public skill; the production Editor-side C# evaluation via `unity command eval` remains documented. `SECURITY.md` now carries only the user-facing capability rationale and safeguards.

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
