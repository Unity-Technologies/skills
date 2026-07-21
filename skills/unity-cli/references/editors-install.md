# Editors, install & modules — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

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

# Remove installed modules from an editor by id (-m/--module, repeatable)
unity editors module remove 6000.0.47f1 --module android --module ios
unity editor module remove 6000.0.47f1 -m android -a arm64   # disambiguate side-by-side installs
unity editors module remove 6000.0.47f1 -m android --yes     # skip the confirm prompt (required non-interactively)

# Refresh module list for a manually located editor
unity editors module refresh 6000.0.47f1
```

`module remove` prompts to confirm before deleting the module files; `-y` / `--yes` skips the prompt and is required in non-interactive mode. Supports `-a` / `--architecture` to disambiguate side-by-side installs and the global `--format human|json|tsv|ndjson`.

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

# Windows: skip the elevated (UAC) install helper — for user-writable install locations
# and CI shells where a UAC prompt can't be answered (installing into a protected path then
# fails with a permission error instead of prompting). Also via UNITY_NO_ELEVATE=1.
unity install 6000.0.47f1 --no-elevate --yes --accept-eula
```

When installing an editor with several modules, a failed module no longer aborts the whole batch — `unity install` (and `unity install-modules`) continue with the remaining items and exit non-zero if any failed. Each editor and module is listed as installed (✓), failed (✗), or pending (·); the NDJSON `result` frame carries the same breakdown as an `items` array (each entry has `uid`, `name`, `kind`, `status`), so scripts can tell exactly which modules succeeded even on a non-zero exit.

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

# Reinstall modules that are already installed (a repair)
unity install-modules --editor-version 6000.0.47f1 --module android --reinstall

# -f/--force implies --reinstall, auto-includes child modules, and skips confirmation prompts
unity install-modules --editor-version 6000.0.47f1 --module android --force

# Tune the automatic retry for modules whose download/validation fails intermittently
# (default retries twice with backoff; 0 disables). Also via UNITY_INSTALL_RETRIES.
unity install-modules --editor-version 6000.0.47f1 --module android --retries 3
unity install-modules --editor-version 6000.0.47f1 --module android --retries 0

# Windows: skip the elevated (UAC) install helper (also via UNITY_NO_ELEVATE=1)
unity install-modules --editor-version 6000.0.47f1 --module android --no-elevate
```

`--list` and `--all` are mutually exclusive. `--list` is also mutually exclusive with `--module`.

A module whose download or validation fails intermittently — common for large modules such as Android SDK/NDK and OpenJDK — is retried automatically (up to twice with exponential backoff by default) instead of failing the whole run; already-installed modules are never re-downloaded, and retry attempts surface in both human and `--format ndjson` output.

`--module android ios` (space-separated values after a single `--module`) and `--module android --module ios` (repeated flag) are equivalent — both install all listed modules.

Module discovery works for editors registered via `unity editors add <path>` (located editors), not just editors installed by the Hub.

---

