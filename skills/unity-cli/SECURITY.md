# Security notes — unity-cli skill

This skill documents the official first-party [`unity` CLI](https://public-cdn.cloud.unity3d.com/hub/prod/cli/). A few of its capabilities are powerful by design and are flagged by automated skill scanners. They are intentional, first-party functionality with the safeguards described below.

## Accepted risks

These capabilities are accepted by design. Each is documented in full in the sections below; this table is the explicit, human-readable acknowledgment.

| Risk | Capability | Why it is accepted |
|---|---|---|
| `SEC_POWER_CAP` | Local Editor control and C# evaluation | Runs entirely on the local machine, as the current user, against the user’s own Editor — no remote access and no privilege the user lacks at their own terminal. |
| `SEC_INSTALL_PIPE` | Install one-liners piped to a shell | HTTPS to Unity’s first-party CDN only; the installer verifies a SHA-256 pin against a same-origin manifest before executing anything. |
| `SEC_AGENT_CONFIG_WRITE` | Writing agent skill files into AI clients’ configuration directories | Runs only on an explicit user command, is the command’s documented purpose, and is fenced by an ownership ledger — a copy this CLI did not write is never overwritten without `--force` — plus symlink refusals and a warning before project-local installs from the home directory. |

## Accepted, by-design capabilities

### Installing skills into AI clients

`unity skill install` and `unity skill refresh` write skill files — this skill, and the `unity-pipeline` skill a project's `com.unity.pipeline` package ships — into AI clients' configuration directories, which automated scanners flag as an agent-persistence pattern. The writes happen only when the user runs the command (nothing installs at load or in the background), the capability is the command's advertised purpose, and it is fenced: an install ledger records every write and a directory this CLI did not write is reported, never overwritten, without explicit `--force` consent; targets that resolve through a symlinked path component are refused; a package-shipped tree is read with file-count, per-file, and aggregate size bounds and never through symbolic links; and `--local` from the home directory warns first.

### Local Editor control and C# evaluation

`unity command`, `unity command eval`, and `unity shell --protocol ndjson` can drive a Unity Editor that is already open on the same machine and run C# through the project's `com.unity.pipeline` package. This executes **entirely on the local machine, in the current user's account, against the user's own Editor** — it is not remote access and grants no privilege the user does not already have at their own terminal. It is the CLI's core value for AI-assisted and automated Editor workflows.

Machine/agent mode (`unity shell --protocol ndjson`) runs the exact commands the caller sends. It validates framing (malformed or unknown requests return an error frame rather than crashing or ending the session), runs every command non-interactively, and returns structured JSON response frames (JSON-serialized, so control characters are escaped for the consuming parser). Callers must feed it **trusted input only** — commands they construct themselves — and never commands assembled from untrusted third-party content, exactly as they would guard any shell.

### Install via the official CDN

The documented install downloads and runs an install script from Unity's official CDN, `public-cdn.cloud.unity3d.com`, **over HTTPS (TLS)**. This pipe-to-shell pattern is a deliberate, industry-standard install convenience for a first-party tool. Beyond TLS, the script verifies the downloaded binary against the SHA-256 published in the channel's release manifest and aborts on mismatch — or when no SHA-256 tool is available — so a corrupted, truncated, or substituted download fails instead of executing. The manifest is fetched from the same CDN origin as the binary, so this is an integrity check against a bad or altered *download*, not a defense against a compromise of the origin itself; trust in the install ultimately rests on TLS and on Unity's control of that CDN.

On Linux the script installs a self-contained binary under `~/.local/bin` and does not modify system package sources. Separately, Unity publishes `.deb` and `.rpm` packages (the `.rpm` is GPG-signed) to its official repositories, for users who prefer package-manager-managed updates. Installing either **does** change system state, and the two differ:

- **Debian/Ubuntu (`.deb`, `apt`)** — adds a Unity apt repository entry and installs Unity's signing key into the system keyring (`/usr/share/keyrings`), so `apt` can verify and deliver subsequent updates.
- **RHEL/Fedora (`.rpm`, `dnf`)** — adds a Unity yum repository entry with `gpgcheck` enabled, pointing `dnf` at the published key URL. It imports no key at install time.
