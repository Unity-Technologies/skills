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

#### config accelerator

View or change the [Unity Accelerator](https://docs.unity3d.com/Manual/UnityAccelerator.html) (asset-import cache server) endpoint. Once persisted, `unity run`, `unity test` and `unity build` inject it into the Editor automatically — see [build-run-test.md](build-run-test.md).

```bash
# Show the resolved endpoint and its source
unity config accelerator
unity config accelerator --json

# Persist an endpoint
unity config accelerator cache.example.com:10080

# A bare host defaults to port 10080 (the Accelerator default)
unity config accelerator cache.example.com

# Clear the persisted endpoint (reports whether there was anything to clear)
unity config accelerator --unset
```

**The value is `host:port`, not a URL.** A scheme is rejected rather than stripped (`http://cache.example.com:10080` fails with exit 6), because `-cacheServerEndpoint` takes a host and a port — persisting a URL would produce a silent non-connection later. IPv6 literals are bracketed: `[::1]:10080`.

**Resolution priority** (highest → lowest):
1. `--accelerator <host:port>` — accepted only on `run` / `test` / `build`, after the command name (one-shot override for that invocation)
2. `UNITY_ACCELERATOR` env var
3. Persisted `accelerator.json` (`unity config accelerator <host:port>`)
4. None

The reported source is one of `flag`, `env`, `settings`, `none`. The env var and the persisted file are resolved on **every** command, which is why `unity diagnose accelerator` can report them; only the flag is scoped to the three commands that inject. A malformed higher-priority candidate is skipped rather than fatal — except an explicit `--accelerator`, which fails as a usage error (exit 2) instead of silently falling through.

`unity config accelerator` with no argument reports the `env` → `settings` → `none` view of persistent configuration. Use `unity diagnose accelerator` (see [diagnostics-maintenance.md](diagnostics-maintenance.md)) for the same resolution plus project settings and reachability. Neither command accepts `--accelerator`, so to see what a one-shot override resolves to, pass it to the `run` / `test` / `build` invocation that uses it.

**A configured endpoint is not always enough.** Every `-cacheServer*` argument overrides *Editor Preferences*, not Project Settings, and `ProjectSettings/EditorSettings.asset`'s `m_CacheServerMode` decides whether preferences are consulted at all. A project set to `Disabled` (mode `2`) ignores the injected flags; the CLI warns when it sees that. Full explanation: `apps/cli/docs/accelerator.md`.

---

### config get / set / list / unset — read or write any setting by key

A generic key-value interface over the same persisted settings the purpose-built subcommands above already manage — read or write one by name instead of having to know its dedicated command.

```bash
# Every configuration key and its resolved value
unity config list
unity config list --format json

# Read one key
unity config get proxy

# Write one key (validated the same way its dedicated command would validate it)
unity config set accelerator cache.example.com:10080
unity config set update-check off

# Clear one back to its default
unity config unset proxy.bypass
```

Recognized keys, and what they back onto:

| Key | Same as | Notes |
|---|---|---|
| `proxy` | `unity config proxy <url>` | Secret-shaped values are redacted on read/echo (`http://***:***@host`) — the real value is still stored and used. |
| `proxy.bypass` | `unity config proxy <url> --bypass <hosts>` | Comma-separated hosts; writing/clearing it leaves the sibling `proxy` key untouched. |
| `accelerator` | `unity config accelerator <endpoint>` | Normalized to `host:port` on write. |
| `update-check` | `unity config update-check on\|off` | Value is `on`/`off`. |

An unknown key, a read-only key (none exist yet — the mechanism exists for a future resolved-only value), or an invalid value for a writable key is rejected with exit **2** and a message pointing at `unity config list`. `--format json` returns `{key, value}` for `get`/`set`, `{key, cleared}` for `unset`, and `{entries: [{key, value, writable}, …]}` for `list`.

#### config resolve — build/test defaults and the per-project `UnityCliConfig.json`

A committed `ProjectSettings/UnityCliConfig.json` declares `unity build` / `unity test` defaults for a project so a team stops repeating them on every invocation and in every CI workflow. The CLI finds it by walking up from the working directory (or the explicit project path) to the project root, and reads nine keys: `build.target`, `build.outputPath`, `build.profile`, `build.timeout`, `test.mode`, `test.reportFormat`, `test.coverage`, `test.coverageOptions`, `test.timeout`. The same keys can be set globally with `unity config set build.target …`.

Precedence, highest first: command-line flag, environment variable (only `build.timeout` / `test.timeout` have one), the project file, the global config layer, then the CLI’s built-in default. A flag exists only on the `unity build` / `unity test` invocation itself, so `config resolve` never reports `flag`: its `source` is one of `env`, `project`, `global` or `default`.

```bash
# Which value applies, and which layer supplied it: env, project, global, or default
unity config resolve build.target /path/to/MyProject
unity config resolve test.mode --format json     # {key, value, source}
```

---

### Context — named sets of the five defaults

The CLI keeps five defaults, each normally set by its own command: the active account (`auth switch`), the default organization (`cloud org set-default`), the default cloud project (`cloud project set-default`), the default editor (`editors default`) and the install path (`install-path`). A **context** is a name for one combination of all five, so moving between two setups is one command instead of five in the right order.

```bash
# Record the current five settings under a name
unity context save work

# Apply them again later — all five, or none
unity context use work

# What exists, and which one is currently in effect
unity context list
unity context current

# Forget one (the settings it named are left as they are)
unity context delete work
```

Names may use letters, digits, dots, dashes and underscores, up to 64 characters, and are matched case-insensitively — re-saving an existing name overwrites it in place.

**Applying is all-or-nothing.** The account, editor and install path are validated before anything is written; the organization and project are then checked against Unity Cloud under the account the context selects. Any failure names the missing item, exits **6**, and leaves every setting exactly as it was — a context pinning an editor you have since uninstalled fails with that editor's version rather than half-switching.

**A context clears what it does not pin.** Switching to a context that names no organization clears the organization rather than leaving the previous one's behind. The one exception is the account: there is no "signed in as nobody", so a context naming no account leaves the active one alone (and `unity context current` will still match such a context whoever is signed in).

**Offline and signed-out still work.** When Unity Cloud cannot be reached the stored organization and project are applied as-is and a warning says they were not verified, rather than the switch being refused.

`unity context use` reports every setting with a `*` against the ones that changed. The `setting` values are stable machine tokens — `account`, `organization`, `project`, `editor`, `install-path` — in every format, human included, so a script can key on them. Only `unity context list`'s column *headers* are translated.

Two name collisions worth knowing: this is unrelated to `unity build --profile`, which names a Unity Build Profile asset; and inside `unity shell`, bare `context` still prints that session's own ephemeral project/org selection, while `context <subcommand>` reaches this group.

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

