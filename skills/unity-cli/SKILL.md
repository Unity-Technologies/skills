---
name: unity-cli
description: Use when interacting with Unity CLI from the terminal, or to control a running/connected Unity Editor from the command line — create or modify GameObjects, edit scenes and assets, inspect the hierarchy, and run C# in a live Editor instead of hand-editing scene or asset files. Also install, upgrade or uninstall editors, create, list or open projects, manage modules, manage licenses, check auth status, read logs, browse Unity releases, build/test projects, configure the Unity MCP server for AI agents, or run any other Unity CLI operation. For a guided idea-to-running-project flow for a brand-new game, use the new-unity-project skill instead.
allowed-tools:
  - Bash
---

# Unity CLI

## Drive a running Unity Editor (if one is open)

**If a Unity Editor is open on this machine, this CLI can control it live** — create and modify GameObjects, edit scenes and assets, inspect the hierarchy, and run arbitrary C# — through the project's **Pipeline** package (`com.unity.pipeline`). This runs entirely on your local machine, in your own user account, against your own open Editor: it is not remote access and grants no privilege you don't already have at your own terminal. When an Editor is available, drive it instead of hand-editing scene or asset files.

```bash
unity status                    # confirm a connected Editor (look for state "ready")
unity command                   # list the commands the Editor exposes
unity command editor_play       # run one — e.g. enter Play mode
# Run arbitrary C# — e.g. add a GameObject named "Joe" — when the Editor exposes eval:
unity command eval 'new UnityEngine.GameObject("Joe");'
```

Requires the project's `com.unity.pipeline` package (Unity 6.0+) — add it once with `unity pipeline install`. Full details — launching a headless Editor to drive, `unity list` tool discovery, and authoring custom `[CliCommand]` tools — are in [integration-advanced.md](references/integration-advanced.md).

> **Can't connect / commands time out? Check for Safe Mode first.** When a project has C# compile errors, the Editor boots into **Safe Mode**, where the Pipeline package doesn't load — so `unity command`, `unity status`, and `unity list` can't connect at all. Don't fall back to blind file-editing: run `unity pipeline list` to confirm, then fix the compile errors and restart Unity. Full recovery loop in [integration-advanced.md → Recovering from Safe Mode](references/integration-advanced.md#recovering-from-safe-mode-connection-fails-because-of-compile-errors).

## Step 1: Install the CLI (if not already installed)

First check if the CLI is available:

```bash
which unity && unity --version
```

If not found, install it:

**macOS / Linux**
```bash
curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash
```

**Windows (PowerShell)**
```powershell
$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
```

After installing, open a new shell so `unity` is on PATH, then verify:

```bash
unity --version
```

If the install script fails or the binary is still not found, tell the user and stop.

## Step 2: Verify it works

```bash
unity --version
```

If this fails with a permissions error or crash, the CLI installation may be broken. Suggest re-running the install script.

---

## Global flags

These work on every command:

| Flag | Description |
|---|---|
| `--format <fmt>` | Output format: `human` (default), `json`, `tsv`, `ndjson`, `github`. Also via `UNITY_FORMAT` env var. |
| `--json` | Global shorthand for `--format json`, accepted on every command (e.g. `unity status --json`, `unity doctor --json`). `--format` takes precedence when both are supplied. |
| `--no-banner` | Suppress the branded header — use in scripts |
| `--no-pager` | Disable the pager for long human output. Also via `UNITY_NO_PAGER` (presence-based — any value, including `0`, disables it). |
| `--non-interactive` | Disable all interactive prompts — use in CI |
| `--quiet` | Suppress non-essential output |
| `--verbose` | Print full error details (stack trace + cause chain) on failure. Also via `UNITY_VERBOSE`. |
| `--proxy <url>` | HTTP/HTTPS/SOCKS/PAC proxy URL for this invocation. Also via `UNITY_PROXY`. Takes precedence over standard `HTTPS_PROXY`/`HTTP_PROXY`/`ALL_PROXY` env vars and the persisted `proxy.json` setting. |
| `--proxy-disable` | Disable proxy for this invocation, ignoring all sources (env vars, persisted config, system settings). |
| `--log-proxy` | Log one redacted entry per outbound request (host-only URL, resolved proxy, auth source, status, duration) to `proxy-request.json` — for reproducing proxy issues for support. Also via `UNITY_LOG_PROXY=1` or the persisted `proxyRequestLogging` setting. |
| `--no-log-proxy` | Opt a single invocation out of proxy request logging when it's enabled globally. |

**Always use `--format json` when you need to parse output programmatically.**

**Long human output is paged, on the `git log` model.** The long listing surfaces — `unity command`, `unity releases`, `unity editors`, `unity changelog`, `unity logs` — route stdout through a pager. The default is `less -RFX`, which quits immediately when the content fits one screen, so short output shows no pager UI at all. Resolution order is `$UNITY_PAGER` → `$PAGER` → `less -RFX` → `more.com` on Windows; when none can be spawned, output falls back to a direct write.

**It never pages when output isn't a human reading a terminal**, so scripts need no special handling: paging is off for non-TTY stdout (pipes, redirects), for every machine format (`json`, `tsv`, `ndjson`), under `--quiet`, under `--no-pager` / `UNITY_NO_PAGER`, when `TERM=dumb`, and inside `unity shell`. Quitting the pager early (`q`) is silent and leaves the command's exit code untouched.

A branded Unity header (logo, wordmark, CLI version) renders on the landing surfaces — bare `unity`, `unity --help` / `-h`, `unity help`, and above the first-run consent prompt. It's shown only on a TTY, prints at most once, and degrades to compact, uncolored text on narrow terminals, without Unicode, or under `NO_COLOR`. Piped output is unaffected. Use `--no-banner` to suppress it in scripts. Bare `unity` prints usage and exits 0.

## Environment variables

All CLI env vars use the `UNITY_` prefix. A CLI flag always overrides the corresponding env var.

| Variable | Mirrors flag | Description |
|---|---|---|
| `UNITY_FORMAT` | `--format` | Output format (`human`, `json`, `tsv`, `ndjson`, `github`). `HUB_FORMAT` is a deprecated alias. |
| `UNITY_EDITOR_VERSION` | `--editor-version` | Editor version (e.g. `2023.3.0f1`, `latest`, `lts`). |
| `UNITY_ARCHITECTURE` | `--architecture` | Chip architecture (`x86_64`, `arm64`). |
| `UNITY_PROJECT_PATH` | path argument | Project path — used by `open`, and also honored by `status` and the cloud commands. |
| `UNITY_QUIET` | `--quiet` | Suppress non-essential output. |
| `UNITY_VERBOSE` | `--verbose` | Show full error details on failure. |
| `UNITY_NON_INTERACTIVE` | `--non-interactive` | Disable interactive prompts. |
| `UNITY_NO_BANNER` | `--no-banner` | Suppress the branded banner. |
| `UNITY_NO_PAGER` | `--no-pager` | Disable the pager for long human output. Presence-based: any value disables it, including `0`. |
| `UNITY_PAGER` | — | Pager command to use, taking precedence over `$PAGER` (e.g. `less -S`). Honors flags and quoting; falls back to `less -RFX`, then `more.com` on Windows. |
| `UNITY_RUN_TIMEOUT` | `--timeout` | Timeout for `unity run` in seconds. |
| `UNITY_TEST_TIMEOUT` | `--timeout` | Timeout for `unity test` in seconds. |
| `UNITY_CLOUD_ORG` | `--cloud-org` | Active Unity Cloud organization id or name for a single call. |
| `UNITY_SERVICE_ACCOUNT_ID` | — | Service account client ID for non-interactive (CI) auth. |
| `UNITY_SERVICE_ACCOUNT_SECRET` | — | Service account client secret for non-interactive (CI) auth. |
| `UNITY_PROXY` | `--proxy` | HTTP/HTTPS/SOCKS/PAC proxy URL. Takes precedence over `HTTPS_PROXY`/`HTTP_PROXY`/`ALL_PROXY` and the persisted `proxy.json` setting. |
| `UNITY_NO_UPDATE_CHECK` | — | Disable the background "update available" check (see `unity config update-check`). |
| `UNITY_NO_CONSENT_PROMPT` | — | Suppress the one-time first-run analytics consent prompt *without* recording a choice — for wrapper scripts on an interactive terminal that must never absorb the prompt. Analytics stay off until you run `unity analytics opt-in`. Unlike `UNITY_NON_INTERACTIVE`, it changes nothing else about command behavior. |
| `UNITY_NO_CRASH_REPORT` | — | Disable anonymous crash/error reporting (Sentry) entirely. |
| `UNITY_LOG_PROXY` | `--log-proxy` | Log one redacted entry per outbound request to `proxy-request.json`. Truthy values: `1`, `true`. |
| `UNITY_NO_ELEVATE` | `--no-elevate` | Windows: skip the elevated (UAC) install helper for `install` / `install-modules`, so the install service runs unelevated. The Editor's NSIS installer still asks for elevation on demand if Windows requires it for your account — an administrator token always does; a standard user never does. |
| `UNITY_INSTALL_RETRIES` | `--retries` | Number of times `install-modules` retries a module whose download/validation fails. `0` disables retries. |

**CI service account auth:** Set both `UNITY_SERVICE_ACCOUNT_ID` and `UNITY_SERVICE_ACCOUNT_SECRET` to skip the browser OAuth flow — this keeps the secret out of the process argument list and shell history. These map to the `--client-id` / `--secret-from-stdin` inputs of `unity auth login`, but reading the credentials from the environment isn't a full login: it doesn't run the interactive flow or persist credentials to the keyring.

## Getting help

If a command fails or you're unsure of the available options, append `-h` or `--help` to any command or subcommand:

```bash
unity --help
unity install --help
unity projects --help
unity projects create --help
```

This works at every level of the command hierarchy.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | Success |
| 1 | General error |
| 2 | Bad arguments |
| 3 | Authentication failure |
| 4 | Precondition not met (e.g. no license active, floating server not configured) |
| 6 | Command-specific failure |
| 8 | `unity test` only — the tests ran and one or more **failed**. Every other way a test run fails (compile error, unavailable license, editor crash, `--timeout`) keeps `6`, so CI can retry an infrastructure failure and never retry a failing test. |
| 130 | Interrupted — Ctrl+C / SIGINT (128 + 2) |
| 143 | Terminated by SIGTERM (128 + 15) — e.g. `kill` or a CI/runner timeout. Emitted by long-running commands that install a signal handler to clean up first (currently `unity build`, which scrubs the temporary Android keystore). |

The `cloud` and `auth` commands map an authentication failure (expired/missing session, rejected sign-in) to `3`, and any other operational failure (network, server error) to `6` — so scripts can reliably tell "sign in again" apart from a genuine command failure.

---

## Commands

The full per-command reference — syntax, flags, and examples — lives in grouped files under
[`references/`](references/). **Read the file for the command group you need**; all the global
flags, environment variables, and exit codes above apply throughout. Every command also supports
`-h` / `--help` (see [Getting help](#getting-help)).

| Commands | Reference file |
|---|---|
| `auth` (login / logout / status / list / switch / default), `license` (activate / return / server), `cloud` (org / project) | [auth-license-cloud.md](references/auth-license-cloud.md) |
| `editors` (list / running / add / default / path / install-path / info / upgrade / prune / verify / module), `install`, `uninstall`, `modules`, `install-modules` | [editors-install.md](references/editors-install.md) |
| `projects` (list / create / new / clone / open / link / require / upgrade / export / import / pin / size / clean / exec), `releases`, `templates` (list / info / create / pack / delete) | [projects-templates.md](references/projects-templates.md) |
| `config` (proxy / update-check), `hub install` | [config-hub.md](references/config-hub.md) |
| `run`, `test`, `build` | [build-run-test.md](references/build-run-test.md) |
| `logs`, `doctor`, `env`, `cache`, `analytics`, `changelog`, `language`, `completion`, `bug`, `upgrade`, `self-uninstall`, `diagnose proxy` | [diagnostics-maintenance.md](references/diagnostics-maintenance.md) |
| `mcp` (+ `configure`), `skill` (install / refresh), connected editors (`pipeline` / `command` / `status` / `list`), `shell` | [integration-advanced.md](references/integration-advanced.md) |
| `collaboration` (alias `collab`) — `annotations` / `attachments` / `thumbnail` / `reactions` / `read` / `subscribe` / `jira` | [collaboration.md](references/collaboration.md) |

## Common workflows

### Edit a scene, GameObject, or asset — `unity status` first

**Before editing any scene, GameObject, prefab, or asset, run `unity status` to detect a connected Editor.** If one is reachable, drive it with live commands instead of touching project files — the Editor applies changes to the *actual active scene* and keeps its in-memory state in sync.

```bash
unity status                       # is an Editor connected? (look for state "ready")
unity command                      # discover the scene/GameObject commands THIS Editor exposes
# then drive it with the commands it lists — for example, if your Editor exposes them:
unity command create_gameobject    # act on the live, active scene
unity command save_scene           # persist the active scene
```

Command names are defined by the Editor, so run `unity command` (or `unity list`) to see the exact set — don't assume a name.

> **Never hand-edit `.unity`, `.prefab`, or `.asset` YAML while a live Editor is reachable.** Raw-file edits are:
> - **error-prone** — fileIDs and GUIDs are assigned by hand and easy to get wrong;
> - **invisible** to the running Editor until a reimport, so the change silently fails to take effect; and
> - **prone to hitting the wrong file** — e.g. writing to `SampleScene.unity` while the Editor's active scene is actually `Demo2.unity`, producing valid-looking YAML that changes nothing the user sees.

Only fall back to editing files directly when `unity status` shows **no** reachable Editor — and say so explicitly ("no live Editor detected, editing the file directly").

**One exception worth ruling out first:** if an Editor *is* running for this project but `unity status` / `unity command` won't connect, it may be stuck in **Safe Mode** from a compile error rather than genuinely absent. Run `unity pipeline list` — if it reports Safe Mode, editing the C# source to fix the compile errors (and then restarting Unity) *is* the correct move, not a fallback. See [integration-advanced.md → Recovering from Safe Mode](references/integration-advanced.md#recovering-from-safe-mode-connection-fails-because-of-compile-errors).

### Bootstrap a new project from scratch

> For a **guided** end-to-end experience — concept questions, installing the Editor in the
> background while you plan, package selection, and monetization handoff — use the
> **`new-unity-project`** skill. This section is the raw CLI recipe that skill builds on; use it
> directly when you just want the commands.

Take an idea to a running, version-controlled project using only the CLI. Decide the **target
platforms first** — they determine which Editor modules you install in step 2. You can add
modules later (`unity install-modules`), but a project can't build for a platform until that
platform's module is installed, so it's simplest to decide up front.

```bash
# 1. Confirm the CLI works and you're signed in and licensed (see references/auth-license-cloud.md).
unity --version
unity auth status --format json      # if signed out:      unity auth login
unity license status --format json   # if none active:      unity license activate

# 2. Pick and install an Editor with the modules your target platforms need.
#    Default to the latest LTS (most stable, ~2 years of patches). Reach for a Tech-stream
#    release (--stream tech) only for a feature not yet in LTS; treat --stream beta/alpha as
#    evaluation-only, never for a project you intend to ship. A deadline argues for LTS.
#    (lts / latest aliases work wherever a version is accepted.)
unity releases --stream lts --limit 5 --format json
unity install lts --module android --module ios --yes --accept-eula   # add --module webgl, etc.
unity editors --installed --format json                               # confirm it landed

# 3. List the real template ids this Editor offers — don't guess them.
unity templates list --editor lts --format json
#    Common ids: com.unity.template.3d, com.unity.template.2d, and a URP template (id varies by version).

# 4. Create the project. The first positional arg is the NAME; --path sets the parent directory.
#    All options supplied, so it won't prompt; add --non-interactive in CI.
unity projects create "MyGame" --path ~/UnityProjects \
  --editor-version lts --template com.unity.template.3d
```

**Source control — let the user choose.** The CLI publishes the new project to a fresh remote in
one step for any provider. **Always pass tokens on stdin** (`--git-token-stdin`) so secrets never
land in shell history or the process list. Pick based on the project — don't default to one:

- **Git — GitHub / GitLab** (`--vcs github` / `--vcs gitlab`). Ubiquitous. For asset-heavy games
  add **Git LFS** (`--git-lfs`) so large binaries don't bloat history.
- **Unity Version Control — UVCS** (`--vcs uvcs`). Unity's own VCS, built for large binary game
  assets: it handles them natively (**no LFS needed**) and supports file locking — often the
  better fit for art-heavy projects or larger teams. Auth uses your Unity sign-in; `--vcs-region`
  selects the region.

```bash
# Git (GitHub) — drop --git-lfs if the game isn't asset-heavy. Add --no-initial-commit if you
# want to add packages/assets BEFORE the first commit (see the new-unity-project flow).
unity projects create "MyGame" --path ~/UnityProjects \
  --editor-version lts --template com.unity.template.3d \
  --vcs github --git-namespace my-org --git-repo my-game \
  --git-visibility private --git-default-branch main --git-token-stdin --git-lfs

# Unity Version Control (UVCS) — handles binaries natively, so no LFS:
unity projects create "MyGame" --path ~/UnityProjects \
  --editor-version lts --template com.unity.template.3d \
  --vcs uvcs --git-namespace my-org --git-repo my-game --vcs-region <region>
```

Feed the token to `--git-token-stdin` from a secret store, never a literal — e.g.
`… --git-token-stdin <<<"$GIT_TOKEN"` where `$GIT_TOKEN` comes from your CI/secret manager
(UVCS uses your Unity sign-in, so no token is needed).

**Git tokens belong to the user's credential manager, not the CLI.** When no token flag or env var
is given, the CLI asks `git credential fill` and uses whatever the configured helper returns; it
stores nothing it is passed or told. Don't suggest the CLI can save a Git token, and don't reach for
a token flag when the user already has a working credential helper. If they want a different token
per organization, that is `git config --global credential.useHttpPath true` plus a multi-account
helper such as [Git Credential Manager](https://github.com/git-ecosystem/git-credential-manager).
The CLI passes the full repo URL so the helper can discriminate, but it never installs or
reconfigures a helper. `UNITY_GITHUB_TOKEN` / `UNITY_GITLAB_TOKEN` are one token per provider, so a
CI job spanning several orgs should pass `--git-token-stdin` per invocation instead. See
[references/projects-templates.md](references/projects-templates.md) for the full
source-control flag set. For a purely local Git repository instead, initialize git with a
Unity-appropriate ignore so the multi-GB `Library/` and other generated folders are never committed:

```bash
cd ~/UnityProjects/MyGame
git init -b main
# Download (do not pipe to a shell) a maintained Unity .gitignore:
curl -fsSL https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -o .gitignore

# Asset-heavy game? Keep large binaries out of git history with Git LFS:
git lfs install
git lfs track "*.psd" "*.fbx" "*.wav" "*.mp3" "*.png"   # adjust to your asset types
git add .gitattributes

git add -A
git status                             # sanity-check: Library/ Temp/ obj/ Build/ must NOT be staged
git commit -m "Initial Unity project: MyGame"
git ls-files | grep -c '^Library/'     # must print 0
```

**What the CLI does and doesn't cover.** The CLI handles editor, project, and source control.
It does **not** manage UPM (Unity Package Manager) packages — to add packages beyond the
template headlessly, use the **`unity-package-management`** skill (C# PackageManager Client
API). For monetization/backend, hand off to the dedicated skills: `implement-in-app-purchases`
(IAP), `levelplay-unity-integration` (ads), or `build-live-game` (accounts, cloud save,
economy, remote config, leaderboards). Open the project to start working:
`unity open ~/UnityProjects/MyGame`.

### Find and install a missing editor

```bash
# 1. Check what's installed
unity editors --installed --format json

# 2. Browse available LTS versions
unity releases --lts --limit 5 --format json

# 3. Install
unity install 6000.0.47f1 --yes --accept-eula
```

### Open a project with the correct editor

```bash
# 1. Check the project's required editor version
unity projects info /path/to/MyProject --format json
# Look at "editorVersion" in the result

# 2. Confirm that editor is installed
unity editors --installed --format json

# 3. Open (warns if the editor version is missing)
unity open /path/to/MyProject
```

### CI: activate a license, then build

```bash
# 1. Sign in non-interactively with a service account
unity auth login --client-id "$UNITY_SERVICE_ACCOUNT_ID" --secret-from-stdin <<<"$UNITY_SERVICE_ACCOUNT_SECRET"

# 2. Activate the entitlement license (or use --serial / --floating)
unity license activate

# 3. Build
unity build /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --target StandaloneLinux64 \
  --execute-method Builder.PerformBuild \
  --allow-install
echo "Exit code: $?"

# 4. Return the seat when done (floating/assigned)
unity license return --yes
```

### CI: headless build

Prefer the dedicated `unity build` command (handles batch mode, logging, and CI flags):

```bash
unity build /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --target StandaloneLinux64 \
  --execute-method Builder.PerformBuild \
  --allow-install
echo "Exit code: $?"
```

Or use `unity run` (batch mode is automatic — never pass `-batchmode`/`-quit`):

```bash
unity run /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --allow-install \
  -- -executeMethod Builder.PerformBuild -logFile build.log
echo "Exit code: $?"
```

### CI: run tests and publish results

```bash
unity test /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --mode EditMode \
  --report-format junit \
  --output ./test-results.xml \
  --allow-install \
  --timeout 600
case $? in
  0) echo "All tests passed" ;;
  8) echo "Tests failed — report to developers, do not retry" ;;
  *) echo "Run did not complete — infrastructure failure, safe to retry" ;;
esac
```

Exit `8` means the run finished and reported failing tests; any other non-zero code means it never produced a verdict. Under `--format json` the same split is `errors[0].code`: `TESTS_FAILED` versus `TEST_RUN_ERROR` / `TEST_TIMED_OUT`.

`--report-format junit` makes `--output` a JUnit-schema report, which GitHub Actions and GitLab ingest as native test results with no converter step. It is written even when tests fail. Drop the flag for the NUnit3 default, or use `--report-format nunit,junit` to get both from one run. Add `--coverage` to collect coverage via the Unity Code Coverage package — it warns and carries on if the project doesn't have the package. See [build-run-test.md](references/build-run-test.md).

### Debug the CLI

```bash
# Check auth + installed editors + recent errors in one command
unity doctor --format json

# Follow live logs during an install
unity logs --follow --level info
```

---

## Notes

- `--non-interactive` and `--yes` together suppress all prompts — use both in CI.
- `--format json` always produces machine-readable output; prefer it over parsing human text. Error envelopes are pretty-printed with the same 2-space indent as success envelopes.
- **Read failures from stdout, not stderr.** A failed command still writes a complete document to stdout: under `--format json` an envelope with `success: false` and a populated `errors` array (`errors[0].code` is the stable token to branch on); under `--format ndjson` the usual terminal `{"type":"result","success":false,…}` frame. **Branch on `success`, never on `data`** — `data` is usually `null` on a failure, but not always: a partial `unity editors add` failure carries a row per path, and an ambiguous `unity auth switch` carries `data.candidates` for you to disambiguate with. Check `success` and the exit code — never treat empty stdout as a failure signal, and do not parse stderr, which carries only human diagnostics in these formats. A handful of commands have not migrated yet and still print `{"error": "…"}` to stderr with empty stdout; if stdout is empty on a non-zero exit, that is a known bug in that command rather than a shape you should code against.
- `unity <version> [path]` is a shorthand for `unity open [path] --editor-version <version>`. Works with `lts`, `latest`, or a full version string like `6000.0.47f1`.
- The CLI supports kubectl-style plugins: any `unity-<name>` binary on PATH is callable as `unity <name>`.
- Terminal output is hardened against control-character / escape-sequence injection from server-provided values (project titles, editor versions, module names) — C0 controls and non-SGR escape sequences are stripped from table/list/tree output, and now also from Commander usage errors, the `unity bug` log-archive warning, and `unity projects add`/`remove` machine (tsv) output, while SGR color/style codes are preserved.
- The CLI reports anonymous crashes and errors via Sentry to help fix bugs (no IP address or hostname; home-directory paths and token-like values scrubbed before send), aligned with the Unity Hub. Opting in to analytics additionally attaches an anonymized machine id; opted-out users stay fully anonymous. Set `UNITY_NO_CRASH_REPORT` to disable reporting entirely.
- The CLI is currently in **beta** (latest: `1.0.0-beta.6`). It moved to 1.0 versioning at `1.0.0-beta.1`; it's still a beta, so keep `UNITY_CLI_CHANNEL=beta` in the install command until GA ships, after which that part can be dropped.
- As of `0.1.0-beta.8` the CLI checks in the background for a newer version and prints an unobtrusive "update available" notice (interactive sessions only; never delays a command). Turn it off with `unity config update-check off` or the `UNITY_NO_UPDATE_CHECK` env var.
- Outbound HTTP from every CLI command honors the resolved proxy (see `unity config proxy`). An invalid `--proxy` value (malformed URL or unsupported scheme) fails with a usage error (exit 2) instead of being silently ignored. Inspect what the CLI actually resolved with `unity env --format json` or `unity doctor --format json` — both surface the active proxy URL, its source, and auth source.
