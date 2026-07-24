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

---

### Doctor — system diagnostics

```bash
# Full system report
unity doctor --format json

# Includes: platform info, auth status, installed editors, recent log lines, resolved proxy
unity doctor --tail 50
```

`unity doctor` reports real session state (matching `unity auth status`) and surfaces the resolved proxy URL, its source, and auth source. It also runs environment health checks and reports pass/warn per check (in every output format): whether the `unity` binary's directory is actually on `PATH` (the top post-install pitfall on Windows, where a new terminal is needed), whether multiple `unity` binaries shadow each other on `PATH`, and whether Windows long-path support is enabled.

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

### Cache

```bash
# Show cache location and size
unity cache info --format json

# Clear download cache
unity cache clean --yes
```

---

### Analytics — usage/telemetry consent

The CLI defaults to **opt-out**. On the first interactive run a prompt is shown once before any data is collected; it now requires an explicit `y` or `n` — pressing Enter alone re-asks instead of silently recording the opt-out default, so an accidental keystroke can't lock in an answer. Ctrl-C skips the prompt and keeps the opt-out default. Non-interactive, CI, piped, and `--quiet` contexts silently keep the opt-out default.

Running `unity analytics opt-in` or `opt-out` **permanently answers the first-run prompt** — a choice recorded from a script (where the prompt never appears) is honored on the next interactive run instead of being asked again. For wrapper scripts on an interactive terminal that must never absorb a prompt but shouldn't record a choice either, set `UNITY_NO_CONSENT_PROMPT` (any value) — it suppresses only the prompt and changes nothing else (unlike `UNITY_NON_INTERACTIVE`).

When opted **in**, collected usage data covers: which commands run (registered command names only — never arguments, paths, or project names), install/uninstall and project open/create outcomes (editor version and template id only), CLI self-upgrade/uninstall results, `unity shell` / `unity mcp` session usage, and `unity doctor` / `unity bug` results. When opted out (the default), no events are sent.

```bash
# Show current consent status
unity analytics status
unity analytics status --format json

# Opt in to anonymous usage data collection
unity analytics opt-in

# Opt out (the default)
unity analytics opt-out
```

Consent is stored in the shared Hub privacy preferences, so opting out in the CLI also opts out in Hub, and vice versa.

---

### Crash reporting

Separately from usage analytics, the CLI reports crashes and unexpected command failures to Sentry — **anonymous** events for all users: no IP address, no hostname, home-directory paths and token-like values scrubbed before send. Analytics opt-in additionally attaches an anonymized machine id (for crash-free-user rates); opted-out users stay fully anonymous. Set `UNITY_NO_CRASH_REPORT` (any value) to disable crash reporting entirely.

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

# Set language by code — common spellings are accepted and resolved case-insensitively:
# BCP-47 (ja-JP), locale (ja_JP), bare language (ja), or bare region (jp)
unity language --set ja
unity language --set ja-JP
unity language --set pt          # resolves to pt_br

# Alias
unity lang --set ko
```

On a TTY with no flags, shows an interactive selection prompt. Ambiguous inputs still ask you to pick (`zh` → choose `zh_cn` or `zh_tw`). The regional variants Spanish (Latin America), French (Canada), and Portuguese (Portugal) are no longer offered; Spanish, French, and Portuguese (Brazil) remain.

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

Bug reporter that collects system info and recent logs, then submits to Unity:

```bash
# Interactive: prompts for title, description, email, and reproducibility level
unity bug

# Non-interactive: supply the report through flags — works from scripts, CI, and piped shells
unity bug --title "Install hangs at 99%" \
  --description "unity install 6000.0.47f1 hangs on the android module" \
  --steps "Run unity install 6000.0.47f1 -m android" --steps "Wait for 99%" \
  --reproducibility always \
  --email dev@example.com
```

Flags: `--title`, `--description`, `--steps` (repeatable, one line per value), `--reproducibility <first-time|sometimes|always>`, `--email` (defaults to your Unity account email when signed in; otherwise required). On a terminal, any flags you pass skip their prompts and the remaining fields still ask. A non-interactive run with missing or invalid fields fails fast with a usage error (exit 2) listing the exact flags to add. It collects the same diagnostic system information as the Unity Hub bug reporter (including GPU details).

---

### Upgrade — update the CLI itself

```bash
# Check for available updates
unity upgrade --check --format json

# Show changelog for the new version
unity upgrade --changelog

# Upgrade (interactive confirmation)
unity upgrade

# Upgrade without prompts
unity upgrade --yes

# Install a specific version
unity upgrade --target 0.2.0

# Select update channel (stable or beta)
unity upgrade --channel beta

# Dry-run: show what would change
unity upgrade --dry-run

# Rollback to previous version
unity upgrade --rollback
```

`unity upgrade` detects how the CLI was installed: the `curl | sh` install keeps upgrading itself in place, while on a package-manager install it points you at the owning manager instead of replacing the binary. When the release manifest says your package manager carries the new version, the background update notice suggests that manager's exact upgrade command (e.g. `sudo apt update && sudo apt upgrade unity-cli`). **AppImage installs upgrade in place**: the new `.AppImage` is downloaded, checksum-verified against the release manifest, and atomically swapped for the file you launched (`--rollback` restores the previous one) — except an AppImage installed through Homebrew's cask, which is left to `brew upgrade unity-cli`. `--check`, `--changelog`, and `--dry-run` work everywhere.

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

