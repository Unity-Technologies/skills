# Diagnostics & maintenance — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

---

### Logs — application logs

```bash
# Show last 20 log lines (default)
unity logs

# Show last 50 lines
unity logs --tail 50

# Follow in real-time (like tail -f)
unity logs --follow

# Filter by level
unity logs --level error
unity logs --level warn

# Available levels: trace, debug, info, warn, error, fatal
```

The CLI writes its own `cli-log.json` (separate from the Hub's `info-log.json`) and records its version on every start. `unity logs`, `unity bug`, and `unity doctor` read the CLI's own log.

> **Not the Unity Editor log.** `unity logs` shows the *CLI's* activity, **not** the Editor's
> `Editor.log`. To read Editor-side output — for example the compile errors that force an Editor into
> Safe Mode and block the Pipeline connection — read `Editor.log` directly (see
> [integration-advanced.md → Recovering from Safe Mode](integration-advanced.md#recovering-from-safe-mode-connection-fails-because-of-compile-errors) for its per-platform path and the full recovery loop).

---

### Doctor — system diagnostics

```bash
# Full system report
unity doctor --format json

# Includes: platform info, auth status, installed editors, recent log lines, resolved proxy
unity doctor --tail 50
```

`unity doctor` reports real session state (matching `unity auth status`) and surfaces the resolved proxy URL, its source, and auth source. It also runs environment health checks and reports pass/warn per check (in every output format): whether the `unity` binary's directory is actually on `PATH` (the top post-install pitfall on Windows, where a new terminal is needed), whether multiple `unity` binaries shadow each other on `PATH`, whether Windows long-path support is enabled, and whether a git credential helper is configured (`git-credential-helper`: advisory for the git-token flows in `projects clone`/`create`/`link vcs`; the row is omitted on machines without git).

A **"Third-party components"** section lists every open-source runtime dependency the binary bundles — the .NET runtime plus each direct NuGet package — with its version and SPDX license identifier, and points at `https://spdx.org/licenses/` for the full texts (not embedded, to keep the binary small). It's generated from the CLI's own build configuration, so it can never drift from what actually shipped, and it's informational only — never a pass/fail check.

---

### CI init — scaffold a working CI workflow

`unity ci init` writes a committed CI workflow generated from the project. Use it instead of assembling the steps by hand — it wires up the CLI's own CI features in the right order and encodes the parts that are otherwise learned by trial and error.

```bash
# GitHub Actions -> .github/workflows/unity.yml (the default provider).
# With no --project-path, it works on the directory you ran it from.
unity ci init

# GitLab CI -> .gitlab-ci.yml
unity ci init --provider gitlab

# Scope the workflow to a build target (validated like `unity build --target`)
unity ci init --target Android

# Review it before committing to anything
unity ci init --dry-run

# Another project; replace a workflow that is already there
unity ci init --project-path ./MyProject --overwrite
```

The generated workflow installs the CLI, restores the editor download cache and the project's `Library/` folder, installs the editor version from `ProjectSettings/ProjectVersion.txt`, activates a license, runs `unity doctor --ci` as a preflight, runs the tests with a JUnit report, builds, uploads the report and the build, and returns the license seat in a teardown step that runs even when the job failed or was cancelled.

What it derives from the project: the editor version (rejected unless it parses as a real Unity version, since it is interpolated into an install command on the runner), and the editor module the build target needs where that is unambiguous — `Android`, `iOS`, `tvOS`, `WebGL`, `WindowsStoreApps`, `VisionOS`, `Lumin`. The standalone desktop targets get no module, because each has il2cpp / mono / server variants and picking one from the target alone would be a guess; asking for `StandaloneOSX` or `StandaloneWindows64` warns that the install step needs a `--module` you choose.

Secrets are referenced by name only. The command never reads a credential and the generated file never contains one — it expects `UNITY_SERVICE_ACCOUNT_ID`, `UNITY_SERVICE_ACCOUNT_SECRET`, and `UNITY_LICENSE_SERIAL` to exist as repository secrets, and reports which ones to add after it writes.

Two things about licensing that the workflow states in comments, because they surprise people: service-account auth covers Unity *services*, not the editor licence, and `unity license activate --personal` is rejected outright for a service account. The generated step therefore activates from a serial, and ships the floating-server and offline-`.ulf` alternatives as commented blocks beside it.

The project rides `--project-path`, like `list`, `status`, `mcp configure`, `pipeline install` and `collaboration` — not a positional operand as on `build` / `test` / `cache key`. It takes a path, not a registry project name, and defaults to the current directory, so the common case is a bare `unity ci init` from the project root.

**Interactive vs scripted.** On a real terminal, anything you didn't pass is asked for: a picker for the provider, one for the build target, and one for how many parallel jobs to split the tests across (all three open on their default, so Enter accepts), plus a confirm before replacing a workflow that already exists. A flag always wins over its prompt, so `--provider gitlab --target Android --shards 4` asks nothing.

Nothing prompts when any one of these holds — `--non-interactive` (or `UNITY_NON_INTERACTIVE`), a machine `--format` (`json`, `ndjson`, `tsv`), `--format github`, or a redirected stdout. In that case the unpassed values take their defaults (`github`, `StandaloneLinux64`, no sharding) and an existing workflow is an exit-2 error naming `--overwrite` rather than a question. That means a CI job or a script gets identical behaviour whether or not it happens to be attached to a terminal.

**Splitting the suite: `--shards <n>`.** Off by default, and offered by the picker on a terminal. Accepts 2 to 64 — `1` is rejected rather than generating a one-leg matrix, so there is no flag spelling for "no sharding"; omit it instead. More shards than tests is safe, because `unity test --shard` reports an empty slice and skips the editor instead of quietly running the whole suite.

```bash
unity ci init --shards 4
unity ci init --provider gitlab --shards 8
```

Two things the generated matrix handles that a hand-written one usually does not. `unity test --shard N/M` splits the suite using an NUnit report from an *earlier* run, because the Editor cannot enumerate tests without running them — so a cold pipeline has nothing to split. Rather than fail, one shard runs the whole suite to write that inventory while the rest stand down: the first run is green and costs exactly one full suite. And a *stale* inventory is the dangerous case, since a test added after it was written would belong to no shard and run nowhere — green, and not running the new test. So the cached inventory is bound to a hash of every `.cs`, `.asmdef`, `.asmref` and `packages-lock.json` in the repository, and any change to them re-seeds the suite in full. (`unity cache key` deliberately ignores script changes, which is right for a `Library/` cache and wrong for this, so the inventory gets its own key.)

The trade: a run that touches C# pays for one full unsharded suite, and a run that does not — assets, scenes, shaders, config, a retry, a scheduled build — is split `n` ways. Never slower than the unsharded workflow, and never under-tested. The build gets its own job, since inside the matrix it would run once per shard and burn `n` licence seats; both providers aggregate the per-shard JUnit reports themselves, so there is no merge step.

`--dry-run` prints the workflow to stdout and nothing else, so it pipes into a file or a diff. `--format json` reports the written paths in `data.paths`, plus the resolved provider, editor version, build target, and module. Exit 2 on an unknown `--provider`, an invalid `--target`, an existing workflow file without `--overwrite`, or a stray positional argument; exit 6 when the directory isn't a Unity project or its editor version can't be read. Templates are embedded in the binary, so this works offline and always matches the CLI it ships with.

---

### Doctor --ci — preflight before a long pipeline step

```bash
# First step of a CI job: fail in seconds instead of after a long build
unity doctor --ci

# Per-check results a workflow can branch on
unity doctor --ci --format json

# Failed checks become inline annotations on the pull request
unity doctor --ci --format github
```

`--ci` replaces the diagnostic report with a **preflight**: it verifies the environment can actually finish a build or test run and exits non-zero when it cannot, so a pipeline fails fast rather than tens of minutes into a build. It checks an activatable license, the project's required editor, free disk space, and reachability of the Unity services endpoint, and folds the `PATH` / long-path / git-credential-helper checks in as advisory rows that never fail a job.

Exit codes distinguish the two kinds of bad news, so a workflow can retry only what is worth retrying:

| Exit | Meaning |
|---|---|
| `0` | Every blocking check passed. Warnings do not fail the preflight. |
| `6` | A definitive failure — no license, the required editor is not installed, disk below the floor. Retrying will not help. |
| `7` | The preflight could not reach a verdict because a required service was unreachable. Worth a retry. |

A `6` outranks a `7` when both occur, so a real blocker is never reported as retryable.

Every check carries a machine-readable `code` (`LICENSE_NONE`, `EDITOR_NOT_INSTALLED`, `DISK_SPACE_LOW`, `NETWORK_UNREACHABLE`, …) plus a remediation `hint`. In `--format json` the per-check results stay in `data` even on failure, with one coded entry per failure in `errors`. Output is redacted and carries no tokens and no absolute user paths, so it is safe to paste into a public CI log.

`--ci` is always explicit — it is never inferred from `CI=true`, because a report that silently changed shape and exit code on a runner would be a trap. Note that the CLI already defaults to `--format tsv` whenever stdout is redirected, which in CI it usually is; that output leads with a `verdict` row.

---

### Diagnose proxy — proxy diagnostic report

```bash
# Print a redacted, paste-safe proxy diagnostic report for support
unity diagnose proxy

# Machine-readable
unity diagnose proxy --json
```

Reports the resolved proxy and where it came from, PAC configuration, CA bundle, and credential-store and Kerberos checks — redacted so it's safe to paste into a support ticket. A copy is also written to the logs directory. For per-request proxy logging over the course of a repro, use the global `--log-proxy` flag (or `UNITY_LOG_PROXY=1`), which writes one redacted entry per outbound request to `proxy-request.json`.

---

### Environment

```bash
# Show environment paths
unity env --format json

# Returns: user data path, editor install path, download cache path, config path, CLI version, resolved proxy
```

---

### Version — machine-readable version object

```bash
# Bare version string (backward compatible, unchanged)
unity --version

# Structured version object
unity version --format json

# The --version flag also honors an explicit format set before it
unity --format json --version
```

`unity version` reports the CLI version, release channel (`stable`, `beta`, …), commit, platform, architecture, and runtime — the same version fact `env` and `doctor` carry, as one parseable object. It honors `--format` (`human`, `json`, `tsv`, `ndjson`); human output is the bare version string.

A bare `unity --version` still prints only the version string, so scripts that parse it are unaffected. The `--version` flag switches to the structured object only when an explicit machine format is given *before* it (`unity --json --version`, `unity --format json --version`, or `UNITY_FORMAT=json unity --version`) — the flag terminates parsing, so a trailing format after `--version` is not seen. For order-independent machine output, prefer the `unity version` subcommand.

---

### Cache

```bash
# Show cache location and size
unity cache info --format json

# Clear download cache
unity cache clean --yes
```

---

### Cache key — deterministic key for CI cache steps

`unity cache key` prints one hash derived from the inputs that actually invalidate a project's build cache: the editor version, the resolved package set, and (optionally) the build target. Use it as the `key:` of a CI cache step instead of hand-rolling the hashing.

```bash
# Print the key for the project in the current directory
unity cache key

# Scope it to a build target — a Library/ folder is platform-specific
unity cache key --target Android

# Any project path
unity cache key ./MyProject
```

Whenever stdout is not an interactive terminal — a pipe, a redirect, or any CI runner — the key is the only thing written to it, so it drops straight into a shell substitution or a workflow expression. (On an interactive colour terminal the CLI's usual one-line banner still prints above it, as it does for every command; `--quiet` suppresses that.)

Pass the project path if the workflow's working directory isn't the project:

```yaml
- id: cachekey
  run: echo "key=$(unity cache key MyProject --target Android)" >> "$GITHUB_OUTPUT"
- uses: actions/cache@v4
  with:
    path: MyProject/Library
    key: Library-${{ steps.cachekey.outputs.key }}
```

What moves the key, and what doesn't:

- **Changes** when the editor version (`ProjectSettings/ProjectVersion.txt`), the package set (`Packages/packages-lock.json`, falling back to `Packages/manifest.json`), or `--target` changes.
- **Does not change** for unrelated edits — scenes, scripts, assets, or project settings other than the version file.
- **Is identical across machines and operating systems** for the same inputs. Line endings and a UTF-8 BOM are normalized before hashing, so a Windows checkout with `core.autocrlf` and a Linux one agree. No paths, usernames, or timestamps enter the hash.

`--format json` returns the key plus each component's raw value and hash, so one invocation can build a layered key with fallback levels:

```bash
unity cache key --target Android --format json
```

`--component <editor|packages|target>` prints just one component's hash for the fallback levels themselves. Under `--format json` it also sets `data.key` to that component, so `jq -r .data.key` means the same thing either way:

```bash
# Restore any cache built for this editor, whatever the packages were
unity cache key --component editor
```

Runs without an editor and without network access. Exit 2 on an unknown `--target` or `--component` (a silently-accepted typo would produce a key nothing else matches); exit 6 when the directory isn't a Unity project. A project with neither package file still emits a key, with a warning that it doesn't cover the package set.

---

### Analytics — usage/telemetry consent

The CLI defaults to **opt-out**. On the first interactive run a prompt is shown once before any data is collected; it now requires an explicit `y` or `n` — pressing Enter alone re-asks instead of silently recording the opt-out default, so an accidental keystroke can't lock in an answer. Ctrl-C skips the prompt and keeps the opt-out default. Non-interactive, CI, piped, and `--quiet` contexts silently keep the opt-out default.

Running `unity analytics opt-in` or `opt-out` permanently answers the first-run prompt, so a choice recorded from a script (where the prompt never appears) isn't asked again on the next interactive run. To suppress the prompt *without* recording a choice — for a wrapper script on an interactive terminal that must never absorb it — set `UNITY_NO_CONSENT_PROMPT` (analytics stay off until you explicitly opt in).

```bash
# Show current consent status
unity analytics status
unity analytics status --format json

# Opt in to anonymous usage data collection
unity analytics opt-in

# Opt out (the default)
unity analytics opt-out
```

Consent is stored in the shared Hub privacy preferences, so opting out in the CLI also opts out in Hub, and vice versa. When opted **in**, the CLI records which commands run (registered command names only — never your arguments, paths, or project names), editor uninstalls, project open/create (editor version and template id only), CLI self-update/uninstall outcomes, `unity shell` and `unity mcp` session usage, and `unity doctor` / `unity bug` results. When opted out (the default), no events are sent.

Separately from analytics, the CLI reports **anonymous crashes and errors** via Sentry to help fix bugs (no IP address or hostname; home-directory paths and token-like values scrubbed before send), aligned with the Unity Hub. Opting in to analytics additionally attaches an anonymized machine id so crash-free-user rates can be computed; opted-out users stay fully anonymous. Set `UNITY_NO_CRASH_REPORT` to disable crash reporting entirely.

Separately again, every run — including one that never sees the consent prompt (CI, an agent, a machine `--format`) — sends one anonymous `cli telemetry` usage ping, so total CLI usage can be counted regardless of the analytics opt-in state. It is not a measure of who opted in and carries no user, machine, or session identifier (a fresh random id per invocation, never persisted) and no consent state — just which Unity tool sent it, the CLI version, and whether the run looks like CI. Set `UNITY_NO_CLI_INVOKED_TELEMETRY=1` to disable it. `unity analytics status` discloses it alongside the opt-in state.

---

### Changelog

Show the embedded release notes for the currently installed CLI version:

```bash
unity changelog
unity changelog --format json
```

---

### Language

```bash
# Show current language and available options
unity language

# Set language by code
unity language --set en
unity language --set ja
unity language --set zh-hans

# Alias
unity lang --set ko
```

On a TTY with no flags, shows an interactive selection prompt. `--set` accepts common spellings of a language code — BCP-47 (`ja-JP`), locale (`ja_JP`), a bare language (`ja`), or a bare region (`jp`) — and resolves them case-insensitively when the match is unambiguous (`zh` still asks you to pick `zh_cn` or `zh_tw`). Display names and ordering come from the shared Hub language catalog. The regional variants Spanish (Latin America), French (Canada), and Portuguese (Portugal) are no longer offered; Spanish, French, and Portuguese (Brazil) remain.

---

### Completion — shell tab completion

Generate and install shell completion scripts:

```bash
# Supported shells: bash, zsh, fish, powershell
unity completion bash
unity completion zsh
unity completion fish
unity completion powershell
```

---

### Bug — report a bug

Interactive bug reporter that collects system info and recent logs, then submits to Unity:

```bash
# Interactive — prompts for each field
unity bug

# Non-interactive — supply the report through flags (works from scripts, CI, piped shells)
unity bug \
  --title "Editor crashes on project open" \
  --description "Opening MyGame hard-crashes the editor." \
  --steps "Open the CLI" --steps "Run unity open MyGame" --steps "Editor window closes" \
  --reproducibility always \
  --email you@example.com \
  --attachments ./crash.log ./notes.txt \
  --share-project .
```

Prompts for title, description, email, and reproducibility level. As of `0.1.0-beta.8` it collects the same diagnostic system information as the Unity Hub bug reporter (including GPU details).

The report can also be supplied entirely through flags — `--title`, `--description`, `--steps` (repeatable, one line per value), `--reproducibility <first-time|sometimes|always>`, and `--email` (defaults to your Unity account email when signed in; otherwise required). On a terminal, any flags you pass skip their prompts and the remaining fields still ask; a non-interactive run submits without prompting. A non-interactive run with missing or invalid fields fails fast with a usage error (exit 2) listing the exact flags to add.

Use `--attachments <paths...>` (repeatable) to attach extra files — for example a crash log or a zipped copy of a subset of assets. Each path must be an existing, readable file; a folder is rejected (zip it yourself first), and a missing or unreadable path fails fast with a usage error (exit 2) naming the offending path.

Use `--share-project <path>` (use `.` for the current directory) to attach a copy of the Unity project the bug is about — the same stripped-project packaging the Editor's bug reporter uses. It sends the source folders plus a slimmed `Library`, excluding the regenerable caches and build output (`Library` caches, `Temp`, `Build`, `Logs`, VCS/IDE metadata, `MemoryCaptures`, `CrashReports`), so you don't have to zip the project yourself. A path that isn't a Unity project fails fast with exit 2. The archive is streamed from disk during upload, so there's no size limit — even a multi-gigabyte project copy uploads without being buffered in memory.

Interactively, when you don't pass `--attachments` or `--share-project`, the reporter asks whether to attach files and whether to include a project copy. Everything — attachments and the project copy — is bundled into the same archive as the auto-collected logs.

---

### Self-update — update the CLI itself

```bash
# Check for available updates
unity self-update --check --format json

# Show changelog for the new version
unity self-update --changelog

# Update (interactive confirmation)
unity self-update

# Update without prompts
unity self-update --yes

# Install a specific version
unity self-update --target 0.2.0

# Select update channel (stable or beta)
unity self-update --channel beta

# Dry-run: show what would change
unity self-update --dry-run

# Rollback to previous version
unity self-update --rollback
```

`unity self-update` detects how the CLI was installed and updates accordingly (`unity upgrade` is still accepted as an alias):

- **`curl | sh` install** — keeps updating itself in place.
- **Linux AppImage** — updates in place: downloads the new `.AppImage` artifact, verifies its checksum against the release manifest, and atomically replaces the AppImage you launched (`--rollback` restores the previous one). The embedded zsync update info is preserved, so external updaters (AppImageUpdate, Gear Lever) keep working.
- **Package-manager install** — points you at the owning manager instead of replacing the binary. The `.deb` and `.rpm` packages are published to Unity's apt and rpm repositories on every beta and GA release (rpm packages are GPG-signed), so a package-managed install stays current through the system package manager: `sudo apt update && sudo apt upgrade unity-cli` on Debian/Ubuntu, `sudo dnf upgrade unity-cli` on Fedora/RHEL.

`--check`, `--changelog`, and `--dry-run` work everywhere. The background "update available" notice is package-manager-aware: when the release manifest says your install's package manager already carries the new version, the notice suggests that manager's exact upgrade command instead of `unity self-update`; installs whose manager doesn't carry the release yet stay quiet.

The same background check also warns when a self-installed `unity` and a Homebrew-managed one are **both** on `PATH` — a state where `which unity` keeps resolving to the self-installed copy while `brew upgrade` updates the other, so the two silently drift apart. The notice points at `unity self-uninstall -y` to remove the self-installed copy so Homebrew is the only one managing updates going forward.

---

### Self-uninstall — remove the CLI

```bash
# Uninstall the CLI (interactive confirmation)
unity self-uninstall

# Uninstall without prompts
unity self-uninstall --yes

# Also remove config and data files
unity self-uninstall --purge --yes

# Dry-run: show what would be removed
unity self-uninstall --dry-run
```

> **`unity implode` was removed** in `0.1.0-beta.8` (it was previously a deprecated alias). Use `unity self-uninstall`.

---

