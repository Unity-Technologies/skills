# UGS CLI Reference

## Table of Contents

- [Overview](#overview)
- [Install and Update](#install-and-update)
- [If the Unity CLI Itself Is Missing](#if-the-unity-cli-itself-is-missing)
- [Auth and Project Setup](#auth-and-project-setup)
- [Running Commands](#running-commands)
- [Headless Deploy by File Type](#headless-deploy-by-file-type)
- [Credentials and Environment](#credentials-and-environment)

## Overview

The [Unity Gaming Services CLI](https://github.com/Unity-Technologies/unity-gaming-services-cli/) manages UGS resources outside the Unity Editor: deploy and fetch cloud resource files, manage triggers and schedules, and reach admin functionality across all UGS services. Every command in this reference is invoked as `unity ugs <command>` — there is no standalone `ugs` form.

## Install and Update

**It ships as a Unity CLI plugin**, so install and update it through the Unity CLI rather than downloading it separately. Before running any `unity ugs` command, confirm the plugin is installed and install it if it's missing:

```bash
unity plugin list            # every component, its state, and where it resolved from — check ugs is "resolved"
unity plugin install ugs     # install the managed copy, if missing
unity plugin upgrade ugs     # re-install when the pinned build changes
```

If `unity plugin list` shows `ugs` resolved from `path` rather than `managed`, a UGS CLI already on PATH is used instead of the plugin, and may be an older version without the features above (e.g. Hub sign-in needs 2.0.0+). Check `unity ugs --version`; remove the other install or upgrade it.

For the plugin system itself (what a managed tool is, `unity plugin remove` / `changelog`), see the **unity-cli** skill.

## If the Unity CLI Itself Is Missing

If `unity` is not on PATH at all, `unity plugin install ugs` can't run. Don't stop at a raw "command not found" — tell the user to install the Unity CLI first, following the **unity-cli** skill's own install instructions (its own `--help`/docs are the source of truth for the current install command).

## Auth and Project Setup

Before any `unity ugs` command that talks to the backend, the CLI needs credentials and a target project/environment:

```bash
unity ugs config set project-id <project-id>
unity ugs config set environment-name <environment-name>
```

**Developer sign-in — Unity Hub (requires UGS CLI 2.0.0+):** with Unity Hub running and signed in, either run `unity ugs login` with its Hub option (see `unity ugs login --help`) to persist Hub auth, or set `UGS_CLI_USE_HUB_AUTH=true` (also accepts `1`/`yes`/`y`/`t`) to use it for one invocation without logging in — it takes priority over any saved `unity ugs login` and the service-account env vars below for that call. Hub sign-in reads the currently-running Hub's session over its local IPC socket (`/tmp/Unity-hubIPCService.sock` on macOS/Linux, a named pipe on Windows), not a file or keychain, so Hub has to actually be running and signed in on the machine the CLI runs on. The CLI prints bare `ugs` in its messages; run the equivalent as `unity ugs logout` then `unity ugs login`. Hub sign-in failures:

| Error string | Cause | Fix |
| :-- | :-- | :-- |
| ``Unity Hub is not running. Start Hub or run `ugs logout` then `ugs login` to switch to service account authentication.`` | Hub is not running on this machine | Start Hub and sign in, or switch to a service account |
| ``Unity Hub user is not signed in. Sign in through Unity Hub or run `ugs logout` then `ugs login` to switch to service account authentication.`` | Hub is running but nobody is signed in | Sign in through Hub, or switch to a service account |

Run `unity ugs --version` first — v1.9.0 predates Hub auth; `unity plugin upgrade ugs` if it reports lower than 2.0.0. Note `unity ugs status` does **not** reflect `UGS_CLI_USE_HUB_AUTH` — it only detects a persisted Hub login, so it can still report "no credentials" while a command using the env var succeeds.

**Service account — CI / headless:**

```bash
unity ugs login --service-key-id <id> --secret-key-stdin   # prompts for the secret key on stdin
# or set UGS_CLI_SERVICE_KEY_ID / UGS_CLI_SERVICE_SECRET_KEY instead of logging in
```

`unity ugs status` reports whether a service account is currently stored. Run `unity ugs login --help` / `unity ugs config --help` to confirm flags against the installed CLI version before relying on this from memory.

## Running Commands

Invoke it as `unity ugs <command>`:

```bash
unity ugs login                                      # or UGS_CLI_SERVICE_KEY_ID / UGS_CLI_SERVICE_SECRET_KEY
unity ugs deploy . --dry-run
unity ugs fetch . -s remote-config -e production
```

## Headless Deploy by File Type

`unity ugs deploy` picks files up by extension and silently skips the rest. A skipped file does not show up in the result at all, so a dry run that lists nothing means nothing was recognised.

| Resource | Headless path |
|---|---|
| `.ccmu` (native C# module) | **Not the UGS CLI** — `unity ugs deploy` skips `.ccmu`. The Editor compiles the module and deploys it: the Deployment Window, or headlessly the `Deploy.cs` template in `references/deployment.md`. |
| `.ccmr` (legacy module) | `unity ugs deploy` skips the `.ccmr` itself — pass the `.sln` it points at: `unity ugs deploy Module~/Module.sln`. The CLI runs `dotnet publish` (needs a .NET SDK on PATH). The Deployment Window deploys the `.ccmr` directly. |
| `.ccm` (packaged module) | `unity ugs deploy <file>.ccm` |
| `.rc`, `.ac`, `.lb`, `.js`, `.tr`, `.sched` | `unity ugs deploy <paths>` and `unity ugs fetch <path>`, or the Deployment Window. `--dry-run` first; `--reconcile` deletes remote content not in the deploy. `unity ugs tr new-file <name>` / `unity ugs sched new-file <name>` generate a default trigger / schedule config. |
| `.ugo` (Game Overrides) | **Not the UGS CLI** — `unity ugs deploy` skips `.ugo`. Deploy through the Deployment Window, or headlessly the `Deploy.cs` template in `references/deployment.md`. |

## Credentials and Environment

`unity ugs deploy`/`fetch` authenticate using whichever CLI credential is active — a service account (`unity ugs login` or its env vars) or, from 2.0.0+, Unity Hub sign-in (see [Auth and Project Setup](#auth-and-project-setup)) — never as the signed-in Editor user, and take the environment as an argument (`-e <name>`). An Editor deploy uses the Editor's own credentials and the project's *active* environment — except that while the local Cloud Code server runs, a Cloud Code module deploy goes to that server instead. Stop the local server before deploying a module remotely.
