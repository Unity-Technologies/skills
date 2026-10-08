# Skill: Advanced RuleTile Creation

This skill provides a template and logic for creating complex `RuleTiles`. Use this guide to programmatically generate or manually configure new RuleTiles that follow consistent connectivity patterns.

## Rule Tile Rule Pattern

The following rules define the connectivity for a standard 2D top-down layout. Each rule checks for the presence (.) or absence (X) of a tile in its 3x3 neighborhood. Depending on the Spritesheets used, the Rule Tile can use Fixed Rule Transforms or Rotated Rule Transforms. Use Rotated Rule Transforms to reduce the number of Tiling Rules used.

## Legend
- `.`: **This** (Tile must be present)
- `X`: **Any** (Does not matter)
- `*`: The tile itself (Center)

### Tiling Rules Template for Fixed Rule Tiles

| Rule ID | Neighbors (Top to Bottom, Left to Right) | Rule Transform | Description                                             |
|:--------|:-----------------------------------------|:---------------|:--------------------------------------------------------|
| 0       | `. . .` / `. * .` / `. . .`              | Fixed          | Fully surrounded (inner tile)                           |
| 1       | `X . .` / `. * .` / `. . .`              | Fixed          | Top left corner missing                                 |
| 2       | `. . X` / `. * .` / `. . .`              | Fixed          | Top right corner missing                                |
| 3       | `. . .` / `. * .` / `X . .`              | Fixed          | Bottom left corner missing                              |
| 4       | `. . .` / `. * .` / `. . X`              | Fixed          | Bottom right corner missing                             |
| 5       | `X . X` / `. * .` / `. . .`              | Fixed          | Top corners missing                                     |
| 6       | `. . .` / `. * .` / `X . X`              | Fixed          | Bottom corners missing                                  |
| 7       | `X . .` / `. * .` / `X . .`              | Fixed          | Left corners missing                                    |
| 8       | `. . X` / `. * .` / `. . X`              | Fixed          | Right corners missing                                   |
| 9       | `X . .` / `. * .` / `. . X`              | Fixed          | Top Left and Bottom Right diagonal corners missing      |
| 10      | `. . X` / `. * .` / `X . .`              | Fixed          | Top Right and Bottom Left diagonal corners missing      |
| 11      | `X . X` / `. * .` / `X . .`              | Fixed          | Top Left, Top Right and Bottom Left corners missing     |
| 12      | `X . X` / `. * .` / `. . X`              | Fixed          | Top Left, Top Right and Bottom Right corners missing    |
| 13      | `X . .` / `. * .` / `X . X`              | Fixed          | Top Left, Bottom Left and Bottom Right corners missing  |
| 14      | `. . X` / `. * .` / `X . X`              | Fixed          | Top Right, Bottom Left and Bottom Right corners missing |
| 15      | `X . X` / `. * .` / `X . X`              | Fixed          | Cross Section                                           |
| 16      | `X X X` / `. * .` / `. . .`              | Fixed          | Flat edge (Bottom)                                      |
| 17      | `. . .` / `. * .` / `X X X`              | Fixed          | Flat edge (Top)                                         |
| 18      | `. . X` / `. * X` / `. . X`              | Fixed          | Flat edge (Left)                                        |
| 19      | `X . .` / `X * .` / `X . .`              | Fixed          | Flat edge (Right)                                       |
| 20      | `X X X` / `. * .` / `. . X`              | Fixed          | L-Shape, Point Right                                    |
| 21      | `. . X` / `. * X` / `X . X`              | Fixed          | L-Shape, Point Bottom                                   |
| 22      | `X . .` / `. * .` / `X X X`              | Fixed          | L-Shape, Point Left                                     |
| 23      | `X . X` / `X * .` / `X . .`              | Fixed          | L-Shape, Point Top                                      |
| 24      | `X X X` / `. * .` / `X . .`              | Fixed          | L-Shape Inverse, Point Left                             |
| 25      | `X . .` / `X * .` / `X . X`              | Fixed          | L-Shape Inverse, Point Bottom                           |
| 26      | `. . X` / `. * .` / `X X X`              | Fixed          | L-Shape Inverse, Point Right                            |
| 27      | `X . X` / `. * .` / `. . X`              | Fixed          | L-Shape Inverse, Point Top                              |
| 28      | `X X X` / `. * .` / `X . X`              | Fixed          | T-Shape, face down                                      |
| 29      | `X . X` / `. * .` / `X X X`              | Fixed          | T-Shape, face top                                       |
| 30      | `X . X` / `. * X` / `X . X`              | Fixed          | T-Shape, face left                                      |
| 31      | `X . X` / `X * .` / `X . X`              | Fixed          | T-Shape, face right                                     |
| 32      | `X X X` / `. * .` / `X X X`              | Fixed          | Horizontal bridge                                       |
| 33      | `X . X` / `X * X` / `X . X`              | Fixed          | Vertical bridge                                         |
| 34      | `X X X` / `X * .` / `X . .`              | Fixed          | Corner piece, Bottom Right                              |
| 35      | `X X X` / `. * X` / `. . X`              | Fixed          | Corner piece, Bottom Left                               |
| 36      | `. . X` / `. * X` / `X X X`              | Fixed          | Corner piece, Top Left                                  |
| 37      | `X . .` / `X * .` / `X X X`              | Fixed          | Corner piece, Top Right                                 |
| 38      | `X X X` / `X * .` / `X . X`              | Fixed          | Edge end, Bottom Right                                  |
| 39      | `X X X` / `. * X` / `X . X`              | Fixed          | Edge end, Bottom Left                                   |
| 40      | `X . X` / `. * X` / `X X X`              | Fixed          | Edge end, Top Left                                      |
| 41      | `X . X` / `X * .` / `X X X`              | Fixed          | Edge end, Top Right                                     |
| 42      | `X X X` / `. * X` / `X X X`              | Fixed          | Single isolated tile, Left                              |
| 43      | `X X X` / `X * .` / `X X X`              | Fixed          | Single isolated tile, Right                             |
| 44      | `X . X` / `X * X` / `X X X`              | Fixed          | Single isolated tile, Top                               |
| 45      | `X X X` / `X * X` / `X . X`              | Fixed          | Single isolated tile, Bottom                            |
| 46      | `X X X` / `X * X` / `X X X`              | Fixed          | Center                                                  |

#### Usage
When applying this skill to a new texture set, first ensure the texture is sliced into sprites matching these 47 logical configurations. Then, run a script using the description to map the sprites to each rule. Ensure that RuleTile has all 47 TilingRules listed in the template.

### Tiling Rules Template for Rotated Rule Tiles

| Rule ID | Neighbors (Top to Bottom, Left to Right) | Rule Transform | Description                   |
|:--------|:-----------------------------------------|:---------------|:------------------------------|
| 0       | `. . .` / `. * .` / `. . .`              | Fixed          | Fully surrounded (inner tile) |
| 1       | `X . .` / `. * .` / `. . .`              | Rotated        | One corner missing            |
| 2       | `X . X` / `. * .` / `. . .`              | Rotated        | Two corners missing           |
| 3       | `X . .` / `. * .` / `. . X`              | Rotated        | Two diagonal corners missing  |
| 4       | `X . X` / `. * .` / `. . X`              | Rotated        | Three corners missing         |
| 5       | `X . X` / `. * .` / `X . X`              | Fixed          | Cross Section                 |
| 6       | `X X X` / `. * .` / `. . .`              | Rotated        | Flat edge                     |
| 7       | `X X X` / `. * .` / `. . X`              | Rotated        | L-Shape                       |
| 8       | `X X X` / `. * .` / `X . .`              | Rotated        | L-Shape Inverse               |
| 9       | `X X X` / `. * .` / `X . X`              | Rotated        | Isolated edge                 |
| 10      | `X X X` / `. * .` / `X X X`              | Rotated        | Vertical/Horizontal bridge    |
| 11      | `X X X` / `X * .` / `X . .`              | Rotated        | Corner piece                  |
| 12      | `X X X` / `X * .` / `X . X`              | Rotated        | Edge end                      |
| 13      | `X X X` / `. * X` / `X X X`              | Rotated        | Single isolated tile          |
| 14      | `X X X` / `X * X` / `X X X`              | Fixed          | Center                        |

#### Usage
When applying this skill to a new texture set, first ensure the texture is sliced into sprites matching these 15 logical configurations. Then, run a script using the description to map the sprites to each rule. Ensure that RuleTile has all 15 TilingRules listed in the template.
