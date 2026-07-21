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

On a TTY with no flags, shows an interactive selection prompt. The regional variants Spanish (Latin America), French (Canada), and Portuguese (Portugal) are no longer offered; Spanish, French, and Portuguese (Brazil) remain.

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
unity bug
```

Prompts for title, description, email, and reproducibility level. As of beta.8 it collects the same diagnostic system information as the Unity Hub bug reporter (including GPU details).

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

`unity upgrade` detects how the CLI was installed: the `curl | sh` install keeps upgrading itself in place, while on a package-manager install it points you at the owning manager instead of replacing the binary (and the background "update available" notice is suppressed there). `--check`, `--changelog`, and `--dry-run` still work everywhere.

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

