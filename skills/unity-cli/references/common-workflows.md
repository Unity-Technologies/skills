# Common workflows — unity-cli reference

Relative links in this file resolve to the skill directory's `references/` folder.

Part of the **`unity-cli`** skill. Read this when the main page points here.

### Contents

- Common workflows
- Inspect and run cloud builds and Pipeline Automation jobs
- Edit a scene, GameObject, or asset — `unity status` first
- Use a catalog plugin for a task the built-in commands don't cover
- Error codes: cause and fix
- Bootstrap a new project from scratch
- Find and install a missing editor
- Open a project with the correct editor
- CI: activate a license, then build
- CI: batch-mode build
- CI: run tests and publish results
- Debug the CLI

## Common workflows

### Inspect and run cloud builds and Pipeline Automation jobs

Read [cloud-automation.md](cloud-automation.md) for every command in
both groups, context/authentication, per-command filters and sorting,
output fields, pagination (including the leaves that refuse `--page`/`--limit`
rather than ignoring them), errors, and redaction. `pipeline cloud-build` works with Build Automation;
`pipeline automation` works with Pipeline Automation. Neither needs a running Editor
or the local Pipeline package. Use numeric organization IDs for service accounts.
The inventory commands are read-only. The commands that act on work start and
cancel builds, download artifacts, trigger pipelines, and follow or act on jobs;
`builds cancel`, `jobs terminate` and `jobs reject` ask first unless `--yes` is
given, and none of them change configuration.

JSON/NDJSON return full API-shaped results under `data`, as Collab does; NDJSON
has one terminal result, without item frames. Preserve native fields and free-form
metadata, subject to the reference's bounded secret protections and public-API
redaction assumption. Table projections remain separate. Explicit local build
targets retain `_local`; missing targets aren't local. These conventions apply
to follow-up cloud-automation work too.

### Edit a scene, GameObject, or asset — `unity status` first

**Before editing any scene, GameObject, prefab, or asset, run `unity status` to detect a connected Editor.** If one is reachable, drive it with live commands instead of touching project files — the Editor applies changes to the *actual active scene* and keeps its in-memory state in sync.

```bash
unity status                       # is an Editor connected? (look for state "ready")
unity command                      # discover the scene/GameObject commands THIS Editor exposes
# then drive it with the commands it lists — for example, if your Editor exposes them:
unity command gameobject create   # act on the live, active scene
unity command scene save          # persist the active scene
```

Commands are grouped and invoked as `unity command <group> <name>`. Their names are defined by the Editor, so run `unity command` to see the exact set — don't assume a name.

> **Never hand-edit `.unity`, `.prefab`, or `.asset` YAML while a live Editor is reachable.** Raw-file edits are:
> - **error-prone** — fileIDs and GUIDs are assigned by hand and easy to get wrong;
> - **invisible** to the running Editor until a reimport, so the change silently fails to take effect; and
> - **prone to hitting the wrong file** — e.g. writing to `SampleScene.unity` while the Editor's active scene is actually `Demo2.unity`, producing valid-looking YAML that changes nothing the user sees.

**Rule out two false negatives before concluding no Editor is reachable — both look identical to a genuinely closed Editor, and both are easy to get wrong under time pressure:**

- **Safe Mode.** If an Editor *is* running for this project but `unity command` won't connect, it may be stuck in **Safe Mode** from a compile error rather than genuinely absent. Run `unity status`: if it reports Safe Mode (`STATUS_SAFE_MODE`, with the compile errors), editing the C# source to fix the compile errors *is* the correct move, not a fallback. See [integration-advanced.md → Recovering from Safe Mode](integration-advanced.md#recovering-from-safe-mode-connection-fails-because-of-compile-errors).
- **A sandboxed agent shell.** If your own shell commands run inside a restrictive sandbox — the normal case for a coding agent like this one — the sandbox can hide a genuinely running Editor from `unity status` the same way. This applies to **every** scene/GameObject/prefab/asset task that reaches this preflight, not only ones that obviously need a live Editor: a task you could otherwise finish without any CLI involvement (e.g. generating an asset through ordinary Editor APIs) can still get funneled into "no Editor" here and derailed. Don't treat "no instances" as proof the Editor is down, and don't quietly improvise a third path — like driving a separate batch-mode Editor process to approximate what a live connection would have done — as a substitute for a disclosed file edit. Say plainly that your sandbox may be blocking your view of a real Editor, and ask whether one is actually open before falling back. Full detail: [integration-advanced.md → Sandboxed agent tooling can hide a running Editor](integration-advanced.md#sandboxed-agent-tooling-can-hide-a-running-editor).

Only fall back to editing files directly once you've ruled out both of the above — and say so explicitly ("no live Editor detected, editing the file directly").

### Use a catalog plugin for a task the built-in commands don't cover

The CLI ships a catalog of Unity plugins, installed on demand. **Before approximating a request
some other way, or saying the CLI can't do it, check whether a catalog plugin covers it.** A plugin
is Unity's own tool for its class of request, and using it is almost always the better answer.
`unity plugins` always lists the full catalog, installed or not, and the command search
(see "Find a command" on the main page) matches each plugin's name and description.
The steps below use the Unity AI Generators plugin (`ai`), which covers "generate a
texture/sprite/image/3D model for my project", as the example. Every catalog plugin works the same
way.

1. **Find the plugin and check whether it's installed.**
   ```bash
   unity commands --grep "<the request's own words>" --format json
   unity plugins --format json
   ```
   Find the entry whose `id` is the plugin's (`"ai"` here) and read its `installed` field
   (`true` / `false`).

2. **Not installed? Tell the user, then ask, never install silently.** Say plainly that Unity
   ships an AI Generators plugin that covers this, and wait for a yes before running:
   ```bash
   unity plugin install ai
   ```
   A refusal is a normal answer: fall back to whatever you would otherwise have done, and don't
   ask again in the same conversation.

   `unity plugin install <id> --local` writes the plugin's agent skill into the current project
   (`<project>/.claude/skills/...` and the other project-local client folders) instead of each AI
   client's user-wide folder. It applies only to clients that already have Unity's skill in that
   project, and Claude Desktop is skipped because its skills are user-wide only. `unity plugin
   upgrade` never adds a copy: it updates the plugin's skill only where it is already installed,
   user-wide or in the current project. Run it from that project to refresh its copy.

   `ai` is alpha, and during alpha it's gated to Unity staff on Unity's internal network. An
   ordinary user's `unity plugin install ai` can fail with a **sign-in** message (Unity AI
   Generators is limited to Unity staff, so sign in with a Unity staff account) or, once
   past that, a plain **download failure** if the network it needs isn't reachable from where
   you're running. Neither is a bug in the request: report the message and fall back, the same as
   any other refusal.

   `unity plugin install` and `unity plugin upgrade` install the newest build the catalog offers
   you, whatever its channel. When that build is a prerelease (`beta`, or `alpha`/`nightly` for
   Unity staff), a warning says so before the download, in the envelope's `warnings` under
   `--format json`. Pass it on to the user: a prerelease may change or break between versions.

   A plugin whose registry entry carries license terms shows them and waits for an answer
   before anything downloads. `unity plugin install <id>` and `unity plugin upgrade <id>` ask on
   a terminal. A run that cannot ask (`--non-interactive`, CI, piped input) fails with the
   error code `PLUGIN_EULA_REQUIRED` and downloads nothing, and a "no" fails with
   `PLUGIN_EULA_DECLINED`. Pass `--accept-eula` only after the user has read the terms and
   agreed: it is their acceptance, never yours to give. An accepted version is remembered, so an
   upgrade asks again only when the terms changed. `--dry-run` on either command prints the terms
   and what would change, asks nothing, records nothing and downloads nothing. In
   `--format json`, a row for a plugin with terms carries `eula` (`required`, `accepted` or
   `declined`); a plugin with no terms has no such field. A `--dry-run` row also carries `dryRun: true` and an `eulaNotice` object (`version`, `url`,
   `label`, `message`, `links`) with the terms to read. `--quiet` never hides the terms. A notice
   the registry carries but the CLI cannot read refuses the install under `PLUGIN_EULA_REQUIRED`.

   A plugin can need other plugins. `unity plugin install <id>` installs the ones it requires
   first, each with its own license step, and reports each as its own row. If one of them cannot
   install, the plugin itself is not installed, and its row names the one that stopped it.
   `unity plugin remove <id>` refuses a plugin another installed plugin requires: remove the
   other one first. `unity plugin --json` lists each entry's `dependencies` (`id`, `optional`).

3. **Pick up its agent skill in the same session.** A catalog plugin can ship its own agent skill
   inside its payload (`ai` does), and installing the plugin does not, by itself, make that skill
   visible to you; it has to be mirrored the same way this skill itself is:
   ```bash
   unity skill install <client>     # e.g. claude-code, first time this session
   unity skill refresh              # already mirrored earlier? re-render it fresh
   ```
   `unity plugin install`'s own last line already tells you which one applies: it points at
   `unity skill install` right after installing a copy that ships a skill, and at
   `unity skill refresh` once one is already mirrored but the plugin's copy just changed. Confirm
   where it landed with `unity skills --format json` (look for the row whose `skill` field
   names the plugin's own skill, e.g. `unity-ai`, and read its `path`), then read that file before
   driving the plugin's commands. Don't guess its command surface from this skill, which only
   documents `unity` itself.

**A command invoked before any of this** (`unity ai …` typed straight, or suggested from memory)
**fails with a clear, actionable message** naming the exact install command (`unity plugin install
ai`), in both human and machine (`--format json` / `--format ndjson`) output, under the stable
error code `TOOL_NOT_INSTALLED`. Read that message rather than guessing why the command did
nothing; the same shape (`TOOL_UNSUPPORTED_PLATFORM`, `TOOL_RUNTIME_NOT_INSTALLED`) covers the
two other reasons a catalog tool can't run.

### Error codes: cause and fix

| Code | Cause | Fix |
|---|---|---|
| `EDITOR_NOT_RUNNING` (exit 12) | A `--port`, `--pid` or `--instance` pin matches no running Editor | Run `unity status`, then fix the pin or open the Editor |
| `AMBIGUOUS_EDITOR` | No running project matches, and the error lists the candidates | Pin with `--project-path` from that list |
| `TOOL_NOT_INSTALLED` | The catalog plugin isn't installed | Run the install command the message names, eg `unity plugin install ai` |
| `TOOL_UNSUPPORTED_PLATFORM` | The tool doesn't run on this OS or CPU | Tell the user; don't retry |
| `TOOL_RUNTIME_NOT_INSTALLED` | The tool's runtime is missing | Follow the install step the message names |
| `PLUGIN_EULA_REQUIRED` | The plugin has license terms the run can't ask about, or can't read | Show the terms with `--dry-run`; pass `--accept-eula` only after the user agrees |
| `PLUGIN_EULA_DECLINED` | The user said no to the terms | Stop; never retry with `--accept-eula` |

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
#    (lts / latest aliases work almost everywhere a version is accepted — `templates` is the
#     exception; see step 3.)
unity releases --stream lts --limit 5 --format json
unity install lts --module android --module ios --yes --accept-eula   # add --module webgl, etc.
unity editors --installed --format json                               # confirm it landed

# 3. List the real template ids this Editor offers — don't guess them — and pick by RENDER
#    PIPELINE, not just by 2D/3D. Default to the URP templates:
#      3D → com.unity.template.urp-blank      ("Universal 3D")
#      2D → com.unity.template.universal-2d   ("Universal 2D": URP + the 2D packages)
#    com.unity.template.3d and com.unity.template.2d are the Built-in Render Pipeline templates
#    (displayName "… (Built-In Render Pipeline)"): deprecated from Unity 6.5, gone in 6.7. Use
#    them only when the user explicitly asks for Built-in. Confirm the pick with the JSON
#    `renderPipeline` field — it is blank for universal-2d on current releases, so match that
#    one by id.
#    `templates` resolves the lts / latest aliases through the release catalog, as `install`
#    does, but the version you just installed (read it from `editors --installed`) is the one
#    whose templates you want.
unity templates --editor-version <6000.x.y> --type core --format json

# 4. Create the project. The first positional arg is the NAME; --output-dir sets the parent directory.
#    All options supplied, so it won't prompt; add --non-interactive in CI.
#    (To publish it to a remote in the same step, use the source-control forms below instead.)
unity projects create "MyGame" --output-dir ~/UnityProjects \
  --editor-version lts --template com.unity.template.urp-blank

# 5. Add the Pipeline package BEFORE the first open. `unity command`, `unity status` and
#    `unity command script eval` all need it, and templates don't include it. The Editor reads
#    Packages/manifest.json when it loads the project, so installing first means the package
#    is live from the first open.
unity pipeline install --project-path ~/UnityProjects/MyGame

# 6. Open the project and wait until its Editor is ready to take commands. Pass the project's
#    full path: a path that exists matches that project's Editor only, so an Editor open on a
#    sibling checkout such as ~/UnityProjects/MyGame-lighting can't end the wait.
cd ~/UnityProjects/MyGame
unity open .
unity status --until-ready --project-path ~/UnityProjects/MyGame --format json
```

**Then build the scene in the live Editor, not in batch mode.** Create GameObjects, wire
components, and set asset references with `unity command script eval` (or the Editor's own `unity command`
tools) against the running Editor, and read the result back the same way. Each step can be
checked before the next one. A script run through `unity run -- -executeMethod` is the
**fallback** for when no Editor can stay open (CI, a headless build box). It can't see what it
produced: a reference saved as null, such as a `UIDocument` with no `PanelSettings`, still
reports success. If you do use batch mode, open the result in a live Editor and inspect it before
calling the work done. How to get an Editor to drive:
[integration-advanced.md → Getting an Editor to drive](integration-advanced.md#getting-an-editor-to-drive).

**Source control — let the user choose.** The CLI publishes the new project to a fresh remote in
one step for any provider. **Always pass tokens on stdin** (`--git-token-from-stdin`) so secrets never
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
unity projects create "MyGame" --output-dir ~/UnityProjects \
  --editor-version lts --template com.unity.template.urp-blank \
  --vcs github --git-namespace my-org --git-repo my-game \
  --git-visibility private --git-default-branch main --git-token-from-stdin --git-lfs

# Unity Version Control (UVCS) — handles binaries natively, so no LFS:
unity projects create "MyGame" --output-dir ~/UnityProjects \
  --editor-version lts --template com.unity.template.urp-blank \
  --vcs uvcs --git-namespace my-org --git-repo my-game --vcs-region <region>
```

Feed the token to `--git-token-from-stdin` from a secret store, never a literal — e.g.
`… --git-token-from-stdin <<<"$GIT_TOKEN"` where `$GIT_TOKEN` comes from your CI/secret manager
(UVCS uses your Unity sign-in, so no token is needed).

**Working with a UVCS workspace day to day: a few wrapped reads, everything else straight through
to `cm`.** The split is deliberate and worth teaching, because guessing wrong wastes a user's time:

- **`unity vcs uvcs <verb>`** wraps the reads that **join `cm`'s data to your project** —
  `locks` (who holds a lock, *and which locks cover files you have already changed*),
  `changesets`, and `review`. Those joins are the thing `cm` cannot do for you, and they come in a
  stable envelope, so prefer them whenever something *parses* the output.
- **`unity uvcs <args>`** forwards the whole command line to `cm` verbatim, `--help` and
  `--format` included. That is the supported route, not a workaround: `cm` owns and versions this
  vocabulary, so wrapping it would pin a paraphrase that goes stale. Reach for it for **partial
  checkout**, **shelves**, and **taking or releasing a lock**, and when a human reads the output.

```bash
unity vcs uvcs locks                       # who holds what, and what collides with your changes
unity uvcs lock list                       # the raw listing, cm's own flags and output
unity uvcs partial update /Assets/Levels   # cm's own vocabulary, unchanged
unity uvcs shelve -c "wip: lighting pass"
```

Every verb, flag and trap: [version-control.md](version-control.md).

`unity cm <args>` is the same passthrough under cm's own name. Both need the `cm` client; install
it with `unity plugin install plastic` if a command says it is missing.

**Beyond setup, the `vcs` group covers the whole day-2 loop** — `status`, `sync`, `switch`,
`merge-setup`, `conflicts` / `explain` / `resolve`, `diff`, `blame`, `summarize`, `affected`,
`hooks`, `doctor`, `providers` — and the Unity semantics are the reason to reach for it over raw
`git`. Full reference, with the flags and the traps:
[version-control.md](version-control.md).

**Git tokens belong to the user's credential manager, not the CLI.** When no token flag or env var
is given, the CLI asks `git credential fill` and uses whatever the configured helper returns; it
stores nothing it is passed or told. Don't suggest the CLI can save a Git token, and don't reach for
a token flag when the user already has a working credential helper. If they want a different token
per organization, that is `git config --global credential.useHttpPath true` plus a multi-account
helper such as [Git Credential Manager](https://github.com/git-ecosystem/git-credential-manager).
The CLI passes the full repo URL so the helper can discriminate, but it never installs or
reconfigures a helper. `UNITY_GITHUB_TOKEN` / `UNITY_GITLAB_TOKEN` are one token per provider, so a
CI job spanning several orgs should pass `--git-token-from-stdin` per invocation instead. See
[references/projects-templates.md](projects-templates.md) for the full
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
It has no top-level package command. To add UPM (Unity Package Manager) packages beyond the
template, use the **`unity-package-management`** skill: it runs the Pipeline `package add`
command through `unity command`, with a batch-mode C# script as the fallback. For monetization/backend, hand off to the dedicated skills: `implement-in-app-purchases`
(IAP), `levelplay-unity-integration` (ads), or `build-live-game` (accounts, cloud save,
economy, remote config, leaderboards). Once steps 5 and 6 above are done, work in the open
Editor.

**Installed the Pipeline package while the Editor was already open?** The Editor picks up the
manifest change only when it next refreshes. Until then `unity status` reports
`STATUS_PIPELINE_LOAD_PENDING` (or `STATUS_NO_INSTANCES` on CLI releases before that code
existed). An Editor that is still opening or importing reports the same code until it finishes,
so wait for that first (`unity status --until-ready`). The CLI can't trigger the refresh itself:
if the Editor has finished opening, ask the user to switch to the Unity Editor window, then
re-run `unity status --until-ready`. Installing before the first open (step 5) avoids this
entirely.

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
# 1. Activate a license without signing in; a service account cannot activate one.
#    A perpetual or legacy serial: store it as the CI secret UNITY_LICENSE_SERIAL
#    (the name `unity ci init` generates) and pass it to the step, e.g. in GitHub
#    Actions `env: { UNITY_LICENSE_SERIAL: ${{ secrets.UNITY_LICENSE_SERIAL }} }`.
#    A serial tied to a subscription needs a signed-in user, so for those seats use
#    --floating, or the offline --generate-request / --file path (see "Headless /
#    fleet license activation" in references/auth-license-cloud.md).
unity license activate   # reads UNITY_LICENSE_SERIAL from the step env, so the serial stays off argv; or: unity license activate --floating

# 2. Only if the job also runs cloud commands: export UNITY_SERVICE_ACCOUNT_ID and
#    UNITY_SERVICE_ACCOUNT_SECRET from the CI secret store. Every later command reads
#    them directly, with no `unity auth login` and nothing written to a credential
#    store (which a headless runner may not have).

# 3. Build
unity build /path/to/MyProject \
  --editor-version 6000.0.47f1 \
  --target StandaloneLinux64 \
  --execute-method Builder.PerformBuild \
  --allow-install
echo "Exit code: $?"

# 4. Return the license when done (serial or assigned seats)
unity license return --yes
```

### CI: batch-mode build

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

Writing scripts for a player build? Keep each `MonoBehaviour` alone in a `.cs` file named after its class. Several in one file can run fine in the Editor and Play Mode, yet the built player crashes at startup saying a scene file such as `level0` is corrupted. See [build-run-test.md](build-run-test.md#build).

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

`--report-format junit` makes `--output` a JUnit-schema report, which GitHub Actions and GitLab ingest as native test results with no converter step. It is written even when tests fail. Drop the flag for the NUnit3 default, or use `--report-format nunit,junit` to get both from one run. Add `--coverage` to collect coverage via the Unity Code Coverage package — it warns and carries on if the project doesn't have the package. See [build-run-test.md](build-run-test.md).

**Iterating on a change, not gating a pipeline?** Run the cheapest test that can fail for the change. Try EditMode first. Use `--filter`, `--affected` or `--rerun-failed` while you work. Run the full suite on the final code, once per mode, before you hand the work back. Never re-run a suite on code that has not changed, unless the run exited `6` with no verdict. Keep the NUnit default for these runs, because `--affected` and `--rerun-failed` cannot read the JUnit-only report the CI example above writes. [build-run-test.md](build-run-test.md#run-the-cheapest-test-that-proves-the-change) has the rule, and which report each flag reads and writes. `unity test` refuses a project that is open in an Editor: don't close the user's Editor to make room, and don't start a test run while they are in Play Mode.

### Debug the CLI

```bash
# Check auth + installed editors + recent errors in one command
unity doctor --format json

# Follow live logs during an install
unity logs --follow --level info
```

---

