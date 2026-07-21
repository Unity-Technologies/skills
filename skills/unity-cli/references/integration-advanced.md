# Integration & advanced — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

---

### MCP — Model Context Protocol server (AI agent integration)

New in `0.1.0-beta.8`. `unity mcp` starts a Model Context Protocol server, built into the `unity` binary, that exposes the commands of a connected Unity Editor as MCP tools. AI agent clients connect over stdio, list those tools, and run them. The server starts even when no Editor is running and reports that it isn't connected; commands that a connected Editor adds show up as tools automatically.

```bash
# Start the MCP stdio server (usually launched by the AI client, not by hand)
unity mcp

# Pin the server to a specific Unity project (the CLI discovers the running Editor itself)
unity mcp --project-path /path/to/MyProject
```

`unity mcp` no longer accepts `--instance <host:port>`: talking to an Editor requires that Editor's per-instance auth token, which a bare host and port can't carry, so the CLI always discovers running Editors itself — run from the project directory or pass `--project-path` to target one. Editors launched to create a new project (`-createproject`) are discovered too.

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
# List the Editors the CLI can reach and the Pipeline package status of each.
# Also shows each project's installed Pipeline version and flags when the registry has a newer one.
unity pipeline list --format json

# Install / update the Pipeline package into a project (auto-detects project if omitted)
unity pipeline install
unity pipeline install --project-path /path/to/MyProject
unity pipeline install --force          # always rewrite the manifest to the latest version

# Install a specific version (validated against the registry first; overwrites any pinned version).
# NOTE: the flag is --package-version, NOT --version (which collides with the global -V, --version).
unity pipeline install --package-version 0.3.0-exp.1

# Upgrade the package to the latest, but only when the registry has a newer one
# (otherwise reports it's already up to date and leaves manifest.json untouched).
# Requires the package to be installed already.
unity pipeline upgrade
unity pipeline upgrade --project-path /path/to/MyProject

# List every version published to the Unity registry, newest first (marks the current latest)
unity pipeline list-versions --format json
```

`pipeline install` options: `--project-path <path>`, `--force`, `--package-version <version>`. The package is resolved from the Unity registry and written to `Packages/manifest.json`. Unlike `pipeline install --force` (which always rewrites to latest), `upgrade` compares the pinned version first.

When multiple Editors are running, `install` and `upgrade` consider only the editors that actually need the operation (`install` → editors without the package; `upgrade` → editors behind the registry's latest). If exactly one needs it, that editor is chosen automatically; if none do, the command reports there's nothing to do; if several do, an interactive terminal shows a selector while non-interactive contexts (machine output, non-TTY, or `--non-interactive`) error and list the projects so you can pass `--project-path`.

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

# Target a specific project (the CLI discovers the running Editor itself) or a Player runtime
unity command editor_play --project-path /path/to/MyProject
unity command <command> --runtime "MyGame"
unity command <command> --runtime-path /path/to/port-file

# Set a timeout (default: 30 seconds)
unity command editor_play --timeout 60
```

If no editor with a reachable Pipeline server is found, the command errors with guidance (make sure the editor is running and its Pipeline server is up).

`unity command` no longer accepts `--instance <host:port>` — the CLI discovers running Editors itself, so run from the project directory or pass `--project-path` to target one.

#### list — discover a connected Editor's tools

`unity list` queries the connected Unity Editor (via the Pipeline package) and prints every registered tool with its name, description, group, and parameter schema. Use it to discover what's callable in the current Editor session without reading source code — especially when the project registers custom `[CliCommand]` tools. Unlike `unity command` (which lists *and* runs), `list` is discovery/introspection only.

```bash
unity list
unity list --format json
```

Honors the global `--quiet` and `--no-banner` flags. On a connection failure it suggests `unity pipeline list` to diagnose.

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

### Shell — interactive REPL

`unity shell` boots the CLI once and runs many commands in the same warm process, avoiding the per-command startup cost of separate `unity …` invocations. Enter any command **without** the `unity` prefix.

```bash
unity shell
# unity> status --format json
# unity> config proxy http://proxy:8080
# unity> config proxy            # the write above is visible to this read
# unity> exit
```

- Arguments are tokenized shell-style (single/double quotes; unquoted Windows backslash paths are preserved).
- Leave with `exit`, `quit`, or Ctrl-D; blank lines and `#` comments are ignored.
- Ctrl-C cancels a cancellable running command (such as `build`) and returns to the prompt; for a command that doesn't yet support cancellation the first Ctrl-C is held (with a hint) and a second quick press force-quits the session.
- The prompt shows the previous command's exit code when it was non-zero.
- Interactive prompts (confirmations, sign-in) work inside the shell, and a write in one command (`auth logout`, `config`, `editors default`, …) is visible to the next.
- Piped/scripted sessions (`… | unity shell`) run every line and always exit 0.

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

Compile failures surface the Roslyn diagnostics and exit non-zero. Targeting options match `command`: `--project-path`, `--runtime <name>`, `--runtime-path <path>` (the CLI discovers the running Editor itself — there is no `--instance`).

### cloud-pipeline — Unity Cloud Pipeline

Manage Unity Cloud Pipeline resources. Subcommand groups: `status`, `onboard`, `assets` (`list`/`status`/`url`), `branches` (`list`/`show`/`create`/`url`/`enable`/`edit`/`disable`), `pending-changes list`, `files` (`create`/`update`/`delete`/`move`), `pull-request create`. Use `unity cloud-pipeline --help` (development build) for the full flag set.

### collab — Unity collaboration (annotations & attachments)

Manage review annotations and attachments. Subcommand groups: `annotations` (`count`/`create`/`delete`/`get`/`update`/`replies`/`resolve`/`status`/`unresolve`) and `attachments` (`list`/`delete`/`update`). Use `unity collab --help` (development build) for the full flag set.

---

