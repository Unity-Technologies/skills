# Security notes — unity-cli skill

This skill documents the official first-party [`unity` CLI](https://public-cdn.cloud.unity3d.com/hub/prod/cli/). A few of its capabilities are powerful by design and are flagged by automated skill scanners. They are intentional, first-party functionality with the safeguards described below.

<!-- skill-security:accept SEC_POWER_CAP, SEC_INSTALL_PIPE -->

## Accepted, by-design capabilities

### Local Editor control and C# evaluation

`unity command`, `unity command eval`, and `unity shell --protocol ndjson` can drive a Unity Editor that is already open on the same machine and run C# through the project's `com.unity.pipeline` package. This executes **entirely on the local machine, in the current user's account, against the user's own Editor** — it is not remote access and grants no privilege the user does not already have at their own terminal. It is the CLI's core value for AI-assisted and automated Editor workflows.

Machine/agent mode (`unity shell --protocol ndjson`) runs the exact commands the caller sends. It validates framing (malformed or unknown requests return an error frame rather than crashing or ending the session), runs every command non-interactively, and returns structured JSON response frames (JSON-serialized, so control characters are escaped for the consuming parser). Callers must feed it **trusted input only** — commands they construct themselves — and never commands assembled from untrusted third-party content, exactly as they would guard any shell.

### Install via the official CDN

The documented install downloads and runs an install script from Unity's official CDN, `public-cdn.cloud.unity3d.com`, **over HTTPS (TLS)**. This pipe-to-shell pattern is a deliberate, industry-standard install convenience for a first-party tool. On Linux the installer also configures Unity's official apt/rpm repositories, so subsequent updates are managed by the system package manager (`apt upgrade` / `dnf upgrade`); managed package-manager installs are the preferred path where available.

## What this file is

`SECURITY.md` is the machine-readable accepted-risk manifest read by the Tier 1 skill validator. The `skill-security:accept` directive above records that the capabilities scanned as `SEC_POWER_CAP` and `SEC_INSTALL_PIPE` are known and accepted, with the rationale above. A **new** powerful capability or install pattern not covered here fails validation until it is reviewed and added — so acceptance stays explicit and auditable.
