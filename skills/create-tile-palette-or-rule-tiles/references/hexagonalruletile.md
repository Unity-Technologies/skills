# Skill: Advanced Hexagonal RuleTile Creation

This skill provides a template and logic for creating complex `HexagonalRuleTiles`. Use this to programmatically generate or manually configure new Hexagonal RuleTiles for pointy-top hex grids.

## Hexagonal Tile Rule Pattern (Pointy Top)

A Hexagonal RuleTile uses 6 possible neighbor directions. These neighbors are defined as follows:

### Neighbor Directions and Coordinate Mappings

For a tile at `(x, y, 0)`, the 6 neighbors are at:
1.  **North-West (NW)**: `(-1, 1, 0)`
2.  **North (N)**: `(0, 1, 0)`
3.  **East (E)**: `(1, 0, 0)`
4.  **South-West (SW)**: `(-1, -1, 0)`
5.  **South (S)**: `(0, -1, 0)`
6.  **West (W)**: `(-1, 0, 0)`

### Legend:
- `.`: **This** (Type 1)
- `X`: **Any** (Type 0)
- **Fixed**: Rule is applied exactly as defined.
- **MirrorX**: Rule is applied as defined and mirrored across the X-axis.

## Full Tiling Rules Template

The hexagonal tile set uses **MirrorX** and **Fixed** transforms. **Rotated** is not used in this specific set.

| Rule ID | Neighbor Configurations (Pos: Type) | Transform | Description |
|:--------|:------------------------------------| :--- | :--- |
| 0       | NW:., N:., E:., SW:., S:., W:.      | Fixed | Full 6-neighbor surrounded |
| 1       | NW:., W:., SW:., S:., E:.           | MirrorX | 5-neighbor surrounded |
| 2       | NW:., W:., E:., SW:., N:.           | MirrorX | 5-neighbor surrounded variant |
| 3       | N:., NW:., W:., SW:., S:.           | MirrorX | 5-neighbor surrounded variant |
| 4       | W:., SW:., S:., E:.                 | Fixed | 4-neighbor connectivity |
| 5       | W:., NW:., N:., E:.                 | Fixed | 4-neighbor connectivity |
| 6       | NW:., W:., SW:., S:.                | MirrorX | 4-neighbor corner/edge |
| 7       | NW:., W:., SW:., N:.                | MirrorX | 4-neighbor corner/edge |
| 8       | NW:., SW:., W:., E:.                | MirrorX | 4-neighbor bridge |
| 9       | NW:., W:., E:., S:.                 | MirrorX | 4-neighbor connectivity |
| 10      | NW:., SW:., S:., E:.                | MirrorX | 4-neighbor connectivity |
| 11      | NW:., N:., S:., SW:.                | Fixed | 4-neighbor vertical-ish |
| 12      | N:., NW:., W:., S:.                 | Fixed | 4-neighbor horizontal-ish |
| 13      | W:., SW:., S:.                      | MirrorX | 3-neighbor corner |
| 14      | NW:., W:., SW:.                     | MirrorX | 3-neighbor flat edge |
| 15      | W:., NW:., N:.                      | MirrorX | 3-neighbor corner |
| 16      | NW:., N:., S:.                      | MirrorX | 3-neighbor T-junction |
| 17      | W:., SW:., E:.                      | MirrorX | 3-neighbor T-junction |
| 18      | W:., N:., SW:.                      | MirrorX | 3-neighbor wide corner |
| 19      | E:., NW:., SW:.                     | MirrorX | 3-neighbor wide corner |
| 20      | NW:., W:., E:.                      | MirrorX | 3-neighbor wide corner |
| 21      | SW:., S:., NW:.                     | MirrorX | 3-neighbor variant |
| 22      | W:., NW:., S:.                      | MirrorX | 3-neighbor variant |
| 23      | NW:., W:.                           | Fixed | 2-neighbor edge |
| 24      | W:., SW:.                           | Fixed | 2-neighbor edge |
| 25      | SW:., S:.                           | Fixed | 2-neighbor edge |
| 26      | E:., S:.                            | Fixed | 2-neighbor edge |
| 27      | N:., E:.                            | Fixed | 2-neighbor edge |
| 28      | NW:., N:.                           | Fixed | 2-neighbor edge |
| 29      | W:., E:.                            | MirrorX | 2-neighbor opposite bridge |
| 30      | N:., SW:.                           | MirrorX | 2-neighbor long bridge |
| 31      | W:., S:.                            | MirrorX | 2-neighbor acute corner |
| 32      | E:., NW:.                           | MirrorX | 2-neighbor long bridge variant |
| 33      | N:., S:.                            | MirrorX | 2-neighbor vertical bridge |
| 34      | N:.                                 | MirrorX | Single neighbor North |
| 35      | E:.                                 | MirrorX | Single neighbor East |
| 36      | S:.                                 | MirrorX | Single neighbor South |
| 37      | all 6 surrounding positions: X    | Fixed | Default / No-neighbor |

#### Usage
When applying this skill to a new texture set, first ensure the texture is sliced into sprites matching these 38 logical configurations. Then, run a script using the description to map the sprites to each rule. Ensure that HexagonalRuleTile has all 38 TilingRules listed in the template.