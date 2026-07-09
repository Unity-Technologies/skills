# Config & Hub — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

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

