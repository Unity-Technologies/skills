---
name: cli
description: Use when interacting with Unity CLI from the terminal — install or uninstall editors, list or open projects, manage modules, check auth status, read logs, browse Unity releases, or run any other Unity CLI operation.
allowed-tools:
  - Bash
---

# Unity CLI

## Step 1: Install the CLI (if not already installed)

First check if the CLI is available:

```bash
which unity && unity --version
```

If not found, install it:

**macOS / Linux**
```bash
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
```

**Windows (PowerShell)**
```powershell
$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
```

After installing, open a new shell so `unity` is on PATH, then verify:

```bash
unity --version
```

If the install script fails or the binary is still not found, tell the user and stop.

## Step 2: Verify it works

```bash
unity --version
```

If this fails with a permissions error or crash, the CLI installation may be broken. Suggest re-running the install script.

---

## Global flags

These work on every command:

| Flag | Description |
|---|---|
| `--format <fmt>` | Output format: `human` (default), `json`, `tsv`. Also via `UNITY_FORMAT` env var. |
| `--no-banner` | Suppress the branded header — use in scripts |
| `--non-interactive` | Disable all interactive prompts — use in CI |
| `--quiet` | Suppress non-essential output |
| `--verbose` | Print full error details (stack trace + cause chain) on failure. Also via `UNITY_VERBOSE`. |

**Always use `--format json` when you need to parse output programmatically.**

## Environment variables

All CLI env vars use the `UNITY_` prefix. A CLI flag always overrides the corresponding env var.

| Variable | Mirrors flag | Description |
|---|---|---|
| `UNITY_FORMAT` | `--format` | Output format (`human`, `json`, `tsv`). `HUB_FORMAT` is a deprecated alias. |
| `UNITY_EDITOR_VERSION` | `--editor-version` | Editor version (e.g. `2023.3.0f1`, `latest`, `lts`). |
| `UNITY_ARCHITECTURE` | `--architecture` | Chip architecture (`x86_64`, `arm64`). |
| `UNITY_PROJECT_PATH` | path argument | Project path for the `open` command. |
| `UNITY_QUIET` | `--quiet` | Suppress non-essential output. |
| `UNITY_VERBOSE` | `--verbose` | Show full error details on failure. |
| `UNITY_NON_INTERACTIVE` | `--non-interactive` | Disable interactive prompts. |
| `UNITY_NO_BANNER` | `--no-banner` | Suppress the branded banner. |
| `UNITY_RUN_TIMEOUT` | `--timeout` | Timeout for `unity run` in seconds. |
| `UNITY_SERVICE_ACCOUNT_ID` | — | Service account client ID for non-interactive (CI) auth. |
| `UNITY_SERVICE_ACCOUNT_SECRET` | — | Service account client secret for non-interactive (CI) auth. |

**CI service account auth:** Set both `UNITY_SERVICE_ACCOUNT_ID` and `UNITY_SERVICE_ACCOUNT_SECRET` to skip the browser OAuth flow. Equivalent to `unity auth login --client-id <id> --client-secret <secret>`.

## Getting help

If a command fails or you're unsure of the available options, append `-h` or `--help` to any command or subcommand:

```bash
unity --help
unity install --help
unity projects --help
unity projects create --help
```

This works at every level of the command hierarchy.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | General error |
| 2 | Bad arguments |
| 3 | Authentication failure |
| 6 | Command-specific failure |
| 130 | Interrupted (Ctrl+C) |

---

## Commands

### Auth

```bash
# Check login status
unity auth status --format json

# Login (opens browser for OAuth)
unity auth login

# Logout
unity auth logout
```

---

### Editors — list, install, uninstall

```bash
# List all editors (installed + available releases)
# Short alias: unity e
unity editors --format json

# List only installed editors
unity editors --installed --format json

# List only available releases
unity editors --releases --format json

# Filter by architecture
unity editors --installed --architecture arm64 --format json

# Show detailed module info
unity editors --verbose

# Watch mode — live-updates as editors are installed or removed
unity editors --watch
unity editors --installed --watch
```

#### editors add

Register one or more existing editor installations by path:

```bash
unity editors add /path/to/Unity/Editor

# Register multiple at once
unity editors add /path/one /path/two

# Skip macOS code-signature check (useful for unsigned or side-loaded builds)
unity editors add /path/to/Unity/Editor --skip-signature-check
```

#### editors default

```bash
# Show current default editor
unity editors default --format json

# Set default by version, alias, or keyword
unity editors default 6000.0.47f1
unity editors default latest
unity editors default lts

# Clear the default
unity editors default --unset
```

On a TTY with no arguments, shows an interactive selection prompt.

#### editors install-path

```bash
# Show current editor install path
unity editors install-path

# Set a new install path
unity editors install-path --set /path/to/editors
```

Also available as the top-level `unity install-path` (with an additional `--get` flag).

#### editors info

```bash
# Show release details for a specific version
unity editors info 6000.0.47f1 --format json
```

#### editors module / editor module

Module management is exposed under **both** `editors module` and the `editor` (singular) command group. Both share the same subcommands:

```bash
# List modules for an installed editor
unity editors module list 6000.0.47f1 --format json
unity editor module list 6000.0.47f1 --architecture arm64 --format json

# Add modules to an installed editor
unity editors module add 6000.0.47f1 --module android --module ios
unity editors module add 6000.0.47f1 --all          # Install every available module
unity editors module add 6000.0.47f1 --module android --child-modules   # Include child modules
unity editors module add 6000.0.47f1 --module android --accept-eula      # Accept EULAs automatically

# Refresh module list for a manually located editor
unity editors module refresh 6000.0.47f1
```

#### editor add (single path, with module-fetch control)

The `editor add` subcommand is similar to `editors add` but targets a single path and supports skipping the module-fetch step:

```bash
unity editor add /path/to/Unity/Editor

# Skip fetching module metadata (faster, but modules won't be listed until refreshed)
unity editor add /path/to/Unity/Editor --no-fetch-modules
```

---

### Install

```bash
# Install an editor (interactive version selection if omitted)
unity install 6000.0.47f1

# Install with specific modules
unity install 6000.0.47f1 --module windows-mono --module android

# Install a specific changeset by hash
unity install 6000.0.47f1 --changeset abc123def456

# Include child modules
unity install 6000.0.47f1 --cm

# Exclude child modules
unity install 6000.0.47f1 --no-cm

# Install and accept EULAs automatically (CI)
unity install 6000.0.47f1 --yes --accept-eula

# Force reinstall even if already present
unity install 6000.0.47f1 --force

# Resume an interrupted download
unity install 6000.0.47f1 --resume

# Dry-run: show what would be installed without doing it
unity install 6000.0.47f1 --dry-run --format json
```

### Uninstall

```bash
# Uninstall an editor version
unity uninstall 6000.0.47f1 --yes

# Uninstall a specific architecture
unity uninstall 6000.0.47f1 --architecture arm64 --yes
```

---

### Modules — add/list per editor

```bash
# List modules for an installed editor
unity modules list 6000.0.47f1 --format json

# Filter by architecture
unity modules list 6000.0.47f1 --architecture arm64 --format json
```

### install-modules

```bash
# List available modules without installing
unity install-modules --editor-version 6000.0.47f1 --list

# Install specific modules
unity install-modules --editor-version 6000.0.47f1 --module android --module ios

# Install all available modules
unity install-modules --editor-version 6000.0.47f1 --all --yes

# Include child modules (default behaviour)
unity install-modules --editor-version 6000.0.47f1 --module android --cm

# Exclude child modules
unity install-modules --editor-version 6000.0.47f1 --module android --no-cm

# Accept EULAs and dry-run
unity install-modules --editor-version 6000.0.47f1 --all --accept-eula --dry-run
```

`--list` and `--all` are mutually exclusive. `--list` is also mutually exclusive with `--module`.

---

### Projects — list, open, create, register

```bash
# List registered projects
unity projects list --format json

# Register an existing project
unity projects add /path/to/MyProject

# Remove from registry (does not delete files)
unity projects remove /path/to/MyProject

# Show project details
unity projects info /path/to/MyProject --format json

# Open a project in the editor
unity open /path/to/MyProject

# Open with a specific editor version
unity open /path/to/MyProject --editor-version 6000.0.47f1

# Pass extra Unity arguments
unity open /path/to/MyProject --args "-logFile output.log"

# Version shorthand (equivalent to open with --editor-version)
unity 6000.0.47f1 /path/to/MyProject
```

#### projects create

Create a project at a given path (non-interactive):

```bash
unity projects create /path/to/NewProject --editor-version 6000.0.47f1 --template com.unity.template.3d
```

#### projects new

Interactive project creation wizard:

```bash
# Launch interactive wizard (prompts for path, editor version, template)
unity projects new

# Pre-fill options to skip individual prompts
unity projects new --path /path/to/NewProject --editor-version 6000.0.47f1 --template com.unity.template.3d

# Open the project immediately after creation
unity projects new --open
```

#### projects pin / unpin

```bash
# Pin a project to the top of the list
unity projects pin /path/to/MyProject

# Unpin
unity projects unpin /path/to/MyProject
```

#### projects require

Ensure the editor version required by a project is installed, installing it if needed:

```bash
unity projects require /path/to/MyProject --yes
```

On a TTY with no path, prompts interactively.

#### projects upgrade

Upgrade a project to a different Unity editor version:

```bash
unity projects upgrade /path/to/MyProject --yes
```

#### projects export / import

```bash
# Export the project registry to a file (or stdout if -o is omitted)
unity projects export -o projects.json

# Import a previously exported registry
unity projects import projects.json
unity projects import --input projects.json
```

---

### Releases — browse Unity versions

```bash
# List recent releases
unity releases --format json

# Filter by stream (alpha, beta, lts, tech)
unity releases --stream lts --format json
unity releases --stream tech --format json
unity releases --stream beta --format json

# LTS only shorthand
unity releases --lts --format json

# Filter from a year onward
unity releases --since 2023 --format json

# Paginate
unity releases --limit 10 --skip 20 --format json
```

---

### Templates

```bash
# List templates for an editor version (uses default editor if --editor is omitted)
unity templates list --editor 6000.0.47f1 --format json

# List only locally installed templates
unity templates list --editor 6000.0.47f1 --installed --format json

# Show template details
unity templates info com.unity.template.3d --editor 6000.0.47f1 --format json
```

---

### Run — batch/headless execution

```bash
# Run a Unity project in batch mode and wait for exit
unity run /path/to/MyProject -- -executeMethod Builder.Build -quit

# Override editor version
unity run /path/to/MyProject --editor-version 6000.0.47f1 -- -batchmode -logFile out.log

# Install editor automatically if missing
unity run /path/to/MyProject --allow-install -- -executeMethod Builder.Build

# Kill the Unity process after 300 seconds (useful in CI to prevent hangs)
unity run /path/to/MyProject --timeout 300 -- -batchmode -quit
# Equivalent via env var:
UNITY_RUN_TIMEOUT=300 unity run /path/to/MyProject -- -batchmode -quit
```

`unity run` passes everything after `--` directly to the Unity executable and returns the editor's exit code.

When `--timeout <seconds>` is set, the process receives SIGTERM at the deadline; if still alive after 2 s it receives SIGKILL. The command exits with code 6 (EXIT_COMMAND_FAILURE) on timeout.

---

### Build

```bash
# Build a project (requires --target and --execute-method)
unity build /path/to/MyProject \
  --target StandaloneOSX \
  --execute-method Builder.PerformBuild \
  --output-path ./build/output

# Common build targets: StandaloneOSX, StandaloneWindows64, StandaloneLinux64, Android, iOS, WebGL

# With --format json, stdout includes newline-delimited JSON progress frames before the final envelope:
unity build /path/to/MyProject --target StandaloneOSX --execute-method Builder.Build --format json
# Output (each line is a JSON object):
# {"type":"progress","command":"build","message":"Resolving project..."}
# {"type":"progress","command":"build","message":"Resolving editor..."}
# {"type":"progress","command":"build","message":"Starting Unity..."}
# {"type":"progress","command":"build","message":"Unity exited (code 0)"}
# { "success": true, "command": "build", "data": { "target": "...", "logFile": "..." } }
```

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

---

### Doctor — system diagnostics

```bash
# Full system report
unity doctor --format json

# Includes: platform info, auth status, installed editors, recent log lines
unity doctor --tail 50
```

---

### Environment

```bash
# Show environment paths
unity env --format json

# Returns: user data path, editor install path, download cache path, config path, CLI version
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

On a TTY with no flags, shows an interactive selection prompt.

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

Prompts for title, description, email, and reproducibility level.

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

---

### Pipeline — Unity Editor automation (experimental)

The `pipeline` command manages the Unity Pipeline package, which enables programmatic control of running Unity Editor instances. Alias: `pipe`.

```bash
# List all running Unity Editor instances and their Pipeline package status
unity pipeline list --format json

# Install the Pipeline package into a project (auto-detects project if omitted)
unity pipeline install
unity pipeline install --project-path /path/to/MyProject

# Use SSH instead of HTTPS when cloning the package
unity pipeline install --project-path /path/to/MyProject --ssh
```

**Requires Unity 6.0 or higher.** The Pipeline package is cloned as an embedded package into `Packages/com.unity.pipeline/`.

---

### Request — send commands to a running Unity Editor (experimental)

The `request` command communicates with a running Unity Editor that has the Pipeline package installed. Alias: `req`.

```bash
# List all commands available on the connected Unity Editor
unity request
unity request --format json

# Execute a specific command
unity request editor_play
unity request log_editor "Hello from CLI"
unity request editor_status --includeMemory true

# Target a specific project or instance
unity request editor_play --project-path /path/to/MyProject
unity request editor_play --instance localhost:8765

# Connect to a Unity Player runtime instance
unity request <command> --runtime "MyGame"
unity request <command> --runtime-path /path/to/port-file

# Set a timeout (default: 30 seconds)
unity request editor_play --timeout 60
```

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

> **`unity implode` is a deprecated alias for `unity self-uninstall`.** It prints a deprecation warning to stderr. Use `unity self-uninstall` instead.

---

## Common workflows

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

### CI: headless build

```bash
unity run /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --allow-install \
  -- -batchmode -quit -executeMethod Builder.PerformBuild -logFile build.log
echo "Exit code: $?"
```

### Debug the CLI

```bash
# Check auth + installed editors + recent errors in one command
unity doctor --format json

# Follow live logs during an install
unity logs --follow --level info
```

---

## Notes

- `--non-interactive` and `--yes` together suppress all prompts — use both in CI.
- `--format json` always produces machine-readable output; prefer it over parsing human text.
- `unity <version> [path]` is a shorthand for `unity open [path] --editor-version <version>`. Works with `lts`, `latest`, or a full version string like `6000.0.47f1`.
- The CLI supports kubectl-style plugins: any `unity-<name>` binary on PATH is callable as `unity <name>`.
- The CLI is currently in **beta**. Once GA ships, the `UNITY_CLI_CHANNEL=beta` part of the install command can be dropped.
