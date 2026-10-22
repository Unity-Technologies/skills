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
# The secret must be piped in: a terminal on stdin is refused, and the
# command stops waiting after 30 seconds if the pipe is never closed.

# A --client-secret flag also exists, but passing a secret as a
# command-line argument exposes it in shell history and the process list.
# Avoid it — use --secret-from-stdin (above) or the
# UNITY_SERVICE_ACCOUNT_ID / UNITY_SERVICE_ACCOUNT_SECRET env vars instead.

# Login without persisting credentials to the keyring (ephemeral CI)
unity auth login --client-id <id> --secret-from-stdin --no-store

# Sign the CLI out (clears its OAuth sign-in and any saved service-account
# credentials). There is no confirmation prompt; --yes is accepted and changes nothing.
unity auth logout

# Sign one stored account out of the CLI, rather than the active one. Its saved
# sign-in stays on the machine while another app, such as the Hub, still uses it.
unity auth logout user@example.com
```

**How sign-in is served.** Every command’s OAuth token read goes through a resident auth broker — a background `unity` process started on demand by the first command that needs a token, exiting on its own after two idle minutes. It seals the token store to the machine’s hardware where available (a non-exportable TPM key on Windows, TPM2 via `systemd-creds` on Linux, then DPAPI, the Secret Service, the Keychain and a local key file, in that order), and `unity doctor` reports the active tier. On macOS and Windows it verifies a connecting process’s code signature and refuses an unsigned or non-Unity-signed peer. The Editor identity helper that signs in an Editor started by `unity open` applies the same rule, so an Editor built from source (unsigned or not Unity-signed) is not supported for automatic sign-in. Set `UNITY_NO_AUTH_BROKER=1` to bypass the broker and read the OS keyring directly. `unity auth consumers` (below) lists the applications in its audit trail and revocation list.

#### Multiple accounts

The CLI stores more than one signed-in account and keeps one of them *active*. `unity auth login` adds an account; these three manage the set.

```bash
# List stored accounts; "*" marks the active one
unity auth list
unity auth ls              # alias
unity auth list --format json

# Make a stored account active (by email or id) — no browser round-trip
unity auth switch user@example.com

# Show the account a project uses for cloud commands
unity auth default

# Pin this project to an account, regardless of which one is active
unity auth default user@example.com

# Target a project other than the current directory
unity auth default user@example.com --project ./MyGame

# Remove the pin; the project follows the active account again
unity auth default --clear
```

Three behaviors worth knowing before scripting these:

- **A project pin beats the active account.** If a project has a default set, commands run inside it keep using that account even after `unity auth switch` — the switch reports this rather than failing silently. Use `auth default --clear` to hand the project back to the active account.
- **Service-account credentials outrank both.** When `UNITY_SERVICE_ACCOUNT_ID` / `UNITY_SERVICE_ACCOUNT_SECRET` are set (or a service account is signed in), they take precedence over every stored account and `auth switch` says so instead of appearing to work. Unset them, or `unity auth logout`, before switching.
- **`auth switch` is ambiguity-aware.** Given a string matching several stored accounts it fails rather than guessing, and under `--format json` carries the candidates in `data.candidates` so a script can disambiguate. Pass the full email or the account id.

`unity auth default` resolves the project from the current directory unless `--project` is given, and errors if that path isn't a Unity project. Passing both an account and `--clear` is rejected.

#### Consumers — see who's used your sign-in, and mark one revoked

Some products use your Unity sign-in without holding your refresh token themselves, brokering through the CLI's own auth broker instead. `unity auth consumers` lists every application in the broker's audit trail on this machine, plus any in the local revocation list, and `unity auth revoke` records one in that list. Read the Granted and Denied columns before concluding an application used your sign-in: one with only denials never received a token, and a revoked application with no audit history appears with no activity at all.

```bash
# Application, scopes, grants/denials, and when it was last used
unity auth consumers
unity auth consumers --format json

# Add an application to the local revocation list (every scope it asked for)
unity auth revoke "Some Tool"

# Remove it from the list
unity auth revoke "Some Tool" --restore
```

Both are local-only — reading the broker's audit trail and a local revocation list — so neither needs sign-in or the network. On a machine that has never run the broker, `auth consumers` reports an empty list rather than an error. `auth revoke` without `--restore` revokes; with it, it un-revokes; either way it reports the actual resulting state (e.g. "already revoked") rather than assuming the write changed anything. **The list is not enforced yet:** the auth broker does not consult it when it hands out tokens, so a revoked application can still use your sign-in. Neither `auth revoke` nor `unity auth logout` currently stops an application that already has broker access; `logout` keeps the account's saved sign-in while the Hub still uses it.

The stored token itself is sealed to the machine that wrote it (TPM-backed on Windows/Linux where available, Keychain on macOS, with a local-key-file fallback everywhere), so a copied token-store file is worthless on another machine. `unity doctor` reports which protection is in force.

**Separate sign-in from Hub.** As of `0.1.0-beta.8`, the CLI and the GUI Hub store their sign-in credentials **separately** — signing in to one no longer signs you out of (or overwrites the account of) the other, so each can stay signed in as a different account. (In earlier betas they shared a single keyring session.)

The two still share one account list. So an account you signed in to in the Hub shows up in `unity auth list` even though the CLI is not signed in to it, and `unity auth status` can report "not signed in" while `auth list` shows that account. To use it from the CLI, try `unity auth switch <account>` first: when that account's sign-in is readable on this machine, it becomes the CLI's active account with no browser round trip. When it has no usable sign-in, run `unity auth login` and sign in as that account in the browser; `auth switch` cannot sign an account in.

**Using the Hub's sign-in needs a yes.** When the CLI is signed out and the Hub is signed in, `unity auth` and `unity auth login` on a terminal ask "Use <email>, signed in to Unity Hub? (y/N)". The default is no. Yes makes that account the CLI's active account, exactly as `unity auth switch` does, and changes nothing in the Hub; no carries on as before (the browser sign-in, or the `auth` help). The CLI never takes the Hub's sign-in on its own. Runs that can't ask (`--json`, `--non-interactive`, no terminal) never prompt and never switch: `unity auth status --json` reports `adoptable: true`, the account and `adoptCommand`, and `unity auth login` prints the `unity auth switch <account>` command to run (as an `adoptable=` progress frame under `--json`). An agent should show the user that command rather than run it unasked. Once switched, signing out of the Hub doesn't sign the CLI out. If you only want the CLI in order to activate a perpetual or legacy serial license, you do not need to sign in at all: see [Headless / fleet license activation](#headless--fleet-license-activation).

**Service-account credentials via env vars** (`UNITY_SERVICE_ACCOUNT_ID` + `UNITY_SERVICE_ACCOUNT_SECRET`) mint bearer tokens automatically for the duration of the process — no browser round-trip, no keyring write. If only one of the two is set, the CLI prints a warning on stderr instead of silently falling back to the keyring/OAuth identity.

The interactive `unity auth login` flow prints the sign-in URL to the terminal **before** attempting to launch the browser, which unblocks remote/headless sessions (SSH, containers, dev VMs) where `xdg-open` / `open` has no graphical session to attach to. With `--format json`, an `auth_url=…` progress frame is written to stderr so machine consumers can capture the URL without parsing human text, and stdout stays the result envelope alone.

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

# Return the active licenses — assigned/subscription AND serial-activated (prompts to confirm; --yes skips)
unity license return
unity license return --yes

# Floating (network) license server
unity license server list      # the configured floating license server(s)
unity license server status    # reachability + available seats
```

`list` columns: product, license type (`Floating` / `Assigned` / `ULF`), organization, and expiry. `status` prints a one-glance summary — the active license(s) and whether you're signed in — and exits non-zero (`4`) when no license is active, so it works as a scriptable health check. The first licensing command downloads the Unity licensing client on demand; as of `0.1.0-beta.8`, if the client is unavailable `list` reports a clear error and exits non-zero (matching `status`), rather than printing an empty list.

`unity license status` also reports `floatingServer` (the licensing server this machine is configured against) and `machineId` (the identity the licensing client reports to it) — the two values to match against a lease record when a floating seat looks stuck. Both are always present in `--format json` / `ndjson`; human and `tsv` output shows them only when a floating server is configured.

`activate` takes a single mode flag (combining them is a usage error). The default (no flag) and `--personal` activate the signed-in user's entitlements — sign in first with `unity auth login`. `--personal` also requires `--accept-eula` to acknowledge the Unity Personal license terms. `--serial`, `--floating`, `--file` and `--generate-request` need no sign-in, and `--file` and `--generate-request` also work offline. `--floating` requires a configured floating license server (exit `4` if none is set). `--generate-request` writes a `.alf` request for air-gapped activation instead of activating. `return` returns the active licenses, prompting for confirmation first — pass `--yes` to skip (required in non-interactive shells and with `--json`). All honor `--json` / `--format` and exit non-zero on failure (`2` bad usage, `3` sign-in required, `4` floating not configured, `6` licensing-client error).

**Service accounts.** The `license` commands recognize service-account sessions (`UNITY_SERVICE_ACCOUNT_ID` / `UNITY_SERVICE_ACCOUNT_SECRET`, or `unity auth login --client-id`): `unity license status` reports `Signed in: yes (service account)` and includes the auth mode in JSON. Unity's licensing backend does **not** accept service-account tokens for license activation, so with a service-account session the default entitlement mode and `--personal` fail up front — before contacting the licensing client — with guidance toward the unattended options (`--floating`, `--file`, `--generate-request`, or a perpetual `--serial`). `unity license return` lists and returns serial-activated licenses too (not just assigned/subscription seats) — important for CI machines that activate per run — and returns each license individually, so when only some can be freed it reports what succeeded (in text and in the JSON `returned` / `failed` fields) instead of an all-or-nothing failure.

#### Headless / fleet license activation

For build agents, lab machines, and any machine with no browser. There is **no username/password option**: the CLI only signs a user in through the browser, and a service account cannot activate a license, because Unity's licensing backend does not accept service-account tokens for activation. None of the paths below needs a signed-in account.

```bash
# Perpetual or legacy Pro/Plus serial: no sign-in needed
unity license activate --serial <serial> --non-interactive --format json

# Air-gapped machine: write a request, exchange it elsewhere, then import the license file
unity license activate --generate-request ./unity.alf      # on the offline machine
# ...exchange unity.alf for a .ulf license file on a machine with internet access
#    (Unity's manual activation page), then copy the .ulf back...
unity license activate --file ./Unity_lic.ulf              # on the offline machine

# Floating (network) licenses: lease a seat from the configured server
unity license server status                                # check the server and its free seats first
unity license activate --floating

# Decommissioning a machine: give the seat back so the serial can be activated elsewhere
unity license return --yes --non-interactive
```

- **`--serial`** activates a serial-number license without signing in, on every platform. A serial tied to a subscription can still be refused with "This license requires a signed-in Unity account"; sign in on that machine, or use one of the other paths.
- **`--generate-request` then `--file`** is the offline path. The request file identifies this machine, so import the resulting license file on the same machine that generated the request. Both talk to the Unity Licensing Client on the machine, and the CLI downloads that client (about 110 MB) the first time a `license` command needs it. Stage it while the machine, or the image it is built from, still has network access: run `unity license status` once. With no client installed, that fetches one into the CLI's data folder. A `Unity.Licensing.Client` already on `PATH` is used instead when no downloaded copy exists, with no version check, and an older one fails the handshake, so update or remove an older `PATH` client before staging. The downloaded client lands in the data folder of the user who ran the command (under `HOME`, or `XDG_CONFIG_HOME` when set), so run the staging step as the same user, with the same `HOME`, that will run the CLI later; a client staged as root in an image build is not found by a container that runs as another user. Otherwise, put a current `Unity.Licensing.Client` on a shared `PATH`.

  On an image with no license yet, `unity license status` exits `4` (no license active), which is expected at this point. A fail-fast shell or a Dockerfile `RUN` stops on it, so accept exactly `4` and keep every other failure, such as `6` when the client could not be fetched:

  ```bash
  unity license status || [ $? -eq 4 ]
  ```

  ```powershell
  unity license status; if ($LASTEXITCODE -notin 0, 4) { exit $LASTEXITCODE }
  ```
- **`--floating`** needs a floating license server configured on the machine first (exit `4` otherwise). `unity license server list` shows which one is configured.
- **`license return`** returns serial-activated licenses as well as assigned ones. Run it before you reimage or retire a machine, or that activation keeps counting against the serial's limit. It needs `--yes` when nothing can answer a prompt. A license imported offline with `--file` cannot be returned this way: `license return` reports it as activated manually and leaves it in place.

`unity license server list` shows the configured floating license server (from the `licensingServiceBaseUrl` machine setting; a pure settings read, no client download). `unity license server status` contacts that server and reports reachability plus available seats — exit `4` when no server is configured, `6` when configured but unreachable.

---

### Cloud — Unity Cloud organizations and projects

Requires being signed in (`unity auth login`).

```bash
# Show cloud sign-in state and active organization
unity cloud status --format json

# Organizations
unity cloud org list --format json
unity cloud org current                       # print the active default org (named when signed in)
unity cloud org set-default <id-or-name>      # set active default org
unity cloud org clear-default                 # revert to "All Organizations"

# Create an organization (company profile: individual or company, plus country)
unity cloud org create "<name>" --first-name Ada --last-name Lovelace --country GB
unity cloud org create "<name>" --company-name "Nebula Games Inc." --country US --region CA --set-default

# Projects in the active organization
unity cloud project list --format json               # * marks the active default project

# To link a local folder to a cloud project, use `unity projects link cloud <path>` (see projects-templates.md);
# `unity cloud project --help` and `unity projects link cloud --help` point at each other

# Default project, stored per organization
unity cloud project current                          # print the saved default project, and the project linked to this folder
unity cloud project set-default <id-or-name>         # project UUID, Genesis id, or exact name
unity cloud project clear-default                     # drop this organization's default

# Override the active organization for a single call
unity cloud project list --cloud-org <id-or-name>   # also via UNITY_CLOUD_ORG env var
```

**Creating an organization takes a name and a company profile**, the same fields the Unity
Dashboard's create dialog asks for. `--type individual` needs `--first-name` and `--last-name`;
`--type company` needs `--company-name`. `--type` can be left out when the name flags already say
which it is, and passing both kinds is refused. `--country` is a two-letter ISO 3166-1 alpha-2 code (`GB`,
not `GBR`, and not `UK`, which is not an ISO code). `--region` is the ISO 3166-2 subdivision (`CA`, or `US-CA`) and is
required for the US, Canada, and Mexico and refused for every other country. The organization name
is trimmed and capped at 40 characters; person and company names follow the Dashboard's own
character rules. Everything passed is validated before the sign-in check and before any request,
so a typo costs no network call. Omitted fields are prompted for on a terminal; under
`--non-interactive`, or when output is redirected, the command fails naming every missing option.
Whether a country or subdivision code actually exists is the server's call, and its field-level
reason is reported as-is. `--set-default` makes the new organization active, exactly as
`org set-default` would; if the setting cannot be written the organization is still reported as
created, with a warning that the default did not take effect. A name already in use is reported as
such rather than as an HTTP status. Machine output is a create-specific shape carrying `id` and
`name` only, deliberately not `org list`'s row, since the create response has no `role` and the
default marker describes a list rather than a single new organization.

**The default project is per organization.** `set-default` stores the project's UUID against the
active organization's Genesis id, so switching your active organization switches which default
applies, and `clear-default` only drops the active organization's. `cloud project current` and
`clear-default` read and write the settings file directly, so they need no network and no session
when the organization comes from your stored default; passing `--cloud-org <name>` needs a lookup,
so that path requires a session like the rest. `cloud project current` labels what it prints as the
saved default, and inside a Unity project folder linked to a cloud project it also prints that linked
project. Setting a default never changes which project a folder is linked to (`projects link cloud`
does that). Under `--format json`, `activeCloudProjectId` still holds the default, with
`defaultCloudProjectId` and `linkedCloudProjectId` beside it (`null` outside a linked project folder).

**What consumes it.** Commands that need a Unity Cloud project but were not given one fall back to
the stored default. The order is the explicit flag (`--cloud-project`), then `UNITY_CLOUD_PROJECT`,
then the cloud link in the project directory's `ProjectSettings/ProjectSettings.asset`, then the
stored default, so inside a cloud-linked project the link still wins. `unity collaboration` uses
this chain. `unity projects link cloud` applies the default too, with its own order (`--cloud-project`,
then `UNITY_CLOUD_PROJECT`, then the default, see projects-templates.md). `unity projects create` does
not read the default: it creates a new cloud project unless `--cloud-project` names one or `--no-cloud`
(or `UNITY_NO_CLOUD`) declines. `cloud project current` prints the saved default, not necessarily the
folder's project: under `--format json`, `linkedCloudProjectId` is the folder's link.

**A project in another organization.** `set-default` looks in the active organization first. When the project is in
another of your organizations, the error names that organization (`UNKNOWN_CLOUD_PROJECT`, exit `2`, with the
organization under `data.organization` in `--format json`). Run `unity cloud org set-default <genesis id>`, then
`set-default` again. When the organization comes from `--cloud-org` or `UNITY_CLOUD_ORG`, the other organizations are
not checked, because that value overrides the saved default.

**Exit codes.** The `cloud` and `auth` commands map an authentication failure (expired or missing session, rejected sign-in) to `3`, and any other operational failure (network, server error) to `6` — so scripts can distinguish "sign in again" from a genuine command failure. `unity auth status` / `logout` follow the same convention. A status command that answers no is a failure in `--format json` too: signed out, `unity auth status` and `unity cloud status` exit `3` with `success: false` and `errors[0].code` `NOT_SIGNED_IN` (or `SESSION_EXPIRED`); `unity license status` with no license exits `4` with `LICENSE_NOT_ACTIVE`; `unity license server status` exits `4` with `LICENSE_SERVER_NOT_CONFIGURED` or `6` with `LICENSE_SERVER_UNREACHABLE`. Each keeps its report in `data` (`loggedIn: false`, `active: false`, `reachable: false`), so read `success` or the exit code, they always agree.

---

