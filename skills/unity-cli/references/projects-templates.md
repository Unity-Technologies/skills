# Projects, releases & templates — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

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

**Flag names differ by subcommand:** `projects create` and `projects link vcs` use `--git-namespace` / `--git-repo`, while `projects clone` (below) uses `--vcs-namespace` / `--vcs-repo`. Copy the names for the exact command you're running, and confirm with `--help` if unsure.

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

