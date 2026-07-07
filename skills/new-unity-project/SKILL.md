---
name: new-unity-project
description: Use when starting a brand-new Unity game or project from scratch — the user says "make/start/create a new game", "bootstrap a Unity project", "I want to build a <genre> game", "scaffold a Unity project", or otherwise has no existing project yet. Walks from an idea to a running, version-controlled Unity project — it gathers the game concept, target platforms and monetization, installs the Unity CLI and Editor, creates the project and source control, installs the right packages via the C# PackageManager Client API, scaffolds a game skeleton, saves, and commits. Triggers on new game, greenfield, from scratch, prototype, game jam, blank project, project setup, scaffold, bootstrap.
allowed-tools:
  - Bash
  - Read
  - Write
  - Edit
  - AskUserQuestion
---

# New Unity Project

Take a user from an idea to a running, version-controlled Unity project with the right
packages installed and a game skeleton in place.

**Work the steps in order, one at a time.** Ask only the questions for the current step —
do not gather information for later steps in advance. Wait for the user at each checkpoint
before proceeding. Every Unity CLI operation in this skill is documented in the
**`unity-cli`** skill; read it whenever you need exact command syntax or flags.

## Overview of the flow

1. **Concept** — what they want to build (genre, look, gameplay, UX).
2. **Ship & money** — target platforms and monetization.
3. **CLI** — install the Unity CLI if it isn't available.
4. **Editor** — install a supported Unity Editor (or the version they pick).
5. **Project + source control** — create the project and initialize git.
6. **Packages** — determine and install the right packages via the C# PackageManager Client API.
7. **Skeleton** — scaffold folders, a bootstrap scene, and starter scripts.
8. **Save & commit** — import, save, and make the first commit.

---

## Step 1 — What do they want to build?

Capture the game concept. Use `AskUserQuestion` so the user can pick fast, but let them
answer freely too. Cover:

- **Genre / core loop** — platformer, top-down shooter, puzzle, idle/clicker, RPG, racing,
  card/board, tower defense, sim, hyper-casual, first-person, etc.
- **Dimension & look** — 2D or 3D; art style (pixel, low-poly, stylized, realistic, flat/UI-only).
- **Gameplay** — the one-sentence "what the player does moment to moment."
- **UX / scope** — single-screen prototype vs. multi-scene game; menus, HUD, save system;
  single-player or multiplayer.

Write the answers down in a short **project brief** (2–4 lines) and read it back to the user
to confirm before moving on. This brief drives package selection (Step 6) and the skeleton
(Step 7). Also settle on a **project name** here.

## Step 2 — When do they ship, and how do they make money?

Two decisions, because both change which packages you install and which Editor modules the
Editor needs.

- **Target platforms** (multi-select): Desktop (Windows / macOS / Linux), Mobile (iOS /
  Android), WebGL, Console. Note whether a release date/deadline matters (a game jam vs. a
  commercial launch pushes toward LTS in Step 4).
- **Monetization**: none / premium (paid up-front), in-app purchases, ads, or a mix.
  - IAP → the **`implement-in-app-purchases`** skill (`com.unity.purchasing`).
  - Ads → the **`levelplay-unity-integration`** skill (`com.unity.services.levelplay`).
  - Live-service / backend (accounts, cloud save, economy, remote config) → the
    **`build-live-game`** skill.

Record platforms and monetization in the brief. Don't integrate monetization yet — you note
the packages now (Step 6) and hand off to the dedicated skills after the skeleton exists.

## Step 3 — Install the Unity CLI

Every later step drives the `unity` CLI. Check for it and install if missing:

```bash
which unity && unity --version
```

If it's not found, install it (see the **`unity-cli`** skill for platform-specific commands
and troubleshooting), then open a new shell and re-verify `unity --version`. If it still
isn't on PATH, tell the user and stop — nothing downstream will work.

Also confirm the user is signed in and licensed, since editor install and project creation
need it:

```bash
unity auth status --format json
unity license status --format json
```

If not signed in, have the user run `unity auth login` (it prints a URL). If no license is
active, guide them through `unity license activate` (see the `unity-cli` skill for personal
vs. serial vs. floating).

## Step 4 — Choose and install the Editor

Ask which Editor version to use, and explain the trade-offs first. See
[references/editor-versions.md](references/editor-versions.md) for the LTS vs. Supported vs.
Pre-release explanation and how to list real current versions with `unity releases`.

**Default recommendation: the latest LTS**, unless the user needs a feature only in a newer
Supported/Tech release, or is deliberately evaluating a pre-release.

Show the actual options, then install the chosen version with the platform modules implied by
Step 2 (e.g. `--module android` / `--module ios` for mobile, `--module webgl` for WebGL):

```bash
# See what's available (LTS shown; also try --stream tech / --stream beta)
unity releases --stream lts --limit 5 --format json

# Install the chosen version + platform modules (installs the latest LTS with the `lts` alias)
unity install <version> --module android --module ios --yes --accept-eula
```

Confirm it landed with `unity editors --installed --format json`.

## Step 5 — Create the project and set up source control

Pick a template that matches the concept (2D vs. 3D, URP vs. built-in vs. HDRP). List what
the chosen Editor actually offers, then create the project:

```bash
# List the real template ids this editor offers — don't guess them. Common ones include
# com.unity.template.3d, com.unity.template.2d, and a URP template (id varies by version).
unity templates list --editor <version> --format json

# Create the project (first positional arg is the NAME; --path sets the parent directory)
unity projects new "<ProjectName>" --path <parent-dir> --editor-version <version> \
  --template <template-id>
```

**Source control.** Initialize git with a Unity-appropriate ignore so the multi-GB `Library/`
and other generated folders never get committed:

```bash
cd <parent-dir>/<ProjectName>
git init -b main
curl -fsSL https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore -o .gitignore
# If offline, write a minimal .gitignore ignoring: [Ll]ibrary/ [Tt]emp/ [Oo]bj/ [Bb]uild/
# [Bb]uilds/ [Ll]ogs/ [Uu]serSettings/ *.csproj *.sln
```

Recommend **Git LFS** for binary assets (textures, audio, models) if the game is asset-heavy —
`git lfs install` then track `*.png *.psd *.fbx *.wav *.mp3` etc. in `.gitattributes`.

To publish straight to a remote (GitHub/GitLab/Unity Version Control), the CLI can do it in one
step via `unity projects create ... --vcs github ...` or `unity projects link vcs` — see the
`unity-cli` skill. Only do this if the user wants a remote now and has provided a token.

**Do the first commit later (Step 8)** — after packages, skeleton, and `.meta` files exist.

## Step 6 — Determine and install packages via the C# PackageManager Client API

Map the brief (Step 1) and platforms/monetization (Step 2) to a concrete package list. See
[references/select-packages.md](references/select-packages.md) for the genre/look/platform →
package mapping and version guidance.

Install them from C# using `UnityEditor.PackageManager.Client` — **not** by hand-editing
`Packages/manifest.json`. Write a small editor script into the project and run it headless.
The full, tested installer script and the exact `unity run` invocation are in
[references/package-manager-api.md](references/package-manager-api.md).

The pattern, briefly:

1. Write `Assets/Editor/ProjectBootstrap/PackageInstaller.cs` containing a static method that
   calls `Client.AddAndRemove(packagesToAdd: ids)` and pumps `EditorApplication.update` until
   the request completes, then `EditorApplication.Exit(<code>)`.
2. Run it headless by launching the **Editor binary directly** in `-batchmode` **without**
   `-quit` (the `Client.Add*` call is async and the Editor must stay alive until it finishes;
   `unity run` can't be used because it always injects `-quit`). The exact binary-resolution
   and command are in [references/package-manager-api.md](references/package-manager-api.md).
3. Verify the resulting `Packages/manifest.json` lists every requested package and that the
   run exited 0.

Hand off monetization/backend package *integration* (not just install) to the dedicated skills
after the skeleton exists.

## Step 7 — Scaffold the game skeleton

Create a minimal but runnable structure matched to the concept. See
[references/game-skeleton.md](references/game-skeleton.md) for the folder layout, assembly
definition, a `GameManager` bootstrap, and genre-specific starter scripts.

At minimum:

- A consistent `Assets/` folder layout (`Scripts/`, `Scenes/`, `Prefabs/`, `Art/`, `Audio/`,
  `UI/`, `Settings/`).
- An assembly definition so scripts compile into a named assembly.
- A bootstrap `GameManager` and one genre-appropriate gameplay script (e.g. a
  `PlayerController` for a platformer, a `BoardController` for a puzzle).
- An initial scene wired to the bootstrap, set as the first scene in Build Settings.

Write these as normal `.cs` / asset files on disk. Unity generates `.meta` files and imports
them on the next Editor launch (Step 8).

## Step 8 — Save, import, and commit

Open (or headless-run) the project once so Unity imports the new scripts and assets and
generates every `.meta` file — these MUST be committed alongside their asset:

```bash
# Headless import + asset-database save (see references/package-manager-api.md for the method)
unity run "<project-path>" --editor-version <version> \
  -- -executeMethod ProjectBootstrap.ProjectSaver.SaveAll
# ...or simply open it once so the user can see it:  unity open "<project-path>"
```

Then commit everything:

```bash
cd "<project-path>"
git add -A
git status            # sanity-check: Library/ etc. must NOT be staged
git commit -m "Initial Unity project: <ProjectName>

<one line on genre + target platforms + key packages>"
```

Verify the ignore worked (`git ls-files | grep -c '^Library/'` must be `0`). Report the
project path, Editor version, installed packages, and next steps — including which
monetization/backend skill to run next (from Step 2).

---

## Checklist

- [ ] Project brief captured and confirmed (genre, look, gameplay, UX, name)
- [ ] Target platforms + monetization recorded
- [ ] `unity` CLI installed, user signed in and licensed
- [ ] Editor version chosen (LTS by default) and installed with the right platform modules
- [ ] Project created from an appropriate template; git initialized with a Unity `.gitignore`
- [ ] Packages installed via the C# PackageManager Client API; `manifest.json` verified
- [ ] Skeleton scaffolded (folders, asmdef, GameManager, one gameplay script, a scene)
- [ ] Project imported/saved so `.meta` files exist
- [ ] First commit made; `Library/` and generated folders excluded
- [ ] Handed off to the monetization/backend skill(s) if applicable

## Common mistakes

- **Committing `Library/`, `Temp/`, `obj/`, `Build/`.** Always write the `.gitignore` before
  the first `git add`. These are large and machine-specific.
- **Committing scripts without their `.meta` files** (or vice versa). Import the project once
  (Step 8) so meta files exist, then `git add -A`.
- **Hand-editing `manifest.json`** instead of using the PackageManager Client API — the user
  asked for the API, and it resolves dependencies/versions correctly.
- **Missing Editor modules.** A mobile target needs the `android`/`ios` modules; WebGL needs
  `webgl`. Installing the Editor without them means the project can't build for the target.
- **Gathering all questions up front.** Ask per step; platform/monetization answers change the
  Editor modules and package list, so don't scaffold before they're settled.
