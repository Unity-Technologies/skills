---
name: new-unity-project
description: Guides creating a new Unity project, gathering concept, platforms, and monetization before setting up the project, source control, and packages. Use when the user asks to start a new game or prototype.
allowed-tools:
  - Bash
  - Write
  - Edit
---

# New Unity Project

A flow from an idea to a running, version-controlled Unity project. This skill owns the **flow** —
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

## Mode: paved by default, guided on request

Run **paved** unless the user asks for the interview — in words ("ask me questions", "guide me")
or by pushing back on the manifest; mode is decided once per session. The paved contract:
**parse the prompt first** · **fill gaps from the Defaults table** · **print the manifest** before
the create launches · **proceed**, asking only on pushback or an unruled fork ·
**never consume a human gate** — sign-in and publishing always ask. A **bare/minimal** ask gets
`--bare`, skips the floors, and states the cost (identity, saves, ranks unwired).

In **guided** mode, ask only the current step's questions and wait. The paved additions (identity,
floors, baton) are asked at their steps there, never assumed.

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
| Craft floors | Always on, via the `craft-game-floors` skill: look, camera, lighting, feedback, audio, shell, live floor, done gate |

Cloud link and diagnostics are **not** a row: create applies its own defaults — narrate them from
the result, don't re-decide.

## The assumptions manifest

Print exactly one block at the end of Step 2, before the create launches. It **predicts** what
create will do (the URP template per `--template 3d|2d`, never a guessed id):

> **Paved assumptions:** Unity <editor> · <URP template, 2D/3D> · <platforms> · <monetization> ·
> anonymous player accounts (create's, else one bootstrap script) · <version control> ·
> cloud-linked with diagnostics (create's defaults, narrated from the result) · floor packages
> (Cinemachine; identity + live services when cloud-linked) · craft floors on via `craft-game-floors` ·
> a one-line handoff note appended to the project's CLAUDE.md.
> Say the word to change any of these; otherwise I'm proceeding.

One block, every assumption present, each reversible, nothing implied; Step 3 reconciles the
result against it.

## Before Step 1: check the delegated skills are loaded

Look for `unity-cli`, `unity-package-management`, `urp-postprocessing`, `ui`, `build-live-game`
and `craft-game-floors` in your client's skills. If any is missing, say which and what it does, and offer the install routes
in [references/skill-setup.md](references/skill-setup.md) — never install without a yes. Don't
stall: a decline gets the fallback at its step, gap named in the report. `unity skill show` prints
`unity-cli` without installing; without `unity-package-management`, Step 5 can't run — say so
before Step 2.

## The flow — and where the parallelism is

1. **Concept** (paved: parsed from the prompt). 2. **Platforms & monetization** — print the
manifest (paved), then **start the create in the background**: `projects create --install-editor`
installs the Editor + modules (minutes), then creates the project; keep talking meanwhile.
3. **(joins)** reconcile the result against the manifest. 4. **Source control.** 5. **Packages.**
6. **Player identity, then the craft floors** — confirm create's identity or wire it; load
`craft-game-floors`. 7. **Visual baseline** on the craft skill's art bible. 8. **Save & first commit.**
9. **Hand off** + the forward baton.

The multi-minute Editor install overlaps the user's answers, so setup feels instant.

## Step 1 — Concept

**Paved:** the prompt is the spec — parse genre, dimension (2D/3D), look, scope, and name into a
2–4 line brief; fill gaps from the Defaults table; no read-back.

**Guided:** ask with your client's multiple-choice tool (`AskUserQuestion` in Claude Code,
`ask_user` in Copilot CLI, `askQuestions` in VS Code); without one, plain text.
Cover **genre / core loop** · **dimension & look** (2D or 3D; pixel, low-poly, stylized,
realistic) · **gameplay** (the one-sentence "what the player does moment to moment") · **scope**
(prototype vs. multi-scene; single- or multiplayer). Settle a **project name**, write the brief,
read it back. The brief drives the template (Step 2), packages (Step 5) and the baseline (Step 7):
note the palette / mood words and pixel-art looks.

## Step 2 — Platforms & monetization, then start installing

Two decisions (paved: filled from the Defaults table, then **print the manifest**):

- **Target platforms**: Desktop, Mobile (iOS/Android), WebGL, Console — they map to Editor
  **modules** (`--module`) and argue for leaner packages on mobile/WebGL.
- **Monetization**: none / premium / IAP / ads / mix — this only decides the Step 9 handoff.

Default to an explicit `--editor-version latest` unless the user names a version — without the
flag, create prefers a stored default or an older installed Editor.

Confirm prerequisites, then **start the create as a background task** (`unity-cli` has module
names and auth / license setup):

```bash
unity --version
unity auth status --format json      # if signed out:  unity auth login
unity license status --format json   # if none active: unity license activate

# Start in the BACKGROUND. Module names per platform are in the unity-cli skill.
# --bare only on the user's ask (see Mode).
unity projects create "<Name>" --path <dir> --editor-version latest \
  --template <3d|2d> --install-editor --module <platform-modules> --accept-eula --format json
```

The command waits for the install, then creates the project, so its one result covers both. If you
have nothing left to ask, it's fine to just wait.

**Template:** `--template 3d` / `2d` resolve to the version's URP templates
(`com.unity.template.urp-blank`, `com.unity.template.universal-2d`), the default for every game.
Built-in templates (`com.unity.template.3d`/`.2d` as full ids) are deprecated from 6.5, removed in
6.7 — pass one only when the user explicitly asks for Built-in.

## Step 3 — Join: project ready, result reconciled

Wait for the background create and read its JSON result: `data.path` is the project,
`data.version` the Editor, `data.editorInstalled` whether this run installed it.

**Paved: reconcile the result against the manifest**, narrating divergence — the cloud-link
soft-fail included (`cloudLinked: false`, warnings, project intact; Step 6 keys off it). A 403 on
the link is often the saved default org refusing new projects — retry
`unity projects link cloud <path> --cloud-org <an org the user owns>` before concluding
no-identity. A result carrying a `steps` field reconciles step-by-step from it.

`--module` only applies when the run installs the Editor: on `data.editorInstalled: false`, add
the platform modules with `unity install-modules -e <version> -m <platform-modules> --accept-eula`
(skips what's already there). If create failed, surface the error (`unity-cli` troubleshooting)
and stop — a failed install creates no project, and nothing downstream works without an Editor.

## Step 4 — Source control

**Paved:** local git + Unity `.gitignore`. **Guided:** ask — Git (GitHub / GitLab; `--git-lfs` for
asset-heavy games), **Unity Version Control** (`--vcs uvcs` — large binaries natively, no LFS), or
purely local. Attach with `unity projects link vcs "<project-path>" --vcs …
--git-token-from-stdin --no-initial-commit` (tokens on stdin) — **`--no-initial-commit`** keeps
the CLI from committing the bare project before packages and `.meta` files exist; the real first
commit is Step 8. Exact flags: the `unity-cli` workflow.

## Step 5 — Packages

Map the brief to a package list and install via **`unity-package-management`** (**never**
hand-edit `manifest.json`): `package add` once the Editor is open with `com.unity.pipeline`
(Step 7); its C# installer script when packages must land pre-open (the `-quit` gotcha applies).
That skill has the genre/platform/monetization → package mapping. Paved
adds the floors' needs: Cinemachine for any gameplay camera (2D rigs included);
`com.unity.services.authentication` when Step 6 wires identity; and the live floor's services set
(cloud save, leaderboards, remote config) on cloud-linked projects — in this pass, not at shell time. The template already provides the
render pipeline — never add `com.unity.render-pipelines.universal` on a Built-in template
(materials go pink) or vice versa. Read the list back before installing (paved: print it now);
verify `manifest.json` after.

## Step 6 — Player identity, then the craft floors

**Identity — the create result is the switch** (a `--bare` run skips this whole step: bare
projects still cloud-link unless `--no-cloud`, and the unwired cost was stated at Mode time):
identity addressed in the result → confirm it in the reconciliation and **skip**, never wire auth
twice. No identity and the project cloud-linked →
wire it: services init plus anonymous sign-in kicked off at bootstrap in **one small bootstrap
script**, never blocked on (mechanics: **`build-live-game`**, auth section — checked in Before
Step 1; missing and declined → skip the wiring, gap named); each service dependency is a named
plan line, declining removes only that line. Cloud link failed or skipped →
skip with the cost stated (saves and ranks stay local) — never wire services init against a
project with no cloud id. Invisible to players, upgradeable later — the root the live floor
builds on.

**A `--bare` run skips the floors too** (the Mode section's stated cost). Otherwise **load the
`craft-game-floors` skill** (checked in Before Step 1) and follow its floors through the
build — art bible first, then camera, lighting, feedback, audio, shell, live floor, its done gate.
Its values supersede the inline fallbacks below whenever present, version skew included. Missing
and declined → fallbacks, gap named in the report.

## Step 7 — Visual baseline

A fresh template renders correctly but looks like a default; left there, agents reach for `OnGUI`
and guess shader names. Apply this floor **before** any gameplay work.

**First, `unity pipeline install --project-path "<project-path>"`, before opening the
Editor.** Running C# in the Editor and `unity command capture screenshot` below both need the
project's `com.unity.pipeline` package. Create adds it by default on newer CLIs (this install then
changes nothing); older CLIs leave the project without it, and Step 5's `-batchmode` installer
never touches it. Install-before-open is the supported order; without the package, everything below fails to
connect (it mimics `unity-cli`'s Safe Mode failure). The failure codes
(`PIPELINE_MANIFEST_WRITE_FAILED`, a stuck `STATUS_PIPELINE_LOAD_PENDING`) and the stuck-Editor
recovery are tabled in [references/skill-setup.md](references/skill-setup.md).

**Then** `unity open "<project-path>"`, wait until `unity status` reports the Editor ready, and
apply each item below by running C# in it — **`unity-cli` owns those commands**; don't re-derive
them here.

Once the Editor is ready, read the `unity-pipeline` guide if your skills don't list it —
locations and mirroring routes: [references/skill-setup.md](references/skill-setup.md).

**Items 1, 2, 5 and 6 assume a URP template**, the Step 2 default. If the user explicitly chose
Built-in, don't run them as written — `UniversalAdditionalCameraData`, `Light2D`, the
`urp-postprocessing` volume framework and `Universal Render Pipeline/*` shaders don't exist there;
post-processing means the legacy Post Processing Stack, the shader names are `Standard` /
`Unlit/Color`, and items 3–4 apply as written. Don't reach for `migrate-birp-to-urp` — the user
asked for Built-in.

1. **Post-processing.** Global Volume — `craft-game-floors`'s scene-lighting values when loaded (they
   supersede these); fallback: Tonemapping (ACES), Bloom intensity 0.5–1 @ threshold 0.9, Vignette
   ≈ 0.25; set `renderPostProcessing = true` on the main camera's `UniversalAdditionalCameraData`.
   **REQUIRED SUB-SKILL:** `urp-postprocessing` — its templates create the volume and check HDR and
   the layer mask; if it isn't loaded, do both yourself from these values.
2. **Camera and light.** 2D → orthographic, solid background from the brief's palette, a `Light2D`
   of type Global if the template scene has none (Sprite-Lit shaders render black without one).
   3D → perspective, main-light shadows on; `craft-game-floors`'s lighting pass replaces the template's
   default light story when loaded, else keep the template's directional light and skybox.
3. **Quality tier.** Read `QualitySettings.names` first — tier names differ per template — then
   `QualitySettings.SetQualityLevel` to the one matching the primary target: the highest tier for
   desktop, the lowest for mobile/WebGL. Leave color space Linear and the Input System as the
   template set them.
4. **UI stack.** HUD and menus use a uGUI Canvas + TextMeshPro or UI Toolkit — **never `OnGUI`**.
   **REQUIRED SUB-SKILL:** `ui` picks between them for the project. If it isn't loaded, choose
   between them from the brief and say which you chose, and why, in your final report.
5. **Materials and shaders.** Under URP use `Universal Render Pipeline/Lit`, `…/Unlit`,
   `…/2D/Sprite-Lit-Default` or `…/2D/Sprite-Unlit-Default`. `Standard` and `Unlit/Color` render
   pink or washed out under URP. Prefer `GraphicsSettings.currentRenderPipeline.defaultMaterial`
   / `.default2DMaterial` over `Shader.Find`, and treat a `null` from `Shader.Find` as an error.
   `currentRenderPipeline`, not `defaultRenderPipeline`: a quality tier can carry its own pipeline
   asset, and item 3 above just set the tier.
6. **Pixel art only.** Point filter mode on sprites and a Pixel Perfect Camera — see
   `2d-pixel-perfect`. If missing, do both yourself and note the skill-setup install route in the
   report.

Save the scene, then confirm both of these — **not with `unity logs`**, which reads the CLI's own
log and reports clean whatever the scene looks like:

- No render-pipeline or shader errors Editor-side. Read the Editor console through `unity-cli`,
  or read `Editor.log` directly — that skill's "Recovering from Safe Mode" section has the
  per-platform paths.
- `unity command capture screenshot --output baseline.png` looks lit and tonemapped rather than flat
  gray.

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

If Step 7 mirrored skills into the project, ask whether to include them. If Step 4 linked
**without** `--no-initial-commit`, the CLI already committed the bare project — add a follow-up
commit rather than double-committing.

## Step 9 — Hand off

Based on Step 2 monetization, invoke the matching skill for the actual integration:
- IAP → **implement-in-app-purchases**
- Ads → **levelplay-unity-integration**
- Accounts / cloud save / economy / remote config / leaderboards → **build-live-game**

If the matching skill isn't loaded, point the next steps at the skill-setup install route rather
than integrating from memory.

**Write the forward baton** (declared on the manifest): one idempotent line appended to the
project's `CLAUDE.md` or `AGENTS.md` — "When building gameplay, feel, audio, shell, or live
features, load the `craft-game-floors` skill; if it isn't installed, ask the user whether to run
`unity skill install <client>` (ask: never run it without a yes)." Commit the note —
appended after Step 8, it otherwise never reaches clones. Later sessions reach the floors through
it without this skill re-triggering.

Report the project path, Editor version, pipeline and template, packages, the manifest with its
reconciliation, identity state, the baseline applied, any missing skill, `craft-game-floors`'s done gate
as the build's finish line, and next steps.

## Scope — what this skill does NOT do

- **No gameplay scaffolding.** A running, wired project with a visual floor; the game is the next
  conversation (with `craft-game-floors`). One exception: Step 6's single bootstrap script.
- **No command reference.** Syntax lives in `unity-cli` / `unity-package-management`.
- **No re-deciding create's defaults** — narrate them from the result. The Defaults table is this
  file's only opinions; the guided interview survives untouched.

## Checklist

- [ ] Delegated skills checked before Step 1 (incl. `craft-game-floors`): a missing one offered, or named
      in the report
- [ ] Mode decided once; in paved, the manifest printed before the create launches
- [ ] Brief captured; platforms + monetization recorded
- [ ] Background create with `--install-editor` + modules, URP template (`--template 3d`/`2d`)
      unless Built-in was requested; `--bare` only on the user's ask; result reconciled
- [ ] Source control attached (Step 4), or local git + Unity `.gitignore`
- [ ] Packages via `package add` (or the C# script pre-open); `manifest.json` verified; no
      render-pipeline package atop the template
- [ ] Identity confirmed from the result or wired in Step 6 (stated cost on a failed link)
- [ ] `craft-game-floors` loaded (or its absence named); `unity-pipeline` read once the Editor was ready
- [ ] Baseline applied: Volume + camera post on, quality tier, UI stack (no `OnGUI`), URP shaders,
      capture checked
- [ ] `.meta` files exist; first commit made; `Library/` excluded
- [ ] Forward baton appended; handoff done if applicable

## Common mistakes

- **Blocking on the create** instead of backgrounding it: with `--install-editor` it waits for the
  whole Editor install.
- **Acting before the manifest prints** (paved), or burying an assumption outside it — if it isn't
  in the manifest, it isn't assumed.
- **Wiring auth twice** — if create's result shows identity, Step 6's wiring is a skip.
- **Hand-editing `manifest.json`** instead of the package commands or the skill's script.
- **Doing a required sub-skill's job from memory** when it isn't loaded, without telling the user
  or offering the install.
- **Committing `Library/`/`Temp/`/`obj/`/`Build/`**, or scripts without their `.meta` files.
- **Missing Editor modules** — a mobile target needs `android`/`ios`; WebGL needs `webgl`.
- **Picking `com.unity.template.2d` / `.3d` by name** — Built-in templates; use `--template 3d`/`2d`.
- **Skipping the visual baseline** — flat template defaults, `OnGUI` HUDs, and
  `Shader.Find("Standard")` under URP.
