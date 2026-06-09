---
name: unity-cli
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
| `--format <fmt>` | Output format: `human` (default), `json`, `tsv`, `ndjson`. Also via `UNITY_FORMAT` env var. |
| `--no-banner` | Suppress the branded header — use in scripts |
| `--non-interactive` | Disable all interactive prompts — use in CI |
| `--quiet` | Suppress non-essential output |
| `--verbose` | Print full error details (stack trace + cause chain) on failure. Also via `UNITY_VERBOSE`. |
| `--proxy <url>` | HTTP/HTTPS/SOCKS/PAC proxy URL for this invocation. Also via `UNITY_PROXY`. Takes precedence over standard `HTTPS_PROXY`/`HTTP_PROXY`/`ALL_PROXY` env vars and the persisted `proxy.json` setting. |
| `--proxy-disable` | Disable proxy for this invocation, ignoring all sources (env vars, persisted config, system settings). |

**Always use `--format json` when you need to parse output programmatically.**

## Environment variables

All CLI env vars use the `UNITY_` prefix. A CLI flag always overrides the corresponding env var.

| Variable | Mirrors flag | Description |
|---|---|---|
| `UNITY_FORMAT` | `--format` | Output format (`human`, `json`, `tsv`, `ndjson`). `HUB_FORMAT` is a deprecated alias. |
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
| `UNITY_PROXY` | `--proxy` | HTTP/HTTPS/SOCKS/PAC proxy URL. Takes precedence over `HTTPS_PROXY`/`HTTP_PROXY`/`ALL_PROXY` and the persisted `proxy.json` setting. |

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

# Login with service account credentials (CI — skips browser)
# Preferred: read secret from stdin to avoid shell-history and process-list exposure
unity auth login --client-id <id> --secret-from-stdin

# Alternative: pass secret directly (visible in shell history and process list)
unity auth login --client-id <id> --client-secret <secret>

# Login without persisting credentials to the keyring (ephemeral CI)
unity auth login --client-id <id> --secret-from-stdin --no-store

# Logout (clears both service-account and OAuth credential slots)
unity auth logout

# Skip the confirmation prompt
unity auth logout --yes
```

**Service-account credentials via env vars** (`UNITY_SERVICE_ACCOUNT_ID` + `UNITY_SERVICE_ACCOUNT_SECRET`) mint bearer tokens automatically for the duration of the process — no browser round-trip, no keyring write. If only one of the two is set, the CLI prints a warning on stderr instead of silently falling back to the keyring/OAuth identity.

The interactive `unity auth login` flow now prints the sign-in URL to the terminal **before** attempting to launch the browser, which unblocks remote/headless sessions (SSH, containers, dev VMs) where `xdg-open` / `open` has no graphical session to attach to. With `--format json`, an `auth_url=…` progress frame is emitted so machine consumers can capture the URL without parsing human text.

---

### Cloud — Unity Cloud organizations and projects

Requires being signed in (`unity auth login`).

```bash
# Show cloud sign-in state and active organization
unity cloud status --format json

# Organizations
unity cloud org list --format json
unity cloud org current                       # print the active default org id
unity cloud org set-default <id-or-name>      # set active default org
unity cloud org clear-default                 # revert to "All Organizations"

# Projects in the active organization
unity cloud project list --format json

# Override the active organization for a single call
unity cloud project list --cloud-org <id-or-name>   # also via UNITY_CLOUD_ORG env var
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

# Resume an interrupted download (also recovers orphaned partials left by a crash or kill)
unity install 6000.0.47f1 --resume

# Dry-run: show what would be installed without doing it
unity install 6000.0.47f1 --dry-run --format json

# Space-separated module values after a single -m are equivalent to repeating -m
unity install 6000.0.47f1 -m android ios          # space-separated
unity install 6000.0.47f1 -m android -m ios       # repeated flag (same effect)
```

**NDJSON progress frames** for `unity install` and `unity install-modules` include a `phase: 'download' | 'install'` field so scripts can switch to an indeterminate spinner during the install phase (which is genuinely indeterminate — NSIS on Windows only reports success/failure). During the install phase, `pct` is locked at 50 and only jumps to 100 on completion. Module download/install progress is nested under the parent editor via `parentItemUid`, so consumers see one editor group with its modules rather than one group per module.

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

`--module android ios` (space-separated values after a single `--module`) and `--module android --module ios` (repeated flag) are equivalent — both install all listed modules.

Module discovery works for editors registered via `unity editors add <path>` (located editors), not just editors installed by the Hub.

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

# Pass a build target (forwarded to Unity as -buildTarget / -buildTargetGroup)
unity open /path/to/MyProject --build-target StandaloneOSX
unity open /path/to/MyProject --build-target-group Standalone

# Use a specific editor binary instead of resolving by version
unity open /path/to/MyProject --editor-path "/Applications/Unity/Hub/Editor/6000.0.47f1/Unity.app"

# Version shorthand (equivalent to open with --editor-version)
unity 6000.0.47f1 /path/to/MyProject
```

The project argument is matched against the Hub registry first (exact name or path opens immediately; a glob like `"My Game*"` prompts when multiple match); with no registry match it falls back to treating the argument as a filesystem path.

#### projects create

Create a project. On a TTY, prompts for any missing options (parent directory, editor version, template). In CI, pass `--non-interactive` or pipe stdin to suppress prompts and rely on stored defaults. The first positional argument is the project **name**; `--path` sets the parent directory:

```bash
unity projects create MyGame --editor-version 6000.0.47f1 --template com.unity.template.3d

# Place the project in a specific directory
unity projects create MyGame --path /path/to/projects --editor-version 6000.0.47f1
```

#### projects new

Create a project without any interactive prompts — resolves missing options from stored defaults, never asks the user. The first positional argument is the project **name**; `--path` sets the parent directory:

```bash
# All omitted options resolve from stored defaults
unity projects new MyGame

# Override stored defaults with explicit values
unity projects new MyGame --path /path/to/projects --editor-version 6000.0.47f1 --template com.unity.template.3d

# Open the project immediately after creation
unity projects new MyGame --open
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

Upgrade a project to a different Unity editor version. `--to` is required:

```bash
unity projects upgrade --to 6000.0.47f1
unity projects upgrade /path/to/MyProject --to 6000.0.47f1 --yes
```

#### projects export / import

```bash
# Export the project registry to a file (or stdout if -o is omitted)
unity projects export -o projects.json

# Import a previously exported registry
unity projects import projects.json
unity projects import --input projects.json
```

#### projects open / link / unlink

```bash
# Open a registered project by name, fuzzy title match, or path
unity projects open MyProject
# (the top-level `unity open` is the same thing)

# Connect a local project to its cloud / version-control link
unity projects link

# Disconnect a local project from its cloud / version-control link
unity projects unlink
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

# Filter by type (core, learning, sample, custom, new, all) — case-insensitive
unity templates list --editor 6000.0.47f1 --type core --format json
unity templates list --editor 6000.0.47f1 --type learning --format json
unity templates list --editor 6000.0.47f1 --type sample --format json
unity templates list --editor 6000.0.47f1 --type new --format json
unity templates list --editor 6000.0.47f1 --type all --format json  # no-op, returns everything

# List only user-generated (custom) templates
unity templates list --editor 6000.0.47f1 --custom --format json
# --type custom is an alias for --custom
unity templates list --editor 6000.0.47f1 --type custom --format json

# --custom and --type are mutually exclusive — using both is an error (exit 1)

# Show template details
unity templates info com.unity.template.3d --editor 6000.0.47f1 --format json

# Create a custom template from an existing Unity project
# --name and --display-name are REQUIRED
unity templates create /path/to/MyProject \
  --name com.myorg.template.mytemplate \
  --display-name "My Template"

# With all optional options
unity templates create /path/to/MyProject \
  --name com.myorg.template.mytemplate \
  --display-name "My Template" \
  --description "A starting point for our projects" \
  --template-version 1.0.0 \
  --output /path/to/templates/dir \
  --keep-embedded-packages \
  --keep-project-settings \
  --overwrite

# JSON output (includes path to created .tgz archive)
unity templates create /path/to/MyProject \
  --name com.myorg.template.mytemplate \
  --display-name "My Template" \
  --json

# NDJSON streaming — emits progress frames then a result frame
unity templates create /path/to/MyProject \
  --name com.myorg.template.mytemplate \
  --display-name "My Template" \
  --format ndjson
```

**`templates create` key notes:**
- `--name` must be a valid npm package name (e.g. `com.myorg.template.mytemplate`)
- `--output` overrides the Hub-configured user templates directory
- `--overwrite` replaces an existing archive of the same name without error
- On success, prints the path to the created `.tgz` archive
- Created templates appear in `unity templates list --editor <v> --custom`

```bash
# Delete a user-generated custom template (prompts for confirmation)
unity templates delete com.myorg.template.mytemplate --editor 6000.0.47f1

# Skip the confirmation prompt (CI-friendly)
unity templates delete com.myorg.template.mytemplate --editor 6000.0.47f1 --yes

# JSON output
unity templates delete com.myorg.template.mytemplate --editor 6000.0.47f1 --yes --json
```

**`templates delete` key notes:**
- Only user-generated templates (created via Hub UI or `templates create`) can be deleted
- Attempting to delete a built-in Unity template exits with a descriptive error (exit 6)
- Attempting to delete a template that doesn't exist exits with a descriptive error (exit 6)
- In interactive mode, prompts for confirmation before deleting; use `--yes` to skip
- On success, the template no longer appears in `unity templates list --editor <v> --custom`

```bash
# Get/set/reset the default storage path for custom templates
# Print current configured templates location
unity templates location

# Set a new default templates directory (must exist as a directory)
unity templates location --set /path/to/templates

# Reset templates location to the Hub default
unity templates location --reset

# JSON output for any variant
unity templates location --json
unity templates location --set /path/to/templates --json
unity templates location --reset --json
```

**`templates location` key notes:**
- `--set` and `--reset` are mutually exclusive (using both is an error)
- `--set` validates that the path exists and is a directory (exits 2 if not)
- `--reset` restores the Hub default templates path
- JSON output: `{ "path": "..." }` inside the standard envelope

```bash
# Edit a user-generated (custom) template's metadata
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1
```

Only user-generated templates can be edited (use `unity templates edit --help` for the editable fields).

---

### Config — persisted CLI configuration

The `config` command group manages settings that persist across invocations.

#### config proxy

View or change the configured HTTP/HTTPS/SOCKS/PAC proxy. The persisted value is read by every CLI command that issues outbound HTTP (releases, install, auth, telemetry, etc.).

```bash
# Show the effective proxy configuration (resolution source + auth source)
unity config proxy
unity config proxy --json

# Persist a proxy URL
unity config proxy http://proxy.example.com:8080

# Persist with embedded credentials (userinfo is redacted in echo output)
unity config proxy http://user:secret@proxy.example.com:8080

# Persist with bypass list (hosts that should NOT go through the proxy)
unity config proxy http://proxy.example.com:8080 --bypass "localhost,127.0.0.1,*.internal"

# SOCKS / PAC variants
unity config proxy socks5://proxy.example.com:1080
unity config proxy pac+http://wpad.example.com/proxy.pac
unity config proxy pac+file:///etc/proxy.pac

# Clear the persisted proxy
unity config proxy --unset
```

**Supported schemes:** `http://`, `https://`, `socks://`, `socks4://`, `socks4a://`, `socks5://`, `socks5h://`, `pac+http://`, `pac+https://`, `pac+file://`.

**Resolution priority** (highest → lowest):
1. `--proxy <url>` global flag (one-shot override for the current invocation)
2. `UNITY_PROXY` env var
3. Persisted `proxy.json` (`unity config proxy <url>`)
4. Standard env vars: `HTTPS_PROXY`, `HTTP_PROXY`, `ALL_PROXY`, `NO_PROXY`
5. System proxy settings (where supported)

`--proxy-disable` short-circuits all of the above for the current invocation, which is the recommended way to diagnose a misconfigured proxy without clearing it.

---

### Run — batch/headless execution

```bash
# Run a Unity project headless (batch mode is automatic — do NOT pass -batchmode/-quit)
unity run /path/to/MyProject -- -executeMethod Builder.Build

# Override editor version
unity run /path/to/MyProject --editor-version 6000.0.47f1 -- -nographics -logFile out.log

# Install editor automatically if missing
unity run /path/to/MyProject --allow-install -- -executeMethod Builder.Build

# Kill the Unity process after 300 seconds (useful in CI to prevent hangs)
unity run /path/to/MyProject --timeout 300 -- -executeMethod Builder.Build
# Equivalent via env var:
UNITY_RUN_TIMEOUT=300 unity run /path/to/MyProject -- -executeMethod Builder.Build
```

`unity run` always launches the editor in batch mode and forwards the args after `--` to the Unity executable, then returns the editor's exit code.

**Reserved flags — do NOT pass these after `--`.** The command adds them itself: `-batchmode`, `-quit`, `-projectPath`, `-useHub`, `-hubIPC`. Passing any of them fails fast (before launch) with exit code 6:

```
Error: Forwarded argument '-batchmode' conflicts with a reserved Unity flag managed by this command. Remove it from the args after `--`.
```

Flags like `-nographics`, `-logFile <path>`, and `-executeMethod <Class.Method>` are not reserved and are forwarded normally.

When `--timeout <seconds>` is set, the process receives SIGTERM at the deadline; if still alive after 2 s it receives SIGKILL. The command exits with code 6 (EXIT_COMMAND_FAILURE) on timeout.

---

### Build

`--target` and `--execute-method` are both **required** — Unity has no built-in command-line build, so your `executeMethod` is responsible for the actual build (including honoring `--output-path`).

```bash
# Build a project (requires --target and --execute-method)
unity build /path/to/MyProject \
  --target StandaloneOSX \
  --execute-method Builder.PerformBuild \
  --output-path ./build/output

# Common build targets: StandaloneOSX, StandaloneWindows64, StandaloneLinux64, Android, iOS, WebGL
```

**Options:**

| Flag | Description |
|---|---|
| `--target <target>` | Build target (required). |
| `--execute-method <method>` | Static C# method to invoke, e.g. `Builder.PerformBuild` (required). |
| `--build-target-group <group>` | Forwarded to Unity as `-buildTargetGroup`. |
| `-o, --output-path <path>` | Passed as `-buildOutput` (your method must honor it). |
| `-l, --log-file <path>` | Log file path. Default: `<project>/Logs/build-<target>-<timestamp>.log`. |
| `--editor-version <version>` | Override editor version (default: from `ProjectVersion.txt`). |
| `-e, --editor-path <path>` | Use a specific editor binary. |
| `-a, --architecture <arch>` | Editor architecture (`x86_64` or `arm64`). |
| `--args <string>` | Extra arguments passed to Unity (shell-split). |
| `--no-tail` | Do not stream the log to stdout in real time. |
| `--allow-install` | Install the project's editor version if missing. |
| `--versioning-strategy <strategy>` | `semantic`, `tag`, `custom`, or `none` (default: `none`). |
| `--build-version <version>` | Explicit version string; only used with `--versioning-strategy custom`. |
| `--allow-dirty-build` | Skip the uncommitted-changes guard (default: false). |

```bash
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

### Analytics — usage/telemetry consent

```bash
# Show current consent status
unity analytics status --format json

# Enable / disable anonymous usage data collection
unity analytics opt-in
unity analytics opt-out
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

> **⚠️ Unity-internal only (for now).** `unity pipeline install` clones the Pipeline package from a repository hosted on Unity's internal network, so it currently only succeeds for users with Unity internal access. External users will see a clone/authentication failure. Because the `command`, `eval`, `editor play/stop/pause`, and `status` commands below all require the Pipeline package to be installed in a running editor first, the entire experimental Pipeline workflow is unavailable to external users until the package is published publicly. If you're not on Unity's internal network, skip this section.

The `pipeline` command manages the Unity Pipeline package, which enables programmatic control of running Unity Editor instances. Alias: `pipe`.

```bash
# List all running Unity Editor instances and their Pipeline package status
unity pipeline list --format json

# Install the Pipeline package into a project (auto-detects project if omitted)
unity pipeline install
unity pipeline install --project-path /path/to/MyProject

# Use SSH instead of HTTPS when cloning the package
unity pipeline install --project-path /path/to/MyProject --ssh

# Keep the Samples / Tests folders in the installed package
unity pipeline install --install-samples --install-tests

# Force reinstall even if the package is already present
unity pipeline install --force
```

`pipeline install` options: `--project-path <path>`, `--ssh`, `--install-samples`, `--install-tests`, `--force`.

**Requires Unity 6.0 or higher.** The Pipeline package is cloned as an embedded package into `Packages/com.unity.pipeline/`.

---

### Command — send commands to a running Unity Editor (experimental)

The `command` command (alias: `cmd`) communicates with a running Unity Editor that has the Pipeline package installed. (`request`/`req` remain as deprecated hidden aliases — prefer `command`.)

```bash
# List all commands available on the connected Unity Editor
unity command
unity command --format json

# Execute a specific command
unity command editor_play
unity command log_editor "Hello from CLI"
unity command editor_status --includeMemory true

# Target a specific project or instance
unity command editor_play --project-path /path/to/MyProject
unity command editor_play --instance localhost:8765

# Connect to a Unity Player runtime instance
unity command <command> --runtime "MyGame"
unity command <command> --runtime-path /path/to/port-file

# Set a timeout (default: 30 seconds)
unity command editor_play --timeout 60
```

If no editor with a reachable Pipeline server is found, the command errors with guidance (make sure the editor is running, the Pipeline package is installed, and its HTTP server is up).

#### eval — evaluate a C# expression in a running editor

```bash
unity eval 'Application.version'
unity eval '1 + 2'
unity eval 'Application.version' --json
unity eval 'Time.realtimeSinceStartup' --timeout 10   # server-side timeout (default: 5s)

# Bare expressions are auto-wrapped as 'return <expr>;'. Include a ';' to run a statement body:
unity eval 'Debug.Log("hello");'
unity eval 'var s = Application.dataPath; return s.Length;'
```

Targeting options match `command`: `--project-path`, `--instance <host:port>`, `--runtime <name>`, `--runtime-path <path>`.

#### editor play / stop / pause — play-mode control

Higher-level wrappers over `command` for the connected editor:

```bash
unity editor play     # enter play mode
unity editor stop     # exit play mode
unity editor pause    # toggle pause

# Target a specific project or instance
unity editor play --project-path /path/to/MyProject
unity editor play --instance localhost:8765
```

#### status — live state of connected editors

```bash
# Show port, state, project, version, PID for every connected Unity Editor
unity status --format json

# Filter to one instance
unity status --port 8765
unity status --project megacity
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
- The CLI is currently in **beta** (latest: `0.1.0-beta.6`). Once GA ships, the `UNITY_CLI_CHANNEL=beta` part of the install command can be dropped.
- Outbound HTTP from every CLI command honors the resolved proxy (see `unity config proxy`). Inspect what the CLI actually resolved with `unity env --format json` or `unity doctor --format json` — both surface the active proxy URL, its source, and auth source.
