---
name: craft-game-floors
description: "Makes a Unity game look and feel finished: art style, lighting, juice, sound, menus, saves and look scoring. Use when building a new game's first playable version, or when a game looks like programmer art, feels flat or silent, or needs polish."
allowed-tools:
  - Bash
  - Read
  - Write
  - Edit
---

# Craft Game Floors

A functional game that looks and feels like programmer art reads as a failure to the person who
asked for it. This skill is the craft layer: **floors** — disciplines where every game needs a
minimum and the engine's defaults are unacceptable — each carried by a reference below with
opinionated values, worked examples to imitate, and a verify step. The floors are the heart of the
**paved** path (Unity's default scaffolding for agent-built games: assumptions declared up front,
reversible on a word); in this skill, "paved" simply means these floors are always on unless the
user declines one.

**Before starting any floor or diagnosis, open and read the matching reference below and the Unity
docs pages it links** — your engine knowledge may be out of date, wrong, or for a different
version. Name the files and pages you read in your report. **Do only what's asked. Don't change
unrelated assets or files. Avoid long explanations.**

Two ways in, same floors:

- **During project creation** — the `new-unity-project` skill loads this one and carries the
  floors through the build in the order below.
- **Mid-project, on any craft ask** ("fix the look", "add sound", "it feels flat", "make a title
  screen", "score it") — jump straight to the matching reference, after the mid-flow check below.

**Scope by the ask**: a request that authorizes changes ("fix", "add", "polish") gets the floor
applied; an assessment-only ask ("score it", "what should I fix?") gets the scored board and
recommendations, with no project modifications until the user says go.

## Entering mid-flow

The references assume an **art bible** (style statement + fixed palette) and, where audio meets
UI, the **mixer handshake** (exposed params `MusicVol`/`SFXVol`). On an existing project, run
these in order before the asked-for floor:

1. **Restore point** — confirm a clean commit to roll back to, or create one, before any change.
2. **Render pipeline** — read `GraphicsSettings.currentRenderPipeline`: the look floors' verifies
   are written for URP; on Built-in or HDRP, apply the opinions, name the pipeline gap, and skip
   the URP-typed checks.
3. **Art bible** — look for a `Palette` type or an art-bible note (CLAUDE.md, docs); if none
   exists, write the three-line bible from the game's current look first — retro-tinting later is
   the expensive order.
4. **Identity (live floor only)** — verify player identity with a live probe (services
   initialized and a PlayerId present), never by assuming an earlier setup step ran.
5. **Scope the done gate** to the surface you touched: a title-screen session scores the shell
   and look axes, not the whole board.

**A floor fills an engine default, never a developer's choice.** If the project already has its
own controller, camera, shader, palette or style, keep it, apply only the floor's opinions that
don't conflict, and name the difference in the report. Write an art bible only from choices the
developer has made, and ask before adding one mid-project.

## The floors — numbered in build order, read each at its moment

1. [art-direction](references/art-direction.md) — before the first material, light, or UI element
2. [scene-lighting](references/scene-lighting.md) — with the visual baseline (3D)
3. [toon-shading](references/toon-shading.md) — when the art bible picks a cel/toon style (3D)
4. [camera-rigs](references/camera-rigs.md) — first gameplay camera (has a 2D section)
5. [placeholder-assets](references/placeholder-assets.md) — first missing asset
6. [character-motion](references/character-motion.md) — first moving character (3D-leaning)
7. *(the playable loop lands here — loop before content, one system per pass)*
8. [game-feel](references/game-feel.md) — the feel pass, after the loop
9. [vfx](references/vfx.md) — with the feel pass (has a 2D section)
10. [game-audio](references/game-audio.md) — before done
11. [game-shell](references/game-shell.md) — before done (2D backdrop delta). Before the first TextMeshPro text, import TMP Essentials (see the reference's failure table).
12. [live-floor](references/live-floor.md) — with the shell (services before gameplay code)
13. [visual-scorecard](references/visual-scorecard.md) — **the done gate, always last; run it once a playable loop exists.** During project creation, stop at the lit-baseline capture and skip the gate.

Mid-project asks enter at the matching number and pull earlier floors only as the mid-flow check
requires (the bible, the pipeline, the identity probe).

**Resolving `<VERSION>` in the references' doc links** — prefer WebFetch of the exact page over
WebSearch: for `docs.unity.com/en-us/engine/<VERSION>` manual and script-reference pages, use the
project's Editor stream (e.g. `6000.3`) from `ProjectSettings/ProjectVersion.txt`, and the same
stream for the remaining `docs.unity3d.com/<VERSION>/Documentation` pages; for
`docs.unity3d.com/Packages/<package>@<VERSION>` pages, use the installed package's major.minor
from `Packages/manifest.json` or the `Library/PackageCache` folder name; the
`docs.unity.com/en-us/<service>.md` services pages are unversioned.

Routing follows the game's 2D/3D nature; **the 2D look floor (sprite art direction, 2D lights,
sprite placeholders) is not yet covered** — on a 2D game, say so at those beats rather than
applying 3D instructions. The scorecard's 2D anchor set still applies.

## The boundary (what this skill is not)

This skill owns **opinionated creation and floors** — which values, in what order, to what bar.
Catalog neighbours own **mechanics on existing setups**: volume/post mechanics →
`urp-postprocessing` · routing sources into existing mixer groups → `audio-setup-mixers` · audio
import/memory → `optimize-audio` · UI framework choice and construction → `ui` · font assets →
`optimize-text-mesh-pro` · backend services → `build-live-game` · pixel-perfect 2D →
`2d-pixel-perfect`. When an ask is purely mechanical, route there; when it's "make it good," it's
this skill, which then delegates the mechanics. If this index wants to grow, the new thing is a
catalog skill routed by the prompt, not another floor — a floor must apply to *every* game.

## The done gate

A craft session ends with [visual-scorecard](references/visual-scorecard.md)'s board — scored
frames, not a green compile. In order:

1. **Score** all six axes per the scorecard. All six at 3 or better is the floor.
2. **Fix pass** — anything below 3 gets its fix before done (assessment-only asks: scores and
   recommendations, no fixes). If a fix doesn't work, reread the relevant reference and its
   linked documentation before trying variations.
3. **Console** — confirm zero console errors.
4. **Play check** — enter Play mode and check the touched behavior with `Debug.Log` lines you
   added for the purpose. If the fix pass or this check fails, reread the relevant reference and
   its linked documentation before trying variations.

## Final report

State: which floors ran and which references were read · the board (or the scoped axes) before and
after the fix pass · every incidental side effect — packages installed, files or settings edited,
assets imported — and any follow-up the user needs · any floor skipped or gap named (the 2D look
floor, a declined install, a missing neighbour skill), so the user knows what deeper guidance was
skipped.

## Common mistakes

- **Polishing without a bible** — fixing one object's look against no palette; write the
  three-line bible first, even mid-project.
- **Replacing a developer's choice with a floor recipe** — an existing custom controller,
  camera, shader or palette stays; the fix pass never swaps it for the floor's default.
- **Treating a floor as a feature** — floors are defaults; the user declines one with a word, and
  the decline is recorded in the report, never argued.
- **Running the mechanics from memory** when a neighbour skill owns them — route and delegate.
- **Skipping the done gate** because the change was small — scope it down instead.
- **Applying 3D look instructions to a 2D game** — name the gap instead.
