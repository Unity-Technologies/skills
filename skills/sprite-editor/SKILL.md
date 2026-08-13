---
name: sprite-editor
description: Edits Unity sprite properties by generating C# editor scripts using ISpriteEditorDataProvider APIs. Handles sprite rectangles, borders, pivots, outlines, and slicing operations (automatic, grid, isometric). Use when working with sprite assets, sprite sheets, texture atlases, or sprite slicing.
modes: [agent, ask]
---

# Sprite Editor

Sprite metadata (rects, borders, pivots, outlines) lives inside the importer, not in a file
you can edit — reaching it means running C# through a live Editor.

**The `unity-cli` skill owns getting you there** — installing the CLI, confirming a connected
Editor, adding the project's `com.unity.pipeline` package, telling a genuinely absent Editor
apart from one stuck in Safe Mode, and discovering the Editor's command catalog. Follow it
first; don't re-derive any of it here.

Two things it can't know for you:

- **You need `eval` in particular**, not just a reachable Editor. Confirm it appears in the
  catalog — its presence depends on the Pipeline package version, not on the CLI.
- **Never hand-edit a `.meta` file to change sprite metadata.** The importer owns that data
  and the capability checks below exist to prevent corruption, so an unreachable Editor is a
  stop, not a cue to improvise.

Run C# through the connected Editor with the `eval` command. Discover its parameter shape
from `unity command --format json` rather than assuming one — the inline form is
`unity command eval --code '<snippet>'`, and some Pipeline versions also register
`eval_file` for running a snippet from a file. **Check the catalog before reaching for
`eval_file`; it is frequently absent.** `unity command` defaults to a 30 second timeout.

Generates C# editor scripts to manipulate Unity sprites using ISpriteEditorDataProvider. Works with TextureImporter, PSBImporter, and custom importers.

### Passing C# to `eval`

`eval` compiles a **statement block, not a file**. Two consequences, both of which cause a
compile error rather than a warning:

- **No `using` directives.** The compiler reads `using UnityEngine;` as a resource-disposal
  statement and rejects it (`CS0210`).
- **Types must be fully qualified.** A bare `AssetDatabase` or `Volume` does not resolve
  (`CS0246` / `CS0103`), and a bare `Object` is ambiguous with `object` (`CS0104`).

Where a snippet below is written as a file — with usings, for readability, or because it is
meant to be saved into the project — qualify the types before passing it to `eval`.

## Workflow

All generated scripts must follow the Safe Core Pattern in [references/templates.md](references/templates.md), which includes MANDATORY capability checks. NEVER attempt operations if capability checks fail - this prevents data corruption. After execution, verify results in Unity console and Project window.

## Common Operations

**Modify Name/Rect/Border/Pivot:** Update corresponding `SpriteRect` fields (see scripts/SetPivotExample.cs for pivot examples)
- Requires: `EditSpriteName`, `EditSpriteRect`, `EditBorder`, or `EditPivot`

**Add/Remove/Slice:** Create or filter `SpriteRect` array (see [references/background.md](references/background.md) for Unity 2021.2+ requirements)
- Requires: `CreateAndDeleteSprite`

**Set Outlines:** Get `ISpriteOutlineDataProvider` → Call `SetOutlines()` with GUID + Vector2 arrays

## Important Notes

- Do NOT use AssetPostprocessor or MenuItem patterns
- Generate standalone snippets only — no `AssetPostprocessor`, no `MenuItem`
- **Enum assignments:** Always use enum values and cast to numeric types. Never use raw numbers.
  - ✅ Correct: `(int)SpriteAlignment.Center`
  - ❌ Wrong: `1` (magic number)
