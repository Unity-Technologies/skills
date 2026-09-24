# Tilemap RuleTile Create Empty

Use ONLY when the user wants a blank RuleTile, HexagonalRuleTile, or IsometricRuleTile for custom rule configuration AND has not provided or referenced any sprites. If the user mentions existing sprites, terrain art, edge tiles, or a tiles folder, use [ruletile-createfromsegment.md](ruletile-createfromsegment.md) instead, never this.

Requires packages: `com.unity.2d.tilemap` >=1.0.0, `com.unity.2d.tilemap.extras` >=4.0.0

## Workflow

### Step 1: Verify No Sprite Inputs
**WAIT** - Confirm that no Sprites or Spritesheets were specified by the user. This skill is only for empty RuleTiles. If there are Sprites or Spritesheets specified by the user, use [ruletile-createfromsegment.md](ruletile-createfromsegment.md) instead.

### Step 2: Determine RuleTile Type
Identify which RuleTile type to create based on user request:
- **RuleTile**: Standard rectangular grid
- **HexagonalRuleTile**: Hexagonal grid layout
- **IsometricRuleTile**: Isometric grid layout

### Step 3: Create Empty TilingRules
For each TilingRule, ensure the Sprite array has one `null` entry.

## Branching Logic (RuleTile Types)

### Path A: RuleTile
- Use template from [ruletile.md](ruletile.md).

### Path B: HexagonalRuleTile
- Use template from [hexagonalruletile.md](hexagonalruletile.md).

### Path C: IsometricRuleTile
- Create empty rules with appropriate neighbor positions for isometric layout.

## Important Notes

- **TilingRuleOutput.Neighbor.This**: Use to identify RuleTiles that are the same (matching neighbors).
- **TilingRuleOutput.Neighbor.NotThis**: Do NOT use unless explicitly specified by the user to ignore a Tile at a certain position.