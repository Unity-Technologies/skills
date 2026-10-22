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
# As of 0.1.0-beta.8 the --installed table includes an "Upgrade to" column flagging editors with a newer patch in their line
unity editors --installed --format json

# List only available releases
unity editors --releases --format json

# Filter by architecture
unity editors --installed --architecture arm64 --format json

# Show detailed module info (the old spelling, --verbose, still works and warns)
unity editors --long

# Watch mode — live-updates as editors are installed or removed
unity editors --watch
unity editors --installed --watch
```

`unity editors` honors `--format tsv` and `--format ndjson` for its default listing. Identifier columns keep their natural width even if the table exceeds the terminal — they are no longer silently truncated.

In the `--json` and `--format ndjson` rows of an installed Editor, read `moduleNames`, an array of the installed platforms (with `--long`, the full module names). The `modules` string joins the same names with `, ` and is deprecated: a module name can itself contain a comma.

#### editors running

List the Unity Editor instances currently running and the project each has open, with the editor version and process id per instance:

```bash
unity editors running
unity editors running --format json
```

Detection is cross-platform (process table plus each project's Pipeline lockfile), and the version falls back to a project's `ProjectSettings/ProjectVersion.txt` for editors without the Pipeline package. An empty list is a normal result (exit 0). Honors the global `--format human|json|tsv|ndjson` (and `--json`). In JSON each row of `data.instances` carries `pid`, `projectPath`, `projectName`, `editorVersion` (`null` when unknown), `port`, `hasPipeline` and `reachable`. Read `editorVersion`: `unityVersion` holds the same value and is deprecated.

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
# Details for a specific architecture's build (default: this machine's)
unity editors info 6000.0.47f1 --architecture x86_64 --format json
```

`downloadUrl` and `modules` describe the build for this machine's architecture. When the release has no build for it, the default falls back to the platform's first download instead: on Linux arm64 that is the x86_64 installer, which runs only under FEX-Emu (see [Linux arm64](#linux-arm64-experimental-x86_64-editor-under-fex-emu)). `--architecture arm64|x86_64` selects a build explicitly, with no fallback; when the release has no build for it, `downloadUrl` is null.

#### editors upgrade

New in `0.1.0-beta.8`. Upgrade an installed editor to the newest official (f-channel) patch in the same `major.minor` line (e.g. `2022.3.10f1` → `2022.3.62f1`), carrying the installed modules over (except the IDE modules, Visual Studio Code and Visual Studio, which update themselves; add `-m visualstudiocode` to install it anyway). The `[editor]` argument accepts an exact version, a `major.minor` line, or the `latest` / `lts` / `default` aliases. Editors install side by side — the old version is kept unless `--replace` (alias `--remove-old`) is passed.

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

#### editors prune

Finds installed editors that **no registered project uses** and, optionally, uninstalls them. Report-only by default — it never deletes anything unless you pass `--remove`.

```bash
# Report only: which editors are unused, and how much they'd reclaim
unity editors prune

# Uninstall the unused editors (prompts to confirm)
unity editors prune --remove

# Non-interactive: --yes is REQUIRED alongside --remove in a script or CI
unity editors prune --remove --yes

# Machine output
unity editors prune --format json
```

The report lists version, architecture, path, size, and status, then the total reclaimable size. With `--remove` in a non-interactive shell and no `-y, --yes`, it refuses rather than deleting unprompted. "Unused" is judged against the **project registry** (`unity projects list`), so an editor used only by a project you never registered counts as unused — register it first, or verify with `unity editors prune` before adding `--remove`.

**`--remove-missing` is a separate, safer candidate class:** editors registered with `unity editors add` (typically internal branch builds) whose recorded install path no longer resolves on disk at all.

```bash
# Also report editors whose install path is already gone
unity editors prune --remove-missing

# Drop those entries from the editor list (prompts to confirm)
unity editors prune --remove-missing --yes
```

Unlike `--remove`, this never touches the filesystem — the folder is already gone, so there is nothing to delete, and confirming only updates the registry. It doesn't require the running-editor check to succeed, since nothing can be running out of a folder that no longer exists. The two flags are independent and neither implies the other; `--format json`/`ndjson` carry the missing-path rows under `data.missing`.

#### editors verify

Structurally verifies an installed editor: checks that its files and modules are actually present on disk. It's the command to reach for when an editor launches oddly, a module seems half-installed, or a download was interrupted.

```bash
# Verify an installed editor
unity editors verify 6000.1.0f1

# Disambiguate when the same version is installed for two architectures
unity editors verify 6000.1.0f1 --architecture arm64

# Machine output
unity editors verify 6000.1.0f1 --json
```

Reports each component as `ok`, `missing`, or `skipped`, and names the exact `unity install-modules` command to repair anything missing. A clean editor exits 0; missing or empty files fail the check. This is a **structural** check — it confirms files exist, not that they are uncorrupted or correctly signed. `--architecture` is inherited from the `editors` parent, so `unity editors --architecture arm64 verify <version>` works too.

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

### Software Terms — `unity terms`

Each Unity Editor stops at its "Unity Editor Software Terms" dialog (Accept and Continue / Decline) the first time it opens windowed, until the terms are accepted for that Editor. Current Editors ignore `-acceptSoftwareTermsForThisRunOnly`, so no launch flag prevents the dialog. `unity terms accept <version>` records the user's acceptance the way the dialog's Accept button does, so a container image or a provisioning script can prepare an Editor before it first opens.

<!-- unity-example: skip (Rust-only) -->
```bash
# Show the terms link and the copy installed with the Editor, then ask (default No)
unity terms accept 6000.3.10f1

# Accept without asking (unattended setup, after the user has read and agreed to the terms)
unity terms accept 6000.3.10f1 --yes
unity terms accept latest --architecture arm64 --yes

# Whether the terms are accepted, for every installed Editor or one
unity terms status
unity terms status 6000.3.10f1 --json
```

- **Accepting is the user's decision.** On a terminal `accept` prints the terms link (https://unity.com/legal/editor-terms-of-service/software) and the path of the terms file the Editor ships, then asks. Without a terminal (`--non-interactive`, `UNITY_NON_INTERACTIVE`, `--json` or another machine format, redirected input or output) it refuses with exit `2` and `TERMS_ACCEPT_NEEDS_CONFIRMATION` unless `--yes` is passed. Never pass `--yes` for the user unasked: show them the link and let them decide, or let them run the command.
- `<version>` resolves like `unity editors path`: an installed version, `latest` or `lts`, and `-a` / `--architecture` when a version is installed for both. An Editor that is not installed fails with `TERMS_EDITOR_NOT_INSTALLED`, an ambiguous version with `TERMS_EDITOR_AMBIGUOUS`, and an Editor that ships no terms file with `TERMS_FILE_MISSING`.
- The acceptance belongs to the exact terms file: its key is `Terms-` and the file's MD5, so Editors that ship the same terms share one answer, and an Editor with new terms asks again. An answer that is already recorded is reported (`alreadyAccepted: true`) and nothing is written.
- It writes only the Editor's own preference, for the current user: `HKCU\Software\Unity Technologies\Unity Editor 5.x` on Windows, the `com.unity3d.UnityEditor5.x` defaults domain on macOS, `~/.local/share/unity3d/prefs` (or `$XDG_DATA_HOME/unity3d/prefs`) on Linux. Close every open Unity Editor first: on macOS and Linux a running Editor can overwrite the answer when it saves its own preferences.
- `terms status` lists each installed Editor as `accepted`, `not-accepted` or `no-terms-file`, in every `--format`; `--json` carries `key`, `termsFile` and the `store` location for each.

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
unity install 6000.0.47f1 --child-modules

# Exclude child modules
unity install 6000.0.47f1 --no-child-modules

# Install and accept EULAs automatically (CI)
unity install 6000.0.47f1 --yes --accept-eula

# Force reinstall even if already present
unity install 6000.0.47f1 --force

# Resume an interrupted download — editor installer or module — (also recovers orphaned partials left by a crash or kill)
unity install 6000.0.47f1 --resume

# Dry-run: show what would be installed without doing it
unity install 6000.0.47f1 --dry-run --format json

# List the editor's available modules and exit without installing
# (a drop-in alias for `unity modules list <version>`; the old --list-components spelling
# still works but is deprecated and prints a warning, so write --list-modules)
unity install 6000.0.47f1 --list-modules --format json

# Space-separated module values after a single -m are equivalent to repeating -m
unity install 6000.0.47f1 -m android ios          # space-separated
unity install 6000.0.47f1 -m android -m ios       # repeated flag (same effect)

# Windows: keep the install service unelevated. The Editor's NSIS installer is manifested
# `highestAvailable`, so it runs unelevated for a STANDARD user (the supported unprivileged
# install — it reports any dependencies an admin must finish) but still asks for elevation on
# demand under an administrator account. In CI, where a prompt can't be answered, run the
# agent elevated instead. Also via UNITY_NO_ELEVATE=1.
unity install 6000.0.47f1 --no-elevate --yes --accept-eula
```

A transient editor or module download failure (a dropped connection, a truncated transfer) is retried with the same bounded policy `install-modules` already uses (two attempts by default; `UNITY_INSTALL_RETRIES` sets the count for both commands, and the `--retries` flag exists on `install-modules` only — `unity install` has no such flag) before the install fails. When installing an editor with several modules, a failed module no longer aborts the whole batch — `unity install` (and `unity install-modules`) continue with the remaining items and exit non-zero if any failed. Each editor and module is listed as installed (✓), failed (✗), or pending (·); the NDJSON `result` frame carries the same breakdown as an `items` array (each entry has `uid`, `name`, `kind`, `status`), so scripts can tell exactly which modules succeeded even on a non-zero exit.

**NDJSON progress frames** for `unity install` and `unity install-modules` include a `phase: 'download' | 'install'` field so scripts can switch to an indeterminate spinner during the install phase (which is genuinely indeterminate — NSIS on Windows only reports success/failure). During the install phase, `pct` is locked at 50 and only jumps to 100 on completion. Module download/install progress is nested under the parent editor via `parentItemUid`, so consumers see one editor group with its modules rather than one group per module.

On an interactive terminal, `unity install` also reports progress to the terminal application itself via the `OSC 9;4` escape sequence — on Windows Terminal the taskbar icon fills with download/install progress and spinners show as indeterminate, so you don't need to keep the window focused. It's emitted only on a TTY (never in piped or machine-consumed output), always cleared on exit, and ignored by terminals that don't support it.

Module installers honor the per-module install command from the release manifest (e.g. Visual Studio on Windows uses `--passive`, not `/S`); the resolved command is surfaced in `unity modules list --json`. In the `--json` and `--format ndjson` rows of `unity modules list`, `unity editors module list` and `unity install-modules --list`, read `downloadBytes`, `installedBytes` (integers) and `isInstalled` (boolean): `status`, `download` and `installed` are display text, and `status` is translated. `unity install` self-heals a corrupted partial download by discarding the bad partial and re-downloading; a cross-process install lock prevents two concurrent installs of the same version from corrupting the unpack.

**Waiting on another install.** The Hub and the CLI share one install lock, so while the Hub (or another CLI) is installing, `unity install`, `unity install-modules` and `unity projects require` print `Waiting for another install to finish before continuing…` on stderr and carry on once it finishes. That wait has no limit by default. In a script or CI job, bound it:

```bash
# Fail at once if another install is already running, before this item is downloaded
unity install 6000.0.47f1 --yes --no-wait

# Wait up to 10 minutes in total, then fail the same way
unity install-modules -e 6000.0.47f1 -m android --timeout 600
```

`--timeout <seconds>` takes a whole number of seconds greater than 0 (digits only: `5m` and `1.5` are rejected) and is one budget for the whole command, not per module; `--no-wait` wins if both are given. CLI releases before it was renamed spell it `--wait-timeout`, which still works but prints a deprecation warning. Either way the command exits **9** with error code `INSTALL_LOCK_BUSY` (in the json envelope's `errors[].code`, and in the ndjson `result` frame), distinct from a failed install's exit 6, so a script can retry later. The error says another install is in progress and to run the command again once it finishes; the "Waiting for another install" line on stderr only appears while a wait is running, the default one or a bounded one, and never under `--no-wait`.

The lock is taken per item, so exit 9 does not mean nothing changed. `--no-wait` refuses an item before its download only when the lock is already held; if the Hub takes it while the item downloads, the item fails after the download. And `unity install <version> -m android --no-wait` can install the editor, then stop at the module: check `data.completedUids` in the ndjson `result` frame, and finish with `unity install-modules -e <version> -m android` rather than re-running `unity install`, which now fails because the editor is already installed. A second `unity install` of the **same** version fails at once with exit 6 ("Another install of … is already in progress") before it reaches this lock, whatever the flags.

#### Another operating system's Editor (`--operating-system`)

`unity releases` and `unity install --dry-run` look up the host's OS by default. `--operating-system <windows|macos|linux>` looks up another one, so you can check what a Windows build machine would download from a Mac. It has no short flag.

```bash
# Windows releases, from any host
unity releases --operating-system windows --limit 5 --format json

# What a Windows install of this version and its modules would download (il2cpp resolves to windows-il2cpp)
unity install 6000.0.47f1 --dry-run --operating-system windows -m il2cpp --format json
```

Another OS's listing asks for the builds that OS ships and prefers its usual one: arm64 on macOS, x86_64 on Windows and Linux. Pass `--architecture arm64` to `install` for a Windows arm64 build. Its `installed` column is always false, because this machine can't hold another OS's Editor. The dry run's `data.editor` gains `"operatingSystem": "windows"` and `alreadyInstalled` is always false; the human plan adds an `Operating system: Windows` line. Naming the host's own OS changes nothing.

The CLI never runs another OS's installer. Without `--dry-run`, or with `--changeset` or `--resume`, it stops with exit 2 and `CROSS_OS_INSTALL_UNSUPPORTED`, naming the dry run and `unity releases`. An unknown value is exit 2 with `INVALID_OPERATING_SYSTEM`. `--list-modules` lists an installed Editor's modules, so it can't be combined with `--operating-system` (exit 2); `unity modules list` and `unity editors --releases` stay on the host's OS.

#### Linux arm64: experimental x86_64 Editor under FEX-Emu

Unity ships no Linux arm64 Editor. On a Linux arm64 host you can opt in to running the x86_64 Editor under [FEX-Emu](https://github.com/FEX-Emu/FEX) by setting `UNITY_EXPERIMENTAL_FEX_EMU=1`. The opt-in takes effect only when a FEX-Emu entry point (`FEX`, or the older `FEXLoader`) is on PATH and an x86_64 RootFS resolves (`FEX_ROOTFS`, or the `RootFS` in FEX-Emu's own `Config.json`); `unity doctor` reports both as its `fex-emu` row. While it is active, `unity install` defaults to `x86_64`, and every Editor launch (`open`, `projects open`, `run`, `test`, `build`, `projects create`) runs as `FEX <editor> <args>` with no `binfmt_misc` needed.

This is **experimental and unsupported**: C# script compilation is known to crash under FEX-Emu ([FEX-Emu/FEX#5766](https://github.com/FEX-Emu/FEX/issues/5766)), so don't use it for real development. Every command it affects says so: a warning on stderr for human and tsv output, and a `FEX_EMU_EXPERIMENTAL` entry in the envelope's `notifications` for json and ndjson. Without the opt-in nothing changes.

#### Portable install into a folder (`--download-directory`)

`-d, --download-directory <dir>` (the old `--install-dir` spelling still works, but it is deprecated and prints a warning) installs a self-contained Editor into a folder you name, for an ephemeral CI workspace or a side-by-side copy. The Editor lands in `<dir>/<version>` (or `<dir>/<version>-<arch>` when that slot is taken), with its own `modules.json` and `metadata.hub.json`. Nothing else changes: the global install path (`unity install-path`), the Hub's Editor list and the Linux desktop entry are left alone, so `unity editors` doesn't list it. A folder outside the Hub's install folder doesn't touch the shared install-state database either. A relative path resolves against the working directory.

```bash
# CI: install into the workspace, no prompts, machine-readable result
unity install 6000.0.47f1 -d ./.unity -m android --non-interactive --yes --accept-eula --format json

# See where it would go first
unity install 6000.0.47f1 -d ./.unity --dry-run --format json

# Add a module later: point install-modules at the same folder
unity install-modules -e 6000.0.47f1 -d ./.unity -m ios --yes

# Register it with the Hub afterwards, if you want it listed (Linux path shown;
# a human install run prints the exact command for your platform)
unity editors add ./.unity/6000.0.47f1/Editor/Unity
```

- The json/ndjson result carries the Editor folder as `data.path` (the dry run too). A human run prints the folder and the `unity editors add` command that registers it.
- Only the target folder is checked for an existing copy: a second install there fails with exit 6 unless you pass `--force`.
- Installs into different folders run at once: each folder has its own lock, and none of them waits on the Hub's install lock. Two installs of one version into the **same** folder still refuse each other.
- A `-d` that names the Hub's own install folder (or any folder, when the install-path config can't be read) locks like a normal install: it waits on the Hub's install lock and uses the shared install-state database.
- `--resume` can't be combined with it (exit 2): there is no per-folder download state to resume. Re-run the install instead.
- `--list-modules` can't be combined with it either (a usage error, exit 2).

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

`unity modules list` honors `--format ndjson` (empty results emit a clean, empty NDJSON stream). The last column is `Aliases` — the alternate module names `-m`/`--module` accepts for that row; under `--format json` it's the `aliases` field (renamed from the old `downloaderName`).

### install-modules

```bash
# List available modules without installing
unity install-modules --editor-version 6000.0.47f1 --list

# Install specific modules
unity install-modules --editor-version 6000.0.47f1 --module android --module ios

# Install all available modules
unity install-modules --editor-version 6000.0.47f1 --all --yes

# Include child modules (default behaviour)
unity install-modules --editor-version 6000.0.47f1 --module android --child-modules

# Exclude child modules
unity install-modules --editor-version 6000.0.47f1 --module android --no-child-modules

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

# An Editor installed with `unity install --download-directory`: look it up in that folder
unity install-modules --editor-version 6000.0.47f1 --download-directory ./.unity --module android
```

`--list` and `--all` are mutually exclusive. `--list` is also mutually exclusive with `--module`.

A module whose download or validation fails intermittently — common for large modules such as Android SDK/NDK and OpenJDK — is retried automatically (up to twice with exponential backoff by default) instead of failing the whole run; already-installed modules are never re-downloaded, and retry attempts surface in both human and `--format ndjson` output.

`--module android ios` (space-separated values after a single `--module`) and `--module android --module ios` (repeated flag) are equivalent — both install all listed modules.

`--child-modules` / `--no-child-modules` is the primary spelling on both `install` and `install-modules`, matching `unity editors module add`; the old `--cm` / `--no-cm` shorts keep working as hidden aliases.

Module discovery works for editors registered via `unity editors add <path>` (located editors), not just editors installed by the Hub.

---

