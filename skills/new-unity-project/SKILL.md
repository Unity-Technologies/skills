---
name: new-unity-project
description: Guides creating a new Unity project, from concept, platforms, and monetization to source control and packages. Use when the user asks to start a new game or prototype. Runs an opinionated paved path by default.
allowed-tools:
  - Bash
  - Write
  - Edit
---

# New Unity Project

A flow from an idea to a running, version-controlled Unity project that plays. This skill owns the **flow** —
mode, defaults, ordering, background installs, handoffs.

**Delegates to (read these for the actual commands — don't reinvent them):**
- **`unity-cli`** — install, auth/license, Editor install, creation, source control, open. Its
  "Bootstrap a new project from scratch" workflow is the backbone here.
- **`unity-package-management`** — packages with the Pipeline `package` commands (C# script fallback).
- **`urp-postprocessing`**, **`ui`**, **`2d-pixel-perfect`** — visual-baseline mechanics (Step 7).
- **`craft-game-floors`** — the craft floors (look, feel, audio, shell, live floor, the done gate),
  loaded at Step 6 and carried through the build.
- **`implement-in-app-purchases`**, **`levelplay-unity-integration`**, **`build-live-game`** —
  monetization / backend *integration* (invoked at the end).

**Before you go forward with a step or a diagnosis, open and read its pages**: the delegated
skill and the `references/` file the step names. Name the files you read in your reply, because
your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.
Prefer fetching a doc page directly (for example `WebFetch`) over searching (for example
`WebSearch`), and only search without an exact URL. Replace `<VERSION>` before fetching:
- **docs.unity3d.com**: the Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: the package version, eg `4.2`; check it if unsure. These sites have no `.md` variant

## Mode: paved by default, guided on request

Run **paved** unless the user asks for the interview — in words ("ask me questions", "guide me")
or by pushing back on the manifest; mode is decided once per session. The paved contract:
**parse the prompt first** · **fill gaps from the Defaults table** · **print the manifest** before
the create launches · **proceed**, asking only on pushback or an unruled fork ·
**never consume a human gate** — sign-in, license activation and publishing each need the user's
yes, asked as a question you wait on (see Sign-in and license). A **bare/minimal** ask gets
`--bare`, skips the floors, and states the cost (identity, saves, ranks unwired; no playable loop).
Bare runs Steps 1–5 (only the packages the user named), skips 6, 7, 9 and 10's publish ask
(the report names the route for later), commits (Step 8), and hands off (Step 11) without the baton.

In **guided** mode, ask only the current step's questions and wait. The paved additions (identity,
floors, baton) are asked at their steps there, never assumed.

## Sign-in and license (before your first question)

Run `unity auth status --format json` and `unity license status --format json` first. If either
shows the user signed out or without an active license, ask **one** question that covers both,
with your client's multiple-choice question tool (plain text only without one), and wait for the
answer. Send it together with the delegated-skills offer (below) and any Step 1 question, so the
user answers once. Offer the free
**Unity Personal** license as the default choice, with the other license types (a subscription the
account already has, a serial, a floating server) as alternatives. The question says that a yes
also accepts the Unity Editor Software Terms
(https://unity.com/legal/editor-terms-of-service/software) for every Editor this run opens, installed now or already on the machine, and
the Personal choice says it accepts the Unity Personal EULA (the same page), so the user sees both
before answering. Every Editor stops on its own terms dialog the first time it opens until they are
accepted, and an agent can't see or click that dialog. When sign-in and license are already set
and no license question goes out, still ask about the terms with your first question; the create
can start before the answer, but `unity terms accept` and the first `unity open` wait for it.

You run every sign-in and activation command yourself, after the user's yes, and never hand them commands to run. Personal and subscription seats need `unity auth login`; serial and floating do not. Exact commands and the declined-sign-in path: [references/sign-in-details.md](references/sign-in-details.md).

## The Defaults table

Every row reverses on a word; change a row only with a run-earned receipt.

| Decision | Paved opinion |
|---|---|
| Editor version | `--editor-version latest`, explicit |
| Template | URP; 3D unless the prompt says 2D (`--template 3d` or `2d`) |
| Platforms | Desktop only; add modules when the prompt implies them ("mobile game" → ios/android) |
| Monetization | None at creation; noted for later |
| Version control | Local git + Unity .gitignore |
| Player identity | Anonymous accounts — create's when its result shows them, else wired in Step 6 |
| Craft floors | Always on, via the `craft-game-floors` skill: look, camera, lighting, character motion, a playable loop (the minimum), feedback, audio, shell, live floor, done gate |

Cloud link and diagnostics are **not** a row: create applies its own defaults — narrate them from
the result, don't re-decide.

## The assumptions manifest

Print exactly one block at the end of Step 2, before the create launches. It **predicts** what
create will do (the URP template per `--template 3d|2d`, never a guessed id):

> **Paved assumptions:** Unity <editor> · <URP template, 2D/3D> · <platforms> · <monetization> ·
> anonymous player accounts (create's, else one bootstrap script) · <version control> ·
> cloud-linked with diagnostics (create's defaults, narrated from the result) · floor packages
> (Cinemachine; identity + live services when cloud-linked) · craft floors on via `craft-game-floors` ·
> at least one playable loop (<verb · goal · pressure>, or the user's stated shape), scored by its
> done gate · a one-line handoff note for the project's CLAUDE.md, written only on your yes.
> Say the word to change any of these; otherwise I'm proceeding.

Step 3 reconciles the result against it.

## Before Step 1: check the delegated skills are loaded

Look for `unity-cli`, `unity-package-management`, `urp-postprocessing`, `ui`, `build-live-game`
and `craft-game-floors` in your client's skills. If any is missing, say which and what it does, and offer the install routes
in [references/skill-setup.md](references/skill-setup.md), in the same question as sign-in and
license — never install without a yes. A decline gets the fallback at its step, gap named in the
report; without `unity-package-management`, Step 5 can't run, so say so before Step 2.

## The flow — and where the parallelism is

1. **Concept** (paved: parsed from the prompt). 2. **Platforms & monetization** — print the
manifest (paved), then **start the create in the background**: `projects create --install-editor`
installs the Editor + modules (minutes), then creates the project; keep talking meanwhile.
3. **(joins)** reconcile the result against the manifest. 4. **Source control.** 5. **Packages.**
6. **Player identity, then the craft floors** — confirm create's identity or wire it; load
`craft-game-floors`. 7. **Visual baseline** on the craft skill's art bible. 8. **Save & first commit.**
9. **First playable loop** + the done gate. 10. **Publish** — asked, never assumed.
11. **Hand off** + the forward baton.

## Step 1 — Concept

**Paved:** the prompt is the spec — parse genre, core loop, dimension (2D/3D), look, scope, and name
into a 2–4 line brief; fill gaps from the Defaults table (no loop named → pick one from the genre and
put it in the manifest); no read-back.

**Guided:** ask with your client's multiple-choice tool (plain text without one): **genre / core
loop** · **dimension & look** · **gameplay** (what the player does moment to moment) · **scope**.
Settle a **project name**, write the brief, read it back. Note the palette and mood words,
pixel-art looks and the loop's verb: they drive Steps 2, 5, 7 and 9.

## Step 2 — Platforms & monetization, then start installing

Two decisions (paved: filled from the Defaults table, then **print the manifest**):

- **Target platforms**: Desktop, Mobile (iOS/Android), WebGL, Console — they map to Editor
  **modules** (`--module`) and argue for leaner packages on mobile/WebGL.
- **Monetization**: none / premium / IAP / ads / mix — this only decides the Step 11 handoff.

Default to an explicit `--editor-version latest` unless the user names a version — without the
flag, create prefers a stored default or an older installed Editor.

For a cloud-linked create, pick the organization first: `unity cloud orgs --format json`. When
`data.activeOrganizationId` is null and `data.organizations` holds exactly one org, pass that org's `id` as
`--cloud-org <id>`; without it create links nothing and skips player accounts, Remote Config and
Cloud Save. With several orgs and no default, ask the user which one before the create.

| Error code | Cause | Resolution |
|---|---|---|
| `CLOUD_LINK_FAILED` (no organization configured) | create had no org to link | pass `--cloud-org <id>` from `unity cloud orgs`; ask when several |
| `PIPELINE_EDITOR_UNSUPPORTED` | Editor 6000.5.0 to 6000.5.2 can't run the Pipeline package | ask to update to 6000.5.3f1 or later ([visual-baseline.md](references/visual-baseline.md)) |
| `PROJECT_UPGRADE_REQUIRED` | `unity open` refuses a move to 6000.7 or later | `unity projects upgrade "<project-path>" --to <version> --yes` |
| `CLOUD_CHOICE_REQUIRED` | upgrading a project the user kept unlinked | add `--no-cloud` to that upgrade |

Confirm prerequisites, then **start the create as a background task** (`unity-cli` has module
names and auth / license setup):

```bash
unity --version   # sign-in and license: settled before your first question (see above)

# Start in the BACKGROUND. Module names per platform are in the unity-cli skill.
# --bare only on the user's ask (see Mode).
unity projects create "<Name>" --output-dir <dir> --editor-version latest \
  --template <3d|2d> --install-editor --module <platform-modules> --accept-eula \
  [--cloud-org <id>] --format json
```

The command waits for the install, then creates the project, so its one result covers both. If you
have nothing left to ask, it's fine to just wait.

**Template:** `--template 3d` / `2d` resolve to the URP templates. Pass a Built-in template id only
when the user asks for Built-in (deprecated from 6.5, removed in 6.7).

## Step 3 — Join: project ready, result reconciled

Wait for the background create and read its JSON result: `data.path` is the project, `data.version`
the Editor, `data.editorInstalled` whether this run installed it. If create failed, surface the error
and stop, because nothing downstream works without an Editor.

Then follow [references/join-and-terms.md](references/join-and-terms.md): reconcile the result
against the manifest (paved), add modules when `editorInstalled` is false, and run
`unity terms accept` only after the user's yes and before any `unity open`, because an unaccepted
Editor stops on a dialog an agent cannot click.

## Step 4 — Source control

**Paved:** local git + Unity `.gitignore`. **Guided:** ask — Git (GitHub / GitLab; `--git-lfs` for
asset-heavy games), **Unity Version Control** (`--vcs uvcs` — large binaries natively, no LFS), or
purely local. Attach with `unity projects link vcs "<project-path>" --vcs …
--git-token-from-stdin --no-initial-commit` (tokens on stdin) — **`--no-initial-commit`** keeps
the CLI from committing the bare project before packages and `.meta` files exist; the real first
commit is Step 8. Exact flags: the `unity-cli` workflow.

## Step 5 — Packages

Map the brief to a package list and install via **`unity-package-management`** (**never**
hand-edit `manifest.json`); a `--bare` run installs only the packages the user named. Paved adds the
floors' needs (Cinemachine, authentication when Step 6 wires identity, the live floor's services on
cloud-linked projects) and skips what create already reports. Never add a render-pipeline package
atop the template. Read the list back (paved: print it), then verify `manifest.json`. Install
routes, the bare path and the services rules: [packages-and-identity.md](references/packages-and-identity.md).

## Step 6 — Player identity, then the craft floors

A `--bare` run skips this step. **Identity:** the create result is the switch. Identity in the
result → confirm it and skip; none and cloud-linked → wire anonymous sign-in in one small bootstrap
script (`build-live-game`); link failed or skipped → skip, cost stated. Then **load
`craft-game-floors`** and follow its floors through the build; its values supersede the inline
fallbacks. Missing and declined → fallbacks, gap named. Full rules:
[packages-and-identity.md](references/packages-and-identity.md).

## Step 7 — Visual baseline

A fresh template renders correctly but looks like a default, and agents then reach for `OnGUI`
and guess shader names. Apply the baseline **before** any gameplay work. A `--bare` run skips this
step: go to Step 8.

1. **Read [references/visual-baseline.md](references/visual-baseline.md) first.** It holds the
   Editor-version stop (6000.5.0 to 6000.5.2 cannot run the Pipeline package), the order of
   `unity pipeline install` before `unity open`, and the six items, because the details differ per
   render pipeline and a wrong guess renders pink or flat.
2. Install the Pipeline package, open the project, and wait until `unity status` reports it ready.
3. Apply post-processing, camera and light, quality tier, UI stack, materials and shaders (and
   pixel-art settings for pixel art). `unity-cli` owns the commands; use `craft-game-floors` values when it is loaded.
4. Save the scene, then check by file and API, never a screenshot: the Editor console has no
   render-pipeline or shader errors (`unity command console status`; never `unity logs`, the CLI's
   own log), and `script eval` reads back the camera post-processing flag, the Volume profile and the
   quality tier. On a failure, reread [visual-baseline.md](references/visual-baseline.md), fix
   that setting and check again.

## Step 8 — Save & first commit

The project is already open from Step 7, so `.meta` files exist (`unity open "<project-path>"`
only if not — headless/CI uses the "Import & save headlessly" method in
`unity-package-management`). Commit **with whichever VCS you set up in Step 4**:

- **Git (GitHub / GitLab / local):**
  ```bash
  cd "<project-path>"
  git add -A
  git status                    # Library/ Temp/ obj/ Build/ must NOT be staged
  git commit -m "Initial Unity project: <Name>"
  ```
  Every `.cs`/asset is committed together with its `.meta`.
- **Unity Version Control (UVCS):** check in through the workspace from Step 4 — no `git` step;
  generated folders stay excluded by the ignore rules.

If Step 7 mirrored skills into the project, ask whether to include them. A Step 4 link without
`--no-initial-commit` already committed the bare project: add a follow-up commit.

## Step 9 — First playable loop

A `--bare` run skips this step. Otherwise the Step 8 commit is the restore point; build on it with
`craft-game-floors`'s floors in its order: the gameplay camera, placeholder assets, character motion
(floor 6), then **at least one playable loop** (floor 7): one verb, one visible goal, one pressure,
a win or lose state, and a restart, or the shape the user stated (a sandbox has no lose state). Close the
loop first, then build as far as the prompt reaches, one system per pass. Then the feel, audio, shell and live floors, and
its **done gate** last, scored on the running build. Commit with the Step 4 VCS, `.meta` files
included. `craft-game-floors` missing and declined → build the loop from the
shape above, skip the gate, gap named in the report.

## Step 10 — Publish

**Publishing is a human gate**: after Step 9's commit, ask once whether to upload to Unity Play,
say plainly what a yes uploads, and wait. It is public and on the user's account, so never assume it.

On a yes, follow [references/publish.md](references/publish.md): confirm the CLI has a publishing
command, build WebGL through a build profile, upload, and read `urlKind` from the JSON result.
Never retry an upload that may have been accepted, because that creates a second public listing.
A declined publish is a one-line note in the report with the route to run later, never a nag.

## Step 11 — Hand off

Based on Step 2 monetization, invoke the matching skill for the actual integration:
- IAP → **implement-in-app-purchases**
- Ads → **levelplay-unity-integration**
- Accounts / cloud save / economy / remote config / leaderboards → **build-live-game**

If it isn't loaded, point at the skill-setup install route; never integrate from memory.

**Offer the forward baton** (declared on the manifest; not on a `--bare` run): one idempotent line
for the project's `CLAUDE.md` or `AGENTS.md` — "When building gameplay, feel, audio, shell, or live
features, load the `craft-game-floors` skill; if it isn't installed, ask the user whether to run
`unity skill install <client>` (ask: never run it without a yes)." That file steers every later
agent session in the project, so **ask before writing it**, even in paved mode, and show the line.
On a yes, append it and commit the note — appended after the last commit, it otherwise never
reaches clones. On a no, put the line in the report for the user to add.

**Final check before the report.** Confirm the project has zero console errors with
`unity command console status` (on a `--bare` run without `com.unity.pipeline`, read `Editor.log`
instead; never install Pipeline for this), and stop there unless the user asked for proof or you suspect a
specific fault. To chase one, enter Play mode once and read state with `unity command script eval`
instead of adding logging, and take no screenshots. If a fix doesn't work, go back and reread the
step's delegated skill and `references/` file before trying again.

## Final report

Summarize what you did: project path, Editor version, pipeline and template, the manifest with its
reconciliation, identity state, the baseline, what the loop built, `craft-game-floors`'s done gate
scores, and the Unity Play link and kind (or Step 10's note). Disclose every side effect: packages
installed, terms accepted, sign-in, and files written such as the `CLAUDE.md` line. Then list the
user's follow-ups, any missing skill included.

## Guardrails

- **Do only what's asked. Don't change unrelated assets or files. Avoid long explanations.** Create one project and make only the changes the steps name, because the user will open this folder as their own.
- **Never leave verification code in the user's script. Never build logging or detection they didn't ask for.**
- **Restore point first.** Step 8's commit is the project's restore point. Make it before any gameplay work, and never rewrite or force-push it, so the user can return to a clean template.
- **Ask before anything public or global**: signing in, installing agent skills, writing to `CLAUDE.md` or `AGENTS.md`, and publishing, because each is hard to undo or affects more than this project.
- **Never run destructive commands** (`rm -rf`, `git reset --hard`, clearing the user's console) in the user's project.
- **End with the Final report**, listing what was and was not done and anything skipped, so nothing is silent.

## Scope

No command reference (syntax lives in `unity-cli` / `unity-package-management`), and no
re-deciding create's defaults: narrate them from the result.

## Common mistakes

Before you report, tick [references/checklist.md](references/checklist.md) and skim [references/common-mistakes.md](references/common-mistakes.md): backgrounding the create, hand-editing `manifest.json`, committing `Library/`, skipping the baseline, and publishing without a yes.
