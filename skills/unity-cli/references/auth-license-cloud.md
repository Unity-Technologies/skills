# Auth, license & cloud — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

---

### Auth

```bash
# Check login status
unity auth status --format json

# Login (opens browser for OAuth)
unity auth login

# Login with service account credentials (CI — skips browser)
# Preferred: read secret from stdin to avoid shell-history and process-list exposure
unity auth login --client-id <id> --secret-from-stdin

# A --client-secret flag also exists, but passing a secret as a
# command-line argument exposes it in shell history and the process list.
# Avoid it — use --secret-from-stdin (above) or the
# UNITY_SERVICE_ACCOUNT_ID / UNITY_SERVICE_ACCOUNT_SECRET env vars instead.

# Login without persisting credentials to the keyring (ephemeral CI)
unity auth login --client-id <id> --secret-from-stdin --no-store

# Logout (clears both service-account and OAuth credential slots)
unity auth logout

# Skip the confirmation prompt
unity auth logout --yes
```

**Separate sign-in from Hub.** As of `0.1.0-beta.8`, the CLI and the GUI Hub store their sign-in credentials **separately** — signing in to one no longer signs you out of (or overwrites the account of) the other, so each can stay signed in as a different account. (In earlier betas they shared a single keyring session.)

**Service-account credentials via env vars** (`UNITY_SERVICE_ACCOUNT_ID` + `UNITY_SERVICE_ACCOUNT_SECRET`) mint bearer tokens automatically for the duration of the process — no browser round-trip, no keyring write. If only one of the two is set, the CLI prints a warning on stderr instead of silently falling back to the keyring/OAuth identity.

The interactive `unity auth login` flow prints the sign-in URL to the terminal **before** attempting to launch the browser, which unblocks remote/headless sessions (SSH, containers, dev VMs) where `xdg-open` / `open` has no graphical session to attach to. With `--format json`, an `auth_url=…` progress frame is emitted so machine consumers can capture the URL without parsing human text.

`unity auth status` reflects real session state (including an explicit "session expired" message), not optimistic local assumptions. `unity doctor` and `unity cloud status` report the same real session state.

---

### License — list, activate, return

```bash
# List the Unity licenses active on this machine
unity license
unity license list             # explicit form, identical output
unity license --format json    # machine-readable

# Summary: active license(s) + sign-in state
unity license status

# Activate a license — choose exactly one mode (default = signed-in subscription)
unity license activate                              # signed-in user's subscription (entitlement) licenses
unity license activate --serial SC-…                # serial-based (ULF) activation, no sign-in needed
unity license activate --personal --accept-eula     # free Unity Personal license (must accept the EULA)
unity license activate --floating                   # lease a seat from the configured floating server
unity license activate --file ./Unity_lic.ulf       # offline activation from a .ulf / .xml file
unity license activate --generate-request ./req.alf # write an offline activation request (air-gapped)

# Return the active assigned/subscription licenses (prompts to confirm; --yes skips)
unity license return
unity license return --yes

# Floating (network) license server
unity license server list      # the configured floating license server(s)
unity license server status    # reachability + available seats
```

`list` columns: product, license type (`Floating` / `Assigned` / `ULF`), organization, and expiry. `status` prints a one-glance summary — the active license(s) and whether you're signed in — and exits non-zero (`4`) when no license is active, so it works as a scriptable health check. The first licensing command downloads the Unity licensing client on demand; as of `0.1.0-beta.8`, if the client is unavailable `list` reports a clear error and exits non-zero (matching `status`), rather than printing an empty list.

`activate` takes a single mode flag (combining them is a usage error). The default (no flag) and `--personal` activate the signed-in user's entitlements — sign in first with `unity auth login`. `--personal` also requires `--accept-eula` to acknowledge the Unity Personal license terms. `--serial` / `--file` work offline without sign-in. `--floating` requires a configured floating license server (exit `4` if none is set). `--generate-request` writes a `.alf` request for air-gapped activation instead of activating. `return` returns the active licenses, prompting for confirmation first — pass `--yes` to skip (required in non-interactive shells and with `--json`). All honor `--json` / `--format` and exit non-zero on failure (`2` bad usage, `3` sign-in required, `4` floating not configured, `6` licensing-client error).

`unity license server list` shows the configured floating license server (from the `licensingServiceBaseUrl` machine setting; a pure settings read, no client download). `unity license server status` contacts that server and reports reachability plus available seats — exit `4` when no server is configured, `6` when configured but unreachable.

---

### Cloud — Unity Cloud organizations and projects

Requires being signed in (`unity auth login`).

```bash
# Show cloud sign-in state and active organization
unity cloud status --format json

# Organizations
unity cloud org list --format json
unity cloud org current                       # print the active default org id
unity cloud org set-default <id-or-name>      # set active default org
unity cloud org clear-default                 # revert to "All Organizations"

# Projects in the active organization
unity cloud project list --format json

# Override the active organization for a single call
unity cloud project list --cloud-org <id-or-name>   # also via UNITY_CLOUD_ORG env var
```

---

