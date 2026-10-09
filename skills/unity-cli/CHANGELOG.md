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

## CLI `1.0.0-beta.13` (2026-10-07)

Aligned to the CLI's `1.0.0-beta.13` release. Everything below was documented ahead of the release and ships in it.

### Added

- **`unity setup codex` and `unity setup grok`**: `integration-advanced.md` documents installing Unity's agent plugin into Codex and Grok Build, the commands each runs, `CODEX_HOME` / `GROK_HOME`, the not-found and install-failed error codes, and the null `reloadCommand`. `SECURITY.md` extends the plugin-install fence to both.
- **A Unity Cloud choice for `unity projects upgrade` into Unity 6.7 or later**: `projects-templates.md` documents that an unlinked project needs `--cloud`, `--cloud-project <id-or-name>` or `--no-cloud`, that the upgrade turns on build-profile diagnostics only when the project ends up linked, and the `data.diagnostics.buildProfiles` and `data.cloud.outcome` result fields.
- **`proxyRequestLogging` in `unity config`**: `config-hub.md` lists the key, the persisted third tier of `--log-proxy`'s precedence.

### Changed

- **`UNITY_PROJECT_PATH` for per-worktree agents**: `SKILL.md`'s one-Editor-per-worktree paragraph now tells agents to set `UNITY_PROJECT_PATH=<full project path>` once for the session, and names the commands that don't read it (`recompile`, `close`, `mcp configure`). The environment-variable table lists every command that honors it, `unity mcp` included. `integration-advanced.md` adds the variable to the target resolver's order and shows `UNITY_PROJECT_PATH=... unity mcp`.
- **`UNITY_PEER_AUTH_MODE=identify-only`**: `SKILL.md` and `auth-license-cloud.md` now say that a broker started this way is a separate dev broker with its own address and sign-in, that it refuses Unity-signed apps, and that Unity apps refuse it.
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
- **`UNITY_NO_AUTH_BROKER`, `UNITY_PEER_AUTH_MODE` / `UNITY_PEER_AUTH_LINUX_ALLOWED_HASHES`, `UNITY_CLI_HOME`, `UNITY_NO_EDITOR_IDENTITY_SERVER`** — added to the environment-variables table in SKILL.md, with a short paragraph under Auth explaining the on-demand resident auth broker, the hardware-sealed token store, and the peer code-signature check.

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
