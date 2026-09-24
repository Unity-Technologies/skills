---
name: create-tile-palette
description: Create a tile palette from sprites or textures. Make sure to use this skill when the user wants to create tiles for 2D level design, even if they don't explicitly ask about tile palettes or tile assets.
---

# Create a tile palette

Pick the matching reference doc based on what the user wants, then follow it:

- **Tile Palette** — [references/palette-create.md](references/palette-create.md) — when the user wants to organize tiles for 2D level design or create a new rectangular, hexagonal, isometric, or isometric Z as Y grid layout.
- **Blank/empty RuleTile** (no sprites provided) — [references/ruletile-createempty.md](references/ruletile-createempty.md) — when the user wants a blank RuleTile (rectangular grid), HexagonalRuleTile, or IsometricRuleTile for custom rule configuration and has not provided or referenced any sprites.
- **RuleTile built from existing sprites** (sprites, terrain art, edge tiles provided) — [references/ruletile-createfromsegment.md](references/ruletile-createfromsegment.md) — when the user wants tiles that autotile as they paint, or a RuleTile built from existing terrain or edge sprites so tiles connect correctly.
