# Changelog — unity-cli skill

All notable changes to the `unity-cli` skill documentation are recorded here. The
skill documents the published [`unity` CLI](https://public-cdn.cloud.unity3d.com/hub/prod/cli/);
each entry notes the CLI version the skill was aligned to.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/). Sections are
normally cut per CLI release once that release has shipped publicly, so pending skill work is
usually tracked alongside the CLI change itself, not here. The `[Unreleased]` section below is
the narrow exception: a surface documented ahead of its CLI release. It folds into that
release's own dated section once the CLI actually ships it.

## [Unreleased]

## CLI `1.0.0-beta.15` (2026-10-29)

Aligned to the CLI's `1.0.0-beta.15` release. Everything below was documented ahead of the release and ships in it.

### Added

- **Plugin areas in `unity bug`**: `diagnostics-maintenance.md` documents `--area ugs|plastic|ai|transform`, that a plugin area attaches that plugin's own logs, and the `Installed plugins:` line every report carries.
- **`unity setup qwen`**: `integration-advanced.md` documents installing Unity's agent plugin into Qwen Code, the `qwen` commands it runs, where it reads an existing install, the enable step that turns a disabled one back on, and the not-found and install-failed error codes. `SECURITY.md` extends the plugin-install fence to it.
- **OpenCode skill client**: `integration-advanced.md` lists `opencode` among the `unity skill install` clients and names its skill folders. It also says the install skips a copy OpenCode already loads from `.claude/skills` or `.agents/skills`.
- **Cinemachine and Run In Background in `projects create`**: `projects-templates.md` says every 3D create adds `com.unity.cinemachine` (the newest release that supports the engine version), that a 2D project skips it with the new `NOT_APPLICABLE` reason, that every create sets `runInBackground: 1` in `ProjectSettings.asset`, and that neither writes an account-scoped value. The `steps` report gains `cinemachine` and `runInBackground`, and `--bare` leaves both out.
- **Unity skills repo in `skill install` and `setup`**: `integration-advanced.md` says `unity skill install` also installs the Unity skills pinned from the Unity-Technologies/skills repository, that `--skill` repeats and takes a comma list, that the source is fixed, what an offline run installs, and the `source` and `commit` record fields. It documents `unity setup <agent> --yes` installing those skills when the plugin can't be installed, with `installedVia` and `skillsToLoad`. `SECURITY.md` covers the download.
- **`skill show --skill`**: `integration-advanced.md` documents `unity skill show --skill new-unity-project`, which prints that embedded skill instead of `unity-cli`.
- **Pipeline on Unity 6000.5.0 to 6000.5.2**: `projects-templates.md` says `projects create` leaves the Pipeline package out on those Editors, which can't start its server, and raises `PIPELINE_EDITOR_UNSUPPORTED` (a stderr warning outside json and ndjson) saying to update to 6000.5.3f1 or later. `integration-advanced.md` says `STATUS_PIPELINE_LOAD_PENDING` on those Editors names the cause.
- **Machine state after `auth login`**: `auth-license-cloud.md` says a login that stores a session ends with the machine-state summary, that json and ndjson carry each blocker as a `notifications` entry, and that ndjson ends with a `result` frame.
- **Project CLI version pin**: `config-hub.md` documents `cli.version` in `ProjectSettings/UnityCliConfig.json`, a range of supported CLI versions that warns outside the range and stops a CI run with exit 1 and `CLI_VERSION_OUT_OF_RANGE`, and the commands that always run.
- **`projects create` prerequisites**: `projects-templates.md` says create checks a license, and with the Unity Cloud link the sign-in, the network and the organization choice, before it does anything, and names the notification codes and exit codes.
- **`unity projects audit`**: `projects-templates.md` documents opening Project Auditor in the running Editor on a workflow page (`--workflow upgrade|optimization|urp|coreclr`), ready for the user to run the analysis, after installing or updating its packages, plus `--report`, `--timeout`, the result data and the exit codes.
- **`unity auth token`**: `auth-license-cloud.md` documents printing an access token for scripts and other tools, alone on stdout, its refresh of an expired session, the service-account bearer, `--json`'s expiry and auth mode, the exit codes, and that `--audience` and `--scope` are refused for now.
- **Running cloud builds and Pipeline Automation jobs**: `SKILL.md` and `cloud-automation.md` document `pipeline cloud-build builds` `steps`, `failures`, `artifacts`, `log`, `download`, `start` and `cancel`, and `pipeline automation pipelines trigger` and `jobs` `watch`, `logs`, `terminate`, `resume`, `approve` and `reject`. A waited run that fails exits `15` (`RUN_FAILED`), and a job waiting on a person exits `16` (`JOB_SUSPENDED`). They say never to re-run `builds start` or `pipelines trigger` after a failure, since that starts the work again.
- **`projects create --install-editor`**: `projects-templates.md` says `--install-editor` installs a missing Editor version, waits, then creates the project, that `--module` and `--accept-eula` go to that install, that a failed install creates nothing, and that `data.editorInstalled` reports it. The `ENGINE_NOT_INSTALLED` remediation is now the create command with `--install-editor`.
- **Anonymous player sign-in in `projects create`**: `projects-templates.md` says a create that links the project to Unity Cloud adds `com.unity.services.authentication` and `Assets/PlayerAuthentication/` (`PlayerAuthentication.cs` and its own assembly definition), that `PlayerAuthentication.SignedIn` is what to await, that nothing is configured server-side, and that `--bare`, `--no-cloud` and a failed link leave it out. The `steps` report gains `playerAuth`.
- **`unity setup copilot`**: `integration-advanced.md` documents installing Unity's agent plugin into GitHub Copilot CLI, the two `copilot plugin` commands it runs, `COPILOT_HOME`, the already-registered marketplace it accepts, and the `COPILOT_NOT_FOUND` / `COPILOT_PLUGIN_INSTALL_FAILED` error codes. `SECURITY.md` extends the plugin-install fence to it, and `SKILL.md`'s `UNITY_NO_AGENT_HINTS` row names the Copilot CLI tip.

### Changed

- **`plugin install` and `plugin upgrade` install a plugin's skill**: `integration-advanced.md` says they write the skill a plugin ships into every AI client that already has the Unity skill, and leave other clients alone.
- **`job wait` timeout parameter**: `playmode-verification-loop.md` uses `--timeout` (seconds, default 30, clamped 0 to 600) for `unity command job wait`, the name `com.unity.pipeline` 0.9.1-exp.1 uses in place of `--timeout_s`, and notes that it needs the `--` fence like `script eval`'s `timeout`.
- **Parse stdout alone**: `SKILL.md` says never to merge stderr into stdout (`2>&1`) when parsing `--format json` or `ndjson`, since stderr carries human diagnostics and the Claude Code install hint line, which trail the JSON and break a parser. It says to capture the streams separately or discard stderr.
- **`unity setup all`**: `integration-advanced.md` documents setting up every detected coding agent in one run, its per-client json `data` (`warnings`, the `skipped` outcome), the MCP retry and project-only notes, the state `notifications`, and its exit code.
- **`--offset` pages by offset**: `projects-templates.md` writes `releases --offset <n>` and `auth-license-cloud.md` shows `cloud project list --search <text> --limit <n> --offset <n>`. `SKILL.md` lists `--skip` on `releases` and `cloud project list` among the old spellings that warn.
- **The rest of the Pipeline group-and-name rename**: `SECURITY.md` and `integration-advanced.md` name `unity command script eval` where they still said `eval`, `SKILL.md` and `integration-advanced.md` say commands run as `unity command <group> <name>`, and `integration-advanced.md`'s custom-command section explains `[CliCommand(name, description)]` (the `global` group) against `[CliCommand(group, name, description)]`, and names the `Tags` property, with `Tags = new[] { "runtime" }` marking a Player-only command, in place of `RuntimeOnly`.
- **MCP tool names**: `integration-advanced.md` says each `unity mcp` tool is named by its full command name with spaces and dots written as `_` (`script_eval`, `settings_player_get`), that an older package's flat names (`eval`, `run_tests`) stay its tool names, and that `--tools` and `--exclude-tools` match that name too.
- **Offline docs**: `docs-workflow.md` adds reading a page with `docs --content` and working offline with `docs cache add`, `list` and `clear`, and drops the "not available yet" list. `diagnostics-maintenance.md` documents `docs cache` and the `cache` source of `--content`.
- **Old spellings are listed once**: the references no longer repeat that a deprecated option spelling still works and warns; `SKILL.md`'s Notes list is the one place, and it now names `--project` on `assets export`, `assets import` and `auth default`, `--path` on `build run` and `--project-path` on `projects clone`. `SECURITY.md` names `build run --build-path`.
- **Machine formats never page `unity projects`**: `SKILL.md` drops the note that `--format tsv` and `--format github` on a terminal fell through to the paged human table; every machine format now turns that paging off.
- **Reference files open with a contents list**: each long reference file lists its sections at the top, and each one with relative links says they resolve from its `references/` folder. `SKILL.md` drops two time-sensitive phrasings, per the skills repo's mechanical checks (R1, R2, B11).
- **CI secrets stay off argv**: `SKILL.md`, `auth-license-cloud.md` and `diagnostics-maintenance.md` activate a serial with a bare `unity license activate` and `UNITY_LICENSE_SERIAL` in the step environment, and `build-run-test.md` documents `UNITY_ANDROID_KEYSTORE_PASSWORD` and `UNITY_ANDROID_KEY_ALIAS_PASSWORD` (the Editor still takes the passwords on its command line).
- **A plugin-provided `unity-cli` skips only itself**: `integration-advanced.md` says that when a client plugin already provides the `unity-cli` skill, `unity skill install` still installs the skills an installed plugin or the project's package ships.
- **`unity skill install` also writes `new-unity-project`**: `integration-advanced.md` says a default install writes it beside `unity-cli`, documents `--skill <name>` and the per-skill `--list` status, and says the plugin-provided skip also covers it when the providing plugin carries it, with its own `plugin-provided` record.
- **Listing is the bare plural noun**: `SKILL.md` and the references write `unity projects`, `unity templates`, `unity config`, `unity editors`, `unity modules <version>`, `unity editors modules <version>`, `unity plugins`, `unity skills`, `unity contexts`, `unity licenses` and `unity license servers` where they wrote the `list` leaf, which the CLI no longer accepts.
- **Whether a package is signed**: `projects-templates.md` says `assets inspect` and `assets import` report `signed` under `--format json` / `ndjson` (inspect also prints a `Signed:` line), that an unsigned import warns and `--require-signed` refuses it with `ASSET_PACKAGE_UNSIGNED` (exit `6`), and that the CLI checks only that a signature is present, never that it is valid.
- **`--editor-version` everywhere, and `-e` means `--editor-path`**: `projects-templates.md`, `SKILL.md`, `editors-install.md`, `cloud-automation.md`, `build-run-test.md` and `version-control.md` write `templates --editor-version`, `install-modules --editor-version` and `pipeline cloud-build ... --editor-version`, say `-e` is `--editor-path` (now on `vcs merge-setup` and `vcs resolve` too), that `templates` resolves `lts` and `latest`, that `UNITY_EDITOR_VERSION` is read by every command with `--editor-version` but the Cloud Build filters, and that `build` reads it and `UNITY_ARCHITECTURE` with a notice. The old `--editor`, `-e` and `--unity-version` spellings still work with a deprecation warning.
- **One meaning per short flag, part two**: `SKILL.md` lists every short flag with its one meaning, and says `--follow`, `--input`, `--log-file`, `--modified`, `--reset`, `--output-path` and `--out` have no short flag. `build-run-test.md` and `projects-templates.md` write `--log-file` and `--modified` in full.
- **Which Editor version create uses**: `projects-templates.md` says `projects create` takes `--editor-version`, else the stored default, else the newest installed Editor, else the latest release when none is installed. It says `latest` and `lts` name releases rather than installed Editors, and that a version with no installed Editor fails with `ENGINE_NOT_INSTALLED`. It also says a json or ndjson run notes a newer official release with an `ENGINE_UPDATE_AVAILABLE` notification.
- **Every listing has one JSON shape**: `SKILL.md` says to read a listing's rows from `.data.items[]`, with `total`, `nextPage` or `nextCursor`, and `partial` beside them, and the ndjson `result` line carrying `count` and the same paging. `cloud-automation.md`, `integration-advanced.md` and `projects-templates.md` read `items` and page with `nextPage`.

### Removed

- **`projects new`**: `projects-templates.md` drops its section, and `SKILL.md` and the unattended-create advice no longer point at it. Use `unity projects create <name> --no-cloud`, with `--path`, `--editor-version` and `--template` for a run that never prompts.

### Fixed

- **Retry a timed-out test run**: `build-run-test.md` says a `unity test` run that hits `--timeout` exits `14` (`TEST_TIMED_OUT`), not `6`, and its rule against re-running unchanged code now allows one more run after exit `14` once `--timeout` is raised or the hang is fixed. A run with a verdict (exit `0` or `8`) is still never repeated on the same code.
- **Batch timeouts exit 14**: `build-run-test.md` says `unity run --timeout` and `unity build --timeout` exit `14` (`BATCH_TIMEOUT`) when the run times out, as the CLI does, instead of `6`.
- **Docs workflow covers package manuals**: `docs-workflow.md` documents package manuals with `docs --package`, points `com.unity.*` topics at it, and allows the `docs.unity3d.com/Packages/` addresses it prints.
- **Auto-tick in each Play Mode request**: `playmode-verification-loop.md` says to call `editor set_auto_tick` before every Play Mode request, since step 8 turns it off, instead of once per Editor session, which froze a second request at frame 1.
- **No sign-in question from `auth login`**: `auth-license-cloud.md` says `unity auth login` shows the Hub's account and the `unity auth switch` command that uses it, then carries on with the browser sign-in, instead of describing the removed y/N question.

## CLI `1.0.0-beta.14` (2026-10-21)

Aligned to the CLI's `1.0.0-beta.14` release. Everything below was documented ahead of the release and ships in it.

### Added

- **Agent install hints**: `SKILL.md` lists `UNITY_NO_AGENT_HINTS`, which turns off the suggestion to install Unity's agent plugin or skill, and names the `AGENT_PLUGIN_AVAILABLE` envelope notification.
- **`unity setup antigravity`**: `integration-advanced.md` documents installing Unity's agent plugin into Antigravity, the `agy` command it runs, where it reads an existing install, and the not-found and install-failed error codes. `SECURITY.md` extends the plugin-install fence to it.
- **Docs workflow for agents**: a new `docs-workflow.md` reference gives the order an agent follows to read the right Unity documentation: find a page with `docs toc`, resolve and cite its address with `docs --url`, and match the Editor version with `--editor-version`. It covers the daily page-index cache and the offline behavior, uses 2D examples, and lists reading page content in the terminal, offline content and package docs as not yet available.
- **Accepting the Unity Editor Software Terms with `unity terms`**: `editors-install.md` documents `unity terms accept <version>` (it shows the terms link and the Editor's own copy, then asks; `--yes` for unattended setup, and the `TERMS_ACCEPT_NEEDS_CONFIRMATION` refusal without a terminal) and `unity terms status`. `projects-templates.md` now says current Editors ignore `-acceptSoftwareTermsForThisRunOnly` and points at `terms accept`, which an agent runs with `--yes` only when the user has agreed to the terms.
- **Reading and following the Editor console with `unity logs --console`**: `diagnostics-maintenance.md` documents `--console`, `--console --follow` and `--project-path` on `unity logs`, the entry fields (Burst and compiler errors carry a `diagnostic` with their code, file and line), how `--level` maps onto the console's levels, and how the follower handles a script reload, an Editor restart and evicted entries.
- **Safe Mode in `unity status`**: the Safe Mode recovery in `integration-advanced.md` and the Safe Mode notes in `SKILL.md` now start from `unity status`, which reports an Editor in Safe Mode (`STATUS_SAFE_MODE`) and lists its compile errors (`safeMode.compileErrors` under `--format json`). Reading `Editor.log` by hand is the fallback when it can't.
- **Using the Hub's sign-in**: `auth-license-cloud.md` says a signed-out CLI on a terminal asks "Use <email>, signed in to Unity Hub? (y/N)" from `unity auth` and `unity auth login`, that yes switches only the CLI's own account, and that `--json`, `--non-interactive` and runs without a terminal never switch but report `adoptable` and the `unity auth switch <account>` command, which an agent should show the user rather than run unasked.
- **Named dialog codes on `unity open --until-ready`**: `projects-templates.md` says the Safe Mode prompt, the Auto Graphics API Notice and the Software Terms dialog are reported as `OPEN_BLOCKED_BY_DIALOG_SAFE_MODE`, `_GRAPHICS_API` and `_SOFTWARE_TERMS`, with the same remedies `unity status` gives (and the Safe Mode compile errors), and that all keep the `OPEN_BLOCKED_BY_DIALOG` prefix. `integration-advanced.md`'s Safe Mode recovery names the `open` code too.
- **Search first, never grep help**: `SKILL.md` now leads with a "Find a command" section telling agents to use `unity commands --grep` (built-in commands and plugins) and `unity command --query` (Pipeline commands of the connected Editor or a running Player) instead of filtering `unity --help` with `grep`, and says which covers what until a unified search ships. Needs a `sync-cli-skill` publish.
- **`--operating-system` on `releases` and `install --dry-run`**: `editors-install.md` documents listing another OS's releases and previewing another OS's install, the `CROSS_OS_INSTALL_UNSUPPORTED` and `INVALID_OPERATING_SYSTEM` refusals, and that `--list-modules`, `modules list` and `editors --releases` stay on the host's OS.
- **Word search and ranking in `commands --grep`**: `SKILL.md` and `integration-advanced.md` say plain words match in any order and rank by command name, parent path, description, then options, while a pattern with a regex character stays a regular expression in tree order.
- **See-also between `cloud project` and `projects link cloud`**: `auth-license-cloud.md` notes that linking a local folder to a cloud project is `unity projects link cloud <path>`, and that the two help pages now point at each other.
- **What each command search skips**: `SKILL.md` says `unity commands --grep` never searches Pipeline commands and that each search names the source it skipped (`data.hint`); `integration-advanced.md` documents `unity command --query` with no connected Editor (exit 0, a pointer at `commands --grep` with its match count) and on an empty result.
- **Search pointer in help and errors**: "Getting help" says `unity --help` and the unknown-command error end with "Not sure which command? `unity commands --grep <term>`".
- **The Unity Editor Software Terms dialog**: `projects-templates.md` says a first windowed open can stop at it despite `-acceptSoftwareTermsForThisRunOnly`, that `unity status` reports it as `STATUS_BLOCKED_BY_DIALOG_SOFTWARE_TERMS`, and that only the user accepts it. It also says `unity open` passes the flag only on a headless open, and `--args` cannot remove it.
- **The Auto Graphics API Notice**: `projects-templates.md` says `unity status` reports the Windows notice an upgraded project shows as `STATUS_BLOCKED_BY_DIALOG_GRAPHICS_API`, and that the user answers it with Confirm.
- **The "Enter Safe Mode?" prompt**: `integration-advanced.md`'s Safe Mode recovery now covers the prompt a windowed Editor stops on before Safe Mode, which `unity pipeline list` can't see. `unity status` reports it as `STATUS_BLOCKED_BY_DIALOG_SAFE_MODE` with the first compile errors. The section says no launch flag answers it, that the user answers it (Quit, or Enter Safe Mode), and that an agent stops only an Editor it started, by PID.
- **Headless / fleet license activation**: `auth-license-cloud.md` has a recipe for machines with no browser: `--serial` for perpetual and legacy Pro/Plus serials, `--generate-request` then `--file` for air-gapped machines (with the licensing client staged before the machine goes offline), `--floating`, and `license return` on decommission. It states that there is no username/password option and that a service account cannot activate a license. Verified: `--serial` needs no sign-in on either engine.
- **Hub accounts in `auth list`**: "Separate sign-in from Hub" explains that an account signed in to in the Hub appears in `unity auth list` without the CLI being signed in to it, and that `unity auth switch` activates it when its sign-in is readable, or `unity auth login` signs it in when not.
- **`UNITY_NO_CLOUD` and where the default project applies**: `SKILL.md`'s env table and `projects-templates.md` document `UNITY_NO_CLOUD` beside `--no-cloud` (presence-based, yields to an explicit cloud flag). `auth-license-cloud.md` says the saved default feeds the service commands and `projects link cloud` but not `projects create`, and that `cloud project current` prints the default rather than the folder's project.
- **`--accept-upgrades` on `unity open` and `unity projects open`**: `projects-templates.md` documents the opt-in that passes `-skipUpgradeDialogs` to the Editor, what it says yes to (the project upgrade and the API Updater's script rewrite, with no backup prompt), why it is never a default, and `unity projects upgrade` as the deliberate path.
- **`--download-directory` (`-d`, alias `--install-dir`) on `unity install` and `unity install-modules`**: `editors-install.md` documents installing a self-contained Editor into a named folder without registering it or changing the install path, the `data.path` result field, the per-folder already-installed check and lock (a `-d` naming the Hub's own install folder locks like a normal install), the `--resume` refusal, and adding modules there later.
- **`--bare` and the `steps` result field on `unity projects create`**: `projects-templates.md` documents creating a project without the default project scaffolding while keeping the cloud link and `--vcs`, the `--with-pipeline` refusal (`BARE_ARGS_CONFLICT`), and the per-step report with its statuses and reason codes.

### Changed

- **SECURITY.md no longer coaches scanners**: the section on how to read an automated scanner's verdict is gone. It asked readers to discount verdicts, which a scanner reads as the skill vouching for itself; the accepted risks and their safeguards are unchanged.
- **Play Mode loop turns auto-tick back off**: `playmode-verification-loop.md` step 8 and the command summary turn `editor set_auto_tick` off after `editor.playmode stop`, next to the Run In Background revert, unless the agent will keep driving the Editor.
- **`setup claude` installs from the official marketplace**: `integration-advanced.md` and `SECURITY.md` say `unity setup claude` runs `claude plugin marketplace add anthropics/claude-plugins-official` and `claude plugin install unity@claude-plugins-official`, and that an earlier `unity@unity-agent-plugin` install is reported as installed.
- **`unity mcp` desktop capture is opt-in**: `integration-advanced.md` says `capture_game_view` and `capture_scene_view` return an error when the Editor can't be captured, and fall back to a screenshot of every display only when `UNITY_MCP_DESKTOP_FALLBACK=1` is set for the server.
- **Search rows say what matched**: `SKILL.md` and `integration-advanced.md` say each `commands --grep` and `command --query` row carries `path` and `matchedOn` (`name`, `description` or `option`) beside `kind`, `description` and `invoke`, that an installed plugin's `invoke` is `unity <id>`, and that tsv appends `path` and `matchedOn` as its last columns.
- **A timeout is whole seconds**: `SKILL.md` says every `--timeout`, and `UNITY_BUILD_TIMEOUT`, `UNITY_RUN_TIMEOUT` and `UNITY_TEST_TIMEOUT`, take a whole number of seconds written as digits, and that `30s`, `5m`, `1.5` or `1e3` is a usage error (exit 2).
- **`--timeout` bounds the wait for another install**: `editors-install.md` writes `unity install`, `unity install-modules` and `unity projects require` with `--timeout <seconds>`, the spelling every other command uses, and says the old `--wait-timeout` still works with a deprecation warning. `SKILL.md` lists `--wait-timeout` among the old spellings that warn.
- **`--since` means a revision**: `version-control.md` writes `vcs git migrate-lfs --after <date>` and `projects-templates.md` writes `releases --from-year <year>`, and the common-traps entry says `--since` takes a revision on `vcs summarize`, `vcs affected` and `test`. `SKILL.md` lists `--since` on `releases` and `vcs git migrate-lfs` among the old spellings that warn.
- **`collaboration` uses `--cloud-org` and `--cloud-project`**: `collaboration.md` writes `--cloud-project <id>` and `--cloud-org <id>` everywhere it wrote `--project-id` and `--organization-id`, says both take an id only and that the old spellings still work and warn, and says the organization of `jira server`, `jira project add`/`delete`/`update` and `jira issues list` falls back to `UNITY_CLOUD_ORG` and then the stored default organization, while `jira configs` reads only the scope typed, and that an empty or whitespace-only flag or variable counts as not set. `SKILL.md` lists the two old spellings among those that warn.
- **One command search**: `SKILL.md` says `unity commands --grep` and `unity command --query` are now one search over built-in commands, plugins and the connected Editor's Pipeline commands, and that each row says how to run it. `integration-advanced.md` documents the `pipeline` kind, the `invoke` field, the line that says why the Editor was not searched, and that `--query` beside another listing flag is still the Editor's own filter. `playmode-verification-loop.md` and the `runtime-verify` skill read a command's schema with `--query <name> --detail full`.
- **Old option spellings warn**: `SKILL.md` says an old spelling the CLI still accepts (`projects list -a`, `status --project`, `--template-version`, `--hub-version`, `--package-version`, `--list-components`, `--install-dir`, `--childModules`) prints a deprecation warning naming the new one, in the envelope's `warnings` under `--format json`. `config-hub.md`, `editors-install.md`, `integration-advanced.md` and `projects-templates.md` call those spellings deprecated rather than hidden aliases, and `integration-advanced.md` filters `status` with `--project-path`.
- **One spelling per option**: `SKILL.md`, `projects-templates.md`, `version-control.md` and the `new-unity-project` skill pipe Git tokens to `--git-token-from-stdin`; `collaboration.md` writes the annotation anchor `--local`, as on spatial attachments; `integration-advanced.md` writes `command --group-by` and drops the note not to correct `--group_by`; `editors-install.md` shows every column with `editors --long`. `SKILL.md` lists the old spellings among the deprecated ones that warn, and says `--verbose` on `editors` and `projects list` still shows every column but warns.
- **URP is the default template**: `projects-templates.md` says `projects create` without `--template` uses the Editor version's own `com.unity.template.urp-blank`, that `--template 3d` and `--template 2d` name the version's URP templates, that create never falls back to a Built-in Render Pipeline template, and that a version without it fails with `TEMPLATE_NOT_FOUND` and lists what it offers in `data.offered`.
- **Peer code-signature check**: `SKILL.md` no longer lists a development-only setting that weakens the peer code-signature check, and `auth-license-cloud.md` no longer documents it. `auth-license-cloud.md` says that the auth broker and the Editor identity helper refuse an unsigned or non-Unity-signed peer on macOS and Windows, and that an Editor built from source isn't supported for automatic sign-in. Earlier entries below are reworded to match.
- **`auth login` progress on stderr**: `auth-license-cloud.md` says the `--format json` `auth_url=` progress frame goes to stderr, so stdout is the envelope alone.
- **One meaning per short flag**: `SKILL.md` says `-y` is `--yes` on every command that has `--yes` (now including `bug mcp`, `mcp configure`, `skill install` and `skill refresh`), and `-a` is `--architecture` everywhere, so `projects list` and `projects size` are written with `--all`.
- **Failures never reach stderr as JSON**: `SKILL.md` no longer says some commands still print `{"error": "…"}` to stderr, and lists `unity cloud org list` and `unity cloud project list` among the table listings that end in a `{"count": N}` frame.
- **`unity mcp` says why it lists no Editor tools**: `integration-advanced.md` says that until an Editor is reachable, the tool list holds one `unity-cli-status` tool whose description gives the reason, and that calling it checks again.
- **`skill install` writes one JSON document**: `integration-advanced.md` says `--format json` writes one envelope whose `data` holds a record per skill considered.
- **`skill install` says when it skips**: `integration-advanced.md` says a non-interactive `unity skill install` without `--yes` skips an outdated copy, or a skill a plugin already provides, still exits `0`, and marks the skipped skill's JSON result with `skipped: true` and a `skipReason`.
- **More ndjson answers end in their frame**: `SKILL.md` no longer lists `unity pipeline list` or `unity changelog` among the commands whose ndjson stream lacks a terminal `{"type":"result"}` frame.
- **`editors upgrade` leaves the IDE modules behind**: `editors-install.md` says the upgrade no longer carries Visual Studio Code or Visual Studio over to the new editor, and that `-m visualstudiocode` installs it anyway.
- **`--version` names a command's own subject below the root**: `unity --version` and `unity -V` still print the CLI version, but subcommands no longer inherit them. `config-hub.md`, `integration-advanced.md` and `projects-templates.md` now use `hub install --version`, `pipeline install --version` and `templates create|pack|edit --version`, and say the old `--hub-version`, `--package-version` and `--template-version` spellings still work.
- **Single-record ndjson answers end in their frame**: `SKILL.md` says `unity version`, `unity language`, `unity config proxy`, `unity cache info` and other single-record answers write one `{"type":"result"}` frame whose `data` matches `--format json`, and that `unity env` writes its rows and then that frame, so it no longer lists `unity env` or `unity version` among the commands that lack one.
- **`status` and `editors running` rows use the shared key names**: `integration-advanced.md` says `unity status` rows name the project as `projectPath` and the Editor as `editorVersion`, with `project` and `version` deprecated, and `editors-install.md` says `unity editors running` rows carry `editorVersion`, with `unityVersion` deprecated.
- **Pin each Editor call to its worktree's project.** `SKILL.md`'s one-Editor-per-worktree paragraph told agents to set `UNITY_PROJECT_PATH` once for the session. Most agent tools run each command in a fresh shell, so the variable never reached later calls, which then targeted the source checkout's Editor. It now says to pass `--project-path` on every call that reaches the Editor, or to prefix each call with the variable, and shows the prefixed form. `integration-advanced.md`'s target-resolution note says the same.
- **Play Mode checks on a locked or unattended desktop.** `playmode-verification-loop.md` now says a locked Windows session can stop Play Mode from advancing, even with Run In Background on. Stepping it by hand works: pause the Editor, then call `Step()` through `script eval`. That is only for a session the agent started, so the loop now opens by reading the Editor's play state. `capture game_view --source screen` needs an unlocked desktop (reported on Windows, where a locked session returned a solid cyan image). `--source camera` works when the machine may be locked, except for a Screen Space - Overlay canvas, which no camera draws. `SKILL.md`'s Play Mode note names the locked case.
- **No hard-coded "latest" version or release stage**: `SKILL.md`'s Notes no longer name a CLI version as the latest, or say the CLI is currently in beta. It points at `unity --version` and `unity self-update --check --format json`, so the installed skill can't call itself a release behind.
- **`unity status` and modal dialogs**: `integration-advanced.md` says a dialog-frozen Editor reads `starting`, that an Editor busy running another command still reads `ready`, and documents `STATUS_BLOCKED_BY_DIALOG` and the row's `blockedBy` (Unity 6000.7 or later names the dialog; an older Editor's row is plain `starting`). It listed four failure codes and no `blockedBy`.
- **The Pipeline package is part of every `unity projects create`**: `projects-templates.md` documents that create adds `com.unity.pipeline` unless `--bare`, that `--with-pipeline` is now a no-op, the quiet `UNSUPPORTED_VERSION` skip before Unity 6, and drops the agent-session hint; `integration-advanced.md` no longer points at `--with-pipeline`.
- `SKILL.md`'s "CI: activate a license, then build" workflow no longer signs in a service account and then runs the default activation, which a service account is refused. It activates with `--serial` (or `--floating`), runs no `unity auth login` (service-account environment variables cover cloud commands without a credential store), and says `license return` covers serial and assigned seats. The CI step reads `UNITY_LICENSE_SERIAL`, the secret `unity ci init` generates, and scopes `--serial` to perpetual or legacy serials. `auth-license-cloud.md` no longer says `unity auth logout` has a confirmation prompt for `--yes` to skip, no longer says `unity auth revoke` stops an application from using your sign-in (it records it in a local list the auth broker does not consult yet), and no longer says `--serial` works offline: it needs no sign-in, but it does reach the licensing server.
- **One `MonoBehaviour` per file for a player build.** `build-run-test.md`'s Build section and `SKILL.md`'s headless build section now tell agents to keep each `MonoBehaviour` they write alone in a `.cs` file named after its class. The symptom of getting it wrong is a built player that crashes at startup saying a scene file such as `level0` is corrupted. That error points at scene data, and the Editor and Play Mode can run without a warning.
- **`vcs git worktree` paths**: `SKILL.md` no longer says the output masks paths, and `version-control.md` says `--format json` carries the real paths while human output masks them. `--into` stays as optional advice.
- **Run the cheapest test that proves a change, and never test during the user's Play Mode.** `build-run-test.md` now carries a short rule for `unity test`: EditMode before PlayMode, `--filter`, `--affected` or `--rerun-failed` while iterating, the full suite on the final code before handing work back, and no re-run on code that has not changed. It says which report each of those runs reads and writes, so a narrow run never replaces the full-suite record. It also says `unity test` refuses a project that is open in an Editor. Closing the user's Editor to make room, or starting a test run while they are in Play Mode, is not the answer. `SKILL.md`'s test section points at it, so the flags no longer read as CI-only.
- **Editors built from source**: `SKILL.md` and `auth-license-cloud.md` no longer suggest a signature-check setting for an Editor built from source: the Editor identity helper signs in only Unity-signed Editors on macOS and Windows.
- **`projects` description**: `SKILL.md`'s command table quotes the new `unity projects` help line, "Create, open, upgrade, and link local Unity projects (Unity Cloud, GitHub/GitLab)", and says linking a folder to Unity Cloud is `projects link cloud`, not `cloud`.
- **Pipeline commands are named by group and name.** `SKILL.md`, `integration-advanced.md` and `playmode-verification-loop.md` run them as `unity command editor status`, `unity command editor.playmode start`, `unity command script eval` and so on, where they used the flat names (`editor_status`, `editor_play`, `eval`). `integration-advanced.md` explains the form, and that a package from before command groups is retried under its old names with a one-time warning to run `unity pipeline upgrade`.

### Removed

- **Paved mode**: the `UNITY_CLI_FTUE` row in `SKILL.md`'s environment variable table and the paved mode paragraph in `projects-templates.md` are gone, since the CLI no longer has the mode. Envelopes no longer carry `"paved"`, and `projects create` no longer writes `UserSettings/UnityCliPaved.json` or reports `pavedSettings`.

## CLI `1.0.0-beta.13` (2026-10-07)

Aligned to the CLI's `1.0.0-beta.13` release. Everything below was documented ahead of the release and ships in it.

### Added

- **`unity setup codex` and `unity setup grok`**: `integration-advanced.md` documents installing Unity's agent plugin into Codex and Grok Build, the commands each runs, `CODEX_HOME` / `GROK_HOME`, the not-found and install-failed error codes, and the null `reloadCommand`. `SECURITY.md` extends the plugin-install fence to both.
- **A Unity Cloud choice for `unity projects upgrade` into Unity 6.7 or later**: `projects-templates.md` documents that an unlinked project needs `--cloud`, `--cloud-project <id-or-name>` or `--no-cloud`, that the upgrade turns on build-profile diagnostics only when the project ends up linked, and the `data.diagnostics.buildProfiles` and `data.cloud.outcome` result fields.
- **`proxyRequestLogging` in `unity config`**: `config-hub.md` lists the key, the persisted third tier of `--log-proxy`'s precedence.

### Changed

- **`UNITY_PROJECT_PATH` for per-worktree agents**: `SKILL.md`'s one-Editor-per-worktree paragraph now tells agents to set `UNITY_PROJECT_PATH=<full project path>` once for the session, and names the commands that don't read it (`recompile`, `close`, `mcp configure`). The environment-variable table lists every command that honors it, `unity mcp` included. `integration-advanced.md` adds the variable to the target resolver's order and shows `UNITY_PROJECT_PATH=... unity mcp`.
- **Auth broker and development apps**: `SKILL.md` and `auth-license-cloud.md` now say that a broker started for a development app is a separate broker with its own address and sign-in, that it refuses Unity-signed apps, and that Unity apps refuse it.
- `build-run-test.md`'s Build section now says `unity build --format json` writes only the result document to stdout, with the progress frames and the streamed Editor log on stderr, and that the streamed log masks the credentials the CLI passes the Editor (the `-accessToken` value, the Android keystore passwords) in `build`, `run` and `test`.
- **One Editor per worktree, and report a failed call.** `SKILL.md` now tells an agent working in several worktrees to make each one with `unity vcs git worktree add --into <path>`, open the project inside it with `unity open`, and target that Editor by its full project path. It never drives an Editor whose project isn't its own checkout. Once Safe Mode, a sandbox and a slow import are ruled out, it also reports a timeout or an Editor lifecycle failure to the user instead of silently working around it. It offers `unity bug --area pipeline-package` and asks first.
- The bootstrap recipe's step 6 and the `unity status` reference now pass the project's full path to `unity status --until-ready --project-path`, and say how the flag matches. A path that exists matches that project only, so a sibling checkout such as `MyGame-lighting` never ends the wait for `MyGame`, and a bare name still matches any project path containing it.
- `build-run-test.md`'s Accelerator example ran `unity build` with `--target StandaloneLinux64` and no `--output-path`. A desktop target with neither `--execute-method` nor `--profile` is a built-in player build, which requires an output path, so the example exited 2. It now passes `--output-path ./Build/MyGame.x86_64`.
- **`open` never moves a project onto Unity 6.7 or later.** `projects-templates.md` documents that `unity open` and `unity projects open` exit `2` with `PROJECT_UPGRADE_REQUIRED` instead, and point at `unity projects upgrade`.
- **`unity cloud org create`** in `auth-license-cloud.md` takes the Unity Dashboard's company profile (`--first-name` and `--last-name`, or `--company-name`, plus `--country`) instead of the removed `--industry`.
- `playmode-verification-loop.md` now explains that the Run In Background Player Setting pauses the game while the Editor is unfocused, and tells agents to turn it on with the Pipeline package's `editor_set_run_in_background` command, which keeps the change out of builds. It also drops a Hub source path that public readers can't open.

## CLI `1.0.0-beta.12` (2026-09-30)

Aligned to the CLI's `1.0.0-beta.12` release. Everything below was documented ahead of the release and ships in it.

### Added

- **`--with-pipeline` on `unity projects create`**: `projects-templates.md` documents adding `com.unity.pipeline` during creation, its never-fails-the-create warnings, the `pipeline` result field and its error codes, and the agent-session hint; `integration-advanced.md` points at it from the Pipeline setup section.
- **`--cloud-org` and `--no-sign` on `unity assets export`**: `projects-templates.md` documents that Unity 6.6+ exports are signed by default, how the signing organization resolves, the pre-spawn refusals (exit `3` / `4`), `ASSETS_EXPORT_SIGNING_FAILED`, and the new `signed` / `signingOrganizationId` result fields.
- **`--no-wait` and `--wait-timeout <seconds>` on `install`, `install-modules` and `projects require`**: documented in `editors-install.md` under "Waiting on another install", with exit code 9 and the `INSTALL_LOCK_BUSY` error code they fail with, including that exit 9 can follow a partial install. Exit 9 is also in `SKILL.md`'s exit code table.
- **Experimental FEX-Emu support on Linux arm64**: `editors-install.md` documents the `UNITY_EXPERIMENTAL_FEX_EMU=1` opt-in, the FEX-Emu and RootFS it requires, the `fex-emu` doctor row, and the `FEX_EMU_EXPERIMENTAL` notification.
- **The `source` field on plugin runtime rows**: `integration-advanced.md`'s `unity plugin` section says what `machine`, `package-manager` and `managed` mean on `plugin install`/`upgrade`/`list` and `doctor` rows, and what a runtime row ending `dependency-unavailable` means.
- `--coppa <status>` on `projects create`, `projects link cloud`, `projects link vcs`, and `vcs setup`, and the `coppa` field those commands report when they create a Unity Cloud project. Documented as a "COPPA declaration" note under `projects create` in `projects-templates.md`, with the flag added to the cloud-question, `link` and URL-form flag lists in the same file.
- **`unity projects close`**: documented in `projects-templates.md` beside the existing `unity close` docs. Same shipping command under its `projects` mount, previously missing from this skill even though it has been part of the published CLI for a while.
- **`--key-from-stdin` on `unity collaboration jira server add`, `update`, and `test`**: the Collaboration reference's `jira server` table lists it next to a warning that `--key` exposes the API token in the process list and CI logs, and every example there now pipes the token with `printf '%s' "$JIRA_TOKEN" | ... --key-from-stdin`.
- **`unity templates list --brief`**: documented in `projects-templates.md`'s Templates section, and the template-picking step now runs the list with `--brief --format json` instead of the untrimmed form. The flag trims the JSON data to the same name/displayName/type/version/status columns the table already renders, which matters because the untrimmed payload can run into the hundreds of KB for the default catalog.

- `unity build`'s json/ndjson result gains `data.outputPath`: the absolute, resolved `--output-path` a build was told to write to, `null` only for an `--execute-method` build invoked without `--output-path`. Documented in the Build section of `build-run-test.md`, under a new "Output path in the result" paragraph beside the existing provenance-manifest one.
- **`unity bug --area <cli|pipeline-package|editor|licensing|not-sure>`**: which part of Unity the issue is in, documented under Bug in `diagnostics-maintenance.md`. Optional and never blocking: on a terminal the reporter asks with "Not sure" preselected, and a non-interactive run without it sends no area. The answer rides as an `Issue area:` line in the report text, which is what triage reads. An `editor` or `pipeline-package` answer makes the project-copy question explain why a copy helps; it still defaults to no, and a `licensing` answer does not get that hint (a licensing or sign-in issue is rarely about the project itself).

### Changed

- **The "Bootstrap a new project from scratch" recipe now installs the Pipeline package before the first open and builds the scene in the live Editor.** It used to end at `unity open` without mentioning `unity pipeline install`, which led agents to build scenes through `unity run -- -executeMethod` and to report success on output they couldn't inspect. New steps 5 and 6 run `pipeline install`, `open` and `status --until-ready`. The recipe now says to set up scenes and assets with `unity command eval` against the running Editor, with batch mode as the headless fallback. `integration-advanced.md` and the recipe also document `STATUS_PIPELINE_LOAD_PENDING`, the `unity status` state for a package installed into an Editor that is already open but hasn't loaded it yet.
- The Auth reference now notes, beside the `unity auth login --secret-from-stdin` example, that the secret must be piped in: a terminal on stdin is refused, and the command stops waiting after 30 seconds if the pipe is never closed.
- The Config reference's proxy section said credentials missing from a proxy URL are read from the OS keyring, which the CLI doesn't do. It now says so, warns that credentials in a URL passed on the command line show in the process list and CI logs, and points at your OS identity or `UNITY_PROXY` instead.
- `projects new`'s `--help` description now says it never links to Unity Cloud, matching the fact already documented in this file's `projects new` section.
- `version-control.md`'s `vcs providers` section documents the new `cm` row, taken from `unity plugin list`'s own registry lookup, and the `unity plugin install uvcs` suggestion inside a UVCS workspace with no `cm`. It previously said the command does not report `cm`.

## CLI `1.0.0-beta.11` (2026-09-22)

Aligned to the CLI's `1.0.0-beta.11` release. Cut automatically by the release that published it, from the skill as it stood in that release — so this entry covers the documentation changes that had already landed by then. Anything worth calling out individually is added here in a follow-up pass.

### Added

- **`unity assets export <asset-path...> --output <file.unitypackage>`**: new subcommand, the write-side companion to `unity assets inspect`. The Assets section in `projects-templates.md` (retitled to cover both) documents the flags (`--project`, `--no-dependencies`), the batchmode-Editor requirement, pre-spawn path validation (exit `6` on a missing, outside-project, or non-`Assets`/`Packages` path), and the `--format json`/`ndjson`/`tsv` shapes.

### Changed

- The install one-liners in SKILL.md no longer set `UNITY_CLI_CHANNEL=beta`. A bare `install.sh` / `install.ps1` run already installs the latest beta until a stable release exists, and the stable release after that.
- `projects-templates.md`'s `projects create` walkthrough said the interactive Unity Cloud question defaults to No. It defaults to Yes, matching what the rest of the same file already said. Corrected the one contradicting sentence.
- `cloud-automation.md` said missing arguments or flag values could produce plain-text usage errors even under `--format json`. They fail with exit `2` and the ordinary JSON failure shape, `errors[].code` `INVALID_COMMAND_ARGS`.
- SKILL.md and `auth-license-cloud.md` no longer mention a feature-gated command family that isn't part of the public command surface.

## CLI `1.0.0-beta.10` (2026-09-14)

Aligned to the CLI's `1.0.0-beta.10` release. Much of this release's surface was already documented ahead of it shipping — `unity context`, `unity watch test`, `unity commands`, `unity vcs blame`, `unity skill refresh`, and the Unity Accelerator settings (`unity config accelerator` / `--accelerator` / `unity diagnose accelerator`, listed under Deferred in the `1.0.0-beta.9` entry) — so this entry records what was still missing.

### Added

- **`unity assets inspect <file.unitypackage>`** — new command family. A new Assets section in `projects-templates.md` covers the offline listing (asset path, GUID, payload size, preview flag), the machine formats, and the constant-memory streaming.
- **`unity build --list-targets` / `--list-profiles` / `--create-profile <target>`** and **`unity build run`** — the build discovery and Build Profile creation options, plus launching the most recent recorded build (`--path` for another). A new subsection under Build in `build-run-test.md`, rows in the Build options table, and the `BUILD_INVALID_TARGET` pointer at `--profile`.
- **`unity open --wait`** (also `projects open` / `projects upgrade`) — documented under Projects in `projects-templates.md`: blocks until the Editor exits, exit `0` / `6`, macOS and Linux only, refused with exit `2` on Windows; plus the signal-killed-Editor reporting (inside the 150 ms startup watch without `--wait`, for the whole run with it).
- **`ProjectSettings/UnityCliConfig.json` and `unity config resolve <key> [project]`** — the committed per-project build/test defaults layer (nine keys), the global `config set build.target …` layer beneath it, the precedence order, and the command that names which layer a value came from — in `config-hub.md`, with a pointer from Build.
- **`unity command <name> --result-only`** — the envelope-free JSON result mode, in `integration-advanced.md`, alongside the readable rendering of `recompile` / `recompile_status` / `test_status` / `run_tests` results.
- **`unity license status`'s `floatingServer` / `machineId` fields** — in `auth-license-cloud.md`.
- **`UNITY_NO_AUTH_BROKER`, `UNITY_CLI_HOME`, `UNITY_NO_EDITOR_IDENTITY_SERVER`** — added to the environment-variables table in SKILL.md, with a short paragraph under Auth explaining the on-demand resident auth broker, the hardware-sealed token store, and the peer code-signature check.

### Changed

- `unity mcp configure` — `continue` now prints `config.yaml` instructions instead of writing a file, `--dry-run` previews only the entry being changed, `codex` also relaxes the Codex sandbox network policy and refuses an unsafe `config.toml` edit, and every client config write is atomic; the MCP section also notes the desktop-screenshot fallback of `capture_game_view` / `capture_scene_view` and the `tools/list_changed` notification.
- `unity status` — the `starting` state (Editor still booting) noted alongside `unreachable`.
- `unity self-update` — noted that an interrupted download resumes on the next run and that a Brotli-compressed artifact is preferred when published.
- `unity install` — noted the bounded retry on transient download failures and that `--resume` also recovers an interrupted module download.
- Command index (SKILL.md) refreshed: `assets` added; `config` gains `resolve`; `build` gains `run`.
- Refreshed the latest-version note to `1.0.0-beta.10`.
- The `unity commands` note no longer links to a Hub-only doc; it was the skill’s only relative link outside its own tree, so the standalone copy published to Unity-Technologies/skills is now self-contained.
- **Template selection defaults to URP.** The bootstrap workflow in `SKILL.md` and the Templates section of `projects-templates.md` now name `com.unity.template.urp-blank` (Universal 3D) and `com.unity.template.universal-2d` (Universal 2D) as the defaults, mark `com.unity.template.3d` / `com.unity.template.2d` as the Built-in Render Pipeline templates (deprecated from 6.5, removed in 6.7), and note that `renderPipeline` is blank for `universal-2d` in `templates list` output. Every `projects create` / `projects new` / `templates info` example now uses `urp-blank`. Previously the skill's examples all used the Built-in `com.unity.template.3d` and said the URP id “varies by version”.

## CLI `1.0.0-beta.9` (2026-09-08)

Aligned to the CLI's `1.0.0-beta.9` release. Several pieces of this release's surface were already documented ahead of it landing in the shipped binary — `unity version`, `unity test --affected`, and the `--child-modules`/`--list-modules` spellings — and needed no change here. This pass documents the rest of the shipped surface for the first time.

### Added

- **`unity skill show`** — read the embedded skill (or one of its reference files) straight to stdout, with `--list` and `--path <file>`, without installing anything. Documented in `integration-advanced.md`, alongside `skill install`/`refresh`.
- **A `unity plugin` reference section**, new — this command family (`install`/`remove`/`upgrade`/`list`/`changelog`) shipped across earlier releases (`install`/`remove`/`upgrade` in `1.0.0-beta.7`) but had never had a dedicated write-up beyond a passing mention of `plugin install plastic`. Added now because `plugin upgrade`'s real version-comparison behavior and the new `plugin changelog <id>` needed somewhere to live, and documenting them in isolation without the surrounding command family would have been more confusing than useful.
- **`--color <auto|always|never>` / `--no-color`** — the new global flag, added to the global-flags table.
- **`unity auth consumers`** / **`unity auth revoke <application>`** — list and manage the applications using this machine's Unity sign-in through the auth broker.
- **`unity config get|set|list|unset <key>`** — the generic key-value interface over the existing `proxy` / `proxy.bypass` / `accelerator` / `update-check` settings, documented in `config-hub.md` alongside the purpose-built subcommands it shares storage with.
- **`unity doctor`'s bundled third-party components section** — noted as a one-paragraph addition to the existing Doctor writeup; it's informational, not a check, so it didn't need more than that.
- The always-on `cli telemetry` usage ping, the sign-in token store's machine-sealing, and the self-installed-vs-Homebrew PATH-conflict warning — each is a background/security behavior with no new command surface, so each got a sentence in the relevant existing section (Analytics, SKILL.md's Notes, and Self-update respectively) rather than a section of its own.
- **"Sandboxed agent tooling can hide a running Editor"**, a new `integration-advanced.md` section under `status`, plus a matching callout in SKILL.md. Interim guidance: a restrictive sandbox around an agent's own shell commands can make `unity status`/`command`/`list` report no reachable Editor even when one is genuinely running, on Windows (a separate restricted account can't read the Editor's discovery file) and macOS (a network sandbox can block the loopback connection to it). Says plainly not to conclude the Editor is down from that alone, and never to suggest disabling the sandbox. Superseded once the CLI itself reports this case with its own distinct message — this section says so and should shrink to match at that point.

### Changed

- **Extended the sandboxed-agent guidance into the scene/GameObject/asset editing workflow.** The "Sandboxed agent tooling can hide a running Editor" note (added above) was previously reachable only from the top-of-skill "Drive a running Editor" quickstart. The `unity status`-first Common workflow — the section that actually fires before any scene/GameObject/prefab/asset edit — had no mention of it, so a sandboxed agent hitting a false "no Editor" there had nothing telling it to doubt that result. Observed in the wild: a Codex-sandboxed agent took a `unity status` false negative at face value and improvised an undocumented headless-Editor workaround instead of saying so and falling back to a disclosed file edit, producing an asset that wasn't actually materialized until a later build ran. The workflow now rules out Safe Mode and sandbox interference side by side before permitting the file-edit fallback, and `integration-advanced.md`'s own guidance now names the improvised-workaround failure mode explicitly, not just silent file-editing.
- Command index (SKILL.md) and global-flags/environment tables refreshed for the above.
- `unity install`/`install-modules`'s child-modules flag examples now lead with `--child-modules`/`--no-child-modules` (matching `unity editors module add`), noting the old `--cm`/`--no-cm` shorts still work.
- `unity modules list`'s column table now names the last column `Aliases` (renamed from `downloaderName` in `--format json`).
- Refreshed the latest-version note to `1.0.0-beta.9`.

### Deferred

- The `unity config accelerator` / `--accelerator` / `unity diagnose accelerator` feature itself is still `[Unreleased]` in the CLI's own changelog as of this release — despite the skill already documenting it from an earlier ahead-of-release pass. Left as-is (documenting a subset of the shipped surface is safe); this stamp does not newly assert that feature shipped.
- Auth broker client libraries (the .NET/TypeScript SDKs for other products to use Unity sign-in) are not `unity` CLI commands, so nothing in this skill changes for them.

## CLI `1.0.0-beta.8` (2026-09-01)

Aligned to the CLI's `1.0.0-beta.8` release, which supersedes the withdrawn `1.0.0-beta.7`. That release reached the production beta channel and was pulled the same day, so beta.8 is what actually carries its surface to users, and this stamp moves on from `1.0.0-beta.6`, which is what the channel served in between. Everything the skill already documents stays accurate. Two additions extend the `unity vcs` provider layer that the beta.7 note below recorded as public but not yet documented: repository creation and readiness reporting through Bitbucket's `bkt` and Azure DevOps' `az`. Both are deferred to that same alignment pass rather than documented piecemeal here. Also deferred, for the same reason: `unity skill install <client> --local` now mirroring the agent skill a project's `com.unity.pipeline` package ships, and `unity install --format json` printing on success the same result envelope the NDJSON `result` frame already carried. The rest of the release is Windows elevation and install fixes that change no flag or exit code this skill documents.

## CLI `1.0.0-beta.7` (2026-08-25)

Aligned to the CLI's `1.0.0-beta.7` release. The surface this release ships that the skill already documents landed with the feature PRs themselves: `unity vcs uvcs locks` and `unity vcs uvcs changesets`, the per-organization default cloud project (`unity cloud project set-default` / `current` / `clear-default` and the project-resolution fallback), the long-output pager, and `unity plugin install` / `remove` / `upgrade`. The release's new `unity vcs` verb family (`setup`, `status`, `sync`, `doctor`, `merge-setup`, `conflicts` / `explain` / `resolve`, `diff`, `summarize`, `hooks`, `git worktree`, `git migrate-lfs`, `providers`, `affected`, `switch`) is now public but not yet documented by this skill — that coverage follows as its own alignment pass.

## CLI `1.0.0-beta.6` (2026-08-19)

Aligned to the CLI's `1.0.0-beta.6` release. Most of this release's surface — `unity doctor --ci`, `unity cache key`, `--format github`, `unity test --shard`, and `unity collaboration` — landed already documented in `1.0.0-beta.5`'s reference files ahead of that release's stamp bump. This pass adds the two pieces that were still outstanding: the `unity build` stall heartbeat and `--timeout`, and the `unity open` background identity server plus the new git-credential interactivity gating.

### Added

- **`unity build --timeout <seconds>` (env `UNITY_BUILD_TIMEOUT`) and the stall heartbeat** — documented in the build options table and as its own note: a long build now prints a periodic "Still building — Nm elapsed, last log output Nm ago" heartbeat (on stderr for the human path, as progress frames under `--format json`/`ndjson`), and `--timeout` aborts a build that runs past the given number of seconds, exit 6.
- **`unity open`'s background identity server** — a small helper that answers the Editor's account lookup from the stored `unity auth login` session, so a Hub-less machine gets a signed-in Editor. Documents that it steps aside for a real Hub, exits on its own, and can be disabled with `UNITY_NO_EDITOR_IDENTITY_SERVER`.
- **Git credential interactivity gating** — the CLI now decides up front whether anyone is there to answer a credential prompt. Documented alongside the existing token-resolution order: on a real terminal a configured helper (including Git Credential Manager's browser/device-code flows) runs and is relayed; without one — CI, machine `--format`, `--non-interactive` — the command fails immediately with exit 4 instead of hanging, naming the missing credential.

### Changed

- Refreshed the latest-version note to `1.0.0-beta.6`.
- **Corrected the pager documentation** to describe the one surface that actually pages. The `git log`-style external pager recorded under Added in the `1.0.0-beta.5` notes below was never ported to the shipped binary — nothing spawns `less`, `more.com`, `$PAGER`, or `$UNITY_PAGER` — so the resolution chain, the `TERM=dumb` and `unity shell` conditions, and the five paging listings (`unity command`, `releases`, `editors`, `changelog`, `logs`) never applied; those listings print in full every time. What does page is `unity projects list`, in-process and on a terminal only, ten projects per screen, and off for redirected stdout, `--format json`/`ndjson`, `--all`, `--watch`, and `--no-pager` / `UNITY_NO_PAGER` — but not `--format tsv` or `--format github`, which on a terminal fall through to the human table and page, so the tables no longer claim that every machine format bypasses the pager. The global flags and environment tables now say that, and `UNITY_PAGER` is documented as having no effect.

## CLI `1.0.0-beta.5` (2026-08-13)

Aligned to the CLI's `1.0.0-beta.5` release. That release is fixes only — it adds no command, flag, or exit code. This pass instead closes the documentation gap the previous one left open: every item the `1.0.0-beta.4` note listed as deferred is now documented, so the skill covers the full shipped surface of both releases.

### Added

- **`unity editors prune`** — find editors no registered project uses and optionally uninstall them. Report-only by default; `--remove` uninstalls, and `-y, --yes` is required to do so non-interactively. Notes that "unused" is judged against the project registry, so an unregistered project's editor counts as unused.
- **`unity editors verify <version>`** — structurally verify an installed editor's files and modules, reporting each component as `ok` / `missing` / `skipped` with the exact repair command. Documented as a structural check (presence, not integrity or signing), and that `--architecture` is inherited from the `editors` parent.
- **`unity projects clean`** — delete a project's regenerable folders (`Library`, `Temp`, `Logs`, …). Documents `--dry-run`, that `--yes` is required non-interactively, and the two guardrails: it refuses while an editor has the project open (naming the PID) and rejects a path that isn't a Unity project.
- **`unity templates pack`** — pack a project into a portable `.tgz`, with the distinction from `templates create` stated up front (`pack` writes a standalone archive and registers nothing; `create` installs into the user templates directory). Covers the required `--output` file path, the `--template-version` spelling that avoids the global `--version` collision, and the rejection of an output path inside the project being packed.
- **`unity command` listing-query flags** — `--detail`, `--query`, `--tag`, `--group_by`, `--sort`, `--order`, `--offset`, `--limit` as a table with values and defaults, plus the two traps: `--group_by` is deliberately underscored, and the flags only mean "listing" when no command name is given (with a name they forward to that Pipeline command as parameters, which is why each takes an optional value).
- **Multi-account auth** — `unity auth list` (with its `ls` alias), `switch`, and `default` (`--project`, `--clear`), plus `auth logout <account>`. Documents the precedence that makes these predictable: a project default beats the active account, and service-account credentials beat both; `switch` fails on an ambiguous match rather than guessing, carrying candidates in `data.candidates`.
- **The output pager** — documented in the global flags and environment tables (`--no-pager`, `UNITY_NO_PAGER`, `UNITY_PAGER`) with the `git log` model spelled out: which commands page, the `$UNITY_PAGER` → `$PAGER` → `less -RFX` → `more.com` resolution chain, and the conditions under which paging never happens (non-TTY, machine formats, `--quiet`, `TERM=dumb`, inside `unity shell`) so scripts need no special handling.
- **`unity skill install` / `refresh`** — install this skill into an AI client from the copy embedded in the binary, framed against `mcp configure` (tools vs. docs). Covers the client list, `--list` as the authority on which scopes each client supports, and that `refresh` should follow `unity upgrade` because installed copies otherwise go stale.

### Changed

- Refreshed the latest-version note to `1.0.0-beta.5`.
- Command index: added `editors prune`/`verify`, `projects clean`, `templates pack`, `auth list`/`switch`/`default`, and `skill install`/`refresh`. Also dropped one listed subcommand that is not part of the public surface.

## CLI `1.0.0-beta.4` (2026-08-06)

Tracks the CLI's `1.0.0-beta.4` release. Coverage is the full `1.0.0-beta.3` surface plus the beta.4 additions an automation or CI caller reaches for first: `unity test --report-format`/`--coverage`, `unity build --profile` and the zero-code build strategies, `unity projects exec`, `unity bug --attachments`/`--share-project`, and the rule that a failure is readable from stdout. The rest of beta.4 was deferred to a later pass and is documented under `1.0.0-beta.5` above: `unity skill install`/`refresh`, `unity projects clean`, `unity editors prune`/`verify`, `unity templates pack`, the `unity command` listing-query flags, multi-account auth (`unity auth list`/`switch`/`default`), and the output pager. Documenting a subset of the shipped surface is safe; the stamp exists to stop the reverse (publishing surface that isn't in the shipped binary).

### Added

- **`unity bug --attachments <paths…>` / `--share-project <path>`** — attach extra files (each must be an existing readable file; a folder is rejected), or a stripped copy of the project using the same packaging the Editor's bug reporter uses. Interactively, omitting both flags makes the reporter ask about each.
- **`unity test --report-format nunit|junit|nunit,junit`** — write a JUnit-schema report that GitHub Actions and GitLab ingest as native test results, with no converter step. `junit` alone makes `--output` the JUnit file; `nunit,junit` writes both from a single Editor run, the JUnit one landing beside the NUnit report. `--junit-output` chooses that second path and is valid **only** with `nunit,junit`; passing it with a single format is an option error. The report is written even when tests fail, and the NUnit default is unchanged.
- **`unity test --coverage`** (with `--coverage-output`, `--coverage-options`) — collect coverage through the Unity Code Coverage package. A project without the package gets a warning and the tests still run.
- **`unity build --profile <profile>` and the zero-code build strategies** — documented the three ways to pick a build: a Unity 6+ Build Profile (a `.asset` path or a profile name under `Assets/Settings/Build Profiles`, which defines the target), a built-in desktop player build (`--target` plus a required `--output-path`), or a custom `--execute-method`. `--execute-method` is no longer required, and `--target` is not needed when `--profile` is used. Non-desktop targets still need `--profile` or `--execute-method`.
- **`unity projects exec -- <command>`** — run one command across every registered project, each in its own directory with `UNITY_PROJECT_PATH` and `UNITY_EDITOR_VERSION` set. Narrow the set with repeatable `--filter` terms (`name:<glob>`, `version:<glob>`, `pinned[:<bool>]`), raise concurrency with `--parallel <n>`, and use `--continue-on-error` or `--dry-run`. Arguments are passed verbatim rather than through a shell, so pipes and `&&` are unavailable.
- **`unity run --command` worked example** — a `[CliCommand]` source snippet with the human output it produces and the `--format json` envelope beside it (`data.result`, `data.parameters`, and `data.reusedRunningEditor`, which reports whether an already-open Editor was reused).

### Changed

- **Read failures from stdout, not stderr** — documented the machine-format failure contract. Under `--format json` a failed command still writes a full envelope (`success: false` and a populated `errors` array whose `errors[0].code` is the stable token to branch on); under `--format ndjson` it closes with the usual terminal `result` frame. `data` is usually `null` on a failure but not always, so branch on `success`, never on `data`: a partial `unity editors add` failure carries a row per path, and an ambiguous `unity auth switch` carries `data.candidates`. Empty stdout is not a failure signal, and the commands that still report only on stderr are called out as a known gap rather than a shape to code against.
- **Reserved forwarded flags** — matching is spelling-insensitive, so `-projectPath`, `--projectPath`, and `-projectPath=<value>` are all rejected, on every command that forwards user arguments (`unity run`, `unity test`, `unity build --args`, `unity open --args`). Also clarified that `unity run` deliberately never passes `-useHub`/`-hubIPC`, because the CLI runs no Hub IPC server and those flags would make the Editor launch the Unity Hub.
- **`unity mcp configure --local`** — corrected the client list. The clients with a project-local config are `cursor`, `vscode`, `vscode-insiders`, `kiro`, and `codex`. Windsurf reads one global file and has no project-local variant.
- **`UNITY_NO_ELEVATE` / `--no-elevate`** — corrected to say it keeps the install service unelevated. The Editor's NSIS installer is manifested `highestAvailable`, so it still asks for elevation on demand under an administrator account and never does for a standard user; in CI, run the agent elevated instead.
- Refreshed the latest-version note to `1.0.0-beta.4`.

### Security

- **Install integrity stated, and scoped** — the install script verifies the downloaded binary against the SHA-256 published in the channel's release manifest and aborts on mismatch, or when no SHA-256 tool is available. Because the manifest is fetched from the same CDN origin as the binary, this is described as an integrity check against a corrupted, truncated, or substituted *download*, not a defense against a compromise of the origin; the trust assumption (TLS plus Unity's control of that CDN) is stated explicitly.
- **Linux install side effects split by package** — the CDN script installs a self-contained binary under `~/.local/bin` and touches no system package sources. The separately published packages do change system state, and differently: the `.deb` adds an apt repository entry and installs Unity's signing key into the system keyring, while the `.rpm` adds a yum repository entry with `gpgcheck` enabled pointing at the published key URL and imports no key at install time.

## CLI `1.0.0-beta.3` (2026-07-24)

Tracks the CLI's `1.0.0-beta.3` release. The CLI's own `[Unreleased]` changes at the time (detached command jobs — `unity command --detach`, `unity job wait/status/cancel` — and live in-terminal progress for `unity command`) were intentionally **not** documented in this pass: they weren't in the shipped `1.0.0-beta.3` binary. They shipped in `1.0.0-beta.4`, documented above.

### Added

- **`unity editors running`** — list running Editor instances and the project each has open (version + PID; cross-platform; an empty list is exit 0).
- **`unity projects size [project]`** — on-disk footprint by top-level folder (`-a, --all`; `--json` emits raw bytes).
- **`unity run --command <name>`** — execute a registered `[CliCommand]` Editor command headlessly (arguments after `--` parsed against its `[CliArg]` schema; requires `com.unity.pipeline`).
- **`unity install --list-components`** — list an editor's available modules and exit (a drop-in alias for `unity modules list <version>`).
- **`unity bug` non-interactive flags** — `--title`, `--description`, `--steps` (repeatable), `--reproducibility <first-time|sometimes|always>`, `--email`.
- **`unity shell`** — command-history persistence (↑/↓; secret-bearing flag values masked on disk), tab completion, session context/defaults (`use project|org`, `set format|verbose|banner`, `unset`, `context`), and the `--protocol ndjson` machine/agent mode.
- Environment variables **`UNITY_NO_CONSENT_PROMPT`** (suppress the first-run consent prompt without recording a choice) and **`UNITY_NO_CRASH_REPORT`** (disable anonymous crash/error reporting).
- Global **`--json`** shorthand (accepted on every command) in the global-flags table.
- OSC 9;4 taskbar progress note for `unity install` on interactive terminals.
- **Driving a running Editor** — three patterns: **persistent headless** (launch the Editor binary in `-batchmode` *without* `-quit`; it stays resident and serves the Pipeline API — drive it with `unity command`/`list` --project-path), **warm/interactive** (`unity open`, which registers with `unity status` as `ready`), and **one-shot CI** (`unity run --command <name>` boots a batch Editor, runs one command, exits). Notes that a bare `unity run` is *not* persistent (batch runs to completion and exits) and — verified — that a batch-mode Editor serves commands but is **not** listed by `unity status`. Closes a gap where the Connected Editors section assumed a running Editor without saying how to get one.
- **Authoring custom `[CliCommand]` tools** — `[CliCommand]` / `[CliArg]` in the `Unity.Pipeline.Commands` namespace (assembly `Unity.Pipeline`), with `MainThreadRequired` / `RuntimeOnly` as **named properties on `[CliCommand]`** (not separate attributes); worked example, and hot-registration via `unity command recompile` → `unity list`.
- **Editor-side `eval` / `eval_file`** — noted the runtime-discoverable production path via `unity command eval` / `unity command eval_file`, discovered from the connected Editor.
- **Live-Editor control surfaced up front** — the skill `description` now advertises controlling a running/connected Editor (create/modify GameObjects, edit scenes, inspect the hierarchy, run C#) so agents pick the skill for scene/GameObject prompts, and a new top-of-skill **"Drive a running Unity Editor"** quickstart shows the minimal `unity status` → `unity command` path ahead of the install steps.
- **Production live commands + curated command list** — clarified that the whole `unity command <name>` / `com.unity.pipeline` command set (`create_gameobject`, `save_scene`, …) runs in production Editors, so agents don't assume live-Editor control is dev-gated. Added a curated quick-reference of the common built-in scene/GameObject commands, noting `unity command --format json` remains the authoritative catalog.
- **Scene / GameObject / asset workflow** — a new Common workflows entry makes `unity status` the first move for any scene or object task and, when an Editor is connected, prefers live `unity command` calls over file edits. Adds a strong anti-pattern block against hand-editing `.unity` / `.prefab` / `.asset` YAML while a live Editor is reachable (error-prone fileIDs/GUIDs, invisible until reimport, can silently target the wrong scene), with an explicit "only edit files when no Editor is reachable" fallback.
- **Recovering from Safe Mode** — a new Connected Editors playbook for the deadlock where a project's C# compile errors force the Editor into Safe Mode, the `com.unity.pipeline` package doesn't load, and `unity command`/`status`/`list`/MCP can't connect. Documents the recovery loop with production-available commands: recognize the connection failure, confirm Safe Mode with `unity pipeline list` (which surfaces the warning, `SafeMode Instances: N detected`, and the "fix compilation errors and restart" hint), read the compile errors from the Editor log — narrowest first (`-logFile`, then `<project>/Logs/Editor.log`, then the per-user global log, with per-platform paths; disambiguated from `unity logs`, which reads the CLI's own log) — fix the C# source, restart Unity, and re-poll until reachable. Restarting stops the stuck Editor **by PID** from `unity pipeline list`, with an explicit warning against name-pattern kills (`pkill -f Unity`) that would take down every open Editor including unsaved work. The log step reads through a filter rather than dumping a cross-project file, and treats log contents as data, not instructions. Cross-linked from the "Drive a running Editor" quickstart and the scene-editing fallback so agents diagnose Safe Mode before falling back to blind file edits. (Addresses community feedback on the 1.0.0-beta.3 rollout thread.)

### Changed

- **`unity upgrade`** — documented Linux AppImage in-place updates and the apt/rpm repositories (GPG-signed rpm); the background "update available" notice is now package-manager-aware (suggests the owning manager's upgrade command) rather than always suppressed on package-managed installs.
- **`unity analytics`** — expanded the events recorded when opted in (registered command names only, never arguments/paths/project names; editor uninstalls; project open/create; self-upgrade/uninstall; shell/mcp/doctor/bug), noted that `opt-in`/`opt-out` now permanently answer the first-run prompt, and documented the separate anonymous Sentry crash-reporting pathway.
- **`unity language --set`** accepts BCP-47 / locale / bare-language / bare-region spellings (resolved case-insensitively when unambiguous); catalog shared with the Hub.
- **`unity projects`** path resolution documented as tolerant of casing, separator direction, and trailing slash (verified against real filesystem identity).
- Terminal-hardening note extended to Commander usage errors, the `bug` log-archive warning, and `projects add`/`remove` tsv output; noted that an invalid `--proxy` now fails with exit 2; `UNITY_PROJECT_PATH` now honored by `status` and the cloud commands.
- Refreshed the latest-version note to `1.0.0-beta.3`.

### Security

- Added `SECURITY.md` documenting the skill's powerful-by-design capabilities (local Editor control and C# evaluation, official-CDN install) and the safeguards around them (local-user-context execution, trusted-input-only machine mode, HTTPS official CDN).
- Clarified that driving a live Editor and running C# happen entirely on the local machine in the user's own account — not remote access — and added a trusted-input warning to `unity shell --protocol ndjson` machine mode.
- Removed internal development-only command documentation from the public skill; the production Editor-side C# evaluation via `unity command eval` remains documented. `SECURITY.md` now carries only the user-facing capability rationale and safeguards.

## CLI `1.0.0-beta.2` (2026-07-21)

Tracks the CLI's move to 1.0 versioning (`1.0.0-beta.1` re-baseline) and `1.0.0-beta.2`. The CLI's own `[Unreleased]` changes at the time (e.g. the universal `--json` shorthand) were intentionally not documented in this section — they weren't in the shipped `1.0.0-beta.2` binary (they shipped in `1.0.0-beta.3`, documented above).

### Added

- **`unity shell`** — interactive REPL that boots the CLI once and runs many commands in a warm process (enter commands without the `unity` prefix; `exit` / `quit` / Ctrl-D to leave).
- **`unity list`** — top-level discovery of a connected Editor's registered tools (name, description, group, parameter schema); introspection-only companion to `unity command`.
- **`unity diagnose proxy`** — redacted, paste-safe proxy diagnostic report for support (`--json`; a copy is written to the logs dir).
- **`unity pipeline upgrade`**, **`unity pipeline list-versions`**, and **`unity pipeline install --package-version <v>`** — upgrade the Pipeline package only when the registry is newer, list all published versions, and pin a specific version. Documented that the flag is `--package-version` (not `--version`, which collides with the global `-V, --version`), and the multi-editor selection behavior.
- **`unity editor module remove` / `unity editors module remove`** — remove installed modules by id (`-m`, repeatable; `-y`, `-a`).
- **`unity install-modules`** `--reinstall`, `-f` / `--force`, and `--retries <n>` (env `UNITY_INSTALL_RETRIES`); **`--no-elevate`** (env `UNITY_NO_ELEVATE`, Windows) on `install` / `install-modules`.
- Global **`--log-proxy` / `--no-log-proxy`** (env `UNITY_LOG_PROXY`) — per-request redacted proxy logging.
- **`unity doctor`** environment health checks (PATH presence, `unity`-binary shadowing, Windows long-path support).
- Exit code **`143`** (SIGTERM) in the exit-code table.

### Changed

- **`--instance <host:port>` removed** from `unity command`, `unity mcp` — the CLI discovers running Editors itself; target via the project directory or `--project-path`.
- **Exit codes** — the `cloud` / `auth` commands map an auth failure to `3` and any other operational failure to `6` (previously `1`); `unity build` interrupts exit `130` (SIGINT) / `143` (SIGTERM).
- **`unity license`** recognizes service-account sessions (`status` reports "Signed in: yes (service account)"); `activate` default/`--personal` fail up front for service accounts, pointing to the unattended modes; `return` now returns serial-activated licenses too, with per-license partial results.
- **`unity install` / `install-modules`** continue past a failed item and report a per-item result (✓/✗/·), with an `items[]` breakdown in NDJSON.
- **`unity upgrade`** detects package-manager installs (points at the owning manager instead of self-replacing); the "update available" notice is suppressed there.
- **`unity analytics`** first-run prompt now requires an explicit `y`/`n` (Enter re-asks); **`unity language`** dropped the regional variants Spanish (Latin America), French (Canada), and Portuguese (Portugal).
- Refreshed the latest-version note to `1.0.0-beta.2`; noted the move to 1.0 versioning at `1.0.0-beta.1`.

## CLI `0.1.0-beta.8` (2026-06-25)

### Added

- **MCP server** — `unity mcp` (built-in Model Context Protocol stdio server
  exposing a connected Editor's commands as tools) and
  `unity mcp configure <client>` (one-step config for 16 AI clients: `claude`,
  `claude-code`, `cursor`, `vscode`, `vscode-insiders`, `copilot-cli`,
  `windsurf`, `cline`, `codex`, `kiro`, `trae`, `openclaw`, `antigravity`,
  `zed`, `continue`, `inspect`; with `--list`, `--local`, `--project-path`,
  `--yes`, `--dry-run`).
- **`unity editors upgrade [editor]`** — upgrade an installed editor to the
  newest f-channel patch in its `major.minor` line, carrying modules over;
  `--all`, `--replace` (`--remove-old`), `--dry-run` (`--check`), `--no-modules`,
  `--module`, `--architecture`, `--yes`, `--accept-eula`. Documented the
  explicit `editors list` subcommand and the new "Upgrade to" column on
  `editors --installed`.
- **`unity config update-check`** and the `UNITY_NO_UPDATE_CHECK` env var, plus
  the background "update available" notice.
- `unity command screenshot` example (a command forwarded to the Editor).

### Changed

- **`pipeline`, `command`, and `status` promoted from development-only to
  production.** They now talk to any running Editor, and the Pipeline package
  (`com.unity.pipeline`) resolves from the **Unity UPM registry** into
  `Packages/manifest.json` — no internal-network clone or SSH. Moved into a new
  "Connected Editors" section; dropped `--ssh` / `--install-samples` /
  `--install-tests` from `pipeline install`; corrected the `command` aliases to
  `cmd`, `request`.
- **Auth:** the CLI and the Hub now store sign-in credentials **separately**
  (previously a shared keyring session).
- **`unity license list`** now reports a clear error and a non-zero exit when
  the licensing client is unavailable (previously an empty list).
- **`unity bug`** collects the same diagnostic system information as the Hub bug
  reporter (including GPU details).
- Refreshed the latest-version note to `0.1.0-beta.8`.

### Removed

- **`unity implode`** — removed (use `unity self-uninstall`).
- Dropped some no-longer-existent command wrappers.

## CLI `0.1.0-beta.7` (2026-06-17)

### Added

- **License management** (`unity license`) — `list`, `status`, `activate`
  (`--serial` / `--personal` / `--floating` / `--file` / `--generate-request`,
  mutually exclusive modes), `return`, and `server list|status`. Documented the
  expected exit codes (`4` when no license / floating server is configured).
- **`unity hub install`** — bootstrap Unity Hub from the CLI, with
  `--force`, `--headless` (Windows), `--architecture`, `--hub-version`, and
  `--skip-signature-check`; documented SHA-512 + code-signature fail-closed
  verification.
- **`unity test`** — run EditMode/PlayMode tests via the Editor's built-in test
  runner, with `--mode`, `--filter`, `--output`, `--editor-version`,
  `--editor-path`, `--architecture`, `--allow-install`, and `--timeout`
  (`UNITY_TEST_TIMEOUT`).
- **`unity editors path <version>`** — print an installed editor's directory
  (local, offline); clarified its distinction from `editors install-path`.
- **Projects source control & cloud** — `unity projects clone`,
  `projects link cloud|vcs`, `projects unlink cloud|vcs` (`--unlink-workspace`),
  and the full source-control flag set on `projects create` / `link vcs`
  (`--vcs`, `--git-namespace`, `--git-repo`, `--git-visibility`,
  `--git-default-branch`, `--git-token` / `--git-token-stdin`,
  `--no-initial-commit`, `--git-lfs`, `--vcs-region`). Also `projects create
  --cloud` / `--cloud-project`, and `--template` accepting a `.tgz`/directory.
- **`unity build` Android signing & export** — `--android-export-type`,
  `--android-keystore-base64`, `--android-keystore-password`,
  `--android-key-alias`, `--android-key-alias-password`,
  `--android-target-sdk-version`, `--android-symbol-type`,
  `--android-version-code`.
- New env vars `UNITY_TEST_TIMEOUT` and `UNITY_CLOUD_ORG`; new exit code `4`
  (precondition not met).
- Notes on the branded landing-surface header, the CLI's own `cli-log.json`,
  shared keyring sign-in with Hub, manifest-driven per-module install commands,
  partial-download self-heal, and terminal output hardening.

### Changed

- **Corrected command availability.** Commands previously presented as generally
  available were regrouped; several are not part of the published CLI's `--help`.
  (`pipeline`, `command`, and `status` were later promoted to production in
  `0.1.0-beta.8`.)
- `unity templates edit` expanded with its full editable-field flag set and the
  "at least one field required" rule.
- Refreshed the latest-version note to `0.1.0-beta.7`.

## CLI `0.1.0-beta.6` — prior baseline

The previous skill revision documented CLI `0.1.0-beta.6`: Unity Cloud
(`unity cloud …`), proxy support (`unity config proxy`, `--proxy`,
`--proxy-disable`), analytics consent (`unity analytics …`), custom templates
(`templates create|edit|delete|location`, `--type`), `unity status`,
and build versioning (`--versioning-strategy`,
`--build-version`).
