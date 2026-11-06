---
name: create-tile-palette-or-rule-tiles
description: Creates a 2D tile palette from sprites or textures, or makes tiles change based on their neighbours. Use when the user wants a tile palette or tiles that connect to their neighbours as they paint, even when they don't name them.
required_packages:
  com.unity.2d.tilemap: ">=1.0.0"
  com.unity.2d.tilemap.extras: ">=4.0.0"
---

# Create tile palettes and RuleTiles

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Before making changes, establish a baseline of the current behavior so you can tell whether a later result is actually caused by your change.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`

Read the Unity version from `ProjectSettings/ProjectVersion.txt`. For package docs, use the package's major.minor from `Packages/packages-lock.json`, eg `1.0` for `com.unity.2d.tilemap` or `6.0` for `com.unity.2d.tilemap.extras`.

## Full topic map

Engine Manual pages: if the Editor install folder has a `Data/Documentation` folder (eg `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder. Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/tilemaps.md`.

- **Create a tile palette** — local `en/Manual/tilemaps/tiles-for-tilemaps/create-tile-assets.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/tilemaps/create-tile-palette/assets.md` — the palette asset, its settings sub-asset, and adding tiles.
- **Tile Palette window reference** — local `en/Manual/tilemaps/tile-palettes/tile-palette-editor-reference.html`, online `https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/tilemaps/create-tile-palette/tile-palette-editor-reference.md` — Cell Size, Sort Mode and Sort Axis settings.

Package manual pages: the docs pages are local markdown conversions of the Unity Manual, stored at `<project-folder>/Library/PackageCache/com.unity.2d.tilemap.extras@*/`. Read only the .md files in that specific `PackageCache` subfolder. You must not look at non-.md files, or look outside at other Library or PackageCache folders, otherwise it takes too much time. If that folder has no `.md` files, use the online pages at `https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@<VERSION>/manual/`, because some packages don't ship their docs.

- **Rule Tile** — `Documentation~/RuleTile.md` — how Rule Tiles match neighbours, rule order, and the Default Sprite.

### API reference

The APIs are `UnityEditor.Tilemaps` (`GridPaletteUtility`) in the `com.unity.2d.tilemap` package, `RuleTile`, `HexagonalRuleTile` and `IsometricRuleTile` in the `com.unity.2d.tilemap.extras` package, and `AssetDatabase` in the Unity Engine. Package APIs are versioned by package version and Engine APIs by Unity version, so check both before relying on a specific class or method, because APIs are frequently marked obsolete between versions.

1. Use the `unity-cli` skill to check the project's Unity version and installed package versions.
2. For package APIs, fetch and cache `https://docs.unity3d.com/Packages/<package-name>@<version>/xrefmap.yml` (eg `com.unity.2d.tilemap.extras@6.0`). It maps each public member's fully qualified name to its docs page `href` (eg `href: api/UnityEngine.RuleTile.TilingRule.html` => `https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@6.0/api/UnityEngine.RuleTile.TilingRule.html`). Grep it locally instead of refetching per lookup; don't read the whole file. Start with `GridPaletteUtility.CreateNewPalette`, `RuleTile.TilingRule` (`m_Neighbors`, `m_NeighborPositions`, `m_RuleTransform`) and `RuleTile.TilingRuleOutput.Neighbor` (`This`, `NotThis`).
3. For Engine APIs, if the Editor install folder has a `Data/Documentation` folder, grep the html files in its `en/ScriptReference` folder and skip the rest of this step. Otherwise fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (about 6 MB, so grep it, don't read it) and convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, add `.md`, and fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEditor.AssetDatabase.LoadAllAssetsAtPath(System.String)` => `https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/assetdatabase/loadallassetsatpath.md`. If that page doesn't exist, use the entry's `href` under `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/`.

## Glossary

The reference files write RuleTile rules as 3x3 text grids, read from the top row to the bottom row:

| Symbol | Meaning | In the API | In the Rule Tile Inspector |
|--------|---------|------------|----------------------------|
| `.` | The neighbour must be this RuleTile | `RuleTile.TilingRuleOutput.Neighbor.This` | Green arrow |
| `X` | Any neighbour (don't care) | Position left out of `m_Neighbors` and `m_NeighborPositions` | Empty cell |
| `*` | The tile itself | Not stored | Centre cell |

Don't read `X` as `NotThis`, because the Inspector draws `NotThis` as a red X, which means the opposite: the neighbour must not be this RuleTile.

## Workflow

Pick the matching reference doc based on what the user wants, then follow it:

- **Tile Palette** — [references/palette-create.md](references/palette-create.md) — when the user wants to organize tiles for 2D level design or create a new rectangular, hexagonal, isometric, or isometric Z as Y grid layout.
- **Blank/empty RuleTile** (no sprites provided) — [references/ruletile-createempty.md](references/ruletile-createempty.md) — when the user wants a blank RuleTile (rectangular grid), HexagonalRuleTile, or IsometricRuleTile for custom rule configuration and has not provided or referenced any sprites.
- **RuleTile built from existing sprites** (sprites, terrain art, edge tiles provided) — [references/ruletile-createfromsegment.md](references/ruletile-createfromsegment.md) — when the user wants tiles that autotile as they paint, or a RuleTile built from existing terrain or edge sprites so tiles connect correctly.

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console_status` for counts.
2. Read the assets back with `eval` (fully qualified), because an asset can exist without its settings having stuck:
   - If you created a tile palette, check that `AssetDatabase.LoadAllAssetsAtPath` on the palette returns a `GridPalette` sub-asset, and that the `Grid` on the palette's root GameObject has the `cellLayout` and `cellSize` you chose.
   - If you created a RuleTile, check that `AssetDatabase.LoadAssetAtPath` with the RuleTile type you created returns the asset, that `m_TilingRules.Count` equals the number of rules you created, and that each rule's `m_Sprites` has exactly one entry: `null` for an empty RuleTile, or the assigned sprite for one built from sprites.
3. If you built a RuleTile from sprites, check that it picks the right sprite as it's painted, because correct fields don't prove the rules match the art. Open a temporary scene with `EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive)`, add a Grid with a Tilemap, use `Tilemap.SetTile` to paint a 3x3 block of the RuleTile, and check that `Tilemap.GetSprite` at each cell returns the sprite of the rule that matches that cell's neighbours. For example, the centre cell gets the fully surrounded rule's sprite, if the RuleTile has that rule. Then close the scene with `EditorSceneManager.CloseScene(scene, true)` without saving it.
4. If a value doesn't match, fix the asset and repeat the check.
5. Stop there unless the user asked you to prove it works or you suspect a specific fault. If you do enter Play mode, do it once only to chase a fault you already suspect. Read the world through `eval` rather than adding logging. Take no screenshots, because they take too much time.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Report the following, in order:

1. What changed: each asset you created, with its path and type, and the settings you used (grid type, cell size, sort axis, or number of tiling rules).
2. Side effects: any other file or scene you created or changed, such as an editor script copied from `scripts/` into `Assets`, so the user can remove it.
3. The verification results from the final step.
4. Follow-up the user must do, such as assigning sprites to an empty RuleTile or adding the RuleTile to a tile palette.
