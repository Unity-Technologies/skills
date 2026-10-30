# Projects, releases & templates — unity-cli command reference

Part of the **`unity-cli`** skill. See that skill's `SKILL.md` for CLI install, global flags,
environment variables, exit codes, and common workflows. All global flags (`--format json`,
`--non-interactive`, `--yes`, `--proxy`, …) apply to every command below.

Relative links in this file resolve to the skill directory's `references/` folder, so `../SKILL.md` is the skill's main page.

### Contents

- Projects — list, open, create, register, clone, link
- Assets: inspect or export a `.unitypackage`
- Assets — export a `.unitypackage`
- Assets — import a `.unitypackage` into a project
- Assets — export project assets to a `.unitypackage`
- Releases — browse Unity versions
- Templates

---

### Projects — list, open, create, register, clone, link

```bash
# List registered projects
unity projects --format json

# Opt extra columns into the listing (repeatable, human/TSV output)
unity projects --editor-version --modified --pipeline --cloud --vcs

# Register an existing project
unity projects add /path/to/MyProject

# Remove from registry (does not delete files)
unity projects remove /path/to/MyProject

# Show project details
unity projects info /path/to/MyProject --format json

# Open a project in the editor
unity open /path/to/MyProject

# Close the editor that has a project open (exits WITHOUT saving)
unity close /path/to/MyProject
unity close /path/to/MyProject --timeout 60
unity close /path/to/MyProject --force

# Same command, under the projects umbrella
unity projects close /path/to/MyProject
unity projects close /path/to/MyProject --timeout 60
unity projects close /path/to/MyProject --force

# Block until the Editor exits and report its real outcome — macOS/Linux only (exit 0 clean, 6 failed)
unity open /path/to/MyProject --wait

# Unattended open: accept the upgrade dialogs (upgrades the project, lets Unity rewrite scripts)
unity open /path/to/MyProject --accept-upgrades

# Return once the Editor is ready for commands; fail fast if a dialog stops it
unity open /path/to/MyProject --until-ready --timeout 600 --format json

# Start a headless batch-mode Editor and return once it is ready; stop it with unity close
unity open /path/to/MyProject --batchmode --until-ready --format json

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

**`open` on a project that is already open is not an error.** Before it launches anything, `unity open` and `unity projects open` check whether an Editor already holds the project, the check `unity run` and `unity test` refuse on, with a stale `EditorInstance.json` left by an exited Editor not counted. If one does, nothing launches and the command exits `0` with one line naming the project's version and the Editor's process id. Under `--format json` or `ndjson`, `data.alreadyOpen` is `true`, `data.pid` is the process id (`null` when only the project's lock file shows it is held), and `data.editorVersion` is the project's saved version. A launch's `data` has `launchedPid` and `editorPath` instead, so branch on `data.alreadyOpen`. The Editor's window is not brought forward, and `--wait` returns at once, because it waits for the Editor this command launched and there is none. Run `unity status --until-ready --project-path <project>` to wait for that Editor to be ready.

**`open --until-ready` waits for a ready Editor, and names the dialog that stops it.** A plain `unity open` returns right after the launch. With `--until-ready`, it keeps checking until the Editor's Pipeline server answers ready, the same check `unity status` makes, and no dialog is showing (exit 0). A modal can hold the Editor while its server still answers ready or stops answering, so the dialog check runs in those states too. It fails fast instead of hanging when a dialog stops the Editor, including dialogs that appear before the Pipeline server starts, which `unity status` also lists (as a row with no `port`). `--timeout <seconds>` sets the budget (default 300). The Editor keeps running whatever the outcome, waiting on whoever answers it. Under `--format json` or `ndjson`, `data.ready` carries `outcome` (`ready`, `dialog`, `exited`, `timeout`), `elapsedSeconds`, and either `dialog` (`title`, `kind`, `buttons`) or `lastSeen` (`noServer`, `starting`, `unreachable`). Read `errors[0].code`:

| Code | Exit | What to do |
|---|---|---|
| `OPEN_BLOCKED_BY_DIALOG` (and `OPEN_BLOCKED_BY_DIALOG_SAFE_MODE`, `_GRAPHICS_API`, `_SOFTWARE_TERMS`, the same codes `unity status` names with `STATUS_` for those three, so match the `OPEN_BLOCKED_BY_DIALOG` prefix) | 6 | `data.ready.dialog.kind` names the remedy. `projectUpgrade` and `scriptUpdating`: the project needs an upgrade for this Editor, so ask the user to answer the dialog, or to quit that Editor and reopen with `--accept-upgrades` (accepts the upgrade without asking) or run the `unity projects upgrade <project> --to <version>` command the message names. `safeMode`: the project has compile errors; fix them, then open it again. `softwareTerms`: the user must open this Editor once themselves and accept the terms. `graphicsApi` or `other`: ask the user to answer it in the Editor. |
| `OPEN_EDITOR_EXITED_BEFORE_READY` | 6 | The Editor quit while starting; its `Editor.log` says why. |
| `OPEN_READY_TIMEOUT` | 10 | Not ready in time, and still running. Check `unity status --project-path <project>`, or wait longer with a bigger `--timeout`. |

Dialogs are matched by their English titles, so an Editor in another UI language is only stopped on dialogs the Editor itself reports, and otherwise times out. On macOS, dialog titles in the window list need the Screen Recording permission for the terminal; without it, only the Project Upgrade Required dialog (which the Editor also writes to its log) is seen before the Pipeline server starts. `--timeout` without `--until-ready` is rejected (exit 2, `OPEN_TIMEOUT_REQUIRES_UNTIL_READY`), and so is `--until-ready` with `--wait` (exit 2, `OPEN_UNTIL_READY_CONFLICTS_WITH_WAIT`). An Editor that already holds the project is reported at once, as above, without waiting.

**`open --batchmode` starts a headless Editor.** It launches the Editor with `-batchmode`: no window, and it keeps running until `unity close <project>` stops it. On a project without the Pipeline package the Editor has no graceful channel to close it through, so use `unity close <project> --force` there. It refuses to open the project with another Editor version than the one it was saved with (exit 2, `OPEN_BATCHMODE_VERSION_CHANGE`), because a batch-mode Editor changes the project's version without showing the dialog a windowed open stops on. A `-batchmode` passed through `--args` gets the same check. Pass `--accept-upgrades` to allow the change. Add `--until-ready` to wait for its Pipeline server: the outcomes and codes are the ones in the table above. `unity status` lists the Editor with `batchmode: true` under `--format json` or `ndjson`, and a `Mode: batchmode` line in its human output. An Editor started with `-batchmode` some other way is marked the same.

**`open` never moves a project onto Unity 6.7 or later.** When the Editor that `unity open` or `unity projects open` would use is 6.7 or later and the project was saved with an older version, the command exits `2` with `PROJECT_UPGRADE_REQUIRED` before it installs or launches anything. That covers `--editor-version` (a partial version such as `6.7` included), the `unity <version> <project>` shorthand, and an Editor picked at the missing-Editor prompt. Use `unity projects upgrade <project> --to <version>` instead, which asks for the Unity Cloud link choice and, when the project ends up linked, turns on build profile diagnostics (see the upgrade section below). Under `--format json` or `ndjson` the refusal carries one `notifications` entry, `OPEN_UPGRADE_WITH_PROJECTS_UPGRADE`, whose `remediation.command` is that `projects upgrade` command with `requiresUserApproval: true`, so ask the user before running it. Opening a project already on 6.7 or later, with the same version, or with an Editor below 6.7 is unchanged.

**Extra listing columns are opt-in.** `projects` shows a compact set by default; `--editor-version`, `--modified` (last modified), `--cloud` (Unity Cloud project id), `--pipeline` (render pipeline), and `--vcs` (provider and repository) each add one to the human table. Piped TSV always prints all of them, as `name path version modified cloudProjectId pipeline vcs`, with empty cells for the columns you did not ask for, so a column's position never depends on the flags. They affect the human and TSV tables only — `--format json` and `ndjson` always carry the full record.

**Is Developer Data diagnostics on?** `projects info` answers from the project on disk, and `projects create` includes the same answer for the project it just created. New projects ship with diagnostics on, and turning it off disables all other Developer Data collection. In JSON, `data.diagnostics` has three parts:

- `buildDefault`: `state` is `enabled`, `disabled`, or `unknown`, and `source` names the file it was read from (`ProjectSettings/UnityConnectSettings.asset`). `unknown` always comes with a `reason`: `missing`, `unreadable`, `notSet` (older Editors don’t write the setting), or `unrecognized`. Never treat `unknown` as off.
- `buildProfiles`: each `.asset` in `searched` (`Assets/Settings/Build Profiles`) with its `state`: `enabled`, `disabled`, `inherit` (uses the build default), or `unknown`. Profiles saved elsewhere aren’t read. If the folder can’t be read safely (for example, it’s a symbolic link), `buildProfiles.reason` is set and the empty list means “not read,” not “no profiles.”
- `cloud`: `linked`, `cloudProjectId`, and `organizationId`.

**`projects info` takes the Unity Cloud link from the project's own file.** When the file says what the link is, `cloudProjectId`, `organizationId` and `data.diagnostics.cloud` come from `ProjectSettings/ProjectSettings.asset`, the file the Editor loads, and `genesisOrgId` is shown only when the two agree. `data.linkConsistency` says how the registry's record compares: `ok`, `registry-stale` (the registry lists a link the file doesn't have, or another one), `asset-only` (the file links a project and the registry's record is blank) or `unknown` (the registry lists a link, but the file is missing, unreadable, has no `cloudProjectId` line under its one `PlayerSettings` block, names `cloudProjectId` or `organizationId` twice, writes either one as anything but a one-line scalar entry (no space after the colon, a value that is not a scalar, a value that goes on past its line, a tab as indentation or a document marker anywhere), or is one git left conflicted, so nothing was checked and the registry's values are shown as they are). A row that holds an organization and no project also keeps its organization when the file links none. Anything but `ok` adds a plain warning to `warnings` (on stderr in human output): it says the registry and the file disagree and that the output follows the file, and names no command. For `unknown` it says the registry's link was not checked.

**`projects info` names the organization when you are signed in.** `organizationId` is whatever the file stores: a Genesis ID from the Hub or the CLI, or a cloud UUID or the organization's name from the Editor. Signed in, `projects info` looks it up once and adds `organizationName`, `organizationGenesisId` and `organizationUuid` right after it in JSON, and the human `Organization` row reads `Name (genesis / uuid)`. Signed out, offline, with no auth broker running (macOS and Linux), or with no match, the three fields are absent and the row shows the raw value. The lookup never starts the auth broker, and the exit code never changes.

This reports only what’s on disk. It can’t tell you whether Unity’s servers accept or receive the data.

**`unity close <project>` exits the editor WITHOUT saving.** It waits up to `--timeout <seconds>` (default 30) for a graceful exit. `--force` terminates the process (SIGTERM, then SIGKILL) when the editor offers no graceful channel, or once the wait runs out. Unsaved work is lost either way, so ask the user before running it. `unity projects close <project>` is the same command under the `projects` umbrella; pick whichever mount reads better in context. A windowed Editor that `unity open` started on a project without the Pipeline package has no graceful channel either: `close` leaves it running and exits 6 with `PROJECTS_CLOSE_PIPELINE_MISSING` (`reason: pipeline_missing` under `--format json`, an `Error:` line on stderr under the human and `tsv` formats). Run `unity pipeline install` before opening the project, or use `--force`; once the Editor has exited, the project is free for `unity build`.

**`--wait` — a real exit code from an interactive open.** By default `unity open`, `unity projects open` and `unity projects upgrade` return once the hand-off to the Editor completes, watching it only briefly for an instant failure. `--wait` blocks for as long as the Editor runs and exits `0` when it exits cleanly or `6` (`OPEN_EDITOR_EXITED`, or the licensing diagnosis for a 198) when it fails. The Editor runs in its own process group: Ctrl-C is absorbed, the wait always runs to completion, and the Editor is never touched. macOS and Linux only for now — Windows refuses `--wait` with exit `2` rather than falling back to the bounded watch. Without `--wait`, the CLI watches the Editor for only about 150 ms after launch, so an Editor killed by a signal is reported as a failure only when that happens inside the startup window; once the command has returned, nothing further can be reported. `--wait` is what covers the Editor’s whole lifetime, and it reports a signal death as a failure too. `projects create --open` does not take `--wait`.

**`--accept-upgrades` — answer yes to the Editor's upgrade dialogs.** `unity open` and `unity projects open` take it to pass `-skipUpgradeDialogs` to the Editor, which clears the "Project Upgrade Required" dialog and the API Updater's "Script Updating Consent" dialog that would otherwise leave an unattended open hanging. It says yes to both: the project is upgraded to the Editor's version and Unity rewrites scripts that use obsolete APIs, with no backup prompt. That changes the project's files, so it is never a default: pass it only when the user or the task has chosen it, and prefer a version-control checkpoint first. To upgrade as a deliberate step, use `unity projects upgrade` instead (`--to <version>`). `--accept-upgrades` does not move a project onto Unity 6.7 or later: opening a project saved with an older version in a 6.7+ Editor still exits `2` with `PROJECT_UPGRADE_REQUIRED`, because that move needs the Unity Cloud link choice that `unity projects upgrade` asks for. `unity projects upgrade`, `unity run` and `unity test` do not take the flag. It does not clear every dialog: on Windows an upgraded project can still show an "Auto Graphics API Notice". `unity status` reports it as `STATUS_BLOCKED_BY_DIALOG_GRAPHICS_API`; ask the user to choose Confirm in the Editor, since no launch flag answers it.

**First windowed open on a new machine: the Unity Editor Software Terms.** The Editor stops at its "Unity Editor Software Terms" dialog (Accept and Continue / Decline) on its first windowed open until the terms are accepted for that Editor. A headless open (`--non-interactive`, `UNITY_NON_INTERACTIVE`, a machine `--format`, redirected output, CI) still passes `-acceptSoftwareTermsForThisRunOnly`, but current Editors ignore that flag, so it does not prevent the dialog. `--args` cannot remove it, and passing it yourself does nothing. `unity status` reports the dialog as `STATUS_BLOCKED_BY_DIALOG_SOFTWARE_TERMS`, and `unity open --until-ready` as `OPEN_BLOCKED_BY_DIALOG_SOFTWARE_TERMS`. Accepting is the user's decision: ask them to accept once in the Editor, or to run `unity terms accept <version>` (it shows the terms and asks; see [editors-install.md](editors-install.md#software-terms--unity-terms)), which also prepares a container image or a provisioning script before the Editor first opens. Unity skips the dialog after that. Never click it for them, and never run `unity terms accept --yes` unless the user has agreed to the terms.

**Reserved flags — do NOT pass these via `--args`.** `-projectPath` is managed by the command (Unity's parser is last-wins, so forwarding it would silently redirect the open to a different project), and `-useHub`/`-hubIPC` are deliberately never passed — they tell the Editor a Unity Hub manages its session, which the CLI is not. Passing any of them fails fast, before launch, with exit code 6:

```
Error: Forwarded argument '-useHub' conflicts with a reserved Unity flag managed by this command. Remove it from `--args`.
```

All three spellings Unity accepts are rejected (`-useHub`, `--useHub`, `-useHub=1`, case-insensitively). Everything else — `-logFile <path>`, `-nographics`, custom flags your project reads — is forwarded verbatim.

#### projects create

Create a project. On a TTY, prompts for any missing options (parent directory, editor version, template) and then asks whether to link the project to a Unity Cloud project. That last question defaults to **Yes**, so pressing Enter links the project. Pass `--no-cloud` to decline without being asked, or answer `No` at the prompt to create an unlinked project instead. Piping stdin (a non-interactive terminal) suppresses every prompt and falls back to stored defaults, including the cloud default, so an unattended run still links unless `--no-cloud` (or `UNITY_NO_CLOUD`) says otherwise. On a real TTY, `--non-interactive` alone suppresses only the cloud question; the parent-directory, editor-version, and template prompts still fire (see below for the fully unattended recipe). The first positional argument is the project **name**; `--path` sets the parent directory:

```bash
unity projects create MyGame --editor-version 6000.0.47f1 --template com.unity.template.urp-blank

# Place the project in a specific directory
unity projects create MyGame --path /path/to/projects --editor-version 6000.0.47f1

# --template also accepts a .tgz file path or a directory, not just a registered template id
unity projects create MyGame --template /path/to/template.tgz
```

**The project folder must be new or empty.** `create` and `new` make `<path>/<name>`, or use it when it is an empty folder. A folder that holds anything (hidden files and `.git` included), a file at that path, or a folder that can't be read fails with `Project path already exists`, exit `6`, and nothing is written.

**Default template: URP.** Without `--template`, create uses the Editor version's own blank URP template, `com.unity.template.urp-blank`. `--template 3d` names the same template and `--template 2d` names `com.unity.template.universal-2d`. URP is the render pipeline going forward, so create never falls back to a Built-in Render Pipeline template. When the version doesn't offer the URP template, create fails with exit `6` and code `TEMPLATE_NOT_FOUND`, and `data.offered` lists the template ids it does offer; pass one of them with `--template`.

**Which Editor version create uses.** `--editor-version` wins; `latest` and `lts` there mean the newest official or LTS release in Unity's release list, not the newest installed Editor, and offline they fall back to the release list bundled with the CLI. Without it, create uses the default set with `unity editors default` (or the Hub's "set as default"), else the newest installed Editor, else, when no Editor is installed, the latest release. A version with no installed Editor fails with exit `6` and code `ENGINE_NOT_INSTALLED`, and `data.engineVersion` names it. Under `--format json` or `ndjson` the envelope's `notifications` then carries an `ENGINE_NOT_INSTALLED` entry whose `remediation.command` is the same `projects create` command with `--install-editor` added (a `--git-token` value shows as `<pat>`), with `requiresUserApproval: true`.

**Prerequisites, checked first.** Before create asks anything, downloads a template or starts the Editor, it checks the machine. An active Unity license is always required: with none, create fails with code `LICENSE_CHOICE_REQUIRED`, exit `4`. While the project links to Unity Cloud (the default, `--bare` included), create also needs you signed in (it fails with `NOT_SIGNED_IN`, exit `3`), the Unity services reachable (`NETWORK_UNREACHABLE`, exit `7`), and, when you belong to several organizations with no default, a chosen one (`ORGANIZATION_CHOICE_REQUIRED`, exit `4`; pass `--cloud-org`). `--no-cloud` removes the Unity Cloud prerequisites. Under `--format json` or `ndjson`, each unmet prerequisite is a `notifications` entry (`AUTH_REQUIRED`, `LICENSE_CHOICE_REQUIRED`, `ORGANIZATION_CHOICE_REQUIRED`, `NETWORK_UNREACHABLE`) whose `remediation.command` fixes it, with `requiresUserApproval: true`, so read them all before retrying. A state the CLI can't read (no licensing client running, for example) never blocks.

**Install a missing Editor with `--install-editor`.** When the version create resolved isn't installed, `--install-editor` installs it, waits for the install to finish, then creates the project. `--module <id...>` adds modules to that install and `--accept-eula` accepts their license agreements, as on `unity install`. They apply only when the run installs the Editor: for an Editor that's already installed, add modules with `unity install-modules`; either one without `--install-editor` is a usage error, exit `2`. Name, location, Unity Cloud and source-control checks still run before the install, so a bad flag fails in seconds, not after the download. A failed install creates no project and fails as `unity install` would. The envelope's `data.editorInstalled` says whether this run installed the Editor. The install takes minutes, so an agent runs the command in the background and keeps working:

```bash
unity projects create MyGame --install-editor --module android --accept-eula --no-cloud --format json
```

**Newer release notice.** When create picked the version from the default or the newest installed Editor and a newer official release exists, the json or ndjson envelope carries an `ENGINE_UPDATE_AVAILABLE` notification: `data.engineVersion` is the version used, `data.latestVersion` the newer one, and `data.resolvedFrom` is `default` or `installed`. Its `remediation.command` is `unity install latest`, or `unity install latest --set-default` when the stored default held the run back. When the stored default held it back but the latest release is already installed, the message says so and the remediation is `unity editors default latest` alone. Both remediations have `requiresUserApproval: true`, so ask the user before running one. The check is best effort: with the release list unreachable there's no notice, and create carries on. There's none when `--editor-version` was passed.

**Cloud linking during creation:**

```bash
# Create and link a NEW Unity Cloud project as part of creation
unity projects create MyGame --cloud --cloud-org <id-or-name>

# Link an EXISTING cloud project instead
unity projects create MyGame --cloud-project <id-or-name>

# Create without Unity Cloud, and without being asked about it
unity projects create MyGame --no-cloud
```

`--no-cloud` is the explicit negative answer: it creates the project unlinked and skips the cloud question. Reach for it in a scripted run on a terminal, where the question would otherwise stop the command.

**`projects create` links to Unity Cloud by DEFAULT, and not being able to ask does not change that.** Passing any of `--cloud`, `--cloud-project`, `--coppa`, `--no-cloud`, or `--cloud-org` answers the cloud question up front, so it is not asked. The question is also skipped in every machine output mode (`--json`, `--format tsv|ndjson`, `--quiet`), under `--non-interactive` (or `UNITY_NON_INTERACTIVE`), and when stdout is not a TTY — but a run that cannot show the prompt still **links**, because the default answer does not depend on whether anyone is watching. `--no-cloud` is the only way to decline without being asked.

That makes authentication a **precondition** of `projects create`, not a capability it discovers late: without `--no-cloud`, a signed-out run fails with `NOT_SIGNED_IN` and a service-account run with `CLOUD_PROJECT_REQUIRES_OAUTH`, both exit `3`, before any scaffolding happens. So in CI, pass `--no-cloud` unless you have an interactive (OAuth) session and actually want the link.

Unlike the other three questions, the cloud one can fire even when every option was supplied on the command line, so `--non-interactive` is what keeps a fully-specified scripted run from stopping on it. Be aware that the global currently gates **only** this question: the parent-directory, editor-version, and template questions still gate on terminal interactivity alone, so `--non-interactive` on a TTY does not make them fall back to stored defaults. For a fully unattended run on a terminal, pass `--path`, `--editor-version`, and `--template` as well.

Once the command is past that precondition, a link that then fails never costs you the project: if it cannot be set up (expired session, no resolvable organization), the project is still created unlinked and the reason is reported as a warning, exit 0. `--cloud` behaves differently and still fails outright — an explicit flag is a contract, not a suggestion.

When a project is created without a cloud link, human output ends with a line pointing at `unity projects link cloud`. It is human-format only: `json`, `ndjson`, and `tsv` output is unchanged.

For machine consumers, cloud state is reported by the presence of the `cloudLinked`, `cloudProject`, and `cloudOrgSource` fields on the result payload — they are emitted **only when a cloud link was requested**. Their absence is itself the signal that the project is unlinked; do not read `cloudLinked` expecting a `false`.

**The Pipeline package is part of every create.** `unity projects create MyGame` adds `com.unity.pipeline` (latest from the Unity registry) to the new project's `Packages/manifest.json` right after scaffolding, before any `--vcs` initial commit and before `--open`, so the Editor resolves it on first open and `unity command` works without a separate `unity pipeline install`. `--bare` is the only way to leave it out. A template that already ships the package is left as it is. It never fails the create: an unreachable registry or a manifest it cannot write leaves the project created without the package, prints a warning (stderr in every format but json), and exits 0, and `unity pipeline install --project-path <path>` retries. An engine version older than Unity 6, which the package does not support, is not attempted: nothing is added or printed. Unity 6000.5.0 to 6000.5.2 is not attempted either: the package can't start its server on those Editors, so create leaves it out (the `pipeline` block carries `skipped: "UNSUPPORTED_VERSION"`) and says to update the Editor to 6000.5.3f1 or later, open the project in it, and run `unity pipeline install --project-path <path>`. In json and ndjson that is a `PIPELINE_EDITOR_UNSUPPORTED` notification with `data.engineVersion`, `data.fixedIn` and the remediation `unity install latest`; in the other formats it is a warning on stderr. A template that already ships the package keeps it, and the notice is raised all the same. The result payload carries `pipeline`: `{ "requested": true, "installed": true, "packageId": "com.unity.pipeline", "version": "<v>", "alreadyInstalled": false }` on success (`alreadyInstalled: true` when the template already shipped it), `installed: false` with `error` set to `PIPELINE_INSTALL_FAILED` on a failure, or `installed: false` with `skipped: "UNSUPPORTED_VERSION"` on an older engine. `--with-pipeline`, which used to opt in, is still accepted and does nothing.

**Anonymous player sign-in is part of every linked create.** When the cloud link succeeds, create adds `com.unity.services.authentication` (the newest release that supports the project's engine version) to `Packages/manifest.json` and writes `Assets/PlayerAuthentication/` (`PlayerAuthentication.cs` and its own assembly definition), which signs the player in anonymously before the first scene loads, so cloud save, telemetry and other services have a player ID. Await `PlayerAuthentication.SignedIn` before calling another Unity Gaming Service: it is `true` once the player is signed in and `false` when sign-in failed. Code in an assembly definition adds `PlayerAuthentication` to that asmdef's `references` first; code in the default `Assembly-CSharp` sees it as is. Nothing is configured server-side: anonymous sign-in works for any linked cloud project. A template that already pins the package and has a runtime script (not in an `Editor` folder) whose code calls `SignInAnonymouslyAsync` is left as it is. With `--no-cloud`, or when the link failed, nothing is added. Like the Pipeline package it never fails the create: a failure prints a warning and the payload's `playerAuth` block carries `error: "PLAYER_AUTH_SETUP_FAILED"`. `--bare` leaves it out.

**Cinemachine is part of every 3D create.** Create adds `com.unity.cinemachine` (the newest release that supports the project's engine version) to `Packages/manifest.json`, so the camera rigs are there on first open. A 2D project does not get it: create reads the mode from `m_DefaultBehaviorMode` in `ProjectSettings/EditorSettings.asset`, or from a `2d` word in the template name when the file has no such key, and the step is skipped with `NOT_APPLICABLE`. A template that already ships the package, 2D or not, is left as it is. With no release for the engine version it is skipped with `UNSUPPORTED_VERSION`. The payload's `cinemachine` block has the same shape as `playerAuth`, and a failure carries `error: "CINEMACHINE_SETUP_FAILED"`.

**Play mode keeps running in the background.** Create sets `runInBackground: 1` in `ProjectSettings/ProjectSettings.asset`, so Play mode keeps ticking while the Editor is not focused, as when an agent drives it from a terminal. Only that one value changes; the rest of the file stays byte for byte as the Editor wrote it. A file that cannot be read, or that does not have exactly one `runInBackground` line set to `0` or `1`, is left as it is with a warning. The payload's `runInBackground` block is `{ "enabled": true, "alreadySet": false }` when create turned it on (`alreadySet: true` when the template already had it), or `{ "enabled": false, "error": "RUN_IN_BACKGROUND_FAILED" }`.

None of the scaffolding steps writes a cloud project id, an organization id or a project name. A cloud-linked create does write them (`cloudProjectId`, `organizationId`, `projectName`) to `ProjectSettings/ProjectSettings.asset`, and `unity templates create --keep-project-settings` preserves that file as is, so a template saved from a cloud-linked project that way carries the cloud linkage. Without the flag, or from a project created with `--no-cloud`, the template carries no account.

**A project without the default scaffolding (`--bare`).** `unity projects create MyGame --bare` creates the project from the template without the default project scaffolding; today that is the Pipeline package, Cinemachine, Run In Background and anonymous player sign-in. The Unity Cloud link and source control are not scaffolding: they behave as without the flag, so the cloud link (and its sign-in) still happens unless you pass `--no-cloud`, and `--vcs`, `--cloud`, `--cloud-org`, `--cloud-project` and `--coppa` all work beside `--bare`. Only `--with-pipeline` is refused beside it, with exit `2` and `BARE_ARGS_CONFLICT`. `--bare --no-cloud` creates a project from the template alone, offline. To drive a bare project from an agent, run `unity pipeline install --project-path <path>` afterwards.

**What create did (`steps`).** The result payload carries `steps`, each setup step create ran in order: the scaffolding steps and the links (`cloudLink`, `vcs`). Each has an `id` and a `status`: `done`, `present` (the template already had it, so nothing was changed), `skipped` with a `reason` (`BARE` for scaffolding under `--bare`, `UNSUPPORTED_VERSION` for an engine version a step does not support, `NOT_APPLICABLE` for a step that has no place in the project, such as Cinemachine in a 2D one, `NOT_REQUESTED`, `NO_CLOUD` for the cloud link and player sign-in, or `CLOUD_LINK_FAILED` for player sign-in or a UVCS repository the failed cloud link kept from being created), or `failed` with an `error` code: the step block's own code where it has one (`PIPELINE_INSTALL_FAILED`, `CINEMACHINE_SETUP_FAILED`, `RUN_IN_BACKGROUND_FAILED`, `PLAYER_AUTH_SETUP_FAILED`, a `cloudLinkError` code, or a `uvcsError` code other than `UVCS_SKIPPED_CLOUD_FAILED`), otherwise `CLOUD_LINK_FAILED`, `VCS_LINK_FAILED` or `UVCS_LINK_FAILED`. A failed step does not fail the create: the command still exits `0`, as it does today, and the project is left usable. Read `steps` rather than the individual blocks to tell what was set up; the list of steps grows as more of the scaffolding moves into create.

When a link was requested, `cloudLinked: false` next to a `cloudProject` means the Unity Cloud project exists but the link could not be saved to the project's `ProjectSettings/ProjectSettings.asset`, so the Editor sees no link. `cloudLinkError.code` says why (`PROJECT_SETTINGS_MISSING`, `PROJECT_SETTINGS_LINK_FIELDS_MISSING` when the file has no `cloudProjectId`/`organizationId` entries, `PROJECT_NOT_TRACKED`, `CLOUD_PROJECT_ID_INVALID`, or `LOCAL_WRITE_FAILED`), and a warning names the fix: `unity projects link cloud <path> --cloud-project <id>`, which saves the link to that project rather than creating another one. `unity projects link cloud` itself fails with `PROJECTS_LINK_CLOUD_LOCAL_WRITE_FAILED` (exit `1`) in the same situation.

**COPPA declaration.** Creating a Unity Cloud project declares its COPPA status, and the CLI sends `not_compliant` unless told otherwise — the same value the Hub sends. `--coppa <status>` sets it on every command that can create a cloud project (`projects create`, `projects link cloud`, `projects link vcs`, `vcs setup`); the accepted values are `not_compliant` (default), `compliant`, and `unspecified`, and anything else is a usage error (exit `2`). Pass `--coppa compliant` when the game is directed at children under 13. Whenever a run creates the project, the result payload carries `coppa` with the value sent (beside `cloudProject` on `create` / `link cloud`, beside `linked` on `link vcs` / `vcs setup`) and human output names it. **Linking an existing project never changes its COPPA setting**: `--coppa` together with `--cloud-project` (or `--no-cloud`) is refused with `COPPA_ARGS_CONFLICT`, exit `2`, and a run that links an existing project or finds one already linked sends nothing and reports no `coppa`. When `--coppa` was given but the run created no cloud project (a git `--vcs` provider, or a project that was already cloud-linked), a warning on stderr says it was not applied; in JSON the absent `coppa` field is the signal. Change an existing project’s COPPA setting in the Unity Cloud dashboard instead. On `create`, `--coppa` alone also counts as asking for a cloud project, like `--cloud`.

**Source-control during creation** — publish the new project to a fresh repository:

```bash
unity projects create MyGame \
  --vcs github \
  --git-namespace my-org \
  --git-repo my-game \
  --git-visibility private \
  --git-default-branch main \
  --git-token-from-stdin
```

Source-control flags (shared with `projects link vcs`): `--vcs github|gitlab|uvcs|<host>`, `--git-namespace <name>`, `--git-repo <name>`, `--git-visibility private|public|internal` (default private), `--git-default-branch <name>`, `--git-remote-protocol https|ssh` (default https), `--git-description <text>`, `--git-token <pat>` / `--git-token-from-stdin`, `--no-initial-commit`, `--git-lfs`, and `--vcs-region <name>` for Unity Version Control.

**Flag names differ by subcommand:** `projects create` and `projects link vcs` use `--git-namespace` / `--git-repo`, while `projects clone` (below) uses `--vcs-namespace` / `--vcs-repo`. Copy the names for the exact command you're running, and confirm with `--help` if unsure.

**`--git-remote-protocol ssh` attaches the created repository's SSH remote instead of its HTTPS one.** The provider REST call that creates the repository still needs the resolved PAT — SSH has no equivalent for that API call — but the local `origin` remote and the initial push then use the repository's `git@<host>:<owner>/<repo>.git` form with pure ambient SSH auth (the running ssh-agent, a repository-local `core.sshCommand`, or an `~/.ssh/config` host alias — whichever the machine already has configured; the CLI never handles keys itself). Passing `--git-token`/`--git-token-from-stdin` alongside `--git-remote-protocol ssh` is fine: the token still authenticates the repository-creation API call, it's only the git transport that switches.

**Self-hosted hosts create through a provider CLI, not REST.** `--vcs` also accepts a bare host (e.g. `--vcs gitea.example.com`) for anything other than github.com/gitlab.com — there is no built-in REST client for those, so the repository is created through `gh`, `glab`, or `tea`, whichever is installed and already signed in for that host (checked in that order). The same fallback covers github/gitlab themselves when no REST token can be resolved: the matching CLI (`gh` for github, `glab` for gitlab) steps in if it is signed in, before the command gives up. No PAT is ever read, stored, or forwarded on a provider-CLI path. When a provider CLI created the repository, the machine-readable result carries `vcs.mechanism` (`gh` | `glab` | `tea`) naming which one — omitted for the REST path and for a `[url]`-form link, so existing scripted consumers see no change on github.com/gitlab.com. If nothing can create it — no REST token and no signed-in provider CLI for that host — the error explains creating the repository yourself and linking it with `unity projects link vcs <path> <url>`.

**Where the Git token comes from.** Resolution order, first hit wins: `--git-token-from-stdin` → `--git-token` → `UNITY_GITHUB_TOKEN` / `UNITY_GITLAB_TOKEN` → `git credential fill` (the user's credential helper) → an interactive masked prompt. The first three are explicit per-command overrides and always beat the helper. **The CLI stores no Git token** at any tier, including one typed at the prompt.

**Whether anyone is there to answer is decided up front.** On a real terminal, a configured credential helper is free to run its own sign-in — including Git Credential Manager's browser and device-code flows — and its instructions are relayed to you rather than swallowed. Without a terminal, in CI, under a machine-readable `--format`, or with `--non-interactive`, nothing prompts at all: the command fails immediately with **exit 4**, naming the credential it needed and how to supply it out of band (`--git-token[-stdin]` or the env vars above). Interaction is judged on all three standard streams, so redirecting stderr alone can no longer leave a password prompt writing into a file while waiting on a keystroke you were never shown.

The credential lookup passes the full repository URL, so a helper that keeps one account per URL can return a different token per organization, but only when `credential.useHttpPath` is set, since git otherwise withholds the path from helpers:

```bash
git config --global credential.useHttpPath true
```

Per-project identity instead lives in the repository's own config (`credential.useHttpPath`, `credential.username` in its `.git/config`); `projects link vcs` runs the lookup inside the project, so the Hub, the CLI, and plain `git` all resolve the same credential. For a CI pipeline spanning several organizations, pass `--git-token-from-stdin` per invocation: the env vars hold one token per provider, and there is no per-org variant.

Per-organization scoping needs the organization to be known before the credential is looked up. `projects clone` always requires `--vcs-namespace`, so it is always scoped. `projects create` and `projects link vcs` take `--git-namespace`, and when it is omitted the lookup stays host-scoped, because the namespace cannot be resolved until you are authenticated and you cannot authenticate without a credential. Pass `--git-namespace` to target a specific organization's credential.

#### projects clone

Clone a remote repository and register the Unity project it contains. Works across providers:

```bash
# Clone by provider + namespace + repo
unity projects clone --vcs github --vcs-namespace my-org --vcs-repo my-game --path ./MyGame

# Check out a specific ref (branch, sha, or UVCS changeset)
unity projects clone --vcs uvcs --vcs-namespace my-org --vcs-repo my-game --ref main

# Authenticate with a personal access token (prefer stdin)
unity projects clone --vcs gitlab --vcs-namespace my-org --vcs-repo my-game --git-token-from-stdin

# Project lives in a subdirectory of the repo
unity projects clone --vcs github --vcs-namespace my-org --vcs-repo monorepo \
  --path ./repo --subpath packages/MyGame

# Clone an arbitrary git URL instead (HTTPS, or SSH via the standard
# git user@host:path shorthand) — no --vcs/--vcs-namespace/--vcs-repo needed
unity projects clone https://github.com/my-org/my-game.git
unity projects clone --ref develop <ssh-clone-url-for-your-host>
```

Options: `--vcs github|gitlab|uvcs`, `--vcs-namespace <name>`, `--vcs-repo <name>`, `--ref <branch|sha|changeset>` (an all-digit ref is treated as a Unity Version Control changeset, anything else as a branch), `--path <dest>` (clone destination), `--subpath <subpath>` (the project's folder, when the repository holds more than one), `--git-token <pat>` / `--git-token-from-stdin`, `--json`. Git LFS assets are fetched as pointer files only.

**The `[url]` form is an alternative to `--vcs`/`--vcs-namespace`/`--vcs-repo`, not an addition to them** — passing a URL alongside any of those three flags is a bad-arguments error. `--ref`, `--path`, `--subpath`, `--no-lfs` always apply. `--git-token`/`--git-token-from-stdin` only apply when the URL is an **explicit HTTPS** URL whose host is github.com or gitlab.com — passing a token for any other host (an SSH-form or SCP-style URL, or a host that isn't github.com/gitlab.com) is a bad-arguments error, since there's nowhere for that credential to go. No token is required: with none supplied, the clone runs with whatever git auth is already set up on the machine (SSH agent, a configured credential helper, `.netrc`, or userinfo embedded in the URL itself) — this includes every SSH-form clone, even against github.com/gitlab.com, since the provider credential helper is HTTPS-only. Unity-project detection is **always** a post-clone scan of the downloaded tree for the URL form (never a provider API call, regardless of host or token) — only Git LFS credentials differ by tier: an HTTPS URL to github.com/gitlab.com with a token uses the provider-scoped LFS credential helper, everything else uses the machine's own git auth for the LFS pull too. A malformed URL, an unreachable host, a rejected/unknown SSH host key, and an authentication failure are reported as distinct errors (exit 2, 7, 3, and 3 respectively).

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
unity vcs uvcs reviews --branch /main --format json

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

**They need a signed-in session, and the three auth-shaped failures mean different things.**
`unity auth login` is all you have to do; the short-lived gateway token these commands
authenticate with is obtained for you. `NOT_SIGNED_IN` and `SESSION_EXPIRED` (both exit 3) mean
sign in again. `UVCS_TOKEN_UNAVAILABLE` (exit 6) means that token could not be obtained at all —
a connectivity or service problem, not a credential one, so re-running sign-in will not help.
`VCS_UVCS_REVIEW_REQUIRE_AUTH` (exit 3) is the reviews service itself refusing a credential the
CLI did obtain. Do not treat them as one condition: only the first two are worth a login retry.

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

The CLI reads and stores no token on any of these paths. It asks each installed provider CLI whether it holds a session for that specific host, with credential-bearing environment variables stripped from the child process: `gh` otherwise applies `GH_ENTERPRISE_TOKEN` to any non-cloud host and dials it to validate, which would leak the token to a host that merely appeared in a failing URL. Stripped, `gh` answers from its own config: an unconfigured host is never contacted. Side effect: authenticating purely via `GH_ENTERPRISE_TOKEN` (no `gh auth login` entry) reads as signed out, so you may be offered a sign-in you do not need. A host with no provider CLI available is not a dead end: plain git still clones and pushes, with credentials from the credential manager or the SSH agent. Note `--git-token`/`--git-token-from-stdin` still only apply to github.com and gitlab.com over HTTPS; for any other host, rely on one of the three paths above.

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

#### projects report

Group every registered project by the editor version its `ProjectVersion.txt`
asks for, flagging a version that isn't installed and a version whose stream
isn't LTS:

```bash
unity projects report
unity projects report --format json
```

Read-only: it never installs, removes, or changes anything. It pairs with
two follow-up commands rather than doing their job itself: `unity projects
require <path>` installs a missing version, and `unity editors prune` reviews
installed editors no registered project asks for. The LTS/non-LTS distinction
is the offline classification the installed `lts` upgrade alias already uses
(no network call); this codebase has no end-of-support date for any stream,
so the report never claims one, a non-LTS flag is the whole signal.

#### projects upgrade

Upgrade a project to a different Unity editor version. `--to` is required:

```bash
unity projects upgrade --to 6000.0.47f1
unity projects upgrade /path/to/MyProject --to 6000.0.47f1 --yes
```

**Into Unity 6.7 or later, an unlinked project needs a Unity Cloud choice.** Unity 6.7 turns
diagnostics on by default for projects linked to Unity Cloud, so the upgrade takes one of:

| Option | Effect |
|---|---|
| `--cloud` | Create a new Unity Cloud project and link it (`--coppa <status>` declares its COPPA status) |
| `--cloud-project <id-or-name>` | Link an existing Unity Cloud project |
| `--no-cloud` (or `UNITY_NO_CLOUD`) | Upgrade without linking |
| `--cloud-org <id-or-name>` | The organization for `--cloud` or `--cloud-project`, as on `projects create` |

Without a choice the command exits `2` with `CLOUD_CHOICE_REQUIRED` before installing or opening
anything. Under `--format json` or `ndjson` it lists one `notifications` entry per choice, each
with a `remediation.command` that repeats the invocation with that flag and
`requiresUserApproval: true`, so ask the user which one to run. The existing-project entry's
command contains the literal placeholder `<id-or-name>` (also in its `data.placeholder`): replace
it with the project's ID or name, quoted for the shell (a name can hold spaces or shell
metacharacters), before running it, because a shell reads an unquoted `<...>` as an input
redirect. A project already linked needs no choice; `--cloud` or `--cloud-project` on a
linked project, or on an upgrade below 6.7, is reported in `warnings` as not applied. When the
project ends the upgrade linked (already linked, or linked by `--cloud` / `--cloud-project`), the
upgrade also sets each build profile whose diagnostics setting was the project default to enabled;
`data.diagnostics.buildProfiles.profiles` lists every profile with its resulting `state` and
whether it was `updated`. On a project that isn't linked, `--no-cloud` leaves every build profile
as it was and reports no `data.diagnostics`, because diagnostics only defaults on for linked
projects. On a project that is already linked, `--no-cloud` keeps the link and the profiles are
still enabled. `data.cloud.outcome`
reports the link (`alreadyLinked`, `linkedNew`, `linkedExisting`, `notLinked`).

```bash
unity projects upgrade ./MyGame --to 6000.7.0f1 --yes --cloud
unity projects upgrade ./MyGame --to 6000.7.0f1 --yes --no-cloud --json
```

#### projects audit

`unity projects audit` gets Project Auditor ready in the project's running Editor and opens its window on one workflow's page, so the user can run the analysis from there. It never runs the analysis itself. The project must be open in a Unity 6 or later Editor with `com.unity.pipeline` installed.

<!-- unity-example: skip (Rust-only) -->
```bash
# Open Project Auditor on the Upgrade page (the default)
unity projects audit

# Open it on another workflow's page: upgrade, optimization, urp or coreclr
unity projects audit --workflow urp

# Open a saved report on that page
unity projects audit --report ./Library/projectauditor-report-autosave.projectauditor

# The Editor that has a given project open, with a longer wait for the install
unity projects audit --project-path ./MyGame --timeout 3600 --json
```

- **Packages first.** In Unity 6.0 to 6.3 Project Auditor is the `com.unity.project-auditor` package; in later Editors it is built in. The command adds or updates that package (when needed) and `com.unity.project-auditor-rules` to the newest versions the Editor supports, then waits for the Editor to reload. A package that is embedded, local or from git is left alone. When the registry can't be read it warns and lets the Package Manager pick a version.
- **The window.** It opens and focuses the Project Auditor window on the workflow's page. When the page can't be reached without a report, the window still opens and the result carries a `note` saying why: `{code, detail}` with code `pageUnavailable`, `viewUnavailable` or `failed` (`detail` holds the Editor's message).
- **Exit codes.** `0` once the window is open. `2` for a bad option or a `--report` file that doesn't exist (`PROJECTS_AUDIT_REPORT_NOT_FOUND`). `10` when Project Auditor did not load within `--timeout` seconds (default 1800, `PROJECTS_AUDIT_TIMED_OUT`). `6` for an Editor older than Unity 6 (`PROJECTS_AUDIT_EDITOR_TOO_OLD`), a failed install (`PROJECTS_AUDIT_INSTALL_FAILED`), scripts that don't compile (`PROJECTS_AUDIT_COMPILE_FAILED`), Project Auditor not loading (`PROJECTS_AUDIT_NOT_LOADED`) or a step the Editor could not run (`PROJECTS_AUDIT_SCRIPT_FAILED`). No running Editor gives the same error as the other commands that drive the Editor through Pipeline.
- **`--json` data:** `unityVersion`, `projectPath`, `projectAuditor` (`builtIn`, `assembly`, `packageVersion`, `rulesVersion`), `packagesAdded`, `workflow`, `reportPath` (`null` without `--report`) and `window` (`windowAssembly`, `reportLoaded` (`null` when Project Auditor can't tell), `reportIssue`, `navigated`, `note`). A report the window would refuse is not handed to it, so no dialog opens: `reportIssue` says why (`incompatible` for another Project Auditor version or an unreadable file, `empty`, `otherProject`) and `warnings` carries the same sentence the human output prints.

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
# ...to a specific existing Unity Cloud project, by id or name
unity projects link cloud /path/to/MyProject --cloud-project <id-or-name>
# Switch an already linked project to another Unity Cloud project, in one step (no unlink first)
unity projects link cloud /path/to/MyProject --cloud-project <id> --replace
# Disconnect from its Unity Cloud project (always name the path: UNITY_PROJECT_PATH is not read here)
unity projects unlink cloud /path/to/MyProject
# Also remove the Unity Version Control workspace bound to this project
unity projects unlink cloud /path/to/MyProject --cascade-vcs
# ...when that workspace is SHARED (rooted above the project), confirm it explicitly
unity projects unlink cloud /path/to/MyProject --cascade-vcs --unlink-workspace

# --- Version-control links ---
# Publish a local project to a NEW GitHub / GitLab / Unity Version Control repository
unity projects link vcs /path/to/MyProject \
  --vcs github --git-namespace my-org --git-repo my-game --git-token-from-stdin
# Attach to an ALREADY-EXISTING remote instead of creating one — pass its URL
unity projects link vcs /path/to/MyProject https://github.com/my-org/my-game.git
# Undo a half-finished link instead of resuming it
unity projects link vcs /path/to/MyProject --rollback
# ...and delete the repository it created, if nothing has been pushed to it yet
unity projects link vcs /path/to/MyProject --rollback --delete-remote
# Remove a project's git remotes (the remote repositories are NOT deleted)
unity projects unlink vcs /path/to/MyProject
# Also detach the Unity Version Control workspace
unity projects unlink vcs /path/to/MyProject --unlink-workspace
```

With piped or redirected stdout and no `--format` (so `tsv`), `link cloud` and `unlink cloud` print one row with six tab-separated columns: result (`linked`, `already-linked`, `unlinked`, `not-linked`), project path, cloud project ID, project name, organization ID, and `true`/`false` for whether a cloud project was created. For `link cloud` the name is the cloud project's. `unlink cloud` reports the link the project held before it was removed, with the name saved alongside it (the local project's title when the CLI made the link), and `not-linked` leaves the three cloud columns empty. For `link cloud`, `--format json` has the full payload (`cloudProject`, `cloudOrgSource`, `coppa`). `unlink cloud` JSON reports only `path` and `unlinked`, so read the removed link from its TSV row.

`link vcs` shares the source-control flag set documented under `projects create`. `link cloud` / `link vcs` accept `--cloud-org <id-or-name>` (env `UNITY_CLOUD_ORG`), and `--coppa <status>` for the cloud project they create (see the COPPA declaration note under `projects create`).

**`UNITY_NO_CLOUD` is the environment form of `--no-cloud`.** Any value counts, including `0`. It declines the cloud link on `projects create` and `projects upgrade` without editing every invocation in a pipeline. On `projects create` and `projects upgrade` it yields to a command that names a cloud flag (`--cloud`, `--cloud-project`, `--cloud-org`, `--coppa`), which then requests the cloud project, while `--no-cloud` on the same command line as one of those is refused. `projects create --vcs uvcs` is refused when the variable is set and no cloud flag overrides it, because UVCS needs a cloud project. `projects create` never reads the saved default project: it creates one unless `--cloud-project` names it.

**`link cloud` links an existing Unity Cloud project when one is named, and creates one only when none is.** The project comes from `--cloud-project <id-or-name>`, then `UNITY_CLOUD_PROJECT` (an id or a name), then the saved default project of the active organization (`unity cloud project set-default`), and only when none of them names a project does it create one, named after the project's folder. Every mode follows that order. On a terminal, a run with no `--cloud-project` that found a project asks whether to use it or create a new one, with the project the other modes would use preselected, so Enter does what a script would do; `--non-interactive`, `--quiet` and a machine format never ask. A project that the variable or the default names but the organization does not list fails with exit `6` and a message naming the setting, and never falls through to creating one. A saved default that is not a usable project id (a name, a number, an empty or missing value) fails the same way, and so does a `user-settings.json` the CLI cannot read (`SETTINGS_STORE_UNREADABLE` or `SETTINGS_STORE_DUPLICATE_KEYS`), because a default it cannot see might be the project you meant. A `link cloud` run on a project that is already linked fails with `ALREADY_LINKED` (exit `2`) unless `--cloud-project` names the project it is linked to (a no-op success) or `--replace` is passed.

**Switch with `--replace`, not unlink then link.** `ALREADY_LINKED` names the current link (with the project and organization names when a signed-in session can look them up; otherwise the ids) and ends with the one command that switches it: `unity projects link cloud <path> --cloud-project <id> --replace`, with the id the run resolved, or the `<cloud-project-id>` placeholder to fill in. JSON carries it as `data.suggestedCommand`, beside `data.currentLink` (`cloudProjectId`, `organizationId`, `genesisOrgId`, `cloudProjectName`, `organizationName`; names are `null` when unknown). `--replace` finds its target as a plain `link cloud` does (`--cloud-project`, then `UNITY_CLOUD_PROJECT`, then the organization's saved default project) but never creates a cloud project and never asks which project to use, even on a terminal: when none of them names one it fails with `REPLACE_NEEDS_CLOUD_PROJECT` (exit `2`; offline when nothing could, otherwise after the organization lookup and before any create), and a named project that doesn't exist stays not found. It writes the new link over the old one in both `ProjectSettings.asset` and the Hub's project list, with no unlink in between. Human output adds a line naming the link it replaced; JSON adds `data.previousLink` (`cloudProjectId`, `organizationId`, `genesisOrgId`). Naming the project it is already linked to is still a no-op success, and `--replace` on a project with no link links it as usual. Before writing anything, `--replace` refuses a project with a Unity Version Control workspace (`VCS_DEPENDS_ON_CLOUD`, exit `2`: run `unity projects unlink vcs` first) and a `ProjectSettings.asset` that can't take the new link, being read-only or having no Unity Cloud fields (`PROJECTS_LINK_CLOUD_LOCAL_WRITE_FAILED`, exit `6`, nothing changed).

**Creating is never silent.** Human output says `Created Unity Cloud project "<name>" (id <id>) in organization <genesis id> and linked it.`, and a run that linked an existing project says `Linked Unity Cloud project ...` and what chose it. `--format json` and `ndjson` report `created` (`true` or `false`) in `data`, beside `cloudProject` with the id, name and organization ids, and `coppa` when a project was created. `--format tsv`, the default when stdout is redirected, keeps its one `path` row on stdout and prints the same sentence on stderr when a project was created, and so does human output under `--quiet`.

**A taken name stops the run.** When the folder's name is already used by a project in the organization, nothing is created and the command fails with `CLOUD_PROJECT_NAME_TAKEN` (exit `6`). The message names the project that holds the name and the exact command that links it, `unity projects link cloud <path> --cloud-project <id>`, and in JSON `data.cloudProject` is that project. The name is never retried with a timestamp, and an existing project is never picked by name for you, because other people's projects share the organization. `--coppa` applies only when a project is created: with a project named by the variable or the default it is not applied, and a warning says so (`--coppa` beside `--cloud-project` is still refused with `COPPA_ARGS_CONFLICT`). `projects create` is unchanged: it always creates a new project unless `--cloud-project` is passed, and does not read `UNITY_CLOUD_PROJECT` or the saved default.

**Close the Editor before relinking.** An open Editor reads the cloud link only when it opens the project, so after `link cloud` or `unlink cloud` it keeps the old link, and its next settings save can write that link back over the change. When an Editor has the project open, both commands still succeed but warn (stderr; `warnings` in `json`/`ndjson`) and add `data.editor: {"open": true, "updated": false, "restartRequired": true}`. Save your work and run `unity close <path>`, then reopen the project. With no Editor open, `data.editor` is absent.

**`--cascade-vcs` alone will not remove a SHARED Unity Version Control workspace.** When the recorded workspace is rooted in a directory ABOVE the project — so sibling projects may live in it too — removing it affects all of them. On a terminal the command asks; in any non-prompting context (a machine format, `--non-interactive`, redirected stdout) it refuses with `UVCS_WORKSPACE_SCOPED` and exit `2` rather than quietly wiping a shared workspace in CI. Add `--unlink-workspace` alongside `--cascade-vcs` to confirm, which is exactly what the error message itself says.

**A link that failed part-way is resumed by default; `--rollback` unwinds it instead.** `--delete-remote` extends a rollback to the repository the command created, and only when that repository still has no commits — so a rollback can never discard work somebody has already pushed. It does nothing without `--rollback`.

The `[url]` second operand attaches to a remote that already exists, instead of creating one — the one thing the flag form of `link vcs` cannot do. It is mutually exclusive with `--vcs`, `--git-namespace`, `--git-repo`, `--git-visibility`, `--git-default-branch`, `--git-remote-protocol`, `--git-description`, `--cloud-org`, `--cloud-project`, and `--coppa` (all meaningless without a repository to create — the URL's own scheme already says which transport to use). `--git-token[-stdin]`, `--no-initial-commit`, and `--git-lfs` still apply, and the same ambient-auth / Tier A rules as `projects clone [url]` govern whether the push uses a supplied token or the machine's own git auth.

### Assets: inspect or export a `.unitypackage`

```bash
# List a package’s contents: asset path, GUID, payload size, and whether a preview image is bundled
unity assets inspect ./MyPackage.unitypackage
unity assets inspect ./MyPackage.unitypackage --format json
```

Works offline, with no Editor installed and no open project. The archive is streamed rather than read into memory, so a multi-gigabyte package is inspected in constant memory. `--format json` / `tsv` / `ndjson` carry the raw byte size and a boolean preview flag for scripts; the human table shows readable sizes and ends with a summary of entry count and total size. A missing file fails with `ASSET_PACKAGE_NOT_FOUND` (exit 6).

`--format json` / `ndjson` also carry `signed`: `true` when the package holds a signature (its first archive entry is the attestation a signed export writes), and the human output ends with a `Signed:` line. The CLI only checks that a signature is there; it does not verify it, so `signed: true` does not prove who published the package or that it was not changed. `--format tsv` is unchanged.

### Assets — export a `.unitypackage`

```bash
# Export one or more asset/folder paths, plus everything they depend on, to a .unitypackage
unity assets export Assets/Art --output Art.unitypackage
unity assets export Assets/Art Assets/Prefabs/Player.prefab -o Player.unitypackage --project-path /path/to/MyProject
# Leave dependencies out: export only the named paths
unity assets export Assets/Art --output Art.unitypackage --no-dependencies
# Unity 6.6+: sign for a specific organization, or skip signing
unity assets export Assets/Art --output Art.unitypackage --cloud-org my-studio
unity assets export Assets/Art --output Art.unitypackage --no-sign
```

`export` is the write-side companion to `inspect`, and needs an Editor: it runs `UnityEditor.AssetPackage.Package.Export` (Unity 6.6+) or `AssetDatabase.ExportPackage` (earlier) in a batchmode Editor (`--project-path`, else `UNITY_PROJECT_PATH`, defaults to the current directory, resolved through the Hub registry), so expect the same startup cost as `unity build`/`unity test` and, in CI, a license seat. Dependencies are included by default, matching the Editor's own Export Package dialog; `--no-dependencies` opts out. Every asset path is validated **before** the Editor starts, so a typo never buys a batchmode boot: a missing path (`ASSET_PATH_NOT_FOUND`), a path outside the project, or a path outside the two AssetDatabase-addressable roots (`Assets/`, `Packages/`, both reported as `ASSET_PATH_OUTSIDE_PROJECT`) all fail immediately with exit `6`. `--format json` / `ndjson` carry `output` (the written file's path), `size` (its raw byte count), `count`, `includeDependencies`, and the resolved `assets` array, plus `signed` and `signingOrganizationId`; `--format tsv` prints the output path, count, and size as three columns.

On Unity 6.6 and later the package is **signed by default** for an organization, resolved like `projects create`: `--cloud-org <id-or-name>`, then `UNITY_CLOUD_ORG`, then the stored default (`unity cloud org set-default`). Signing needs a signed-in session. A numeric organization id is used as given; a service account must pass one, since it can't look up an organization by name (`CLOUD_ORG_NAME_NEEDS_OAUTH`, exit `2`). When the CLI can't sign (signed out, or no organization), it fails before the Editor starts: exit `3` (`NOT_SIGNED_IN`) or `4` (`CLOUD_NEEDS_ACTIVE_ORG`). Pass `--no-sign` to export unsigned. If the Editor writes the package without a signature, the CLI deletes the file and fails with `ASSETS_EXPORT_SIGNING_FAILED`. If the written package can't be read back to check, the CLI keeps it and fails with `ASSETS_EXPORT_SIGNING_UNVERIFIED`. Editors older than 6.6 can't sign, so their packages are always unsigned.

### Assets — import a `.unitypackage` into a project

```bash
# Import into the project in the current directory
unity assets import ./MyPackage.unitypackage

# Import into a specific project
unity assets import ./MyPackage.unitypackage --project-path /path/to/MyProject

# Import only part of the package (repeat --include to select more)
unity assets import ./MyPackage.unitypackage --include "Assets/Art/**" --include Assets/Scripts/Player.cs

# Refuse a package that has no signature
unity assets import ./MyPackage.unitypackage --require-signed
```

Wraps the Editor's own `-importPackage` batchmode argument — `--project-path` (else `UNITY_PROJECT_PATH`) defaults to the current directory the same way `run`/`build`/`test` resolve their project, and the project must not already be open in another Editor (it holds the project lock). The package is read locally first, the same streaming pass `assets inspect` uses, so a missing or unreadable archive is rejected in milliseconds rather than after a 30-300s Editor boot; an archive that reads cleanly but declares no importable entry is refused the same way, under its own `ASSET_PACKAGE_EMPTY` code. `--format json`/`ndjson`/`tsv` all emit a result row (project, package, entry count) on success — nothing is silently swallowed on a redirected/piped run.

`--include <glob>` imports only part of the package. Each pattern is matched against the entry paths `unity assets inspect` lists, one `/`-separated segment at a time and case-sensitively: `*` and `?` stay inside one folder, a `**` segment spans any number of folders, and a plain path selects that asset or that folder. A pattern that matches a folder selects everything under it, and the folders above every selected entry come along so they keep the package's GUIDs. Repeat `--include` for more; the selection is their union. Dependencies are not followed: a selected prefab whose material you leave out imports without it. Every pattern must match at least one entry, or the import stops before the Editor starts with `ASSET_PACKAGE_INCLUDE_NO_MATCH` (exit `6`), naming each pattern that matched nothing. The CLI writes the selected entries to a temporary package, has the Editor import that, and deletes it afterwards, whatever the outcome. With `--include`, `--format json`/`ndjson` add `include` (the patterns), `selectedEntries` (the count) and `selected` (one row per entry, the `path`, `guid`, `size`, `hasPreview` rows `assets inspect` prints) beside `packageEntries`, and `--format tsv` prints one row per selected entry, with the project, package and entry count repeated on each.

An unsigned package still imports (exit 0) with a warning: on stderr in human and tsv (not under `--quiet`), in the envelope's `warnings` under `--format json` / `ndjson`, where `data.signed` is also `false`. `--require-signed` refuses an unsigned package before the Editor starts, with `ASSET_PACKAGE_UNSIGNED` (exit 6). As with `inspect`, the CLI checks only that a signature is present and does not verify it. Packages exported by Editors older than Unity 6.6, or with `--no-sign`, are unsigned.


---

### Assets — export project assets to a `.unitypackage`

```bash
# Export a folder and everything it depends on (the default)
unity assets export Assets/Art --output ./Art.unitypackage

# Export only the named paths — leave out the assets they depend on
unity assets export Assets/Art/Logo.png Assets/Art/Icon.png --output ./Logo.unitypackage --no-dependencies

# Export from a project other than the current directory
unity assets export Assets/Art --output ./Art.unitypackage --project-path /path/to/MyProject

# Unity 6.6+: choose the signing organization, or opt out of signing
unity assets export Assets/Art --output ./Art.unitypackage --cloud-org 1234567
unity assets export Assets/Art --output ./Art.unitypackage --no-sign
```

Unlike `assets inspect`, this drives a real batchmode Editor: `UnityEditor.AssetPackage.Package.Export` (Unity 6.6+, which also signs) and `AssetDatabase.ExportPackage` (earlier Editors) are the only APIs that can select individual asset paths and control dependency inclusion, and Unity's own `-exportPackage` batchmode argument accepts whole folders only. Every asset path is validated — inside `Assets/` or `Packages/`, and present on disk or in the package cache — BEFORE the Editor spawns, so a typo in a path fails immediately instead of after a batchmode boot. Dependencies are included by default; `--no-dependencies` exports only the paths you name. The project must not already be open in another Editor instance, and a matching Editor version must be installed (`unity install`).

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
unity releases --from-year 2023 --format json

# Paginate: pass .data.nextPage as the next --offset (null on the last page)
unity releases --limit 10 --offset 20 --format json
```

---

### Templates

**Choosing a core template.** Pick by render pipeline, not just by 2D/3D. Default to the URP
templates; the Built-in Render Pipeline templates are deprecated from Unity 6.5 and removed in
6.7, so use them only when the user explicitly asks for Built-in. Verify with
`templates --brief --format json` for the target Editor. Ids below are as of Unity 6000.3
to 6000.7:

| Brief | Template id | Display name | Pipeline |
|---|---|---|---|
| 3D (default) | `com.unity.template.urp-blank` | Universal 3D | URP |
| 2D (default) | `com.unity.template.universal-2d` | Universal 2D | URP + `com.unity.2d.*` packages (the JSON `renderPipeline` field is blank for this one — match by id) |
| High-fidelity PC/console 3D | `com.unity.template.hdrp-blank` | High Definition 3D | HDRP |
| VR / MR / AR | `com.unity.template.vr` / `.mixed-reality` / `.ar-mobile` | VR, Mixed Reality (MR), AR Mobile | URP |
| Built-in, only on request | `com.unity.template.3d` / `com.unity.template.2d` | 3D / 2D (Built-In Render Pipeline) | Built-in; absent from 6.7+ |

`--editor-version` picks the Editor whose templates you see, else `UNITY_EDITOR_VERSION`, else the
default Editor. `lts` and `latest` resolve through the release catalog, as `install` resolves them.
The old `--editor` and `-e` spellings still work, with a deprecation warning.

```bash
# List templates for an editor version (uses the default editor if --editor-version is omitted)
unity templates --editor-version 6000.0.47f1 --format json

# --brief: trim the json data to the same name/displayName/type/version/status
# columns the table shows. Use this when the only thing you need out of the
# list is a template id to pass to `projects create --template`: the
# untrimmed payload above carries every raw field (icons, package lists,
# previous versions, …) and can run into the hundreds of KB for the default
# catalog, most of which a template lookup never uses.
unity templates --editor-version 6000.0.47f1 --brief --format json

# List only locally installed templates
unity templates --editor-version 6000.0.47f1 --installed --format json

# Filter by type (core, learning, sample, custom, new, all) — case-insensitive
unity templates --editor-version 6000.0.47f1 --type core --format json
unity templates --editor-version 6000.0.47f1 --type learning --format json
unity templates --editor-version 6000.0.47f1 --type sample --format json
unity templates --editor-version 6000.0.47f1 --type new --format json
unity templates --editor-version 6000.0.47f1 --type all --format json  # no-op, returns everything

# List only user-generated (custom) templates
unity templates --editor-version 6000.0.47f1 --custom --format json
# --type custom is an alias for --custom
unity templates --editor-version 6000.0.47f1 --type custom --format json

# --custom and --type are mutually exclusive — using both is an error (exit 1)

# Show template details
unity templates info com.unity.template.urp-blank --editor-version 6000.0.47f1 --format json

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
  --version 1.0.0 \
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
- The project is the path argument, `--project-path <path>`, or `UNITY_PROJECT_PATH`, in that order (same for `templates pack`, `projects link` and `projects unlink vcs`); giving both the argument and the flag is a usage error. `projects unlink cloud` never reads `UNITY_PROJECT_PATH`: pass its path as the argument or `--project-path`
- `--name` must be a valid npm package name (e.g. `com.myorg.template.mytemplate`)
- `--output` overrides the Hub-configured user templates directory
- `--overwrite` replaces an existing archive of the same name without error
- On success, prints the path to the created `.tgz` archive
- Created templates appear in `unity templates --editor-version <v> --custom`

**`templates pack` — portable archive, not a registered template.** `create` installs into the Hub-configured user templates directory so the template shows up in `templates --custom`; `pack` writes a standalone `.tgz` to a file path you choose and registers nothing. Reach for `pack` when the archive is an artifact to check in, attach to a release, or hand to someone else.

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
- `--version` is the template's own version
- The output path may not be **inside** the project being packed; that's rejected, so the archive can't include itself
- An existing output file is an error unless `--overwrite` is passed
- `--keep-embedded-packages` and `--keep-project-settings` retain content that is otherwise stripped
- Consumable directly by project creation: `unity projects create MyGame --template ./my-template.tgz`

```bash
# Delete a user-generated custom template (prompts for confirmation)
unity templates delete com.myorg.template.mytemplate --editor-version 6000.0.47f1

# Skip the confirmation prompt (CI-friendly)
unity templates delete com.myorg.template.mytemplate --editor-version 6000.0.47f1 --yes

# JSON output
unity templates delete com.myorg.template.mytemplate --editor-version 6000.0.47f1 --yes --json
```

**`templates delete` key notes:**
- Only user-generated templates (created via Hub UI or `templates create`) can be deleted
- Attempting to delete a built-in Unity template exits with a descriptive error (exit 6)
- Attempting to delete a template that doesn't exist exits with a descriptive error (exit 6)
- In interactive mode, prompts for confirmation before deleting; use `--yes` to skip
- On success, the template no longer appears in `unity templates --editor-version <v> --custom`

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
# At least one of --display-name, --description, --version,
# --preview-image, --remove-preview-image is required
unity templates edit com.myorg.template.mytemplate --editor-version 6000.0.47f1 --display-name "My Updated Template"

# Update multiple fields at once
unity templates edit com.myorg.template.mytemplate \
  --editor-version 6000.0.47f1 \
  --display-name "My Updated Template" \
  --description "A new description for the template" \
  --version 1.1.0

# Replace / remove preview image
unity templates edit com.myorg.template.mytemplate --editor-version 6000.0.47f1 --preview-image /path/to/image.png
unity templates edit com.myorg.template.mytemplate --editor-version 6000.0.47f1 --remove-preview-image

# JSON / NDJSON output (--yes required because these are non-interactive)
unity templates edit com.myorg.template.mytemplate --editor-version 6000.0.47f1 --display-name "Updated" --yes --json
```

**`templates edit` key notes:**
- Only works on user-generated (custom) templates; built-in templates cannot be edited
- Use `--editor` to specify which editor version's template list to search, or omit to use the stored default
- `--preview-image <path>` resolves to an absolute path before passing to the service
- `--remove-preview-image` is only applied when no valid `--preview-image` path is given; if both are passed with a valid image path, the new image wins and `--remove-preview-image` is ignored
- On success (human format), prints the updated template's display name

---

