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

