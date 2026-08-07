---
name: tilemap-ruletile-createfromsegment
description: Create Unity RuleTile TilingRules from sprite-segment-3x3grid output patterns. Converts 3x3 grid text patterns (X, ., *) into TilingRule neighbor configurations, mapping '.' to 'This' rules and 'X' to 'DontCare'. Rules are sorted by specificity (more 'This' rules first). Use when creating RuleTiles from sprite analysis or defining tile neighbor rules programmatically. Sprites must be provided as input.
required_packages:
  com.unity.2d.tilemap: ">=1.0.0"
  com.unity.2d.tilemap.extras: ">=4.0.0"
---

# Tilemap RuleTile Create From Segment

## Purpose

This skill creates Unity RuleTile TilingRules by first analyzing sprites using the `sprite-segment-3x3grid` skill, then converting the resulting text patterns into neighbor rule configurations. The user must specify Sprites or a Spritesheet input.

## Workflow

```
Step 1: sprite-segment-3x3grid    Step 2: Create TilingRules
+--------------------------+      +--------------------------+
|  Analyze each Sprite     |      |  Parse text patterns     |
|  using color matching    |  ->  |  Map to neighbor rules   |
|  Output: text pattern    |      |  Sort by specificity     |
+--------------------------+      +--------------------------+
```

**CRITICAL**: Always run the `sprite-segment-3x3grid` skill first on each Sprite to generate the text pattern.

## Step 1: Apply sprite-segment-3x3grid (Required)

For each Sprite in your texture, analyze it with `sprite-segment-3x3grid`:

```csharp
string pattern = SpriteSegment3x3Grid.AnalyzeSpriteGrid(sprite, matchThreshold: 0.75f);
```

### Pattern Output Format

Single-line text pattern: `[TopRow] / [MiddleRow] / [BottomRow]`

Each row contains 3 characters separated by spaces:
- `X` - Cell does NOT meet match threshold
- `.` - Cell MEETS match threshold (matches center color)
- `*` - Center cell (the tile itself)

Example: `X X X / X * X / . . .`

## Step 2: Create TilingRules from Patterns

### Symbol to Rule Mapping

| Symbol | RuleTile Neighbor | Value | Behavior                                      |
|--------|-------------------|-------|-----------------------------------------------|
| `.`    | `This`            | 1     | Neighbor must be an instance of this RuleTile |
| `X`    | `DontCare`        | 0     | Position not added to neighbors list          |
| `*`    | (center)          | N/A   | Ignored - represents the tile itself          |

**Note**: "DontCare" is not an explicit constant. A position is "don't care" when it is simply not included in the `m_Neighbors` and `m_NeighborPositions` lists.

### Neighbor Position Mapping

```
Grid Layout:              Unity Vector3Int positions:
[0] [1] [2]  (Top)        (-1,1,0)  (0,1,0)  (1,1,0)
[3] [*] [5]  (Middle)     (-1,0,0)    ---    (1,0,0)
[6] [7] [8]  (Bottom)     (-1,-1,0) (0,-1,0) (1,-1,0)
```

| Index | Position     | Vector3Int     |
|-------|--------------|----------------|
| 0     | Top-Left     | `(-1, 1, 0)`  |
| 1     | Top          | `(0, 1, 0)`   |
| 2     | Top-Right    | `(1, 1, 0)`   |
| 3     | Left         | `(-1, 0, 0)`  |
| 4     | Center       | (skipped)      |
| 5     | Right        | `(1, 0, 0)`   |
| 6     | Bottom-Left  | `(-1, -1, 0)` |
| 7     | Bottom       | `(0, -1, 0)`  |
| 8     | Bottom-Right | `(1, -1, 0)`  |

## Pattern Filtering

By default, detected patterns that do not match any entry in the Common Tile Patterns table are discarded. Only the 47 known patterns are kept. This prevents unexpected or noisy rules from appearing in the final RuleTile.

If the user explicitly asks to keep non-standard patterns, set `filterToKnownPatterns: false` to bypass this filtering.

## Deduplication

Duplicate TilingRules with identical neighbor configurations are automatically removed. When multiple sprites produce the same pattern, only the first sprite encountered is kept.

## Rule Sorting

Rules are sorted by specificity (number of `This` rules) in descending order. More specific rules are evaluated first.

| This Count | Example Pattern                 | Priority |
|------------|---------------------------------|----------|
| 8          | `. . . / . * . / . . .`        | First    |
| 5          | `. . . / . * . / X X X`        | ...      |
| 3          | `X X X / X * X / . . .`        | ...      |
| 0          | `X X X / X * X / X X X`        | Last     |

## Sprite Assignment

Each TilingRule is assigned at most 1 Sprite:
- Set `m_Output` to `TilingRuleOutput.OutputSprite.Single`
- Assign the sprite to `m_Sprites[0]`

The RuleTile's `m_DefaultSprite` is set to the sprite from the bottom-most (least specific) rule. Since rules are sorted by specificity descending, this is the last rule in the list. This sprite is used when no TilingRule matches.

## Algorithm Summary

1. **For each Sprite**: Run `sprite-segment-3x3grid` and store the pattern
2. **Filter**: Discard patterns not in Common Tile Patterns (unless user opts out)
3. **Deduplicate**: Remove duplicate patterns (first sprite wins)
4. **Parse**: Split by ` / ` for rows, then by space for cells
5. **Extract Neighbors**: `.` -> add position with `This` (1); `X` -> skip
6. **Create TilingRule**: Populate `m_Neighbors` and `m_NeighborPositions`
7. **Assign Sprite**: Set single sprite output
8. **Sort**: Order by specificity descending
9. **Apply**: Add sorted rules to RuleTile asset
10. **Default Sprite**: Set `m_DefaultSprite` to the last (least specific) rule's sprite

## Scripts

Implementation and usage examples are in the `scripts/` folder:

| File | Description |
|------|-------------|
| [`TilemapRuleTileCreateFromSegment.cs`](scripts/TilemapRuleTileCreateFromSegment.cs) | Core implementation with `CreateRuleTileFromSprites`, `CreateTilingRuleFromPattern`, `ParsePattern`, and `ApplyRulesToTile` |
| [`RuleTileGenerator.cs`](scripts/RuleTileGenerator.cs) | Editor window example (`Tools > Generate RuleTile from Sprites`) |
| [`ManualWorkflowExample.cs`](scripts/ManualWorkflowExample.cs) | Manual two-step workflow for finer control over analysis |

### Quick Usage

```csharp
// Get sprites and create rules in one call
var rules = TilemapRuleTileCreateFromSegment.CreateRuleTileFromSprites(
    sprites, matchThreshold: 0.75f, colorTolerance: 0.04f);

// Apply to RuleTile
var ruleTile = ScriptableObject.CreateInstance<RuleTile>();
TilemapRuleTileCreateFromSegment.ApplyRulesToTile(ruleTile, rules);
```

## Common Tile Patterns

| Tile Type                                            | Pattern                          | This Count |
|------------------------------------------------------|----------------------------------|------------|
| Fully surrounded (inner tile)                        | `. . . / . * . / . . .`          | 8          |
| Top left corner missing                              | `X . . / . * . / . . .`          | 7          |
| Top right corner missing                             | `. . X / . * . / . . .`          | 7          |
| Bottom left corner missing                           | `. . . / . * . / X . .`          | 7          |
| Bottom right corner missing                          | `. . . / . * . / . . X`          | 7          |
| Top corners missing                                  | `X . X / . * . / . . .`          | 6          |
| Bottom corners missing                               | `. . . / . * . / X . X`          | 6          |
| Left corners missing                                 | `X . . / . * . / X . .`          | 6          |
| Right corners missing                                | `. . X / . * . / . . X`          | 6          |
| Top Left and Bottom Right diagonal corners missing   | `X . . / . * . / . . X`          | 6          |
| Top Right and Bottom Left diagonal corners missing   | `. . X / . * . / X . .`          | 6          |
| Top Left, Top Right and Bottom Left corners missing  | `X . X / . * . / X . .`          | 5          |
| Top Left, Top Right and Bottom Right corners missing | `X . X / . * . / . . X`          | 5          |
| Top Left, Bottom Left and Bottom Right corners missing | `X . . / . * . / X . X`        | 5          |
| Top Right, Bottom Left and Bottom Right corners missing | `. . X / . * . / X . X`       | 5          |
| Cross Section                                        | `X . X / . * . / X . X`          | 4          |
| Flat edge (Bottom)                                   | `X X X / . * . / . . .`          | 5          |
| Flat edge (Top)                                      | `. . . / . * . / X X X`          | 5          |
| Flat edge (Left)                                     | `. . X / . * X / . . X`          | 5          |
| Flat edge (Right)                                    | `X . . / X * . / X . .`          | 5          |
| L-Shape, Point Right                                 | `X X X / . * . / . . X`          | 4          |
| L-Shape, Point Bottom                                | `. . X / . * X / X . X`          | 3          |
| L-Shape, Point Left                                  | `X . . / . * . / X X X`          | 4          |
| L-Shape, Point Top                                   | `X . X / X * . / X . .`          | 3          |
| L-Shape Inverse, Point Left                          | `X X X / . * . / X . .`          | 4          |
| L-Shape Inverse, Point Bottom                        | `X . . / X * . / X . X`          | 3          |
| L-Shape Inverse, Point Right                         | `. . X / . * . / X X X`          | 4          |
| L-Shape Inverse, Point Top                           | `X . X / . * . / . . X`          | 5          |
| T-Shape, face down                                   | `X X X / . * . / X . X`          | 3          |
| T-Shape, face top                                    | `X . X / . * . / X X X`          | 3          |
| T-Shape, face left                                   | `X . X / . * X / X . X`          | 3          |
| T-Shape, face right                                  | `X . X / X * . / X . X`          | 3          |
| Horizontal bridge                                    | `X X X / . * . / X X X`          | 2          |
| Vertical bridge                                      | `X . X / X * X / X . X`          | 2          |
| Corner piece, Bottom Right                           | `X X X / X * . / X . .`          | 3          |
| Corner piece, Bottom Left                            | `X X X / . * X / . . X`          | 3          |
| Corner piece, Top Left                               | `. . X / . * X / X X X`          | 3          |
| Corner piece, Top Right                              | `X . . / X * . / X X X`          | 3          |
| Edge end, Bottom Right                               | `X X X / X * . / X . X`          | 2          |
| Edge end, Bottom Left                                | `X X X / . * X / X . X`          | 2          |
| Edge end, Top Left                                   | `X . X / . * X / X X X`          | 2          |
| Edge end, Top Right                                  | `X . X / X * . / X X X`          | 2          |
| Single isolated tile, Left                           | `X X X / . * X / X X X`          | 1          |
| Single isolated tile, Right                          | `X X X / X * . / X X X`          | 1          |
| Single isolated tile, Top                            | `X . X / X * X / X X X`          | 1          |
| Single isolated tile, Bottom                         | `X X X / X * X / X . X`          | 1          |
| Center                                               | `X X X / X * X / X X X`          | 0          |

## Notes

- Always run `sprite-segment-3x3grid` first -- this skill depends on its output
- Rules are evaluated in order; first matching rule wins
- A rule with 0 `This` conditions matches any configuration (use as fallback)
- The center cell (`*`) is always ignored in neighbor calculations
- Unity's RuleTile supports up to 8 neighbors in a standard 3x3 grid
- For hexagonal or isometric grids, use `HexagonalRuleTile` or `IsometricRuleTile`

## Prerequisites

- **`sprite-segment-3x3grid` skill**: generates the input patterns. It ships in this
  same plugin as `sprite-segment-3x3grid`, so it is always available — invoke
  it rather than treating it as an unmet dependency.
- **com.unity.2d.tilemap.extras package**: Required for RuleTile class

## See Also

- `sprite-segment-3x3grid` - required prerequisite; generates the input patterns
- `Packages/com.unity.2d.tilemap.extras/Runtime/Tiles/RuleTile/RuleTile.cs` for RuleTile implementation
- Unity Manual: [Rule Tile](https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@latest)
