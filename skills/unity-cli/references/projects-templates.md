# Projects, releases & templates — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

---

### Projects — list, open, create, register, clone, link

```bash
# List registered projects
unity projects list --format json

# Register an existing project
unity projects add /path/to/MyProject

# Remove from registry (does not delete files)
unity projects remove /path/to/MyProject

# Show project details
unity projects info /path/to/MyProject --format json

# Open a project in the editor
unity open /path/to/MyProject

# Open with a specific editor version
unity open /path/to/MyProject --editor-version 6000.0.47f1

# Pass extra Unity arguments
unity open /path/to/MyProject --args "-logFile output.log"

# Pass a build target (forwarded to Unity as -buildTarget / -buildTargetGroup)
unity open /path/to/MyProject --build-target StandaloneOSX
unity open /path/to/MyProject --build-target-group Standalone

# Version shorthand (equivalent to open with --editor-version)
unity 6000.0.47f1 /path/to/MyProject
```

The project argument is matched against the Hub registry first (exact name or path opens immediately; a glob like `"My Game*"` prompts when multiple match); with no registry match it falls back to treating the argument as a filesystem path. Path matching is tolerant of casing, separator direction, and a trailing slash — resolved against real filesystem path identity — so a registered project is found even when the path is spelled differently, while two genuinely distinct case-variant folders on a case-sensitive volume stay distinct. `unity open` forwards `--args` to the Editor correctly on all platforms (including Windows).

**Signed-in Editor, no Hub required.** `unity open` starts a small background identity helper that answers the Editor's account lookup with the session `unity auth login` stored — your account, organization list (so Package Manager entitlements resolve), and the service addresses for your resolved `--cloudEnvironment` — so a Hub-less machine gets a signed-in Editor instead of an anonymous one. It steps aside whenever a real Hub is running or starting, exits on its own a few minutes after the Editor stops using it, and can be disabled with `UNITY_NO_EDITOR_IDENTITY_SERVER`. Signed out, the Editor just starts anonymous, as before.

**Reserved flags — do NOT pass these via `--args`.** `-projectPath` is managed by the command (Unity's parser is last-wins, so forwarding it would silently redirect the open to a different project), and `-useHub`/`-hubIPC` are deliberately never passed — they tell the Editor a Unity Hub manages its session, which the CLI is not. Passing any of them fails fast, before launch, with exit code 6:

```
Error: Forwarded argument '-useHub' conflicts with a reserved Unity flag managed by this command. Remove it from `--args`.
```

All three spellings Unity accepts are rejected (`-useHub`, `--useHub`, `-useHub=1`, case-insensitively). Everything else — `-logFile <path>`, `-nographics`, custom flags your project reads — is forwarded verbatim.

#### projects create

Create a project. On a TTY, prompts for any missing options (parent directory, editor version, template) and then asks whether to link the project to a Unity Cloud project — that last question defaults to **No**, so pressing Enter creates an unlinked project. In CI, pass `--non-interactive` or pipe stdin to suppress prompts and rely on stored defaults. The first positional argument is the project **name**; `--path` sets the parent directory:

```bash
unity projects create MyGame --editor-version 6000.0.47f1 --template com.unity.template.3d

# Place the project in a specific directory
unity projects create MyGame --path /path/to/projects --editor-version 6000.0.47f1

# --template also accepts a .tgz file path or a directory, not just a registered template id
unity projects create MyGame --template /path/to/template.tgz
```

**Cloud linking during creation:**

```bash
# Create and link a NEW Unity Cloud project as part of creation
unity projects create MyGame --cloud --cloud-org <id-or-name>

# Link an EXISTING cloud project instead
unity projects create MyGame --cloud-project <id-or-name>
```

Passing any of `--cloud`, `--cloud-project`, or `--cloud-org` answers the cloud question, so it is not asked again. The question is also skipped in every machine output mode (`--json`, `--format tsv|ndjson`, `--quiet`), under `--non-interactive` (or `UNITY_NON_INTERACTIVE`), when stdout is not a TTY, and when the current credentials cannot create a cloud project (signed out, or service-account auth) — those keep today's flag-only, default-off behaviour. Unlike the other three questions, this one can fire even when every option was supplied on the command line, so `--non-interactive` is what keeps a fully-specified scripted run from stopping on it. Be aware that the global currently gates **only** this question: the parent-directory, editor-version, and template questions still gate on terminal interactivity alone, so `--non-interactive` on a TTY does not make them fall back to stored defaults. For a fully unattended run on a terminal, pass `--path`, `--editor-version`, and `--template` as well — or use `projects new`, which never prompts at all.

Answering Yes never costs you the project: if the link cannot be set up (expired session, no resolvable organization), the project is still created unlinked and the reason is reported as a warning, exit 0. `--cloud` behaves differently and still fails outright — an explicit flag is a contract, not a suggestion.

When a project is created without a cloud link, human output ends with a line pointing at `unity projects link cloud`. It is human-format only: `json`, `ndjson`, and `tsv` output is unchanged.

For machine consumers, cloud state is reported by the presence of the `cloudLinked`, `cloudProject`, and `cloudOrgSource` fields on the result payload — they are emitted **only when a cloud link was requested**. Their absence is itself the signal that the project is unlinked; do not read `cloudLinked` expecting a `false`.

**Source-control during creation** — publish the new project to a fresh repository:

```bash
unity projects create MyGame \
  --vcs github \
  --git-namespace my-org \
  --git-repo my-game \
  --git-visibility private \
  --git-default-branch main \
  --git-token-stdin
```

Source-control flags (shared with `projects link vcs`): `--vcs github|gitlab|uvcs|<host>`, `--git-namespace <name>`, `--git-repo <name>`, `--git-visibility private|public|internal` (default private), `--git-default-branch <name>`, `--git-remote-protocol https|ssh` (default https), `--git-description <text>`, `--git-token <pat>` / `--git-token-stdin`, `--no-initial-commit`, `--git-lfs`, and `--vcs-region <name>` for Unity Version Control.

**Flag names differ by subcommand:** `projects create` and `projects link vcs` use `--git-namespace` / `--git-repo`, while `projects clone` (below) uses `--vcs-namespace` / `--vcs-repo`. Copy the names for the exact command you're running, and confirm with `--help` if unsure.

**`--git-remote-protocol ssh` attaches the created repository's SSH remote instead of its HTTPS one.** The provider REST call that creates the repository still needs the resolved PAT — SSH has no equivalent for that API call — but the local `origin` remote and the initial push then use the repository's `git@<host>:<owner>/<repo>.git` form with pure ambient SSH auth (the running ssh-agent, a repository-local `core.sshCommand`, or an `~/.ssh/config` host alias — whichever the machine already has configured; the CLI never handles keys itself). Passing `--git-token`/`--git-token-stdin` alongside `--git-remote-protocol ssh` is fine: the token still authenticates the repository-creation API call, it's only the git transport that switches.

**Self-hosted hosts create through a provider CLI, not REST.** `--vcs` also accepts a bare host (e.g. `--vcs gitea.example.com`) for anything other than github.com/gitlab.com — there is no built-in REST client for those, so the repository is created through `gh`, `glab`, or `tea`, whichever is installed and already signed in for that host (checked in that order). The same fallback covers github/gitlab themselves when no REST token can be resolved: the matching CLI (`gh` for github, `glab` for gitlab) steps in if it is signed in, before the command gives up. No PAT is ever read, stored, or forwarded on a provider-CLI path. When a provider CLI created the repository, the machine-readable result carries `vcs.mechanism` (`gh` | `glab` | `tea`) naming which one — omitted for the REST path and for a `[url]`-form link, so existing scripted consumers see no change on github.com/gitlab.com. If nothing can create it — no REST token and no signed-in provider CLI for that host — the error explains creating the repository yourself and linking it with `unity projects link vcs <path> <url>`.

**Where the Git token comes from.** Resolution order, first hit wins: `--git-token-stdin` → `--git-token` → `UNITY_GITHUB_TOKEN` / `UNITY_GITLAB_TOKEN` → `git credential fill` (the user's credential helper) → an interactive masked prompt. The first three are explicit per-command overrides and always beat the helper. **The CLI stores no Git token** at any tier, including one typed at the prompt.

**Whether anyone is there to answer is decided up front.** On a real terminal, a configured credential helper is free to run its own sign-in — including Git Credential Manager's browser and device-code flows — and its instructions are relayed to you rather than swallowed. Without a terminal, in CI, under a machine-readable `--format`, or with `--non-interactive`, nothing prompts at all: the command fails immediately with **exit 4**, naming the credential it needed and how to supply it out of band (`--git-token[-stdin]` or the env vars above). Interaction is judged on all three standard streams, so redirecting stderr alone can no longer leave a password prompt writing into a file while waiting on a keystroke you were never shown.

The credential lookup passes the full repository URL, so a helper that keeps one account per URL can return a different token per organization, but only when `credential.useHttpPath` is set, since git otherwise withholds the path from helpers:

```bash
git config --global credential.useHttpPath true
```

Per-project identity instead lives in the repository's own config (`credential.useHttpPath`, `credential.username` in its `.git/config`); `projects link vcs` runs the lookup inside the project, so the Hub, the CLI, and plain `git` all resolve the same credential. For a CI pipeline spanning several organizations, pass `--git-token-stdin` per invocation: the env vars hold one token per provider, and there is no per-org variant.

Per-organization scoping needs the organization to be known before the credential is looked up. `projects clone` always requires `--vcs-namespace`, so it is always scoped. `projects create` and `projects link vcs` take `--git-namespace`, and when it is omitted the lookup stays host-scoped, because the namespace cannot be resolved until you are authenticated and you cannot authenticate without a credential. Pass `--git-namespace` to target a specific organization's credential.

#### projects new

Create a project without any interactive prompts — resolves missing options from stored defaults, never asks the user. The first positional argument is the project **name**; `--path` sets the parent directory:

```bash
# All omitted options resolve from stored defaults
unity projects new MyGame

# Override stored defaults with explicit values
unity projects new MyGame --path /path/to/projects --editor-version 6000.0.47f1 --template com.unity.template.3d

# Open the project immediately after creation
unity projects new MyGame --open
```

`new` never links to Unity Cloud and never asks. Its human output ends with the same pointer at `unity projects link cloud`; machine output is unchanged. To link during creation, use `projects create --cloud`, or link afterwards with `projects link cloud`.

#### projects clone

Clone a remote repository and register the Unity project it contains. Works across providers:

```bash
# Clone by provider + namespace + repo
unity projects clone --vcs github --vcs-namespace my-org --vcs-repo my-game --path ./MyGame

# Check out a specific ref (branch, sha, or UVCS changeset)
unity projects clone --vcs uvcs --vcs-namespace my-org --vcs-repo my-game --ref main

# Authenticate with a personal access token (prefer stdin)
unity projects clone --vcs gitlab --vcs-namespace my-org --vcs-repo my-game --git-token-stdin

# Project lives in a subdirectory of the repo
unity projects clone --vcs github --vcs-namespace my-org --vcs-repo monorepo \
  --path ./repo --project-path packages/MyGame

# Clone an arbitrary git URL instead (HTTPS, or SSH via the standard
# git user@host:path shorthand) — no --vcs/--vcs-namespace/--vcs-repo needed
unity projects clone https://github.com/my-org/my-game.git
unity projects clone --ref develop <ssh-clone-url-for-your-host>
```

Options: `--vcs github|gitlab|uvcs`, `--vcs-namespace <name>`, `--vcs-repo <name>`, `--ref <branch|sha|changeset>` (an all-digit ref is treated as a Unity Version Control changeset, anything else as a branch), `--path <dest>` (clone destination), `--project-path <subpath>` (project subdirectory), `--git-token <pat>` / `--git-token-stdin`, `--json`. Git LFS assets are fetched as pointer files only.

**The `[url]` form is an alternative to `--vcs`/`--vcs-namespace`/`--vcs-repo`, not an addition to them** — passing a URL alongside any of those three flags is a bad-arguments error. `--ref`, `--path`, `--project-path`, `--no-lfs` always apply. `--git-token`/`--git-token-stdin` only apply when the URL is an **explicit HTTPS** URL whose host is github.com or gitlab.com — passing a token for any other host (an SSH-form or SCP-style URL, or a host that isn't github.com/gitlab.com) is a bad-arguments error, since there's nowhere for that credential to go. No token is required: with none supplied, the clone runs with whatever git auth is already set up on the machine (SSH agent, a configured credential helper, `.netrc`, or userinfo embedded in the URL itself) — this includes every SSH-form clone, even against github.com/gitlab.com, since the provider credential helper is HTTPS-only. Unity-project detection is **always** a post-clone scan of the downloaded tree for the URL form (never a provider API call, regardless of host or token) — only Git LFS credentials differ by tier: an HTTPS URL to github.com/gitlab.com with a token uses the provider-scoped LFS credential helper, everything else uses the machine's own git auth for the LFS pull too. A malformed URL, an unreachable host, a rejected/unknown SSH host key, and an authentication failure are reported as distinct errors (exit 2, 7, 3, and 3 respectively).

**SSH transport policy.** No SSH URL is ever rewritten to HTTPS, and no key handling happens in the CLI — a key held by a running ssh-agent is used automatically, and a repository-local `core.sshCommand` or an `~/.ssh/config` host alias behaves identically to plain `git`, because the CLI only supplies its own SSH defaults when none of those (nor `GIT_SSH_COMMAND`/`GIT_SSH`) are already set — and it supplies none of them at all on an interactive terminal, deferring entirely to ssh's own prompts (a real fingerprint prompt for an unfamiliar host, a real passphrase prompt for a protected key with no agent), since a person at the terminal can answer them. Only in a non-interactive invocation (no TTY, or `--non-interactive`/`UNITY_NON_INTERACTIVE`), where no prompt could ever be answered, does the CLI supply its own defaults: an unknown host is trusted on first connect and pinned (so a *later* change to that host's key still fails loudly — this is not silenced), and a passphrase-protected key with no agent fails fast with an actionable error instead of hanging.

#### Working in a Unity Version Control workspace

Two reads are wrapped, because Unity-aware wrapping adds something, and code reviews get their own
subgroup because `cm` cannot reach them at all. Everything else forwards to `cm` unchanged, because
wrapping it would add nothing.

```bash
# Wrapped: the lock listing joined to YOUR pending changes. This is the one thing cm cannot
# do for you, since it has no single command that knows both.
unity vcs uvcs locks                        # who holds what, and what collides with your work
unity vcs uvcs locks --format json          # stable envelope: asOf, inLocalChanges, unlockCommand

# Wrapped: recent history in a shape a script or an agent can rely on.
unity vcs uvcs changesets --limit 50
unity vcs uvcs changesets --format json

# Straight through to cm: partial checkout, shelves, and lock mutations. cm's own flags,
# cm's own output, cm's own --help. This is the supported route, not a workaround.
unity uvcs partial configure
unity uvcs partial update /Assets/Levels
unity uvcs shelve -c "wip: lighting pass"
unity uvcs shelve --apply sh:12
unity uvcs lock list
unity uvcs lock unlock itemid:42@my-game
unity uvcs --help                           # cm's own help, forwarded verbatim
```

`unity vcs uvcs --help` says the same thing at the point of confusion: anything it does not list
forwards straight to `cm`. `unity cm <args>` is the identical passthrough under cm's own name.

**Lock state is shared and racy.** `unity vcs uvcs locks` reports the state as of the moment it
read it, and says so in both the human output and the `asOf` field. Do not cache a reading or treat
one as authoritative; read again before you act. The wrapper never mutates a lock for the same
reason it never paraphrases cm's flags: releasing someone else's lock is a decision about shared
team state, so it stays an explicit `unity uvcs lock unlock` the user types.

#### Code review comments

`unity vcs uvcs review` reads the review comments a person left in the Unity Version Control GUI,
and answers them. This is the one UVCS surface with no `cm` route at all — `cm codereview` manages
review objects but has no comment verbs — so without these commands a reviewer's feedback is
invisible to anything outside the GUI, and a human has to restate every comment in the prompt.

```bash
# Find a review. Any of --changeset, --branch or --status narrows it server-side.
unity vcs uvcs review list --branch /main --format json

# Read its comments, with the file and line each one is anchored to.
unity vcs uvcs review comments --review 42 --format json
unity vcs uvcs review comments --changeset 118      # the review attached to a changeset
unity vcs uvcs review comments --review 42 --all-activity

# Answer one, then record which change addressed it.
unity vcs uvcs review reply   --review 42 --comment 7 --body "Fixed in the next changeset."
unity vcs uvcs review resolve --review 42 --comment 7 --changeset 118
```

All four take the usual optional `[path]` operand; everything else is an option. `--limit` defaults
to 50 and caps at 500, and the envelope's `truncated` tells you when there was more.

**Unity Cloud only.** The reviews service resolves a per-organization cloud region, so a
self-hosted workspace has no reviews API — those commands refuse with
`VCS_UVCS_REVIEW_SELF_HOSTED` and point at the GUI rather than failing obscurely.

**`line` is one-based, and may be absent.** The service anchors a comment with a zero-based line in
a string field whose `-1` means "not anchored to a line". The CLI does that arithmetic once: `line`
in the envelope matches what the dashboard shows, and is `null` — never `0` — for a comment that
is not tied to a line. Do not add one yourself.

**The default is file comments, not the whole feed.** The service's comments route is an activity
feed of fourteen types, only four of which (`Comment`, `Change`, `Question`, `Conversation`) are
things a person wrote against a file. `comments` returns those four; `--all-activity` adds the
status changes, reviewer requests and timeline entries. Read `activityRead` alongside `returned` to
tell "no comments" from "activity, but nothing to act on".

**Resolving means naming a changeset, and is not idempotent.** There is no resolved flag on the
wire — the state IS the changeset the comment was applied in — so `--changeset` is required and is
never inferred from whatever the workspace happens to be sitting on. A comment that is already
resolved is refused (`VCS_UVCS_REVIEW_ALREADY_RESOLVED`) rather than silently re-pointed, because
overwriting it would lose which change actually addressed the comment. The service performs no
already-resolved check of its own, so that guard is entirely client-side: on a review with more
activity than one page, where the comment falls outside the window, `resolve` **refuses**
(`VCS_UVCS_REVIEW_STATE_UNKNOWN`) rather than writing blind — resolve those from the GUI. The guard
is not atomic either: another writer can resolve between the read and the write, and the service
offers no conditional write to close that window.

**Writes name both ids explicitly.** `reply` and `resolve` take `--review` and `--comment` and
accept no `--changeset`/`--branch` selector, unlike the read leaves: they write a durable record
other people read, and an indirect selector resolving to the wrong review would post into a
conversation nobody looked at. Both ids come straight out of `review comments`. A reply body
carrying control characters is refused rather than stripped, so what lands in the review is exactly
what was written.

The `cm` client is needed for all of this. `unity plugin install plastic` installs it; a command
that needs it and cannot find it says so and names that command.

#### Connecting to a self-hosted or enterprise host

GitHub Enterprise Server, self-managed GitLab, and self-hosted Gitea/Forgejo all work with the URL form of `projects clone` and `projects link vcs` (not `projects create --vcs`, which accepts only `github`, `gitlab`, and `uvcs`), but **each host signs in separately**: being signed in to github.com grants nothing on `ghe.example.com`. The first attempt against a new host fails to authenticate (exit 3) until you sign in to that host specifically; the CLI then prints the exact command for whichever mechanism your machine has.

Three ways in, any one is enough:

```bash
# 1. The provider's own CLI, scoped to the host (enables repo browsing/creation)
gh auth login --hostname ghe.example.com
glab auth login --hostname gitlab.example.com
tea login add --name work --url https://gitea.example.com

# 2. Git Credential Manager: one credential per host, no per-host setup,
#    picked up once `git credential approve` has stored one for that host

# 3. SSH: needs neither of the above; uses your SSH agent
unity projects clone <ssh-clone-url-for-your-host>
```

`gh` and `glab` hold one session per host and can be signed in to several simultaneously, which is why the sign-in commands are host-scoped rather than bare. `tea` has no default host at all: every login is a named entry for one instance URL.

The CLI reads and stores no token on any of these paths. It asks each installed provider CLI whether it holds a session for that specific host, with credential-bearing environment variables stripped from the child process: `gh` otherwise applies `GH_ENTERPRISE_TOKEN` to any non-cloud host and dials it to validate, which would leak the token to a host that merely appeared in a failing URL. Stripped, `gh` answers from its own config: an unconfigured host is never contacted. Side effect: authenticating purely via `GH_ENTERPRISE_TOKEN` (no `gh auth login` entry) reads as signed out, so you may be offered a sign-in you do not need. A host with no provider CLI available is not a dead end: plain git still clones and pushes, with credentials from the credential manager or the SSH agent. Note `--git-token`/`--git-token-stdin` still only apply to github.com and gitlab.com over HTTPS; for any other host, rely on one of the three paths above.

#### projects pin / unpin

```bash
# Pin a project to the top of the list
unity projects pin /path/to/MyProject

# Unpin
unity projects unpin /path/to/MyProject
```

#### projects size

Report a project's on-disk footprint broken down by top-level folder (Assets, Library, Packages, …) with a total, so you can see how much is regenerable build state (Library, Temp) versus source and assets:

```bash
# Size of one project (defaults to the current project when the argument is omitted)
unity projects size /path/to/MyProject

# Summarize every registered project, largest first
unity projects size --all

# Machine output — raw bytes instead of readable KB/MB/GB units
unity projects size --all --json
```

Human output uses readable units; `--json` (and `--format ndjson`) emit raw byte counts.

#### projects clean

The counterpart to `projects size`: deletes the **regenerable** folders (`Library`, `Temp`, `Logs`, …) to reclaim disk space. Unity rebuilds them on the next open — at the cost of a slow first import.

```bash
# Preview: what would be deleted, with sizes — deletes nothing
unity projects clean --dry-run

# Clean the current project (prompts to confirm)
unity projects clean

# Clean a project by path or registered name
unity projects clean ./MyGame

# Non-interactive: --yes is REQUIRED in a script or CI
unity projects clean MyGame --yes
```

The project argument defaults to the current directory and accepts a path or a registered project name. Guardrails worth relying on:

- **It refuses while the project is open in a running editor**, naming the PID — cleaning `Library` under a live editor corrupts the session. If the CLI cannot determine whether an editor has it open, it warns and proceeds, so close editors first in automation.
- **It refuses to delete unprompted.** In a non-interactive shell without `-y, --yes` it stops rather than deleting.
- A path that isn't a Unity project (no `ProjectVersion.txt`) is rejected outright, so a mistyped path can't delete anything.

`--dry-run` is the safe way to size the win first; it reports what it *would* reclaim and exits without touching the filesystem.

#### projects verify

An Editor-free integrity check on the **project**, meant as the first step of a CI job. `unity doctor` answers "can this machine build?"; this answers "is this project sound?" — the version-control damage that otherwise surfaces after the expensive build step, as a confusing import error or an artifact that is wrong rather than missing:

```bash
# Verify the current project
unity projects verify

# Verify a project by path or registered name
unity projects verify ./MyGame

# CI gate: warnings fail the job too
unity projects verify --strict

# Only the checks you care about (either spelling works)
unity projects verify --check meta-missing,guid-duplicate

# Confirm the project targets the version the pipeline pins
unity projects verify --expect-editor 6000.0.30f1

# Inline annotations on the job, anchored to the offending file and line
unity projects verify --format github
```

Exits `0` when nothing error-severity is found, `6` otherwise. Warnings alone still exit `0` — `--strict` promotes them. Every finding carries a stable code, a severity, a project-relative path, and a remediation hint:

| Code | Severity | Detects |
|---|---|---|
| `META_MISSING` | error | An asset under `Assets/` with no sibling `.meta`. Unity assigns a fresh guid, silently breaking every reference to it. |
| `META_ORPHAN` | warning | A `.meta` whose asset no longer exists. |
| `GUID_DUPLICATE` | error | Two `.meta` files claiming the same `guid` — typically two branches that each added one. |
| `CONFLICT_MARKERS` | error | Unresolved merge markers in a `.meta`, `ProjectSettings/*.asset`, `Packages/manifest.json`, or `packages-lock.json`. |
| `MANIFEST_INVALID` | error | `Packages/manifest.json` does not parse, or a dependency version is not a string. |
| `EDITOR_VERSION_DRIFT` | warning | `ProjectVersion.txt` disagrees with `--expect-editor`. |
| `PATH_UNVERIFIABLE` | warning | A path the scan did not inspect, named so you know which subtree went unchecked. Always on — not selectable via `--check`. |

Worth knowing:

- **Editor-version drift is opt-in.** Nothing in the CLI stores a pinned version, so the check only runs when you pass `--expect-editor <version>` — the version your pipeline pins.
- **`--format json` returns the full report** (findings plus an errors/warnings/filesScanned summary); **`--format ndjson` emits one `type: "finding"` record per finding** as it is found, then a terminal result frame. Piped stdout defaults to `tsv`, like the rest of the CLI.
- **Safe to paste into a public log.** Finding paths are project-relative, terminal-escape-stripped, and the project root has its home directory masked.
- **It is built to scan an untrusted repository** — a fork, an unreviewed pull-request branch, a third-party template. So it refuses to follow a symlinked `Assets/`, `ProjectSettings/`, or `Packages/` (exit 6, `PROJECTS_VERIFY_UNSCANNABLE_DIR`), which would otherwise make it enumerate or read a tree outside the project into your CI log, and it bounds every file read at 16 MiB. A symlink **deeper** in the tree is not refused — it is skipped and reported (next bullet), since one link inside `Assets/` should not fail the whole scan.
- **`summary.unverifiable` tells you whether the pass is complete, and each skipped path is named.** Anything the scan could not inspect — a symlinked directory or file at any depth, an unreadable subtree, a walk past the depth cap, an over-budget file, a missing `Assets/` — is counted there and reported as a `PATH_UNVERIFIABLE` warning carrying the path, so you can see which subtree went unchecked instead of only how many did. Under `--strict` those warnings promote to errors like any other, so a project cannot satisfy the gate by making verification impossible rather than by being sound.
- **`--expect-editor` must be a real Unity version.** A typo like `6000.x` exits 2 rather than becoming a drift warning that passes — otherwise a misconfigured pipeline would silently satisfy its own version gate.
- **`data.checks` lists what actually ran.** `EDITOR_VERSION_DRIFT` is absent unless you passed `--expect-editor`, since without a version to compare against there is nothing to check.
- **No Editor, no license, no network, no installed editor** — and it does not read asset bodies, so it stays fast on a large project.
- **Detection only.** It does not repair anything; fixing meta/guid divergence needs the Editor's own asset database.
- Names Unity's importer ignores (dot-prefixed, `~`-suffixed, `.tmp`, `cvs`) are skipped, so a `.gitignore` or a `Documentation~` folder never reports a missing `.meta`.

#### projects require

Ensure the editor version required by a project is installed, installing it if needed:

```bash
unity projects require /path/to/MyProject --yes
```

On a TTY with no path, prompts interactively.

#### projects upgrade

Upgrade a project to a different Unity editor version. `--to` is required:

```bash
unity projects upgrade --to 6000.0.47f1
unity projects upgrade /path/to/MyProject --to 6000.0.47f1 --yes
```

#### projects export / import

```bash
# Export the project registry to a file (or stdout if -o is omitted)
unity projects export -o projects.json

# Import a previously exported registry
unity projects import projects.json
unity projects import --input projects.json
```

#### projects exec — run a command across every registered project

Run one command in each registered project. The command runs in that project's own directory, with `UNITY_PROJECT_PATH` and `UNITY_EDITOR_VERSION` set in its environment. Everything after `--` is the command:

```bash
# Every registered project
unity projects exec -- git status --short

# Only pinned projects
unity projects exec --filter pinned -- git pull

# Only Unity 6 projects, four at a time, without stopping on failures
unity projects exec --filter 'version:6000.*' --parallel 4 --continue-on-error -- npm test

# See what would run, without running it
unity projects exec --dry-run --filter 'name:My*' -- ./build.sh

# Machine-readable per-project results
unity projects exec --json -- git rev-parse HEAD
```

`--filter` is repeatable and every term must match (AND):

| Term | Matches |
|---|---|
| `name:<glob>` | project name or path — a bare glob (`My*`) is shorthand for this |
| `version:<glob>` | the project's required editor version (`6000.*`) |
| `pinned` / `pinned:false` | pin state; bare `pinned` means pinned |

Globs are path-aware, so use `**/` to match inside a path: `name:My*` matches by project name, `name:**/work/*` by location.

Behavior worth knowing:

- Projects run **one at a time** and the run **stops at the first failure**. Raise `--parallel <n>` for concurrency, or pass `--continue-on-error` to run the whole fleet regardless. With `--parallel > 1`, each project's output is buffered and flushed when it finishes so runs can't interleave; "stop" then means no *new* projects start — those already running finish.
- Buffered output is capped at **4 MiB per project**, after which it is cut short and the run warns. Sequential mode (`--parallel 1`) streams live and is never capped, so use it when you need the full output of a chatty command.
- **Ctrl-C** stops scheduling *and* terminates the projects already running, then exits **130**.
- Exit code is **6** if any project failed, **2** for a usage error (unknown filter key, bad `--parallel`, a command not on your `PATH`), **0** otherwise. No matching projects is a success (exit 0) with a warning.
- Arguments are passed to the command **verbatim, not through a shell** — pipes, `&&`, and shell globbing are not available. Put that logic in a script and exec the script.
- In `--json` / `--format ndjson` / `--format tsv`, the child's own output goes to **stderr** so stdout stays machine-parseable.
- `--format ndjson` streams one `{"type":"project",…}` frame per project as it settles and always closes with the standard `{"type":"result",…}` envelope (`success`, `command`, `data`, `errors`, `warnings`) — including under `--dry-run`.

#### projects open / link / unlink

```bash
# Open a registered project by name, fuzzy title match, or path
unity projects open MyProject
# (the top-level `unity open` is the same thing)

# --- Cloud links ---
# Connect an existing local project to a Unity Cloud project
unity projects link cloud /path/to/MyProject --cloud-org <id-or-name>
# Disconnect from its Unity Cloud project
unity projects unlink cloud /path/to/MyProject

# --- Version-control links ---
# Publish a local project to a NEW GitHub / GitLab / Unity Version Control repository
unity projects link vcs /path/to/MyProject \
  --vcs github --git-namespace my-org --git-repo my-game --git-token-stdin
# Attach to an ALREADY-EXISTING remote instead of creating one — pass its URL
unity projects link vcs /path/to/MyProject https://github.com/my-org/my-game.git
# Remove a project's git remotes (the remote repositories are NOT deleted)
unity projects unlink vcs /path/to/MyProject
# Also detach the Unity Version Control workspace
unity projects unlink vcs /path/to/MyProject --unlink-workspace
```

`link vcs` shares the source-control flag set documented under `projects create`. `link cloud` / `link vcs` accept `--cloud-org <id-or-name>` (env `UNITY_CLOUD_ORG`).

The `[url]` second operand attaches to a remote that already exists, instead of creating one — the one thing the flag form of `link vcs` cannot do. It is mutually exclusive with `--vcs`, `--git-namespace`, `--git-repo`, `--git-visibility`, `--git-default-branch`, `--git-remote-protocol`, `--git-description`, `--cloud-org`, and `--cloud-project` (all meaningless without a repository to create — the URL's own scheme already says which transport to use). `--git-token[-stdin]`, `--no-initial-commit`, and `--git-lfs` still apply, and the same ambient-auth / Tier A rules as `projects clone [url]` govern whether the push uses a supplied token or the machine's own git auth.

---

### Releases — browse Unity versions

```bash
# List recent releases
unity releases --format json

# Filter by stream (alpha, beta, lts, tech)
unity releases --stream lts --format json
unity releases --stream tech --format json
unity releases --stream beta --format json

# LTS only shorthand
unity releases --lts --format json

# Filter from a year onward
unity releases --since 2023 --format json

# Paginate
unity releases --limit 10 --skip 20 --format json
```

---

### Templates

```bash
# List templates for an editor version (uses default editor if --editor is omitted)
unity templates list --editor 6000.0.47f1 --format json

# List only locally installed templates
unity templates list --editor 6000.0.47f1 --installed --format json

# Filter by type (core, learning, sample, custom, new, all) — case-insensitive
unity templates list --editor 6000.0.47f1 --type core --format json
unity templates list --editor 6000.0.47f1 --type learning --format json
unity templates list --editor 6000.0.47f1 --type sample --format json
unity templates list --editor 6000.0.47f1 --type new --format json
unity templates list --editor 6000.0.47f1 --type all --format json  # no-op, returns everything

# List only user-generated (custom) templates
unity templates list --editor 6000.0.47f1 --custom --format json
# --type custom is an alias for --custom
unity templates list --editor 6000.0.47f1 --type custom --format json

# --custom and --type are mutually exclusive — using both is an error (exit 1)

# Show template details
unity templates info com.unity.template.3d --editor 6000.0.47f1 --format json

# Create a custom template from an existing Unity project
# --name and --display-name are REQUIRED
unity templates create /path/to/MyProject \
  --name com.myorg.template.mytemplate \
  --display-name "My Template"

# With all optional options
unity templates create /path/to/MyProject \
  --name com.myorg.template.mytemplate \
  --display-name "My Template" \
  --description "A starting point for our projects" \
  --template-version 1.0.0 \
  --output /path/to/templates/dir \
  --keep-embedded-packages \
  --keep-project-settings \
  --overwrite

# JSON output (includes path to created .tgz archive)
unity templates create /path/to/MyProject \
  --name com.myorg.template.mytemplate \
  --display-name "My Template" \
  --json

# NDJSON streaming — emits progress frames then a result frame
unity templates create /path/to/MyProject \
  --name com.myorg.template.mytemplate \
  --display-name "My Template" \
  --format ndjson
```

**`templates create` key notes:**
- `--name` must be a valid npm package name (e.g. `com.myorg.template.mytemplate`)
- `--output` overrides the Hub-configured user templates directory
- `--overwrite` replaces an existing archive of the same name without error
- On success, prints the path to the created `.tgz` archive
- Created templates appear in `unity templates list --editor <v> --custom`

**`templates pack` — portable archive, not a registered template.** `create` installs into the Hub-configured user templates directory so the template shows up in `templates list --custom`; `pack` writes a standalone `.tgz` to a file path you choose and registers nothing. Reach for `pack` when the archive is an artifact to check in, attach to a release, or hand to someone else.

```bash
# Pack a project into a portable template archive (--output is REQUIRED)
unity templates pack ./MyProject \
  --output ./my-template.tgz \
  --name com.myorg.template.mytemplate \
  --display-name "My Template"

# Minimal form — prompts for name and display name on a TTY
unity templates pack ./MyProject --output ./my-template.tgz

# Replace an existing archive, with machine output
unity templates pack ./MyProject --output ./my-template.tgz --overwrite --json
```

**`templates pack` key notes:**
- `--output <file>` is a **file path**, not a directory, and is required
- `--name` and `--display-name` are required; on a TTY they're prompted for when omitted, so pass both in CI
- Use `--template-version`, **not** `--version` — the latter collides with the global `-V, --version` flag
- The output path may not be **inside** the project being packed; that's rejected, so the archive can't include itself
- An existing output file is an error unless `--overwrite` is passed
- `--keep-embedded-packages` and `--keep-project-settings` retain content that is otherwise stripped
- Consumable directly by project creation: `unity projects create MyGame --template ./my-template.tgz`

```bash
# Delete a user-generated custom template (prompts for confirmation)
unity templates delete com.myorg.template.mytemplate --editor 6000.0.47f1

# Skip the confirmation prompt (CI-friendly)
unity templates delete com.myorg.template.mytemplate --editor 6000.0.47f1 --yes

# JSON output
unity templates delete com.myorg.template.mytemplate --editor 6000.0.47f1 --yes --json
```

**`templates delete` key notes:**
- Only user-generated templates (created via Hub UI or `templates create`) can be deleted
- Attempting to delete a built-in Unity template exits with a descriptive error (exit 6)
- Attempting to delete a template that doesn't exist exits with a descriptive error (exit 6)
- In interactive mode, prompts for confirmation before deleting; use `--yes` to skip
- On success, the template no longer appears in `unity templates list --editor <v> --custom`

```bash
# Get/set/reset the default storage path for custom templates
# Print current configured templates location
unity templates location

# Set a new default templates directory (must exist as a directory)
unity templates location --set /path/to/templates

# Reset templates location to the Hub default
unity templates location --reset

# JSON output for any variant
unity templates location --json
unity templates location --set /path/to/templates --json
unity templates location --reset --json
```

**`templates location` key notes:**
- `--set` and `--reset` are mutually exclusive (using both is an error)
- `--set` validates that the path exists and is a directory (exits 2 if not)
- `--reset` restores the Hub default templates path
- JSON output: `{ "path": "..." }` inside the standard envelope

```bash
# Edit a user-generated (custom) template's metadata
# At least one of --display-name, --description, --template-version,
# --preview-image, --remove-preview-image is required
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1 --display-name "My Updated Template"

# Update multiple fields at once
unity templates edit com.myorg.template.mytemplate \
  --editor 6000.0.47f1 \
  --display-name "My Updated Template" \
  --description "A new description for the template" \
  --template-version 1.1.0

# Replace / remove preview image
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1 --preview-image /path/to/image.png
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1 --remove-preview-image

# JSON / NDJSON output (--yes required because these are non-interactive)
unity templates edit com.myorg.template.mytemplate --editor 6000.0.47f1 --display-name "Updated" --yes --json
```

**`templates edit` key notes:**
- Only works on user-generated (custom) templates; built-in templates cannot be edited
- Use `--editor` to specify which editor version's template list to search, or omit to use the stored default
- `--preview-image <path>` resolves to an absolute path before passing to the service
- `--remove-preview-image` is only applied when no valid `--preview-image` path is given; if both are passed with a valid image path, the new image wins and `--remove-preview-image` is ignored
- On success (human format), prints the updated template's display name

---

