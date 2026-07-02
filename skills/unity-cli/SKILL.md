---
name: unity-cli
description: Use when interacting with Unity CLI from the terminal — install, upgrade or uninstall editors, list or open projects, manage modules, manage licenses, check auth status, read logs, browse Unity releases, build/test projects, configure the Unity MCP server for AI agents, or run any other Unity CLI operation.
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

A branded Unity header (logo, wordmark, CLI version) renders on the landing surfaces — bare `unity`, `unity --help` / `-h`, `unity help`, and above the first-run consent prompt. It's shown only on a TTY, prints at most once, and degrades to compact, uncolored text on narrow terminals, without Unicode, or under `NO_COLOR`. Piped output is unaffected. Use `--no-banner` to suppress it in scripts. Bare `unity` prints usage and exits 0.

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
| `UNITY_TEST_TIMEOUT` | `--timeout` | Timeout for `unity test` in seconds. |
| `UNITY_CLOUD_ORG` | `--cloud-org` | Active Unity Cloud organization id or name for a single call. |
| `UNITY_SERVICE_ACCOUNT_ID` | — | Service account client ID for non-interactive (CI) auth. |
| `UNITY_SERVICE_ACCOUNT_SECRET` | — | Service account client secret for non-interactive (CI) auth. |
| `UNITY_PROXY` | `--proxy` | HTTP/HTTPS/SOCKS/PAC proxy URL. Takes precedence over `HTTPS_PROXY`/`HTTP_PROXY`/`ALL_PROXY` and the persisted `proxy.json` setting. |
| `UNITY_NO_UPDATE_CHECK` | — | Disable the background "update available" check (see `unity config update-check`). |

**CI service account auth:** Set both `UNITY_SERVICE_ACCOUNT_ID` and `UNITY_SERVICE_ACCOUNT_SECRET` to skip the browser OAuth flow — this keeps the secret out of the process argument list and shell history. These map to the `--client-id` / `--secret-from-stdin` inputs of `unity auth login`, but reading the credentials from the environment isn't a full login: it doesn't run the interactive flow or persist credentials to the keyring.

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
| 4 | Precondition not met (e.g. no license active, floating server not configured) |
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

# A --client-secret flag also exists, but passing a secret as a
# command-line argument exposes it in shell history and the process list.
# Avoid it — use --secret-from-stdin (above) or the
# UNITY_SERVICE_ACCOUNT_ID / UNITY_SERVICE_ACCOUNT_SECRET env vars instead.

# Login without persisting credentials to the keyring (ephemeral CI)
unity auth login --client-id <id> --secret-from-stdin --no-store

# Logout (clears both service-account and OAuth credential slots)
unity auth logout

# Skip the confirmation prompt
unity auth logout --yes
```

**Separate sign-in from Hub.** As of `0.1.0-beta.8`, the CLI and the GUI Hub store their sign-in credentials **separately** — signing in to one no longer signs you out of (or overwrites the account of) the other, so each can stay signed in as a different account. (In earlier betas they shared a single keyring session.)

**Service-account credentials via env vars** (`UNITY_SERVICE_ACCOUNT_ID` + `UNITY_SERVICE_ACCOUNT_SECRET`) mint bearer tokens automatically for the duration of the process — no browser round-trip, no keyring write. If only one of the two is set, the CLI prints a warning on stderr instead of silently falling back to the keyring/OAuth identity.

The interactive `unity auth login` flow prints the sign-in URL to the terminal **before** attempting to launch the browser, which unblocks remote/headless sessions (SSH, containers, dev VMs) where `xdg-open` / `open` has no graphical session to attach to. With `--format json`, an `auth_url=…` progress frame is emitted so machine consumers can capture the URL without parsing human text.

`unity auth status` reflects real session state (including an explicit "session expired" message), not optimistic local assumptions. `unity doctor` and `unity cloud status` report the same real session state.

---

### License — list, activate, return

```bash
# List the Unity licenses active on this machine
unity license
unity license list             # explicit form, identical output
unity license --format json    # machine-readable

# Summary: active license(s) + sign-in state
unity license status

# Activate a license — choose exactly one mode (default = signed-in subscription)
unity license activate                              # signed-in user's subscription (entitlement) licenses
unity license activate --serial SC-…                # serial-based (ULF) activation, no sign-in needed
unity license activate --personal --accept-eula     # free Unity Personal license (must accept the EULA)
unity license activate --floating                   # lease a seat from the configured floating server
unity license activate --file ./Unity_lic.ulf       # offline activation from a .ulf / .xml file
unity license activate --generate-request ./req.alf # write an offline activation request (air-gapped)

# Return the active assigned/subscription licenses (prompts to confirm; --yes skips)
unity license return
unity license return --yes

# Floating (network) license server
unity license server list      # the configured floating license server(s)
unity license server status    # reachability + available seats
```

`list` columns: product, license type (`Floating` / `Assigned` / `ULF`), organization, and expiry. `status` prints a one-glance summary — the active license(s) and whether you're signed in — and exits non-zero (`4`) when no license is active, so it works as a scriptable health check. The first licensing command downloads the Unity licensing client on demand; as of `0.1.0-beta.8`, if the client is unavailable `list` reports a clear error and exits non-zero (matching `status`), rather than printing an empty list.

`activate` takes a single mode flag (combining them is a usage error). The default (no flag) and `--personal` activate the signed-in user's entitlements — sign in first with `unity auth login`. `--personal` also requires `--accept-eula` to acknowledge the Unity Personal license terms. `--serial` / `--file` work offline without sign-in. `--floating` requires a configured floating license server (exit `4` if none is set). `--generate-request` writes a `.alf` request for air-gapped activation instead of activating. `return` returns the active licenses, prompting for confirmation first — pass `--yes` to skip (required in non-interactive shells and with `--json`). All honor `--json` / `--format` and exit non-zero on failure (`2` bad usage, `3` sign-in required, `4` floating not configured, `6` licensing-client error).

`unity license server list` shows the configured floating license server (from the `licensingServiceBaseUrl` machine setting; a pure settings read, no client download). `unity license server status` contacts that server and reports reachability plus available seats — exit `4` when no server is configured, `6` when configured but unreachable.

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
# Short alias: unity e. The bare `unity editors` is shorthand for the explicit `unity editors list` (matches projects/templates/modules)
unity editors list --format json

# List only installed editors
# As of beta.8 the --installed table includes an "Upgrade to" column flagging editors with a newer patch in their line
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

`unity editors` honors `--format tsv` and `--format ndjson` for its default listing. Identifier columns keep their natural width even if the table exceeds the terminal — they are no longer silently truncated.

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

#### editors path

```bash
# Print the install directory of an installed editor (local, offline — no release-feed fetch)
unity editors path 6000.0.47f1
unity editors path 6000.0.47f1 --architecture arm64 --json
```

Honors `--architecture` and `--format` / `--json`, and reports ambiguous matches so you can narrow by version or architecture.

#### editors install-path

```bash
# Show the directory where editors are installed
unity editors install-path

# Set a new install path
unity editors install-path --set /path/to/editors
```

Also available as the top-level `unity install-path` (with an additional `--get` flag). Distinct from `editors path`: `install-path` gets/sets the *root* install directory; `editors path` prints the install directory of *one* editor version.

#### editors info

```bash
# Show release details for a specific version
unity editors info 6000.0.47f1 --format json
```

#### editors upgrade

New in `0.1.0-beta.8`. Upgrade an installed editor to the newest official (f-channel) patch in the same `major.minor` line (e.g. `2022.3.10f1` → `2022.3.62f1`), carrying the installed modules over. The `[editor]` argument accepts an exact version, a `major.minor` line, or the `latest` / `lts` / `default` aliases. Editors install side by side — the old version is kept unless `--replace` (alias `--remove-old`) is passed.

```bash
# Upgrade a specific editor (or the default / lts / latest) to the newest patch in its line
unity editors upgrade 2022.3.10f1
unity editors upgrade lts

# Upgrade every installed editor that has a newer patch
unity editors upgrade --all --yes --accept-eula

# Report current → target without installing (--check is an alias for --dry-run)
unity editors upgrade --all --dry-run --format json

# Remove the old editor after a successful upgrade; skip carrying modules; add extra modules
unity editors upgrade 2022.3.10f1 --replace --yes
unity editors upgrade 2022.3.10f1 --no-modules
unity editors upgrade 2022.3.10f1 --module android --module ios
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

Module installers honor the per-module install command from the release manifest (e.g. Visual Studio on Windows uses `--passive`, not `/S`); the resolved command is surfaced in `unity modules list --json`. `unity install` self-heals a corrupted partial download by discarding the bad partial and re-downloading; a cross-process install lock prevents two concurrent installs of the same version from corrupting the unpack.

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

`unity modules list` honors `--format ndjson` (empty results emit a clean, empty NDJSON stream).

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

### Projects — list, open, create, register, clone, link

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

# Version shorthand (equivalent to open with --editor-version)
unity 6000.0.47f1 /path/to/MyProject
```

The project argument is matched against the Hub registry first (exact name or path opens immediately; a glob like `"My Game*"` prompts when multiple match); with no registry match it falls back to treating the argument as a filesystem path. `unity open` forwards `--args` to the Editor correctly on all platforms (including Windows).

#### projects create

Create a project. On a TTY, prompts for any missing options (parent directory, editor version, template). In CI, pass `--non-interactive` or pipe stdin to suppress prompts and rely on stored defaults. The first positional argument is the project **name**; `--path` sets the parent directory:

```bash
unity projects create MyGame --editor-version 6000.0.47f1 --template com.unity.template.3d

# Place the project in a specific directory
unity projects create MyGame --path /path/to/projects --editor-version 6000.0.47f1

# --template also accepts a .tgz file path or a directory, not just a registered template id
unity projects create MyGame --template /path/to/template.tgz
```

**Cloud linking during creation:**

```bash
# Create and link a NEW Unity Cloud project as part of creation
unity projects create MyGame --cloud --cloud-org <id-or-name>

# Link an EXISTING cloud project instead
unity projects create MyGame --cloud-project <id-or-name>
```

**Source-control during creation** — publish the new project to a fresh repository:

```bash
unity projects create MyGame \
  --vcs github \
  --git-namespace my-org \
  --git-repo my-game \
  --git-visibility private \
  --git-default-branch main \
  --git-token-stdin
```

Source-control flags (shared with `projects link vcs`): `--vcs github|gitlab|uvcs`, `--git-namespace <name>`, `--git-repo <name>`, `--git-visibility private|public|internal` (default private), `--git-default-branch <name>`, `--git-token <pat>` / `--git-token-stdin`, `--no-initial-commit`, `--git-lfs`, and `--vcs-region <name>` for Unity Version Control.

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

#### projects clone

Clone a remote repository and register the Unity project it contains. Works across providers:

```bash
# Clone by full repo URL / shorthand
unity projects clone --vcs github --vcs-namespace my-org --vcs-repo my-game --path ./MyGame

# Check out a specific ref (branch, sha, or UVCS changeset)
unity projects clone --vcs uvcs --vcs-namespace my-org --vcs-repo my-game --ref main

# Authenticate with a personal access token (prefer stdin)
unity projects clone --vcs gitlab --vcs-namespace my-org --vcs-repo my-game --git-token-stdin

# Project lives in a subdirectory of the repo
unity projects clone --vcs github --vcs-namespace my-org --vcs-repo monorepo \
  --path ./repo --project-path packages/MyGame
```

Options: `--vcs github|gitlab|uvcs`, `--vcs-namespace <name>`, `--vcs-repo <name>`, `--ref <branch|sha|changeset>` (an all-digit ref is treated as a Unity Version Control changeset, anything else as a branch), `--path <dest>` (clone destination), `--project-path <subpath>` (project subdirectory), `--git-token <pat>` / `--git-token-stdin`, `--json`. Git LFS assets are fetched as pointer files only.

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

# --- Cloud links ---
# Connect an existing local project to a Unity Cloud project
unity projects link cloud /path/to/MyProject --cloud-org <id-or-name>
# Disconnect from its Unity Cloud project
unity projects unlink cloud /path/to/MyProject

# --- Version-control links ---
# Publish a local project to a NEW GitHub / GitLab / Unity Version Control repository
unity projects link vcs /path/to/MyProject \
  --vcs github --git-namespace my-org --git-repo my-game --git-token-stdin
# Remove a project's git remotes (the remote repositories are NOT deleted)
unity projects unlink vcs /path/to/MyProject
# Also detach the Unity Version Control workspace
unity projects unlink vcs /path/to/MyProject --unlink-workspace
```

`link vcs` shares the source-control flag set documented under `projects create`. `link cloud` / `link vcs` accept `--cloud-org <id-or-name>` (env `UNITY_CLOUD_ORG`).

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
# At least one of --display-name, --description, --template-version,
# --preview-image, --remove-preview-image is required
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1 --display-name "My Updated Template"

# Update multiple fields at once
unity templates edit com.myorg.template.mytemplate \
  --editor 6000.0.47f1 \
  --display-name "My Updated Template" \
  --description "A new description for the template" \
  --template-version 1.1.0

# Replace / remove preview image
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1 --preview-image /path/to/image.png
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1 --remove-preview-image

# JSON / NDJSON output (--yes required because these are non-interactive)
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1 --display-name "Updated" --yes --json
```

**`templates edit` key notes:**
- Only works on user-generated (custom) templates; built-in templates cannot be edited
- Use `--editor` to specify which editor version's template list to search, or omit to use the stored default
- `--preview-image <path>` resolves to an absolute path before passing to the service
- `--remove-preview-image` is only applied when no valid `--preview-image` path is given; if both are passed with a valid image path, the new image wins and `--remove-preview-image` is ignored
- On success (human format), prints the updated template's display name

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

# Embedded userinfo (user:password@host) is supported and redacted in echo
# output, but prefer leaving credentials out of the URL — the CLI looks them
# up in the OS keyring instead (see Resolution priority below).

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
3. Standard env vars: `HTTPS_PROXY`, `HTTP_PROXY`, `ALL_PROXY`, `NO_PROXY`
4. Persisted `proxy.json` (`unity config proxy <url>`)
5. System proxy settings (where supported)

Credentials missing from the URL are looked up in the OS keyring (shared with the GUI Hub); Kerberos/SPNEGO-authenticated proxies are supported. `--proxy-disable` short-circuits all of the above for the current invocation, which is the recommended way to diagnose a misconfigured proxy without clearing it.

#### config update-check

New in `0.1.0-beta.8`. Enable or disable the background check for a newer CLI version (the unobtrusive "update available" notice; interactive sessions only, never delays a command). Equivalent to the `UNITY_NO_UPDATE_CHECK` env var.

```bash
unity config update-check          # show the current setting
unity config update-check off      # disable
unity config update-check on       # enable
unity config update-check --json
```

---

### Hub — install the Unity Hub application

Bootstrap Unity Hub on a clean machine from the command line.

```bash
# Install the latest stable Hub for the current OS + architecture
unity hub install

# Install a specific Hub version
unity hub install --hub-version 3.17.0

# Force reinstall even when Hub is already detected
unity hub install --force

# Run the installer silently (Windows only)
unity hub install --headless

# Override architecture (e.g. x64 Hub on Apple Silicon via Rosetta)
unity hub install --architecture x64

# Skip the installer code-signature check (unsigned/local builds — not recommended)
unity hub install --skip-signature-check
```

Options: `-f` / `--force`, `--headless` (silent installer, Windows only), `-a` / `--architecture x64|arm64` (env `UNITY_ARCHITECTURE`), `--hub-version <version>` (default latest), `--skip-signature-check`.

**Integrity & signature verification** — every download is checked against the SHA-512 from the HTTPS manifest, then the installer's **code signature** is verified before it runs with elevation: on macOS via `codesign` (signer `Developer ID Application: Unity Technologies`), on Windows via Authenticode (signer subject `Unity Technologies`), checked *before* the UAC prompt. Verification is **fail-closed** — if it fails or the verifier is unavailable, the command aborts with exit 6 and does not run the installer. Linux `.AppImage` has no standard verifier, so it is SHA-512-only. Pass `--skip-signature-check` to bypass (prints a warning; not recommended).

**`--hub-version` behaviour** — fetches the version-specific manifest from the CDN; if that version does not exist, the command exits with code 6 (no fallback to latest).

```bash
# JSON output
unity hub install --format json
```

Emits `{ "success": true, "command": "hub install", "data": { "version": "3.x.x", "installed": true } }` on success, or an `{ "alreadyInstalled": true, "installedPath": "…" }` payload when Hub was already present.

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

### Test — run EditMode/PlayMode tests

```bash
# Run tests and write an NUnit XML report (omitting --mode runs the editor's default platform)
unity test /path/to/MyProject

# Run a specific platform (--mode is case-insensitive: EditMode/editmode both work)
unity test /path/to/MyProject --mode EditMode
unity test /path/to/MyProject --mode PlayMode --output ./results/play.xml

# Run only tests whose names match a filter
unity test /path/to/MyProject --filter "MyNamespace.MyTests"

# Pin the editor version, installing it if missing; cap the run at 600 s
unity test /path/to/MyProject --editor-version 6000.0.47f1 --allow-install --timeout 600
# Equivalent via env var:
UNITY_TEST_TIMEOUT=600 unity test /path/to/MyProject

# Forward extra editor args after -- (reserved test flags are rejected)
unity test /path/to/MyProject -- -nographics
```

`unity test` launches the editor's built-in test runner in batch mode (`-runTests -testPlatform <mode> -testResults <path> -testFilter <pattern>`), waits for it to finish, and writes the report to `--output` (default `test-results.xml`). It exits 0 when the run succeeds and 6 (EXIT_COMMAND_FAILURE) when the editor exits non-zero — i.e. reports test failures or fails to run. It runs the tests **directly via the editor command line** — no pipeline package or server is involved. `--mode` is optional; when omitted, `-testPlatform` is not passed and the editor runs its default platform.

It deliberately does **not** pass `-quit`: `-runTests` quits the editor itself once results are written, so forcing `-quit` would terminate it before the report exists. Anything after `--` is forwarded to the editor verbatim, except reserved flags managed by the command (`-projectPath`, `-batchmode`, `-runTests`, `-testPlatform`, `-testResults`, `-testFilter`, `-quit`, `-useHub`, `-hubIPC`), which are rejected.

Options: `--mode EditMode|PlayMode`, `--filter <pattern>`, `--output <path>`, `--editor-version <version>` (env `UNITY_EDITOR_VERSION`), `-e, --editor-path <path>`, `-a, --architecture <arch>`, `--allow-install`, `--timeout <seconds>` (env `UNITY_TEST_TIMEOUT`).

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

**Android signing & export** (applied to Android targets only):

| Flag | Description |
|---|---|
| `--android-export-type <type>` | `apk`, `aab`, or `android-studio-project`. |
| `--android-keystore-base64 <b64>` | Keystore file, base64-encoded. |
| `--android-keystore-password <pass>` | Keystore password. |
| `--android-key-alias <alias>` | Key alias within the keystore. |
| `--android-key-alias-password <pass>` | Key alias password. |
| `--android-target-sdk-version <N>` | Target SDK version. |
| `--android-symbol-type <type>` | `none`, `public`, or `debugging`. |
| `--android-version-code <N>` | Android version code. |

Keystore flags are validated together. Secrets passed as command-line flags surface in the process list and can be echoed into CI logs. Supply `--android-keystore-base64`, `--android-keystore-password`, and `--android-key-alias-password` from CI secret environment variables (e.g. `--android-keystore-password "$KEYSTORE_PASSWORD"`), never as inline literals, and source those variables from a dedicated CI secret store. Note that sourcing from an env var only avoids hard-coding the literal — the expanded value still appears in `argv`, so also mask it in CI log output.

**Versioning** — `semantic` and `tag` derive the version from git tags/history; `custom` requires an explicit `--build-version`; a dirty working tree is rejected unless `--allow-dirty-build` is passed.

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

The CLI writes its own `cli-log.json` (separate from the Hub's `info-log.json`) and records its version on every start. `unity logs`, `unity bug`, and `unity doctor` read the CLI's own log.

---

### Doctor — system diagnostics

```bash
# Full system report
unity doctor --format json

# Includes: platform info, auth status, installed editors, recent log lines, resolved proxy
unity doctor --tail 50
```

`unity doctor` reports real session state (matching `unity auth status`) and surfaces the resolved proxy URL, its source, and auth source.

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

The CLI defaults to **opt-out**. On the first interactive run a y/N prompt is shown once before any data is collected; non-interactive, CI, piped, and `--quiet` contexts silently keep the opt-out default.

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

### MCP — Model Context Protocol server (AI agent integration)

New in `0.1.0-beta.8`. `unity mcp` starts a Model Context Protocol server, built into the `unity` binary, that exposes the commands of a connected Unity Editor as MCP tools. AI agent clients connect over stdio, list those tools, and run them. The server starts even when no Editor is running and reports that it isn't connected; commands that a connected Editor adds show up as tools automatically.

```bash
# Start the MCP stdio server (usually launched by the AI client, not by hand)
unity mcp

# Pin the server to a specific Unity project / Editor instance
unity mcp --project-path /path/to/MyProject
unity mcp --instance localhost:55000
```

#### mcp configure — register the server in an AI client

Writes the Unity MCP server entry into an AI client's config in one step, preserving every other key in the file. 16 clients are supported: `claude`, `claude-code`, `cursor`, `vscode`, `vscode-insiders`, `copilot-cli`, `windsurf`, `cline`, `codex`, `kiro`, `trae`, `openclaw`, `antigravity`, `zed`, `continue`, `inspect`.

```bash
# List all supported clients and their config paths
unity mcp configure --list

# Configure a client
unity mcp configure claude
unity mcp configure claude-code

# Project-local config for clients that support it (e.g. cursor, windsurf)
unity mcp configure cursor --local

# Pin to a project; skip the "already exists, update?" prompt; preview without writing
unity mcp configure claude --project-path /path/to/MyProject
unity mcp configure vscode --yes
unity mcp configure vscode --dry-run
```

---

### Connected Editors — pipeline / command / status

> **Promoted to production in `0.1.0-beta.8`.** In earlier betas these were development-only (and the Pipeline package was Unity-internal). They now talk to any running Unity Editor over its Pipeline server, and the supporting Editor-side package (`com.unity.pipeline`) is resolved from the **Unity (UPM) registry** and added to the project's `Packages/manifest.json` — no internal access or manual setup required. The Editor defines each command's parameters, help, and error messages, so the commands a connected Editor exposes are usable without a CLI update.

#### pipeline (alias: pipe) — manage the Unity Pipeline package

```bash
# List the Editors the CLI can reach and the Pipeline package status of each
unity pipeline list --format json

# Install / update the Pipeline package into a project (auto-detects project if omitted)
unity pipeline install
unity pipeline install --project-path /path/to/MyProject
unity pipeline install --force          # re-resolve to the latest version even if present
```

`pipeline install` options: `--project-path <path>`, `--force`. The package is resolved from the Unity registry and written to `Packages/manifest.json`.

#### command (aliases: cmd, request) — send commands to a running Unity Editor

Forwards a command to a connected Editor. Run it with no arguments to list the commands the connected Editor exposes.

```bash
# List all commands available on the connected Unity Editor
unity command
unity command --format json

# Execute a specific command (names/params come from the Editor)
unity command editor_play
unity command log_editor "Hello from CLI"
unity command editor_status --includeMemory true

# Capture a Scene/Game view screenshot (forwarded to the Editor's screenshot command, new in beta.8)
unity command screenshot --output ./shot.png --width 1920 --height 1080

# Target a specific project / instance / Player runtime
unity command editor_play --project-path /path/to/MyProject
unity command editor_play --instance localhost:8765
unity command <command> --runtime "MyGame"
unity command <command> --runtime-path /path/to/port-file

# Set a timeout (default: 30 seconds)
unity command editor_play --timeout 60
```

If no editor with a reachable Pipeline server is found, the command errors with guidance (make sure the editor is running and its Pipeline server is up).

#### status — live state of connected editors

```bash
# Show port, state, project, version, PID for every connected Unity Editor
unity status --format json

# Filter to one instance
unity status --port 8765
unity status --project megacity
```

Reads the lockfile the Pipeline package writes per running Editor (faster and more CI-friendly than `pipeline list`). Stale-heartbeat instances are reported as `unreachable` without an HTTP probe. With `--format json`/`ndjson`, emits a `success: false` envelope (`STATUS_NO_INSTANCES` / `STATUS_ALL_UNREACHABLE`) and a non-zero exit when no Editor is reachable, so CI scripts can gate on Editor availability.

---

## Development-only commands (hidden in production builds)

The commands below are **absent from the published production CLI** — they only register when `HUB_ENV=development`, so they won't appear in `unity --help` for a normal install. Documented here for completeness; if you don't see them, they're not available in your build.

### eval — evaluate a C# expression in a running editor

Requires a connected Editor with the Pipeline package (see *Connected Editors* above).

```bash
unity eval 'Application.version'
unity eval '1 + 2'
unity eval 'Application.version' --json
unity eval 'Time.realtimeSinceStartup' --timeout 10   # server-side timeout (default: 5s)

# Bare expressions are auto-wrapped as 'return <expr>;'. Include a ';' to run a statement body:
unity eval 'Debug.Log("hello");'
unity eval 'var s = Application.dataPath; return s.Length;'
```

Compile failures surface the Roslyn diagnostics and exit non-zero. Targeting options match `command`: `--project-path`, `--instance <host:port>`, `--runtime <name>`, `--runtime-path <path>`.

### cloud-pipeline — Unity Cloud Pipeline

Manage Unity Cloud Pipeline resources. Subcommand groups: `status`, `onboard`, `assets` (`list`/`status`/`url`), `branches` (`list`/`show`/`create`/`url`/`enable`/`edit`/`disable`), `pending-changes list`, `files` (`create`/`update`/`delete`/`move`), `pull-request create`. Use `unity cloud-pipeline --help` (development build) for the full flag set.

### collab — Unity collaboration (annotations & attachments)

Manage review annotations and attachments. Subcommand groups: `annotations` (`count`/`create`/`delete`/`get`/`update`/`replies`/`resolve`/`status`/`unresolve`) and `attachments` (`list`/`delete`/`update`). Use `unity collab --help` (development build) for the full flag set.

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

### CI: activate a license, then build

```bash
# 1. Sign in non-interactively with a service account
unity auth login --client-id "$UNITY_SERVICE_ACCOUNT_ID" --secret-from-stdin <<<"$UNITY_SERVICE_ACCOUNT_SECRET"

# 2. Activate the entitlement license (or use --serial / --floating)
unity license activate

# 3. Build
unity build /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --target StandaloneLinux64 \
  --execute-method Builder.PerformBuild \
  --allow-install
echo "Exit code: $?"

# 4. Return the seat when done (floating/assigned)
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

### CI: run tests and publish results

```bash
unity test /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --mode EditMode \
  --output ./test-results.xml \
  --allow-install \
  --timeout 600
echo "Exit code: $?"   # 0 = pass, 6 = test failures
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
- `--format json` always produces machine-readable output; prefer it over parsing human text. Error envelopes are pretty-printed with the same 2-space indent as success envelopes.
- `unity <version> [path]` is a shorthand for `unity open [path] --editor-version <version>`. Works with `lts`, `latest`, or a full version string like `6000.0.47f1`.
- The CLI supports kubectl-style plugins: any `unity-<name>` binary on PATH is callable as `unity <name>`.
- Terminal output is hardened against control-character / escape-sequence injection from server-provided values (project titles, editor versions, module names) — C0 controls and non-SGR escape sequences are stripped from table/list/tree output, while SGR color/style codes are preserved.
- The CLI is currently in **beta** (latest: `0.1.0-beta.8`). Once GA ships, the `UNITY_CLI_CHANNEL=beta` part of the install command can be dropped.
- As of beta.8 the CLI checks in the background for a newer version and prints an unobtrusive "update available" notice (interactive sessions only; never delays a command). Turn it off with `unity config update-check off` or the `UNITY_NO_UPDATE_CHECK` env var.
- Outbound HTTP from every CLI command honors the resolved proxy (see `unity config proxy`). Inspect what the CLI actually resolved with `unity env --format json` or `unity doctor --format json` — both surface the active proxy URL, its source, and auth source.
