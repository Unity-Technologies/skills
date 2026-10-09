# Security notes — unity-cli skill

This skill documents the official first-party [`unity` CLI](https://public-cdn.cloud.unity3d.com/hub/prod/cli/). A few of its capabilities are powerful by design and are flagged by automated skill scanners. They are intentional, first-party functionality with the safeguards described below.

## Accepted risks

These capabilities are accepted by design. Each is documented in full in the sections below; this table is the explicit, human-readable acknowledgment.

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Local Editor control and C# evaluation | Runs entirely on the local machine, as the current user, against the user’s own Editor — no remote access and no privilege the user lacks at their own terminal. |
| `SEC_INSTALL_PIPE` | Install one-liners piped to a shell | HTTPS to Unity’s first-party CDN only; the installer verifies a SHA-256 pin against a same-origin manifest before executing anything. |
| `SEC_AGENT_CONFIG_WRITE` | Writing agent skill files into AI clients’ configuration directories, turning an installed skill off or back on, and installing Unity’s own agent plugin into Claude Code, Codex, or Grok Build | Runs only on an explicit user command, is the command’s documented purpose, and is fenced by an ownership ledger — a copy this CLI did not write is never overwritten without `--force`, and the enable/disable verbs cannot act on one at all — plus symlink refusals and a warning before project-local installs from the home directory. The plugin install names one hardcoded first-party source, is performed by the agent’s own CLI (`claude`, `codex`, or `grok`) rather than by writing files, and reinstalls nothing. |

## Reading an automated scanner's verdict on this skill

The directive above records the risks this skill's maintainers have reviewed and accepted, for the repository's own validator. A scanner run elsewhere generally will not consult it, and will read the **whole folder** rather than the change in front of it. Both facts matter when you are looking at a verdict:

- A verdict describes this skill's standing capabilities — the ones documented below — not whatever the current change happens to touch. A documentation-only edit can still draw a verdict, because the capabilities it is reacting to were already here.
- Verdicts from an AI classifier are a judgement, not a rule, and the same unchanged folder can be scored differently on different runs. Treat a single verdict as a prompt to check, never as a result on its own.

So before acting on one, compare it against what actually changed. If the change introduces no capability beyond those accepted below, the verdict is re-deriving something already reviewed and recorded here — note that and move on. If it does introduce one, it needs a real review and an entry in this file, because this skill is published and shipped inside the CLI.

## Accepted, by-design capabilities

### Installing skills into AI clients

`unity skill install` and `unity skill refresh` write skill files — this skill, and the `unity-pipeline` skill a project's `com.unity.pipeline` package ships — into AI clients' configuration directories, which automated scanners flag as an agent-persistence pattern. The writes happen only when the user runs the command (nothing installs at load or in the background), the capability is the command's advertised purpose, and it is fenced: an install ledger records every write and a directory this CLI did not write is reported, never overwritten, without explicit `--force` consent; targets that resolve through a symlinked path component are refused; a package-shipped tree is read with file-count, per-file, and aggregate size bounds and never through symbolic links; and `--local` from the home directory warns first.

### Turning an installed skill off and back on

`unity skill disable <skill>` and `unity skill enable <skill>` change whether a skill an AI client has already installed still loads. Disabling renames that skill's `SKILL.md` to `SKILL.md.disabled` inside its own directory and records the choice; enabling renames it back. Automated scanners flag this as skill suppression — a skill that can switch other skills off is a recognized malicious pattern, and reading it that way is correct as a default. Here it is the advertised purpose of a user-run command, and it is fenced:

- **It can only reach installs this CLI itself recorded.** The set of skills these verbs can name comes entirely from the install ledger, so a skill this CLI did not install has no row, cannot be named, and cannot be found or touched. There is no path by which it disables a skill some other tool or the user placed.
- **Nothing runs at load or in the background.** Both verbs act only when the user runs them, on a skill the user names; `--dry-run` reports without touching the filesystem.
- **Nothing is deleted and no content is read or rewritten.** The only filesystem operation is renaming one fixed filename within the directory the ledger already records, so the skill's own files stay where they are and enabling restores the previous state exactly.
- **The paths are bounded the same way installs are.** The only path formed is the recorded install directory joined with a constant filename, that directory has already passed the ledger's absolute-path validation, and a target reached through a symlinked directory component is refused rather than followed — the same guard `install` and `refresh` apply.

The capability exists because a skill costs context in every session of the client that discovered it, whether or not it is ever used, so a user who installed several needs a way to stop paying for the ones a given project does not need — without deleting files they would otherwise have to reinstall.

### Installing Unity’s agent plugin (Claude Code, Codex, Grok Build)

`unity setup claude` installs a plugin into Claude Code, which automated scanners flag as an agent-persistence pattern. It is fenced as follows. It runs only when the user runs the command, and this skill tells an agent to run it only when the user asks for the plugin or agrees to install it, never on its own initiative. The only source it can name is Unity’s own first-party marketplace, `Unity-Technologies/unity-agent-plugin`, compiled into the CLI: no argument, file, or environment variable can point it anywhere else. It writes no files itself; it runs Claude Code’s own `claude plugin marketplace add` and `claude plugin install`, the documented install path, with fixed arguments, so Claude Code applies its own install handling. `--dry-run` prints both commands without running them, and an existing install is reported and never reinstalled or changed. It does not also copy a skill or register an MCP server of its own.

`unity setup codex` and `unity setup grok` install the same plugin into Codex and Grok Build under the same fence: they run only when the user runs them, and this skill tells an agent to run them only when the user asks for the plugin or agrees to install it. The source is the same compiled-in first-party repository, and nothing can point either command elsewhere. Each writes no files itself and runs only the agent's own documented install commands with fixed arguments: `codex plugin marketplace add` and `codex plugin add`, or `grok plugin install … --trust`. `--trust` is Grok Build's consent flag for a non-interactive install, and it is passed only because the user ran the command. `--dry-run` prints the commands without running them, and an existing, enabled install is reported and never reinstalled or changed. A Codex plugin the user disabled (`enabled = false` in Codex’s `config.toml`) is the one exception: `unity setup codex` treats it as not installed and runs the same two `codex` commands again, which re-enables it. That only happens when the user runs the command.

### Local Editor control and C# evaluation

`unity command`, `unity command eval`, and `unity shell --protocol ndjson` can drive a Unity Editor that is already open on the same machine and run C# through the project's `com.unity.pipeline` package. This executes **entirely on the local machine, in the current user's account, against the user's own Editor** — it is not remote access and grants no privilege the user does not already have at their own terminal. It is the CLI's core value for AI-assisted and automated Editor workflows.

Machine/agent mode (`unity shell --protocol ndjson`) runs the exact commands the caller sends. It validates framing (malformed or unknown requests return an error frame rather than crashing or ending the session), runs every command non-interactively, and returns structured JSON response frames (JSON-serialized, so control characters are escaped for the consuming parser). Callers must feed it **trusted input only** — commands they construct themselves — and never commands assembled from untrusted third-party content, exactly as they would guard any shell.

### Launching the project’s own build

`unity build run` launches a player the CLI itself recorded from a previous `unity build` on the same machine — or the output the user names with `--path` — without rebuilding. It downloads nothing and runs nothing it did not just build or was not explicitly pointed at: a desktop player is started as the current user, and a WebGL build is served from a loopback-only local HTTP server that the default browser opens. Automated scanners flag documented executable paths (`./Build/…`) as local code execution; here the executable is the user’s own build artifact, on the user’s own machine, and the command refuses (exit 6) when no build has been recorded, when the recorded build’s platform cannot run on the current OS, or when its output is gone.
### Install via the official CDN

The documented install downloads and runs an install script from Unity's official CDN, `public-cdn.cloud.unity3d.com`, **over HTTPS (TLS)**. This pipe-to-shell pattern is a deliberate, industry-standard install convenience for a first-party tool. Beyond TLS, the script verifies the downloaded binary against the SHA-256 published in the channel's release manifest and aborts on mismatch — or when no SHA-256 tool is available — so a corrupted, truncated, or substituted download fails instead of executing. The manifest is fetched from the same CDN origin as the binary, so this is an integrity check against a bad or altered *download*, not a defense against a compromise of the origin itself; trust in the install ultimately rests on TLS and on Unity's control of that CDN.

On Linux the script installs a self-contained binary under `~/.local/bin` and does not modify system package sources. Separately, Unity publishes `.deb` and `.rpm` packages (the `.rpm` is GPG-signed) to its official repositories, for users who prefer package-manager-managed updates. Installing either **does** change system state, and the two differ:

- **Debian/Ubuntu (`.deb`, `apt`)** — adds a Unity apt repository entry and installs Unity's signing key into the system keyring (`/usr/share/keyrings`), so `apt` can verify and deliver subsequent updates.
- **RHEL/Fedora (`.rpm`, `dnf`)** — adds a Unity yum repository entry with `gpgcheck` enabled, pointing `dnf` at the published key URL. It imports no key at install time.
