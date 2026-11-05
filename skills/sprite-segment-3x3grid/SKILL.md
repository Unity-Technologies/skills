---
name: sprite-segment-3x3grid
description: Segments a Sprite texture into a 3x3 grid and outputs a pattern of cells matching the center cell's color. Use when asked to analyze, compare, or document a sprite's structure or color distribution.
---
# Sprite Color Grid Analysis

Segment a sprite's texture into a 3x3 grid and output a text pattern showing which cells match the center cell's majority color.

## Important

- Use the unity cli (`unity command --format json`) and the `unity-cli` and `unity-pipeline` skills for each step.
- Don't use `using` directives or unqualified types in the `snippet` for `eval`. The compiler reads using `UnityEngine` and a bare AssetDatabase, Volume, or Object causes errors (CS0246 / CS0103 / CS0104). `return` what you want to read; it arrives at `data.result.result`.
- Create a restore point you can roll back to if your changes fail.
- Do only what's asked. Don't change unrelated assets or files.
- Never leave verification code in the user's script. Never build logging or detection they didn't ask for. See [Final step](#final-step).
- Avoid long explanations.
- This skill reads a sprite and reports a pattern, so don't edit the user's files unless they ask. If they do, create the restore point just before the first edit.

## Step 1

Before you go forward with a workflow or diagnosis:

1. Open and read the [documentation page(s)](#full-topic-map) that cover the specific subject related to what you intend to do or fix and check if you've missed anything.
2. Name the file(s) you read in your response.

This is because your training knowledge about Unity might be out of date, incorrect, or for the wrong Unity version.

## Full topic map

If the Editor install folder has a `Data/Documentation` folder (eg `...Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), read the html files from the `en/Manual` folder.

Otherwise the docs pages are online at `https://docs.unity.com/en-us/engine/<VERSION>/manual/`. Prefer fetching the page directly (for example `WebFetch`) over searching (for example `WebSearch`) — faster and lands on the exact reference. Only fetch what you need, and only search if you don't have the exact URL. Replace `<VERSION>` with the following before fetching:

- **docs.unity3d.com**: The Unity version, eg `6000.6`
- **Package on docs.unity3d.com**: The package version, eg `@6.6` or `@2.0`. Check the package version if unsure.
- **Engine on docs.unity.com**: The Unity version, eg `6000.3`
- **docs.unity.com**: The product version, eg `7000.0`

For these Engine pages, `<VERSION>` is the Unity major.minor from `ProjectSettings/ProjectVersion.txt`, for example `6000.0` from `6000.0.58f1`, because the full build number gives a 404.

Before fetching, check that `<VERSION>` is listed as a `## Version` heading in https://docs.unity.com/en-us/engine/llms.txt. If it isn't, use the docs.unity3d.com Manual instead. The two sites use different page names, so don't reuse the paths below. Instead, find each page by its title in `https://docs.unity3d.com/<VERSION>/Documentation/Manual/docdata/toc.js`, then fetch `https://docs.unity3d.com/<VERSION>/Documentation/Manual/<link>.html`, where `<link>` is the `link` value next to the title.

- [Import a sprite or spritesheet texture](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/import-images-sprites.md) — local `sprite/import-images-sprites/import-images-sprites-landing.html`: setting a texture's type to Sprite (2D and UI).
- [Cut out sprites from a texture](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/use-editor.md) — local `sprite/sprite-editor/use-editor.html`: how the Sprite Editor defines each sprite's rectangle.
- [Sprite asset reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/unity2d/sprite/class-sprite.md) — local `class-Sprite.html`: the properties of a sprite asset in the Inspector.
- [Sprite (2D and UI) texture Import Settings window reference](https://docs.unity.com/en-us/engine/<VERSION>/manual/materials-and-shaders/textures/textures-reference/texture-type-sprite.md) — local `texture-type-sprite.html`: sprite texture import settings, including Read/Write access.

### API reference

The API is `UnityEngine.Sprite` (`rect`, `texture`) and `UnityEngine.Texture2D` (`GetPixel`), part of the Unity Engine. It is versioned by Unity version, so check the project's Unity version before relying on a specific class or method.

1. Use the `unity-cli` skill to check the project's Unity version.
2. If the Editor install folder has a `Data/Documentation` folder (e.g. `.../Unity/Hub/Editor/6000.6.0b2/Editor/Data/Documentation`), grep the html files in the `en/ScriptReference` folder inside, and skip the remaining steps.
3. Otherwise, fetch and cache `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/xrefmap.yml` (replace `<VERSION>` with the Unity version, e.g. `6000.3`). This file lists every public API member's fully qualified name as a `uid` (e.g. `uid: 'UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)'`).
4. Grep the yml file locally for a class or method instead of refetching per lookup. The file is large (about 6 MB), so don't read the whole file.
5. Convert the `uid` to a markdown page: remove the parameters in brackets, lowercase it, replace `.` with `/`, and add `.md`. Then fetch `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/<converted uid>`. For example, `UnityEngine.Rigidbody.AddForce(UnityEngine.Vector3)` => `https://docs.unity.com/en-us/engine/<VERSION>/script-reference/unityengine/rigidbody/addforce.md`. All overloads are on the same page.
   - Constructors use `ctor` (e.g. `unityengine/vector3/ctor.md`), and indexers use `item` (e.g. `unityengine/vector3/item.md`).
   - For operators, fetch the class page (e.g. `unityengine/vector3.md`) and follow its link.
   - Links inside the pages start with `/engine/`. Add `https://docs.unity.com/en-us` before them.
   - If the page doesn't exist for that Unity version, use the `href` in the `xrefmap.yml` entry instead (e.g. `href: 'Rigidbody.AddForce.html'` => `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Rigidbody.AddForce.html`).

On Unity versions earlier than 6000.0, which have no `xrefmap.yml`, skip steps 3 to 5 and fetch the class or member page directly, for example `https://docs.unity3d.com/<VERSION>/Documentation/ScriptReference/Sprite-rect.html`.

Always use the docs for the project's Unity version rather than relying on training knowledge, since APIs are frequently marked obsolete between versions.

## Step 2. Procedure

1. Get the Unity version from `ProjectSettings/ProjectVersion.txt`, the sprite to analyze, and the match threshold. If there's no project or no version, assume Unity 6000.0, say so in your answer, and carry on. If the user gives no threshold, use the default from [Match Threshold](#match-threshold). If the user names no sprite, ask for it.
2. Read the [documentation pages](#full-topic-map) for what you're about to do.
3. Run the [Algorithm](#algorithm) on the sprite, following the [Implementation Guidance](#implementation-guidance). Use [scripts/SpriteGridAnalysis.cs](scripts/SpriteGridAnalysis.cs) as the code template. If you can't read the sprite's pixels (for example, there's no project), say so, explain how you'd calculate each cell, and carry on to the report.
4. Check the result against the [Output Format](#output-format): one line, three groups of three symbols separated by ` / `, with `*` in the center. If it doesn't match, reread the [documentation pages](#full-topic-map) and the [Algorithm](#algorithm), and run the analysis again.
5. If you changed the project or ran code in the Editor, run the checks in the [Final step](#final-step).
6. Report the result as described in the [Final report](#final-report).

## Parameters

### Match Threshold

The **match threshold** determines what percentage of pixels in a non-center cell must match the center's majority color for you to mark that cell as a match (`.`).

| Threshold | Description | Use Case |
|-----------|-------------|----------|
| **50%** | At least half the cell's pixels match center color | Lenient matching for sprites with mixed regions |
| **75%** | At least three-quarters match center color | Moderate matching for mostly solid regions |
| **90%** | Nearly all pixels must match center color | Strict matching for very uniform sprites |

**Default**: 75%

## Algorithm

1. **Segment the Sprite**: Divide the Sprite's texture region into a 3x3 grid (9 cells total)
2. **Identify Center Color**: Calculate the majority (most frequent) color in the center cell (position [1,1])
3. **Compare Each Cell**: For each of the 8 surrounding cells, calculate what percentage of pixels match the center's majority color. If the percentage meets or exceeds the **match threshold**, the cell is a match (`.`); otherwise it is not (`X`)
4. **Generate Output**: Create a text representation using the legend below

## Output Legend

| Symbol | Meaning |
|--------|---------|
| `X` | Cell does NOT meet the match threshold (insufficient pixels match center color) |
| `.` | Cell MEETS the match threshold (enough pixels match center color) |
| `*` | The center cell itself (always position [1,1] in the grid) |

## Output Format

Output a single line with three groups of three characters, separated by ` / `. Each group represents one row of the grid.

**Order**: Top to Bottom rows, Left to Right within each row.

**Format**: `[TopRow] / [MiddleRow] / [BottomRow]`

Each row contains 3 characters separated by spaces: `[Left] [Center] [Right]`

### Grid Position Mapping

```
Grid Layout:        Output Order:
[0,0] [1,0] [2,0]   1  2  3   -> First group
[0,1] [1,1] [2,1]   4  5  6   -> Second group (5 is always *)
[0,2] [1,2] [2,2]   7  8  9   -> Third group
```

### Examples

**Example 1**: Center matches top-left and bottom-right only
```
. X X / X * X / X X .
```
Grid interpretation:
```
.  X  X
X  *  X
X  X  .
```

**Example 2**: Center matches all surrounding cells
```
. . . / . * . / . . .
```

**Example 3**: Center matches none of the surrounding cells
```
X X X / X * X / X X X
```

**Example 4**: Center matches bottom row only
```
X X X / X * X / . . .
```

## Implementation Guidance

### Calculating Center's Majority Color

For the center cell:
1. Sample all pixels within the cell's bounds
2. Group pixels by color (consider using a color tolerance threshold for similar colors)
3. The majority color is the color with the highest pixel count
4. For tie-breaking, use the first color encountered

### Matching Non-Center Cells

For each of the 8 surrounding cells:
1. Count the total number of pixels in the cell
2. Count how many pixels match the center's majority color (using color tolerance)
3. Calculate the match percentage: `matchingPixels / totalPixels`
4. If match percentage >= threshold, cell is a match (`.`); otherwise (`X`)

### Color Tolerance

When comparing pixel colors:
- Consider using a tolerance threshold (e.g., RGB distance < 10) for "matching"
- Alternatively, for sprites with limited palettes, exact matching may be appropriate
- Account for alpha channel if relevant to the use case

### Sprite Bounds

Use the Sprite's `rect` property to determine the pixel region to analyze:
- `sprite.rect.x`, `sprite.rect.y` for the origin
- `sprite.rect.width`, `sprite.rect.height` for dimensions
- Divide width and height by 3 to get cell dimensions

## Notes

- This analysis works best with sprites that have distinct color regions
- For sprites with gradients or many colors, consider increasing color tolerance
- Flip the y-axis from Unity's texture coordinates so the output runs top to bottom
- Treat transparent pixels as a distinct "color", or ignore them, depending on the use case

## Final step

Do the following checks:

1. Check the project has zero console errors. Use `console_status` for counts.
2. Stop there unless the user asked you to prove it works or you suspect a specific fault.

If a check fails, go back and reread the [docs pages](#full-topic-map) to find out what you missed.

## Final report

Give the user a concise but not dense checklist of what you did and why, including any incidental side effects (for example, settings you toggled to test something, or temporary files you created). If the user needs to follow up with more steps or decisions, list them. For example:

```
I analyzed the `Rock_01` sprite with the default 75% match threshold:

. X X / X * X / X X .

[✓] Split the sprite's `rect` into a 3x3 grid.
[✓] Compared each cell with the center cell's majority color.

If you want a stricter or more lenient result, give me a threshold, for example 90% or 50%.
```
