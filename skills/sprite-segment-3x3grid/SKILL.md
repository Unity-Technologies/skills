---
name: sprite-segment-3x3grid
description: Analyze Sprite textures and output a 3x3 grid representation based on color matching. Segments a Sprite into a 3x3 grid, identifies the majority color of the center cell, and outputs a text pattern showing which cells match the center color. Use when analyzing sprite patterns, documenting sprite structure, or describing sprite color distribution.
---
# Sprite Color Grid Analysis

## Purpose

This skill analyzes Sprite textures by segmenting them into a 3x3 grid and outputting a text representation based on color matching with the center cell.

## Parameters

### Match Threshold

The **match threshold** determines what percentage of pixels in a non-center cell must match the center's majority color for that cell to be considered a "match" (`.`).

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

The output is a single line with three groups of three characters, separated by ` / `. Each group represents one row of the grid.

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

## References

Code Template: "scripts/SpriteGridAnalysis.cs"

## Use Cases

- **Documentation**: Describe sprite patterns in a compact text format
- **Testing**: Verify expected sprite structure in automated tests
- **Analysis**: Quickly identify sprites with similar color distributions
- **Debugging**: Understand why sprites look different than expected
- **Asset Cataloging**: Generate searchable metadata for sprite assets

## Notes

- This analysis works best with sprites that have distinct color regions
- For sprites with gradients or many colors, consider increasing color tolerance
- The Y-axis is flipped from Unity's texture coordinates to produce top-to-bottom output
- Transparent pixels can be treated as a distinct "color" or ignored based on use case
