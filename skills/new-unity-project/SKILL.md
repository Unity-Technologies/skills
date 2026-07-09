---
name: new-unity-project
description: Use when starting a brand-new Unity game or project from scratch — "make/start/create a new game", "bootstrap a Unity project", "I want to build a <genre> game", "scaffold/prototype a game", game jam, greenfield, blank project, project setup. A guided flow that gathers the concept, target platforms, and monetization, installs the Editor in the background while it asks, then creates the project and source control and installs packages — delegating the mechanics to the unity-cli and unity-package-management skills and handing off monetization to the dedicated skills. Does not scaffold gameplay code.
allowed-tools:
  - Bash
  - Read
  - Write
  - Edit
  - AskUserQuestion
---

# New Unity Project

A guided flow from an idea to a running, version-controlled Unity project. This skill owns the
**flow** — the questions, their ordering, running slow installs in the background while you ask,
and the handoffs. It deliberately does **not** re-document commands; it delegates the mechanics
to other skills.

**Delegates to (read these for the actual commands — don't reinvent them):**
- **`unity-cli`** — CLI install, auth/license, Editor install, project creation, source control,
  opening the project. Its "Bootstrap a new project from scratch" workflow is the backbone here.
- **`unity-package-management`** — installing packages via the C# PackageManager Client API, and
  choosing packages by genre / platform / monetization.
- **`implement-in-app-purchases`**, **`levelplay-unity-integration`**, **`build-live-game`** —
  monetization / backend *integration* (invoked at the end).

**Work one step at a time.** Ask only the current step's questions and wait for the user before
moving on — platform and monetization answers change what you install, so don't gather everything
up front or scaffold before they're settled.

## The flow — and where the parallelism is

1. **Concept** — what they're building.
2. **Platforms & monetization** — then, as soon as platforms are known, **kick off the Editor
   install in the background** (it takes minutes) and keep talking.
3. **(joins)** Editor + platform modules finish installing.
4. **Project + source control** — create from a matching template; init git.
5. **Packages** — install via the C# Client API.
6. **Save & first commit.**
7. **Hand off** monetization / backend.

The whole point of a guided flow over a raw recipe: the multi-minute Editor install overlaps the
minutes the user spends answering concept questions, so setup feels instant.

## Step 1 — Concept

Use `AskUserQuestion` so the user can pick fast, but let them answer freely too. Cover:

- **Genre / core loop** — platformer, top-down shooter, puzzle, idle, RPG, racing, card, tower
  defense, sim, hyper-casual, first-person, etc.
- **Dimension & look** — 2D or 3D; art style (pixel, low-poly, stylized, realistic, UI-only).
- **Gameplay** — the one-sentence "what the player does moment to moment."
- **Scope** — single-screen prototype vs. multi-scene game; single-player or multiplayer.

Also settle on a **project name**. Write a 2–4 line **project brief**, read it back to confirm.
The brief drives template choice (Step 4) and packages (Step 5).

## Step 2 — Platforms & monetization, then start installing

Two decisions, because both change what you install:

- **Target platforms** (multi-select): Desktop (Win/macOS/Linux), Mobile (iOS/Android), WebGL,
  Console. These map to Editor **modules** (Step 3) and argue for leaner packages on mobile/WebGL.
- **Monetization**: none / premium / in-app purchases / ads / mix. This only decides which
  handoff skill you invoke in Step 7 — don't integrate it now.

Confirm the Editor version to use (**default: latest LTS** — see `unity-cli` for the LTS vs. Tech
vs. beta trade-off). Ask this *now*, before kicking off the install, so you don't install the
wrong one.

Then confirm prerequisites and **launch the Editor install as a background task** so it runs while
you continue. See the `unity-cli` skill for exact syntax, module names per platform, and auth /
license setup:

```bash
unity --version
unity auth status --format json      # if signed out:  unity auth login
unity license status --format json   # if none active: unity license activate

# Start in the BACKGROUND, then go straight back to the conversation. Module names per platform
# (android / ios / webgl / …) are in the unity-cli skill.
unity install lts --module <platform-modules> --yes --accept-eula
```

Run that install as a **background task** (don't block on it). If you have nothing left to ask,
it's fine to just wait — the parallelism only helps when there's a conversation to overlap.

## Step 3 — Join: Editor ready

Before creating the project, confirm the background install finished:

```bash
unity editors --installed --format json
```

If it failed, surface the error (see `unity-cli` troubleshooting) and stop — nothing downstream
works without an Editor.

## Step 4 — Create the project + source control

Follow the **`unity-cli`** "Bootstrap a new project from scratch" workflow verbatim:

- List the **real** template ids the Editor offers (`unity templates list`) and pick one matching
  2D/3D and render pipeline from the brief — don't guess ids.
- Create with `unity projects create "<Name>" --path <dir> --editor-version <v> --template <id>`.
- Set up source control — **ask the user which they want**, don't assume: Git (GitHub / GitLab;
  add `--git-lfs` for asset-heavy games) or **Unity Version Control** (`--vcs uvcs`, which handles
  large binary assets natively — no LFS), or a purely local `git init` + Unity `.gitignore`.
  Publish in one step with `unity projects create --vcs … --git-token-stdin --no-initial-commit`
  (tokens on stdin). Pass **`--no-initial-commit`** so the CLI doesn't commit the bare project
  before packages and `.meta` files exist — you make the real first commit/check-in in Step 6.
  See the `unity-cli` workflow for exact flags.

## Step 5 — Packages

Map the brief to a concrete package list and install it via the **`unity-package-management`**
skill (C# PackageManager Client API — **never** hand-edit `manifest.json`). Read that skill for
the genre/platform/monetization → package mapping, the installer script, and the `-quit` gotcha.
Read the final list back to the user before installing; verify `manifest.json` afterward.

## Step 6 — Save & first commit

Open the project once so Unity imports the assets and generates every `.meta` file, then make
the first commit **with whichever VCS you set up in Step 4**:

```bash
unity open "<project-path>"     # imports + generates .meta; for headless/CI use the
                                # "Import & save headlessly" method in unity-package-management
```

- **Git (GitHub / GitLab / local):**
  ```bash
  cd "<project-path>"
  git add -A
  git status                    # Library/ Temp/ obj/ Build/ must NOT be staged
  git commit -m "Initial Unity project: <Name>"
  ```
  Every `.cs`/asset must be committed together with its `.meta`.
- **Unity Version Control (UVCS):** check in through your UVCS client/workspace (created during
  Step 4) — there's no `git` step. Generated folders are still excluded by the ignore rules.

If you published via `--vcs` in Step 4 **without** `--no-initial-commit`, the CLI already made an
initial commit of the bare project — add a follow-up commit here rather than double-committing.

## Step 7 — Hand off

Based on Step 2 monetization, invoke the matching skill for the actual integration:
- IAP → **implement-in-app-purchases**
- Ads → **levelplay-unity-integration**
- Accounts / cloud save / economy / remote config / leaderboards → **build-live-game**

Report the project path, Editor version, installed packages, and next steps.

## Scope — what this skill does NOT do

- **No gameplay scaffolding.** It gets you to a running, empty-but-wired project; building the
  actual game (scenes, controllers, art) is the next conversation — iterate there with the Editor
  via the `unity-cli` MCP server and the Package Manager. Generic genre skeletons tend to produce
  throwaway mocked primitives, so this skill intentionally stops at a clean starting point.
- **No command reference.** Syntax lives in `unity-cli` / `unity-package-management`.

## Checklist

- [ ] Concept brief captured and confirmed (genre, look, gameplay, scope, name)
- [ ] Platforms + monetization recorded; Editor version chosen
- [ ] Editor + platform modules installed (started in the background during Step 2)
- [ ] Project created from a matching template; git initialized with a Unity `.gitignore`
- [ ] Packages installed via the C# Client API; `manifest.json` verified
- [ ] Project opened/saved so `.meta` files exist; first commit made; `Library/` excluded
- [ ] Handed off to the monetization/backend skill if applicable

## Common mistakes

- **Blocking on the Editor install** instead of backgrounding it while you ask questions.
- **Installing the wrong Editor** because the version wasn't confirmed before the background install.
- **Gathering all questions up front** — platform/monetization answers change the modules and packages.
- **Hand-editing `manifest.json`** instead of using the Client API (see `unity-package-management`).
- **Committing `Library/`/`Temp/`/`obj/`/`Build/`**, or scripts without their `.meta` files.
- **Missing Editor modules** — a mobile target needs `android`/`ios`; WebGL needs `webgl`.
