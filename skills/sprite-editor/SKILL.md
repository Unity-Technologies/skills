---
name: sprite-editor
description: Edits sprite rectangles, names, borders, pivots, outlines, and slicing in a texture's import data by generating C# Editor scripts. Use when asked to slice a sprite sheet or change sprite pivots, borders, or outlines by script.
required_packages:
  com.unity.2d.sprite: ">=1.0.0"
---

# Sprite Editor

Edit sprite metadata (rects, names, borders, pivots, outlines) through `ISpriteEditorDataProvider` in a connected Unity Editor.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Don't use `using` directives or unqualified types in the `snippet` for `script eval`. The compiler reads using `UnityEngine` and a bare AssetDatabase, Volume, or Object causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

## Step 1

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

## Full topic map

The Sprite Editor Data Provider API docs ship with the 2D Sprite package. The docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.2d.sprite@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. If that folder has no `.md` files, fetch `https://docs.unity3d.com/Packages/com.unity.2d.sprite@<VERSION>/manual/DataProvider.html`, because that website has no `.md` variant.

The Sprite Editor window pages are in the Unity Manual. Check the project's Unity version first, because the docs location depends on it:

- **Unity 6000.0 and later**: If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `<editor-install>/Editor/Data/Documentation/en/Manual` folder. Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/`.
- **Earlier versions**: Read the same local folder if it exists. Otherwise fetch `https://docs.unity3d.com/<VERSION>/Documentation/Manual/SpriteEditor.html` and follow its links, because docs.unity.com has no pages for those versions and the file names differ.

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`
- **docs.unity.com**: The product version, eg `7000.0`

Before fetching, check that `<VERSION>` is listed as a `## Version` heading in https://docs.unity.com/en-us/engine/llms.txt. If it isn't, use the docs.unity3d.com Manual instead. The two sites use different page names, so don't reuse the paths below. Instead, find each page by its title in `https://docs.unity3d.com/<VERSION>/Documentation/Manual/docdata/toc.js`, then fetch `https://docs.unity3d.com/<VERSION>/Documentation/Manual/<link>.html`, where `<link>` is the `link` value next to the title.

For Engine pages, `<VERSION>` is the Unity major.minor from `ProjectSettings/ProjectVersion.txt`, for example `6000.0` from `6000.0.58f1`, because the full build number gives a 404. For the 2D Sprite package, `<VERSION>` is the `com.unity.2d.sprite` major.minor from `Packages/packages-lock.json`, for example `1.0`.

- **Sprite Editor Data Provider API** — `Documentation~/DataProvider.md` in `PackageCache`: code examples for editing sprite data through the providers.
- [Cut out sprites from a texture](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/use-editor.md) — local `sprite/sprite-editor/use-editor.html`: slicing a texture into sprites in the Sprite Editor window.
- [Sprite Editor tab reference for the Sprite Editor window](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/sprite-editor-window-reference/reference.md) — local `sprite/sprite-editor/sprite-editor-window-reference.html`: slice types, pivot, border, and sprite rect settings.
- [Custom Outline tab reference for the Sprite Editor window](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/sprite-editor-window-reference/custom-outline-editor-reference.md) — local `sprite/sprite-editor/custom-outline-editor-reference.html`: editing the outline Unity uses to build the sprite mesh.
- [Custom Physics Shape tab reference for the Sprite Editor window](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/sprite-editor-window-reference/custom-physics-shape-editor-reference.md) — local `sprite/sprite-editor/custom-physics-shape-editor-reference.html`: editing the physics shape used by 2D colliders.
- [9-slice a sprite](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/9-slice/set-sprite-9slicing.md) — local `sprite/9-slice/set-sprite-9slicing.html`: setting borders that divide a sprite into nine sections.

### API reference

For the provider interfaces, capabilities, and `SpriteRect` fields this skill uses, read [references/api_reference.md](references/api_reference.md).

The API is `UnityEditor.U2D.Sprites`, part of the `com.unity.2d.sprite` package. It is versioned by package version, not Unity version (e.g. `@1.0`), so check the project's installed package version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the installed package version.
2. Fetch and cache `https://docs.unity3d.com/Packages/com.unity.2d.sprite@<VERSION>/xrefmap.yml` (replace `<VERSION>` with the installed package version, e.g. `1.0`). This file maps every public API member's fully qualified name to its docs page `href` (e.g. `href: api/UnityEditor.U2D.Sprites.ISpriteEditorDataProvider.html` => `https://docs.unity3d.com/Packages/com.unity.2d.sprite@<VERSION>/api/UnityEditor.U2D.Sprites.ISpriteEditorDataProvider.html`).
3. Grep the yml file locally for a class or method instead of refetching per lookup. Don't read the whole file.

Always fetch the version matching the installed package rather than relying on training knowledge, since APIs are frequently marked obsolete between major versions.

The importer APIs (`UnityEditor.AssetImporter`, `UnityEditor.TextureImporter`, `UnityEngine.SpriteAlignment`) are part of the Unity Engine. They change between Unity versions, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)` => `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/rigidbody/addforce.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector3/ctor.md`), and indexers use `item` (e.g. `unityengine/vector3/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector3.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Rigidbody.AddForce.html'` => `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Rigidbody.AddForce.html`).

Before Unity 6000.0 there's no `xrefmap.yml`, so skip steps 3 to 5 and fetch the class page directly, for example `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/TextureImporter.html`.

Always use the docs for the project's Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

## Use the Editor

Sprite metadata (rects, borders, pivots, outlines) lives inside the importer, not in a file
you can edit — reaching it means running C# through a live Editor.

**The `unity-cli` skill owns getting you there** — installing the CLI, confirming a connected
Editor, adding the project's `com.unity.pipeline` package, telling a genuinely absent Editor
apart from one stuck in Safe Mode, and discovering the Editor's command catalog. Follow it
first; don't re-derive any of it here.

Two things it can't know for you:

- **You need `script eval` in particular**, not just a reachable Editor. Confirm it appears in the
  catalog — its presence depends on the Pipeline package version, not on the CLI.
- **Never hand-edit a `.meta` file to change sprite metadata.** The importer owns that data
  and the capability checks below exist to prevent corruption, so an unreachable Editor is a
  stop, not a cue to improvise.

Run C# through the connected Editor with the `script eval` command. Discover its parameter shape
from `unity command --format json` rather than assuming one — the inline form is
`unity command script eval --code '<snippet>'`, and some Pipeline versions also register
`script eval_file` for running a snippet from a file. **Check the catalog before reaching for
`script eval_file`; it is frequently absent.** `script eval`'s default timeout depends on the Pipeline version, so read it from the command's schema (`unity command --detail full --format json`) and pass `--timeout <seconds>` for a slow call.

Generate C# editor scripts that manipulate sprites through ISpriteEditorDataProvider. They work with TextureImporter, PSBImporter, and custom importers.

### Passing C# to `script eval`

`script eval` compiles a **statement block, not a file**. Two consequences, both of which cause a
compile error rather than a warning:

- **No `using` directives.** The compiler reads `using UnityEngine;` as a resource-disposal
  statement and rejects it (`CS0210`).
- **Types must be fully qualified.** A bare `AssetDatabase` or `Volume` does not resolve
  (`CS0246` / `CS0103`), and a bare `Object` is ambiguous with `object` (`CS0104`).

Where a snippet below is written as a file — with usings, for readability, or because it is
meant to be saved into the project — qualify the types before passing it to `script eval`.

## Workflow

All generated scripts must follow the Safe Core Pattern in [references/templates.md](references/templates.md), which includes MANDATORY capability checks. NEVER attempt operations if capability checks fail - this prevents data corruption. After execution, verify results in Unity console and Project window.

## Common Operations

**Modify Name/Rect/Border/Pivot:** Update corresponding `SpriteRect` fields (see scripts/SetPivotExample.cs for pivot examples)
- Requires: `EditSpriteName`, `EditSpriteRect`, `EditBorder`, or `EditPivot`

**Add/Remove/Slice:** Create or filter `SpriteRect` array (see [references/background.md](references/background.md) for version-specific requirements)
- Requires: `CreateAndDeleteSprite`

**Set Outlines:** Get `ISpriteOutlineDataProvider` → Call `SetOutlines()` with GUID + Vector2 arrays

## Important Notes

- Do NOT use AssetPostprocessor or MenuItem patterns
- Generate standalone snippets only — no `AssetPostprocessor`, no `MenuItem`
- **Enum assignments:** Always use enum values and cast to numeric types. Never use raw numbers.
  - ✅ Correct: `(int)SpriteAlignment.Center`
  - ❌ Wrong: `1` (magic number)

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.
3. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `script eval` rather than adding logging. Take no screenshots as they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Give the user a concise but not dense checklist of what you did and why, including any incidental side effects (for example, settings you toggled to test something, or temporary files you created). If the user needs to follow up with more steps or decisions, list them. For example:

```
I made the following changes:

[✓] Set the pivot of the `Player_Idle` sprite in `Assets/Sprites/PlayerSheet.png` to Center.

Follow these steps to see the effect:

1. Select `PlayerSheet.png` in the Project window.
2. Open the Sprite Editor window and select `Player_Idle` to check its pivot.
```
