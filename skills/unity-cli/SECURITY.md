# Security notes — unity-cli skill

This skill documents the official first-party [`unity` CLI](https://public-cdn.cloud.unity3d.com/hub/prod/cli/). A few of its capabilities are powerful by design and are flagged by automated skill scanners. They are intentional, first-party functionality with the safeguards described below.

<!-- skill-security:accept SEC_POWER_CAP, SEC_INSTALL_PIPE -->

## Accepted, by-design capabilities

### Local Editor control and C# evaluation

`unity command`, `unity command eval`, and `unity shell --protocol ndjson` can drive a Unity Editor that is already open on the same machine and run C# through the project's `com.unity.pipeline` package. This executes **entirely on the local machine, in the current user's account, against the user's own Editor** — it is not remote access and grants no privilege the user does not already have at their own terminal. It is the CLI's core value for AI-assisted and automated Editor workflows.

Machine/agent mode (`unity shell --protocol ndjson`) runs the exact commands the caller sends. It validates framing (malformed or unknown requests return an error frame rather than crashing or ending the session), runs every command non-interactively, and returns structured JSON response frames (JSON-serialized, so control characters are escaped for the consuming parser). Callers must feed it **trusted input only** — commands they construct themselves — and never commands assembled from untrusted third-party content, exactly as they would guard any shell.

### Install via the official CDN

The documented install downloads and runs an install script from Unity's official CDN, `public-cdn.cloud.unity3d.com`, **over HTTPS (TLS)**. This pipe-to-shell pattern is a deliberate, industry-standard install convenience for a first-party tool. Beyond TLS, the script verifies the downloaded binary against the SHA-256 published in the channel's release manifest and aborts on mismatch — or when no SHA-256 tool is available — so a tampered payload is rejected rather than executed.

On Linux the script installs a self-contained binary under `~/.local/bin` and does not modify system package sources. Separately, Unity publishes `.deb`/`.rpm` packages (rpm packages GPG-signed) to its official apt/rpm repositories, for users who prefer package-manager-managed updates. Installing those packages **does** change system state: their maintainer scripts add a Unity apt/yum repository entry and install Unity's signing key into the system keyring, so `apt`/`dnf` can verify and deliver subsequent updates.
