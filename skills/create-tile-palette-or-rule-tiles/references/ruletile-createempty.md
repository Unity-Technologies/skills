# Tilemap RuleTile Create Empty

Links in this file are relative to this `references/` folder; `..` resolves to the skill directory.

Use ONLY when the user wants a blank RuleTile, HexagonalRuleTile, or IsometricRuleTile for custom rule configuration AND has not provided or referenced any sprites. If the user mentions existing sprites, terrain art, edge tiles, or a tiles folder, use [ruletile-createfromsegment.md](ruletile-createfromsegment.md) instead, never this.

Requires packages: `com.unity.2d.tilemap` >=1.0.0, `com.unity.2d.tilemap.extras` >=4.0.0

## Workflow

### Step 1: Verify No Sprite Inputs
**WAIT** - Confirm that no Sprites or Spritesheets were specified by the user. This skill is only for empty RuleTiles, because it leaves every rule's sprite empty. If there are Sprites or Spritesheets specified by the user, use [ruletile-createfromsegment.md](ruletile-createfromsegment.md) instead.

### Step 2: Determine RuleTile Type
Identify which RuleTile type to create based on user request:
- **RuleTile**: Standard rectangular grid
- **HexagonalRuleTile**: Hexagonal grid layout
- **IsometricRuleTile**: Isometric grid layout

### Step 3: Create Empty TilingRules
For each TilingRule, ensure the Sprite array has one `null` entry, because each rule outputs a single sprite and the user assigns it later.

## Branching Logic (RuleTile Types)

### Path A: RuleTile
- Use the rule pattern from [ruletile.md](ruletile.md). Skip its Usage steps that slice the texture and map sprites, because an empty RuleTile leaves every sprite `null` (Step 3).

### Path B: HexagonalRuleTile
- Use the rule pattern from [hexagonalruletile.md](hexagonalruletile.md). Skip its Usage steps that map sprites, because an empty RuleTile leaves every sprite `null` (Step 3).

### Path C: IsometricRuleTile
- Use the rule pattern from [ruletile.md](ruletile.md), because `IsometricRuleTile` uses the same 8 neighbor positions as `RuleTile`, from `(-1, 1, 0)` to `(1, -1, 0)`. Only the on-screen direction of each position differs. Skip the Usage steps that slice the texture and map sprites, because an empty RuleTile leaves every sprite `null` (Step 3).

## Important Notes

- **TilingRuleOutput.Neighbor.This**: Use to identify RuleTiles that are the same (matching neighbors).
- **TilingRuleOutput.Neighbor.NotThis**: Do NOT use unless explicitly specified by the user to ignore a Tile at a certain position, because a `NotThis` condition makes the rule fail whenever that neighbour is this RuleTile, which the templates don't expect.